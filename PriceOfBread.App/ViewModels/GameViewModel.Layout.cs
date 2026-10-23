// GameViewModel.Layout.cs ＝ 画面の配置（(501) UI の作り直し 段2）。分割の形・各枠のページ・選んでいる枠・左右の列の開閉は LayoutModel が持ち、
// ここは「何が見えているか」を配置から導く読み取り専用の名前（旧の Tab*／ProdOpen／DiscoveryOpen）と、見えた瞬間の更新・ヘッダーのページの押し釦・
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

/// <summary>問題の一覧の 1 行（設計 §5）。中身は段4（ProblemScan）が入れる＝今は器だけ。</summary>
public sealed partial class ProblemRow : ObservableObject
{
    public string Key { get; init; } = "";
    [ObservableProperty] private string _kind = "";
    [ObservableProperty] private string _target = "";
    [ObservableProperty] private string _sizeText = "";
    [ObservableProperty] private string _trend = "";
}

/// <summary>ピン留めの 1 枠（設計 §4-3）。中身は段4が入れる＝今は器だけ。行が消えた枠は薄くして名前だけ残す（Faded）。</summary>
public sealed partial class PinSlot : ObservableObject
{
    public string Key { get; init; } = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _valuesText = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Opacity))] private bool _faded;
    public double Opacity => Faded ? 0.45 : 1.0;
}

public partial class GameViewModel
{
    /// <summary>画面の配置。前回の配置を読む（無ければ初めの配置）。撮影と計測は保存先を別へ向けてある（<see cref="LayoutStore.PathOverride"/>）。</summary>
    public LayoutModel Layout { get; } = LayoutStore.Load();

    // ---- 見えているか（旧の名前のまま、配置から導く読み取り専用）----
    public bool ProdOpen => Layout.IsShown(Slot.Production);
    public bool DiscoveryOpen => Layout.IsShown(Slot.Discovery);
    public bool TabEcon => Layout.IsShown(Slot.FiscalMoney);
    public bool TabTrade => Layout.IsShown(Slot.Market);
    public bool TabWar => Layout.IsShown(Slot.DiplomacyWar);

    public bool LeftOpen => Layout.LeftOpen;
    public bool LeftClosed => !Layout.LeftOpen;
    public bool RightOpen => Layout.RightOpen;
    public bool RightClosed => !Layout.RightOpen;

    public ObservableCollection<PageButtonVM> PageButtons { get; } = new();
    public ObservableCollection<ProblemRow> ProblemRows { get; } = new();
    public ObservableCollection<PinSlot> Pins { get; } = new();

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
    private void OpenSlot(Slot slot) => Layout.OpenInSelected(slot);

    /// <summary>キー（Shift を足すと次の枠へ開く）。</summary>
    public void OpenSlotByKey(Slot slot, bool toNext)
    {
        if (toNext) Layout.OpenInNext(slot); else Layout.OpenInSelected(slot);
    }

    [RelayCommand] private void ToggleLeftColumn() => Layout.SetColumns(!Layout.LeftOpen, Layout.RightOpen);
    [RelayCommand] private void ToggleRightColumn() => Layout.SetColumns(Layout.LeftOpen, !Layout.RightOpen);
}
