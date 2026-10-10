using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★時間の単位の換算の番人（README §1-9・(243)・2026-10-10）。
/// ① 換算の関数は、置き換える前に呼び元が書いていた 1 回の除算か乗算と**ビット単位で同じ**（丸めを変えない＝恒等リファクタの前提）。
///   ⚠突然変異「PerYearToPerMonth を x * (1.0 / 12) にする」で赤（逆数を掛けると一部の x で最後の桁が変わる）。
/// ② 量（率・流れ）の換算は Calendar の名前のある関数を通す。SimCore に裸の `* Calendar.MonthsPerYear`／`/ Calendar.MonthsPerQuarter` などが
///   残ってよいのは、期間（四半期数→日数・月数）と暦の添字（日→月・日→年）だけ＝下の表が宣言した数。増やすなら表を上げて理由を書く（＝宣言）。
///   ⚠突然変異「Fiscal.cs に `x / Calendar.MonthsPerYear` を足す」で赤。</summary>
public class CalendarTests
{
    private static string SimCoreDir([CallerFilePath] string self = "")
        => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(self))!, "PriceOfBread.SimCore");

    private static readonly double[] Samples =
    [
        0.0, 1.0, 0.01, 0.04, 0.06, 0.0075, 1e-9, 123456.789, 3.0e12, -5.5, 0.1, 0.2, 1.0 / 3.0, 2.0 / 7.0, 98765.4321, 0.30, 0.5,
    ];

    private static void SameBits(double expect, double got, string what)
        => Assert.True(BitConverter.DoubleToInt64Bits(expect) == BitConverter.DoubleToInt64Bits(got), $"{what}：{expect:R} ≠ {got:R}（ビットが違う）");

    [Fact]
    public void 換算の関数は呼び元の1回の算術とビット単位で同じ()
    {
        foreach (var x in Samples)
        {
            SameBits(x / 12.0, Calendar.PerYearToPerMonth(x), "PerYearToPerMonth");
            SameBits(x * 12.0, Calendar.PerMonthToPerYear(x), "PerMonthToPerYear");
            SameBits(x / 3.0, Calendar.PerQuarterToPerMonth(x), "PerQuarterToPerMonth");
            SameBits(x * 3.0, Calendar.PerMonthToPerQuarter(x), "PerMonthToPerQuarter");
            SameBits(x * 4.0, Calendar.PerQuarterToPerYear(x), "PerQuarterToPerYear");
            SameBits(x / 360.0, Calendar.PerYearToPerDay(x), "PerYearToPerDay");
            SameBits(x / 30.0, Calendar.PerMonthToPerDay(x), "PerMonthToPerDay");
            SameBits(x * 30.0, Calendar.PerDayToPerMonth(x), "PerDayToPerMonth");
            SameBits(x * 12.0, Calendar.YearsToMonths(x), "YearsToMonths");
        }
        // 月の刻みは年・四半期・日と整合している（暦の定数そのものの番人）
        Assert.Equal(Calendar.MonthsPerYear, Calendar.MonthsPerQuarter * Calendar.QuartersPerYear);
        Assert.Equal(Calendar.DaysPerYear, Calendar.DaysPerMonth * Calendar.MonthsPerYear);
        Assert.Equal(Calendar.DaysPerQuarter, Calendar.DaysPerMonth * Calendar.MonthsPerQuarter);
    }

    /// <summary>期間（四半期数→日数・月数）と暦の添字（日→月・日→年）だけが裸の換算で残る＝宣言した数（2026-10-10 (243) の実測 12）。</summary>
    private const int BareConversionBaseline = 12;

    [Fact]
    public void 量の換算は暦の関数を通す_裸の換算は宣言した数まで()
    {
        var rx = new Regex(@"[*/]\s*(\(double\))?\s*Calendar\.(MonthsPerYear|MonthsPerQuarter|QuartersPerYear|DaysPerYear|DaysPerMonth|DaysPerQuarter)\b");
        var sites = new System.Collections.Generic.List<string>();
        foreach (var f in Directory.EnumerateFiles(SimCoreDir(), "*.cs"))
        {
            if (Path.GetFileName(f) == "Calendar.cs") continue;
            foreach (var line in File.ReadLines(f))
            {
                var code = line; int c = code.IndexOf("//", StringComparison.Ordinal);
                if (c >= 0) code = code[..c];   // コメントの言及は数えない
                if (rx.IsMatch(code)) sites.Add(Path.GetFileName(f) + ": " + code.Trim());
            }
        }
        Assert.True(sites.Count <= BareConversionBaseline,
            $"裸の時間換算が {sites.Count} 箇所（宣言 {BareConversionBaseline}）＝増えている。率・流れの換算は Calendar.PerYearToPerMonth ほかの名前のある関数を通すこと：\n" + string.Join("\n", sites));
    }
}
