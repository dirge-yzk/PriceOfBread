using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1380) c146rf 束 1 の番人（恒等の整理の足場）：①正貨の覆い率 World.SpecieCoverRatio（A2-09）の床 1e-9 ②与信の評価 CreditRating が 4 項の積と
/// ビットまで同じ（V01・左からの順）③段の対応の表（A1-07：年金＝CornFactor・領主制＝LandReformDegreeFor）④Clamp01 の NaN の扱い（A2-18）。
/// 期待は試験が手で置いた数（リテラル）と、独立に書いた式から作る。突然変異＝M1 床を 0.0 へ・M3 掛ける順の入れ替えで赤
/// （measurements/m1380_20261126/mutation.txt）。AI とプレイヤーの区別は無い（どれも World だけを読む）。</summary>
public class RefactorBundle1Tests
{
    private static (MultiNationSim Sim, World Uk, World Fr) Setup(Coefficients coef)
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent(), coef) { DiplomacyFrozen = true };
        return (sim, sim.Nations[0].World, sim.Nations[1].World);
    }

    [Fact]
    public void 覆い率は広義の貨幣が床以下なら1_それより上なら基礎を広義で割る()
    {
        var (_, uk, _) = Setup(new Coefficients());
        // 広義＝全 pop の貯蓄の和。1 個の pop だけ 1e-10（床 1e-9 以下）にして残りを 0 にする。
        bool first = true;
        foreach (var (_, p) in uk.AllPopsWithRegion()) { p.ApplyBakedSavings(first ? 1e-10 : 0.0); first = false; }
        uk.SetTreasuryRaw(5.0);   // 基礎貨幣＝広義＋国庫−貸出＝広義と違う値にして、床の枝を外すと 1.0 でなくなるようにする
        Assert.True(uk.BroadMoney.Value > 0 && uk.BroadMoney.Value <= 1e-9);
        Assert.Equal(1.0, uk.SpecieCoverRatio());   // 床の枝＝1.0（床を 0.0 にすると 基礎÷広義 へ落ちて 1.0 でなくなる）
        foreach (var (_, p) in uk.AllPopsWithRegion()) p.ApplyBakedSavings(1000.0);
        double broad = uk.BroadMoney.Value;
        Assert.True(broad > 1e-9);
        double expect = (broad + uk.Treasury.Value - uk.OutstandingLoans.Value) / broad;   // BaseMoney の定義から独立に作る
        Assert.Equal(expect, uk.SpecieCoverRatio(), 12);
    }

    [Fact]
    public void 与信の評価は4項の積と左からの順でビットまで同じ()
    {
        var coef = new Coefficients();
        var (sim, uk, fr) = Setup(coef);
        uk.Convertibility = 0.37; fr.Convertibility = 0.81;
        uk.ExchangeRate = 0.83; fr.ExchangeRate = 0.61;
        uk.GovDebt = new MoneyStock(uk.NominalGdpPerMonth.Value * 12 * 0.37); fr.GovDebt = new MoneyStock(fr.NominalGdpPerMonth.Value * 12 * 0.53);
        CapitalMarket.Step(sim.Nations, coef);
        int differ = 0;
        foreach (var w in new[] { uk, fr })
        {
            // 手で左から掛ける（((債務×金)×為替)×債券）。
            double hand = w.CreditDebtTerm * w.CreditGoldTerm * w.CreditFxTerm * w.CreditBondTerm;
            Assert.Equal(System.BitConverter.DoubleToInt64Bits(hand), System.BitConverter.DoubleToInt64Bits(w.CreditRating));
            Assert.True(w.CreditDebtTerm > 0 && w.CreditDebtTerm < 1 && w.CreditGoldTerm > 0 && w.CreditGoldTerm < 1 && w.CreditFxTerm > 0 && w.CreditFxTerm < 1);
            // 掛け方を替えた積（債務×(金×為替)×債券）と丸めが違う国があること＝この試験が順の取り違えを見分けられる前提。
            if (w.CreditDebtTerm * (w.CreditGoldTerm * w.CreditFxTerm) * w.CreditBondTerm != hand) differ++;
        }
        Assert.True(differ >= 1, "盤面の値では掛ける順の違いが丸めに出ない");
    }

    [Theory]
    [InlineData(ReformLevel.None, 0.0, 1.0)]
    [InlineData(ReformLevel.Limited, 0.5, 0.5)]
    [InlineData(ReformLevel.Full, 1.0, 0.0)]
    public void 段の対応の表は手のリテラルと同じ(ReformLevel lv, double pension, double seigneurial)
    {
        Assert.Equal(pension, InternationalMarket.CornFactor(lv));   // 年金の段（Fiscal.cs の pensionLevel）
        Assert.Equal(seigneurial, Privileges.LandReformDegreeFor(lv));   // 領主制の度合い（SeigneurialDegree が同じ表を使う）
        var (_, uk, _) = Setup(new Coefficients());
        uk.SerfEmancipation = lv;
        Assert.Equal(seigneurial, Privileges.SeigneurialDegree(uk));
    }

    [Theory]
    [InlineData(double.NaN, 0.0)]
    [InlineData(-0.5, 0.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(0.25, 0.25)]
    [InlineData(1.0, 1.0)]
    [InlineData(3.0, 1.0)]
    public void Clamp01はNaNを0へ倒す(double x, double expect)
        => Assert.Equal(expect, PoliticalQuantities.Clamp01(x));

    [Fact]
    public void 寄せる式は旧の書き方とビットまで同じ()
    {
        // 旧：x + k*(t−x)（Fiscal.cs:101・World.cs の期待インフレ）。手で書いた旧の式と Toward を比べる。
        double[] xs = [0.0, 0.1, 3.3, 17.77, 1e5], ts = [0.0, 0.07, 5.5, 123.456, 9e4], ks = [0.15, 0.34, 0.5];
        foreach (double x in xs) foreach (double t in ts) foreach (double k in ks)
            Assert.Equal(System.BitConverter.DoubleToInt64Bits(x + k * (t - x)), System.BitConverter.DoubleToInt64Bits(PoliticalQuantities.Toward(x, t, k)));
    }
}
