using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.Views;

/// <summary>--uishot 専用の部品の見本（設計 §7-3）。共通の表・線グラフ・半円を、決まった見本の値で 1 枚に並べる＝部品の見た目を無人で確かめる。
/// 本体の盤面は読まない（固定の見本）。実プレイの経路には通らない。</summary>
internal static class UiShotParts
{
    private static IBrush B(string key, IBrush fallback)
        => Application.Current is { } app && app.TryFindResource(key, app.ActualThemeVariant, out var v) && v is IBrush b ? b : fallback;

    public static Control Build()
    {
        // 表：文字・数・数（添え書き）・半円・段・動詞の 6 種を群つきで
        string[] labels = ["なし", "低率", "中率", "高率"];
        var columns = new List<ColumnInfo>
        {
            new("name", "名前", ColumnKind.Text, null, false, null),
            new("amount", "金額", ColumnKind.Number, Unit.MoneyPerMonth, false, null),
            new("share", "割合", ColumnKind.Number, Unit.Share, false, null),
            new("rate", "年率", ColumnKind.Number, Unit.RatePerYear, false, null),
            new("rule", "内訳", ColumnKind.Share, null, false, null),
            new("stage", "段", ColumnKind.Stage, null, false, null),
            new("verb", "動詞", ColumnKind.Verb, null, false, null),
        };
        var rows = new List<RowData>();
        string[] names = ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "オスマン帝国", "日本", "アメリカ", "ネーデルラント", "スペイン"];
        for (int i = 0; i < names.Length; i++)
        {
            double a = (i + 1) * 913.7 * (i % 3 == 0 ? -1 : 1);
            var share = new ShareCell([("国家", 0.55 - i * 0.03), ("在地", 0.25), ("外国", 0.1), ("無統治", 0.1 + i * 0.03)]);
            var stage = new StageCell(labels, 0, i % 4, (i + 1) % 4, l => l != 3, l => l == 3 ? null : $"{names[0]}:{labels[l]}", _ => { });
            rows.Add(new RowData("r" + i, names[i], i < 5 ? "列強" : "その他", [names[i], new NumberCell(a, i == 1 ? "(×1.05)" : null), new NumberCell(0.04 * i - 0.001, null), new NumberCell(-0.0001 * i, null), share, stage, new VerbCell("侵略", i % 2 == 0, () => { })]));
        }
        var snap = TableSnapshot.From(new TableData("sample", "見本", columns, rows));

        var table = new TableView { Snapshot = snap, Filterable = true, InnerScroll = true, Height = 300 };
        var line = new LineChart
        {
            Height = 70, Width = 330, Stroke = B("BlueBrush", Brushes.SteelBlue), LabelFormat = v => Fmt.Of(Unit.MoneyStock, v),
            Values = [100, 140, 180, 160, double.NaN, 240, 400, 380, 520, 9000],
        };
        var lineFlat = new LineChart { Height = 40, Width = 150, Stroke = B("RustBrush", Brushes.Firebrick), Values = [3, 3, 3], ShowLabels = false };
        var lineEmpty = new LineChart { Height = 40, Width = 80, Stroke = B("BlueBrush", Brushes.SteelBlue), Values = [double.NaN, double.PositiveInfinity] };
        var pie = new HalfPie
        {
            Radius = 50,
            Slices = [new(0.4, Brushes.SteelBlue), new(0.3, Brushes.Goldenrod), new(0.2, Brushes.IndianRed), new(0.1, Brushes.SeaGreen)],
        };
        var pieOne = new HalfPie { Radius = 30, Slices = [new(1.0, Brushes.SeaGreen)] };
        var pieNone = new HalfPie { Radius = 30, Slices = [new(0.0, Brushes.SeaGreen), new(double.NaN, Brushes.Red)] };

        var ink = B("InkBrush", Brushes.Black);
        var parts = new StackPanel
        {
            Spacing = 10, Margin = new Thickness(16), Width = 720,
            Children =
            {
                new TextBlock { Text = "共通の表（絞り込み・並べ替え・群・半円・段・動詞）", Foreground = ink, FontWeight = FontWeight.Bold },
                table,
                new TextBlock { Text = "線グラフ（欠けた点・平ら・空）", Foreground = ink, FontWeight = FontWeight.Bold },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, Children = { line, lineFlat, lineEmpty } },
                new TextBlock { Text = "半円（4 区分・全部・無し）", Foreground = ink, FontWeight = FontWeight.Bold },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 24, Children = { pie, pieOne, pieNone } },
            },
        };
        return new Border { Background = B("ParchmentBrush", Brushes.Beige), Child = parts, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
    }
}
