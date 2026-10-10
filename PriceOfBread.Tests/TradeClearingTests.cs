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
        Assert.Equal(0.0, w.LastTariffRevenuePerClearing);
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
            return (gen.LastExportValue / q, tus.LastTariffRevenuePerClearing, tus.TreasuryLedger[(int)TreasuryCat.Tariff] - led0, gen.LastExportValue);
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
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) foreach (var p in r.Pops) before[p] = p.Savings.Value;
        sim.Market.Step(sim.Nations, sim.Coef);
        int checkedRegions = 0;
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                if (r.State != RegionState.Active || ReferenceEquals(r, seller) || ReferenceEquals(r, seller2) || !(r.Markets[g].LastImport > 1e-9)) continue;
                Assert.True(r.MerchantPopIndex >= 0, "前提が崩れた：買い手の州に商人が居ない");
                for (int i = 0; i < r.Pops.Count; i++)
                    if (i != r.MerchantPopIndex) Assert.Equal(before[r.Pops[i]], r.Pops[i].Savings.Value);   // 商人以外は払わない
                Assert.True(r.Pops[r.MerchantPopIndex].Savings.Value < before[r.Pops[r.MerchantPopIndex]], "輸入した州の商人の貯蓄が減っていない");
                checkedRegions++;
            }
        Assert.True(checkedRegions > 0, "輸入した買い手の州が無い（テスト前提の破れ）");
    }

    /// <summary>★段3-4（2026-09-30・C10＝処理の一元化 段4d）：港湾料と海上保険の保険料は**成約の時点**で輸入の捌き手（荷主＝買い手の州の商人）が払う
    /// （単価の上乗せ＝手形の振り出しも同じ額）。港湾料は海路の成約の港の端（港の州）の投資家＝港の持ち主へ・取扱高×率。保険料は輸入国の商人（引受手）へ頭数で。
    /// 荷主でも受け手でもない POP の貯蓄は1ビットも動かない（旧＝四半期末に国の全POP から貯蓄の比で取った）。
    /// 盤面＝ジェノヴァとトスカーナで財 g だけを取引する同じ清算を3つの双子で1回（率 0／保険料だけ／港湾料だけ）＝手形で量は同じ。
    /// ⚠突然変異「保険料を全POP から（旧）」「港湾料を取らない」「保険料を取らない」で赤。</summary>
    [Fact]
    public void 港湾料と保険料は荷主が成約の時点で払い港の持ち主と引受手が受け取る()
    {
        (MultiNationSim sim, int g, Region s1, Region s2) Run(double portRate, double premRate)
        {
            var (sim, g, seller, seller2, _) = Make();
            foreach (var n in sim.Nations) foreach (var r in n.World.Regions) foreach (var m in r.Markets)   // ほかの財は取引しない（払いを財 g だけにする）
                if (!ReferenceEquals(m, r.Markets[g])) { m.LastDemand = 0; m.TargetStock = 0; m.LastProduced = 0; }
            sim.Coef.PortFeeRate = portRate; sim.Coef.MarinePremiumRate = premRate;
            sim.Market.Step(sim.Nations, sim.Coef);
            return (sim, g, seller, seller2);
        }
        double portRate = new Coefficients().PortFeeRate, premRate = new Coefficients().MarinePremiumRate;
        Assert.True(portRate > 0 && premRate > 0, "前提＝港湾料と保険料の率が正");
        var off = Run(0, 0); var prem = Run(0, premRate); var port = Run(portRate, 0);
        static IEnumerable<(int n, Region r, int i, Pop p)> Pops(MultiNationSim s)
        {
            for (int n = 0; n < s.Nations.Count; n++)
                foreach (var r in s.Nations[n].World.Regions)
                    if (r.State == RegionState.Active) for (int i = 0; i < r.Pops.Count; i++) yield return (n, r, i, r.Pops[i]);
        }
        var o = Pops(off.sim).ToList(); var pp = Pops(prem.sim).ToList(); var pt = Pops(port.sim).ToList();
        Assert.Equal(o.Count, pp.Count); Assert.Equal(o.Count, pt.Count);
        Assert.Equal(0.0, off.s1.Markets[off.g].LastImport); Assert.Equal(0.0, off.s2.Markets[off.g].LastImport);   // 前提：売り手の港は買わない
        // 保険料だけ：払い手＝海路で輸入した州の商人（取扱高×率）・受け手＝その国の商人の頭数＝ほかの POP は動かない
        var premPool = new double[off.sim.Nations.Count]; var merSize = new double[off.sim.Nations.Count];
        foreach (var (n, r, i, p) in o)
        {
            if (i != r.MerchantPopIndex) continue;
            merSize[n] += p.Size;
            if (r.Markets[off.g].LastImport > 1e-12) premPool[n] += premRate * r.LastPortThroughput / off.sim.Nations[n].World.FxRate;
        }
        Assert.True(premPool.Sum() > 1e-9, "前提が崩れた：海路の輸入が立っていない");
        for (int k = 0; k < o.Count; k++)
        {
            var (n, r, i, p) = o[k];
            double d = pp[k].p.Savings.Value - p.Savings.Value;
            if (i != r.MerchantPopIndex) { Assert.Equal(p.Savings.Value, pp[k].p.Savings.Value); continue; }   // 荷主でも引受手でもない
            double paid = r.Markets[off.g].LastImport > 1e-12 ? premRate * r.LastPortThroughput / off.sim.Nations[n].World.FxRate : 0;
            double got = premPool[n] * p.Size / merSize[n];
            Assert.True(Math.Abs(d - (got - paid)) <= 1e-9 * Math.Max(1.0, Math.Abs(p.Savings.Value)), $"{r.Name} の商人：保険料の差し引き {d} ≠ 受け取り {got}−払い {paid}");
        }
        // 港湾料だけ：港の州の投資家が 取扱高×率 を受け取る・払うのは荷主の商人だけ＝ほかの POP は動かない・払いと受け取りは金の値打ちで閉じる
        double owners = 0, goldNet = 0;
        for (int k = 0; k < o.Count; k++)
        {
            var (n, r, i, p) = o[k];
            double fx = off.sim.Nations[n].World.FxRate, d = pt[k].p.Savings.Value - p.Savings.Value;
            goldNet += d * fx;
            if (r.IsPort && i == r.InvestorPopIndex)
            {
                double want = portRate * r.LastPortThroughput / fx;
                Assert.True(Math.Abs(d - want) <= 1e-9 * Math.Max(1.0, Math.Abs(p.Savings.Value)), $"{r.Name} の港の持ち主：港湾料 {d} ≠ 取扱高×率 {want}");
                owners += want;
            }
            else if (i != r.MerchantPopIndex) Assert.Equal(p.Savings.Value, pt[k].p.Savings.Value);
        }
        Assert.True(owners > 1e-9, "前提が崩れた：港湾料が立っていない（港の州の海路の成約が無い）");
        Assert.True(Math.Abs(goldNet) <= 1e-9 * Math.Max(1.0, owners), $"港湾料の払いと受け取りが金の値打ちで閉じない（{goldNet}）");
    }

    /// <summary>★為替手形（2026-09-29 S3b・補給の鎖の緩衝）：貯蓄の無い捌き手の商人も、積荷を担保に自国の銀行で手形を割り引いて輸入し
    /// （債務の記録・預金・家計の与信が同じ額＝国ごとの D が閉じる）、国内で売った代金で次の清算の始めに返す。
    /// 旧＝支払能力は商人の貯蓄だけ＝貯蓄の尽きた州は何十年も輸入できなかった（S3a）。⚠突然変異「手形なし（貯蓄だけ）」「返さない」で赤。</summary>
    [Fact]
    public void 貯蓄の無い商人も手形で輸入し売った代金で次の清算に返す()
    {
        var (sim, g, seller, seller2, _) = Make();
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) foreach (var m in r.Markets)   // ほかの財は欲しがらない（手形を財 g だけにする）
            if (!ReferenceEquals(m, r.Markets[g])) { m.LastDemand = 0; m.TargetStock = 0; m.LastProduced = 0; }
        var buyers = new List<Region>();
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
                if (r.State == RegionState.Active && !ReferenceEquals(r, seller) && !ReferenceEquals(r, seller2) && r.MerchantPopIndex >= 0)
                { r.Pops[r.MerchantPopIndex].SetSavings(0); buyers.Add(r); }
        Assert.True(buyers.Count > 0, "前提が崩れた：買い手の州に商人が居ない");
        var before = new Dictionary<Pop, double>();
        foreach (var r in buyers) foreach (var p in r.Pops) before[p] = p.Savings.Value;
        var bm0 = sim.Nations.ConvertAll(n => n.World.BaseMoney); var tf0 = sim.Nations.ConvertAll(n => n.World.CumulativeTradeFlow);
        sim.Market.Step(sim.Nations, sim.Coef);
        var drew = new List<Region>();
        foreach (var r in buyers)
        {
            var mer = r.Pops[r.MerchantPopIndex];
            for (int i = 0; i < r.Pops.Count; i++) if (i != r.MerchantPopIndex) Assert.Equal(before[r.Pops[i]], r.Pops[i].Savings.Value);   // 払うのは商人だけ
            if (r.Markets[g].LastImport > 1e-9) { Assert.True(mer.Debt.Value > 0, $"{r.Name}：輸入したのに手形の債務が立っていない"); drew.Add(r); }
        }
        Assert.True(drew.Count > 0, "貯蓄の無い商人の州が1つも輸入していない（手形を割り引けていない）");
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World; double debt = 0;
            foreach (var r in w.Regions) foreach (var p in r.Pops) debt += p.Debt.Value;
            double tol = 1e-9 * Math.Max(1.0, Math.Abs(bm0[i].Value) + debt);
            Assert.True(Math.Abs(w.Bank!.HouseholdLoans.Value - debt) <= tol, $"{w.SelfPower?.Name}：家計の与信 {w.Bank.HouseholdLoans} ≠ POP の債務の和 {debt}");
            Assert.True(Math.Abs((w.BaseMoney - bm0[i]).Value - (w.CumulativeTradeFlow - tf0[i])) <= tol, $"{w.SelfPower?.Name}：国ごとの D が閉じない（手形の貸しが基礎貨幣を動かした）");
        }
        // 売った代金が入った（同じ州の豊かな POP から商人へ＝貨幣は州の中で移るだけ）・買い手の在庫は満ちた（もう買わない）→ 次の清算の始めに返す
        var debts = new Dictionary<Pop, double>();
        foreach (var r in drew)
        {
            var mer = r.Pops[r.MerchantPopIndex]; debts[mer] = mer.Debt.Value;
            Pop rich = r.Pops[0]; foreach (var p in r.Pops) if (p.Savings.Value > rich.Savings.Value) rich = p;
            double proceeds = (2 * mer.Debt).Value; rich.SetSavings(rich.Savings.Value - (proceeds)); mer.SetSavings(mer.Savings.Value + (proceeds));
        }
        foreach (var r in buyers) r.Markets[g].Stock = 1e6;
        sim.Market.Step(sim.Nations, sim.Coef);
        foreach (var r in drew)
        {
            var mer = r.Pops[r.MerchantPopIndex];
            Assert.True(mer.Debt.Value == 0, $"{r.Name}：売った代金が入ったのに手形を返していない（債務 {mer.Debt}）");
            Assert.True(Math.Abs(mer.Savings.Value - debts[mer]) <= 1e-9 * Math.Max(1.0, debts[mer]), $"{r.Name}：返した後の貯蓄 {mer.Savings} ≠ 代金−手形 {debts[mer]}");
        }
        foreach (var n in sim.Nations) Assert.True(Math.Abs((n.World.Bank!.HouseholdLoans).Value) <= 1e-9 * Math.Max(1.0, debts.Values.Sum()), $"{n.World.SelfPower?.Name}：返した後も家計の与信が残る（{n.World.Bank.HouseholdLoans}）");
    }

    /// <summary>★為替手形の引受（2026-09-29 S3b）：前の手形を返し切れない商人は新しい手形を振り出せない（不渡りの商人は引き受けてもらえない）
    /// ＝支払能力は貯蓄だけ（0なら輸入しない）。天井でもタイマーでもない＝貸さない理由は返っていない手形。⚠突然変異「返していなくても振り出す」で赤。</summary>
    [Fact]
    public void 手形を返し切れない商人は新しい手形を振り出せない()
    {
        var (sim, g, seller, seller2, _) = Make();
        var buyers = new List<Region>();
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
                if (r.State == RegionState.Active && !ReferenceEquals(r, seller) && !ReferenceEquals(r, seller2) && r.MerchantPopIndex >= 0)
                {
                    var mer = r.Pops[r.MerchantPopIndex];
                    mer.SetSavings(0); mer.Debt = new MoneyStock(100); n.World.Bank!.HouseholdLoans += new MoneyStock(100);   // 前の清算の手形が返っていない（貯蓄0＝返せない）
                    buyers.Add(r);
                }
        Assert.True(buyers.Count > 0, "前提が崩れた：買い手の州に商人が居ない");
        sim.Market.Step(sim.Nations, sim.Coef);
        foreach (var r in buyers)
        {
            Assert.Equal(0.0, r.Markets[g].LastImport);
            Assert.Equal(100.0, r.Pops[r.MerchantPopIndex].Debt.Value);
        }
    }

    /// <summary>★(166) 鉄道以前の陸の容量は人口×基礎の目標（Transport.PreRailCapacity＝人口×TransportTargetPerPop）＝国ごとの定数ではない・清算の内陸の州の容量はこの1本を読む（呼び手の側）：
    /// 英の1国・鉄道0・船は十分（内陸の州の容量は陸で決まる）・港の容量の上乗せ0 → 内陸の州の容量（LastTransportCap）は基礎の目標を2倍にすると2倍・関数は人口×基礎の目標と同じ値。
    /// ⚠壊し方＝清算が国ごとの定数を足す（旧 TransportBaseline の形）・関数が人口を読まない → 赤。</summary>
    [Fact]
    public void 鉄道以前の陸の容量は人口に比例し清算の内陸の州の容量はそれを読む()
    {
        double[] InlandCaps(double perPop)
        {
            var (sim, _, _, _, _) = Make(WorldData.ByName("イギリス"));
            var w = sim.Nations[0].World;
            sim.Coef.TransportTargetPerPop = perPop; sim.Coef.PortCapToThroughput = 0; w.RailCapacity = 0; w.ShipCapacity = 1e12;
            Assert.Equal(w.TotalPopulation * perPop, Transport.PreRailCapacity(w, sim.Coef), 9);
            sim.Market.Step(sim.Nations, sim.Coef);
            return w.Regions.Where(r => r.State == RegionState.Active && !r.IsPort).Select(r => r.LastTransportCap).ToArray();
        }
        var a = InlandCaps(0.08); var b = InlandCaps(0.16);
        Assert.True(a.Length > 0, "英に内陸の州が無い（テスト前提の破れ）");
        Assert.True(a.All(x => x > 0), "内陸の州の容量が0（鉄道以前の陸の容量が清算に届いていない）");
        for (int i = 0; i < a.Length; i++) Assert.Equal(2.0, b[i] / a[i], 9);
    }

    /// <summary>潜在量は組ごとに1回：買い手の容量が尽きた行でも、同じ不足を次善の売り手ごとに数え直さない（作者裁定 2026-09-29 の2）＝行の潜在量は不足（X）を超えない。</summary>
    [Fact]
    public void 潜在量は同じ不足を売り手の数だけ重ねない()
    {
        // 盤面＝英の1国：鉄道と基礎の陸の容量を0にすると内陸の州は運べない（港の州は船で運べる）。売り手は港の2州（どちらも出す量が厚い）。
        //   内陸の買い手の行＝有効な売り手が2人・量は容量で0に切られる＝旧は同じ不足（X）を2人の売り手で2回数えた。
        //   ★(166) 鉄道以前の陸の容量＝人口×TransportTargetPerPop（Transport.PreRailCapacity・旧＝TransportBaseline）＝その係数を0にする。
        var (sim, g, seller, seller2, _) = Make(WorldData.ByName("イギリス"));
        var w = sim.Nations[0].World;
        sim.Coef.TransportTargetPerPop = 0; sim.Coef.PortCapToThroughput = 0; w.RailCapacity = 0;
        foreach (var r in w.Regions) foreach (var m in r.Markets)   // ほかの財は欲しがらない（潜在量を財 g だけにする）
            if (!ReferenceEquals(m, r.Markets[g])) { m.LastDemand = 0; m.TargetStock = 0; m.LastProduced = 0; }
        foreach (var sr in new[] { seller, seller2 }) { var m = sr.Markets[g]; m.Stock = 5000; m.TargetStock = 10; m.LastDemand = 10; m.LastProduced = 5000; }
        seller.Markets[g].Stock = 110;   // 最安の売り手は薄い（出せる量100＜不足）＝次善の売り手へ回る＝旧は次善でも同じ不足を丸ごと数え直した
        foreach (var r in w.Regions) if (r.MerchantPopIndex >= 0) r.Pops[r.MerchantPopIndex].SetSavings(1e9);   // 捌き手の支払能力で量が切られない（切るのは容量だけ）
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

    /// <summary>★月の清算は潜在量を書かず、買い手の容量が尽きた行を回さない（2026-09-30 (130) 計算量の予算・清算の眠りの切り分け）：
    /// 潜在量と律速率（州の輸送の容量の取り分・混雑の信号）は四半期の最初の清算（眠る行が無い）だけが書く。月の清算で、買い手の州の容量が尽きている行は
    /// 成約できない＝売り手を回らない（着値の充填が0）・印は走査して成約しなかった行と同じ（その時の自州価格）。
    /// ⚠突然変異「月の清算も潜在量を書く」「容量の尽きた行も回す」「回さない行に印を付けない」で赤。</summary>
    [Fact]
    public void 月の清算は潜在量を書かず容量の尽きた行を回さない()
    {
        // 盤面＝英の1国：鉄道と基礎の陸の容量を0にすると内陸の州は運べない（容量が尽きた行）。売り手は港の2州・内陸の州だけが欲しがる（潜在量の番人と同じ盤面）。
        //   ★(166) 鉄道以前の陸の容量＝人口×TransportTargetPerPop（Transport.PreRailCapacity）＝その係数を0にする。
        var (sim, g, seller, seller2, _) = Make(WorldData.ByName("イギリス"));
        var w = sim.Nations[0].World;
        sim.Coef.TransportTargetPerPop = 0; sim.Coef.PortCapToThroughput = 0; w.RailCapacity = 0;
        foreach (var r in w.Regions) foreach (var m in r.Markets)
            if (!ReferenceEquals(m, r.Markets[g])) { m.LastDemand = 0; m.TargetStock = 0; m.LastProduced = 0; }
        foreach (var sr in new[] { seller, seller2 }) { var m = sr.Markets[g]; m.Stock = 5000; m.TargetStock = 10; m.LastDemand = 10; m.LastProduced = 5000; }
        foreach (var r in w.Regions) if (r.MerchantPopIndex >= 0) r.Pops[r.MerchantPopIndex].SetSavings(1e9);
        var inland = new List<Region>();
        foreach (var r in w.Regions)
            if (!ReferenceEquals(r, seller) && !ReferenceEquals(r, seller2))
            {
                var m = r.Markets[g];
                if (r.IsPort) { m.LastDemand = 0; m.TargetStock = 0; m.Stock = 0; }
                else { m.LastDemand = 5000; m.TargetStock = 50; m.Stock = 4500; if (r.State == RegionState.Active) inland.Add(r); }
            }
        Assert.True(inland.Count > 0, "英に内陸の買い手の州が無い（テスト前提の破れ）");
        long Fills() => sim.CostTotals()[CostLedger.ClearingFills];
        long f0 = Fills();
        sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
        Assert.True(Fills() > f0, "四半期の最初の清算で容量の尽きた行が回っていない（テスト前提の破れ）");
        var pot = inland.ToDictionary(r => r, r => (r.LastTradePotential, r.LastTradeThrottle));
        Assert.True(inland.Any(r => r.LastTradePotential > 1e-9), "運べない買い手の潜在量が0（テスト前提の破れ）");
        foreach (var r in inland)
        {   // 月の清算で起きる行（前の月に輸入が無く不足だけ・自州価格が印を超えて上がった）・欲しい量も増やす（書けば潜在量が変わる）
            var m = r.Markets[g];
            Assert.Equal(0.0, m.LastImport);
            m.PrevImport = 0; m.LastShortage = 100; m.Price *= 1.01; m.LastDemand = 6000;
        }
        long f1 = Fills();
        sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: false);
        Assert.Equal(f1, Fills());   // 容量の尽きた行は売り手を回らない（着値の充填が0）
        foreach (var r in inland)
        {
            Assert.Equal(pot[r].LastTradePotential, r.LastTradePotential);   // 月の清算は潜在量を書かない
            Assert.Equal(pot[r].LastTradeThrottle, r.LastTradeThrottle);
            Assert.Equal(r.Markets[g].Price * w.FxRate, r.Markets[g].ClearFailLp);   // 印＝走査して成約しなかった行と同じ（今の自州価格）
            Assert.Equal(0.0, r.Markets[g].LastImport);
        }
        sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
        Assert.Contains(inland, r => r.LastTradePotential != pot[r].LastTradePotential);   // 四半期の最初の清算は書く（欲しい量が増えた分）
    }

    /// <summary>★不足で起きたが成約しない行を眠らせる（2026-09-30 計算量の予算 第2回・近似）：月の清算（四半期の2・3回目）で、不足だけで起きた行
    /// （前の月に輸入が無い）が前の走査で成約しなかったなら、自州価格がその時の値（Market.ClearFailLp）を超えて上がるまで眠る＝売り手が安くなっても月の途中では起きない
    /// （売り手の側は四半期の最初の清算が拾う）。自州価格が上がれば起きて買う。四半期の最初の清算は、買い手の行に入らない州の印も消す。
    /// ⚠突然変異「印を読まない」「印を立てない」「最初の清算で消さない」で赤。</summary>
    [Fact]
    public void 不足で起きたが成約しない行は自州価格が上がるまで眠る()
    {
        static double Imported(MultiNationSim sim, int g) { double s = 0; foreach (var n in sim.Nations) foreach (var r in n.World.Regions) if (r.State == RegionState.Active) s += r.Markets[g].LastImport; return s; }
        var (sim, g, seller, seller2, _) = Make(WorldData.ByName("イギリス"));   // 州の多い1国（買い手の州が2つ以上）
        double wp = sim.Market.Price[g];
        Region? hungry = null, idle = null;
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                if (r.State != RegionState.Active || ReferenceEquals(r, seller) || ReferenceEquals(r, seller2)) continue;
                r.Markets[g].PrevImport = 0; r.Markets[g].LastShortage = 0;
                if (hungry is null && r.Markets[g].LastDemand >= 500) hungry = r;
                else if (idle is null) idle = r;
            }
        Assert.True(hungry is not null && idle is not null, "買い手の州が2つ無い（テスト前提の破れ）");
        foreach (var sr in new[] { seller, seller2 }) sr.Markets[g].Price = 5.0 * wp;   // 売り手は買い手（2倍）より高い＝着値≧自州価格＝成約しない
        hungry!.Markets[g].LastShortage = 100;   // 不足だけで起きる行（前の月に輸入が無い）
        idle!.Markets[g].Price = 0; idle.Markets[g].ClearFailLp = 123;   // 買い手の行に入らない州（値段0）に古い印
        sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
        Assert.Equal(0.0, Imported(sim, g));   // 盤面の前提＝成約しない
        Assert.Equal(0.0, idle.Markets[g].ClearFailLp);   // 四半期の最初の清算は古い印を消す
        double failLp = hungry.Markets[g].ClearFailLp;
        Assert.True(failLp > 0, "走査して成約しなかった行に印が立たない");
        seller2.Markets[g].Price = 0.6 * wp;   // 売り手が安くなった（売り手の側の変化）
        double before = Imported(sim, g);
        sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: false);
        Assert.Equal(before, Imported(sim, g));   // 自州価格が上がっていない＝眠る（月の途中では起きない）
        hungry.Markets[g].Price *= 1.01;   // 自州価格が印の値を超えて上がる＝起きる
        sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: false);
        Assert.True(hungry.Markets[g].LastImport > 1e-9, "自州価格が上がった行が月の清算で起きなかった");
        Assert.Equal(0.0, hungry.Markets[g].ClearFailLp);   // 成約した行は印なし
    }

    /// <summary>金建ての成約を各国の通貨へ換算する（A7）。国ごとに基礎貨幣の動き＝世界の台帳の動き（D が閉じる）、
    /// 世界では金建てで閉じ（Σ 台帳の動き×為替＝0）、名目の差は為替の換算の台帳（清算所）に入る。</summary>
    [Fact]
    public void 為替で換算して国ごとにも世界でも保存する()
    {
        var (sim, g, _, _, _) = Make();
        var gen = sim.Nations[0].World; var tus = sim.Nations[1].World;
        gen.ExchangeRate = 0.5;   // ジェノヴァの通貨は金の半分の値打ち
        double bm0 = (gen.BaseMoney + tus.BaseMoney).Value, tfg0 = gen.CumulativeTradeFlow, tft0 = tus.CumulativeTradeFlow, bmg0 = gen.BaseMoney.Value, bmt0 = tus.BaseMoney.Value, fx0 = sim.Diplomacy.CumulativeFxResidual;
        sim.Market.Step(sim.Nations, sim.Coef);
        double dg = gen.CumulativeTradeFlow - tfg0, dt = tus.CumulativeTradeFlow - tft0;
        Assert.True(Math.Abs(dg) > 1e-9 && gen.LastExportValue > 1e-9, "越境の取引が立っていない（テスト前提の破れ）");
        Assert.Equal(dg, gen.BaseMoney.Value - bmg0, 6);   // 国ごとの D が閉じる
        Assert.Equal(dt, tus.BaseMoney.Value - bmt0, 6);
        Assert.True(Math.Abs(dg * 0.5 + dt * 1.0) <= 1e-9 * Math.Max(1.0, Math.Abs(dt)), $"金建てで世界が閉じない（{dg * 0.5 + dt}）＝換算していない");
        Assert.True(Math.Abs(sim.Diplomacy.CumulativeFxResidual - fx0) > 1e-9, "為替が違うのに換算の台帳が動かない");
        Assert.Equal(sim.Diplomacy.CumulativeFxResidual - fx0, (gen.BaseMoney + tus.BaseMoney).Value - bm0, 6);   // 世界の名目の動き＝換算の台帳
    }
}
