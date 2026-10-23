using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.Layout;

/// <summary>分割の1枠に置けるもの＝7 ページと地図（設計 §2・§3-4）。宣言の順が <see cref="PageKeys"/> のキーの割り当ての順（ページの宣言順・地図は最後）。</summary>
public enum Slot { Production, Market, FiscalMoney, Population, PoliticsLaw, DiplomacyWar, Discovery, Map }

/// <summary>分割の形（設計 §2-3）。</summary>
public enum SplitShape { One, LeftRight, TopBottom, LeftAndRightStack, Four }

/// <summary>枠と区切りの置き場所（3×3 の格子の中）。枠は A→D の順。</summary>
public readonly record struct Cell(int Row, int Col, int RowSpan, int ColSpan);

/// <summary>枠の置き場所と区切り（境目）の置き場所。Avalonia に依らない＝番人から直接見る。区切りは 縦（列の境目）と 横（行の境目）が各 0〜1 本。</summary>
public sealed record ShapeLayout(Cell[] Panes, Cell? VerticalSplitter, Cell? HorizontalSplitter);

/// <summary>枠に置くものの名前とキー。キーは本体の <see cref="PageKeys"/> が持つ（作者の指示 QWERASDFZXCV の順）。ここは引くだけ。</summary>
public static class SlotInfo
{
    public static readonly IReadOnlyList<Slot> All = Enum.GetValues<Slot>();

    public static string Name(Slot s) => s == Slot.Map ? PageKeys.MapName : PageKeys.Name((Page)s);
    public static char Key(Slot s) => s == Slot.Map ? PageKeys.MapKey : PageKeys.Key((Page)s);
    public static string ButtonText(Slot s) => $"{Key(s)}:{Name(s)}";

    /// <summary>キーで引く。ページのキーでも地図のキーでもなければ null（空きのキーは何にも割り当てない）。</summary>
    public static Slot? OfKey(char key)
    {
        char k = char.ToUpperInvariant(key);
        foreach (var s in All) if (Key(s) == k) return s;
        return null;
    }

    public static Page? PageOf(Slot s) => s == Slot.Map ? null : (Page)s;

    public static string ShapeName(SplitShape s) => s switch
    {
        SplitShape.One => "1枚",
        SplitShape.LeftRight => "左右に2つ",
        SplitShape.TopBottom => "上下に2つ",
        SplitShape.LeftAndRightStack => "左に1つ右に2つ",
        SplitShape.Four => "4つ",
        _ => "",
    };

    public static int PaneCount(SplitShape s) => s switch { SplitShape.One => 1, SplitShape.LeftRight or SplitShape.TopBottom => 2, SplitShape.LeftAndRightStack => 3, _ => 4 };

    /// <summary>3×3 の格子（列 0・境目 1・列 2／行 0・境目 1・行 2）の中の置き場所。</summary>
    public static ShapeLayout Place(SplitShape s) => s switch
    {
        SplitShape.One => new([new(0, 0, 3, 3)], null, null),
        SplitShape.LeftRight => new([new(0, 0, 3, 1), new(0, 2, 3, 1)], new(0, 1, 3, 1), null),
        SplitShape.TopBottom => new([new(0, 0, 1, 3), new(2, 0, 1, 3)], null, new(1, 0, 1, 3)),
        SplitShape.LeftAndRightStack => new([new(0, 0, 3, 1), new(0, 2, 1, 1), new(2, 2, 1, 1)], new(0, 1, 3, 1), new(1, 2, 1, 1)),
        _ => new([new(0, 0, 1, 1), new(0, 2, 1, 1), new(2, 0, 1, 1), new(2, 2, 1, 1)], new(0, 1, 3, 1), new(1, 0, 1, 3)),
    };
}

/// <summary>画面の配置＝分割の形・各枠に置くもの・選んでいる枠・区切りの位置・左右の列の開閉（設計 §2）。Avalonia に依らない。
/// <list type="bullet">
/// <item>各枠に置くものは 4 つの席で持ち、**4 つは常に互いに違う**（同じページを2枠に置かない＝ページの部品は盤面に1つだけ）。形が小さい間は後ろの席が見えないだけで、中身は残る。</item>
/// <item>開く操作は「すでに別の席にあれば入れ替え・どの席にも無ければ置き換え」＝4 つが違う性質を保つ。</item>
/// <item>見えているもの＝先頭から <see cref="PaneCount"/> 席。</item>
/// </list></summary>
public sealed class LayoutModel
{
    public const double DefaultMainRatio = 0.62;   // 左右に分ける形の左の枠の割合（既定は生産を広め）
    public const double DefaultSubRatio = 0.5;
    public const double LeftWidth = 260, RightWidth = 300;   // 左右の列の幅（開いているとき）

    private readonly Slot[] _seats = [Slot.Production, Slot.Market, Slot.FiscalMoney, Slot.Population];

    public SplitShape Shape { get; private set; } = SplitShape.LeftRight;
    public int Selected { get; private set; }
    /// <summary>縦の境目（左右に分ける）の、左の枠の割合。</summary>
    public double MainRatio { get; private set; } = DefaultMainRatio;
    /// <summary>横の境目（上下に分ける）の、上の枠の割合。</summary>
    public double SubRatio { get; private set; } = DefaultSubRatio;
    public bool LeftOpen { get; private set; } = true;
    public bool RightOpen { get; private set; } = true;

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

    // ---- 全面（生産のキーを押したとき・作者の指示 2026-10-23「生産は大きくあってほしい」）----
    // 1 枚の枠に置き、左右の列を畳む。入る前の配置の写しを 1 つだけ持ち（これが全面の印）、出るときに戻す。保存するのは写しの方（全面のまま落ちても、次の起動は元の配置）。

    /// <summary>全面に入る前の配置（形・席・選択・区切り・左右の列）。</summary>
    private sealed record Before(SplitShape Shape, Slot[] Seats, int Selected, double MainRatio, double SubRatio, bool LeftOpen, bool RightOpen);
    private Before? _before;

    /// <summary>いま全面か（ページを 1 枚の全面に置き、左右の列を畳んでいる）。</summary>
    public bool IsFull => _before is not null;

    /// <summary>全面にしているページ（全面でなければ null）。</summary>
    public Slot? FullSlot => IsFull ? _seats[0] : null;

    /// <summary>全面の入り切り。全面でなければ入る（入る前の配置を写す）・全面なら前の配置へ戻る。</summary>
    public void ToggleFull(Slot slot)
    {
        if (IsFull) { LeaveFull(); return; }
        _before = new Before(Shape, [.. _seats], Selected, MainRatio, SubRatio, LeftOpen, RightOpen);
        int at = Array.IndexOf(_seats, slot);
        if (at > 0) (_seats[at], _seats[0]) = (_seats[0], _seats[at]);
        else if (at < 0) _seats[0] = slot;
        Shape = SplitShape.One; Selected = 0; LeftOpen = false; RightOpen = false;
        Raise();
    }

    /// <summary>全面から前の配置へ戻る。全面でなければ何もしない（戻ったときだけ真）。</summary>
    public bool LeaveFull()
    {
        if (_before is not { } b) return false;
        _before = null;
        for (int i = 0; i < _seats.Length; i++) _seats[i] = b.Seats[i];
        Shape = b.Shape; Selected = b.Selected; MainRatio = b.MainRatio; SubRatio = b.SubRatio; LeftOpen = b.LeftOpen; RightOpen = b.RightOpen;
        Raise();
        return true;
    }

    /// <summary>配置が変わった（形・席・選択・区切り・列の開閉のどれでも）。</summary>
    public event Action? Changed;

    public int PaneCount => SlotInfo.PaneCount(Shape);
    public Slot SlotAt(int pane) => _seats[pane];
    public IReadOnlyList<Slot> Seats => _seats;
    public IEnumerable<Slot> Shown() => _seats.Take(PaneCount);
    public bool IsShown(Slot s) => PaneOf(s) >= 0;
    /// <summary>見えている枠のうち、そのものが入っている枠の番号。見えていなければ -1。</summary>
    public int PaneOf(Slot s) { for (int i = 0; i < PaneCount; i++) if (_seats[i] == s) return i; return -1; }

    private void Raise() => Changed?.Invoke();

    /// <summary>枠 <paramref name="pane"/> に <paramref name="slot"/> を開く。別の席にあれば入れ替え、どの席にも無ければ置き換える。開いた枠が選んでいる枠になる。</summary>
    public void Open(Slot slot, int pane)
    {
        if (LeaveFull()) pane = Selected;   // 全面の間に別のページを開く＝前の配置へ戻り、その配置で選んでいた枠へ開く（全面の枠の番号は使わない）
        pane = Math.Clamp(pane, 0, PaneCount - 1);
        int at = Array.IndexOf(_seats, slot);
        if (at >= 0 && at != pane) (_seats[at], _seats[pane]) = (_seats[pane], _seats[at]);
        else if (at < 0) _seats[pane] = slot;
        Selected = pane;
        Raise();
    }

    /// <summary>選んでいる枠へ開く（ヘッダーの押し釦とキー）。</summary>
    public void OpenInSelected(Slot slot) => Open(slot, Selected);

    /// <summary>次の枠へ開く（Shift を足したキー）。枠が1つなら選んでいる枠のまま。</summary>
    public void OpenInNext(Slot slot) => Open(slot, (Selected + 1) % PaneCount);

    public void Select(int pane)
    {
        pane = Math.Clamp(pane, 0, PaneCount - 1);
        if (pane == Selected) return;
        Selected = pane;
        Raise();
    }

    public void SetShape(SplitShape shape)
    {
        if (shape == Shape) return;
        LeaveFull();   // 分割の形を選び直す＝前の配置へ戻ってから変える
        if (shape == Shape) return;
        Shape = shape;
        Selected = Math.Min(Selected, PaneCount - 1);
        Raise();
    }

    public void SetRatios(double main, double sub)
    {
        double m = Clamp(main, DefaultMainRatio), s = Clamp(sub, DefaultSubRatio);
        if (Math.Abs(m - MainRatio) < 1e-9 && Math.Abs(s - SubRatio) < 1e-9) return;
        MainRatio = m; SubRatio = s;
        Raise();
    }

    public void SetColumns(bool left, bool right)
    {
        if (left == LeftOpen && right == RightOpen) return;
        _before = null;   // 列を自分で開け閉めした＝全面の印を外す（今の配置のまま）
        LeftOpen = left; RightOpen = right;
        Raise();
    }

    // 区切りを端までは動かさない（枠が消えて取り戻せなくなる）。非有限は既定へ。
    private static double Clamp(double v, double fallback) => double.IsFinite(v) ? Math.Clamp(v, 0.15, 0.85) : fallback;

    /// <summary>初めの配置へ戻す（撮影が毎回同じ所から始める）。</summary>
    public void Reset()
    {
        var d = new LayoutModel();
        for (int i = 0; i < _seats.Length; i++) _seats[i] = d._seats[i];
        Shape = d.Shape; Selected = d.Selected; MainRatio = d.MainRatio; SubRatio = d.SubRatio; LeftOpen = d.LeftOpen; RightOpen = d.RightOpen;
        _sections.Clear();
        _before = null;
        Raise();
    }

    // ---- 保存（%LOCALAPPDATA%/PriceOfBread/配置.json）----

    private sealed class Dto
    {
        public string Shape { get; set; } = "";
        public string[] Seats { get; set; } = [];
        public int Selected { get; set; }
        public double MainRatio { get; set; } = DefaultMainRatio;
        public double SubRatio { get; set; } = DefaultSubRatio;
        public bool LeftOpen { get; set; } = true;
        public bool RightOpen { get; set; } = true;
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
        // 全面の間は入る前の配置を書く（全面は一時の見え方＝次の起動は元の配置から）
        Shape = (_before?.Shape ?? Shape).ToString(), Seats = (_before?.Seats ?? _seats).Select(s => s.ToString()).ToArray(), Selected = _before?.Selected ?? Selected,
        MainRatio = _before?.MainRatio ?? MainRatio, SubRatio = _before?.SubRatio ?? SubRatio, LeftOpen = _before?.LeftOpen ?? LeftOpen, RightOpen = _before?.RightOpen ?? RightOpen,
        Sections = _sections.Count == 0 ? null : _sections.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToDictionary(kv => kv.Key, kv => new SectionDto { Opened = kv.Value.Opened, Closed = kv.Value.Closed }),
    }, JsonOptions);

    /// <summary>JSON から配置を作る。読めない・不正な物は初めの配置（保存の壊れで画面が出なくなる道を作らない）。</summary>
    public static LayoutModel FromJson(string? json)
    {
        var m = new LayoutModel();
        if (string.IsNullOrWhiteSpace(json)) return m;
        try
        {
            var d = JsonSerializer.Deserialize<Dto>(json, JsonOptions);
            if (d is null) return m;
            if (!Enum.TryParse<SplitShape>(d.Shape, out var shape) || !Enum.IsDefined(shape)) return m;
            var seats = new List<Slot>();
            foreach (var name in d.Seats ?? [])
                if (Enum.TryParse<Slot>(name, out var s) && Enum.IsDefined(s) && !seats.Contains(s)) seats.Add(s);
            if (seats.Count != 4) return m;   // 4 席が揃って違う物でなければ捨てる
            for (int i = 0; i < 4; i++) m._seats[i] = seats[i];
            m.Shape = shape;
            m.Selected = Math.Clamp(d.Selected, 0, m.PaneCount - 1);
            m.MainRatio = Clamp(d.MainRatio, DefaultMainRatio);
            m.SubRatio = Clamp(d.SubRatio, DefaultSubRatio);
            m.LeftOpen = d.LeftOpen; m.RightOpen = d.RightOpen;
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
