using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(756) 議会の掌握の起債の項の分母の単位の番人（旗 <see cref="Coefficients.ParliamentDebtGripYearGdp"/>）。
/// 機構だけを固定する（旗 1＝国債÷年の名目GDP／旗 0＝旧の月の名目GDP・床 1.0）。創発の大きさ（張り付きの割合・掌握の新÷旧）は帯の計器 pgrip_s が読む。</summary>
public class ParliamentGripYearTests
{
    private static World Make()
    {
        var w = new Simulation(NationConfig.France().ToScenario(1), new NullAgent()).World;
        // 議会が噛める税の掌握を 0 に固定する（常設の税だけ）＝掌握は起債の項だけで決まる
        w.TaxRate = new Share(0); w.ProgressiveTax = 0; w.PollTax = 0; w.LandTax = 1.0; w.SetStateMonopolyTier(0); w.Tariff = 0;
        return w;
    }

    private static double YearGdp(World w) => Calendar.PerMonthToPerYear(w.NominalGdpPerMonth).Value;

    [Fact]
    public void 旗1は国債を年の名目GDPで割る()
    {
        var w = Make(); w.Coef.ParliamentDebtGripYearGdp = 1.0;
        double y = YearGdp(w); Assert.True(y > 0, "前提：年の名目GDPが正");
        w.GovDebt = new MoneyStock(0.3 * y);
        Assert.Equal(0.3, PowerShares.DebtGrip(w, true), 9);
        Assert.Equal(0.3, PowerShares.ParliamentGrip(w), 9);   // 税の掌握 0 との noisy-or＝起債の項そのもの
    }

    [Fact]
    public void 旗0は旧の月の分母で1へ張り付く()
    {
        var w = Make(); w.Coef.ParliamentDebtGripYearGdp = 0.0;
        double y = YearGdp(w); Assert.True(y > 0);
        w.GovDebt = new MoneyStock(0.3 * y);   // 年の比 0.3 ＝ 月の分母なら 3.6 → 1 に張り付く
        Assert.Equal(1.0, PowerShares.DebtGrip(w, false), 9);
        Assert.Equal(1.0, PowerShares.ParliamentGrip(w), 9);
        // 旧の式そのもの（独立に書く）
        Assert.Equal(Math.Clamp(Math.Max(0, w.GovDebt.Value) / Math.Max(1.0, w.NominalGdpPerMonth.Value), 0.0, 1.0), PowerShares.DebtGrip(w, false), 12);
    }

    [Fact]
    public void 年の名目GDPが0の国は起債の項が0()
    {
        var w = Make(); w.Coef.ParliamentDebtGripYearGdp = 1.0;
        foreach (var r in w.Regions) foreach (var p in r.Producers) p.Output = 0;
        Assert.Equal(0.0, YearGdp(w), 12);
        w.GovDebt = new MoneyStock(1e9);
        Assert.Equal(0.0, PowerShares.DebtGrip(w, true), 12);
        Assert.Equal(0.0, PowerShares.ParliamentGrip(w), 12);
    }

    [Fact]
    public void 実効と文民制度は旗で選んだ掌握を通る()
    {
        var w = Make(); double y = YearGdp(w);
        w.GovDebt = new MoneyStock(0.3 * y);
        w.Coef.ParliamentDebtGripYearGdp = 1.0;
        double gNew = PowerShares.ParliamentGrip(w), peNew = PowerShares.ParliamentEffective(w), civNew = PowerShares.CivilianInstitutions(w);
        w.Coef.ParliamentDebtGripYearGdp = 0.0;
        double gOld = PowerShares.ParliamentGrip(w), peOld = PowerShares.ParliamentEffective(w), civOld = PowerShares.CivilianInstitutions(w);
        Assert.True(gNew < gOld, "年の分母は張り付きを緩める");
        Assert.Equal(PowerShares.ParliamentEffectiveOf(w, gNew), peNew, 12);
        Assert.Equal(PowerShares.ParliamentEffectiveOf(w, gOld), peOld, 12);
        Assert.Equal(PowerShares.CivilianInstitutionsOf(w, peNew), civNew, 12);
        Assert.Equal(PowerShares.CivilianInstitutionsOf(w, peOld), civOld, 12);
    }

    [Fact]
    public void 債務が年の名目GDPを超えれば旗1でも1へ飽和する()
    {
        var w = Make(); w.Coef.ParliamentDebtGripYearGdp = 1.0;
        w.GovDebt = new MoneyStock(3.0 * YearGdp(w));
        Assert.Equal(1.0, PowerShares.DebtGrip(w, true), 12);
    }
}
