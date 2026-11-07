using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(920) c80eb 人の居ない商人の手形の詰まりの計器（恒等の線・事前登録 measurements/m920_20261107_prereg_eb.txt）。
/// cearn_s の末尾 5 列（billBlockedClearings・billBlockedDustClearings・emptyDebtAfterEstate・estateDebtMovedPerQuarter・heirDebtSettledPerQuarter）と
/// cpop_s の末尾 3 列（outOtherTop1Route・outOtherTop1PerQuarter・debtInCarryPerQuarter）の番人。期待は試験が置いた債務・貯蓄・移す人数から手で計算する（計器の式を写さない）。
/// エンジンの経路を通す＝清算は Market.Step・相続の段は EmptyPopEstate.Step・運搬は Pop.Blend（呼び口を外す突然変異で赤になる）。</summary>
[Collection("Instruments")]
public class BillBlockedColumnsTests
{
    private static MultiNationSim Gt() => new([WorldData.ByName("ジェノヴァ"), WorldData.ByName("トスカーナ")], seed: 1789) { DiplomacyFrozen = true };
    private static MultiNationSim Two() => new([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789) { DiplomacyFrozen = true };
    private static string NewDir() => System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_c80eb_" + Guid.NewGuid().ToString("N"));
    private static void Done(string dir) { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }

    /// <summary>輸入する買い手の州を置いて 1 回の清算（Market.Step）を通し、買い手の州の台帳を返す。<paramref name="setup"/> は商人と他の POP の債務・貯蓄を置く（貯蓄 1e4 の下地の後）。</summary>
    internal static Instruments.CearnAcc RunClearing(Action<Region, Pop, List<Pop>> setup, out Region buyer)
    {
        var sim = Gt();
        sim.Coef.ClearingIdleSearchQuarters = 1;
        var gd = sim.Nations[0].World.Region.Goods
            .First(g => !g.IsColonial && !g.NonTradeable && g.Tier != GoodTier.Staple && !g.GrainChain && Array.IndexOf(sim.Coef.MilitaryExportGoods, g.Id) < 0);
        double wp = sim.Market.Price[gd.Id];
        var all = new List<Region>();
        foreach (var n in sim.Nations) { n.World.Tariff = 0; n.World.RailCapacity = 1e9; n.World.ShipCapacity = 1e9; Instruments.Begin(n.World); foreach (var r in n.World.Regions) all.Add(r); }
        var seller = all.Where(r => r.IsPort).First();
        buyer = all.First(r => !ReferenceEquals(r, seller) && r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count
                              && r.Pops.Count(p => p.Size > 1e-6 && !ReferenceEquals(p, r.Pops[r.MerchantPopIndex])) >= 2);
        foreach (var r in all)
        {
            var m = r.Markets[gd.Id]; m.LastDemand = 0; m.LastProduced = 0; m.LastExport = 0; m.LastImport = 0;
            if (ReferenceEquals(r, seller)) { m.Price = 0.5 * wp; m.Stock = 5000; m.TargetStock = 10; m.LastDemand = 10; m.LastProduced = 10; }
            else if (ReferenceEquals(r, buyer)) { m.Price = 2.0 * wp; m.Stock = 0; m.TargetStock = 50; m.LastDemand = 500; m.LastProduced = 0; }
            else { m.Price = wp; m.Stock = 0; m.TargetStock = 0; }
        }
        foreach (var r in all) foreach (var p in r.Pops) { p.Debt = new MoneyStock(0); if (p.Size > 1e-6) p.SetSavings(Math.Max(p.Savings.Value, 1e4)); }
        var mer = buyer.Pops[buyer.MerchantPopIndex];
        setup(buyer, mer, buyer.Pops.Where(p => !ReferenceEquals(p, mer)).ToList());
        var a = Instruments.CearnAccOf(buyer)!; a.Clear();
        sim.Market.Step(sim.Nations, sim.Coef);
        return a;
    }

    [Fact]
    public void 商人が手形を振り出せない清算を数え_塵は債務が1e12以下の分だけ()
    {
        string dir = NewDir(); Instruments.Configure(dir, "*");
        try
        {
            // 債務 5・貯蓄 1：決済で 1 返しても 4 残る＝手形を振り出せない（塵ではない）
            var a1 = RunClearing((r, m, o) => { m.Debt = new MoneyStock(5); m.SetSavings(1); }, out _);
            Assert.True(a1.BillBlocked > 0, "前提が崩れた：清算の頭が数えられていない");
            Assert.Equal(0, a1.BillBlockedDust);
            // 債務 1e-13・貯蓄 0：返せず残る＝塵（閾値 1e-12 以下）
            var a2 = RunClearing((r, m, o) => { m.Debt = new MoneyStock(1e-13); m.SetSavings(0); }, out _);
            Assert.True(a2.BillBlocked > 0);
            Assert.Equal(a2.BillBlocked, a2.BillBlockedDust);
            // 債務 5e-10・貯蓄 0：塵の閾値（1e-12）より大きい＝塵に数えない（閾値を 1e-9 へ動かす突然変異で赤）
            var a3 = RunClearing((r, m, o) => { m.Debt = new MoneyStock(5e-10); m.SetSavings(0); }, out _);
            Assert.True(a3.BillBlocked > 0);
            Assert.Equal(0, a3.BillBlockedDust);
            // 債務 0：手形を振り出せる＝数えない
            var a4 = RunClearing((r, m, o) => { m.Debt = new MoneyStock(0); m.SetSavings(100); }, out _);
            Assert.Equal(0, a4.BillBlocked); Assert.Equal(0, a4.BillBlockedDust);
            // 自己検証：塵 ≦ 振り出せなかった数
            Assert.True(a1.BillBlocked >= a1.BillBlockedDust && a2.BillBlocked >= a2.BillBlockedDust && a3.BillBlocked >= a3.BillBlockedDust);
        }
        finally { Done(dir); }
    }

    [Fact]
    public void 決済で返した額は人の居る非商人のPOPだけを数える_商人と人の居ないPOPは数えない()
    {
        string dir = NewDir(); Instruments.Configure(dir, "*");
        try
        {
            var a = RunClearing((r, m, o) =>
            {
                m.Debt = new MoneyStock(2); m.SetSavings(100);          // 商人（返す・数えない）
                var h = o.First(p => p.Size > 1e-6); h.Debt = new MoneyStock(3); h.SetSavings(100);   // 人の居る非商人（返す・数える）
                var e = o.First(p => !ReferenceEquals(p, h)); e.Size = 0; e.Debt = new MoneyStock(4); e.SetSavings(100);   // 人の居ない非商人（返す・数えない）
            }, out _);
            Assert.Equal(3.0, a.HeirDebtSettled, 9);
        }
        finally { Done(dir); }
    }

    private static Region ManyPopsRegion(World w) => w.Regions.First(r => r.Pops.Count(p => p.Size > 1e-6) >= 3);

    [Fact]
    public void 相続の段の直後の人の居ない債務者を数え_返し以外で減った債務を別に出す()
    {
        string dir = NewDir(); Instruments.Configure(dir, "*");
        try
        {
            foreach (double debtFirst in new[] { 0.0, 1.0 })
            {
                var sim = Two(); var w = sim.Nations[0].World; w.Coef.EmptyPopDebtFirst = debtFirst; w.Coef.EmptyPopDebtToHeirs = 0;   // World.Coef は国ごと別インスタンス・(936) 前提を既定に頼らず明示（B1 は切る）
                 Instruments.Begin(w);
                var r = ManyPopsRegion(w);
                foreach (var p in r.Pops) if (!(p.Size > 1e-12)) p.Debt = new MoneyStock(0);
                var alive = r.Pops.Where(p => p.Size > 1e-6 && !ReferenceEquals(p, r.MerchantPop)).ToList();
                var e1 = alive[0]; var e2 = alive[1];
                e1.Size = 0; e1.Debt = new MoneyStock(7); e1.SetSavings(100);
                e2.Size = 0; e2.Debt = new MoneyStock(1e-13); e2.SetSavings(0);
                var a = Instruments.CearnAccOf(r)!; a.Clear();
                EmptyPopEstate.Step(w);
                if (debtFirst == 0.0) { Assert.Equal(2, a.EmptyDebtAfter); Assert.Equal(0.0, a.EstDebtSettled); }
                else { Assert.Equal(1, a.EmptyDebtAfter); Assert.Equal(7.0, a.EstDebtSettled, 9); }   // 7 を貯蓄から返した（e1 は消え e2 だけ残る）
                Assert.Equal(0.0, a.EstDebtMoved, 9);   // 返し以外に債務は動いていない（旗 0 も 1 も 0）
            }
            // 返し以外で債務が減る（債務を相続人へ移す案が入ったときの形）：頭と直後の間に手で 7 → 4 にする
            {
                var sim = Two(); var w = sim.Nations[0].World; Instruments.Begin(w);
                var r = ManyPopsRegion(w);
                foreach (var p in r.Pops) if (!(p.Size > 1e-12)) p.Debt = new MoneyStock(0);
                var e1 = r.Pops.First(p => p.Size > 1e-6 && !ReferenceEquals(p, r.MerchantPop));
                e1.Size = 0; e1.Debt = new MoneyStock(7); e1.SetSavings(0);
                var a = Instruments.CearnAccOf(r)!; a.Clear();
                Instruments.EstateDue(w); e1.Debt = new MoneyStock(4); Instruments.EstateAfter(w);
                Assert.Equal(3.0, a.EstDebtMoved, 9); Assert.Equal(1, a.EmptyDebtAfter);
            }
        }
        finally { Done(dir); }
    }

    private static string[] HeaderOf(string file) => System.IO.File.ReadAllLines(file)[0].Split('\t');

    [Fact]
    public void 尾の列の名前は並びの通りで_行の幅はヘッダーと同じ()
    {
        string dir = NewDir(); Instruments.Configure(dir, "*");
        try
        {
            var sim = Two(); var w = sim.Nations[0].World; Instruments.Begin(w);
            sim.StepQuarter(); Instruments.Quarter(sim, 5UL, 0); Instruments.Close(sim);
            var cf = System.IO.Directory.GetFiles(dir, "cearn_s*").Single();
            var ch = HeaderOf(cf);
            Assert.Contains("\tmerchantDebt\testateDebtSettledPerQuarter\tbillBlockedClearings\tbillBlockedDustClearings\temptyDebtAfterEstate\testateDebtMovedPerQuarter\theirDebtSettledPerQuarter", Instruments.CearnHeaderText);
            Assert.EndsWith("\theirDebtSettledPerQuarter", Instruments.CearnHeaderText);
            foreach (var l in System.IO.File.ReadAllLines(cf).Skip(1)) Assert.Equal(ch.Length, l.Split('\t').Length);
            var pf = System.IO.Directory.GetFiles(dir, "cpop_s*").Single();
            var ph = HeaderOf(pf);
            Assert.EndsWith("\tdebt\toutOtherTop1Route\toutOtherTop1PerQuarter\tdebtInCarryPerQuarter", Instruments.CpopHeaderText);
            foreach (var l in System.IO.File.ReadAllLines(pf).Skip(1)) Assert.Equal(ph.Length, l.Split('\t').Length);
        }
        finally { Done(dir); }
    }

    [Fact]
    public void 運搬で受けた債務は受け手の欄に出て_outOtherの上位の経路は額の大きい順()
    {
        string dir = NewDir(); Instruments.Configure(dir, "*");
        try
        {
            var sim = Two(); Region? col = null; World? cw = null;
            foreach (var n in sim.Nations) foreach (var r in n.World.Regions)
                if (col is null && r.Pops.Count(p => p.Size > 1e-3) >= 2) { col = r; cw = n.World; }
            Assert.NotNull(col);
            col!.Tier = RegionTier.Colonial;   // cpop_s の母集合（植民地格の州だけ）へ入れる
            Instruments.Begin(cw!);
            var alive = col!.Pops.Where(p => p.Size > 1e-3).ToList();
            var from = alive[0]; var to = alive[1];
            from.Debt = new MoneyStock(10); to.Debt = new MoneyStock(1);
            double half = from.Size / 2;
            Pop.Blend(from, to, half);   // 運ぶ債務＝10×(1/2)＝5（債務の全額 10 や受け手の合計 6 ではない）
            // outOther の経路：同じ POP に 2 つの経路（額の大きい方が上位）
            Instruments.Route(@"C:\x\Foo.cs", 12, 'P', 'H'); Instruments.Sav(to, Instruments.SavKind.Take, -5);
            Instruments.Route(@"C:\x\Bar.cs", 7, 'P', 'H'); Instruments.Sav(to, Instruments.SavKind.Take, -9);
            string nation = Instruments.NameOf(cw!);
            Instruments.Quarter(sim, 5UL, 0); Instruments.Close(sim);
            var pf = System.IO.Directory.GetFiles(dir, "cpop_s*").Single();
            var lines = System.IO.File.ReadAllLines(pf); var h = lines[0].Split('\t');
            int Ix(string c) => Array.IndexOf(h, c);
            var rows = lines.Skip(1).Select(l => l.Split('\t')).Where(c => c[Ix("nation")] == nation && c[Ix("region")] == col.Name).ToList();
            var rt = rows.Single(c => c[Ix("popIndex")] == col.Pops.IndexOf(to).ToString());
            var rf = rows.Single(c => c[Ix("popIndex")] == col.Pops.IndexOf(from).ToString());
            double D(string[] c, string n) => double.Parse(c[Ix(n)], System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(5.0, D(rt, "debtInCarryPerQuarter"), 9);
            Assert.Equal(0.0, D(rf, "debtInCarryPerQuarter"), 9);
            Assert.Equal("Bar.cs:7:take:H", rt[Ix("outOtherTop1Route")]);
            Assert.Equal(9.0, D(rt, "outOtherTop1PerQuarter"), 9);
            Assert.Equal("-", rf[Ix("outOtherTop1Route")]);
            Assert.True(double.IsNaN(D(rf, "outOtherTop1PerQuarter")));
        }
        finally { Done(dir); }
    }
}
