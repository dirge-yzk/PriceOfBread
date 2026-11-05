using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(874) c73tc cearn_s の末尾 4 列（importBudgetExcessMaxPerClearingSub・importPaidPayerSizeLe1e12PerQuarter・importPaidPayerSizeMidPerQuarter・importPaidPayerSizeGt1e9PerQuarter）と、
/// (868) の列 importOldRuleBudgetStartPerQuarter の kind 2（商人の人数 ≦1e-12）・kind 3（商人の添字なし）の旧の規則の枝の番人。事前登録 measurements/m874_20261106_prereg_tc.txt [3]。
/// 期待は計器と別の出所（試験が自分で置いた額と人数から手で計算）から作る。
/// ⚠突然変異（measurements/m874_20261106/mutation_874.txt）：M1 補助の絞りを外す→「補助は Subsidy だけ…」が赤／M2 帯を四半期の末の人数で振る→「帯は払いの時点…」が赤／M3 kind 2・3 の旧の規則の枝を外す→「旧の規則の予算は kind 2・3…」が赤。</summary>
[Collection("Instruments")]
public class CearnTradeColumnsTests
{
    private const string BmPath = @"C:\x\PriceOfBread.SimCore\BilateralMarket.cs";   // CearnFileName は名前だけを見る

    private static MultiNationSim Two() => new([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789) { DiplomacyFrozen = true };

    private static string NewDir() => System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_c73tc_" + Guid.NewGuid().ToString("N"));

    private static void Done(string dir) { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }

    [Fact]
    public void 補助はSubsidyだけを足し輸出の関税の戻しは足さない()
    {
        string dir = NewDir(); Instruments.Configure(dir, "*");
        try
        {
            var sim = Two(); var w = sim.Nations[0].World; Instruments.Begin(w);
            var r = w.Regions.First(x => x.MerchantPopIndex >= 0 && x.Pops[x.MerchantPopIndex].Size > 1e-12);
            var m = r.Pops[r.MerchantPopIndex]; m.Debt = new MoneyStock(5); m.SetSavings(100);
            var a = Instruments.CearnAccOf(r)!; a.Clear();
            Instruments.CearnImportHeadInto(a, r);   // 頭の在庫 100（2 つの在庫とも）
            Instruments.Route(BmPath, 1141, 'T', 'P'); Instruments.CearnRoute(TreasuryCat.Subsidy, null);
            Instruments.Sav(m, Instruments.SavKind.Give, 25);   // 売買方針の補助（払いの前に払い手へ）
            Instruments.Route(BmPath, 1165, 'T', 'P'); Instruments.CearnRoute(TreasuryCat.Tariff, null);
            Instruments.Sav(m, Instruments.SavKind.Give, 40);   // 輸出の関税の戻し（払いの後・Subsidy ではない）
            Instruments.Route(BmPath, 1150, 'P', 'T'); Instruments.CearnRoute(TreasuryCat.Tariff, null);
            Instruments.Sav(m, Instruments.SavKind.Take, -150);   // 払い 150
            Instruments.CearnImportFoldInto(a);
            Assert.Equal(150.0 - (100 + 25 + 40), a.ImpExMax);        // 今の列は 2 つとも足す（記録として残す）
            Assert.Equal(150.0 - (100 + 25), a.ImpExMaxSub);          // 新しい列は Subsidy だけ
            // :1141 だけの盤面では 2 列が一致する
            var b = new Instruments.CearnAcc();
            Instruments.CearnImportHeadInto(b, r);
            Instruments.Route(BmPath, 1141, 'T', 'P'); Instruments.CearnRoute(TreasuryCat.Subsidy, null);
            Instruments.CearnImportSubsidyInto(b, 25); Instruments.CearnImportSubsidySubInto(b, 25);
            Instruments.CearnImportPaidInto(b, 150);
            Instruments.CearnImportFoldInto(b);
            Assert.Equal(b.ImpExMax, b.ImpExMaxSub);
            Assert.Equal(25.0, b.ImpExMaxSub);
            // 手形の清算（債務 0 の人の居る商人＝予算 ∞）は新しい列も畳まない
            m.Debt = new MoneyStock(0);
            var c = new Instruments.CearnAcc();
            Instruments.CearnImportHeadInto(c, r);
            Instruments.CearnImportSubsidySubInto(c, 25); Instruments.CearnImportPaidInto(c, 1e6);
            Instruments.CearnImportFoldInto(c);
            Assert.True(double.IsNegativeInfinity(c.ImpExMaxSub));
        }
        finally { Done(dir); }
    }

    [Fact]
    public void 帯は払いの時点の払い手の人数で決まり四半期の末の人数ではない()
    {
        string dir = NewDir(); Instruments.Configure(dir, "*");
        try
        {
            var sim = Two(); var w = sim.Nations[0].World; Instruments.Begin(w);
            var r = w.Regions.First(x => x.Pops.Count > 0);
            var p = r.Pops.First(); var a = Instruments.CearnAccOf(r)!; a.Clear();
            Instruments.Route(BmPath, 1150, 'P', 'T'); Instruments.CearnRoute(TreasuryCat.Tariff, null);
            p.Size = 0; Instruments.Sav(p, Instruments.SavKind.Take, -10);            // ≦1e-12
            p.Size = 1e-12; Instruments.Sav(p, Instruments.SavKind.Take, -1);         // 境界は下の帯（≦1e-12）
            p.Size = 5e-10; Instruments.Sav(p, Instruments.SavKind.Take, -7);         // 1e-12 超〜1e-9
            p.Size = 1e-9; Instruments.Sav(p, Instruments.SavKind.Take, -2);          // 境界は中の帯（≦1e-9）
            p.Size = 1.0; Instruments.Sav(p, Instruments.SavKind.Take, -3);           // 1e-9 超
            Assert.Equal(11.0, a.ImpPaidBand[0]);
            Assert.Equal(9.0, a.ImpPaidBand[1]);
            Assert.Equal(3.0, a.ImpPaidBand[2]);
            Assert.Equal(a.ImpPaid, a.ImpPaidBand.Sum(), 9);   // 3 列の和 = importPaidPerQuarter
            Assert.Equal(23.0, a.ImpPaid, 9);
            Instruments.Route(@"C:\x\Other.cs", 1, 'P', 'T');   // BilateralMarket.cs 以外の払いは数えない
            Instruments.Sav(p, Instruments.SavKind.Take, -5);
            Assert.Equal(23.0, a.ImpPaid, 9);
        }
        finally { Done(dir); }
    }

    [Fact]
    public void 旧の規則の予算はkind2と3でも旗に依らず手で数えた値_書き出しの行を通して()
    {
        foreach (double flag in new[] { 0.0, 1.0 })
        {
            string dir = NewDir(); Instruments.Configure(dir, "*");
            try
            {
                var sim = Two(); sim.Coef.ImportPayerLive = flag;
                var w = sim.Nations[0].World; Instruments.Begin(w);
                var rs = w.Regions.Where(x => x.MerchantPopIndex >= 0 && x.Pops.Count(p => p.Size > 1e-12) >= 2).Take(2).ToList();
                var r2 = rs[0]; var r3 = rs[1];
                // kind 2：商人の人数 ≦1e-12（債務 0＝旧の規則では手形＝+∞・新の規則では人の居る POP の在庫の和）
                var m2 = r2.Pops[r2.MerchantPopIndex]; m2.Size = 0; m2.Debt = new MoneyStock(0); m2.SetSavings(40);
                double live2 = 0;
                foreach (var p in r2.Pops) if (p.Size > 1e-12) live2 += Math.Max(0, p.Savings.Value);
                // kind 3：商人の添字なし（旧の規則も新の規則も全 POP / 人の居る POP の Σmax(0, 貯蓄)）
                r3.MerchantPopIndex = -1;
                double all3 = 0, live3 = 0;
                foreach (var p in r3.Pops) { all3 += Math.Max(0, p.Savings.Value); if (p.Size > 1e-12) live3 += Math.Max(0, p.Savings.Value); }
                var a2 = Instruments.CearnAccOf(r2)!; a2.Clear(); Instruments.CearnImportHeadInto(a2, r2);
                var a3 = Instruments.CearnAccOf(r3)!; a3.Clear(); Instruments.CearnImportHeadInto(a3, r3);
                Assert.Equal(2, a2.ImpKind); Assert.Equal(3, a3.ImpKind);
                Assert.True(double.IsPositiveInfinity(a2.ImpOldBudget), "kind 2：債務 0 の人の居ない商人は旧の規則で手形＝+∞");
                Assert.Equal(live2, a2.ImpBudget, 6);
                m2.Debt = new MoneyStock(3);
                var a2b = new Instruments.CearnAcc(); Instruments.CearnImportHeadInto(a2b, r2);
                Assert.Equal(40.0, a2b.ImpOldBudget);   // kind 2：債務のある人の居ない商人は旧の規則で max(0, 貯蓄)
                Assert.Equal(all3, a3.ImpOldBudget, 6);
                Assert.Equal(live3, a3.ImpBudget, 6);
                // 書き出しの行を通す：Quarter が cearn_s の行を書く（末尾の列に同じ値が出る）
                m2.Debt = new MoneyStock(0);
                a2.Clear(); Instruments.CearnImportHeadInto(a2, r2);
                a3.Clear(); Instruments.CearnImportHeadInto(a3, r3);
                a3.ImpPaidBand[0] = 1; a3.ImpPaidBand[1] = 2; a3.ImpPaidBand[2] = 4; a3.ImpPaid = 7; a3.ImpExMaxSub = 0.5;
                string nation = Instruments.NameOf(w);
                Instruments.Quarter(sim, 7UL, 0);
                Instruments.Close(sim);
                var file = System.IO.Directory.GetFiles(dir, "cearn_s*").Single();
                var lines = System.IO.File.ReadAllLines(file);
                Assert.Equal(Instruments.CearnHeaderText, lines[0]);
                var h = lines[0].Split('\t'); int Ix(string n) => Array.IndexOf(h, n);
                var row2 = lines.Skip(1).Select(l => l.Split('\t')).Single(c => c[2] == nation && c[3] == r2.Name);
                var row3 = lines.Skip(1).Select(l => l.Split('\t')).Single(c => c[2] == nation && c[3] == r3.Name);
                double D(string[] c, string n) => double.Parse(c[Ix(n)], System.Globalization.CultureInfo.InvariantCulture);
                Assert.Equal("2", row2[Ix("importPayerKind")]); Assert.Equal("3", row3[Ix("importPayerKind")]);
                Assert.Equal("inf", row2[Ix("importOldRuleBudgetStartPerQuarter")], ignoreCase: true);
                Assert.Equal(all3, D(row3, "importOldRuleBudgetStartPerQuarter"), 6);
                Assert.Equal(1.0, D(row3, "importPaidPayerSizeLe1e12PerQuarter"));
                Assert.Equal(2.0, D(row3, "importPaidPayerSizeMidPerQuarter"));
                Assert.Equal(4.0, D(row3, "importPaidPayerSizeGt1e9PerQuarter"));
                Assert.Equal(0.5, D(row3, "importBudgetExcessMaxPerClearingSub"));
                Assert.Equal(-live2, D(row2, "importBudgetExcessMaxPerClearingSub"), 6);   // 払い 0・在庫 = 人の居る POP の和
                Assert.Equal(D(row2, "importBudgetExcessMaxPerClearing"), D(row2, "importBudgetExcessMaxPerClearingSub"));   // 補助が無ければ 2 列は一致
                var rx = w.Regions.First(x => !ReferenceEquals(x, r2) && !ReferenceEquals(x, r3));
                var rowx = lines.Skip(1).Select(l => l.Split('	')).Single(c => c[2] == nation && c[3] == rx.Name);
                Assert.Equal("nan", rowx[Ix("importBudgetExcessMaxPerClearingSub")], ignoreCase: true);   // 清算が無ければ未定義
            }
            finally { Done(dir); }
        }
    }

    /// <summary>エンジンを通す：人の居ない商人（人数 0）が払い手になる旗 0 では最初の帯が正・旗 1 では 0（人の居る POP の帯だけ）。3 列の和 = importPaidPerQuarter。</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void 旗0は人の居ない商人の帯が正_旗1は0_三列の和は払った額(double flag)
    {
        string dir = NewDir(); Instruments.Configure(dir, "*");
        try
        {
            var sim = new MultiNationSim([WorldData.ByName("ジェノヴァ"), WorldData.ByName("トスカーナ")], seed: 1789) { DiplomacyFrozen = true };
            sim.Coef.ClearingIdleSearchQuarters = 1; sim.Coef.ImportPayerLive = flag;
            var gd = sim.Nations[0].World.Region.Goods
                .First(g => !g.IsColonial && !g.NonTradeable && g.Tier != GoodTier.Staple && !g.GrainChain && Array.IndexOf(sim.Coef.MilitaryExportGoods, g.Id) < 0);
            double wp = sim.Market.Price[gd.Id];
            var all = new List<Region>();
            foreach (var n in sim.Nations) { n.World.Tariff = 0; n.World.RailCapacity = 1e9; n.World.ShipCapacity = 1e9; Instruments.Begin(n.World); foreach (var r in n.World.Regions) all.Add(r); }
            var ports = all.Where(r => r.IsPort).ToList();
            var seller = ports[0];
            var buyer = all.First(r => !ReferenceEquals(r, seller) && r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count
                                       && r.Pops.Count(p => p.Size > 1e-6 && !ReferenceEquals(p, r.Pops[r.MerchantPopIndex])) >= 2);
            foreach (var r in all)
            {
                var m = r.Markets[gd.Id]; m.LastDemand = 0; m.LastProduced = 0; m.LastExport = 0; m.LastImport = 0;
                if (ReferenceEquals(r, seller)) { m.Price = 0.5 * wp; m.Stock = 5000; m.TargetStock = 10; m.LastDemand = 10; m.LastProduced = 10; }
                else if (ReferenceEquals(r, buyer)) { m.Price = 2.0 * wp; m.Stock = 0; m.TargetStock = 50; m.LastDemand = 500; m.LastProduced = 0; }
                else { m.Price = wp; m.Stock = 0; m.TargetStock = 0; }
            }
            foreach (var r in all) foreach (var p in r.Pops) { p.Debt = new MoneyStock(0); if (p.Size > 1e-6) p.SetSavings(Math.Max(p.Savings.Value, 1e4)); }
            var mer = buyer.Pops[buyer.MerchantPopIndex]; mer.Size = 0; mer.SetSavings(1e7);
            var a = Instruments.CearnAccOf(buyer)!; a.Clear();
            sim.Market.Step(sim.Nations, sim.Coef);
            Assert.True(buyer.Markets[gd.Id].LastImport > 1e-9, "前提が崩れた：買い手の州が輸入していない");
            Assert.True(a.ImpPaid > 0, "前提が崩れた：払いが記録されていない");
            Assert.Equal(a.ImpPaid, a.ImpPaidBand.Sum(), 9);
            if (flag == 0.0) { Assert.True(a.ImpPaidBand[0] > 0, "旗 0：人の居ない商人が払う＝最初の帯が正"); Assert.Equal(a.ImpPaid, a.ImpPaidBand[0], 9); }
            else { Assert.Equal(0.0, a.ImpPaidBand[0]); Assert.True(a.ImpPaidBand[2] > 0, "旗 1：人の居る POP が払う＝最後の帯が正"); }
        }
        finally { Done(dir); }
    }
}
