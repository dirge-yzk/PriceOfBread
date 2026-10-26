using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (579) 線 ukf の最初のコミット：AI の国営工場の着工の門（<c>UtilityAgent.TryBuildFacility</c>）の旗 <c>AiBuildFromTreasury</c> の番人。
/// 旗 0 は旧の式（総費用の天井と月の出費の天井）のまま・払える額を読まない。旗 1 は天井 2 本を外し、国家の支払い率 r を決めるのと同じ量
/// （<c>World.LastSpareBeforeBorrowPerMonth</c>＝月初の払える額−請求）から、着工の月の出費の見込み（工賃＋素材）が出せるかで着工する。
/// 期待値は SelfPlay の式から作らない：見込みは <c>NewFacilityOutlayPerMonth</c>（着工の記帳と一致することが別の番人で確かめてある）、払える額の側は外から置く値。
/// </summary>
public class AiBuildFromTreasuryTests
{
    private static MultiNationSim FourNations()
        => new([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], seed: 1789);

    /// <summary>身の丈の門（総費用 ≤ 1.0×バッファ）が旧では重工業を落とすよう、バッファを床の 1.0 に置く（Last* の請求を全て 0）。国庫は初日の労務費に足りる大きさ。</summary>
    private static void SmallBufferRichTreasury(World w)
    {
        w.LastAdminClaimPerMonth = new MoneyPerMonth(0);
        w.LastInterestPerMonth = new MoneyPerMonth(0);
        w.LastMilitaryClaimPerMonth = new MoneyPerMonth(0);
        w.SetTreasuryRaw(1e9);
        w.FiscalFundingRatio = new Share(1.0);
    }

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

    private static (MultiNationSim sim, World w, Region rg, Producer pr) Setup(double flag, out double est)
    {
        var sim = FourNations();
        var w = sim.Nations[2].World;   // イギリス
        w.Coef.AiBuildFromTreasury = flag;
        SmallBufferRichTreasury(w);
        var rg = w.Regions.First(r => PickHeavy(r) is not null);
        var pr = PickHeavy(rg)!;
        Plant(rg, pr);
        est = UtilityAgent.NewFacilityOutlayPerMonth(w, rg, pr);
        Assert.True(est > 0);
        Assert.True(Catalog.FactorySpec(pr.GoodId).Cost > UtilityAgent.FiscalBufferOf(w), "前提：この工場の総費用はバッファを超える＝旧の身の丈の門は落とす");
        return (sim, w, rg, pr);
    }

    [Fact]
    public void 既定は0で旧の式のまま()
    {
        string ov = Coefficients.OverrideSummary();
        if (!ov.Contains("AiBuildFromTreasury", StringComparison.Ordinal)) Assert.Equal(0.0, new Coefficients().AiBuildFromTreasury);
    }

    [Fact]
    public void 旗0は払える額がいくら大きくても身の丈の門で重工業を落とす()
    {
        var (_, w, _, pr) = Setup(0, out _);
        w.LastSpareBeforeBorrowPerMonth = new MoneyPerMonth(1e12);
        new UtilityAgent().DecideQuarter(w);
        Assert.True(pr.ConstructionLeft <= 0, "旗 0 は旧の身の丈の門（総費用 ≤ バッファ）で落とす＝払える額は読まない");
    }

    [Fact]
    public void 旗1は払える額が見込みの月の出費に足りれば総費用がバッファを超える工場も着工する()
    {
        var (_, w, _, pr) = Setup(1, out double est);
        w.LastSpareBeforeBorrowPerMonth = new MoneyPerMonth(est * 1.01);
        new UtilityAgent().DecideQuarter(w);
        Assert.True(pr.ConstructionLeft > 0, "旗 1 は天井で落とさない＝払える額が見込みを上回れば着工する");
    }

    [Theory]
    [InlineData(0.99)]    // 見込みにわずかに足りない
    [InlineData(0.0)]
    [InlineData(-5000.0)]  // 払えない額がある月
    public void 旗1は払える額が見込みの月の出費に足りなければ着工しない(double spareOverEst)
    {
        var (_, w, _, pr) = Setup(1, out double est);
        w.LastSpareBeforeBorrowPerMonth = new MoneyPerMonth(est * spareOverEst);
        new UtilityAgent().DecideQuarter(w);
        Assert.True(pr.ConstructionLeft <= 0, "払える額が見込みに足りない月は着工しない");
    }

    [Fact]
    public void 旗1は払える額が定まらない月は着工しない_否定形()
    {
        var (_, w, _, pr) = Setup(1, out _);
        w.LastSpareBeforeBorrowPerMonth = new MoneyPerMonth(double.NaN);
        new UtilityAgent().DecideQuarter(w);
        Assert.True(pr.ConstructionLeft <= 0, "NaN は着工しない側（fail-closed）");
    }

    [Fact]
    public void 旗1は着工のたびに余地を引き二つ目は余地が尽きれば飛ばす()
    {
        var (_, w, rg1, pr1) = Setup(1, out double est1);
        // もう 1 つ別の州に同じ財の候補を置く
        int ri1 = w.Regions.IndexOf(rg1);
        var rg2 = w.Regions.Where((r, i) => i != ri1).First(r => r.Producers.Any(p => p.GoodId == pr1.GoodId && ReferenceEquals(World.FindFactory(r, p.GoodId), p) && Discovery.Gate(r, p) > 0));
        var pr2 = rg2.Producers.First(p => p.GoodId == pr1.GoodId && ReferenceEquals(World.FindFactory(rg2, p.GoodId), p));
        Plant(rg2, pr2);
        double est2 = UtilityAgent.NewFacilityOutlayPerMonth(w, rg2, pr2);
        w.LastSpareBeforeBorrowPerMonth = new MoneyPerMonth(est1 + est2 * 0.3);   // 1 件分と 2 件目の 3 割
        new UtilityAgent().DecideQuarter(w);
        int started = (pr1.ConstructionLeft > 0 ? 1 : 0) + (pr2.ConstructionLeft > 0 ? 1 : 0);
        Assert.Equal(1, started);
    }

    [Fact]
    public void 旗1は走っている工事の出費を数え直さない_旗0は数える()
    {
        // 走っている巨大な工事（工賃が桁違い）がある国：旧は月の出費の上限を食い尽くして軽工業すら着工しない。
        // 新は走っている工事の出費を先月の請求（払える額の側）に入っているものとして引かない。
        (bool started0, bool started1) = (Run(0), Run(1));
        Assert.False(started0, "旗 0：走っている工事の出費が上限を超えて新規着工しない");
        Assert.True(started1, "旗 1：走っている工事は払える額の側に既に入っている＝見込みに足りる払える額があれば着工する");

        bool Run(double flag)
        {
            var sim = FourNations();
            var w = sim.Nations[2].World;
            w.Coef.AiBuildFromTreasury = flag;
            w.LastAdminClaimPerMonth = new MoneyPerMonth(1_000_000);   // バッファ 500 万＝旧の身の丈の門も月の出費の上限（50 万）も軽工業には余裕
            w.SetTreasuryRaw(1.5 * UtilityAgent.FiscalBufferOf(w));
            w.FiscalFundingRatio = new Share(1.0);
            var rg = w.Regions[0];
            var light = PickLight(rg);
            Plant(rg, light);
            var other = rg.Producers.First(p => !ReferenceEquals(p, light) && Catalog.Goods[p.GoodId].Tier != GoodTier.Service);
            other.ConstructionLeft = 400; other.ConstructionPayPerDay = 1e5;   // 走っている工事（月の出費 約 300 万＝旧の月の出費の上限 50 万の 6 倍・新の払える額 10 万の 30 倍＝数えれば着工が止まる大きさ）
            w.LastSpareBeforeBorrowPerMonth = new MoneyPerMonth(1e5);   // 軽工業 1 件の見込み（数千）には足りるが走っている工事の出費 約 300 万には足りない
            new UtilityAgent().DecideQuarter(w);
            return light.ConstructionLeft > 0;
        }
    }

    /// <summary>旗 2＝総費用の身の丈の門だけ外し、月の出費の上限（0.10×バッファ）と払える額を読まない旧の形のまま。バッファを <paramref name="bufferMonthly"/>×5 に置く。</summary>
    private static (World w, Producer pr, double est) Mode2(double bufferMonthly)
    {
        var sim = FourNations();
        var w = sim.Nations[2].World;
        w.Coef.AiBuildFromTreasury = 2;
        SmallBufferRichTreasury(w);
        w.LastAdminClaimPerMonth = new MoneyPerMonth(bufferMonthly);   // バッファ＝5×これ
        w.SetTreasuryRaw(1e9);
        var rg = w.Regions.First(r => PickHeavy(r) is not null);
        var pr = PickHeavy(rg)!;
        Plant(rg, pr);
        w.LastSpareBeforeBorrowPerMonth = new MoneyPerMonth(-1e9);   // 払える額は読まない（負でも結果は変わらない）
        double est = UtilityAgent.NewFacilityOutlayPerMonth(w, rg, pr);
        Assert.True(Catalog.FactorySpec(pr.GoodId).Cost > UtilityAgent.FiscalBufferOf(w), "前提：総費用がバッファを超える＝旧なら身の丈の門で落ちる");
        return (w, pr, est);
    }

    [Fact]
    public void 旗2は身の丈の門だけ外し月の出費の上限が足りれば着工する_払える額は読まない()
    {
        var (w, pr, est) = Mode2(10_000);   // バッファ 5 万＝月の出費の上限 5,000
        Assert.True(est < 0.10 * UtilityAgent.FiscalBufferOf(w), "前提：見込みは月の出費の上限の内");
        new UtilityAgent().DecideQuarter(w);
        Assert.True(pr.ConstructionLeft > 0);
    }

    [Fact]
    public void 旗2は月の出費の上限を越える工事は着工しない()
    {
        var (w, pr, est) = Mode2(1_000);   // バッファ 5 千＝月の出費の上限 500
        Assert.True(est > 0.10 * UtilityAgent.FiscalBufferOf(w), "前提：見込みは月の出費の上限を超える");
        new UtilityAgent().DecideQuarter(w);
        Assert.True(pr.ConstructionLeft <= 0);
    }

    [Fact]
    public void 月初の払える額の記録は支払い率が1でない月は負_負でない月は支払い率が1()
    {
        // 独立の恒等：spare ≥ 0 ⇒ payable ≥ claims ⇒ 国家の支払い率 r = 1（借入と切り下げは払える額を足すだけ）。
        var sim = FourNations();
        sim.StartWarmup();
        int checkedMonths = 0;
        for (int q = 0; q < 6; q++)
        {
            sim.StepQuarter();
            foreach (var n in sim.Nations)
            {
                double spare = n.World.LastSpareBeforeBorrowPerMonth.Value;
                Assert.False(double.IsNaN(spare), "払える額の記録が NaN");
                if (spare >= 0) Assert.Equal(1.0, n.World.FiscalFundingRatio.Value);
                checkedMonths++;
            }
        }
        Assert.True(checkedMonths > 0);
    }
}
