using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PriceOfBread.App;

/// <summary>画面の様式（色・字体）を App.axaml の資源から読む口（2026-11-12）。値の定義は App.axaml の 1 か所＝コードは**名前**で読む。
/// 窓の無い所（試験・Application 未起動）では fallback を返す＝描画が要らない所では値は問われない。</summary>
internal static class UiRes
{
    public static IBrush Brush(string key, IBrush fallback)
        => Application.Current is { } app && app.TryFindResource(key, app.ActualThemeVariant, out var v) && v is IBrush b ? b : fallback;

    /// <summary>画面の字体（Times New Roman → 落ち先は Noto Sans JP）。自前で文字を描く所（グラフ・人口ピラミッド）が使う。</summary>
    public static FontFamily Font
        => Application.Current is { } app && app.TryFindResource("UiFont", app.ActualThemeVariant, out var v) && v is FontFamily f ? f : FontFamily.Default;
}
