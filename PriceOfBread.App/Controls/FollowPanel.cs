using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PriceOfBread.App.Controls;

/// <summary>縦の位置の計算（Avalonia に依らない）。選んだ行の上端にそろえ、下の端からはみ出す時は欄の中に収まる所まで上げる。</summary>
public static class FollowLayout
{
    /// <summary>中身の上端の位置。<paramref name="rowTop"/>＝選んだ行の上端（欄の座標・負や欄の外も入る）・null は先頭。結果は 0 以上で、欄の高さから中身の高さを引いた値以下（中身が欄より高ければ 0）。</summary>
    public static double Top(double? rowTop, double childHeight, double hostHeight)
    {
        double max = hostHeight - childHeight;
        if (!(max > 0)) return 0;
        double t = rowTop ?? 0;
        if (!(t > 0)) return 0;   // 否定形＝NaN と負は先頭
        return t < max ? t : max;
    }
}

/// <summary>1 つの子を持ち、その縦の位置だけを <see cref="RowTop"/> に従わせる欄の中身（法の表の選んだ行に付いて動く賛否の欄）。位置は描画の移動だけで動かす＝並べ直しを起こさない。</summary>
public sealed class FollowPanel : Panel
{
    private double? _rowTop;

    public FollowPanel() { ClipToBounds = true; }

    /// <summary>選んだ行の上端（この部品の座標）。</summary>
    public double? RowTop
    {
        get => _rowTop;
        set { if (_rowTop == value) return; _rowTop = value; ApplyOffset(); }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double h = 0;
        foreach (var c in Children) { c.Measure(new Size(availableSize.Width, double.PositiveInfinity)); h = Math.Max(h, c.DesiredSize.Height); }
        return new Size(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width, double.IsInfinity(availableSize.Height) ? h : availableSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var c in Children) c.Arrange(new Rect(0, 0, finalSize.Width, c.DesiredSize.Height));
        ApplyOffset(finalSize.Height);
        return finalSize;
    }

    private void ApplyOffset(double? hostHeight = null)
    {
        double host = hostHeight ?? Bounds.Height;
        foreach (var c in Children)
        {
            double y = FollowLayout.Top(_rowTop, c.Bounds.Height > 0 ? c.Bounds.Height : c.DesiredSize.Height, host);
            c.RenderTransform = new TranslateTransform(0, y);
        }
    }
}
