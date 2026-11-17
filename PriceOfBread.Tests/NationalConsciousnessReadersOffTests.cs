using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1186) c118nc 国民意識の廃止の 1 段目：読み手の重み 3 本（SecessionConsciousnessWeight・NationBuildMobilizationWeight・RevolutionConsciousness）を既定 0 にした。
/// 作者 2026-11-16「国民意識は…移民キャパシティとして設計されていますが、不要でも移民問題が経済的な利害から出るのであれば、廃止しても良いです」。
/// 盤面は試験の中で置く。既定の係数（new Coefficients()）で国民意識の 0 と 1 が同じ結果になることを見る。
/// 対照として旧の値を明示した盤面で結果が違うことを同じ試験で確かめる（盤面がその量を感じることの確かめ＝何もしない盤面で緑にしない）。
/// 突然変異：既定を旧の値（1.0・0.5・0.25）へ 1 本ずつ戻す → その試験が赤（measurements/m1186_20261117/mut_*.txt）。</summary>
public class NationalConsciousnessReadersOffTests
{
    // T1：分離の圧は州の国民意識を読まない（Politics.CheckSecession・州の支配度の動き）。
    // 盤面は SecessionLaborLawInstrumentTests の〈直に CheckSecession を踏む〉と同じ作法（英・大衆のいる非首都の州を全て離れる向き）。
    private static double[] SecessionDominance(double nc, double? weight)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World; var c = w.Coef;
        if (weight is double wt) c.SecessionConsciousnessWeight = wt;
        w.Tariff = 1.0; w.Legitimacy = 0.02;
        var targets = w.Regions.Where(x => x.Id != 0 && Politics.RegionCrowd(x) > 1).ToList();
        foreach (var q in w.Regions) foreach (var p in q.Pops) p.Militancy = 0;
        foreach (var r in targets)
        {
            r.Dominance = 0.5;
            foreach (var p in r.Pops) { p.Militancy = 1.0; p.CoreEthnicShare = new Share(0.0); p.NationalConsciousness = nc; }
        }
        Politics.CheckSecession(w, c, 0.0);
        return targets.Select(r => r.Dominance).ToArray();
    }

    [Fact]
    public void T1_分離の圧は既定で州の国民意識を読まない()
    {
        var lo = SecessionDominance(0.0, null);
        var hi = SecessionDominance(1.0, null);
        Assert.True(lo.Length >= 2, $"前提が崩れた：大衆のいる非首都の州が {lo.Length}");
        Assert.Equal(lo, hi);
        var loOld = SecessionDominance(0.0, 1.0);
        var hiOld = SecessionDominance(1.0, 1.0);
        int moved = loOld.Zip(hiOld).Count(t => System.Math.Abs(t.First - t.Second) > 1e-9);
        Assert.True(moved >= 1, "対照が崩れた：旧の重み 1.0 でも全ての州の支配度が同じ＝盤面が分離の圧を感じない");
    }

    // T2：戦時の徴兵の深さは国民意識を読まない（Military.Step・兵の数）。
    private static double WartimeSoldiers(double nc, double? weight)
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], seed: 1789);
        var w = sim.Nations[0].World;
        if (weight is double wt) w.Coef.NationBuildMobilizationWeight = wt;
        w.ConscriptionLaw = ConscriptionLaw.Selective;
        w.Mobilized = true;
        foreach (var p in w.AllPops()) p.NationalConsciousness = nc;
        w.NationalConsciousness = nc;
        Military.Step(w, w.Coef, atWar: true);
        return w.ComputeSoldierSizeK();
    }

    [Fact]
    public void T2_戦時の徴兵は既定で国民意識を読まない()
    {
        double lo = WartimeSoldiers(0.0, null), hi = WartimeSoldiers(1.0, null);
        Assert.True(lo > 0, $"前提が崩れた：兵の数 {lo}");
        Assert.Equal(lo, hi);
        double loOld = WartimeSoldiers(0.0, 0.5), hiOld = WartimeSoldiers(1.0, 0.5);
        Assert.True(hiOld > loOld + 1e-9,
            $"対照が崩れた：旧の重み 0.5 で国民意識 1 の兵 {hiOld} が 0 の兵 {loOld} を超えない＝盤面が徴兵の門を感じない");
    }

    // T3：暴力の政体の変化は国民意識を上げない（Politics.RevolutionAwakensNation・POP の国民意識）。
    private static double[] AfterAwaken(double? rc)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(41), new NullAgent());
        var w = sim.World;
        if (rc is double v) w.Coef.RevolutionConsciousness = v;
        foreach (var p in w.AllPops()) p.NationalConsciousness = 0.3;
        var m = typeof(Politics).GetMethod("RevolutionAwakensNation", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(m);
        m!.Invoke(null, [w, w.Coef]);
        return w.AllPops().Select(p => p.NationalConsciousness).ToArray();
    }

    [Fact]
    public void T3_革命は既定で国民意識を上げない()
    {
        var now = AfterAwaken(null);
        Assert.True(now.Length >= 2, $"前提が崩れた：POP が {now.Length}");
        Assert.All(now, x => Assert.Equal(0.3, x));
        var old = AfterAwaken(0.25);
        Assert.True(old.Count(x => x > 0.3 + 1e-9) == old.Length,
            "対照が崩れた：旧の値 0.25 で上がらない POP がある＝盤面が書き手を通らない");
    }
}
