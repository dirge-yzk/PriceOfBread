using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1215) c122bd：金本位でない月の不履行の累計は〈開いた切り下げの余地で払えなかった分〉だけ（旗 ArrearsBeyondOpenedRoom）。
/// 盤面＝英を1四半期温め、国債（年名目GDP の1倍）を置き、借入の門を閉じ（限界0）、Fiscal.Step を同じ r の月で踏む（FiscalTests の不履行の番人と同じ作り）。
/// 余地（World.DevaluationRoomPerMonth）は Simulation が月初に書く値＝試験は払えない額 Ample×(1−兌換) を手で置く（金の法の式）。
/// 事前登録 measurements/m1215_20261118_prereg_bd.txt §3・§4。</summary>
public class ArrearsOpenedRoomTests
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
        w.Coef.MaxInterestShareOfRevenue = new Share(0);   // 借入の門が閉じている
        w.Coef.BondRepayFracPerMonth = new RatePerMonth(0);
        w.Coef.ArrearsBeyondOpenedRoom = flag;
        w.InsolvencyMonths = 0;
        return w;
    }

    /// <summary>払えない額 Ample の月を r で踏む。余地＝Ample×(1−兌換)（金の法）・切り下げ額＝devalued。</summary>
    private static void Month(World w, double r, double devalued = 0)
    {
        w.DevaluationRoomPerMonth = Ample * (1.0 - w.Convertibility);
        w.FiscalFundingRatio = new Share(r);
        Fiscal.Step(w, w.Coef, -Ample, devalued);
    }

    [Fact]
    public void T1_離脱の後は余地が開く前に不履行しない_旗0は月m加2に不履行()
    {
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var w = Board(flag);
            Assert.True(w.MonetaryEnabled && w.MetallicStandardLaw, "前提が崩れた：英に貨幣経済か金属の本位の法が無い");
            w.OnGoldStandard = true; w.Convertibility = 1.0;
            Month(w, 0.6);   // 月 m：金本位の月の未払い 0.4（旧と同じ）→ この月に強制離脱（r＜0.7）
            Assert.False(w.OnGoldStandard, "前提が崩れた：r＝0.6 の月に兌換を止めない");
            Assert.InRange(w.InsolvencyMonths, 0.4 - 1e-12, 0.4 + 1e-12);
            Assert.Equal(0, w.SovereignDefaultCount);
            Month(w, 0.6);   // 月 m+1：兌換はまだ 1（追従は四半期の末）＝余地 0
            Month(w, 0.6);   // 月 m+2
            if (flag > 0)
            {
                Assert.Equal(0, w.SovereignDefaultCount);
                Assert.InRange(w.InsolvencyMonths, 0.4 - 1e-12, 0.4 + 1e-12);   // 金本位でない月は 0 を積む（余地を全て取った＝余地 0・切り下げ 0）
            }
            else Assert.Equal(1, w.SovereignDefaultCount);   // 旧：0.4＋0.4＋0.4＝1.2 で月 m+2 に不履行
        }
    }

    [Fact]
    public void T1b_金本位でない月は余地で払えなかった分だけを積む()
    {
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var w = Board(flag);
            w.OnGoldStandard = false; w.Convertibility = 0.6;
            Month(w, 0.7);   // 切り下げ 0（貯蓄の不足の型）・余地＝Ample×0.4
            Assert.Equal(0, w.SovereignDefaultCount);
            double want = flag > 0 ? 0.12 : 0.3;   // 旗 1＝(1−r)×(余地−切り下げ)÷払えない額＝0.3×0.4・旗 0＝1−r
            Assert.InRange(w.InsolvencyMonths, want - 1e-12, want + 1e-12);
            var v = Board(flag);
            v.OnGoldStandard = false; v.Convertibility = 0.6;
            Month(v, 0.7, devalued: Ample * 0.4);   // 余地を全て取った月
            double want2 = flag > 0 ? 0.0 : 0.3;
            Assert.InRange(v.InsolvencyMonths, want2 - 1e-12, want2 + 1e-12);
        }
    }

    [Fact]
    public void T2_金本位のままの月は両方の旗で同じに積む()
    {
        int[] at = new int[2]; double[] after1 = new double[2];
        for (int k = 0; k < 2; k++)
        {
            var w = Board(k == 0 ? 1.0 : 0.0);
            w.OnGoldStandard = true; w.Convertibility = 1.0;
            for (int m = 1; m <= 8 && w.SovereignDefaultCount == 0; m++)
            {
                Month(w, 0.75);
                Assert.True(w.OnGoldStandard || w.SovereignDefaultCount > 0, "前提が崩れた：r＝0.75 の月に兌換を止めた（群 A の型にならない）");
                if (m == 1) after1[k] = w.InsolvencyMonths;
                if (w.SovereignDefaultCount > 0) at[k] = m;
            }
        }
        Assert.InRange(after1[0], 0.25 - 1e-12, 0.25 + 1e-12);
        Assert.Equal(after1[1], after1[0]);
        Assert.Equal(4, at[0]);   // 0.25×4＝1.0（2 進で丁度）で 4 か月目に不履行
        Assert.Equal(at[1], at[0]);
    }

    [Fact]
    public void T3_兌換の追従は旗で変わらない()
    {
        foreach (bool on in new[] { true, false })
        {
            double[] got = new double[2];
            for (int k = 0; k < 2; k++)
            {
                var w = Board(k == 0 ? 1.0 : 0.0);
                w.OnGoldStandard = on; w.Convertibility = on ? 0.5 : 0.9;
                w.UpdatePriceLevel(w.Coef);
                got[k] = w.Convertibility;
            }
            Assert.Equal(got[1], got[0]);
            if (on) Assert.True(got[0] > 0.5 && got[0] <= 0.5 + 0.05 * 0.5 + 1e-12, $"金本位の上がる側が 0.05 の速さでない：{got[0]}");
            else Assert.InRange(got[0], 0.594 - 1e-12, 0.594 + 1e-12);   // 0.9＋0.34×(0−0.9)
        }
    }
}
