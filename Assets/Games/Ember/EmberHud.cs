using System.Collections.Generic;
using UnityEngine;

public sealed class EmberHud
{
    static readonly Color Coal = new Color32(21, 25, 31, 247);
    static readonly Color Panel = new Color32(34, 39, 45, 255);
    static readonly Color Selected = new Color32(65, 54, 43, 255);
    static readonly Color Iron = new Color32(63, 67, 69, 255);
    static readonly Color Parchment = new Color32(248, 239, 212, 255);
    static readonly Color Ash = new Color32(199, 198, 185, 255);
    static readonly Color Ember = new Color32(255, 185, 100, 255);
    static readonly Color Health = new Color32(230, 111, 91, 255);
    static readonly Color Vigor = new Color32(129, 207, 185, 255);
    static readonly Color Disabled = new Color32(157, 159, 149, 255);
    static readonly string[] Numerals = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII" };
    static readonly string[] ForgeSigns = { "sword", "cloak", "flask" };
    static readonly Dictionary<string, Texture2D> Signs = new Dictionary<string, Texture2D>();
    EmberGame game;

    public void Draw(EmberGame g)
    {
        game = g;
        switch (g.Mode)
        {
            case EmberMode.Hub: Hub(); break;
            case EmberMode.Codex: Codex(); break;
            case EmberMode.Districts: Districts(); break;
            case EmberMode.Forge: Forge(); break;
            case EmberMode.Fight: Fight(); break;
            case EmberMode.Sanctum: Sanctuary(); break;
            case EmberMode.Relic: Relics(); break;
            case EmberMode.Retreat: ReturnChoice(); break;
            case EmberMode.Result: Results(); break;
        }
    }

    void Hub()
    {
        const float x = 21, y = 19;
        Backplate(x, y, 264, 65);
        Sigil("fire", x + 9, y + 12, 42, Ember);
        Type("L A N T E R N   K E E P", x + 63, y + 6, 190, 19, 11, Ember);
        Type("灯守の拠点", x + 61, y + 24, 190, 34, 24, Parchment, true);

        Backplate(x, y + 74, 286, 55);
        Block(x, y + 84, 3, 33, Ember);
        Type("次の探索", x + 13, y + 79, 76, 18, 12, Ash);
        string next = game.Cleared < EmberGame.DistrictNames.Length ? EmberGame.DistrictNames[Mathf.Clamp(game.Cleared, 0, EmberGame.DistrictNames.Length - 1)] : "深層の巡回";
        Type(next, x + 12, y + 97, 263, 25, 16, Parchment);

        float sx = game.W - 310;
        Backplate(sx, y, 289, 57);
        Sigil("shard", sx + 11, y + 15, 29, Ember);
        Type("預けた火片", sx + 47, y + 5, 95, 18, 12, Ash);
        Type(game.Wallet.ToString("N0"), sx + 46, y + 21, 94, 29, 20, Ember);
        Block(sx + 145, y + 10, 1, 36, Iron);
        Type("踏破した地区", sx + 160, y + 5, 118, 18, 12, Ash);
        Type($"{game.Cleared} / {EmberGame.DistrictNames.Length}", sx + 160, y + 21, 116, 29, 20, Parchment);

        float by = game.H - 59;
        if (KeyButton(x, by, 162, "J", "地区を選ぶ")) game.HomeMenu(false);
        if (KeyButton(x + 169, by, 137, "K", "鍛冶場")) game.HomeMenu(true);
        if (KeyButton(x + 312, by, 149, "B", "魔物誌")) game.ShowCodex();
        Type("WASD ／ 矢印　歩く", game.W - 265, by + 1, 244, 37, 14, Parchment, align: TextAnchor.MiddleRight, shadow: true);
        if (!string.IsNullOrWhiteSpace(game.Prompt)) Prompt(game.Prompt, by - 48);
    }

    void Fight()
    {
        float x = 19, y = 16;
        Backplate(x, y, 304, 106);
        Sigil("fire", x + 9, y + 9, 26, Ember);
        Type(Location(), x + 41, y + 5, 257, 30, 16, Parchment, true);
        Meter(x + 14, y + 42, 275, "体力 HP", game.Hp, game.MaxHp, Health);
        Meter(x + 14, y + 73, 275, "スタミナ", game.Stamina, game.MaxStamina, Vigor);

        float rx = game.W - 224;
        Backplate(rx, y, 205, 61);
        Sigil("shard", rx + 9, y + 12, 31, Ember);
        Type("携行する火片", rx + 48, y + 3, 148, 20, 12, Ash);
        Type(game.Satchel.ToString("N0"), rx + 47, y + 24, 146, 29, 21, Ember);

        bool boss = false;
        foreach (var foe in game.Enemies)
        {
            if ((int)foe.kind != 4 || foe.hp <= 0) continue;
            boss = true;
            float bw = Mathf.Min(345, game.W - 568), bx = (game.W - bw) / 2;
            Backplate(bx, y, bw, 50);
            Type(game.BossName, bx + 8, y + 2, bw - 16, 26, 16, Parchment, true, TextAnchor.MiddleCenter);
            Bar(bx + 11, y + 34, bw - 22, 6, foe.hp / (float)Mathf.Max(1, foe.maxHp), Health);
            break;
        }
        if (game.Dangers.Count > 0)
        {
            float progress = 0, incoming = 0;
            bool exposed = false;
            foreach (var danger in game.Dangers)
            {
                progress = Mathf.Max(progress, danger.progress);
                if (EmberJourney.DistanceToDanger(game.Hero, danger) <= danger.radius + .22f)
                {
                    exposed = true;
                    incoming = Mathf.Max(incoming, danger.progress);
                }
            }
            float by = boss ? y + 56 : y;
            float bx = game.W / 2 - 127;
            Backplate(bx, by, 254, 35);
            Color warning = exposed ? Health : Ember;
            Block(bx, by + 3, 3, 27, warning);
            Type(exposed ? "攻撃範囲内　床の印から離れる" : "敵の攻撃予兆　足元に注意", bx + 6, by + 1, 242, 29, 14, Parchment, align: TextAnchor.MiddleCenter);
            Bar(bx + 8, by + 30, 238, 3, exposed ? incoming : progress, warning);
        }
        Type($"第 {game.Wave} / {game.WaveMax} 波　 敵 {game.Enemies.Count}", rx - 5, y + 67, 209, 25, 15, Parchment, align: TextAnchor.MiddleRight, shadow: true);
        Type("T　途中帰還", rx + 8, y + 93, 194, 25, 14, Ash, align: TextAnchor.MiddleRight, shadow: true);

        float fy = game.H - 59;
        Backplate(x, fy, 218, 43);
        Ability(x + 8, fy + 5, 101, "J", "斬撃", game.Stamina < 5 ? "残量不足" : null);
        Ability(x + 110, fy + 5, 107, "K", "回避", WaitText(game.DashWait, game.Stamina < game.DashCost));
        float skillX = game.W - 270;
        Backplate(skillX, fy, 251, 43);
        Ability(skillX + 8, fy + 5, 122, "L", "衝撃波", WaitText(game.SkillWait, false));
        Ability(skillX + 132, fy + 5, 115, "Q", $"薬 {game.Flasks}/{game.FlaskMax}", game.Flasks == 0 ? "空" : null);
        if (!string.IsNullOrWhiteSpace(game.Prompt)) Prompt(game.Prompt, fy - 49);
    }

    void Sanctuary()
    {
        float x = 19, y = 16;
        Backplate(x, y, 288, 93);
        Sigil("fire", x + 11, y + 8, 33, Ember);
        Type("灯の間", x + 55, y + 4, 220, 33, 23, Parchment, true);
        Type(Location(), x + 56, y + 38, 222, 20, 13, Ash);
        Meter(x + 13, y + 63, 259, "体力 HP", game.Hp, game.MaxHp, Health);
        float rx = game.W - 301;
        Backplate(rx, y, 282, 101);
        Type(game.RestChoice == 0 ? "寄り道をひとつ選べます" : "この部屋の寄り道", rx + 12, y + 4, 260, 27, 16, Ember);
        Type(game.RestChoice == 1 ? "左　祭壇で回復しました" : "左　祭壇　体力を半分回復", rx + 13, y + 37, 257, 25, 14, game.RestChoice == 2 ? Disabled : Parchment);
        Type(game.RestChoice == 2 ? "右　古櫃から火片を入手" : "右　古櫃　火片を5個入手", rx + 13, y + 65, 257, 25, 14, game.RestChoice == 1 ? Disabled : Parchment);
        float bottom = game.H - 57;
        Type("WASD ／ 矢印　歩く", x, bottom + 4, 232, 32, 15, Parchment, shadow: true);
        if (KeyButton(game.W - 170, bottom, 150, "T", "途中帰還")) game.RequestReturn();
        string text = game.Prompt;
        if (string.IsNullOrEmpty(text)) return;
        float w = Mathf.Clamp(text.Length * 15 + 32, 182, game.W - 82);
        var hint = new Rect((game.W - w) / 2, bottom - 48, w, 35);
        Prompt(text, hint.y);
        if (Hit(hint)) game.InteractRoom();
    }

    string Location() => (game.Cycle > 0 ? $"深層 {game.Cycle}　" : "") + EmberGame.DistrictNames[Mathf.Clamp(game.District, 0, 3)] + $"　{game.Room}/3";

    static string WaitText(float seconds, bool low)
    {
        if (seconds > .04f) return seconds.ToString("0.0") + " 秒";
        return low ? "残量不足" : null;
    }

    void Meter(float x, float y, float w, string title, float value, float max, Color tint)
    {
        Type(title, x, y - 4, 64, 23, 13, Parchment);
        Type($"{Mathf.CeilToInt(value)} / {Mathf.CeilToInt(max)}", x + 65, y - 5, w - 66, 21, 12, Ash, align: TextAnchor.MiddleRight);
        Bar(x + 63, y + 15, w - 64, 5, value / Mathf.Max(1f, max), tint);
    }

    void Bar(float x, float y, float w, float h, float portion, Color tint)
    {
        Block(x, y, w, h, Iron);
        float fill = w * Mathf.Clamp01(portion);
        if (fill > 0) Block(x, y, fill, h, tint);
    }

    void Ability(float x, float y, float w, string key, string action, string wait)
    {
        Block(x, y + 3, 26, 26, wait == null ? Ember : Iron);
        Type(key, x, y + 2, 26, 27, 14, wait == null ? Coal : Parchment, align: TextAnchor.MiddleCenter);
        Type(action, x + 32, y + (wait == null ? 1 : -4), w - 33, wait == null ? 29 : 22, 14, Parchment);
        if (wait != null) Type(wait, x + 32, y + 16, w - 33, 15, 11, Ash);
    }

    void Districts()
    {
        Rect p = Window(916, 496, "DISTRICT GATE", "探索する地区", "gate");
        Type("守り手を倒すと、次の地区への道が開く。", p.x + 22, p.y + 76, p.width - 44, 27, 15, Ash);
        const float listWidth = 431;
        int deep = EmberGame.DistrictNames.Length;
        int preview = Mathf.Clamp(game.Selection, 0, deep);
        for (int i = 0; i <= deep; i++)
        {
            bool open = game.CanEnter(i), selected = game.Selection == i;
            var r = new Rect(p.x + 19 + (i == deep ? 0 : i % 2 * 218), p.y + 116 + (i / 2) * 62, i == deep ? listWidth : 212, 55);
            bool hover = Hover(r);
            if (hover) preview = i;
            Row(r, open, selected, hover);
            Type(i == deep ? "∞" : Numerals[i], r.x + 2, r.y, 36, r.height, 21, open ? Ember : Ash, true, TextAnchor.MiddleCenter);
            Block(r.x + 39, r.y + 9, 1, r.height - 18, Iron);
            string areaName = i == deep ? "深層の巡回" : EmberGame.DistrictNames[i];
            Type(areaName, r.x + 47, r.y + 5, r.width - 49, 26, 18, open ? Parchment : Ash, true);
            Type(i == deep ? $"最高記録　深層 {game.BestCycle}" : $"第 {i + 1} 地区", r.x + 48, r.y + 31, r.width - 50, 20, 12, Ash);
            string tag = open ? i < game.Cleared ? "再探索" : "探索" : "未開放";
            Type(tag, r.xMax - 65, r.y + 30, 56, 20, 12, open ? Ember : Disabled, align: TextAnchor.MiddleRight);
            if (open && Hit(r)) game.EnterDistrict(i);
        }
        float px = p.x + listWidth + 34, py = p.y + 116, pw = p.xMax - 19 - px;
        Block(px, py, pw, 308, Panel);
        var art = new Rect(px + 6, py + 6, pw - 12, 171);
        Block(art.x, art.y, art.width, art.height, Coal);
        Sigil("gate", art.center.x - 21, art.center.y - 22, 42, Iron);
        if (preview < deep) DistrictArt(art, preview);
        else
        {
            float aw = (art.width - 2) / 2, ah = (art.height - 2) / 2;
            for (int i = 0; i < 4; i++) DistrictArt(new Rect(art.x + i % 2 * (aw + 2), art.y + i / 2 * (ah + 2), aw, ah), i);
        }
        Type(preview == deep ? "深層の巡回" : EmberGame.DistrictNames[preview], px + 13, py + 182, pw - 26, 34, 22, Parchment, true);
        string note = preview == deep ? "最初の四地区を踏破すると開放。踏破済みの地区を順に巡る探索。" : EmberGame.DistrictNotes[preview];
        Type(note, px + 14, py + 222, pw - 28, 56, 14, Ash);
        Block(px + 13, py + 281, pw - 26, 1, Iron);
        Type(game.CanEnter(preview) ? "入域可" : preview == deep ? "開放条件　4地区を踏破" : "開放条件　前の地区を踏破", px + 14, py + 283, pw - 28, 22, 13, Ember);
        Footer(p);
    }

    void DistrictArt(Rect area, int index)
    {
        if (index >= 4)
        {
            var plate = Resources.Load<Texture2D>("Ember/OuterDistricts");
            if (plate) GUI.DrawTextureWithTexCoords(area, plate, new Rect((index % 2) * .5f, (1 - (index - 4) / 2) * .5f, .5f, .5f));
            return;
        }
        Texture image = game.skin.illustrations;
        if (!image) return;
        // Each district occupies one quarter of the atlas. Preserve its aspect ratio within the frame.
        float ratio = image.width / (float)image.height;
        float w = Mathf.Max(area.width, area.height * ratio), h = w / ratio;
        GUI.BeginGroup(area);
        game.Art(new Rect((area.width - w) / 2, (area.height - h) / 2, w, h), index);
        GUI.EndGroup();
    }

    void Codex()
    {
        var p=Window(888,486,"KEEPER'S FIELD NOTES","回廊の魔物誌","fire");
        int ordinary=EmberJourney.Creatures.Length,total=ordinary+EmberJourney.Bosses.Length,seen=0;
        for(int i=0;i<total;i++)if(game.Observed(i))seen++;
        Type($"遭遇の記録　{seen} / {total}",p.x+22,p.y+76,p.width-44,28,16,Ember);
        for(int i=0;i<total;i++)
        {
            var r=new Rect(p.x+21+(i/6)*161,p.y+119+(i%6)*45,154,39);
            bool known=game.Observed(i);Row(r,known,game.Selection==i,Hover(r));
            string name=i<ordinary?EmberJourney.Creatures[i]:EmberJourney.Bosses[i-ordinary];
            Type(known?name:"未遭遇",r.x+7,r.y+2,r.width-14,34,15,known?Parchment:Ash);
            if(Hit(r))game.Inspect(i);
        }
        int n=game.Selection;bool revealed=game.Observed(n),boss=n>=ordinary;
        float bx=p.x+516,bw=p.width-537;
        Block(bx,p.y+119,bw,263,Panel);
        if(boss&&revealed)DistrictArt(new Rect(bx+9,p.y+128,bw-18,91),n-ordinary);
        else Sigil("fire",bx+bw/2-21,p.y+147,42,revealed?Ember:Iron);
        Type(revealed?(boss?EmberJourney.Bosses[n-ordinary]:EmberJourney.Creatures[n]):"未遭遇",bx+12,p.y+224,bw-24,35,21,Parchment,true);
        string note=boss?EmberJourney.Notes[n-ordinary]:EmberJourney.Tactics[n];
        Type(revealed?note:"探索で出会うと記録が開く。",bx+12,p.y+272,bw-24,85,15,Ash);
        Footer(p);
    }
    void Forge()
    {
        Rect p = Window(728, 481, "THE FORGE", "灯守の鍛冶場", "sword");
        Type("預けた火片を使って、次の探索から使う装備を強化する。", p.x + 22, p.y + 77, p.width - 220, 32, 14, Ash);
        Type($"火片　{game.Wallet:N0}", p.xMax - 184, p.y + 75, 159, 34, 17, Ember, align: TextAnchor.MiddleRight);
        for (int i = 0; i < 3; i++)
        {
            int level = game.Level(i), cost = game.Cost(i);
            bool capped = cost <= 0, enabled = game.CanUpgrade(i);
            var r = new Rect(p.x + 19, p.y + 122 + i * 91, p.width - 38, 80);
            Row(r, true, game.Selection == i, enabled && Hover(r));
            Sigil(ForgeSigns[i], r.x + 13, r.y + 20, 39, Ember);
            Type(EmberGame.UpgradeNames[i], r.x + 67, r.y + 4, r.width - 304, 31, 19, Parchment, true);
            Type($"Lv. {level}", r.xMax - 224, r.y + 6, 65, 25, 14, Ember, align: TextAnchor.MiddleRight);
            Type(EmberGame.UpgradeNotes[i], r.x + 68, r.y + 38, r.width - 228, 32, 14, Ash);
            Type(capped ? "強化完了" : $"火片 {cost}", r.xMax - 142, r.y + 10, 121, 29, 17, enabled ? Ember : Ash, align: TextAnchor.MiddleRight);
            Type(capped ? "最高段階" : enabled ? "強化する" : "火片不足", r.xMax - 142, r.y + 40, 121, 26, 14, enabled ? Parchment : Disabled, align: TextAnchor.MiddleRight);
            if (enabled && Hit(r)) game.Upgrade(i);
        }
        Footer(p);
    }

    void Relics()
    {
        Rect p = Window(870, 353, "RELIC CHAMBER", "遺物をひとつ選ぶ", "shard");
        Type("次の部屋へ持っていく力を選びましょう。", p.x + 22, p.y + 77, p.width - 44, 27, 14, Ash);
        float cardWidth = (p.width - 62) / 3;
        for (int i = 0; i < 3; i++)
        {
            var r = new Rect(p.x + 20 + i * (cardWidth + 11), p.y + 119, cardWidth, 157);
            Row(r, true, game.Selection == i, Hover(r));
            Sigil("relic" + i, r.center.x - 16, r.y + 10, 32, Ember);
            Type(game.RelicName(i), r.x + 8, r.y + 46, r.width - 16, 34, 18, Parchment, true, TextAnchor.MiddleCenter);
            Block(r.x + 17, r.y + 85, r.width - 34, 1, Iron);
            Type(game.RelicText(i), r.x + 12, r.y + 92, r.width - 24, 55, 14, Ash, align: TextAnchor.MiddleCenter);
            if (Hit(r)) game.SelectRelic(i);
        }
        Type("矢印　選択　 Enter　決定　 T　途中帰還", p.x + 23, p.yMax - 56, 291, 36, 14, Ash);
        Type("携行中　" + Loadout(), p.x + 306, p.yMax - 58, p.width - 331, 43, 13, Ash, align: TextAnchor.MiddleRight);
    }

    string Loadout() => string.IsNullOrWhiteSpace(game.Loadout) ? "遺物なし" : game.Loadout;

    void ReturnChoice()
    {
        Rect p = Window(622, 307, "RETURN TO THE KEEP", "途中で拠点へ戻る", "gate");
        Type("ここで探索を終えますか？", p.x + 23, p.y + 84, p.width - 46, 38, 21, Parchment, true);
        Type("携行中の火片をすべて持ち帰り、今回の遺物を返します。", p.x + 24, p.y + 132, p.width - 48, 29, 15, Ash);
        Type($"携行中の火片　{game.Satchel:N0}", p.x + 24, p.y + 174, p.width - 48, 31, 17, Ember);
        if (Decision(p, 0, "探索を続ける")) game.Back();
        if (Decision(p, 1, "拠点に帰還する")) game.ConfirmReturn();
    }

    void Results()
    {
        bool win = game.Outcome == EmberOutcome.Victory, lost = game.Outcome == EmberOutcome.Defeat;
        Rect p = Window(693, 420, win ? "DISTRICT CLEARED" : lost ? "THE FLAME FADES" : "BACK AT THE KEEP", game.ResultTitle, win ? "fire" : "gate");
        Type(game.ResultText, p.x + 24, p.y + 80, p.width - 48, 76, 17, Parchment);
        float column = (p.width - 54) / 3;
        string[] labels = { "倒した敵", "預けた火片", "踏破した地区" };
        string[] values = { game.Kills.ToString(), game.Wallet.ToString("N0"), $"{game.Cleared} / {EmberGame.DistrictNames.Length}" };
        for (int i = 0; i < 3; i++)
        {
            float x = p.x + 19 + i * (column + 8);
            Block(x, p.y + 171, column, 71, Panel);
            Type(labels[i], x + 10, p.y + 177, column - 20, 20, 13, Ash);
            Type(values[i], x + 10, p.y + 200, column - 20, 31, 22, Ember, true);
        }
        Type("今回の遺物", p.x + 22, p.y + 260, p.width - 44, 23, 13, Ash);
        Type(Loadout(), p.x + 22, p.y + 287, p.width - 44, 45, 16, Parchment);
        if (Decision(p, 0, "拠点へ戻る")) game.GoHome();
        if (Decision(p, 1, "同じ条件で再挑戦")) game.Retry();
    }

    bool Decision(Rect p, int slot, string label)
    {
        float w = (p.width - 49) / 2;
        Rect r = new Rect(p.x + 20 + slot * (w + 9), p.yMax - 63, w, 44);
        Row(r, true, game.Selection == slot, Hover(r));
        Type(label, r.x + 5, r.y, r.width - 10, r.height, 16, Parchment, align: TextAnchor.MiddleCenter);
        return Hit(r);
    }

    Rect Window(float w, float h, string subtitle, string title, string mark)
    {
        Block(0, 0, game.W, game.H, new Color(.032f, .045f, .063f, .63f));
        var p = new Rect((game.W - w) / 2, (game.H - h) / 2, w, h);
        Backplate(p.x, p.y, w, h);
        Block(p.x + 8, p.y + 10, 2, 47, Ember);
        Sigil(mark, p.x + 19, p.y + 17, 36, Ember);
        Type(subtitle, p.x + 70, p.y + 8, w - 94, 21, 11, Ember);
        Type(title, p.x + 68, p.y + 28, w - 92, 36, 24, Parchment, true);
        Block(p.x + 16, p.y + 68, w - 32, 1, Iron);
        return p;
    }

    void Footer(Rect p)
    {
        Type(game.Mode==EmberMode.Codex?"矢印　記録を読む":"↑ ↓　選択　　 Enter　決定", p.x + 22, p.yMax - 54, p.width - 225, 35, 14, Ash);
        if (KeyButton(p.xMax - 171, p.yMax - 55, 150, "BS", "拠点へ戻る")) game.Back();
    }

    void Prompt(string prompt, float y)
    {
        float w = Mathf.Clamp(prompt.Length * 15 + 32, 182, game.W - 82);
        float x = (game.W - w) / 2;
        Backplate(x, y, w, 35);
        Type(prompt, x + 6, y, w - 12, 35, 15, Parchment, align: TextAnchor.MiddleCenter);
    }

    void Row(Rect r, bool enabled, bool selected, bool hover)
    {
        Block(r.x, r.y, r.width, r.height, selected || hover ? Selected : Panel);
        if (selected || hover) Block(r.x, r.y, 3, r.height, enabled ? Ember : Iron);
    }

    bool KeyButton(float x, float y, float w, string key, string label)
    {
        var r = new Rect(x, y, w, 40);
        Backplate(x, y, w, 40);
        if (Hover(r)) Block(x + 1, y + 1, w - 2, 38, Selected);
        Block(x + 7, y + 8, 25, 24, Iron);
        Type(key, x + 7, y + 6, 25, 28, 13, Ember, align: TextAnchor.MiddleCenter);
        Type(label, x + 36, y + 2, w - 42, 36, 15, Parchment, align: TextAnchor.MiddleCenter);
        return Hit(r);
    }

    void Backplate(float x, float y, float w, float h)
    {
        Block(x + 2, y + 3, w, h, new Color(0, 0, 0, .28f));
        Block(x, y, w, h, Coal);
        Block(x, y, w, 1, Iron);
    }

    static bool Hover(Rect r) => r.Contains(Event.current.mousePosition);
    static bool Hit(Rect r) => GUI.Button(r, GUIContent.none, GUIStyle.none);
    void Block(float x, float y, float w, float h, Color c) => game.Box(new Rect(x, y, w, h), c);

    void Type(string words, float x, float y, float w, float h, int size, Color c, bool serif = false, TextAnchor align = TextAnchor.MiddleLeft, bool shadow = false)
    {
        if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(words)) return;
        if (Screen.width < 800 && size >= 11 && size <= 16) size += 2;
        Rect r = new Rect(x, y, w, h);
        if (shadow) game.Label(new Rect(x + 1, y + 1, w, h), words, size, Coal, serif, align);
        game.Label(r, words, size, c, serif, align);
    }

    // The signs are drawn on a 24-unit grid to stay clear at each HUD scale.
    void Sigil(string name, float x, float y, float size, Color color)
    {
        if (Event.current.type != EventType.Repaint) return;
        if (!Signs.TryGetValue(name, out var texture) || !texture)
            Signs[name] = texture = MakeSign(name);
        GUI.DrawTexture(new Rect(x, y, size, size), texture, ScaleMode.ScaleToFit, true, 1, color, 0, 0);
    }

    static Texture2D MakeSign(string name)
    {
        const int n = 64;
        var pixels = new Color32[n*n];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255,255,255,0);
        void Path(params float[] points)
        {
            for (int i = 2; i < points.Length; i += 2)
            {
                var a = new Vector2(points[i-2], points[i-1]);
                var d = new Vector2(points[i], points[i+1]) - a;
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var v = new Vector2((x + .5f) * 24 / n, (y + .5f) * 24 / n) - a;
                    float t = Mathf.Clamp01(Vector2.Dot(v,d) / Mathf.Max(.001f,d.sqrMagnitude));
                    float distance = (v - d * t).magnitude;
                    byte alpha = (byte)(255 * Mathf.Clamp01((.9f - distance) * n / 24));
                    int index = (n - 1 - y) * n + x;
                    if (alpha > pixels[index].a) pixels[index].a = alpha;
                }
            }
        }
        switch (name)
        {
            case "fire":
                Path(6,18, 4,13, 8,8, 10,11, 14,2, 19,10, 21,14, 17,19, 9,20, 6,18);
                Path(9,17, 13,10, 15,14, 15,17); Path(6,23, 18,23); break;
            case "gate":
                Path(3,22, 3,9, 7,4, 12,2, 17,4, 21,9, 21,22); Path(7,22, 7,10, 12,6, 17,10, 17,22);
                Path(1,22, 9,22); Path(15,22, 23,22); break;
            case "sword":
                Path(5,20, 10,15); Path(6,13, 12,19); Path(9,14, 17,5, 22,2, 19,8, 11,16); break;
            case "cloak":
                Path(8,4, 12,2, 16,4, 17,9, 22,21, 16,19, 12,22, 8,19, 2,21, 7,9, 8,4);
                Path(8,8, 12,10, 16,8); Path(12,11, 9,17); break;
            case "flask":
                Path(9,2, 15,2, 15,5, 14,5, 14,9, 19,15, 19,19, 16,22, 8,22, 5,19, 5,15, 10,9, 10,5, 9,5, 9,2);
                Path(7,16, 17,16); break;
            case "relic0":
                Path(12,2, 21,12, 12,22, 3,12, 12,2); Path(8,13, 12,8, 16,13); break;
            case "relic1":
                Path(12,2, 20,6, 19,15, 12,22, 5,15, 4,6, 12,2); Path(8,8, 12,12, 16,8); break;
            case "relic2":
                Path(3,15, 8,8, 13,13, 20,3); Path(5,20, 9,15, 14,19, 21,10); break;
            default:
                Path(11,2, 18,7, 20,14, 12,22, 5,15, 6,7, 11,2);
                Path(11,2, 10,13, 12,22); Path(10,13, 18,7); break;
        }
        var texture = new Texture2D(n,n,TextureFormat.RGBA32,false) { hideFlags=HideFlags.HideAndDontSave, filterMode=FilterMode.Bilinear, wrapMode=TextureWrapMode.Clamp };
        texture.SetPixels32(pixels);
        texture.Apply(false,true);
        return texture;
    }
}
