using System;
using Avalonia.Controls;
using PriceOfBread.App.Layout;

namespace PriceOfBread.App.Views.Pages;

/// <summary>ページの部品の作り手（(501) 段2）。種類ごとに 1 つだけ作る（呼び手 GameView が使い回す）。地図は <see cref="MapPane"/>（GameView が持つ）なのでここには無い。
/// <para>段3 の持ち物（ページごとに別の担当が並行で作る＝自分の行のファイルだけを触る）：</para>
/// <list type="table">
/// <item><term>生産</term><description>Views/Pages/ProductionPage.axaml・ViewModels/GameViewModel.Production.cs</description></item>
/// <item><term>市場・清算・貿易</term><description>Views/Pages/MarketPage.axaml・GameViewModel.Trade.cs</description></item>
/// <item><term>財政と貨幣</term><description>Views/Pages/FiscalMoneyPage.axaml・GameViewModel.Fiscal.cs</description></item>
/// <item><term>人口</term><description>Views/Pages/PopulationPage.axaml・GameViewModel.Pop.cs・Rows.cs の人口の行</description></item>
/// <item><term>政治と法</term><description>Views/Pages/PoliticsLawPage.axaml・GameViewModel.Politics.cs・Law.cs・LawRowVM.cs</description></item>
/// <item><term>外交と戦争</term><description>Views/Pages/DiplomacyWarPage.axaml・GameViewModel.Diplomacy.cs・War.cs・Popups.cs の動詞</description></item>
/// <item><term>発見</term><description>Views/Pages/DiscoveryPage.axaml・GameViewModel.Discovery.cs</description></item>
/// </list>
/// 旧のタブ（Views/Tabs/*）は、ページが揃った回にその担当が消す。外枠（GameView.axaml・SplitHost・Layout）と目録の型は 1 人が持つ。</summary>
public static class PageFactory
{
    public static Control Create(Slot slot) => slot switch
    {
        Slot.Production => new ProductionPage(),
        Slot.Market => new MarketPage(),
        Slot.FiscalMoney => new FiscalMoneyPage(),
        Slot.Population => new PopulationPage(),
        Slot.PoliticsLaw => new PoliticsLawPage(),
        Slot.DiplomacyWar => new DiplomacyWarPage(),
        Slot.Discovery => new DiscoveryPage(),
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "地図はページの作り手の外（GameView.MapView）"),
    };
}
