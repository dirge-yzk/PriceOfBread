using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using PriceOfBread.App.Controls;
using PriceOfBread.App.Layout;
using PriceOfBread.App.ViewModels;

namespace PriceOfBread.App.Views;

/// <summary>--uishot の生産ページ・市場ページの撮影の段と検査（(501) 段3）。MainWindow の撮影の流れから 1 行で呼ぶ。
/// ・生産と市場を 1 枚ずつ撮る。外側が縦に流れていないことは呼び手の判定（<c>LogOuterFlow</c>）を使う。
/// ・各表について〈見出しの列の左端＝値の列の左端〉を測って BootLog に書く。盤（財カード）が中央の幅に追従したことも書く。</summary>
internal static class UiShotPageP1
{
    /// <summary>撮影の段。<paramref name="capture"/>＝画面を名前つきで保存する口／<paramref name="logOuter"/>＝外側の流れの判定を BootLog に書く口。</summary>
    public static async Task Run(Window window, GameViewModel gvm, string dir, Action<string> capture, Action<string> logOuter, Func<int> nextNo)
    {
        var lay = gvm.Layout;
        // 盤は先頭（素材の層）から撮る（前の撮影の送りを戻す）
        foreach (var sv in window.GetVisualDescendants().OfType<ScrollViewer>().Where(v => v.Name == "ProdScroll")) sv.Offset = default;
        // 財を 1 つ選んだ状態（詳細と州別の工場に中身が出る）
        gvm.SelectProdGood(PriceOfBread.SimCore.Catalog.SteelId);
        lay.Open(Slot.Production);
        await Task.Delay(1400);
        capture($"{nextNo():D2}_生産.png");
        logOuter("生産");
        Report(window, "生産");
        if (UiShotWindows.GameViewOf(window) is { } gvw) UiShotWindows.OpenFull(gvw, lay, Slot.Market); else lay.Open(Slot.Market);
        await Task.Delay(900);
        capture($"{nextNo():D2}_市場.png");
        logOuter("市場");
        Report(window, "市場");
        lay.Reset();
        await Task.Delay(300);
    }

    /// <summary>載っているページの検査：欄の割り当て・表の列の左端・盤の幅。</summary>
    private static void Report(Window window, string tag)
    {
        foreach (var grid in window.GetVisualDescendants().OfType<PageGrid>())
        {
            if (!grid.IsEffectivelyVisible || grid.LastPlan is not { } plan) continue;
            string cols = string.Join(" ｜ ", plan.Select(c => string.Join(" ", c.Select(p => $"{p.Key}{p.Height:F0}"))));
            BootLog.Write($"uishot: 欄の割り当て[{tag}] 幅{grid.Bounds.Width:F0}×{grid.Bounds.Height:F0}　{cols}");
        }
        foreach (var tv in window.GetVisualDescendants().OfType<TableView>())
        {
            if (!tv.IsEffectivelyVisible || tv.Bounds.Width <= 0) continue;
            BootLog.Write($"uishot: 表の列の左端[{tag}] {tv.Snapshot?.Key}　{Align(tv)}");
        }
        foreach (var sv in window.GetVisualDescendants().OfType<ScrollViewer>().Where(v => v.Name == "ProdScroll" && v.IsEffectivelyVisible))
            BootLog.Write($"uishot: 盤[{tag}] 視野{sv.Viewport.Width:F0}×{sv.Viewport.Height:F0}　盤{sv.Extent.Width:F0}×{sv.Extent.Height:F0}　横に{(sv.Extent.Width > sv.Viewport.Width + 1 ? "はみ出す" : "収まる")}");
    }

    /// <summary>見出しの列の左端と、最初の行の同じ列の左端の差（最大・px）。列の大きさは見出しと行が共有する＝差 0 が正しい。</summary>
    private static string Align(TableView tv)
    {
        var root = tv.GetVisualDescendants().OfType<Grid>().FirstOrDefault();
        var header = root?.Children.OfType<Grid>().FirstOrDefault(g => Grid.GetRow(g) == 1);
        var host = root?.Children.OfType<Border>().FirstOrDefault(b => Grid.GetRow(b) == 2);
        if (root is null || header is null || host is null || header.ColumnDefinitions.Count == 0) return "見出しなし";
        var row = host.GetVisualDescendants().OfType<Grid>().FirstOrDefault(g => g.ColumnDefinitions.Count == header.ColumnDefinitions.Count && g.Children.Count > 0);
        if (row is null) return $"行なし（列{header.ColumnDefinitions.Count}）";
        var h0 = header.TranslatePoint(new Point(0, 0), tv); var r0 = row.TranslatePoint(new Point(0, 0), tv);
        if (h0 is null || r0 is null) return "位置が取れない";
        double maxDiff = 0; double hx = h0.Value.X, rx = r0.Value.X;
        var diffs = new List<string>();
        for (int i = 0; i < header.ColumnDefinitions.Count; i++)
        {
            double d = Math.Abs(hx - rx);
            if (d > maxDiff) maxDiff = d;
            if (d > 0.5) diffs.Add($"列{i}:{d:F1}");
            hx += header.ColumnDefinitions[i].ActualWidth; rx += row.ColumnDefinitions[i].ActualWidth;
        }
        return $"列{header.ColumnDefinitions.Count}　見出しと値の左端の差 最大{maxDiff:F1}px（見出しの幅{header.Bounds.Width:F0}・行の幅{row.Bounds.Width:F0}）" + (diffs.Count > 0 ? "　ずれ " + string.Join(" ", diffs) : "");
    }
}
