using Avalonia.Controls;
using Avalonia.VisualTree;
using PriceOfBread.App.Controls;
using PriceOfBread.App.Layout;
using PriceOfBread.App.ViewModels;
using PriceOfBread.App.Views.Pages;
using PriceOfBread.SimCore.Display;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PriceOfBread.App.Views;

// Views/MainWindow.Page3Shots.cs ＝ --uishot の撮影の段（(501) 段3：政治と法・外交と戦争）。
// 各ページを撮り、外側が縦に流れていないこと・欄の鍵が並べの表と一致すること・表の見出しと値の列の左端が揃うことを BootLog に書く。
// 加えて、仕組みの確かめ（段の押し釦が目標を置く・動詞が確かめの窓を開く・地図が欄と中央を往復する）を BootLog に書く。
public partial class MainWindow
{
    private async Task Page3Shots(GameView gameView, GameViewModel gvm, string dir)
    {
        var lay = gvm.Layout;
        try
        {
            foreach (var slot in new[] { Slot.PoliticsLaw, Slot.DiplomacyWar })
            {
                lay.Reset(); UiShotWindows.OpenFull(gameView, lay, slot);
                await Task.Delay(1100);
                string tag = SlotInfo.Name(slot);
                Capture(dir, $"p3_{(slot == Slot.PoliticsLaw ? "政治と法" : "外交と戦争")}.png");
                LogOuterFlow(gameView, tag);
                LogPage3(gameView, slot, tag);
            }

            // ---- 仕組みの確かめ ----
            // 段の押し釦：法の表の段を 1 つ押すと目標が置かれる（置く先は AI・議会と同じ口）。押した後の表が読み直されて目標の印が付く。確かめたら元の目標に戻す。
            lay.Reset(); UiShotWindows.OpenFull(gameView, lay, Slot.PoliticsLaw);
            await Task.Delay(700);
            CheckLawSelect(gvm);

            // 動詞：国の一覧の侵略は押すと確かめの窓が開く（押しただけでは侵略は起きない）。窓は取り消す。
            lay.Reset(); UiShotWindows.OpenFull(gameView, lay, Slot.DiplomacyWar);
            await Task.Delay(700);
            CheckNationVerb(gvm);

            // 地図の往復：欄（外交と戦争）→ 中央 → 欄。同じ部品が 2 か所の親に付く瞬間が無いこと（例外が出ないこと）と、各時点で欄が空かを書く。
            lay.Reset(); lay.Open(Slot.Production); UiShotWindows.OpenFull(gameView, lay, Slot.DiplomacyWar);   // 背景が生産の間は地図は外交と戦争の窓の欄に載る
            await Task.Delay(900);
            var dw = UiShotWindows.PageOf(gameView, Slot.DiplomacyWar) as DiplomacyWarPage;
            // 直前までのカメラは全景（撮影順の産物）＝自国の名の国の中心へ振る。行を押すと地図が動く経路（CenterNationCommand）の確かめも兼ねる
            gvm.CenterNationCommand.Execute(gvm.PlayerName.Split('　')[0]);
            await Task.Delay(600);
            BootLog.Write($"uishot: 地図の往復 1/4 欄に載せた 欄={(dw?.HostsMap == true)}・地図の部品 Loaded={gameView.MapView.IsLoaded}・{gameView.MapView.BoxReport()}");
            Capture(dir, "p3_外交と戦争_地図の欄.png");
            lay.Open(Slot.Map);
            await Task.Delay(900);
            BootLog.Write($"uishot: 地図の往復 2/4 背景の地図へ 欄={(dw?.HostsMap == true)}・地図の部品 Loaded={gameView.MapView.IsLoaded}・親={gameView.MapView.Parent?.GetType().Name}");
            Capture(dir, "p3_外交と戦争_地図は別の枠.png");
            lay.Open(Slot.Production);
            await Task.Delay(900);
            BootLog.Write($"uishot: 地図の往復 3/4 欄へ戻った 欄={(dw?.HostsMap == true)}・地図の部品 Loaded={gameView.MapView.IsLoaded}・親={gameView.MapView.Parent?.GetType().Name}");
            lay.Open(Slot.Map); await Task.Delay(500); lay.Close(Slot.DiplomacyWar); lay.Open(Slot.Discovery); await Task.Delay(500);
            BootLog.Write($"uishot: 地図の往復 4/4 もう一度 欄={(dw?.HostsMap == true)}・地図の部品 Loaded={gameView.MapView.IsLoaded}");
        }
        catch (Exception ex) { BootLog.Write($"uishot: 段3の撮影 失敗 {ex}"); }
        finally { lay.Reset(); }
    }

    /// <summary>ページの欄の鍵・畳んだ欄・表の列の揃いを BootLog に書く。</summary>
    private static void LogPage3(GameView gameView, Slot slot, string tag)
    {
        foreach (var (pane, s, page, _) in UiShotWindows.MountedAll(gameView))
        {
            if (s != slot) continue;
            var grid = page.GetVisualDescendants().OfType<PageGrid>().FirstOrDefault();
            var table = slot == Slot.PoliticsLaw ? PoliticsLawPage.Table : DiplomacyWarPage.Table;
            var keys = page.GetVisualDescendants().OfType<Section>().Select(x => x.Key).OrderBy(k => k, StringComparer.Ordinal).ToList();
            var want = table.Sections.Select(x => x.Key).OrderBy(k => k, StringComparer.Ordinal).ToList();
            string collapsed = grid?.LastPlan is { } plan ? string.Join("・", plan.SelectMany(c => c).Where(p => p.Collapsed).Select(p => table.Sections.First(q => q.Key == p.Key).Title)) : "?";
            string cols = grid?.LastPlan is { } pl ? pl.Count.ToString() : "?";
            var al = page.GetVisualDescendants().OfType<TableView>().Select(t => t.MeasureAlignment()).Where(a => a is not null).Select(a => a!.Value).ToList();
            string align = al.Count == 0 ? "表なし" : string.Join(" / ", al.Select(a => $"{a.Columns}列 ずれ最大{a.MaxDelta:F1}" + (a.MaxDelta > 0.5 ? $"(列「{a.Worst}」)" : "")));
            BootLog.Write($"uishot: 段3[{tag}] 欄の鍵 {(keys.SequenceEqual(want) ? "表と一致" : "不一致 画面=" + string.Join(",", keys) + " 表=" + string.Join(",", want))}・{cols}列・畳んだ欄 {(collapsed.Length == 0 ? "なし" : collapsed)}・見出しと値の列の左端 {align}");
        }
    }

    /// <summary>法の表の段の押し釦が目標を置くことを確かめる（確かめたら元の目標に戻す）。</summary>
    private static void CheckLawSelect(GameViewModel gvm)
    {
        StageCell? Stage(string key) => gvm.LawTable.Rows.FirstOrDefault(r => r.Key == key)?.Cells.Select(c => c.Extra).OfType<StageCell>().FirstOrDefault();
        foreach (var row in gvm.LawTable.Rows)
        {
            if (Stage(row.Key) is not { } st) continue;
            int before = st.Target, pick = -1;
            for (int i = 0; i < st.Labels.Count; i++) { int lv = st.LevelBase + i; if (lv != st.Current && lv != before && st.CanSelect(lv)) { pick = lv; break; } }
            if (pick < 0) continue;
            st.Select(pick);
            int after = Stage(row.Key)?.Target ?? -1;
            BootLog.Write($"uishot: 段の押し釦 {row.Name} 目標 {before}→{after}（押した段 {pick}）{(after == pick ? "反映" : "反映されない")}・ホバー先頭行「{st.Hover(pick)?.Split((char)10)[0]}」");
            Stage(row.Key)!.Select(before);
            BootLog.Write($"uishot: 段の押し釦 {row.Name} 元の目標へ戻した {(Stage(row.Key)?.Target == before ? "確認" : "戻らない")}");
            return;
        }
        BootLog.Write($"uishot: 段の押し釦 押せる段のある法が無い（{gvm.LawTable.Rows.Count}行）");
    }

    /// <summary>国の一覧の動詞が確かめの窓を開くことを確かめる（窓は取り消す＝実際には侵略しない）。</summary>
    private static void CheckNationVerb(GameViewModel gvm)
    {
        var verbs = gvm.NationTable.Rows.SelectMany(r => r.Cells.Select(c => c.Extra).OfType<VerbCell>()).ToList();
        var enabled = verbs.FirstOrDefault(v => v.Enabled);
        var any = enabled ?? verbs.FirstOrDefault();
        BootLog.Write($"uishot: 国の一覧 {gvm.NationTable.Rows.Count}行・動詞 {verbs.Count}（押せる {verbs.Count(v => v.Enabled)}）");
        if (any is null) return;
        bool wasOpen = gvm.ConfirmOpen;
        TableView.DispatchVerb(any, TableView.DefaultConfirm);
        BootLog.Write($"uishot: 動詞「{any.Name}」を押した直後 確かめの窓={(gvm.ConfirmOpen ? "開いた" : "開かない")}（押す前 {wasOpen}）・題「{gvm.ConfirmTitle}」");
        gvm.ConfirmCancelCommand.Execute(null);
        BootLog.Write($"uishot: 確かめの窓を取り消した 窓={gvm.ConfirmOpen}");
    }
}
