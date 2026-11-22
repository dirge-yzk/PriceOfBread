using Avalonia;
using Avalonia.Controls;
using PriceOfBread.App.Controls;

namespace PriceOfBread.App.Views.Pages;

/// <summary>貿易のページ（作者指示 2026-11-22）。輸出を左・輸入を右に並べる。欄の並べの表（ページごとに 1 つだけ手で書く）はここ。</summary>
public partial class TradePage : UserControl
{
    /// <summary>欄の宣言と置き場（左＝輸出・右＝輸入）。幅が 2 列入る限り左右に並ぶ（幅で決まる 1 本の規則）・狭ければ 1 列に積む。</summary>
    public static readonly PageTable Plan = new(
    [
        new SectionSpec("exports", "輸出", MinRows: 12),
        new SectionSpec("imports", "輸入", MinRows: 12),
    ],
    [["exports"], ["imports"]],
    ColumnMinWidth: 430,
    ColumnWeights: [1, 1]);

    /// <summary>2 列に並べないとき、輸出と輸入が自然な高さ（行が多い）で下の欄を押し出さないための、1 つの欄の高さの上限（表は欄の中で流れる）。</summary>
    private const double StackedMaxHeight = 330;

    public TradePage()
    {
        InitializeComponent();
        TradeGrid.Table = Plan;
        TradeGrid.PropertyChanged += (_, e) =>
        {
            if (e.Property != Visual.BoundsProperty) return;
            double cap = PagePlan.ColumnCount(TradeGrid.Bounds.Width, Plan) >= 2 ? double.PositiveInfinity : StackedMaxHeight;
            if (ExportsSection.MaxHeight != cap) { ExportsSection.MaxHeight = cap; ImportsSection.MaxHeight = cap; }
        };
    }
}
