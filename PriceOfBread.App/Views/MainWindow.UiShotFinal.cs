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

// Views/MainWindow.UiShotFinal.cs ＝ --uishot の最後の撮影の段（(501) 段4・段5）：ピン留め 3 枠・問題の一覧・狭い分割で畳んだ欄を開いた図と、検査の総括。
// 各ページを 1 枚・左右 2・4 分割で撮る段は、ページごとの担当が持っている（UiShotPageP1・MainWindow.UiShot.P2・MainWindow.Page3Shots）。ここはそれに足す形。
// 外側が縦に流れていないことと表の列の左端が揃うことの判定は LogOuterFlow が全ての撮影で数える（総括はその数）。
public partial class MainWindow
{
    // ---- 検査の数え（LogOuterFlow が足す・総括が読む）----
    private int _gateOuter, _gateOuterFlowing, _gateTables, _gateTablesBad;

    /// <summary>載っているページの全ての表について、見出しの列の左端と値の列の左端が揃っているかを数える（ずれた表は名前を BootLog に残す）。</summary>
    private void TallyAlignment(SplitHost host, string tag)
    {
        foreach (var (_, _, page) in host.Mounted())
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
        BootLog.Write($"uishot: 総括 外側の流れ {_gateOuter}回の判定中 流れている{_gateOuterFlowing}回・表の左端 {_gateTables}表の判定中 ずれた{_gateTablesBad}表・"
                    + $"UIの例外 この走り{BootLog.UiExceptions}件・crash.log の行 前{crashBefore}→後{crashAfter}（他のプロセスの分も含む）");
    }

    private SplitHost? Host(GameView gv) => gv.FindControl<SplitHost>("Splits");

    private static string PinLine(PinSlot s) => $"「{s.Name}」{(string.IsNullOrEmpty(s.Source) ? "" : "（" + s.Source + "）")} 数字「{s.ValuesText}」線{(s.Line?.Count ?? 0)}点{(s.Faded ? "・薄い（行が消えた）" : "")}";

    /// <summary>ピン留め 3 枠（行・ヘッダーの指標・線グラフ）と消えた行の枠、問題の一覧（上位・全部）、押して飛ぶ確かめ、狭い分割で畳んだ欄を開いた図。</summary>
    private async Task ShootFinal(string dir, GameViewModel gvm, GameView gameView, Func<int> no)
    {
        var lay = gvm.Layout;
        try
        {
            // ---- ピン留め：行・ヘッダーの指標・線グラフ（国庫）。線グラフは木に載った瞬間に名乗る＝財政と貨幣のページを一度載せる ----
            lay.Reset(); lay.SetColumns(true, true); lay.SetShape(SplitShape.LeftRight);
            lay.Open(Slot.FiscalMoney, 1);
            await Task.Delay(1100);
            lay.Open(Slot.Production, 0);
            await Task.Delay(900);
            gvm.PinForCapture(new PinRef(PinKind.Row, "Production", "production.goods", Catalog.SteelId.ToString(), "鋼", "財の表"));
            var stat = gvm.HeaderStats.FirstOrDefault(h => h.Values is { Count: > 0 });
            if (stat is not null) gvm.PinForCapture(gvm.HeaderPin(stat));
            gvm.PinForCapture(new PinRef(PinKind.Chart, "FiscalMoney", "chart.treasury", "", "国庫", ""));
            var gone = new PinRef(PinKind.Row, "Population", "pop.regions", "存在しない州", "存在しない州", "州の表");
            gvm.PinForCapture(gone);   // 行が消えた枠＝薄くして名前だけ残る
            await Task.Delay(500);
            foreach (var s in gvm.Pins) BootLog.Write("uishot: ピン留め " + PinLine(s));
            int resolved = gvm.Pins.Count(p => !p.Faded);
            BootLog.Write($"uishot: ピン留め {gvm.Pins.Count}枠（引けた{resolved}・薄い{gvm.Pins.Count - resolved}）・消えた行の枠は薄い={(gvm.Pins.FirstOrDefault(p => p.Key == gone.Id)?.Faded == true ? "はい" : "いいえ")}・右の列={(lay.RightOpen ? "開" : "閉")}");
            Capture(dir, $"{no():D2}_ピン留め_3枠と消えた行.png");
            LogOuterFlow(gameView, "ピン留め");
            gvm.PinForCapture(gone);   // 消えた行の枠を外す（付け外しの切替）
            BootLog.Write($"uishot: 消えた行の枠を外した 残り{gvm.Pins.Count}枠");

            // ---- 枠を押して飛ぶ：別のページが選ばれている状態から、鋼の枠を押すと生産のページが開き、財の表の鋼の行へ着く ----
            lay.Open(Slot.Discovery, lay.Selected);
            await Task.Delay(600);
            var steelPin = gvm.Pins.FirstOrDefault(p => p.Key.StartsWith("r|production.goods|", StringComparison.Ordinal));
            steelPin?.OpenCommand.Execute(null);
            await Task.Delay(1200);
            ReportJump("ピン留め「鋼」", Slot.Production, "production.goods", Catalog.SteelId.ToString(), gvm, gameView);

            // ---- 問題の一覧：上位だけ／全部、押して飛ぶ ----
            lay.Reset(); lay.SetColumns(true, true);
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
                ReportJump($"問題「{ProblemScan.KindName(g.Key)}・{it.Target}」", (Slot)(int)it.Page, it.TableKey, it.RowKey, gvm, gameView, it.Section);
            }
            lay.Reset();
            await Task.Delay(600);

            // ---- 狭い分割で畳んだ欄：4 分割の生産のページで、畳まれた欄の見出しを押して開く（代わりに順位の低い開いた欄が畳まれる）----
            lay.SetShape(SplitShape.Four); lay.Open(Slot.Production, 0);
            await Task.Delay(1200);
            var page = Host(gameView)?.PaneAt(lay.PaneOf(Slot.Production))?.Page;
            var grid = page?.GetVisualDescendants().OfType<PageGrid>().FirstOrDefault();
            if (grid?.LastPlan is { } before)
            {
                var closedBefore = before.SelectMany(c => c).Where(p => p.Collapsed).Select(p => p.Key).ToList();
                Capture(dir, $"{no():D2}_狭い分割_畳んだ欄.png");
                if (closedBefore.Count > 0)
                {
                    grid.Reveal(closedBefore[0]);
                    await Task.Delay(900);
                    var closedAfter = grid.LastPlan!.SelectMany(c => c).Where(p => p.Collapsed).Select(p => p.Key).ToList();
                    BootLog.Write($"uishot: 狭い分割（4分割の生産）畳んだ欄 {string.Join(",", closedBefore)} → 「{closedBefore[0]}」を開く → 畳んだ欄 {string.Join(",", closedAfter)}（開いた欄が畳まれていない={!closedAfter.Contains(closedBefore[0])}）");
                    Capture(dir, $"{no():D2}_狭い分割_畳んだ欄を開いた図.png");
                    LogOuterFlow(gameView, "狭い分割で畳んだ欄を開いた図");
                }
                else BootLog.Write("uishot: 狭い分割（4分割の生産）畳んだ欄が無い");
            }
            else BootLog.Write("uishot: 狭い分割 生産のページの欄の割り当てが取れない");
        }
        catch (Exception ex) { BootLog.Write($"uishot: 最後の段 失敗 {ex}"); }
        finally { lay.Reset(); }
    }

    /// <summary>飛び先の確かめ：そのページが選んでいる枠に開いたか・表の行に着いたか（表を持たない対象は欄が畳まれていないか）。</summary>
    private void ReportJump(string what, Slot slot, string tableKey, string rowKey, GameViewModel gvm, GameView gameView, string section = "")
    {
        var lay = gvm.Layout;
        int pane = lay.PaneOf(slot);
        var page = pane >= 0 ? Host(gameView)?.PaneAt(pane)?.Page : null;
        string place = pane >= 0 ? $"枠{(char)('A' + pane)}に開いた" : "開いていない";
        string landed = "";
        if (page is not null && tableKey.Length > 0)
        {
            var tv = page.GetVisualDescendants().OfType<TableView>().FirstOrDefault(t => t.Snapshot?.Key == tableKey);
            landed = tv is null ? "・表が見つからない" : $"・表の行 {(tv.FocusedKey == rowKey ? "に着いた" : $"に着かない（着いたのは{tv.FocusedKey ?? "なし"}・行は{(tv.Snapshot!.Rows.Any(r => r.Key == rowKey) ? "ある" : "表に無い")}）")}";
        }
        if (page is not null && section.Length > 0 && page.GetVisualDescendants().OfType<PageGrid>().FirstOrDefault() is { LastPlan: { } plan })
            landed += $"・欄「{section}」{(plan.SelectMany(c => c).FirstOrDefault(p => p.Key == section) is { } sp ? (sp.Collapsed ? "は畳まれている" : "は開いている") : "が無い")}";
        BootLog.Write($"uishot: {what} → {SlotInfo.Name(slot)}が{place}{landed}");
    }
}
