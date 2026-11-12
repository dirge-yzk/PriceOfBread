using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.Controls;

/// <summary>共通の表（設計 §6-7）。本体の目録の列から、見出しと行を 1 つの定義で作る（見出しと行の列の定義を 2 か所に書かない）。
/// ・行は作り直さず、鍵で値だけ流す（SyncRows の規律＝毎月の更新でボタンとツールチップを壊さない）。
/// ・見出しは止めて、中身だけを流す（<see cref="InnerScroll"/> が真のとき。ページの外側は縦に流れない／長い一覧は欄の中だけで流れる）。
/// ・見出しを押すと並べ替え（同じ列をもう一度で降順）。群（<see cref="RowView.Group"/>）があれば群の見出しで区切る。
/// ・<see cref="Filterable"/> が真なら絞り込みの文字の欄を出す。
/// ・列の幅は単位から決めた最小幅（<c>MinWidth</c>）。固定の <c>Width</c> は値を黙って切るので使わない。見出しと行は列の大きさを共有する。
/// ・Avalonia の描画の部品だけ。XAML を持たない＝結線は <see cref="SnapshotProperty"/> の 1 本（呼び手の XAML が x:DataType 付きで束縛する）。</summary>
/// <summary>凡例の 1 項（色の丸と名）。</summary>
public readonly record struct LegendItem(string Name, IBrush Brush);

public sealed class TableView : UserControl
{
    public static readonly StyledProperty<TableSnapshot?> SnapshotProperty = AvaloniaProperty.Register<TableView, TableSnapshot?>(nameof(Snapshot));
    public static readonly StyledProperty<bool> FilterableProperty = AvaloniaProperty.Register<TableView, bool>(nameof(Filterable));
    public static readonly StyledProperty<bool> InnerScrollProperty = AvaloniaProperty.Register<TableView, bool>(nameof(InnerScroll));
    public static readonly StyledProperty<string?> SelectedKeyProperty = AvaloniaProperty.Register<TableView, string?>(nameof(SelectedKey));

    public TableSnapshot? Snapshot { get => GetValue(SnapshotProperty); set => SetValue(SnapshotProperty, value); }
    /// <summary>絞り込みの文字の欄を出すか。</summary>
    public bool Filterable { get => GetValue(FilterableProperty); set => SetValue(FilterableProperty, value); }
    /// <summary>中身（行）だけを欄の中で流すか。偽なら行の数だけ高くなる（外側の流れに任せる）。</summary>
    public bool InnerScroll { get => GetValue(InnerScrollProperty); set => SetValue(InnerScrollProperty, value); }

    /// <summary>選んでいる行の鍵（行の背景で示す）。行を選ぶ表（生産ページの財の表）が使う。null なら選ばない。</summary>
    public string? SelectedKey { get => GetValue(SelectedKeyProperty); set => SetValue(SelectedKeyProperty, value); }

    /// <summary>行の余白（押し釦の外）を押した。引数は行の鍵（国の一覧の行を押すと地図がその国へ動く、など）。</summary>
    public event Action<string>? RowActivated;

    /// <summary>確かめの窓を出す口。確かめの文（<see cref="VerbCell.ConfirmText"/>）を持つ動詞は、押したとき直接は呼ばずにこの口へ渡す（設計 §6-3「押す前に確かめの窓を出す物は出す」）。
    /// 表ごとの口が無ければ <see cref="DefaultConfirm"/>。どちらも無ければ何もしない（確かめ無しの発動を許さない側へ倒す）。</summary>
    public Action<VerbCell>? ConfirmVerb { get; set; }

    /// <summary>表ごとの口が無いときの既定の口。盤面の画面（GameView）が、ゲームの確かめの窓へ繋ぐ。</summary>
    public static Action<VerbCell>? DefaultConfirm { get; set; }

    /// <summary>動詞の押下の振り分け（Avalonia に依らない＝番人から直接呼べる）。確かめの文があれば <paramref name="confirm"/> へ渡して終わり・無ければそのまま発動。
    /// 確かめの文があるのに口が無いときは発動しない（BootLog に残す）。</summary>
    public static void DispatchVerb(VerbCell vb, Action<VerbCell>? confirm)
    {
        if (string.IsNullOrEmpty(vb.ConfirmText)) { vb.Invoke(); return; }
        if (confirm is null) { BootLog.Write($"動詞「{vb.Name}」は確かめの文を持つが確かめの口が無い＝発動しない"); return; }
        confirm(vb);
    }

    /// <summary>段の列の最大の幅（これを超える段の押し釦は欄の中で折り返す）。選べない段の下の短い理由が横に並ぶ分まで広げた（B42・2026-11-12）。</summary>
    public const double StageMaxWidth = 440;

    /// <summary>文字の 1 つのセルの最大の幅（これを超える長い文字は末尾を省略記号にして全文を吹き出しに出す）。</summary>
    public const double MaxTextWidth = 260;

    /// <summary>見出しの列の左端と、値の列の左端が揃っているかの検査（撮影の判定）。ずれた列の数と測った列の数。
    /// 行が無い・まだ測られていない表は測らない（<c>Measured</c>＝0）。測り方は <see cref="MeasureAlignment"/> の 1 本。</summary>
    public (int Gaps, int Measured) AlignmentGaps()
    {
        var m = MeasureAlignment();
        return m is { } a ? (a.Gaps, a.Columns) : (0, 0);
    }

    /// <summary>中身の高さの見積り（絞り込みの欄・見出し・行・群の見出し・横の流れの目盛りの分）。欄の高さを中身に合わせる上限に使う（A10）。
    /// 押し釦の行は高いので行の高さを大きく見積もる＝足りなくて最後の行が半分に切れることのないよう、やや多めに取る。</summary>
    public double EstimatedHeight()
    {
        var snap = Snapshot ?? TableSnapshot.Empty;
        bool tall = snap.Columns.Any(c => c.Kind is ColumnKind.Stage or ColumnKind.Verb or ColumnKind.Share);
        double row = tall ? 34 : 20;
        int groups = snap.Rows.Select(r => r.Group ?? "").Distinct().Count(g => g.Length > 0);
        return ChromeHeight() + snap.Rows.Count * row + groups * 34;
    }

    /// <summary>行の外の高さ（絞り込みの欄・見出し・横の流れの目盛り・余白）。</summary>
    public double ChromeHeight() => (Filterable ? 28 : 0) + (_legendCount > 0 ? 20 : 0) + 24 + (ScrollX ? 14 : 0) + 6;

    /// <summary>表の行の数（撮影の検査用）。</summary>
    public int RowCount => _rows.Count;

    /// <summary>表の上に出す色の凡例（呼び手が渡す・例：国名の前の丸の政体の色）。割合の列（半円）がある表は、その区分の色と名を自動で出す（B34・B49）。</summary>
    public IReadOnlyList<LegendItem>? Legend { get => _legend; set { _legend = value; RefreshLegend(); } }
    private IReadOnlyList<LegendItem>? _legend;
    private readonly WrapPanel _legendPanel = new() { Orientation = Orientation.Horizontal, IsVisible = false, Margin = new Thickness(0, 0, 0, 2) };
    private int _legendCount;

    /// <summary>凡例の中身を作り直す。呼び手の凡例が無ければ、割合の列の最初の行の区分（名と半円の色）。</summary>
    private void RefreshLegend()
    {
        IReadOnlyList<LegendItem>? items = _legend;
        var snap = Snapshot ?? TableSnapshot.Empty;
        if (items is null)
        {
            int shareCol = -1;
            for (int i = 0; i < snap.Columns.Count; i++) if (snap.Columns[i].Kind == ColumnKind.Share) { shareCol = i; break; }
            if (shareCol >= 0)
                foreach (var r in snap.Rows)
                    if (shareCol < r.Cells.Count && r.Cells[shareCol].Extra is ShareCell sc && sc.Parts.Count > 0)
                    {
                        items = sc.Parts.Select((p, i) => new LegendItem(p.Name, PaletteOf(sc)[i % PaletteOf(sc).Count])).ToList();
                        break;
                    }
        }
        _legendPanel.Children.Clear();
        _legendCount = items?.Count ?? 0;
        if (items is not null)
            foreach (var it in items)
                _legendPanel.Children.Add(new StackPanel
                {
                    Orientation = Orientation.Horizontal, Spacing = 3, Margin = new Thickness(0, 0, 10, 0),
                    Children =
                    {
                        new Avalonia.Controls.Shapes.Ellipse { Width = 10, Height = 10, Fill = it.Brush, VerticalAlignment = VerticalAlignment.Center },
                        new TextBlock { Text = it.Name, FontSize = Small - 1, Foreground = Brush("InkSoftBrush", Brushes.Gray), VerticalAlignment = VerticalAlignment.Center },
                    },
                });
        _legendPanel.IsVisible = _legendCount > 0;
    }

    private readonly StackPanel _top = new() { Orientation = Orientation.Vertical };
    private readonly Grid _root = new() { RowDefinitions = new RowDefinitions("Auto,Auto,*") };
    private readonly TextBox _filterBox = new() { Watermark = "絞り込み", MinHeight = 22, Margin = new Thickness(0, 0, 0, 2), IsVisible = false };
    private readonly Grid _header = new();
    private readonly StackPanel _body = new() { Spacing = 0 };
    private readonly RowSnapBorder _bodyHost = new() { ClipToBounds = true };   // 内側で流れる表の行が見出しに重ならない（流した行は見出しの下で切る）
    private readonly Dictionary<string, RowPresenter> _rows = new();
    private readonly Dictionary<string, Control> _groups = new();
    private string _columnSig = "";
    private int _sortColumn = -1;
    private bool _sortDescending;
    private string _filter = "";
    private TextBlock[] _headerCells = [];
    private ColumnKind[] _kinds = [];

    static TableView()
    {
        SnapshotProperty.Changed.AddClassHandler<TableView>((v, _) => v.Rebuild());
        FilterableProperty.Changed.AddClassHandler<TableView>((v, _) => { v._filterBox.IsVisible = v.Filterable; v.Rebuild(); });
        InnerScrollProperty.Changed.AddClassHandler<TableView>((v, _) => v.AttachBody());
        SelectedKeyProperty.Changed.AddClassHandler<TableView>((v, _) => v.ApplySelection());
        ScrollXProperty.Changed.AddClassHandler<TableView>((v, _) => v.ApplyScrollX());
    }

    /// <summary>選んでいる行の背景を付け直す（行は作り直さない）。</summary>
    private void ApplySelection()
    {
        foreach (var kv in _rows) kv.Value.SetSelected(SelectedKey is not null && kv.Key == SelectedKey);
    }

    /// <summary>行へ飛ぶ（問題の一覧から）。行を見える所へ送り、目印の背景を付ける。行が無ければ false（表がまだ読まれていない・消えた行）。</summary>
    public bool FocusRow(string rowKey)
    {
        if (!_rows.TryGetValue(rowKey, out var rp)) return false;
        FocusedKey = rowKey;
        foreach (var kv in _rows) kv.Value.SetFocused(kv.Key == rowKey);
        rp.Root.BringIntoView();
        return true;
    }

    /// <summary>最後に飛んできた行の鍵（撮影の検査が飛び先の行に着いたかを読む）。</summary>
    public string? FocusedKey { get; private set; }

    public TableView()
    {
        Grid.SetIsSharedSizeScope(_root, true);   // 見出しと行が同じ列の大きさを共有する
        _top.Children.Add(_legendPanel); _top.Children.Add(_filterBox);   // 行 0＝凡例と絞り込み（行の番号は変えない＝見出しは 1・行は 2）
        _bodyHost.RowControls = () => _body.Children;
        Grid.SetRow(_top, 0); Grid.SetRow(_header, 1); Grid.SetRow(_bodyHost, 2);
        _root.Children.Add(_top); _root.Children.Add(_header); _root.Children.Add(_bodyHost);
        _filterBox.TextChanged += (_, _) => { _filter = _filterBox.Text ?? ""; Rebuild(); };
        Content = _root;
        AttachBody();
        AttachedToVisualTree += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(ClearJump, Avalonia.Threading.DispatcherPriority.Loaded);   // 付いた直後も（流れの部品が前回の位置を持ち越すことがある）
        DetachedFromVisualTree += (_, _) => ClearJump();   // 画面へ付いた直後と外れたとき、行へ飛んだ状態（目印と流れの位置）を捨てる＝使い回すページを開き直したとき前回の飛び先が残らない
    }

    /// <summary>行へ飛んだ状態を捨てる：目印の背景を消し、行の流れを先頭へ戻す。</summary>
    private void ClearJump()
    {
        if (FocusedKey is not null)
        {
            FocusedKey = null;
            foreach (var kv in _rows) kv.Value.SetFocused(false);
        }
        if (_bodyHost.Child is ScrollViewer sv && sv.Offset.Y != 0) sv.Offset = new Vector(sv.Offset.X, 0);
    }

    /// <summary>列が多くて欄の幅に収まらないことがある表は、横に流す（列を黙って切らない）。広い欄では最後の列の右まで広がる。既定は偽（段の押し釦のように折り返せる列を持つ表は、横に流さず折り返す）。
    /// ⚠横の流れ（Auto）は中身を無限の幅で測る＝折り返しの列は折り返さず 1 行の幅になる。長い文字は <see cref="MaxTextWidth"/> で省略記号にする。</summary>
    public static readonly StyledProperty<bool> ScrollXProperty = AvaloniaProperty.Register<TableView, bool>(nameof(ScrollX));
    public bool ScrollX { get => GetValue(ScrollXProperty); set => SetValue(ScrollXProperty, value); }
    private ScrollViewer? _hscroll;

    private void ApplyScrollX()
    {
        if (ScrollX)
        {
            _hscroll ??= new ScrollViewer { HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
            Content = null; _hscroll.Content = _root; Content = _hscroll;
        }
        else
        {
            if (_hscroll is not null) _hscroll.Content = null;
            Content = _root;
        }
    }

    private void AttachBody()
    {
        if (_bodyHost.Child is ScrollViewer old) old.Content = null;   // 行の入れ物は同時に 1 か所にしか置けない
        _bodyHost.Child = null;
        if (InnerScroll) _bodyHost.Child = new ScrollViewer { HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, Content = _body };
        else _bodyHost.Child = _body;
        // 縦の流れの目盛り（細い帯）が最後の列の値に重ならないよう、流れる表は見出しと行の右に同じ空きを置く（見出しと行の列を揃えたまま・余白は中身の Margin）
        var gap = new Thickness(0, 0, InnerScroll ? ScrollbarGap : 0, 0);
        _header.Margin = gap; _body.Margin = gap;
    }

    /// <summary>流れる表の右の空き（縦の目盛りの帯が値に重ならない幅）。</summary>
    private const double ScrollbarGap = 12;

    // ---- 資源（App.axaml のトークン。無ければ既定）----
    private static IBrush Brush(string key, IBrush fallback)
        => Application.Current is { } app && app.TryFindResource(key, app.ActualThemeVariant, out var v) && v is IBrush b ? b : fallback;
    /// <summary>半円の区分の色の並び。支配度の 4 区分は専用の色（App.axaml の RuleStateBrush ほか＝州の窓と同じ）・他は既定の並び。</summary>
    internal static IReadOnlyList<IBrush> PaletteOf(ShareCell sc) => sc.Palette == PopTables.RulePalette
        ? [Brush("RuleStateBrush", Brushes.SteelBlue), Brush("RuleLocalBrush", Brushes.Peru), Brush("RuleForeignBrush", Brushes.IndianRed), Brush("RuleUngovernedBrush", Brushes.Gray)]
        : RowPresenter.PiePalette;
    private static double Small => Application.Current is { } app && app.TryFindResource("FontSmall", app.ActualThemeVariant, out var v) && v is double d ? d : 12;

    /// <summary>表の中身が入れ替わった（<see cref="Section"/> が欄の並べを測り直す）。</summary>
    public event Action? SnapshotChanged;
    private int _lastRowCount = -1;

    private void Rebuild()
    {
        var snap = Snapshot ?? TableSnapshot.Empty;
        RefreshLegend();
        if (snap.Rows.Count != _lastRowCount) { _lastRowCount = snap.Rows.Count; SnapshotChanged?.Invoke(); }   // 行の数が変わった時だけ（毎月の値の更新では並べ直さない）
        string sig = string.Join("|", snap.Columns.Select(c => c.Key + ":" + c.Kind));
        if (sig != _columnSig)
        {
            _columnSig = sig;
            _sortColumn = -1;
            BuildHeader(snap);
            _rows.Clear(); _groups.Clear(); _body.Children.Clear();
        }
        var wanted = new List<Control>();
        var live = new HashSet<string>();
        string? lastGroup = null;
        foreach (var row in snap.View(_filter, _sortColumn, _sortDescending))
        {
            if (row.Group is { Length: > 0 } g && g != lastGroup)
            {
                if (!_groups.TryGetValue(g, out var gh)) _groups[g] = gh = MakeGroupHeader(g);
                wanted.Add(gh);
            }
            lastGroup = row.Group;
            if (!_rows.TryGetValue(row.Key, out var rp)) _rows[row.Key] = rp = new RowPresenter(this, snap.Columns);
            rp.Update(snap.Columns, row);
            live.Add(row.Key);
            wanted.Add(rp.Root);
        }
        foreach (var dead in _rows.Keys.Where(k => !live.Contains(k)).ToList()) _rows.Remove(dead);
        if (!_body.Children.SequenceEqual(wanted)) { _body.Children.Clear(); _body.Children.AddRange(wanted); }
        UpdateHeaderMarks(snap);
    }

    /// <summary>伸びる列。段の列があれば段の列（段の幅まで・押し釦は幅が足りなければ折り返す）。無ければ取らない＝名前の列を引き伸ばさず、
    /// 数の列を名前の直後に寄せて、余りは行の右に空ける（<see cref="AddSpacer"/>・A8）。</summary>
    private static int StarIndex(IReadOnlyList<ColumnSpec> cols)
    {
        for (int i = 0; i < cols.Count; i++) if (cols[i].Kind == ColumnKind.Stage) return i;
        return -1;
    }

    /// <summary>星の列が無い表は、最後に空の星の列を足して余りの幅を受ける（見出しと行で同じ）。</summary>
    private static void AddSpacer(ColumnDefinitions defs, int starAt)
    {
        if (starAt < 0) defs.Add(new ColumnDefinition(1, GridUnitType.Star));
    }

    /// <summary>見出しの文字と、最初の行の各セルの中身の位置のずれ（撮影の確かめ・設計 §7-3）。列ごとに、見出しの文字の左端と値の左端（数の列は右端どうし）を、この表の左上からの位置で比べる。
    /// 列の幅の積み上げでは比べない（見出しと行は列の大きさを共有するので積み上げは構造上いつも一致し、セルが列の中でどこへ寄っているかを見られない）。
    /// 測れる行が無い表は null。値の無い列（空のセル）は測らない。</summary>
    public (int Columns, double MaxDelta, string Worst, int Gaps)? MeasureAlignment()
    {
        var row = _body.Children.OfType<Grid>().FirstOrDefault(g => g.ColumnDefinitions.Count == _header.ColumnDefinitions.Count && g.ColumnDefinitions.Count > 0);
        if (row is null || !(_header.Bounds.Width > 0)) return null;
        double maxDelta = 0; string worst = ""; int gaps = 0, measured = 0;
        for (int i = 0; i < _header.ColumnDefinitions.Count && i < _headerCells.Length && i < _kinds.Length; i++)
        {
            var hc = _headerCells[i];
            var cell = row.Children.FirstOrDefault(c => Grid.GetColumn(c) == i);
            if (cell is null || !(cell.Bounds.Width > 0)) continue;
            var hp = hc.TranslatePoint(new Point(0, 0), this); var cp = cell.TranslatePoint(new Point(0, 0), this);
            if (hp is null || cp is null) continue;
            bool right = _kinds[i] == ColumnKind.Number;   // 数の列は右揃え＝右端どうし・それ以外は左端どうし
            double hx = right ? hp.Value.X + hc.Bounds.Width : hp.Value.X, cx = right ? cp.Value.X + cell.Bounds.Width : cp.Value.X;
            measured++;
            double delta = Math.Abs(hx - cx);
            if (delta > 0.5) gaps++;
            if (delta > maxDelta) { maxDelta = delta; worst = hc.Text ?? i.ToString(); }
        }
        return measured == 0 ? null : (measured, maxDelta, worst, gaps);
    }

    private static ColumnDefinition DefinitionOf(ColumnSpec c, int index, bool star)
    {
        if (star) return new ColumnDefinition(1, GridUnitType.Star) { MinWidth = c.MinWidth, MaxWidth = StageMaxWidth };   // 段の列は段の幅まで（広い欄でも数の列を段の直後に寄せる・A8）
        var d = new ColumnDefinition(GridLength.Auto) { MinWidth = c.MinWidth, SharedSizeGroup = "c" + index };
        if (!double.IsPositiveInfinity(c.MaxWidth)) d.MaxWidth = c.MaxWidth;
        return d;
    }

    private void BuildHeader(TableSnapshot snap)
    {
        _header.Children.Clear(); _header.ColumnDefinitions.Clear();
        _headerCells = new TextBlock[snap.Columns.Count];
        _kinds = snap.Columns.Select(c => c.Kind).ToArray();
        int starAt = StarIndex(snap.Columns);
        for (int i = 0; i < snap.Columns.Count; i++)
        {
            var c = snap.Columns[i];
            _header.ColumnDefinitions.Add(DefinitionOf(c, i, i == starAt));
            var tb = new TextBlock
            {
                Text = c.Name, FontSize = Small,
                TextAlignment = c.Kind == ColumnKind.Number ? TextAlignment.Right : TextAlignment.Left,
                Margin = i == 0 ? new Thickness(0) : new Thickness(8, 0, 0, 0),
                Cursor = new Cursor(StandardCursorType.Hand),
            };
            tb.Classes.Add("section");
            int ci = i;
            tb.PointerPressed += (_, e) =>
            {
                if (!e.GetCurrentPoint(tb).Properties.IsLeftButtonPressed) return;
                if (_sortColumn == ci) _sortDescending = !_sortDescending; else { _sortColumn = ci; _sortDescending = c.Kind == ColumnKind.Number; }
                Rebuild();
            };
            Grid.SetColumn(tb, i);
            _header.Children.Add(tb);
            _headerCells[i] = tb;
        }
        AddSpacer(_header.ColumnDefinitions, starAt);
    }

    private void UpdateHeaderMarks(TableSnapshot snap)
    {
        for (int i = 0; i < _headerCells.Length && i < snap.Columns.Count; i++)
            _headerCells[i].Text = snap.Columns[i].Name + (i == _sortColumn ? (_sortDescending ? " ↓" : " ↑") : "");
    }

    private static Control MakeGroupHeader(string group) => new StackPanel
    {
        Background = Brushes.Transparent,   // 吹き出しの当たり判定（透明でも箱全体が当たる）
        Children =
        {
            new Border { Height = 1, Background = Brush("LineBrush", Brushes.Tan), Margin = new Thickness(0, 6, 0, 2) },
            new TextBlock { Text = group, Foreground = Brush("GrayBrush", Brushes.SaddleBrown), FontWeight = FontWeight.Bold, FontSize = Small + 4, Margin = new Thickness(0, 0, 0, 2) },   // 群の題（素材・主食…）は表の字より大きく（2026-11-11 作者指示）
        },
    };

    /// <summary>行 1 本の描画（据え置き＝値だけ流す）。列の種類ごとのセルを持つ。</summary>
    private sealed class RowPresenter
    {
        public readonly Grid Root = new() { Margin = new Thickness(0, 1), Background = Brushes.Transparent };   // 透明でも当たる＝行のホバーと押下を受ける
        private Control?[] _cells = [];
        private string _sig = "";
        private string _key = "";
        private bool _emphasis, _selected, _focused;
        private readonly TableView _owner;

        public RowPresenter(TableView owner, IReadOnlyList<ColumnSpec> cols)
        {
            _owner = owner;
            Layout(cols);
            Root.PointerPressed += (_, e) =>
            {
                if (!e.GetCurrentPoint(Root).Properties.IsLeftButtonPressed) return;
                if (e.Source is Visual v && (v is Button || v.FindAncestorOfType<Button>() is not null)) return;   // 押し釦は押し釦の仕事
                owner.RowActivated?.Invoke(_key);
            };
        }

        /// <summary>選んでいる行か（背景で示す）。</summary>
        public void SetSelected(bool selected) { _selected = selected; Paint(); }

        /// <summary>飛んできた行か（目印の背景）。</summary>
        public void SetFocused(bool focused) { _focused = focused; Paint(); }

        private void Paint() => Root.Background = _focused ? Brush("AccentSoftBrush", Brushes.Tan)
            : _selected || _emphasis ? Brush("AccentFaintBrush", Brushes.Tan) : Brushes.Transparent;   // 選んだ行と自国の行＝淡い橙（手本の選んだ行・2026-11-12 点検⑫）   // 透明でも当たる＝行のホバーと押下を受ける

        private void Layout(IReadOnlyList<ColumnSpec> cols)
        {
            _sig = string.Join("|", cols.Select(c => c.Key + ":" + c.Kind));
            Root.Children.Clear(); Root.ColumnDefinitions.Clear();
            _cells = new Control?[cols.Count];
            int starAt = StarIndex(cols);
            for (int i = 0; i < cols.Count; i++) Root.ColumnDefinitions.Add(DefinitionOf(cols[i], i, i == starAt));
            AddSpacer(Root.ColumnDefinitions, starAt);
        }

        public void Update(IReadOnlyList<ColumnSpec> cols, RowView row)
        {
            if (_sig != string.Join("|", cols.Select(c => c.Key + ":" + c.Kind))) Layout(cols);
            _key = row.Key;
            _emphasis = row.Emphasis; _selected = _owner.SelectedKey is not null && row.Key == _owner.SelectedKey;
            Paint();
            for (int i = 0; i < cols.Count && i < row.Cells.Count; i++)
            {
                var cell = row.Cells[i];
                var spec = cols[i];
                if (_cells[i] is null)
                {
                    _cells[i] = Create(spec, i);
                    Grid.SetColumn(_cells[i]!, i);
                    Root.Children.Add(_cells[i]!);
                }
                Fill(_cells[i]!, spec, cell, !string.IsNullOrEmpty(row.Tip), i);
            }
        }

        private static Control Create(ColumnSpec spec, int index)
        {
            var margin = index == 0 ? new Thickness(0) : new Thickness(8, 0, 0, 0);
            switch (spec.Kind)
            {
                case ColumnKind.Number:
                    return new TextBlock
                    {
                        FontSize = Small, TextAlignment = TextAlignment.Right, Margin = margin, HorizontalAlignment = HorizontalAlignment.Right, Background = Brushes.Transparent,
                        Foreground = Brush("InkBrush", Brushes.Black),   // 割合の列も本文の色（青は意味のある所だけ・A5）
                    };
                case ColumnKind.Bar:
                    return new StackBar { Margin = margin, Width = 120, Height = 10, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
                case ColumnKind.Share:
                    return new HalfPie { Radius = 16, Margin = margin, HorizontalAlignment = HorizontalAlignment.Left };
                case ColumnKind.Stage:
                    return new BalancedWrapPanel { Margin = margin, MaxWidth = StageMaxWidth, HorizontalAlignment = HorizontalAlignment.Left };   // 段が多い行は欄の中で折り返す（段の列が名前の列を押しつぶさない）
                case ColumnKind.Verb:
                    return new Border { Margin = margin, HorizontalAlignment = HorizontalAlignment.Left };
                default:
                    return new TextBlock
                    {
                        FontSize = Small, TextTrimming = TextTrimming.CharacterEllipsis, Margin = index == 0 ? new Thickness(0, 0, 4, 0) : margin,   // 先頭の名前の列は右に隙間＝長い名前が次の列の数に接しない（W18）
                         Background = Brushes.Transparent, MaxWidth = MaxTextWidth, HorizontalAlignment = HorizontalAlignment.Left,   // 最大の幅より広い欄でも左に寄せる（中央に寄せない）
                        Foreground = index == 0 ? Brush("InkBrush", Brushes.Black) : Brush("InkMutedBrush", Brushes.Gray),
                    };
            }
        }

        internal static readonly IBrush[] PiePalette = [Brushes.SteelBlue, Brushes.Goldenrod, Brushes.IndianRed, Brushes.SeaGreen, Brushes.MediumPurple, Brushes.Peru];

        private static readonly Dictionary<string, IBrush> InkCache = new();

        /// <summary>「#RRGGBB」の色の筆（同じ色は使い回す）。読めなければ null。</summary>
        private static IBrush? InkOf(string? hex)
        {
            if (string.IsNullOrEmpty(hex)) return null;
            if (InkCache.TryGetValue(hex, out var b)) return b;
            if (!Color.TryParse(hex, out var c)) return null;
            return InkCache[hex] = new SolidColorBrush(c);
        }

        /// <summary>文字のセルの中身と飾り（色の丸・バッジの背景・数の文字色）。飾りが無いセルは既定の見た目へ戻す（前の月に飾りがあった行も）。</summary>
        private static readonly FontFamily BulletFont = new("avares://PriceOfBread.App/Assets/Fonts#Noto Sans JP");

        private static void ApplyText(TextBlock tb, ColumnSpec spec, CellView cell, int index)
        {
            IBrush defaultInk = spec.Kind == ColumnKind.Number ? Brush("InkBrush", Brushes.Black)
                                                                : index == 0 ? Brush("InkBrush", Brushes.Black) : Brush("InkMutedBrush", Brushes.Gray);
            var badge = InkOf(cell.BadgeFill);
            tb.Foreground = badge is not null ? InkOf("#FFF6E8") ?? Brushes.White : InkOf(cell.Color) ?? defaultInk;
            tb.Background = badge ?? Brushes.Transparent;
            tb.Padding = badge is not null ? new Thickness(4, 0) : default;
            if (InkOf(cell.Swatch) is { } dot)
            {
                string sig = cell.Swatch + "|" + cell.Text;
                if (!Equals(tb.Tag, sig))
                {
                    tb.Text = null; tb.Inlines?.Clear();
                    tb.Inlines ??= new Avalonia.Controls.Documents.InlineCollection();
                    tb.Inlines.Add(new Avalonia.Controls.Documents.Run("● ") { Foreground = dot, FontFamily = BulletFont, FontSize = tb.FontSize + 3 });   // 丸は日本語側の字体で描く（Times の「●」は点ほどに小さい・点検⑥）
                    tb.Inlines.Add(new Avalonia.Controls.Documents.Run(cell.Text));
                    tb.Tag = sig;
                }
            }
            else
            {
                if (tb.Tag is not null) { tb.Inlines?.Clear(); tb.Tag = null; tb.Text = null; }
                if (tb.Text != cell.Text) tb.Text = cell.Text;
            }
        }

        private static void Fill(Control control, ColumnSpec spec, CellView cell, bool rowHasTip, int index)
        {
            switch (control)
            {
                case TextBlock tb:
                    ApplyText(tb, spec, cell, index);
                    break;
                case StackBar bar:
                    bar.Cell = cell.Extra as BarCell;
                    break;
                case HalfPie pie when cell.Extra is ShareCell sc:
                    pie.Slices = sc.Parts.Select((p, i) => new HalfPieSlice(p.Share, PaletteOf(sc)[i % PaletteOf(sc).Count])).ToList();
                    break;
                case BalancedWrapPanel wp when cell.Extra is StageCell st:
                    FillStages(wp, st);
                    break;
                case Border b:
                    if (cell.Extra is not VerbCell vb) { b.Child = null; break; }   // この行にこの動詞が無い（前の月にはあった行も空にする）
                    if (b.Child is not StackPanel sp)
                    {
                        var note0 = new TextBlock { FontSize = 11, TextWrapping = TextWrapping.NoWrap, VerticalAlignment = VerticalAlignment.Center, IsVisible = false };
                        note0.Foreground = Application.Current is { } app && app.TryFindResource("InkMutedBrush", app.ActualThemeVariant, out var r) && r is IBrush mb ? mb : Brushes.Gray;
                        sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { new Button { Classes = { "mini" } }, note0 } };   // 理由は押し釦の右＝行を 1 行に保つ（2026-11-11 作者指摘「2 行になっている」）
                        b.Child = sp;
                    }
                    var btn = (Button)sp.Children[0];
                    btn.Content = vb.Name; btn.IsEnabled = vb.Enabled;
                    if (InkOf(vb.Fill) is { } fill) { btn.Background = fill; btn.Foreground = InkOf("#FFF6E8") ?? Brushes.White; }
                    else { btn.ClearValue(Button.BackgroundProperty); btn.ClearValue(Button.ForegroundProperty); }
                    btn.Tag = vb;   // 押した時に呼ぶのは最新の動詞
                    btn.Click -= OnVerbClick; btn.Click += OnVerbClick;
                    b.Background = Brushes.Transparent;
                    // 押せない理由は動詞の下に常に見せる（ホバーは廃止・作者の指示 2026-11-11）。押せる動詞には出さない
                    var note = (TextBlock)sp.Children[1];
                    note.Text = vb.Enabled || string.IsNullOrEmpty(vb.Tip) ? "" : vb.Tip;
                    note.IsVisible = note.Text.Length > 0;
                    break;
            }
        }

        private static void OnVerbClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button { Tag: VerbCell vb } btn && vb.Enabled)
                DispatchVerb(vb, (btn.FindAncestorOfType<TableView>()?.ConfirmVerb) ?? DefaultConfirm);
        }

        private static void FillStages(BalancedWrapPanel wp, StageCell st)
        {
            // 段の釦は理由の有無で幅も間も変えない＝理由は釦の列の最後（右の続き・入りきらなければ次の行）に 1 つだけ置く（V2）
            if (wp.Children.Count != st.Labels.Count + 1)
            {
                wp.Children.Clear();
                for (int i = 0; i < st.Labels.Count; i++)
                {
                    var btn = new Button { Classes = { "lawstage" } };
                    btn.Click += (s, _) => { if (s is Button { Tag: (StageCell cell, int level) } && cell.CanSelect(level)) cell.Select(level); };
                    wp.Children.Add(new Border { Background = Brushes.Transparent, Margin = new Thickness(0, 0, 3, 2), Child = btn });
                }
                var why = new TextBlock { FontSize = Small, TextWrapping = TextWrapping.NoWrap, IsVisible = false, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 2) };
                why.Foreground = Application.Current is { } app && app.TryFindResource("InkMutedBrush", app.ActualThemeVariant, out var r) && r is IBrush mb ? mb : Brushes.Gray;
                wp.Children.Add(why);
            }
            string? up = null, down = null;   // 理由は 1 つだけ＝進める側（現在の段より上の最初の押せない段）を優先・無ければ下げる側（V3）
            for (int i = 0; i < st.Labels.Count; i++)
            {
                int level = st.LevelBase + i;
                var btn = (Button)((Border)wp.Children[i]).Child!;
                btn.Content = st.Labels[i];
                btn.Tag = (st, level);   // 最新の段の中身を持たせる
                btn.IsEnabled = st.CanSelect(level);
                if (!btn.IsEnabled && st.Why?.Invoke(level) is { Length: > 0 } why)
                {
                    if (level > st.Current) up ??= why; else down = why;
                }
                btn.Classes.Set("current", st.Current == level);
                btn.Classes.Set("target", st.Target == level && st.Target != st.Current);
            }
            var whyText = (TextBlock)wp.Children[^1];
            whyText.Text = up ?? down ?? "";
            whyText.IsVisible = whyText.Text.Length > 0;
        }
    }
}
