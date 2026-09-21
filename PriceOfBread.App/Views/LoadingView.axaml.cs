using Avalonia.Controls;
using PriceOfBread.App.ViewModels;

namespace PriceOfBread.App.Views;

public partial class LoadingView : UserControl
{
    public LoadingView()
    {
        InitializeComponent();
        // 画面遷移でこのViewが視覚木から外れたら点のタイマーを必ず止める（どの経路で差し替わってもリークしない保険。
        // 主経路は MainWindowViewModel 側が Stop を呼ぶ＝二重呼びは安全）。
        DetachedFromVisualTree += (_, _) => (DataContext as LoadingViewModel)?.Stop();
    }
}
