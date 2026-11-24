using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1351) I5：AI とプレイヤーは同じ機構（等価の原則）。SimCore の非コメント行で IsPlayer を読む・書く行の数をファイル別の許可表で縛る。
/// 増えたら赤＝新しい分岐を足さない。減ったら表を直す（減った場合も赤＝表が嘘にならない）。事前登録 measurements/m1351_20261125_prereg_ik.txt の [3]。</summary>
public class IsPlayerBranchCountTests
{
    private static readonly (string File, int Lines)[] Allowed =
    [
        ("Diplomacy.cs", 11), ("InstrumentsUnif.cs", 1), ("MultiNationSim.cs", 2), ("Parliament.cs", 1), ("PlayText.cs", 1), ("Simulation.cs", 1),
    ];

    [Fact]
    public void IsPlayerを含む行の数はファイル別の許可表のとおり()
    {
        string dir = System.IO.Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore");
        var counts = new SortedDictionary<string, int>();
        int scanned = 0;
        foreach (var f in System.IO.Directory.EnumerateFiles(dir, "*.cs", System.IO.SearchOption.AllDirectories))
        {
            string rel = System.IO.Path.GetRelativePath(dir, f);
            if (rel.StartsWith("obj" + System.IO.Path.DirectorySeparatorChar) || rel.StartsWith("bin" + System.IO.Path.DirectorySeparatorChar)) continue;
            scanned++;
            int n = 0;
            foreach (var line in System.IO.File.ReadAllLines(f))
            {
                int c = line.IndexOf("//", StringComparison.Ordinal);
                if ((c >= 0 ? line[..c] : line).Contains("IsPlayer", StringComparison.Ordinal)) n++;
            }
            if (n > 0) counts[System.IO.Path.GetFileName(f)] = counts.GetValueOrDefault(System.IO.Path.GetFileName(f)) + n;
        }
        Assert.True(scanned > 100, $"対照：走査したファイルが {scanned} 本（母集合が痩せている）");
        var allowed = Allowed.ToDictionary(x => x.File, x => x.Lines);
        var bad = new List<string>();
        foreach (var (file, n) in counts)
        {
            int a = allowed.GetValueOrDefault(file);
            if (n > a) bad.Add($"{file}: {n} 行（許可 {a}）＝増えた。AI とプレイヤーで分岐を足さない");
            else if (n < a) bad.Add($"{file}: {n} 行（許可 {a}）＝減った。許可表を直すこと");
        }
        foreach (var (file, a) in allowed) if (!counts.ContainsKey(file)) bad.Add($"{file}: 0 行（許可 {a}）＝減った。許可表を直すこと");
        Assert.True(bad.Count == 0, string.Join("\n", bad));
    }
}
