using System.IO;
using System.Linq;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1111) 表示の層が別アセンブリ（PriceOfBread.Display）になったので、SimCore の再帰走査で読まれていた Display の 18 本が、
/// ソースを走査する番人の目から外れる事故を防ぐ。走査の番人の一覧に <c>PriceOfBread.Display</c> が書いてあること（外すと赤）。
/// 機構の向き・大きさは主張しない＝走査の母集合の取りこぼしだけを見る。</summary>
public class SourceScanCoverageTests
{
    /// <summary>SimCore のフォルダを再帰で走査していた番人（旧は Display を含んでいた）のファイル。</summary>
    private static readonly string[] Guards =
    [
        "GoodIdGuardTests.cs", "SulfuricAcidSodaAshTests.cs", "WriterRatchetTests.cs", "UnitTypeTests.cs",
        "ScoreReaderTests.cs", "TechLevelRemovalTests.cs", "TreasuryLedgerTests.cs", "DiscontentReaderTests.cs",
    ];

    [Fact]
    public void 走査の番人は表示の層のフォルダも読む()
    {
        string root = GoodIdGuardTests.RepoRoot();
        string display = Path.Combine(root, "PriceOfBread.Display");
        // 対照：Display のフォルダに実際に走査される .cs がある（空のフォルダを読むだけの緑にしない）
        Assert.True(Directory.EnumerateFiles(display, "*.cs", SearchOption.AllDirectories).Count() >= 18, "PriceOfBread.Display に .cs が少ない");
        // 旧の SimCore/Display が残っていない（二重に走査・置き場の戻りを防ぐ）
        string old = Path.Combine(root, "PriceOfBread.SimCore", "Display");
        Assert.False(Directory.Exists(old) && Directory.EnumerateFiles(old, "*.cs", SearchOption.AllDirectories).Any(), "SimCore/Display に .cs が復活している");
        string dir = Path.Combine(root, "PriceOfBread.Tests");
        var missing = Guards.Where(g => !File.ReadAllText(Path.Combine(dir, g)).Contains("\"PriceOfBread.Display\"")).ToList();
        Assert.True(missing.Count == 0, "表示の層のフォルダを読まない走査の番人: " + string.Join("、", missing));
    }
}
