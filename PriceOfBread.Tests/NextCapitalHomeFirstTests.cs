using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1304) c135nc 旗 NextCapitalHomeFirst の機構の試験（事前登録 measurements/m1304_20261122_prereg_nc.txt）。
/// 盤面は試験の中で置く：フランスの州を全て植民地格・産出 0 にしてから、A（首都・Core）・B（植民地格・産出 大）・C（Integrated・中）などを置く。
/// 期待の州は試験が置いた格と値から字で決める（エンジンの選びの関数を期待の出所にしない）。</summary>
[Collection("Instruments")]
public class NextCapitalHomeFirstTests
{
    private const ulong Seed = 1304UL;

    private static (MultiNationSim Sim, World Fr, World Br) Board(double flag)
    {
        var sim = new MultiNationSim([WorldData.ByName("フランス"), WorldData.ByName("イギリス")], seed: Seed, coef: new Coefficients()) { DiplomacyFrozen = true };
        var fr = sim.Nations[0].World; var br = sim.Nations[1].World;
        fr.Coef.NextCapitalHomeFirst = flag;
        Assert.True(fr.Regions.Count >= 6, $"前提：フランスの州が 6 つ以上（{fr.Regions.Count}）");
        foreach (var r in fr.Regions) { foreach (var s in r.Producers) s.Output = 0; r.Tier = RegionTier.Colonial; }
        fr.Regions[0].Tier = RegionTier.Core;
        return (sim, fr, br);
    }

    /// <summary>州の産出の名目の値を v にする（サービス以外の財の生産者 1 つだけに産出を置く）。</summary>
    private static void Val(Region r, double v)
    {
        foreach (var s in r.Producers)
        {
            var g = r.GoodOf(s.GoodId);
            if (g is null || g.Tier == GoodTier.Service || !(g.RefPrice > 0)) continue;
            s.Output = v / g.RefPrice; return;
        }
        Assert.Fail($"前提：{r.Name} にサービス以外の財の生産者が無い");
    }

    // T1 盤面：A 首都 Core 1・B 植民地格 1e9・C Integrated 1e6・ほかは植民地格 0
    private static (Region A, Region B, Region C) T1Board(World fr)
    {
        Region a = fr.Regions[0], b = fr.Regions[1], c = fr.Regions[2];
        Val(a, 1); Val(b, 1e9); Val(c, 1e6); c.Tier = RegionTier.Integrated;
        return (a, b, c);
    }

    [Fact]
    public void T1_割譲の後の首都は本国格のC_旗0では植民地格のB()
    {
        var (_, fr, br) = Board(1.0);
        var (a, b, c) = T1Board(fr);
        Territory.TransferRegion(fr, br, a, Territory.Rehoming.Ceded);
        Assert.Same(c, fr.Regions[0]); Assert.Equal(0, c.Id); Assert.Single(fr.Regions, r => r.Id == 0);

        var (_, fr0, br0) = Board(0.0);
        var (a0, b0, _) = T1Board(fr0);
        Territory.TransferRegion(fr0, br0, a0, Territory.Rehoming.Ceded);
        Assert.Same(b0, fr0.Regions[0]);   // 旗 0＝今の形（格を見ない）
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(0.0)]
    public void T2_本国格が無い時は今と同じ全ての州から選ぶ(double flag)
    {
        var (_, fr, br) = Board(flag);
        Region a = fr.Regions[0], b = fr.Regions[1], d = fr.Regions[3];
        Val(a, 1); Val(b, 1e9); Val(d, 1e3);
        Territory.TransferRegion(fr, br, a, Territory.Rehoming.Ceded);
        Assert.Same(b, fr.Regions[0]); Assert.Equal(0, b.Id); Assert.Single(fr.Regions, r => r.Id == 0);
    }

    [Fact]
    public void T3_同点はIdの小さい方()
    {
        var (_, fr, br) = Board(1.0);
        Region a = fr.Regions[0], b = fr.Regions[1], c = fr.Regions[2], e = fr.Regions[4];
        Val(a, 1); Val(b, 1e9); c.Tier = RegionTier.Integrated; e.Tier = RegionTier.Integrated;   // C と E は同じ 0
        Assert.NotEqual(c.Id, e.Id);
        var want = c.Id < e.Id ? c : e;
        Territory.TransferRegion(fr, br, a, Territory.Rehoming.Ceded);
        Assert.Same(want, fr.Regions[0]);
    }

    [Fact]
    public void T4_蜂起のleavingを選ばない()
    {
        var (_, fr, _) = Board(1.0);
        Region a = fr.Regions[0], b = fr.Regions[1], c = fr.Regions[2], d = fr.Regions[3];
        Val(a, 1); Val(b, 1e9); Val(c, 1e6); Val(d, 1e3);
        c.Tier = RegionTier.Integrated; d.Tier = RegionTier.Integrated;
        Assert.Same(d, Territory.NextCapital(fr, [a, c]));   // C は leaving・B は植民地格
        d.Tier = RegionTier.Colonial;                         // leaving の外に本国格が無い＝全ての州（leaving を除く）
        Assert.Same(b, Territory.NextCapital(fr, [a, c]));
    }

    [Fact]
    public void T5_割譲の艦隊の戻り先はT1と同じC()
    {
        var (_, fr, br) = Board(1.0);
        var (a, b, c) = T1Board(fr);
        a.FleetUnits = [3.0, 4.0];
        double before = c.FleetUnits?.Sum() ?? 0, beforeB = b.FleetUnits?.Sum() ?? 0;
        Territory.TransferRegion(fr, br, a, Territory.Rehoming.Ceded);
        Assert.Equal(before + 7.0, c.FleetUnits?.Sum() ?? 0);
        Assert.Equal(beforeB, b.FleetUnits?.Sum() ?? 0);
    }

    private static void WithInstruments(Action body)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cc_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try { body(); }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    private static Dictionary<string, string> RowOf(MultiNationSim sim, int q, int nationIndex)
    {
        var h = Instruments.CapColHeaderText.Split('\t');
        var rows = Instruments.CapColPeek(sim).Select(l => l.Split('\t')).Where(v => v[0] == q.ToString() && v[2] == nationIndex.ToString()).ToList();
        Assert.Single(rows);
        Assert.Equal(h.Length, rows[0].Length);
        return h.Zip(rows[0]).ToDictionary(x => x.First, x => x.Second);
    }

    [Fact]
    public void T6_計器capcol_sの受け口の行は付け替えの回数と旧新の選びに一致する()
    {
        WithInstruments(() =>
        {
            var (sim, fr, br) = Board(1.0);
            var (a, b, c) = T1Board(fr);
            Instruments.Quarter(sim, Seed, 0);
            var r0 = RowOf(sim, 0, 0);
            Assert.Equal("0", r0["picksCount"]); Assert.Equal("0", r0["capitalChangedFlag"]); Assert.Equal(a.Name, r0["capitalRegion"]);

            Territory.TransferRegion(fr, br, a, Territory.Rehoming.Ceded);   // 1 回目：本国格の C
            Territory.TransferRegion(fr, br, c, Territory.Rehoming.Ceded);   // 2 回目：本国格が 0＝B
            Instruments.Quarter(sim, Seed, 1);
            var r1 = RowOf(sim, 1, 0);
            Assert.Equal("2", r1["picksCount"]);
            Assert.Equal("1", r1["diffOldCount"]);       // 1 回目だけ旧（B）と違う
            Assert.Equal("0", r1["diffNewCount"]);
            Assert.Equal("0", r1["diffOldHome0Count"]);
            Assert.Equal(b.Name, r1["lastActual"]); Assert.Equal(b.Name, r1["lastOldRulePick"]); Assert.Equal(b.Name, r1["lastNewRulePick"]);
            Assert.Equal("0", r1["lastPickHomeRegions"]);
            Assert.Equal("1", r1["capitalChangedFlag"]); Assert.Equal("0", r1["otherChangeFlag"]);
            Assert.Equal("Colonial", r1["capitalTier"]); Assert.Equal("0", r1["homeRegions"]); Assert.Equal("0", r1["g1Flag"]);
            Assert.Equal("1", r1["homeFirstFlag"]);

            Instruments.Quarter(sim, Seed, 2);
            var r2 = RowOf(sim, 2, 0);
            Assert.Equal("0", r2["picksCount"]); Assert.Equal("0", r2["capitalChangedFlag"]); Assert.Equal("-", r2["lastActual"]);

            fr.SetCapital(fr.Regions[^1]);   // 受け口を通らない遷都（Formations の形）
            Instruments.Quarter(sim, Seed, 3);
            var r3 = RowOf(sim, 3, 0);
            Assert.Equal("0", r3["picksCount"]); Assert.Equal("1", r3["capitalChangedFlag"]); Assert.Equal("1", r3["otherChangeFlag"]);
        });
    }

    [Fact]
    public void T7_旗0の計器の行は旧の選びと一致し_g1Flagが立つ()
    {
        WithInstruments(() =>
        {
            var (sim, fr, br) = Board(0.0);
            var (a, b, c) = T1Board(fr);
            Instruments.Quarter(sim, Seed, 0);
            Territory.TransferRegion(fr, br, a, Territory.Rehoming.Ceded);
            Instruments.Quarter(sim, Seed, 1);
            var r1 = RowOf(sim, 1, 0);
            Assert.Equal("1", r1["picksCount"]);
            Assert.Equal(b.Name, r1["lastActual"]); Assert.Equal(b.Name, r1["lastOldRulePick"]); Assert.Equal(c.Name, r1["lastNewRulePick"]);
            Assert.Equal("0", r1["diffOldCount"]); Assert.Equal("1", r1["diffNewCount"]);
            Assert.Equal("1", r1["homeRegions"]); Assert.Equal("Colonial", r1["capitalTier"]); Assert.Equal("0", r1["civilWarFlag"]); Assert.Equal("1", r1["g1Flag"]);
            Assert.Equal("0", r1["homeFirstFlag"]);
        });
    }
}
