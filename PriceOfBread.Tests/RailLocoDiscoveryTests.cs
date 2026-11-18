using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1130) 線 c110lo：旗 RailLocoNeedsDiscovery（機関車の需要と払いに鉄道の発見の門・線路には掛けない＝案 B）の機構の番人と、計器 railspend_s の自己検査。
/// 事前登録 measurements/m1130_20261114_prereg_lo.txt の xUnit T1〜T3。期待の値は試験の盤面の旗 0 の走り（同じ出所の式を写さない）。
/// </summary>
[Collection("Instruments")]
public class RailLocoDiscoveryTests
{
    static World Board(double flag, bool locoKnown)
    {
        var sc = NationConfig.Britain().ToScenario(41, sharedWorld: true);
        var co = new Coefficients { RailLocoNeedsDiscovery = flag };
        typeof(Scenario).GetProperty(nameof(Scenario.Coef))!.SetValue(sc, co);
        var w = new Simulation(sc, new NullAgent()).World;
        foreach (var r in w.Regions) if (r.InvestorPopIndex >= 0) r.Pops[r.InvestorPopIndex].EarnedPerMonth = new MoneyPerMonth(0.05 * Math.Max(0, r.Pops[r.InvestorPopIndex].Savings.Value));   // (1223) 盤面の前提：投資家の前の月の稼ぎ（旗 InvestBudgetFromEarnings＝1 の建設の予算）を旧の予算（貯蓄の 5%）と同じ額に置く＝私的な建設の月が立つ
        w.Discovered[Catalog.DiscoveryRowOf(Catalog.LocomotiveId, "")] = locoKnown;
        foreach (var r in w.Regions) r.Discovered = w.Discovered;   // 月の頭（Simulation の州の設定）と同じ＝国の配列への参照
        return w;
    }

    static void RunAll(World w) { foreach (var r in w.Regions) EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w); }

    [Fact]
    public void T1_未発見の州で旗1は機関車の需要が0_線路の需要は残る()
    {
        var off = Board(0.0, false); var on = Board(1.0, false);
        RunAll(off); RunAll(on);
        int loco = Catalog.LocomotiveId, rail = Catalog.RailId;
        double offLoco = 0, onLoco = 0; int railBoth = 0, railOff = 0;
        for (int i = 0; i < off.Regions.Count; i++)
        {
            var a = off.Regions[i]; var b = on.Regions[i];
            Assert.Equal(a.Name, b.Name);
            offLoco += a.Markets[loco].LastDemand;
            Assert.True(b.Markets[loco].LastDemand == 0.0, $"{b.Name}：旗 1・未発見で機関車の需要 {b.Markets[loco].LastDemand:R}");
            onLoco += b.Markets[loco].LastDemand;
            if (a.Markets[rail].LastDemand > 0) { railOff++; if (b.Markets[rail].LastDemand > 0) railBoth++; }
        }
        Assert.True(offLoco > 0, $"前提：旗 0 の盤面で機関車の需要の和が正でない（{offLoco:R}）");
        Assert.True(railOff >= 3, $"前提：旗 0 で線路の需要のある州 {railOff}");
        Assert.Equal(railOff, railBoth);
        Assert.Equal(0.0, onLoco);
    }

    [Fact]
    public void T2_発見の後は旗1と旗0で全ての財の需要と値段がビットで同じ()
    {
        var off = Board(0.0, true); var on = Board(1.0, true);
        RunAll(off); RunAll(on);
        int checkedRegions = 0;
        for (int i = 0; i < off.Regions.Count; i++)
        {
            var a = off.Regions[i]; var b = on.Regions[i];
            for (int g = 0; g < a.Markets.Length; g++)
            {
                Assert.True(a.Markets[g].LastDemand.Equals(b.Markets[g].LastDemand), $"{a.Name} 財#{g} の需要 {a.Markets[g].LastDemand:R} ≠ {b.Markets[g].LastDemand:R}");
                Assert.True(a.Markets[g].Price.Equals(b.Markets[g].Price), $"{a.Name} 財#{g} の値段 {a.Markets[g].Price:R} ≠ {b.Markets[g].Price:R}");
            }
            if (a.Markets[Catalog.LocomotiveId].LastDemand > 0) checkedRegions++;
        }
        Assert.True(checkedRegions >= 1, $"前提：機関車の需要のある州 {checkedRegions}");
    }

    static double[] RunIns(double flag, out int rows, out string dir)
    {
        const ulong Seed = 1789UL;
        dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_rs_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        Instruments.SelectTables("railspend_s");
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], Seed, _ => new UtilityAgent(), null);
        foreach (var n in sim.Nations) { n.World.Coef.CivilWarLandslideRatio = 0; n.World.Coef.RailLocoNeedsDiscovery = flag; }
        sim.DiplomacyFrozen = true;
        try
        {
            Instruments.Phase(sim, Seed, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, Seed, "warm");
            for (int q = 0; q < 8; q++) { sim.StepQuarter(); Instruments.Quarter(sim, Seed, q); }
        }
        finally { Instruments.Close(sim); }
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"railspend_s{Seed}.tsv"));
        var h = lines[0].Split('\t');
        Assert.Equal(Instruments.RailSpendHeaderText.Split('\t'), h);
        var sum = new double[h.Length];
        rows = 0;
        foreach (var l in lines.Skip(1))
        {
            var x = l.Split('\t'); Assert.Equal(h.Length, x.Length);
            Assert.Equal("3", x[4]);
            for (int i = 4; i < h.Length; i++)
            {
                double v = double.Parse(x[i], System.Globalization.CultureInfo.InvariantCulture);
                Assert.True(v >= 0, $"{h[i]} が負 {v}");
                sum[i] += v;
            }
            rows++;
        }
        return sum;
    }

    [Fact]
    public void T3_計器railspend_sは旗0で発見の前の機関車の払いが正_旗1で0_線路は残る()
    {
        string d0 = "", d1 = "";
        try
        {
            var s0 = RunIns(0.0, out int r0, out d0);
            var s1 = RunIns(1.0, out int r1, out d1);
            var h = Instruments.RailSpendHeaderText.Split('\t');
            int C(string n) { int i = Array.IndexOf(h, n); Assert.True(i >= 0, n); return i; }
            Assert.True(r0 >= 4 * 8 && r1 == r0, $"行の数 {r0}・{r1}");
            Assert.Equal(s0[C("months")], s0[C("monthsPre")]);   // 誰も機関車を知らない期間
            double pre0 = s0[C("preBuildLocoAmtGold")] + s0[C("preMaintLocoAmtGold")], pre1 = s1[C("preBuildLocoAmtGold")] + s1[C("preMaintLocoAmtGold")];
            Assert.True(pre0 > 0, $"前提：旗 0 で発見の前の機関車の払い {pre0:R}");
            Assert.Equal(0.0, pre1);
            Assert.Equal(0.0, s1[C("preMaintLocoQty")] + s1[C("preBuildLocoQty")]);
            Assert.True(s1[C("preBuildRailAmtGold")] + s1[C("preMaintRailAmtGold")] > 0, "旗 1 で発見の前の線路の払いが 0");
            foreach (var n in h) if (n.StartsWith("post", StringComparison.Ordinal)) { Assert.Equal(0.0, s0[C(n)]); Assert.Equal(0.0, s1[C(n)]); }
        }
        finally
        {
            Instruments.SelectTables(null); Instruments.Configure(null, null);
            foreach (var d in new[] { d0, d1 }) if (d.Length > 0) try { System.IO.Directory.Delete(d, true); } catch (System.IO.IOException) { }
        }
    }
}
