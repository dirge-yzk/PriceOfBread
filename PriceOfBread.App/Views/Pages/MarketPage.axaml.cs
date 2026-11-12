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
        new SectionSpec("prices", "財の値段", MinRows: 10, Weight: 2),
        new SectionSpec("exports", "輸出", MinRows: 6),
        new SectionSpec("imports", "輸入", MinRows: 6),
    ],
    [["prices"], ["exports", "imports"]],
    ColumnWeights: [3, 2]);

    public MarketPage()
    {
        InitializeComponent();
        MarketGrid.Table = Plan;
        // 表は欄の高さに合わせる＝見出しを止めて行だけを欄の中で流す
        SectionFit.Attach(PricesSection, PricesTable);
        SectionFit.Attach(ImportsSection, ImportsBody);
        SectionFit.Attach(ExportsSection, ExportsBody);
    }
}
