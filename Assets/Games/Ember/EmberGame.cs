using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EmberGame : StudioGame
{
    static readonly Vector2 Portal = new Vector2(5.5f, 1.3f), Forge = new Vector2(-5.5f, 1.3f);
    public static string[] DistrictNames => EmberJourney.Districts;
    public static string[] DistrictNotes => EmberJourney.Notes;
    public static string[] UpgradeNames => EmberJourney.Tools;
    public static string[] UpgradeNotes => EmberJourney.ToolNotes;
    EmberJourney journey;
    EmberView view;
    EmberHud hud;
    float checkpointWait;
    EmberRecord S => journey.S;
    public EmberMode Mode => S.mode;
    public EmberOutcome Outcome => S.outcome;
    public int Selection { get; private set; }
    public bool IsHub => journey.AtHome;
    public int District => S.district;
    public int Room => S.room;
    public int Wave => S.wave;
    public int WaveMax => journey.WaveMax;
    public int RestChoice => S.restChoice;
    public int Cycle => S.cycle;
    public int Hp => S.hp;
    public int MaxHp => journey.MaxHp;
    public int AttackPower => journey.AttackPower;
    public int Flasks => S.flasks;
    public int FlaskMax => journey.FlaskMax;
    public int Wallet => S.wallet;
    public int Satchel => S.satchel;
    public int Cleared => S.cleared;
    public int BestCycle => S.bestCycle;
    public int Wins => S.wins;
    public int Kills => S.kills;
    public float Stamina => S.stamina;
    public float MaxStamina => 100;
    public float SkillWait => S.skillWait;
    public float DashWait => S.dashWait;
    public int DashCost => S.relics.Contains(2) ? 15 : 20;
    public float Motion => S.motion;
    public float Swing => S.swing;
    public float Dodge => S.dodge;
    public float Pulse => S.pulse;
    public float Hurt => S.hurt;
    public Vector2 Hero => S.hero;
    public Vector2 Facing => S.facing;
    public List<EmberFoe> Enemies => S.enemies;
    public List<EmberDanger> Dangers => S.dangers;
    public string BossName => EmberJourney.Bosses[District];
    public string Loadout
    {
        get
        {
            if (S.relics.Count == 0) return "遺物なし";
            return string.Join("・", S.relics.ConvertAll(index => EmberJourney.Relics[index]));
        }
    }
    public string ResultTitle => Outcome == EmberOutcome.Defeat ? "灯火を抱いて撤退" : Outcome == EmberOutcome.Return ?
        "生還の灯" : Cycle > 0 ? $"深層 {Cycle} を踏破" : District == 3 ? "四つの炉に灯が戻った" : "地区の守り手を浄化";
    public string ResultText
    {
        get
        {
            if (Outcome == EmberOutcome.Defeat) return $"携行していた火片の半分、{S.lastTakings}個を守り抜いた。体力と薬びんは拠点で戻ります。";
            if (Outcome == EmberOutcome.Return) return $"火片{S.lastTakings}個を拠点へ届けた。鍛冶場で装備を強化して、次の探索に備えよう。";
            if (Cycle > 0) return $"火片{S.lastTakings}個を持ち帰った。最高記録は深層{BestCycle}。門から次の深層へ進めます。";
            if (District == 3) return $"四灯がそろい、地下回廊に夜明けが差した。火片{S.lastTakings}個を持ち帰り、深層の巡回が開放された。";
            return $"火片{S.lastTakings}個を持ち帰った。門の先に「{DistrictNames[District + 1]}」が続いている。";
        }
    }
    public string Prompt
    {
        get
        {
            if (Mode == EmberMode.Fight && Enemies.Count == 0 && Wave < WaveMax) return "増援が近づいている";
            if (Mode == EmberMode.Sanctum)
            {
                switch (journey.NearbySite())
                {
                    case 1: return RestChoice != 0 ? "この部屋の寄り道は利用済み" :
                        Hp == MaxHp ? "体力は全快。古櫃か奥の門へ" : "E　灯の祭壇で体力を回復";
                    case 2: return RestChoice != 0 ? "この部屋の寄り道は利用済み" : "E　古櫃から火片を5個取り出す";
                    case 3: return "E　奥の門で遺物を選ぶ";
                    default: return RestChoice == 0 ? "左は回復、右は火片。寄り道は一つ、奥の門へはいつでも進めます" : "奥の門に近づき、Eで遺物を選ぼう";
                }
            }
            if (Mode != EmberMode.Hub) return "";
            if (Vector2.Distance(Hero, Portal) <= 2.6f) return "E　回廊の門を開く";
            if (Vector2.Distance(Hero, Forge) <= 2.6f) return "E　鍛冶場で装備を強化";
            return Cleared == 0 && Wins == 0 ? "右の門へ歩くか、Jで最初の回廊を選ぼう" : "右の門は探索、左の炉は装備の強化";
        }
    }
    public string RelicName(int i) => EmberJourney.Relics[S.offers[i]];
    public string RelicText(int i) => EmberJourney.RelicNotes[S.offers[i]];
    public int Level(int i) => S.upgrades[i];
    public int Cost(int i) => journey.Cost(i);
    public bool CanUpgrade(int i) => journey.CanUpgrade(i);
    public bool CanEnter(int i) => journey.CanEnter(i);

    protected override void BeginGame()
    {
        journey = new EmberJourney(Load<EmberRecord>());
        hud = new EmberHud();
        view = GetComponent<EmberView>();
        if (!view) view = gameObject.AddComponent<EmberView>();
        view.Render(this);
    }
    protected override void LateGame()
    {
        if (!view || journey == null) return;
        SetIntenseMusic(Playing && Mode == EmberMode.Fight);
        view.Render(this);
    }
    protected override void DrawGame() { hud.Draw(this); }
    protected override void SaveCheckpoint() { if (journey != null) Save(S); checkpointWait = 3f; }

    public void HomeMenu(bool forge)
    {
        if (!IsHub) return;
        S.mode = forge ? EmberMode.Forge : EmberMode.Districts;
        Selection = forge ? 0 : Mathf.Min(4, Cleared); Tone(1);
    }
    public void Upgrade(int i)
    {
        if (Mode != EmberMode.Forge) return;
        if (!journey.Upgrade(i)) { Notice(Cost(i) == 0 ? "最大段階です" : "火片が足りません"); return; }
        Tone(3); Notice(UpgradeNames[i] + "を強化しました"); SaveCheckpoint();
    }
    public void EnterDistrict(int i)
    {
        if (!journey.Enter(i)) { Notice("前の地区の守り手を倒すと開放されます"); return; }
        Selection = 0; Tone(3); SaveCheckpoint();
    }
    public void Back() { journey.Back(); Selection = 0; }
    public void SelectRelic(int i) { if (journey.Choose(i)) { Selection = 0; Tone(3); SaveCheckpoint(); } }
    public void InteractRoom()
    {
        if (Mode != EmberMode.Sanctum) return;
        int result = journey.UseSite();
        if (result == 0) { Notice(Prompt); return; }
        if (result == 1) Notice("灯の祭壇で体力が戻った");
        if (result == 2) Notice("古櫃から火片を5個手に入れた");
        Selection = 0; Tone(3); SaveCheckpoint();
    }
    public void RequestReturn() { journey.RequestReturn(); Selection = 0; }
    public void ConfirmReturn() { if (journey.ConfirmReturn()) { Selection = 0; Tone(3); SaveCheckpoint(); } }
    public void GoHome() { if (Mode == EmberMode.Result) { journey.Home(); Selection = 0; SaveCheckpoint(); } }
    public void Retry() { if (journey.Retry()) { Selection = 0; Tone(1); SaveCheckpoint(); } }

    protected override void TickGame()
    {
        float dt = Time.deltaTime;
        if (Mode == EmberMode.Hub)
        {
            journey.Step(dt, new EmberInput { move = Movement() });
            if (Input.GetKeyDown(KeyCode.J)) HomeMenu(false);
            else if (Input.GetKeyDown(KeyCode.K)) HomeMenu(true);
            else if (Input.GetKeyDown(KeyCode.E))
            {
                if (Vector2.Distance(Hero, Portal) <= 2.6f) HomeMenu(false);
                else if (Vector2.Distance(Hero, Forge) <= 2.6f) HomeMenu(true);
                else Notice("右の門か左の炉に近づこう");
            }
            return;
        }
        if (Mode == EmberMode.Sanctum)
        {
            journey.Step(dt, new EmberInput { move = Movement() });
            if (Input.GetKeyDown(KeyCode.E)) InteractRoom();
            if (Input.GetKeyDown(KeyCode.T)) RequestReturn();
            checkpointWait -= dt; if (checkpointWait <= 0) SaveCheckpoint();
            return;
        }
        if (Mode != EmberMode.Fight) { ReadPanel(); return; }
        if (Input.GetKeyDown(KeyCode.T)) { RequestReturn(); return; }
        journey.Step(dt, new EmberInput { move = Movement(),
            attack = Input.GetKey(KeyCode.J) || Input.GetMouseButton(0),
            dodge = Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Space),
            pulse = Input.GetKeyDown(KeyCode.L), heal = Input.GetKeyDown(KeyCode.Q) });
        if (journey.Chime >= 0) Tone(journey.Chime);
        checkpointWait -= dt;
        if (Mode != EmberMode.Fight) Selection = 0;
        if (Mode != EmberMode.Fight || checkpointWait <= 0) SaveCheckpoint();
    }

    static Vector2 Movement()
    {
        float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) -
            (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
        float y = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) -
            (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
        return Vector2.ClampMagnitude(new Vector2(x, y), 1);
    }
    void ReadPanel()
    {
        if (Input.GetKeyDown(KeyCode.Backspace)) { if (Mode == EmberMode.Result) GoHome(); else Back(); return; }
        if (Mode == EmberMode.Relic && Input.GetKeyDown(KeyCode.T)) { RequestReturn(); return; }
        int count = Mode == EmberMode.Districts ? 5 : Mode == EmberMode.Relic || Mode == EmberMode.Forge ? 3 : 2;
        int dir = 0;
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A)) dir = -1;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D)) dir = 1;
        if (dir != 0) { Selection = (Selection + dir + count) % count; Tone(1); }
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter)) return;
        switch (Mode)
        {
            case EmberMode.Districts: EnterDistrict(Selection); break;
            case EmberMode.Forge: Upgrade(Selection); break;
            case EmberMode.Relic: SelectRelic(Selection); break;
            case EmberMode.Retreat: if (Selection == 0) Back(); else ConfirmReturn(); break;
            case EmberMode.Result: if (Selection == 0) GoHome(); else Retry(); break;
        }
    }
    public override void VerifyGame()
    {
        var record = EmberVerification.Run();
        if (!Reviewing) return;
        Save(record);
        var resumed = new EmberJourney(Load<EmberRecord>()).S;
        if (resumed.cleared != 4 || resumed.bestCycle < 1 || resumed.wallet != record.wallet)
            throw new InvalidOperationException("地下回廊のディスク再開検証に失敗しました");
    }
    public override void PrepareShowcase()
    {
        journey = new EmberJourney(new EmberRecord { cleared = 3, upgrades = new[] { 1, 1, 0 } });
        journey.Enter(3);
        for (int i = 0; i < 37; i++) journey.Step(.04f, new EmberInput { move = i < 18 ? Vector2.up : Vector2.left * .45f });
        view.Render(this);
    }
    public override int ShowcaseViews => 3;
    public override void ShowcaseView(int index)
    {
        if (index == 0) { PrepareShowcase(); return; }
        journey = new EmberJourney(new EmberRecord { cleared = 2, upgrades = new[] {1, 1, 0}, wallet = 23 });
        if (index == 2) { S.mode = EmberMode.Districts; Selection = 2; }
        view.Render(this);
    }
}
