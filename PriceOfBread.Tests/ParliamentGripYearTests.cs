using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(756) 議会の掌握の起債の項の分母の単位の番人（(761) で旗 ParliamentDebtGripYearGdp を焼き込んだ）。
/// 機構だけを固定する（国債÷年の名目GDP）。創発の大きさ（張り付きの割合）は帯が見る。</summary>
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
        var w = Make();
        double y = YearGdp(w); Assert.True(y > 0, "前提：年の名目GDPが正");
        w.GovDebt = new MoneyStock(0.3 * y);
        Assert.Equal(0.3, PowerShares.DebtGrip(w), 9);
        Assert.Equal(0.3, PowerShares.ParliamentGrip(w), 9);   // 税の掌握 0 との noisy-or＝起債の項そのもの
    }

    [Fact]
    public void 年の名目GDPが0の国は起債の項が0()
    {
        var w = Make();
        foreach (var r in w.Regions) foreach (var p in r.Producers) p.Output = 0;
        Assert.Equal(0.0, YearGdp(w), 12);
        w.GovDebt = new MoneyStock(1e9);
        Assert.Equal(0.0, PowerShares.DebtGrip(w), 12);
        Assert.Equal(0.0, PowerShares.ParliamentGrip(w), 12);
    }

    [Fact]
    public void 債務が年の名目GDPを超えれば旗1でも1へ飽和する()
    {
        var w = Make();
        w.GovDebt = new MoneyStock(3.0 * YearGdp(w));
        Assert.Equal(1.0, PowerShares.DebtGrip(w), 12);
    }
}
