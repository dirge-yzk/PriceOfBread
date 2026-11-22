using Avalonia.Controls;

namespace PriceOfBread.App.Views;

public partial class TitleView : UserControl
{
    // MainWindow.axaml の初期サイズ（スタートメニューへ戻るときはここへ必ず復帰する）
    private const double InitialWidth = 980, InitialHeight = 660;

    public TitleView()
    {
        InitializeComponent();
        // スタートメニューは常に初期ウィンドウサイズ（2026-07-27ユーザー要望）。
        // ゲーム画面は GameView の Loaded でフルスクリーン化するが、解除は F11 トグルしか無いため
        // タイトルへ戻ると（CurrentPage を差し替えるだけで WindowState に触れないため）フルスクリーンが持ち越されていた。
        // ViewLocator は毎回 View を作り直す＝この Loaded がタイトル復帰の確実なフックになる。
        Loaded += (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is not Window win) return;
            if (Program.UiShotDir is not null && !Program.UiShotVisible) return;   // 画面に出さない撮影は 1920×1080 のまま（MainWindow.HideForShots）
            if (win.WindowState != WindowState.Normal) win.WindowState = WindowState.Normal;
            win.Width = InitialWidth;
            win.Height = InitialHeight;
        };
    }
}
