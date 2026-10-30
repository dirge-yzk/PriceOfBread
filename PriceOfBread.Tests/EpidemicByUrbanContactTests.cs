using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(638) 線 urc：疫病を都市の人の数で起こす・選ぶ・殺すの番人。期待値は州の POP を直に足す手書きの走査から作る（World の読み口を期待値の出所にしない）。
/// 事前登録 measurements/urc_638_20261028_prereg.txt [1][6][12]。発生の確率＝基礎×州ごとの危険度の和／伝播の定着＝国の都市の割合／標的＝都市の人口の重みで抽選／死＝都市の POP だけ。</summary>
public class EpidemicByUrbanContactTests
{
    /// <summary>州の (都市の人口, 人口)。</summary>
    private static (double urban, double pop) Sums(Region r)
    {
        double u = 0, p = 0;
        foreach (var pop in r.Pops) { p += pop.Size; if (pop.IsUrban) u += pop.Size; }
        return (u, p);
    }

    /// <summary>都市の POP と農村の POP を両方持つ州を 3 つ選び、都市の人口の和を <paramref name="urb"/>（3 本）に揃える（農村はそのまま）。他の州の POP は全て 0。選んだ 3 州を返す。</summary>
    private static Region[] Board3(World w, double[] urb)
    {
        var picks = w.Regions.Where(r => r.Pops.Any(p => p.IsUrban && p.Size > 0) && r.Pops.Any(p => !p.IsUrban && p.Size > 0)).Take(3).ToArray();
        Assert.Equal(3, picks.Length);
        foreach (var r in w.Regions) if (!picks.Contains(r)) foreach (var p in r.Pops) p.Size = 0;
        for (int i = 0; i < 3; i++)
        {
            double cur = Sums(picks[i]).urban;
            foreach (var p in picks[i].Pops) if (p.IsUrban) p.Size *= urb[i] / cur;
        }
        return picks;
    }

    private static World FranceWorld(ulong seed = 155) => new Simulation(NationConfig.France().ToScenario(seed), new NullAgent()).World;

    /// <summary>州 0 は小さく高い割合（約 9 割）・州 1・2 は大きく低い割合の盤面。</summary>
    private static Region[] SkewedBoard3(World w)
    {
        var p = Board3(w, new[] { 10.0, 30.0, 60.0 });
        foreach (var pp in p[0].Pops) if (!pp.IsUrban) pp.Size = 0.3;
        foreach (var pp in p[1].Pops.Concat(p[2].Pops)) if (!pp.IsUrban) pp.Size = Math.Max(pp.Size, 1.0) * 500;
        return p;
    }

    [Fact]
    public void 抽選は都市の人口の逆累積_境界の手計算と一致し重みが0の州は選ばれない()
    {
        var w = FranceWorld();
        var p = Board3(w, new[] { 10.0, 30.0, 60.0 });   // 累積 10・40・100（和 100）
        double sum = p.Sum(r => Sums(r).urban);
        Assert.Equal(100.0, sum, 9);
        Assert.Same(p[0], w.DrawByUrbanPopulation(sum, null, 0.0));
        Assert.Same(p[0], w.DrawByUrbanPopulation(sum, null, 0.0999));
        Assert.Same(p[1], w.DrawByUrbanPopulation(sum, null, 0.1001));
        Assert.Same(p[1], w.DrawByUrbanPopulation(sum, null, 0.3999));
        Assert.Same(p[2], w.DrawByUrbanPopulation(sum, null, 0.4001));
        Assert.Same(p[2], w.DrawByUrbanPopulation(sum, null, 0.9999));
        // 1 つ目（p[0]）を除く＝重み 30・60（和 90）：累積 30・90
        Assert.Same(p[1], w.DrawByUrbanPopulation(90.0, p[0], 0.0999));   // 8.99 < 30
        Assert.Same(p[1], w.DrawByUrbanPopulation(90.0, p[0], 0.3299));   // 29.69 < 30
        Assert.Same(p[2], w.DrawByUrbanPopulation(90.0, p[0], 0.3401));   // 30.6 ≥ 30
        Assert.Same(p[2], w.DrawByUrbanPopulation(90.0, p[0], 0.9999));
        // 4 つ目の州：農村の人口だけあって都市の人口が 0 ＝重み 0（どの u でも選ばれない）
        var rural = w.Regions.First(r => !p.Contains(r) && r.Pops.Count > 0);
        foreach (var pp in rural.Pops) pp.Size = pp.IsUrban ? 0 : 50;
        for (double u = 0; u < 1.0; u += 0.01)
            Assert.NotSame(rural, w.DrawByUrbanPopulation(sum, null, u));
    }

    [Fact]
    public void 発生の危険度は州ごとの和_衛生の割引がなければ国の都市の割合で最大の割合を超えない()
    {
        var w = FranceWorld();
        var p = SkewedBoard3(w);
        var c = w.Coef; c.EpidemicOnsetSanitationWeight = 0;
        double u = 0, tot = 0, max = 0;
        foreach (var r in p) { var (ur, po) = Sums(r); u += ur; tot += po; max = Math.Max(max, ur / po); }
        Assert.True(max > 0.8 && u / tot < 0.5 * max, $"前提：1 州の割合と国の割合が離れた盤面（最大 {max}・国 {u / tot}・州 {string.Join("/", p.Select(r => Sums(r)))}）");
        Assert.Equal(u / tot, w.EpidemicOnsetRisk(c), 12);
        Assert.True(w.EpidemicOnsetRisk(c) < max);
        // 衛生の割引：州ごとに違う充足（InfraFill）を読む＝最悪の州の衛生を全州に掛けると赤
        c.EpidemicOnsetSanitationWeight = 0.4;
        p[0].InfraFill = 0.9; p[1].InfraFill = 0.5; p[2].InfraFill = 0.1;
        double hand = 0;
        foreach (var r in p) { var (ur, po) = Sums(r); hand += (ur / po) * (po / tot) * (1.0 - 0.4 * Math.Clamp(r.InfraFill, 0, 1)); }
        Assert.Equal(hand, w.EpidemicOnsetRisk(c), 12);
    }

    [Fact]
    public void 発生の確率は基礎かける州ごとの危険度の和_最大の割合に最悪州の衛生を掛けた式とは別()
    {
        var w = FranceWorld();
        var p = SkewedBoard3(w);
        var c = w.Coef; c.EpidemicOnsetSanitationWeight = 0.8; c.EpidemicChance = 0.2;
        p[0].InfraFill = 0.9; p[1].InfraFill = 0.1; p[2].InfraFill = 0.1;            // 最も過密な州（州 0）だけ衛生が整っている
        double tot = 0; foreach (var r in p) tot += Sums(r).pop;
        double max = 0, handRisk = 0; int worst = 0;
        for (int i = 0; i < 3; i++)
        {
            var (ur, po) = Sums(p[i]); double us = ur / po;
            if (us > max) { max = us; worst = i; }
            handRisk += us * (po / tot) * (1.0 - 0.8 * Math.Clamp(p[i].InfraFill, 0, 1));
        }
        Assert.Equal(0.2 * handRisk, w.EpidemicOnsetProbability(c), 12);
        double handMaxShare = 0.2 * max * (1.0 - 0.8 * Math.Clamp(p[worst].InfraFill, 0, 1));   // 旧の式（最大の割合 × 最悪州の衛生の割引）を手で書く
        Assert.True(w.EpidemicOnsetProbability(c) < 0.5 * handMaxShare, "前提：州ごとの和は旧の式から離れた盤面（旧の式を読むと赤）");
    }

    [Fact]
    public void 一つの州だけに人口がある国の危険度は最大の割合の式と一致し_都市の州が一つで残りの州が都市0なら一致しない()
    {
        var w = FranceWorld();
        var keep = w.Regions.First(r => r.Pops.Any(p => p.IsUrban && p.Size > 0));
        foreach (var r in w.Regions) if (!ReferenceEquals(r, keep)) foreach (var p in r.Pops) p.Size = 0;
        var c = w.Coef; c.EpidemicOnsetSanitationWeight = 0.4;
        var (ur, po) = Sums(keep);
        double old = (ur / po) * (1.0 - 0.4 * keep.Sanitation(c));
        Assert.True(old > 0);
        Assert.Equal(old, w.EpidemicOnsetRisk(c), 12);   // 相対 1e-12（掛け算の順序の最下位の桁は許す）
        // 別の州に都市 0 の農村だけの人口を置く：p_r < 1 になり、最大の割合の式とは一致しない
        var other = w.Regions.First(r => !ReferenceEquals(r, keep));
        foreach (var p in other.Pops) p.Size = p.IsUrban ? 0 : po;
        Assert.True(w.EpidemicOnsetRisk(c) < 0.75 * old, $"{w.EpidemicOnsetRisk(c)} vs {old}");
    }

    /// <summary>TriggerEpidemic が引いた乱数の個数（同じ種の参照の乱数を進めて次の値を比べる）。</summary>
    private static int Draws(World w, Coefficients c)
    {
        var rng = new DeterministicRng(777); var refr = new DeterministicRng(777);
        w.TriggerEpidemic(c, 1.0, false, rng);
        double next = rng.NextDouble();
        for (int k = 0; k < 5; k++) { if (refr.NextDouble() == next) return k; }
        return -1;
    }

    [Fact]
    public void 乱数を引く個数_重みが正の州が1つなら0_2つなら1_3つなら二次ありで2_二次なしで1()
    {
        var w3 = FranceWorld(); Board3(w3, new[] { 10.0, 30.0, 60.0 }); w3.Coef.EpidemicIntraCountrySpread = 0.5;
        Assert.Equal(2, Draws(w3, w3.Coef));
        var w3b = FranceWorld(); Board3(w3b, new[] { 10.0, 30.0, 60.0 }); w3b.Coef.EpidemicIntraCountrySpread = 0;
        Assert.Equal(1, Draws(w3b, w3b.Coef));
        // 重みが正の州が 2 つ：1 つ目だけ引く（二次は残り 1 つで決まる）
        var w2 = FranceWorld(); var p2 = Board3(w2, new[] { 10.0, 30.0, 60.0 }); foreach (var pp in p2[2].Pops) if (pp.IsUrban) pp.Size = 0;
        w2.Coef.EpidemicIntraCountrySpread = 0.5;
        Assert.Equal(1, Draws(w2, w2.Coef));
        // 重みが正の州が 1 つ：引かない
        var w1 = FranceWorld(); var p1 = Board3(w1, new[] { 10.0, 30.0, 60.0 }); foreach (var pp in p1[1].Pops.Concat(p1[2].Pops)) if (pp.IsUrban) pp.Size = 0;
        w1.Coef.EpidemicIntraCountrySpread = 0.5;
        Assert.Equal(0, Draws(w1, w1.Coef));
    }

    [Fact]
    public void 一つ目の標的は都市の人口に比例し二次は一つ目と別の州()
    {
        var w = FranceWorld();
        var p = Board3(w, new[] { 10.0, 30.0, 60.0 });
        var c = w.Coef; c.EpidemicIntraCountrySpread = 0.5; c.EpidemicSeverity = new Share(1e-7);
        var first = new int[3];
        const int N = 400;
        for (int t = 0; t < N; t++)
        {
            var before = p.Select(r => Sums(r).urban).ToArray();
            w.TriggerEpidemic(c, 1.0, false, new DeterministicRng((ulong)(1000 + t)));
            var hit = Enumerable.Range(0, 3).Where(i => Sums(p[i]).urban < before[i]).ToArray();
            Assert.Equal(2, hit.Length);   // 一つ目と二次の 2 州（同じ州に 2 度当たらない）
            // 一つ目＝二次より都市の死亡率が高い（二次は ×EpidemicIntraCountrySpread）
            int a = hit[0], b = hit[1];
            double da = 1.0 - Sums(p[a]).urban / before[a], db = 1.0 - Sums(p[b]).urban / before[b];
            first[da > db ? a : b]++;
        }
        // 都市の人口 10:30:60 に比例（最大の重みの州が最も多く、最小の重みの州が最も少ない）
        Assert.True(first[2] > first[1] && first[1] > first[0], $"{first[0]}/{first[1]}/{first[2]}");
        Assert.True(first[0] < 0.2 * N && first[2] > 0.45 * N, $"{first[0]}/{first[1]}/{first[2]}");
    }

    [Fact]
    public void 死は都市のPOPだけ_農村は動かず総死者は都市の人口と死亡率の積で都市のPOPがない州は死者0()
    {
        var w = FranceWorld();
        var r = w.Regions.First(x => x.Pops.Any(p => p.IsUrban && p.Size > 0) && x.Pops.Any(p => !p.IsUrban && p.Size > 0));
        var c = w.Coef; c.EpidemicSeverity = new Share(1e-6);   // 規模の下限に当たらない小さな率・衛生は実衛生財だけ
        var ruralBefore = r.Pops.Where(p => !p.IsUrban).Select(p => p.Size).ToArray();
        var (ub, _) = Sums(r);
        double deathFrac = 1e-6 * (1.0 - 0.7 * Math.Clamp(r.Sanitation(c), 0, 1)) * 1.0 * 1.0;   // 致死率＝基礎×(1−0.7×衛生)×揺らぎ×scale を手で書く
        double dead = w.ApplyEpidemicDeaths(r, c, 1.0, 1.0);
        Assert.True(dead > 0);
        Assert.Equal(ub * deathFrac, dead, 9);   // 総死者＝都市の POP の和 × 致死率
        Assert.Equal(ruralBefore, r.Pops.Where(p => !p.IsUrban).Select(p => p.Size).ToArray());   // 農村は 1 人も動かない
        // 都市の POP が 0 の州は死者 0
        var wc = FranceWorld();
        var rc = wc.Regions.First(x => x.Pops.Any(p => !p.IsUrban && p.Size > 0));
        foreach (var p in rc.Pops) if (p.IsUrban) p.Size = 0;
        Assert.Equal(0.0, wc.ApplyEpidemicDeaths(rc, wc.Coef, 1.0, 1.0));
    }

    [Fact]
    public void 発生の配線_州ごとの危険度の和を読む_1州が約10割都市でも国の都市の人が少なければ発生しない()
    {
        // 盤面：1 州が小さく 97% 都市・他は大きく農村＝最大の割合を読むなら確率は 1 を超える（EpidemicChance 2）。州ごとの危険度の和は桁違いに小さい。
        var sim = new Simulation(NationConfig.Britain().ToScenario(9), new NullAgent());
        var w = sim.World;
        var pick = w.Regions.First(r => r.Pops.Any(p => p.IsUrban && p.Size > 0));
        var keep = pick.Pops.First(p => p.IsUrban && p.Size > 0);
        var sink = w.Regions.Where(r => !ReferenceEquals(r, pick)).SelectMany(r => r.Pops).First(p => !p.IsUrban && p.Size > 0);
        double moved = 0;
        foreach (var r in w.Regions)
            foreach (var pop in r.Pops)
            {
                if (ReferenceEquals(pop, keep) || ReferenceEquals(pop, sink)) continue;
                if (!pop.IsUrban && !ReferenceEquals(r, pick)) continue;
                double leave = ReferenceEquals(r, pick) && !pop.IsUrban ? 1e-9 : 1e-12;
                moved += pop.Size - leave; pop.Size = leave;
            }
        moved += keep.Size - 1e-6; keep.Size = 1e-6;
        sink.Size += moved;
        w.Coef.EpidemicChance = 2; w.Coef.EpidemicOnsetSanitationWeight = 0;
        Assert.Equal(0, w.EpidemicActiveYears);
        for (int d = 0; d < Calendar.DaysPerMonth; d++) sim.StepDay();
        Assert.Equal(0, w.EpidemicActiveYears);   // 最大の割合（約 1）× 2 を読む実装なら発生して赤
    }

    [Fact]
    public void 発生の標的は国の乱数で散る_Simulationの経路()
    {
        // 発生は必ず起きる大きさ・二次なし・死は大きく（当たった州の人口の減りが月次の動きより桁違いに大きい）。種を替えると当たる州が替わる（乱数を渡し忘れると 0.5 の州に固定されて赤）。
        var hit = new HashSet<int>();
        for (ulong seed = 21; seed <= 32; seed++)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(seed), new NullAgent());
            var w = sim.World;
            double total0 = w.Regions.Sum(r => Sums(r).pop);
            var p = Board3(w, new[] { 10.0, 30.0, 60.0 });
            p[2].Pops.First(q => !q.IsUrban).Size += total0 - w.Regions.Sum(r => Sums(r).pop);   // 総人口を保つ（人口保存の不変条件）
            var c = w.Coef; c.EpidemicIntraCountrySpread = 0; c.EpidemicChance = 1e6; c.EpidemicSeverity = new Share(0.9);   // 衛生は実衛生財だけ（(667) 工業化度の代理を混ぜない）＝死は大きく取る
            var before = p.Select(r => Sums(r).urban).ToArray();
            for (int d = 0; d < Calendar.DaysPerMonth; d++) sim.StepDay();
            Assert.True(w.EpidemicActiveYears > 0, "前提：発生した");
            int best = 0; double bestDrop = 0;
            for (int i = 0; i < 3; i++) { double drop = 1.0 - Sums(p[i]).urban / before[i]; if (drop > bestDrop) { bestDrop = drop; best = i; } }
            Assert.True(bestDrop > 0.1, $"前提：当たった州は 1 割以上減る（{bestDrop}）");
            hit.Add(best);
        }
        Assert.True(hit.Count >= 2, $"当たった州の種類 {hit.Count}");
    }

    /// <summary>1 州だけが小さく高い割合・他は大きく低い割合の盤面を作る（州の割合の最大と国の割合が大きく離れる）。</summary>
    private static void SkewBoard(World w, double smallScale, double otherUrbanScale)
    {
        var pick = w.Regions.First(r => r.Pops.Any(p => p.IsUrban && p.Size > 0));
        foreach (var pop in pick.Pops) pop.Size *= smallScale;
        foreach (var r in w.Regions)
        {
            if (ReferenceEquals(r, pick)) continue;
            foreach (var pop in r.Pops) if (pop.IsUrban) pop.Size *= otherUrbanScale;
        }
    }

    [Fact]
    public void 伝播の定着は国の都市の割合を読む_最大の割合を読む実装なら伝播して赤()
    {
        var step = typeof(MultiNationSim).GetMethod("StepEpidemicTransmission", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(step);
        var sim = new MultiNationSim(MultiNationShared.FourNations(), seed: 1789);
        var recv = sim.Nations[0].World;
        SkewBoard(recv, 1e-9, 1e-12);
        double max = 0, urbanSum = 0, popSum = 0;
        foreach (var r in recv.Regions)
        {
            var (u, p) = Sums(r); urbanSum += u; popSum += p;
            if (p >= 1e-9 && u / p > max) max = u / p;
        }
        Assert.True(max > 0.01);
        Assert.True(urbanSum / popSum < 1e-6 * max, $"前提：国の割合が 1 州の割合よりはるかに小さい（{urbanSum / popSum} / {max}）");
        Assert.Equal(urbanSum / popSum, recv.UrbanShare, 12);
        sim.Coef.EpidemicTransmitChance = 1e6;   // 国の割合を読む確率は 1e6×曝露(≤1)×国の割合(<1e-6×max)＜1、最大の割合を読む確率は曝露が 1e-6 を超えれば 1 超
        for (int i = 0; i < sim.Nations.Count; i++) sim.Nations[i].World.EpidemicActiveYears = i == 0 ? 0 : 3;   // 他国を全て感染源に・受け手を 0 番に固定
        step!.Invoke(sim, null);
        Assert.Equal(0, recv.EpidemicActiveYears);
    }

    [Fact]
    public void 伝播の配線_受け手は自国の乱数で標的を引く()
    {
        var step = typeof(MultiNationSim).GetMethod("StepEpidemicTransmission", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(step);
        // 受け手の盤面は種ごとに作り直す。標的が乱数で散る（pickRng を渡さないと u=0.5 の州に固定される）。
        var hit = new HashSet<int>();
        for (ulong seed = 1; seed <= 8; seed++)
        {
            var sim = new MultiNationSim(MultiNationShared.FourNations(), seed: 1789 + seed);
            int ri = Enumerable.Range(0, sim.Nations.Count).First(i => sim.Nations[i].World.Regions.Count(r => r.Pops.Any(p => p.IsUrban && p.Size > 0) && r.Pops.Any(p => !p.IsUrban && p.Size > 0)) >= 3);
            var recv = sim.Nations[ri].World;
            var p = Board3(recv, new[] { 10.0, 30.0, 60.0 });
            sim.Coef.EpidemicTransmitChance = 1e6; sim.Coef.EpidemicIntraCountrySpread = 0;   // 二次なし＝1 件で当たる州は 1 つ
            foreach (var n in sim.Nations) n.World.Coef.EpidemicIntraCountrySpread = 0;
            for (int i = 0; i < sim.Nations.Count; i++) sim.Nations[i].World.EpidemicActiveYears = i == ri ? 0 : 3;
            var before = p.Select(r => Sums(r).pop).ToArray();
            step!.Invoke(sim, null);
            Assert.True(recv.EpidemicActiveYears > 0, "前提：受け手に伝播する");
            for (int i = 0; i < 3; i++) if (Sums(p[i]).pop < before[i]) hit.Add(i);
        }
        Assert.True(hit.Count >= 2, "標的が乱数で散る（受け手の乱数を標的の抽選へ渡し忘れると 1 州に固定されて赤）");
    }
}
