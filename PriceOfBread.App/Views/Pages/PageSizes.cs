using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.App.Controls;
using PriceOfBread.App.Layout;

namespace PriceOfBread.App.Views.Pages;

/// <summary>窓の初めの大きさの見積り（作者指示 2026-11-23「全てのタブは、初期サイズで全情報が出るように」）。
/// 頁ごとに「いちばん広い列の自然な幅」（欄の枠と表の列の最小幅の合計の見積り）を 1 つだけ持ち、列の数ごとの窓の幅をここから導く。
/// 幅は列の数ごとの候補（<see cref="Candidates"/>）として窓の層へ渡す＝層が中身の高さを測り、盤の高さに全部入る最小の列の数を選ぶ。</summary>
public static class PageSizes
{
    /// <summary>窓の枠と頁の余白の合計（窓の縁 2＋頁の余白 8）。</summary>
    public const double Frame = 10;

    /// <summary>頁の欄の並べの表。窓にならない枠（生産・地図・州）は null。</summary>
    public static PageTable? PlanOf(Slot slot) => slot switch
    {
        Slot.Market => MarketPage.Plan,
        Slot.Trade => TradePage.Plan,
        Slot.Finance => FinancePage.Plan,
        Slot.FiscalMoney => FiscalMoneyPage.Plan,
        Slot.Population => PopulationPage.Plan,
        Slot.PoliticsLaw => PoliticsLawPage.Table,
        Slot.DiplomacyWar => DiplomacyWarPage.Table,
        Slot.Discovery => DiscoveryPage.Plan,
        Slot.Rights => RightsPage.Table,
        _ => null,
    };

    /// <summary>いちばん広い列の自然な幅（欄の枠を含む）。表の自然な幅（列の最小幅と名前の幅）に欄の枠 16 を足した見積り。</summary>
    public static double ColumnWidth(Slot slot) => slot switch
    {
        Slot.Market => 752,         // 財の値段を左右に 2 つ並べる（表 1 つが約 364＝名前 110・数の列 5 つ 188・列の間 40・縦の帯 14・間 8・欄の枠 16・作者指示 2026-11-23 列の外の余白を削る）
        Slot.Trade => 513,          // 輸出と輸入（段の押し釦を持たない表）
        Slot.FiscalMoney => 513,    // 支出と収入（段の押し釦 約 236 を持つ表）
        Slot.Finance => 513,        // 金融の表（段の押し釦 約 236 を持つ＝最も広い列）。国債と外資信用の表は右の列で約 316（比は ColumnWeights）
        Slot.Population => 610,     // 階級の表（列が多い）
        Slot.Discovery => 580,      // 技術（名前と棒だけの表が 2 つ並び＝各 約 270・間 8・欄の枠 16）が最も広い列・政府と思想と産地は右の列（比は ColumnWeights）
        Slot.DiplomacyWar => 666,   // 国の一覧が最も広い列（ColumnWeights の真ん中）
        _ => 530,                   // 法の表と選んだ法・権利と議会
    };

    /// <summary><paramref name="columns"/> 列に割ったときの窓の幅。重みの宣言（列の数が合うとき）があれば最も広い列に比を合わせ、無ければ均等。</summary>
    public static double Width(Slot slot, int columns)
    {
        var t = PlanOf(slot) ?? throw new ArgumentOutOfRangeException(nameof(slot), slot, "窓の頁ではない");
        double col = ColumnWidth(slot);
        double body;
        if (t.ColumnWeights is { } w && w.Count == columns && w.All(x => x > 0)) body = col * w.Sum() / w.Max();
        else body = col * columns;
        return Math.Ceiling(body + PagePlan.ColumnGap * (columns - 1) + Frame);
    }

    /// <summary>頁が割れる列の数（欄の置き場の列の数。最大 3）。</summary>
    public static int MaxColumns(Slot slot) => PlanOf(slot) is { } t ? Math.Clamp(t.Columns3.Count, 1, 3) : 1;

    /// <summary>最低でもこの列の数で開く頁（支出と収入・輸出と輸入・法の表と賛否・金融と国債は左右に並べる）。</summary>
    public static int MinColumns(Slot slot) => slot is Slot.Trade or Slot.FiscalMoney or Slot.PoliticsLaw or Slot.Finance ? Math.Min(2, MaxColumns(slot)) : 1;

    /// <summary>最小の列の数から順の窓の幅の候補（狭い順）。各候補の幅で頁が実際にその列の数に割れること（<see cref="PagePlan.ColumnCount(double, PageTable)"/>）は番人が確かめる。</summary>
    public static IReadOnlyList<double> Candidates(Slot slot) => PlanOf(slot) is null ? [540] : Enumerable.Range(MinColumns(slot), MaxColumns(slot) - MinColumns(slot) + 1).Select(n => Width(slot, n)).ToList();

    /// <summary>窓の初めの幅（候補の先頭）。</summary>
    public static double DefaultWidth(Slot slot) => Candidates(slot)[0];
}
