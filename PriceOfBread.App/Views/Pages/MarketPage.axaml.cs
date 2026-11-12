using Avalonia.Controls;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.Views.Pages;

/// <summary>市場・清算・貿易のページ（設計 §3-3）。欄の並べの表（ページごとに 1 つだけ手で書く）はここ。</summary>
public partial class MarketPage : UserControl
{
    /// <summary>欄の宣言（鍵・名前・畳む順位・最小の行数・余りの高さの重み）と、幅が 3 列のときの置き場（列ごとに上から下）。2 面：財の値段／貿易（輸出＝左・輸入＝右・輸送力）。</summary>
    public static readonly PageTable PricesPlan = new(
    [
        new SectionSpec("prices", "財の値段", 1, MinRows: 10, Weight: 2),
    ],
    [["prices"]]);

    public static readonly PageTable TradePlan = new(
    [
        new SectionSpec("exports", "輸出", 1, MinRows: 6),
        new SectionSpec("imports", "輸入", 2, MinRows: 6),
        new SectionSpec("transport", "輸送力", 3, MinRows: 4),
    ],
    [["exports"], ["imports"], ["transport"]]);

    public MarketPage()
    {
        InitializeComponent();
        PriceGrid.Table = PricesPlan;
        TradeGrid.Table = TradePlan;
        // 表は欄の高さに合わせる＝見出しを止めて行だけを欄の中で流す
        SectionFit.Attach(PricesSection, PricesTable);
        SectionFit.Attach(ImportsSection, ImportsBody);
        SectionFit.Attach(ExportsSection, ExportsBody);
    }
}
