using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using PriceOfBread.App.Controls;
using PriceOfBread.App.Layout;
using PriceOfBread.App.ViewModels;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PriceOfBread.App.Views;

// Views/MainWindow.UiShotFinal.cs ＝ --uishot の最後の撮影の段（(501) 段4・段5）：問題の一覧と、検査の総括。
// 各ページを撮る段は、ページごとの担当が持っている（UiShotPageP1・MainWindow.UiShot.P2・MainWindow.Page3Shots）。ここはそれに足す形。
// 外側が縦に流れていないことと表の列の左端が揃うことの判定は LogOuterFlow が全ての撮影で数える（総括はその数）。
public partial class MainWindow
{
    // ---- 検査の数え（LogOuterFlow が足す・総括が読む）----
    private int _gateOuter, _gateOuterFlowing, _gateTables, _gateTablesBad, _gateCollapsed, _gateClipped;

    /// <summary>画像でしか拾えなかった壊れ方の検査（作者の指示 2026-11-12・点検 重い1〜3）：
    /// 〈潰れた欄〉＝見えている欄で、中身の高さが 14 未満（中身の無い表と、隠れている中身は除く）。〈欠けた題〉＝大きい字の文字が、それを囲む箱の右端より外へ出ている（箱で切られる）。
    /// 例外も表の左端のずれも出ないまま、供給の網の欄が 0 の高さに潰れ・財の名前が 1 文字で切れていた。</summary>
    private void TallyBroken(GameView gv, string tag)
    {
        foreach (var (_, _, page, _) in UiShotWindows.MountedAll(gv))
        {
            if (page is MapPane) continue;
            foreach (var sec in page.GetVisualDescendants().OfType<Section>())
            {
                if (!sec.IsEffectivelyVisible || !(sec.Bounds.Width > 0)) continue;
                if (sec.Body is TableView { RowCount: 0 } || sec.Body is { IsVisible: false }) continue;
                double bodyH = sec.Bounds.Height - PagePlan.HeaderHeight - 1;
                if (bodyH < 14) { _gateCollapsed++; BootLog.Write($"uishot: 欄が潰れている[{tag}] {sec.Key}「{sec.Title}」中身の高さ {bodyH:F0}"); }
            }
            foreach (var tb in page.GetVisualDescendants().OfType<TextBlock>())
            {
                if (!tb.IsEffectivelyVisible || tb.FontSize < 16 || string.IsNullOrEmpty(tb.Text) || !(tb.Bounds.Width > 0)) continue;
                if (tb.Bounds.Width + 1 < tb.DesiredSize.Width && tb.TextTrimming == Avalonia.Media.TextTrimming.None) { _gateClipped++; BootLog.Write($"uishot: 題が欠けている[{tag}] 「{tb.Text}」自分の幅 {tb.Bounds.Width:F0} が望む幅 {tb.DesiredSize.Width:F0} に足りない"); continue; }
                foreach (var a in tb.GetVisualAncestors().TakeWhile(x => !ReferenceEquals(x, page)).OfType<Control>())
                {
                    if (a is ScrollViewer || !(a.Bounds.Width > 0)) continue;
                    if (tb.TranslatePoint(new Point(tb.Bounds.Width, 0), a) is { } q && q.X > a.Bounds.Width + 1)
                    { _gateClipped++; BootLog.Write($"uishot: 題が欠けている[{tag}] 「{tb.Text}」{a.GetType().Name} の右端を {q.X - a.Bounds.Width:F0} 超える"); break; }
                }
            }
        }
    }

    /// <summary>載っているページの全ての表について、見出しの列の左端と値の列の左端が揃っているかを数える（ずれた表は名前を BootLog に残す）。</summary>
    private void TallyAlignment(GameView gv, string tag)
    {
        foreach (var (_, _, page, _) in UiShotWindows.MountedAll(gv))
            foreach (var tv in page.GetVisualDescendants().OfType<TableView>())
            {
                if (!tv.IsEffectivelyVisible || !(tv.Bounds.Width > 0)) continue;
                var (gaps, measured) = tv.AlignmentGaps();
                if (measured == 0) continue;   // 行が無い・まだ測られていない表は数えない
                _gateTables++;
                if (gaps > 0) { _gateTablesBad++; BootLog.Write($"uishot: 表の左端がずれている[{tag}] {tv.Snapshot?.Key}（{gaps}/{measured}列）"); }
            }
    }

    /// <summary>crash.log の中の UI スレッドの例外の行数（他のプロセスも同じファイルへ書くので、この走りだけの数は <see cref="BootLog.UiExceptions"/>）。</summary>
    private static int CrashLogUiExceptions()
    {
        try
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PriceOfBread", "crash.log");
            if (!File.Exists(path)) return 0;
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var sr = new StreamReader(fs);
            int n = 0;
            string? line;
            while ((line = sr.ReadLine()) is not null) if (line.Contains("UIThread例外", StringComparison.Ordinal)) n++;
            return n;
        }
        catch { return -1; }
    }

    /// <summary>撮影の総括（外側の流れ・表の左端・UI の例外の前後）。</summary>
    private void LogUiShotSummary(int crashBefore)
    {
        int crashAfter = CrashLogUiExceptions();
        BootLog.Write($"uishot: 総括 外側の流れ {_gateOuter}回の判定中 流れている{_gateOuterFlowing}回・表の左端 {_gateTables}表の判定中 ずれた{_gateTablesBad}表・潰れた欄 {_gateCollapsed}・欠けた題 {_gateClipped}・"
                    + $"UIの例外 この走り{BootLog.UiExceptions}件・crash.log の行 前{crashBefore}→後{crashAfter}（他のプロセスの分も含む）");
    }

    /// <summary>問題の一覧（上位・全部）と、押して飛ぶ確かめ。</summary>
    private async Task ShootFinal(string dir, GameViewModel gvm, GameView gameView, Func<int> no)
    {
        var lay = gvm.Layout;
        try
        {
            // ---- 問題の一覧：上位だけ／全部、押して飛ぶ ----
            lay.Reset();
            await Task.Delay(1000);
            gvm.ExpandAllProblemsForCapture(false);
            await Task.Delay(300);
            var items = gvm.ProblemItemsForCapture;
            BootLog.Write($"uishot: 問題の一覧 {items.Count}件　" + string.Join("　", items.GroupBy(p => p.Kind).Select(g => $"{ProblemScan.KindName(g.Key)}{g.Count()}（先頭「{g.First().Target}」{Fmt.Of(g.First().Unit, g.First().Size)}）")));
            Capture(dir, $"{no():D2}_問題の一覧_上位.png");
            LogOuterFlow(gameView, "問題の一覧");
            gvm.ExpandAllProblemsForCapture(true);
            await Task.Delay(400);
            BootLog.Write($"uishot: 問題の一覧 全部を見る 行{gvm.ProblemRows.Count}（見出し{gvm.ProblemRows.Count(r => r.IsHeader)}）");
            Capture(dir, $"{no():D2}_問題の一覧_全部.png");
            gvm.ExpandAllProblemsForCapture(false);
            await Task.Delay(300);
            // 種類ごとの先頭の問題を押す（表を持つ種類を先に・全種類）
            int tried = 0;
            foreach (var g in items.GroupBy(p => p.Kind).OrderByDescending(g => g.First().TableKey.Length > 0).ThenBy(g => (int)g.Key))
            {
                if (tried++ >= 9) break;
                var it = g.First();
                var row = gvm.ProblemRows.FirstOrDefault(r => r.Key == it.Key);
                if (row is null) { BootLog.Write($"uishot: 問題を押す {ProblemScan.KindName(g.Key)}「{it.Target}」一覧に行が無い"); continue; }
                lay.Reset();
                await Task.Delay(500);
                row.ActivateCommand.Execute(null);
                await Task.Delay(1300);
                ReportJump($"問題「{ProblemScan.KindName(g.Key)}・{it.Target}」", (Slot)(int)it.Page, it.TableKey, it.RowKey, gvm, gameView);
            }
            lay.Reset();
            await Task.Delay(600);
        }
        catch (Exception ex) { BootLog.Write($"uishot: 最後の段 失敗 {ex}"); }
        finally { lay.Reset(); }
    }

    /// <summary>飛び先の確かめ：そのページが中央に開いたか・表の行に着いたか。</summary>
    private void ReportJump(string what, Slot slot, string tableKey, string rowKey, GameViewModel gvm, GameView gameView)
    {
        var lay = gvm.Layout;
        bool shown = lay.IsShown(slot);
        var page = shown ? UiShotWindows.PageOf(gameView, slot) : null;
        string place = shown ? "中央に開いた" : "開いていない";
        string landed = "";
        if (page is not null && tableKey.Length > 0)
        {
            var tv = page.GetVisualDescendants().OfType<TableView>().FirstOrDefault(t => t.Snapshot?.Key == tableKey);
            landed = tv is null ? "・表が見つからない" : $"・表の行 {(tv.FocusedKey == rowKey ? "に着いた" : $"に着かない（着いたのは{tv.FocusedKey ?? "なし"}・行は{(tv.Snapshot!.Rows.Any(r => r.Key == rowKey) ? "ある" : "表に無い")}）")}";
        }
        BootLog.Write($"uishot: {what} → {SlotInfo.Name(slot)}が{place}{landed}");
    }
}
