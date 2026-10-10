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

    /// <summary>期間（四半期数→日数・月数）と暦の添字（日→月・日→年）だけが裸の換算で残る＝ファイルごとに宣言した数
    /// （2026-10-10 (243) の実測 12 → (244) 合流で被演算子の左右の両方を数えるよう広げ・計器 Instruments.cs の月の添字を足して 15）。
    /// ⚠数えるのは SimCore の「Calendar の暦の定数を掛けるか割る」形だけ（左右どちらの被演算子でも）。裸のリテラル（*12・/12.0・*4.0）と Runner／App は対象外（12 は暦以外にも多い＝誤検出が多すぎる）。
    /// ファイルごとの数を見る＝1つ消して別のファイルに1つ足すと赤。</summary>
    private static readonly System.Collections.Generic.Dictionary<string, int> BareConversionSites = new()
    {
        ["Coefficients.cs"] = 1,   // 1950 年の断面の四半期数（期間）
        ["CrownParliament.cs"] = 1,   // 在任の四半期数÷（年数×4）（期間）
        ["Diplomacy.cs"] = 1,   // 順位の更新の間隔（四半期数→月数）
        ["EconomyStep.cs"] = 1,   // 建設期間（四半期数→月数）
        ["Instruments.cs"] = 1,   // 計器の月の添字（日→月）
        ["MultiNationSim.cs"] = 3,   // 月の添字・外交の月の通し番号・残部の再併合の期間（年数→四半期数）
        ["Privileges.cs"] = 1,   // 年の指数平均を月で畳む刻み（1/12 年）
        ["SelfPlay.cs"] = 1,   // 10 年の窓（四半期数）
        ["Simulation.cs"] = 2,   // 収穫の年の添字・外交の月の通し番号
        ["World.cs"] = 3,   // 建設期間（四半期数→日数・月数）
    };

    [Fact]
    public void 量の換算は暦の関数を通す_裸の換算は宣言した数まで()
    {
        const string unit = @"Calendar\.(MonthsPerYear|MonthsPerQuarter|QuartersPerYear|DaysPerYear|DaysPerMonth|DaysPerQuarter)\b";
        var rx = new Regex(@"[*/]\s*(\(double\))?\s*" + unit + "|" + unit + @"\s*[*/]");
        var sites = new System.Collections.Generic.List<string>();
        var perFile = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var f in Directory.EnumerateFiles(SimCoreDir(), "*.cs"))
        {
            string name = Path.GetFileName(f);
            if (name == "Calendar.cs") continue;
            foreach (var line in File.ReadLines(f))
            {
                var code = line; int c = code.IndexOf("//", StringComparison.Ordinal);
                if (c >= 0) code = code[..c];   // コメントの言及は数えない
                if (rx.IsMatch(code)) { sites.Add(name + ": " + code.Trim()); perFile[name] = perFile.GetValueOrDefault(name) + 1; }
            }
        }
        var over = perFile.Where(kv => kv.Value > BareConversionSites.GetValueOrDefault(kv.Key)).Select(kv => $"{kv.Key} {kv.Value}（宣言 {BareConversionSites.GetValueOrDefault(kv.Key)}）").ToList();
        Assert.True(over.Count == 0,
            "裸の時間換算が宣言より多いファイル：" + string.Join("・", over) + "。率・流れの換算は Calendar.PerYearToPerMonth ほかの名前のある関数を通すこと：\n" + string.Join("\n", sites));
    }
}
