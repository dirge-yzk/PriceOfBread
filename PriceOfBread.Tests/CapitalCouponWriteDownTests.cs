using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(815) 線 c14fl：旗 <see cref="Coefficients.CapitalCouponUnpaidWriteDown"/>（払えない利札で請求権を落とす）の番人＝機構と不変条件（保存）。
/// 盤面＝英仏の 2 国・仏が受け皿（外資の負債 F）・英が持ち主（対外資産 A＝F）・為替 1・逃避と新規の流れと国内の逃避は 0（利札だけ）。
/// 期待は試験が置いた盤面の数から作る：請求＝率×F（率は試験が置く）・払い＝仏の投資家の貯蓄の減り（エンジンの局所の値を読まない別の経路）・未払い＝請求−払い。
/// AI とプレイヤーの区別は無い（CapitalMarket.Step は World だけを読む）。
/// 突然変異（使い捨ての木・measurements/m815_20261104/mutation_unit.txt）：落とさない（資産だけ）・2 倍・負債だけ・÷10。</summary>
public class CapitalCouponWriteDownTests
{
    private static Coefficients Quiet(double flag, double rate) => new()
    {
        CapitalCouponUnpaidWriteDown = flag,
        CapitalDividendRatePerQuarter = new RatePerQuarter(rate),
        CapitalFlightRatePerQuarter = new RatePerQuarter(0), CapitalDemandScale = 0,
        CapitalDomesticFlightRatePerQuarter = new RatePerQuarter(0),   // 利札だけ
    };

    private static (MultiNationSim Sim, World Uk, World Fr) Setup(Coefficients coef)
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent(), coef) { DiplomacyFrozen = true };
        CapitalMarket.Step(sim.Nations, coef);   // 開始盤面の繰上げを 1 回
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        uk.ExchangeRate = 1.0; fr.ExchangeRate = 1.0;
        foreach (var w in new[] { uk, fr }) { w.ForeignIntPaidThisQuarter = 0; w.ForeignCapitalIn = default; w.CapitalAbroad = default; }
        return (sim, uk, fr);
    }

    /// <summary>仏の外資の負債と英の対外資産を <paramref name="f"/> に置いて Step を 1 回。返り値＝(仏の投資家の払い, 外資の負債の減り, 英の対外資産の減り)。</summary>
    private static (double Paid, double DropF, double DropA) Run(Coefficients coef, double fMultipleOfSavings)
    {
        var (sim, uk, fr) = Setup(coef);
        double s = CapitalMarket.InvestorSavings(fr);
        Assert.True(s > 0, "前提が崩れた：仏の投資家の貯蓄が 0");
        double f = fMultipleOfSavings * s;
        fr.ForeignCapitalIn = new MoneyStock(f); uk.CapitalAbroad = new MoneyStock(f);
        double before = CapitalMarket.InvestorSavings(fr);
        CapitalMarket.Step(sim.Nations, coef);
        double paid = before - CapitalMarket.InvestorSavings(fr);
        return (paid, f - fr.ForeignCapitalIn.Value, f - uk.CapitalAbroad.Value);
    }

    [Fact]
    public void 払える時は請求の全額が移り外資の負債と対外資産は落ちない()
    {
        // F＝貯蓄の 1 倍・率 0.02＝請求 0.02s（貯蓄に収まる）
        var coef = Quiet(1, 0.02);
        var (_, _, fr) = Setup(coef);
        double s = CapitalMarket.InvestorSavings(fr);
        var (paid, dF, dA) = Run(coef, 1.0);
        Assert.True(Math.Abs(paid - 0.02 * s) <= 1e-9 * s, $"払い {paid} ≠ 請求 {0.02 * s}");
        Assert.True(Math.Abs(dF) <= 1e-9 * s, $"払えたのに外資の負債が {dF} 落ちた");
        Assert.True(Math.Abs(dA) <= 1e-9 * s, $"払えたのに対外資産が {dA} 落ちた");
    }

    [Fact]
    public void 払えない時は未払いと同じ額だけ外資の負債と持ち主の対外資産が減る()
    {
        // F＝貯蓄の 100 倍・率 0.02＝請求 2s＞貯蓄 s＝未払い＝2s−払い
        var coef = Quiet(1, 0.02);
        var (_, _, fr) = Setup(coef);
        double s = CapitalMarket.InvestorSavings(fr);
        var (paid, dF, dA) = Run(coef, 100.0);
        double claim = 0.02 * 100.0 * s;
        Assert.True(paid > 0 && paid < claim, $"前提が崩れた：払い {paid} が (0, 請求 {claim}) の外");
        double unpaid = claim - paid;
        Assert.True(Math.Abs(dF - unpaid) <= 1e-9 * claim, $"外資の負債の減り {dF} ≠ 未払い {unpaid}");
        Assert.True(Math.Abs(dA - unpaid) <= 1e-9 * claim, $"持ち主の対外資産の減り {dA} ≠ 未払い {unpaid}（同じ額＝保存）");
    }

    [Fact]
    public void 旗0では払えなくても外資の負債と対外資産は変わらない()
    {
        var coef = Quiet(0, 0.02);
        var (_, _, fr) = Setup(coef);
        double s = CapitalMarket.InvestorSavings(fr);
        var (paid, dF, dA) = Run(coef, 100.0);
        Assert.True(paid > 0 && paid < 2.0 * s, $"前提が崩れた：払い {paid} が (0, 請求 {2.0 * s}) の外");
        Assert.True(Math.Abs(dF) <= 1e-9 * s, $"旗 0 で外資の負債が {dF} 落ちた");
        Assert.True(Math.Abs(dA) <= 1e-9 * s, $"旗 0 で対外資産が {dA} 落ちた");
    }

    [Fact]
    public void 未払いが外資の負債より大きい時は外資の負債が0で止まり対外資産も同じ額だけ減る()
    {
        // 率 1.5（試験の盤面）・F＝貯蓄の 10 倍＝請求 15s・払い≦s＝未払い≧14s＞F＝落とす額は F で頭打ち
        var coef = Quiet(1, 1.5);
        var (_, _, fr) = Setup(coef);
        double s = CapitalMarket.InvestorSavings(fr);
        var (paid, dF, dA) = Run(coef, 10.0);
        double f = 10.0 * s;
        Assert.True(1.5 * f - paid > f, $"前提が崩れた：未払い {1.5 * f - paid} が外資の負債 {f} 以下");
        Assert.True(Math.Abs(dF - f) <= 1e-9 * f, $"外資の負債の減り {dF} ≠ 外資の負債の全額 {f}");
        Assert.True(Math.Abs(dA - f) <= 1e-9 * f, $"対外資産の減り {dA} ≠ 外資の負債の全額 {f}（同じ額）");
    }
}
