using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(403) 清算の見る範囲の絞り（結果の変わる高速化・採用して既定へ焼き込んだ）の番人。測る前の登録＝measurements/cs_20261018_prereg.txt。
/// ②<c>ClearingIdleSearchQuarters</c>＝取引も不足も無かった買い手行は N 四半期に 1 回だけ、四半期の最初の清算で売り手を探す
/// （<c>ClearingIdleShortageWakesStaplesOnly</c>＝不足が起こす行を主食に限る）。
/// ①<c>ClearingPartnerScope</c>＝四半期の 2・3 月目の清算で、最初の清算に成約した行は覚えの売り手（K 人）から先に買い、尽きたら全ての売り手を探す。
/// 2026-10-18 に採用して既定へ焼き込んだ（N＝ClearingIdleSearchQuarters 既定 4・K＝ClearingPartnerScope 既定 4・主食の線は無条件）。旧の挙動との 1 ビット一致は旗のあった木の成績表 3 枚が証明した（ここでは対照を〈N=1＝全ての行が毎四半期探す〉と〈覚えの無い行〉で書く）。
/// ここは機構が働いていること（ゲート緑の嘘を避ける）と、覚えが作業領域であること・覚えが尽きたらN=1 と同じ（か覚えの無い行と同じ）になることを見る。
/// 盤面＝イギリスとフランス（港州が 3 つ以上）・売り手 3 州（価格 0.5・0.6・0.7 倍・在庫は厚い）・買い手（高値・需要大・在庫ゼロ）。
/// 買い手の全てが成約できるわけではない（連結しない州・容量）＝成約できる買い手の集合はN=1 の最初の清算で決める（<see cref="Baseline"/>）。
/// ⚠突然変異（測った）：番の判定を消す／PrevImport の起こす理由を消す／LastShortage の起こす理由を消す／主食の線を消す／覚えで絞る条件から thin を消す／探し直しを消す／覚えの目印を消す。</summary>
public class ClearingScopeTests
{
    private static NationConfig[] Cfg() => [WorldData.ByName("イギリス"), WorldData.ByName("フランス")];   // 港州が 3 つ以上ある 2 国

    /// <summary>売り手 nSellers 州（価格の昇順 0.5・0.6・…倍・在庫は厚い）と、買い手（高値・需要大・在庫ゼロ）。TradeClearingTests.Make と同じ盤面の作り。</summary>
    private static (MultiNationSim sim, int g, List<Region> sellers, List<Region> buyers) Board(int nSellers, Action<Coefficients>? coef = null, int nBuyers = int.MaxValue, bool staple = false)
    {
        var sim = new MultiNationSim(Cfg(), seed: 1789) { DiplomacyFrozen = true };
        sim.Coef.WinterPortFreeze = 0;   // 暦で凍る港が成約を左右しない（四半期ごとに盤面の成約できる行が変わらない）
        sim.Coef.TransportShareByPotential = 0;   // 輸送の容量の取り分が前の清算の潜在量で動かない（2 回目の清算で売り手の容量が偏らない）
        sim.Coef.ClearingIdleSearchQuarters = 1;   // ②は各試験が自分で入れる（既定 4 だと最初の清算で番でない行が飛ばされる）
        coef?.Invoke(sim.Coef);
        var gd = sim.Nations[0].World.Region.Goods
            .First(x => !x.IsColonial && !x.NonTradeable && (staple ? (x.Tier == GoodTier.Staple || x.GrainChain) : (x.Tier != GoodTier.Staple && !x.GrainChain))
                        && Array.IndexOf(sim.Coef.MilitaryExportGoods, x.Id) < 0);
        double wp = sim.Market.Price[gd.Id];
        var all = new List<Region>();
        foreach (var n in sim.Nations)
        {
            n.World.Tariff = 0;
            n.World.RailCapacity = 1e9; n.World.ShipCapacity = 1e9;
            foreach (var r in n.World.Regions) if (r.State == RegionState.Active) all.Add(r);
        }
        var ports = all.Where(r => r.IsPort).ToList();
        Assert.True(ports.Count >= nSellers, "港州が足りない");
        var sellers = ports.Take(nSellers).ToList();
        var buyers = new List<Region>();
        foreach (var r in all)
        {
            var m = r.Markets[gd.Id];
            m.LastDemand = 0; m.LastProduced = 0; m.LastExport = 0; m.LastImport = 0; m.PrevImport = 0; m.PrevExport = 0; m.LastShortage = 0;
            int si = sellers.IndexOf(r);
            if (si >= 0) { m.Price = (0.5 + 0.1 * si) * wp; m.Stock = 1e6; m.TargetStock = 10; m.LastDemand = 10; m.LastProduced = 1e5; }   // 売り手の価格静止の量 (産出×単価÷参照価格−需要−輸出) が厚い
            else if (buyers.Count < nBuyers) { m.Price = 8.0 * wp; m.Stock = 0; m.TargetStock = 50; m.LastDemand = 500; m.LastProduced = 0; buyers.Add(r); }
            else { m.Price = wp; m.Stock = 0; m.TargetStock = 0; }   // 買い手にも売り手にもならない州
        }
        return (sim, gd.Id, sellers, buyers);
    }

    private static void SetCalendar(MultiNationSim sim, int year, int quarter)
    {
        foreach (var n in sim.Nations) { n.World.Year = year; n.World.Quarter = quarter; }
    }

    /// <summary>月が替わった形に盤面を戻す（月の決算が毎月することの代わり）：流れの記帳を 0 へ・買い手の在庫を 0 へ・買い手を起こす（前の月に輸入があった）。</summary>
    private static void NextMonth(int g, List<Region> sellers, List<Region> buyers)
    {
        foreach (var r in sellers) { var m = r.Markets[g]; m.LastExport = 0; m.LastImport = 0; }
        foreach (var r in buyers) { var m = r.Markets[g]; m.PrevImport = 1; m.LastImport = 0; m.LastExport = 0; m.Stock = 0; }
    }

    /// <summary>N=1 の最初の清算で成約する買い手の添字の集合（連結しない州・容量で成約できない買い手を除く）。②の試験はこの集合の上で見る。</summary>
    private static HashSet<int> Baseline(bool staple = false)
    {
        var (sim, g, _, buyers) = Board(3, staple: staple);
        SetCalendar(sim, 1800, 1);
        sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
        var set = new HashSet<int>();
        for (int i = 0; i < buyers.Count; i++) if (buyers[i].Markets[g].LastImport > 1e-9) set.Add(i);
        return set;
    }

    // ================================================================================================
    // ② 取引も不足も無い行は番の四半期だけ

    private static HashSet<int> TradedIndices(int N, int quarter, Action<List<Region>, int>? prepare, Action<Coefficients>? extra = null, bool staple = false, bool warmup = false)
    {
        var (sim, g, _, buyers) = Board(3, c => { c.ClearingIdleSearchQuarters = N; extra?.Invoke(c); }, staple: staple);
        SetCalendar(sim, 1800, quarter);
        if (warmup) foreach (var n in sim.Nations) n.World.WarmupFrozen = true;   // 暦が止まる
        prepare?.Invoke(buyers, g);
        sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
        var set = new HashSet<int>();
        for (int i = 0; i < buyers.Count; i++) if (buyers[i].Markets[g].LastImport > 1e-9) set.Add(i);
        return set;
    }

    private static void WakeByImport(List<Region> buyers, int g) { foreach (var r in buyers) r.Markets[g].PrevImport = 1; }
    private static void WakeByShortage(List<Region> buyers, int g) { foreach (var r in buyers) r.Markets[g].LastShortage = 1; }

    [Fact]
    public void 取引も不足も無い行は連続するN四半期のうちちょうど1回だけ売り手を探す()
    {
        const int N = 4;
        var t0 = Baseline();
        Assert.True(t0.Count >= 8, $"N=1 で成約する買い手が少なすぎる（{t0.Count}）＝盤面の前提の破れ");
        var perQuarter = Enumerable.Range(1, N).Select(q => TradedIndices(N, q, null)).ToList();
        foreach (var i in t0)
            Assert.True(perQuarter.Count(s => s.Contains(i)) == 1, $"買い手 {i} が N 四半期に {perQuarter.Count(s => s.Contains(i))} 回探した（1 回のはず）");
        foreach (var s in perQuarter) Assert.True(s.IsSubsetOf(t0), "N=1 で成約しない買い手が成約した");
        // 位相が散る＝1 つの四半期に全行が起きない（全行が同じ四半期に起きるなら高速化にならない）
        Assert.All(perQuarter, s => Assert.True(s.Count < t0.Count, "全ての行が同じ四半期に起きた＝位相が散っていない"));
        Assert.True(perQuarter.Count(s => s.Count > 0) >= 2, "番が 1 つの四半期にしか来ない");
    }

    [Fact]
    public void 前の月に輸入があった行は番でなくても探す()
    {
        const int N = 4;
        var t0 = Baseline();
        for (int q = 1; q <= N; q++)
            Assert.True(TradedIndices(N, q, WakeByImport).SetEquals(t0), $"四半期 {q}：起こす理由（取引があった）でN=1 と同じ（か覚えの無い行と同じ）行が探していない");
    }

    [Fact]
    public void 主食の不足のあった行は番でなくても探す()
    {
        const int N = 4;
        var s0 = Baseline(staple: true);
        Assert.True(s0.Count >= 8, $"主食の盤面で成約する買い手が少なすぎる（{s0.Count}）");
        for (int q = 1; q <= N; q++)
            Assert.True(TradedIndices(N, q, WakeByShortage, staple: true).SetEquals(s0), $"四半期 {q}：主食の不足で N=1 と同じ行が探していない＝飢饉の輸入を遅らせる");
    }

    [Fact]
    public void 主食でない財の不足だけの行は番の四半期だけ探す()
    {
        const int N = 4;
        var t0 = Baseline();
        var perQuarter = Enumerable.Range(1, N).Select(q => TradedIndices(N, q, WakeByShortage)).ToList();
        foreach (var i in t0)
            Assert.True(perQuarter.Count(set => set.Contains(i)) == 1, $"主食でない財の不足だけの行 {i} が N 四半期に {perQuarter.Count(set => set.Contains(i))} 回探した（1 回のはず）");
    }

    [Fact]
    public void 番でない不足だけの行は2回目の清算で走査し直さず自州価格が上がれば起きる()
    {
        // 最初の清算で番でないために飛ばした行は、走査して成約しなかった行と同じ印（ClearFailLp）を持つ＝2 月目に不足だけで起きても眠る。自州価格が上がれば起きる
        const int N = 4;
        var t0 = Baseline();
        int q = 1;
        var turn = TradedIndices(N, q, WakeByShortage);
        var offTurn = t0.Where(i => !turn.Contains(i)).ToList();
        Assert.True(offTurn.Count >= 2 && turn.Count >= 1, "番でない行と番の行が両方要る（盤面の前提）");
        HashSet<int> Month2(double priceUp)
        {
            var (sim, g, sellers, buyers) = Board(3, c => c.ClearingIdleSearchQuarters = N);
            SetCalendar(sim, 1800, q);
            WakeByShortage(buyers, g);
            sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
            foreach (var r in sellers) { var m = r.Markets[g]; m.LastExport = 0; m.LastImport = 0; }
            foreach (var r in buyers) { var m = r.Markets[g]; m.PrevImport = 0; m.LastImport = 0; m.LastExport = 0; m.Stock = 0; m.LastShortage = 1; m.Price *= priceUp; }
            sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: false);
            var set = new HashSet<int>();
            for (int i = 0; i < buyers.Count; i++) if (buyers[i].Markets[g].LastImport > 1e-9) set.Add(i);
            return set;
        }
        var same = Month2(1.0);
        foreach (var i in offTurn) Assert.DoesNotContain(i, same);          // 眠る
        var up = Month2(1.1);
        foreach (var i in offTurn) Assert.Contains(i, up);                  // 自州価格が上がれば起きる
    }

    [Fact]
    public void 助走中は番を使わない()
    {
        var t0 = Baseline();
        Assert.True(TradedIndices(4, 1, null, warmup: true).SetEquals(t0), "助走中に眠った行がある");
    }

    [Fact]
    public void 四半期の2回目以降の清算は今のまま取引の無い行を眠らせる()
    {
        // ②は四半期の最初の清算の話＝2・3 回目（thin）の挙動はK に依らず今のまま（取引も不足も無い行は眠る）
        var (sim, g, _, buyers) = Board(3, c => c.ClearingIdleSearchQuarters = 4);
        SetCalendar(sim, 1800, 1);
        sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: false);
        Assert.All(buyers, r => Assert.Equal(0.0, r.Markets[g].LastImport));
    }

    // ================================================================================================
    // ① 覚えのある行は覚えの売り手から先に買い、尽きたら全ての売り手を探す

    /// <summary>買い手 1 つの盤面で最初の清算を回し、次の月の盤面（買い手を起こす・流れを 0 へ）を作る。</summary>
    private static (MultiNationSim sim, int g, List<Region> sellers, Region buyer, double wp) AfterFirstClearing(int K)
    {
        var (sim, g, sellers, buyers) = Board(3, c => c.ClearingPartnerScope = Math.Max(K, 1), nBuyers: 1);   // K=0＝覚えを消した行（覚えの無い行の対照）
        SetCalendar(sim, 1800, 1);
        double wp = buyers[0].Markets[g].Price / 8.0;
        sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
        Assert.True(sellers.Sum(r => r.Markets[g].LastExport) > 1e-9, "最初の清算で買い手が成約していない（盤面の前提の破れ）");
        if (K == 0) buyers[0].Markets[g].ClearPartners = null;
        NextMonth(g, sellers, buyers);
        return (sim, g, sellers, buyers[0], wp);
    }

    /// <summary>最初の清算で最初に相手にした売り手の添字（K=1 の覚えの先頭）。最初の清算はK に依らないので、覚えの無い走りの x にも使える。</summary>
    private static int FirstDealt()
    {
        var (_, g, sellers, buyer, _) = AfterFirstClearing(1);
        var mem = buyer.Markets[g].ClearPartners;
        Assert.NotNull(mem);
        Assert.Single(mem!);
        int xi = sellers.IndexOf(mem[0]);
        Assert.True(xi >= 0, "覚えの売り手が売り手の一覧に無い");
        return xi;
    }

    [Fact]
    public void 覚えのある行は2回目の清算で覚えの売り手から先に買う()
    {
        int xi = FirstDealt();
        string detail = "";
        double FromX(int K)
        {
            var (sim, g, sellers, buyer, wp) = AfterFirstClearing(K);
            sellers[xi].Markets[g].Price = 1.0 * wp;   // 覚えの相手は他より高くなったが、まだ買える（着値＜自州価格）
            sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: false);
            detail += $" K={K}:[" + string.Join(",", sellers.Select((r, i) => $"{(i == xi ? "X" : "-")}{r.Name}:exp{r.Markets[g].LastExport:F1}/price{r.Markets[g].Price / wp:F2}/stock{r.Markets[g].Stock:F0}/target{r.Markets[g].TargetStock:F0}")) + $" 買い手 {buyer.Name} の輸入 {buyer.Markets[g].LastImport:F1}]";
            return sellers[xi].Markets[g].LastExport;
        }
        // 買い手は 1 つの売り手から売り手の価格静止の量までしか買えない＝高くなった X にも覚えの無い行では余りが回る。覚えのある行は X から先に買う＝X の取り分が増える
        double off = FromX(0), on = FromX(1);
        Assert.True(on > off * (1 + 1e-9) + 1e-9, $"覚えの売り手から先に買っていない（覚えの無い行の X の取り分 {off}・覚えのある行 {on}）{detail}");
    }

    [Fact]
    public void 覚えの売り手が尽きたら全ての売り手を探して覚えの無い行と同じになる()
    {
        int xi = FirstDealt();
        List<double> Run(int K)
        {
            var (sim, g, sellers, buyer, wp) = AfterFirstClearing(K);
            sellers[xi].Markets[g].Stock = sellers[xi].Markets[g].TargetStock;   // 覚えの先頭の相手は売れる分が無い
            sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: false);
            var l = new List<double>();
            foreach (var n in sim.Nations)
                foreach (var r in n.World.Regions)
                {
                    if (r.State != RegionState.Active) continue;
                    var m = r.Markets[g];
                    l.Add(m.Stock); l.Add(m.LastImport); l.Add(m.LastExport); l.Add(m.Price);
                    foreach (var p in r.Pops) l.Add(p.Savings.Value);
                }
            return l;
        }
        var a = Run(0); var b = Run(1); var c5 = Run(5);
        Assert.True(a.Count == b.Count && a.Count == c5.Count);
        Assert.True(a.Any(v => v > 0), "盤面が空");
        for (int i = 0; i < a.Count; i++)
        {
            Assert.True(BitConverter.DoubleToInt64Bits(a[i]) == BitConverter.DoubleToInt64Bits(b[i]), $"K=1 と覚えの無い行が {i} 番目で違う（{a[i]} 対 {b[i]}）");
            Assert.True(BitConverter.DoubleToInt64Bits(a[i]) == BitConverter.DoubleToInt64Bits(c5[i]), $"K=5 と覚えの無い行が {i} 番目で違う（{a[i]} 対 {c5[i]}）");
        }
    }

    [Fact]
    public void 覚えの無い行は2回目の清算でも全ての売り手を見る()
    {
        // 最初の清算を飛ばした盤面（覚えが無い）：K が違っても同じ（より安い売り手から買う）
        (double fromA, double fromB) Run(int K)
        {
            var (sim, g, sellers, buyers) = Board(3, c => c.ClearingPartnerScope = Math.Max(K, 1), nBuyers: 1);
            SetCalendar(sim, 1800, 1);
            NextMonth(g, sellers, buyers);
            sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: false);
            return (sellers[0].Markets[g].LastExport, sellers[1].Markets[g].LastExport);
        }
        var off = Run(0); var on = Run(1);
        Assert.True(off.fromA > 1e-9, "覚えの無い行で A から買っていない（対照の破れ）");
        Assert.Equal(off.fromA, on.fromA, 12);
        Assert.Equal(off.fromB, on.fromB, 12);
    }

    [Fact]
    public void 最初の清算の結果はKに依らない()
    {
        // ①は四半期の 2・3 回目だけ変える＝最初の清算（全ての売り手を見る）は K に依らず同じ
        static List<double> Snap(int K)
        {
            var (sim, g, _, _) = Board(3, c => c.ClearingPartnerScope = Math.Max(K, 1));
            SetCalendar(sim, 1800, 1);
            sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
            var l = new List<double>();
            foreach (var n in sim.Nations) foreach (var r in n.World.Regions) if (r.State == RegionState.Active) { var m = r.Markets[g]; l.Add(m.Stock); l.Add(m.LastImport); l.Add(m.LastExport); }
            return l;
        }
        var a = Snap(0); var b = Snap(1); var c2 = Snap(3);
        Assert.Equal(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++) { Assert.Equal(a[i], b[i]); Assert.Equal(a[i], c2[i]); }
    }

    [Fact]
    public void 覚えは先のK人までで重複が無い()
    {
        var (_, g, sellers, buyer, _) = AfterFirstClearing(2);
        var mem = buyer.Markets[g].ClearPartners;
        Assert.NotNull(mem);
        Assert.True(mem!.Length >= 1 && mem.Length <= 2);
        Assert.Equal(mem.Length, mem.Distinct().Count());
        Assert.All(mem, r => Assert.Contains(r, sellers));
    }

    [Fact]
    public void 覚えは作業領域でありセーブに載らない()
    {
        var f = typeof(Market).GetField("ClearPartners", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(f);
        Assert.True(f!.IsNotSerialized, "覚え（Market.ClearPartners）は [NonSerialized]＝状態ではなく作業領域");
        foreach (var name in new[] { "ClearSellerPos", "ClearSellerStamp" })
        {
            var rf = typeof(Region).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.NotNull(rf);
            Assert.True(rf!.IsNotSerialized, $"Region.{name} は [NonSerialized]");
        }
    }

    [Fact]
    public void 前の四半期の覚えは次の四半期に持ち越さない()
    {
        // 覚えを作った四半期の次の四半期の最初の清算で、その行が飛ばされる（買えない）と覚えは作り直されない。覚えが残っていれば次の月の清算がそれで絞る＝消えていなければ旗 0 と違う
        int xi = FirstDealt();
        double FromX(int K)
        {
            var (sim, g, sellers, buyer, wp) = AfterFirstClearing(K);
            var m = buyer.Markets[g];
            if (K > 0) Assert.NotNull(m.ClearPartners);
            double demand = m.LastDemand;
            m.LastDemand = 0;   // 次の四半期の最初の清算ではこの行は需要が無く飛ばされる（覚えを作り直せない）
            sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
            Assert.Null(m.ClearPartners);   // 最初の清算の頭で消えている
            m.LastDemand = demand; m.PrevImport = 1; m.LastImport = 0; m.Stock = 0;
            foreach (var r in sellers) { var sm = r.Markets[g]; sm.LastExport = 0; sm.LastImport = 0; }
            sellers[xi].Markets[g].Price = 1.0 * wp;
            sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: false);
            return sellers[xi].Markets[g].LastExport;
        }
        Assert.Equal(BitConverter.DoubleToInt64Bits(FromX(0)), BitConverter.DoubleToInt64Bits(FromX(1)));
    }

    [Fact]
    public void 四半期の最初の清算は覚えを消して作り直す()
    {
        // 次の四半期の最初の清算の頭で覚えが消える＝その清算はK に依らず同じ（覚えのある行と無い行の双子が 1 ビットも違わない）
        int xi = FirstDealt();
        (double fromX, double imported) Run(int K)
        {
            var (sim, g, sellers, buyer, wp) = AfterFirstClearing(K);
            if (K > 0) Assert.NotNull(buyer.Markets[g].ClearPartners);
            sellers[xi].Markets[g].Price = 1.0 * wp;
            sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
            return (sellers[xi].Markets[g].LastExport, buyer.Markets[g].LastImport);
        }
        var off = Run(0); var on = Run(1);
        Assert.True(off.imported > 1e-9, "最初の清算で成約していない（盤面の前提の破れ）");
        Assert.Equal(BitConverter.DoubleToInt64Bits(off.fromX), BitConverter.DoubleToInt64Bits(on.fromX));
        Assert.Equal(BitConverter.DoubleToInt64Bits(off.imported), BitConverter.DoubleToInt64Bits(on.imported));
    }
}

/// <summary>(403 追記3) ①の価格の効果を機構として固定する番人＝覚えで絞った行は、候補表が覚えの売り手だけなので次善の着値が無い／少ない
/// ＝単価 unitA（次善の着値と自州価格の小さい方）が自州価格へ寄る。覚えの無い月の行（全ての売り手を見る）は次善の着値で払う。
/// これは〈輸入国から輸出国への移転〉を起こす（事前登録 追記3・測る前）。期待値は盤面の側（買い手の自州価格＝参照価格の 8 倍を測る前に決めた値）から書き、計器の計算とは別の出所。
/// 計器（<c>Instruments.ScopeGet</c>）を入れ替えるので Instruments の収集に入れる（他の計器の番人と並ばない）。</summary>
[Collection("Instruments")]
public class ClearingScopePriceTests
{
    private static (double unit, double lp, double best, long fb, double qAfter, double qty, double fromOthers) Month2(int K, double xStockSlack)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_csc_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789) { DiplomacyFrozen = true };
            sim.Coef.WinterPortFreeze = 0; sim.Coef.TransportShareByPotential = 0; sim.Coef.ClearingPartnerScope = Math.Max(K, 1); sim.Coef.ClearingIdleSearchQuarters = 1;
            var gd = sim.Nations[0].World.Region.Goods.First(x => !x.IsColonial && !x.NonTradeable && x.Tier != GoodTier.Staple && !x.GrainChain && Array.IndexOf(sim.Coef.MilitaryExportGoods, x.Id) < 0);
            int g = gd.Id; double wp = sim.Market.Price[g];
            var all = new List<Region>();
            foreach (var n in sim.Nations) { n.World.Tariff = 0; n.World.RailCapacity = 1e9; n.World.ShipCapacity = 1e9; foreach (var r in n.World.Regions) if (r.State == RegionState.Active) all.Add(r); }
            var sellers = all.Where(r => r.IsPort).Take(3).ToList();
            Region? buyer = null;
            foreach (var r in all)
            {
                var m = r.Markets[g]; m.LastDemand = 0; m.LastProduced = 0; m.LastExport = 0; m.LastImport = 0; m.PrevImport = 0; m.PrevExport = 0; m.LastShortage = 0;
                int si = sellers.IndexOf(r);
                if (si >= 0) { m.Price = (0.5 + 0.005 * si) * wp; m.Stock = 1e6; m.TargetStock = 10; m.LastDemand = 10; m.LastProduced = 1e5; }   // 売り手の価格が近い＝2 人目の下界が最良の着値の下に入る（遠いと旗 0 でも次善を見る前に打ち切る＝単価は自州価格）
                else if (buyer is null) { m.Price = 8.0 * wp; m.Stock = 0; m.TargetStock = 50; m.LastDemand = 500; m.LastProduced = 0; buyer = r; }
                else { m.Price = wp; m.Stock = 0; m.TargetStock = 0; }
            }
            foreach (var n in sim.Nations) { n.World.Year = 1800; n.World.Quarter = 1; }
            sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
            Assert.True(sellers.Sum(r => r.Markets[g].LastExport) > 1e-9, "最初の清算で成約していない（盤面の前提の破れ）");
            var mem = buyer!.Markets[g].ClearPartners;
            int xi = K > 0 ? sellers.IndexOf(mem![0]) : -1;
            if (K == 0) buyer.Markets[g].ClearPartners = null;   // 覚えの無い行の対照
            double lp = buyer.Markets[g].Price;
            foreach (var r in sellers) { var m = r.Markets[g]; m.LastExport = 0; m.LastImport = 0; }
            { var m = buyer.Markets[g]; m.PrevImport = 1; m.LastImport = 0; m.LastExport = 0; m.Stock = 0; }
            // 最初の清算の計器の蓄えは月の行と混ざらないよう、月の行の種類だけを読む
            if (K > 0 && xStockSlack >= 0) sellers[xi].Markets[g].Stock = sellers[xi].Markets[g].TargetStock + xStockSlack;   // 覚えの相手は少ししか売れない
            sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: false);
            var got = Instruments.ScopeGet(sim, K > 0 ? 3 : 2);
            double others = 0; for (int i = 0; i < sellers.Count; i++) if (i != xi) others += sellers[i].Markets[g].LastExport;
            return (got.QUnit / Math.Max(got.Qty, 1e-300), lp, got.QBest / Math.Max(got.Qty, 1e-300), got.FallbackRows, got.QAfterFallback, got.Qty, others);
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 覚えで絞った行は単価が自州価格へ寄り覚えの無い月の行は次善の着値で払う()
    {
        var off = Month2(0, -1);
        Assert.True(off.qty > 1e-9, "覚えの無い月の行が成約していない");
        Assert.True(off.unit < off.lp * 0.999, $"覚えの無い月の行の単価 {off.unit} が自州価格 {off.lp} に張り付いている（対照の破れ）");
        Assert.True(off.unit >= off.best * (1 - 1e-12), "単価が最良の着値を下回る");
        var on = Month2(1, -1);   // 覚えの売り手は 1 人＝次善の着値が無い＝単価は自州価格
        Assert.True(on.qty > 1e-9, "覚えで絞った月の行が成約していない");
        Assert.Equal(on.lp, on.unit, 9);
        Assert.True(on.unit > off.unit * 1.01, $"覚えで絞った行の単価 {on.unit} が覚えの無い行 {off.unit} より高くなっていない（価格の移転が機構として働いていない）");
    }

    [Fact]
    public void 覚えの売り手が少しだけ売って尽きたら全ての売り手を探し直し残りを買う()
    {
        var on = Month2(1, 1e-3);
        Assert.True(on.fb >= 1, "覚えが尽きて探し直した行が無い（盤面の前提の破れ）");
        Assert.True(on.qAfter > 1e-9, "探し直しの後の成約が無い");
        Assert.True(on.fromOthers > 1e-9, "覚えの外の売り手から買っていない");
    }
}
