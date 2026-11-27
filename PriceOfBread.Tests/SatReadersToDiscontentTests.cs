using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1371) c145rv 旗 SatReadersToDiscontent：不満度 D＝Clamp01(WantWeight×Pos(R−s))（声の項を抜く・案 D1）・参加と思想は D だけを読む（闘争度を読まない）。
/// 盤面は試験の中で置く（仏 1789・全 POP の意識と識字 0.9・満足度 0.5）。期待値は手計算のリテラル（WantWeight＝0.8）で本体と同じ式から作らない。
/// 旗 0＝今の式（声の項あり・参加は max(D, 闘争度)）＝T1・T2 が赤になる対照を T1・T2 の中に持つ。
/// (1385) c147wb：T6（AI の不穏が D̄）・T7（信任が D̄）を足した。突然変異 μ2（D̄ を満足へ戻す）→T6・T7 赤。
/// 突然変異 M4 声の項を戻す→T1 赤・M5 参加の max を戻す→T2a 赤・M6 思想の闘争度を戻す→T2b 赤（事前登録 m1371 の表）。</summary>
[Collection("Instruments")]
public class SatReadersToDiscontentTests
{
    private static World Board(double flag, double drop, double militancy)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.Run(8);
        var w = sim.World; var c = w.Coef;
        c.SatReadersToDiscontent = flag;
        Assert.Equal(0.8, c.WantWeight);   // 手計算のリテラルの前提
        c.ConsciousnessSpeed = 0;
        foreach (var r in w.Regions)
            foreach (var p in r.Pops) { p.Militancy = militancy; p.Consciousness = 0.9; p.Literacy = 0.9; p.Satisfaction = 0.5; p.LivingRef = 0.5 + drop; p.CoreEthnicShare = new Share(1); }
        return w;
    }

    private static List<Pop> Active(World w) { var l = new List<Pop>(); foreach (var r in w.Regions) foreach (var p in r.Pops) if (p.Size > 1e-9) l.Add(p); return l; }

    [Fact]
    public void T1_声が届かず法の隔たりが大きい意識の高い人でR等しsならDが0()
    {
        var w = Board(1, 0.0, 0.1); var c = w.Coef;
        var coord = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c);
        var pops = Active(w);
        Assert.True(pops.Count > 50);
        // 対照＝旗 0 の同じ盤面では声の項が D を立てる（前提：この盤面が声の項に感度を持つ）
        c.SatReadersToDiscontent = 0;
        int voiced = 0; foreach (var p in pops) if (Politics.Discontent(w, c, p, coord, 0) > 0.02) voiced++;
        Assert.True(voiced > 50, $"声の項が立った POP が少ない {voiced}");
        c.SatReadersToDiscontent = 1;
        foreach (var p in pops) Assert.Equal(0.0, Politics.Discontent(w, c, p, coord, 0));
    }

    [Fact]
    public void T2a_参加が闘争度に依らない()
    {
        var wa = Board(1, 0.1, 0.0); var wb = Board(1, 0.1, 0.9);
        var coord = PoliticalQuantities.DriftedPolityCoord(wa.Polity, wa.Suffrage, wa.Coef);
        var pa = Active(wa); var pb = Active(wb);
        Assert.Equal(pa.Count, pb.Count);
        for (int i = 0; i < pa.Count; i++)
        {
            double a = FactionSnapshot.Participation(wa, pa[i], coord, []), b = FactionSnapshot.Participation(wb, pb[i], coord, []);
            Assert.Equal(a, b, 12);
            Assert.Equal(0.8 * 0.1, a, 12);   // D＝0.8×落差 0.1（手計算）＝闘争度 0.9 より小さい盤面
        }
    }

    [Fact]
    public void T2b_思想の更新が闘争度に依らない()
    {
        var wa = Board(1, 0.1, 0.0); var wb = Board(1, 0.1, 0.9);
        Ideologies.UpdateShares(wa); Ideologies.UpdateShares(wb);
        var pa = Active(wa); var pb = Active(wb);
        double diff = 0;
        for (int i = 0; i < pa.Count; i++)
            for (int k = 0; k < pa[i].IdeologyShares.Length; k++) diff += Math.Abs(pa[i].IdeologyShares[k] - pb[i].IdeologyShares[k]);
        Assert.Equal(0.0, diff, 12);
        // 対照＝旗 0 では闘争度で思想が動く
        var xa = Board(0, 0.1, 0.0); var xb = Board(0, 0.1, 0.9);
        Ideologies.UpdateShares(xa); Ideologies.UpdateShares(xb);
        var qa = Active(xa); var qb = Active(xb);
        double d0 = 0;
        for (int i = 0; i < qa.Count; i++)
            for (int k = 0; k < qa[i].IdeologyShares.Length; k++) d0 += Math.Abs(qa[i].IdeologyShares[k] - qb[i].IdeologyShares[k]);
        Assert.True(d0 > 1e-6, $"旗 0 で思想が闘争度に感度を持たない {d0}");
    }

    [Fact]
    public void T3_DはGに比例する()
    {
        foreach (double g in new[] { 0.05, 0.10, 0.20 })
        {
            var w = Board(1, g, 0.0); var c = w.Coef;
            var coord = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c);
            foreach (var p in Active(w)) Assert.Equal(0.8 * g, Politics.Discontent(w, c, p, coord, 0), 12);
        }
        var wn = Board(1, -0.2, 0.0);   // R＜s（成長期）＝Pos で 0
        foreach (var p in Active(wn)) Assert.Equal(0.0, Politics.Discontent(wn, wn.Coef, p, PoliticalQuantities.DriftedPolityCoord(wn.Polity, wn.Suffrage, wn.Coef), 0));
    }

    // ── (1385) c147wb 2 つ目の半分の器：AI の不穏と信任が大衆の層の不満度の平均 D̄ を読む（旗 1）。期待値は試験の中のリテラル（D＝0.8×(R−s)・D̄＝その平均） ──
    private static World BoardSR(double flag, double s, double r)
    {
        var w = Board(flag, 0.0, 0.0);
        foreach (var reg in w.Regions) foreach (var p in reg.Pops) { p.Satisfaction = s; p.LivingRef = r; }
        return w;
    }

    [Fact]
    public void T6_AIの不穏は満足が同じでDが違う2国でD平均に比例する_旗0は今の式()
    {
        var a = BoardSR(1, 0.5, 0.6); var b = BoardSR(1, 0.5, 0.8);   // D̄＝0.08 と 0.24（満足度 0.5 は同じ）
        Assert.Equal(NationalScore.MeanSatisfaction(a), NationalScore.MeanSatisfaction(b), 12);   // 前提：満足度は同じ
        double ua = UtilityAgent.UnrestOf(a, NationalScore.MeanSatisfaction(a)), ub = UtilityAgent.UnrestOf(b, NationalScore.MeanSatisfaction(b));
        Assert.Equal(0.8 * 0.1, ua, 9);
        Assert.Equal(0.8 * 0.3, ub, 9);
        Assert.Equal(3.0, ub / ua, 9);
        // 旗 0＝今の式（0.62−満足度・満足度が同じなら同じ値）
        var a0 = BoardSR(0, 0.5, 0.6); var b0 = BoardSR(0, 0.5, 0.8);
        Assert.Equal(0.62 - NationalScore.MeanSatisfaction(a0), UtilityAgent.UnrestOf(a0, NationalScore.MeanSatisfaction(a0)), 12);
        Assert.Equal(UtilityAgent.UnrestOf(a0, NationalScore.MeanSatisfaction(a0)), UtilityAgent.UnrestOf(b0, NationalScore.MeanSatisfaction(b0)), 12);
    }

    private static double ConfAfterCredit(double flag, double s, double r)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(1);
        var w = sim.World; w.Coef.SatReadersToDiscontent = flag;
        foreach (var reg in w.Regions) foreach (var p in reg.Pops) { p.Satisfaction = s; p.LivingRef = r; }
        w.Bank!.Confidence = 1.0;
        PriceOfBread.SimCore.Credit.Step(w, w.Coef);
        return w.Bank.Confidence;
    }

    [Fact]
    public void T7_信任の目標はD平均に従い_満足だけを変えても動かない_旗0は満足に従う()
    {
        double a = ConfAfterCredit(1, 0.5, 0.6), b = ConfAfterCredit(1, 0.3, 0.4), c = ConfAfterCredit(1, 0.5, 0.8);   // D̄ が a＝b＜c
        Assert.True(a < 1.0, $"前提：信任が動く盤面でない {a}");
        Assert.Equal(a, b, 12);                 // 満足度だけが違い D̄ が同じ＝同じ
        Assert.True(c < a - 1e-6, $"D̄ が大きいのに信任が下がらない {a} {c}");
        // 旗 0＝満足度で動く（対照）：D̄ が同じでも満足度が違えば違う
        double a0 = ConfAfterCredit(0, 0.5, 0.6), b0 = ConfAfterCredit(0, 0.3, 0.4);
        Assert.True(b0 < a0 - 1e-6, $"旗 0 で満足度に感度が無い {a0} {b0}");
    }

    // ── (1398) c149sd 点火の盤面：満足度は低い（0.3）が R−s≤0（R＝s と R＜s）の POP だけの国。旗 1＝全ての POP の D が 0・AI の不穏 0・信任は不穏 0 の値。
    //    旗 0＝AI の不穏は 0.62−0.3＝0.32（手計算のリテラル）・信任は旗 1 より低い。突然変異 μ1（声の項を戻す）・μ2（UnrestOf を旧の式へ）・μ3（Credit の dBar を逆に）で赤。
    [Fact]
    public void T8_満足は低いがRがs以下ならDと不穏が0_旧の式なら不穏が正()
    {
        foreach (double r in new[] { 0.3, 0.2 })
        {
            var w = BoardSR(1, 0.3, r); var c = w.Coef;
            var coord = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c);
            var pops = Active(w);
            Assert.True(pops.Count > 50, $"前提：POP が少ない {pops.Count}");
            foreach (var p in pops) Assert.Equal(0.0, Politics.Discontent(w, c, p, coord, 0));
            Assert.Equal(0.0, Politics.AvgMassDiscontent(w));
            Assert.Equal(0.0, UtilityAgent.UnrestOf(w, NationalScore.MeanSatisfaction(w)));
            // 対照＝旗 0 の同じ盤面：満足度 0.3 の旧の式は不穏 0.32
            var w0 = BoardSR(0, 0.3, r);
            Assert.Equal(0.3, NationalScore.MeanSatisfaction(w0), 9);
            Assert.Equal(0.62 - 0.3, UtilityAgent.UnrestOf(w0, NationalScore.MeanSatisfaction(w0)), 9);
        }
        // 信任：旗 1 は不穏 0（R＝s の盤面と R＜s の盤面で同じ値）・旗 0 は満足度 0.3＜平穏線で不穏が正＝旗 1 より低い
        double c1 = ConfAfterCredit(1, 0.3, 0.3), c1n = ConfAfterCredit(1, 0.3, 0.2), c0 = ConfAfterCredit(0, 0.3, 0.3);
        Assert.Equal(c1, c1n, 12);
        Assert.True(c0 < c1 - 1e-6, $"旧の式の信任が不穏 0 の信任より下がらない {c0} {c1}");
    }

    // ── (1406) c150sd 本番の信任の入口（Simulation.StepDayDaily の月の写し CreditMonth の SatOrD）。T7・T8 は Credit.Step を直に呼ぶ＝本番の入口を通らない。
    //    盤面＝英 41・全 POP の満足度 0.5・参照点 0.8（D＝0.8×0.3＝0.24・手計算のリテラル）。月の初日の旬で写しを取り直させる（箱を外して日次を 1 回）。
    //    旗 1＝SatOrD が D̄ 0.24／旗 0＝満足度 0.5。突然変異 μ6（SatOrD に旧の満足度を入れる）で旗 1 の側が赤。
    private static double CreditMonthSatOrD(double flag)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(1);
        var w = sim.World; w.Coef.SatReadersToDiscontent = flag;
        Assert.Equal(0.8, w.Coef.WantWeight);   // 手計算のリテラルの前提
        Assert.NotNull(w.Bank);
        foreach (var reg in w.Regions) foreach (var p in reg.Pops) { p.Satisfaction = 0.5; p.LivingRef = 0.8; }
        Simulation.CreditMonth.Remove(w);
        sim.StepDayDaily();
        Assert.True(Simulation.CreditMonth.TryGetValue(w, out var box), "前提：日次の後に月の写しが無い");
        return box!.Value.SatOrD;
    }

    [Fact]
    public void T9_本番の信任の入口の月の写しは旗1でD平均_旗0で満足度()
    {
        Assert.Equal(0.8 * 0.3, CreditMonthSatOrD(1), 9);
        Assert.Equal(0.5, CreditMonthSatOrD(0), 9);
    }
}
