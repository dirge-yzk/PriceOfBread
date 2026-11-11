using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App;

/// <summary>`--uishot` の時点ごとの表の書き出し（表.txt）と機械の点検（点検.txt）。画像より安く読むための観測だけ＝Display の中身は変えない・盤面に書かない。
/// 表は <see cref="TableCatalog.All"/> を <see cref="TableSnapshot.From"/>（画面と同じ書式の文字）で読む＝描画を通さない。
/// 画面（GameViewModel）だけが持つ表と文字の欄は <see cref="UiShotExtra"/> で受ける（画面に出している文字列をそのまま・新しい計算を足さない）。Avalonia に依らない（試験が読める）。</summary>
public static class UiShotDump
{
    /// <summary>画面の側だけが持つ物（ヘッダーの文字と、財政・外交・POP の表）。呼び側（MainWindow）が GameViewModel から集める。</summary>
    public sealed record UiShotExtra(IReadOnlyList<string> Header, IReadOnlyList<TableSnapshot> Tables);

    /// <summary>1 つの規則。<see cref="Needs"/>＝要る表の鍵（無ければ判定できなかった規則として点検.txt の先頭に出す）。<see cref="NeedsPrevious"/>＝前回の撮影の写しが要る。</summary>
    public sealed record Rule(string Name, string[] Needs, bool NeedsPrevious, Func<CheckContext, IEnumerable<string>> Run);

    public sealed class CheckContext(IReadOnlyList<TableSnapshot> tables, IReadOnlyList<TableSnapshot>? previous)
    {
        public IReadOnlyList<TableSnapshot> Tables { get; } = tables;
        public IReadOnlyList<TableSnapshot>? Previous { get; } = previous;
        public TableSnapshot? Get(string key) => Tables.FirstOrDefault(t => t.Key == key);
    }

    public sealed record CheckReport(List<string> Findings, List<string> Ran, List<string> Skipped);

    /// <summary>点検が表の文字だけからは判定できない規則（点検.txt の先頭に必ず出す）。</summary>
    public static readonly string[] Unjudgeable =
    [
        "価格比×為替＝世界価格比の検算：為替を掛ける前後の値が表の文字に無い",
        "稼働率×工場数と生産量の桁の検算：工場 1 棟あたりの産出が表の文字に無い",
        "世界価格比が複数の財で同じ値にそろう：丸めた値の一致は普通に起こり、誤検出が多い",
        "解禁済みの財の親の行が未解禁：親子の関係が表の文字に無い",
        "巨大建造物の年が史実より 30 年以上早い：巨大建造物の表も史実の年も表の文字に無い",
    ];

    /// <summary>点検の網が見ていない範囲（点検.txt の先頭に必ず出す）。</summary>
    public static readonly string[] NotCovered =
    [
        "地図・線グラフ・半円・積み上げ棒・段の押し釦・動詞の押し釦の押した結果（文字にならないもの）",
        "吹き出しの文・ポップアップ・窓の中身（表に載る吹き出しは国の一覧の行の詳細だけ書き出す）",
        "生産ページの巨大建造物・問題の一覧・年表、政治と法ページの議会・支持基盤・勢力の表",
        "GameViewModel が行を絞った表（生産の財の表の絞り込み）と、読みの中で前回との差を採る表の矢印",
        "表どうしの突き合わせは同じ行の名前・同じ列の名前・同じ単位の組だけ（名前が違う量・単位が違う量・輸入と輸出の組は見ていない）",
        "構成比の和は列の鍵が share の列だけ（割合の意味で使われるほかの列は見ていない）",
        "見た目（切れ・重なり・色）は画像と LogOuterFlow の判定が見る分担で、ここでは見ない",
    ];

    public sealed record Result(string TablesText, string CheckText, int TableCount, int Findings, IReadOnlyList<TableSnapshot> Snapshots);

    /// <summary>表の鍵のうち、行ごとの詳細（吹き出しの文）も書き出す表。</summary>
    private static readonly HashSet<string> WithRowDetail = ["diplomacy.nations"];

    /// <summary>文字だけの表（画面が文字の欄で出している物）を表の形にする。1 つ目の欄を行の名前にする。</summary>
    public static TableSnapshot TextTable(string key, string name, Page page, IReadOnlyList<string> columns, IEnumerable<IReadOnlyList<string>> rows, IEnumerable<string?>? groups = null)
    {
        var cols = columns.Select((c, i) => new ColumnSpec(i == 0 ? "name" : "c" + i, c, ColumnKind.Text, null, 24, double.PositiveInfinity)).ToList();
        var gs = groups?.ToList();
        var rv = new List<RowView>();
        int n = 0;
        foreach (var r in rows)
        {
            var cells = Enumerable.Range(0, cols.Count).Select(i => new CellView(i < r.Count ? r[i] : "", double.NaN)).ToList();
            rv.Add(new RowView(key + "." + n, r.Count > 0 ? r[0] : "", gs != null && n < gs.Count ? gs[n] : null, cells));
            n++;
        }
        return new TableSnapshot(key, name, cols, rv, page);
    }

    /// <summary>目録の全ての表と、画面の側の表を読み、表.txt の本文と点検.txt の本文を作る。読めない表は名前を出して飛ばす（全体を止めない）。
    /// <paramref name="previous"/>＝前回の撮影の写し（前回より下がった行の規則が読む）。</summary>
    public static Result Build(MultiNationSim sim, World w, string label, UiShotExtra? extra = null, IReadOnlyList<TableSnapshot>? previous = null)
    {
        var snaps = new List<(Page? Page, TableSnapshot S)>();
        var failures = new List<string>();
        foreach (var t in TableCatalog.All)
        {
            try { snaps.Add((t.Page, TableSnapshot.From(t.Read(sim, w)))); }
            catch (Exception ex) { failures.Add($"{t.Key}（{t.Name}）を読めない: {ex.GetType().Name} {ex.Message}"); }
        }
        int catalogRead = snaps.Count;
        if (extra is not null) foreach (var s in extra.Tables) snaps.Add((s.Page, s));

        var sb = new StringBuilder();
        sb.AppendLine($"# {label}");
        if (extra is { Header.Count: > 0 })
        {
            sb.AppendLine().AppendLine("■ ヘッダー");
            foreach (var h in extra.Header) sb.AppendLine(Flat(h));
        }
        foreach (var page in Enum.GetValues<Page>())
        {
            var inPage = snaps.Where(x => x.Page == page).ToList();
            if (inPage.Count == 0) continue;
            sb.AppendLine().AppendLine($"■ {PageKeys.Name(page)}");
            foreach (var (_, s) in inPage) AppendTable(sb, s);
        }
        foreach (var f in failures) sb.AppendLine().AppendLine("！" + f);

        var all = snaps.Select(x => x.S).ToList();
        var report = Evaluate(all, previous);
        var findings = report.Findings;
        findings.InsertRange(0, failures.Select(f => "表を読めない: " + f));
        var cb = new StringBuilder();
        cb.AppendLine($"# 点検 {label}");
        cb.AppendLine("回した規則（" + report.Ran.Count + "本）：" + string.Join("・", report.Ran));
        cb.AppendLine("判定できなかった規則：");
        foreach (var k in report.Skipped) cb.AppendLine("・" + k);
        foreach (var u in Unjudgeable) cb.AppendLine("・" + u);
        cb.AppendLine("この点検は次を見ていない：");
        foreach (var n in NotCovered) cb.AppendLine("・" + n);
        cb.AppendLine();
        cb.AppendLine($"読んだ表 {catalogRead}/{TableCatalog.All.Count}（画面の表 {all.Count - catalogRead} 枚を足す）・指摘 {findings.Count}件");
        foreach (var f in findings) cb.AppendLine("- " + f);
        return new Result(sb.ToString(), cb.ToString(), all.Count, findings.Count, all);
    }

    private static void AppendTable(StringBuilder sb, TableSnapshot s)
    {
        sb.AppendLine().AppendLine($"□ {s.Name}（{s.Key}）");
        sb.AppendLine(string.Join('\t', s.Columns.Select(c => c.Name)));
        string? lastGroup = null;
        bool detail = WithRowDetail.Contains(s.Key);
        foreach (var r in s.Rows)
        {
            if (r.Group is { Length: > 0 } g && g != lastGroup) { sb.AppendLine($"【{g}】"); lastGroup = g; }
            sb.AppendLine(string.Join('\t', r.Cells.Select(c => Flat(CellText(c)))));
            if (detail && r.Tip is { Length: > 0 } tip) sb.AppendLine("　　" + Flat(tip));
        }
    }

    /// <summary>セルの文字。段は「施行→目標」・棒（発見の解禁）は「済／未 点数/満」・動詞は名前と不可の印と理由。画面が色や形で見せている物を文字にするだけ。</summary>
    public static string CellText(CellView c) => c.Extra switch
    {
        StageCell st => StageText(st, c.Text),
        BarCell bc => BarText(bc),
        VerbCell vb => vb.Name + (vb.Enabled ? "" : "（不可）") + (string.IsNullOrEmpty(vb.Tip) ? "" : "「" + vb.Tip + "」"),
        _ => c.Text,
    };

    private static string StageText(StageCell st, string current)
    {
        if (st.Target == st.Current) return current;
        int t = st.Target - st.LevelBase;
        string target = t >= 0 && t < st.Labels.Count ? st.Labels[t] : st.Target.ToString();
        return $"{current}→{target}";
    }

    private static string BarText(BarCell bc)
    {
        if (bc.Parts.Count == 1 && bc.Parts[0].Color == DiscoveryTables.KnownInk) return "済";
        double sum = 0; foreach (var p in bc.Parts) if (double.IsFinite(p.Value)) sum += p.Value;
        return $"未 {sum:F0}/{bc.Full:F0}";
    }

    /// <summary>ディレクトリへ表.txt と点検.txt を書く。</summary>
    public static Result Write(string dir, MultiNationSim sim, World w, string label, UiShotExtra? extra = null, IReadOnlyList<TableSnapshot>? previous = null)
    {
        var r = Build(sim, w, label, extra, previous);
        System.IO.Directory.CreateDirectory(dir);
        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "表.txt"), r.TablesText, new UTF8Encoding(false));
        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "点検.txt"), r.CheckText, new UTF8Encoding(false));
        return r;
    }

    private static string Flat(string s) => s.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    private static bool NotANumberText(string s) =>
        s.Contains("NaN", StringComparison.OrdinalIgnoreCase) || s.Contains('∞') || s.Contains("Infinity", StringComparison.OrdinalIgnoreCase);

    /// <summary>機械の点検（指摘の文だけ）。指摘の文は「表の名前・行の名前・列の名前・見た文字」を含める。</summary>
    public static List<string> Check(IReadOnlyList<TableSnapshot> tables, IReadOnlyList<TableSnapshot>? previous = null) => Evaluate(tables, previous).Findings;

    /// <summary>規則を回し、指摘と、回した規則・判定できなかった規則を返す。</summary>
    public static CheckReport Evaluate(IReadOnlyList<TableSnapshot> tables, IReadOnlyList<TableSnapshot>? previous = null)
    {
        var ctx = new CheckContext(tables, previous);
        var findings = new List<string>(); var ran = new List<string>(); var skipped = new List<string>();
        foreach (var rule in Rules)
        {
            var missing = rule.Needs.Where(k => ctx.Get(k) is null).ToList();
            if (missing.Count > 0) { skipped.Add($"{rule.Name}：表が無い（{string.Join("・", missing)}）"); continue; }
            if (rule.NeedsPrevious && previous is null) { skipped.Add($"{rule.Name}：前回の撮影が無い（最初の時点）"); continue; }
            ran.Add(rule.Name);
            findings.AddRange(rule.Run(ctx));
        }
        return new CheckReport(findings, ran, skipped);
    }

    private static int ColOf(TableSnapshot s, string name) { for (int i = 0; i < s.Columns.Count; i++) if (s.Columns[i].Name == name) return i; return -1; }

    private static readonly Regex PeopleRe = new(@"(?<n>[\d.,]+)(?<u>[kM]?)人", RegexOptions.Compiled);

    /// <summary>「戦死 自国 1.3k人　相手 1.9k人」の形から、見出しの後ろの人数を順に取る（自国・相手）。取れなければ空。</summary>
    internal static List<double> CountsAfter(string text, string head)
    {
        var o = new List<double>();
        int at = text.IndexOf(head, StringComparison.Ordinal);
        if (at < 0) return o;
        string rest = text[(at + head.Length)..];
        foreach (var stop in new[] { "民間人の死", "戦費" })
        {
            int k = rest.IndexOf(stop, StringComparison.Ordinal);
            if (k >= 0 && stop != head) rest = rest[..k];
        }
        foreach (Match m in PeopleRe.Matches(rest))
        {
            double v = double.Parse(m.Groups["n"].Value.Replace(",", ""), System.Globalization.CultureInfo.InvariantCulture);
            o.Add(v * (m.Groups["u"].Value == "k" ? 1e3 : m.Groups["u"].Value == "M" ? 1e6 : 1));
        }
        return o;
    }

    private static readonly Rule[] Rules =
    [
        new("数でない文字", [], false, ctx => NotANumber(ctx.Tables)),
        new("負の人数", [], false, ctx => NegativePeople(ctx.Tables)),
        new("構成比の和", [], false, ctx => ShareSums(ctx.Tables)),
        new("同じ量が表で違う", [], false, ctx => SameQuantity(ctx.Tables)),
        new("輸入元・輸出先が自国の州", ["trade.imports", "trade.exports", "pop.regions"], false, OwnProvinceAsPartner),
        new("労働の能力が供給より小さい", ["production.labor"], false, CapacityBelowSupply),
        new("輸出の世界シェアが 100%", ["trade.exports"], false, FullWorldShare),
        new("未解禁の発見と同じ名前の財に量がある", ["discovery.rows", "discovery.social", "production.goods"], false, LockedButProduced),
        new("満足度 0% の階級が人口の 1% 超", ["pop.classes"], false, ZeroSatisfaction),
        new("戦争の民間人の死が兵の戦死の 5 倍超", ["diplomacy.wars", "diplomacy.pastWars"], false, CivilianToSoldier),
        new("予算の支出・収入と国庫の台帳の同名項目の額の比が 0.5〜2 の外", ["fiscal.spend", "fiscal.income", "fiscal.ledger"], false, BudgetVsLedger),
        new("技術力が前回の撮影より下がった行", ["discovery.rows", "discovery.social", "discovery.sites"], true, TechDropped),
    ];

    private static IEnumerable<string> NotANumber(IReadOnlyList<TableSnapshot> tables)
    {
        foreach (var s in tables)
        {
            foreach (var c in s.Columns) if (NotANumberText(c.Name)) yield return $"{s.Name}：列の名前に数でない文字「{c.Name}」";
            foreach (var r in s.Rows)
            {
                if (NotANumberText(r.Name)) yield return $"{s.Name}：行の名前に数でない文字「{r.Name}」";
                for (int i = 0; i < r.Cells.Count && i < s.Columns.Count; i++)
                    if (NotANumberText(r.Cells[i].Text)) yield return $"{s.Name}／{r.Name}／{s.Columns[i].Name}：数でない文字「{r.Cells[i].Text}」";
            }
        }
    }

    private static IEnumerable<string> NegativePeople(IReadOnlyList<TableSnapshot> tables)
    {
        foreach (var s in tables)
            foreach (var r in s.Rows)
                for (int i = 0; i < r.Cells.Count && i < s.Columns.Count; i++)
                    if (s.Columns[i].Unit == Unit.People && r.Cells[i].Sort < 0) yield return $"{s.Name}／{r.Name}／{s.Columns[i].Name}：負の人数「{r.Cells[i].Text}」";
    }

    /// <summary>構成比の列（鍵が share）の和が 100% から 1 ポイントを超えて外れる（群ごと・群が無ければ表全体）。</summary>
    private static IEnumerable<string> ShareSums(IReadOnlyList<TableSnapshot> tables)
    {
        foreach (var s in tables)
            for (int i = 0; i < s.Columns.Count; i++)
            {
                if (s.Columns[i].Key != "share" || s.Columns[i].Unit != Unit.Share) continue;
                foreach (var grp in s.Rows.GroupBy(r => r.Group ?? ""))
                {
                    double sum = 0; int n = 0;
                    foreach (var r in grp) { double v = r.Cells[i].Sort; if (double.IsFinite(v)) { sum += v; n++; } }
                    if (n > 0 && !(Math.Abs(sum - 1.0) <= 0.01))
                        yield return $"{s.Name}{(grp.Key.Length > 0 ? "／" + grp.Key : "")}／{s.Columns[i].Name}：和が{sum * 100:F1}%（{n}行）";
                }
            }
    }

    /// <summary>同じ名前の量が表によって違う値（行の名前・列の名前・単位が同じ組・別の表どうし。文字で比べる＝画面で見える差だけ）。</summary>
    private static IEnumerable<string> SameQuantity(IReadOnlyList<TableSnapshot> tables)
    {
        var seen = new Dictionary<(string Row, string Col, Unit Unit), (string Table, string Key, string Text)>();
        var reported = new HashSet<(string, string, Unit)>();
        foreach (var s in tables)
        {
            var inThis = new HashSet<(string, string, Unit)>();
            foreach (var r in s.Rows)
                for (int i = 0; i < r.Cells.Count && i < s.Columns.Count; i++)
                {
                    if (s.Columns[i].Kind != ColumnKind.Number || s.Columns[i].Unit is not { } u) continue;
                    var cell = r.Cells[i];
                    if (cell.Text.Length == 0) continue;
                    var key = (r.Name, s.Columns[i].Name, u);
                    if (!inThis.Add(key)) continue;   // 同じ表の中の重複（群違いの同名）は比べない
                    if (seen.TryGetValue(key, out var prev))
                    {
                        if (prev.Table != s.Name && prev.Text != cell.Text && !(prev.Key.StartsWith("trade.") && s.Key.StartsWith("trade.")) && reported.Add(key))   // 輸入と輸出は別の量（同じ財の名前で違って当然）
                            yield return $"同じ量が表で違う：「{r.Name}／{s.Columns[i].Name}」{prev.Table}＝{prev.Text}・{s.Name}＝{cell.Text}";
                    }
                    else seen[key] = (s.Name, s.Key, cell.Text);
                }
        }
    }

    private static IEnumerable<string> OwnProvinceAsPartner(CheckContext ctx)
    {
        var own = ctx.Get("pop.regions")!.Rows.Select(r => r.Name).ToHashSet();
        foreach (var key in new[] { "trade.imports", "trade.exports" })
        {
            var s = ctx.Get(key)!;
            int pc = Math.Max(ColOf(s, "輸入元"), ColOf(s, "輸出先"));
            if (pc < 0) continue;
            foreach (var r in s.Rows)
                foreach (var part in r.Cells[pc].Text.Split(['、', ',', ' ', '　', '・'], StringSplitOptions.RemoveEmptyEntries))
                    if (own.Contains(part)) yield return $"{s.Name}／{r.Name}／{s.Columns[pc].Name}：自国の州「{part}」が相手になっている";
        }
    }

    private static IEnumerable<string> CapacityBelowSupply(CheckContext ctx)
    {
        var s = ctx.Get("production.labor")!;
        int cap = ColOf(s, "人力の能力"), sup = ColOf(s, "供給");
        if (cap < 0 || sup < 0) yield break;
        foreach (var r in s.Rows)
        {
            double c = r.Cells[cap].Sort, v = r.Cells[sup].Sort;
            if (double.IsFinite(c) && double.IsFinite(v) && v > 0 && c < v) yield return $"{s.Name}／{r.Name}：人力の能力 {r.Cells[cap].Text} が供給 {r.Cells[sup].Text} より小さい";
        }
    }

    private static IEnumerable<string> FullWorldShare(CheckContext ctx)
    {
        var s = ctx.Get("trade.exports")!;
        int sh = ColOf(s, "世界シェア");
        if (sh < 0) yield break;
        foreach (var r in s.Rows)
            if (r.Cells[sh].Sort >= 0.995) yield return $"{s.Name}／{r.Name}：世界シェア {r.Cells[sh].Text}";
    }

    private static IEnumerable<string> LockedButProduced(CheckContext ctx)
    {
        var locked = new HashSet<string>();
        foreach (var key in new[] { "discovery.rows", "discovery.social" })
        {
            var d = ctx.Get(key)!;
            int bar = ColOf(d, "解禁");
            if (bar < 0) continue;
            foreach (var r in d.Rows) if (CellText(r.Cells[bar]).StartsWith("未", StringComparison.Ordinal)) locked.Add(r.Name);
        }
        var g = ctx.Get("production.goods")!;
        foreach (var colName in new[] { "生産量", "輸出量", "輸入量" })
        {
            int ci = ColOf(g, colName);
            if (ci < 0) continue;
            foreach (var r in g.Rows)
                if (locked.Contains(r.Name) && r.Cells[ci].Sort > 0) yield return $"{g.Name}／{r.Name}／{colName}：発見の表で未解禁なのに {r.Cells[ci].Text}";
        }
    }

    private static IEnumerable<string> ZeroSatisfaction(CheckContext ctx)
    {
        var s = ctx.Get("pop.classes")!;
        int sat = ColOf(s, "満足度"), size = ColOf(s, "規模");
        if (sat < 0 || size < 0) yield break;
        foreach (var r in s.Rows)
        {
            var m = Regex.Match(r.Cells[size].Text, @"\((?<p>[\d.]+)%\)");
            if (!m.Success) continue;
            double share = double.Parse(m.Groups["p"].Value, System.Globalization.CultureInfo.InvariantCulture) / 100;
            if (r.Cells[sat].Sort <= 0 && share > 0.01) yield return $"{s.Name}／{r.Name}：満足度 {r.Cells[sat].Text} で規模 {r.Cells[size].Text}";
        }
    }

    private static IEnumerable<string> CivilianToSoldier(CheckContext ctx)
    {
        foreach (var key in new[] { "diplomacy.wars", "diplomacy.pastWars" })
            foreach (var r in ctx.Get(key)!.Rows)
            {
                string text = string.Join(" ", r.Cells.Select(c => c.Text));
                var dead = CountsAfter(text, "戦死");
                var civ = CountsAfter(text, "民間人の死");
                string[] sides = ["自国", "相手"];
                for (int i = 0; i < sides.Length && i < dead.Count && i < civ.Count; i++)
                    if (dead[i] > 0 && civ[i] > 5 * dead[i]) yield return $"戦争「{r.Name}」：{sides[i]}の民間人の死が兵の戦死の{civ[i] / dead[i]:F0}倍（{civ[i]:F0}人÷{dead[i]:F0}人）";
            }
    }

    private static IEnumerable<string> BudgetVsLedger(CheckContext ctx)
    {
        var led = ctx.Get("fiscal.ledger")!;
        int la = ColOf(led, "金額");
        if (la < 0) yield break;
        foreach (var (key, side) in new[] { ("fiscal.spend", "国庫の支出"), ("fiscal.income", "国庫の収入") })
        {
            var b = ctx.Get(key)!;
            int ba = ColOf(b, "実効");
            if (ba < 0) continue;
            foreach (var r in b.Rows)
            {
                var lr = led.Rows.FirstOrDefault(x => x.Name == r.Name && x.Group == side);
                if (lr is null) continue;
                double bv = Math.Abs(r.Cells[ba].Sort), lv = Math.Abs(lr.Cells[la].Sort);
                if (!(bv >= 100) || !(lv >= 100)) continue;   // 月 £100 未満は丸めの揺れ
                double ratio = bv / lv;
                if (!(ratio >= 0.5 && ratio <= 2)) yield return $"{b.Name}／{r.Name}：予算の実効 {r.Cells[ba].Text} と台帳の金額 {lr.Cells[la].Text} の比 {ratio:F2}";
            }
        }
    }

    private static IEnumerable<string> TechDropped(CheckContext ctx)
    {
        foreach (var key in new[] { "discovery.rows", "discovery.social", "discovery.sites" })
        {
            var now = ctx.Get(key)!;
            var before = ctx.Previous!.FirstOrDefault(t => t.Key == key);
            int c1 = ColOf(now, "技術力"), c0 = before is null ? -1 : ColOf(before, "技術力");
            if (before is null || c1 < 0 || c0 < 0) continue;
            var old = before.Rows.GroupBy(r => r.Key).ToDictionary(g => g.Key, g => g.First());
            foreach (var r in now.Rows)
            {
                if (!old.TryGetValue(r.Key, out var o)) continue;
                double a = o.Cells[c0].Sort, b = r.Cells[c1].Sort;
                if (double.IsFinite(a) && double.IsFinite(b) && b < a - 0.5) yield return $"{now.Name}／{r.Name}：技術力が前回 {o.Cells[c0].Text} から {r.Cells[c1].Text} へ下がった";
            }
        }
    }
}
