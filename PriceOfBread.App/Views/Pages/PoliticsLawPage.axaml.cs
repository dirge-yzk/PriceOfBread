using System;
using Avalonia;
using Avalonia.Controls;
using PriceOfBread.App.Controls;
using PriceOfBread.App.ViewModels;

namespace PriceOfBread.App.Views.Pages;

/// <summary>政治と法のページ（(501) 段3）。欄の並べの表（<see cref="Table"/>）を持つ＝ページのどこに欄を置くかは手で決める（設計 §6-9）。</summary>
public partial class PoliticsLawPage : UserControl
{
    /// <summary>欄と畳む順位（1＝最後まで残る）・最小の行数・3 列のときの置き場（列ごとに上から下）。2 列・1 列は 3 列の並びを左から順に積む。
    /// 法の表が最後まで残る。制度と政体の実績は先に見出しの 1 行へ畳まれる。</summary>
    public static PageTable Table { get; } = new(
    [
        new SectionSpec("law", "法の表", 1, 12, 3),
        new SectionSpec("power", "権力", 2, 9),
        new SectionSpec("crown", "王権と議会", 3, 7),
        new SectionSpec("parliament", "議会", 4, 10, 2),
        new SectionSpec("polity", "政体", 4, 3),
        new SectionSpec("bloc", "政治影響力", 5, 4),
        new SectionSpec("struggle", "権力の奪い合い", 5, 4),
        new SectionSpec("regime", "政体の実績", 6, 4),
        new SectionSpec("institution", "制度", 6, 3),
    ],
    [
        ["law", "institution"],
        ["power", "crown", "polity", "bloc", "struggle"],
        ["parliament", "regime"],
    ]);

    public PoliticsLawPage()
    {
        InitializeComponent();
        Sections.Table = Table;
    }

    /// <summary>枠に載って見えた瞬間（DataContext が付いた時）に、法の表を読み直す。見えている間は毎回の更新が追従する。</summary>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is GameViewModel vm) vm.RefreshPoliticsLawPage();
    }
}
