using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1364) `.Value` のラチェット（作者 2026-11-25「全て推奨でよい」）：単位つきの型（Units.cs）と暦（Calendar.cs）の**外**で `.Value` を取り出す所の数が、今の数を超えないこと。
/// 値を型の外へ取り出す所が増えるほど、単位の食い違い（月と四半期・金額と実質）がコンパイラに見えなくなる＝**増やさない**ことだけを止める。
///
/// ── 数え方 ──
/// ・母集合＝SimCore の .cs（Instruments を除く）と PriceOfBread.Display の .cs（obj と bin を除く）。Units.cs と Calendar.cs の中は数えない（型の中の取り出しは正しい置き場）。
///   Runner・App・試験は数えない。
/// ・注釈と文字列の中は数えない。`?.Value` は数える・`.HasValue`・`.ValueOrDefault` は数えない（語の切れ目で数える）。
/// ・例外の枠は作らない（今の数の表だけが猶予）。減らしたら表を下げる（＝宣言）。
/// ⚠指示の出典（D:/PoB_work/review_units/report.md の「Units と Calendar の外 594」）とは範囲が違う（出典の範囲は未確認）＝この表の数はこの定義の 1 つだけ。
/// ⚠ut の線（型の線）が同じ波で .Value を動かす＝合流が合わせた木で数え直す（2 本の基の数の和を超えたら赤）。</summary>
public class ValueCountRatchetTests
{
    private static string RepoRoot([CallerFilePath] string self = "")
        => Path.GetDirectoryName(Path.GetDirectoryName(self))!;

    private static bool IsTypeFile(string f) => Path.GetFileName(f) is "Units.cs" or "Calendar.cs";

    private static string[] SimCoreFiles()
        => ValueScan.EngineFiles(Path.Combine(RepoRoot(), "PriceOfBread.SimCore")).Where(f => !IsTypeFile(f)).ToArray();

    private static string[] DisplayFiles()
        => Directory.EnumerateFiles(Path.Combine(RepoRoot(), "PriceOfBread.Display"), "*.cs", SearchOption.AllDirectories)
            .Where(f =>
            {
                string rel = Path.GetRelativePath(Path.Combine(RepoRoot(), "PriceOfBread.Display"), f).Replace(Path.DirectorySeparatorChar, '/');
                return !rel.StartsWith("obj/", StringComparison.Ordinal) && !rel.StartsWith("bin/", StringComparison.Ordinal) && !IsTypeFile(f);
            })
            .OrderBy(f => f, StringComparer.Ordinal).ToArray();

    private static string[] InstrumentFiles()
        => Directory.EnumerateFiles(Path.Combine(RepoRoot(), "PriceOfBread.SimCore", "Instruments"), "*.cs", SearchOption.AllDirectories)
            .Where(f =>
            {
                string rel = Path.GetRelativePath(Path.Combine(RepoRoot(), "PriceOfBread.SimCore", "Instruments"), f).Replace(Path.DirectorySeparatorChar, '/');
                return !rel.StartsWith("obj/", StringComparison.Ordinal) && !rel.StartsWith("bin/", StringComparison.Ordinal);
            })
            .OrderBy(f => f, StringComparer.Ordinal).ToArray();

    // 2026-11-25 実測（基 11bfbfcd6）：SimCore 755・Display 48。
    // (1373) 計器（SimCore/Instruments）の .Value を足した（母集合の外だった＝(1369) の穴③）。今の数で固定。
    [Theory]
    [InlineData("Instruments", 417)]
    [InlineData("SimCore", 758)]   // (1369) 合わせた木で数え直した＝wr の基 755＋ut (1363) の +3（PoliticalQuantities.cs の Toward の多重定義 2・Parliament.cs:335 の 1）＝予言どおり（★(764)）
    [InlineData("Display", 48)]
    public void Valueの取り出しは型の外で増やさない(string place, int baseline)
    {
        var files = place == "SimCore" ? SimCoreFiles() : place == "Instruments" ? InstrumentFiles() : DisplayFiles();
        Assert.True(files.Length >= 10, $"{place} の .cs が少ない（{files.Length}）＝走査が空");
        int n = files.Sum(f => ValueScan.CountValue(File.ReadAllText(f)));
        Assert.True(n <= baseline,
            $"{place} の .Value が {n} か所（表 {baseline}）＝増えている。単位つきの型の中（Units.cs・Calendar.cs）へ取り出しを寄せるか、型のまま渡すこと。減らしたら表を下げる。");
    }

    /// <summary>自己試験：`?.Value` は数える・`.HasValue`・`.ValueOrDefault`・注釈と文字列の中は数えない。
    /// 突然変異「ValueScan.CountValue の Strip を外す」で赤。期待は手のリテラル。</summary>
    [Fact]
    public void M11_Valueの数え方は注釈と文字列と別の語を数えない()
    {
        Assert.Equal(2, ValueScan.CountValue("var a = w.Tax.Value + b?.Value;"));
        Assert.Equal(0, ValueScan.CountValue("var a = x.HasValue || y.ValueOrDefault > 0; // z.Value"));
        Assert.Equal(0, ValueScan.CountValue("var s = \"w.Tax.Value\"; /* q.Value */"));
        Assert.Equal(1, ValueScan.CountValue("/* a.Value\n b.Value */ c.Value"));
    }
}
