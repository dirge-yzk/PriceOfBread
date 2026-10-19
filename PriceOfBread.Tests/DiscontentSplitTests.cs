using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(431) 線 dq（不満度の二量分離・00-DISC の続き）の番人。機構と不変条件だけ（創発の主張＝狙いの数字は成績表と計器が見る）。事前登録 measurements/dq_20261019_prereg.txt [6]。
/// ①既定（DiscontentSplit＝0）：不満度の本体 DiscontentRaw はクランプしない（負の生活の失望が負のまま）・Discontent はそれを [0,1] に切る・Step の MilLiving＋MilVoice＝DiscontentRaw（厳密等号）。
/// ②旗 1：闘争度の目標から生活の項が消える（MilLiving が厳密 0・目標の差は生活の項の分だけ）・民族/厭戦/飢餓の項は旗に依らず同じ。
/// ③旗 1：思想の更新の入力が max(不満度, 闘争度)（不満度が大きい POP は闘争度をその値に置いた世界と同じシェア・闘争度が大きい POP は旗 0 と同じ）。旗 0 は不満度を1度も呼ばない。
/// ④計算量の台帳：思想の更新の分が旗 1 のときだけ載る。
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
    public void 既定ではStepの内訳の生活と法の項の和が不満度の本体に厳密に等しい()
    {
        // 意識を高くして法の項も立てる（正の落差と負の落差の両方の盤面）。別々に作った2つの同一の盤面の一方を Step して内訳の和を取り、
        // もう一方で更新前に DiscontentRaw を取る（4 軸の距離÷2 はテストの側の手計算）。MilLiving＋MilVoice＝DiscontentRaw（同じ左結合の加算＝ビット一致）。
        foreach (double drop in new[] { 0.25, -0.25 })
        {
            World Make() { var w = Board(drop); foreach (var p in ActivePops(w)) { p.Consciousness = 0.9; p.Literacy = 0.9; } return w; }
            var wa = Make(); var wb = Make();
            Assert.Equal(0.0, wa.Coef.DiscontentSplit);
            var coord = PoliticalQuantities.DriftedPolityCoord(wa.Polity, wa.Suffrage, wa.Coef);
            var raw = ActivePops(wa).Select(p => Politics.DiscontentRaw(wa, wa.Coef, p, Math.Sqrt(Distance2(p, coord)) / 2.0)).ToList();
            Politics.Step(wb, wb.Coef);
            var pops = ActivePops(wb).ToList();
            Assert.Equal(raw.Count, pops.Count);
            int voiced = 0;
            for (int i = 0; i < pops.Count; i++)
            {
                Assert.Equal(raw[i], pops[i].MilLiving + pops[i].MilVoice);        // 厳密等号
                Assert.Equal(0.8 * drop, pops[i].MilLiving, 12);                    // 生活の項の手計算（WantWeight×落差）
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
    public void 旗1では闘争度の目標から生活の項だけが消える()
    {
        // 闘争度の追従を 1 にして（Militancy＝目標）、旗 0 と旗 1 の同じ盤面を比べる。落差 +0.25 → 生活の項は 0.8×0.25＝0.2（手計算）。
        double[] mil0 = [], mil1 = [];
        double[] famine0 = [], famine1 = [], nation0 = [], nation1 = [];
        foreach (int flag in new[] { 0, 1 })
        {
            var w = Board(drop: 0.25, militancy: 0.0);
            w.Coef.MilitancySpeed = 1.0;
            w.Coef.DiscontentSplit = flag;
            Politics.Step(w, w.Coef);
            var ps = ActivePops(w).ToList();
            if (flag == 0) { Assert.All(ps, p => Assert.Equal(0.2, p.MilLiving, 12)); mil0 = ps.Select(p => p.Militancy).ToArray(); famine0 = ps.Select(p => p.MilFamine).ToArray(); nation0 = ps.Select(p => p.MilNation).ToArray(); }
            else { Assert.All(ps, p => Assert.Equal(0.0, p.MilLiving)); mil1 = ps.Select(p => p.Militancy).ToArray(); famine1 = ps.Select(p => p.MilFamine).ToArray(); nation1 = ps.Select(p => p.MilNation).ToArray(); }
        }
        Assert.Equal(mil0.Length, mil1.Length);
        int checkedPops = 0;
        for (int i = 0; i < mil0.Length; i++)
        {
            Assert.Equal(famine0[i], famine1[i]); Assert.Equal(nation0[i], nation1[i]);   // 飢餓・民族の項は旗に依らない
            if (mil0[i] < 0.999 && mil0[i] > 0.0)   // 上の切りに掛からない POP＝目標の差は生活の項の分（0.2）
            {
                Assert.Equal(0.2, mil0[i] - mil1[i], 9);
                checkedPops++;
            }
        }
        Assert.True(checkedPops > 50, $"目標の差を見た POP が少ない {checkedPops}");
    }

    [Fact]
    public void 旗1では負の生活の失望が民族と厭戦と飢餓を打ち消さない()
    {
        // 成長期（落差 −0.25）に飢餓の駆動を立てる（平常 FoodRef＝1・今の充足 0.9133…・FamineDriveGain×(平常−充足−起点)＝0.5 になる手計算の置き方）。
        // 旗 0 は目標の頭が −0.2 なので 飢餓 0.5 から引かれて 0.3・旗 1 は生活の項が無いので 0.5（差はちょうど 0.2＝生活の項の分）。
        double gain = 0, onset = 0;
        double[] m0 = [], m1 = [];
        foreach (int flag in new[] { 0, 1 })
        {
            var w = Board(drop: -0.25, militancy: 0.0);
            gain = w.Coef.FamineDriveGain; onset = w.Coef.FamineDropOnset;
            Assert.True(gain > 0, "前提：飢餓の駆動が点火している");
            double fill = 1.0 - onset - 0.5 / gain;   // 駆動 0.5 になる充足（平常 1.0 から）
            foreach (var p in ActivePops(w)) { p.FoodNeed = 1.0; p.FoodMet = fill; p.FoodRef = 1.0; }
            w.Coef.MilitancySpeed = 1.0;
            w.Coef.DiscontentSplit = flag;
            Politics.Step(w, w.Coef);
            var ps = ActivePops(w).ToList();
            Assert.All(ps, p => Assert.Equal(flag == 0 ? -0.2 : 0.0, p.MilLiving, 12));
            Assert.All(ps, p => Assert.Equal(0.5, p.MilFamine, 9));   // 飢餓の項は旗に依らない
            if (flag == 0) m0 = ps.Select(p => p.Militancy).ToArray(); else m1 = ps.Select(p => p.Militancy).ToArray();
        }
        Assert.Equal(m0.Length, m1.Length);
        Assert.True(m0.Length > 50);
        for (int i = 0; i < m0.Length; i++)
        {
            Assert.Equal(0.5, m1[i], 9);               // 旗 1＝飢餓だけ（政治の項は意識 0 で 0・民族と厭戦は 0 の盤面）… 他の項が立つ POP は下の差で見る
            Assert.Equal(0.2, m1[i] - m0[i], 9);       // 打ち消されなくなった分＝生活の項（0.8×0.25）
        }
    }

    [Fact]
    public void 旗1では思想の更新の入力が不満度と闘争度の大きい方で_旗0は不満度を読まない()
    {
        // 落差 0.5・意識 0 → 不満度＝0.8×0.5＝0.4（手計算）。闘争度 0.1（不満度が大きい）と 0.9（闘争度が大きい）の 2 つの盤面で、旗 1 の更新後のシェアを
        // 〈旗 0 で闘争度をその大きい方の値に置いた世界〉のシェアと比べる（同じ決定論の盤面＝ビット一致）。
        foreach (var (milLow, milHigh) in new[] { (0.1, 0.4), (0.9, 0.9) })
        {
            var wSplit = Board(drop: 0.5, militancy: milLow); wSplit.Coef.DiscontentSplit = 1.0;
            var wRef = Board(drop: 0.5, militancy: milHigh); wRef.Coef.DiscontentSplit = 0.0;
            Ideologies.UpdateShares(wSplit);
            Ideologies.UpdateShares(wRef);
            var a = wSplit.AllPops().ToList(); var b = wRef.AllPops().ToList();
            Assert.Equal(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
                Assert.True(a[i].IdeologyShares.SequenceEqual(b[i].IdeologyShares), $"POP {i} のシェアが違う（闘争度 {milLow}→{milHigh}）");
        }
        // 旗 0 は不満度を呼ばない：件数 0（台帳）。旗 1 は全 POP の数。
        var w0 = Board(drop: 0.5); Ideologies.UpdateShares(w0);
        Assert.Equal(0L, w0.Cost[CostLedger.ParticipationPops]);
        var w1 = Board(drop: 0.5); w1.Coef.DiscontentSplit = 1.0; Ideologies.UpdateShares(w1);
        Assert.Equal((long)w1.AllPops().Count(), w1.Cost[CostLedger.ParticipationPops]);
    }

    [Fact]
    public void 旗1の思想の入力の不満度は更新の前のシェアの理想点で測る_参加の述語と同じ呼びの値()
    {
        // 意識・識字を 0.9 にして法の項（理想点に依る）を立てる。旗 1 の世界 X で更新の前に参加の述語と同じ呼び（DiscontentOf）で POP ごとの不満度 d を取り、
        // 旗 0 の同じ盤面 R の闘争度を max(d, 闘争度) に置く。更新後のシェアがビット一致なら、思想の入力は更新の前の理想点で測っている
        // （BaseWeights が r＝IdeologyShares を 0 クリアして上書きした後で読むと理想点が途中の値になり、一致しない）。
        World Make()
        {
            var w = Board(drop: 0.5, militancy: 0.1);
            foreach (var r in w.Regions) foreach (var p in r.Pops) { p.Consciousness = 0.9; p.Literacy = 0.9; }
            return w;
        }
        var wX = Make(); wX.Coef.DiscontentSplit = 1.0;
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
    public void 旗1の思想の入力は旗0と違う世界を作る_旗0とのシェアの差がある()
    {
        // 不満度が闘争度を上回る盤面（0.4 ＞ 0.1）で、旗 1 のシェアは旗 0 と違う（旗が何もしない＝休眠の自明な緑を避ける）。
        var w0 = Board(drop: 0.5, militancy: 0.1);
        var w1 = Board(drop: 0.5, militancy: 0.1); w1.Coef.DiscontentSplit = 1.0;
        Ideologies.UpdateShares(w0); Ideologies.UpdateShares(w1);
        var a = w0.AllPops().ToList(); var b = w1.AllPops().ToList();
        int diff = 0;
        for (int i = 0; i < a.Count; i++) if (!a[i].IdeologyShares.SequenceEqual(b[i].IdeologyShares)) diff++;
        Assert.True(diff > 50, $"シェアが変わった POP が少ない {diff}");
    }
}
