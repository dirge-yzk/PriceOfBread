using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PriceOfBread.App.ViewModels;
using PriceOfBread.App.Views;

namespace PriceOfBread.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            BootLog.Write("MainWindow生成");
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            // 起動フレーク（まれに数秒でコード0終了）の診断：何が窓を閉じたかを記録（2026-07-19）
            desktop.MainWindow.Opened += (_, _) => BootLog.Write("MainWindow表示");
            desktop.MainWindow.Closed += (_, _) => BootLog.Write("MainWindow Closed");
            desktop.ShutdownRequested += (_, e) => BootLog.Write($"ShutdownRequested cancel={e.Cancel}");
        }

        base.OnFrameworkInitializationCompleted();
    }
}