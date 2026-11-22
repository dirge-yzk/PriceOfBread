using Avalonia.Controls;
using PriceOfBread.App.Controls;

namespace PriceOfBread.App.Views.Pages;

/// <summary>金融のページ。欄の並べの表はここに 1 つだけ手で書く（金融の表を上・国債と外資信用の表を下に積む 1 列）。</summary>
public partial class FinancePage : UserControl
{
    public static PageTable Plan { get; } = new(
    [
        new SectionSpec("finance", "金融", 8), new SectionSpec("debt", "国債と外資信用", 8),
    ],
    [["finance", "debt"]]);

    public FinancePage()
    {
        InitializeComponent();
        PageBody.Table = Plan;
    }
}
