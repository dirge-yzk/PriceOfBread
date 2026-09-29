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

    private static (MultiNationSim sim, int good, Region seller, Region seller2, List<Region> buyers) Make(params NationConfig[] cfg)
    {
        var sim = new MultiNationSim(cfg.Length > 0 ? cfg : TwoPorts(), seed: 1789) { DiplomacyFrozen = true };
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

    // ★2026-09-27（回4-1・項目12①・新1）：輸出額は越境の輸出だけ。旧＝総取引額（買い手側と売り手側の両方に積む）−輸入額
    //   ＝輸出額に国内の州間取引の2倍が入っていた（分子と分母の非対称）。国内の取引しかない世界（1国）で輸出額は0。
    //   同じ国の沿岸の海路は港の取扱高には積む（分ける軸は「国境」と「経路」の2本）。
    [Fact]
    public void 国内の州間取引は輸出額に入らない()
    {
        var (sim, g, _, _, _) = Make(WorldData.ByName("イギリス"));
        var w = sim.Nations[0].World;
        w.Tariff = 0.5;   // ★段3-1：国境の税＝同じ国の州どうしには掛からない
        double tf0 = w.CumulativeTradeFlow;
        sim.Market.Step(sim.Nations, sim.Coef);
        double moved = 0, port = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active) { moved += r.Markets[g].LastImport; port += r.LastPortThroughput; }
        Assert.True(moved > 1e-9, "国内の成約が1件も立っていない（テスト前提の破れ）");
        Assert.Equal(0.0, w.LastExportValue);
        Assert.Equal(0.0, w.LastImportValue);
        Assert.Equal(0.0, w.ExportsByGood!.Sum());
        Assert.True(port > 1e-9, "同じ国の海路（本国と海外の州）が港の取扱高に積まれていない");
        // ★段3-1（2026-09-29）：国内の決済＝関税は取らず、世界の台帳は清算所への払いと受け取りが打ち消して動かない（国境をまたいでいない）
        Assert.Equal(0.0, w.LastTariffRevenue);
        Assert.True(Math.Abs(w.CumulativeTradeFlow - tf0) <= 1e-9 * Math.Max(1.0, Math.Abs(tf0)), $"国内の取引が世界の台帳を動かした（{w.CumulativeTradeFlow - tf0}）");
    }

    // ★回4-1（項目12②）：港の取扱高は海路の成約だけ。陸の単価を極小にして陸で繋がる組を全て陸路の直行にすると、成約は立つが港の取扱高は0。
    //   対照＝同じ世界を既定の陸の単価で清算すると海路が勝つ組があり、港の取扱高が立つ（上の0は「取引が無いから」ではない）。
    //   あわせて、2国の世界で世界の輸出＝世界の輸入（越境の同じ成約を両側で1回ずつ）、国内でしか売らない国の輸出額は0。
    [Fact]
    public void 陸路の取引は港の取扱高に入らない()
    {
        var (land, g, _, _, _) = Make();
        land.Coef.LandSeaCostRatio = 1e-6;
        land.Market.Step(land.Nations, land.Coef);
        double moved = 0, port = 0;
        foreach (var n in land.Nations) foreach (var r in n.World.Regions)
            if (r.State == RegionState.Active) { moved += r.Markets[g].LastExport; port += r.LastPortThroughput; }
        Assert.True(moved > 1e-9, "陸路の成約が1件も立っていない（テスト前提の破れ）");
        Assert.Equal(0.0, port);

        var (sea, g2, _, seller2, _) = Make();
        sea.Market.Step(sea.Nations, sea.Coef);
        double port2 = 0;
        foreach (var n in sea.Nations) foreach (var r in n.World.Regions) if (r.State == RegionState.Active) port2 += r.LastPortThroughput;
        Assert.True(port2 > 1e-9, "既定の陸の単価で海路の成約が立たない（対照の破れ）");
        var genoa = sea.Nations[0].World; var tuscany = sea.Nations[1].World;
        Assert.True(seller2.Markets[g2].LastExport > 1e-9, "トスカーナの港が国内の州へ売っていない（テスト前提の破れ）");
        Assert.True(genoa.LastExportValue > 1e-9, "越境の輸出が立っていない（テスト前提の破れ）");
        Assert.Equal(0.0, tuscany.LastExportValue);                                 // 買い手はトスカーナの内陸州だけ＝トスカーナの売りは全て国内
        Assert.Equal(genoa.ExportsByGood!.Sum(), genoa.LastExportValue, 9);
        // ★段3-1（2026-09-29）：輸出＝積出し価格（FOB）・輸入＝積出し価格＋運賃（CIF）＝世界の輸入は世界の輸出より運賃の分だけ大きい（旧＝どちらも単価で等しい）。
        Assert.True(tuscany.LastImportValue > genoa.LastExportValue, "輸入（CIF）が輸出（FOB）に運賃を足した額になっていない");
        Assert.True(tuscany.LastImportValue - genoa.LastExportValue <= genoa.LastExportValue * sea.Coef.FreightRatePer10Mm * 2.0, "運賃が積出し価格×運賃率×(距離÷1万km) の桁を超えた");
    }

    // ================================================================================================
    // ★支払いの一元化 段3-1（貿易・2026-09-29）の番人。清算の払い手と受け手（計画 §10-12-9 の 3-1・econ-07・A7・作者裁定 2026-09-29 の2・3）。

    /// <summary>関税と穀物法の分は輸入国の国庫へ入り、外国の売り手の受取（単価）は関税で上がらない（econ-07・旧＝次善の着値を丸ごと売り手に払った＝関税を上げるほど輸出国の収入が増えた）。</summary>
    [Fact]
    public void 関税は輸入国の国庫へ入り外国の売り手の単価を上げない()
    {
        (double unit, double rev, double led, double fob) Run(double tariff)
        {
            var (sim, g, seller, seller2, _) = Make();
            var gen = sim.Nations[0].World; var tus = sim.Nations[1].World;
            Assert.Contains(seller, gen.Regions);
            seller2.Markets[g].Stock = 0;   // 売り手はジェノヴァの1州だけ＝トスカーナの買い手には次善が無い＝単価＝自州価格（関税で動かない）
            tus.Tariff = tariff;
            double led0 = tus.TreasuryLedger[(int)TreasuryCat.Tariff];
            sim.Market.Step(sim.Nations, sim.Coef);
            double q = 0; foreach (var r in gen.Regions) if (r.State == RegionState.Active) q += r.Markets[g].LastExport;
            Assert.True(q > 1e-9 && gen.LastExportValue > 1e-9, "ジェノヴァの越境の輸出が立っていない（テスト前提の破れ）");
            return (gen.LastExportValue / q, tus.LastTariffRevenue, tus.TreasuryLedger[(int)TreasuryCat.Tariff] - led0, gen.LastExportValue);
        }
        var free = Run(0.0);
        var taxed = Run(0.3);
        Assert.Equal(0.0, free.rev);
        Assert.True(taxed.rev > 1e-9, "関税の実収が0");
        Assert.Equal(0.3 * taxed.fob, taxed.rev, 6);   // 関税＝積出し価格（トスカーナの越境の輸入＝ジェノヴァの越境の輸出）×率
        Assert.Equal(taxed.rev, taxed.led, 6);          // 国庫の区分 Tariff へ（通関の時点で輸入者から）
        Assert.True(taxed.unit < free.unit * (1 - 1e-6), $"関税を上げても外国の売り手の受取（単価）が下がらない（{free.unit}→{taxed.unit}）＝関税が売り手の収入になっている（買い手の単価は自州価格のまま＝受取は関税の楔で割り戻した値のはず）");
    }

    /// <summary>輸入代金・関税・運賃は輸入の捌き手（買い手の州の商人）が払い、同じ州のほかのPOP の貯蓄は動かない（Q7 の b・旧＝国の全POP の貯蓄比）。</summary>
    [Fact]
    public void 輸入代金は捌き手の商人が払う()
    {
        var (sim, g, seller, seller2, _) = Make();
        foreach (var n in sim.Nations) n.World.Tariff = 0.2;
        var before = new Dictionary<Pop, double>();
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) foreach (var p in r.Pops) before[p] = p.Savings;
        sim.Market.Step(sim.Nations, sim.Coef);
        int checkedRegions = 0;
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                if (r.State != RegionState.Active || ReferenceEquals(r, seller) || ReferenceEquals(r, seller2) || !(r.Markets[g].LastImport > 1e-9)) continue;
                Assert.True(r.MerchantPopIndex >= 0, "前提が崩れた：買い手の州に商人が居ない");
                for (int i = 0; i < r.Pops.Count; i++)
                    if (i != r.MerchantPopIndex) Assert.Equal(before[r.Pops[i]], r.Pops[i].Savings);   // 商人以外は払わない
                Assert.True(r.Pops[r.MerchantPopIndex].Savings < before[r.Pops[r.MerchantPopIndex]], "輸入した州の商人の貯蓄が減っていない");
                checkedRegions++;
            }
        Assert.True(checkedRegions > 0, "輸入した買い手の州が無い（テスト前提の破れ）");
    }

    /// <summary>潜在量は組ごとに1回：買い手の容量が尽きた行でも、同じ不足を次善の売り手ごとに数え直さない（作者裁定 2026-09-29 の2）＝行の潜在量は不足（X）を超えない。</summary>
    [Fact]
    public void 潜在量は同じ不足を売り手の数だけ重ねない()
    {
        // 盤面＝英の1国：鉄道と基礎の陸の容量を0にすると内陸の州は運べない（港の州は船で運べる）。売り手は港の2州（どちらも出す量が厚い）。
        //   内陸の買い手の行＝有効な売り手が2人・量は容量で0に切られる＝旧は同じ不足（X）を2人の売り手で2回数えた。
        var (sim, g, seller, seller2, _) = Make(WorldData.ByName("イギリス"));
        var w = sim.Nations[0].World;
        sim.Coef.TransportBaseline = 0; sim.Coef.PortCapToThroughput = 0; w.RailCapacity = 0;
        foreach (var r in w.Regions) foreach (var m in r.Markets)   // ほかの財は欲しがらない（潜在量を財 g だけにする）
            if (!ReferenceEquals(m, r.Markets[g])) { m.LastDemand = 0; m.TargetStock = 0; m.LastProduced = 0; }
        foreach (var sr in new[] { seller, seller2 }) { var m = sr.Markets[g]; m.Stock = 5000; m.TargetStock = 10; m.LastDemand = 10; m.LastProduced = 5000; }
        seller.Markets[g].Stock = 110;   // 最安の売り手は薄い（出せる量100＜不足）＝次善の売り手へ回る＝旧は次善でも同じ不足を丸ごと数え直した
        foreach (var r in w.Regions) if (r.MerchantPopIndex >= 0) r.Pops[r.MerchantPopIndex].Savings = 1e9;   // 捌き手の支払能力で量が切られない（切るのは容量だけ）
        foreach (var r in w.Regions)   // 買い手は内陸の州だけ（港の州は欲しがらない＝最安の売り手の在庫を先に取らない）・量は物理の不足（550）で決まる（価格の量＝需要×参照÷単価 が十分に大きい）
            if (!ReferenceEquals(r, seller) && !ReferenceEquals(r, seller2))
            { var m = r.Markets[g]; if (r.IsPort) { m.LastDemand = 0; m.TargetStock = 0; m.Stock = 0; } else { m.LastDemand = 5000; m.TargetStock = 50; m.Stock = 4500; } }
        double wg = Catalog.TransportWeight(g);
        var need = new Dictionary<Region, double>();
        foreach (var r in w.Regions)
            if (r.State == RegionState.Active && !r.IsPort && !ReferenceEquals(r, seller) && !ReferenceEquals(r, seller2))
            { var m = r.Markets[g]; need[r] = m.TargetStock + m.LastDemand - m.LastProduced - m.Stock; }
        Assert.True(need.Count > 0, "英に内陸の買い手の州が無い（テスト前提の破れ）");
        double acc = w.ExternalTradeAccess();   // 買い手の量 X＝入手率×min(価格の量, 不足)＝行の潜在量の上限
        sim.Market.Step(sim.Nations, sim.Coef);
        double pot = 0, got = 0;
        foreach (var (r, x) in need)
        {
            Assert.True(r.LastTradePotential <= acc * x * wg * (1 + 1e-9), $"{r.Name}：潜在量 {r.LastTradePotential} が入手率×不足×重量 {acc * x * wg} を超えた＝同じ不足を売り手ごとに数え直した");
            pot += r.LastTradePotential; got += r.Markets[g].LastImport;
        }
        Assert.True(pot > 1e-9, "運べない買い手の潜在量が0（行を飛ばしている＝翌月の容量の配分に不足が届かない）");
        Assert.Equal(0.0, got);
    }

    /// <summary>国際市場の間引き（作者裁定 2026-09-29 の3）：前の清算で成約の無かった行は、四半期の最初の清算でないと回らない。
    /// その月に足りなかった（飢饉）行と、四半期の最初の清算（値段の差が運賃を超えたかを確かめる）では起きる。</summary>
    [Fact]
    public void 眠っている組は四半期の最初の清算か不足で起きる()
    {
        static double Imported(MultiNationSim sim, int g) { double s = 0; foreach (var n in sim.Nations) foreach (var r in n.World.Regions) if (r.State == RegionState.Active) s += r.Markets[g].LastImport; return s; }
        static void Sleep(MultiNationSim sim, int g) { foreach (var n in sim.Nations) foreach (var r in n.World.Regions) { r.Markets[g].PrevImport = 0; r.Markets[g].LastShortage = 0; } }

        var (a, ga, _, _, _) = Make(); Sleep(a, ga);
        a.Market.Step(a.Nations, a.Coef, firstClearingOfQuarter: false);
        Assert.Equal(0.0, Imported(a, ga));   // 眠っている行は月の途中の清算で回らない

        var (b, gb, _, _, _) = Make(); Sleep(b, gb);
        Region? hungry = null;
        foreach (var n in b.Nations) foreach (var r in n.World.Regions) if (hungry is null && r.State == RegionState.Active && r.Markets[gb].LastDemand >= 500) hungry = r;
        Assert.True(hungry is not null, "買い手の州が無い（テスト前提の破れ）");
        hungry!.Markets[gb].LastShortage = 100;   // この月に足りなかった＝起きる
        b.Market.Step(b.Nations, b.Coef, firstClearingOfQuarter: false);
        Assert.True(hungry.Markets[gb].LastImport > 1e-9, "足りなかった行が月の途中の清算で起きなかった（飢饉の輸入が遅れる）");
        Assert.Equal(hungry.Markets[gb].LastImport, Imported(b, gb), 9);   // ほかの眠っている行は回らない

        var (c, gc, _, _, _) = Make(); Sleep(c, gc);
        c.Market.Step(c.Nations, c.Coef, firstClearingOfQuarter: true);
        Assert.True(Imported(c, gc) > 1e-9, "四半期の最初の清算で眠っている行が起きなかった");
    }

    /// <summary>金建ての成約を各国の通貨へ換算する（A7）。国ごとに基礎貨幣の動き＝世界の台帳の動き（D が閉じる）、
    /// 世界では金建てで閉じ（Σ 台帳の動き×為替＝0）、名目の差は為替の換算の台帳（清算所）に入る。</summary>
    [Fact]
    public void 為替で換算して国ごとにも世界でも保存する()
    {
        var (sim, g, _, _, _) = Make();
        var gen = sim.Nations[0].World; var tus = sim.Nations[1].World;
        gen.ExchangeRate = 0.5;   // ジェノヴァの通貨は金の半分の値打ち
        double bm0 = gen.BaseMoney + tus.BaseMoney, tfg0 = gen.CumulativeTradeFlow, tft0 = tus.CumulativeTradeFlow, bmg0 = gen.BaseMoney, bmt0 = tus.BaseMoney, fx0 = sim.Market.CumulativeFxResidual;
        sim.Market.Step(sim.Nations, sim.Coef);
        double dg = gen.CumulativeTradeFlow - tfg0, dt = tus.CumulativeTradeFlow - tft0;
        Assert.True(Math.Abs(dg) > 1e-9 && gen.LastExportValue > 1e-9, "越境の取引が立っていない（テスト前提の破れ）");
        Assert.Equal(dg, gen.BaseMoney - bmg0, 6);   // 国ごとの D が閉じる
        Assert.Equal(dt, tus.BaseMoney - bmt0, 6);
        Assert.True(Math.Abs(dg * 0.5 + dt * 1.0) <= 1e-9 * Math.Max(1.0, Math.Abs(dt)), $"金建てで世界が閉じない（{dg * 0.5 + dt}）＝換算していない");
        Assert.True(Math.Abs(sim.Market.CumulativeFxResidual - fx0) > 1e-9, "為替が違うのに換算の台帳が動かない");
        Assert.Equal(sim.Market.CumulativeFxResidual - fx0, (gen.BaseMoney + tus.BaseMoney) - bm0, 6);   // 世界の名目の動き＝換算の台帳
    }
}
