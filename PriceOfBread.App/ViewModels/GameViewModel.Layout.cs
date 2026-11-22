// GameViewModel.Layout.cs ＝ 画面の配置（(501) UI の作り直し 段2。2026-11-04 に分割とピン留めを廃止）。中央に出しているもの・左の列の開閉は LayoutModel が持ち、
// ここは「何が見えているか」を配置から導く読み取り専用の名前（旧の Tab*／ProductionShown／DiscoveryShown）と、見えた瞬間の更新・ヘッダーのページの押し釦を持つ。
// 新しい状態は作らない（見えているかは毎回 Layout から導く）。
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.App.Layout;

namespace PriceOfBread.App.ViewModels;

/// <summary>ヘッダーのページの押し釦 1 つ。見えているか（Active）・動かせる法がある（Alert）・交戦中（War）の色だけを持つ。</summary>
public sealed partial class PageButtonVM : ObservableObject
{
    public PageButtonVM(Slot slot) { Slot = slot; Text = SlotInfo.ButtonText(slot); }
    public Slot Slot { get; }
    public string Text { get; }
    [ObservableProperty] private bool _active;
    [ObservableProperty] private bool _alert;
    [ObservableProperty] private bool _war;
}

public partial class GameViewModel
{
    /// <summary>画面の配置。前回の配置を読む（無ければ初めの配置）。撮影と計測は保存先を別へ向けてある（<see cref="LayoutStore.PathOverride"/>）。</summary>
    public LayoutModel Layout { get; } = LayoutStore.Load();

    // ---- 見えているか（旧の名前のまま、配置から導く読み取り専用）----
    public bool ProductionShown => Layout.IsShown(Slot.Production);
    public bool DiscoveryShown => Layout.IsShown(Slot.Discovery);
    public bool FiscalShown => Layout.IsShown(Slot.FiscalMoney);
    public bool MarketShown => Layout.IsShown(Slot.Market);
    public bool TradeShown => Layout.IsShown(Slot.Trade);
    public bool DiplomacyShown => Layout.IsShown(Slot.DiplomacyWar);

    public bool LeftOpen => Layout.LeftOpen;
    public bool LeftClosed => !Layout.LeftOpen;

    public ObservableCollection<PageButtonVM> PageButtons { get; } = new();

    private HashSet<Slot> _shownPrev = new();

    private void InitLayout()
    {
        Layout.Open(Slot.Province);   // 窓「州」は最初から開く（地図で指した州の情報。閉じても D などのキーは無い＝ヘッダーの釦も無い）
        foreach (var s in SlotInfo.Pages) PageButtons.Add(new PageButtonVM(s));   // 地図は釦を持たない（Tab で生産と切り替える）
        _shownPrev = new HashSet<Slot>(Layout.Shown());
        Layout.Changed += OnLayoutChanged;
        SyncPageButtons();
    }

    private void OnLayoutChanged()
    {
        var now = new HashSet<Slot>(Layout.Shown());
        var added = new List<Slot>();
        foreach (var s in now) if (!_shownPrev.Contains(s)) added.Add(s);
        _shownPrev = now;
        if (now.Contains(Slot.PoliticsLaw)) PoliticsTabAlert = false;   // 動かせる法の点灯は、政治と法のページを見たら消灯
        foreach (var s in added) RefreshOnShown(s);
        OnPropertyChanged(nameof(LeftOpen)); OnPropertyChanged(nameof(LeftClosed));
        SyncPageButtons();
        LayoutStore.Save(Layout);
    }

    /// <summary>ページが見えた瞬間の更新（旧は「タブを開いた瞬間」）。以後は四半期の Refresh が見えているページだけを追従する。</summary>
    private void RefreshOnShown(Slot s)
    {
        switch (s)
        {
            case Slot.Production: TimeIt("生産ページが見えた(RefreshProduction)", RefreshProduction); break;
            case Slot.Market or Slot.Trade: RefreshTrade(); break;
            case Slot.FiscalMoney: UpdateForecast(); break;
            case Slot.Population: RefreshPopulation(); break;
            case Slot.DiplomacyWar: RefreshFactions(); break;
            case Slot.Discovery: RefreshDiscovery(); break;
        }
    }

    /// <summary>ヘッダーの押し釦の色を配置と状態から付け直す。</summary>
    private void SyncPageButtons()
    {
        foreach (var b in PageButtons)
        {
            b.Active = Layout.IsShown(b.Slot);
            b.Alert = b.Slot == Slot.PoliticsLaw && PoliticsTabAlert;
            b.War = b.Slot == Slot.DiplomacyWar && PlayerAtWar;
        }
    }

    partial void OnPlayerAtWarChanged(bool value) => SyncPageButtons();

    /// <summary>ヘッダーの押し釦＝キーと同じ（窓は開閉・生産は背景の地図と切り替え）。</summary>
    [RelayCommand]
    private void OpenSlot(Slot slot) { if (slot == Slot.Production) Layout.ToggleProductionMap(); else Layout.Toggle(slot); }

    /// <summary>Tab（作者の指示 2026-11-04）：生産と地図（背景）を切り替える。</summary>
    public void ToggleProductionMap() => Layout.ToggleProductionMap();

    [RelayCommand] private void ToggleLeftColumn() => Layout.SetLeftOpen(!Layout.LeftOpen);
}
