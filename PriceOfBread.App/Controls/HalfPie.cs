using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PriceOfBread.App.Controls;

/// <summary>半円の扇形 1 枚（割合 0〜1・色）。割合の和は 1 以下（残りは描かない）。</summary>
public readonly record struct HalfPieSlice(double Fraction, IBrush Brush);

/// <summary>半円のグラフ（作者指示 2026-09-07「円グラフより半円グラフの方が可視性が高い」）。割合の内訳を左端（9 時）から時計回りに頂点を通って右端（3 時）へ並べる。
/// 箱は幅＝2r＋4・高さ＝r＋4（設計 §6-7）。扇形の Path データは <see cref="Slice"/> の 1 本（地図の州パネルと政治タブも同じ関数を呼ぶ）。</summary>
public sealed class HalfPie : Control
{
    public static readonly StyledProperty<IReadOnlyList<HalfPieSlice>?> SlicesProperty = AvaloniaProperty.Register<HalfPie, IReadOnlyList<HalfPieSlice>?>(nameof(Slices));
    public static readonly StyledProperty<double> RadiusProperty = AvaloniaProperty.Register<HalfPie, double>(nameof(Radius), 34.0);

    static HalfPie()
    {
        AffectsMeasure<HalfPie>(RadiusProperty);
        AffectsRender<HalfPie>(SlicesProperty, RadiusProperty);
    }

    public IReadOnlyList<HalfPieSlice>? Slices { get => GetValue(SlicesProperty); set => SetValue(SlicesProperty, value); }
    public double Radius { get => GetValue(RadiusProperty); set => SetValue(RadiusProperty, value); }

    /// <summary>箱の大きさ（幅＝2r＋4・高さ＝r＋4）。</summary>
    public static Size BoxOf(double radius) => new(2 * radius + 4, radius + 4);

    protected override Size MeasureOverride(Size availableSize) => BoxOf(!(Radius > 0) ? 0 : Radius);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        double r = !(Radius > 0) ? 0 : Radius;
        if (!(r > 0) || Slices is not { Count: > 0 } slices) return;
        double cx = r + 2, cy = r + 2, f = 0;
        foreach (var s in slices)
        {
            double frac = !(s.Fraction > 0) ? 0 : s.Fraction;   // ⚠Math.Clamp は NaN を落とさない＝否定形で書く
            string d = Slice(cx, cy, r, f, Math.Min(1.0, f + frac));
            f += frac;
            if (d.Length == 0) continue;
            context.DrawGeometry(s.Brush, null, Geometry.Parse(d));
        }
    }

    /// <summary>★半円の扇形の Path データ（SVG 記法）：底辺の中点 cx,cy・半径 r・開始 frac0〜終了 frac1∈[0,1]。
    /// 左端（9時）を0・右端（3時）を1として頂点を通り時計回り＝合計1で上半分の半円が閉じる。弧は常に180°以下＝large-arc は0。
    /// ⚠ほぼ全部（>0.999）は半円で描く。ほぼ0（<0.001）は空＝描かない。</summary>
    public static string Slice(double cx, double cy, double r, double f0, double f1)
    {
        double span = f1 - f0;
        if (!(span > 0.001)) return "";
        if (span > 0.999) return FormattableString.Invariant($"M {cx - r},{cy} A {r},{r} 0 0 1 {cx + r},{cy} Z");
        double a0 = f0 * Math.PI, a1 = f1 * Math.PI;
        double x0 = cx - r * Math.Cos(a0), y0 = cy - r * Math.Sin(a0);
        double x1 = cx - r * Math.Cos(a1), y1 = cy - r * Math.Sin(a1);
        return FormattableString.Invariant($"M {cx},{cy} L {x0:F2},{y0:F2} A {r},{r} 0 0 1 {x1:F2},{y1:F2} Z");
    }

    /// <summary>★半円の外周の帯（線だけ・塗らない）：0〜frac の弧。州パネルの「国家区分の10年前」に使う。</summary>
    public static string Arc(double cx, double cy, double r, double frac)
    {
        if (!(frac > 0.001)) return "";
        double a1 = Math.Min(1.0, frac) * Math.PI;
        double x1 = cx - r * Math.Cos(a1), y1 = cy - r * Math.Sin(a1);
        return FormattableString.Invariant($"M {cx - r},{cy} A {r},{r} 0 0 1 {x1:F2},{y1:F2}");
    }
}
