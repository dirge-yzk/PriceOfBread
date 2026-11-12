using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PriceOfBread.App.Controls;

/// <summary>線グラフ（設計 §6-7）。名前つきの系列の倉から点を受け、折れ線を 1 本描く。目盛りは数字だけ（最小と最大・説明文を書かない）。
/// ⚠Polyline の点が null だと毎フレーム例外で描画が止まる（2026-08-02 の事故）＝この部品は Polyline を使わず、描画の中で有限の点だけから線を組む
/// （null を返す経路が無い）。非有限は否定形 <c>!(v &gt; double.MinValue &amp;&amp; v &lt; double.MaxValue)</c> で落とす（Math.Clamp/Min/Max は NaN を除去しない）。</summary>
public sealed class LineChart : Control
{
    public static readonly StyledProperty<IReadOnlyList<double>?> ValuesProperty = AvaloniaProperty.Register<LineChart, IReadOnlyList<double>?>(nameof(Values));
    public static readonly StyledProperty<IBrush?> StrokeProperty = AvaloniaProperty.Register<LineChart, IBrush?>(nameof(Stroke));
    public static readonly StyledProperty<double> StrokeThicknessProperty = AvaloniaProperty.Register<LineChart, double>(nameof(StrokeThickness), 1.5);
    public static readonly StyledProperty<Func<double, string>?> LabelFormatProperty = AvaloniaProperty.Register<LineChart, Func<double, string>?>(nameof(LabelFormat));
    public static readonly StyledProperty<bool> ShowLabelsProperty = AvaloniaProperty.Register<LineChart, bool>(nameof(ShowLabels), true);

    static LineChart() => AffectsRender<LineChart>(ValuesProperty, StrokeProperty, StrokeThicknessProperty, LabelFormatProperty, ShowLabelsProperty);

    public IReadOnlyList<double>? Values { get => GetValue(ValuesProperty); set => SetValue(ValuesProperty, value); }
    public IBrush? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    public double StrokeThickness { get => GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }
    /// <summary>目盛りの数字の書式（単位から決まる書式を渡す）。null なら素の数。</summary>
    public Func<double, string>? LabelFormat { get => GetValue(LabelFormatProperty); set => SetValue(LabelFormatProperty, value); }
    public bool ShowLabels { get => GetValue(ShowLabelsProperty); set => SetValue(ShowLabelsProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
        => new(double.IsInfinity(availableSize.Width) ? 120 : availableSize.Width, double.IsInfinity(availableSize.Height) ? 40 : availableSize.Height);

    /// <summary>有限の点だけの最小と最大。有限の点が無ければ false。</summary>
    public static bool Range(IReadOnlyList<double> values, out double min, out double max)
    {
        min = double.MaxValue; max = double.MinValue; bool any = false;
        foreach (var v in values)
        {
            if (!(v > double.MinValue && v < double.MaxValue)) continue;
            any = true;
            if (v < min) min = v;
            if (v > max) max = v;
        }
        return any;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Values is not { Count: > 0 } values || Stroke is not { } stroke) return;
        if (!Range(values, out double min, out double max)) return;
        double w = Bounds.Width, h = Bounds.Height;
        if (!(w > 4 && h > 4)) return;
        double padL = 0;
        double span = max - min;
        bool flat = !(span > Math.Max(Math.Abs(max), 1.0) * 1e-9);   // 点が 1 つ・値が変わっていない＝上下の目盛りが同じ値になる
        if (ShowLabels)
        {
            var fmt = LabelFormat ?? (v => v.ToString("0.##", CultureInfo.InvariantCulture));
            var tf = new Typeface(UiRes.Font);
            IBrush ink = Application.Current?.TryFindResource("InkMutedBrush", out var b) == true && b is IBrush ib ? ib : Brushes.Gray;
            var top = new FormattedText(fmt(max), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, 10, ink);
            if (flat)
            {
                // 目盛りは 1 つ（線と同じ高さ）
                padL = Math.Min(w / 3, top.Width + 4);
                context.DrawText(top, new Point(0, Math.Max(0, (h - top.Height) / 2)));
            }
            else
            {
                var bottom = new FormattedText(fmt(min), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, 10, ink);
                padL = Math.Min(w / 3, Math.Max(top.Width, bottom.Width) + 4);
                context.DrawText(top, new Point(0, 0));
                context.DrawText(bottom, new Point(0, Math.Max(0, h - bottom.Height)));
            }
        }
        double x0 = padL + 1, x1 = w - 1, y0 = h - 2, y1 = 2;   // y0=最小の高さ・y1=最大の高さ
        int n = values.Count;
        var pen = new Pen(stroke, StrokeThickness, lineJoin: PenLineJoin.Round);
        if (flat)
        {
            // 値が変わっていない＝横 1 本（点が 1 つでも線が見える）
            context.DrawLine(pen, new Point(x0, (y0 + y1) / 2), new Point(x1, (y0 + y1) / 2));
            return;
        }
        var geo = new StreamGeometry();
        using (var g = geo.Open())
        {
            bool open = false;
            for (int i = 0; i < n; i++)
            {
                double v = values[i];
                if (!(v > double.MinValue && v < double.MaxValue)) { if (open) { g.EndFigure(false); open = false; } continue; }
                double x = n > 1 ? x0 + (x1 - x0) * i / (n - 1) : (x0 + x1) / 2;
                double y = span > 0 ? y0 - (y0 - y1) * (v - min) / span : (y0 + y1) / 2;
                if (!open) { g.BeginFigure(new Point(x, y), false); open = true; }
                else g.LineTo(new Point(x, y));
            }
            if (open) g.EndFigure(false);
        }
        context.DrawGeometry(null, pen, geo);
    }
}
