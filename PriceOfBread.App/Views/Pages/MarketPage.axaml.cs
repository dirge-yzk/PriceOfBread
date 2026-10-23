using Avalonia.Controls;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.Views.Pages;

/// <summary>市場・清算・貿易のページ（設計 §3-3）。欄の並べの表（ページごとに 1 つだけ手で書く）はここ。</summary>
public partial class MarketPage : UserControl
{
    /// <summary>欄の宣言（鍵・名前・畳む順位・最小の行数・余りの高さの重み）と、幅が 3 列のときの置き場（列ごとに上から下）。
    /// 順位：財の値段 1・輸入 2・輸出 3・輸送 4・清算の線 5（数が大きいほど先に見出しの 1 行へ畳まれる）。</summary>
    public static readonly PageTable Plan = new(
    [
        new SectionSpec("prices", "財の値段", 1, MinRows: 10, Weight: 2),
        new SectionSpec("imports", "輸入", 2, MinRows: 6),
        new SectionSpec("exports", "輸出", 3, MinRows: 6),
        new SectionSpec("transport", "輸送", 4, MinRows: 4),
        new SectionSpec("clearing", "清算の線", 5, MinRows: 9),
    ],
    [["prices"], ["imports", "exports"], ["transport", "clearing"]]);

    public MarketPage()
    {
        InitializeComponent();
        PageRoot.Table = Plan;
        // 表は欄の高さに合わせる＝見出しを止めて行だけを欄の中で流す
        SectionFit.Attach(PricesSection, PricesTable);
        SectionFit.Attach(ImportsSection, ImportsBody);
        SectionFit.Attach(ExportsSection, ExportsBody);
        // 線グラフの目盛りは単位から決まる書式（月割の金額）
        foreach (var chart in new[] { ExportChart, ImportChart, BalanceChart })
            chart.LabelFormat = v => Fmt.Of(Unit.MoneyPerMonth, v);
    }
}
