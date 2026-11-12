using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace PriceOfBread.App.Controls;

/// <summary>ページの欄 1 つの宣言（設計 §2-5）。最小の行数はその欄に最低限見せる行数。重みは余った高さの分け方（欄どうしの比）。</summary>
public sealed record SectionSpec(string Key, string Title, int MinRows = 6, double Weight = 1);

/// <summary>ページの並べの表（設計 §2-5・§6-9）＝ページごとに 1 つだけ手で書く。欄の宣言と、幅が 3 列のときの欄の置き場（列ごとに上から下の順）。
/// 2 列・1 列のときは 3 列の並びを左の列から順に下へ積んだ列を、最小の高さの合計が半分になる所で割る（表に書かない）。</summary>
/// <param name="ColumnMinWidth">1 列の最小の幅。null（既定）なら幅で決める（1200 以上＝3 列・700 以上＝2 列）。幅の広い表を持つページは、列を増やしすぎない最小の幅を宣言する
/// （列の数＝幅 ÷ 最小の幅・1〜3 の間・置き場の列の数を超えない）。</param>
/// <param name="ColumnWeights">3 列のときの列の幅の比。null（既定）なら均等。幅の広い表を持つ欄がある列だけを広げる（列の数は変えずに、広い表を欄の外へ押し出さない）。</param>
public sealed record PageTable(IReadOnlyList<SectionSpec> Sections, IReadOnlyList<IReadOnlyList<string>> Columns3, double? ColumnMinWidth = null, IReadOnlyList<double>? ColumnWeights = null);

/// <summary>欄 1 つの割り当て＝高さ（見出しを含む）。</summary>
public readonly record struct SectionPlace(string Key, double Height);

/// <summary>ページの欄の並べの計算（Avalonia に依らない＝番人から直接見る）。
/// <list type="number">
/// <item>幅で列の数を決める（1200 以上＝3・700 以上＝2・それ未満＝1）。</item>
/// <item>欄を列へ置く（<see cref="Arrange"/>）。</item>
/// <item>列の高さに最小の高さの合計が入らなければ、全ての欄を同じ比で狭める（欄は常に開いている・中身は欄の中で流れる）。</item>
/// <item>余りの高さは欄へ重みの比で分ける。</item>
/// </list></summary>
public static class PagePlan
{
    public const double HeaderHeight = 22;   // 欄の見出しの 1 行
    public const double RowHeight = 22;      // 行の高さの見積り（最小の行数 × これ＝最小の高さ）
    public const double ColumnGap = 4, SectionGap = 4;   // 欄どうしの隙間（作者の指示 2026-11-12：空白を詰める）
    public const double BodyPadV = 4;                    // 欄の中身の上下の余白の合計（Section の中身の Margin の上 2＋下 2）

    public static int ColumnCount(double width) => width >= 1200 ? 3 : width >= 700 ? 2 : 1;

    /// <summary>表の宣言（<see cref="PageTable.ColumnMinWidth"/>）を踏まえた列の数。</summary>
    public static int ColumnCount(double width, PageTable t)
    {
        if (t.ColumnMinWidth is not { } min || !(min > 0)) return ColumnCount(width);
        int n = (int)Math.Floor((width + ColumnGap) / (min + ColumnGap));
        return Math.Clamp(n, 1, Math.Max(1, Math.Min(3, t.Columns3.Count)));
    }

    public static double MinHeight(SectionSpec s) => HeaderHeight + Math.Max(0, s.MinRows) * RowHeight;

    /// <summary>最小の高さ。中身の高さの上限（<paramref name="caps"/>）が小さい欄は、その上限まで（中身より高く確保せず・最後の行も切らない・A10・A11）。</summary>
    private static double MinHeight(SectionSpec s, IReadOnlyDictionary<string, double>? caps, IReadOnlyDictionary<string, double>? chrome = null)
    {
        double min = MinHeight(s);
        if (caps is null || !caps.TryGetValue(s.Key, out double cap)) return min;
        // 中身が最小の高さに近い（全部の行が収まる）なら中身の全部を最小にする＝最後の行が半分に切れない（A11）。
        // 中身が多い欄は最小の行数のまま（表の見出しと絞り込みの欄の分だけ足す＝最小の行数はデータの行数）
        if (cap <= min + 3 * RowHeight) return Math.Max(HeaderHeight, cap);
        return chrome is not null && chrome.TryGetValue(s.Key, out double ch) ? min + ch : min;
    }

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

    /// <summary>1 つの列の割り当て。<paramref name="height"/> が無限大なら最小の高さのまま。</summary>
    /// <param name="chrome">表の見出し・絞り込みの欄の高さ（最小の行数に足す＝データの行が最低でも最小の行数だけ見える）。</param>
    /// <param name="caps">欄ごとの高さの上限（中身に合わせる。見出しを含む）。余りの高さをこの上限を超えて分けない（超えた分は他の欄へ回し、全部が上限なら空ける）。</param>
    public static IReadOnlyList<SectionPlace> PlanColumn(IReadOnlyList<SectionSpec> column, double height, IReadOnlyDictionary<string, double>? caps = null, IReadOnlyDictionary<string, double>? chrome = null)
    {
        int n = column.Count;
        var mins = new double[n];
        double gaps = Math.Max(0, n - 1) * SectionGap, need = gaps;
        for (int i = 0; i < n; i++) { mins[i] = MinHeight(column[i], caps, chrome); need += mins[i]; }
        bool finite = !double.IsPositiveInfinity(height);
        double spare = finite ? Math.Max(0, height - need) : 0;
        if (finite && need > height + 1e-9)
        {
            // 入らない：床（見出し＋1 行）まで、最小の高さを同じ比で狭める
            double floor = HeaderHeight + RowHeight, sumFloor = n * floor, sumMin = mins.Sum(), room = height - gaps;
            double k = sumMin - sumFloor > 1e-9 ? Math.Clamp((room - sumFloor) / (sumMin - sumFloor), 0, 1) : 0;
            for (int i = 0; i < n; i++) mins[i] = Math.Min(mins[i], floor + (mins[i] - floor) * k);
        }
        // 余りの高さを重みの比で分ける。上限（caps）に届いた欄はそこで止め、残りを他の欄へ回す
        var extra = new double[n];
        var capped = new bool[n];
        for (int round = 0; round < n && spare > 1e-9; round++)
        {
            double weights = 0;
            for (int i = 0; i < n; i++) if (!capped[i]) weights += Math.Max(0, column[i].Weight);
            if (!(weights > 0)) break;
            double given = 0; bool any = false;
            for (int i = 0; i < n; i++)
            {
                if (capped[i]) continue;
                double want = spare * Math.Max(0, column[i].Weight) / weights;
                double room = caps is not null && caps.TryGetValue(column[i].Key, out double cap) ? Math.Max(0, cap - mins[i] - extra[i]) : double.PositiveInfinity;
                if (want > room) { extra[i] += room; given += room; capped[i] = true; any = true; }
                else { extra[i] += want; given += want; }
            }
            spare -= given;
            if (!any) break;
        }
        var result = new List<SectionPlace>(n);
        for (int i = 0; i < n; i++) result.Add(new SectionPlace(column[i].Key, Math.Max(HeaderHeight, mins[i] + extra[i])));
        return result;
    }

    /// <summary>列ごとの幅。重みの宣言があればその比・無ければ均等割り。</summary>
    public static double[] ColumnWidths(int columns, double width, IReadOnlyList<double>? weights = null)
    {
        var result = new double[columns];
        if (columns == 0) return result;
        double avail = Math.Max(0, width - ColumnGap * (columns - 1));
        if (weights is { } w && w.Count == columns && w.All(x => x > 0)) { double sum = w.Sum(); for (int i = 0; i < columns; i++) result[i] = avail * w[i] / sum; }
        else Array.Fill(result, avail / columns);
        return result;
    }

    /// <summary>ページ全体の割り当て。</summary>
    public static IReadOnlyList<IReadOnlyList<SectionPlace>> Plan(PageTable t, double width, double height, IReadOnlyDictionary<string, double>? caps = null, IReadOnlyDictionary<string, double>? chrome = null)
        => Arrange(t, ColumnCount(width, t)).Select(c => PlanColumn(c, height, caps, chrome)).ToList();
}

/// <summary>ページの欄 1 つ＝見出しの 1 行（名前）と、その下の中身（<see cref="Body"/>）。中身は欄の中だけで縦に流れる（ページの外側は流れない）。高さは <see cref="PageGrid"/> が決める。</summary>
public sealed class Section : Border
{
    public static readonly StyledProperty<string> KeyProperty = AvaloniaProperty.Register<Section, string>(nameof(Key), "");
    public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<Section, string>(nameof(Title), "");
    public static readonly StyledProperty<bool> ScrollsProperty = AvaloniaProperty.Register<Section, bool>(nameof(Scrolls), true);


    public string Key { get => GetValue(KeyProperty); set => SetValue(KeyProperty, value); }
    public string Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    /// <summary>中身を欄の中で縦に流すか（既定は流す）。偽なら中身は欄の大きさで測られる＝自分で見出しを止めて中身だけ流す表（TableView）や、決まった大きさの絵（地図）を置く欄に使う。</summary>
    public bool Scrolls { get => GetValue(ScrollsProperty); set => SetValue(ScrollsProperty, value); }

    /// <summary>欄の中身（XAML では子として書く）。</summary>
    [Avalonia.Metadata.Content]
    public Control? Body
    {
        get => _inner.Content as Control;
        set
        {
            if (_inner.Content is TableView old) old.SnapshotChanged -= OnBodyTableChanged;
            _inner.Content = value;
            if (value is TableView tv) tv.SnapshotChanged += OnBodyTableChanged;
        }
    }

    /// <summary>表の中身が入れ替わった（0 行↔行あり）。欄の空と高さの上限が変わるので、並べ直す（中の流れが大きさの変化を吸うため、親の測り直しが届かない）。</summary>
    private void OnBodyTableChanged() => (Parent as PageGrid)?.InvalidateMeasure();

    private readonly TextBlock _title = new() { FontSize = 13, VerticalAlignment = VerticalAlignment.Center };
    private readonly ScrollViewer _scroll = new() { VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    private readonly Border _body = new();   // 中身の入れ物（流す欄は ScrollViewer を、流さない欄は中身そのものを持つ）
    private readonly Avalonia.Controls.Presenters.ContentPresenter _inner = new() { Margin = new Thickness(6, 2, 8, 2) };   // 余白は中身の Margin（スクロールの Padding はスクロールの範囲に入らない）

    static Section()
    {
        ScrollsProperty.Changed.AddClassHandler<Section>((s, _) => s.MountBody());
        TitleProperty.Changed.AddClassHandler<Section>((s, _) => LatinText.Fill(s._title, s.Title, Brush("GrayBrush", Brushes.Gray)));   // 見出しの中の数字は本文の灰（見出しの字は淡い灰・点検⑯）
    }

    public Section()
    {
        _title.Classes.Add("section");
        MountBody();
        var header = new Border
        {
            Height = PagePlan.HeaderHeight, Padding = new Thickness(6, 0), Background = Brush("TraceBrush", Brushes.Beige),
            Child = _title,
        };
        var line = new Border { Height = 1, Background = Brush("LineBrush", Brushes.Tan) };
        var dock = new DockPanel();
        DockPanel.SetDock(header, Dock.Top); DockPanel.SetDock(line, Dock.Top);
        dock.Children.Add(header); dock.Children.Add(line); dock.Children.Add(_body);
        Child = dock;
        BorderThickness = new Thickness(1); BorderBrush = Brush("LineBrush", Brushes.Tan); Background = Brush("PaperBrush", Brushes.Beige);
        ClipToBounds = true;
    }

    /// <summary>中身に合わせた高さの上限（見出しを含む・A10）。表（直接か、入れ物の最初の子）は行数からの見積り・他の流れる中身は幅 <paramref name="width"/> で測った自然な高さ。
    /// 流さない欄（自分で見出しを止める物）で表でなければ null＝上限なし。</summary>
    public double? ContentHeightCap(double width)
    {
        double chrome = PagePlan.HeaderHeight + 1 + PagePlan.BodyPadV;
        if (Body is TableView tv) return chrome + tv.EstimatedHeight();
        if (Body is Panel p && p.Children.OfType<TableView>().FirstOrDefault() is { } t)
            return chrome + t.EstimatedHeight() + p.Children.Where(c => c != t).Sum(c => c.DesiredSize.Height);   // 表の下の合計の行など
        if (!Scrolls || Body is null) return null;
        _inner.Measure(new Size(Math.Max(0, width - 2 - 10), double.PositiveInfinity));   // 欄の枠と縦の目盛りの帯の分を引く（中身の左右の余白は測りが引く）
        return chrome + _inner.DesiredSize.Height - 4;   // 中身の Margin は BodyPadV に含めて数え済み
    }

    private void MountBody()
    {
        _scroll.Content = null; _body.Child = null;   // 中身の入れ物は同時に 1 か所にしか置けない
        if (Scrolls) { _scroll.Content = _inner; _body.Child = _scroll; }
        else _body.Child = _inner;
    }

    private static IBrush Brush(string key, IBrush fallback)
        => Application.Current is { } app && app.TryFindResource(key, app.ActualThemeVariant, out var v) && v is IBrush b ? b : fallback;
}

/// <summary>ページの欄の並べ（設計 §2-5）。子は <see cref="Section"/>。<see cref="Table"/> の宣言に従い、幅で 3／2／1 列に置く。欄は常に開いている。</summary>
public sealed class PageGrid : Panel
{
    public static readonly StyledProperty<PageTable?> TableProperty = AvaloniaProperty.Register<PageGrid, PageTable?>(nameof(Table));
    public PageTable? Table { get => GetValue(TableProperty); set => SetValue(TableProperty, value); }

    private IReadOnlyList<IReadOnlyList<SectionPlace>>? _lastPlan;
    /// <summary>直近の割り当て（撮影の検査用）。</summary>
    public IReadOnlyList<IReadOnlyList<SectionPlace>>? LastPlan => _lastPlan;

    static PageGrid()
    {
        AffectsMeasure<PageGrid>(TableProperty);
    }

    /// <summary>画面に付いた直後にもう一度測る（欄の中の表がまだ中身を持たないうちに最初の測りが済み、高さの上限を見落とすことがある・A10）。</summary>
    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Avalonia.Threading.Dispatcher.UIThread.Post(InvalidateMeasure, Avalonia.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>欄ごとの高さの上限（中身に合わせる）と、表の見出し・絞り込みの高さ。幅 <paramref name="w"/> の列に割ったときの見積り。</summary>
    private (Dictionary<string, double> Caps, Dictionary<string, double> Chrome) CapsOf(PageTable t, double w)
    {
        var caps = new Dictionary<string, double>();
        var chrome = new Dictionary<string, double>();
        double colW = PagePlan.ColumnWidths(PagePlan.ColumnCount(w, t), w, t.ColumnWeights).Min();
        foreach (var s in Children.OfType<Section>())
        {
            if (s.ContentHeightCap(colW) is double cap) caps[s.Key] = cap;
            if (s.Body is TableView tv) chrome[s.Key] = tv.ChromeHeight();
        }
        return (caps, chrome);
    }

    /// <summary>幅 <paramref name="width"/> に置いたとき、中身がちょうど収まる高さ（列ごとに欄の中身の高さを積んだ最大）。窓の初めの高さに使う。</summary>
    public double FitHeight(double width)
    {
        if (Table is not { } t) return 0;
        var (caps, _) = CapsOf(t, width);
        return PagePlan.Arrange(t, PagePlan.ColumnCount(width, t))
            .Max(col => col.Sum(s => caps.TryGetValue(s.Key, out double c) ? c : PagePlan.MinHeight(s)) + Math.Max(0, col.Count - 1) * PagePlan.SectionGap);
    }

    protected override Size MeasureOverride(Size available)
    {
        if (Table is not { } t) { foreach (var c in Children) c.Measure(available); return default; }
        double w = double.IsPositiveInfinity(available.Width) ? 1200 : available.Width;
        double h = available.Height;
        var (caps, chrome) = CapsOf(t, w);
        var plan = PagePlan.Plan(t, w, h, caps, chrome);
        _lastPlan = plan;
        int cols = plan.Count;
        var colWs = PagePlan.ColumnWidths(plan.Count, w, t.ColumnWeights);
        var sections = Children.OfType<Section>().ToDictionary(s => s.Key);
        double used = 0; int ci = 0;
        foreach (var col in plan)
        {
            double colW = colWs[ci++];
            double y = 0;
            foreach (var p in col)
            {
                if (sections.TryGetValue(p.Key, out var s))
                {
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
        var colWs = PagePlan.ColumnWidths(plan.Count, finalSize.Width, t.ColumnWeights);
        var sections = Children.OfType<Section>().ToDictionary(s => s.Key);
        double x = 0;
        for (int i = 0; i < cols; i++)
        {
            double y = 0, colW = colWs[i], x0 = x;
            x += colW + PagePlan.ColumnGap;
            foreach (var p in plan[i])
            {
                if (sections.TryGetValue(p.Key, out var s)) s.Arrange(new Rect(x0, y, colW, p.Height));
                y += p.Height + PagePlan.SectionGap;
            }
        }
        return finalSize;
    }
}
