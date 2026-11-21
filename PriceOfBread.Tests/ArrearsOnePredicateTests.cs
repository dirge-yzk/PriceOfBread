using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1284) c132df：英の国債の不履行の入口・形 (e) の C1＋C2（旗 ArrearsOnePredicate）。兌換の停止の門を不履行の判定の前に置き、
/// 述語を不履行の導火線の積む条件（r＜1・国債＞0・利払いが上限・助走でない）と同じにする（正貨カバーと本位の法の条件は残す）。事前登録 measurements/m1284_20261121_prereg_df.txt。
/// 盤面＝ArrearsOpenedRoomTests と同じ作り（英を1四半期温め、国債＝年名目GDP の1倍・全て国内・借入の門を閉じる〔利払いの上限 0〕・Fiscal.Step を同じ r の月で踏む）。
/// まだ開いていない切り下げの余地は Simulation が月初に渡す〈払えない額 D×兌換〉（金の法）を手で置く。期待は試験の中の算術で作る（エンジンの出力から作らない）。</summary>
public class ArrearsOnePredicateTests
{
    private const double Ample = 1e18;

    internal static World Board(double flag)
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
        w.Coef.MaxInterestShareOfRevenue = new Share(0);   // 借入の門が閉じている（利払い÷歳入≧限界）＝払えない月の述語が立つ
        w.Coef.BondRepayFracPerMonth = new RatePerMonth(0);
        w.Coef.ArrearsOnePredicate = flag;
        w.InsolvencyMonths = 0;
        w.OnGoldStandard = true; w.Convertibility = 1.0;
        Assert.True(w.MonetaryEnabled && w.MetallicStandardLaw, "前提が崩れた：英に貨幣経済か金属の本位の法が無い");
        Assert.True(w.BroadMoney.Value > 1e-9 && w.BaseMoney / w.BroadMoney >= w.Coef.GoldCoverMin.Value, "前提が崩れた：正貨カバーが下限の下（カバーの門で離れる盤面）");
        return w;
    }

    /// <summary>切り下げ前の払えない額 D＝Ample の月を r で踏む。まだ開いていない余地＝D×兌換（金の法）。</summary>
    internal static void Month(World w, double r)
    {
        w.FiscalFundingRatio = new Share(r);
        Fiscal.Step(w, w.Coef, -Ample, 0, Ample * w.Convertibility);
    }

    [Fact]
    public void T1_金本位で払えない月が始まると1か月目に本位を離れ不履行しない_旗0は金本位のまま不履行()
    {
        const double r = 0.8;
        int want = 0; double acc = 0;   // 旧：金本位の月は 1−r を積み、月の請求 1 か月分に届いた月に不履行（試験の算術）
        for (int m = 1; m <= 12 && want == 0; m++) { acc += 1.0 - r; if (acc >= 1.0) want = m; }
        Assert.InRange(want, 5, 6);
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var w = Board(flag);
            int s0 = w.GoldSuspensions;
            int at = 0;
            for (int m = 1; m <= 8 && w.SovereignDefaultCount == 0; m++)
            {
                Month(w, r);
                if (m == 1 && flag > 0)
                {
                    Assert.False(w.OnGoldStandard, "旗 1：払えない月（r＝0.8・利払いが上限）の 1 か月目の末に金本位のまま");
                    Assert.True(w.GoldStandardSuspendedInCrisis, "旗 1：危機の停止として数えない");
                    Assert.Equal(s0 + 1, w.GoldSuspensions);
                }
                if (w.SovereignDefaultCount > 0) at = m;
            }
            if (flag > 0)
            {
                Assert.Equal(0, w.SovereignDefaultCount);
                Assert.Equal(0.0, w.InsolvencyMonths);   // 兌換 1＝余地はまだ開いていない＝金本位でない月の式で 0
                Assert.Equal(s0 + 1, w.GoldSuspensions);
            }
            else
            {
                Assert.Equal(want, at);   // 旧：r＝0.8≧0.7 で金本位のまま 1−r を積む
                Assert.Equal(s0, w.GoldSuspensions);
            }
        }
    }

    [Fact]
    public void T4_累計07でその月r04なら旗1は不履行せず本位を離れる_旗0は不履行()
    {
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var w = Board(flag);
            w.InsolvencyMonths = 0.7;
            Month(w, 0.4);
            Assert.False(w.OnGoldStandard, "どちらの旗でも月の末は金本位でない（旗 1＝門・旗 0＝不履行の RepudiateDebt）");
            if (flag > 0)
            {
                Assert.Equal(0, w.SovereignDefaultCount);
                Assert.InRange(w.InsolvencyMonths, 0.7 - 1e-12, 0.7 + 1e-12);   // 門が判定の前に開いた＝その月は金本位でない月の式（兌換 1＝0）
            }
            else Assert.Equal(1, w.SovereignDefaultCount);   // 旧：0.7＋0.6≧1 で不履行
        }
    }

    [Fact]
    public void T5_rが1未満でも利払いが上限の内なら旗1は本位のまま_旗0はr07未満で離れる()
    {
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var w = Board(flag);
            w.Coef.MaxInterestShareOfRevenue = new Share(1e9);
            int s0 = w.GoldSuspensions;
            for (int m = 0; m < 3; m++)
            {
                w.LastInterestPerMonth = new MoneyPerMonth(0); w.LastRevenuePerMonth = w.NominalGdpPerMonth;   // 利払い÷歳入が限界の下＝払えない月の述語が立たない
                Month(w, 0.6);
            }
            Assert.Equal(0.0, w.InsolvencyMonths);
            Assert.Equal(0, w.SovereignDefaultCount);
            if (flag > 0) { Assert.True(w.OnGoldStandard, "旗 1：上限の内の月（述語が立たない）に本位を離れた"); Assert.Equal(s0, w.GoldSuspensions); }
            else { Assert.False(w.OnGoldStandard, "旗 0：r＝0.6＜0.7 で旧の門が開かない"); Assert.Equal(s0 + 1, w.GoldSuspensions); }
        }
    }
}

/// <summary>(1284) 計器 arrears_s（Instruments/InstrumentsArrears.cs）の番人：同じ盤面で旧と新の規則の門と積む量を、旗に依らず同じ行で数える。
/// 期待は試験の算術（1−r と兌換）で書く。</summary>
[Collection("Instruments")]
public class ArrearsInstrumentTests
{
    [Fact]
    public void I1_旧と新の門と積む量を旗に依らず同じ行で数える()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ar_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        Instruments.SelectTables("arrears_s");
        try
        {
            var rows = new string[2];
            int k = 0;
            foreach (double flag in new[] { 1.0, 0.0 })
            {
                var w = ArrearsOnePredicateTests.Board(flag);
                ArrearsOnePredicateTests.Month(w, 0.8);
                var a = Instruments.ArrearsPeek(w);
                Assert.NotNull(a);
                Assert.Equal(1, a!.Rows);
                Assert.True(a.LastUnpaidMonth && a.LastNewTrip && !a.LastOldTrip, "r＝0.8・利払いが上限：新の門だけが開く");
                Assert.Equal(1.0 - 0.8, a.LastOldArrears);   // 旧：月の頭に金本位＝1−r
                Assert.Equal(0.0, a.LastNewArrears);          // 新：門が先に開く＝金本位でない月の式・兌換 1＝(1−r)×(1−1)
                rows[k++] = a.Pending[0];
                if (flag > 0)
                {
                    ArrearsOnePredicateTests.Month(w, 0.8);   // 2 か月目：金本位でない＝どちらの門も開かない・積む述語は立つ
                    Assert.Equal(2, a.Rows);
                    Assert.True(a.LastUnpaidMonth && !a.LastNewTrip && !a.LastOldTrip, "金本位でない月に門が開いた");
                }
            }
            Assert.Equal(rows[1], rows[0]);   // 同じ盤面の 1 か月目の行は旗に依らずバイトで同じ（計器は門の前で読む）
        }
        finally { Instruments.SelectTables(null); Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
