// GameViewModel.War.cs ＝ 戦争タブ＝交戦中/終わった戦争の行と、戦費按分・勝敗基準の追跡（WarTrack）。
// ★2026-08-10のUI分割：GameViewModel.cs から**メンバーを移しただけ**（ロジック・順序とも不変）。
//   同じ partial class GameViewModel の一部＝どのファイルからでも全メンバーに触れる。
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    // 戦争タブ（2026-07-19）：交戦中の戦争ごとの行（戦勝点・犠牲者・戦費・厭戦）＋終わった戦争（講和後 PastWarKeepYears 年残す）。
    // 開戦で自動的に戦争タブを開く（旧・戦争モーダルは廃止）。
    public ObservableCollection<WarRow> WarRows { get; } = new();
    public ObservableCollection<PastWarRow> PastWarRows { get; } = new();
    // ★他国の戦争への調停（作者裁定2026-09-06「プレイヤー側UIはやる」）：AIと同じ述語（CanOfferPeace）と同じ本体（PlayerOfferPeace）。
    public ObservableCollection<MediationRow> MediationRows { get; } = new();
    [ObservableProperty] private bool _noMediation = true;

    [RelayCommand]
    private void RequestMediate(string key)
    {
        if (_player.SelfPower is not { } me) return;
        var war = FindWarByKey(key);
        if (war is null) return;
        var c = _player.Coef;
        if (!_sim.Diplomacy.CanOfferPeace(me, war, c, out var reason)) { DeliverNews("⚖", $"調停できない：{reason}", NewsRed, _simDays); return; }
        double cost = _sim.Diplomacy.InterventionWillCost(me, war, c);
        ConfirmTitle = "調停の提案";
        ConfirmBody = $"{war.A.Name} と {war.B.Name}\n余剰戦力 {WillPct(cost)} を消費　現在 {WillPct(_player.ExpansionWill)}\n拒まれれば面子＝緊張";
        ConfirmAcceptLabel = "提案する";
        ConfirmCan = true;
        _confirmDo = () =>
        {
            var w2 = FindWarByKey(key);
            if (w2 is null) return;
            string? r = _sim.Diplomacy.PlayerOfferPeace(me, w2, _player.Coef);
            DeliverNews("⚖", r is null ? $"{w2.A.Name}と{w2.B.Name}に調停を提案した" : $"調停できない：{r}", r is null ? NewsBlue : NewsRed, _simDays);
        };
        ConfirmOpen = true;
    }

    private PriceOfBread.SimCore.War? FindWarByKey(string key)
    {
        foreach (var w in _sim.Diplomacy.Wars) if ($"{w.A.Id}|{w.B.Id}" == key) return w;
        return null;
    }
    [ObservableProperty] private bool _playerAtWar;
    [ObservableProperty] private bool _warTabVisible;    // タブボタンの表示（常時true・国内勢力統合のため戦争がなくても使う）
    [ObservableProperty] private bool _noActiveWars = true;
    [ObservableProperty] private bool _hasPastWars;
    private const int PastWarKeepYears = 5;              // 終わった戦争をタブに残す年数
    // 戦争ごとのUI追跡（レビュー2026-07-19で全面改構）：戦死＝エンジンの戦争別台帳 War.DeadA/DeadB（兵士＋将校）、
    // 民間人の死＝War.CivDeadA/CivDeadB（★2026-09-23で追加）＝どちらも内戦・粛清の混入なし・多正面は戦線数で按分済み。
    // ★戦費＝**エンジンが実際に払った軍の支出**（TreasuryLedger の陸軍・海軍・戦費の月次差分の和＝_monthWarCost）をその月の戦線数で均等按分した累計。
    //   2026-09-30（支払いの一元化 段3-2）から戦費の独自の支払い（旧 Diplomacy.Fund）は無い＝戦争の費用は軍需の調達と兵士の給与と賠償。
    // 開戦日＝war.Months から導出（ロードしても正しい）。
    // 鍵＝敵Power.Id（変名しても同一の戦争）。Last* は講和で War が消えた後の残置表示用スナップショット。
    private sealed class WarTrack
    {
        public int Won0, Lost0;                       // 開戦時の勝敗回数（結果推定のフォールバック）
        public double LastScore;                      // 直近の戦勝点（自国視点）
        public int LastMonths;                        // 直近の経過月
        public double LastDeadSelf, LastDeadEnemy;    // 直近の戦争別戦死（War.DeadA/DeadB＝兵士＋将校）
        public double LastCivDeadSelf, LastCivDeadEnemy;   // ★2026-09-23：直近の戦争別の民間人の死（War.CivDeadA/CivDeadB）
        public double CostShare;                      // この戦争に按分した自国戦費の累計
        // ★内戦の種類（2026-09-28 処理の一元化 段2 束2）＝終戦の報せの得失を種類で書き分ける（旧＝内戦でも「賠償金」と書いた）。
        //   毎回の Refresh で生きている戦争から取り直す（セーブしない＝ロード後の最初の Refresh で入る）。
        public bool Civil, Secession, MeParent, Demand;
        public string EnemyName = "";                 // 相手の名（再併合で相手の国が消えた後の表示）
    }
    private readonly Dictionary<long, WarTrack> _warTracks = new();
    // 内戦の終戦の得失（エンジンの戦後処理 Diplomacy.ResolvePeace の枝と同じ分け方＝分断／独立の確定／再併合〔反乱の勝ち＝政体の転換・同じ政体なら要求の受諾〕）。
    private string CivilWarOutcome(WarTrack tr, bool? won)   // ★(456) 勝ち／負け／白紙は終戦の判定の 1 本（EndedWarRecord.VerdictOf＝政体の内戦は戦勝点がちょうど 0 のときだけ白紙）
    {
        string foe = tr.EnemyName;
        if (won is null)
            return tr.MeParent ? $"失ったもの:{foe}の州(分断)" : "得たもの:独立(分断)";
        if (won.Value)
            return tr.MeParent ? $"得たもの:{foe}の再併合"
                 : tr.Secession ? "得たもの:独立"
                 : tr.Demand ? "得たもの:要求の受諾" : "得たもの:政体の転換";
        return !tr.MeParent ? "失ったもの:国家(再併合)"
             : tr.Secession ? $"失ったもの:{foe}の独立"
             : tr.Demand ? "失ったもの:要求の受諾" : "失ったもの:政体";
    }
    private int _warCostStamp = -1;   // 戦費按分の月スタンプ（Refreshは月次以外にも呼ばれる＝二重加算の防止）
    private bool _warSeeded;          // 初回Refresh（新規開始/ロード直後）は戦争タブを自動オープンしない
    // ロード時に復元する戦争UI状態（セーブ末尾の追記ブロック POBWAR2）。旧セーブには無い＝null（新規追跡で再開）。
    // ★POBWAR2（2026-09-23）＝民間人の死の2列を追記。POBWAR1 のセーブは同じ読み手が8項目で読み、民間は0で始まる（次のRefreshでエンジンから入る）。
    internal sealed record WarUiSave(int CostStamp, List<(long Id, int Won0, int Lost0, double LastScore, int LastMonths, double DeadSelf, double DeadEnemy, double CivSelf, double CivEnemy, double Cost)> Tracks,
                                     List<(string Key, double EndDay, string Title, string Detail)> Pasts);
}
