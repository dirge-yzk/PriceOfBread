using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1371) c145rv 旗 SatReadersToDiscontent：不満度 D＝Clamp01(WantWeight×Pos(R−s))（声の項を抜く・案 D1）・参加と思想は D だけを読む（闘争度を読まない）。
/// 盤面は試験の中で置く（仏 1789・全 POP の意識と識字 0.9・満足度 0.5）。期待値は手計算のリテラル（WantWeight＝0.8）で本体と同じ式から作らない。
/// 旗 0＝今の式（声の項あり・参加は max(D, 闘争度)）＝T1・T2 が赤になる対照を T1・T2 の中に持つ。
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
}
