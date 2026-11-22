// GameViewModel.Discovery.cs ＝ 発見のページ（(501) UI の作り直し 段3）。
// 発見＝解禁の唯一の形の現在地をデータで見せる：発見の表（政府・思想・技術＝Discovery.Evaluate の 2 値・解禁済みは 100%・まだの行は点数の割合・技術力と必要性と伝わりの 3 項）と産地の表（大鉱床・植林の 2 項）。
// 行と列は本体の目録（SimCore/Display/DiscoveryTables）が渡す表を共通の表（Controls/TableView）が描く。値は全てエンジンが判定に読む同じ関数＝画面に第二の式を置かない。
// ⚠UI 規律＝説明文なし・データのみ・漢字 1 字の略称なし。閾値は出さない（作者裁定 2026-09-23）・割合は出す。
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    [ObservableProperty] private TableSnapshot _discoveryTable = TableSnapshot.Empty;
    [ObservableProperty] private TableSnapshot _socialDiscoveryTable = TableSnapshot.Empty;
    [ObservableProperty] private TableSnapshot _siteTable = TableSnapshot.Empty;

    /// <summary>技術の表を左右に割った 2 本（行の順は今のまま・前半が左・後半が右）。</summary>
    [ObservableProperty] private TableSnapshot _discoveryTableLeft = TableSnapshot.Empty;
    [ObservableProperty] private TableSnapshot _discoveryTableRight = TableSnapshot.Empty;

    /// <summary>発見の棒の色の凡例（本体の目録の色＝棒と数の列の文字と同じ）。</summary>
    public static IReadOnlyList<LegendItem> DiscoveryLegend { get; } =
    [
        new("技術力", Avalonia.Media.Brush.Parse(DiscoveryTables.TechInk)),
        new("必要性", Avalonia.Media.Brush.Parse(DiscoveryTables.NeedInk)),
        new("伝播", Avalonia.Media.Brush.Parse(DiscoveryTables.TransmitInk)),
        new("解禁済み", Avalonia.Media.Brush.Parse(DiscoveryTables.KnownInk)),
    ];

    /// <summary>発見のページの再計算（見えている間は四半期の Refresh から追従・見えた瞬間にも 1 回）。</summary>
    public void RefreshDiscovery()
    {
        DiscoveryTable = TableSnapshot.From(DiscoveryTables.Discoveries.Read(_sim, _player));
        int half = (DiscoveryTable.Rows.Count + 1) / 2;
        DiscoveryTableLeft = DiscoveryTable with { Key = DiscoveryTable.Key + ".left", Rows = DiscoveryTable.Rows.Take(half).ToList() };
        DiscoveryTableRight = DiscoveryTable with { Key = DiscoveryTable.Key + ".right", Rows = DiscoveryTable.Rows.Skip(half).ToList() };
        SocialDiscoveryTable = TableSnapshot.From(DiscoveryTables.SocialDiscoveries.Read(_sim, _player));
        SiteTable = TableSnapshot.From(DiscoveryTables.Sites.Read(_sim, _player));
    }
}
