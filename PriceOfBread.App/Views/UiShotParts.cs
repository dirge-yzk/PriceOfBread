using System;
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

    /// <summary>ページの欄の並べ（PageGrid）の見本＝同じ表を 3 つの大きさで並べる（広い＝3 列・中＝2 列・狭くて低い＝順位の低い欄が見出しの 1 行へ畳まれる）。
    /// 欄の中身は固定の行（本体の盤面は読まない）。</summary>
    public static Control BuildPages()
    {
        var table = new PageTable(
        [
            new SectionSpec("supply", "供給の網", 1, 8), new SectionSpec("goods", "財の表", 2, 8), new SectionSpec("detail", "選んだ財の詳細", 3, 6),
            new SectionSpec("factory", "州別の工場", 4, 6), new SectionSpec("equip", "設備", 6, 5), new SectionSpec("mega", "巨大建造物", 7, 4),
        ], [["supply", "detail"], ["goods", "factory"], ["equip", "mega"]]);
        Control Rows(int n) => new StackPanel { Children = { } }.With(p => { for (int i = 0; i < n; i++) p.Children.Add(new TextBlock { Text = $"行 {i + 1}　£{(i + 1) * 137}.0k/月", Margin = new Thickness(0, 1) }); });
        PageGrid Make(double w, double h)
        {
            var g = new PageGrid { Table = table, Width = w, Height = h };
            foreach (var sp in table.Sections) g.Children.Add(new Section { Key = sp.Key, Title = sp.Title, Body = Rows(sp.MinRows + 6) });
            return g;
        }
        Border Frame(string caption, PageGrid g) => new()
        {
            BorderBrush = B("SandDarkBrush", Brushes.Tan), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 10),
            Child = new StackPanel { Children = { new TextBlock { Text = caption, FontWeight = FontWeight.Bold, Margin = new Thickness(6, 2), Foreground = B("InkBrush", Brushes.Black) }, g } },
        };
        var wide = Make(1280, 330); var mid = Make(900, 330); var narrowLow = Make(560, 250);
        var panel = new StackPanel { Margin = new Thickness(12), Width = 1300, Children = { Frame("幅 1280・高さ 330：3 列", wide), Frame("幅 900・高さ 330：2 列", mid), Frame("幅 560・高さ 250：1 列（低い欄から畳む）", narrowLow) } };
        return new Border { Background = B("ParchmentBrush", Brushes.Beige), Child = panel, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
    }

    private static T With<T>(this T x, Action<T> f) { f(x); return x; }

    public static Control Build()
    {
        // 表：文字（色の丸・バッジつきも）・数（文字色つきも）・半円・段・動詞の種類を群つきで
        var labels = FiscalCatalog.TaxLabels;   // 段の名前の写しは持たない（目録から引く）
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
            var share = new ShareCell([(PopTables.RuleStateName, 0.55 - i * 0.03), (PopTables.RuleLocalName, 0.25), (PopTables.RuleForeignName, 0.1), (PopTables.RuleUngovernedName, 0.1 + i * 0.03)], PopTables.RulePalette);
            var stage = new StageCell(labels, 0, i % 4, (i + 1) % 4, l => l != 3, l => l == 3 ? null : $"{names[0]}:{labels[l]}", _ => { });
            rows.Add(new RowData("r" + i, names[i], i < 5 ? "列強" : "その他", [i % 3 == 0 ? new TextCell(names[i], null, Swatch: "#2E6E5E", BadgeFill: i == 3 ? "#A0432D" : null) : names[i], new NumberCell(a), new NumberCell(0.04 * i - 0.001, Ink: i % 2 == 0 ? "#B8860B" : null), new NumberCell(-0.0001 * i), share, stage, new VerbCell("侵略", i % 2 == 0, () => { })]));
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
