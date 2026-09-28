using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// テーマD（貨幣金融の価格・バランスシート層）の dormant 配線検証。
/// 既定（全係数0/恒等値）は現行恒等＝較正中立（142本体が担保）。有効化時の保存則を確認する。
/// 特に**不換シニョリッジ(FiatIssueFrac)はBaseMoneyを増やす唯一の新発行源**＝Invariantsのexpectedに CumulativeFiatIssued を算入した整合を直接検証する。
/// </summary>
public class MoneyThemeTests
{
    [Fact]
    public void テーマD_既定係数が方針どおり()   // 休眠復旧2026-07-08 第(iii)段：国債二次流通・Fisherデフレ側・福祉クランプを有効化（トリアージ文書の提案値）
    {
        var c = new Coefficients();
        // 金利doc B4（2026-07-30・点火）：借り手(投資家)と預金者(全pop)が別階級になり、金利が分配のレバーになる。
        // 金利doc B4（2026-07-30）＝**点火を試み、実測で差し戻した**。docの値(0.02/0.16＝年2〜10%)で `MobilityTests.教育市場は…` が
        // 190.3 →（1/8）23.7 →（満額）5.6 の構造的崩壊（seed60/61でも4.6＝軌道ノイズでない）。1/16 は単体で緑だが同クラス同時実行で22.4＝赤。
        // 診断＝利息が**残高ストック**に課され支払いが**借り手の現金ストック**から取られる（与信を投資に回した借り手ではクランプが常時binding）。
        // 機構の作り直し（課税ベースを当期利潤へ／不足分を債務へ繰延）を経て #17 較正で点火する。B3（除数 /30→/360 の是正）は恒等なので既に入っている。
        Assert.Equal(0.0, c.LoanSpread);
        Assert.Equal(0.0, c.PolicyRateLoanWeight);
        Assert.Equal(0.5, c.LolrReserveBoost);      // リアリティ群採用2026-07-18＝最後の貸し手(バジョット)
        Assert.Equal(0.5, c.BondLossConfWeight);    // 第(iii)段有効化（国債暴落→銀行信頼＝ドゥームループ）
        Assert.Equal(0.25, c.GoldCoverMin);         // 第(iv)段有効化2026-07-10（キュー1）：実測レンジ（健全帯中央0.52/p1 0.43・危機の谷≦0.21）→0.25＝下回り≈0.1%＝稀な兌換停止クライシス
        Assert.Equal(0.2, c.FiatIssueFrac);         // リアリティ群採用2026-07-18＝不換シニョリッジ
        Assert.Equal(-0.02, c.FisherDeflationFloor);          // 第(iii)段有効化（デフレ→低名目金利）
        // WelfarePensionClampToTreasury（破産国は福祉を払えない）は 2026-09-28 に削除＝福祉・年金は請求×国家の支払い率 r（支払いの一元化 段2-1）
    }

    [Fact]
    public void テーマD_全係数有効化でも貨幣保存と境界が保たれる()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        var c = sim.World.Coef;
        c.LoanSpread = 0.05; c.PolicyRateLoanWeight = 0.1;   // 民間貸出金利CF（pop→pop）
        c.LolrReserveBoost = 0.5;                            // LOLR準備拡張
        c.BondLossConfWeight = 0.3;                          // 国債二次流通の評価損
        c.GoldCoverMin = 0.5;                                // 兌換準備カバー率ゲート
        c.FiatIssueFrac = 0.5;                               // 不換シニョリッジ（BaseMoney増＝Invariants整合を試す）
        var ex = Record.Exception(() => sim.Run(200));
        Assert.Null(ex);   // §11.6 貨幣保存(基礎貨幣＝baseline+交易+鋳造+不換発行)・非負境界が全ティック保持＝シニョリッジの台帳算入が正しい
        Assert.True(sim.World.TotalPopulation > 0, "人口が消えた");
    }

    [Fact]
    public void 不換シニョリッジ_有効化で新規発行が積まれ貨幣保存が整合()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        sim.World.Policy.MonetaryTarget = 0;   // 不換に固定＝赤字を切り下げで埋める→シニョリッジが発火
        sim.World.BaseCoercion = 20000;                // 高治安維持力＝維持費が歳入を食う＝慢性赤字を強制（赤字なしでは devalue が発火しない）
        sim.World.Coef.FiatIssueFrac = 0.6;
        var ex = Record.Exception(() => sim.Run(240));
        Assert.Null(ex);   // 新発行ぶんをexpectedに算入できているか（破れれば MoneyConservation 例外）
        Assert.True(sim.World.CumulativeFiatIssued > 0, "不換新規発行(CumulativeFiatIssued)が積まれていない＝シニョリッジ経路が発火していない");
    }

    [Fact]
    public void 民間貸出金利_有効化でも純移転で貨幣保存が保たれる()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(9), new NullAgent());
        sim.World.Coef.LoanSpread = 0.08;   // 借り手→預金者の利息移転
        var ex = Record.Exception(() => sim.Run(200));
        Assert.Null(ex);   // 金利CFはpop→pop純移転＝BaseMoney不変（Invariants変更なしで保存）
    }

    /// <summary>★器 MintParitySells の ON/OFF 経路（2026-09-07）：密閉1国（国家世界のフランス1国・交易なし）を銀本位に置き、正の銀準備と平価の上の銀価格を与えて
    /// MintGold を1回だけ走らせる。ON＝準備−qty／市場在庫＋qty（同量）／買い手の貯蓄−qty×平価／CumulativeGoldMinted−同額＝
    /// 保存量 D＝BaseMoney−累積貿易−鋳造−不換 が不変（MoneyConservation と同じ恒等式）。既定0＝何も動かない（準備・在庫・貯蓄・台帳が同値）。</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void 造幣局の平価売り_準備が市場へ戻り保存量Dが不変(double gate)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1), new NullAgent());   // 国家世界の1国＝密閉（交易相手なし）・商人/投資家POPが立つ（FullChain の最小ビルダーは商人索引を持たない）
        var w = sim.World;
        Assert.True(w.Coef.MintPriceFloorGate > 0 && w.Coef.StandardMetalMintGate > 0, "前提＝床モードと本位金属が live");
        w.Coef.MintParitySells = gate;
        w.Coef.MintBuysAtMarket = 0;   // 隔離＝準備不足の市場買い（atMarket）を切る。ON の売りは atMarket の月に走らない設計なので、買い側が同じ州で発火すると器が測れない
        w.MonetaryLaw = MonetaryStandardLaw.Silver;
        var r = w.Region;
        foreach (var other in w.Regions)   // 隔離＝首都州以外の銀在庫を0にして州ループから外す（Stock<=1e-12 で買いも売りも通らない）＝数量が首都州1回ぶんに閉じる
            if (!ReferenceEquals(other, r) && other.GoodOf(Catalog.SilverId) is not null) other.Markets[Catalog.SilverId].Stock = 0;
        Assert.True(r.State == RegionState.Active && r.GoodOf(Catalog.SilverId) is not null, "前提＝首都州に銀市場がある");
        var m = r.Markets[Catalog.SilverId];
        double parity = Catalog.RefPrices[Catalog.SilverId];
        m.Price = parity * 1.5;   // 平価の上＝従来の造幣局は何もしない（片側）
        if (!(m.Stock > 1e-12)) m.Stock = 1.0;   // 州ループの在庫ゲートを通す（買い側は価格＞平価で必ず continue）
        w.SilverReserveQty = 100.0;
        int buyer = r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count ? r.MerchantPopIndex : r.InvestorPopIndex;
        Assert.True(buyer >= 0, "前提＝買い手（商人/投資家）がいる");
        var pop = r.Pops[buyer];
        pop.Savings = Math.Max(pop.Savings, 1000.0 * parity);   // 貯蓄側で縛られない＝数量は準備×率で決まる
        double D(World x) => x.BaseMoney - x.CumulativeTradeFlow - x.CumulativeGoldMinted - x.CumulativeFiatIssued;
        double d0 = D(w), reserve0 = w.SilverReserveQty, stock0 = m.Stock, sav0 = pop.Savings, minted0 = w.CumulativeGoldMinted, base0 = w.BaseMoney, imp0 = m.LastImport;
        var mint = typeof(Simulation).GetMethod("MintGold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(mint);
        mint!.Invoke(sim, null);
        Assert.True(Math.Abs(D(w) - d0) <= 1e-9 * Math.Max(1.0, Math.Abs(d0)), $"保存量Dが動いた {d0}→{D(w)}（gate={gate}）");
        double soldQty = reserve0 - w.SilverReserveQty;
        if (gate > 0)
        {
            double expectQty = reserve0 * Math.Clamp(w.Coef.MintFloorBuyRate, 0, 1);
            Assert.True(Math.Abs(soldQty - expectQty) <= 1e-9, $"準備の減少 {soldQty} ≠ 準備×率 {expectQty}");
            Assert.True(Math.Abs((m.Stock - stock0) - soldQty) <= 1e-9, "市場在庫の増分が準備の減少と一致しない");
            Assert.True(Math.Abs((m.LastImport - imp0) - soldQty) <= 1e-9, "LastImport の増分が準備の減少と一致しない（在庫恒等）");
            Assert.True(Math.Abs((sav0 - pop.Savings) - soldQty * parity) <= 1e-9, "買い手の支払いが qty×平価でない");
            Assert.True(Math.Abs((minted0 - w.CumulativeGoldMinted) - soldQty * parity) <= 1e-9, "鋳造台帳の逆記帳が qty×平価でない");
            Assert.True(Math.Abs((base0 - w.BaseMoney) - soldQty * parity) <= 1e-9 * Math.Max(1.0, base0), "基礎貨幣の減少が qty×平価でない");
        }
        else
        {
            Assert.Equal(reserve0, w.SilverReserveQty);
            Assert.Equal(stock0, m.Stock);
            Assert.Equal(sav0, pop.Savings);
            Assert.Equal(minted0, w.CumulativeGoldMinted);
        }
    }
}
