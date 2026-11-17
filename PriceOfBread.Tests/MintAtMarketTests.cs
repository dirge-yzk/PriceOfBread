using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(854) 線 c70id：造幣局の市場買い（<c>Simulation.MintGold</c> の <c>atMarket</c> の枝・旗は (1168) で焼き込んだ）を踏む番人。
/// (846) の反証役が名乗った穴：FullChainTests の鋳造の番人の盤面（金本位・400 ティック）の鋳造は自由鋳造の床の枝で起きていて、市場買いの枝を外しても緑（measurements/m846_20261105/mut_summary.txt の MF3）。
/// 盤面＝密閉 1 国の仏・銀本位。銀の値段を平価の上に置き、基礎貨幣のカバー率を目標の下に置く（係数 GoldCoverMin を大きく置く＝貸出や国庫を触らずに準備不足が立つ）。MintGold を反射で 1 回だけ走らせる（MoneyThemeTests の造幣局の平価売りと同じ型）。
/// 期待は試験が自分で数える：予算 gapLeft＝max(0, 目標カバー率×広義貨幣−基礎貨幣)×月次率・数量＝min(在庫×床と同じ月次率, gapLeft÷市場価格)・鋳造価値＝数量×**市場価格**（平価でない）。
/// 突然変異の表は measurements/m854_20261105/mutation_854.txt。</summary>
public class MintAtMarketTests
{
    private static (Simulation Sim, World W, Region R, Market M, double Parity) Board()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1), new NullAgent());   // 国家世界の 1 国＝密閉（交易相手なし）・商人 POP が立つ
        var w = sim.World;
        w.Coef.MintReserveTargetWeight = 1.0;          // 前提を既定に頼らず明示（準備不足に応じて買う）
        w.Coef.MintParitySells = 0.0;                  // 隔離＝平価の売りを切る（準備不足の月は売らない設計だが、買いの試験に混ぜない）
        w.Coef.GoldCoverMin = new Share(0.99);         // 目標カバー率＝0.99×余白＞1＞実際のカバー率＝準備不足（貸出や国庫を触らない）
        w.MonetaryLaw = MonetaryStandardLaw.Silver;
        var r = w.Region;
        foreach (var other in w.Regions)               // 隔離＝首都州以外の銀在庫を 0 にして州ループを 1 州に閉じる
            if (!ReferenceEquals(other, r) && other.GoodOf(Catalog.SilverId) is not null) other.Markets[Catalog.SilverId].Stock = 0;
        Assert.True(r.GoodOf(Catalog.SilverId) is not null, "前提＝首都州に銀市場がある");
        Assert.True(r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count, "前提＝首都州に商人がいる（受け手不在で買わない月にならない）");
        var m = r.Markets[Catalog.SilverId];
        double parity = Catalog.RefPrices[Catalog.SilverId];
        m.Price = parity * 1.5;                        // 平価の上＝自由鋳造の床の枝は動かない（市場買いだけが立つ）
        return (sim, w, r, m, parity);
    }

    /// <summary>試験が自分で組む今月の予算（Simulation.MintGold の gapLeft と同じ式を、係数と盤面の量から手で組む）。</summary>
    private static double Budget(World w)
    {
        double coverTarget = w.Coef.GoldCoverMin.Value * w.Coef.GoldRejoinCoverMargin;
        return System.Math.Max(0.0, coverTarget * w.BroadMoney.Value - w.BaseMoney.Value) * w.Coef.MintReserveSellRatePerMonth.Value;
    }

    private static void Mint(Simulation sim)
    {
        var mint = typeof(Simulation).GetMethod("MintGold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(mint);
        mint!.Invoke(sim, null);
    }

    private static double D(World x) => x.BaseMoney.Value - x.CumulativeTradeFlow.Value - x.CumulativeGoldMinted.Value - x.CumulativeFiatIssued.Value;

    [Theory]
    [InlineData(0.01)]   // 在庫で縛る（在庫×月次率が予算÷値段より小さい）
    [InlineData(8.0)]    // 予算で縛る（在庫×月次率が予算÷値段の 8 倍）
    public void 準備が足りない月は平価の上でも市場で買う_鋳造の価値は市場価格_保存量Dは動かない(double stockOverBudgetQty)
    {
        var (sim, w, r, m, parity) = Board();
        double rate = w.Coef.MintFloorBuyRatePerMonth.Value;
        double budget = Budget(w);
        Assert.True(budget > 1e-6, $"前提が崩れた：準備不足の予算が立たない（{budget}）");
        double budgetQty = budget / m.Price;
        m.Stock = stockOverBudgetQty * budgetQty / rate;
        double stockQty = m.Stock * rate;
        double expectQty = System.Math.Min(stockQty, budgetQty);
        Assert.Equal(stockOverBudgetQty < 1.0, stockQty < budgetQty);   // 前提：行ごとに縛る側が決まっている
        double reserve0 = w.SilverReserveQty, stock0 = m.Stock, sold0 = m.LastSold, minted0 = w.CumulativeGoldMinted.Value, d0 = D(w), price0 = m.Price;
        Mint(sim);
        double qty = w.SilverReserveQty - reserve0;
        Assert.True(qty > 0, "市場買いが立っていない（準備が増えない）");
        Assert.Equal(expectQty, qty, 9);                                               // 数量＝在庫と予算の小さい方
        Assert.Equal(stock0 - qty, m.Stock, 9);                                         // 在庫は買った分だけ減る
        Assert.Equal(sold0 + qty, m.LastSold, 9);                                       // 造幣局への引き取り（LastSold＋）
        Assert.Equal(price0, m.Price);                                                  // 鋳造は値段を書かない
        double value = w.CumulativeGoldMinted.Value - minted0;
        Assert.Equal(qty * price0, value, 6);                                           // 鋳造の価値＝数量×市場価格（平価建てでない）
        Assert.True(System.Math.Abs(value - qty * parity) > 1e-6 * value, "前提が崩れた：市場価格と平価が同じで価値を見分けられない");
        Assert.True(System.Math.Abs(D(w) - d0) <= 1e-9 * System.Math.Max(1.0, System.Math.Abs(d0)), $"保存量Dが動いた {d0}→{D(w)}");
    }

    /// <summary>(861) MM7（(854) の反証役 中 1）：今月の予算 gapLeft は州をまたいで 1 本で消費される（<c>if (atMarket) gapLeft -= value;</c>）。
    /// 銀を持つ州が 2 つ以上あり、予算が 1 州の在庫の容量を超える盤面＝片方の州は在庫で縛られ（容量＝予算の 0.3 倍）、もう片方は予算を超える在庫（容量＝予算の 5 倍）を持つ。
    /// 期待は試験が自分で数える：今月の引き取りの価値の合計＝Budget(w)（州の順番に依らない＝小さい州が先なら 0.3＋0.7・大きい州が先なら 1.0＋0）。予算の消費を外すと 2 州目が予算を丸ごと使い 1.3 倍になる。</summary>
    [Fact]
    public void 市場買いの予算は州をまたいで1本で消費される_2州目は残りだけ買う()
    {
        var (sim, w, r, m, parity) = Board();
        w.Coef.MintBuysAtCapital = 0.0;               // 旗 0 の経路の番人（(1187) 点火＝旗 1 の経路は MintcapInstrumentTests の T2・T4）
        Region? r2 = null;
        foreach (var x in w.Regions)
            if (!ReferenceEquals(x, r) && x.GoodOf(Catalog.SilverId) is not null && x.MerchantPopIndex >= 0 && x.MerchantPopIndex < x.Pops.Count) { r2 = x; break; }
        Assert.True(r2 is not null, "前提＝銀市場と商人（受け手）を持つ州が 2 つある");
        var m2 = r2!.Markets[Catalog.SilverId];
        m2.Price = m.Price;                              // 平価の上（市場買いだけが立つ）・2 州とも同じ値段で数量と価値を比べやすくする
        double rate = w.Coef.MintFloorBuyRatePerMonth.Value;
        double budget = Budget(w);
        Assert.True(budget > 1e-6, $"前提が崩れた：準備不足の予算が立たない（{budget}）");
        double budgetQty = budget / m.Price;
        m.Stock = 0.3 * budgetQty / rate;                // 在庫で縛る（容量＝予算の 0.3 倍）
        m2.Stock = 5.0 * budgetQty / rate;               // 予算で縛る（容量＝予算の 5 倍）
        double cap1 = m.Stock * rate, cap2 = m2.Stock * rate;
        Assert.True(cap1 < budgetQty && cap2 > budgetQty && cap1 + cap2 > budgetQty, "前提が崩れた：片方は在庫で縛られ、もう片方は予算を超える");
        double stock1 = m.Stock, stock2 = m2.Stock, price0 = m.Price, minted0 = w.CumulativeGoldMinted.Value, d0 = D(w), reserve0 = w.SilverReserveQty;
        Mint(sim);
        double q1 = stock1 - m.Stock, q2 = stock2 - m2.Stock;
        Assert.True(q1 >= 0 && q2 >= 0 && q1 + q2 > 0, "市場買いが立っていない");
        Assert.True(q1 <= cap1 * (1 + 1e-9) && q2 <= cap2 * (1 + 1e-9), "各州の引き取りは自分の在庫×月次率を超えない");
        Assert.Equal(budget, (q1 + q2) * price0, 6);                                                // 州をまたいで予算を 1 本で使い切る（二重に使わない）
        Assert.Equal(budgetQty, w.SilverReserveQty - reserve0, 9);                                   // 準備の増え＝予算÷市場価格
        Assert.Equal(budget, w.CumulativeGoldMinted.Value - minted0, 6);                             // 鋳造の価値の合計＝予算
        Assert.True(System.Math.Abs(D(w) - d0) <= 1e-9 * System.Math.Max(1.0, System.Math.Abs(d0)), $"保存量Dが動いた {d0}→{D(w)}");
    }

    [Fact]
    public void 準備が足りていれば市場買いは立たない_予算が0()
    {
        var (sim, w, r, m, parity) = Board();
        w.Coef.GoldCoverMin = new Share(0.0001);           // 目標カバー率が実際のカバー率を下回る＝準備は足りている
        Assert.Equal(0.0, Budget(w));
        m.Stock = 100.0;
        double reserve0 = w.SilverReserveQty, stock0 = m.Stock, minted0 = w.CumulativeGoldMinted.Value;
        Mint(sim);
        Assert.Equal(reserve0, w.SilverReserveQty);
        Assert.Equal(stock0, m.Stock);
        Assert.Equal(minted0, w.CumulativeGoldMinted.Value);
    }
}
