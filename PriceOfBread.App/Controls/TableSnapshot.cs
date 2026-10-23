using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.Controls;

/// <summary>共通の表が描く列の仕様（本体の目録の列に、描き方の幅を足した物）。
/// 幅は単位から既定の最小幅を決める（金額 7 文字分・割合 4 文字分など）。固定の幅でなく最小幅＝値を黙って切らない。</summary>
public sealed record ColumnSpec(string Key, string Name, ColumnKind Kind, Unit? Unit, double MinWidth, double MaxWidth);

/// <summary>セル 1 つ。<see cref="Text"/>＝画面の文字／<see cref="Sort"/>＝並べ替えの値（数の列は数・文字の列は NaN）／<see cref="Extra"/>＝段・動詞・半円の列の中身（<see cref="StageCell"/> ほか）。</summary>
public sealed record CellView(string Text, double Sort, object? Extra = null);

/// <summary>行 1 本。<see cref="Key"/> は据え置き同期の鍵（値だけ流す）。</summary>
public sealed record RowView(string Key, string Name, string? Group, IReadOnlyList<CellView> Cells);

/// <summary>共通の表へ渡す物（本体が渡した <see cref="TableData"/> を、単位から決まる書式で文字にした結果）。
/// Avalonia に依らない＝xUnit で読める。表を作り直さず、鍵で値だけ流す（SyncRows の規律）。</summary>
public sealed record TableSnapshot(string Key, string Name, IReadOnlyList<ColumnSpec> Columns, IReadOnlyList<RowView> Rows)
{
    public static TableSnapshot Empty { get; } = new("", "", [], []);

    /// <summary>単位から決まる最小幅（FontSmall=12px で半角の数字 約 6.65px・全角 12px）。</summary>
    public static double MinWidthOf(Unit unit) => unit switch
    {
        Unit.MoneyStock or Unit.MoneyPerMonth or Unit.MoneyPerYear or Unit.RealGdpPerMonth => 48,   // 「£1.2k/月」7 文字分
        Unit.Share or Unit.RatePerMonth => 28,                                                   // 「100%」4 文字分
        Unit.RatePerYear or Unit.RatePerQuarter => 46,
        Unit.RatioToGdpYear => 70,
        Unit.People or Unit.Quantity => 40,
        Unit.Count => 24,
        _ => 24,
    };

    /// <summary>本体の表の値を文字にする。数の列は <see cref="Fmt.Of"/>（単位から）＋添え書き・文字の列はそのまま・その他の列は <see cref="CellView.Extra"/> に中身を持つ。</summary>
    public static TableSnapshot From(TableData data)
    {
        var cols = new List<ColumnSpec>(data.Columns.Count);
        bool firstText = true;
        foreach (var c in data.Columns)
        {
            double min = c.Kind switch
            {
                ColumnKind.Number => MinWidthOf(c.Unit ?? Unit.Count),
                ColumnKind.Text => firstText ? 40 : 24,
                _ => 24,
            };
            // 最初の文字の列は残りの幅を全部取る（最大幅なし）。2 つ目以降の文字の列は長い名前で他を圧迫しない（末尾を省略記号）。
            double max = c.Kind == ColumnKind.Text ? (firstText ? double.PositiveInfinity : 96) : double.PositiveInfinity;
            if (c.Kind == ColumnKind.Text) firstText = false;
            cols.Add(new ColumnSpec(c.Key, c.Name, c.Kind, c.Unit, min, max));
        }
        var rows = new List<RowView>(data.Rows.Count);
        foreach (var r in data.Rows)
        {
            var cells = new CellView[cols.Count];
            for (int i = 0; i < cells.Length; i++)
            {
                object? v = i < r.Cells.Count ? r.Cells[i] : null;
                cells[i] = v switch
                {
                    NumberCell n => new CellView(Fmt.Of(cols[i].Unit ?? Unit.Count, n.Value) + (n.Note ?? ""), n.Value),
                    string s => new CellView(s, double.NaN),
                    StageCell st => new CellView(st.Current - st.LevelBase >= 0 && st.Current - st.LevelBase < st.Labels.Count ? st.Labels[st.Current - st.LevelBase] : "", st.Current, st),
                    VerbCell vb => new CellView(vb.Name, double.NaN, vb),
                    ShareCell sh => new CellView(string.Join(" ", sh.Parts.Select(p => $"{p.Name} {Fmt.Of(Unit.Share, p.Share)}")), sh.Parts.Count > 0 ? sh.Parts[0].Share : double.NaN, sh),
                    _ => new CellView("", double.NaN),
                };
            }
            rows.Add(new RowView(r.Key, r.Name, r.Group, cells));
        }
        return new TableSnapshot(data.Key, data.Name, cols, rows);
    }

    /// <summary>並べ替えと絞り込みを掛けた行（表の描き方が使う）。<paramref name="sortColumn"/> が負なら本体の並びのまま。数の列は数で・他は文字で、同値は元の並びのまま。</summary>
    public IReadOnlyList<RowView> View(string? filter, int sortColumn, bool descending)
    {
        IEnumerable<RowView> q = Rows;
        if (!string.IsNullOrWhiteSpace(filter))
        {
            string f = filter.Trim();
            q = q.Where(r => r.Name.Contains(f, StringComparison.OrdinalIgnoreCase));
        }
        if (sortColumn >= 0 && sortColumn < Columns.Count)
        {
            int ci = sortColumn;
            bool numeric = Columns[ci].Kind is ColumnKind.Number or ColumnKind.Stage;
            var rows = q.ToList();
            var groupOrder = new Dictionary<string, int>();   // 群は本体の並び（最初に現れた順）のまま・群の中だけ並べ替える
            foreach (var r in rows) groupOrder.TryAdd(r.Group ?? "", groupOrder.Count);
            var list = rows.Select((r, i) => (r, i)).ToList();
            list.Sort((a, b) =>
            {
                int byGroup = groupOrder[a.r.Group ?? ""].CompareTo(groupOrder[b.r.Group ?? ""]);
                if (byGroup != 0) return byGroup;
                int c = numeric ? CompareNum(a.r.Cells[ci].Sort, b.r.Cells[ci].Sort, descending) : (descending ? -1 : 1) * string.CompareOrdinal(a.r.Cells[ci].Text, b.r.Cells[ci].Text);
                return c != 0 ? c : a.i.CompareTo(b.i);
            });
            return list.Select(t => t.r).ToList();
        }
        return q.ToList();
    }

    // 数でない値（NaN）は昇順でも降順でも常に末尾。比べは否定形で書く（NaN は大小の比べがすべて偽）。
    private static int CompareNum(double x, double y, bool descending)
    {
        bool nx = !(x > double.MinValue && x < double.MaxValue), ny = !(y > double.MinValue && y < double.MaxValue);
        if (nx || ny) return nx == ny ? 0 : nx ? 1 : -1;
        int c = x < y ? -1 : x > y ? 1 : 0;
        return descending ? -c : c;
    }
}
