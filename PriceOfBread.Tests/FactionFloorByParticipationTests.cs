using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1422) c152sd 旗 FactionFloorByParticipation：勢力の足切りを 0.01×国の人口から 0.01×参加の総量 P_n（Σ N_p π_p）へ。
/// 盤面は試験の中で置く（仏 1789 を 8 か月進めた世界の POP を書き換える・旗 SatReadersToDiscontent 1＝参加は不満度 D だけ＝D＝0.8×落差）。
/// 組の大きさは落差で手計算の値に置く（期待値は 0.8×落差×頭数のリテラルの式で、本体の足切りの関数から作らない）。
/// 旗 0 の対照を各試験の中に持つ（T1・T2・TG3 は旗 0 で結論が逆になる＝旗 0 で赤）。突然変異の表は measurements/m1422_20261127_prereg_sd.txt の [9]。</summary>
[Collection("Instruments")]
public class FactionFloorByParticipationTests
{
    private const int Lib = (int)Ideology.Liberalism;

    private static World Board(double floorFlag, NationConfig? cfg = null)
    {
        var sim = new Simulation((cfg ?? NationConfig.France()).ToScenario(1789), new NullAgent());
        sim.Run(8);
        var w = sim.World; var c = w.Coef;
        c.SatReadersToDiscontent = 1;
        c.FactionFloorByParticipation = floorFlag;
        c.ConsciousnessSpeed = 0;
        Assert.Equal(0.8, c.WantWeight);              // D＝0.8×落差（手計算の前提）
        Assert.Equal(0.01, FactionSnapshot.MinSizeFraction);
        Assert.Equal(0.05, c.FactionCohesionGrowth);  // T3 の手計算の前提
        Assert.True(w.ParliamentEnabled && w.FactionRng is not null, "前提：Factions.Step が走る世界");
        w.Suffrage = Suffrage.None;
        foreach (var r in w.Regions)
            foreach (var p in r.Pops)
            {
                p.Militancy = 0.5; p.Consciousness = 0; p.Literacy = 0.9; p.Satisfaction = 0.5; p.LivingRef = 0.5; p.CoreEthnicShare = new Share(1);
                Array.Clear(p.IdeologyShares); p.IdeologyShares[Lib] = 1.0;
            }
        w.Factions.Clear();
        return w;
    }

    private static List<Pop> Mass(Region r) => r.Pops.Where(p => p.Size > 1e-9 && !p.Closed && !p.IsModernElite).ToList();

    /// <summary>州 r の大衆の組の参加の頭数を s に置く（D＝0.8×落差・落差＝s÷(0.8×頭数)）。</summary>
    private static void SetCell(Region r, double s)
    {
        var ps = Mass(r);
        double n = ps.Sum(p => p.Size);
        double drop = s / (0.8 * n);
        Assert.InRange(drop, 0.0, 1.25);   // D≦1（クランプに当たらない）
        foreach (var p in ps) p.LivingRef = p.Satisfaction + drop;
    }

    /// <summary>大衆の頭数が大きい順の州 2 つ。</summary>
    private static (Region A, Region B) TwoRegions(World w)
    {
        var ordered = w.Regions.OrderByDescending(r => Mass(r).Sum(p => p.Size)).ThenBy(r => r.Id).ToList();
        return (ordered[0], ordered[1]);
    }

    private static HashSet<(int, int, int)> Created(World w)
        => w.Factions.Select(f => (f.Region.Id, f.Bloc, f.Ideo)).ToHashSet();

    [Fact]
    public void T1_人口の1パーセント未満でも参加の総量の1パーセント以上の組は旗1で勢力になる()
    {
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var w = Board(flag);
            var (a, _) = TwoRegions(w);
            double n = w.TotalPopulation;
            SetCell(a, 0.005 * n);
            var cl = FactionSnapshot.ComputeClusters(w);
            Assert.Single(cl);
            Assert.Equal(0.005 * n, cl[0].Size, 0.005 * n * 1e-9);
            Factions.Step(w, w.Coef);
            var made = Created(w);
            if (flag > 0) Assert.Equal(new HashSet<(int, int, int)> { (a.Id, 2, Lib) }, made);   // 0.005N ≧ 0.01×P_n（P_n＝0.005N）
            else Assert.Empty(made);                                                           // 対照：0.005N ＜ 0.01N
        }
    }

    [Fact]
    public void T2_全ての不満度を半分にしても旗1では勢力の組が変わらない()
    {
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var w = Board(flag);
            var (a, b) = TwoRegions(w);
            double n = w.TotalPopulation;
            SetCell(a, 0.015 * n); SetCell(b, 0.002 * n);
            var before = FactionSnapshot.Compute(w).Select(f => f.Name).ToHashSet();
            foreach (var r in w.Regions) foreach (var p in r.Pops) p.LivingRef = p.Satisfaction + (p.LivingRef - p.Satisfaction) * 0.5;
            var after = FactionSnapshot.Compute(w).Select(f => f.Name).ToHashSet();
            if (flag > 0)
            {
                Assert.Equal(2, before.Count);
                Assert.Equal(before, after);
            }
            else
            {
                Assert.Single(before);   // 対照：0.015N だけが 0.01N を越える
                Assert.Empty(after);     // 半分の 0.0075N は越えない＝尺度で組が変わる
            }
        }
    }

    [Fact]
    public void T3_作った組は同じStepの中で見えないと読まれない()
    {
        var w = Board(1.0);
        var (a, _) = TwoRegions(w);
        SetCell(a, 0.005 * w.TotalPopulation);
        Factions.Step(w, w.Coef);
        var f = Assert.Single(w.Factions);
        Assert.Equal(0.025, f.Cohesion, 12);   // 0.05×(0.5＋意識 0)＝0.025（見えない組の減衰 ×0.9 を受けていない）
        Factions.Step(w, w.Coef);
        f = Assert.Single(w.Factions);
        Assert.Equal(0.025 + 0.025 * (1 - 0.025), f.Cohesion, 12);
    }

    [Fact]
    public void T4_選挙権の無い大衆の組は強さを持ち_同じ組でも男子普通選挙なら強さは0()
    {
        var w = Board(1.0); var c = w.Coef;
        Assert.Equal(0.0, c.RebelArmsMarketEnabled);
        Assert.Equal(0.0, c.RevoltVoiceFloor);
        var (a, _) = TwoRegions(w);
        var f = new FactionState { Region = a, Bloc = 2, Ideo = Lib, Size = 1000, AvgMilitancy = 0.5, Cohesion = 0.9 };
        w.Suffrage = Suffrage.None;
        Assert.Equal(450.0, Politics.FactionStrength(w, c, f), 9);   // 1000×0.5×0.9×(1−声 0)
        w.Suffrage = Suffrage.AdultMale;
        Assert.Equal(0.0, Politics.FactionStrength(w, c, f), 12);
    }

    [Fact]
    public void T5_足切りを越えた組の数は1割るファイ以下()
    {
        var w = Board(1.0, NationConfig.Russia());   // 組の数が 1/φ を超える盤面（州の多い国）
        var rng = new Random(1422);
        foreach (var r in w.Regions)
            foreach (var p in r.Pops)
            {
                Array.Clear(p.IdeologyShares); p.IdeologyShares[rng.Next(Ideologies.Count)] = 1.0;
                p.LivingRef = p.Satisfaction + 0.05 + 0.5 * rng.NextDouble();
            }
        int cells = FactionSnapshot.ComputeClusters(w).Count;
        Assert.True(cells > 100, $"前提：組の数 {cells} が 1/φ＝100 を超える盤面");
        var bodies = FactionSnapshot.Compute(w);
        Assert.InRange(bodies.Count, 1, 100);
        Factions.Step(w, w.Coef);
        Assert.InRange(w.Factions.Count, 1, 100);
    }

    [Fact]
    public void TG3_旗1では実際に作った勢力の中の対象の層が旧の足切りより多い_旗0では等しい()
    {
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var w = Board(flag);
            var (a, b) = TwoRegions(w);
            double n = w.TotalPopulation;
            SetCell(a, 0.005 * n); SetCell(b, 0.02 * n);
            Factions.Step(w, w.Coef);
            var x = Instruments.FacfloorCount(w);
            Assert.Equal(0.025 * n, x.TargetK, 0.025 * n * 1e-9);         // 声 0・意識 0＝対象の層の重みは参加そのもの
            Assert.Equal(0.02 * n, x.TargetAboveNK, 0.02 * n * 1e-9);     // 旧の足切りを越えるのは 0.02N の組だけ
            if (flag > 0) Assert.Equal(0.025 * n, x.FacSeenTargetK, 0.025 * n * 1e-9);   // 両方の組が勢力になった
            else Assert.Equal(x.TargetAboveNK, x.FacSeenTargetK);                          // 対照：旗 0 では旧の足切りと同じ
            Assert.Equal(flag > 0 ? 2 : 1, x.FacSeenCount);
        }
    }
}
