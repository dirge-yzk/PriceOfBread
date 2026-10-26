using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (579) 線 ukf：AI の国営工場の着工の門（<c>UtilityAgent.TryBuildFacility</c>）の番人。総費用の身の丈の門（総費用 ≤ 1.0×バッファ）は (579) で削除した。
/// 総費用がバッファを超える重工業でも、月の出費の上限（0.10×バッファ）・走っている工事の月の出費・国家の支払い率 r の門が許せば着工する。月の出費の上限は外さない。
/// 期待値は SelfPlay の式から作らない：見込みは <c>NewFacilityOutlayPerMonth</c>（着工の記帳と一致することが別の番人で確かめてある）、バッファは Last* の請求を外から置いて作る。
/// </summary>
public class AiBuildOutlayGateTests
{
    private static MultiNationSim FourNations()
        => new([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], seed: 1789);

    /// <summary>軽工業でなく（総費用が 3 万を超える）、発見の門が開いていて、エンジンの動詞が掴む個体の生産者。</summary>
    private static Producer? PickHeavy(Region rg, int skip = 0)
        => rg.Producers.Where(p => Catalog.Goods[p.GoodId].Tier != GoodTier.Service && !p.Bankrupt && p.ConstructionLeft <= 0
                                   && ReferenceEquals(World.FindFactory(rg, p.GoodId), p)
                                   && Catalog.FactorySpec(p.GoodId).Category != "軽工業"
                                   && Discovery.Gate(rg, p) > 0).Skip(skip).FirstOrDefault();

    private static Producer PickLight(Region rg)
        => rg.Producers.Where(p => Catalog.Goods[p.GoodId].Tier != GoodTier.Service && !p.Bankrupt && p.ConstructionLeft <= 0
                                   && ReferenceEquals(World.FindFactory(rg, p.GoodId), p)
                                   && Catalog.FactorySpec(p.GoodId).Category == "軽工業"
                                   && Discovery.Gate(rg, p) > 0).First();

    private static void Plant(Region rg, Producer pr)   // 不足の深い黒字財＝AI が建てたい候補
    {
        rg.Markets[pr.GoodId].Price = 2.0 * Catalog.RefPrices[pr.GoodId] * Math.Max(rg.PriceLevel, 1e-9);
        pr.SmoothedMargin = 0.5;
    }

    /// <summary>バッファを bufferMonthly×5 に置く（行政の請求だけを入れ、他の Last* は 0）。国庫は十分に大きく r は 1。月の出費の上限＝0.10×バッファ。</summary>
    private static (World w, Region rg, Producer pr, double est) Heavy(double bufferMonthly)
    {
        var sim = FourNations();
        var w = sim.Nations[2].World;   // イギリス
        w.LastAdminClaimPerMonth = new MoneyPerMonth(bufferMonthly);
        w.LastInterestPerMonth = new MoneyPerMonth(0);
        w.LastMilitaryClaimPerMonth = new MoneyPerMonth(0);
        w.SetTreasuryRaw(1e9);
        w.FiscalFundingRatio = new Share(1.0);
        var rg = w.Regions.First(r => PickHeavy(r) is not null);
        var pr = PickHeavy(rg)!;
        Plant(rg, pr);
        double est = UtilityAgent.NewFacilityOutlayPerMonth(w, rg, pr);
        Assert.True(est > 0);
        Assert.True(Catalog.FactorySpec(pr.GoodId).Cost > UtilityAgent.FiscalBufferOf(w), "前提：この工場の総費用はバッファを超える＝旧の身の丈の門は落とす");
        return (w, rg, pr, est);
    }

    [Fact]
    public void 総費用がバッファを超える工場も月の出費の上限が足りれば着工する()
    {
        var (w, _, pr, est) = Heavy(10_000);
        Assert.True(est < 0.10 * UtilityAgent.FiscalBufferOf(w), "前提：見込みは月の出費の上限の内");
        new UtilityAgent().DecideQuarter(w);
        Assert.True(pr.ConstructionLeft > 0, "総費用の身の丈の門は無い");
    }

    [Fact]
    public void 月の出費の上限を越える工事は着工しない()
    {
        var (w, _, pr, est) = Heavy(1_000);   // バッファ 5 千＝月の出費の上限 500
        Assert.True(est > 0.10 * UtilityAgent.FiscalBufferOf(w), "前提：見込みは月の出費の上限を超える");
        new UtilityAgent().DecideQuarter(w);
        Assert.True(pr.ConstructionLeft <= 0, "月の出費の上限は外さない");
    }

    [Fact]
    public void 国家の支払い率が1に届かない月は着工しない()
    {
        var (w, _, pr, _) = Heavy(10_000);
        w.FiscalFundingRatio = new Share(0.99);
        new UtilityAgent().DecideQuarter(w);
        Assert.True(pr.ConstructionLeft <= 0, "借入と同じ規律の門は残す");
    }

    [Fact]
    public void 走っている工事の月の出費は上限に数える()
    {
        // 走っている巨大な工事（月の出費 約 300 万）がある国：月の出費の上限（50 万）を食い尽くして軽工業すら着工しない。
        Assert.False(Run(), "走っている工事は数える（数えなければ国庫を食う向きの守りが消える）");

        bool Run()
        {
            var sim = FourNations();
            var w = sim.Nations[2].World;
            w.LastAdminClaimPerMonth = new MoneyPerMonth(1_000_000);   // バッファ 500 万＝月の出費の上限 50 万
            w.SetTreasuryRaw(1.5 * UtilityAgent.FiscalBufferOf(w));
            w.FiscalFundingRatio = new Share(1.0);
            var rg = w.Regions[0];
            var light = PickLight(rg);
            Plant(rg, light);
            var other = rg.Producers.First(p => !ReferenceEquals(p, light) && Catalog.Goods[p.GoodId].Tier != GoodTier.Service);
            other.ConstructionLeft = 400; other.ConstructionPayPerDay = 1e5;
            new UtilityAgent().DecideQuarter(w);
            return light.ConstructionLeft > 0;
        }
    }

    [Fact]
    public void 着工のたびに余地を引き二つ目は余地が尽きれば飛ばす()
    {
        var (w, rg1, pr1, est1) = Heavy(10_000);
        int ri1 = w.Regions.IndexOf(rg1);
        var rg2 = w.Regions.Where((r, i) => i != ri1).First(r => r.Producers.Any(p => p.GoodId == pr1.GoodId && ReferenceEquals(World.FindFactory(r, p.GoodId), p) && Discovery.Gate(r, p) > 0));
        var pr2 = rg2.Producers.First(p => p.GoodId == pr1.GoodId && ReferenceEquals(World.FindFactory(rg2, p.GoodId), p));
        Plant(rg2, pr2);
        double est2 = UtilityAgent.NewFacilityOutlayPerMonth(w, rg2, pr2);
        w.LastAdminClaimPerMonth = new MoneyPerMonth((est1 + est2 * 0.3) / 0.5);   // 月の出費の上限＝0.10×5×これ＝1 件分と 2 件目の 3 割
        new UtilityAgent().DecideQuarter(w);
        int started = (pr1.ConstructionLeft > 0 ? 1 : 0) + (pr2.ConstructionLeft > 0 ? 1 : 0);
        Assert.Equal(1, started);
    }
}
