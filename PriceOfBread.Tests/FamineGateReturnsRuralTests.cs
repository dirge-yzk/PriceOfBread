using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1283) 線 c132fg：旗 <c>FamineGateReturnsRural</c>＝飢えの門（PopDecision.Run：都市の充足が門の 0.8 の下で農村より低い月に農村→都市を 0 にする）が掛かった月に、
/// 移る値打ちの g（農村は現金＋自給・都市は現金−住居）が負なら都市→農村へ Mobility×g×都市の規模を戻す。
/// 盤面は試験の中で置く（英の開幕の州の最初の農村と都市の対）・期待は手の算術（g は盤面の literal から作る・エンジンの式を呼ばない）。
/// 旗 0 の世界は今のまま（門は 0 にするだけ）＝①の〈戻る〉Assert は旗 0 で赤。
/// </summary>
[Collection("Instruments")]
public class FamineGateReturnsRuralTests
{
    /// <summary>対の盤面：income は 1 人・月。州の都市の割合を 0.6 に置き住居の門の上（住居＝0.03×住居の値段）。食べ物の充足は農村 0.95・都市 <paramref name="urbanFill"/>。</summary>
    static (World w, Region r, Pop rural, Pop urban, Coefficients c) Board(double earnedR, double subR, double earnedU, double urbanFill, double flag)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World; var r = w.Region; var c = w.Coef;
        var (rural0, urban0) = PopDecision.MigrationPair(r);
        var rural = rural0!; var urban = urban0!;
        double tOther = 0, uOther = 0;
        foreach (var p in r.Pops) { if (ReferenceEquals(p, urban)) continue; tOther += p.Size; if (p.IsUrban) uOther += p.Size; }
        urban.Size = Math.Max((0.6 * tOther - uOther) / (1.0 - 0.6), 1.0);
        Assert.True(r.SumUrbanPopulation() / r.SumPopulation() > c.HousingUrbanThreshold, "前提：州の都市の割合が住居の門の上でない");
        rural.EarnedPerMonth = new MoneyPerMonth(earnedR * rural.Size); rural.SubsistenceValuePerMonth = new MoneyPerMonth(subR * rural.Size);
        urban.EarnedPerMonth = new MoneyPerMonth(earnedU * urban.Size);
        rural.FoodNeed = 0.8 * rural.Size; rural.FoodMet = 0.95 * rural.FoodNeed;
        urban.FoodNeed = 0.8 * urban.Size; urban.FoodMet = urbanFill * urban.FoodNeed;
        r.Markets[Catalog.HousingId].Price = 2.0;
        r.CapitalEnabled = false;
        Assert.Equal(0.0, c.UrbanFlowReal);   // 前提：今の既定（シグモイドの旧の式）の上で門を見る
        Assert.Equal(0.8, c.FamineMigrationFillGate);
        Assert.Equal(0.02, c.Mobility);
        c.FamineGateReturnsRural = flag;
        return (w, r, rural, urban, c);
    }

    static double G(double vr, double vu) => vr + vu > 1e-9 ? 2.0 * (vu - vr) / (vr + vu) : 0.0;

    /// <summary>本物の Run の前後の都市 POP の規模の差（農村→都市が正）。州の人口と、農村の減り＝都市の増えを確かめる（保存）。</summary>
    static double Moved(Region r, Pop rural, Pop urban, Coefficients c)
    {
        double u0 = urban.Size, r0 = rural.Size, t0 = r.SumPopulation();
        PopDecision.Run(r, c, 1.0);
        double d = urban.Size - u0;
        Assert.True(Math.Abs((r0 - rural.Size) - d) <= 1e-9 * Math.Max(1, Math.Abs(d)), "保存：農村の減りと都市の増えが合わない");
        Assert.True(Math.Abs(r.SumPopulation() - t0) <= 1e-9 * Math.Max(1, t0), "保存：州の人口が変わった");
        return d;
    }

    // 盤面 A（門が掛かる・g＜0）：農村 vr＝0.10＋1.00＝1.10・都市 vu＝0.60−0.03×2＝0.54・都市の現金＞農村の現金＝旧の式の流れは農村→都市（正）・都市の充足 0.5＜0.8 かつ＜農村 0.95
    [Fact]
    public void 旗1_飢えの門が掛かりgが負なら都市から農村へ_Mobility掛けるg掛ける都市の規模_旗0は0()
    {
        var (_, r0, ru0, ur0, c0) = Board(0.10, 1.00, 0.60, 0.5, flag: 0);
        Assert.Equal(0.0, Moved(r0, ru0, ur0, c0));   // 旗 0：門は流れを 0 にするだけ（今の規則）
        var (_, r, rural, urban, c) = Board(0.10, 1.00, 0.60, 0.5, flag: 1);
        double g = G(1.10, 0.54), us = urban.Size;
        Assert.True(g < 0, "前提：g が負でない");
        double moved = Moved(r, rural, urban, c);
        Assert.True(moved < 0, $"旗 1 で都市から農村へ戻らない（{moved}）");
        Assert.Equal(0.02 * g * us, moved, 9);
    }

    // 盤面 B（門の外＝都市の充足 0.9≧0.8）：旗 1 も旗 0 と 1 ビットも違わない（旧の式の農村→都市のまま）
    [Fact]
    public void 旗1_門の外では旗0と同じ流れ_ビット一致()
    {
        var (_, r0, ru0, ur0, c0) = Board(0.10, 1.00, 0.60, 0.9, flag: 0);
        var (_, r1, ru1, ur1, c1) = Board(0.10, 1.00, 0.60, 0.9, flag: 1);
        double d0 = Moved(r0, ru0, ur0, c0), d1 = Moved(r1, ru1, ur1, c1);
        Assert.True(d0 > 0, $"前提：門の外の旧の式の流れが農村→都市でない（{d0}）");
        Assert.Equal(BitConverter.DoubleToInt64Bits(d0), BitConverter.DoubleToInt64Bits(d1));
    }

    // 盤面 C（門が掛かる・g＞0）：自給 0・農村 vr＝0.10・都市 vu＝0.54 → g＞0 → 旗 1 でも流れは 0（戻すのは g が負の時だけ・都市へも押さない）
    [Fact]
    public void 旗1_門が掛かってもgが正なら流れは0()
    {
        var (_, r, rural, urban, c) = Board(0.10, 0.0, 0.60, 0.5, flag: 1);
        Assert.True(G(0.10, 0.54) > 0, "前提：g が正でない");
        Assert.Equal(0.0, Moved(r, rural, urban, c));
    }

    // 計器 urxpd_s の駆動の行（InstrumentsUrx.UrxPdDrive の写しの式）：旗 1 の門の月の流れが本物の Run の都市の増分と一致し、famineGated は真のまま
    [Fact]
    public void 計器の駆動の行は旗1の門の月に本物のRunと一致する()
    {
        var (_, r, rural, urban, c) = Board(0.10, 1.00, 0.60, 0.5, flag: 1);
        Assert.True(Instruments.UrxPdDrive(c, r, 1.0, out var d));
        Assert.True(d.FamineGated);
        double moved = Moved(r, rural, urban, c);
        Assert.True(moved < 0);
        Assert.True(Math.Abs(moved - d.Flow) <= 1e-9 * Math.Max(1, Math.Abs(moved)), $"計器の flow {d.Flow} と本物の Run {moved}");
        var (_, r0, _, _, c0) = Board(0.10, 1.00, 0.60, 0.5, flag: 0);
        Assert.True(Instruments.UrxPdDrive(c0, r0, 1.0, out var d0));
        Assert.True(d0.FamineGated); Assert.Equal(0.0, d0.Flow);
    }
}
