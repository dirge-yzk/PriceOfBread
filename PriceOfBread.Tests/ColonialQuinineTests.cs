using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1153) 線 c113qn：植民地の毎月のキニーネ（Colonial.cs の quinineWant＝係数 MalariaQuininePerColony×マラリアの植民地格の州）を係数 0 で閉じる線の機構の番人と、計器 colqn_s の自己検査。
/// 事前登録 measurements/m1153_20261115_prereg_qn.txt の xUnit Q1〜Q3。盤面＝英仏の英（1 四半期の後）・植民地格の州 2 つにだけマラリアの印・資金の比 1・キニーネの在庫 1e6・値段 1e-3。
/// 期待の値は手で書いた数（6×2＝12・係数 0 で 0）＝同じ出所の式を写さない。
/// </summary>
[Collection("Instruments")]
public class ColonialQuinineTests
{
    const ulong Seed = 1789UL;

    static MultiNationSim NewSim(double perColony)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: Seed, coef: new Coefficients()) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations) n.World.Coef.MalariaQuininePerColony = perColony;
        return sim;
    }

    /// <summary>手で置く盤面：英の植民地格の州のうち最初の 2 つにだけマラリアの印。燃料が要る状態（汽船 1.0・船腹 1 万）。キニーネと石炭の在庫 1e6。</summary>
    static World Board(MultiNationSim sim)
    {
        var w = sim.Nations[0].World;
        int k = 0;
        foreach (var r in w.Regions) r.Malarial = r.Tier == RegionTier.Colonial && k++ < 2;
        int mal = 0; foreach (var r in w.Regions) if (r.Tier == RegionTier.Colonial && r.Malarial) mal++;
        Assert.Equal(2, mal);   // 前提：マラリアの植民地格の州がちょうど 2 つ
        w.FiscalFundingRatio = new Share(1.0);
        w.ShipSteamShare = new Share(1.0); w.ShipCapacity = 10_000;
        var m = w.Region.Markets;
        m[Catalog.QuinineId].Stock = 1e6; m[Catalog.QuinineId].Price = 1e-3;
        m[Catalog.CoalId].Stock = 1e6; m[Catalog.CrudeOilId].Stock = 1e6;   // 燃料は石炭か原油（ChooseFuelGood）
        return w;
    }

    static (double Quinine, double Coal) StepOnce(double perColony)
    {
        var sim = NewSim(perColony);
        sim.StepQuarter();
        var w = Board(sim);
        var m = w.Region.Markets;
        double q0 = m[Catalog.QuinineId].Stock, c0 = m[Catalog.CoalId].Stock + m[Catalog.CrudeOilId].Stock;
        Colonial.Step(w, w.Coef);
        return (q0 - m[Catalog.QuinineId].Stock, c0 - m[Catalog.CoalId].Stock - m[Catalog.CrudeOilId].Stock);
    }

    [Fact]
    public void Q1_係数6でマラリアの州2つに月12のキニーネを買い_係数0で0_燃料は同じ()
    {
        var on = StepOnce(6.0);
        var off = StepOnce(0.0);
        Assert.Equal(12.0, on.Quinine, 6);
        Assert.Equal(0.0, off.Quinine);
        Assert.True(on.Coal > 0, $"前提：燃料（石炭か原油）の調達が 0（{on.Coal:R}）");
        Assert.Equal(on.Coal, off.Coal);   // 燃料の調達は係数に依らない
    }

    static (string[] Head, string[] Row) InsRow(double perColony, out string dir)
    {
        dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_qn_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        Instruments.SelectTables("colqn_s");
        var sim = NewSim(perColony);
        string uk;
        try
        {
            Instruments.Phase(sim, Seed, "open");
            sim.StepQuarter(); Instruments.Quarter(sim, Seed, 0);   // ここまでの月を q=0 の行へ流す
            var w = Board(sim);
            uk = Instruments.NameOf(w);
            Colonial.Step(w, w.Coef);                                // q=1 の行＝手で置いた盤面の 1 か月だけ
            Instruments.Quarter(sim, Seed, 1);
        }
        finally { Instruments.Close(sim); }
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"colqn_s{Seed}.tsv"));
        var head = lines[0].Split('\t');
        Assert.Equal(Instruments.ColqnHeaderText.Split('\t'), head);
        int iq = Array.IndexOf(head, "q"), inat = Array.IndexOf(head, "nation");
        string[]? row = null; int rows = 0;
        foreach (var l in lines.Skip(1))
        {
            var x = l.Split('\t'); Assert.Equal(head.Length, x.Length); rows++;
            if (x[iq] == "1" && x[inat] == uk) row = x;
        }
        Assert.True(rows >= 4, $"前提：行の数 {rows}（2 国×2 四半期）");
        Assert.NotNull(row);
        return (head, row!);
    }

    static double Col(string[] head, string[] row, string name)
    {
        int i = Array.IndexOf(head, name); Assert.True(i >= 0, name);
        return double.Parse(row[i], System.Globalization.CultureInfo.InvariantCulture);
    }

    [Fact]
    public void Q2_計器colqn_sは係数6で旧の規則12と実の調達12が一致し_係数0で旧の規則12と実の調達0()
    {
        string d0 = "", d1 = "";
        try
        {
            var (h, on) = InsRow(6.0, out d0);
            var (_, off) = InsRow(0.0, out d1);
            Assert.Equal(1.0, Col(h, on, "months")); Assert.Equal(1.0, Col(h, off, "months"));
            Assert.Equal(2.0, Col(h, on, "malRegionMonths")); Assert.Equal(2.0, Col(h, off, "malRegionMonths"));
            Assert.Equal(12.0, Col(h, on, "oldWant")); Assert.Equal(12.0, Col(h, off, "oldWant"));
            Assert.Equal(12.0, Col(h, on, "obsGot"), 6);
            Assert.Equal(12.0, Col(h, on, "obsStockDrop"), 6);
            Assert.Equal(0.0, Col(h, on, "maxAbsDiffGot"));
            Assert.True(Col(h, on, "maxRelDiffReq") <= 1e-12, "旧の規則の求めと実の需要の増えが合わない");
            Assert.True(Col(h, on, "obsClaim") > 0, "係数 6 でキニーネの請求が 0");
            Assert.Equal(12.0, Col(h, off, "oldBuyable"), 6);   // 係数 0 の木でも旧の規則で買えた量を同じ盤面で数える
            Assert.Equal(0.0, Col(h, off, "obsGot")); Assert.Equal(0.0, Col(h, off, "obsDemand")); Assert.Equal(0.0, Col(h, off, "obsClaim"));
            Assert.Equal(Col(h, on, "fuelClaim"), Col(h, off, "fuelClaim"));
            Assert.True(Col(h, on, "fuelClaim") > 0, "前提：燃料の請求が 0");
        }
        finally
        {
            Instruments.SelectTables(null); Instruments.Configure(null, null);
            foreach (var d in new[] { d0, d1 }) if (d.Length > 0) try { System.IO.Directory.Delete(d, true); } catch (System.IO.IOException) { }
        }
    }

    static double InvadeQuinine(double perColony)
    {
        var sim = NewSim(perColony);
        sim.StepQuarter();
        var w = sim.Nations[0].World; var me = w.SelfPower!;
        var target = sim.Diplomacy.Powers.Find(p => p.Nation is null && p.RequiresQuinine && p.ColonyList.Count > 0 && sim.Diplomacy.Suzerain(p) is null);
        Assert.NotNull(target);   // 前提：キニーネの標的（熱帯の抽象標的）がある
        w.ExpansionWill = 1e12; me.Military = 1e9; target!.Military = 1.0;
        w.FiscalFundingRatio = new Share(1.0);
        var m = w.Region.Markets;
        foreach (int g in new[] { Catalog.SailingShipId, Catalog.SteamshipId, Catalog.CannedFoodId, Catalog.QuinineId }) m[g].Stock = 1e6;
        Assert.True(w.CanInvade(target, out string why), $"前提が崩れた：侵攻できない（{why}）");
        double q0 = m[Catalog.QuinineId].Stock;
        w.Invade(target);
        return q0 - m[Catalog.QuinineId].Stock;
    }

    [Fact]
    public void Q3_遠征のキニーネは係数6と0で同じ量を買う()
    {
        double on = InvadeQuinine(6.0), off = InvadeQuinine(0.0);
        Assert.True(off > 0, $"係数 0 で遠征のキニーネを買わない（{off:R}）");
        Assert.True(Math.Abs(on - off) <= 1e-9, $"遠征のキニーネが係数で変わる（{on:R}・{off:R}）");
    }
}
