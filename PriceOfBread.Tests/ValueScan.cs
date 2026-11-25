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

    /// <summary>量の指定。`名前` か `名前@colony`（受け手の名が col で始まる）か `名前@pop`（それ以外の受け手）。
    /// (1373) Pop と Colony の Militancy は別の行に分ける（受け手の名で分ける＝機械で数えられる限り）。</summary>
    internal static (string Member, Func<string, bool>? Recv) Parse(string spec)
    {
        int at = spec.IndexOf('@');
        if (at < 0) return (spec, null);
        string member = spec[..at];
        return spec[(at + 1)..] switch
        {
            "colony" => (member, r => r.StartsWith("col", StringComparison.OrdinalIgnoreCase)),
            "pop" => (member, r => !r.StartsWith("col", StringComparison.OrdinalIgnoreCase)),
            var k => throw new ArgumentException("量の指定の受け手が不明: " + k),
        };
    }

    /// <summary>`.` の直前の識別子（無ければ空）。</summary>
    private static string ReceiverOf(string t, int dotIndex)
    {
        int e = dotIndex;
        while (e > 0 && char.IsWhiteSpace(t[e - 1])) e--;
        int s = e;
        while (s > 0 && (char.IsLetterOrDigit(t[s - 1]) || t[s - 1] == '_')) s--;
        return t[s..e];
    }

    private static Regex WriterRx(string member) => new($@"\.{Regex.Escape(member)}{Ops}");
    private static Regex BareRx(string member) => new($@"(?<![\w.]){Regex.Escape(member)}\s*(\[[^\]]*\])?\s*(\+=|-=|\*=|/=|=(?![=>]))");
    private static readonly Regex NewTail = new(@"\bnew\b[\w.<>,\[\]\s]*(\((?:[^()]|\([^()]*\))*\))?\s*$", RegexOptions.Singleline);

    /// <summary>書き手の位置（ドットつき＋量の指定の受け手の絞り込み）。</summary>
    private static IEnumerable<Match> DottedWriters(string t, string spec)
    {
        var (member, recv) = Parse(spec);
        foreach (Match m in WriterRx(member).Matches(t))
            if (recv == null || recv(ReceiverOf(t, m.Index))) yield return m;
    }

    /// <summary>(1373) ドットなしの書き手＝自分の型の中で自分の欄へ代入する文（World.cs の `Legitimacy = Math.Max(0, Legitimacy - …)`）。
    /// 直前の字が `;` `}` `)` `:` か `else`・`do`・文頭、または `{`（ただし `new 型 {` の初期化子は除く）の時だけ。
    /// 宣言（`public double X = 0`）・初期化子の 2 つ目以降（`,` の後）・比較と `=>` は数えない。</summary>
    private static IEnumerable<Match> BareWriters(string t, string member)
    {
        foreach (Match m in BareRx(member).Matches(t))
        {
            int i = m.Index - 1;
            while (i >= 0 && char.IsWhiteSpace(t[i])) i--;
            if (i < 0) { yield return m; continue; }
            char ch = t[i];
            if (ch is ';' or '}' or ')' or ':') { yield return m; continue; }
            if (ch == '{') { if (!NewTail.IsMatch(t[..i])) yield return m; continue; }
            if (i >= 3 && string.CompareOrdinal(t, i - 3, "else", 0, 4) == 0 && (i == 3 || !char.IsLetterOrDigit(t[i - 4]))) yield return m;
        }
    }

    internal static int CountWriters(string text, string member) => DottedWriters(Strip(text), member).Count();

    /// <summary>ドットなしの書き手の数（量の指定に受け手が無い時だけ）。</summary>
    internal static int CountBareWriters(string text, string spec)
        => Parse(spec).Recv != null ? 0 : BareWriters(Strip(text), spec).Count();

    /// <summary>ドットつき＋ドットなしの書き手の数（導いた値の表が使う）。</summary>
    internal static int CountAllWriters(string text, string spec) => CountWriters(text, spec) + CountBareWriters(text, spec);

    /// <summary>`.量名` の読み（書き手でない出現）。</summary>
    internal static int CountReads(string text, string spec)
    {
        var (member, recv) = Parse(spec);
        string t = Strip(text);
        int n = 0;
        foreach (Match m in new Regex($@"\.{Regex.Escape(member)}\b(?!{Ops})").Matches(t))
            if (recv == null || recv(ReceiverOf(t, m.Index))) n++;
        return n;
    }

    /// <summary>欄の無い値：関数の呼び出し（宣言を除く）。</summary>
    internal static int CountCalls(string text, string function)
    {
        string t = Strip(text);
        int all = new Regex($@"\b{Regex.Escape(function)}\s*\(").Matches(t).Count;
        int decl = new Regex($@"\b(static|double|float|int|bool)\s+{Regex.Escape(function)}\s*\(").Matches(t).Count;
        return all - decl;
    }

    private static string RhsAt(string t, Match m)
    {
        int d = 0, j = m.Index + m.Length;
        for (; j < t.Length; j++)
        {
            char ch = t[j];
            if ("([{".IndexOf(ch) >= 0) d++;
            else if (")]}".IndexOf(ch) >= 0) { if (--d < 0) break; }
            else if (ch == ';' && d == 0) break;
        }
        return t[(m.Index + m.Length)..j];
    }

    /// <summary>書き手の右辺（代入演算子の後から `;` まで・括弧の対の外へ出たら終わり）。ドットつきだけ。</summary>
    internal static IEnumerable<string> WriterRhs(string text, string spec)
    {
        string t = Strip(text);
        foreach (Match m in DottedWriters(t, spec)) yield return RhsAt(t, m);
    }

    /// <summary>(1373) ドットつき＋ドットなしの書き手の右辺。</summary>
    internal static IEnumerable<string> WriterRhsAll(string text, string spec)
    {
        string t = Strip(text);
        foreach (Match m in DottedWriters(t, spec)) yield return RhsAt(t, m);
        if (Parse(spec).Recv == null)
            foreach (Match m in BareWriters(t, spec)) yield return RhsAt(t, m);
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

    /// <summary>入力の項の種類の数（全てのファイルの書き手の和集合・ドットなしの書き手を含む）。
    /// Toward(x, target, k) の項の数え方（(1373) で名乗る）：x は自分自身なら数えない（他の量なら 1 項）・target は普通の項・k は係数なので数えない（`c.` で始まる）。</summary>
    internal static int CountInputs(IEnumerable<string> texts, string spec)
        => texts.SelectMany(t => WriterRhsAll(t, spec)).SelectMany(r => InputTerms(r, Parse(spec).Member)).Distinct(StringComparer.Ordinal).Count();

    /// <summary>(1373) 機械で数えられない入力（局所変数を通る右辺）の指紋。書き手の右辺のうち局所変数を含む物を空白を除いて並べ、先頭 12 桁の SHA-256 にする。
    /// 局所変数の中身が変わっても項の数は動かない＝その盲点を、右辺の文面で守る（表の指紋と違えば赤＝宣言して更新する）。</summary>
    internal static string LocalFedFingerprint(IEnumerable<string> texts, string spec)
    {
        var (member, _) = Parse(spec);
        var rhs = texts.SelectMany(t => WriterRhsAll(t, spec))
            .Where(r => InputTerms(r, member).Any(x => x.StartsWith("local:", StringComparison.Ordinal)))
            .Select(r => Regex.Replace(r, @"\s+", ""))
            .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var h = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", rhs)));
        return rhs.Count + ":" + Convert.ToHexString(h)[..12].ToLowerInvariant();
    }

    /// <summary>`.Value`（Units.cs と Calendar.cs の外）。注釈と文字列の中は数えない。`?.Value` は数える。</summary>
    internal static int CountValue(string text) => new Regex(@"\.Value\b").Matches(Strip(text)).Count;
}
