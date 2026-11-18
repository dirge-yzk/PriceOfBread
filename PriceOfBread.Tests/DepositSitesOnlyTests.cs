using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1202) 線 c120t2：貿易の T2-1 案 S（旗 DepositSitesOnly）＝枯れるのは開いた鉱床だけ。事前登録 measurements/m1202_20261118_prereg_t2.txt §2。
/// T1 ふつうの鉱物の生産者は州の品位を読まない／T1′ 産地の生産者の能力は州の品位に依らない／T2 産地の品位は残りに比例／
/// T3 捕捉は開いた後の最初の産出の月／T4 ふつうの生産者は在庫を持たない／T5 産地の在庫は四半期の途中のセーブで往復する／T6 計器 depmin_s。
/// 盤面は試験の中で置く。期待の数は試験に数で書く（係数から作らない）。旗は <see cref="Flag"/> 1 か所（旗 0 にすると T1〜T4・T1′ が赤）。
/// </summary>
public class DepositSitesOnlyTests
{
    private const double Flag = 1.0;
    private static Coefficients On() => new() { DepositSitesOnly = Flag };

    /// <summary>手組みの 2 階級の州（財 0 は小世界で鉱物扱い）。州の品位を数で直に置く。</summary>
    private static Region MineRegion(double regionGrade)
    {
        var ore = new Good { Id = 0, Name = "鉱石", Tier = GoodTier.Staple, BaseNeed = 0.8, RefPrice = 1.0, Elasticity = 0.3 };
        var laborer = new Pop { ClassId = 0, ClassName = "農業労働者", IsUrban = false, Size = 1000, Savings = new MoneyStock(500_000) };
        var owner = new Pop { ClassId = 1, ClassName = "地主", IsUrban = false, Size = 50, Savings = new MoneyStock(500_000) };
        var rgo = new Producer { GoodId = 0, Productivity = 0.85, Output = 800, MarginalCost = 0.5, OwnerPopIndex = 1, LaborPopIndex = 0, RentShare = new Share(0.3), IsRgo = true };
        var market = new Market { Price = 1.0, Stock = 1200, TargetStock = 1200 };
        return new Region { Id = 0, Goods = [ore], Pops = [laborer, owner], Producers = [rgo], Markets = [market], MineralGradeFactor = regionGrade };
    }

    private static int SulfurSiteRow() { int row = Array.FindIndex(Catalog.Sites, x => x.Name == "九州の硫黄"); Assert.True(row >= 0); return row; }

    private static Region SiteRegion(double regionGrade, double init, double left)
    {
        var r = MineRegion(regionGrade);
        var p = r.Producers[0];
        p.SiteRow = SulfurSiteRow(); p.SiteFound = true; p.SiteDepositInitial = init; p.SiteDeposit = left;
        return r;
    }

    [Fact]
    public void T1_ふつうの鉱物の生産者は州の品位を読まない()
    {
        var c = On();
        var poor = MineRegion(0.15); var rich = MineRegion(1.0);
        Assert.True(Catalog.IsMineral(poor.Producers[0].GoodId) && poor.Producers[0].SiteRow < 0);
        EconomyStep.Run(poor, c); EconomyStep.Run(rich, c);
        Assert.True(rich.Producers[0].Capacity > 0);
        Assert.Equal(rich.Producers[0].Capacity, poor.Producers[0].Capacity);
    }

    [Fact]
    public void T1prime_産地の生産者の能力は州の品位に依らない()
    {
        var c = On();
        var poor = SiteRegion(0.15, 1000, 500); var rich = SiteRegion(1.0, 1000, 500);
        EconomyStep.Run(poor, c); EconomyStep.Run(rich, c);
        Assert.True(rich.Producers[0].Capacity > 0);
        Assert.Equal(rich.Producers[0].Capacity, poor.Producers[0].Capacity);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(0.15)]
    public void T2_産地の品位は残りに比例する(double regionGrade)
    {
        var c = On();
        var half = SiteRegion(regionGrade, 1000, 500); var full = SiteRegion(regionGrade, 1000, 1000);
        EconomyStep.Run(half, c); EconomyStep.Run(full, c);
        Assert.True(full.Producers[0].Capacity > 0);
        double ratio = half.Producers[0].Capacity / full.Producers[0].Capacity;
        Assert.True(Math.Abs(ratio - 0.575) <= 1e-12, $"残り半分の能力の比 {ratio:R}（期待 0.575＝0.15＋0.85×0.5）");
    }

    private static (World W, Region R, Producer P) Site(MultiNationSim sim, string siteName)
    {
        int row = Array.FindIndex(Catalog.Sites, x => x.Name == siteName);
        Assert.True(row >= 0, $"表に {siteName} が無い");
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) foreach (var p in r.Producers)
            if (p.SiteRow == row) return (n.World, r, p);
        throw new Xunit.Sdk.XunitException($"{siteName} の生産者が盤面に無い");
    }

    private static MultiNationSim WorldSim(ulong seed, double flag = Flag)
    {
        var sim = new MultiNationSim(WorldData.Nations(), seed, _ => new UtilityAgent());
        foreach (var n in sim.Nations) n.World.Coef.DepositSitesOnly = flag;   // World.Coef は国ごと別の欄
        return sim;
    }

    [Fact]
    public void T3_捕捉は開いた後の最初の産出の月()
    {
        var sim = WorldSim(7);
        sim.StepQuarter();
        var (w, r, p) = Site(sim, "九州の硫黄");
        Assert.Equal(Flag, w.Coef.DepositSitesOnly);
        Assert.False(p.SiteFound, "前提：開幕の 1 四半期で開いている");
        double ri = r.MineralDepositInitial, rd = r.MineralDeposit, rg = r.MineralGradeFactor;
        Discovery.OpenSite(w, r, p, w.Coef);
        Assert.True(p.SiteFound);
        Assert.Equal(-1.0, p.SiteDepositInitial);   // 開いただけでは捕えない
        p.Output = 0.0; Simulation.StepDeposits(w);
        Assert.Equal(-1.0, p.SiteDepositInitial);   // 産出 0 の月は捕えない
        p.Output = 3.0; Simulation.StepDeposits(w);
        Assert.Equal(2160.0, p.SiteDepositInitial);   // 3.0×12×60
        Assert.Equal(2157.0, p.SiteDeposit);          // 同じ月の産出だけ減る
        Assert.Equal(ri, r.MineralDepositInitial); Assert.Equal(rd, r.MineralDeposit); Assert.Equal(rg, r.MineralGradeFactor);   // 州の在庫へ足さない
    }

    [Fact]
    public void T4_ふつうの生産者は在庫を持たない()
    {
        var sim = WorldSim(7);
        sim.StepQuarter();
        int n = 0, pos = 0, regions = 0;
        foreach (var nat in sim.Nations)
            foreach (var r in nat.World.Regions)
            {
                bool mine = false;
                foreach (var p in r.Producers)
                {
                    if (!Catalog.IsMineral(p.GoodId) || p.SiteRow >= 0) continue;
                    mine = true; n++; if (p.Output > 0) pos++;
                    Assert.Equal(-1.0, p.SiteDepositInitial); Assert.Equal(-1.0, p.SiteDeposit);
                }
                if (!mine) continue;
                regions++;
                Assert.Equal(-1.0, r.MineralDepositInitial); Assert.Equal(1.0, r.MineralGradeFactor);
            }
        Assert.True(n >= 100 && pos >= 1 && regions >= 10, $"調べた鉱物の生産者 {n}・産出＞0 {pos}・州 {regions}");
    }

    [Fact]
    public void T5_産地の在庫は四半期の途中のセーブで往復する()
    {
        var sim1 = WorldSim(1789);
        sim1.StartWarmup();
        sim1.StepQuarter();
        var (w, r, p) = Site(sim1, "九州の硫黄");
        if (!p.SiteFound) Discovery.OpenSite(w, r, p, w.Coef);
        for (int q = 0; q < 2; q++) sim1.StepQuarter();
        Assert.True(p.SiteDepositInitial > 0 && p.SiteDeposit < p.SiteDepositInitial, $"前提：捕捉が済んでいない（初期 {p.SiteDepositInitial}・残り {p.SiteDeposit}）");
        for (int d = 0; d < 45; d++) sim1.StepDay();
        int ni = sim1.Nations.FindIndex(x => ReferenceEquals(x.World, w)), ri = w.Regions.IndexOf(r), pi = r.Producers.IndexOf(p);
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var sim2 = SaveGame.Load(ms);
        var p2 = sim2.Nations[ni].World.Regions[ri].Producers[pi];
        Assert.Equal(p.SiteDepositInitial, p2.SiteDepositInitial); Assert.Equal(p.SiteDeposit, p2.SiteDeposit);
        for (int d = 0; d < 135; d++) { sim1.StepDay(); sim2.StepDay(); }
        Assert.Equal(p.SiteDepositInitial, p2.SiteDepositInitial); Assert.Equal(p.SiteDeposit, p2.SiteDeposit);
        for (int i = 0; i < sim1.Nations.Count; i++)
        {
            var a = sim1.Nations[i].World; var b = sim2.Nations[i].World;
            Assert.True(a.BaseMoney.Value == b.BaseMoney.Value && a.TotalPopulation == b.TotalPopulation, $"{a.SelfPower?.Name}：継続系列と復元系列が食い違う");
        }
    }

    [Theory]
    [InlineData(Flag)]
    [InlineData(0.0)]
    public void T6_計器depmin_sは欄を写し品位は欄からの式と一致する(double flag)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_dm_" + Guid.NewGuid().ToString("N"));
        const ulong seed = 7;
        Instruments.Configure(dir, "*");
        Instruments.SelectTables("depmin_s");
        var sim = WorldSim(seed, flag);
        try
        {
            Instruments.Phase(sim, seed, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, seed, "warm");
            for (int q = 0; q < 8; q++)
            {
                sim.StepQuarter();
                if (q == 0) { var (w, r, p) = Site(sim, "九州の硫黄"); if (!p.SiteFound) Discovery.OpenSite(w, r, p, w.Coef); }
                Instruments.Quarter(sim, seed, q);
            }
        }
        finally { Instruments.Close(sim); Instruments.SelectTables(null); Instruments.Configure(null, null); }
        try
        {
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"depmin_s{seed}.tsv"));
            var h = lines[0].Split('\t');
            Assert.Equal(new[] { "year", "q", "region", "ownerNation", "good", "recipe", "siteRow", "siteFound", "output", "capacity", "productivity", "heads", "scale", "regionGrade", "depInit", "depLeft", "grade" }, h);
            int C(string cn) => Array.IndexOf(h, cn);
            double D(string[] f, string cn) => double.Parse(f[C(cn)], System.Globalization.CultureInfo.InvariantCulture);
            int normal = 0, site = 0, captured = 0;
            foreach (var l in lines.Skip(1))
            {
                var f = l.Split('\t');
                bool isSite = int.Parse(f[C("siteRow")]) >= 0;
                if (isSite) site++; else normal++;
                double init = D(f, "depInit"), left = D(f, "depLeft"), g = D(f, "grade");
                if (flag > 0 && isSite && init > 0)
                {
                    captured++;
                    Assert.True(Math.Abs(g - (0.15 + 0.85 * left / init)) <= 1e-12, $"産地の品位 {g}（残り {left}・初期 {init}）");
                }
                else if (flag > 0) { Assert.Equal(-1.0, init); Assert.Equal(1.0, g); }
                else { Assert.Equal(-1.0, init); Assert.Equal(D(f, "regionGrade"), g); }
            }
            Assert.True(normal >= 100 && site >= 1, $"ふつうの行 {normal}・産地の行 {site}");
            if (flag > 0) Assert.True(captured >= 1, "旗 1 で捕捉した産地の行が無い");
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { } }
    }
}
