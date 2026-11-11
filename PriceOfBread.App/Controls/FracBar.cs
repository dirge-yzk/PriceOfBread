using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PriceOfBread.App.Controls;

/// <summary>割合の棒。溝の全幅を 1 として、<see cref="Frac"/>（0 から 1）の長さを左から塗る＝溝の幅が変わっても割合が長さに比例する。</summary>
public sealed class FracBar : Control
{
    public static readonly StyledProperty<double> FracProperty = AvaloniaProperty.Register<FracBar, double>(nameof(Frac));
    public static readonly StyledProperty<IBrush?> FillProperty = AvaloniaProperty.Register<FracBar, IBrush?>(nameof(Fill));
    public static readonly StyledProperty<IBrush?> TrackProperty = AvaloniaProperty.Register<FracBar, IBrush?>(nameof(Track));

    static FracBar() => AffectsRender<FracBar>(FracProperty, FillProperty, TrackProperty);

    public double Frac { get => GetValue(FracProperty); set => SetValue(FracProperty, value); }
    public IBrush? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }
    public IBrush? Track { get => GetValue(TrackProperty); set => SetValue(TrackProperty, value); }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        double w = Bounds.Width, h = Bounds.Height;
        if (!(w > 0 && h > 0)) return;
        var box = new Rect(0, 0, w, h);
        if (Track is { } t) context.DrawRectangle(t, null, box, 2, 2);
        double f = Frac;
        if (!(f > 0)) return;
        double len = Math.Max(1.0, Math.Min(1.0, f) * w);
        if (Fill is { } b) context.DrawRectangle(b, null, new Rect(0, 0, len, h), 2, 2);
    }
}
