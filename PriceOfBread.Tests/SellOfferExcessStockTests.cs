using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1229) c124pf 値段の形成の線：清算の売りの申し出（BilateralMarket の yPrice）に目標を超える在庫を入れる旗 SellOfferExcessStock の機構の試験。
/// 事前登録 measurements/m1229_20261119_prereg_pf.txt [3]。盤面は試験の中で置く（ジェノヴァとトスカーナ・港の州 1 つが売り手・他の全ての州が買い手・他の財の在庫は 0＝買い手の予算を他の財が食わない）。
/// 期待は試験が置いた在庫・目標・需要から手で作る（エンジンの式を写さない）。
/// ⚠突然変異（measurements/m1229_20261119/mut_*.txt）：μ1 旗の門を外す→T1 の旗 0 の側が赤／μ2 足す位置を unitA÷rpS の内へ→T2 が赤／μ3 目標を引かない→T2 が赤／μ4 売り手の在庫を減らさない→T3 が赤。</summary>
public class SellOfferExcessStockTests
{
    private static NationConfig[] TwoPorts() => [WorldData.ByName("ジェノヴァ"), WorldData.ByName("トスカーナ")];

    /// <summary>盤面：売り手（港の州 1 つ）の財 g は値段 0.5×世界の値段・在庫と目標と需要と産出は引数。他の全ての州の g は買い手（値段 2×世界の値段・需要 500・在庫 0・目標 50）。g 以外の財の在庫は全ての州で 0。</summary>
    private static (MultiNationSim sim, int g, Region seller, List<Region> buyers) Board(double flag, double stock, double target, double demand, double produced)
    {
        var sim = new MultiNationSim(TwoPorts(), seed: 1789) { DiplomacyFrozen = true };
        sim.Coef.ClearingIdleSearchQuarters = 1;
        sim.Coef.SellOfferExcessStock = flag;
        foreach (var n in sim.Nations) n.World.Coef.SellOfferExcessStock = flag;
        var gd = sim.Nations[0].World.Region.Goods
            .First(x => !x.IsColonial && !x.NonTradeable && x.Tier != GoodTier.Staple && !x.GrainChain
                        && Array.IndexOf(sim.Coef.MilitaryExportGoods, x.Id) < 0);
        double wp = sim.Market.Price[gd.Id];
        var all = new List<Region>();
        foreach (var n in sim.Nations)
        {
            n.World.Tariff = 0;
            n.World.RailCapacity = 1e9; foreach (var rgT in n.World.Regions) rgT.RailTrack = 0; n.World.ShipCapacity = 1e9;
            foreach (var r in n.World.Regions) all.Add(r);
        }
        var seller = all.First(r => r.IsPort);
        var buyers = new List<Region>();
        foreach (var r in all)
        {
            for (int k = 0; k < r.Markets.Length; k++) if (k != gd.Id) r.Markets[k].Stock = 0;
            var m = r.Markets[gd.Id];
            m.LastExport = 0; m.LastImport = 0;
            if (ReferenceEquals(r, seller))
            { m.Price = 0.5 * wp; m.Stock = stock; m.TargetStock = target; m.LastDemand = demand; m.LastProduced = produced; }
            else
            { m.Price = 2.0 * wp; m.Stock = 0; m.TargetStock = 50; m.LastDemand = 500; m.LastProduced = 0; buyers.Add(r); }
        }
        Assert.True(buyers.Count >= 1, "買い手の州が無い（盤面の前提の破れ）");
        return (sim, gd.Id, seller, buyers);
    }

    [Fact]
    public void T1_産出0で在庫が目標を超える売り手は旗1で売り_旗0で売らない()
    {
        var (s1, g1, sel1, _) = Board(1.0, 5000, 10, 0, 0);
        s1.Market.Step(s1.Nations, s1.Coef);
        Assert.True(sel1.Markets[g1].LastExport > 1e-6, $"旗 1 で在庫の余りが売りに出ていない（{sel1.Markets[g1].LastExport}）");
        var (s0, g0, sel0, _) = Board(0.0, 5000, 10, 0, 0);
        s0.Market.Step(s0.Nations, s0.Coef);
        Assert.Equal(0.0, sel0.Markets[g0].LastExport);   // 旧の規則＝産出 0 の売り手の申し出は流れだけ＝0
    }

    [Fact]
    public void T2_売れる量は在庫の余りから自州の需要を引いた量()
    {
        // 在庫 60・目標 10（余り 50）・産出 0・需要 30 → 申し出＝余り−需要＝20（着値に依らない・左の残り 50 で切られない）
        var (s1, g1, sel1, buyers) = Board(1.0, 60, 10, 30, 0);
        s1.Market.Step(s1.Nations, s1.Coef);
        double ex = sel1.Markets[g1].LastExport;
        Assert.True(Math.Abs(ex - 20.0) <= 1e-9 * 20.0, $"旗 1 の売りが 余り−需要＝20 でない（{ex}）");
        Assert.True(buyers.Sum(r => r.Markets[g1].LastImport) > 1e-9, "買い手が受け取っていない（盤面の前提の破れ）");
        var (s0, g0, sel0, _) = Board(0.0, 60, 10, 30, 0);
        s0.Market.Step(s0.Nations, s0.Coef);
        Assert.Equal(0.0, sel0.Markets[g0].LastExport);
    }

    [Fact]
    public void T2b_在庫が目標以下なら両方の旗で同じ()
    {
        var (s1, g1, _, _) = Board(1.0, 10, 10, 10, 100);
        var (s0, g0, _, _) = Board(0.0, 10, 10, 10, 100);
        s1.Market.Step(s1.Nations, s1.Coef); s0.Market.Step(s0.Nations, s0.Coef);
        Assert.Equal(g0, g1);
        int n = 0;
        for (int i = 0; i < s0.Nations.Count; i++)
            for (int j = 0; j < s0.Nations[i].World.Regions.Count; j++)
            {
                var a = s0.Nations[i].World.Regions[j].Markets; var b = s1.Nations[i].World.Regions[j].Markets;
                for (int k = 0; k < a.Length; k++)
                {
                    Assert.Equal(BitConverter.DoubleToInt64Bits(a[k].Stock), BitConverter.DoubleToInt64Bits(b[k].Stock));
                    Assert.Equal(BitConverter.DoubleToInt64Bits(a[k].LastExport), BitConverter.DoubleToInt64Bits(b[k].LastExport));
                    Assert.Equal(BitConverter.DoubleToInt64Bits(a[k].LastImport), BitConverter.DoubleToInt64Bits(b[k].LastImport));
                    n++;
                }
            }
        Assert.True(n >= 100, $"比べた市場が少ない {n}");
    }

    [Fact]
    public void T3_在庫を売った分だけ買い手が受け取る()
    {
        var (sim, g, seller, buyers) = Board(1.0, 5000, 10, 0, 0);
        double s0 = seller.Markets[g].Stock, b0 = buyers.Sum(r => r.Markets[g].Stock);
        sim.Market.Step(sim.Nations, sim.Coef);
        double sold = s0 - seller.Markets[g].Stock, got = buyers.Sum(r => r.Markets[g].Stock) - b0;
        double ex = seller.Markets[g].LastExport, im = buyers.Sum(r => r.Markets[g].LastImport);
        Assert.True(ex > 1e-6, "成約が無い（盤面の前提の破れ＝何もしない実装でも通る形にしない）");
        Assert.True(Math.Abs(sold - got) <= 1e-9 * Math.Max(1.0, sold), $"売り手の減り {sold} ≠ 買い手の増え {got}");
        Assert.True(Math.Abs(sold - ex) <= 1e-9 * Math.Max(1.0, sold), $"売り手の減り {sold} ≠ LastExport {ex}");
        Assert.True(Math.Abs(ex - im) <= 1e-9 * Math.Max(1.0, ex), $"LastExport {ex} ≠ 買い手の LastImport の和 {im}");
        Assert.True(seller.Markets[g].Stock >= seller.Markets[g].TargetStock - 1e-9, "目標を割って売った");
    }

    [Fact]
    public void 計器deadmkの申し出の影は手で数えた値()
    {
        var m = new Market { Stock = 60, TargetStock = 10, LastDemand = 30, LastProduced = 0, LastImport = 0, LastExport = 0 };
        var (o, n) = Instruments.DeadmkOfferOf(m);
        Assert.Equal(0.0, o);    // 旧＝max(0, 0＋0−30−0)
        Assert.Equal(20.0, n);   // 新＝max(0, −30＋50)
        var m2 = new Market { Stock = 5, TargetStock = 10, LastDemand = 0, LastProduced = 7, LastImport = 1, LastExport = 2 };
        var (o2, n2) = Instruments.DeadmkOfferOf(m2);
        Assert.Equal(6.0, o2); Assert.Equal(6.0, n2);   // 余りが負＝新は旧と同じ
    }
}
