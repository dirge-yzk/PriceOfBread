using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(688) 線 navf5 (b)：旗 <see cref="Coefficients.NavalCedeFleetReturn"/>（割譲のとき出ていく州の艦の在庫を送り手の首都の州へ戻す）の番人。既定 0＝今のまま（艦ごと渡る）。
/// 期待値は手で置いた艦の在庫（財ごとの隻数）から独立に作る（実装の式を写さない）。突然変異で赤になる物（記録＝`measurements/navf688_20261030_mutation.txt`）：
/// M1 旗の条件を外す・M2 戻し先を出ていく州のままにする・M3 how の条件を外す（建国も戻す）・M4 首都が出るとき戻し先を null にする・M5 出ていく州の艦を空にしない（二重に数える）・M6 戻し先の在庫を作らず捨てる（〈送り手の州が 0 のときの守り〉は homeReg の null 判定が担う＝同値の突然変異で単独では作らなかった）。</summary>
public class NavalCedeFleetReturnTests
{
    private static MultiNationSim Frozen(params string[] names)
        => new(System.Array.ConvertAll(names, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };

    private static double Total(World w) { double s = 0; foreach (var r in w.Regions) if (r.FleetUnits is { } f) foreach (double x in f) s += x; return s; }
    private static double Sum(double[]? a) { double s = 0; if (a is not null) foreach (double x in a) s += x; return s; }

    private static (World from, World to, Region r) Setup(double flag, bool ceddCapital)
    {
        var sim = Frozen("イギリス", "フランス");
        var from = sim.Nations[0].World; var to = sim.Nations[1].World;
        from.Coef.NavalCedeFleetReturn = flag;
        for (int d = 0; d < 60; d++) sim.StepDay();
        Assert.True(from.Regions.Count >= 3, "前提が崩れた：英の州が3つ未満");
        foreach (var q in from.Regions) q.FleetUnits = null;
        foreach (var q in to.Regions) q.FleetUnits = null;
        var cap = from.Region;
        var r = ceddCapital ? cap : from.Regions[2];
        cap.FleetUnits = new double[] { 1, 2, 3 };
        r.FleetUnits = new double[] { 10, 20, 30 };   // 首都を割譲する盤面では首都の艦 1,2,3 を 10,20,30 で置き直す（出ていく州の艦＝10,20,30）
        return (from, to, r);
    }

    [Fact]
    public void 旗1で割譲の州の艦は送り手の首都の州へ戻り受け手に渡らない()
    {
        var (from, to, r) = Setup(1.0, false);
        var cap = from.Region;
        double before = Total(from) + Total(to);
        Territory.TransferRegion(from, to, r, Territory.Rehoming.Ceded);
        Assert.Null(r.FleetUnits);
        Assert.Equal(new double[] { 11, 22, 33 }, from.Region.FleetUnits);   // 首都の元の艦 1,2,3 に 10,20,30 が足された
        Assert.Equal(0.0, Total(to));                                         // 受け手の艦は 0 のまま
        Assert.Equal(before, Total(from) + Total(to), 9);                     // 艦の総数は保存
        Assert.Contains(r, to.Regions);                                       // 州は受け手の国へ移った
    }

    [Fact]
    public void 旗0では割譲の州の艦は州ごと受け手に渡る()
    {
        var (from, to, r) = Setup(0.0, false);
        Territory.TransferRegion(from, to, r, Territory.Rehoming.Ceded);
        Assert.Equal(new double[] { 10, 20, 30 }, r.FleetUnits);
        Assert.Equal(60.0, Total(to), 9);
        Assert.Equal(new double[] { 1, 2, 3 }, from.Region.FleetUnits);
    }

    [Fact]
    public void 旗1で首都の州が割譲されるときは遷都の後の新しい首都へ送る()
    {
        var (from, to, r) = Setup(1.0, true);
        var oldCap = r;
        Territory.TransferRegion(from, to, r, Territory.Rehoming.Ceded);
        Assert.Null(oldCap.FleetUnits);
        Assert.Equal(0.0, Total(to));
        Assert.Equal(0, from.Region.Id);
        Assert.False(ReferenceEquals(from.Region, oldCap));
        Assert.Equal(new double[] { 10, 20, 30 }, from.Region.FleetUnits);   // 出ていった首都の艦 10,20,30 が新しい首都にだけある
        double others = 0; foreach (var q in from.Regions) if (!ReferenceEquals(q, from.Region)) others += Sum(q.FleetUnits);
        Assert.Equal(0.0, others);
    }

    [Fact]
    public void 旗1でも建国と送り手の州が0になる割譲は艦が州に付いて行く()
    {
        // 建国（Seceded・to＝null）は旗を見ない
        var (from, _, r) = Setup(1.0, false);
        Territory.TransferRegion(from, null, r, Territory.Rehoming.Seceded);
        Assert.Equal(new double[] { 10, 20, 30 }, r.FleetUnits);
        Assert.Equal(new double[] { 1, 2, 3 }, from.Region.FleetUnits);
        // 送り手の州が 0 になる割譲（国が消える）は艦ごと渡る
        var (from2, to2, r2) = Setup(1.0, false);
        foreach (var q in from2.Regions.ToList()) if (!ReferenceEquals(q, r2)) from2.Regions.Remove(q);
        r2.Id = 0;
        Territory.TransferRegion(from2, to2, r2, Territory.Rehoming.Ceded);
        Assert.Equal(new double[] { 10, 20, 30 }, r2.FleetUnits);
        Assert.Equal(60.0, Total(to2), 9);
    }
}
