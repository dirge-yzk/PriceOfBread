using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.Controls;

/// <summary>共通の表（設計 §6-7）。本体の目録の列から、見出しと行を 1 つの定義で作る（見出しと行の列の定義を 2 か所に書かない）。
/// ・行は作り直さず、鍵で値だけ流す（SyncRows の規律＝毎月の更新でボタンとツールチップを壊さない）。
/// ・見出しは止めて、中身だけを流す（<see cref="InnerScroll"/> が真のとき。ページの外側は縦に流れない／長い一覧は欄の中だけで流れる）。
/// ・見出しを押すと並べ替え（同じ列をもう一度で降順）。群（<see cref="RowView.Group"/>）があれば群の見出しで区切る。
/// ・<see cref="Filterable"/> が真なら絞り込みの文字の欄を出す。
/// ・列の幅は単位から決めた最小幅（<c>MinWidth</c>）。固定の <c>Width</c> は値を黙って切るので使わない。見出しと行は列の大きさを共有する。
/// ・Avalonia の描画の部品だけ。XAML を持たない＝結線は <see cref="SnapshotProperty"/> の 1 本（呼び手の XAML が x:DataType 付きで束縛する）。</summary>
public sealed class TableView : UserControl
{
    public static readonly StyledProperty<TableSnapshot?> SnapshotProperty = AvaloniaProperty.Register<TableView, TableSnapshot?>(nameof(Snapshot));
    public static readonly StyledProperty<bool> FilterableProperty = AvaloniaProperty.Register<TableView, bool>(nameof(Filterable));
    public static readonly StyledProperty<bool> InnerScrollProperty = AvaloniaProperty.Register<TableView, bool>(nameof(InnerScroll));

    public TableSnapshot? Snapshot { get => GetValue(SnapshotProperty); set => SetValue(SnapshotProperty, value); }
    /// <summary>絞り込みの文字の欄を出すか。</summary>
    public bool Filterable { get => GetValue(FilterableProperty); set => SetValue(FilterableProperty, value); }
    /// <summary>中身（行）だけを欄の中で流すか。偽なら行の数だけ高くなる（外側の流れに任せる）。</summary>
    public bool InnerScroll { get => GetValue(InnerScrollProperty); set => SetValue(InnerScrollProperty, value); }

    private readonly Grid _root = new() { RowDefinitions = new RowDefinitions("Auto,Auto,*") };
    private readonly TextBox _filterBox = new() { Watermark = "", MinHeight = 22, Margin = new Thickness(0, 0, 0, 2), IsVisible = false };
    private readonly Grid _header = new();
    private readonly StackPanel _body = new() { Spacing = 0 };
    private readonly Border _bodyHost = new();
    private readonly Dictionary<string, RowPresenter> _rows = new();
    private readonly Dictionary<string, Control> _groups = new();
    private string _columnSig = "";
    private int _sortColumn = -1;
    private bool _sortDescending;
    private string _filter = "";
    private TextBlock[] _headerCells = [];

    static TableView()
    {
        SnapshotProperty.Changed.AddClassHandler<TableView>((v, _) => v.Rebuild());
        FilterableProperty.Changed.AddClassHandler<TableView>((v, _) => { v._filterBox.IsVisible = v.Filterable; v.Rebuild(); });
        InnerScrollProperty.Changed.AddClassHandler<TableView>((v, _) => v.AttachBody());
    }

    public TableView()
    {
        Grid.SetIsSharedSizeScope(_root, true);   // 見出しと行が同じ列の大きさを共有する
        Grid.SetRow(_filterBox, 0); Grid.SetRow(_header, 1); Grid.SetRow(_bodyHost, 2);
        _root.Children.Add(_filterBox); _root.Children.Add(_header); _root.Children.Add(_bodyHost);
        _filterBox.TextChanged += (_, _) => { _filter = _filterBox.Text ?? ""; Rebuild(); };
        Content = _root;
        AttachBody();
    }

    private void AttachBody()
    {
        if (_bodyHost.Child is ScrollViewer old) old.Content = null;   // 行の入れ物は同時に 1 か所にしか置けない
        _bodyHost.Child = null;
        if (InnerScroll) _bodyHost.Child = new ScrollViewer { HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, Content = _body };
        else _bodyHost.Child = _body;
    }

    // ---- 資源（App.axaml のトークン。無ければ既定）----
    private static IBrush Brush(string key, IBrush fallback)
        => Application.Current is { } app && app.TryFindResource(key, app.ActualThemeVariant, out var v) && v is IBrush b ? b : fallback;
    private static double Small => Application.Current is { } app && app.TryFindResource("FontSmall", app.ActualThemeVariant, out var v) && v is double d ? d : 12;

    private void Rebuild()
    {
        var snap = Snapshot ?? TableSnapshot.Empty;
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
            if (!_rows.TryGetValue(row.Key, out var rp)) _rows[row.Key] = rp = new RowPresenter(snap.Columns);
            rp.Update(snap.Columns, row);
            live.Add(row.Key);
            wanted.Add(rp.Root);
        }
        foreach (var dead in _rows.Keys.Where(k => !live.Contains(k)).ToList()) _rows.Remove(dead);
        if (!_body.Children.SequenceEqual(wanted)) { _body.Children.Clear(); _body.Children.AddRange(wanted); }
        UpdateHeaderMarks(snap);
    }

    private static ColumnDefinition DefinitionOf(ColumnSpec c, int index, bool firstText)
    {
        if (firstText) return new ColumnDefinition(1, GridUnitType.Star) { MinWidth = c.MinWidth };   // 最初の文字の列＝残りの幅を全部取る
        var d = new ColumnDefinition(GridLength.Auto) { MinWidth = c.MinWidth, SharedSizeGroup = "c" + index };
        if (!double.IsPositiveInfinity(c.MaxWidth)) d.MaxWidth = c.MaxWidth;
        return d;
    }

    private void BuildHeader(TableSnapshot snap)
    {
        _header.Children.Clear(); _header.ColumnDefinitions.Clear();
        _headerCells = new TextBlock[snap.Columns.Count];
        bool firstText = true;
        for (int i = 0; i < snap.Columns.Count; i++)
        {
            var c = snap.Columns[i];
            bool star = c.Kind == ColumnKind.Text && firstText;
            if (c.Kind == ColumnKind.Text) firstText = false;
            _header.ColumnDefinitions.Add(DefinitionOf(c, i, star));
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
    }

    private void UpdateHeaderMarks(TableSnapshot snap)
    {
        for (int i = 0; i < _headerCells.Length && i < snap.Columns.Count; i++)
            _headerCells[i].Text = snap.Columns[i].Name + (i == _sortColumn ? (_sortDescending ? " ▼" : " ▲") : "");
    }

    private static Control MakeGroupHeader(string group) => new StackPanel
    {
        Children =
        {
            new Border { Height = 1, Background = Brush("SandBrush", Brushes.Tan), Margin = new Thickness(0, 6, 0, 2) },
            new TextBlock { Text = group, Foreground = Brush("BronzeBrush", Brushes.SaddleBrown), FontWeight = FontWeight.Bold, FontSize = Small, Margin = new Thickness(0, 0, 0, 1) },
        },
    };

    /// <summary>行 1 本の描画（据え置き＝値だけ流す）。列の種類ごとのセルを持つ。</summary>
    private sealed class RowPresenter
    {
        public readonly Grid Root = new() { Margin = new Thickness(0, 1) };
        private Control?[] _cells = [];
        private string _sig = "";

        public RowPresenter(IReadOnlyList<ColumnSpec> cols) => Layout(cols);

        private void Layout(IReadOnlyList<ColumnSpec> cols)
        {
            _sig = string.Join("|", cols.Select(c => c.Key + ":" + c.Kind));
            Root.Children.Clear(); Root.ColumnDefinitions.Clear();
            _cells = new Control?[cols.Count];
            bool firstText = true;
            for (int i = 0; i < cols.Count; i++)
            {
                bool star = cols[i].Kind == ColumnKind.Text && firstText;
                if (cols[i].Kind == ColumnKind.Text) firstText = false;
                Root.ColumnDefinitions.Add(DefinitionOf(cols[i], i, star));
            }
        }

        public void Update(IReadOnlyList<ColumnSpec> cols, RowView row)
        {
            if (_sig != string.Join("|", cols.Select(c => c.Key + ":" + c.Kind))) Layout(cols);
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
                Fill(_cells[i]!, spec, cell);
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
                        FontSize = Small, TextAlignment = TextAlignment.Right, Margin = margin, HorizontalAlignment = HorizontalAlignment.Right,
                        Foreground = spec.Unit == Unit.Share ? Brush("BlueBrush", Brushes.SteelBlue) : Brush("InkBrush", Brushes.Black),
                    };
                case ColumnKind.Share:
                    return new HalfPie { Radius = 16, Margin = margin, HorizontalAlignment = HorizontalAlignment.Left };
                case ColumnKind.Stage:
                    return new WrapPanel { Orientation = Orientation.Horizontal, Margin = margin };
                case ColumnKind.Verb:
                    return new Border { Margin = margin, HorizontalAlignment = HorizontalAlignment.Left };
                default:
                    return new TextBlock
                    {
                        FontSize = Small, TextTrimming = TextTrimming.CharacterEllipsis, Margin = margin,
                        Foreground = index == 0 ? Brush("InkBrush", Brushes.Black) : Brush("InkMutedBrush", Brushes.Gray),
                    };
            }
        }

        private static readonly IBrush[] PiePalette = [Brushes.SteelBlue, Brushes.Goldenrod, Brushes.IndianRed, Brushes.SeaGreen, Brushes.MediumPurple, Brushes.Peru];

        private static void Fill(Control control, ColumnSpec spec, CellView cell)
        {
            switch (control)
            {
                case TextBlock tb:
                    if (tb.Text != cell.Text) tb.Text = cell.Text;
                    ToolTip.SetTip(tb, spec.Kind == ColumnKind.Text ? cell.Text : null);   // 溢れて省略されても全文が読める
                    break;
                case HalfPie pie when cell.Extra is ShareCell sc:
                    pie.Slices = sc.Parts.Select((p, i) => new HalfPieSlice(p.Share, PiePalette[i % PiePalette.Length])).ToList();
                    break;
                case WrapPanel wp when cell.Extra is StageCell st:
                    FillStages(wp, st);
                    break;
                case Border b when cell.Extra is VerbCell vb:
                    if (b.Child is not Button btn)
                    {
                        btn = new Button { Classes = { "mini" } };
                        b.Child = btn;
                    }
                    btn.Content = vb.Name; btn.IsEnabled = vb.Enabled;
                    btn.Tag = vb;   // 押した時に呼ぶのは最新の動詞
                    btn.Click -= OnVerbClick; btn.Click += OnVerbClick;
                    break;
            }
        }

        private static void OnVerbClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button { Tag: VerbCell vb } && vb.Enabled) vb.Invoke();
        }

        private static void FillStages(WrapPanel wp, StageCell st)
        {
            if (wp.Children.Count != st.Labels.Count)
            {
                wp.Children.Clear();
                for (int i = 0; i < st.Labels.Count; i++)
                {
                    var btn = new Button { Classes = { "lawstage" } };
                    btn.Click += (s, _) => { if (s is Button { Tag: (StageCell cell, int level) } && cell.CanSelect(level)) cell.Select(level); };
                    wp.Children.Add(new Border { Background = Brushes.Transparent, Margin = new Thickness(0, 0, 3, 2), Child = btn });
                }
            }
            for (int i = 0; i < st.Labels.Count; i++)
            {
                int level = st.LevelBase + i;
                var border = (Border)wp.Children[i];
                var btn = (Button)border.Child!;
                btn.Content = st.Labels[i];
                btn.Tag = (st, level);   // 最新の段の中身を持たせる
                btn.IsEnabled = st.CanSelect(level);
                btn.Classes.Set("current", st.Current == level);
                btn.Classes.Set("target", st.Target == level && st.Target != st.Current);
                ToolTip.SetTip(border, st.Hover(level));   // 空なら null＝空の吹き出しを出さない
            }
        }
    }
}
