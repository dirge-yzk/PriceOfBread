using Avalonia.Controls;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.Views.Pages;

/// <summary>財政と貨幣のページ（設計 §3-3）。欄の並べの表（幅 3 列のときの置き場）はここに 1 つだけ手で書く。</summary>
public partial class FiscalMoneyPage : UserControl
{
    /// <summary>欄の宣言と、3 列のときの欄の置き場（列ごとに上から下）。</summary>
    public static PageTable Plan { get; } = new(
    [
        new SectionSpec("spend", "支出", 10), new SectionSpec("income", "収入", 8), new SectionSpec("finance", "金融", 8),
        new SectionSpec("debt", "国債と外資信用", 8),
    ],
    // 2 列（2026-11-11 作者指示）：支出＝左・収入＝右／その下に金融（左）と国債と外資信用（右）を隣に並べる（国庫の台帳は支出と収入の表へ吸収・2026-11-12）
    [["spend", "finance"], ["income", "debt"]],
    ColumnMinWidth: 430);   // 支出と収入の表は段の押し釦（約 236）を持つ＝項目の名前が読める幅（約 430）を下回る列には割らない

    public FiscalMoneyPage()
    {
        InitializeComponent();
        PageBody.Table = Plan;
        TreasuryChart.LabelFormat = v => Fmt.Of(Unit.MoneyStock, v);
        DebtChart.LabelFormat = v => Fmt.Of(Unit.MoneyStock, v);
        GdpChart.LabelFormat = v => Fmt.Of(Unit.MoneyStock, v);
    }
}
