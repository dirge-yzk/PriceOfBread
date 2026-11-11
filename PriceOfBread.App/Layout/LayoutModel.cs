using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.Layout;

/// <summary>画面に出せるもの＝7 ページと地図と州の窓。宣言の順が <see cref="PageKeys"/> のページの順（地図・州は後ろ＝キーを持たない）。
/// 地図と生産は背景・それ以外の 6 ページと州は浮かぶ窓（州＝地図で指している州の情報。釦もキーも持たず、最初から開いている）。</summary>
public enum Slot { Production, Market, FiscalMoney, Population, PoliticsLaw, DiplomacyWar, Discovery, Map, Province }

/// <summary>画面に出せるものの名前とキー。キーは本体の <see cref="PageKeys"/> が持つ。ここは引くだけ。</summary>
public static class SlotInfo
{
    public static readonly IReadOnlyList<Slot> All = Enum.GetValues<Slot>();

    /// <summary>ヘッダーの押し釦に並ぶもの（7 ページ。地図は釦を持たない）。</summary>
    public static readonly IReadOnlyList<Slot> Pages = All.Where(s => s != Slot.Map && s != Slot.Province).ToArray();

    public static string Name(Slot s) => s == Slot.Map ? PageKeys.MapName : s == Slot.Province ? "州" : PageKeys.Name((Page)s);
    /// <summary>文字のキー。生産（Tab）と地図（キー無し）は '\0'。</summary>
    public static char Key(Slot s) => s == Slot.Map ? '\0' : PageKeys.Key((Page)s);
    public static string ButtonText(Slot s) => PageKeys.ButtonText((Page)s);

    /// <summary>文字のキーで引く。文字のキーを持つページでなければ null（空きのキー・生産の Tab・地図は何にも割り当てない）。</summary>
    public static Slot? OfKey(char key) => PageKeys.PageOfKey(key) is { } p ? (Slot)(int)p : null;

    public static Page? PageOf(Slot s) => s is Slot.Map or Slot.Province ? null : (Page)s;
}

/// <summary>浮かぶ窓の位置と大きさ（窓を置く領域の左上を原点とする画素）。</summary>
public readonly record struct WindowRect(double X, double Y, double W, double H);

/// <summary>画面の配置＝背景（地図か生産）・浮かぶ窓（開いている順＝重なりの順）・各窓の位置と大きさ・左の列の開閉・ページごとの開け閉めした欄（設計 §2-6「畳んだ欄」）。Avalonia に依らない。
/// 作者の指示 2026-11-04：分割とピン留めは廃止。2026-11-11：背景は地図と生産の 2 枚の切り替え（Tab）・それ以外のページは浮かぶ窓＝
/// 開閉・動かす・大きさを変える・複数を同時に開く・押した窓が前に出る。窓の位置と大きさは配置の保存（アプリの設定）に覚える＝セーブには入れない。
/// 起動は地図の背景で窓は 1 つも開いていない。</summary>
public sealed class LayoutModel
{
    public const double LeftWidth = 260;   // 左の列の幅（開いているとき）
    public const double MinWindowWidth = 420, MinWindowHeight = 280;

    /// <summary>背景＝地図か生産（この 2 つだけ）。初めは地図。</summary>
    public Slot Background { get; private set; } = Slot.Map;
    public bool LeftOpen { get; private set; } = true;

    private readonly List<Slot> _open = new();                       // 開いている窓。後ろほど前（重なりの順）
    private readonly Dictionary<Slot, WindowRect> _rects = new();    // 覚えている位置と大きさ（開閉に依らず残る）

    /// <summary>開いている窓（重なりの順・最後が一番前）。</summary>
    public IReadOnlyList<Slot> OpenWindows => _open;
    /// <summary>一番前の窓（無ければ null）。</summary>
    public Slot? Front => _open.Count > 0 ? _open[^1] : null;
    /// <summary>窓として開くものか（背景の地図・生産以外）。</summary>
    public static bool IsWindow(Slot s) => s != Slot.Map && s != Slot.Production;

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

    /// <summary>配置が変わった（背景・窓の開閉と重なりと位置・左の列の開閉・欄の開け閉めのどれでも）。</summary>
    public event Action? Changed;

    /// <summary>見えているか＝背景であるか、窓として開いている。</summary>
    public bool IsShown(Slot s) => s == Background || _open.Contains(s);
    /// <summary>見えているもの（背景→開いている窓の順）。</summary>
    public IEnumerable<Slot> Shown() { yield return Background; foreach (var w in _open) yield return w; }

    private void Raise() => Changed?.Invoke();

    /// <summary><paramref name="slot"/> を出す。地図・生産は背景にする・それ以外は窓を開いて一番前に出す（すでに開いていれば前へ）。
    /// 問題の一覧の飛び先・戦争の自動表示が使う。</summary>
    public void Open(Slot slot)
    {
        if (!IsWindow(slot))
        {
            if (slot == Background) return;
            Background = slot;
            Raise();
            return;
        }
        int at = _open.IndexOf(slot);
        if (at >= 0 && at == _open.Count - 1) return;   // すでに一番前
        if (at >= 0) _open.RemoveAt(at);
        _open.Add(slot);
        Raise();
    }

    /// <summary>窓を閉じる（開いていなければ何もしない）。位置と大きさは覚えたまま。</summary>
    public void Close(Slot slot)
    {
        if (_open.Remove(slot)) Raise();
    }

    /// <summary>ページのキーと押し釦：窓は開いていれば閉じ・閉じていれば開く。生産と地図は背景にする。</summary>
    public void Toggle(Slot slot)
    {
        if (IsWindow(slot) && _open.Contains(slot)) Close(slot); else Open(slot);
    }

    /// <summary>窓を一番前に出す（押された窓。すでに一番前なら何もしない）。</summary>
    public void BringToFront(Slot slot) { if (_open.Contains(slot)) Open(slot); }

    /// <summary>Tab：背景の地図と生産の切り替え。</summary>
    public void ToggleProductionMap() => Open(Background == Slot.Production ? Slot.Map : Slot.Production);

    /// <summary>Esc：一番前の窓を閉じる→無ければ生産の背景から地図へ。どちらもしなければ false（メニューの仕事）。</summary>
    public bool Escape()
    {
        if (Front is { } f) { Close(f); return true; }
        if (Background == Slot.Production) { Open(Slot.Map); return true; }
        return false;
    }

    public void SetLeftOpen(bool left)
    {
        if (left == LeftOpen) return;
        LeftOpen = left;
        Raise();
    }

    // ---- 窓の位置と大きさ ----

    /// <summary>覚えている位置と大きさ（無ければ null）。</summary>
    public WindowRect? RectOf(Slot slot) => _rects.TryGetValue(slot, out var r) ? r : null;

    /// <summary>位置と大きさを覚える（ドラッグや大きさの変更が終わったとき）。非有限の値は捨てる・最小の大きさへ丸める。変わらなければ何もしない。</summary>
    public void SetRect(Slot slot, WindowRect rect)
    {
        if (!IsWindow(slot) || !(double.IsFinite(rect.X) && double.IsFinite(rect.Y) && double.IsFinite(rect.W) && double.IsFinite(rect.H))) return;
        rect = new WindowRect(rect.X, rect.Y, Math.Max(MinWindowWidth, rect.W), Math.Max(MinWindowHeight, rect.H));
        if (_rects.TryGetValue(slot, out var cur) && cur == rect) return;
        _rects[slot] = rect;
        Raise();
    }

    /// <summary>まだ覚えていない窓の初めの位置と大きさ＝領域の大きさに応じ、窓ごとに少しずつずらした起点にする。</summary>
    public static WindowRect DefaultRect(Slot slot, double hostW, double hostH)
    {
        if (slot == Slot.Province) return Fit(new WindowRect(12, 12, 460, Math.Min(420, hostH - 24)), hostW, hostH);   // 州の窓＝幅は固定（情報の量で変わらない）・左上
        double w = Math.Clamp(hostW * 0.55, MinWindowWidth, 900), h = Math.Clamp(hostH * 0.72, MinWindowHeight, 760);
        int step = (int)slot % 6;
        return Fit(new WindowRect(24 + 36 * step, 24 + 32 * step, w, h), hostW, hostH);
    }

    /// <summary>領域の中に収める（大きさは領域まで・位置は領域の内側）。表示のときだけ使う＝覚えている値は書き換えない。</summary>
    public static WindowRect Fit(WindowRect r, double hostW, double hostH)
    {
        double w = Math.Min(r.W, Math.Max(hostW, 1)), h = Math.Min(r.H, Math.Max(hostH, 1));
        double x = Math.Clamp(r.X, 0, Math.Max(0, hostW - w)), y = Math.Clamp(r.Y, 0, Math.Max(0, hostH - h));
        return new WindowRect(x, y, w, h);
    }

    /// <summary>初めの配置へ戻す（撮影が毎回同じ所から始める）。</summary>
    public void Reset()
    {
        Background = Slot.Map; LeftOpen = true;
        _open.Clear(); _rects.Clear();
        _sections.Clear();
        Raise();
    }

    // ---- 保存（%LOCALAPPDATA%/PriceOfBread/配置.json）＝左の列の開閉・窓の位置と大きさ・欄の開け閉めだけ（背景と開く窓は起動のたびに地図・窓なし）----

    private sealed class Dto
    {
        public bool LeftOpen { get; set; } = true;
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, double[]>? Windows { get; set; }   // 窓の名前 → [X, Y, W, H]
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
        Windows = _rects.Count == 0 ? null : _rects.OrderBy(kv => (int)kv.Key).ToDictionary(kv => kv.Key.ToString(), kv => new[] { kv.Value.X, kv.Value.Y, kv.Value.W, kv.Value.H }),
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
            if (d.Windows is not null)
                foreach (var (name, v) in d.Windows)
                {
                    if (!Enum.TryParse<Slot>(name, out var slot) || !Enum.IsDefined(slot) || !IsWindow(slot) || v is not { Length: 4 }) continue;   // 知らない窓・壊れた記録は捨てる
                    if (!v.All(double.IsFinite)) continue;
                    m._rects[slot] = new WindowRect(v[0], v[1], Math.Max(MinWindowWidth, v[2]), Math.Max(MinWindowHeight, v[3]));
                }
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
