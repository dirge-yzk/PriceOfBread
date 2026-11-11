using System;
using Avalonia;
using Avalonia.Controls;
using PriceOfBread.App.Controls;
using PriceOfBread.App.ViewModels;

namespace PriceOfBread.App.Views.Pages;

/// <summary>政治と法のページ（(501) 段3）。欄の並べの表（<see cref="Table"/>）を持つ＝ページのどこに欄を置くかは手で決める（設計 §6-9）。</summary>
public partial class PoliticsLawPage : UserControl
{
    /// <summary>欄と畳む順位（1＝最後まで残る）・最小の行数・置き場。2026-11-11 作者指示：法を左の列いっぱい・右の列に政治（権力・政体・議会）。
    /// 政体の詳細・政治影響力・権力の奪い合い・政体の実績は畳んで政体の欄へ要点だけを寄せた。制度は発見のページ。</summary>
    public static PageTable Table { get; } = new(
    [
        new SectionSpec("law", "法の表", 1, 12, 3),
        new SectionSpec("power", "権力", 2, 9),
        new SectionSpec("crown", "政体", 2, 7),
        new SectionSpec("parliament", "議会", 3, 10, 2),
    ],
    [
        ["law"],
        ["power", "crown", "parliament"],
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
