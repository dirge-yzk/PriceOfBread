using Avalonia.Controls;

namespace PriceOfBread.App.Views.Pages;

/// <summary>法の表の選んだ行の下に開く中身（前提法・賛成と反対の半円・階級ごとの賛成寄りと反対寄り）。値は <c>GameViewModel</c> の StakeXxx。</summary>
public partial class StakeDetail : UserControl
{
    public StakeDetail() => InitializeComponent();
}
