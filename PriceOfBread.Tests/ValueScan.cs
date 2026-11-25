using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PriceOfBread.Tests;

/// <summary>(1364) 走査の番人が使う数え方の持ち主（1 本だけ）。書き手・入力・出力と `.Value` の数は全てここで数える。
/// ── 数え方 ──
/// ・母集合＝エンジンの本体（SimCore の .cs・SimCore/Instruments は含めない・obj と bin は含めない）。試験・Runner・App・表示は数えない。
///   AI の判断（SelfPlay など）はエンジンの本体なので数える。
/// ・本文の整え＝文字列の中身は空へ・`//` と `/* */` の注釈は消す（注釈と文字列の中の言及は数えない）。
/// ・書き手＝`.量名` への代入・複合代入（`=`・`+=`・`-=`・`*=`・`/=`・添字つき可・`==` と `=>` は除く）。1 行に 2 つあれば 2 と数える。
///   Toward の結果の代入も代入文なので同じ規則で 1 と数える。**初期化子（`new Pop { 量名 = … }`）は数えない**（構築であって書き込みではない）。
///   ドットなしの自分の型の中の書き込みも数えない（ドットつきだけ）。
/// ・出力＝`.量名` の読み（書き手でない出現）。ドットつきだけ。欄の無い値は関数の呼び出しを読みとして数える（宣言は除く）。
/// ・入力＝書き手の右辺（`;` まで・括弧の中は対で読む）にある項の種類の数（量ごとに全ての書き手の和集合）。項の規則は <see cref="InputTerms"/>。
///   **機械で数えられない限り**＝局所変数は 1 項（その変数の中身は展開しない）＝入力は少なく数える側に偏る。</summary>
internal static class ValueScan
{
    private static readonly Regex Tok = new(@"//[^\n]*|/\*.*?\*/|""(?:[^""\\\n]|\\.)*""|'(?:[^'\\\n]|\\.)+'", RegexOptions.Singleline);
    private static readonly Regex PathRx = new(@"(?<![\w.])[A-Za-z_]\w*(?:\s*\.\s*[A-Za-z_]\w*)*");
    private const string Ops = @"(\[[^\]]*\])?\s*(\+=|-=|\*=|/=|=(?![=>]))";

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "new","true","false","null","int","double","long","float","bool","var","default","this","base","is","as","and","or","not","in","out","ref",
        "checked","unchecked","typeof","sizeof","nameof","string","char","byte","short","uint","ulong","decimal","object","when","if","else","return",
    };

    /// <summary>注釈と文字列の中身を消す。行の数は保つ（行番号が変わらない）。</summary>
    internal static string Strip(string text)
        => Tok.Replace(text, m => m.Value[0] switch
        {
            '/' => m.Value[1] == '*' ? new string('\n', m.Value.Count(ch => ch == '\n')) : "",
            '"' => "\"\"",
            _ => "' '",
        });

    /// <summary>エンジンの本体の .cs（SimCore の下・Instruments と obj と bin を除く）。順序は固定。</summary>
    internal static IEnumerable<string> EngineFiles(string simCoreDir)
        => Directory.EnumerateFiles(simCoreDir, "*.cs", SearchOption.AllDirectories)
            .Where(f =>
            {
                string rel = Path.GetRelativePath(simCoreDir, f).Replace('\\', '/');
                return !rel.StartsWith("Instruments/", StringComparison.Ordinal) && !rel.StartsWith("obj/", StringComparison.Ordinal) && !rel.StartsWith("bin/", StringComparison.Ordinal);
            })
            .OrderBy(f => f, StringComparer.Ordinal);

    private static Regex WriterRx(string member) => new($@"\.{Regex.Escape(member)}{Ops}");

    internal static int CountWriters(string text, string member) => WriterRx(member).Matches(Strip(text)).Count;

    /// <summary>`.量名` の読み（書き手でない出現）。</summary>
    internal static int CountReads(string text, string member)
        => new Regex($@"\.{Regex.Escape(member)}\b(?!{Ops})").Matches(Strip(text)).Count;

    /// <summary>欄の無い値：関数の呼び出し（宣言を除く）。</summary>
    internal static int CountCalls(string text, string function)
    {
        string t = Strip(text);
        int all = new Regex($@"\b{Regex.Escape(function)}\s*\(").Matches(t).Count;
        int decl = new Regex($@"\b(static|double|float|int|bool)\s+{Regex.Escape(function)}\s*\(").Matches(t).Count;
        return all - decl;
    }

    /// <summary>書き手の右辺（代入演算子の後から `;` まで・括弧の対の外へ出たら終わり）。</summary>
    internal static IEnumerable<string> WriterRhs(string text, string member)
    {
        string t = Strip(text);
        foreach (Match m in WriterRx(member).Matches(t))
        {
            int d = 0, j = m.Index + m.Length;
            for (; j < t.Length; j++)
            {
                char ch = t[j];
                if ("([{".IndexOf(ch) >= 0) d++;
                else if (")]}".IndexOf(ch) >= 0) { if (--d < 0) break; }
                else if (ch == ';' && d == 0) break;
            }
            yield return t[(m.Index + m.Length)..j];
        }
    }

    /// <summary>右辺の項。ドット連鎖を 1 つの経路と読み、
    /// ・メソッド呼び出しは最後の名を落とす・先頭が大文字の経路（型・定数・列挙）と先頭が `c`／`Coef`／経路に `Coef` を含む物（係数）と予約語は数えない
    /// ・ドットのある経路は最後の名（状態の名前）を 1 項・自分自身の名は数えない・ドットのない名は 3 文字以上だけ局所変数として 1 項（`local:名`）。</summary>
    internal static IEnumerable<string> InputTerms(string rhs, string member)
    {
        foreach (Match m in PathRx.Matches(rhs))
        {
            var segs = m.Value.Split('.').Select(s => s.Trim()).ToList();
            if (rhs[(m.Index + m.Length)..].TrimStart().StartsWith('(')) segs.RemoveAt(segs.Count - 1);
            if (segs.Count == 0) continue;
            string root = segs[0];
            if (Keywords.Contains(root) || char.IsUpper(root[0])) continue;
            if (root is "c" or "Coef" || segs.Contains("Coef")) continue;
            if (segs.Count == 1) { if (root.Length >= 3) yield return "local:" + root; }
            else if (segs[^1] != member) yield return segs[^1];
        }
    }

    /// <summary>入力の項の種類の数（全てのファイルの書き手の和集合）。</summary>
    internal static int CountInputs(IEnumerable<string> texts, string member)
        => texts.SelectMany(t => WriterRhs(t, member)).SelectMany(r => InputTerms(r, member)).Distinct(StringComparer.Ordinal).Count();

    /// <summary>`.Value`（Units.cs と Calendar.cs の外）。注釈と文字列の中は数えない。`?.Value` は数える。</summary>
    internal static int CountValue(string text) => new Regex(@"\.Value\b").Matches(Strip(text)).Count;
}
