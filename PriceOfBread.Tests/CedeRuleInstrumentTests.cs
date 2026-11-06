using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(888) c75ce 割譲の支配度 C1 の段 C-0 の表 cederule_s（計器・読むだけ・事前登録 measurements/m888_20261106_prereg_ce.txt）。
/// 州の移し（<see cref="Territory.TransferRegion"/>）を手で踏み、StepQuarter を挟まずに <see cref="Instruments.Quarter"/> を呼ぶ＝移しと四半期の末の間に Rule の書き手が走らない盤面。
/// 期待値は試験が移す前に写した盤面の値と手のリテラルから作る（計器の列どうしで比べない）。
/// 突然変異（使い捨ての木で 1 本ずつ・結果は measurements/m888_20261106/mut888.txt）：鍵を Name だけ→T2・印を外す→T5・movedAgain を持ち主≠受け手だけ→T4・year を Quarter の時→T7・助走を捨てない→T6・門の許しを 1 へ→T8・差に持分を入れない→T8・前を Rehome の後に読む→T1（(904) で tierBefore／tierAfter の Assert を足した。(888) では緑のまま＝穴だった）。
/// 計器の全体の口（<c>Instruments.Configure</c>）を立てるので、並列を外す集まり〈Instruments〉に入れた。</summary>
[Collection("Instruments")]
public class CedeRuleInstrumentTests
{
    static (MultiNationSim Sim, World Uk, World Fr) Setup()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        return (sim, sim.Nations[0].World, sim.Nations[1].World);
    }

    static Region Pick(World w, int nth) => w.Regions.Where(x => x.Id != 0 && x.Tier == RegionTier.Core).Skip(nth).First();

    /// <summary>計器を立てて盤面の手順を踏み、試合を閉じて cederule_s を読む。</summary>
    static (string[] H, List<string[]> Rows) Run(ulong seed, Action<MultiNationSim, World, World> steps)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cede_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, null);
        var (sim, uk, fr) = Setup();
        try { steps(sim, uk, fr); }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"cederule_s{seed}.tsv"));
        try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        var h = lines[0].Split('\t');
        Assert.Equal(Instruments.CedeHeaderText.Split('\t'), h);
        var rows = lines.Skip(1).Select(l => l.Split('\t')).ToList();
        Assert.All(rows, x => Assert.Equal(h.Length, x.Length));
        return (h, rows);
    }

    static int Col(string[] h, string c) { int i = Array.IndexOf(h, c); Assert.True(i >= 0, "列が無い " + c); return i; }
    static double D(string s) => s == "nan" ? double.NaN : s == "inf" ? double.PositiveInfinity : double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
    static void Near(double want, string got) => Assert.True(Math.Abs(want - D(got)) <= 1e-9 * Math.Max(1, Math.Abs(want)), $"want {want} got {got}");

    [Fact]
    public void T1_割譲の前と後は旗0で同じ_門は破れない()
    {
        double[] rule = [0.55, 0.25, 0.05, 0.15];
        double dom = 0.7, init1 = 0.4;
        var (h, rows) = Run(1789UL, (sim, uk, fr) =>
        {
            var r = Pick(uk, 0);
            r.Rule = (double[])rule.Clone(); r.RuleInit = [0.6, init1, 0, 0]; r.Dominance = dom;
            r.ForeignRulers = ["スペイン"]; r.ForeignShares = [0.05];
            Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Ceded);
            Instruments.Quarter(sim, 1789UL, 0);
        });
        var x = Assert.Single(rows);
        Assert.Equal("Ceded", x[Col(h, "how")]);
        Assert.Equal("イギリス", x[Col(h, "sender")]);
        Assert.Equal("フランス", x[Col(h, "receiver")]);
        Assert.Equal("フランス", x[Col(h, "ownerAtQuarterEnd")]);
        Assert.Equal("Core", x[Col(h, "tierBefore")]);   // (904) 割譲の前の格（Pick は Core の州）
        Assert.Equal("Integrated", x[Col(h, "tierAfter")]);   // 割譲で Core→Integrated（Territory.cs の Ceded 枝）
        string[] parts = ["State", "Local", "Foreign", "Ungov"];
        for (int k = 0; k < 4; k++) { Near(rule[k], x[Col(h, $"rule{parts[k]}BeforeShare")]); Near(rule[k], x[Col(h, $"rule{parts[k]}AfterShare")]); }
        Near(init1, x[Col(h, "ruleInitLocalBeforeShare")]); Near(init1, x[Col(h, "ruleInitLocalAfterShare")]);
        Near(dom, x[Col(h, "dominanceBeforeShare")]); Near(dom, x[Col(h, "dominanceAfterShare")]);
        Near(0.05, x[Col(h, "foreignShareBeforeSum")]); Near(0.05, x[Col(h, "foreignShareAfterSum")]);
        Near(rule[0] + rule[1], x[Col(h, "c1LocalShare")]);
        Assert.Equal("0", x[Col(h, "c1StateShare")]);
        Assert.Equal("0", x[Col(h, "identDiffMaxIndex")]);
        Assert.Equal("0", x[Col(h, "identBreakFlag")]);
        Assert.Equal("0", x[Col(h, "movedAgainFlag")]);
        Assert.Equal("0", x[Col(h, "foreignAddedFlag")]);
    }

    [Fact]
    public void T2_受け手の鍵は名を替えた国でもBaseNameで_BaseNameが空ならName()
    {
        var (h, rows) = Run(1790UL, (sim, uk, fr) =>
        {
            var fp = fr.SelfPower!;
            fp.Name = "フランス帝国";   // BaseName は「フランス」のまま＝鍵は BaseName
            var r1 = Pick(uk, 0);
            r1.ForeignRulers = ["フランス", "スペイン"]; r1.ForeignShares = [0.2, 0.05];
            Territory.TransferRegion(uk, fr, r1, Territory.Rehoming.Ceded);
            Instruments.Quarter(sim, 1790UL, 0);
            fp.BaseName = "";   // BaseName が空＝鍵は Name
            var r2 = Pick(uk, 0);
            r2.ForeignRulers = ["フランス帝国", "スペイン"]; r2.ForeignShares = [0.3, 0.07];
            Territory.TransferRegion(uk, fr, r2, Territory.Rehoming.Ceded);
            Instruments.Quarter(sim, 1790UL, 1);
        });
        Assert.Equal(2, rows.Count);
        Assert.Equal("フランス", rows[0][Col(h, "receiver")]);
        Near(0.2, rows[0][Col(h, "receiverOwnBeforeShare")]); Near(0.05, rows[0][Col(h, "thirdPartyBeforeSum")]);
        Near(0.2, rows[0][Col(h, "receiverOwnAfterShare")]); Near(0.05, rows[0][Col(h, "thirdPartyAfterSum")]);
        Assert.Equal("フランス帝国", rows[1][Col(h, "receiver")]);
        Near(0.3, rows[1][Col(h, "receiverOwnBeforeShare")]); Near(0.07, rows[1][Col(h, "thirdPartyBeforeSum")]);
        Assert.Equal("0", rows[0][Col(h, "haOwnShare")]);
    }

    [Fact]
    public void T3_建国と再併合の行が出る_建国の受け手は四半期の末の持ち主()
    {
        string? newName = null;
        var (h, rows) = Run(1791UL, (sim, uk, fr) =>
        {
            var r = Pick(uk, 0);
            int n0 = sim.Nations.Count;
            uk.PendingSecessionWars = [r];
            sim.SpawnSecededNations();
            Assert.Equal(n0 + 1, sim.Nations.Count);
            var nw = sim.Nations[^1].World;
            Assert.Contains(r, nw.Regions);
            newName = nw.SelfPower!.BaseName.Length > 0 ? nw.SelfPower.BaseName : nw.SelfPower.Name;
            Instruments.Quarter(sim, 1791UL, 0);
            Territory.TransferRegion(nw, uk, r, Territory.Rehoming.Returned);
            Instruments.Quarter(sim, 1791UL, 1);
        });
        Assert.Equal(2, rows.Count);
        Assert.Equal("Seceded", rows[0][Col(h, "how")]);
        Assert.Equal("イギリス", rows[0][Col(h, "sender")]);
        Assert.Equal(newName, rows[0][Col(h, "receiver")]);
        Assert.Equal(newName, rows[0][Col(h, "ownerAtQuarterEnd")]);
        Assert.Equal("nan", rows[0][Col(h, "sameCoreFlag")]);
        Assert.Equal("Returned", rows[1][Col(h, "how")]);
        Assert.Equal(newName, rows[1][Col(h, "sender")]);
        Assert.Equal("イギリス", rows[1][Col(h, "receiver")]);
        Assert.Equal("0", rows[0][Col(h, "identBreakFlag")]);
        Assert.Equal("0", rows[1][Col(h, "identBreakFlag")]);
        Assert.Equal("0", rows[0][Col(h, "movedAgainFlag")]);
        Assert.Equal("0", rows[1][Col(h, "movedAgainFlag")]);
    }

    [Fact]
    public void T4_同じ四半期に2回動いた州は2行とも印が立つ_割譲の行でも()
    {
        var (h, rows) = Run(1792UL, (sim, uk, fr) =>
        {
            var r = Pick(uk, 0); var other = Pick(uk, 1);
            Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Ceded);
            Territory.TransferRegion(fr, uk, r, Territory.Rehoming.Ceded);   // 同じ四半期に送り手へ戻る（2 回目も割譲）
            Territory.TransferRegion(uk, fr, other, Territory.Rehoming.Ceded);   // 1 回だけ動いた州
            Instruments.Quarter(sim, 1792UL, 0);
        });
        Assert.Equal(3, rows.Count);
        Assert.Equal(["1", "1", "0"], rows.Select(x => x[Col(h, "movedAgainFlag")]).ToArray());
        Assert.Equal(["フランス", "イギリス", "フランス"], rows.Select(x => x[Col(h, "receiver")]).ToArray());
        Assert.All(rows.Take(2), x => Assert.Equal("イギリス", x[Col(h, "ownerAtQuarterEnd")]));
    }

    [Fact]
    public void T5_同じ四半期に外国持分が足された州は印が立つ()
    {
        var (h, rows) = Run(1793UL, (sim, uk, fr) =>
        {
            var r = Pick(uk, 0); var other = Pick(uk, 1);
            Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Ceded);
            Territory.TransferRegion(uk, fr, other, Territory.Rehoming.Ceded);
            Politics.AddForeignShare(r, "スペイン", 0.1);   // 講和の条約港の書き手（Diplomacy.cs の呼び口）が同じ四半期に当たった
            Instruments.Quarter(sim, 1793UL, 0);
        });
        Assert.Equal(2, rows.Count);
        Assert.Equal("1", rows[0][Col(h, "foreignAddedFlag")]);
        Assert.Equal("0", rows[1][Col(h, "foreignAddedFlag")]);
        Near(0.1, rows[0][Col(h, "thirdPartyAfterSum")]);
        Assert.Equal("1", rows[0][Col(h, "identBreakFlag")]);   // 持分の要素の数が増えた＝門の母集合から印で外す行
        Assert.Equal("0", rows[1][Col(h, "identBreakFlag")]);
    }

    [Fact]
    public void T6_助走の間の割譲は行にならない()
    {
        var (_, rows) = Run(1794UL, (sim, uk, fr) =>
        {
            var r = Pick(uk, 0); var other = Pick(uk, 1);
            uk.WarmupFrozen = true;
            Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Ceded);
            uk.WarmupFrozen = false;
            Instruments.Quarter(sim, 1794UL, 0);
            Assert.Empty(Instruments.CedeRowsPending(sim.Diplomacy));
            Territory.TransferRegion(uk, fr, other, Territory.Rehoming.Ceded);   // 助走の外の割譲は行になる（試験そのものが空振りでないことの確かめ）
            Instruments.Quarter(sim, 1794UL, 1);
        });
        var x = Assert.Single(rows);
        Assert.Equal("1", x[0]);
    }

    [Fact]
    public void T7_四半期はQuarterのq_年は移した時の送り手の年()
    {
        var (h, rows) = Run(1795UL, (sim, uk, fr) =>
        {
            var r = Pick(uk, 0);
            uk.Year = 1790; fr.Year = 1790;
            Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Ceded);
            uk.Year = 1791; fr.Year = 1791;   // 手の盤面で年を跨がせる（実の盤面では Year の繰り上げは共有層より前＝同じ値）
            Instruments.Quarter(sim, 1795UL, 29);
        });
        var x = Assert.Single(rows);
        Assert.Equal("29", x[Col(h, "quarter")]);
        Assert.Equal("1790", x[Col(h, "year")]);
    }

    [Fact]
    public void T8_計器の門は人工の破れを落とす_支配の区分と外国持分の両方()
    {
        var (h, rows) = Run(1796UL, (sim, uk, fr) =>
        {
            var r = Pick(uk, 0); var other = Pick(uk, 1);
            other.ForeignRulers = ["スペイン"]; other.ForeignShares = [0.05];
            Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Ceded);
            Territory.TransferRegion(uk, fr, other, Territory.Rehoming.Ceded);
            r.Rule[0] += 1e-6; r.Rule[3] -= 1e-6;   // 割譲から四半期の末までに Rule を動かす人工の書き手
            other.ForeignShares[0] -= 1e-6;          // 持分を減らす＝印（増えた）は立たない・門だけが見る
            Instruments.Quarter(sim, 1796UL, 0);
        });
        Assert.Equal(2, rows.Count);
        foreach (var x in rows)
        {
            Assert.Equal("1", x[Col(h, "identBreakFlag")]);
            Assert.True(D(x[Col(h, "identDiffMaxIndex")]) >= 0.9e-6, x[Col(h, "identDiffMaxIndex")]);
            Assert.Equal("0", x[Col(h, "foreignAddedFlag")]);
        }
    }
}
