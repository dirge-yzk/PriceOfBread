using Avalonia.Controls;
using PriceOfBread.App.Controls;

namespace PriceOfBread.App.Views.Pages;

/// <summary>財の価格のページ（設計 §3-3）。財の値段の表だけを持つ（輸出と輸入は <see cref="TradePage"/>）。欄の並べの表（ページごとに 1 つだけ手で書く）はここ。</summary>
public partial class MarketPage : UserControl
{
    /// <summary>欄の宣言（鍵・名前・最小の行数）と置き場。欄は 1 つ（財の値段）。</summary>
    public static readonly PageTable Plan = new(
    [
        new SectionSpec("prices", "財の値段", MinRows: 16),
    ],
    [["prices"]]);

    public MarketPage()
    {
        InitializeComponent();
        MarketGrid.Table = Plan;
    }
}
