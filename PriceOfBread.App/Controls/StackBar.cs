using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.Controls;

/// <summary>積み上げの棒（表の棒の列）。入れ物（<see cref="BarCell.Full"/>）の枠を描き、項ごとの色で左から長さを積む。満ちれば入れ物いっぱい。</summary>
public sealed class StackBar : Control
{
    private BarCell? _cell;
    public BarCell? Cell { get => _cell; set { _cell = value; InvalidateVisual(); } }

    private static readonly IPen Frame = new Pen(new SolidColorBrush(Color.Parse("#8C7A5E")), 1);
    private static readonly IBrush Empty = new SolidColorBrush(Color.Parse("#EFE6D2"));

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        double w = Bounds.Width, h = Bounds.Height;
        if (!(w > 2 && h > 2)) return;
        var box = new Rect(0.5, 0.5, w - 1, h - 1);
        context.FillRectangle(Empty, box);
        if (_cell is { } c && c.Full > 0)
        {
            double x = box.X;
            foreach (var (v, color) in c.Parts)
            {
                if (!(v > 0)) continue;
                double len = Math.Min(box.Right - x, box.Width * v / c.Full);
                if (len <= 0) break;
                context.FillRectangle(new SolidColorBrush(Color.Parse(color)), new Rect(x, box.Y, len, box.Height));
                x += len;
            }
        }
        context.DrawRectangle(Frame, box);
    }
}
