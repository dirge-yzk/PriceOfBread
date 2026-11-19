using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1244) c126pf 旗 CapSkipSellerPotential の番人（事前登録 measurements/m1244_20261119_prereg_pf.txt の T1〜T4）。
/// 旧：清算で容量が尽きた売り手（BilateralMarket.cs の :963 の分岐）は飛ばされ、潜在量を残さない＝船腹の取り分（潜在量の比）が 0 の州は次の四半期も容量 0（片方向の錠）。
/// 新：その行で通るはずだった量 min(残り, X_lp−その行の未充足)×重さを売り手の州の潜在量へ足す（買い手の側には足さない）。
/// 盤面＝英仏の世界（ClearingScopeTests.Board の作り）・英の港の州が売り手（A が最安）・仏の 1 州だけが買い手・他の財は動かない・英の州の潜在量は B だけ 1（A の取り分 0＝容量 0）。
/// 期待値は試験の中で市場の欄から組む（エンジンの CapSkipQty を呼ばない）。
/// ⚠突然変異（事前登録の表）：μ1 足す量 0 → T1・T2・T3 赤／μ2 買い手の側だけ → T1・T2 赤／μ3 飛ばしの後の位置（次の売り手）で足す → T1・T2 赤／μ4 未充足を引かない → T4 赤。</summary>
[Collection("Instruments")]
public class CapSkipSellerPotentialTests
{
    private sealed record Bd(MultiNationSim Sim, int G, List<Region> Sellers, Region Buyer, World Uk);

    /// <summary>売り手 n 州（英の港・価格 0.5・0.6…×世界の値段）・買い手は仏の 1 州。英の州の潜在量は最後の売り手だけ 1＝他の売り手は容量 0。</summary>
    private static Bd Make(double flag, int nSellers)
    {
        var (sim, g, sellers, buyersAll) = ClearingScopeTests.Board(nSellers, c => { c.CapSkipSellerPotential = flag; c.PortCapToThroughput = 0; });
        ClearingScopeTests.SetCalendar(sim, 1800, 1);
        var uk = sim.Nations[0].World;
        Assert.True(sellers.All(s => uk.Regions.Contains(s)), "売り手が英の州でない（盤面の前提の破れ）");
        double wp = sim.Market.Price[g];
        var buyer = buyersAll.First(r => !uk.Regions.Contains(r));
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                foreach (var mk in r.Markets) if (!ReferenceEquals(mk, r.Markets[g])) { mk.LastDemand = 0; mk.TargetStock = 0; mk.LastProduced = 0; mk.Stock = 0; }   // 他の財は売り手にも買い手にもならない
                if (sellers.Contains(r) || ReferenceEquals(r, buyer)) continue;
                var m = r.Markets[g]; m.LastDemand = 0; m.TargetStock = 0; m.Stock = 0; m.Price = wp;   // 買い手は 1 州だけ
            }
        foreach (var r in uk.Regions) r.LastTradePotential = 0;
        sellers[^1].LastTradePotential = 1.0;   // 英の船腹の取り分は最後の売り手だけ＝他の売り手の容量は 0（港の上乗せ 0）
        return new Bd(sim, g, sellers, buyer, uk);
    }

    /// <summary>買い手の X を買い手の自分の値段で読んだ値（事前登録 [1] の X_lp・Q＝P）。為替は rpT÷lp で約される。</summary>
    private static double XLp(Bd b)
    {
        var m = b.Buyer.Markets[b.G]; var c = b.Sim.Coef; double eps = c.PriceElasticity;
        var bw = b.Sim.Nations.First(n => n.World.Regions.Contains(b.Buyer)).World;
        double ratio = bw.Region.GoodOf(b.G)!.RefPrice * b.Buyer.PriceLevel / m.Price;
        double xPrice = m.LastDemand * Math.Pow(ratio, 2 * eps) - (m.LastProduced + m.LastImport);
        double xNeed = c.TradeBuyBySupplyCurve > 0 ? m.TargetStock + m.LastDemand * Math.Pow(ratio, eps) - m.LastProduced - m.Stock : m.TargetStock + m.LastDemand - m.LastProduced - m.Stock;
        return bw.ExternalTradeAccess() * Math.Max(0, Math.Min(xPrice, xNeed));
    }

    private static void Clear(Bd b) => b.Sim.Market.Step(b.Sim.Nations, b.Sim.Coef, firstClearingOfQuarter: true);

    /// <summary>次の四半期の最初の清算の前：流れの記帳を 0 へ・買い手の在庫を 0 へ（潜在量は残す＝次の取り分の入力）。</summary>
    private static void NextQuarter(Bd b)
    {
        foreach (var n in b.Sim.Nations) foreach (var r in n.World.Regions) { var m = r.Markets[b.G]; m.LastExport = 0; m.LastImport = 0; }
        var mb = b.Buyer.Markets[b.G]; mb.Stock = 0; mb.PrevImport = 1;
        ClearingScopeTests.SetCalendar(b.Sim, 1800, 2);
    }

    private static string Flows(Bd b)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var n in b.Sim.Nations) foreach (var r in n.World.Regions) { var m = r.Markets[b.G]; sb.Append(BitConverter.DoubleToInt64Bits(m.Stock)).Append(',').Append(BitConverter.DoubleToInt64Bits(m.LastExport)).Append(',').Append(BitConverter.DoubleToInt64Bits(m.LastImport)).Append(';'); }
        return sb.ToString();
    }

    [Fact]
    public void T1_容量0で飛ばした売り手の潜在量は旗1でその行の量になり旗0では0()
    {
        var b0 = Make(0.0, 2); var b1 = Make(1.0, 2);
        double x = XLp(b1); double wg = Catalog.TransportWeight(b1.G);
        double leftA = b1.Sellers[0].Markets[b1.G].Stock - b1.Sellers[0].Markets[b1.G].TargetStock;
        Assert.True(x > 1e-9, $"買い手の X_lp が 0（盤面の前提の破れ）{x}");
        Clear(b0); Clear(b1);
        var a0 = b0.Sellers[0]; var a1 = b1.Sellers[0];
        Assert.Equal(0.0, a0.LastTransportCap);   // 前提：A の容量 0
        Assert.True(b1.Buyer.Markets[b1.G].LastImport > 1e-9, "買い手が成約していない（行が走査されていない＝盤面の前提の破れ）");
        Assert.Equal(0.0, a0.LastTradePotential);   // 旧：飛ばした売り手は潜在量を残さない
        double expect = Math.Min(leftA, x) * wg;
        Assert.True(a1.LastTradePotential > 1e-12, $"旗 1 で A の潜在量が 0（{a1.LastTradePotential}）");
        Assert.Equal(1.0, a1.LastTradePotential / expect, 9);
        Assert.Equal(Flows(b0), Flows(b1));   // 対照：この清算の成約・在庫は旗で変わらない（潜在量の記帳だけ）
        Assert.Equal(b0.Sellers[1].LastTradePotential, b1.Sellers[1].LastTradePotential);   // 対照：容量のある売り手の潜在量は変わらない
        Assert.Equal(b0.Buyer.LastTradePotential, b1.Buyer.LastTradePotential);   // 買い手の側には足さない
    }

    [Fact]
    public void T2_次の四半期に旗1では取り分と容量が開き成約し旗0では閉じたまま()
    {
        var b0 = Make(0.0, 2); var b1 = Make(1.0, 2);
        Clear(b0); Clear(b1); NextQuarter(b0); NextQuarter(b1); Clear(b0); Clear(b1);
        var a0 = b0.Sellers[0]; var a1 = b1.Sellers[0];
        Assert.Equal(0.0, a0.LastTransportCap);
        Assert.Equal(0.0, a0.Markets[b0.G].LastExport);
        Assert.True(a1.LastTransportCap > 1e-9, $"旗 1 で次の四半期の A の容量が 0（{a1.LastTransportCap}）");
        Assert.True(a1.Markets[b1.G].LastExport > 1e-9, $"旗 1 で次の四半期に A が売れていない（{a1.Markets[b1.G].LastExport}）");
    }

    [Fact]
    public void T4_同じ行の2人目の容量0の売り手は行の残りの量だけ()
    {
        var b0 = Make(0.0, 3); var b1 = Make(1.0, 3);
        double x = XLp(b1); double wg = Catalog.TransportWeight(b1.G);
        foreach (var bb in new[] { b0, b1 }) { var ma = bb.Sellers[0].Markets[bb.G]; ma.Stock = ma.TargetStock + x / 2; }   // A の余り＝X_lp の半分
        double leftA2 = b1.Sellers[1].Markets[b1.G].Stock - b1.Sellers[1].Markets[b1.G].TargetStock;
        Clear(b0); Clear(b1);
        Assert.Equal(0.0, b0.Sellers[0].LastTradePotential); Assert.Equal(0.0, b0.Sellers[1].LastTradePotential);
        double qA = Math.Min(x / 2, x);
        Assert.Equal(1.0, b1.Sellers[0].LastTradePotential / (qA * wg), 9);
        Assert.Equal(1.0, b1.Sellers[1].LastTradePotential / (Math.Min(leftA2, x - qA) * wg), 9);
    }

    private static double[] InsRun(double flag)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ck_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var b = Make(flag, 2);
            Clear(b);
            var q = Instruments.CapskipPeek(b.Uk);
            Assert.NotNull(q);
            return Enumerable.Range(0, q!.GetLength(1)).Select(k => q[b.G, k]).ToArray();
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void T3_計器は同じ盤面で旧と新を両方数え旗1の足した量は影と一致()
    {
        var r0 = InsRun(0.0); var r1 = InsRun(1.0);   // 列＝skips・skipsPricedOut・offerWeight・shadowWeight・addedWeight
        Assert.True(r0[0] >= 1, "旗 0 で飛ばしが数えられていない");
        Assert.True(r0[3] > 1e-12, "旗 0 で影の量が 0（新の規則の影を数えていない）");
        Assert.Equal(0.0, r0[4]);
        Assert.Equal(r0[3], r1[3]);   // 影は旗に依らない
        Assert.Equal(1.0, r1[4] / r1[3], 9);
    }
}
