using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using PriceOfBread.App.Layout;
using PriceOfBread.App.ViewModels;

namespace PriceOfBread.App.Views;

// Views/MainWindow.UiShotWindows.cs ＝ --uishot の浮かぶ窓の撮影（2026-11-11・作者の指示）：複数の窓を開いて重ねた図・動かして大きさを変えた図・押した窓が前に出た図を、
// 背景が地図のときと生産のときで撮る。窓の順と位置は BootLog に書く。ポインタの操作そのもの（題の帯をつかむ・縁をつかむ）は撮影では打てない＝
// 配置の口（SetRect・BringToFront）を直接呼ぶ。
public partial class MainWindow
{
    private async Task ShootWindows(string dir, GameViewModel gvm, GameView gameView, Func<int> no)
    {
        var lay = gvm.Layout;
        string Now() => $"背景 {lay.Background}・窓の順 [{string.Join(" → ", lay.OpenWindows.Select(SlotInfo.Name))}]（右が前）・位置 " +
                        string.Join(" ", lay.OpenWindows.Select(s => lay.RectOf(s) is { } r ? $"{SlotInfo.Name(s)}({r.X:F0},{r.Y:F0},{r.W:F0}×{r.H:F0})" : $"{SlotInfo.Name(s)}(初め)"));
        try
        {
            foreach (var background in new[] { Slot.Map, Slot.Production })
            {
                string bg = background == Slot.Map ? "地図" : "生産";
                lay.Reset();
                lay.Open(background);
                await Task.Delay(1200);
                // 3 枚を開く（開いた順に重なる＝後が前）
                foreach (var s in new[] { Slot.Market, Slot.FiscalMoney, Slot.Population })
                {
                    gvm.OpenSlotCommand.Execute(s);
                    await Task.Delay(700);
                }
                BootLog.Write($"uishot: 窓[{bg}の背景・3枚を開いた] {Now()}");
                Capture(dir, $"{no():D2}_窓_{bg}_3枚.png");
                LogOuterFlow(gameView, $"窓 {bg}の背景・3枚");

                // 動かして大きさを変える（市場を右上へ・広く）
                var host = gameView.FindControl<WindowLayer>("Windows")!;
                lay.SetRect(Slot.Market, new WindowRect(host.Bounds.Width * 0.42, 16, host.Bounds.Width * 0.56, host.Bounds.Height * 0.55));
                await Task.Delay(700);
                BootLog.Write($"uishot: 窓[{bg}の背景・市場を動かして大きさを変えた] {Now()}");
                Capture(dir, $"{no():D2}_窓_{bg}_動かした.png");

                // 押した窓が前に出る（市場を押す）
                lay.BringToFront(Slot.Market);
                await Task.Delay(500);
                BootLog.Write($"uishot: 窓[{bg}の背景・市場を前へ] {Now()}");
                Capture(dir, $"{no():D2}_窓_{bg}_前へ.png");
                LogOuterFlow(gameView, $"窓 {bg}の背景・前へ");

                // キーで閉じる（Q＝市場）・Esc で一番前の窓を閉じる・背景はそのまま
                gvm.OpenSlotCommand.Execute(Slot.Market);
                await Task.Delay(400);
                BootLog.Write($"uishot: 窓[{bg}の背景・Q で市場を閉じた] {Now()}");
                gvm.HandleEscape();
                await Task.Delay(400);
                BootLog.Write($"uishot: 窓[{bg}の背景・Esc で前の窓を閉じた] {Now()}・メニュー {(gvm.MenuOpen ? "開いた" : "開かない")}");
                Capture(dir, $"{no():D2}_窓_{bg}_閉じた後.png");
            }

            // 位置と大きさの保存の往復（セーブには入れない・配置の保存だけ）
            var back = LayoutModel.FromJson(lay.ToJson());
            BootLog.Write($"uishot: 窓の位置の往復 {(back.ToJson() == lay.ToJson() ? "一致" : "不一致")}・市場 {back.RectOf(Slot.Market)}");
        }
        catch (Exception ex) { BootLog.Write($"uishot: 窓の撮影 失敗 {ex}"); }
        finally { lay.Reset(); }
    }
}
