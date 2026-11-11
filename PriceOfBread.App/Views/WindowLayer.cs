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

/// <summary>浮かぶ窓を重ねる層（作者の指示 2026-11-11）。背景（<see cref="PageHost"/>）と同じ場所に重ね、開いている窓ごとに 1 つの <see cref="FloatingWindow"/> を置く。
/// 層そのものは何も描かない・当たりも持たない（窓の無い所の押下・ホイールは下の背景＝地図へ通る）。
/// 窓の本数・順・位置は <see cref="LayoutModel"/> が決める＝ここは映すだけ。窓の部品は開いている間だけ生き、中のページの部品は種類ごとに 1 度だけ作って使い回す。</summary>
public sealed class WindowLayer : Canvas
{
    private LayoutModel? _layout;
    private Func<Slot, Control>? _factory;
    private readonly Dictionary<Slot, FloatingWindow> _windows = new();

    public WindowLayer() { ClipToBounds = true; }

    /// <summary>配置とページの作り手をつなぐ。</summary>
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
        foreach (var w in _windows.Values) { w.SetPage(null); Children.Remove(w); }
        _windows.Clear();
    }

    /// <summary>開いている窓（重なりの順・撮影の検査用）。枠番号は背景が 0＝窓は 1 から。</summary>
    public IEnumerable<(int Pane, Slot Slot, Control Page, FloatingWindow Frame)> Mounted()
    {
        if (_layout is null) yield break;
        int i = 1;
        foreach (var s in _layout.OpenWindows)
            if (_windows.TryGetValue(s, out var w) && w.Page is { } p) yield return (i++, s, p, w);
    }

    public FloatingWindow? WindowOf(Slot slot) => _windows.TryGetValue(slot, out var w) ? w : null;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        foreach (var w in _windows.Values) w.Page?.SetCurrentValue(DataContextProperty, DataContext);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var r = base.ArrangeOverride(finalSize);
        Place();   // 領域の大きさが変わったとき収め直す
        return r;
    }

    private void Apply()
    {
        if (_layout is null || _factory is null) return;
        var open = _layout.OpenWindows;
        // 閉じた窓を外す（中身のページは DataContext を外して離す）
        foreach (var slot in _windows.Keys.Where(k => !open.Contains(k)).ToList())
        {
            var w = _windows[slot];
            w.SetPage(null);
            Children.Remove(w);
            _windows.Remove(slot);
        }
        // 開いた窓を作る
        foreach (var slot in open)
            if (!_windows.ContainsKey(slot))
            {
                var w = new FloatingWindow(slot, SlotInfo.Name(slot), _layout);
                var page = _factory(slot);
                w.SetPage(page);
                page.DataContext = DataContext;
                _windows[slot] = w;
                Children.Add(w);
            }
        // 重なりの順（開いた順＝後ろほど前）と、一番前の窓の縁の色
        for (int i = 0; i < open.Count; i++)
        {
            var w = _windows[open[i]];
            w.ZIndex = i;
            w.SetFront(i == open.Count - 1);
        }
        Place();
    }

    /// <summary>覚えている位置と大きさ（無ければ初めの値）を領域の中に収めて窓へ置く。ドラッグ中の窓は触らない。</summary>
    private void Place()
    {
        if (_layout is null || !(Bounds.Width > 0) || !(Bounds.Height > 0)) return;
        foreach (var (slot, w) in _windows)
        {
            if (w.Interacting) continue;
            var r = _layout.RectOf(slot) ?? LayoutModel.DefaultRect(slot, Bounds.Width, Bounds.Height);
            w.SetRect(LayoutModel.Fit(r, Bounds.Width, Bounds.Height));
        }
    }
}

/// <summary>浮かぶ窓 1 枚＝題の帯（ページの名前・閉じる）と、ページの部品。題の帯をつかむと動き、縁と角をつかむと大きさが変わる。
/// 窓のどこを押しても一番前に出る。動かし・大きさ変えの最中は窓の部品だけを動かし、終わったときに 1 度だけ <see cref="LayoutModel.SetRect"/> へ渡す
/// （配置の保存を毎フレーム書かない）。</summary>
public sealed class FloatingWindow : Border
{
    private const double Grip = 6, CornerGrip = 14, TitleHeight = 30;

    private readonly Slot _slot;
    private readonly LayoutModel _layout;
    private readonly Border _body = new();
    private readonly Border _titleBar;
    private enum Mode { None, Move, Resize }
    private Mode _mode;
    private bool _left, _top, _right, _bottom;       // 大きさ変えでつかんだ辺
    private Point _startPoint;                        // つかんだ位置（層の座標）
    private WindowRect _startRect;

    public Control? Page { get; private set; }
    /// <summary>動かし・大きさ変えの最中か（層が置き直さない）。</summary>
    public bool Interacting => _mode != Mode.None;

    public FloatingWindow(Slot slot, string title, LayoutModel layout)
    {
        _slot = slot; _layout = layout;
        MinWidth = LayoutModel.MinWindowWidth; MinHeight = LayoutModel.MinWindowHeight;
        Background = Brush("ParchmentBrush", Brushes.Beige);
        BorderThickness = new Thickness(1.5);
        CornerRadius = new CornerRadius(4);
        ClipToBounds = true;
        BoxShadow = BoxShadows.Parse("0 4 16 0 #55000000");

        var close = new Button { Content = "×", Classes = { "edge" }, Padding = new Thickness(10, 0), FontSize = 16, VerticalAlignment = VerticalAlignment.Stretch };
        close.Click += (_, _) => _layout.Close(_slot);
        var name = new TextBlock { Text = title, FontWeight = FontWeight.Bold, FontSize = 14, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0), Foreground = Brush("InkBrush", Brushes.Black) };
        var bar = new Grid { ColumnDefinitions = { new ColumnDefinition(1, GridUnitType.Star), new ColumnDefinition(GridLength.Auto) } };
        bar.Children.Add(name); Grid.SetColumn(close, 1); bar.Children.Add(close);
        _titleBar = new Border { Height = TitleHeight, Background = Brush("SandBrush", Brushes.Tan), Child = bar, Cursor = new Cursor(StandardCursorType.SizeAll) };
        _titleBar.PointerPressed += (s, e) => Begin(s, e, Mode.Move, false, false, false, false);
        _titleBar.PointerMoved += OnMoved;
        _titleBar.PointerReleased += OnReleased;
        _titleBar.PointerCaptureLost += (_, _) => Finish();

        var content = new DockPanel();
        DockPanel.SetDock(_titleBar, Dock.Top);
        content.Children.Add(_titleBar);
        content.Children.Add(_body);

        // 縁と角のつかみ代（見えない帯）。内側へ少し入れて、ページの端の部品と重なっても当たるようにする
        var root = new Grid();
        root.Children.Add(content);
        root.Children.Add(Handle(HorizontalAlignment.Stretch, VerticalAlignment.Top, double.NaN, Grip, StandardCursorType.SizeNorthSouth, false, true, false, false));
        root.Children.Add(Handle(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, double.NaN, Grip, StandardCursorType.SizeNorthSouth, false, false, false, true));
        root.Children.Add(Handle(HorizontalAlignment.Left, VerticalAlignment.Stretch, Grip, double.NaN, StandardCursorType.SizeWestEast, true, false, false, false));
        root.Children.Add(Handle(HorizontalAlignment.Right, VerticalAlignment.Stretch, Grip, double.NaN, StandardCursorType.SizeWestEast, false, false, true, false));
        root.Children.Add(Handle(HorizontalAlignment.Left, VerticalAlignment.Top, CornerGrip, CornerGrip, StandardCursorType.TopLeftCorner, true, true, false, false));
        root.Children.Add(Handle(HorizontalAlignment.Right, VerticalAlignment.Top, CornerGrip, CornerGrip, StandardCursorType.TopRightCorner, false, true, true, false));
        root.Children.Add(Handle(HorizontalAlignment.Left, VerticalAlignment.Bottom, CornerGrip, CornerGrip, StandardCursorType.BottomLeftCorner, true, false, false, true));
        root.Children.Add(Handle(HorizontalAlignment.Right, VerticalAlignment.Bottom, CornerGrip, CornerGrip, StandardCursorType.BottomRightCorner, false, false, true, true));
        Child = root;

        // どこを押しても一番前へ（子が押下を握っても届くよう Tunnel・握られた後も受ける）
        AddHandler(PointerPressedEvent, (_, _) => _layout.BringToFront(_slot), RoutingStrategies.Tunnel, handledEventsToo: true);
        SetFront(false);
    }

    private static IBrush Brush(string key, IBrush fallback)
        => Application.Current is { } app && app.TryFindResource(key, app.ActualThemeVariant, out var v) && v is IBrush b ? b : fallback;

    private Border Handle(HorizontalAlignment ha, VerticalAlignment va, double w, double h, StandardCursorType cursor, bool l, bool t, bool r, bool b)
    {
        var hd = new Border { HorizontalAlignment = ha, VerticalAlignment = va, Background = Brushes.Transparent, Cursor = new Cursor(cursor) };
        if (!double.IsNaN(w)) hd.Width = w;
        if (!double.IsNaN(h)) hd.Height = h;
        hd.PointerPressed += (s, e) => Begin(s, e, Mode.Resize, l, t, r, b);
        hd.PointerMoved += OnMoved;
        hd.PointerReleased += OnReleased;
        hd.PointerCaptureLost += (_, _) => Finish();
        return hd;
    }

    public void SetPage(Control? page)
    {
        if (Page is { } old) { old.DataContext = null; _body.Child = null; }
        Page = page;
        if (page is not null) _body.Child = page;
    }

    public void SetFront(bool front)
        => BorderBrush = front ? Brush("BlueBrush", Brushes.SteelBlue) : Brush("SandDarkBrush", Brushes.Tan);

    /// <summary>層の座標での位置と大きさを置く（Canvas の置き方）。</summary>
    public void SetRect(WindowRect r)
    {
        Canvas.SetLeft(this, r.X); Canvas.SetTop(this, r.Y);
        Width = r.W; Height = r.H;
    }

    private WindowRect CurrentRect() => new(Canvas.GetLeft(this) is var x && double.IsFinite(x) ? x : 0, Canvas.GetTop(this) is var y && double.IsFinite(y) ? y : 0, Width, Height);

    private Canvas? LayerOf() => Parent as Canvas;

    private void Begin(object? sender, PointerPressedEventArgs e, Mode mode, bool l, bool t, bool r, bool b)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || LayerOf() is not { } layer) return;
        _mode = mode; _left = l; _top = t; _right = r; _bottom = b;
        _startPoint = e.GetPosition(layer);
        _startRect = CurrentRect();
        e.Pointer.Capture(sender as IInputElement ?? this);
        e.Handled = true;
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (_mode == Mode.None || LayerOf() is not { } layer) return;
        var p = e.GetPosition(layer);
        double dx = p.X - _startPoint.X, dy = p.Y - _startPoint.Y;
        double hostW = layer.Bounds.Width, hostH = layer.Bounds.Height;
        var r = _startRect;
        if (_mode == Mode.Move)
            r = new WindowRect(r.X + dx, r.Y + dy, r.W, r.H);
        else
        {
            double x = r.X, y = r.Y, w = r.W, h = r.H;
            if (_right) w = r.W + dx;
            if (_bottom) h = r.H + dy;
            if (_left) { w = r.W - dx; x = r.X + dx; }
            if (_top) { h = r.H - dy; y = r.Y + dy; }
            if (w < LayoutModel.MinWindowWidth) { if (_left) x -= LayoutModel.MinWindowWidth - w; w = LayoutModel.MinWindowWidth; }
            if (h < LayoutModel.MinWindowHeight) { if (_top) y -= LayoutModel.MinWindowHeight - h; h = LayoutModel.MinWindowHeight; }
            r = new WindowRect(x, y, w, h);
        }
        SetRect(LayoutModel.Fit(r, hostW, hostH));
        e.Handled = true;
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        e.Pointer.Capture(null);
        Finish();
        e.Handled = true;
    }

    /// <summary>つかみ終わり＝最終の位置と大きさを配置へ渡す（1 度だけ）。</summary>
    private void Finish()
    {
        if (_mode == Mode.None) return;
        _mode = Mode.None;
        _layout.SetRect(_slot, CurrentRect());
    }
}
