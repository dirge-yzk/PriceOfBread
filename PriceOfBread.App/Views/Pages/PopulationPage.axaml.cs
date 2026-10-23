using Avalonia.Controls;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.Views.Pages;

/// <summary>人口のページ（設計 §3-3）。階級の表は列が多い（規模・失業者・満足度・闘争度・識字率・なし・保守・自由・社会・特権）ので、左の列に広く取る＝列は 2 つ（幅が足りなければ 1 列へ積む）。
/// 欄の並べの表（置き場と畳む順位）はここに 1 つだけ手で書く。</summary>
public partial class PopulationPage : UserControl
{
    public static PageTable Plan { get; } = new(
    [
        new SectionSpec("classes", "階級", 1, 16), new SectionSpec("regions", "州の表", 2, 8, 3), new SectionSpec("age", "年齢の層", 3, 8),
        new SectionSpec("births", "出生と死", 4, 6), new SectionSpec("religion", "宗教", 6, 4), new SectionSpec("culture", "文化", 6, 4),
    ],
    [["classes", "regions"], ["age", "births", "religion", "culture"]],
    ColumnMinWidth: 640);   // 階級の表は列が多い（約 610 の幅）＝それより狭くなる列には割らず、1 列へ積む

    public PopulationPage()
    {
        InitializeComponent();
        PageBody.Table = Plan;
        BirthChart.LabelFormat = v => Fmt.Of(Unit.RatePerYear, v);
        DeathChart.LabelFormat = v => Fmt.Of(Unit.RatePerYear, v);
        FertilityChart.LabelFormat = v => v.ToString("0.0");
    }
}
