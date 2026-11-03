using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(810) 線 c13fl：旗 <see cref="Coefficients.CapitalDividendFromHostRent"/> のやり直しの番人（事前登録 measurements/m810_20261103_prereg_fl.txt [7]）。
/// (a) 請求＝額面 F×R÷(F＋S＋K)（R＝受け皿の投資家の工業レントの四半期分・S＝投資家の貯蓄・K＝州の工業化度を今の値段で買い直す費用の和）
/// (b) 払えない利札（未払い）は外資の負債と持ち主の対外資産から同じ額を落とす　(c) 払った利回りは流入の収益率に入る（高い利回りで流入が増える）。
/// 期待の K は試験が市場の値段・工業化度・所得から事前登録の文の式で自分で書く（<see cref="CapitalMarket.HostRentDividend"/> と EconomyStep の関数を呼ばない）。
/// 突然変異（使い捨ての木・measurements/m810_*）：落とさない・2 倍落とす・利回りの項を消す・符号を逆・旧の単位（分母に K を入れない）で赤になること。</summary>
public class CapitalCouponWriteDownTests
{
    private static Coefficients Quiet(double flag, double demandScale = 0) => new()
    {
        CapitalDividendFromHostRent = flag,
        CapitalFlightRatePerQuarter = new RatePerQuarter(0), CapitalDemandScale = demandScale,
        CapitalDomesticFlightRatePerQuarter = new RatePerQuarter(0),
    };

    private static (MultiNationSim Sim, World Uk, World Fr) Setup(Coefficients coef)
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent(), coef) { DiplomacyFrozen = true };
        CapitalMarket.Step(sim.Nations, coef);   // 開始盤面の繰上げを 1 回
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        uk.ExchangeRate = 1.0; fr.ExchangeRate = 1.0;
        foreach (var w in new[] { uk, fr }) { w.ForeignIntPaidThisQuarter = 0; w.ForeignCapitalIn = default; w.CapitalAbroad = default; }
        foreach (var w in new[] { uk, fr }) foreach (var (_, p) in w.AllPopsWithRegion()) p.IndustrialRentPerMonth = new MoneyPerMonth(0);
        foreach (var w in new[] { uk, fr }) foreach (var r in w.Regions) { r.Industrialization = 0; r.OldVintageCapital = 0; }   // K＝0 から始める（試験が置いた州だけ K を持つ）
        return (sim, uk, fr);
    }

    private static Pop FirstInvestor(World w)
    {
        foreach (var (_, p) in w.AllPopsWithRegion()) if (CapitalMarket.IsInvestor(p)) return p;
        Assert.Fail("前提が崩れた：投資家の POP が無い"); return null!;
    }

    /// <summary>事前登録 [1] の表の文の式：州 r の工業化度 1 点を今の値段で買い直す費用＝束の今の費用×(1＋工賃の割合)×(1 点に要る束の数)。</summary>
    private static double PerPoint(Region r, Coefficients c)
    {
        bool bld = c.BuildingGoodBundle > 0 && Catalog.BuildingId < r.Markets.Length;
        double P(int g) => System.Math.Max(r.Markets[g].Price, 1e-9);
        double now = bld ? c.ConsBuilding * P(Catalog.BuildingId) + c.ConsMachine * P(c.MachineGoodId)
                         : c.ConsSteel * P(c.SteelGoodId) + c.ConsMachine * P(c.MachineGoodId) + c.ConsCement * P(c.CementGoodId) + c.ConsLumber * P(c.LumberGoodId);
        double rf = c.BuildingGoodBundle > 0
            ? c.ConsBuilding * Catalog.RefPrices[Catalog.BuildingId] + c.ConsMachine * Catalog.RefPrices[c.MachineGoodId]
            : c.ConsSteel * Catalog.RefPrices[c.SteelGoodId] + c.ConsMachine * Catalog.RefPrices[c.MachineGoodId] + c.ConsCement * Catalog.RefPrices[c.CementGoodId] + c.ConsLumber * Catalog.RefPrices[c.LumberGoodId];
        double inc = 0; foreach (var p in r.Pops) if (p.LastEarnedPerMonth.Value > 0) inc += p.LastEarnedPerMonth.Value;
        double bundles = c.IndCapitalOutputYears * inc * 12.0 / (rf * (1.0 + c.ConstructionLaborWageFrac) * System.Math.Max(r.PriceLevel, 1e-9));
        if (!(bundles > 0) || double.IsInfinity(bundles)) bundles = 1.0 / c.IndPerConstruction;
        return now * (1.0 + c.ConstructionLaborWageFrac) * bundles;
    }

    private static double Rel(double a, double b) => System.Math.Abs(a - b) / System.Math.Max(1e-300, System.Math.Max(System.Math.Abs(a), System.Math.Abs(b)));

    [Fact]
    public void 払える時は請求の全額が移り_請求は今の値段の生産資本を分母に入れる_外資の負債は減らない()
    {
        var coef = Quiet(1);
        var (sim, uk, fr) = Setup(coef);
        double s = CapitalMarket.InvestorSavings(fr);
        Assert.True(s > 0, "前提が崩れた：仏の投資家の貯蓄が 0");
        var reg = fr.Regions[0];
        double pp = PerPoint(reg, coef);
        Assert.True(pp > 0 && double.IsFinite(pp), "前提が崩れた：1 点の費用が正の有限でない");
        reg.Industrialization = s / pp;   // K＝s
        double K = reg.Industrialization * pp;
        double R = 0.3 * s;               // 四半期のレント
        FirstInvestor(fr).IndustrialRentPerMonth = new MoneyPerMonth(R / 3.0);
        double F = s;
        fr.ForeignCapitalIn = new MoneyStock(F); uk.CapitalAbroad = new MoneyStock(F);
        double expected = R * F / (F + s + K);   // ＝R/3（旧の単位 F＋S なら R/2）
        double claim = CapitalMarket.HostRentDividend(fr, coef);
        Assert.True(Rel(claim, expected) <= 1e-9, $"請求 {claim} ≠ R×F÷(F＋S＋K) {expected}（K＝{K}・旧の単位なら {R * F / (F + s)}）");
        double before = CapitalMarket.InvestorSavings(fr);
        CapitalMarket.Step(sim.Nations, coef);
        double paid = before - CapitalMarket.InvestorSavings(fr);
        Assert.True(Rel(paid, expected) <= 1e-9, $"仏の投資家の払い {paid} ≠ 請求 {expected}");
        Assert.True(Rel(fr.ForeignCapitalIn.Value, F) <= 1e-12, $"払えたのに外資の負債が動いた {fr.ForeignCapitalIn.Value} ≠ {F}");
        Assert.True(Rel(uk.CapitalAbroad.Value, F) <= 1e-12, $"払えたのに持ち主の対外資産が動いた {uk.CapitalAbroad.Value} ≠ {F}");
    }

    [Fact]
    public void 払えない時は未払いと同じ額だけ外資の負債と持ち主の対外資産が減る()
    {
        var coef = Quiet(1);
        var (sim, uk, fr) = Setup(coef);
        foreach (var (_, p) in fr.AllPopsWithRegion()) if (CapitalMarket.IsInvestor(p)) p.SetSavings(0);
        var inv = FirstInvestor(fr); inv.SetSavings(1000);
        double F = 1e6, R = 4e5;
        inv.IndustrialRentPerMonth = new MoneyPerMonth(R / 3.0);
        fr.ForeignCapitalIn = new MoneyStock(F); uk.CapitalAbroad = new MoneyStock(F);
        double claim = R * F / (F + 1000);   // K＝0（Setup が工業化度を 0 にした）
        double unpaid = claim - 1000;
        Assert.True(unpaid > 0 && unpaid < F, "前提が崩れた：未払いが (0, F) に無い");
        CapitalMarket.Step(sim.Nations, coef);
        Assert.True(Rel(F - fr.ForeignCapitalIn.Value, unpaid) <= 1e-9, $"落とした外資の負債 {F - fr.ForeignCapitalIn.Value} ≠ 未払い {unpaid}");
        Assert.True(Rel(F - uk.CapitalAbroad.Value, unpaid) <= 1e-9, $"減った持ち主の対外資産 {F - uk.CapitalAbroad.Value} ≠ 未払い {unpaid}");
    }

    [Fact]
    public void 未払いが元本を超える時は外資の負債が0で止まり対外資産も同じ額だけ減る()
    {
        var coef = Quiet(1);
        var (sim, uk, fr) = Setup(coef);
        foreach (var (_, p) in fr.AllPopsWithRegion()) if (CapitalMarket.IsInvestor(p)) p.SetSavings(0);
        double F = 1e4;
        FirstInvestor(fr).IndustrialRentPerMonth = new MoneyPerMonth(10 * F / 3.0);   // R＝10F・S＝0・K＝0 → 請求 10F・払い 0
        fr.ForeignCapitalIn = new MoneyStock(F); uk.CapitalAbroad = new MoneyStock(3 * F);
        CapitalMarket.Step(sim.Nations, coef);
        Assert.True(fr.ForeignCapitalIn.Value >= 0 && fr.ForeignCapitalIn.Value <= 1e-9 * F, $"外資の負債 {fr.ForeignCapitalIn.Value} が 0 で止まらない");
        Assert.True(Rel(3 * F - uk.CapitalAbroad.Value, F) <= 1e-9, $"減った対外資産 {3 * F - uk.CapitalAbroad.Value} ≠ 元本 {F}");
    }

    /// <summary>同じ盤面で受け皿（仏）の工業レントが 0 の場合と正の場合の新規の流入（Δ外資の負債）。供給（英の投資家の貯蓄）を十分に置き、仏の政策金利で仏を需要の側に置く。</summary>
    private static double Inflow(double flag, double rentMonthly)
    {
        var coef = Quiet(flag, demandScale: 0.02);
        coef.CapitalExportFrac = new Share(1.0);
        var (sim, uk, fr) = Setup(coef);
        fr.InterestRate = 0.5; uk.InterestRate = 0;
        foreach (var (_, p) in uk.AllPopsWithRegion()) if (CapitalMarket.IsInvestor(p)) p.SetSavings(1e12);
        double s = CapitalMarket.InvestorSavings(fr);
        FirstInvestor(fr).IndustrialRentPerMonth = new MoneyPerMonth(rentMonthly);
        double F = s;
        fr.ForeignCapitalIn = new MoneyStock(F); uk.CapitalAbroad = new MoneyStock(F);
        CapitalMarket.Step(sim.Nations, coef);
        return fr.ForeignCapitalIn.Value - F;   // 逃避 0・払える（落とす 0）＝新規の流れ (3) だけ
    }

    [Fact]
    public void 払った利回りが高いと新規の流入が増える()
    {
        var c0 = Quiet(1); var (_, _, fr0) = Setup(c0);
        double s = CapitalMarket.InvestorSavings(fr0);
        double a = Inflow(1, 0);
        double b = Inflow(1, 0.02 * s / 3.0);   // 四半期のレント 0.02 s（払える）
        Assert.True(a >= 0, $"前提が崩れた：仏が需要の側でない（流入 {a}）");
        Assert.True(b > a * (1 + 1e-6) + 1e-9, $"利回りが正なのに流入が増えない（0 のとき {a}・正のとき {b}）");
    }

    [Fact]
    public void 旗0では払えない利札でも外資の負債は減らず利回りは流入に入らない()
    {
        var coef = Quiet(0);
        var (sim, uk, fr) = Setup(coef);
        foreach (var (_, p) in fr.AllPopsWithRegion()) if (CapitalMarket.IsInvestor(p)) p.SetSavings(0);
        FirstInvestor(fr).SetSavings(10);
        double F = 1e6;
        fr.ForeignCapitalIn = new MoneyStock(F); uk.CapitalAbroad = new MoneyStock(F);
        CapitalMarket.Step(sim.Nations, coef);
        Assert.Equal(F, fr.ForeignCapitalIn.Value);
        Assert.Equal(F, uk.CapitalAbroad.Value);
        Assert.Equal(Inflow(0, 0), Inflow(0, 1e6));   // 旗 0 はレントを読まない＝流入も同じ
    }
}
