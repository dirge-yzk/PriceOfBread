using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1372) 計器 lawstep_s（法の段が変わった 1 件ごとに 1 行）の番人（読むだけ）。盤面は試験の中で置く：時短（MandateMargin が無限大＝門に止められない軸）の目標を TargetDemand で動かし、
/// Government.Step を直に回す。書き手の別は盤面が置いた物から期待する（要求の旗を立てた＝2・3、何も立てない＝1、施行の外で段を書く＝6、GovernanceSnapshot の戻し＝5、譲歩の前後の hook＝4）。
/// 突然変異（使い捨ての木・実測）：M3 書き手の別を常に 1 で書く → L1 赤。M5 〈その他〉（6）の行を書かない → L2 赤。M6 段が動かない法にも行を書く → L1・L1b・L2・L3・L4 赤。
/// </summary>
[Collection("Instruments")]
public class LawstepInstrumentTests
{
    private const ulong Seed = 1372UL;

    private sealed record Board(MultiNationSim Sim, World W, int Axis);

    private static Board Make()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: Seed, coef: new Coefficients()) { DiplomacyFrozen = true };
        var w = sim.Nations.First(n => Instruments.NameOf(n.World) == "イギリス").World;
        return new(sim, w, (int)PolicyAxis.ShorterHours);
    }

    /// <summary>計器を lawstep_s だけにして盤面を回し、各手のあとの四半期の末で書かれた行を返す。</summary>
    private static System.Collections.Generic.List<string[][]> Run(System.Action<Board, System.Func<string[][]>> scenario)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_lawstep_" + System.Guid.NewGuid().ToString("N"));
        Instruments.SelectTables("lawstep_s");
        Instruments.Configure(dir, "*");
        var b = Make(); int q = 0;
        var outs = new System.Collections.Generic.List<string[][]>();
        try
        {
            Instruments.Phase(b.Sim, Seed, "open");
            scenario(b, () => { Instruments.Quarter(b.Sim, Seed, q++); var rows = Instruments.LawstepPeek(b.Sim).Select(l => l.Split('\t')).ToArray(); outs.Add(rows); return rows; });
        }
        finally { Instruments.Close(b.Sim); Instruments.Configure(null, null); Instruments.SelectTables(null); try { System.IO.Directory.Delete(dir, true); } catch { } }
        return outs;
    }

    private static string[] Head => Instruments.LawstepHeaderText.Split('\t');
    private static string Col(string[] r, string n) { int i = System.Array.IndexOf(Head, n); Assert.True(i >= 0, "列が無い " + n); return r[i]; }

    private static void Step(Board b) => Government.Step(b.W, b.W.Coef);

    private static void Pin(Board b) { b.W.Policy.WorkHours = b.W.WorkHours; b.W.Policy.Suffrage = b.W.Suffrage; }   // AI の目標を今の段に据える（盤面が置いた動きだけが出る）

    [Fact]
    public void L1_書き手の別は盤面が置いた要求どおり_向きが逆の要求は書き手にならず_蜂起の要求は2_派閥の危機は3()
    {
        int ax = (int)PolicyAxis.ShorterHours;
        var all = Run((b, flush) =>
        {
            var w = b.W;
            w.WorkHours = WorkHours.Unlimited; Pin(b);
            Step(b); flush();   // 最初の施行：記録を置くだけ
            w.PendingRevoltDemand = ax + 1; Parliament.TargetDemand(w, PolicyAxis.ShorterHours, false); Step(b); flush();   // 下げるのに〈上げ〉の要求 → 向きが逆＝1
            w.PendingRevoltDemand = -(ax + 1); Parliament.TargetDemand(w, PolicyAxis.ShorterHours, false); Step(b); flush();   // 蜂起の要求（下げ）→ 2
            w.PendingRevoltDemand = 0;
            w.PendingFactionCrisis = new FactionState { CrisisDemands = [-(ax + 1)] }; Parliament.TargetDemand(w, PolicyAxis.ShorterHours, false); Step(b); flush();   // 派閥の危機（下げ）→ 3
            w.PendingFactionCrisis = null;
        });
        Assert.Empty(all[0]);
        string[] expect = ["1", "2", "3"];
        for (int k = 1; k <= 3; k++)
        {
            Assert.True(all[k].Length == 1, $"手 {k}：行 {all[k].Length}");
            var r = all[k][0];
            Assert.Equal("ShorterHours", Col(r, "axis"));
            int from = int.Parse(Col(r, "fromStage")), to = int.Parse(Col(r, "toStage"));
            Assert.Equal(-1, to - from);
            Assert.Equal(expect[k - 1], Col(r, "writerCode"));
            Assert.Equal(to.ToString(), Col(r, "aiTargetStage"));
            Assert.NotEqual("nan", Col(r, "netUpIndex")); Assert.NotEqual("nan", Col(r, "netDownIndex"));   // 施行の道の行は変える前の純支持を持つ
        }
        Assert.Equal(ax + 1, int.Parse(Col(all[1][0], "pendingRevoltDemandCode")));
        Assert.Equal(-(ax + 1), int.Parse(Col(all[2][0], "pendingRevoltDemandCode")));
    }

    [Fact]
    public void L1b_要求が何も立っていなければ書き手は_1_AI_の目標()
    {
        var all = Run((b, flush) =>
        {
            var w = b.W; w.WorkHours = WorkHours.Unlimited; Pin(b);
            Step(b); flush();
            Parliament.TargetDemand(w, PolicyAxis.ShorterHours, false); Step(b); flush();
        });
        Assert.Single(all[1]); Assert.Equal("1", Col(all[1][0], "writerCode")); Assert.Equal("0", Col(all[1][0], "pendingRevoltDemandCode"));
    }

    [Fact]
    public void L2_施行の外で段を書くと次の施行の頭で_その他_6_の行になる()
    {
        var all = Run((b, flush) =>
        {
            Pin(b); Step(b); flush();
            b.W.WorkHours = (WorkHours)(((int)b.W.WorkHours + 1) % 4);   // 施行の外の書き手（政体の変化・事件の代わり）
            Pin(b); Step(b); flush();
        });
        Assert.Empty(all[0]);
        Assert.Single(all[1]);
        Assert.Equal("6", Col(all[1][0], "writerCode")); Assert.Equal("ShorterHours", Col(all[1][0], "axis"));
        Assert.Equal("-1", Col(all[1][0], "aiTargetStage"));
    }

    [Fact]
    public void L3_政体の戻しは_5_譲歩の前後の_hook_は_4_で前後の闘争度を持つ()
    {
        var all = Run((b, flush) =>
        {
            var w = b.W;
            Pin(b); Step(b); flush();
            var snap = GovernanceSnapshot.Capture(w);
            w.WorkHours = (WorkHours)(((int)w.WorkHours + 1) % 4);
            snap.RestoreTo(w);   // 戻しの前後で段が変わる（元の段へ）→ 5
            Pin(b); Step(b); flush();
            GovernanceSnapshot.Capture(w).RestoreTo(w);   // 戻しても段が変わらない → 行なし
            Pin(b); Step(b); flush();
            Instruments.LawstepSideBegin(w); w.Suffrage = (Suffrage)(((int)w.Suffrage + 1) % 4); Instruments.LawstepSideEnd(w, 4);   // 譲歩の前後
            Pin(b); Step(b); flush();
        });
        Assert.Single(all[1]); Assert.Equal("5", Col(all[1][0], "writerCode")); Assert.Equal("ShorterHours", Col(all[1][0], "axis"));
        Assert.Empty(all[2]);
        Assert.Single(all[3]); Assert.Equal("4", Col(all[3][0], "writerCode")); Assert.Equal("Suffrage", Col(all[3][0], "axis"));
        Assert.False(Col(all[3][0], "massMilitancyBefore") == "nan" || Col(all[3][0], "massMilitancyAfter") == "nan");
    }

    [Fact]
    public void L4_段が動かない月は_0_行()
    {
        var all = Run((b, flush) => { Pin(b); Step(b); flush(); Step(b); flush(); Step(b); flush(); });
        Assert.All(all, rows => Assert.Empty(rows));
    }

    /// <summary>(1379) 派閥の譲歩（Politics.SettleRevolt → ConcedeToFaction）を本物の入口から通す。要求の軸と向きは盤面の選好から出る＝試験は軸を決めず、
    /// 前後の hook が書いた行（writer 4）が出ることと、その軸の段が実際に動いていることを見る。突然変異「ConcedeToFaction の LawstepSideEnd を消す」で赤。</summary>
    [Fact]
    public void L3b_派閥の譲歩の本物の入口を通ると_4_の行が出る()
    {
        var all = Run((b, flush) =>
        {
            var w = b.W;
            Pin(b); Step(b); flush();
            int concededBlocs = 0;
            for (int bloc = 0; bloc < 3; bloc++)
            {
                var f = new FactionState { Region = w.Regions[0], Bloc = bloc };
                if (FactionSnapshot.ComputeDemands(w, bloc).Count == 0) continue;
                concededBlocs++;
                Politics.SettleRevolt(w, w.Coef, true, w.Polity, f, System.Array.Empty<Pop>());
                Pin(b); Step(b); flush();
            }
            Assert.True(concededBlocs >= 1, "要求の出る派閥が盤面に無い＝試験の前提が崩れた");
        });
        var rows = all.Skip(1).SelectMany(x => x).Where(r => Col(r, "writerCode") == "4").ToList();
        Assert.True(rows.Count >= 1, "譲歩の入口を通ったのに writer 4 の行が無い");
        foreach (var r in rows) Assert.NotEqual(Col(r, "fromStage"), Col(r, "toStage"));
    }

    [Fact]
    public void L5_計器を切ると何も作られない()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_lawstep_" + System.Guid.NewGuid().ToString("N"));
        string file = System.IO.Path.Combine(dir, $"lawstep_s{Seed}.tsv");
        MultiNationSim? sim = null;
        try
        {
            // 1 度 On にして出力の場所を決める（行が出て、ファイルが作られることを先に確かめる＝切った後の「無い」が空振りでない）
            Instruments.SelectTables("lawstep_s");
            Instruments.Configure(dir, "*");
            var b = Make(); sim = b.Sim;
            Instruments.Phase(b.Sim, Seed, "open");
            Pin(b); Step(b);
            b.W.WorkHours = (WorkHours)(((int)b.W.WorkHours + 1) % 4);   // 施行の外の書き手＝次の施行で 1 行
            Pin(b); Step(b);
            Instruments.Quarter(b.Sim, Seed, 0);
            Assert.Single(Instruments.LawstepPeek(b.Sim));
            Assert.True(System.IO.File.Exists(file), "On の間に出力が作られていない（試験の前提）");
            Instruments.Close(b.Sim);
            // 切る：切っている間に段を動かして施行を通しても、行は溜まらない（On に戻して四半期の末を書いても出ない）
            Instruments.Configure(null, null); Instruments.SelectTables(null);
            b.W.WorkHours = (WorkHours)(((int)b.W.WorkHours + 1) % 4);
            Pin(b); Step(b);
            Instruments.SelectTables("lawstep_s");
            Instruments.Configure(dir, "*");
            Instruments.Quarter(b.Sim, Seed, 1);
            Assert.Empty(Instruments.LawstepPeek(b.Sim));
        }
        finally { if (sim != null) Instruments.Close(sim); Instruments.Configure(null, null); Instruments.SelectTables(null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
