using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App;

/// <summary>`--uishot` の時点ごとの表の書き出し（表.txt）と機械の点検（点検.txt）。画像より安く読むための観測だけ＝Display の中身は変えない・盤面に書かない。
/// 表は <see cref="TableCatalog.All"/> を <see cref="TableSnapshot.From"/>（画面と同じ書式の文字）で読む＝描画を通さない。Avalonia に依らない（試験が読める）。</summary>
public static class UiShotDump
{
    /// <summary>点検の網が見ていない範囲（点検.txt の先頭に必ず出す）。</summary>
    public static readonly string[] NotCovered =
    [
        "ヘッダーの数字（日付・国庫・物価・人口など）と、ヘッダーとページの突き合わせ",
        "地図・線グラフ・半円・積み上げ棒・段の押し釦・動詞の押し釦の中身（文字にならないもの）",
        "吹き出しの文・ポップアップ・窓の中身・表の目録（TableCatalog）に載らない画面の欄",
        "GameViewModel が行を絞った表（生産の財の表の絞り込み）と、読みの中で前回との差を採る表の矢印",
        "表どうしの突き合わせは同じ行の名前・同じ列の名前・同じ単位の組だけ（名前が違う量・単位が違う量・輸入と輸出の組は見ていない）",
        "構成比の和は列の鍵が share の列だけ（割合の意味で使われるほかの列は見ていない）",
        "見た目（切れ・重なり・色）は画像と LogOuterFlow の判定が見る分担で、ここでは見ない",
    ];

    public sealed record Result(string TablesText, string CheckText, int TableCount, int Findings);

    /// <summary>目録の全ての表を読み、表.txt の本文と点検.txt の本文を作る。読めない表は名前を出して飛ばす（全体を止めない）。</summary>
    public static Result Build(MultiNationSim sim, World w, string label)
    {
        var snaps = new List<(ITable T, TableSnapshot S)>();
        var failures = new List<string>();
        foreach (var t in TableCatalog.All)
        {
            try { snaps.Add((t, TableSnapshot.From(t.Read(sim, w)))); }
            catch (Exception ex) { failures.Add($"{t.Key}（{t.Name}）を読めない: {ex.GetType().Name} {ex.Message}"); }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"# {label}");
        foreach (var page in Enum.GetValues<Page>())
        {
            var inPage = snaps.Where(x => x.T.Page == page).ToList();
            if (inPage.Count == 0) continue;
            sb.AppendLine().AppendLine($"■ {PageKeys.Name(page)}");
            foreach (var (t, s) in inPage)
            {
                sb.AppendLine().AppendLine($"□ {s.Name}（{s.Key}）");
                sb.AppendLine(string.Join('\t', s.Columns.Select(c => c.Name)));
                string? lastGroup = null;
                foreach (var r in s.Rows)
                {
                    if (r.Group is { Length: > 0 } g && g != lastGroup) { sb.AppendLine($"【{g}】"); lastGroup = g; }
                    sb.AppendLine(string.Join('\t', r.Cells.Select(c => Flat(c.Text))));
                }
            }
        }
        foreach (var f in failures) sb.AppendLine().AppendLine("！" + f);

        var findings = Check(snaps.Select(x => x.S).ToList());
        findings.InsertRange(0, failures.Select(f => "表を読めない: " + f));
        var cb = new StringBuilder();
        cb.AppendLine($"# 点検 {label}");
        cb.AppendLine("この点検は次を見ていない：");
        foreach (var n in NotCovered) cb.AppendLine("・" + n);
        cb.AppendLine();
        cb.AppendLine($"読んだ表 {snaps.Count}/{TableCatalog.All.Count}・指摘 {findings.Count}件");
        foreach (var f in findings) cb.AppendLine("- " + f);
        return new Result(sb.ToString(), cb.ToString(), snaps.Count, findings.Count);
    }

    /// <summary>ディレクトリへ表.txt と点検.txt を書く。</summary>
    public static Result Write(string dir, MultiNationSim sim, World w, string label)
    {
        var r = Build(sim, w, label);
        System.IO.Directory.CreateDirectory(dir);
        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "表.txt"), r.TablesText, new UTF8Encoding(false));
        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "点検.txt"), r.CheckText, new UTF8Encoding(false));
        return r;
    }

    private static string Flat(string s) => s.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    private static bool NotANumberText(string s) =>
        s.Contains("NaN", StringComparison.OrdinalIgnoreCase) || s.Contains('∞') || s.Contains("Infinity", StringComparison.OrdinalIgnoreCase);

    /// <summary>機械の点検。指摘の文は「表の名前・行の名前・列の名前・見た文字」を含める。</summary>
    public static List<string> Check(IReadOnlyList<TableSnapshot> tables)
    {
        var o = new List<string>();
        foreach (var s in tables)
        {
            // 1. 数でない文字
            foreach (var c in s.Columns) if (NotANumberText(c.Name)) o.Add($"{s.Name}：列の名前に数でない文字「{c.Name}」");
            foreach (var r in s.Rows)
            {
                if (NotANumberText(r.Name)) o.Add($"{s.Name}：行の名前に数でない文字「{r.Name}」");
                for (int i = 0; i < r.Cells.Count && i < s.Columns.Count; i++)
                {
                    var cell = r.Cells[i];
                    if (NotANumberText(cell.Text)) o.Add($"{s.Name}／{r.Name}／{s.Columns[i].Name}：数でない文字「{cell.Text}」");
                    // 2. 負の人数
                    if (s.Columns[i].Unit == Unit.People && cell.Sort < 0) o.Add($"{s.Name}／{r.Name}／{s.Columns[i].Name}：負の人数「{cell.Text}」");
                }
            }
            // 3. 構成比の列（鍵が share）の和が 100% から 1 ポイントを超えて外れる（群ごと・群が無ければ表全体）
            for (int i = 0; i < s.Columns.Count; i++)
            {
                if (s.Columns[i].Key != "share" || s.Columns[i].Unit != Unit.Share) continue;
                foreach (var grp in s.Rows.GroupBy(r => r.Group ?? ""))
                {
                    double sum = 0; int n = 0;
                    foreach (var r in grp) { double v = r.Cells[i].Sort; if (double.IsFinite(v)) { sum += v; n++; } }
                    if (n > 0 && !(Math.Abs(sum - 1.0) <= 0.01))
                        o.Add($"{s.Name}{(grp.Key.Length > 0 ? "／" + grp.Key : "")}／{s.Columns[i].Name}：和が{sum * 100:F1}%（{n}行）");
                }
            }
        }
        // 4. 同じ名前の量が表によって違う値（行の名前・列の名前・単位が同じ組・別の表どうし。文字で比べる＝画面で見える差だけ）
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
                            o.Add($"同じ量が表で違う：「{r.Name}／{s.Columns[i].Name}」{prev.Table}＝{prev.Text}・{s.Name}＝{cell.Text}");
                    }
                    else seen[key] = (s.Name, s.Key, cell.Text);
                }
        }
        return o;
    }
}
