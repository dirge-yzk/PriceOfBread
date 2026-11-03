// GameViewModel.Layout.cs ＝ 画面の配置（(501) UI の作り直し 段2）。分割の形・各枠のページ・選んでいる枠・左右の列の開閉は LayoutModel が持ち、
// ここは「何が見えているか」を配置から導く読み取り専用の名前（旧の Tab*／ProductionShown／DiscoveryShown）と、見えた瞬間の更新・ヘッダーのページの押し釦・
// 左の列の問題の一覧とピン留めの器を持つ。新しい状態は作らない（見えているかは毎回 Layout から導く）。
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
    public bool DiplomacyShown => Layout.IsShown(Slot.DiplomacyWar);

    public bool LeftOpen => Layout.LeftOpen;
    public bool LeftClosed => !Layout.LeftOpen;
    public bool RightOpen => Layout.RightOpen;
    public bool RightClosed => !Layout.RightOpen;

    public ObservableCollection<PageButtonVM> PageButtons { get; } = new();

    private HashSet<Slot> _shownPrev = new();

    private void InitLayout()
    {
        foreach (var s in SlotInfo.All) PageButtons.Add(new PageButtonVM(s));
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
        OnPropertyChanged(nameof(LeftOpen)); OnPropertyChanged(nameof(LeftClosed)); OnPropertyChanged(nameof(RightOpen)); OnPropertyChanged(nameof(RightClosed));
        SyncPageButtons();
        LayoutStore.Save(Layout);
    }

    /// <summary>ページが見えた瞬間の更新（旧は「タブを開いた瞬間」）。以後は四半期の Refresh が見えているページだけを追従する。</summary>
    private void RefreshOnShown(Slot s)
    {
        switch (s)
        {
            case Slot.Production: TimeIt("生産ページが見えた(RefreshProduction)", RefreshProduction); break;
            case Slot.Market: RefreshTrade(); break;
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

    /// <summary>ヘッダーの押し釦＝選んでいる枠へ開く。</summary>
    [RelayCommand]
    private void OpenSlot(Slot slot) => OpenSlotByKey(slot, false);   // 押し釦＝キーと同じ（生産は全面）

    /// <summary>キー（Shift を足すと次の枠へ開く）。</summary>
    public void OpenSlotByKey(Slot slot, bool toNext)
    {
        if (toNext) Layout.OpenInNext(slot);
        else if (slot == Slot.Production) Layout.ToggleFull(Slot.Production);   // 生産のキー＝1 枚の全面（もう一度で前の配置へ）。見出しの一覧から選んだときは分割の中（PaneView.PickSlot は Open を呼ぶ）
        else Layout.OpenInSelected(slot);
    }

    /// <summary>Tab（作者の指示 2026-11-03）：生産の全面と地図の全面を切り替える。全面でなければ生産の全面へ入る（戻るのは Q・Esc・他のページのキー）。</summary>
    public void ToggleProductionMap() => Layout.FullTo(Layout.FullSlot == Slot.Production ? Slot.Map : Slot.Production);

    [RelayCommand] private void ToggleLeftColumn() => Layout.SetColumns(!Layout.LeftOpen, Layout.RightOpen);
    [RelayCommand] private void ToggleRightColumn() => Layout.SetColumns(Layout.LeftOpen, !Layout.RightOpen);
}
