using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>S4 裁定清算（2026-08-30 点火・docs/202608/20260822_貿易数量則の書き換え_設計_部分完了.md §1）の番人。
/// ここでは (a)既定値 (c)財保存＝売り手減=買い手増=LastImport
/// (d)最安の売り手が尽きたら**同じ買い手が同じ清算内で次善から買う**（旧経路は1買い手1成約＝構造的に不可能だった）を固定する。
/// ★2026-09-26（棚卸し econ-15）：旧経路（TradeArbitrageClearing=0）と同期容量の旗（TradeCapacityHard）は旗ごと削除した＝
/// OFF と ON の指紋の比較（b）も一緒に外した（比べる相手が無い）。</summary>
public class TradeClearingTests
{
    private static NationConfig[] TwoPorts() => [WorldData.ByName("ジェノヴァ"), WorldData.ByName("トスカーナ")];

    private static (MultiNationSim sim, int good, Region seller, Region seller2, List<Region> buyers) Make()
    {
        var sim = new MultiNationSim(TwoPorts(), seed: 1789) { DiplomacyFrozen = true };
        var gd = sim.Nations[0].World.Region.Goods
            .First(g => !g.IsColonial && !g.NonTradeable && g.Tier != GoodTier.Staple && !g.GrainChain
                        && Array.IndexOf(sim.Coef.MilitaryExportGoods, g.Id) < 0);
        double wp = sim.Market.Price[gd.Id];
        var all = new List<Region>();
        foreach (var n in sim.Nations)
        {
            n.World.Tariff = 0;
            n.World.RailCapacity = 1e9; n.World.ShipCapacity = 1e9;
            foreach (var r in n.World.Regions) if (r.State == RegionState.Active) all.Add(r);
        }
        var ports = all.Where(r => r.IsPort).ToList();
        Assert.True(ports.Count >= 2, "港州が2つ要る（次善ロールオーバーの検査）");
        var seller = ports[0]; var seller2 = ports[1];
        var buyers = new List<Region>();
        foreach (var r in all)
        {
            var m = r.Markets[gd.Id];
            m.LastDemand = 0; m.LastProduced = 0; m.LastExport = 0; m.LastImport = 0;
            if (ReferenceEquals(r, seller))
            {   // 最安・在庫は薄い（TargetStock を残すと avail=Stock−Target が小さい）＝すぐ尽きる
                m.Price = 0.5 * wp; m.Stock = 60; m.TargetStock = 10; m.LastDemand = 10; m.LastProduced = 10;
            }
            else if (ReferenceEquals(r, seller2))
            {   // 次善（少し高いがまだ買い手より安い）・在庫は厚い
                m.Price = 0.6 * wp; m.Stock = 5000; m.TargetStock = 100; m.LastDemand = 100; m.LastProduced = 100;
            }
            else
            {   // 買い手：高値・需要大・在庫ゼロ（X_need=Target+D−P−B が大きく立つ）
                m.Price = 2.0 * wp; m.Stock = 0; m.TargetStock = 50; m.LastDemand = 500; m.LastProduced = 0;
            }
        }
        return (sim, gd.Id, seller, seller2, buyers);
    }

    [Fact]
    public void 既定はフルスタック点火済み()
    {
        // ★2026-08-30 点火宣言（batch10段階測定＝貿易/GDP 0.5%→42〜66%・開放度次元が初の非ゼロ・本国96.7/経済41.2）
        var c = new Coefficients();
        Assert.Equal(1.0, c.TradeMonthlyClearing);
        Assert.Equal(1.0, c.TradeLedgerFix);
        Assert.Equal(1.0, c.PriceLawSeesExports);
    }

    [Fact]
    public void 裁定清算は財を保存する_売り手減は買い手増と一致()
    {
        var (sim, g, seller, seller2, _) = Make();
        double s0 = seller.Markets[g].Stock + seller2.Markets[g].Stock;
        double othersBefore = 0;
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions)
            if (!ReferenceEquals(r, seller) && !ReferenceEquals(r, seller2)) othersBefore += r.Markets[g].Stock;
        sim.Market.Step(sim.Nations, sim.Coef);
        double s1 = seller.Markets[g].Stock + seller2.Markets[g].Stock;
        double othersAfter = 0, imported = 0, exported = seller.Markets[g].LastExport + seller2.Markets[g].LastExport;
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions)
            if (!ReferenceEquals(r, seller) && !ReferenceEquals(r, seller2))
            { othersAfter += r.Markets[g].Stock; imported += r.Markets[g].LastImport; }
        Assert.True(exported > 1e-9, "1件も成約していない（機構が働いていない＝ゲート緑の嘘）");
        Assert.Equal(s0 - s1, othersAfter - othersBefore, 6);   // 売り手が減った分だけ買い手が増える（無から湧かない）
        Assert.Equal(exported, imported, 6);                    // 台帳の対称
        Assert.True(seller.Markets[g].Stock >= seller.Markets[g].TargetStock - 1e-9,
            "地元1ヶ月分（TargetStock）を割って剥ぎ取った（裁定3違反）");
    }

    [Fact]
    public void 最安が尽きたら同じ清算内で次善から買う()
    {
        var (sim, g, seller, seller2, _) = Make();
        sim.Market.Step(sim.Nations, sim.Coef);
        // 最安（avail=50）はほぼ喰い尽くされ、なお足りない需要が次善（0.6wp）から買われている
        Assert.True(seller.Markets[g].LastExport > 1e-9, "最安の売り手から買っていない");
        Assert.True(seller2.Markets[g].LastExport > 1e-9,
            "次善の売り手から買っていない＝ロールオーバー（skip→continue）が機能していない（旧経路は1買い手1成約でここが構造的に0になる）");
    }
}
