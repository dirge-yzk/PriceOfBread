using Avalonia.Controls;
using PriceOfBread.App.Controls;

namespace PriceOfBread.App.Views.Pages;

/// <summary>発見のページ（設計 §3-3）。発見の表（政府・思想・技術の群）を広く・産地の表を右に置く。欄の並べの表（置き場）はここに 1 つだけ手で書く。</summary>
public partial class DiscoveryPage : UserControl
{
    public static PageTable Plan { get; } = new(
    [
        new SectionSpec("discovery", "技術", 10), new SectionSpec("social", "政府と思想", 8), new SectionSpec("sites", "産地", 3),
    ],
    // 2 段組み（2026-11-11 作者指示「空白が多すぎる」）：技術＝左・政府と思想・産地＝右（制度は政府の行の状態の列へ吸収・2026-11-12）
    [["discovery"], ["social", "sites"]],
    ColumnMinWidth: 340,
    ColumnWeights: [1.6, 1]);   // 左の技術は名前と棒の表が 2 つ並ぶ（約 580）・右の政府と思想と産地は約 362

    public DiscoveryPage()
    {
        InitializeComponent();
        PageBody.Table = Plan;
        // 棒の色（技術力・必要性・伝播・解禁済み）の凡例は頁に 1 つ（表ごとには出さない）
        foreach (var it in PriceOfBread.App.ViewModels.GameViewModel.DiscoveryLegend)
            LegendPanel.Children.Add(new Avalonia.Controls.StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 3, Margin = new Avalonia.Thickness(0, 0, 10, 0),
                Children =
                {
                    new Avalonia.Controls.Shapes.Ellipse { Width = 10, Height = 10, Fill = it.Brush, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center },
                    new TextBlock { Text = it.Name, FontSize = 11, Foreground = Avalonia.Application.Current is { } app && app.TryFindResource("InkSoftBrush", app.ActualThemeVariant, out var v) && v is Avalonia.Media.IBrush b ? b : Avalonia.Media.Brushes.Gray, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center },
                },
            });
    }
}
