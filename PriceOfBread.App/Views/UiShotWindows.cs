using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using PriceOfBread.App.Layout;

namespace PriceOfBread.App.Views;

/// <summary>--uishot の道具：背景と浮かぶ窓の両方を同じ形で数える・ページを従来と同じ広さで撮る（2026-11-11）。画面の見た目・盤面には触れない。</summary>
internal static class UiShotWindows
{
    /// <summary>撮影用に出す。背景（地図・生産）はそのまま背景にする・窓は開いて窓の領域いっぱいに広げる
    /// （ページごとの撮影の表の網羅と外側の流れの判定が、窓より広かった頃と同じ広さで見る）。</summary>
    public static void OpenFull(GameView gv, LayoutModel lay, Slot slot)
    {
        lay.Open(slot);
        if (LayoutModel.IsWindow(slot) && gv.FindControl<WindowLayer>("Windows") is { } layer && layer.Bounds.Width > 0)
            lay.SetRect(slot, new WindowRect(0, 0, layer.Bounds.Width, layer.Bounds.Height));
    }

    /// <summary>開いている窓を全部閉じる（背景はそのまま）。</summary>
    public static void CloseAll(LayoutModel lay)
    {
        foreach (var w in lay.OpenWindows.ToList()) lay.Close(w);
    }

    /// <summary>載っているページの部品を、背景（枠番号 0）→窓（重なりの順）で返す。<c>Frame</c>＝そのページを囲む箱（背景は中央の箱・窓は窓の枠）。</summary>
    public static IEnumerable<(int Pane, Slot Slot, Control Page, Control Frame)> MountedAll(GameView gv)
    {
        if (gv.FindControl<PageHost>("Center") is { } host)
            foreach (var (pane, slot, page) in host.Mounted()) yield return (pane, slot, page, host);
        if (gv.FindControl<WindowLayer>("Windows") is { } layer)
            foreach (var m in layer.Mounted()) yield return (m.Pane, m.Slot, m.Page, m.Frame);
    }

    public static Control? PageOf(GameView gv, Slot slot) => MountedAll(gv).Where(m => m.Slot == slot).Select(m => m.Page).FirstOrDefault();

    /// <summary>窓から GameView を引く（静的な撮影の部品用）。</summary>
    public static GameView? GameViewOf(Control any) => any.GetVisualDescendants().OfType<GameView>().FirstOrDefault();
}
