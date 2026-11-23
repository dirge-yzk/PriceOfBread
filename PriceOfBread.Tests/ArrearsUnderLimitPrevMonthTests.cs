using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>arrears_s の underLimit は、エンジン（Fiscal.cs の頭）と同じく前の月の比（利払い÷歳入）で判定する。盤面は ArrearsOnePredicateTests と同じ作り（英・税 0・国内の国債）で、
/// 月 1 の後に国債を 1.5 倍にして月 2 の利払いだけを上げ、上限 L を「月 1 の比 ＜ L ＜ 月 2 の比」に置く＝前の月の比なら under＝1・今月の比なら 0。
/// 月 2 の比は同じ種の別の盤面で先に読む（期待の L は試験の算術・計器の式を呼ばない）。</summary>
[Collection("Instruments")]
public class ArrearsUnderLimitPrevMonthTests
{
    private static World Board(double limit)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(1);
        var w = sim.World;
        w.SocialSecurity = 0; w.Pension = ReformLevel.None;
        w.Coef.ForeignBondCapacity = new Share(0);
        w.GovDebt = new MoneyStock(Calendar.PerMonthToPerYear(0.1 * w.NominalGdpPerMonth).Value); w.ForeignDebt = new MoneyStock(0);
        Fiscal.SeedBondHoldings(w);
        w.WarmupFrozen = false;
        w.Coef.MaxInterestShareOfRevenue = new Share(limit);
        w.Coef.BondRepayFracPerMonth = new RatePerMonth(0);
        w.Coef.GoldCoverMin = new Share(0);
        w.InsolvencyMonths = 0; w.OnGoldStandard = true; w.Convertibility = 1.0;
        return w;
    }
    private const double Ample = 1e18;
    private static void Month(World w, double r) { w.FiscalFundingRatio = new Share(r); Fiscal.Step(w, w.Coef, -Ample, 0, Ample * w.Convertibility); }
    private static void Bigger(World w) { w.GovDebt = new MoneyStock(w.GovDebt.Value * 1.5); Fiscal.SeedBondHoldings(w); w.OnGoldStandard = true; w.Convertibility = 1.0; w.InsolvencyMonths = 0; }   // 月 1 で本位を離れていたら戻す（月 2 の行は本位の停止の述語で書かれる）

    [Fact]
    public void T5_underLimitは前の月の比で判定する()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_tlu_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var a = Board(0); Month(a, 0.5);
            double i1 = a.LastInterestPerMonth.Value, r1 = a.LastRevenuePerMonth.Value;
            Bigger(a); Month(a, 0.5);
            double i2 = a.LastInterestPerMonth.Value, r2 = a.LastRevenuePerMonth.Value;
            Assert.True(r1 > 0 && r2 > 0 && i1 > 0 && i2 > i1, $"前提：歳入と利払い i1={i1} r1={r1} i2={i2} r2={r2}");
            double x1 = i1 / r1, x2 = i2 / r2;
            Assert.True(x2 > x1 && x2 < 1, $"前提：比 {x1} → {x2}");
            double limit = 0.5 * (x1 + x2);
            var w = Board(limit); Month(w, 0.5); Bigger(w);
            int before = Instruments.ArrearsPeek(w)!.Count;
            Month(w, 0.5);
            var rows = Instruments.ArrearsPeek(w)!;
            Assert.Equal(before + 1, rows.Count);
            var h = Instruments.ArrearsHeaderText.Split('\t')[1..];
            var v = rows[^1].Split('\t');
            Assert.Equal("1", v[Array.IndexOf(h, "underLimit")]);   // 月 1 の比 x1 ＜ L（前の月の比）＝上限の内
            Assert.True(w.LastInterestPerMonth.Value >= limit * w.LastRevenuePerMonth.Value, "前提：今月の比は上限の外");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>(1322) T6：列 prevInterestToRevenue は前の月の呼びの頭で見た比（underLimit に使った比）で、列 interestToRevenue（今の呼びの頭の比）とは別の値。期待値は試験が呼びの前後に読んだ欄から組む（計器の式を呼ばない）。</summary>
    [Fact]
    public void T6_列prevInterestToRevenueは前の呼びの頭の比で今の比ではない()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_tlu6_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var w = Board(0.67);
            double i0 = w.LastInterestPerMonth.Value, r0 = w.LastRevenuePerMonth.Value;   // 呼び 1 の頭で見える値
            Month(w, 0.5);
            double i1 = w.LastInterestPerMonth.Value, r1 = w.LastRevenuePerMonth.Value;   // 呼び 2 の頭で見える値
            Bigger(w);
            int before = Instruments.ArrearsPeek(w)!.Count;
            Month(w, 0.5);
            var rows = Instruments.ArrearsPeek(w)!;
            Assert.Equal(before + 1, rows.Count);
            var h = Instruments.ArrearsHeaderText.Split('\t')[1..];
            var v = rows[^1].Split('\t');
            Assert.Equal("prevInterestToRevenue", h[^1]);
            Assert.True(r0 > 0 && r1 > 0 && i0 >= 0, $"前提：歳入と利払い i0={i0} r0={r0} i1={i1} r1={r1}");
            double prev = double.Parse(v[Array.IndexOf(h, "prevInterestToRevenue")], System.Globalization.CultureInfo.InvariantCulture);
            double cur = double.Parse(v[Array.IndexOf(h, "interestToRevenue")], System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(i0 / r0, prev, 8);
            Assert.Equal(i1 / r1, cur, 8);
            Assert.True(Math.Abs(i0 / r0 - i1 / r1) > 1e-6, $"前提：前の呼びの比と今の呼びの比が同じ（この試験は取り違えを見分けられない）{i0 / r0} {i1 / r1}");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
