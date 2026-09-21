using System;
using System.Reflection;
using CommunityToolkit.Mvvm.Input;

namespace PriceOfBread.App.ViewModels;

public partial class TitleViewModel : ViewModelBase
{
    /// <summary>配布版のバージョン（csproj の &lt;InformationalVersion&gt; を読む＝画面と exe で二重に持たない）。</summary>
    public static string VersionText =>
        "ver " + (System.Reflection.Assembly.GetExecutingAssembly()
            .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0] ?? "?");

    private readonly Action _onStart;
    private readonly Action? _onContinue;
    public bool CanContinue => GameViewModel.NewestSlot() >= 0;   // どれかのスロットにセーブがある時だけ「続きから」（最新のスロットを読む）

    public TitleViewModel(Action onStart, Action? onContinue = null)
    {
        _onStart = onStart;
        _onContinue = onContinue;
    }

    [RelayCommand] private void Start() => _onStart();
    [RelayCommand] private void ContinueGame() => _onContinue?.Invoke();

    [RelayCommand]
    private void Exit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}
