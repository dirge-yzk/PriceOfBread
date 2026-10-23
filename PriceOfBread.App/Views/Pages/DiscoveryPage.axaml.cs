using Avalonia.Controls;
using PriceOfBread.App.Controls;

namespace PriceOfBread.App.Views.Pages;

/// <summary>発見のページ（設計 §3-3）。発見の表（政府・思想・技術の群）を広く・産地の表を右に置く。欄の並べの表（置き場と畳む順位）はここに 1 つだけ手で書く。</summary>
public partial class DiscoveryPage : UserControl
{
    public static PageTable Plan { get; } = new(
    [
        new SectionSpec("discovery", "発見", 1, 10), new SectionSpec("sites", "産地", 2, 4),
    ],
    [["discovery"], ["sites"]],
    ColumnMinWidth: 520);

    public DiscoveryPage()
    {
        InitializeComponent();
        PageBody.Table = Plan;
    }
}
