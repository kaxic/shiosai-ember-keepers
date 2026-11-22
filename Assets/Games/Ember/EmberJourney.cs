using System;
using System.Collections.Generic;
using UnityEngine;

public enum EmberMode { Hub, Districts, Forge, Fight, Relic, Retreat, Result, Sanctum }
public enum EmberOutcome { Victory, Defeat, Return }

[Serializable]
public sealed class EmberFoe
{
    public int id, kind, hp, maxHp, pattern;
    public Vector2 pos, face = Vector2.down;
    public float hurt, wind, windDuration, cooldown;
    public bool awakened;
}

[Serializable]
public sealed class EmberDanger
{
    public int id, owner, damage;
    public Vector2 from, to;
    public float radius, duration, remaining;
    public bool rush;
    public float progress => Mathf.Clamp01(1 - remaining / Mathf.Max(.01f, duration));
}

// One serializable snapshot includes active attack warnings, consumables, room rewards and the bank.
[Serializable]
public sealed class EmberRecord
{
    public int version = 1, wallet, cleared, bestCycle, wins, voyages, totalKills;
    public int[] upgrades = new int[3];
    public EmberMode mode;
    public EmberOutcome outcome;
    public Vector2 hero = new Vector2(0, -1.8f), facing = Vector2.up, dashDir;
    public int district, room = 1, cycle, hp = 44, flasks = 2, satchel, kills, lastTakings, nextId = 1;
    public float stamina = 100, swing, dodge, pulse, hurt, invulnerable, attackWait, dashWait, skillWait, motion;
    public bool retreatFromReward, retreatFromSanctum;
    public int wave = 1, restChoice;
    public float waveDelay;
    public List<int> relics = new List<int>();
    public int[] offers = new int[3];
    public List<EmberFoe> enemies = new List<EmberFoe>();
    public List<EmberDanger> dangers = new List<EmberDanger>();
}

public struct EmberInput
{
    public Vector2 move;
    public bool attack, dodge, pulse, heal;
}

public sealed class EmberJourney
{
    public static readonly string[] Districts = { "灰灯の坑道", "翠根の聖堂", "星硝子の水路", "暁鐘の炉心" };
    public static readonly string[] Notes =
    {
        "門衛の斬撃を避け、消えた坑道の種火を取り戻す。", "根に覆われた回廊。俊敏な影狼と連続突進に注意。",
        "水晶の足場を渡り、遠くから降る灯弾をかいくぐる。", "四つの灯を結ぶ最奥の炉。交差する予兆を読み切ろう。"
    };
    public static readonly string[] Bosses = { "灰角の門衛", "根冠の狩主", "星硝子の司祭", "暁鐘の鍛神" };
    public static readonly string[] Tools = { "灯剣", "守火の外套", "薬びん" };
    public static readonly string[] ToolNotes = { "斬撃の威力 +2。衝撃波も強くなる。", "最大体力 +8。", "探索開始時の回復びん +1。回復量も増える。" };
    public static readonly string[] Relics = { "火打ちの欠片", "若木の心臓", "風渡りの紐", "鳴火の紋", "宵咲きの種", "旅薬師の鈴", "煤染めの護符", "残り火の靴" };
    public static readonly string[] RelicNotes =
    {
        "この探索中、斬撃の威力 +3。", "最大体力 +10。受け取ると体力を10回復。",
        "回避の再使用が0.35秒早くなり、消費が5減る。", "衝撃波の威力 +6、再使用が2秒早くなる。",
        "魔物を倒すたびに体力を2回復。", "薬びんを1本補充し、携帯数も1増える。",
        "受けるダメージを2軽減。", "歩行速度 +18%。スタミナ回復も速くなる。"
    };
    static readonly Vector2[] SpawnPoints =
    {
        new Vector2(-4.6f, 2.3f), new Vector2(3.6f, 2.9f), new Vector2(.1f, 4.25f),
        new Vector2(6, -.3f), new Vector2(-6, -.55f)
    };
    static readonly int[][] Encounters =
    {
        new[] {0, 1, 0}, new[] {0, 2, 1, 0},
        new[] {1, 0, 1}, new[] {1, 3, 2, 1},
        new[] {2, 0, 2}, new[] {2, 3, 1, 2},
        new[] {3, 1, 2, 0}, new[] {3, 2, 1, 0, 2}
    };
    static readonly int[] BaseHealth = { 18, 13, 14, 26, 82 };
    static readonly float[] Speeds = { 1.85f, 2.9f, 1.7f, 1.2f, 1.55f };
    public static readonly Vector2 Altar = new Vector2(-4, 1.1f), Chest = new Vector2(4, 1.1f), Door = new Vector2(0, 4.25f);
    public const float UseRadius = 1.9f;
    public EmberRecord S { get; }
    public int Chime { get; private set; } = -1;
    public int MaxHp => 44 + 8 * S.upgrades[1] + 10 * Relic(1);
    public int AttackPower => 5 + 2 * S.upgrades[0] + 3 * Relic(0);
    public int FlaskMax => 2 + S.upgrades[2] + Relic(5);
    public int WaveMax => S.room == 3 || (S.district == 0 && S.room == 1 && S.cycle == 0) ? 1 : 2;
    public bool AtHome => S.mode == EmberMode.Hub || S.mode == EmberMode.Forge || S.mode == EmberMode.Districts;

    public EmberJourney(EmberRecord record)
    {
        S = record ?? new EmberRecord();
        if (S.upgrades == null || S.upgrades.Length != 3) Array.Resize(ref S.upgrades, 3);
        for (int i = 0; i < 3; i++) S.upgrades[i] = Mathf.Clamp(S.upgrades[i], 0, 4);
        S.relics ??= new List<int>(); S.enemies ??= new List<EmberFoe>(); S.dangers ??= new List<EmberDanger>();
        if (S.offers == null || S.offers.Length != 3) S.offers = new[] {0, 1, 2};
        S.relics.RemoveAll(i => i < 0 || i >= Relics.Length);
        for (int i = 0; i < 3; i++) S.offers[i] = Mathf.Clamp(S.offers[i], 0, Relics.Length - 1);
        S.enemies.RemoveAll(e => e == null || e.kind < 0 || e.kind >= BaseHealth.Length || e.hp <= 0);
        S.dangers.RemoveAll(d => d == null || d.duration <= 0 || d.remaining <= 0);
        S.cleared = Mathf.Clamp(S.cleared, 0, 4); S.district = Mathf.Clamp(S.district, 0, 3);
        S.room = Mathf.Clamp(S.room, 1, 3); S.wallet = Mathf.Max(0, S.wallet);
        S.wave = Mathf.Clamp(S.wave, 1, WaveMax);
        S.cycle = Mathf.Max(0, S.cycle); S.bestCycle = Mathf.Max(0, S.bestCycle);
        S.hp = Mathf.Clamp(S.hp, 0, MaxHp); S.stamina = Mathf.Clamp(S.stamina, 0, 100);
        if (!Enum.IsDefined(typeof(EmberMode), S.mode)) S.mode = EmberMode.Hub;
        if (AtHome) { S.hp = MaxHp; S.flasks = FlaskMax; }
        if (S.mode == EmberMode.Fight && S.hp <= 0) Finish(EmberOutcome.Defeat);
    }

    int Relic(int id) => S.relics.Contains(id) ? 1 : 0;
    public int Cost(int i)
    {
        if (i < 0 || i >= 3 || S.upgrades[i] >= 4) return 0;
        int level = S.upgrades[i];
        return 7 + level * 7 + (i == 0 ? 2 : 0);
    }
    public bool CanUpgrade(int i) => AtHome && Cost(i) > 0 && S.wallet >= Cost(i);
    public bool Upgrade(int i)
    {
        if (!CanUpgrade(i)) return false;
        S.wallet -= Cost(i); S.upgrades[i]++; S.hp = MaxHp; S.flasks = FlaskMax;
        return true;
    }
    public bool CanEnter(int i) => i >= 0 && i <= 4 && (i == 4 ? S.cleared == 4 : i <= S.cleared);
    public bool Enter(int index)
    {
        if (!AtHome || !CanEnter(index)) return false;
        int depth = index == 4 ? S.bestCycle + 1 : 0;
        Start(index == 4 ? (depth - 1) % 4 : index, depth); return true;
    }
    public bool Retry()
    {
        if (S.mode != EmberMode.Result) return false;
        Start(S.district, S.cycle); return true;
    }
    void Start(int district, int cycle)
    {
        S.voyages++; S.district = district; S.cycle = cycle; S.room = 1; S.kills = S.satchel = 0;
        S.relics.Clear(); S.hp = MaxHp; S.flasks = FlaskMax;
        SpawnRoom();
    }
    void SpawnRoom()
    {
        S.wave = 1; S.waveDelay = .85f; S.restChoice = 0;
        S.mode = EmberMode.Fight; S.hero = new Vector2(0, -3.2f); S.facing = Vector2.up;
        S.enemies.Clear(); S.dangers.Clear(); S.stamina = 100;
        S.swing = S.dodge = S.pulse = S.hurt = S.motion = S.attackWait = S.dashWait = S.skillWait = 0;
        S.invulnerable = .6f;
        if (S.room < 3)
        {
            var group = Encounters[S.district * 2 + S.room - 1];
            for (int i = 0; i < group.Length; i++) Spawn(group[i], SpawnPoints[i], .65f + i * .35f);
            return;
        }
        Spawn(4, new Vector2(0, 2.4f), 1.05f);
        if (S.district > 0 || S.cycle > 0) Spawn(S.district == 2 ? 2 : 0, SpawnPoints[0], 1.9f);
        if (S.district == 3 || S.cycle > 1) Spawn(1, SpawnPoints[1], 2.5f);
    }
    void Spawn(int kind, Vector2 pos, float cooldown)
    {
        int tier = S.district + Mathf.Min(16, S.cycle);
        int hp = BaseHealth[kind] + tier * (kind == 4 ? 15 : 3);
        if (Vector2.Distance(S.hero, pos) < 2.5f) pos = -pos;
        S.enemies.Add(new EmberFoe { id = S.nextId++, kind = kind, hp = hp, maxHp = hp, pos = pos, cooldown = cooldown });
    }
    void Reinforce()
    {
        S.wave++;
        Spawn((S.district + S.room) % 4, SpawnPoints[3], 1.15f);
        Spawn(S.room == 1 ? 0 : 1, SpawnPoints[4], 1.65f);
        if (S.district >= 2 || S.cycle > 0) Spawn(2, SpawnPoints[2], 2f);
        Chime = 1;
    }
    public void Home()
    {
        S.mode = EmberMode.Hub; S.hero = new Vector2(0, -1.8f); S.facing = Vector2.up;
        S.enemies.Clear(); S.dangers.Clear(); S.relics.Clear(); S.hp = MaxHp; S.flasks = FlaskMax; S.stamina = 100;
        S.swing = S.dodge = S.pulse = S.hurt = S.motion = 0;
    }
    public void RequestReturn()
    {
        if (S.mode != EmberMode.Fight && S.mode != EmberMode.Relic && S.mode != EmberMode.Sanctum) return;
        S.retreatFromReward = S.mode == EmberMode.Relic;
        S.retreatFromSanctum = S.mode == EmberMode.Sanctum; S.mode = EmberMode.Retreat;
    }
    public void Back()
    {
        if (S.mode == EmberMode.Retreat) S.mode = S.retreatFromReward ? EmberMode.Relic :
            S.retreatFromSanctum ? EmberMode.Sanctum : EmberMode.Fight;
        if (S.mode == EmberMode.Forge || S.mode == EmberMode.Districts) S.mode = EmberMode.Hub;
    }
    public bool ConfirmReturn()
    {
        if (S.mode != EmberMode.Retreat) return false;
        Finish(EmberOutcome.Return); return true;
    }
    public bool Choose(int i)
    {
        if (S.mode != EmberMode.Relic || i < 0 || i >= S.offers.Length) return false;
        int relic = S.offers[i]; S.relics.Add(relic);
        if (relic == 1) S.hp = Mathf.Min(MaxHp, S.hp + 10);
        if (relic == 5) S.flasks = Mathf.Min(FlaskMax, S.flasks + 1);
        S.room++; SpawnRoom(); return true;
    }
    void ClearRoom()
    {
        S.dangers.Clear(); S.motion = 0;
        if (S.room == 3) { Finish(EmberOutcome.Victory); return; }
        S.hp = Mathf.Min(MaxHp, S.hp + 3); S.mode = EmberMode.Sanctum;
        S.stamina = 100; S.swing = S.dodge = S.pulse = S.hurt = 0;
        var eligible = new List<int>();
        for (int i = 0; i < Relics.Length; i++) if (!S.relics.Contains(i)) eligible.Add(i);
        var random = new System.Random(unchecked(S.voyages * 7477 + S.district * 113 + S.room * 59 + S.cycle * 31));
        for (int i = 0; i < 3; i++)
        {
            int pick = random.Next(eligible.Count); S.offers[i] = eligible[pick]; eligible.RemoveAt(pick);
        }
        Chime = 4;
    }
    // 1 = altar, 2 = chest, 3 = exit. The player may go straight to the exit.
    public int NearbySite()
    {
        if (S.mode != EmberMode.Sanctum) return 0;
        if (Vector2.Distance(S.hero, Door) <= UseRadius) return 3;
        if (Vector2.Distance(S.hero, Altar) <= UseRadius) return 1;
        return Vector2.Distance(S.hero, Chest) <= UseRadius ? 2 : 0;
    }
    public int UseSite()
    {
        int site = NearbySite();
        if (site == 3) { S.mode = EmberMode.Relic; S.motion = 0; return 3; }
        if (S.restChoice != 0 || site == 0) return 0;
        if (site == 1 && S.hp >= MaxHp) return 0;
        S.restChoice = site;
        if (site == 1) S.hp = Mathf.Min(MaxHp, S.hp + (MaxHp + 1) / 2);
        else S.satchel += 5;
        return site;
    }
    void Finish(EmberOutcome outcome)
    {
        if (S.mode == EmberMode.Result) return;
        S.outcome = outcome;
        if (outcome == EmberOutcome.Victory)
        {
            S.satchel += 8 + 3 * S.district + Mathf.Min(12, S.cycle);
            S.wins++; S.cleared = Mathf.Max(S.cleared, S.district + 1);
            S.bestCycle = Mathf.Max(S.bestCycle, S.cycle);
        }
        S.lastTakings = outcome == EmberOutcome.Defeat ? (S.satchel + 1) / 2 : S.satchel;
        S.wallet = (int)Math.Min(int.MaxValue, (long)S.wallet + S.lastTakings);
        S.satchel = 0; S.mode = EmberMode.Result; S.motion = 0; S.dangers.Clear();
        Chime = outcome == EmberOutcome.Defeat ? 2 : 4;
    }

    public void Step(float seconds, EmberInput input)
    {
        Chime = -1;
        float dt = Mathf.Clamp(seconds, 0, .065f);
        Vector2 move = Vector2.ClampMagnitude(input.move, 1);
        if (S.mode == EmberMode.Hub || S.mode == EmberMode.Sanctum)
        {
            S.hero = Clamp(S.hero + move * (5.15f * dt)); S.motion = move.magnitude;
            if (move.sqrMagnitude > .01f) S.facing = move.normalized;
            return;
        }
        if (S.mode != EmberMode.Fight) { S.motion = 0; return; }
        S.attackWait = Down(S.attackWait, dt); S.dashWait = Down(S.dashWait, dt); S.skillWait = Down(S.skillWait, dt);
        S.invulnerable = Down(S.invulnerable, dt); S.swing = Down(S.swing, dt * 4.4f);
        S.hurt = Down(S.hurt, dt * 3.8f); S.pulse = Down(S.pulse, dt * 2.6f); S.dodge = Down(S.dodge, dt * 3.9f);
        S.stamina = Mathf.Min(100, S.stamina + (24 + 8 * Relic(7)) * dt);
        if (input.heal && S.flasks > 0 && S.hp < MaxHp)
        { S.flasks--; S.hp = Mathf.Min(MaxHp, S.hp + 19 + S.upgrades[2] * 3); Chime = 3; }
        if (input.dodge && S.dashWait <= 0 && S.stamina >= 20 - 5 * Relic(2))
        {
            S.dashDir = move.sqrMagnitude > .01f ? move.normalized : S.facing;
            S.stamina -= 20 - 5 * Relic(2); S.dashWait = 1.25f - .35f * Relic(2);
            S.dodge = 1; S.invulnerable = Mathf.Max(.4f, S.invulnerable); Chime = 1;
        }
        if (S.dodge > 0) { S.hero = Clamp(S.hero + S.dashDir * (11 * dt)); S.facing = S.dashDir; }
        else
        {
            S.hero = Clamp(S.hero + move * ((5.1f + .9f * Relic(7)) * dt));
            if (move.sqrMagnitude > .01f && S.swing < .5f) S.facing = move.normalized;
        }
        S.motion = S.dodge > 0 ? 1 : move.magnitude;
        if (input.pulse && S.skillWait <= 0)
        {
            S.skillWait = 7 - 2 * Relic(3); S.pulse = 1; Chime = 3;
            for (int i = S.enemies.Count - 1; i >= 0; i--)
                if (Vector2.Distance(S.enemies[i].pos, S.hero) <= 3.8f) Hit(i, AttackPower + 3 + 6 * Relic(3));
        }
        if (input.attack && S.attackWait <= 0 && S.stamina >= 5 && S.dodge <= 0) Slash();
        if (S.enemies.Count == 0)
        {
            if (S.wave < WaveMax)
            {
                S.waveDelay -= dt;
                if (S.waveDelay <= 0) Reinforce();
            }
            else ClearRoom();
            return;
        }
        foreach (var foe in S.enemies) MoveFoe(foe, dt);
        UpdateDangers(dt);
    }
    static float Down(float n, float amount) => Mathf.Max(0, n - amount);
    static Vector2 Clamp(Vector2 p) => new Vector2(Mathf.Clamp(p.x, -8.65f, 8.65f), Mathf.Clamp(p.y, -5.15f, 5.15f));
    void Slash()
    {
        EmberFoe nearest = null; float reach = 2.45f;
        foreach (var foe in S.enemies)
        {
            float distance = Vector2.Distance(foe.pos, S.hero);
            if (distance > reach) continue;
            reach = distance; nearest = foe;
        }
        if (nearest != null && reach > .015f) S.facing = (nearest.pos - S.hero).normalized;
        S.stamina -= 5; S.attackWait = .36f; S.swing = 1; Chime = 0;
        for (int i = S.enemies.Count - 1; i >= 0; i--)
        {
            Vector2 away = S.enemies[i].pos - S.hero;
            if (away.magnitude <= 2.45f && (away.magnitude < .62f || Vector2.Dot(S.facing, away.normalized) >= .05f))
                Hit(i, AttackPower);
        }
    }
    void Hit(int i, int damage)
    {
        var foe = S.enemies[i]; foe.hp -= damage; foe.hurt = 1;
        if (foe.hp > 0)
        {
            if (foe.kind == 4 && !foe.awakened && foe.hp <= foe.maxHp / 2)
            {
                foe.awakened = true;
                // The guardian's awakened phase introduces a themed escort and an immediate, visible warning.
                Spawn(S.district, SpawnPoints[foe.pattern % SpawnPoints.Length], 1.5f);
                Warn(foe);
            }
            return;
        }
        S.enemies.RemoveAt(i); S.dangers.RemoveAll(d => d.owner == foe.id);
        S.kills++; S.totalKills++; S.satchel += foe.kind == 4 ? 6 : 2;
        S.hp = Mathf.Min(MaxHp, S.hp + 2 * Relic(4));
    }
    void MoveFoe(EmberFoe foe, float dt)
    {
        foe.hurt = Down(foe.hurt, dt * 4.8f);
        if (foe.wind > 0) { foe.wind = Down(foe.wind, dt / Mathf.Max(.01f, foe.windDuration)); return; }
        foe.cooldown = Down(foe.cooldown, dt);
        Vector2 delta = S.hero - foe.pos;
        float distance = delta.magnitude;
        if (distance > .02f) foe.face = delta / distance;
        float reach = foe.kind switch { 0 => 2f, 1 => 4.5f, 2 => 7f, 3 => 2.45f, _ => 6.3f };
        if (foe.cooldown <= 0 && distance <= reach) { Warn(foe); return; }
        float stop = foe.kind == 2 ? 4.35f : foe.kind == 4 ? 1.55f : .95f;
        Vector2 velocity = distance > stop ? foe.face : Vector2.zero;
        if (foe.kind == 2 && distance < 2.8f) velocity = -foe.face * .75f;
        foreach (var other in S.enemies)
        {
            if (other == foe) continue;
            Vector2 separation = foe.pos - other.pos;
            float d = separation.magnitude;
            if (d > .01f && d < .95f) velocity += separation.normalized * ((.95f - d) / .95f);
        }
        foe.pos = Clamp(foe.pos + Vector2.ClampMagnitude(velocity, 1) * (Speeds[foe.kind] * dt));
    }
    void Warn(EmberFoe foe)
    {
        int hit = 4 + S.district + Mathf.Min(10, S.cycle);
        float delay = foe.kind == 4 ? (foe.awakened ? .88f : 1.15f) : foe.kind == 0 ? .72f : .93f;
        void Area(Vector2 center, float radius, int damage, float extra = 0) =>
            Danger(foe, center, center, radius, damage, delay + extra, false);
        void Rush(float width) => Danger(foe, foe.pos, Clamp(S.hero + foe.face * .55f), width, hit + 1, delay, true);
        switch (foe.kind)
        {
            case 0: Area(foe.pos + foe.face * .72f, 1.5f, hit); break;
            case 1: Rush(.74f); break;
            case 2: Area(S.hero, 1.28f, hit); break;
            case 3: Area(foe.pos, 2.65f, hit + 2); break;
            case 4:
                bool alternate = foe.pattern++ % 2 != 0;
                if (S.district == 0)
                {
                    if (alternate) Rush(1.05f);
                    else { Area(foe.pos, 2.7f, hit + 2); Area(S.hero, 1.22f, hit, .35f); }
                }
                else if (S.district == 1)
                {
                    if (alternate) Rush(1.1f);
                    else { Area(S.hero, 1.6f, hit); Area(Clamp(S.hero + foe.face * 2), 1.5f, hit, .35f); }
                }
                else if (S.district == 2)
                {
                    if (alternate) Area(foe.pos, 3.2f, hit + 2);
                    else for (int j = -1; j <= 1; j++) Area(Clamp(S.hero + new Vector2(j * 2.25f, 0)), 1.32f, hit, .12f * (j + 1));
                }
                else if (alternate) { Rush(1.3f); Area(S.hero, 2.4f, hit + 2, .52f); }
                else
                {
                    Danger(foe, Clamp(S.hero + Vector2.left * 4), Clamp(S.hero + Vector2.right * 4), .82f, hit, delay, false);
                    Danger(foe, Clamp(S.hero + Vector2.down * 4), Clamp(S.hero + Vector2.up * 4), .82f, hit, delay + .15f, false);
                }
                break;
        }
        foe.wind = 1; foe.windDuration = delay;
        foe.cooldown = foe.kind == 4 ? (foe.awakened ? .82f : 1.2f) : foe.kind == 2 ? 1.8f : 1.28f;
    }
    void Danger(EmberFoe owner, Vector2 start, Vector2 end, float radius, int damage, float delay, bool rush)
    {
        S.dangers.Add(new EmberDanger { id = S.nextId++, owner = owner.id, from = start, to = end,
            radius = radius, duration = delay, remaining = delay, damage = damage, rush = rush });
    }
    public static float DistanceToDanger(Vector2 p, EmberDanger d)
    {
        Vector2 path = d.to - d.from;
        float along = path.sqrMagnitude < .001f ? 0 : Mathf.Clamp01(Vector2.Dot(p - d.from, path) / path.sqrMagnitude);
        return Vector2.Distance(p, d.from + along * path);
    }
    void UpdateDangers(float dt)
    {
        for (int i = S.dangers.Count - 1; i >= 0; i--)
        {
            var danger = S.dangers[i]; danger.remaining -= dt;
            if (danger.remaining > 0) continue;
            S.dangers.RemoveAt(i);
            if (danger.rush)
            {
                var foe = S.enemies.Find(e => e.id == danger.owner);
                if (foe != null) foe.pos = danger.to;
            }
            if (S.invulnerable > 0 || DistanceToDanger(S.hero, danger) > danger.radius + .22f) continue;
            S.hp = Mathf.Max(0, S.hp - Mathf.Max(1, danger.damage - 2 * Relic(6)));
            S.hurt = 1; S.invulnerable = .65f; Chime = 2;
            if (S.hp == 0) { Finish(EmberOutcome.Defeat); return; }
        }
    }
}
