using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>裁定流量の価格差感応化（2026-08-17・docs/202608/20260817_裁定流量の価格差感応化_完了.md）の番人。
///
/// 診断（鋼市場診断β／8/15「残る律速は速度」）＝売り手が動かせる量は アクセス×TradeSpeed×規模 だけで
/// **価格差を1文字も見ない**＝国内が値崩れしても世界へ出る速さが変わらない（ダンピングで世界価格を下げられない）。
///
/// ここは <see cref="Coefficients.TradeArbitrageMax"/> の **ON経路** を測る。既定0のビット恒等は既存フルスイートが番人
/// （新コードは全て `arbOn` 分岐の内側＝0では1命令も通らない）ので、この場では
/// (a)既定値 (b)休眠側の「定数蓋」そのもの＋点火で増えること (c)自己有界 (d)金と財の保存 (e)決定論と「点火が世界を動かす」ことを固定する。
/// ⚠OFF側の対照は既定に寄りかからず**明示的に0へピン**する（将来点火したとき対照が無言で空洞化しない＝2026-08-16の作法）。
/// </summary>
public class TradeArbitrageTests
{
    // 地中海の小国2つ＝港州があり距離が近い（輸送マージンで取引が落ちない）。実データの州構成に依存しないよう、
    // 州数は数えず「売り手1州（港）＋残り全部が買い手」という置き方だけを使う。
    private static NationConfig[] TwoPorts() => [WorldData.ByName("ジェノヴァ"), WorldData.ByName("トスカーナ")];

    private sealed class Bench
    {
        public required MultiNationSim Sim;
        public required int Good;
        public required Region Seller;
        public required List<Region> Buyers;
    }

    /// <summary>合成盤：売り手1州（世界平均よりはるかに安い・在庫は事実上無限・**規模は小さい**）×買い手多数（高値・需要は事実上無限）。
    /// 売り手の規模を小さく取るので `left = min(在庫, 流量)` は必ず**流量**側で決まる＝
    /// 「流量の蓋が価格差を見るか」だけを分離して測れる（買い手の q は蓋の数百倍あるので買い手側は律速しない）。</summary>
    private static Bench MakeBench(double sellerPriceFrac, double arbMax, double sellerScale = 1000.0)
    {
        var sim = new MultiNationSim(TwoPorts(), seed: 1789) { DiplomacyFrozen = true };
        sim.Coef.TradeArbitrageClearing = 0;   // ★2026-08-30 S4点火後＝このファイルは**旧経路（定数速度×規模）の番人**として恒久ピン（新経路の番人は TradeClearingTests）
        sim.Coef.TradeArbitrageMax = arbMax;   // OFF側も明示ピン（呼び出し側が0を渡す）
        var gd = sim.Nations[0].World.Region.Goods
            .First(g => !g.IsColonial && !g.NonTradeable && g.Tier != GoodTier.Staple && !g.GrainChain
                        && Array.IndexOf(sim.Coef.MilitaryExportGoods, g.Id) < 0);
        double wp = sim.Market.Price[gd.Id];
        var all = new List<Region>();
        foreach (var n in sim.Nations)
        {
            n.World.Tariff = 0;                                        // 関税wedgeを黙らせる（測るのは量だけ）
            n.World.RailCapacity = 1e9; n.World.ShipCapacity = 1e9;    // 容量律速を外す
            foreach (var r in n.World.Regions) if (r.State == RegionState.Active) all.Add(r);
        }
        var seller = all.First(r => r.IsPort);
        foreach (var r in all)
        {
            var m = r.Markets[gd.Id];
            m.LastDemand = 0; m.LastProduced = 0; m.LastExport = 0; m.LastImport = 0;   // scaleS/scaleT を TargetStock だけで決める
            if (ReferenceEquals(r, seller)) { m.Price = sellerPriceFrac * wp; m.Stock = 1e6; m.TargetStock = sellerScale; }
            else { m.Price = 2.0 * wp; m.Stock = 0; m.TargetStock = 1e6; }
        }
        return new Bench
        {
            Sim = sim,
            Good = gd.Id,
            Seller = seller,
            Buyers = [.. all.Where(r => !ReferenceEquals(r, seller))],
        };
    }

    /// <summary>1回だけ清算して、売り手が実際に動かせた量を返す。</summary>
    private static double ExportOnce(Bench b)
    {
        b.Sim.Market.Step(b.Sim.Nations, b.Sim.Coef);
        double q = b.Seller.Markets[b.Good].LastExport;
        Assert.True(q > 0, "合成盤で取引が1件も立たなかった（テスト前提の破れ）");
        return q;
    }

    // ============ (a) 既定は休眠＝0 ============

    [Fact]
    public void 裁定流量の価格差感応は既定で休眠している()
    {
        // ⚠POB_COEF で明示的に上書きした測定走行では見ない（点火A/Bを同一バイナリで取るため）。
        // ★2026-08-29：既定を4.0へ点火（作者一任の輸送コスト是正＝貿易0.12→0.15%・政体+0.5・列強+0.8・硬性全緑）。
        //   本テストの検証対象は「既定が意図した値であること」＝点火後はその値を番人する（0の休眠アサートは点火の宣言と同時に更新）。
        var c = new Coefficients();
        if (!Coefficients.OverrideSummary().Contains("TradeArbitrageMax", StringComparison.Ordinal))
            Assert.Equal(4.0, c.TradeArbitrageMax);
    }

    // ============ (b) 休眠＝定数蓋そのもの／点火＝安い産地ほど速く出る ============

    [Fact]
    public void 休眠では価格差が5桁変わっても売り手の流量は動かない()
    {
        // これが診断βの本体＝**売り手の蓋は価格を見ていない**。旧挙動の姿を明示的に固定しておく
        //（点火後にこの番人が赤くなったら、それは0の恒等が壊れたということ）。
        double mid = ExportOnce(MakeBench(sellerPriceFrac: 0.5, arbMax: 0.0));
        double deep = ExportOnce(MakeBench(sellerPriceFrac: 1e-5, arbMax: 0.0));
        Assert.True(Math.Abs(deep - mid) <= 1e-9 * mid,
            $"休眠なのに価格差で流量が動いた（半値={mid:F6} / 十万分の一={deep:F6}）");
    }

    [Fact]
    public void 点火すると世界より安い産地ほど速く出す()
    {
        const double arb = 4.0;   // 上限＝5倍
        double off = ExportOnce(MakeBench(0.5, 0.0));
        double onMid = ExportOnce(MakeBench(0.5, arb));
        double onCheap = ExportOnce(MakeBench(0.05, arb));
        Assert.True(onMid > off * 1.5, $"半値の産地で流量が増えない（休眠{off:F3}→点火{onMid:F3}）");
        Assert.True(onCheap > onMid * 1.5, $"より安い産地のほうが速く出ていない（半値{onMid:F3} / 二十分の一{onCheap:F3}）");
        Assert.True(onCheap <= off * (1.0 + arb) * (1.0 + 1e-9), $"上限倍率（{1 + arb}倍）を超えた（{onCheap / off:F3}倍）");
    }

    // ============ (c) 自己有界＝壁ではなく逓減 ============

    [Fact]
    public void 価格差が極端でも流量は上限倍率へ漸近して発散しない()
    {
        const double arb = 4.0;
        double off = ExportOnce(MakeBench(0.05, 0.0));
        double cheap = ExportOnce(MakeBench(0.05, arb));       // 世界平均の約1/20
        double collapse = ExportOnce(MakeBench(1e-6, arb));    // 値崩れ＝世界平均の百万分の一
        Assert.True(double.IsFinite(collapse) && collapse > 0, "極端な価格差で流量が非有限/消滅");
        Assert.True(collapse <= off * (1.0 + arb) * (1.0 + 1e-9),
            $"自己有界が効いていない（{collapse / off:F3}倍 > {1 + arb}倍）");
        // 価格が5万倍安くなっても流量は1.25倍未満＝x/(1+x/上限) の逓減（clampの壁なら段差が出るところ）
        Assert.True(collapse < cheap * 1.25,
            $"逓減していない（1/20で{cheap / off:F3}倍・百万分の一で{collapse / off:F3}倍）");
    }

    // ============ (d) 金と財の保存 ============

    [Fact]
    public void 点火経路でも売り手の減と買い手の増が一致し金が湧かない()
    {
        var b = MakeBench(0.05, 4.0);
        double D(World w) => w.BaseMoney - w.CumulativeTradeFlow - w.CumulativeGoldMinted - w.CumulativeFiatIssued;
        var d0 = b.Sim.Nations.Select(n => D(n.World)).ToArray();
        double sellerBefore = b.Seller.Markets[b.Good].Stock;
        double buyersBefore = b.Buyers.Sum(r => r.Markets[b.Good].Stock);

        double q = ExportOnce(b);

        double sellerDrop = sellerBefore - b.Seller.Markets[b.Good].Stock;
        double buyersGain = b.Buyers.Sum(r => r.Markets[b.Good].Stock) - buyersBefore;
        Assert.True(Math.Abs(sellerDrop - q) < 1e-9 * q, $"輸出量と在庫の減りが一致しない（{q:F6} vs {sellerDrop:F6}）");
        Assert.True(Math.Abs(buyersGain - sellerDrop) < 1e-9 * Math.Max(1.0, sellerDrop),
            $"財が湧いた/消えた（売り手の減 {sellerDrop:F6} ≠ 買い手の増 {buyersGain:F6}）");
        Assert.True(Math.Abs(b.Buyers.Sum(r => r.Markets[b.Good].LastImport) - sellerDrop) < 1e-9 * Math.Max(1.0, sellerDrop),
            "輸入の記帳が実物の移動と合わない");
        for (int i = 0; i < d0.Length; i++)
        {
            var w = b.Sim.Nations[i].World;
            Assert.True(Math.Abs(D(w) - d0[i]) < 1e-6 * Math.Max(1.0, Math.Abs(w.BaseMoney)),
                $"{w.SelfPower!.Name}の保存ブラケットDが動いた（{D(w) - d0[i]:E2}）");
        }
    }

    // ============ (e) 決定論／点火が実際に世界を動かす ============

    [Fact]
    public void 点火した係数でも同一シードは同一の世界を生み点火が世界を動かす()
    {
        static MultiNationSim Play(double arb)
        {
            var sim = new MultiNationSim(
                [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], seed: 1789);
            sim.Coef.TradeArbitrageClearing = 0;   // ★2026-08-30 S4点火後＝arbMaxは新経路で不読＝旧経路ピンでレバーの意味を保つ
            sim.Coef.TradeArbitrageMax = arb;   // OFF側も明示ピン
            sim.Run(12);
            return sim;
        }
        static double Fingerprint(MultiNationSim s)
        {
            double f = 0;
            foreach (var n in s.Nations) f += n.World.BaseMoney + n.World.CumulativeTradeFlow * 7.0 + n.World.Region.Markets[0].Price * 13.0;
            return f;
        }
        var a = Play(2.0);
        var b = Play(2.0);
        Assert.Equal(a.Nations.Count, b.Nations.Count);
        for (int i = 0; i < a.Nations.Count; i++)
        {
            Assert.Equal(a.Nations[i].World.BaseMoney, b.Nations[i].World.BaseMoney);
            Assert.Equal(a.Nations[i].World.CumulativeTradeFlow, b.Nations[i].World.CumulativeTradeFlow);
            Assert.Equal(a.Nations[i].World.Region.Markets[0].Stock, b.Nations[i].World.Region.Markets[0].Stock);
        }
        // 「何もしない機構は何も壊さないので必ず緑になる」への番人＝点火した腕は休眠の腕と**違う世界**でなければならない
        Assert.NotEqual(Fingerprint(Play(0.0)), Fingerprint(a));
    }
}
