using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.Controls;

/// <summary>年齢の層の横棒（設計 §3-2）。年齢の層を 1 行ずつ、上が高齢・下が若年で並べ、中央の名前の左に男性・右に女性の人数を棒で描く（最大の棒が箱の半分を使い切る）。
/// 層の数と名前は本体の目録（<see cref="AgePyramid"/>）が決める＝層の数を決め打ちしない。非有限の値は 0 の棒にする（否定形で落とす）。
/// 数字は棒の外側（入らなければ棒の中）に人数（K 単位）で出す。説明文は書かない。</summary>
public sealed class AgeBars : Control
{
    public static readonly StyledProperty<AgePyramid?> DataProperty = AvaloniaProperty.Register<AgeBars, AgePyramid?>(nameof(Data));

    static AgeBars() => AffectsRender<AgeBars>(DataProperty);

    public AgePyramid? Data { get => GetValue(DataProperty); set => SetValue(DataProperty, value); }

    public const double RowHeight = 17, HeaderHeight = 16;

    protected override Size MeasureOverride(Size availableSize)
    {
        int n = Data?.Labels.Count ?? 0;
        return new Size(double.IsInfinity(availableSize.Width) ? 300 : availableSize.Width, HeaderHeight + n * RowHeight);
    }

    private static IBrush Brush(string key, IBrush fallback)
        => Application.Current is { } app && app.TryFindResource(key, app.ActualThemeVariant, out var v) && v is IBrush b ? b : fallback;

    private static double Clean(double v) => !(v > 0) || !(v < double.MaxValue) ? 0 : v;   // ⚠NaN・負・無限は棒にしない（Math.Clamp は NaN を落とさない）

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Data is not { Labels.Count: > 0 } d) return;
        double w = Bounds.Width;
        if (!(w > 80)) return;
        int n = d.Labels.Count;
        double max = 0;
        for (int i = 0; i < n; i++) max = Math.Max(max, Math.Max(Clean(d.Male[i]), Clean(d.Female[i])));
        var tf = new Typeface(FontFamily.Default);
        var ink = Brush("InkBrush", Brushes.Black); var muted = Brush("InkMutedBrush", Brushes.Gray);
        var male = Brush("BlueBrush", Brushes.SteelBlue); var female = Brush("RustBrush", Brushes.IndianRed);
        FormattedText Text(string s, IBrush b, double size = 11) => new(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, size, b);
        double labelW = 0;
        for (int i = 0; i < n; i++) labelW = Math.Max(labelW, Text(d.Labels[i], ink).Width);
        labelW += 10;
        double cx = w / 2, half = cx - labelW / 2;   // 中央の名前の左右に残る長さ
        // 凡例（男性と女性）
        var hm = Text("男性", male, 11); var hf = Text("女性", female, 11);
        context.DrawText(hm, new Point(cx - labelW / 2 - hm.Width, 0));
        context.DrawText(hf, new Point(cx + labelW / 2, 0));
        for (int i = 0; i < n; i++)
        {
            int band = n - 1 - i;   // 上が高齢
            double y = HeaderHeight + i * RowHeight;
            var label = Text(d.Labels[band], ink);
            context.DrawText(label, new Point(cx - label.Width / 2, y + (RowHeight - label.Height) / 2));
            DrawBar(context, Clean(d.Male[band]), max, half, cx - labelW / 2, y, true, male, Text, muted);
            DrawBar(context, Clean(d.Female[band]), max, half, cx + labelW / 2, y, false, female, Text, muted);
        }
    }

    private static void DrawBar(DrawingContext context, double value, double max, double half, double x0, double y, bool toLeft, IBrush brush,
        Func<string, IBrush, double, FormattedText> text, IBrush muted)
    {
        double len = max > 0 ? half * value / max : 0;
        double bar = RowHeight - 5;
        if (len > 0.5) context.DrawRectangle(brush, null, new Rect(toLeft ? x0 - len : x0, y + 2.5, len, bar));
        string s = PriceOfBread.App.Fmt.PopFmtK(value);
        var num = text(s, muted, 10);
        // 数字は棒の外側（入らなければ棒の内側＝紙の色で）
        double room = half - len;
        bool outside = room >= num.Width + 2;
        if (!outside) num = text(s, Brushes.White, 10);
        double nx = outside ? (toLeft ? x0 - len - 2 - num.Width : x0 + len + 2) : (toLeft ? x0 - len + 2 : x0 + len - 2 - num.Width);
        context.DrawText(num, new Point(nx, y + (RowHeight - num.Height) / 2));
    }
}
