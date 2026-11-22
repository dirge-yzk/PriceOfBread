using Avalonia.Controls;
using PriceOfBread.App.Controls;

namespace PriceOfBread.App.Views.Pages;

/// <summary>金融のページ。欄の並べの表はここに 1 つだけ手で書く（金融の表を左・国債と外資信用の表を右に並べる 2 列・狭ければ 1 列に積む）。</summary>
public partial class FinancePage : UserControl
{
    public static PageTable Plan { get; } = new(
    [
        new SectionSpec("finance", "金融", 8), new SectionSpec("debt", "国債と外資信用", 8),
    ],
    [["finance"], ["debt"]],
    ColumnMinWidth: 300,
    ColumnWeights: [1.6, 1]);   // 金融の表は段の押し釦を持つ（約 513）・国債と外資信用は約 316（窓の初めの幅の見積り PageSizes と揃える）

    public FinancePage()
    {
        InitializeComponent();
        PageBody.Table = Plan;
    }
}
