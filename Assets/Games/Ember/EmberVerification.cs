using System;
using UnityEngine;

// Uses the same directional movement, attack, dodge, potion and skill commands as the player.
// All runs use fresh records. No save slots are used by this simulation.
public static class EmberVerification
{
    const float Step = .04f;
    public static EmberRecord Run()
    {
        CheckRetreatAndDefeat();
        var run = new EmberJourney(new EmberRecord());
        int frames = 0, elixirs = 0, relicCount = 0, dodges = 0, camps = 0;
        for (int expedition = 0; expedition < 6; expedition++)
        {
            int index = Mathf.Min(4, expedition);
            Check(run.CanEnter(index) && run.Enter(index), "district accessible " + index);
            int spent = 0;
            bool warned = false, awakened = false, reinforcements = false;
            for (int frame = 0; frame < 6000 && run.S.mode != EmberMode.Result; frame++)
            {
                if (run.S.mode == EmberMode.Sanctum)
                {
                    run = VisitSanctum(run, camps++ % 3);
                    continue;
                }
                if (run.S.mode == EmberMode.Relic)
                {
                    run = RoundTrip(run);
                    Check(run.S.offers[0] != run.S.offers[1] && run.S.offers[1] != run.S.offers[2] &&
                        run.S.offers[0] != run.S.offers[2], "distinct relic choices");
                    int pick = 0;
                    for (int i = 0; i < 3; i++) if (run.S.offers[i] == 0 || run.S.offers[i] == 3) pick = i;
                    int offered = run.S.offers[pick];
                    Check(run.Choose(pick) && run.S.relics.Contains(offered) && !run.Choose(pick), "relic chosen once");
                    relicCount++; continue;
                }
                int flasks = run.S.flasks;
                var input = Driver(run);
                run.Step(Step, input); frames++;
                if (run.S.flasks < flasks) spent++;
                if (run.S.dodge > .98f) dodges++;
                if (run.S.wave > 1) reinforcements = true;
                if (run.S.enemies.Exists(e => e.kind == 4 && e.awakened)) awakened = true;
                if (!warned && run.S.dangers.Count > 0)
                {
                    var d = run.S.dangers[0]; int foes = run.S.enemies.Count, hp = run.S.hp;
                    run = RoundTrip(run);
                    Check(run.S.dangers.Count > 0 && run.S.dangers[0].id == d.id &&
                        Mathf.Abs(run.S.dangers[0].remaining - d.remaining) < .001f && run.S.enemies.Count == foes &&
                        run.S.hp == hp, "ongoing telegraph survives resume");
                    warned = true;
                }
            }
            Check(warned && run.S.mode == EmberMode.Result && run.S.outcome == EmberOutcome.Victory,
                $"expedition {expedition + 1} cleared (hp {run.S.hp}, room {run.S.room}, kills {run.S.kills})");
            Check(run.S.kills >= 7 && run.S.lastTakings > 0 && run.S.satchel == 0, "full run banked");
            Check(awakened && reinforcements, "guardian second phase and room reinforcements");
            elixirs += spent;
            run = RoundTrip(run);
            int bank = run.S.wallet;
            Check(!run.ConfirmReturn() && run.S.wallet == bank, "result cannot pay twice");
            Check(run.S.cleared == Mathf.Min(4, expedition + 1), "district progression");
            if (expedition >= 4)
                Check(run.S.bestCycle == expedition - 3 && run.S.district == expedition - 4, "deep record and rotation");
            run.Home();
            if (expedition == 0) { Buy(run, 0); Buy(run, 1); }
            if (expedition == 1) { Buy(run, 2); Buy(run, 0); }
            if (expedition == 2) { Buy(run, 1); Buy(run, 0); }
            if (expedition == 3) Buy(run, 2);
        }
        Check(run.S.wins == 6 && relicCount == 12 && camps == 12 && dodges > 0, "four guardians and two deep expeditions");
        Debug.Log($"Ember verification passed: four districts, two deep cycles, {run.S.totalKills} enemies, " +
            $"{relicCount} relic choices, {camps} sanctuaries, {dodges} dodges, {elixirs} healing flasks and {frames * Step:0.0} simulated combat seconds.");
        return run.S;
    }

    static void Buy(EmberJourney run, int i)
    {
        int before = run.S.wallet, cost = run.Cost(i), level = run.S.upgrades[i];
        Check(run.Upgrade(i) && run.S.wallet == before - cost && run.S.upgrades[i] == level + 1, "forge upgrade " + i);
    }
    static EmberJourney RoundTrip(EmberJourney run) =>
        new EmberJourney(JsonUtility.FromJson<EmberRecord>(JsonUtility.ToJson(run.S)));

    static EmberJourney VisitSanctum(EmberJourney run, int route)
    {
        void Walk(Vector2 target)
        {
            for (int frame = 0; frame < 150 && Vector2.Distance(run.S.hero, target) > .3f; frame++)
                run.Step(Step, new EmberInput { move = (target - run.S.hero).normalized });
            Check(Vector2.Distance(run.S.hero, target) < .4f, "walk to sanctuary site");
        }
        run.RequestReturn(); run.Back();
        Check(run.S.mode == EmberMode.Sanctum, "continue exploring after canceling retreat");
        if (route == 1)
        {
            Walk(EmberJourney.Chest);
            int carried = run.S.satchel;
            Check(run.UseSite() == 2 && run.S.satchel == carried + 5, "collect cache");
            run = RoundTrip(run);
            Walk(EmberJourney.Altar);
            Check(run.S.restChoice == 2 && run.UseSite() == 0, "one optional prize, persisted");
        }
        else if (route == 2)
        {
            Walk(EmberJourney.Altar);
            int hp = run.S.hp;
            if (hp < run.MaxHp)
                Check(run.UseSite() == 1 && run.S.hp == Mathf.Min(run.MaxHp, hp + (run.MaxHp + 1) / 2), "genuine recovery");
            else Check(run.UseSite() == 0 && run.S.restChoice == 0, "full HP preserves choice");
        }
        Walk(EmberJourney.Door);
        Check(run.UseSite() == 3 && run.S.mode == EmberMode.Relic, "gate accepts detour and direct routes");
        return run;
    }

    static void CheckRetreatAndDefeat()
    {
        var run = new EmberJourney(new EmberRecord());
        Check(!run.CanEnter(1) && !run.Enter(4) && !run.Upgrade(0) && run.Enter(0), "initial gates");
        for (int i = 0; i < 1500 && run.S.kills == 0; i++) run.Step(Step, Driver(run));
        int satchel = run.S.satchel;
        Check(satchel > 0, "materials from field combat");
        run.RequestReturn(); run = RoundTrip(run); run.Back();
        Check(run.S.mode == EmberMode.Fight && run.S.satchel == satchel, "cancel return after reload");
        run.RequestReturn();
        Check(run.ConfirmReturn() && run.S.outcome == EmberOutcome.Return && run.S.wallet == satchel && !run.ConfirmReturn(), "safe early return");
        Check(run.Retry(), "retry same mission");
        for (int i = 0; i < 1500 && run.S.kills == 0; i++) run.Step(Step, Driver(run));
        int carried = run.S.satchel, bank = run.S.wallet;
        for (int i = 0; i < 5000 && run.S.mode == EmberMode.Fight; i++) run.Step(Step, default);
        Check(run.S.mode == EmberMode.Result && run.S.outcome == EmberOutcome.Defeat &&
            run.S.wallet == bank + (carried + 1) / 2, "defeat payout");
        run.Home();
        Check(run.S.hp == run.MaxHp && run.S.flasks == run.FlaskMax && !run.CanEnter(1), "recover in hub");

        // Dodge through an attack on its impact frame, then verify normal damage and healing at the same location.
        run.Enter(0);
        var source = run.S.enemies[0];
        void Warning()
        {
            run.S.dangers.Add(new EmberDanger { id = run.S.nextId++, owner = source.id, from = run.S.hero, to = run.S.hero,
                radius = 2, remaining = .01f, duration = .7f, damage = 6 });
        }
        for (int i = 0; i < 18; i++) run.Step(Step, default);
        Warning(); int hp = run.S.hp;
        run.Step(Step, new EmberInput { dodge = true, move = Vector2.right });
        Check(run.S.hp == hp && run.S.dodge > 0 && run.S.stamina < 100, "invulnerable dodge");
        for (int i = 0; i < 11; i++) run.Step(Step, default);
        Warning(); run.Step(Step, default);
        Check(run.S.hp < hp, "unguarded damage");
        int flasks = run.S.flasks; hp = run.S.hp;
        run.Step(Step, new EmberInput { heal = true });
        Check(run.S.hp > hp && run.S.flasks == flasks - 1, "heal with flask");
    }
    static EmberInput Driver(EmberJourney run)
    {
        var s = run.S;
        EmberFoe closest = null; float distance = float.MaxValue;
        foreach (var foe in s.enemies)
        {
            float d = Vector2.Distance(s.hero, foe.pos);
            if (d < distance) { closest = foe; distance = d; }
        }
        Vector2 move = closest != null && distance > 1.55f ? (closest.pos - s.hero).normalized : Vector2.zero;
        bool dodge = false;
        foreach (var danger in s.dangers)
        {
            if (danger.remaining > .34f || EmberJourney.DistanceToDanger(s.hero, danger) > danger.radius + .3f) continue;
            Vector2 away = s.hero - danger.to;
            if ((danger.to - danger.from).sqrMagnitude > .25f)
            {
                Vector2 path = (danger.to - danger.from).normalized;
                away = new Vector2(-path.y, path.x);
            }
            if (away.sqrMagnitude < .05f) away = Vector2.right;
            if (Mathf.Abs(s.hero.x + away.normalized.x * 2) > 8.5f || Mathf.Abs(s.hero.y + away.normalized.y * 2) > 5)
                away = -s.hero;
            move = away.normalized; dodge = s.dashWait <= 0; break;
        }
        return new EmberInput { move = move, dodge = dodge, attack = distance <= 2.4f,
            pulse = distance < 3.55f && s.skillWait <= 0, heal = s.hp < run.MaxHp * .48f };
    }
    static void Check(bool ok, string label)
    {
        if (!ok) throw new InvalidOperationException("Ember gameplay verification failed: " + label);
    }
}
