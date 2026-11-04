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

    // =====================================================================================================
    // ★(828) 線 c66dw：旗 CapitalClaimCancelFromLedger（消えた請求権の額を外資の負債の台帳の差で書く）の番人＝機構と不変条件（両側が同じ数）。
    // 盤面：仏の外資の負債 F＝1e17（double の刻み＝16）・英の対外資産 A＝100（刻みが細かい）＝落とす額の端数は仏の側だけで丸められる。
    // 期待は試験が置いた盤面の数から作る：未払い＝率×F−払い（払い＝仏の投資家の貯蓄の減り）・仏の側の減り＝F−落とした後の負債（試験が読む台帳）・
    // 逃避の額＝min(F, F×逃避の率×(1−CreditRating))（CreditRating＝同じ Step の信用の写し・FiscalTests と同じ読み方）。エンジンの局所の値を読まない。
    // 突然変異（使い捨ての木・measurements/m828_20261104/）：旗 1 でも落とした額を足す（下の旗 1 の行が赤）・逃避の行だけ直す（利札の 2 本の旗 1 が赤）・利札の行だけ直す（逃避の 1 本の旗 1 が赤）・
    // 持ち主の側へ足さない（利札の端数の旗 1 と 払えない時は… が赤）。
    // =====================================================================================================
    private const double BigF = 1e17;   // 2^56 と 2^57 の間＝double の刻み 16
    private const double SmallA = 100.0;

    /// <summary>仏の外資の負債を BigF・英の対外資産を SmallA に置いて Step を 1 回。返り値＝(仏の投資家の払い, 仏の外資の負債の減り, 英の対外資産の減り, 仏の CreditRating)。</summary>
    private static (double Paid, double DropF, double DropA, double Cred) RunBig(Coefficients coef, MultiNationSim sim, World uk, World fr)
    {
        fr.ForeignCapitalIn = new MoneyStock(BigF); uk.CapitalAbroad = new MoneyStock(SmallA);
        double before = CapitalMarket.InvestorSavings(fr);
        CapitalMarket.Step(sim.Nations, coef);
        return (before - CapitalMarket.InvestorSavings(fr), BigF - fr.ForeignCapitalIn.Value, SmallA - uk.CapitalAbroad.Value, fr.CreditRating);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void 利札の落としが外資の負債に吸収された時_旗1は持ち主の対外資産も動かない(double ledger)
    {
        // 未払い≈5（刻み 16 の半分より小さい）＝仏の外資の負債は 1 ビットも動かない。旗 0 は持ち主だけが未払いの額だけ落ちる（片側の書き込み）・旗 1 は両側とも 0。
        var coef = Quiet(1, 0.0); coef.CapitalClaimCancelFromLedger = ledger;
        var (sim, uk, fr) = Setup(coef);
        double s = CapitalMarket.InvestorSavings(fr);
        coef.CapitalDividendRatePerQuarter = new RatePerQuarter((s + 5.0) / BigF);
        var (paid, dF, dA, _) = RunBig(coef, sim, uk, fr);
        double unpaid = BigF * coef.CapitalDividendRatePerQuarter.Value - paid;
        Assert.True(unpaid > 1.0 && unpaid < 7.0, $"前提が崩れた：未払い {unpaid} が (1, 7) の外（吸収の盤面にならない）");
        Assert.True(dF == 0, $"前提が崩れた：仏の外資の負債が {dF} 動いた（吸収されていない）");
        if (ledger > 0) Assert.True(dA == 0, $"旗 1：仏の負債が動かないのに英の対外資産が {dA} 落ちた（片側の書き込み）");
        else Assert.True(Math.Abs(dA - unpaid) <= 1e-9, $"旗 0（旧）：英の対外資産の減り {dA} ≠ 未払い {unpaid}");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void 利札の落としの端数が丸められた時_旗1は持ち主の減りが外資の負債の減りとビットで同じ(double ledger)
    {
        // 未払い≈50.3（刻み 16 の倍数でない）＝仏の外資の負債の減りは 48（丸め）。旗 1 は英の減りも同じ 48・旗 0 は未払いの 50.3。
        var coef = Quiet(1, 0.0); coef.CapitalClaimCancelFromLedger = ledger;
        var (sim, uk, fr) = Setup(coef);
        double s = CapitalMarket.InvestorSavings(fr);
        coef.CapitalDividendRatePerQuarter = new RatePerQuarter((s + 50.3) / BigF);
        var (paid, dF, dA, _) = RunBig(coef, sim, uk, fr);
        double unpaid = BigF * coef.CapitalDividendRatePerQuarter.Value - paid;
        Assert.True(unpaid > 40 && unpaid < 60, $"前提が崩れた：未払い {unpaid} が (40, 60) の外");
        Assert.True(dF > 0 && Math.Abs(dF - unpaid) > 0.5, $"前提が崩れた：仏の負債の減り {dF} が未払い {unpaid} と丸めの外で同じ（端数の盤面にならない）");
        if (ledger > 0) Assert.True(dA == dF, $"旗 1：英の対外資産の減り {dA} ≠ 仏の外資の負債の減り {dF}（両側が別の数）");
        else Assert.True(Math.Abs(dA - unpaid) <= 1e-9, $"旗 0（旧）：英の対外資産の減り {dA} ≠ 未払い {unpaid}");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void 逃避の端数が丸められた時_旗1は持ち主の減りが外資の負債の減りとビットで同じ(double ledger)
    {
        // 利札 0・逃避の額≈50.3（刻み 16 の倍数でない）。旗 1 は英の減り＝仏の外資の負債の減り・旗 0 は逃避の額。
        var coef = Quiet(1, 0.0); coef.CapitalClaimCancelFromLedger = ledger;
        var (sim, uk, fr) = Setup(coef);
        double cr0 = fr.CreditRating;
        Assert.True(cr0 < 0.999, $"前提が崩れた：仏の信用 {cr0} が 1 に近い（逃避が起きない）");
        coef.CapitalFlightRatePerQuarter = new RatePerQuarter(50.3 / (BigF * (1.0 - cr0)));
        var (_, dF, dA, cr) = RunBig(coef, sim, uk, fr);
        double flight = Math.Min(BigF, BigF * coef.CapitalFlightRatePerQuarter.Value * (1.0 - cr));
        Assert.True(flight > 20 && flight < 100, $"前提が崩れた：逃避の額 {flight} が (20, 100) の外");
        Assert.True(dF > 0 && Math.Abs(dF - flight) > 0.5, $"前提が崩れた：仏の負債の減り {dF} が逃避の額 {flight} と丸めの外で同じ（端数の盤面にならない）");
        if (ledger > 0) Assert.True(dA == dF, $"旗 1：英の対外資産の減り {dA} ≠ 仏の外資の負債の減り {dF}（両側が別の数）");
        else Assert.True(Math.Abs(dA - flight) <= 1e-9, $"旗 0（旧）：英の対外資産の減り {dA} ≠ 逃避の額 {flight}");
    }
    [Fact]
    public void 旗1でも受け皿の側の外資の負債の減りは旧の規則の額から刻みの半分以内_利札も逃避も()
    {
        // (835) (828) の反証役：旗 1 が直すのは持ち主の側が読む額だけ＝受け皿の側（仏の外資の負債）の動きは旧のまま（旧の規則の額と、double の刻みの丸めの分しか違わない）。
        // 上限の門 U1（scripts/instruments/c66dw/gate828.py の hostDev＝|旧の規則の額−台帳の差|÷刻み ≦ 0.5）の単位版。両側が台帳の差で一致していても、受け皿の側が旧からずれる誤りを拾う
        // （既存の旗 1 の 3 本は持ち主の減り＝仏の減りだけを比べるので、仏の減りが旧から半分ずれても緑のまま）。刻み＝BigF（1e17）の double の刻み 16・丸めの誤差はその半分 8 以内。
        double half = (Math.BitIncrement(BigF) - BigF) / 2.0;
        Assert.Equal(8.0, half);   // 前提：BigF の刻みは 16
        // 利札：未払い≈50.3（旧の規則の額＝請求−払い）
        var coef = Quiet(1, 0.0); coef.CapitalClaimCancelFromLedger = 1;
        var (sim, uk, fr) = Setup(coef);
        double s = CapitalMarket.InvestorSavings(fr);
        coef.CapitalDividendRatePerQuarter = new RatePerQuarter((s + 50.3) / BigF);
        var (paid, dF, _, _) = RunBig(coef, sim, uk, fr);
        double unpaid = BigF * coef.CapitalDividendRatePerQuarter.Value - paid;
        Assert.True(unpaid > 40 && unpaid < 60, $"前提が崩れた：未払い {unpaid} が (40, 60) の外");
        Assert.True(Math.Abs(dF - unpaid) <= half, $"旗 1：仏の外資の負債の減り {dF} が旧の規則の額 {unpaid} から刻みの半分 {half} を超えてずれた（受け皿の側が旧でない）");
        // 逃避：逃避の額≈50.3（旧の規則の額＝min(F, F×率×(1−信用))）
        var coef2 = Quiet(1, 0.0); coef2.CapitalClaimCancelFromLedger = 1;
        var (sim2, uk2, fr2) = Setup(coef2);
        double cr0 = fr2.CreditRating;
        Assert.True(cr0 < 0.999, $"前提が崩れた：仏の信用 {cr0} が 1 に近い（逃避が起きない）");
        coef2.CapitalFlightRatePerQuarter = new RatePerQuarter(50.3 / (BigF * (1.0 - cr0)));
        var (_, dF2, _, cr) = RunBig(coef2, sim2, uk2, fr2);
        double flight = Math.Min(BigF, BigF * coef2.CapitalFlightRatePerQuarter.Value * (1.0 - cr));
        Assert.True(flight > 20 && flight < 100, $"前提が崩れた：逃避の額 {flight} が (20, 100) の外");
        Assert.True(Math.Abs(dF2 - flight) <= half, $"旗 1：仏の外資の負債の減り {dF2} が旧の規則の額 {flight} から刻みの半分 {half} を超えてずれた（受け皿の側が旧でない）");
    }
}
