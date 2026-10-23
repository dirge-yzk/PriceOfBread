namespace PriceOfBread.SimCore.Display;

/// <summary>列の単位（書式は単位から決まる＝設計 §6-4）。専用の値型（<see cref="Units"/> の 9 型）は <see cref="Col.Of"/> が型から取る。
/// 専用の値型が無い物（人口・財の量・件数）は目録の側で名乗る。</summary>
public enum Unit
{
    MoneyStock, MoneyPerMonth, MoneyPerYear, Share, RatePerYear, RatePerMonth, RatePerQuarter, RealGdpPerMonth, RatioToGdpYear,
    People, Quantity, Count,
    /// <summary>財 1 単位の値段（£0.52 のような小さい金額を小数 2 桁で出す。月・年・在庫の別を持たない単価）。</summary>
    PricePerUnit,
}

/// <summary>段の押し釦 1 つの中身（法・税・支出の行の段。今の LawRowVM の段が持つ物をそのまま型にする）。</summary>
/// <summary><see cref="Labels"/> の添字 i はエンジン側の段番号 <paramref name="LevelBase"/> + i。<see cref="Current"/>・<see cref="Target"/> はエンジン側の段番号。</summary>
public sealed record StageCell(IReadOnlyList<string> Labels, int LevelBase, int Current, int Target, Func<int, bool> CanSelect, Func<int, string?> Hover, Action<int> Select)
{
    /// <summary>段の無い行の段の列（押し釦を 1 つも出さない）。</summary>
    public static StageCell None { get; } = new([], 0, 0, 0, _ => false, _ => null, _ => { });
}

/// <summary>行の動詞 1 つの中身（侵略・統一戦争・調停・補助金・専売・建設）。押した時に呼ぶのは本体の口だけ。
/// <paramref name="Tip"/>＝押し釦のホバーの文（押せない理由など。null か空なら出さない）。</summary>
public sealed record VerbCell(string Name, bool Enabled, Action Invoke, string? ConfirmText = null, string? Tip = null);

/// <summary>割合の内訳 1 つの中身（半円で描く。支配度の 4 区分・権力）。</summary>
public sealed record ShareCell(IReadOnlyList<(string Name, double Share)> Parts);

/// <summary>列＝名前と単位と読み口。説明文の欄は持たない（画面に説明文を書かない）。閾値の欄も持たない。</summary>
public abstract record Column<TRow>(string Key, string Name);
/// <summary><paramref name="Note"/>＝数の後ろに添える短い文字（価格比など。null か空なら添えない）。<paramref name="Tip"/>＝その数にカーソルを合わせた時の内訳の文（null なら出さない）。
/// <paramref name="Hide"/>＝真の行は値を出さない（空欄・数でない値の「—」とは別）。</summary>
public sealed record NumberColumn<TRow>(string Key, string Name, Unit Unit, Func<TRow, double> Read, string? ExplainKey = null, bool Series = false, Func<TRow, string>? Note = null,
                                        Func<TRow, string?>? Tip = null, Func<TRow, bool>? Hide = null) : Column<TRow>(Key, Name);
/// <summary><paramref name="Tip"/>＝その文字にカーソルを合わせた時の文（null なら文字そのもの）。</summary>
public sealed record TextColumn<TRow>(string Key, string Name, Func<TRow, string> Read, Func<TRow, string?>? Tip = null) : Column<TRow>(Key, Name);
public sealed record StageColumn<TRow>(string Key, string Name, Func<TRow, StageCell> Read) : Column<TRow>(Key, Name);
public sealed record VerbColumn<TRow>(string Key, string Name, Func<TRow, VerbCell> Read) : Column<TRow>(Key, Name);
public sealed record ShareColumn<TRow>(string Key, string Name, Func<TRow, ShareCell> Read) : Column<TRow>(Key, Name);

/// <summary>列の種類（共通の表が描き分ける）。</summary>
public enum ColumnKind { Number, Text, Stage, Verb, Share }

/// <summary>列の実行時の情報（行の型に依らない）。<see cref="Unit"/> は数の列だけ。<see cref="Series"/>＝この列は線グラフの系列になる。</summary>
public sealed record ColumnInfo(string Key, string Name, ColumnKind Kind, Unit? Unit, bool Series, string? ExplainKey);

/// <summary>数の列の 1 つの値（値と、後ろに添える短い文字と、カーソルを合わせた時の文）。<see cref="Blank"/>＝値を出さない。</summary>
public sealed record NumberCell(double Value, string? Note, string? Tip = null, bool Blank = false)
{
    public static NumberCell Empty { get; } = new(double.NaN, null, null, true);
}

/// <summary>文字の列の 1 つの値（文字と、カーソルを合わせた時の文）。</summary>
public sealed record TextCell(string Text, string? Tip);

/// <summary>行 1 本の実行時の値。<see cref="Cells"/> は列の並びのまま：数の列＝<see cref="NumberCell"/>・文字の列＝string・
/// 段の列＝<see cref="StageCell"/>・動詞の列＝<see cref="VerbCell"/>・半円の列＝<see cref="ShareCell"/>。</summary>
/// <summary><paramref name="Tip"/>＝行のホバーの文（null か空なら出さない）。<paramref name="Emphasis"/>＝自国の行など目立たせる行。<paramref name="GroupTip"/>＝群の見出しにカーソルを合わせた時の文（同じ群の行は同じ文）。</summary>
public sealed record RowData(string Key, string Name, string? Group, IReadOnlyList<object?> Cells, string? Tip = null, bool Emphasis = false, string? GroupTip = null);

/// <summary>表を読んだ結果（共通の表・Runner の報告・画面と仕様を知らない AI のテストプレイが同じ物を読む）。</summary>
/// <summary><paramref name="Page"/>＝この表が属するページ（ピン留めが元のページへ飛ぶ。表の持ち主が決まらない見本は null）。</summary>
public sealed record TableData(string Key, string Name, IReadOnlyList<ColumnInfo> Columns, IReadOnlyList<RowData> Rows, Page? Page = null);

/// <summary>行の型に依らない表の口（App の共通の表が型を知らずに読む）。</summary>
public interface ITable
{
    string Key { get; }
    string Name { get; }
    Page Page { get; }
    IReadOnlyList<ColumnInfo> ColumnInfos { get; }
    /// <summary>行を読んで値にする。盤面は読むだけ（書かない）。</summary>
    TableData Read(MultiNationSim sim, World w);
}

/// <summary>表＝行の一覧と列。行の鍵は据え置き同期（SyncRows）と並べ替えとピン留めの鍵。</summary>
public sealed record Table<TRow>(string Key, string Name, Page Page, Func<MultiNationSim, World, IReadOnlyList<TRow>> Rows,
                                 Func<TRow, string> RowKey, Func<TRow, string> RowName, IReadOnlyList<Column<TRow>> Columns,
                                 Func<TRow, string>? Group = null, Func<TRow, string?>? GroupTip = null) : ITable
{
    private static ColumnKind KindOf(Column<TRow> c) => c switch
    {
        NumberColumn<TRow> => ColumnKind.Number,
        TextColumn<TRow> => ColumnKind.Text,
        StageColumn<TRow> => ColumnKind.Stage,
        VerbColumn<TRow> => ColumnKind.Verb,
        ShareColumn<TRow> => ColumnKind.Share,
        _ => throw new NotSupportedException($"列の種類が未対応: {c.GetType().Name}"),
    };

    public IReadOnlyList<ColumnInfo> ColumnInfos => Columns.Select(c =>
        new ColumnInfo(c.Key, c.Name, KindOf(c), (c as NumberColumn<TRow>)?.Unit, (c as NumberColumn<TRow>)?.Series ?? false, (c as NumberColumn<TRow>)?.ExplainKey)).ToList();

    public TableData Read(MultiNationSim sim, World w)
    {
        var rows = Rows(sim, w);
        var list = new List<RowData>(rows.Count);
        foreach (var r in rows)
        {
            var cells = new object?[Columns.Count];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = Columns[i] switch
                {
                    NumberColumn<TRow> n => n.Hide?.Invoke(r) == true ? NumberCell.Empty : new NumberCell(n.Read(r), n.Note?.Invoke(r), n.Tip?.Invoke(r)),
                    TextColumn<TRow> t => t.Tip is null ? t.Read(r) : new TextCell(t.Read(r), t.Tip(r)),
                    StageColumn<TRow> s => s.Read(r),
                    VerbColumn<TRow> v => v.Read(r),
                    ShareColumn<TRow> sh => sh.Read(r),
                    _ => null,
                };
            list.Add(new RowData(RowKey(r), RowName(r), Group?.Invoke(r), cells, GroupTip: GroupTip?.Invoke(r)));
        }
        return new TableData(Key, Name, ColumnInfos, list, Page);
    }
}

/// <summary>列の作り口。型つきの数の列は単位を型から取る（Unit を手で書かない）。</summary>
public static class Col
{
    /// <summary>専用の値型 → 単位。9 型のどれでもなければ null（呼び手が <see cref="Unit"/> を名乗る）。</summary>
    public static Unit? UnitOf(Type t)
    {
        if (t == typeof(MoneyStock)) return Unit.MoneyStock;
        if (t == typeof(MoneyPerMonth)) return Unit.MoneyPerMonth;
        if (t == typeof(MoneyPerYear)) return Unit.MoneyPerYear;
        if (t == typeof(Share)) return Unit.Share;
        if (t == typeof(RatePerYear)) return Unit.RatePerYear;
        if (t == typeof(RatePerMonth)) return Unit.RatePerMonth;
        if (t == typeof(RatePerQuarter)) return Unit.RatePerQuarter;
        if (t == typeof(RealGdpPerMonth)) return Unit.RealGdpPerMonth;
        if (t == typeof(RatioToGdpYear)) return Unit.RatioToGdpYear;
        return null;
    }

    /// <summary>型つきの数の列（単位は型から）。型が 9 つのどれでもなければ例外＝<see cref="Number"/> で単位を名乗る。</summary>
    public static NumberColumn<TRow> Of<TRow, TUnit>(string key, string name, Func<TRow, TUnit> read, string? explainKey = null, bool series = false)
        where TUnit : struct, IUnitValue
        => new(key, name, UnitOf(typeof(TUnit)) ?? throw new ArgumentException($"単位の型でない: {typeof(TUnit).Name}"), r => read(r).Value, explainKey, series);

    /// <summary>数の後ろに短い文字を添える数の列（単位は名乗る）。</summary>
    public static NumberColumn<TRow> NumberNote<TRow>(string key, string name, Unit unit, Func<TRow, double> read, Func<TRow, string> note, Func<TRow, string?>? tip = null)
        => new(key, name, unit, read, Note: note, Tip: tip);

    /// <summary>型の無い数の列（人口・財の量・件数・型の無い割合など）。単位は名乗る。</summary>
    public static NumberColumn<TRow> Number<TRow>(string key, string name, Unit unit, Func<TRow, double> read, string? explainKey = null, bool series = false,
                                                  Func<TRow, string?>? tip = null, Func<TRow, bool>? hide = null)
        => new(key, name, unit, read, explainKey, series, Tip: tip, Hide: hide);

    public static TextColumn<TRow> Text<TRow>(string key, string name, Func<TRow, string> read, Func<TRow, string?>? tip = null) => new(key, name, read, tip);

    /// <summary>段の押し釦の列。</summary>
    public static StageColumn<TRow> Stage<TRow>(string key, string name, Func<TRow, StageCell> read) => new(key, name, read);

    /// <summary>割合の内訳（半円）の列。</summary>
    public static ShareColumn<TRow> Share<TRow>(string key, string name, Func<TRow, ShareCell> read) => new(key, name, read);
}

/// <summary>列の名前の規則（設計 §6-7・§7-5）：漢字 1 字だけの名前とかっこの補足は置かない。番人が全ての目録の名前に当てる。</summary>
public static class DisplayNames
{
    /// <summary>規則に合うか（空でない・漢字 1 字だけでない・かっこを含まない）。</summary>
    public static bool IsValid(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        string t = name.Trim();
        if (t.Length == 1 && IsKanji(t[0])) return false;
        foreach (char c in t) if (c is '(' or ')' or '（' or '）') return false;
        return true;
    }

    private static bool IsKanji(char c) => (c >= '一' && c <= '鿿') || (c >= '㐀' && c <= '䶿') || (c >= '豈' && c <= '﫿');
}
