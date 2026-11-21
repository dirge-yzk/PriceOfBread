using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1283) c132fg 旗 <c>FamineGateReturnsRural</c>：飢えの門（都市の食べ物の充足が門 0.8 の下かつ農村より下のとき農村→都市を止める）が掛かった月に、
/// 移る値打ちの g（農村＝現金＋自給の値・都市＝現金−家賃）が負なら、Mobility×g×都市の規模だけ都市→農村へ戻す。
/// 盤面は試験の中で置く（英の開幕の州の対の POP に稼ぎ・自給の値・食べ物の充足を書く）。g の期待は盤面の量から手で作る（MigrationValues を呼ばない）。
/// </summary>
public class FamineGateReturnsRuralTests
{
    /// <summary>対の盤面：1 人・月の稼ぎと自給の値・食べ物の充足を書く。家賃は 0（住居の価格 0）。</summary>
    static (Region r, Pop rural, Pop urban, Coefficients c) Board(double earnedR, double subR, double earnedU, double fillR, double fillU)
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent()).World;
        var r = w.Region; var c = w.Coef;
        var (rural0, urban0) = PopDecision.MigrationPair(r);
        var rural = rural0!; var urban = urban0!;
        // 対の都市の割合を目標（シグモイド）より下に置く＝旧の式の流れが農村→都市の正になる盤面
        urban.Size = 0.05 * rural.Size;
        rural.EarnedPerMonth = new MoneyPerMonth(earnedR * rural.Size); rural.SubsistenceValuePerMonth = new MoneyPerMonth(subR * rural.Size);
        urban.EarnedPerMonth = new MoneyPerMonth(earnedU * urban.Size); urban.SubsistenceValuePerMonth = new MoneyPerMonth(0.0);
        rural.FoodNeed = 0.8 * rural.Size; rural.FoodMet = fillR * rural.FoodNeed;
        urban.FoodNeed = 0.8 * urban.Size; urban.FoodMet = fillU * urban.FoodNeed;
        r.Markets[Catalog.HousingId].Price = 0.0;
        r.CapitalEnabled = false;
        Assert.Equal(0.8, c.FamineMigrationFillGate);
        Assert.Equal(0.02, c.Mobility);
        return (r, rural, urban, c);
    }

    static double G(double vr, double vu) => vr + vu > 1e-9 ? 2.0 * (vu - vr) / (vr + vu) : 0.0;

    /// <summary>本物の Run の前後の都市の規模の差（農村→都市が正）と、州の人口・貯蓄の和が閉じることの確かめ。</summary>
    static double Moved(Region r, Pop rural, Pop urban, Coefficients c)
    {
        double u0 = urban.Size, r0 = rural.Size, pop0 = r.SumPopulation(), sav0 = 0;
        foreach (var p in r.Pops) sav0 += p.Savings.Value;
        PopDecision.Run(r, c, 1.0);
        double d = urban.Size - u0;
        Assert.True(Math.Abs((r0 - rural.Size) - d) <= 1e-9 * Math.Max(1, Math.Abs(d)), "保存：農村の減りと都市の増えが合わない");
        Assert.True(Math.Abs(r.SumPopulation() - pop0) <= 1e-9 * pop0, "保存：州の人口が閉じない");
        double sav1 = 0; foreach (var p in r.Pops) sav1 += p.Savings.Value;
        Assert.True(Math.Abs(sav1 - sav0) <= 1e-9 * Math.Max(1, Math.Abs(sav0)), "保存：州の貯蓄の和が閉じない");
        return d;
    }

    [Fact]
    public void T1_飢えた都市で農村の値打ちが上なら旗1は都市から農村へMobility掛けるg掛ける都市の規模だけ戻す()
    {
        // 都市の現金 0.6＞農村の現金 0.1（旧の式は農村→都市）・都市の充足 0.5＜0.8 かつ＜農村 0.95（門が掛かる）・農村の値打ち 0.1＋2.0＝2.1＞都市 0.6 → g＝2(0.6−2.1)/2.7
        var (r, rural, urban, c) = Board(0.1, 2.0, 0.6, 0.95, 0.5);
        c.FamineGateReturnsRural = 1;
        double g = G(0.1 + 2.0, 0.6), us = urban.Size;
        Assert.True(g < 0);
        Assert.Equal(0.02 * g * us, Moved(r, rural, urban, c), 9);
    }

    [Fact]
    public void T2_旗0は同じ盤面で動かさない_門は止めるだけ()
    {
        var (r, rural, urban, c) = Board(0.1, 2.0, 0.6, 0.95, 0.5);
        Assert.Equal(0.0, c.FamineGateReturnsRural);
        Assert.Equal(0.0, Moved(r, rural, urban, c));
    }

    [Fact]
    public void T3_門の外_都市の充足が門の上_では旗1も旧の式と同じビット()
    {
        // 都市の充足 0.9≧0.8＝門は掛からない＝旧の式の農村→都市（正）が旗 0 と 1 で同じビット
        var (r0, ru0, ur0, c0) = Board(0.1, 2.0, 0.6, 0.95, 0.9);
        var (r1, ru1, ur1, c1) = Board(0.1, 2.0, 0.6, 0.95, 0.9);
        c1.FamineGateReturnsRural = 1;
        double d0 = Moved(r0, ru0, ur0, c0), d1 = Moved(r1, ru1, ur1, c1);
        Assert.True(d0 > 1e-9, "前提：旧の式で農村→都市が出ていない");
        Assert.Equal(BitConverter.DoubleToInt64Bits(d0), BitConverter.DoubleToInt64Bits(d1));
    }

    [Fact]
    public void T4_門が掛かってもgが正なら旗1は動かさない()
    {
        // 農村の値打ち 0.1＋0.1＝0.2＜都市 0.6 → g＞0（都市の方が値打ちが上）＝戻さない・止めるだけ
        var (r, rural, urban, c) = Board(0.1, 0.1, 0.6, 0.95, 0.5);
        c.FamineGateReturnsRural = 1;
        Assert.True(G(0.2, 0.6) > 0);
        Assert.Equal(0.0, Moved(r, rural, urban, c));
    }

    [Fact]
    public void T5_計器の写しUrxPdDriveは旗1の本物のRunの流れと同じ()
    {
        var (r, rural, urban, c) = Board(0.1, 2.0, 0.6, 0.95, 0.5);
        c.FamineGateReturnsRural = 1;
        Assert.True(Instruments.UrxPdDrive(c, r, 1.0, out var d));
        Assert.True(d.FamineGated);
        double moved = Moved(r, rural, urban, c);
        Assert.True(moved < -1e-9, "前提：戻りが出ていない");
        Assert.Equal(moved, d.Flow, 9);
    }
}
