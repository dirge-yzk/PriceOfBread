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

    static MultiNationSim NewSim()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], Seed, _ => new UtilityAgent(), null);
        foreach (var n in sim.Nations) { n.World.Coef.CivilWarLandslideRatio = 0; }
        sim.DiplomacyFrozen = true;
        return sim;
    }

    /// <summary>手で置く盤面（組 F：従属＝凍結・緊張 120）：組 H（衝撃 1.0 の国を含む・緊張 120・遺恨 1.0）・組 G（衝撃 0・緊張 120・遺恨 0.5）・組 L（緊張 50）。他の組は消す。</summary>
    static (long H, long G, long L, long F, System.Collections.Generic.Dictionary<long, double> T) Board(MultiNationSim sim)
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
        long fz = Key(ps[1], ps[3]);
        d.SetDependency(ps[3], ps[1]);   // 組 F：従属＝凍結の枝（(1145) M3 の突然変異を赤にする）
        tension[h] = 120.0; tension[g] = 120.0; tension[l] = 50.0; tension[fz] = 120.0;
        d._grudge[h] = 1.0; d._grudge[g] = 0.5;
        return (h, g, l, fz, tension);
    }

    static void Drift(MultiNationSim sim) =>
        typeof(Diplomacy).GetMethod("DriftTension", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(sim.Diplomacy, [sim.Nations[0].World.Coef]);

    [Fact]
    public void H2_ハイエナの枝は無い_衝撃の組も遺恨の式で冷める_遺恨1なら動かない()
    {
        var sim = NewSim();
        var b = Board(sim);
        Drift(sim);
        Assert.Equal(120.0, b.T[b.H], 12);    // 遺恨 1.0＝率 0（(1154) 枝の削除後＝枝が戻れば 118 になって赤）
        Assert.Equal(119.79, b.T[b.G], 12);
        Assert.Equal(49.58, b.T[b.L], 12);
        Assert.Equal(119.8, b.T[b.F], 12);
    }

    static string[] InsRow(out string[] head, out string dir)
    {
        dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_hy_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        Instruments.SelectTables("dipcool_s");
        var sim = NewSim();
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
    public void H3_計器dipcool_sは枝の削除後もハイエナの旧の規則を同じ盤面で数え_実際の値は新の規則に合う()
    {
        string d0 = "";
        try
        {
            var r0 = InsRow(out var h, out d0);
            Assert.Equal(1.0, Col(h, r0, "months"));
            Assert.Equal(3.0, Col(h, r0, "highRows")); Assert.Equal(1.0, Col(h, r0, "lowRows"));
            Assert.Equal(1.0, Col(h, r0, "frozenRows")); Assert.Equal(0.2, Col(h, r0, "sumObsCoolFrozen"), 9);
            Assert.True(Col(h, r0, "maxAbsDiffFrozen") <= 1e-9, $"凍結の差 {Col(h, r0, "maxAbsDiffFrozen")}");
            Assert.Equal(1.0, Col(h, r0, "oldHyenaRows"));
            Assert.Equal(1.0, Col(h, r0, "oldHyenaFreshRows")); Assert.Equal(1.0, Col(h, r0, "freshRows"));
            Assert.Equal(0.0, Col(h, r0, "obsHyenaRows")); Assert.Equal(0.0, Col(h, r0, "obsHyenaFreshRows"));
            Assert.Equal(2.0, Col(h, r0, "grudgeRows"));
            Assert.True(Col(h, r0, "maxAbsDiffNewGrudge") <= 1e-9, $"新の規則との差 {Col(h, r0, "maxAbsDiffNewGrudge")}");
            Assert.Equal(0.21, Col(h, r0, "sumObsCoolGrudge"), 9);
            Assert.Equal(2.21, Col(h, r0, "sumOldCoolGrudge"), 9);     // 同じ盤面の旧の規則（計器が自分の定数 2.0 で組む）
            Assert.Equal(0.0, Col(h, r0, "obsOtherRows"));
        }
        finally
        {
            Instruments.SelectTables(null); Instruments.Configure(null, null);   // 選びと出力の先を戻す（同じ集まりの他の試験へ漏らさない）
            if (d0.Length > 0 && System.IO.Directory.Exists(d0)) System.IO.Directory.Delete(d0, true);
        }
    }
}
