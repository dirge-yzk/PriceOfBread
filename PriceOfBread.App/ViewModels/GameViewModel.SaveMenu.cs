// GameViewModel.SaveMenu.cs ＝ セーブ/ロード（F5/F9）とEscメニュー・終局ランキング・タイトル復帰。
// ★2026-08-10のUI分割：GameViewModel.cs から**メンバーを移しただけ**（ロジック・順序とも不変）。
//   同じ partial class GameViewModel の一部＝どのファイルからでも全メンバーに触れる。
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.SimCore;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    // === セーブ/ロード（F5=クイックセーブ・F9=クイックロード・タイトル「続きから」）=====================
    // 中身＝UIエンベロープ（経過日数・年次グラフ）＋SimCoreの汎用グラフ直列化（SaveGame）。ロード後の進行はセーブ時点とビット一致（savetest検証済み）。

    private const string SaveMagic = "POBSAVE1";
    public static string SaveDir => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PriceOfBread");
    // ★スロット化（作者裁定2026-09-06「プレイヤー側UIはやる」・残作業11-1）：0＝クイック（F5/F9）・1〜3＝Escメニューのスロット。
    public const int SlotCount = 3;
    public static string SlotPath(int slot) => System.IO.Path.Combine(SaveDir, slot == 0 ? "quicksave.pob" : $"slot{slot}.pob");
    public static bool SaveExists => SaveExistsAt(0);
    public static bool SaveExistsAt(int slot) => System.IO.File.Exists(SlotPath(slot));
    /// <summary>最新のセーブのスロット（タイトル「続きから」）。無ければ -1。</summary>
    public static int NewestSlot()
    {
        int best = -1; DateTime t = DateTime.MinValue;
        for (int i = 0; i <= SlotCount; i++) if (SaveExistsAt(i) && System.IO.File.GetLastWriteTime(SlotPath(i)) > t) { t = System.IO.File.GetLastWriteTime(SlotPath(i)); best = i; }
        return best;
    }
    public string SlotLabel(int slot) => SaveExistsAt(slot) ? System.IO.File.GetLastWriteTime(SlotPath(slot)).ToString("yyyy/MM/dd HH:mm") : "（空）";
    public string Slot1Label => SlotLabel(1);
    public string Slot2Label => SlotLabel(2);
    public string Slot3Label => SlotLabel(3);
    public bool CanLoadSlot1 => SaveExistsAt(1);
    public bool CanLoadSlot2 => SaveExistsAt(2);
    public bool CanLoadSlot3 => SaveExistsAt(3);
    private void NotifySlots()
    {
        OnPropertyChanged(nameof(CanLoadSave)); OnPropertyChanged(nameof(Slot1Label)); OnPropertyChanged(nameof(Slot2Label)); OnPropertyChanged(nameof(Slot3Label));
        OnPropertyChanged(nameof(CanLoadSlot1)); OnPropertyChanged(nameof(CanLoadSlot2)); OnPropertyChanged(nameof(CanLoadSlot3));
    }

    [RelayCommand]
    private void SaveGameNow() => SaveToSlot(0);
    [RelayCommand]
    private void SaveSlot(string slot) => SaveToSlot(int.Parse(slot));

    private void SaveToSlot(int slot)
    {
        try
        {
            System.IO.Directory.CreateDirectory(SaveDir);
            using var fs = System.IO.File.Create(SlotPath(slot));
            using var bw = new System.IO.BinaryWriter(fs, System.Text.Encoding.UTF8, leaveOpen: true);
            bw.Write(SaveMagic);
            bw.Write(_playerIndex); bw.Write(_simDays); bw.Write(_dayInQuarterUI); bw.Write(_lastSampledYear);
            List<double>[] lists = [_histInd, _histCredit, _histFin, _histPop, _histGdp, _histProd, _histTech, _histLit, _histSat, _histMil, _histCorr, _histFood, _histLeg, _histCons, _histMandate, _histTreasury, _histGini, _histDis];   // 他20：_histFood追加・_histTreasury/_satHistory削除（保存/復元と同一の並び）。末尾に正統性・国民意識（2026-07-15）・政治資本（2026-07-16）・ジニ係数（2026-08-10）・★不満度（2026-09-23＝末尾追加・GameViewModel.cs の targets と同一順）
            bw.Write(lists.Length);
            foreach (var l in lists) { bw.Write(l.Count); foreach (var v in l) bw.Write(v); }
            bw.Flush();
            SaveGame.Save(_sim, fs);
            // 戦争UIの追跡状態（追記ブロック・レビュー2026-07-19）：戦費按分・勝敗基準・終わった戦争の残置。
            // 旧セーブには無い＝読み側が EOF を許容して null（新規追跡で再開）。
            bw.Write("POBWAR2");   // ★2026-09-23：民間人の死の2列を追記（POBWAR1＝その2列が無い版も同じ読み手で読める）
            bw.Write(_warCostStamp);
            bw.Write(_warTracks.Count);
            foreach (var (id, tr) in _warTracks)
            {
                bw.Write(id); bw.Write(tr.Won0); bw.Write(tr.Lost0); bw.Write(tr.LastScore);
                bw.Write(tr.LastMonths); bw.Write(tr.LastDeadSelf); bw.Write(tr.LastDeadEnemy);
                bw.Write(tr.LastCivDeadSelf); bw.Write(tr.LastCivDeadEnemy); bw.Write(tr.CostShare);
            }
            bw.Write(PastWarRows.Count);
            foreach (var pw in PastWarRows) { bw.Write(pw.Key); bw.Write(pw.EndDay); bw.Write(pw.Title); bw.Write(pw.Detail); }
            bw.Flush();
            DeliverNews("💾", slot == 0 ? "セーブした" : $"スロット{slot}にセーブした", NewsBlue, _simDays);
            NotifySlots();   // 初セーブでEscメニューの「ロード」が活性化
        }
        catch (Exception ex)
        {
            DeliverNews("⚠", $"セーブに失敗({ex.Message})", NewsRed, _simDays);
        }
    }

    public static GameViewModel? TryLoad(Action? onExit, Action? onLoad) => TryLoad(0, onExit, onLoad);
    public static GameViewModel? TryLoad(int slot, Action? onExit, Action? onLoad)
    {
        try
        {
            if (!SaveExistsAt(slot)) return null;
            using var fs = System.IO.File.OpenRead(SlotPath(slot));
            using var br = new System.IO.BinaryReader(fs, System.Text.Encoding.UTF8, leaveOpen: true);
            if (br.ReadString() != SaveMagic) return null;
            int pi = br.ReadInt32(); double simDays = br.ReadDouble(); int dq = br.ReadInt32(); int lsy = br.ReadInt32();
            int nLists = br.ReadInt32();
            var hist = new List<double>[nLists];
            for (int i = 0; i < nLists; i++)
            {
                int n = br.ReadInt32();
                var l = new List<double>(n);
                for (int k = 0; k < n; k++) l.Add(br.ReadDouble());
                hist[i] = l;
            }
            var sim = SaveGame.Load(fs);
            // 戦争UIの追跡状態（追記ブロック・レビュー2026-07-19）。旧セーブは EOF → null＝新規追跡で再開（自動オープンもしない）。
            WarUiSave? warUi = null;
            try
            {
                string warMagic = br.ReadString();
                bool war1 = warMagic == "POBWAR1";   // ★旧版＝民間人の死の2列が無い（0で始まり、次のRefreshでエンジンから入る）
                if (war1 || warMagic == "POBWAR2")
                {
                    int stamp = br.ReadInt32();
                    int nT = br.ReadInt32();
                    var tracks = new List<(long, int, int, double, int, double, double, double, double, double)>(nT);
                    for (int i = 0; i < nT; i++)
                    {
                        long id = br.ReadInt64(); int won0 = br.ReadInt32(), lost0 = br.ReadInt32();
                        double sc = br.ReadDouble(); int mo = br.ReadInt32();
                        double ds = br.ReadDouble(), de = br.ReadDouble();
                        double cs = war1 ? 0 : br.ReadDouble(), ce = war1 ? 0 : br.ReadDouble();
                        tracks.Add((id, won0, lost0, sc, mo, ds, de, cs, ce, br.ReadDouble()));
                    }
                    int nP = br.ReadInt32();
                    var pasts = new List<(string, double, string, string)>(nP);
                    for (int i = 0; i < nP; i++)
                        pasts.Add((br.ReadString(), br.ReadDouble(), br.ReadString(), br.ReadString()));
                    warUi = new WarUiSave(stamp, tracks, pasts);
                }
            }
            catch { warUi = null; }
            return new GameViewModel(sim, pi, simDays, dq, lsy, hist, onExit, onLoad, warUi);
        }
        catch
        {
            return null;   // 壊れたセーブ＝ロードせず現状維持（タイトルの「続きから」は SaveExists でのみ活性）
        }
    }

    private readonly Action? _onLoad;   // クイックロード（MainWindowViewModel がセーブから新VMを作って差し替える）

    public bool CanLoadSave => SaveExists;   // Escメニュー「ロード」の活性（staticはバインドできないためインスタンス経由）

    [RelayCommand]
    private void RequestLoad() => RequestLoadSlot("0");
    [RelayCommand]
    private void RequestLoadSlot(string slotText)
    {
        int slot = int.Parse(slotText);
        if (!SaveExistsAt(slot)) return;
        MenuOpen = false;   // メニュー（ZIndex10）が確認モーダル（ZIndex9）を覆わないよう先に閉じる（RequestReturnToTitleと同型）
        ConfirmTitle = slot == 0 ? "ロード" : $"ロード（スロット{slot}）";
        ConfirmBody = $"{SlotLabel(slot)}\n現在 {DateText}　{PlayerNationName}";
        ConfirmAcceptLabel = "ロード";
        ConfirmCan = true;
        _confirmDo = () => { Playing = false; if (slot == 0) _onLoad?.Invoke(); else OnLoadSlot?.Invoke(slot); };
        ConfirmOpen = true;
    }
    public Action<int>? OnLoadSlot { get; set; }        // MainWindowViewModel が差す（スロットのロード＝新VMへ差し替え）
    public Action<int>? OnSwitchPlayer { get; set; }    // 内戦の陣営選択（P8）：反乱側の新国家へ乗り換える

    /// <summary>ビューが外れた時に旧VMのタイマーを止める（ロード/タイトル復帰でバックグラウンド進行しない）。</summary>
    public void Shutdown() { Playing = false; _timerStopped = true; _timer?.Dispose(); }

    // === Escメニュー・終局・タイトル復帰 ===============================================

    /// <summary>Escキーの一元処理：開いている物を手前から閉じ、何も無ければメニューをトグル。</summary>
    public void HandleEscape()
    {
        if (MenuOpen) { MenuOpen = false; return; }
        if (DiscoveryOpen) { DiscoveryOpen = false; return; }   // 発見パネル（F）
        if (ProdOpen) { ProdOpen = false; return; }
        if (ConfirmOpen) { ConfirmCancel(); return; }
        if (EndingOpen) { EndingOpen = false; return; }
        if (TensionPopupOpen) { TensionIgnore(); return; }   // ✕と同じ（何もせず閉じる）
        if (WarEndPopupOpen) { WarEndAck(); return; }        // ✕／了解と同じ
        if (WarPopupOpen || FactionCrisisOpen || ElectionPopupOpen || CallToArmsOpen) return;   // 選択必須＝Escでは閉じず、メニューも重ねない
        if (ActiveTab.Length > 0) { ActiveTab = ""; return; }
        MenuOpen = true;
    }

    [RelayCommand] private void CloseMenu() => MenuOpen = false;

    // ★2026-08-24作者指示：Escメニューも時間停止（開いている間に世界が進まない）＝ポップアップと同じ1本。
    partial void OnMenuOpenChanged(bool value)
    {
        if (value) { OnPropertyChanged(nameof(CanLoadSave)); PopupPause(); }   // 開くたびセーブ有無を再評価
        else PopupResume();
    }

    [RelayCommand]
    private void ReturnToTitle()
    {
        Shutdown();   // タイマーを止めてから遷移（旧VMがバックグラウンドで動き続けない）
        _onExit?.Invoke();
    }

    // Escメニューの「タイトルに戻る」：確認モーダルを挟む（本文はデータのみ＝現在日付と国名）。
    [RelayCommand]
    private void RequestReturnToTitle()
    {
        MenuOpen = false;   // メニュー（ZIndex10）が確認モーダル（ZIndex9）を覆わないよう先に閉じる
        ConfirmTitle = "タイトルに戻る";
        ConfirmBody = $"{DateText}　{PlayerNationName}";
        ConfirmAcceptLabel = "タイトルに戻る";
        ConfirmCan = true;
        _confirmDo = ReturnToTitle;
        ConfirmOpen = true;
    }

    // 終局：1949年12月が終わった（=EndYear の1950年に入った＝GameQuarters 経過）らランキングを表示。以後は続行可（観戦）。
    private void ShowEnding()
    {
        _endingShown = true;
        var ranked = _sim.Nations.Select(n => n.World).OrderByDescending(NationalScore.Of).ToList();
        var rows = new List<EndingRow>();
        for (int i = 0; i < ranked.Count; i++)
        {
            var w = ranked[i];
            bool mine = ReferenceEquals(w, _player);
            rows.Add(new EndingRow
            {
                Order = $"{i + 1}",
                Name = w.SelfPower?.Name ?? w.Region.Name,
                Score = NationalScore.Of(w).ToString("F0"),
                Polity = PolityRegimeJa(PowerAffinity.PolityFromShares(w)),   // 導出政体＝ヘッダーのPolityTextと同じ物差し（2026-08-21作者裁定）
                Population = PopFmt(w.TotalPopulation),   // 内部単位=千人（他10）
                Industry = w.MeanIndustrialization.ToString("F2"),
                RowBrush = mine ? SelfRowHighlight : Brushes.Transparent,
            });
        }
        EndingRows = rows;
        EndingAnnals = _annals.ToList();   // 終局年代記（#129）：このプレイの政変・戦争・恐慌・デフォルト・建国の年表
        EndingTitle = $"{Calendar.EndYear}年 最終ランキング　{PlayerNationName}:{ranked.IndexOf(_player) + 1}位";
        Playing = false;
        EndingOpen = true;
    }

    [RelayCommand] private void CloseEnding() => EndingOpen = false;
}
