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

    /// <summary>(1120) 走査の絞りまで見る。<c>rel.StartsWith("PriceOfBread.SimCore/")</c> は Display を黙って外し、<c>rel.StartsWith("PriceOfBread.Tests/")</c> は Tests.Ui を外し損ねた。
    /// 試験のソースは絞りを <see cref="GoodIdGuardTests.IsEngineRel"/>・<see cref="GoodIdGuardTests.IsTestRel"/> へ通す。直に書く行は、同じ行に <c>エンジンだけ</c> の注を持つ物だけ許す。</summary>
    [Fact]
    public void 走査の絞りはエンジンと試験の判定を通す_直書きは目的を名乗る()
    {
        string root = GoodIdGuardTests.RepoRoot();
        var raw = new System.Text.RegularExpressions.Regex(@"StartsWith\(""PriceOfBread\.(SimCore|Tests)/""");
        int files = 0; var bad = new System.Collections.Generic.List<string>();
        foreach (var proj in new[] { "PriceOfBread.Tests", "PriceOfBread.Tests.Ui" })
            foreach (var f in Directory.EnumerateFiles(Path.Combine(root, proj), "*.cs", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(root, f).Replace((char)92, '/');
                if (rel.Contains("/obj/") || rel.Contains("/bin/")) continue;
                files++;
                if (Path.GetFileName(f) is "GoodIdGuardTests.cs" or "SourceScanCoverageTests.cs") continue;   // 判定の定義と、この番人の模様
                int ln = 0;
                foreach (var line in File.ReadLines(f)) { ln++; if (raw.IsMatch(line) && !line.Contains("エンジンだけ")) bad.Add(rel + ":" + ln); }
            }
        Assert.True(files >= 300, $"調べた試験のソースが少ない {files}");   // 対照：空の母集合で緑にしない（今は Tests 293＋Tests.Ui 21）
        Assert.True(bad.Count == 0, "走査の絞りを直書きしている行: " + string.Join("、", bad));
        // 判定の定義そのものが旧の母集合（SimCore と Display／Tests と Tests.Ui）を指している
        Assert.True(GoodIdGuardTests.IsEngineRel("PriceOfBread.Display/Fmt.cs") && GoodIdGuardTests.IsEngineRel("PriceOfBread.SimCore/World.cs") && !GoodIdGuardTests.IsEngineRel("PriceOfBread.App/X.cs"));
        Assert.True(GoodIdGuardTests.IsTestRel("PriceOfBread.Tests.Ui/X.cs") && GoodIdGuardTests.IsTestRel("PriceOfBread.Tests/X.cs") && !GoodIdGuardTests.IsTestRel("PriceOfBread.Runner/X.cs"));
    }
}
