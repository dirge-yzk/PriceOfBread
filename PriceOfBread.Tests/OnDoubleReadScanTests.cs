using System.Text.RegularExpressions;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(927) c81sm Instruments.On の二重読みの走査の番人。事前登録 measurements/m927_20261107_prereg_sm.txt [3]・表 measurements/m913_20261107/on_double_read_table.txt の型。
/// 計器の門 <c>Instruments.On</c> を 1 つの関数の中で 2 回以上読むと、確保と使用が別の読みになり得る（並列の試験で On が途中で変わると NRE か計器の値のずれ・(913)(921) の型）。
/// SimCore の各ファイルを字句で走査し（コメント・文字列を飛ばし、型の直下の丸括弧を持つ波括弧を関数と見なす）、関数ごとの <c>Instruments.On</c> の読みの数を数える。
/// 今の木で 2 回以上読む関数を下の表に名乗る（ファイル::関数名#同名の何番目か＝読みの数）。表に無い関数が 2 回以上読む・表より読みが増える＝赤。減るのは良い方向なので緑（表を縮める）。
/// 限界（自己申告）：式本体のメンバー（=>）と局所関数・ラムダは外側の関数に数える。字句の走査であって構文木ではない。読みの 1 本が確保に使われるかは見ない（件数だけ）。ヘッダの中の文字列の // も剥ぐ（コメントを剥ぐ正規表現は文字列を区別しない＝関数の名が隣の括弧から取られうる）。</summary>
public class OnDoubleReadScanTests
{
    const char BS = (char)92, NL = (char)10, NUL = (char)0;

    // 今の木（11c35e54）で Instruments.On を 2 回以上読む関数。直した関数はここから消す。
    static readonly Dictionary<string, int> Allowed = new()
    {
        ["BilateralMarket.cs::StepBilateral#0"] = 12,
        ["CapitalMarket.cs::Step#0"] = 8,
        ["Diplomacy.cs::ConcludeWar#0"] = 2,
        ["Diplomacy.cs::ResolvePeace#0"] = 5,
        ["EconomyStep.cs::Run#0"] = 11,
        ["EmptyPopEstate.cs::Core#0"] = 3,
        ["Fiscal.cs::Step#0"] = 4,
        ["Fiscal.cs::Borrow#0"] = 2,
        ["Government.cs::Step#0"] = 5,
        ["Government.cs::CountOpenFronts#0"] = 3,
        ["Government.cs::MandateGate#0"] = 5,
        ["Government.cs::Move#0"] = 7,
        ["Government.cs::StepLevel#0"] = 5,
        ["InternationalMarket.cs::PaySales#0"] = 4,
        ["Model.cs::Take#0"] = 2,
        ["Model.cs::Blend#0"] = 2,
        ["MultiNationSim.cs::StepDiscoveries#0"] = 5,
        ["MultiNationSim.cs::StepDay#0"] = 3,
        ["MultiNationSim.cs::StepPorts#0"] = 3,
        ["Politics.cs::Transition#0"] = 7,
        ["SelfPlay.cs::GateAllLawTargets#0"] = 2,
        ["SelfPlay.cs::TryBuildFacility#0"] = 8,
        ["SelfPlay.cs::Apply#0"] = 10,
        ["SelfPlay.cs::Run#0"] = 4,
        ["Simulation.cs::QuarterStart#0"] = 3,
        ["Simulation.cs::AddStateClaim#0"] = 2,
        ["Simulation.cs::QuarterEnd#0"] = 2,
        ["Simulation.cs::MonthSettle#0"] = 18,
        ["Simulation.cs::DemographicStep#0"] = 5,
        ["Territory.cs::static#0"] = 4,
        ["World.cs::TriggerEpidemic#0"] = 7,
        ["World.cs::UpdatePriceLevel#0"] = 2,
        ["World.cs::StepConstruction#0"] = 2,
    };

    static bool W(char c) => char.IsLetterOrDigit(c) || c == '_';

    static string StripComments(string h) => Regex.Replace(h, @"//[^\n]*|/\*.*?\*/", " ", RegexOptions.Singleline);

    internal static List<(string Name, int Reads)> Scan(string src)
    {
        var res = new List<(string, int)>();
        var stack = new List<(char Kind, string Name, int[] Count)>();
        int hdr = 0, i = 0, n = src.Length;
        while (i < n)
        {
            char ch = src[i], nx = i + 1 < n ? src[i + 1] : NUL;
            if (ch == '/' && nx == '/') { int j = src.IndexOf(NL, i); i = j < 0 ? n : j; continue; }
            if (ch == '/' && nx == '*') { int j = src.IndexOf("*/", i + 2, StringComparison.Ordinal); i = j < 0 ? n : j + 2; continue; }
            if (ch == '"' || ((ch == '$' || ch == '@') && (nx == '"' || ((nx == '$' || nx == '@') && i + 2 < n && src[i + 2] == '"'))))
            {
                int k = i; bool verbatim = false;
                while (src[k] == '$' || src[k] == '@') { if (src[k] == '@') verbatim = true; k++; }
                k++;
                while (k < n)
                {
                    if (verbatim) { if (src[k] == '"') { if (k + 1 < n && src[k + 1] == '"') { k += 2; continue; } break; } }
                    else { if (src[k] == BS) { k += 2; continue; } if (src[k] == '"') break; }
                    k++;
                }
                i = k + 1; continue;
            }
            if (ch == (char)39)
            {
                int k = i + 1;
                while (k < n && src[k] != (char)39) { if (src[k] == BS) k++; k++; }
                i = k + 1; continue;
            }
            if (string.CompareOrdinal(src, i, "Instruments.On", 0, 14) == 0 && !(i + 14 < n && W(src[i + 14])) && !(i > 0 && (W(src[i - 1]) || src[i - 1] == '.')))
            {
                for (int s = stack.Count - 1; s >= 0; s--) if (stack[s].Kind == 'm') { stack[s].Count[0]++; break; }
                i += 14; continue;
            }
            if (ch == '{')
            {
                string h = StripComments(src[hdr..i]);   // 鍵の名は、コメントを剥いだ頭から取る（コメントの中の括弧や class の語を名にしない）
                if (Regex.IsMatch(h, "(?<![A-Za-z0-9_])(class|struct|namespace|record|enum|interface)(?![A-Za-z0-9_])")) stack.Add(('t', "", new int[1]));
                else if (stack.Count > 0 && stack[^1].Kind == 't' && h.Contains('('))
                {
                    var m = Regex.Match(h, @"([\p{L}\p{N}_]+)\s*(?:<[^>]*>)?\s*[(]");
                    stack.Add(('m', m.Success ? m.Groups[1].Value : "?", new int[1]));
                }
                else stack.Add(('o', "", new int[1]));
                hdr = i + 1;
            }
            else if (ch == '}')
            {
                if (stack.Count > 0) { var e = stack[^1]; stack.RemoveAt(stack.Count - 1); if (e.Kind == 'm') res.Add((e.Name, e.Count[0])); }
                hdr = i + 1;
            }
            else if (ch == ';') hdr = i + 1;
            i++;
        }
        return res;
    }

    static Dictionary<string, int> Current()
    {
        var found = new Dictionary<string, int>();
        string dir = Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore");
        foreach (var f in Directory.GetFiles(dir, "*.cs").OrderBy(x => x, StringComparer.Ordinal))
        {
            var seen = new Dictionary<string, int>();
            foreach (var (name, reads) in Scan(File.ReadAllText(f)))
            {
                seen.TryGetValue(name, out int k); seen[name] = k + 1;
                if (reads >= 2) found[$"{Path.GetFileName(f)}::{name}#{k}"] = reads;
            }
        }
        return found;
    }

    [Fact]
    public void InstrumentsOnを2回以上読む関数は表の物だけで_読みは増えない()
    {
        var cur = Current();
        var bad = new List<string>();
        foreach (var (k, v) in cur)
            if (!Allowed.TryGetValue(k, out int a)) bad.Add($"新しい二重読み {k} 読み{v}");
            else if (v > a) bad.Add($"読みが増えた {k} {a}→{v}");
        Assert.True(bad.Count == 0, string.Join(NL.ToString(), bad));
    }

    [Fact]
    public void 走査は字句を正しく飛ばし関数ごとに数える()
    {
        string src = string.Join(NL.ToString(),
            "class A {",
            " void F() { var a = Instruments.On; // Instruments.On",
            " var s = \"Instruments.On\"; if (Instruments.On) { } }",
            " void G() { var b = Instruments.On; }",
            " bool H => Instruments.On;",
            "}");
        var r = Scan(src);
        Assert.Equal(new[] { ("F", 2), ("G", 1) }, r.ToArray());
    }

    [Fact]
    public void ヘッダのコメントの_class_の語と括弧は関数の名と種類に影響しない()
    {
        // コメントを剥がないと、// の class が型の頭と読まれて F が関数に数えられない
        string src = string.Join(NL.ToString(),
            "class A {",
            " // class helper ( x )",
            " void F(int a /* ( */) { var a = Instruments.On; var b = Instruments.On; }",
            "}");
        var r = Scan(src);
        Assert.Equal(new[] { ("F", 2) }, r.ToArray());
    }
}
