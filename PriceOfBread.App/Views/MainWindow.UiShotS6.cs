using Avalonia.Controls;
using Avalonia.VisualTree;
using PriceOfBread.App.Controls;
using PriceOfBread.App.Layout;
using PriceOfBread.App.ViewModels;
using PriceOfBread.SimCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PriceOfBread.App.Views;

// Views/MainWindow.UiShotS6.cs ＝ --uishot の段 6（2026-10-23・作者の指示）：生産と地図の Tab 切り替え・戻り・年表の 3 つの絞り・外交と戦争の地図の欄の首都・流した表。
// 数字は BootLog（外側の流れ・表の列の左端は LogOuterFlow が数える）。キーは打てないので、キーの処理が呼ぶ口（ToggleProductionMap・OpenSlotCommand・HandleEscape）をそのまま呼ぶ。
public partial class MainWindow
{
    private async Task ShootS6(string dir, GameViewModel gvm, GameView gameView, Func<int> no)
    {
        var lay = gvm.Layout;
        try
        {
            // ---- Tab：地図（背景）⇄ 生産。Esc・同じページのキーで地図へ戻る ----
            lay.Reset();
            await Task.Delay(900);
            string Now() => $"背景 {lay.Background}・窓 [{string.Join(",", lay.OpenWindows)}]・左{(lay.LeftOpen ? "開" : "閉")}";
            Capture(dir, $"{no():D2}_S6_起動_地図.png");
            BootLog.Write($"uishot: 起動 {Now()}（地図から始まる）");
            gvm.ToggleProductionMap();   // Tab
            await Task.Delay(1800);
            BootLog.Write($"uishot: 生産[Tab] {Now()}");
            Capture(dir, $"{no():D2}_S6_生産.png");
            LogOuterFlow(gameView, "生産");
            ReportProductionBoard(gameView, gvm, "生産");
            gvm.SelectProdGood(Catalog.SteelId);
            await Task.Delay(1000);
            Capture(dir, $"{no():D2}_S6_生産_財を選んだ.png");
            LogOuterFlow(gameView, "生産・財を選んだ");
            gvm.SelectProdGood(Catalog.SteelId);   // 同じ財をもう一度＝選び直しで外す
            await Task.Delay(300);
            gvm.ToggleProductionMap();   // もう一度 Tab
            await Task.Delay(1200);
            BootLog.Write($"uishot: 生産[もう一度Tab] {Now()}（地図へ戻る）");
            Capture(dir, $"{no():D2}_S6_地図へ戻った.png");

            // Esc で地図へ戻る（メニューは開かない）・もう一度 Esc でメニュー・別のキー（市場）で開いて同じキーで戻る
            gvm.ToggleProductionMap(); await Task.Delay(700);
            gvm.HandleEscape(); await Task.Delay(500);
            BootLog.Write($"uishot: 生産[Esc＝地図へ] {Now()}・メニュー {(gvm.MenuOpen ? "開いた" : "開かない")}");
            gvm.HandleEscape(); await Task.Delay(300);
            BootLog.Write($"uishot: 地図[Esc] {Now()}・メニュー {(gvm.MenuOpen ? "開いた" : "開かない")}");
            if (gvm.MenuOpen) gvm.MenuOpen = false;
            gvm.OpenSlotCommand.Execute(Slot.Market); await Task.Delay(700);
            BootLog.Write($"uishot: 市場[Q＝窓が開く] {Now()}");
            gvm.OpenSlotCommand.Execute(Slot.Market); await Task.Delay(500);
            BootLog.Write($"uishot: 市場[もう一度Q＝窓が閉じる] {Now()}");

            // ---- 年表の 3 つの絞り（左の列）----
            lay.Reset();
            await Task.Delay(800);
            foreach (var (key, name) in new[] { ("world", "全国"), ("great", "列強"), ("mine", "自国") })
            {
                gvm.SetNewsFilterCommand.Execute(key);
                await Task.Delay(500);
                var rows = gvm.Alerts.ToList();
                bool ok = key switch { "great" => rows.All(r => r.Great), "mine" => rows.All(r => r.Mine), _ => true };
                BootLog.Write($"uishot: 年表[{name}] 表示 {rows.Count}行（全件のうち列強{gvm.AlertsForCapture.Count(r => r.Great)}・自国{gvm.AlertsForCapture.Count(r => r.Mine)}・全{gvm.AlertsForCapture.Count}）　絞りに合わない行 {(ok ? "なし" : "あり")}　先頭「{rows.FirstOrDefault()?.Text}」");
                Capture(dir, $"{no():D2}_S6_年表_{name}.png");
            }
            gvm.SetNewsFilterCommand.Execute("world");

            // ---- 列の左端の判定が働くことの確かめ：段の押し釦の入れ物を中央寄せに戻すと、判定が左端のずれを出す（直した判定は積み上げでなく中身の位置を測る）----
            lay.Reset(); UiShotWindows.OpenFull(gameView, lay, Slot.PoliticsLaw);
            await Task.Delay(1200);
            var lawPage = UiShotWindows.PageOf(gameView, Slot.PoliticsLaw);
            var lawTv = lawPage?.GetVisualDescendants().OfType<TableView>().FirstOrDefault(t => t.Snapshot?.Key == "politics.laws");
            var stagePanel = lawTv?.GetVisualDescendants().OfType<WrapPanel>().FirstOrDefault();
            if (lawTv is not null && stagePanel is not null)
            {
                var a0 = lawTv.MeasureAlignment();
                var keep = stagePanel.HorizontalAlignment;
                stagePanel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                await Task.Delay(400);
                var a1 = lawTv.MeasureAlignment();
                stagePanel.HorizontalAlignment = keep;
                await Task.Delay(300);
                BootLog.Write($"uishot: 左端の判定の感度 法の表 通常 ずれ最大{a0?.MaxDelta:F1}（ずれた列{a0?.Gaps}/{a0?.Columns}）→ 段の入れ物を中央寄せ ずれ最大{a1?.MaxDelta:F1}（ずれた列{a1?.Gaps}）→ 戻した {lawTv.MeasureAlignment()?.MaxDelta:F1}");
            }
            else BootLog.Write("uishot: 左端の判定の感度 法の表または段の入れ物が見つからない");

            // ---- 内側で流れる表を流した図（行が見出しに重ならない）----
            lay.Reset(); UiShotWindows.OpenFull(gameView, lay, Slot.Market);   // 財の値段の表の行が欄の中で流れる
            await Task.Delay(1200);
            var page = UiShotWindows.PageOf(gameView, Slot.Market);
            var tv = page?.GetVisualDescendants().OfType<TableView>().FirstOrDefault(t => t.Snapshot?.Key == "market.prices");
            var sv = tv?.GetVisualDescendants().OfType<ScrollViewer>().OrderByDescending(v => v.Extent.Height).FirstOrDefault();   // 絞り込みの文字の欄の中の流れ（小さい）でなく行の流れ
            if (sv is not null)
            {
                sv.Offset = new Avalonia.Vector(0, Math.Max(0, Math.Min(sv.Extent.Height - sv.Viewport.Height, 120)));
                await Task.Delay(500);
                BootLog.Write($"uishot: 流した表（財の値段）送り {sv.Offset.Y:F0}／全長 {sv.Extent.Height:F0}・窓 {sv.Viewport.Height:F0}・本体のクリップ {(tv!.GetVisualDescendants().OfType<Avalonia.Controls.Border>().Any(b => b.ClipToBounds))}");
                Capture(dir, $"{no():D2}_S6_表を流した図.png");
                LogOuterFlow(gameView, "流した表");
            }
            else BootLog.Write("uishot: 流した表 財の値段の表が見つからない");
        }
        catch (Exception ex) { BootLog.Write($"uishot: 段6 失敗 {ex}"); }
        finally { lay.Reset(); }
    }

    /// <summary>生産の盤の幅と並び（旧の生産タブ＝盤の視野 約 1380・1 行の幅予算 1330・1 行 6 枚と並べて見る）。</summary>
    private void ReportProductionBoard(GameView gameView, GameViewModel gvm, string tag)
    {
        var page = UiShotWindows.PageOf(gameView, Slot.Production);
        var ps = page?.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault(v => v.Name == "ProdScroll");
        if (ps is null) { BootLog.Write($"uishot: 生産の盤[{tag}] 見つからない"); return; }
        var rows = gvm.ProdNodes.GroupBy(n => Math.Round(n.Y)).OrderBy(g => g.Key).Select(g => g.Count()).ToList();
        BootLog.Write($"uishot: 生産の盤[{tag}] ページ {page!.Bounds.Width:F0}×{page.Bounds.Height:F0}・盤の視野 {ps.Viewport.Width:F0}×{ps.Viewport.Height:F0}・盤 {ps.Extent.Width:F0}×{ps.Extent.Height:F0}・1 行の枚数 先頭{rows.FirstOrDefault()}・最大{rows.DefaultIfEmpty(0).Max()}（行数{rows.Count}）・旧の生産タブは視野 約1380・1 行 6 枚");
    }
}
