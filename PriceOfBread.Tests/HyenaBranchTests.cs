using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1145) 線 c112hy：D-b ハイエナ抑制の枝（DriftTension の DefeatShockTensionRelief×衝撃）を係数 0 で閉じる線の機構の番人と、計器 dipcool_s の自己検査。
/// 事前登録 measurements/m1145_20261115_prereg_hy.txt の xUnit H1〜H3。期待の値は手で置いた盤面の数（緊張 120・遺恨 1.0／0.5・衝撃 1.0）から手で書いた値（同じ出所の式を写さない）。
/// </summary>
[Collection("Instruments")]
public class HyenaBranchTests
{
    const ulong Seed = 1789UL;

    static long Key(Power a, Power b) => a.Id < b.Id ? ((long)a.Id << 32) | (uint)b.Id : ((long)b.Id << 32) | (uint)a.Id;

    static MultiNationSim NewSim(double relief)
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], Seed, _ => new UtilityAgent(), null);
        foreach (var n in sim.Nations) { n.World.Coef.CivilWarLandslideRatio = 0; n.World.Coef.DefeatShockTensionRelief = relief; }
        sim.DiplomacyFrozen = true;
        return sim;
    }

    /// <summary>手で置く盤面：組 H（衝撃 1.0 の国を含む・緊張 120・遺恨 1.0）・組 G（衝撃 0・緊張 120・遺恨 0.5）・組 L（緊張 50）。他の組は消す。</summary>
    static (long H, long G, long L, System.Collections.Generic.Dictionary<long, double> T) Board(MultiNationSim sim)
    {
        var d = sim.Diplomacy;
        var ps = d.Powers.FindAll(p => p.Nation is not null);
        Assert.True(ps.Count >= 4, $"前提：国のある力 {ps.Count}");
        foreach (var p in ps) p.Nation!.DefeatShock = 0;
        ps[0].Nation!.DefeatShock = 1.0;
        var tension = (System.Collections.Generic.Dictionary<long, double>)typeof(Diplomacy)
            .GetField("_tension", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(d)!;
        tension.Clear(); d._grudge.Clear();
        long h = Key(ps[0], ps[1]), g = Key(ps[2], ps[3]), l = Key(ps[1], ps[2]);
        Assert.False(d.IsDependentOf(ps[0], ps[1]) || d.IsDependentOf(ps[1], ps[0]) || d.IsDependentOf(ps[2], ps[3]) || d.IsDependentOf(ps[3], ps[2]), "前提：手で置いた組に従属がある");
        tension[h] = 120.0; tension[g] = 120.0; tension[l] = 50.0;
        d._grudge[h] = 1.0; d._grudge[g] = 0.5;
        return (h, g, l, tension);
    }

    static void Drift(MultiNationSim sim) =>
        typeof(Diplomacy).GetMethod("DriftTension", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(sim.Diplomacy, [sim.Nations[0].World.Coef]);

    [Fact]
    public void H1_係数2では衝撃の組がハイエナの枝で冷める_遺恨の組と低い帯は平時の式()
    {
        var sim = NewSim(2.0);
        var b = Board(sim);
        Drift(sim);
        Assert.Equal(118.0, b.T[b.H], 12);    // 120 − 2.0×1.0
        Assert.Equal(119.79, b.T[b.G], 12);   // 120 − 0.42×(1−0.5)
        Assert.Equal(49.58, b.T[b.L], 12);    // 50 − 0.42
    }

    [Fact]
    public void H2_係数0では衝撃の組も遺恨の式で冷める_遺恨1なら動かない()
    {
        var sim = NewSim(0.0);
        var b = Board(sim);
        Drift(sim);
        Assert.Equal(120.0, b.T[b.H], 12);    // 遺恨 1.0＝率 0
        Assert.Equal(119.79, b.T[b.G], 12);
        Assert.Equal(49.58, b.T[b.L], 12);
    }

    static string[] InsRow(double relief, out string[] head, out string dir)
    {
        dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_hy_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        Instruments.SelectTables("dipcool_s");
        var sim = NewSim(relief);
        try
        {
            Instruments.Phase(sim, Seed, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, Seed, "warm");
            sim.StepQuarter(); Instruments.Quarter(sim, Seed, 0);   // ここまでの月を 1 行目へ流す
            Board(sim);
            Drift(sim);                                              // 2 行目＝手で置いた盤面の 1 か月だけ
            Instruments.Quarter(sim, Seed, 1);
        }
        finally { Instruments.Close(sim); }
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"dipcool_s{Seed}.tsv"));
        head = lines[0].Split('\t');
        Assert.Equal(Instruments.DipcoolHeaderText.Split('\t'), head);
        Assert.True(lines.Length >= 3, $"前提：行の数 {lines.Length}");
        var row = lines[2].Split('\t');
        Assert.Equal(head.Length, row.Length);
        return row;
    }

    static double Col(string[] head, string[] row, string name) =>
        double.Parse(row[System.Array.IndexOf(head, name)], System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void H3_計器dipcool_sは係数2でハイエナを1行数え_係数0で0行_旧の規則の行は両方で1()
    {
        string d0 = "", d1 = "";
        try
        {
            var r2 = InsRow(2.0, out var h, out d0);
            var r0 = InsRow(0.0, out _, out d1);
            Assert.Equal(1.0, Col(h, r2, "months")); Assert.Equal(1.0, Col(h, r0, "months"));
            Assert.Equal(2.0, Col(h, r2, "highRows")); Assert.Equal(1.0, Col(h, r2, "lowRows"));
            Assert.Equal(1.0, Col(h, r2, "oldHyenaRows")); Assert.Equal(1.0, Col(h, r0, "oldHyenaRows"));
            Assert.Equal(1.0, Col(h, r2, "oldHyenaFreshRows")); Assert.Equal(1.0, Col(h, r2, "freshRows"));
            Assert.Equal(1.0, Col(h, r2, "obsHyenaRows")); Assert.Equal(1.0, Col(h, r2, "obsHyenaFreshRows"));
            Assert.Equal(0.0, Col(h, r0, "obsHyenaRows")); Assert.Equal(0.0, Col(h, r0, "obsHyenaFreshRows"));
            Assert.Equal(2.0, Col(h, r2, "grudgeRows"));
            Assert.Equal(2.0, Col(h, r2, "maxAbsDiffNewGrudge"), 9);    // 118 と 120 の差
            Assert.True(Col(h, r0, "maxAbsDiffNewGrudge") <= 1e-9, $"係数 0 の差 {Col(h, r0, "maxAbsDiffNewGrudge")}");
            Assert.Equal(2.21, Col(h, r2, "sumObsCoolGrudge"), 9);     // 2.0＋0.21
            Assert.Equal(0.21, Col(h, r0, "sumObsCoolGrudge"), 9);
            Assert.Equal(2.21, Col(h, r0, "sumOldCoolGrudge"), 9);     // 同じ盤面の旧の規則
            Assert.Equal(0.0, Col(h, r2, "obsOtherRows")); Assert.Equal(0.0, Col(h, r0, "obsOtherRows"));
        }
        finally
        {
            foreach (var d in new[] { d0, d1 }) if (d.Length > 0 && System.IO.Directory.Exists(d)) System.IO.Directory.Delete(d, true);
        }
    }
}
