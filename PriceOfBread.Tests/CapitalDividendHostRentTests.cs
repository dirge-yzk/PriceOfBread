using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(801) 線 c11fl：旗 <see cref="Coefficients.CapitalDividendFromHostRent"/>（外資の利札を受け皿の投資家の工業レントの持ち分に結ぶ・設計思想⑨⑩）の番人。
/// 利札＝受け皿の投資家の工業レント（四半期分＝第 3 月の <see cref="Pop.IndustrialRentPerMonth"/> の 3 倍）×外資の負債÷(外資の負債＋投資家の貯蓄)。
/// 単位：試験の中の R は<b>四半期分</b>のレント。POP の欄（月の値）には R÷3 を置く。AI とプレイヤーの区別は無い（<see cref="CapitalMarket.HostRentDividend"/> は World だけを読む）。
/// 突然変異（使い捨ての木・結果は measurements/m801_*）：①率×外資の負債のまま②貯蓄で引く③0④持ち分の二乗⑤月の値を 3 倍しない。</summary>
public class CapitalDividendHostRentTests
{
    private static Coefficients Quiet(double flag) => new()
    {
        CapitalDividendFromHostRent = flag,
        CapitalFlightRatePerQuarter = new RatePerQuarter(0), CapitalDemandScale = 0,
        CapitalDomesticFlightRatePerQuarter = new RatePerQuarter(0),   // 配当だけ
    };

    private static (MultiNationSim Sim, World Uk, World Fr) Setup(Coefficients coef)
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent(), coef) { DiplomacyFrozen = true };
        CapitalMarket.Step(sim.Nations, coef);   // 開始盤面の繰上げを 1 回
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        uk.ExchangeRate = 1.0; fr.ExchangeRate = 1.0;
        foreach (var w in new[] { uk, fr }) { w.ForeignIntPaidThisQuarter = 0; w.ForeignCapitalIn = default; w.CapitalAbroad = default; }
        foreach (var w in new[] { uk, fr }) foreach (var (_, p) in w.AllPopsWithRegion()) p.IndustrialRentPerMonth = new MoneyPerMonth(0);
        return (sim, uk, fr);
    }

    /// <summary>仏の最初の投資家の POP に第 3 月のレント <paramref name="monthly"/> を置く（四半期分 R＝3×monthly）。</summary>
    private static void PutRent(World w, double monthly)
    {
        foreach (var (_, p) in w.AllPopsWithRegion()) if (CapitalMarket.IsInvestor(p)) { p.IndustrialRentPerMonth = new MoneyPerMonth(monthly); return; }
        Assert.Fail("前提が崩れた：投資家の POP が無い");
    }

    /// <summary>Step を 1 回回し、仏の投資家が払った額を返す。</summary>
    private static double PaidByFrance(MultiNationSim sim, World fr, Coefficients coef)
    {
        double before = CapitalMarket.InvestorSavings(fr);
        CapitalMarket.Step(sim.Nations, coef);
        return before - CapitalMarket.InvestorSavings(fr);
    }

    [Fact]
    public void 工業レントが0なら外資の負債が正でも利札は0()
    {
        var coef = Quiet(1);
        var (sim, uk, fr) = Setup(coef);
        double s = CapitalMarket.InvestorSavings(fr);
        Assert.True(s > 0, "前提が崩れた：仏の投資家の貯蓄が 0");
        fr.ForeignCapitalIn = new MoneyStock(1000 * s); uk.CapitalAbroad = new MoneyStock(1000 * s);
        Assert.Equal(0.0, CapitalMarket.HostRentDividend(fr));
        double paid = PaidByFrance(sim, fr, coef);
        Assert.True(Math.Abs(paid) <= 1e-9 * s, $"レント 0 なのに仏の投資家が {paid} 払った（旧の形なら {coef.CapitalDividendRatePerQuarter.Value * 1000 * s}）");
    }

    [Fact]
    public void 外資の負債が投資家の貯蓄と等しければ持ち分は半分_利札は四半期のレントの半分()
    {
        var coef = Quiet(1);
        var (sim, uk, fr) = Setup(coef);
        double s = CapitalMarket.InvestorSavings(fr);
        double R = 0.2 * s;   // 四半期分のレント（利札 0.5R＝0.1s は貯蓄に収まる）
        PutRent(fr, R / 3.0);
        fr.ForeignCapitalIn = new MoneyStock(s); uk.CapitalAbroad = new MoneyStock(s);
        double expected = 0.5 * R;
        Assert.True(Math.Abs(CapitalMarket.HostRentDividend(fr) - expected) <= 1e-12 * s, $"利札 {CapitalMarket.HostRentDividend(fr)} ≠ 0.5R {expected}");
        double paid = PaidByFrance(sim, fr, coef);
        Assert.True(Math.Abs(paid - expected) <= 1e-9 * s, $"仏の投資家の払い {paid} ≠ 0.5R {expected}");
    }

    [Fact]
    public void 外資の負債が貯蓄よりずっと大きいと持ち分は1に近づくが利札は四半期のレントを超えない()
    {
        var coef = Quiet(1);
        var (sim, uk, fr) = Setup(coef);
        double s = CapitalMarket.InvestorSavings(fr);
        double R = 0.2 * s;
        PutRent(fr, R / 3.0);
        fr.ForeignCapitalIn = new MoneyStock(1e6 * s); uk.CapitalAbroad = new MoneyStock(1e6 * s);
        double d = CapitalMarket.HostRentDividend(fr);
        Assert.True(d <= R * (1 + 1e-12), $"利札 {d} が四半期のレント {R} を超えた");
        Assert.True(d >= R * (1 - 1e-5), $"持ち分が 1 に近いのに利札 {d} が R {R} に届かない");
        // 旧の形（率×外資の負債＝0.02×1e6×s＝2e4 s）なら払い切れずに投資家の貯蓄が尽きる＝新の形は R だけ
        double paid = PaidByFrance(sim, fr, coef);
        Assert.True(Math.Abs(paid - d) <= 1e-9 * s, $"仏の投資家の払い {paid} ≠ 利札 {d}");
        Assert.True(paid < s, "払いが貯蓄を尽くした（新の形は四半期のレントまで）");
    }

    [Fact]
    public void 旗0では今の形_率かける外資の負債でレントを読まない()
    {
        var coef = Quiet(0);
        var (sim, uk, fr) = Setup(coef);
        double s = CapitalMarket.InvestorSavings(fr);
        PutRent(fr, 1e6 * s);   // 旗 0 では読まれない
        double F = 0.3 * s / coef.CapitalDividendRatePerQuarter.Value;   // 旧の利札 0.3 s（貯蓄に収まる）
        fr.ForeignCapitalIn = new MoneyStock(F); uk.CapitalAbroad = new MoneyStock(F);
        double paid = PaidByFrance(sim, fr, coef);
        Assert.True(Math.Abs(paid - coef.CapitalDividendRatePerQuarter.Value * F) <= 1e-9 * s, $"旗 0 の払い {paid} ≠ 率×外資の負債 {coef.CapitalDividendRatePerQuarter.Value * F}");
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(-5.0)]
    [InlineData(double.PositiveInfinity)]
    public void レントが負_非有限なら利札は0で貨幣はNaNにならない(double monthly)
    {
        var coef = Quiet(1);
        var (sim, uk, fr) = Setup(coef);
        double s = CapitalMarket.InvestorSavings(fr);
        PutRent(fr, monthly);
        fr.ForeignCapitalIn = new MoneyStock(s); uk.CapitalAbroad = new MoneyStock(s);
        Assert.Equal(0.0, CapitalMarket.HostRentDividend(fr));
        double paid = PaidByFrance(sim, fr, coef);
        Assert.True(Math.Abs(paid) <= 1e-9 * s, $"レント {monthly} なのに払った {paid}");
        Assert.False(double.IsNaN(CapitalMarket.InvestorSavings(fr)) || double.IsNaN(CapitalMarket.InvestorSavings(uk)), "投資家の貯蓄が NaN");
    }

    [Fact]
    public void 払えない利札は積まない_貯蓄を超えて取らない()
    {
        var coef = Quiet(1);
        var (sim, uk, fr) = Setup(coef);
        double s = CapitalMarket.InvestorSavings(fr);
        PutRent(fr, 10.0 * s / 3.0);   // 四半期のレント R＝10 s（貯蓄より大きい）
        fr.ForeignCapitalIn = new MoneyStock(1e6 * s); uk.CapitalAbroad = new MoneyStock(1e6 * s);
        double paid = PaidByFrance(sim, fr, coef);
        Assert.True(paid <= s * (1 + 1e-9), $"払い {paid} が貯蓄 {s} を超えた");
        Assert.True(paid >= s * (1 - 1e-9), $"請求が貯蓄を超えるのに払い切っていない {paid} < {s}");
    }
}
