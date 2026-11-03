using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.Layout;

/// <summary>画面の中央に出せるもの＝7 ページと地図。宣言の順が <see cref="PageKeys"/> のページの順（地図は最後＝キーを持たない）。</summary>
public enum Slot { Production, Market, FiscalMoney, Population, PoliticsLaw, DiplomacyWar, Discovery, Map }

/// <summary>中央に出せるものの名前とキー。キーは本体の <see cref="PageKeys"/> が持つ。ここは引くだけ。</summary>
public static class SlotInfo
{
    public static readonly IReadOnlyList<Slot> All = Enum.GetValues<Slot>();

    /// <summary>ヘッダーの押し釦に並ぶもの（7 ページ。地図は釦を持たない）。</summary>
    public static readonly IReadOnlyList<Slot> Pages = All.Where(s => s != Slot.Map).ToArray();

    public static string Name(Slot s) => s == Slot.Map ? PageKeys.MapName : PageKeys.Name((Page)s);
    /// <summary>文字のキー。生産（Tab）と地図（キー無し）は '\0'。</summary>
    public static char Key(Slot s) => s == Slot.Map ? '\0' : PageKeys.Key((Page)s);
    public static string ButtonText(Slot s) => PageKeys.ButtonText((Page)s);

    /// <summary>文字のキーで引く。文字のキーを持つページでなければ null（空きのキー・生産の Tab・地図は何にも割り当てない）。</summary>
    public static Slot? OfKey(char key) => PageKeys.PageOfKey(key) is { } p ? (Slot)(int)p : null;

    public static Page? PageOf(Slot s) => s == Slot.Map ? null : (Page)s;
}

/// <summary>画面の配置＝いま中央に出しているもの（7 ページか地図の 1 つ）・左の列の開閉・ページごとの開け閉めした欄（設計 §2-6「畳んだ欄」）。Avalonia に依らない。
/// 作者の指示 2026-11-04：分割（同時に複数ページ）とピン留めは廃止した＝中央は常に 1 つ。起動は地図（背景）から始まり、
/// Tab で生産と地図を切り替え、開いているページのキーをもう一度押すか Esc で地図へ戻る。</summary>
public sealed class LayoutModel
{
    public const double LeftWidth = 260;   // 左の列の幅（開いているとき）

    /// <summary>いま中央に出しているもの。初めは地図。</summary>
    public Slot Current { get; private set; } = Slot.Map;
    public bool LeftOpen { get; private set; } = true;

    // ページごとの、プレイヤーが見出しを押して開け閉めした欄（設計 §2-6「畳んだ欄」）。鍵＝ページの名前（<see cref="Slot"/> の名前）。
    private readonly Dictionary<string, (string[] Opened, string[] Closed)> _sections = new();

    /// <summary>そのページの、プレイヤーが開けた欄・閉じた欄の鍵。</summary>
    public (IReadOnlyList<string> Opened, IReadOnlyList<string> Closed) SectionState(string page)
        => _sections.TryGetValue(page, out var s) ? (s.Opened, s.Closed) : ([], []);

    /// <summary>そのページの開け閉めを置く（見出しを押すたび）。変わらなければ何もしない。</summary>
    public void SetSectionState(string page, IEnumerable<string> opened, IEnumerable<string> closed)
    {
        var o = opened.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var c = closed.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();
        if (_sections.TryGetValue(page, out var cur) && cur.Opened.SequenceEqual(o) && cur.Closed.SequenceEqual(c)) return;
        if (o.Length == 0 && c.Length == 0) _sections.Remove(page); else _sections[page] = (o, c);
        Raise();
    }

    /// <summary>配置が変わった（中央に出すもの・左の列の開閉・欄の開け閉めのどれでも）。</summary>
    public event Action? Changed;

    public bool IsShown(Slot s) => s == Current;
    public IEnumerable<Slot> Shown() { yield return Current; }

    private void Raise() => Changed?.Invoke();

    /// <summary><paramref name="slot"/> を中央に出す（すでに出ていれば何もしない）。問題の一覧の飛び先・戦争の自動表示が使う。</summary>
    public void Open(Slot slot)
    {
        if (slot == Current) return;
        Current = slot;
        Raise();
    }

    /// <summary>ページの押し釦とキー：出ていなければ出す・もう出ていれば地図へ戻る。</summary>
    public void Toggle(Slot slot) => Open(Current == slot ? Slot.Map : slot);

    /// <summary>Tab：生産と地図の切り替え。生産以外（地図か他のページ）からは生産へ・生産からは地図へ。</summary>
    public void ToggleProductionMap() => Open(Current == Slot.Production ? Slot.Map : Slot.Production);

    /// <summary>地図へ戻る。もう地図なら何もしない（戻ったときだけ真）。Esc が使う。</summary>
    public bool LeaveToMap()
    {
        if (Current == Slot.Map) return false;
        Open(Slot.Map);
        return true;
    }

    public void SetLeftOpen(bool left)
    {
        if (left == LeftOpen) return;
        LeftOpen = left;
        Raise();
    }

    /// <summary>初めの配置へ戻す（撮影が毎回同じ所から始める）。</summary>
    public void Reset()
    {
        Current = Slot.Map; LeftOpen = true;
        _sections.Clear();
        Raise();
    }

    // ---- 保存（%LOCALAPPDATA%/PriceOfBread/配置.json）＝左の列の開閉と欄の開け閉めだけ（中央に出すものは起動のたびに地図）----

    private sealed class Dto
    {
        public bool LeftOpen { get; set; } = true;
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, SectionDto>? Sections { get; set; }
    }

    private sealed class SectionDto
    {
        public string[] Opened { get; set; } = [];
        public string[] Closed { get; set; } = [];
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, IncludeFields = false };

    public string ToJson() => JsonSerializer.Serialize(new Dto
    {
        LeftOpen = LeftOpen,
        Sections = _sections.Count == 0 ? null : _sections.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToDictionary(kv => kv.Key, kv => new SectionDto { Opened = kv.Value.Opened, Closed = kv.Value.Closed }),
    }, JsonOptions);

    /// <summary>JSON から配置を作る。読めない・不正な物は初めの配置（保存の壊れで画面が出なくなる道を作らない）。
    /// 分割の廃止前の保存（Shape・Seats などを持つ）も読める＝知らない項目は読み捨てる。</summary>
    public static LayoutModel FromJson(string? json)
    {
        var m = new LayoutModel();
        if (string.IsNullOrWhiteSpace(json)) return m;
        try
        {
            var d = JsonSerializer.Deserialize<Dto>(json, JsonOptions);
            if (d is null) return m;
            m.LeftOpen = d.LeftOpen;
            if (d.Sections is not null)
                foreach (var (page, s) in d.Sections)
                {
                    if (!Enum.TryParse<Slot>(page, out var slot) || !Enum.IsDefined(slot) || s is null) continue;   // 知らないページの記録は捨てる
                    var o = (s.Opened ?? []).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x, StringComparer.Ordinal).Take(32).ToArray();
                    var c = (s.Closed ?? []).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x, StringComparer.Ordinal).Take(32).ToArray();
                    if (o.Length > 0 || c.Length > 0) m._sections[page] = (o, c);
                }
            return m;
        }
        catch { return new LayoutModel(); }
    }
}

/// <summary>配置の保存先。既定は %LOCALAPPDATA%/PriceOfBread/配置.json（セーブと同じ場所）。撮影と計測は <see cref="PathOverride"/> で別の場所へ向ける＝作者の本物の配置を読まず・書かない。
/// 保存の失敗は黙って捨てる（配置が残らないだけで、ゲームは止めない）。</summary>
public static class LayoutStore
{
    public static string? PathOverride;
    public static string Path => PathOverride ?? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PriceOfBread", "配置.json");

    public static LayoutModel Load()
    {
        try { return File.Exists(Path) ? LayoutModel.FromJson(File.ReadAllText(Path)) : new LayoutModel(); }
        catch { return new LayoutModel(); }
    }

    public static void Save(LayoutModel m)
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            File.WriteAllText(Path, m.ToJson());
        }
        catch { /* 配置が残らないだけ */ }
    }
}
