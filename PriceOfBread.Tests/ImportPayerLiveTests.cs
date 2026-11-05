using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(863) c71ip 輸入の払い手を人の居る POP に限る（旗 <see cref="Coefficients.ImportPayerLive"/>・案 A・形 (ii)・事前登録 measurements/m863_20261105_prereg_ip.txt）の番人。
/// 盤面＝ジェノヴァとトスカーナ（TradeClearingTests と同じ）で、買い手の州を 1 つだけ立てる（ほかの州は需要 0・在庫が目標どおり）＝払いの出所をその州に閉じる。
/// 旗 0：人の居ない商人（人数 0）がその 1 POP として払う（今の形）。旗 1：人数 ＞1e-12 の POP だけが払い、手形を振り出さず、人の居ない POP は払わない。
/// ⚠突然変異（事前登録 [3]）：E1 条件を逆さ→「旗1は人の居る…」が赤／E2 落とした枝で手形→「旗1で人の居るPOPが1つ…」が赤／E3 別の州の POP→「旗1は人の居る…」が赤。</summary>
public class ImportPayerLiveTests
{
    private static (MultiNationSim sim, int good, Region buyer, Pop merchant) Make(double flag)
    {
        var sim = new MultiNationSim([WorldData.ByName("ジェノヴァ"), WorldData.ByName("トスカーナ")], seed: 1789) { DiplomacyFrozen = true };
        sim.Coef.ClearingIdleSearchQuarters = 1;
        sim.Coef.ImportPayerLive = flag;
        var gd = sim.Nations[0].World.Region.Goods
            .First(g => !g.IsColonial && !g.NonTradeable && g.Tier != GoodTier.Staple && !g.GrainChain
                        && Array.IndexOf(sim.Coef.MilitaryExportGoods, g.Id) < 0);
        double wp = sim.Market.Price[gd.Id];
        var all = new List<Region>();
        foreach (var n in sim.Nations)
        {
            n.World.Tariff = 0;
            n.World.RailCapacity = 1e9; n.World.ShipCapacity = 1e9;
            foreach (var r in n.World.Regions) all.Add(r);
        }
        var ports = all.Where(r => r.IsPort).ToList();
        Assert.True(ports.Count >= 2, "前提が崩れた：港州が 2 つ要る");
        var seller = ports[0];
        var buyer = all.First(r => !ReferenceEquals(r, seller) && r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count
                                   && r.Pops.Count(p => p.Size > 1e-6 && !ReferenceEquals(p, r.Pops[r.MerchantPopIndex])) >= 2);
        foreach (var r in all)
        {
            var m = r.Markets[gd.Id];
            m.LastDemand = 0; m.LastProduced = 0; m.LastExport = 0; m.LastImport = 0;
            if (ReferenceEquals(r, seller)) { m.Price = 0.5 * wp; m.Stock = 5000; m.TargetStock = 10; m.LastDemand = 10; m.LastProduced = 10; }
            else if (ReferenceEquals(r, buyer)) { m.Price = 2.0 * wp; m.Stock = 0; m.TargetStock = 50; m.LastDemand = 500; m.LastProduced = 0; }
            else { m.Price = wp; m.Stock = 0; m.TargetStock = 0; }
        }
        foreach (var r in all) foreach (var p in r.Pops) { p.Debt = new MoneyStock(0); if (p.Size > 1e-6) p.SetSavings(Math.Max(p.Savings.Value, 1e4)); }
        var mer = buyer.Pops[buyer.MerchantPopIndex];
        mer.Size = 0; mer.SetSavings(1e7);   // 人の居ない商人（階級が絶えた）＝在庫は大きい・債務 0
        return (sim, gd.Id, buyer, mer);
    }

    private static Dictionary<Pop, double> Snap(MultiNationSim sim)
    {
        var d = new Dictionary<Pop, double>(ReferenceEqualityComparer.Instance);
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) foreach (var p in r.Pops) d[p] = p.Savings.Value;
        return d;
    }
    private static double HouseholdLoans(MultiNationSim sim) => sim.Nations.Sum(n => n.World.Bank!.HouseholdLoans.Value);

    [Fact]
    public void 旗0は人の居ない商人が1POPとして払う_今の形()
    {
        var (sim, g, buyer, mer) = Make(0);
        var before = Snap(sim);
        sim.Market.Step(sim.Nations, sim.Coef);
        Assert.True(buyer.Markets[g].LastImport > 1e-9, "前提が崩れた：買い手の州が輸入していない");
        Assert.True(mer.Savings.Value < before[mer], "旗 0 で人の居ない商人が払っていない（今の形が変わった）");
        foreach (var p in buyer.Pops) if (!ReferenceEquals(p, mer)) Assert.Equal(before[p], p.Savings.Value);   // 商人以外は払わない
    }

    [Fact]
    public void 旗1は人の居るPOPだけが払い人の居ない商人と他の州のPOPは払わない()
    {
        var (sim, g, buyer, mer) = Make(1);
        var before = Snap(sim);
        double live0 = buyer.Pops.Where(p => p.Size > 1e-12).Sum(p => Math.Max(0, p.Savings.Value));
        sim.Market.Step(sim.Nations, sim.Coef);
        Assert.True(buyer.Markets[g].LastImport > 1e-9, "旗 1 で買い手の州が輸入していない（人の居る POP の在庫で払えるはず）");
        Assert.Equal(before[mer], mer.Savings.Value);                                                   // 人の居ない商人は払わない
        foreach (var p in buyer.Pops) if (!(p.Size > 1e-12)) Assert.True(p.Savings.Value >= before[p], "人数 ≦1e-12 の POP が払った");
        double paid = buyer.Pops.Where(p => p.Size > 1e-12).Sum(p => before[p] - p.Savings.Value);
        Assert.True(paid > 0, "人の居る POP が払っていない");
        Assert.True(paid <= live0 * (1 + 1e-9), $"払った額 {paid} が清算の頭の人の居る POP の在庫 {live0} を超えた（手形が振り出された）");
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions)
            if (!ReferenceEquals(r, buyer)) foreach (var p in r.Pops) Assert.True(p.Savings.Value >= before[p] - 1e-9, $"別の州 {r.Name} の POP が払った（州をまたぐ払い）");
    }

    [Fact]
    public void 旗1で人の居るPOPが1つでも手形を振り出さない()
    {
        var (sim, g, buyer, mer) = Make(1);
        var one = buyer.Pops.First(p => p.Size > 1e-6 && !ReferenceEquals(p, mer));
        foreach (var p in buyer.Pops) if (!ReferenceEquals(p, one)) p.Size = 0;   // 人の居る POP を 1 つだけにする（払い手の並びが 1 要素＝手形の枝の型 [var mer] に当たる形）
        Assert.Single(buyer.Pops, p => p.Size > 1e-12);
        one.SetSavings(1);   // 代金（旗 1 で予算が縛らなければ約 16）より小さい在庫＝手形を振り出せる形なら借りる額が立つ
        double h0 = HouseholdLoans(sim);
        sim.Market.Step(sim.Nations, sim.Coef);
        Assert.Equal(0.0, one.Debt.Value);                       // 落とした枝は手形を振り出さない
        Assert.Equal(0.0, mer.Debt.Value);
        Assert.True(Math.Abs(HouseholdLoans(sim) - h0) <= 1e-9, "家計の与信が動いた（手形が振り出された）");
        Assert.True(one.Savings.Value >= -1e-9, "払い手の在庫が負になった");
    }

    [Fact]
    public void 旗1で人の居るPOPが0の州は輸入しない()
    {
        var (sim, g, buyer, mer) = Make(1);
        foreach (var p in buyer.Pops) p.Size = 0;
        var before = Snap(sim);
        sim.Market.Step(sim.Nations, sim.Coef);
        Assert.Equal(0.0, buyer.Markets[g].LastImport);
        foreach (var p in buyer.Pops) Assert.True(p.Savings.Value >= before[p], "人の居ない州の POP が払った");
    }

    [Fact]
    public void 旗1でも商人が人の居る州は旗0と同じ軌道()
    {
        double[] Run(double flag)
        {
            var (sim, g, buyer, mer) = Make(flag);
            mer.Size = 1.0;   // 人の居る商人に戻す（今の形の (a) の枝）
            sim.Market.Step(sim.Nations, sim.Coef);
            return sim.Nations.SelectMany(n => n.World.Regions).SelectMany(r => r.Pops).Select(p => p.Savings.Value).Append(buyer.Markets[g].LastImport).ToArray();
        }
        var a = Run(0); var b = Run(1);
        Assert.Equal(a.Length, b.Length);
        for (int i = 0; i < a.Length; i++) Assert.Equal(a[i], b[i]);
    }

    [Fact]
    public void 計器の清算の頭は州の状態と人の居るPOPの予算を自分で数える()
    {
        bool saved = Instruments.On;
        Instruments.On = true;
        try
        {
            var (sim, g, buyer, mer) = Make(1);
            double live0 = buyer.Pops.Where(p => p.Size > 1e-12).Sum(p => Math.Max(0, p.Savings.Value));
            var a0 = Instruments.CearnAccOf(buyer);
            int k0 = a0?.ImpKind ?? 0; double b0 = a0?.ImpBudget ?? 0;
            sim.Market.Step(sim.Nations, sim.Coef);
            var a = Instruments.CearnAccOf(buyer);
            Assert.NotNull(a);
            Assert.Equal(2, a!.ImpKind);                                                         // 商人の POP の人数 ≦1e-12
            Assert.True(Math.Abs(a.ImpBudget - b0 - live0) <= 1e-9 * Math.Max(1, live0), $"予算 {a.ImpBudget - b0} ≠ 人の居る POP の在庫の和 {live0}");
            mer.Size = 1.0; mer.Debt = new MoneyStock(0);
            sim.Market.Step(sim.Nations, sim.Coef);
            Assert.Equal(1, a.ImpKind);
            Assert.True(double.IsPositiveInfinity(a.ImpBudget), "債務 0 の人の居る商人の予算が ∞ でない");
        }
        finally { Instruments.On = saved; }
    }
}
