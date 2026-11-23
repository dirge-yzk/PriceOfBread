using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1318) c137ak 旗 SecessionTreasuryByMoney の機構の試験（事前登録 measurements/m1318_20261123_prereg_ak.txt）。
/// 盤面は試験の中で置く：イギリスの全ての POP の貯蓄を 0 にしてから、首都 C・移る州 X（兵でない POP が 2 つ以上ある最後の州）と Y（その次に後ろの州）の兵でない POP に貯蓄を置き、国庫を置く。
/// 期待の分与は試験が置いた額の算術（max(0,国庫)×移る州の貯蓄の和÷親の貯蓄の和）で、エンジンの和（SumSavings・TotalMoney）を期待の出所にしない。
/// 分離は MultiNationSim.SpawnNation を直に呼ぶ（TreasLedgerInstrumentTests の T7 と同じ入口）。</summary>
[Collection("Instruments")]
public class SecessionTreasuryShareTests
{
    private const ulong Seed = 1318UL;

    private sealed record Board(MultiNationSim Sim, World Uk, Region C, Region X, Region Y);

    private static List<Pop> Civ(Region r) => r.Pops.Where(p => !p.IsSoldier && p.Size > 1e-9).ToList();

    /// <summary>盤面：国庫 tre・首都 C の 1 POP に c・X の POP に xs（先頭から）・Y の 1 POP に y・ほかの POP の貯蓄は 0。</summary>
    private static Board Make(double flag, double tre, double c, double[] xs, double y)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: Seed, coef: new Coefficients()) { DiplomacyFrozen = true };
        var uk = sim.Nations.First(n => Instruments.NameOf(n.World) == "イギリス").World;
        uk.Coef.SecessionTreasuryByMoney = flag;
        Assert.True(uk.Regions.Count >= 4, $"前提：イギリスの州が 4 つ以上（{uk.Regions.Count}）");
        foreach (var r in uk.Regions) foreach (var p in r.Pops) p.ApplyBakedSavings(0);
        var C = uk.Regions[0];
        var X = uk.Regions.Last(r => r != C && Civ(r).Count >= 2);   // 負の貯蓄の盤面（T6・T8）は X に 2 つの POP が要る
        var Y = uk.Regions.Last(r => r != C && r != X && Civ(r).Count >= 1);
        var px = Civ(X);
        Assert.True(px.Count >= xs.Length && Civ(C).Count >= 1 && Civ(Y).Count >= 1, "前提：貯蓄を置く POP が足りない");
        Civ(C)[0].ApplyBakedSavings(c);
        for (int i = 0; i < xs.Length; i++) px[i].ApplyBakedSavings(xs[i]);
        Civ(Y)[0].ApplyBakedSavings(y);
        foreach (var r in new[] { X, Y }) foreach (var p in r.Pops) if (p.IsSoldier) p.Militancy = 1;   // 守備兵は全て寝返る（stay＝0）＝分与の前に州の人口が動かない盤面（旧の規則の期待を試験が POP の大きさから組める）
        uk.SetTreasuryRaw(tre);
        return new(sim, uk, C, X, Y);
    }

    private static World Spawn(Board b, params Region[] regs)
    {
        var parent = b.Sim.Nations.First(n => ReferenceEquals(n.World, b.Uk));
        int n0 = b.Sim.Nations.Count;
        typeof(MultiNationSim).GetMethod("SpawnNation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(b.Sim, [parent, new List<Region>(regs), null]);
        Assert.Equal(n0 + 1, b.Sim.Nations.Count);
        return b.Sim.Nations[^1].World;
    }

    /// <summary>試験が POP の大きさを自分で足した人口（旧の規則の期待の出所）。</summary>
    private static double PopSum(IEnumerable<Region> rs) { double s = 0; foreach (var r in rs) foreach (var p in r.Pops) s += p.Size; return s; }

    private static void Near(double want, double got, string what) =>
        Assert.True(Math.Abs(want - got) <= 1e-9 * Math.Max(1.0, Math.Abs(want)), $"{what}: 期待 {want:R} 実際 {got:R}");

    [Fact]
    public void T1_旗1の分与は移る州の貯蓄の和を親の貯蓄の和で割った比で_親の減りが子の国庫()
    {
        var b = Make(1, 1e5, 1e6, [40], 10);
        double want = 1e5 * 50 / (1e6 + 50);
        double tre0 = b.Uk.Treasury.Value;
        var child = Spawn(b, b.X, b.Y);
        Near(want, child.Treasury.Value, "子の国庫");
        Near(want, tre0 - b.Uk.Treasury.Value, "親の国庫の減り（保存）");
        Near((1e6 + 50) / 1e5, child.TotalMoney.Value / child.Treasury.Value, "子の民間÷国庫＝親の分離前の民間÷国庫");
    }

    [Fact]
    public void T2_旗0は今の人口の比のまま()
    {
        var b = Make(0, 1e5, 1e6, [40], 10);
        double pm = PopSum([b.X, b.Y]), pp = PopSum(b.Uk.Regions);
        double want = 1e5 * pm / pp;
        Assert.True(Math.Abs(want - 1e5 * 50 / (1e6 + 50)) > 1.0, $"前提：旧と新の規則が見分けられない（{want}）");
        var child = Spawn(b, b.X, b.Y);
        Near(want, child.Treasury.Value, "子の国庫（旧の規則）");
    }

    [Fact]
    public void T3_移る州の貯蓄が大きい盤面でも分母は移す前の親の貯蓄の和()
    {
        var b = Make(1, 1e5, 1e6, [5e5], 10);
        var child = Spawn(b, b.X, b.Y);
        Near(1e5 * (5e5 + 10) / (1e6 + 5e5 + 10), child.Treasury.Value, "子の国庫");
    }

    [Fact]
    public void T4_片側の貨幣が0なら分与は0()
    {
        var b = Make(1, 1e5, 1e6, [0], 0);   // 移る州の貯蓄 0
        double tre0 = b.Uk.Treasury.Value;
        var child = Spawn(b, b.X, b.Y);
        Assert.Equal(0.0, child.Treasury.Value);
        Assert.Equal(tre0, b.Uk.Treasury.Value);

        var b2 = Make(1, 1e5, -50, [40], 10);   // 親の貯蓄の和 0（首都の借越 −50＋40＋10）
        double tre2 = b2.Uk.Treasury.Value;
        var child2 = Spawn(b2, b2.X, b2.Y);
        Assert.Equal(0.0, child2.Treasury.Value);
        Assert.Equal(tre2, b2.Uk.Treasury.Value);
    }

    [Fact]
    public void T5_負の国庫からは分与しない()
    {
        var b = Make(1, -1e5, 1e6, [40], 10);
        var child = Spawn(b, b.X, b.Y);
        Assert.Equal(0.0, child.Treasury.Value);
        Assert.Equal(-1e5, b.Uk.Treasury.Value);
    }

    [Fact]
    public void T6_移る州の負の貯蓄は分子と分母の両方に入る()
    {
        var b = Make(1, 1e5, 1e6, [60, -20], 10);   // X の和は 40（T1 と同じ）
        var child = Spawn(b, b.X, b.Y);
        Near(1e5 * 50 / (1e6 + 50), child.Treasury.Value, "子の国庫");
    }

    [Fact]
    public void T7_残る州の貯蓄の和が負で比が1を超える盤面も削らない_保存は保つ()
    {
        var b = Make(1, 1e5, -500, [400], 200);   // 移る 600・親の和 100＝比 6
        double tre0 = b.Uk.Treasury.Value;
        var child = Spawn(b, b.X, b.Y);
        Near(1e5 * 600 / 100, child.Treasury.Value, "子の国庫");
        Near(tre0 - 6e5, b.Uk.Treasury.Value, "親の国庫（負へ）");
    }

    // ---- 計器 sectreas_s

    private static void WithInstruments(Action body)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_st_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try { body(); }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    private static Dictionary<string, string> OnlyRow(MultiNationSim sim, int q)
    {
        var h = Instruments.SecTreasHeaderText.Split('\t');
        var rows = Instruments.SecTreasPeek(sim).Select(l => l.Split('\t')).ToList();
        Assert.Single(rows);
        Assert.Equal(h.Length, rows[0].Length);
        Assert.Equal(q.ToString(), rows[0][0]);
        return h.Zip(rows[0]).ToDictionary(x => x.First, x => x.Second);
    }
    private static double D(Dictionary<string, string> r, string k) => double.Parse(r[k], System.Globalization.CultureInfo.InvariantCulture);

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void T8_計器は同じ盤面で旧と新の規則と実際の分与を並べる(double flag)
    {
        WithInstruments(() =>
        {
            var b = Make(flag, 1e5, 1e6, [60, -20], 10);
            double pm = PopSum([b.X, b.Y]), pp = PopSum(b.Uk.Regions);
            double wantOld = 1e5 * pm / pp, wantNew = 1e5 * 50 / (1e6 + 50);
            Instruments.Quarter(b.Sim, Seed, 0);
            Assert.Empty(Instruments.SecTreasPeek(b.Sim));
            var child = Spawn(b, b.X, b.Y);
            Instruments.Quarter(b.Sim, Seed, 1);
            var r = OnlyRow(b.Sim, 1);
            Assert.Equal("イギリス", r["parent"]); Assert.Equal(b.X.Name, r["firstRegion"]); Assert.Equal("2", r["regions"]);
            Assert.Equal("0", r["capitalRisingFlag"]); Assert.Equal(flag > 0 ? "1" : "0", r["byMoneyFlag"]); Assert.Equal("0", r["shareOverOneFlag"]);
            Near(50, D(r, "movedMoney"), "movedMoney"); Near(-20, D(r, "movedNegSavings"), "movedNegSavings"); Near(1e6 + 50, D(r, "parentMoney"), "parentMoney");
            Near(pm, D(r, "movedPopK"), "movedPopK"); Near(pp, D(r, "parentPopK"), "parentPopK"); Near(1e5, D(r, "parentTreasury"), "parentTreasury");
            Near(wantOld, D(r, "oldRuleT"), "oldRuleT"); Near(wantNew, D(r, "newRuleT"), "newRuleT");
            Near(flag > 0 ? wantNew : wantOld, D(r, "actualT"), "actualT");
            Near(child.Treasury.Value, D(r, "actualT"), "actualT＝子の国庫");
            Instruments.Quarter(b.Sim, Seed, 2);
            Assert.Empty(Instruments.SecTreasPeek(b.Sim));
        });
    }
}
