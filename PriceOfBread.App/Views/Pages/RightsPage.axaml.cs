using Avalonia.Controls;
using PriceOfBread.App.Controls;

namespace PriceOfBread.App.Views.Pages;

/// <summary>権利と議会のページ（作者指示 2026-11-22）。政治と法のページから権力の欄と議会の欄を移した。欄の並べの表はここ。</summary>
public partial class RightsPage : UserControl
{
    public static PageTable Table { get; } = new(
    [
        new SectionSpec("power", "権力", 9),
        new SectionSpec("parliament", "議会", 10, 2),
    ],
    [
        ["power"],
        ["parliament"],
    ]);

    public RightsPage()
    {
        InitializeComponent();
        Sections.Table = Table;
    }
}
