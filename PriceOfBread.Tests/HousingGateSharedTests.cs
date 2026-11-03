using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(805) 線 c12cm：住居の門（<see cref="PopDecision.HousingGateOpen"/>）と工業の飽和（<see cref="EconomyStep.IndSaturation"/>）の 1 本化の番人。
/// 住居の門は 2 か所が読む：<c>EconomyStep.Run</c> の都市住宅の需要（住居の市場の需要が立つか）と <c>PopDecision.MigrationValues</c> の住居の費用（HousingCost が立つか）。
/// 都市の割合ちょうど・1 ulp 下・1 ulp 上に閾値を置き、2 か所が同じ側になる。期待値は盤面の都市の割合と閾値の比べを test の中で書く（述語の関数を呼ばない）。
/// 飽和は手書きの式 <c>1＋稼働資本÷IndProductivityScale</c> と一致し、設備の限界収益 <c>InvestmentReturn.Sat</c> も同じ値を返す。
/// ⚠突然変異〈述語の > を >=〉〈述語の向きを <〉〈PopDecision 側だけ >=〉〈飽和の + を -〉〈飽和の / を *〉で赤（measurements/m805_20261103_mutation.txt）。</summary>
public class HousingGateSharedTests
{
    private static World NewWorld(double threshold, out double share)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());   // 住居の市場がある盤面（UrbanFlowRealTests と同じ）
        var w = sim.World; var r = w.Region;
        share = r.SumUrbanPopulation() / r.SumPopulation();
        w.Coef.HousingUrbanThreshold = threshold < 0 ? share : threshold;
        r.Markets[Catalog.HousingId].Price = 2.0;
        r.Markets[Catalog.HousingId].LastDemand = 0;
        r.CapitalEnabled = false;
        return w;
    }

    private static (bool econ, bool migr) Open(double thrOffset)
    {
        NewWorld(-1, out double share);
        double thr = thrOffset == 0 ? share : thrOffset < 0 ? System.Math.BitDecrement(share) : System.Math.BitIncrement(share);
        var w = NewWorld(thr, out double share2);
        Assert.Equal(share, share2);   // 前提：同じ盤面は同じ都市の割合
        var r = w.Region; var c = w.Coef;
        Assert.Equal(0.03, c.HousingNeedPerUrban);   // 前提：住居の必要量は都市 1 人あたり 0.03
        var (rural, urban) = PopDecision.MigrationPair(r);
        double cost = PopDecision.MigrationValues(r, c, rural!, urban!).HousingCost;
        EconomyStep.Run(r, c, 1.0, w.Bank, w);
        bool econ = r.Markets[Catalog.HousingId].LastDemand > 0;
        return (econ, cost > 0);
    }

    [Fact]
    public void 都市の割合が閾値ちょうどなら両方の読み手で門は閉じる()
    {
        var (econ, migr) = Open(0);
        Assert.False(share_gt(0));   // 期待：share > thr は偽（等しい）
        Assert.Equal(false, econ);
        Assert.Equal(false, migr);
    }

    [Fact]
    public void 閾値が1ulp下なら両方の読み手で門は開く()
    {
        var (econ, migr) = Open(-1);
        Assert.True(share_gt(-1));   // 期待：share > BitDecrement(share) は真
        Assert.Equal(true, econ);
        Assert.Equal(true, migr);
    }

    [Fact]
    public void 閾値が1ulp上なら両方の読み手で門は閉じる()
    {
        var (econ, migr) = Open(+1);
        Assert.False(share_gt(+1));
        Assert.Equal(false, econ);
        Assert.Equal(false, migr);
    }

    /// <summary>期待値＝盤面の都市の割合と閾値の比べを test の中で書く（述語の関数を呼ばない）。</summary>
    private static bool share_gt(int off)
    {
        NewWorld(-1, out double share);
        double thr = off == 0 ? share : off < 0 ? System.Math.BitDecrement(share) : System.Math.BitIncrement(share);
        return share > thr;
    }

    [Fact]
    public void 工業の飽和は1足す稼働資本割るスケールの手書きの式と設備の限界収益のsatに一致する()
    {
        var c = new Coefficients { IndProductivityScale = 2.5, IndProductivityGain = 1.2 };
        Assert.Equal(1.0 + 3.0 / 2.5, EconomyStep.IndSaturation(3.0, c));
        Assert.Equal(1.0, EconomyStep.IndSaturation(0.0, c));
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var r = sim.World.Region; var wc = sim.World.Coef;
        wc.IndProductivityScale = 2.5; wc.IndProductivityGain = 1.2;
        r.Industrialization = 2.0; r.OldVintageCapital = 0; r.StrandedVintage = 0;
        Assert.Equal(2.0, r.ProductiveIndustrialization);   // 前提：稼働資本は 2.0
        var (_, sat, gross, _) = EconomyStep.InvestmentReturn(r, wc, 1.0);
        double want = 1.0 + 2.0 / 2.5;                      // 手書き：1.8
        Assert.Equal(want, sat);
        Assert.Equal(1.2 / (want * want), gross);           // 限界収益の総収益＝Gain÷sat²（技術の梃子 0）
    }
}
