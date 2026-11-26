using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1384) c147vs 兵の給与の請求書 LastSoldierPayPerMonth を同じ月の予算が読む時点の値。
/// 月の決算は請求書を州のループで積み直す。旧の形は月の頭で 0 を置いてから軍の予算の枠（Military.EnvelopeOf）と行政の予算（SpendableRevenue）を読む＝両方が 0 を読む。
/// 新の形は前の月の請求書を読み、月の終わりに州の順の和を 1 回だけ書く。
/// 盤面＝仏（1 四半期回した後）・幹部の給与の割合 0（請求＝兵卒の給与×充足）・国庫を潤沢の線の上に置き、軍の予算の枠を給与の満額より小さくする（請求書＝枠）。
/// 期待値は潤沢の線の式（緊急の月数×(利払い＋行政＋軍の請求)＋重み×月数×請求書）×8 四半期を試験の中で手で書く＝エンジンの OpulenceLineOf を呼ばない。</summary>
public class SoldierPayBillTests
{
    const double Q = 8.0;   // 潤沢の線の四半期の数（SelfPlay.OpulenceBufferQuarters と同じ値を試験の側で書く）

    static Simulation Board()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
        sim.Run(1);
        return sim;
    }

    readonly record struct Month2(double Bill1, double Bill2, double EnvNew, double EnvOld, double SumBudgets,
                                  double AdmNew, double AdmOld, double AdmActual, int AdmRegions);

    /// <summary>1 か月回して請求書を作り、2 か月目の決算の直前に国庫と枠を置いて決算を回す。</summary>
    static Month2 Run2(double flag)
    {
        var sim = Board(); var w = sim.World; var c = w.Coef;
        c.MilitaryCadrePayRatio = 0.0; c.SoldierPayBillPrevMonth = flag;
        for (int d = 0; d < Calendar.DaysPerMonth; d++) sim.StepDay();   // 1 か月目（請求書を積む）
        double bill1 = w.LastSoldierPayPerMonth.Value;
        for (int d = 0; d < Calendar.DaysPerMonth - 1; d++) sim.StepDay();   // 2 か月目の決算の直前まで
        Assert.Equal(bill1, w.LastSoldierPayPerMonth.Value);   // 日々の歩みは請求書に触れない
        double buf = Math.Max(1.0, c.FiscalStressBufferMonths * (w.LastInterestPerMonth.Value + w.LastAdminClaimPerMonth.Value + w.LastMilitaryClaimPerMonth.Value));
        double extra = c.FiscalBufferSoldierPayWeight * c.FiscalStressBufferMonths * bill1 * Q;   // 請求書が線を上げる量
        double lineNew = (buf * Q) + extra;
        MoveToTreasury(w, lineNew + extra);   // 線の上＝新の余り extra・旧の余り 2×extra
        double rate = c.OpulenceSpendRatePerYear.Value, rev = w.LastRevenuePerMonth.Value;
        double sNew = rev + Calendar.PerYearToPerMonth(rate * extra);
        double sOld = rev + Calendar.PerYearToPerMonth(rate * 2 * extra);
        double level = w.MilitarySpending.Value;
        double share = 0.1 * bill1 / (level * sOld);   // 旧の枠＝前の月の請求の 1 割＝給与の満額より小さい（請求書＝枠）
        c.MilitaryEnvelopeShare = new Share(share);
        double envNew = level * share * sNew, envOld = level * share * sOld;
        double nationPop = w.TotalPopulation, admNew = 0, admOld = 0; int admRegions = 0;
        foreach (var r in w.Regions)
        {
            if (r.BureaucratPopIndex < 0) continue;
            double b = w.AdminSpending * c.AdminBudgetFrac.Value * (r.SumPopulation() / nationPop) * Fiscal.AdminDistanceMul(w, r);
            admNew += b * sNew; admOld += b * sOld; admRegions++;
        }
        sim.StepDay();   // 2 か月目の決算
        double sum = 0, adm = 0;
        foreach (var r in w.Regions)
        {
            if (r.SoldierPopIndex >= 0) sum += r.SoldierPayBudget;
            if (r.BureaucratPopIndex >= 0) adm += r.AdminBudget;
        }
        return new Month2(bill1, w.LastSoldierPayPerMonth.Value, envNew, envOld, sum, admNew, admOld, adm, admRegions);
    }

    /// <summary>国庫を target にする。差額は貯蓄の最も多い POP から移す（基礎貨幣の保存を崩さない）。</summary>
    static void MoveToTreasury(World w, double target)
    {
        double delta = target - w.Treasury.Value;
        Pop? rich = null;
        foreach (var r in w.Regions) foreach (var p in r.Pops) if (rich is null || p.Savings.Value > rich.Savings.Value) rich = p;
        Assert.True(rich is not null && rich.Savings.Value > delta, $"前提：国庫へ移す {delta} を持つ POP が無い");
        rich!.ApplyBakedSavings(rich.Savings.Value - delta);
        w.SetTreasuryRaw(target);
    }

    static bool Near(double a, double b) => Math.Abs(a - b) <= 1e-9 * Math.Max(Math.Abs(a), Math.Abs(b));

    /// <summary>旗 1＝前の月の請求書を読んだ枠と行政の予算・旗 0＝請求書 0 を読んだ値（今の形の対照）。
    /// ⚠突然変異 M1〈旗 1 でも月の頭で 0 を置く〉で旗 1 が赤（直す前の木で赤を確かめた＝measurements/m1384_20261126/flag0_tests_red.txt）。</summary>
    [Theory]
    [InlineData(1.0)]
    [InlineData(0.0)]
    public void 二か月目の予算は前の月の請求書を読む(double flag)
    {
        var m = Run2(flag);
        Assert.True(m.Bill1 > 0, $"前提：1 か月目の請求書 {m.Bill1} が正でない");
        Assert.True(m.EnvOld > m.EnvNew * (1 + 1e-6), $"前提：旧と新の枠 {m.EnvOld}・{m.EnvNew} の差が無い");
        Assert.True(m.AdmRegions > 0, "前提：官僚の居る州が無い");
        double env = flag > 0 ? m.EnvNew : m.EnvOld, admE = flag > 0 ? m.AdmNew : m.AdmOld;
        Assert.True(Near(m.Bill2, env), $"旗 {flag}：2 か月目の請求書 {m.Bill2:R} ≠ 期待の枠 {env:R}（前の月の請求を読んだ枠 {m.EnvNew:R}・請求書 0 を読んだ枠 {m.EnvOld:R}）");
        Assert.True(Near(m.AdmActual, admE), $"旗 {flag}：2 か月目の行政の予算 {m.AdmActual:R} ≠ 期待 {admE:R}（前の月の請求 {m.AdmNew:R}・0 {m.AdmOld:R}）");
    }

    /// <summary>月の終わりの請求書＝州の給与の予算の州の順の和（どちらの旗でも）。⚠突然変異 M2〈旗 1 で局所の和を書かない〉で旗 1 が赤（前の月の値が残る）。</summary>
    [Theory]
    [InlineData(1.0)]
    [InlineData(0.0)]
    public void 月の終わりの請求書は州の給与の予算の和(double flag)
    {
        var m = Run2(flag);
        Assert.True(!Near(m.Bill2, m.Bill1), $"前提：2 か月目の請求書 {m.Bill2} が 1 か月目 {m.Bill1} と同じ");
        Assert.True(Near(m.Bill2, m.SumBudgets), $"月の終わりの請求書 {m.Bill2:R} ≠ 州の給与の予算の和 {m.SumBudgets:R}");
    }
}
