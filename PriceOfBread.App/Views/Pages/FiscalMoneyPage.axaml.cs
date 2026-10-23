using Avalonia.Controls;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.Views.Pages;

/// <summary>財政と貨幣のページ（設計 §3-3）。欄の並べの表（幅 3 列のときの置き場と、低い欄から畳む順位）はここに 1 つだけ手で書く。</summary>
public partial class FiscalMoneyPage : UserControl
{
    /// <summary>欄の宣言（順位は畳む順＝1 が最後まで残る）と、3 列のときの欄の置き場（列ごとに上から下）。</summary>
    public static PageTable Plan { get; } = new(
    [
        new SectionSpec("spend", "支出", 1, 10), new SectionSpec("income", "収入", 1, 8), new SectionSpec("finance", "金融", 2, 8),
        new SectionSpec("ledger", "国庫の台帳", 3, 6), new SectionSpec("debt", "国債と外資信用", 4, 8), new SectionSpec("tech", "技術水準", 5, 3),
    ],
    [["spend", "ledger"], ["income", "tech"], ["finance", "debt"]],
    ColumnMinWidth: 430);   // 支出と収入の表は段の押し釦（約 236）を持つ＝項目の名前が読める幅（約 430）を下回る列には割らない

    public FiscalMoneyPage()
    {
        InitializeComponent();
        PageBody.Table = Plan;
        TreasuryChart.LabelFormat = v => Fmt.Of(Unit.MoneyStock, v);
        DebtChart.LabelFormat = v => Fmt.Of(Unit.MoneyStock, v);
        TechChart.LabelFormat = v => v.ToString("0.##");
    }
}
