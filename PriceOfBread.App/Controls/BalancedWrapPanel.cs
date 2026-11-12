using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace PriceOfBread.App.Controls;

/// <summary>折り返す横並び。幅が足りずに折り返す時、最後の行に釦が 1 つだけ落ちない（5 つ並べて 4＋1 でなく 3＋2 に）ように、各行の個数をそろえる。
/// 最後の子が文字（段の押せない理由）なら、釦とは別に扱い、最後の行に入れば右に・入らなければ次の行に置く。</summary>
public sealed class BalancedWrapPanel : Panel
{
    private readonly List<(Control Child, int Row, double X)> _place = new();
    private readonly List<double> _rowHeights = new();

    protected override Size MeasureOverride(Size availableSize)
    {
        var kids = Children.Where(c => c.IsVisible).ToList();
        foreach (var c in kids) c.Measure(Size.Infinity);
        _place.Clear(); _rowHeights.Clear();
        if (kids.Count == 0) return default;
        var tail = kids.Count > 1 && kids[^1] is TextBlock ? kids[^1] : null;
        var items = tail is null ? kids : kids.Take(kids.Count - 1).ToList();
        double w = double.IsInfinity(availableSize.Width) ? items.Sum(c => c.DesiredSize.Width) : availableSize.Width;

        // 貪欲に折り返した行数
        var rowsGreedy = new List<List<Control>> { new() };
        double x = 0;
        foreach (var c in items)
        {
            double cw = c.DesiredSize.Width;
            if (rowsGreedy[^1].Count > 0 && x + cw > w + 0.01) { rowsGreedy.Add(new()); x = 0; }
            rowsGreedy[^1].Add(c); x += cw;
        }
        var rows = rowsGreedy;
        int r = rowsGreedy.Count;
        if (r > 1)
        {
            // 行数は変えずに、1 行の個数を (全体 ÷ 行数) の切り上げにそろえる。入らなければ貪欲の折り返しのまま
            int per = (int)Math.Ceiling(items.Count / (double)r);
            var even = new List<List<Control>>();
            for (int i = 0; i < items.Count; i += per) even.Add(items.Skip(i).Take(per).ToList());
            if (even.Count == r && even.All(row => row.Sum(c => c.DesiredSize.Width) <= w + 0.01)) rows = even;
        }
        double maxW = 0, y = 0;
        for (int ri = 0; ri < rows.Count; ri++)
        {
            double rx = 0, rh = 0;
            foreach (var c in rows[ri]) { _place.Add((c, ri, rx)); rx += c.DesiredSize.Width; rh = Math.Max(rh, c.DesiredSize.Height); }
            if (tail is not null && ri == rows.Count - 1)
            {
                if (rx + tail.DesiredSize.Width <= w + 0.01) { _place.Add((tail, ri, rx)); rx += tail.DesiredSize.Width; rh = Math.Max(rh, tail.DesiredSize.Height); tail = null; }
            }
            _rowHeights.Add(rh); maxW = Math.Max(maxW, rx); y += rh;
        }
        if (tail is not null) { _place.Add((tail, rows.Count, 0)); _rowHeights.Add(tail.DesiredSize.Height); maxW = Math.Max(maxW, tail.DesiredSize.Width); y += tail.DesiredSize.Height; }
        return new Size(maxW, y);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var c in Children.Where(c => !c.IsVisible)) c.Arrange(default);
        var tops = new double[_rowHeights.Count + 1];
        for (int i = 0; i < _rowHeights.Count; i++) tops[i + 1] = tops[i] + _rowHeights[i];
        foreach (var (c, row, x) in _place)
            c.Arrange(new Rect(x, tops[row], c.DesiredSize.Width, _rowHeights[row]));
        return finalSize;
    }
}
