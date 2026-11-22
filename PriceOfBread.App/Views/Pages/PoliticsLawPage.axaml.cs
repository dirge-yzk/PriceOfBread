using System;
using Avalonia;
using Avalonia.Controls;
using PriceOfBread.App.Controls;
using PriceOfBread.App.ViewModels;

namespace PriceOfBread.App.Views.Pages;

/// <summary>政治と法のページ（(501) 段3）。欄の並べの表（<see cref="Table"/>）を持つ＝ページのどこに欄を置くかは手で決める（設計 §6-9）。</summary>
public partial class PoliticsLawPage : UserControl
{
    /// <summary>欄・最小の行数・置き場。2026-11-11 作者指示：法を左の列いっぱい・右の列に政体（権力と議会は権利と議会のページ）。
    /// 政体の詳細・政治影響力・権力の奪い合い・政体の実績は畳んで政体の欄へ要点だけを寄せた。制度は発見のページ。</summary>
    public static PageTable Table { get; } = new(
    [
        new SectionSpec("law", "法の表", 12, 3),
        new SectionSpec("stakes", "選んだ法", 16),
        new SectionSpec("crown", "政体", 7),
    ],
    [
        ["law"],
        ["stakes", "crown"],
    ]);

    public PoliticsLawPage()
    {
        InitializeComponent();
        Sections.Table = Table;
        // 法の表の行（名前の余白）を押す＝その法を選ぶ（生産のページの財の表と同じ動詞）
        LawTableView.RowActivated += key => (DataContext as GameViewModel)?.SelectLaw(key);
    }

    /// <summary>枠に載って見えた瞬間（DataContext が付いた時）に、法の表を読み直す。見えている間は毎回の更新が追従する。</summary>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is GameViewModel vm) vm.RefreshPoliticsLawPage();
    }
}
