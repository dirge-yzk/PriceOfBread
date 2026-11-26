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
///   ドットなしの書き込み（自分の型の中の `量名 = …`）は、受け手の指定が無い量は全てのファイルで数え、受け手の指定がある量（`@pop`・`@colony`）は
///   その型（Pop・Colony）の宣言の本体の中だけで数える（(1379) 突然変異 M1b で確かめた）。
/// ・出力＝`.量名` の読み（書き手でない出現）。ドットつきだけ。欄の無い値は関数の呼び出しを読みとして数える（宣言は除く）。
/// ・入力＝書き手の右辺（`;` まで・括弧の中は対で読む）にある項の種類の数（量ごとに全ての書き手の和集合）。項の規則は <see cref="InputTerms"/>。
///   **機械で数えられない限り**＝局所変数は 1 項（その変数の中身は展開しない）＝入力は少なく数える側に偏る。
/// ── 名乗る穴（(1387) c147rk・直すと DerivedValueRatchetTests の指紋か ValueCountRatchetTests の数が動く＝wb と同じ表なので直さずに名乗る）──
/// ・指紋は深さ 1＝書き手の右辺の局所変数の定義の文までで、その定義の中の局所変数の定義は拾わない（突然変異 dmFood〔定義の中の定義を変える〕が緑のまま＝★(790)）。
/// ・局所変数の定義は〈書き手より前で最も近い同名の宣言〉＝別のメソッドの同名の宣言やメソッドの外の同名のフィールドを拾いうる（複数の宣言・メソッドの外の同名）。
/// ・`.Value` の数は補間の文字列（`$"…{x.Value}"`）の中を数えない（Strip が文字列の中身を空にする）＝エンジンの本体に 2 本（Coefficients.p10_ai_silver_overrides.cs・SaveGame.cs）。</summary>
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

    /// <summary>ドットなしの書き手の位置と、その位置が指す本文。受け手の指定が無い量は全体・`@pop`／`@colony` はその型（Pop・Colony）の宣言の本体の中だけ（(1379)）。</summary>
    private static (string Scope, List<Match> At) BareWritersOf(string t, string spec)
    {
        var (member, recv) = Parse(spec);
        if (recv == null) return (t, BareWriters(t, member).ToList());
        string type = spec.EndsWith("@colony", StringComparison.Ordinal) ? "Colony" : "Pop";
        var d = Regex.Match(t, $@"\b(class|struct)\s+{type}\b[^{{;]*\{{");
        if (!d.Success) return (t, new List<Match>());
        int s = d.Index + d.Length, depth = 1, e = s;
        for (; e < t.Length && depth > 0; e++) { if (t[e] == '{') depth++; else if (t[e] == '}') depth--; }
        string body = t[s..e];
        return (body, BareWriters(body, member).ToList());
    }

    /// <summary>ドットなしの書き手の数。</summary>
    internal static int CountBareWriters(string text, string spec)
        => BareWritersOf(Strip(text), spec).At.Count;

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
        => WriterSites(text, spec).Select(s => s.Rhs);

    /// <summary>(1379) 書き手の右辺と、その書き手より前の本文（局所変数の定義を探す範囲）。ドットつき＋ドットなし。</summary>
    internal static IEnumerable<(string Rhs, string Before)> WriterSites(string text, string spec)
    {
        string t = Strip(text);
        foreach (Match m in DottedWriters(t, spec)) yield return (RhsAt(t, m), t[..m.Index]);
        var (scope, at) = BareWritersOf(t, spec);
        foreach (Match m in at) yield return (RhsAt(scope, m), scope[..m.Index]);
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

    /// <summary>(1391) 深さ 2＝局所変数の定義の文の右辺にある局所変数の定義（その文より前で最も近い物）も並べる（深さ 1 までだった穴〔定義の中の定義を変えても指紋が動かない〕を塞ぐ）。</summary>
    internal const int FingerprintDepth = 2;
    private static void AddDefinitions(HashSet<string> lines, string before, string name, string member, int depth)
    {
        Match last = null!;
        foreach (Match d in Regex.Matches(before, $@"\b(?:var|double|float|int|long)\s+{Regex.Escape(name)}\s*=[^;]*;")) last = d;
        if (last == null) return;
        lines.Add("D:" + Regex.Replace(last.Value, @"\s+", ""));
        if (depth >= FingerprintDepth) return;
        string inner = last.Value[(last.Value.IndexOf('=') + 1)..];
        foreach (var t in InputTerms(inner, member).Where(x => x.StartsWith("local:", StringComparison.Ordinal)).Select(x => x[6..]).Where(n => n != name))
            AddDefinitions(lines, before[..last.Index], t, member, depth + 1);
    }

    /// <summary>(1373・1379) 機械で数えられない入力（局所変数を通る右辺）の指紋。書き手の右辺のうち局所変数を含む物を空白を除いて並べ、
    /// (1379) その局所変数の定義の文（書き手より前で最も近い `var／double／float／int／long 名 = …;`）も並べて、先頭 12 桁の SHA-256 にする。
    /// 定義が引数や foreach の変数で文として見つからない時は定義の行を足さない（右辺の文面だけ）。
    /// 変えたら宣言して指紋を更新する（その時は局所変数の中身を見て入力が増えていないか確かめる）。</summary>
    internal static string LocalFedFingerprint(IEnumerable<string> texts, string spec)
    {
        var (member, _) = Parse(spec);
        var lines = new HashSet<string>(StringComparer.Ordinal);
        int count = 0;
        foreach (var site in texts.SelectMany(t => WriterSites(t, spec)))
        {
            var locals = InputTerms(site.Rhs, member).Where(x => x.StartsWith("local:", StringComparison.Ordinal)).Select(x => x[6..]).ToList();
            if (locals.Count == 0) continue;
            if (lines.Add("R:" + Regex.Replace(site.Rhs, @"\s+", ""))) count++;
            foreach (var name in locals) AddDefinitions(lines, site.Before, name, member, 1);
        }
        var rhs = lines.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var h = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", rhs)));
        return count + ":" + Convert.ToHexString(h)[..12].ToLowerInvariant();
    }

    /// <summary>`.Value`（Units.cs と Calendar.cs の外）。注釈と文字列の中は数えない。`?.Value` は数える。</summary>
    internal static int CountValue(string text) => new Regex(@"\.Value\b").Matches(Strip(text)).Count;
}
