using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (579) 線 ukf：AI の国営工場の着工の門（<c>UtilityAgent.TryBuildFacility</c>）の旗 <c>AiBuildNoCostCap</c> の番人。
/// 旗 0 は旧の式（総費用 ≤ 1.0×バッファの身の丈の門と、月の出費 ≤ 0.10×バッファの上限）のまま。旗が正なら総費用の身の丈の門だけを外す＝
/// 総費用がバッファを超える重工業でも、月の出費の上限・走っている工事の月の出費・国家の支払い率 r の門が許せば着工する。月の出費の上限は外さない。
/// 期待値は SelfPlay の式から作らない：見込みは <c>NewFacilityOutlayPerMonth</c>（着工の記帳と一致することが別の番人で確かめてある）、バッファは Last* の請求を外から置いて作る。
/// （前半では払える額−請求から着工を判断する形を旗 1 として持っていたが、国庫の在庫と月の流れを比べる形で国庫を流出させたので消した＝事前登録の追記 3。）
/// </summary>
public class AiBuildNoCostCapTests
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
    private static (World w, Region rg, Producer pr, double est) Heavy(double flag, double bufferMonthly)
    {
        var sim = FourNations();
        var w = sim.Nations[2].World;   // イギリス
        w.Coef.AiBuildNoCostCap = flag;
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
    public void 既定は0で旧の式のまま()
    {
        string ov = Coefficients.OverrideSummary();
        if (!ov.Contains("AiBuildNoCostCap", StringComparison.Ordinal)) Assert.Equal(0.0, new Coefficients().AiBuildNoCostCap);
    }

    [Fact]
    public void 旗0は月の出費の上限が足りていても身の丈の門で重工業を落とす()
    {
        var (w, _, pr, est) = Heavy(0, 10_000);   // バッファ 5 万＝月の出費の上限 5,000
        Assert.True(est < 0.10 * UtilityAgent.FiscalBufferOf(w), "前提：見込みは月の出費の上限の内＝旗が正なら着工できる大きさ");
        new UtilityAgent().DecideQuarter(w);
        Assert.True(pr.ConstructionLeft <= 0, "旗 0 は総費用の身の丈の門で落とす");
    }

    [Fact]
    public void 旗1は月の出費の上限が足りれば総費用がバッファを超える工場も着工する()
    {
        var (w, _, pr, est) = Heavy(1, 10_000);
        Assert.True(est < 0.10 * UtilityAgent.FiscalBufferOf(w), "前提：見込みは月の出費の上限の内");
        new UtilityAgent().DecideQuarter(w);
        Assert.True(pr.ConstructionLeft > 0, "旗 1 は総費用の身の丈の門を課さない");
    }

    [Fact]
    public void 旗1でも月の出費の上限を越える工事は着工しない()
    {
        var (w, _, pr, est) = Heavy(1, 1_000);   // バッファ 5 千＝月の出費の上限 500
        Assert.True(est > 0.10 * UtilityAgent.FiscalBufferOf(w), "前提：見込みは月の出費の上限を超える");
        new UtilityAgent().DecideQuarter(w);
        Assert.True(pr.ConstructionLeft <= 0, "月の出費の上限は外さない");
    }

    [Fact]
    public void 旗1でも国家の支払い率が1に届かない月は着工しない()
    {
        var (w, _, pr, _) = Heavy(1, 10_000);
        w.FiscalFundingRatio = new Share(0.99);
        new UtilityAgent().DecideQuarter(w);
        Assert.True(pr.ConstructionLeft <= 0, "借入と同じ規律の門は残す");
    }

    [Fact]
    public void 旗1でも走っている工事の月の出費は上限に数える()
    {
        // 走っている巨大な工事（月の出費 約 300 万）がある国：月の出費の上限（50 万）を食い尽くして軽工業すら着工しない。旗 0 も同じ。
        Assert.False(Run(0));
        Assert.False(Run(1), "旗 1 でも走っている工事は数える（数えなければ国庫を食う向きの守りが消える）");

        bool Run(double flag)
        {
            var sim = FourNations();
            var w = sim.Nations[2].World;
            w.Coef.AiBuildNoCostCap = flag;
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
    public void 旗1でも着工のたびに余地を引き二つ目は余地が尽きれば飛ばす()
    {
        var (w, rg1, pr1, est1) = Heavy(1, 10_000);
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
