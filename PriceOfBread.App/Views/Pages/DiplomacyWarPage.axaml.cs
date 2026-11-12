using System;
using Avalonia.Controls;
using PriceOfBread.App.Controls;
using PriceOfBread.App.ViewModels;

namespace PriceOfBread.App.Views.Pages;

/// <summary>外交と戦争のページ（(501) 段3）。欄の並べの表（<see cref="Table"/>）を持つ＝ページのどこに欄を置くかは手で決める（設計 §6-9）。
/// 地図の欄は 2026-11-11 作者指示で削除（地図は背景だけ）。行を押すと背景の地図がその国の中心へ動く。</summary>
public partial class DiplomacyWarPage : UserControl
{
    /// <summary>欄と畳む順位（1＝最後まで残る）・最小の行数・3 列のときの置き場（列ごとに上から下）。2 列・1 列は 3 列の並びを左から順に積む。
    /// 国の一覧が最後まで残る。戦争は次。終わった戦争と他国の戦争と反乱勢力は先に見出しの 1 行へ畳まれる。</summary>
    public static PageTable Table { get; } = new(
    [
        new SectionSpec("nations", "国の一覧", 1, 10, 3),
        new SectionSpec("war", "戦争", 2, 6),
        new SectionSpec("army", "軍事力の必要量", 4, 10),
        new SectionSpec("faction", "反乱勢力", 5, 5),
        new SectionSpec("mediation", "他国の戦争", 6, 4),
        new SectionSpec("past", "終わった戦争", 7, 3),
    ],
    [
        ["war", "past"],
        ["nations"],
        ["army", "faction", "mediation"],
    ], ColumnWeights: [1, 1.4, 1]);   // 国の一覧は列が多い（順位・国名・関係・緊張・宣戦布告とその理由＝約 650）＝真ん中の列だけを広げ、理由の列が欄の外へ出ないようにする。列の数は幅で決まる（広い幅では 3 列のまま・2026-11-12 点検2回目 2-2）

    public DiplomacyWarPage()
    {
        InitializeComponent();
        Sections.Table = Table;
        NationList.Legend = GameViewModel.PolityLegend;   // 国名の前の丸の色（政体）の凡例（B49）
        NationList.RowActivated += key => (DataContext as GameViewModel)?.CenterNationCommand.Execute(key);   // 行を押すと地図がその国の中心へ動く（地図が欄か枠に載っている間だけ効く）
    }

    /// <summary>枠に載って見えた瞬間（DataContext が付いた時）に、国の一覧と国内勢力を読み直す。見えている間は毎月の更新が追従する。</summary>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is GameViewModel vm) vm.RefreshDiplomacyPage();
    }
}
