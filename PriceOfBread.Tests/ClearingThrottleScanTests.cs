using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(434) 清算の片方向の錠（読む線 (411) 問い 2・測る前の登録 measurements/cl_20261019_prereg.txt）の番人。
/// 番でなくて飛ばした眠る行は潜在量の加算の手前で飛ばされ、<c>LastTradePotential</c> が番の行が見た分だけに過小に記帳される。
/// ClearingIdleThrottledScan（(ii)）＝買い手の州が前の四半期の最初の清算で律速だった行は番でなくても走査する。
/// ClearingIdlePotentialReweight（①）＝番の標本として走査した行の潜在量を N 倍で記帳する（両端・(ii) が起こした行は重み 1）。
/// どちらも 2026-10-19 (434) に既定 1 で点火（0 が差し戻し口＝点火前と 1 ビットも違わない）。ここは機構が働いていること（ゲート緑の嘘を避ける）を見る。期待値は N=1 の最初の清算で成約する買い手の集合と、係数 N の側から書く（計器の計算とは別の出所）。
/// 潜在量の読みは計器（<c>Instruments.ScopeGetThr</c>）を入れ替えるので Instruments の収集に入れる。
/// ⚠突然変異（測った）：律速の条件を落とす／NaN を律速から外す／重みを落とす／重みを買い手側だけにする／(ii) の起こした行にも重みを掛ける／旗 0 でも N 倍にする。</summary>
[Collection("Instruments")]
public class ClearingThrottleScanTests
{
    private const int N = 4;

    /// <summary>N=4・最初の清算で、買い手の州の律速率を <paramref name="throttle"/> に置いて回し、成約した買い手の添字の集合を返す。</summary>
    private static HashSet<int> Traded(int quarter, double throttle, double wakeFlag, Func<int, double>? perBuyer = null)
    {
        var (sim, g, _, buyers) = ClearingScopeTests.Board(3, c => { c.ClearingIdleSearchQuarters = N; c.ClearingIdleThrottledScan = wakeFlag; });
        ClearingScopeTests.SetCalendar(sim, 1800, quarter);
        for (int i = 0; i < buyers.Count; i++) buyers[i].LastTradeThrottle = perBuyer is null ? throttle : perBuyer(i);
        sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
        var set = new HashSet<int>();
        for (int i = 0; i < buyers.Count; i++) if (buyers[i].Markets[g].LastImport > 1e-9) set.Add(i);
        return set;
    }

    /// <summary>N=1（全ての行が毎四半期探す）の最初の清算で成約する買い手の集合＝眠らせない世界の答え。</summary>
    private static HashSet<int> NoSleep()
    {
        var (sim, g, _, buyers) = ClearingScopeTests.Board(3);
        ClearingScopeTests.SetCalendar(sim, 1800, 1);
        sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
        var set = new HashSet<int>();
        for (int i = 0; i < buyers.Count; i++) if (buyers[i].Markets[g].LastImport > 1e-9) set.Add(i);
        return set;
    }

    [Fact]
    public void 律速の州の番でない行は旗1で走査され旗0では飛ばされる()
    {
        var all = NoSleep();
        Assert.True(all.Count >= 8, $"N=1 で成約する買い手が少なすぎる（{all.Count}）＝盤面の前提の破れ");
        for (int q = 1; q <= N; q++)
        {
            var off = Traded(q, 0.5, 0.0);   // 旗 0＝州の律速は読まれない＝番の行だけ
            var on = Traded(q, 0.5, 1.0);    // 旗 1＝全ての買い手の州が律速＝全ての行が走査される＝N=1 と同じ
            Assert.True(off.IsSubsetOf(all), $"q={q}：旗 0 で N=1 に無い成約がある");
            Assert.True(off.Count < all.Count, $"q={q}：旗 0 で全ての行が走査された＝番の門が働いていない（盤面の前提の破れ）");
            Assert.True(on.SetEquals(all), $"q={q}：旗 1 で律速の州の行が全て走査されていない（成約 {on.Count}／N=1 の {all.Count}）");
        }
    }

    [Fact]
    public void 律速でない州の行は旗1でも番のまま()
    {
        for (int q = 1; q <= N; q++)
        {
            var off = Traded(q, 1.0, 0.0);
            Assert.True(Traded(q, 1.0, 1.0).SetEquals(off), $"q={q}：律速率 1（律速でない）の行が旗 1 で起こされた");
            Assert.True(Traded(q, 2.0, 1.0).SetEquals(off), $"q={q}：律速率が 1 を超える行が旗 1 で起こされた");
        }
    }

    [Fact]
    public void 律速の州だけが起こされる()
    {
        var all = NoSleep();
        var ordered = all.OrderBy(i => i).ToList();
        var thrSet = ordered.Where((_, k) => k % 2 == 0).ToHashSet();   // 成約できる買い手の半分を律速にする
        bool anyExtra = false;
        for (int q = 1; q <= N; q++)
        {
            var off = Traded(q, 1.0, 0.0);
            var on = Traded(q, 1.0, 1.0, i => thrSet.Contains(i) ? 0.5 : 1.0);
            var expect = new HashSet<int>(off); expect.UnionWith(thrSet);
            Assert.True(on.SetEquals(expect), $"q={q}：旗 1 の成約が〈番の行＋律速の州の行〉と一致しない（成約 {on.Count}／期待 {expect.Count}）");
            if (on.Count > off.Count) anyExtra = true;
        }
        Assert.True(anyExtra, "律速の州の行が一度も増えていない（盤面の前提の破れ）");
    }

    [Fact]
    public void 律速率が数でない州も走査する側へ倒れる()
    {
        var all = NoSleep();
        for (int q = 1; q <= N; q++)
            Assert.True(Traded(q, double.NaN, 1.0).SetEquals(all), $"q={q}：NaN の律速率の行が飛ばされた（否定形 !(x>=1) の向き）");
    }

    // ============================================================================================
    // ① 番の標本の行の潜在量は N 倍（両端）・(ii) が起こした行は重み 1

    private static (long thrRows, long skippedThr, long skipped, (long ThrRows, long Fills, long ThrFills, double ThrQty, double PotRaw, double PotAdded, double ThrPotRaw) c0,
        (long ThrRows, long Fills, long ThrFills, double ThrQty, double PotRaw, double PotAdded, double ThrPotRaw) c1, double sumRegionPotential) Run(int quarter, double wake, double reweight, Func<int, double> throttle)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_clt_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var (sim, g, _, buyers) = ClearingScopeTests.Board(3, c => { c.ClearingIdleSearchQuarters = N; c.ClearingIdleThrottledScan = wake; c.ClearingIdlePotentialReweight = reweight; });
            ClearingScopeTests.SetCalendar(sim, 1800, quarter);
            for (int i = 0; i < buyers.Count; i++) buyers[i].LastTradeThrottle = throttle(i);
            // 買い手以外の州（他の財の行を持つ）の律速率は 1 のまま＝旗 1 でも起こされない
            sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
            double sum = 0; foreach (var n in sim.Nations) foreach (var r in n.World.Regions) sum += r.LastTradePotential;
            var sk = Instruments.ScopeGet(sim, 4);
            var c0 = Instruments.ScopeGetThr(sim, 0); var c1 = Instruments.ScopeGetThr(sim, 1); var c4 = Instruments.ScopeGetThr(sim, 4);
            return (c1.ThrRows, c4.ThrRows, sk.Rows, c0, c1, sum);
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>番の標本の行（種類 1）が走査され、潜在量を足している四半期を探す（位相は行ごとに散る＝4 つの四半期のうち少なくとも 1 つで居る）。</summary>
    private static int QuarterWithSampleRows()
    {
        for (int q = 1; q <= N; q++)
        {
            var r = Run(q, 0.0, 0.0, _ => 1.0);
            if (r.c1.PotRaw > 1e-9) return q;
        }
        throw new Exception("番の標本の行が潜在量を足している四半期が無い（盤面の前提の破れ）");
    }

    [Fact]
    public void 旗0では素の量と足した量が一致し最初の清算の潜在量は元のまま()
    {
        int q = QuarterWithSampleRows();
        var r = Run(q, 0.0, 0.0, _ => 0.5);   // 旗 0＝律速でも重みは掛からない
        Assert.True(r.c1.PotRaw > 1e-9 && r.c0.PotRaw >= 0);
        Assert.Equal(r.c1.PotRaw, r.c1.PotAdded, 9);
        Assert.Equal(r.c0.PotRaw, r.c0.PotAdded, 9);
    }

    [Fact]
    public void 番の標本の行はN倍で記帳され他の行は重み1で両端の和が合う()
    {
        int q = QuarterWithSampleRows();
        var off = Run(q, 0.0, 0.0, _ => 1.0);
        var on = Run(q, 0.0, 1.0, _ => 1.0);
        // 旗が成約の経路を変えない（同じ組が成約する）＝素の量は同じ
        Assert.Equal(off.c1.PotRaw, on.c1.PotRaw, 9);
        Assert.Equal(off.c0.PotRaw, on.c0.PotRaw, 9);
        // 番の標本の行（種類 1）は N 倍・最初から起きている行（種類 0）は重み 1
        Assert.Equal(N * off.c1.PotRaw, on.c1.PotAdded, 9);
        Assert.Equal(off.c0.PotRaw, on.c0.PotAdded, 9);
        // 両端：全ての州の潜在量の和の増分＝2×(N−1)×(種類 1 の素の量)（買い手の州と売り手の州に同じ重み）。期待値は係数 N と旗 0 の走りの素の量から
        Assert.Equal(2.0 * (N - 1) * off.c1.PotRaw, on.sumRegionPotential - off.sumRegionPotential, 6);
    }

    [Fact]
    public void 旗1で律速の州が起こした行は重み1のまま()
    {
        int q = QuarterWithSampleRows();
        var all = NoSleep();
        var thrSet = all.OrderBy(i => i).Where((_, k) => k % 2 == 0).ToHashSet();
        Func<int, double> half = i => thrSet.Contains(i) ? 0.5 : 1.0;
        var r = Run(q, 1.0, 1.0, half);
        Assert.True(r.c1.ThrRows > 0, "律速の州の行が走査されていない（盤面の前提の破れ）");
        Assert.True(r.c1.ThrPotRaw > 1e-9, "律速の州の行が潜在量を足していない（盤面の前提の破れ）");
        // 種類 1 の足した量＝律速の州の行（重み 1）＋律速でない番の行（重み N）
        double expect = r.c1.ThrPotRaw + N * (r.c1.PotRaw - r.c1.ThrPotRaw);
        Assert.Equal(expect, r.c1.PotAdded, 9);
        // 全ての買い手が律速なら重みは全て 1
        var allThr = Run(q, 1.0, 1.0, _ => 0.5);
        Assert.Equal(allThr.c1.PotRaw, allThr.c1.PotAdded, 9);
    }

    [Fact]
    public void 計器は飛ばした行のうち律速の州の行を数える()
    {
        var off1 = Run(1, 0.0, 0.0, _ => 0.5);   // 旗 0＝全ての買い手の州が律速でも飛ばす
        Assert.True(off1.skipped > 0, "番でなくて飛ばした行が無い（盤面の前提の破れ）");
        Assert.True(off1.skippedThr > 0 && off1.skippedThr <= off1.skipped);
        var none = Run(1, 0.0, 0.0, _ => 1.0);
        Assert.True(none.skippedThr < off1.skippedThr, "律速の州を増やしても飛ばした行の律速の内訳が増えない");
        var wake = Run(1, 1.0, 0.0, _ => 0.5);   // 旗 1＝律速の州の行は起こされる＝飛ばす行が減る
        Assert.True(wake.skipped < off1.skipped, "旗 1 で律速の州の行が起こされず飛ばす行が減らない");
    }
}
