using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(431) 線 dq（不満度の二量分離・00-DISC の続き）の番人。機構と不変条件だけ（創発の主張＝狙いの数字は成績表と計器が見る）。事前登録 measurements/dq_20261019_prereg.txt [6]。
/// 2026-10-19 に点火（旗 DiscontentSplit は焼き込んで削除）。新しい既定の主張：
/// ①不満度の本体 DiscontentRaw はクランプしない（負の生活の失望が負のまま）・Discontent はそれを [0,1] に切る。
/// ②Step の内訳：MilLiving は厳密に 0（生活の項は闘争度の目標に入らない）・MilVoice＝DiscontentRaw−生活の項（手計算）・闘争度は生活の失望の符号と大きさに依らない。
/// ③好景気の負の失望が飢餓の怒りを打ち消さない。④思想の更新の入力が max(不満度, 闘争度)（不満度の大きい POP は闘争度をその値に置いた世界と同じシェア・更新の前の理想点で測る・不満度が上がれば闘争度が同じでもシェアが動く）。
/// ⑤計算量の台帳：思想の更新の分が載る。
/// 期待値はテストの側の手計算のリテラル（WantWeight＝0.8・落差 0.25 など）で、本体と同じ式から作らない。
/// </summary>
[Collection("Instruments")]
public class DiscontentSplitTests
{
    /// <summary>仏 1789 の盤面（8 四半期）。全 POP の意識 0（法の項が 0＝生活の項だけが不満度を立てる）・満足度 0.5・参照点＝0.5＋drop・闘争度を既知にする。</summary>
    private static World Board(double drop, double militancy = 0.1)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.Run(8);
        var w = sim.World; var c = w.Coef;
        Assert.True(c.LivingRefWeight >= 1.0, "前提が崩れた：失望（相対）でない＝drop の予言が成り立たない");
        Assert.Equal(0.8, c.WantWeight);   // 手計算のリテラルの前提
        c.ConsciousnessSpeed = 0;
        foreach (var r in w.Regions)
            foreach (var p in r.Pops) { p.Militancy = militancy; p.Consciousness = 0; p.Satisfaction = 0.5; p.LivingRef = 0.5 + drop; }
        return w;
    }

    private static IEnumerable<Pop> ActivePops(World w)
    {
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Pops) if (p.Size > 1e-9) yield return p;
        }
    }

    [Fact]
    public void 不満度の本体は負の生活の失望を切らず_不満度はそれを0へ切る()
    {
        var w = Board(drop: -0.25);   // 参照点 0.25 ＜ 満足度 0.5＝成長期＝生活の失望は −0.25
        var c = w.Coef;
        var coord = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c);
        int n = 0;
        foreach (var p in ActivePops(w))
        {
            n++;
            Assert.Equal(-0.2, Politics.DiscontentRaw(w, c, p, 0.0), 12);   // 手計算＝0.8×(−0.25)（意識 0＝法の項 0）
            Assert.Equal(0.0, Politics.Discontent(w, c, p, coord, 0));       // 不満度は [0,1] に切る
        }
        Assert.True(n > 50, $"比べた POP が少ない {n}");
    }

    [Fact]
    public void Stepの内訳は生活の項が0で法の項が不満度の本体から生活の項を引いた値に等しい()
    {
        // 意識を高くして法の項も立てる（正の落差と負の落差の両方の盤面）。別々に作った2つの同一の盤面の一方を Step して内訳を取り、
        // もう一方で更新前に DiscontentRaw を取る（4 軸の距離÷2 はテストの側の手計算）。MilLiving＝0（厳密）・MilVoice＝DiscontentRaw−0.8×落差（手計算のリテラル）。
        foreach (double drop in new[] { 0.25, -0.25 })
        {
            World Make() { var w = Board(drop); foreach (var p in ActivePops(w)) { p.Consciousness = 0.9; p.Literacy = 0.9; } return w; }
            var wa = Make(); var wb = Make();
            var coord = PoliticalQuantities.DriftedPolityCoord(wa.Polity, wa.Suffrage, wa.Coef);
            var raw = ActivePops(wa).Select(p => Politics.DiscontentRaw(wa, wa.Coef, p, Math.Sqrt(Distance2(p, coord)) / 2.0)).ToList();
            Politics.Step(wb, wb.Coef);
            var pops = ActivePops(wb).ToList();
            Assert.Equal(raw.Count, pops.Count);
            int voiced = 0;
            for (int i = 0; i < pops.Count; i++)
            {
                Assert.Equal(0.0, pops[i].MilLiving);                                  // 生活の項は闘争度の目標に入らない（欄のまま厳密に 0）
                Assert.Equal(raw[i] - 0.8 * drop, pops[i].MilVoice, 12);               // 法の項＝不満度の本体−生活の項（WantWeight×落差の手計算）
                if (pops[i].MilVoice > 0) voiced++;
            }
            Assert.True(pops.Count > 50, $"比べた POP が少ない {pops.Count}");
            Assert.True(voiced > 50, $"法の項が立った POP が少ない {voiced}＝この番人は法の項を見ていない");
        }
    }

    private static double Distance2(Pop p, double[] coord)
    {
        var ideal = Ideologies.IdealPoint(p.IdeologyShares);
        double s2 = 0; for (int k = 0; k < 4; k++) s2 += (ideal[k] - coord[k]) * (ideal[k] - coord[k]);
        return s2;
    }

    [Fact]
    public void 闘争度は生活の失望の符号と大きさに依らない()
    {
        // 闘争度の追従を 1 にして（Militancy＝目標）、落差 +0.25・0・−0.25 の同じ盤面を比べる。意識 0 の盤面なので法・民族・厭戦・飢餓の項は 0 ＝目標は 0 のまま。
        // 生活の項（0.8×落差＝±0.2）が目標に入っていれば闘争度は 0.2／0（切り）に分かれる。
        foreach (double drop in new[] { 0.25, 0.0, -0.25 })
        {
            var w = Board(drop, militancy: 0.0);
            w.Coef.MilitancySpeed = 1.0;
            Politics.Step(w, w.Coef);
            var ps = ActivePops(w).ToList();
            Assert.True(ps.Count > 50);
            Assert.All(ps, p => { Assert.Equal(0.0, p.MilLiving); Assert.Equal(0.0, p.Militancy, 12); });
        }
    }

    [Fact]
    public void 負の生活の失望が飢餓の怒りを打ち消さない()
    {
        // 成長期（落差 −0.25）に飢餓の駆動を立てる（平常 FoodRef＝1・FamineDriveGain×(平常−充足−起点)＝0.5 になる手計算の置き方）。
        // 生活の項が目標に入っていれば 0.5−0.2＝0.3・入っていなければ 0.5（政治・民族・厭戦は 0 の盤面）。
        var w = Board(drop: -0.25, militancy: 0.0);
        double gain = w.Coef.FamineDriveGain, onset = w.Coef.FamineDropOnset;
        Assert.True(gain > 0, "前提：飢餓の駆動が点火している");
        double fill = 1.0 - onset - 0.5 / gain;   // 駆動 0.5 になる充足（平常 1.0 から）
        foreach (var p in ActivePops(w)) { p.FoodNeed = 1.0; p.FoodMet = fill; p.FoodRef = 1.0; }
        w.Coef.MilitancySpeed = 1.0;
        Politics.Step(w, w.Coef);
        var ps = ActivePops(w).ToList();
        Assert.True(ps.Count > 50);
        Assert.All(ps, p => { Assert.Equal(0.0, p.MilLiving); Assert.Equal(0.5, p.MilFamine, 9); Assert.Equal(0.5, p.Militancy, 9); });
    }

    [Fact]
    public void 思想の更新の入力は不満度と闘争度の大きい方()
    {
        // 落差 0.5・意識 0 → 不満度＝0.8×0.5＝0.4（手計算）。闘争度 0.1（不満度が大きい）の盤面のシェアが、闘争度を 0.4 に置いた盤面のシェアとビット一致し
        // （入力が闘争度だけなら 0.1 と 0.4 で別になる）、闘争度が 0.9 の盤面は不満度に引かれず 0.9 のまま（同じ盤面とビット一致）。
        foreach (var (milA, milB) in new[] { (0.1, 0.4), (0.9, 0.9) })
        {
            var wA = Board(drop: 0.5, militancy: milA);
            var wB = Board(drop: 0.5, militancy: milB);
            Ideologies.UpdateShares(wA);
            Ideologies.UpdateShares(wB);
            var a = wA.AllPops().ToList(); var b = wB.AllPops().ToList();
            Assert.Equal(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
                Assert.True(a[i].IdeologyShares.SequenceEqual(b[i].IdeologyShares), $"POP {i} のシェアが違う（闘争度 {milA}→{milB}）");
        }
        // 闘争度が 0.1 と 0.3（どちらも不満度 0.4 より小さい）の盤面は、入力が同じ 0.4 になるのでシェアが一致する。
        var w1 = Board(drop: 0.5, militancy: 0.1); var w3 = Board(drop: 0.5, militancy: 0.3);
        Ideologies.UpdateShares(w1); Ideologies.UpdateShares(w3);
        var p1 = w1.AllPops().ToList(); var p3 = w3.AllPops().ToList();
        for (int i = 0; i < p1.Count; i++) Assert.True(p1[i].IdeologyShares.SequenceEqual(p3[i].IdeologyShares), $"POP {i}：不満度 0.4 が両方の入力のはず");
        // 計算量の台帳：思想の更新で不満度を問うた POP の数が載る（盤面を作る Run が載せた分からの差分で見る）。
        long b0 = w1.Cost[CostLedger.ParticipationPops];
        Ideologies.UpdateShares(w1);
        Assert.Equal(b0 + w1.AllPops().Count(), w1.Cost[CostLedger.ParticipationPops]);
    }

    [Fact]
    public void 思想の入力の不満度は更新の前のシェアの理想点で測る_参加の述語と同じ呼びの値()
    {
        // 意識・識字を 0.9 にして法の項（理想点に依る）を立てる。世界 X で更新の前に参加の述語と同じ呼び（DiscontentOf）で POP ごとの不満度 d を取り、
        // 同じ盤面 R の闘争度を max(d, 闘争度) に置く。更新後のシェアがビット一致なら、思想の入力は更新の前の理想点で測っている
        // （BaseWeights が r＝IdeologyShares を 0 クリアして上書きした後で読むと理想点が途中の値になり、一致しない）。
        World Make()
        {
            var w = Board(drop: 0.5, militancy: 0.1);
            foreach (var r in w.Regions) foreach (var p in r.Pops) { p.Consciousness = 0.9; p.Literacy = 0.9; }
            return w;
        }
        var wX = Make();
        var wR = Make();
        var (coord, gap) = FactionSnapshot.MembershipContext(wX, true);
        var px = wX.AllPops().ToList(); var pr = wR.AllPops().ToList();
        Assert.Equal(px.Count, pr.Count);
        int lawVoiced = 0;
        for (int i = 0; i < px.Count; i++)
        {
            double d = FactionSnapshot.DiscontentOf(wX, px[i], coord, gap);
            if (d > 0.4 + 1e-9) lawVoiced++;   // 生活の項 0.8×0.5＝0.4 を超える＝法の項が立っている
            pr[i].Militancy = Math.Max(d, px[i].Militancy);
        }
        Assert.True(lawVoiced > 50, $"法の項が立った POP が少ない {lawVoiced}＝この番人は理想点を見ていない");
        Ideologies.UpdateShares(wX); Ideologies.UpdateShares(wR);
        for (int i = 0; i < px.Count; i++)
            Assert.True(px[i].IdeologyShares.SequenceEqual(pr[i].IdeologyShares), $"POP {i} のシェアが違う");
    }

    [Fact]
    public void 不満度が上がると闘争度が同じでも思想のシェアが動く()
    {
        // 闘争度は同じ 0.1。落差 0.5（不満度 0.4）と落差 0（不満度 0）でシェアが違う＝入力が闘争度だけではない（入力が闘争度だけなら一致して緑になってしまう自明さを避ける）。
        var wHi = Board(drop: 0.5, militancy: 0.1);
        var wLo = Board(drop: 0.0, militancy: 0.1);
        Ideologies.UpdateShares(wHi); Ideologies.UpdateShares(wLo);
        var a = wHi.AllPops().ToList(); var b = wLo.AllPops().ToList();
        int diff = 0;
        for (int i = 0; i < a.Count; i++) if (!a[i].IdeologyShares.SequenceEqual(b[i].IdeologyShares)) diff++;
        Assert.True(diff > 50, $"シェアが変わった POP が少ない {diff}");
    }
}
