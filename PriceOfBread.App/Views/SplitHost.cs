using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using PriceOfBread.App.Layout;

namespace PriceOfBread.App.Views;

/// <summary>分割の外枠（設計 §2-3）。3×3 の格子（列 0・境目・列 2／行 0・境目・行 2）に、最大 4 つの枠（<see cref="PaneView"/>）と 2 本の境目（GridSplitter）を置き、
/// 形ごとに枠の置き場所だけを変える（<see cref="SlotInfo.Place"/>）。枠の中身（ページ）は <see cref="LayoutModel"/> が決める＝ここは映すだけ。
/// ・ページの部品は盤面に 1 つだけ（同じページを 2 枠に置けない＝<see cref="LayoutModel"/> の 4 席は常に違う）。外れたページは DataContext を外す（見えていないページは更新を受けない）。
/// ・境目を放したとき、割合を <see cref="LayoutModel.SetRatios"/> へ返す（保存は配置の変更で自動）。</summary>
public sealed class SplitHost : Grid
{
    private readonly PaneView[] _panes = new PaneView[4];
    private readonly GridSplitter _vSplit = new() { Background = Brush("SandBrush", Brushes.Tan), Width = 6, ResizeDirection = GridResizeDirection.Columns, ResizeBehavior = GridResizeBehavior.PreviousAndNext };
    private readonly GridSplitter _hSplit = new() { Background = Brush("SandBrush", Brushes.Tan), Height = 6, ResizeDirection = GridResizeDirection.Rows, ResizeBehavior = GridResizeBehavior.PreviousAndNext };
    private LayoutModel? _layout;
    private Func<Slot, Control>? _factory;

    private static IBrush Brush(string key, IBrush fallback)
        => Application.Current is { } app && app.TryFindResource(key, app.ActualThemeVariant, out var v) && v is IBrush b ? b : fallback;

    public SplitHost()
    {
        ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star) { MinWidth = 120 });
        ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Pixel));
        ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star) { MinWidth = 120 });
        RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star) { MinHeight = 80 });
        RowDefinitions.Add(new RowDefinition(0, GridUnitType.Pixel));
        RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star) { MinHeight = 80 });
        for (int i = 0; i < _panes.Length; i++)
        {
            int idx = i;
            var p = new PaneView(i);
            p.Pressed += () => _layout?.Select(idx);
            p.PickSlot += s => _layout?.Open(s, idx);
            p.PickShape += sh => _layout?.SetShape(sh);
            _panes[i] = p;
            Children.Add(p);
        }
        Children.Add(_vSplit);
        Children.Add(_hSplit);
        _vSplit.DragCompleted += (_, _) => ReadRatios();
        _hSplit.DragCompleted += (_, _) => ReadRatios();
    }

    /// <summary>配置とページの作り手をつなぐ。ページの部品は種類ごとに 1 度だけ作って使い回す（枠を行き来しても並べ替えの状態などが残る）。</summary>
    public void Attach(LayoutModel layout, Func<Slot, Control> factory)
    {
        if (_layout is not null) _layout.Changed -= Apply;
        _layout = layout; _factory = factory;
        layout.Changed += Apply;
        Apply();
    }

    public void Detach()
    {
        if (_layout is not null) _layout.Changed -= Apply;
        _layout = null;
        foreach (var p in _panes) p.SetContent(null, null);
    }

    /// <summary>いま枠に載っているページの部品（撮影の検査用）。</summary>
    public IEnumerable<(int Pane, Slot Slot, Control Page)> Mounted() => _panes.Where(p => p.Current is not null && p.IsVisible).Select(p => (p.Index, p.Current!.Value, p.Page!));

    public PaneView PaneAt(int i) => _panes[i];

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        foreach (var p in _panes) if (p.Page is { } page) page.DataContext = DataContext;
    }

    private void ReadRatios()
    {
        if (_layout is null) return;
        double c0 = ColumnDefinitions[0].ActualWidth, c2 = ColumnDefinitions[2].ActualWidth, r0 = RowDefinitions[0].ActualHeight, r2 = RowDefinitions[2].ActualHeight;
        double main = c0 + c2 > 1 ? c0 / (c0 + c2) : _layout.MainRatio;
        double sub = r0 + r2 > 1 ? r0 / (r0 + r2) : _layout.SubRatio;
        _layout.SetRatios(main, sub);
    }

    private void Apply()
    {
        if (_layout is null || _factory is null) return;
        var place = SlotInfo.Place(_layout.Shape);
        int n = place.Panes.Length;

        // 置き場所（形ごと）
        for (int i = 0; i < _panes.Length; i++)
        {
            var p = _panes[i];
            p.IsVisible = i < n;
            if (i < n)
            {
                var c = place.Panes[i];
                SetRow(p, c.Row); SetColumn(p, c.Col); SetRowSpan(p, c.RowSpan); SetColumnSpan(p, c.ColSpan);
            }
        }
        Place(_vSplit, place.VerticalSplitter);
        Place(_hSplit, place.HorizontalSplitter);
        bool v = place.VerticalSplitter is not null, h = place.HorizontalSplitter is not null;
        ColumnDefinitions[0].Width = new GridLength(v ? _layout.MainRatio : 1, GridUnitType.Star);
        ColumnDefinitions[1].Width = new GridLength(v ? 6 : 0, GridUnitType.Pixel);
        ColumnDefinitions[2].Width = new GridLength(v ? 1 - _layout.MainRatio : 1, GridUnitType.Star);
        RowDefinitions[0].Height = new GridLength(h ? _layout.SubRatio : 1, GridUnitType.Star);
        RowDefinitions[1].Height = new GridLength(h ? 6 : 0, GridUnitType.Pixel);
        RowDefinitions[2].Height = new GridLength(h ? 1 - _layout.SubRatio : 1, GridUnitType.Star);

        // 中身：外れる物を先にすべて外してから載せる（同じ部品が 2 か所の親に付く瞬間を作らない）
        var want = new Slot?[_panes.Length];
        for (int i = 0; i < _panes.Length; i++) want[i] = i < n ? _layout.SlotAt(i) : null;
        for (int i = 0; i < _panes.Length; i++) if (_panes[i].Current != want[i]) _panes[i].SetContent(null, null);
        for (int i = 0; i < _panes.Length; i++)
            if (want[i] is { } s && _panes[i].Current != s)
            {
                var page = _factory(s);
                page.DataContext = DataContext;
                _panes[i].SetContent(s, page);
            }
        _panes[0].SetShape(_layout.Shape);
        for (int i = 0; i < n; i++) _panes[i].SetSelected(i == _layout.Selected);
    }

    private static void Place(Control c, Cell? cell)
    {
        c.IsVisible = cell is not null;
        if (cell is not { } k) return;
        SetRow(c, k.Row); SetColumn(c, k.Col); SetRowSpan(c, k.RowSpan); SetColumnSpan(c, k.ColSpan);
    }
}

/// <summary>分割の 1 枠＝上に 1 行の見出し（ページの名前の押し釦・先頭の枠だけ分割の形の押し釦）と、下にページ。名前を押すと 7 ページと地図の一覧が出て選べる。
/// 選んでいる枠は縁の色で示す。</summary>
public sealed class PaneView : Border
{
    public int Index { get; }
    public Slot? Current { get; private set; }
    public Control? Page { get; private set; }

    public event Action? Pressed;
    public event Action<Slot>? PickSlot;
    public event Action<SplitShape>? PickShape;

    private readonly Button _name = new() { Classes = { "mini" }, HorizontalAlignment = HorizontalAlignment.Left };
    private readonly ContentControl _host = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
    private SplitShape _shapeNow;

    private static IBrush Brush(string key, IBrush fallback)
        => Application.Current is { } app && app.TryFindResource(key, app.ActualThemeVariant, out var v) && v is IBrush b ? b : fallback;

    public PaneView(int index)
    {
        Index = index;
        BorderThickness = new Thickness(2);
        BorderBrush = Brush("SandDarkBrush", Brushes.Tan);
        Background = Brush("ParchmentBrush", Brushes.Beige);
        ClipToBounds = true;

        var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(4, 2) };
        _name.Flyout = NameFlyout();
        headerGrid.Children.Add(_name);
        if (index == 0)
        {
            var shape = new Button { Classes = { "mini" }, Content = "分割", HorizontalAlignment = HorizontalAlignment.Right };
            shape.Flyout = ShapeFlyout();
            Grid.SetColumn(shape, 1);
            headerGrid.Children.Add(shape);
        }
        var header = new Border { Background = Brush("SandLightBrush", Brushes.Tan), BorderBrush = Brush("SandDarkBrush", Brushes.Tan), BorderThickness = new Thickness(0, 0, 0, 1), Child = headerGrid };
        DockPanel.SetDock(header, Dock.Top);
        Child = new DockPanel { Children = { header, _host } };
        AddHandler(PointerPressedEvent, (_, _) => Pressed?.Invoke(), RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private MenuFlyout NameFlyout()
    {
        var f = new MenuFlyout();
        f.Opening += (_, _) =>
        {
            var items = new List<object>();
            foreach (var s in SlotInfo.All)
            {
                var mi = new MenuItem { Header = (Current == s ? "● " : "　") + SlotInfo.ButtonText(s) };
                var slot = s;
                mi.Click += (_, _) => PickSlot?.Invoke(slot);
                items.Add(mi);
            }
            f.ItemsSource = items;
        };
        return f;
    }

    private MenuFlyout ShapeFlyout()
    {
        var f = new MenuFlyout();
        f.Opening += (_, _) =>
        {
            var items = new List<object>();
            foreach (var sh in Enum.GetValues<SplitShape>())
            {
                var mi = new MenuItem { Header = (_shapeNow == sh ? "● " : "　") + SlotInfo.ShapeName(sh) };
                var shape = sh;
                mi.Click += (_, _) => PickShape?.Invoke(shape);
                items.Add(mi);
            }
            f.ItemsSource = items;
        };
        return f;
    }

    /// <summary>形（先頭の枠の「分割」の一覧が現在の形に印を付ける）。</summary>
    public void SetShape(SplitShape s) => _shapeNow = s;

    /// <summary>中身を載せる（null で外す）。外すとき DataContext も外す＝見えていないページは更新を受けない。</summary>
    public void SetContent(Slot? slot, Control? page)
    {
        if (Page is { } old && !ReferenceEquals(old, page)) { _host.Content = null; old.DataContext = null; }
        Current = slot; Page = page;
        if (page is not null) _host.Content = page;
        _name.Content = slot is { } s ? SlotInfo.Name(s) + " ▾" : "";
    }

    public void SetSelected(bool selected)
        => BorderBrush = selected ? Brush("BlueBrush", Brushes.SteelBlue) : Brush("SandDarkBrush", Brushes.Tan);
}
