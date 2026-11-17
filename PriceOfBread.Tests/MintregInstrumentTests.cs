using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1168) 線 c115sb：造幣の床の効きの計器 mintreg_s（Instruments/InstrumentsMintreg.cs）の機構の番人。事前登録 measurements/m1168_20261116_prereg_sb.txt の T1〜T3。
/// 盤面＝密閉 1 国の仏・銀本位・銀の市場と商人を持つ州 2 つ（r・r2）・他の州の銀の在庫は 0。MintGold を反射で月ごとに呼び、月ごとに（本位・被覆・値段・在庫）を台本で置く。
/// 期待の件数は台本から手で数える（エンジンの出力・計器の出力から作らない）。
/// </summary>
[Collection("Instruments")]
public class MintregInstrumentTests
{
    private static (Simulation Sim, World W, Region R1, Region R2) Board()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1), new NullAgent());
        var w = sim.World;
        w.Coef.MintReserveTargetWeight = 1.0;
        w.Coef.MintParitySells = 0.0;
        w.Coef.MintBuysAtCapital = 0.0;   // 旗 0 の経路の番人（(1187) 点火・mintreg_s の模型は全ての州の旧の規則）
        w.MonetaryLaw = MonetaryStandardLaw.Silver;
        Region? a = null, b = null;
        foreach (var x in w.Regions)
            if (x.GoodOf(Catalog.SilverId) is not null && x.MerchantPopIndex >= 0 && x.MerchantPopIndex < x.Pops.Count)
            { if (a is null) a = x; else if (b is null) { b = x; break; } }
        Assert.True(a is not null && b is not null, "前提＝銀市場と商人を持つ州が 2 つある");
        foreach (var other in w.Regions)
            if (!ReferenceEquals(other, a) && !ReferenceEquals(other, b) && other.GoodOf(Catalog.SilverId) is not null) other.Markets[Catalog.SilverId].Stock = 0;
        return (sim, w, a!, b!);
    }

    private static void Mint(Simulation sim)
    {
        var mint = typeof(Simulation).GetMethod("MintGold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(mint);
        mint!.Invoke(sim, null);
    }

    /// <summary>今月の予算（試験が手で組む式）。</summary>
    private static double Budget(World w)
    {
        double coverTarget = w.Coef.GoldCoverMin.Value * w.Coef.GoldRejoinCoverMargin;
        return System.Math.Max(0.0, coverTarget * w.BroadMoney.Value - w.BaseMoney.Value) * w.Coef.MintReserveSellRatePerMonth.Value;
    }

    private static void Short(World w) => w.Coef.GoldCoverMin = new Share(0.99);   // 準備不足
    private static void Ok(World w) => w.Coef.GoldCoverMin = new Share(0.0001);    // 準備は足りている

    private static IDisposable Instrumented(string tables = "mintreg_s")
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_mr_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        Instruments.SelectTables(tables);
        return new Off(dir);
    }
    private sealed class Off(string dir) : IDisposable
    {
        public void Dispose() { Instruments.SelectTables(null); Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void T1_理由ごとの月の和は全ての月で台本の件数と一致する()
    {
        using var _ = Instrumented();
        var (sim, w, r1, r2) = Board();
        double parity = Catalog.RefPrices[Catalog.SilverId];
        double rate = w.Coef.MintFloorBuyRatePerMonth.Value;
        var m1 = r1.Markets[Catalog.SilverId]; var m2 = r2.Markets[Catalog.SilverId];

        // A 不換の月（2 州とも不換に数える）
        w.MonetaryLaw = MonetaryStandardLaw.Fiat; Ok(w); Mint(sim);
        w.MonetaryLaw = MonetaryStandardLaw.Silver;
        // B 在庫なしの月（被覆は足りている）
        Ok(w); m1.Stock = 0; m2.Stock = 0; m1.Price = parity * 1.5; m2.Price = parity * 1.5; Mint(sim);
        // C 平価超え・予算なし（被覆は足りている）
        Ok(w); m1.Stock = 100; m2.Stock = 100; Mint(sim);
        // D1 平価超え・予算あり（準備不足）・r1 だけ在庫＝r1 は市場で買い r2 は在庫なし
        Short(w); double budget = Budget(w);
        Assert.True(budget > 1e-6, $"前提が崩れた：準備不足の予算が立たない（{budget}）");
        m1.Stock = 0.3 * budget / m1.Price / rate; m2.Stock = 0; Mint(sim);
        // D2 平価超え・準備不足・2 州とも予算の 5 倍の在庫＝先の州が予算を使い切り後の州は予算が尽きる
        Short(w); budget = Budget(w);
        m1.Stock = 5.0 * budget / m1.Price / rate; m2.Stock = 5.0 * budget / m2.Price / rate; Mint(sim);
        // E 平価の下・被覆は足りている・2 州とも在庫＝床の買い
        Ok(w); m1.Price = parity * 0.5; m2.Price = parity * 0.5; m1.Stock = 100; m2.Stock = 100; Mint(sim);
        // G 平価の下・準備不足・2 州とも在庫＝床の買い（市場買いではない）
        Short(w); m1.Stock = 100; m2.Stock = 100; Mint(sim);

        var a1 = Instruments.MintregPeek(r1)!; var a2 = Instruments.MintregPeek(r2)!;
        int Sum(Func<Instruments.MintregAcc, int> f) => f(a1) + f(a2);
        Assert.Equal(14, Sum(a => a.Months));
        Assert.Equal(2, Sum(a => a.Fiat));
        Assert.Equal(0, Sum(a => a.NoGood));
        Assert.Equal(3, Sum(a => a.NoStock));      // B 2 件＋D1 の r2
        Assert.Equal(2, Sum(a => a.Above));        // C
        Assert.Equal(1, Sum(a => a.Gap));          // D2 の後の州
        Assert.Equal(6, Sum(a => a.WouldBuy));     // D1 1＋D2 1＋E 2＋G 2
        Assert.Equal(6, Sum(a => a.Bought));
        Assert.Equal(2, Sum(a => a.AtMarket));     // D1・D2 の先の州
        Assert.Equal(6, Sum(a => a.CoverShort));   // D1 2＋D2 2＋G 2（不換の月・B・C・E は被覆が足りている）
        Assert.Equal(0, Sum(a => a.MissBought));
        Assert.Equal(Sum(a => a.Months), Sum(a => a.Fiat + a.NoGood + a.NoStock + a.Above + a.Gap + a.WouldBuy));   // 理由ごとの月の和＝全ての月
        Assert.Equal(Sum(a => a.WouldBuy), Sum(a => a.Bought));
    }

    [Fact]
    public void T2_atMarketの月はcoverShortが正の月の部分集合()
    {
        using var _ = Instrumented();
        var (sim, w, r1, r2) = Board();
        double parity = Catalog.RefPrices[Catalog.SilverId];
        var m1 = r1.Markets[Catalog.SilverId]; var m2 = r2.Markets[Catalog.SilverId];
        m2.Stock = 0;
        // 被覆が足りて平価超え・在庫あり＝市場買いは立たない
        Ok(w); m1.Price = parity * 1.5; m1.Stock = 100; Mint(sim);
        Assert.Equal(0, Instruments.MintregPeek(r1)!.AtMarket);
        Assert.Equal(0, Instruments.MintregPeek(r1)!.CoverShort);
        // 準備不足で平価超え・在庫あり＝市場買いが立つ
        Short(w); m1.Price = parity * 1.5; m1.Stock = 100; Mint(sim);
        Assert.Equal(1, Instruments.MintregPeek(r1)!.AtMarket);
        // 被覆が足りて平価の下
        Ok(w); m1.Price = parity * 0.5; m1.Stock = 100; Mint(sim);
        // 準備不足で平価の下＝床の買いで市場買いではない
        Short(w); m1.Price = parity * 0.5; m1.Stock = 100; Mint(sim);
        var a1 = Instruments.MintregPeek(r1)!; var a2 = Instruments.MintregPeek(r2)!;
        Assert.Equal(4, a1.Months);
        Assert.Equal(1, a1.AtMarket);
        Assert.Equal(2, a1.CoverShort);
        Assert.True(a1.AtMarket <= a1.CoverShort && a2.AtMarket <= a2.CoverShort);
        Assert.Equal(0, a2.AtMarket);
    }

    [Fact]
    public void T3_計器が切れている時は何も溜めず軌道を変えない()
    {
        static double[] Script(bool ins, string tables)
        {
            IDisposable? scope = ins ? Instrumented(tables) : null;
            try
            {
                var (sim, w, r1, r2) = Board();
                double parity = Catalog.RefPrices[Catalog.SilverId];
                var m1 = r1.Markets[Catalog.SilverId]; var m2 = r2.Markets[Catalog.SilverId];
                Short(w); m1.Price = parity * 1.5; m1.Stock = 100; m2.Price = parity * 0.5; m2.Stock = 100; Mint(sim);
                Ok(w); m1.Stock = 50; Mint(sim);
                bool collected = Instruments.MintregPeek(r1) is not null;
                return [collected ? 1 : 0, w.BaseMoney.Value, w.SilverReserveQty, w.CumulativeGoldMinted.Value, m1.Stock, m2.Stock, m1.LastDemand, m2.LastDemand];
            }
            finally { scope?.Dispose(); }
        }
        var off = Script(false, "mintreg_s");
        var other = Script(true, "colqn_s");   // 別の表だけ選ぶ＝mintreg_s は切れている
        var on = Script(true, "mintreg_s");
        Assert.Equal(0.0, off[0]);
        Assert.Equal(0.0, other[0]);
        Assert.Equal(1.0, on[0]);
        for (int i = 1; i < off.Length; i++)
        {
            Assert.Equal(System.BitConverter.DoubleToInt64Bits(off[i]), System.BitConverter.DoubleToInt64Bits(on[i]));
            Assert.Equal(System.BitConverter.DoubleToInt64Bits(off[i]), System.BitConverter.DoubleToInt64Bits(other[i]));
        }
        Assert.True(off[3] > 0, "前提＝台本の月に造幣が実際に動いている");
    }
}
