using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1180) 線 c117b0：造幣の買いの置き場の計器 mintcap_s（Instruments/InstrumentsMintcap.cs）と旗 MintBuysAtCapital の器の機構の番人。事前登録 measurements/m1180_20261116_prereg_b0.txt の ①②。
/// 盤面は試験の中で置く（密閉 1 国の仏・首都の州 Regions[0]＋首都の外の州 b・どちらも本位の金属の市場と商人を持つ・他の州の在庫は 0）。MintGold を反射で月ごとに呼ぶ。
/// 期待の量は台本の在庫と月次率から手で書く（エンジン・計器の出力から作らない）。
/// </summary>
[Collection("Instruments")]
public class MintcapInstrumentTests
{
    private static (Simulation Sim, World W, Region Cap, Region Other) Board(int metal, double flag)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1), new NullAgent());
        var w = sim.World;
        w.Coef.MintReserveTargetWeight = 1.0;
        w.Coef.MintParitySells = 0.0;
        w.Coef.MintBuysAtCapital = flag;
        w.MonetaryLaw = metal == Catalog.SilverId ? MonetaryStandardLaw.Silver : MonetaryStandardLaw.Gold;
        var cap = w.Regions[0];
        Region? other = null;
        foreach (var x in w.Regions)
            if (!ReferenceEquals(x, cap) && x.GoodOf(metal) is not null && x.MerchantPopIndex >= 0 && x.MerchantPopIndex < x.Pops.Count) { other = x; break; }
        Assert.True(cap.GoodOf(metal) is not null && cap.MerchantPopIndex >= 0 && cap.MerchantPopIndex < cap.Pops.Count, "前提＝首都の州が本位の金属の市場と商人を持つ");
        Assert.True(other is not null, "前提＝首都の外に本位の金属の市場と商人を持つ州がある");
        foreach (var x in w.Regions)
            if (!ReferenceEquals(x, cap) && !ReferenceEquals(x, other) && x.GoodOf(metal) is not null) x.Markets[metal].Stock = 0;
        return (sim, w, cap, other!);
    }

    private static void Mint(Simulation sim)
    {
        var mint = typeof(Simulation).GetMethod("MintGold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(mint);
        mint!.Invoke(sim, null);
    }

    private static double Budget(World w)
    {
        double coverTarget = w.Coef.GoldCoverMin.Value * w.Coef.GoldRejoinCoverMargin;
        return System.Math.Max(0.0, coverTarget * w.BroadMoney.Value - w.BaseMoney.Value) * w.Coef.MintReserveSellRatePerMonth.Value;
    }
    private static void Short(World w) => w.Coef.GoldCoverMin = new Share(0.99);
    private static void Ok(World w) => w.Coef.GoldCoverMin = new Share(0.0001);

    private static IDisposable Instrumented()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_mc_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        Instruments.SelectTables("mintcap_s");
        return new Off(dir);
    }
    private sealed class Off(string dir) : IDisposable
    {
        public void Dispose() { Instruments.SelectTables(null); Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void T1_旗0で旧の規則の影は実際に買えた量と一致する()
    {
        T1Body(1);   // 銀本位
        T1Body(2);   // 金本位
    }

    private static void T1Body(int law)
    {
        int metal = law == 1 ? Catalog.SilverId : Catalog.GoldOreId;
        using var _ = Instrumented();
        var (sim, w, cap, oth) = Board(metal, 0.0);
        double parity = Catalog.RefPrices[metal];
        double rate = w.Coef.MintFloorBuyRatePerMonth.Value;
        var mc = cap.Markets[metal]; var mo = oth.Markets[metal];

        // 月 1：被覆は足りている・平価の下・2 州とも在庫＝床の買い（首都 100・外 60）
        Ok(w); mc.Price = parity * 0.5; mo.Price = parity * 0.5; mc.Stock = 100; mo.Stock = 60; Mint(sim);
        // 月 2：準備不足・平価超え・2 州とも予算の 5 倍の在庫＝首都が予算を使い切り外の州は予算が尽きる
        Short(w); double budget = Budget(w);
        Assert.True(budget > 1e-6, $"前提が崩れた：準備不足の予算が立たない（{budget}）");
        mc.Price = parity * 1.5; mo.Price = parity * 1.5;
        mc.Stock = 5.0 * budget / mc.Price / rate; mo.Stock = 5.0 * budget / mo.Price / rate; Mint(sim);

        var a = Instruments.MintcapPeek(w)!;
        double wantCap = 100 * rate + budget / (parity * 1.5);   // 月 1 の首都の床＋月 2 の首都の市場買い（予算ぶん）
        double wantOth = 60 * rate;                              // 月 1 の外の床だけ（月 2 は予算が尽きる）
        double oldS = law == 1 ? a.OldAg : a.OldAu, capB = law == 1 ? a.BoughtCapAg : a.BoughtCapAu, othB = law == 1 ? a.BoughtOtherAg : a.BoughtOtherAu;
        double oldOtherMetal = law == 1 ? a.OldAu : a.OldAg, boughtOtherMetal = law == 1 ? a.BoughtCapAu + a.BoughtOtherAu : a.BoughtCapAg + a.BoughtOtherAg;
        Assert.Equal(2, a.Months);
        Assert.Equal(wantCap, capB, 9);
        Assert.Equal(wantOth, othB, 9);
        Assert.True(System.Math.Abs(oldS - (capB + othB)) <= 1e-9 * System.Math.Max(1.0, oldS), $"旧の影 {oldS} ＝実際に買えた量 {capB + othB}");
        Assert.Equal(wantCap + wantOth, oldS, 9);
        Assert.Equal(0.0, oldOtherMetal);        // もう一方の金属は数えない
        Assert.Equal(0.0, boughtOtherMetal);
        Assert.True(a.CapFloor == 1 && a.CapAtMarket == 1 && a.CapAbove == 0, "首都の月の区分＝床 1・市場買い 1");
        Assert.Equal(0, a.AboveBought);          // 平価超えの月に床の枝で買えた回数は 0（市場買いの月は数えない）
    }

    /// <summary>(1192) c119kf：列 capImportAgSum は首都の銀の輸入を読む。月の頭の EconomyStep.Run が LastImport を 0 にしてから MintGold が走るので、
    /// 直前の清算の輸入は PrevImport にある（旧の LastImport の読みは世界の中で常に 0）。期待値は台本で置いた 7 と 3 の和（本体の式から作らない）。</summary>
    [Fact]
    public void T3_首都の銀の輸入が列に入る()
    {
        using var _ = Instrumented();
        var (sim, w, cap, oth) = Board(Catalog.SilverId, 0.0);
        var mc = cap.Markets[Catalog.SilverId];
        Ok(w); mc.Price = Catalog.RefPrices[Catalog.SilverId] * 0.5; mc.Stock = 100;
        mc.LastImport = 0.0; mc.PrevImport = 7.0; oth.Markets[Catalog.SilverId].PrevImport = 50.0;   // 首都の外の輸入は数えない
        Mint(sim);
        Assert.Equal(7.0, Instruments.MintcapPeek(w)!.CapImportAg, 9);
        mc.PrevImport = 3.0; Mint(sim);
        Assert.Equal(10.0, Instruments.MintcapPeek(w)!.CapImportAg, 9);
    }

    [Fact]
    public void T2_旗1で首都の外の買いは0で首都は旧と同じ規則で買う()
    {
        int metal = Catalog.SilverId;
        double parity = Catalog.RefPrices[metal];
        static (double CapStock, double OthStock, Instruments.MintcapAcc? Acc) Run(double flag, bool ins)
        {
            int metal = Catalog.SilverId;
            IDisposable? scope = ins ? Instrumented() : null;
            try
            {
                var (sim, w, cap, oth) = Board(metal, flag);
                double parity = Catalog.RefPrices[metal];
                Ok(w);
                cap.Markets[metal].Price = parity * 0.5; oth.Markets[metal].Price = parity * 0.5;
                cap.Markets[metal].Stock = 100; oth.Markets[metal].Stock = 60;
                Mint(sim);
                var acc = Instruments.MintcapPeek(w);
                var copy = acc is null ? null : new Instruments.MintcapAcc
                {
                    Months = acc.Months, OldAg = acc.OldAg, NewAg = acc.NewAg, BoughtCapAg = acc.BoughtCapAg, BoughtOtherAg = acc.BoughtOtherAg, CapFloor = acc.CapFloor
                };
                return (cap.Markets[metal].Stock, oth.Markets[metal].Stock, copy);
            }
            finally { scope?.Dispose(); }
        }
        double rate = new Coefficients().MintFloorBuyRatePerMonth.Value;
        var f0 = Run(0.0, true);
        var f1 = Run(1.0, true);
        // 旗 0＝両方の州で買う・旗 1＝首都だけ（外の州の在庫は 60 のまま・首都は旗 0 と同じだけ減る）
        Assert.Equal(100 * (1 - rate), f0.CapStock, 9);
        Assert.Equal(60 * (1 - rate), f0.OthStock, 9);
        Assert.Equal(System.BitConverter.DoubleToInt64Bits(f0.CapStock), System.BitConverter.DoubleToInt64Bits(f1.CapStock));
        Assert.Equal(60.0, f1.OthStock);
        Assert.NotNull(f1.Acc);
        Assert.Equal(0.0, f1.Acc!.BoughtOtherAg);
        Assert.Equal(100 * rate, f1.Acc.BoughtCapAg, 9);
        Assert.Equal(100 * rate, f1.Acc.NewAg, 9);             // 新の影＝首都だけ
        Assert.Equal((100 + 60) * rate, f1.Acc.OldAg, 9);      // 旧の影は旗に依らず全ての州（旗 1 でも同じ）
        Assert.Equal(f0.Acc!.OldAg, f1.Acc.OldAg, 9);
        Assert.Equal(60 * rate, f0.Acc.BoughtOtherAg, 9);      // 旗 0 では外の州も買う（対照）
        // 計器が切れている時は何も溜めない
        var off = Run(1.0, false);
        Assert.Null(off.Acc);
        Assert.Equal(System.BitConverter.DoubleToInt64Bits(f1.CapStock), System.BitConverter.DoubleToInt64Bits(off.CapStock));
        Assert.Equal(System.BitConverter.DoubleToInt64Bits(f1.OthStock), System.BitConverter.DoubleToInt64Bits(off.OthStock));
        Assert.True(parity > 0);
    }
}
