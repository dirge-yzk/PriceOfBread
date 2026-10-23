using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace PriceOfBread.App.Controls;

/// <summary>ページの欄 1 つの宣言（設計 §2-5）。順位は畳む順（1＝最後まで残る・数が大きいほど先に見出しの 1 行へ畳まれる）。最小の行数はその欄が開いている間に最低限見せる行数。
/// 重みは余った高さの分け方（開いている欄どうしの比）。</summary>
public sealed record SectionSpec(string Key, string Title, int Rank, int MinRows = 6, double Weight = 1);

/// <summary>ページの並べの表（設計 §2-5・§6-9）＝ページごとに 1 つだけ手で書く。欄の宣言と、幅が 3 列のときの欄の置き場（列ごとに上から下の順）。
/// 2 列・1 列のときは 3 列の並びを左の列から順に下へ積んだ列を、最小の高さの合計が半分になる所で割る（表に書かない）。</summary>
/// <param name="ColumnMinWidth">1 列の最小の幅。null（既定）なら幅で決める（1200 以上＝3 列・700 以上＝2 列）。幅の広い表を持つページは、列を増やしすぎない最小の幅を宣言する
/// （列の数＝幅 ÷ 最小の幅・1〜3 の間・置き場の列の数を超えない）。</param>
public sealed record PageTable(IReadOnlyList<SectionSpec> Sections, IReadOnlyList<IReadOnlyList<string>> Columns3, double? ColumnMinWidth = null);

/// <summary>欄 1 つの割り当て＝畳むか・高さ（見出しを含む）。</summary>
public readonly record struct SectionPlace(string Key, bool Collapsed, double Height);

/// <summary>ページの欄の並べの計算（Avalonia に依らない＝番人から直接見る）。
/// <list type="number">
/// <item>幅で列の数を決める（1200 以上＝3・700 以上＝2・それ未満＝1）。</item>
/// <item>欄を列へ置く（<see cref="Arrange"/>）。</item>
/// <item>列の高さに最小の高さの合計が入らなければ、順位の数が大きい欄（同順位なら下の欄）から見出しの 1 行へ畳む。利用者が開いた欄は最後に畳まれる。</item>
/// <item>余りの高さは開いている欄へ重みの比で分ける。</item>
/// </list></summary>
public static class PagePlan
{
    public const double HeaderHeight = 26;   // 欄の見出しの 1 行
    public const double RowHeight = 22;      // 行の高さの見積り（最小の行数 × これ＝最小の高さ）
    public const double ColumnGap = 12, SectionGap = 8;

    public static int ColumnCount(double width) => width >= 1200 ? 3 : width >= 700 ? 2 : 1;

    /// <summary>表の宣言（<see cref="PageTable.ColumnMinWidth"/>）を踏まえた列の数。</summary>
    public static int ColumnCount(double width, PageTable t)
    {
        if (t.ColumnMinWidth is not { } min || !(min > 0)) return ColumnCount(width);
        int n = (int)Math.Floor((width + ColumnGap) / (min + ColumnGap));
        return Math.Clamp(n, 1, Math.Max(1, Math.Min(3, t.Columns3.Count)));
    }

    public static double MinHeight(SectionSpec s) => HeaderHeight + Math.Max(0, s.MinRows) * RowHeight;

    /// <summary>欄を <paramref name="columns"/> 列へ置く。表に無い鍵・重複は例外（宣言の間違いは起動時に落とす）。</summary>
    public static IReadOnlyList<IReadOnlyList<SectionSpec>> Arrange(PageTable t, int columns)
    {
        var byKey = new Dictionary<string, SectionSpec>();
        foreach (var s in t.Sections) if (!byKey.TryAdd(s.Key, s)) throw new ArgumentException($"欄の鍵が重なっている: {s.Key}");
        var three = t.Columns3.Select(c => (IReadOnlyList<SectionSpec>)c.Select(k => byKey.TryGetValue(k, out var s) ? s : throw new ArgumentException($"表に無い欄: {k}")).ToList()).ToList();
        if (three.SelectMany(c => c).Select(s => s.Key).Distinct().Count() != three.Sum(c => c.Count)) throw new ArgumentException("同じ欄を 2 つの列に置いている");
        if (columns >= 3 || columns >= three.Count || three.Count <= 1) return columns == 1 ? [three.SelectMany(c => c).ToList()] : three;
        var flat = three.SelectMany(c => c).ToList();
        if (columns <= 1) return [flat];
        // 2 列：左から順に積み、最小の高さの合計が半分に届いた所で次の列へ（各列に最低 1 つ）
        double total = flat.Sum(MinHeight), acc = 0;
        var left = new List<SectionSpec>(); var right = new List<SectionSpec>();
        foreach (var s in flat)
        {
            bool toLeft = left.Count == 0 || (acc < total / 2 && flat.Count - left.Count > 1);
            if (toLeft) { left.Add(s); acc += MinHeight(s); } else right.Add(s);
        }
        return [left, right];
    }

    /// <summary>1 つの列の割り当て。<paramref name="height"/> が無限大なら畳まず、最小の高さのまま。</summary>
    public static IReadOnlyList<SectionPlace> PlanColumn(IReadOnlyList<SectionSpec> column, double height, ISet<string>? userOpened = null, ISet<string>? userClosed = null)
    {
        int n = column.Count;
        var collapsed = new bool[n];
        for (int i = 0; i < n; i++) if (userClosed?.Contains(column[i].Key) == true) collapsed[i] = true;
        double Need()
        {
            double sum = Math.Max(0, n - 1) * SectionGap;
            for (int i = 0; i < n; i++) sum += collapsed[i] ? HeaderHeight : MinHeight(column[i]);
            return sum;
        }
        if (!double.IsPositiveInfinity(height))
        {
            while (Need() > height + 1e-9)
            {
                // 畳む相手：開いている欄のうち、利用者が開いていない物を順位の数が大きい順（同じなら下の欄）、無ければ利用者が開いた物
                int pick = -1;
                for (int pass = 0; pass < 2 && pick < 0; pass++)
                    for (int i = 0; i < n; i++)
                    {
                        if (collapsed[i]) continue;
                        bool opened = userOpened?.Contains(column[i].Key) == true;
                        if ((pass == 0) == opened) continue;
                        if (pick < 0 || column[i].Rank >= column[pick].Rank) pick = i;
                    }
                if (pick < 0 || Enumerable.Range(0, n).Count(i => !collapsed[i]) <= 1) break;   // 最後の 1 つは畳まない（全部を畳むより順位の高い欄を狭めて見せる）
                collapsed[pick] = true;
            }
        }
        double spare = double.IsPositiveInfinity(height) ? 0 : Math.Max(0, height - Need());
        double weights = 0;
        for (int i = 0; i < n; i++) if (!collapsed[i]) weights += Math.Max(0, column[i].Weight);
        double over = double.IsPositiveInfinity(height) ? 0 : Math.Max(0, Need() - height);   // 開いている欄が 1 つになってもまだ入らない分（その欄を狭める）
        var result = new List<SectionPlace>(n);
        for (int i = 0; i < n; i++)
        {
            double h = collapsed[i] ? HeaderHeight : Math.Max(HeaderHeight, MinHeight(column[i]) + (weights > 0 ? spare * Math.Max(0, column[i].Weight) / weights : 0) - over);
            result.Add(new SectionPlace(column[i].Key, collapsed[i], h));
        }
        return result;
    }

    /// <summary>ページ全体の割り当て。</summary>
    public static IReadOnlyList<IReadOnlyList<SectionPlace>> Plan(PageTable t, double width, double height, ISet<string>? userOpened = null, ISet<string>? userClosed = null)
        => Arrange(t, ColumnCount(width, t)).Select(c => PlanColumn(c, height, userOpened, userClosed)).ToList();
}

/// <summary>ページの欄 1 つ＝見出しの 1 行（名前・開閉の印）と、その下の中身（<see cref="Body"/>）。中身は欄の中だけで縦に流れる（ページの外側は流れない）。見出しを押すと開閉する。
/// 開閉と高さは <see cref="PageGrid"/> が決める。</summary>
public sealed class Section : Border
{
    public static readonly StyledProperty<string> KeyProperty = AvaloniaProperty.Register<Section, string>(nameof(Key), "");
    public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<Section, string>(nameof(Title), "");
    public static readonly StyledProperty<bool> IsCollapsedProperty = AvaloniaProperty.Register<Section, bool>(nameof(IsCollapsed));
    public static readonly StyledProperty<bool> ScrollsProperty = AvaloniaProperty.Register<Section, bool>(nameof(Scrolls), true);


    public string Key { get => GetValue(KeyProperty); set => SetValue(KeyProperty, value); }
    public string Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public bool IsCollapsed { get => GetValue(IsCollapsedProperty); set => SetValue(IsCollapsedProperty, value); }
    /// <summary>中身を欄の中で縦に流すか（既定は流す）。偽なら中身は欄の大きさで測られる＝自分で見出しを止めて中身だけ流す表（TableView）や、決まった大きさの絵（地図）を置く欄に使う。</summary>
    public bool Scrolls { get => GetValue(ScrollsProperty); set => SetValue(ScrollsProperty, value); }

    /// <summary>欄の中身（XAML では子として書く）。</summary>
    [Avalonia.Metadata.Content]
    public Control? Body { get => _inner.Content as Control; set => _inner.Content = value; }

    /// <summary>見出しを押した。</summary>
    public event Action<Section>? HeaderPressed;

    private readonly TextBlock _glyph = new() { FontSize = 11, Text = "▾", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) };
    private readonly TextBlock _title = new() { FontSize = 13, VerticalAlignment = VerticalAlignment.Center };
    private readonly ScrollViewer _scroll = new() { VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    private readonly Border _body = new();   // 中身の入れ物（流す欄は ScrollViewer を、流さない欄は中身そのものを持つ）
    private readonly Avalonia.Controls.Presenters.ContentPresenter _inner = new() { Margin = new Thickness(8, 4, 12, 6) };   // 余白は中身の Margin（スクロールの Padding はスクロールの範囲に入らない）

    static Section()
    {
        IsCollapsedProperty.Changed.AddClassHandler<Section>((s, _) => s.Sync());
        ScrollsProperty.Changed.AddClassHandler<Section>((s, _) => s.MountBody());
        TitleProperty.Changed.AddClassHandler<Section>((s, _) => s._title.Text = s.Title);
    }

    public Section()
    {
        _title.Classes.Add("section");
        _glyph.Foreground = Brush("BronzeBrush", Brushes.SaddleBrown);
        MountBody();
        var header = new Border
        {
            Height = PagePlan.HeaderHeight, Padding = new Thickness(8, 0), Background = Brush("ParchmentDimBrush", Brushes.Beige), Cursor = new Cursor(StandardCursorType.Hand),
            Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { _glyph, _title } },
        };
        header.PointerPressed += (_, e) => { if (e.GetCurrentPoint(header).Properties.IsLeftButtonPressed) HeaderPressed?.Invoke(this); };
        var line = new Border { Height = 1, Background = Brush("SandDarkBrush", Brushes.Tan) };
        var dock = new DockPanel();
        DockPanel.SetDock(header, Dock.Top); DockPanel.SetDock(line, Dock.Top);
        dock.Children.Add(header); dock.Children.Add(line); dock.Children.Add(_body);
        Child = dock;
        BorderThickness = new Thickness(1); BorderBrush = Brush("SandBrush", Brushes.Tan); Background = Brush("ParchmentBrush", Brushes.Beige);
        ClipToBounds = true;
    }

    private void MountBody()
    {
        _scroll.Content = null; _body.Child = null;   // 中身の入れ物は同時に 1 か所にしか置けない
        if (Scrolls) { _scroll.Content = _inner; _body.Child = _scroll; }
        else _body.Child = _inner;
    }

    private void Sync()
    {
        _glyph.Text = IsCollapsed ? "▸" : "▾";
        _body.IsVisible = !IsCollapsed;
        _inner.IsVisible = !IsCollapsed;
    }

    private static IBrush Brush(string key, IBrush fallback)
        => Application.Current is { } app && app.TryFindResource(key, app.ActualThemeVariant, out var v) && v is IBrush b ? b : fallback;
}

/// <summary>ページの欄の並べ（設計 §2-5）。子は <see cref="Section"/>。<see cref="Table"/> の宣言に従い、幅で 3／2／1 列に置き、高さに入らなければ順位の低い欄から見出しの 1 行へ畳む。
/// 畳んだ見出しを押すと開き（代わりに順位の一番低い開いた欄が畳まれる）、開いている欄の見出しを押すと畳む。</summary>
public sealed class PageGrid : Panel
{
    public static readonly StyledProperty<PageTable?> TableProperty = AvaloniaProperty.Register<PageGrid, PageTable?>(nameof(Table));
    public PageTable? Table { get => GetValue(TableProperty); set => SetValue(TableProperty, value); }

    private readonly HashSet<string> _userOpened = new(), _userClosed = new();
    private IReadOnlyList<IReadOnlyList<SectionPlace>>? _lastPlan;
    /// <summary>直近の割り当て（撮影の検査用）。</summary>
    public IReadOnlyList<IReadOnlyList<SectionPlace>>? LastPlan => _lastPlan;

    static PageGrid()
    {
        AffectsMeasure<PageGrid>(TableProperty);
    }

    protected override void ChildrenChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        base.ChildrenChanged(sender, e);
        if (e.NewItems is not null) foreach (var c in e.NewItems.OfType<Section>()) { c.HeaderPressed -= OnHeader; c.HeaderPressed += OnHeader; }
    }

    /// <summary>欄を開く（問題の一覧とピン留めの飛び先が畳まれていたとき）。見出しを押して開いたのと同じ扱い（代わりに順位の一番低い開いた欄が畳まれる）。</summary>
    public void Reveal(string sectionKey)
    {
        _userClosed.Remove(sectionKey); _userOpened.Add(sectionKey);
        InvalidateMeasure();
        UserStateChanged?.Invoke();
    }

    /// <summary>プレイヤーが見出しを押して開け閉めした欄が変わった（配置の保存へ渡す）。</summary>
    public event Action? UserStateChanged;
    public IReadOnlyCollection<string> UserOpened => _userOpened;
    public IReadOnlyCollection<string> UserClosed => _userClosed;

    /// <summary>保存された開け閉めを戻す（ページの部品を作った直後）。</summary>
    public void SetUserState(IEnumerable<string> opened, IEnumerable<string> closed)
    {
        _userOpened.Clear(); _userClosed.Clear();
        foreach (var k in opened) _userOpened.Add(k);
        foreach (var k in closed) _userClosed.Add(k);
        InvalidateMeasure();
    }

    private void OnHeader(Section s)
    {
        if (s.IsCollapsed) { _userClosed.Remove(s.Key); _userOpened.Add(s.Key); }
        else { _userOpened.Remove(s.Key); _userClosed.Add(s.Key); }
        InvalidateMeasure();
        UserStateChanged?.Invoke();
    }

    protected override Size MeasureOverride(Size available)
    {
        if (Table is not { } t) { foreach (var c in Children) c.Measure(available); return default; }
        double w = double.IsPositiveInfinity(available.Width) ? 1200 : available.Width;
        double h = available.Height;
        var plan = PagePlan.Plan(t, w, h, _userOpened, _userClosed);
        _lastPlan = plan;
        int cols = plan.Count;
        double colW = (w - PagePlan.ColumnGap * (cols - 1)) / cols;
        var sections = Children.OfType<Section>().ToDictionary(s => s.Key);
        double used = 0;
        foreach (var col in plan)
        {
            double y = 0;
            foreach (var p in col)
            {
                if (sections.TryGetValue(p.Key, out var s))
                {
                    s.IsCollapsed = p.Collapsed;
                    s.Measure(new Size(colW, p.Height));
                }
                y += p.Height + PagePlan.SectionGap;
            }
            used = Math.Max(used, y - PagePlan.SectionGap);
        }
        foreach (var c in Children) if (c is not Section) c.Measure(default);
        return new Size(w, double.IsPositiveInfinity(h) ? used : Math.Min(h, Math.Max(used, 0)));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Table is not { } t || _lastPlan is not { } plan) { foreach (var c in Children) c.Arrange(new Rect(finalSize)); return finalSize; }
        int cols = plan.Count;
        double colW = (finalSize.Width - PagePlan.ColumnGap * (cols - 1)) / cols;
        var sections = Children.OfType<Section>().ToDictionary(s => s.Key);
        for (int i = 0; i < cols; i++)
        {
            double y = 0;
            foreach (var p in plan[i])
            {
                if (sections.TryGetValue(p.Key, out var s)) s.Arrange(new Rect(i * (colW + PagePlan.ColumnGap), y, colW, p.Height));
                y += p.Height + PagePlan.SectionGap;
            }
        }
        return finalSize;
    }
}
