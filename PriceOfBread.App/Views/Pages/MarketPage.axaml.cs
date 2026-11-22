using Avalonia;
using Avalonia.Controls;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.Views.Pages;

/// <summary>市場・清算・貿易のページ（設計 §3-3）。欄の並べの表（ページごとに 1 つだけ手で書く）はここ。</summary>
public partial class MarketPage : UserControl
{
    /// <summary>欄の宣言（鍵・名前・最小の行数・余りの高さの重み）と、置き場（列ごとに上から下）。左＝財の値段・右＝輸出と輸入（作者指示 2026-11-12）。</summary>
    public static readonly PageTable Plan = new(
    [
        new SectionSpec("prices", "財の値段", MinRows: 16, Weight: 2),   // 2 列のとき値段だけが左の列になる大きさ（輸出と輸入を右の列へ割る）
        new SectionSpec("exports", "輸出", MinRows: 6),
        new SectionSpec("imports", "輸入", MinRows: 6),
        new SectionSpec("transport", "輸送", MinRows: 3),
    ],
    [["prices"], ["exports", "transport"], ["imports"]],
    ColumnMinWidth: 420,    // 幅が 3 列入れば値段・輸出・輸入が横に並ぶ（輸入が輸出の下へ押し出されない）・2 列なら左に値段・右に輸出と輸入・狭ければ 1 列に積む（幅で決まる 1 本の規則）
    ColumnWeights: [1, 1, 1]);

    /// <summary>3 列に並べないとき、輸出と輸入が自然な高さ（輸出は 40 行超）で下の欄を押し出さないための、1 つの欄の高さの上限（表は欄の中で流れる）。</summary>
    private const double StackedMaxHeight = 330;

    public MarketPage()
    {
        InitializeComponent();
        MarketGrid.Table = Plan;
        MarketGrid.PropertyChanged += (_, e) =>
        {
            if (e.Property != Visual.BoundsProperty) return;
            double cap = PagePlan.ColumnCount(MarketGrid.Bounds.Width, Plan) >= 3 ? double.PositiveInfinity : StackedMaxHeight;
            if (ExportsSection.MaxHeight != cap) { ExportsSection.MaxHeight = cap; ImportsSection.MaxHeight = cap; }
        };
    }
}
