using Avalonia.Controls;
using PriceOfBread.App.Controls;

namespace PriceOfBread.App.Views.Pages;

/// <summary>財政と貨幣のページ（設計 §3-3）。欄の並べの表はここに 1 つだけ手で書く。</summary>
public partial class FiscalMoneyPage : UserControl
{
    /// <summary>欄の宣言と置き場（左＝支出・右＝収入）。幅が 2 列入る限り左右に並ぶ。金融と国債と外資信用は金融のページ。</summary>
    public static PageTable Plan { get; } = new(
    [
        new SectionSpec("spend", "支出", 10), new SectionSpec("income", "収入", 8),
    ],
    [["spend"], ["income"]],
    ColumnMinWidth: 430,   // 支出と収入の表は段の押し釦（約 236）を持つ＝項目の名前が読める幅（約 430）を下回る列には割らない
    ColumnWeights: [1, 1]);

    public FiscalMoneyPage()
    {
        InitializeComponent();
        PageBody.Table = Plan;
    }
}
