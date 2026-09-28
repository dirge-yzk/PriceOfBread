using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class TerritoryTests
{
    private static Scenario Frontier(ulong s) => new() { Seed = s, Kind = ScenarioKind.FrontierDevelopment };

    [Fact]
    public void 辺境はロックで始まり開発で解錠され移民が流入する()   // §A-8／§A-9
    {
        var sim = new Simulation(Frontier(1), new NullAgent());
        var frontier = sim.World.Regions[1];
        Assert.Equal(RegionState.Locked, frontier.State);
        double startPop = frontier.SumPopulation();

        sim.Run(400);

        Assert.Equal(RegionState.Active, frontier.State);                       // 解錠
        Assert.True(frontier.SumPopulation() > startPop + 100, $"辺境に移民が入らない（{frontier.SumPopulation():F0}）");
        Assert.True(sim.World.Regions[0].SumPopulation() < 1000, "本土から流出していない");
    }

    [Fact]
    public void 地域間移民でも人口と基礎貨幣は保存される()
    {
        var sim = new Simulation(Frontier(2), new NullAgent());
        double p0 = sim.World.TotalPopulation, m0 = sim.World.BaseMoney;

        var ex = Record.Exception(() => sim.Run(400));
        Assert.Null(ex);
        Assert.True(Math.Abs(sim.World.TotalPopulation - (p0 + sim.World.PopulationDelta)) < 1e-6 * p0, "人口保存の破れ");
        Assert.True(Math.Abs(sim.World.BaseMoney - m0) < 1e-3 * Math.Max(1, m0), "基礎貨幣保存の破れ");
    }

    [Fact]
    public void 同一シードで辺境開発は決定論的()
    {
        var a = new Simulation(Frontier(3), new NullAgent());
        var b = new Simulation(Frontier(3), new NullAgent());
        a.Run(200);
        b.Run(200);
        Assert.Equal(a.World.Regions[1].SumPopulation(), b.World.Regions[1].SumPopulation());
        Assert.Equal(a.World.Regions[0].SumPopulation(), b.World.Regions[0].SumPopulation());
    }

    // ===== 州の移し替えの1本（2026-09-28 処理の一元化 段2 B）の番人 =====

    private static MultiNationSim Board(params string[] names)
        => new(names.Select(WorldData.ByName).ToArray(), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };

    [Fact]
    public void 再併合の返還州は親の中で一意のIdを受ける()
    {
        // 旧＝返還州の Id を「親の州の数」で振った＝建国で末尾でない州が抜けて Id が歯抜けのとき、返還州に既存の州と同じ Id が付いた。
        var sim = Board("フランス", "イギリス");
        var pw = sim.Nations[0].World;
        Assert.True(pw.Regions.Count >= 3, "前提が崩れた：親の州が3つ未満（末尾でない州が抜けられない）");
        var leaving = pw.Regions[1];
        pw.PendingRebelRegions = [leaving]; pw.PendingRebelPolity = Polity.Republic;
        int n0 = sim.Nations.Count;
        sim.StepQuarter();
        Assert.True(sim.Nations.Count > n0, "前提が崩れた：反乱国家が建っていない");
        var rw = sim.Nations[^1].World;
        int maxId = pw.Regions.Max(r => r.Id);
        Assert.Equal(pw.Regions.Count, maxId);   // 前提＝歯抜け（旧の「州の数」がちょうど既存の最大 Id に当たる）

        sim.Diplomacy.PendingReintegrations.Add((pw.SelfPower!, rw.SelfPower!, false, pw.Polity));
        sim.ReabsorbNations();

        Assert.Contains(leaving, pw.Regions);
        Assert.Equal(pw.Regions.Count, pw.Regions.Select(r => r.Id).Distinct().Count());
        Assert.Equal(maxId + 1, leaving.Id);
        Assert.Equal(0, pw.Regions[0].Id);
    }

    [Fact]
    public void 国を除いた四半期も交易の接触は正しい国に付く()
    {
        // 四半期のペア貿易額 PairFlowQ は国の番号で引く。旧＝再併合で国を除いても行と列を残した＝後ろの国が1つ前の国の貿易で接触度を受け取った。
        var sim = Board("フランス", "イギリス", "スペイン");
        var fr = sim.Nations[0].World; var es = sim.Nations[2].World;
        fr.PendingRebelRegions = [fr.Regions[1]]; fr.PendingRebelPolity = Polity.Republic;
        es.PendingRebelRegions = [es.Regions[1]]; es.PendingRebelPolity = Polity.Republic;
        sim.StepQuarter();
        Assert.Equal(5, sim.Nations.Count);   // 前提：反乱国が2つ（仏の反乱国＝添字3・西の反乱国＝添字4）
        sim.StepQuarter();                    // 反乱国にも名目GDPを立てる（接触度の分母）
        var r1 = sim.Nations[3].World; var r2 = sim.Nations[4].World;
        Assert.True(r2.NominalGdp > 0, "前提が崩れた：西の反乱国の名目GDPが0");
        // 西の反乱国（添字4）と英（添字1）だけが交易した四半期
        int n = sim.Nations.Count;
        var pf = new double[n][];
        for (int i = 0; i < n; i++) pf[i] = new double[n];
        pf[4][1] = pf[1][4] = 1000;
        sim.Market.PairFlowQ = pf;

        sim.Diplomacy.PendingReintegrations.Add((fr.SelfPower!, r1.SelfPower!, false, fr.Polity));
        sim.ReabsorbNations();   // 仏の反乱国（添字3）を除く＝西の反乱国は添字3へ詰まる

        Assert.Same(r2, sim.Nations[3].World);
        var q = sim.Market.PairFlowQ!;
        Assert.Equal(sim.Nations.Count, q.Length);
        Assert.Equal(1000.0, q[3][1]); Assert.Equal(1000.0, q[1][3]);
        // 四半期末の伝播の段（StepDiffusion）が交易の接触度を西の反乱国に付ける（除いた国の隣の国ではなく）
        typeof(MultiNationSim).GetMethod("StepDiffusion", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(sim, null);
        Assert.True(r2.TradeContactShare > 0, $"交易した西の反乱国に接触度が付いていない（{r2.TradeContactShare}）");
        Assert.Equal(0.0, fr.TradeContactShare);
        Assert.Equal(0.0, es.TradeContactShare);
    }

    [Fact]
    public void 首都を失った国は遷都し距離の基準の座標も新しい首都へ移る()
    {
        // 旧＝割譲の首都の補修と国家形成の遷都（Formations）が別々の手順を持ち、どちらも World.Latitude/Longitude を構築時のまま残した。
        var sim = Board("フランス", "イギリス");
        var fr = sim.Nations[0].World; var br = sim.Nations[1].World;
        var oldCap = fr.Regions[0];
        double lat0 = fr.Latitude, lon0 = fr.Longitude;

        Territory.TransferRegion(fr, br, oldCap, Territory.Rehoming.Ceded);   // 講和で首都を取られた

        var cap = fr.Regions[0];
        Assert.NotSame(oldCap, cap);
        Assert.Equal(0, cap.Id);
        Assert.Single(fr.Regions, r => r.Id == 0);
        var cc = Places.Coord(cap.Name);
        Assert.False(cc.Lat == 0 && cc.Lon == 0, "前提が崩れた：新しい首都の座標が未登録");
        Assert.Equal((cc.Lat, cc.Lon), (fr.Latitude, fr.Longitude));
        Assert.NotEqual((lat0, lon0), (fr.Latitude, fr.Longitude));

        // 国家形成の遷都も同じ1本＝Id の入れ替え・座標・旧首都は植民地財の産出をやめる（植民地財は首都州だけ）
        var next = fr.Regions[^1]; int nextId = next.Id;
        cap.ColonialResources = [1];
        fr.SetCapital(next);
        Assert.Same(next, fr.Regions[0]);
        Assert.Equal(0, next.Id);
        Assert.Equal(nextId, cap.Id);
        Assert.Empty(cap.ColonialResources!);
        var nc = Places.Coord(next.Name);
        Assert.Equal((nc.Lat, nc.Lon), (fr.Latitude, fr.Longitude));
    }
}
