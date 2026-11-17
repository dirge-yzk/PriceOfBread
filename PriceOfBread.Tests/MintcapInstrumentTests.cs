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

    /// <summary>(1187) E2 の列（aboveBoughtN）に値が立つ盤面：被覆が足りている月に首都の値段が平価を超える＝首都では床の枝も市場買いも立たない。
    /// その月に首都で買えた量が計器に届けば（壊れたエンジンの型を試験が MintcapBought で直に注ぐ）列が立つ。首都の外の買いと、首都が平価の下の月の買いでは立たない。</summary>
    [Fact]
    public void T3_E2の列は平価超えの月に首都で買えた量が届くと立つ()
    {
        T3Body(1);   // 銀本位
        T3Body(2);   // 金本位
    }

    private static void T3Body(int law)
    {
        int metal = law == 1 ? Catalog.SilverId : Catalog.GoldOreId;
        using var _ = Instrumented();
        var (sim, w, cap, oth) = Board(metal, 0.0);
        double parity = Catalog.RefPrices[metal];
        double rate = w.Coef.MintFloorBuyRatePerMonth.Value;
        var mc = cap.Markets[metal]; var mo = oth.Markets[metal];

        // 月 1：被覆は足りている（予算 0）・首都は平価×1.5・外の州は平価×0.5
        Ok(w); mc.Price = parity * 1.5; mo.Price = parity * 0.5; mc.Stock = 100; mo.Stock = 60; Mint(sim);
        var a = Instruments.MintcapPeek(w)!;
        Assert.True(a.CapAboveNotAt, "首都の月の模型が〈平価超えで市場買いでない〉と言っていない");
        Assert.True(a.CapAbove == 1 && a.CapFloor == 0 && a.CapAtMarket == 0, "首都の月の区分＝平価超え 1");
        Assert.Equal(100.0, mc.Stock);           // 本物の MintGold は平価超えの首都で買わない
        Assert.Equal(0, a.AboveBought);
        Assert.Equal(0.0, law == 1 ? a.BoughtCapAg : a.BoughtCapAu);
        Assert.Equal(60 * rate, law == 1 ? a.BoughtOtherAg : a.BoughtOtherAu, 9);   // 外の州の床の買い（E2 に数えない）
        Instruments.MintcapBought(w, oth, metal, 5.0);
        Assert.Equal(0, a.AboveBought);          // 首都の外に届いた量は数えない
        Instruments.MintcapBought(w, cap, metal, 5.0);
        Assert.Equal(1, a.AboveBought);          // 平価超えの月に首都で買えた量が届いた＝列が立つ

        // 月 2：首都は平価×0.5（床）＝首都で本物の買いが届いても E2 は立たない
        mc.Price = parity * 0.5; mo.Stock = 0; Mint(sim);
        Assert.False(a.CapAboveNotAt);
        Assert.Equal(1, a.CapFloor);
        Assert.Equal(1, a.AboveBought);
        Assert.Equal(5.0 + 100 * rate, law == 1 ? a.BoughtCapAg : a.BoughtCapAu, 9);
        Assert.Equal(2, a.Months);
    }

    /// <summary>(1187) E4 の材料の列（銀を産む州の値段÷平価の州の数・中央・最小・最大）と首都の列（在庫・値段÷平価・越境の輸入の和）に値が立つ盤面。
    /// 英仏の 2 国（外交は凍結）で、仏の首都の外の銀の産地の州 3 つに平価×0.8・1.3・1.0 を置き、ほかの産地の生産者は倒産に（値段は平価×9）・生産者の無い銀の州が在れば平価×7。
    /// 首都の在庫 123・値段 平価×0.9・月の輸入 4 を置き、計器の月の口を 2 回・四半期の表を 1 回。期待は台本の値から手で書く。</summary>
    [Fact]
    public void T5_E4の材料の列と首都の列に値が立つ()
    {
        const ulong seed = 1187UL;
        int ag = Catalog.SilverId;
        double parity = Catalog.RefPrices[ag];
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_mc5_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        Instruments.SelectTables("mintcap_s");
        MultiNationSim? sim = null;
        int bankruptRegions = 0;
        try
        {
            sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed) { DiplomacyFrozen = true };
            var fr = sim.Nations[1].World;
            Assert.Equal("フランス", Instruments.NameOf(fr));
            fr.MonetaryLaw = MonetaryStandardLaw.Silver;
            var cap = fr.Regions[0];
            Assert.True(cap.GoodOf(ag) is not null, "前提＝仏の首都の州に銀の市場がある");
            var prod = new List<Region>(); Region? plain = null;
            foreach (var r in fr.Regions)
            {
                if (ReferenceEquals(r, cap) || r.GoodOf(ag) is null) continue;
                bool has = false;
                foreach (var p in r.Producers) if (p.GoodId == ag) { has = true; break; }
                if (has) prod.Add(r); else if (plain is null) plain = r;
            }
            Assert.True(prod.Count >= 4, $"前提＝首都の外に銀の産地の州が 4 つ以上（{prod.Count}）");
            double[] mul = [0.8, 1.3, 1.0];
            for (int i = 0; i < prod.Count; i++)
            {
                bool live = i < mul.Length;
                prod[i].Markets[ag].Price = parity * (live ? mul[i] : 9.0);
                foreach (var p in prod[i].Producers) if (p.GoodId == ag) p.Bankrupt = !live;
                if (!live) bankruptRegions++;
            }
            foreach (var p in cap.Producers) if (p.GoodId == ag) p.Bankrupt = true;   // 首都が産地でも数えない側へ
            if (plain is not null) plain.Markets[ag].Price = parity * 7.0;   // 生産者の無い銀の州（仏の盤面には無いことがある＝在れば数えない側の対照）
            var cm = cap.Markets[ag];
            cm.Stock = 123.0; cm.Price = parity * 0.9; cm.LastImport = 4.0;
            Instruments.MintcapMonth(fr);
            Instruments.MintcapMonth(fr);
            Instruments.Quarter(sim, seed, 0);
        }
        finally
        {
            if (sim is not null) Instruments.Close(sim);
            Instruments.SelectTables(null); Instruments.Configure(null, null);
        }
        var rows = SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"mintcap_s{seed}.tsv"), Instruments.MintcapHeaderText);
        try { System.IO.Directory.Delete(dir, true); } catch { }
        var row = rows.Single(r => r["nation"] == "フランス");
        double D(string c) => SubstInstrumentTests.D(row, c);
        static void Near(double want, double got, string what) => Assert.True(System.Math.Abs(want - got) <= 1e-12 * System.Math.Max(1.0, System.Math.Abs(want)), $"{what}: 期待 {want} 実際 {got}");
        Assert.True(bankruptRegions >= 1, "対照＝倒産にした産地の州が 1 つ以上");
        Assert.Equal(2.0, D("months"));
        Assert.Equal(3.0, D("originN"));
        Near(1.0, D("originPoverMed"), "産地の値段÷平価の中央");
        Near(0.8, D("originPoverMin"), "産地の値段÷平価の最小");
        Near(1.3, D("originPoverMax"), "産地の値段÷平価の最大");
        Near(123.0, D("capStock"), "首都の在庫");
        Near(0.9, D("capPriceOverParity"), "首都の値段÷平価");
        Near(8.0, D("capImportAgSum"), "首都の越境の輸入の和（4×2 か月）");
    }
}
