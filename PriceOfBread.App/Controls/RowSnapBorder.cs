using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace PriceOfBread.App.Controls;

/// <summary>流れる（スクロールする）行の並びの入れ物。表示する高さを行の下端に揃える＝最後の行が半分で切れない（切り落とした 1 行に満たない分は下の空き）。
/// 行が全部入るとき・行の位置がまだ測られていないときは、そのまま（欄の高さいっぱい）。</summary>
public sealed class RowSnapBorder : Border
{
    /// <summary>行の部品（行の入れ物の子）。位置は入れ物の中の座標（上端が 0）。</summary>
    public Func<IEnumerable<Control>>? RowControls { get; set; }

    private Control? _watched;

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (RowControls is { } rows && Child is ScrollViewer sv && double.IsFinite(finalSize.Height))
        {
            double bottom = 0, best = 0; int n = 0; Control? last = null;
            foreach (var c in rows()) { n++; last = c; bottom = Math.Max(bottom, c.Bounds.Bottom); if (c.Bounds.Bottom <= finalSize.Height + 0.5 && c.Bounds.Bottom > best) best = c.Bounds.Bottom; }
            if (last is not null && !ReferenceEquals(last.Parent, _watched))
            {
                if (_watched is not null) _watched.LayoutUpdated -= OnRowsLayout;
                _watched = last.Parent as Control;
                if (_watched is not null) _watched.LayoutUpdated += OnRowsLayout;
            }
            if (n > 0 && bottom > finalSize.Height + 0.5 && best >= 1 && best < finalSize.Height - 0.5)
            {
                base.ArrangeOverride(new Size(finalSize.Width, best));
                return finalSize;
            }
        }
        return base.ArrangeOverride(finalSize);
    }

    private double _lastRowsHeight = -1;
    private void OnRowsLayout(object? sender, EventArgs e)
    {
        double h = _watched?.Bounds.Height ?? -1;
        if (Math.Abs(h - _lastRowsHeight) > 0.5) { _lastRowsHeight = h; InvalidateArrange(); }   // 行の位置が変わった時だけ（毎回は並べ直さない）
    }
}
