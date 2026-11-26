using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1391) 短い確かめの道具（scripts/instruments/statediff/）。
/// 書き出し：assembly の置き場から上へたどって statediff.cfg（seed・quarters・depth・out）を探し、あれば四半期ごとの群の指紋を out へ書く。無ければ何もしない（普段のフルスイートを重くしない）。
/// 自己試験：同じ木の 2 回は一致し、係数 SupplySlope を 1e-8 動かした木は 1 四半期以内に指紋が分かれて群の名が出る。</summary>
public class StateDigestToolTests
{
    static string? FindCfg()
    {
        for (var d = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory); d is not null; d = d.Parent)
        { var f = System.IO.Path.Combine(d.FullName, "statediff.cfg"); if (System.IO.File.Exists(f)) return f; }
        return null;
    }

    static IEnumerable<(int Q, SortedDictionary<string, ulong> Dg)> Run(ulong seed, int quarters, int depth, Coefficients? coef)
    {
        var sim = new MultiNationSim(WorldData.Nations(), seed, _ => new UtilityAgent(), coef);
        sim.StartWarmup();
        yield return (0, StateDigest.Digest(sim, depth));
        for (int q = 1; q <= quarters; q++) { sim.StepQuarter(); yield return (q, StateDigest.Digest(sim, depth)); }
    }

    [Fact]
    public void 状態の指紋を書き出す_cfgがある時だけ()
    {
        var cfg = FindCfg();
        if (cfg is null) return;
        var kv = System.IO.File.ReadAllLines(cfg).Select(l => l.Split('=', 2)).Where(a => a.Length == 2).ToDictionary(a => a[0].Trim(), a => a[1].Trim());
        using var w = new System.IO.StreamWriter(kv["out"], false, new System.Text.UTF8Encoding(false)) { NewLine = "\n" };
        foreach (var (q, dg) in Run(ulong.Parse(kv["seed"]), int.Parse(kv["quarters"]), int.Parse(kv["depth"]), null))
            foreach (var (g, h) in dg) w.WriteLine($"{q}\t{g}\t{h:x16}");
    }

    sealed class Probe { public double X; public int N; public Dictionary<string, double> D = new(); }

    /// <summary>(1391) 小さい物での検査（盤面に依らない）：数 1 ビットの違いを拾う・同じ内容は一致・辞書の挿入の順には依らない。突然変異 μ0＝double を数えない → 赤。</summary>
    [Fact]
    public void 指紋は数の違いを拾い辞書の順に依らない()
    {
        static string H(Probe p) => string.Join(",", StateDigest.Digest(p, 2).Select(kv => $"{kv.Key}={kv.Value:x}"));
        var a = new Probe { X = 1.0, N = 3 }; a.D["あ"] = 1; a.D["い"] = 2;
        var b = new Probe { X = 1.0, N = 3 }; b.D["い"] = 2; b.D["あ"] = 1;
        var c = new Probe { X = System.BitConverter.Int64BitsToDouble(System.BitConverter.DoubleToInt64Bits(1.0) + 1), N = 3 }; c.D["あ"] = 1; c.D["い"] = 2;
        var d = new Probe { X = 1.0, N = 4 }; d.D["あ"] = 1; d.D["い"] = 2;
        Assert.Equal(H(a), H(b));
        Assert.NotEqual(H(a), H(c));
        Assert.NotEqual(H(a), H(d));
    }

    /// <summary>(1399) 係数の欄（_coef を含む群の名）は比べから外す＝係数を動かした木が四半期 0 で係数の群だけ分かれて緑になる（シミュの状態のずれを拾えていない）のを防ぐ。
    /// 比べるのはシミュの状態の群だけ（名の判定は skipCoef）。</summary>
    static (int Q, string Group)? FirstDiff(IEnumerable<(int, SortedDictionary<string, ulong>)> a, IEnumerable<(int, SortedDictionary<string, ulong>)> b, bool skipCoef = false)
    {
        static bool Keep(string g, bool skip) => !(skip && g.Contains("_coef"));
        foreach (var ((q, x), (_, y)) in a.Zip(b))
        {
            if (x.Keys.Count(g => Keep(g, skipCoef)) != y.Keys.Count(g => Keep(g, skipCoef))) return (q, "群の数");
            foreach (var (g, h) in x) if (Keep(g, skipCoef) && (!y.TryGetValue(g, out var h2) || h2 != h)) return (q, g);
        }
        return null;
    }

    [Fact]
    public void 自己試験_同じ木は一致し_係数を動かした木は群の名を出す()
    {
        var same = FirstDiff(Run(1789, 1, 4, null).ToList(), Run(1789, 1, 4, null).ToList());
        Assert.Null(same);
        var c = new Coefficients { SupplySlope = 0.50000001 };
        var diff = FirstDiff(Run(1789, 1, 4, null).ToList(), Run(1789, 1, 4, c).ToList());
        Assert.NotNull(diff);
        Assert.True(diff!.Value.Q <= 1 && diff.Value.Group.StartsWith("sim"), $"{diff}");
        // (1399) 係数の欄を外しても、シミュの状態（係数ではない群）のずれが四半期 1 以内に拾える（係数の群だけで緑にならない）。SupplySlope は既定の道で読まれず 1e-8 も 0.9 も状態へ届かない（係数の群だけが分かれる）＝状態へ届く MigrationDistanceScale（1 四半期で非係数の 62 群が分かれる）で見る
        var big = new Coefficients { MigrationDistanceScale = 3000.0 };
        var state = FirstDiff(Run(1789, 1, 4, null).ToList(), Run(1789, 1, 4, big).ToList(), skipCoef: true);
        Assert.NotNull(state);
        Assert.True(state!.Value.Q <= 1 && !state.Value.Group.Contains("_coef"), $"シミュの状態のずれを拾えない：{state}");
    }
}
