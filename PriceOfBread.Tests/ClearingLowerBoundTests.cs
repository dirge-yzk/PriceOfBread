using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(1029) 線 c97sp：清算の着値の充填で、〈買い手の州×売り手の国〉の最短距離の下界の着値が自州価格以上の売り手を飛ばす（高速化・ビット恒等）。
/// 事前登録 measurements/m1029_20261111_prereg_sp.txt の T1〜T4。期待は手で置く（エンジンの関数を期待に使わない）：
/// T1＝下界の着値 ≦ 真の着値（全ての充填した対・1783 年の盤面を数か月・英仏普露）／T2＝州の入力（陸脚の長さ PortLandKm を 0.05 倍＝距離が縮む向き）を途中で動かしても T1 が成り立つ（表は清算ごとに作り直す＝持ち越すと縮んだ距離に対し古い下界が大きすぎて落ちる）／
/// T3＝下界の着値 ≧ 自州価格の対は真の着値も自州価格以上（飛ばしてよい）で、その対が実際に現れる／T4＝下界を使う道と使わない道（旧）で、軌道の写しが 1 ビットも違わず、
/// 充填の件数＋飛ばした件数＝旧の充填の件数・同じ道を 2 回回して飛ばした件数が同じ。
/// 突然変異（使い捨ての木）：最小を陸路だけで取る→T1・risk を 0 にしない→T1・break にする→T4・表を作り直さない→T2・飛ばす条件に 1e-9 の余裕を逆に付ける（delLB＞lp×(1−1e-9)）→T3。</summary>
[Collection("Instruments")]
public class ClearingLowerBoundTests
{
    private static MultiNationSim Four(ulong seed)
        => new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス"), WorldData.ByName("プロイセン"), WorldData.ByName("ロシア")], seed: seed) { DiplomacyFrozen = true };

    private static List<long> Digest(MultiNationSim sim)
    {
        var d = new List<long>();
        void A(double v) => d.Add(BitConverter.DoubleToInt64Bits(v));
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            A(w.Treasury.Value); A(w.TotalMoney.Value); A(w.GdpPerMonth.Value); A(w.PriceLevel); A(w.TotalPopulation);
            foreach (var r in w.Regions) { A(r.SumPopulation()); A(r.SumSavings().Value); foreach (var m in r.Markets) { A(m.Price); A(m.Stock); A(m.LastImport); A(m.LastExport); } }
        }
        return d;
    }

    private sealed class Audit { public long N, LbOverTrue, LbGeLp, LbGeLpTrueLt, NaN; }

    private static Audit RunAudited(int quarters, Action<MultiNationSim>? mid = null)
    {
        var a = new Audit();
        InternationalMarket.LbAudit = ((delLB, del2, lp, buyer, seller) =>
        {
            a.N++;
            if (double.IsNaN(delLB) || double.IsNaN(del2)) a.NaN++;
            else
            {
                if (delLB > del2) a.LbOverTrue++;
                if (InternationalMarket.LbSkip(delLB, lp)) { a.LbGeLp++; if (!(del2 >= lp)) a.LbGeLpTrueLt++; }
            }
        });
        try
        {
            var sim = Four(1783UL);
            for (int q = 0; q < quarters; q++) { if (q == quarters / 2) mid?.Invoke(sim); sim.StepQuarter(); }
        }
        finally { InternationalMarket.LbAudit = null; }
        return a;
    }

    [Fact]
    public void T1_下界の着値は真の着値以下()
    {
        var a = RunAudited(4);
        Assert.True(a.N > 2000, $"監査した充填が少ない（前提の破れ）{a.N}");
        Assert.Equal(0L, a.LbOverTrue);
        Assert.Equal(0L, a.NaN);
    }

    [Fact]
    public void T2_州の入力を途中で動かしても下界は真の着値以下()
    {
        var a = RunAudited(4, sim => { foreach (var n in sim.Nations) foreach (var r in n.World.Regions) r.PortLandKm = r.PortLandKm * 0.05; });
        Assert.True(a.N > 2000, $"監査した充填が少ない（前提の破れ）{a.N}");
        Assert.Equal(0L, a.LbOverTrue);
    }

    [Fact]
    public void T3_下界が自州価格以上の対は真の着値も自州価格以上でその対が現れる()
    {
        var a = RunAudited(4);
        Assert.True(a.LbGeLp > 100, $"下界で飛ばせる対が現れない（前提の破れ）{a.LbGeLp}");
        Assert.Equal(0L, a.LbGeLpTrueLt);
        // 境界（手で置く）：ちょうど等しい＝飛ばす・1 ulp 下＝飛ばさない・1e-9 だけ下＝飛ばさない・NaN＝飛ばさない（旧の経路へ）
        double lp0 = 1.2345;
        Assert.True(InternationalMarket.LbSkip(lp0, lp0));
        Assert.False(InternationalMarket.LbSkip(Math.BitDecrement(lp0), lp0));
        Assert.False(InternationalMarket.LbSkip(lp0 * (1 - 1e-9), lp0));
        Assert.False(InternationalMarket.LbSkip(double.NaN, lp0));
    }

    private static (List<long> D, long Fills, long Skips) RunPlain(bool lbOff, int quarters)
    {
        InternationalMarket.LbOffForTest = lbOff;
        try
        {
            var sim = Four(1783UL);
            for (int q = 0; q < quarters; q++) sim.StepQuarter();
            var cost = sim.CostTotals();
            return (Digest(sim), cost[CostLedger.ClearingFills], cost[CostLedger.ClearingSkipsLB]);
        }
        finally { InternationalMarket.LbOffForTest = false; }
    }

    [Fact]
    public void T4_下界を使う道は旧と軌道が同じで件数が合い飛ばした件数は再現する()
    {
        var off = RunPlain(true, 4);
        var on1 = RunPlain(false, 4);
        var on2 = RunPlain(false, 4);
        Assert.Equal(0L, off.Skips);
        Assert.True(on1.Skips > 100, $"飛ばした件数が少ない（前提の破れ）{on1.Skips}");
        Assert.Equal(off.D, on1.D);                         // 軌道の写しが 1 ビットも違わない
        Assert.Equal(off.Fills, on1.Fills + on1.Skips);     // 充填＋飛ばした＝旧の充填
        Assert.Equal(on1.Skips, on2.Skips);                 // 再現（並列・順に依らない）
        Assert.Equal(on1.D, on2.D);
    }
}
