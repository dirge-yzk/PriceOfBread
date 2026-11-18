using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1223) 線 c123iv：私的な建設の予算を投資家の前の月の稼ぎで縛る旗 <c>InvestBudgetFromEarnings</c>（既定 0）。事前登録 <c>measurements/m1223_20261119_prereg_iv.txt</c> の T1〜T3。
/// 盤面は試験の中で置く：英の首都の州・接収の萎縮 0・希望の速さを大きく（希望が予算を必ず超える）・投資家の貯蓄と今月の稼ぎ（Run の頭で LastEarnedPerMonth へ写る）を置く。
/// 観測は計器 U4（<see cref="Instruments.U4Of"/>）の Planned（建設の量×単価＝予算で切った計画）と Paid（決済で実際に払った額）。期待は手の値（500・5e4・0）。
/// </summary>
[Collection("Instruments")]
public class InvestBudgetFromEarningsTests
{
    private static (Region r, World w) Board(double flag, double savings, double earned)
    {
        var sc = NationConfig.Britain().ToScenario(41, sharedWorld: true);
        var co = new Coefficients();
        co.InvestBudgetFromEarnings = flag;
        co.IndInvestmentSpeed = 1e12;   // 希望が予算を必ず超える
        typeof(Scenario).GetProperty(nameof(Scenario.Coef))!.SetValue(sc, co);
        var sim = new Simulation(sc, new NullAgent());
        var w = sim.World;
        var r = w.Regions.First(x => x.CapitalEnabled && x.InvestorPopIndex >= 0);
        r.SeizureChill = 0;
        var inv = r.Pops[r.InvestorPopIndex];
        inv.SetSavings(savings);
        inv.EarnedPerMonth = new MoneyPerMonth(earned);   // Run の頭で LastEarnedPerMonth へ写る＝前の月の稼ぎ
        return (r, w);
    }

    private static (double planned, double paid, double popPaid, double debtUp) Month(double flag, double savings, double earned)
    {
        var (r, w) = Board(flag, savings, earned);
        var inv = r.Pops[r.InvestorPopIndex];
        double debt0 = inv.Debt.Value;
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_iv_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var ex = Record.Exception(() => EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w));
            Assert.Null(ex);
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { } }
        var a = Instruments.U4Of(r);
        Assert.NotNull(a);
        Assert.Equal(1, a!.Judged);   // 対照：私的な建設の判定がこの月に 1 回あった
        return (a.Planned, a.Paid, inv.PaidPerMonth.Value, inv.Debt.Value - debt0);
    }

    [Fact]
    public void T1_旗1の計画は前の月の稼ぎ_旗0は貯蓄の5パーセント()
    {
        var (p1, paid1, _, _) = Month(1.0, 1e6, 500.0);
        Assert.True(Math.Abs(p1 - 500.0) <= 500.0 * 1e-9, $"旗 1 の計画 {p1} が前の月の稼ぎ 500 でない");
        Assert.True(paid1 <= p1 * 1.05, $"旗 1 の支払い {paid1} が計画 {p1} の 1.05 倍を超える");
        var (p0, _, _, _) = Month(0.0, 1e6, 500.0);
        Assert.True(Math.Abs(p0 - 5e4) <= 5e4 * 1e-9, $"旗 0 の計画 {p0} が貯蓄の 5%＝5e4 でない（盤面の前提：希望が予算を超える）");
    }

    [Fact]
    public void T2_稼ぎが負か非有限なら予算は0()
    {
        foreach (double e in new[] { -500.0, double.NaN })
        {
            var (p, paid, _, _) = Month(1.0, 1e6, e);
            Assert.True(p == 0.0, $"稼ぎ {e} で計画 {p} が 0 でない");
            Assert.True(paid == 0.0, $"稼ぎ {e} で支払い {paid} が 0 でない");
        }
    }

    [Fact]
    public void T3_旗1で消費と投資の和は使えるお金を超えない()
    {
        const double S = 600.0;
        var (p, _, popPaid, debtUp) = Month(1.0, S, 500.0);
        Assert.True(Math.Abs(p - 500.0) <= 500.0 * 1e-9, $"前提：計画 {p} が 500 でない");
        Assert.True(popPaid >= 0.5 * S, $"前提：投資家の支払い {popPaid} が貯蓄の半分に届かない（消費が予算に縛られる盤面でない）");
        double room = S + (debtUp > 0 ? debtUp : 0.0);
        Assert.True(popPaid <= room * 1.05, $"投資家の支払い {popPaid} が使えるお金 {room} の 1.05 倍を超える");
    }
}
