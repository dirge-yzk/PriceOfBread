using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace PriceOfBread.App.ViewModels;

/// <summary>
/// イベント文言のカタログ。文言は alerts.csv（埋め込みリソース）から読む＝文言変更は再ビルドが要る。
/// 検出条件はコード側（GameViewModel.DetectEvents）に置く。
/// 形式：id,icon,color,text（text は最終列＝読点を含んでよい。{0} 等の書式穴あり）。
/// リソースが見つからない/壊れている場合は読めた分だけ使い、無いidは素朴な代替表示に落ちる（落ちない）。
/// </summary>
public static class AlertCatalog
{
    private sealed record Entry(string Icon, string Color, string Text);
    private static Dictionary<string, Entry>? _entries;
    private const int AlertCsvFields = 4;   // alerts.csv の列数（id,icon,color,text＝text は最終列）

    private static Dictionary<string, Entry> Load()
    {
        var map = new Dictionary<string, Entry>();
        try
        {
            // ★2026-08-26：ファイルでなく**埋め込みリソース**から読む（配布物を exe とマニュアルの2つだけにする＝Data フォルダを置かない）。
            //   探し方は world.json（SimCore.WorldData）と同じ＝末尾一致で1本引く。見つからなければ空のまま＝表示は id にフォールバック。
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            string? res = System.Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith("alerts.csv", System.StringComparison.Ordinal));
            using (var stream = res is null ? null : asm.GetManifestResourceStream(res))
            using (var reader = stream is null ? null : new StreamReader(stream))
                while (reader?.ReadLine() is { } line)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("id,")) continue;
                    var parts = line.Split(',', AlertCsvFields);   // text は最終列＝読点OK
                    if (parts.Length < AlertCsvFields) continue;
                    map[parts[0].Trim()] = new Entry(parts[1].Trim(), parts[2].Trim(), parts[3]);
                }
        }
        catch { /* 読めない場合は代替表示に落ちる */ }
        return map;
    }

    /// <summary>国名を文の中に持つ報せ（頭に「{国}で、」を付けない）。</summary>
    public static bool WholeSentence(string id) => id == "ev_revolution";

    /// <summary>csv にある全ての id（試験が書式の穴を全 id で確かめる）。</summary>
    public static IReadOnlyCollection<string> Ids { get { _entries ??= Load(); return _entries.Keys; } }

    /// <summary>id の文言で AlertRow を作る。CSVに無ければ id をそのまま出す（壊れても気づける）。</summary>
    public static AlertRow Make(string id, params object[] args)
    {
        _entries ??= Load();
        if (_entries.TryGetValue(id, out var e))
        {
            string text;
            try { text = string.Format(CultureInfo.InvariantCulture, e.Text, args); }
            catch (FormatException) { text = e.Text; }
            return new AlertRow(e.Icon, text, e.Color);
        }
        return new AlertRow("⚠", id, "#808080");
    }
}
