using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1284) c132df：本位の停止の門を不履行の導火線と同じ述語（払えない月・国債あり・利払いが上限の外・助走でない）にし、累計と判定の前に置く（旗 ArrearsOnePredicate・形 (e) の C1＋C2）。
/// 盤面＝英を1四半期温め、国内の国債（年名目GDP の1倍）を置き、借入の門を閉じ（限界0＝利払いが上限の外）、正貨カバーの項を塞ぎ（GoldCoverMin 0）、Fiscal.Step を同じ r の月で踏む（ArrearsOpenedRoomTests と同じ作り）。
/// 払えない額 D＝Ample・まだ開いていない余地＝D×兌換（金の法の式）を手で置く。兌換は Fiscal.Step では動かない（追従は四半期の末）。期待は試験の中の算術。
/// 事前登録 measurements/m1284_20261122_prereg_df.txt §2-3・§2-4（突然変異 M1・M4・M6）。</summary>
[Collection("Instruments")]
public class ArrearsOnePredicateTests
{
    private const double Ample = 1e18;

    private static World Board(double flag)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(1);
        var w = sim.World;
        w.TaxRate = new Share(0); w.PollTax = 0; w.LandTax = 0; w.ProgressiveTax = 0;
        w.SocialSecurity = 0; w.Pension = ReformLevel.None;
        w.Coef.ForeignBondCapacity = new Share(0);
        w.GovDebt = new MoneyStock(Calendar.PerMonthToPerYear(1.0 * w.NominalGdpPerMonth).Value); w.ForeignDebt = new MoneyStock(0);
        Fiscal.SeedBondHoldings(w);
        w.WarmupFrozen = false;
        w.Coef.MaxInterestShareOfRevenue = new Share(0);   // 利払いが上限の外（underLimit 偽）
        w.Coef.BondRepayFracPerMonth = new RatePerMonth(0);
        w.Coef.GoldCoverMin = new Share(0);                 // 正貨カバーの項で離れない
        w.Coef.ArrearsOnePredicate = flag;
        w.InsolvencyMonths = 0;
        w.OnGoldStandard = true; w.Convertibility = 1.0;
        Assert.True(w.MonetaryEnabled && w.MetallicStandardLaw, "前提が崩れた：英に貨幣経済か金属の本位の法が無い");
        Assert.True(w.GovDebt.Value > 0, "前提が崩れた：国債が無い");
        return w;
    }

    private static void Month(World w, double r)
    {
        w.FiscalFundingRatio = new Share(r);
        Fiscal.Step(w, w.Coef, -Ample, 0, Ample * w.Convertibility);
    }

    [Fact]
    public void T1_払えない月の1か月目の末に本位を離れ不履行しない_旗0は金本位のまま累計が線に届いて不履行()
    {
        const double r = 0.8; const int months = 8;
        // 旗 0 の期待（試験の算術）：金本位の月は 1−r を積む＝累計が DefaultArrearsMonths に届いた月に不履行
        double line = new Coefficients().DefaultArrearsMonths, acc = 0; int expect = 0;
        for (int m = 1; m <= months && expect == 0; m++) { acc += 1.0 - r; if (acc >= line) expect = m; }
        Assert.True(expect >= 2 && expect <= months, $"前提が崩れた：旗 0 の期待の月 {expect}");
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var w = Board(flag);
            int at = 0;
            for (int m = 1; m <= months; m++)
            {
                Month(w, r);
                if (m == 1 && flag > 0)
                {
                    Assert.False(w.OnGoldStandard, "旗 1：r＝0.8・上限の外の月に本位を離れない");
                    Assert.Equal(0.0, w.InsolvencyMonths);   // 離れた月は余地がまだ開いていない＝0 を積む（(1250) の規則）
                }
                if (at == 0 && w.SovereignDefaultCount > 0) at = m;
            }
            if (flag > 0) { Assert.Equal(0, w.SovereignDefaultCount); Assert.Equal(0.0, w.InsolvencyMonths); }
            else Assert.Equal(expect, at);
        }
    }

    [Fact]
    public void T4_累計07でその月r04は本位を離れて不履行しない_旗0は不履行()
    {
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var w = Board(flag);
            w.InsolvencyMonths = 0.7;
            Assert.True(0.7 + (1.0 - 0.4) >= w.Coef.DefaultArrearsMonths, "前提が崩れた：金本位の月の 1−r を足すと線に届かない");
            Month(w, 0.4);
            if (flag > 0)
            {
                Assert.Equal(0, w.SovereignDefaultCount);
                Assert.False(w.OnGoldStandard, "旗 1：その月に本位を離れない");
                Assert.Equal(0.7, w.InsolvencyMonths);
            }
            else Assert.Equal(1, w.SovereignDefaultCount);
        }
    }

    [Fact]
    public void T5_払えない月でも利払いが上限の内なら本位のまま_旗0は離れる()
    {
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var w = Board(flag);
            w.Coef.MaxInterestShareOfRevenue = new Share(0.67);
            w.LastInterestPerMonth = new MoneyPerMonth(0); w.LastRevenuePerMonth = new MoneyPerMonth(1.0);   // 利払い÷歳入 0＜0.67＝上限の内
            Month(w, 0.5);
            if (flag > 0) { Assert.True(w.OnGoldStandard, "旗 1：上限の内の月に本位を離れた"); Assert.Equal(0.0, w.InsolvencyMonths); }
            else Assert.False(w.OnGoldStandard, "旗 0：r＝0.5＜0.7 で離れない");
        }
    }

    [Fact]
    public void T6_計器arrears_sは同じ月の旧と新の述語と月の量を自分の算術で並べる()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ar_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            foreach (double flag in new[] { 1.0, 0.0 })
            {
                var w = Board(flag);
                int before = Instruments.ArrearsPeek(w)?.Count ?? 0;
                Month(w, 0.8);
                var rows = Instruments.ArrearsPeek(w);
                Assert.NotNull(rows);
                Assert.Equal(before + 1, rows!.Count);
                var h = Instruments.ArrearsHeaderText.Split('\t')[1..];   // 四半期の列は表を書くときに頭へ付く
                var v = rows[^1].Split('\t');
                Assert.Equal(h.Length, v.Length);
                string Col(string n) => v[System.Array.IndexOf(h, n)];
                Assert.Equal("1", Col("onGoldBefore"));
                Assert.Equal("1", Col("owes"));
                Assert.Equal("0", Col("predOld"));   // 0.8 ≥ 0.7
                Assert.Equal("1", Col("predNew"));
                Assert.Equal(1.0 - 0.8, double.Parse(Col("arrOldMonth"), System.Globalization.CultureInfo.InvariantCulture), 12);
                Assert.Equal(0.0, double.Parse(Col("arrNewMonth"), System.Globalization.CultureInfo.InvariantCulture));
            }
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
