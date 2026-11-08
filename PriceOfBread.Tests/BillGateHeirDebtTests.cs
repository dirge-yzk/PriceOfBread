using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(971) c87hd 案 (c1)〈手形の門が相続人の債務も読む〉（旗 <see cref="Coefficients.BillGateReadsHeirDebt"/>・事前登録 measurements/m971_20261108_prereg_hd.txt）の機構の番人 H1〜H7。
/// 盤面は BillBlockedColumnsTests.RunClearing と同じ組み方（ジェノヴァ・トスカーナ・売り手の港の州と買い手の州・1 回の清算 Market.Step）を、旗を置けるように写した。
/// 期待は試験が置いた債務・貯蓄からの手の判定（計器もエンジンの関数も読まない）：手形を振り出した＝商人（貯蓄 0）の債務が清算の後に ＞0／振り出さない＝商人の債務が清算の前と同じ・買い手の州の輸入 0。</summary>
[Collection("Instruments")]
public class BillGateHeirDebtTests
{
    private static MultiNationSim Gt() => new([WorldData.ByName("ジェノヴァ"), WorldData.ByName("トスカーナ")], seed: 1789) { DiplomacyFrozen = true };

    /// <summary>輸入する買い手の州を置いて 1 回の清算を通す。<paramref name="setup"/> は（買い手の州・商人・他の POP・同じ国の別の州）を受け、債務・貯蓄・人数を置く（貯蓄 1e4 の下地の後）。
    /// 返り＝（清算の前の商人の債務・清算の後の商人の債務・買い手の州のその財の輸入）。</summary>
    private static (double Before, double After, double Import) Run(double flag, Action<Region, Pop, List<Pop>, Region> setup)
    {
        var sim = Gt();
        sim.Coef.ClearingIdleSearchQuarters = 1;
        sim.Coef.BillGateReadsHeirDebt = flag;
        foreach (var n in sim.Nations) n.World.Coef.BillGateReadsHeirDebt = flag;
        var gd = sim.Nations[0].World.Region.Goods
            .First(g => !g.IsColonial && !g.NonTradeable && g.Tier != GoodTier.Staple && !g.GrainChain && Array.IndexOf(sim.Coef.MilitaryExportGoods, g.Id) < 0);
        double wp = sim.Market.Price[gd.Id];
        var all = new List<Region>();
        foreach (var n in sim.Nations) { n.World.Tariff = 0; n.World.RailCapacity = 1e9; n.World.ShipCapacity = 1e9; foreach (var r in n.World.Regions) all.Add(r); }
        var seller = all.Where(r => r.IsPort).First();
        var buyer = all.First(r => !ReferenceEquals(r, seller) && r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count
                              && r.Pops.Count(p => p.Size > 1e-6 && !ReferenceEquals(p, r.Pops[r.MerchantPopIndex])) >= 2);
        var bw = sim.Nations.First(n => n.World.Regions.Contains(buyer)).World;
        var other = bw.Regions.First(r => !ReferenceEquals(r, buyer) && !ReferenceEquals(r, seller) && r.Pops.Any(p => p.Size > 1e-6));
        foreach (var r in all)
        {
            var m = r.Markets[gd.Id]; m.LastDemand = 0; m.LastProduced = 0; m.LastExport = 0; m.LastImport = 0;
            if (ReferenceEquals(r, seller)) { m.Price = 0.5 * wp; m.Stock = 5000; m.TargetStock = 10; m.LastDemand = 10; m.LastProduced = 10; }
            else if (ReferenceEquals(r, buyer)) { m.Price = 2.0 * wp; m.Stock = 0; m.TargetStock = 50; m.LastDemand = 500; m.LastProduced = 0; }
            else { m.Price = wp; m.Stock = 0; m.TargetStock = 0; }
        }
        foreach (var r in all) foreach (var p in r.Pops) { p.Debt = new MoneyStock(0); if (p.Size > 1e-6) p.SetSavings(Math.Max(p.Savings.Value, 1e4)); }
        var mer = buyer.Pops[buyer.MerchantPopIndex];
        mer.SetSavings(0);   // 商人の貯蓄 0＝払えるのは手形だけ（手形の有無が商人の債務に出る）
        setup(buyer, mer, buyer.Pops.Where(p => !ReferenceEquals(p, mer) && p.Size > 1e-6).ToList(), other);   // 他の POP＝人の居る POP だけ（2 人以上は buyer の選び方が保つ）
        double before = mer.Debt.Value;
        sim.Market.Step(sim.Nations, sim.Coef);
        return (before, mer.Debt.Value, buyer.Markets[gd.Id].LastImport);
    }

    [Fact]
    public void H1_旗1_決済の後も相続人に債務が残る州は手形を振り出さない_旗0は振り出す()
    {
        // 他の POP：債務 5・貯蓄 1＝決済 ① で 1 返して 4 残る（＞1e-9）
        Action<Region, Pop, List<Pop>, Region> s = (r, m, o, x) => { o[0].Debt = new MoneyStock(5); o[0].SetSavings(1); };
        var f0 = Run(0, s);
        Assert.True(f0.After > 0, $"前提が崩れた：旗 0 で手形を振り出していない（商人の債務 {f0.After}）");
        Assert.True(f0.Import > 0, $"前提が崩れた：旗 0 で輸入していない {f0.Import}");
        var f1 = Run(1, s);
        Assert.Equal(BitConverter.DoubleToInt64Bits(f1.Before), BitConverter.DoubleToInt64Bits(f1.After));   // 手形なし＝商人の債務は動かない
        Assert.Equal(0.0, f1.Import);   // 商人の貯蓄 0・手形なし＝輸入 0
    }

    [Fact]
    public void H2_決済の後に読む_返し切れた相続人は手形を止めない()
    {
        // 債務 5・貯蓄 10＝決済 ① で全て返す＝門は開く（① の前に読む突然変異で赤）
        var f1 = Run(1, (r, m, o, x) => { o[0].Debt = new MoneyStock(5); o[0].SetSavings(10); });
        Assert.True(f1.After > 0, $"返し切れた相続人で手形が止まった（商人の債務 {f1.After}）");
        Assert.True(f1.Import > 0);
    }

    [Fact]
    public void H3_塵は門にしない_閾値は債務の和の1e9()
    {
        var a = Run(1, (r, m, o, x) => { o[0].Debt = new MoneyStock(1e-13); o[0].SetSavings(0); });
        Assert.True(a.After > 0, $"塵 1e-13 で手形が止まった（{a.After}）");
        var b = Run(1, (r, m, o, x) => { o[0].Debt = new MoneyStock(5e-10); o[0].SetSavings(0); });
        Assert.True(b.After > 0, $"5e-10 で手形が止まった（{b.After}）");
        // 和で読む：2 人が 6e-10 ずつ＝和 1.2e-9 ＞1e-9＝止まる（1 人ずつの最大で読む突然変異で赤）
        var c = Run(1, (r, m, o, x) => { o[0].Debt = new MoneyStock(6e-10); o[0].SetSavings(0); o[1].Debt = new MoneyStock(6e-10); o[1].SetSavings(0); });
        Assert.Equal(BitConverter.DoubleToInt64Bits(c.Before), BitConverter.DoubleToInt64Bits(c.After));
        Assert.Equal(0.0, c.Import);
    }

    [Fact]
    public void H4_同じ州だけを読む_同じ国の別の州の債務は門にしない()
    {
        var f1 = Run(1, (r, m, o, x) => { foreach (var p in x.Pops) if (p.Size > 1e-6) { p.Debt = new MoneyStock(5); p.SetSavings(0); } });
        Assert.True(f1.After > 0, $"別の州の債務で手形が止まった（{f1.After}）");
        Assert.True(f1.Import > 0);
    }

    [Fact]
    public void H5_人の居ないPOPの債務は読まない()
    {
        var f1 = Run(1, (r, m, o, x) => { o[0].Size = 0; o[0].Debt = new MoneyStock(5); o[0].SetSavings(0); });
        Assert.True(f1.After > 0, $"人の居ない POP の債務で手形が止まった（{f1.After}）");
    }

    [Fact]
    public void H6_商人の債務の規則2は旗1でも残る()
    {
        // 商人：債務 5・貯蓄 1＝決済 ① で 1 返して 4 残る＝振り出せない（他の POP は債務 0）。商人の債務を読まない突然変異で赤
        var f1 = Run(1, (r, m, o, x) => { m.Debt = new MoneyStock(5); m.SetSavings(1); });
        Assert.True(Math.Abs(f1.After - 4.0) <= 1e-9, $"商人の債務 {f1.After} ≠ 4（手形を振り出した）");
        Assert.Equal(0.0, f1.Import);
    }

    [Fact]
    public void H7_債務が無ければ旗1も旗0と同じく振り出す()
    {
        var f0 = Run(0, (r, m, o, x) => { });
        var f1 = Run(1, (r, m, o, x) => { });
        Assert.True(f0.After > 0 && f1.After > 0);
        Assert.Equal(BitConverter.DoubleToInt64Bits(f0.After), BitConverter.DoubleToInt64Bits(f1.After));
        Assert.Equal(BitConverter.DoubleToInt64Bits(f0.Import), BitConverter.DoubleToInt64Bits(f1.Import));
    }
}
