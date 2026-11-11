using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using PriceOfBread.App.Controls;
using PriceOfBread.App.Layout;
using PriceOfBread.App.ViewModels;
using PriceOfBread.SimCore.Display;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PriceOfBread.App.Views;

/// <summary>--uishot の撮影の段（(501) 段3・財政と貨幣／人口／発見）。各ページを撮り、外側が縦に流れていないこと（LogOuterFlow）・
/// 表の見出しの列と値の列の左端が揃うこと・畳まれた欄・段の押し釦の書き込みが表へ戻ることを BootLog に書く。</summary>
public partial class MainWindow
{
    private async Task ShootPagesFiscalPopulationDiscovery(string dir, GameViewModel gvm, GameView gameView, LayoutModel lay)
    {
        foreach (var slot in new[] { Slot.FiscalMoney, Slot.Population, Slot.Discovery })
        {
            lay.Reset(); UiShotWindows.OpenFull(gameView, lay, slot);
            await Task.Delay(1300);
            string tag = SlotInfo.Name(slot);
            Capture(dir, $"P2_{SlotInfo.Name(slot)}.png");
            LogOuterFlow(gameView, tag);
            LogTables(gameView, slot, tag);
        }

        // 段の押し釦の書き込み：行政費を最上段へ置く→表の目標の段が動く→元へ戻す（押した直後に停止中でも表が読み直されること）
        lay.Reset(); UiShotWindows.OpenFull(gameView, lay, Slot.FiscalMoney);
        await Task.Delay(900);
        try
        {
            StageCell? Find(string rowKey) => gvm.SpendTable.Rows.FirstOrDefault(r => r.Key == rowKey)?.Cells.Select(c => c.Extra).OfType<StageCell>().FirstOrDefault();
            if (Find("admin") is { } st0)
            {
                int before = st0.Target;
                int top = st0.LevelBase + st0.Labels.Count - 1;
                st0.Select(top);
                var st1 = Find("admin");
                bool moved = st1 is not null && st1.Target == top;
                // 押せない段（前提が足りない）を押しても盤面は動かさない：政策金利はトリレンマで掴めない時だけ全段が押せない
                st1?.Select(before);
                var st2 = Find("admin");
                BootLog.Write($"uishot: 段の押し釦の書き込み 行政費 {before}→{top}→{before}　目標が追従={(moved ? "はい" : "いいえ")}　戻した後={(st2 is not null && st2.Target == before ? "元どおり" : "ずれ")}");
            }
            else BootLog.Write("uishot: 段の押し釦の書き込み 行政費の段が表に無い");
        }
        catch (Exception ex) { BootLog.Write($"uishot: 段の押し釦の書き込み 失敗 {ex}"); }
        BootLog.Write($"uishot: 財政の表 支出{gvm.SpendTable.Rows.Count}行・収入{gvm.IncomeTable.Rows.Count}行・金融{gvm.FinanceTable.Rows.Count}行・国債{gvm.DebtTable.Rows.Count}行・見出し「{gvm.SpendTitle}」「{gvm.IncomeTitle}」");
        BootLog.Write($"uishot: 年次の線の点の数 国庫{gvm.TreasuryValues.Count}・国債{gvm.GovDebtValues.Count}・粗出生率{gvm.BirthRateValues.Count}・粗死亡率{gvm.DeathRateValues.Count}・合計出生率{gvm.FertilityValues.Count}（人口の線は人口のページを開いた後）　年齢の層{gvm.AgePyramid.Labels.Count}層");
        lay.Reset();
        await Task.Delay(300);
    }

    /// <summary>ページの表ごとに、見出しの列と値の列の左端が揃っているか・行の数・畳まれた欄を BootLog に書く（設計 §7-3）。</summary>
    private void LogTables(GameView gv, Slot slot, string tag)
    {
        foreach (var (_, s, page, _) in UiShotWindows.MountedAll(gv))
        {
            if (s != slot) continue;
            var parts = new System.Collections.Generic.List<string>();
            int gaps = 0, measured = 0;
            foreach (var tv in page.GetVisualDescendants().OfType<TableView>())
            {
                if (!tv.IsVisible || !(tv.Bounds.Width > 0)) continue;
                var (g, m) = tv.AlignmentGaps();
                gaps += g; measured += m;
                parts.Add($"{tv.Snapshot?.Name}:{tv.RowCount}行{(g > 0 ? "・左端のずれ" + g : "")}");
            }
            string collapsed = "";
            if (page.GetVisualDescendants().OfType<PageGrid>().FirstOrDefault() is { LastPlan: { } plan })
                collapsed = "畳んだ欄=" + string.Join(",", plan.SelectMany(c => c).Where(p => p.Collapsed).Select(p => p.Key));
            BootLog.Write($"uishot: 表の左端[{tag}] 見出しと値の列の左端のずれ {gaps}/{measured}列　{string.Join(" ", parts)}　{collapsed}");
        }
    }
}
