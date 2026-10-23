// GameViewModel.Popups.cs ＝ ポップアップ群（1934拡張#106）＝緊張・開戦/終戦・国内危機・参戦要請・議会解散。
// ★2026-08-10のUI分割：GameViewModel.cs から**メンバーを移しただけ**（ロジック・順序とも不変）。
//   同じ partial class GameViewModel の一部＝どのファイルからでも全メンバーに触れる。
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.SimCore;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    // ★2026-08-24作者指示：**選択肢を押しても時間は再開しない**（プレイヤーが自分で再開する）＝閉じても止まったまま。
    //   モーダル中は時間を動かすこともできない（TogglePause がここを見る／キー入力も GameView 側で無視）。
    //   ⚠2026-08-16の「開く直前の状態へ戻す」復帰は撤去＝選択の直後に世界が走り出さない（結果を読む時間を奪わない）。
    // ★2026-08-24作者指示「選挙含め全てのポップアップは時間停止に」＝確認モーダル・Escメニュー・終局もこの1本に載せる。
    //   ⚠ここに載せ忘れた画面があると、別のポップアップが閉じた瞬間に**開いたままの画面の裏で時間が動き出す**（旧＝確認モーダルが外にいた）。
    internal bool AnyPopupOpen => TensionPopupOpen || WarPopupOpen || WarEndPopupOpen || FactionCrisisOpen || ElectionPopupOpen || CallToArmsOpen
                              || ConfirmOpen || MenuOpen || EndingOpen || SideChoiceOpen;

    // ★内戦の陣営選択（P8・作者裁定2026-09-06「プレイヤー側UIはやる」）：自国から反乱国家（内戦タグの戦争の相手）が立ったとき、
    //   政府側に残るか反乱側で続けるかを選ぶ。乗り換え＝MainWindowViewModel.SwitchPlayer（旧国は効用AIへ・新国は操作者へ）。
    [ObservableProperty] private bool _sideChoiceOpen;
    [ObservableProperty] private string _sideChoiceBody = "";
    private int _sideChoiceIndex = -1;
    partial void OnSideChoiceOpenChanged(bool value) { if (value) PopupPause(); }
    [RelayCommand] private void SideChoiceStay() { SideChoiceOpen = false; }
    [RelayCommand] private void SideChoiceSwitch() { SideChoiceOpen = false; if (_sideChoiceIndex >= 0) OnSwitchPlayer?.Invoke(_sideChoiceIndex); }
    private void PopupPause() => Playing = false;
    private void PopupResume() { }   // ★2026-08-24：閉じても再開しない（このメソッドは「閉じた」ことの明示＝将来の再開処理の受け皿として残す）
    // === 1934拡張 #106：外交・戦争ポップアップ（緊張が高まると自動で出す／開戦の最後通牒＋総動員／終戦報告） ===
    [ObservableProperty] private bool _tensionPopupOpen;
    partial void OnTensionPopupOpenChanged(bool value) { if (value) PopupPause(); else PopupResume(); }   // 表示中は時間停止（開いた瞬間の兵力/緊張のまま）
    [ObservableProperty] private string _tensionPopupTitle = "";
    [ObservableProperty] private string _tensionPopupWhy = "";   // 緊張が高まった理由（内訳・寄与の大きい順。作者FB①・2026-08-10）
    [ObservableProperty] private string _tensionPopupBody = "";
    private Power? _tensionPopupFoe;
    private readonly System.Collections.Generic.HashSet<long> _tensionPrompted = new();   // プロンプト済み（連発防止・緊張が十分下がると解除）

    [ObservableProperty] private bool _warPopupOpen;
    partial void OnWarPopupOpenChanged(bool value) { if (value) PopupPause(); else PopupResume(); }
    [ObservableProperty] private string _warPopupBody = "";
    [ObservableProperty] private bool _warPopupMobilize;   // 開戦時に総動員するか（チェックボックス）

    [ObservableProperty] private bool _warEndPopupOpen;
    partial void OnWarEndPopupOpenChanged(bool value) { if (value) PopupPause(); else PopupResume(); }
    [ObservableProperty] private string _warEndPopupBody = "";

    // 国内危機（2026-07-20・戦争ポップアップと同型）：組織運動が蜂起寸前になると通知を出す（時間停止・閉じるだけ＝要求は法の経路・作者裁定2026-09-11）。
    // ⚠段6（2026-09-08）：FactionCrisisSuppressNote（＝無血鎮圧の値段「必要○%／現在○%」）は撤去＝払う余剰戦力そのものが無くなった（死んだ表示）。
    [ObservableProperty] private bool _factionCrisisOpen;
    partial void OnFactionCrisisOpenChanged(bool value) { if (value) PopupPause(); else PopupResume(); }
    [ObservableProperty] private string _factionCrisisBody = "";

    // ★E3（選挙doc §3-1/3-2・点火2026-08-17・四半期の公示時に出る）：
    // 総選挙の公示＝「どの思想に加担するか」1つだけの択（議題は情報＝個別に倒す対象ではない・§3-2a）。
    [ObservableProperty] private bool _electionPopupOpen;
    partial void OnElectionPopupOpenChanged(bool value) { if (value) PopupPause(); else PopupResume(); }
    [ObservableProperty] private string _electionPopupTitle = "総選挙";   // ★2026-08-31：タイトル＝「yyyy年 総選挙」
    // ★E3''（確定形2026-08-17）：レバーの意味が「0..1の押す強さ」→**裁量余剰の何%を注ぐか**に変わった
    //   ＝軍事費・教育費と同じ物差しの上での配分（注いだ分だけ他が減る）。既定は控えめな2割。
    [ObservableProperty] private double _electionPushShare = 0.2;
    partial void OnElectionPushShareChanged(double value) { OnPropertyChanged(nameof(ElectionSpendText)); }
    /// <summary>スライダーの実効値＝注ぐ実額と効き（エンジンと同じ1本の式＝World.ElectionSpendOf/ElectionPushEffectOf を読む）。</summary>
    public string ElectionSpendText => $"{ElectionPushShare:P0}　資金 {Fmt.Money(_player.ElectionSpendOf(ElectionPushShare))}　効き {_player.ElectionPushEffectOf(ElectionPushShare):P0}";
    public System.Collections.ObjectModel.ObservableCollection<ElectionAgendaVm> ElectionAgendas { get; } = [];
    public sealed record ElectionAgendaVm(int Ideology, string Label);
    [RelayCommand] private void ElectionBack(ElectionAgendaVm a) { _player.ResolveElection(a.Ideology, ElectionPushShare); ElectionPopupOpen = false; Refresh(); }
    [RelayCommand] private void ElectionAbstain() { _player.ResolveElection(-1, 0); ElectionPopupOpen = false; Refresh(); }

    // 機能5-③（2026-07-28）：同盟からの参戦要請＝受ける／断る（断ると要請元との緊張＋AllyRefusalTension＝同盟破棄水準）。
    [ObservableProperty] private bool _callToArmsOpen;
    partial void OnCallToArmsOpenChanged(bool value) { if (value) PopupPause(); else PopupResume(); }
    [ObservableProperty] private string _callToArmsBody = "";
    [RelayCommand] private void CallToArmsAccept() { _sim.Diplomacy.ResolveCallToArms(Me, true, _player.Coef); CallToArmsOpen = false; Refresh(); }
    [RelayCommand] private void CallToArmsRefuse() { _sim.Diplomacy.ResolveCallToArms(Me, false, _player.Coef); CallToArmsOpen = false; Refresh(); }

    // 機能2-4（2026-07-28）：議会の解散＝任期途中の総選挙（プレイヤーの動詞。AI側の等価物＝不信任の繰り上げ選挙2-5）。
    // 次のParliament.Stepで選挙が走る（民意VoteSharesで新与党）。乱発防止＝不信任と同じ最短間隔。
    public bool CanDissolveParliament => _player.Polity is Polity.ConstitutionalMonarchy or Polity.Republic
        && _player.ParliamentEnabled && _player.QuartersSinceElection >= (int)_player.Coef.EarlyElectionMinQuarters;
    public string DissolveParliamentTip => $"前回選挙から {_player.QuartersSinceElection / (double)Calendar.QuartersPerYear:F1} 年";
    [RelayCommand]
    private void DissolveParliament()
    {
        if (!CanDissolveParliament) return;
        Parliament.Dissolve(_player);
        _sim.Diplomacy.Log.Add($"{_player.Year} {Me.Name}が議会を解散（総選挙へ）");
        OnPropertyChanged(nameof(CanDissolveParliament));
        Refresh();
    }

    /// <summary>総動員（戦争タブのチェック＝後から追加できる）。交戦中ONで戦時徴兵を levée en masse 水準へ＝非兵士POP(農民/都市労働者)を兵士へ大量変換（Military.cs）。代償は自然（労働喪失で経済悪化・兵増で戦死増）。</summary>
    public bool PlayerMobilized { get => _player.Mobilized; set { _player.Mobilized = value; OnPropertyChanged(); } }

    // 緊張が高まった／開戦寸前を四半期ごとに検出（DetectEvents から）。プレイヤー経路のみ＝AI/自己対戦は従来どおり。
    private void DetectDiplomacyPopups()
    {
        var dip = _sim.Diplomacy; var me = Me;
        // ① 最後通牒：プレイヤーが絡む緊張戦が成立寸前（PendingWar）＝要求を受け入れる／開戦を選ぶ
        if (!WarPopupOpen && me.PendingWar is { } foe)
        {
            var o = dip.WarOutlook(me, foe, _player.Coef);
            string win = o.WinShare >= 0.6 ? "優勢" : o.WinShare <= 0.4 ? "劣勢" : "拮抗";
            // ★実兵数表示（2026-08-08）：陸海空は**人員の配分**（海軍＝維持費比の乗員・空軍＝航空装備で立つ要員）。
            //   質の差は数に混ぜず勝勢%が担う＝「大軍だが勝勢は低い」（技術劣位・低即応）がそのまま読める。
            double myU = TroopUnits(me), enU = TroopUnits(foe);
            double myAirFill = me.Military > 1e-9 ? o.MyAir / me.Military : 0;
            double enAirFill = foe.Military > 1e-9 ? o.EnAir / foe.Military : 0;
            // ★2026-09-07 作者指示「要求が出ていないので表示」：争点＝緊張の内訳の最大項（Diplomacy.TensionBreakdown＝エンジンと同じ1本）。
            //   受諾の代価も実値で出す＝緊張の下げ先（ConcedeTensionTarget）と正統性の傷（ResolvePendingWar が引く AISubmitLegitCost）。
            string issue = ""; double topV = 0;
            foreach (var (reason, value) in dip.TensionBreakdown(me, foe)) if (value > topV) { topV = value; issue = Diplomacy.TensionReasonJa(reason); }
            double tNow = dip.Tension(me, foe), tTo = _player.Coef.ConcedeTensionTarget;
            double legCost = System.Math.Min(_player.Coef.AISubmitLegitCost, _player.Legitimacy);
            WarPopupBody = $"{foe.Name} との緊張が限界に達した。\n"
                + (issue.Length > 0 ? $"争点:{issue} {topV:F0}/緊張 {tNow:F0}\n\n" : "\n")
                + $"自軍　陸軍 {Fmt.Men(myU * (1 - me.NavalShare.Value))}/海軍 {Fmt.Men(myU * me.NavalShare.Value)}/空軍 {Fmt.Men(myU * myAirFill)}\n"
                + $"{foe.Name}　陸軍 {Fmt.Men(enU * (1 - foe.NavalShare.Value))}/海軍 {Fmt.Men(enU * foe.NavalShare.Value)}/空軍 {Fmt.Men(enU * enAirFill)}\n\n"
                + $"戦況予測:勝勢 {o.WinShare:P0}　{win}\n"
                + $"要求を受け入れる:緊張 {tNow:F0}→{tTo:F0}　王権の正統性 −{legCost:F2}";
            WarPopupMobilize = _player.Mobilized;
            WarPopupOpen = true;
            return;   // 最後通牒が最優先
        }
        // ①' 機能5-③（2026-07-28）：同盟からの参戦要請（PendingCallToArms）＝受ける／断る（PendingWarと同型・最後通牒の次点）
        if (!CallToArmsOpen && !WarPopupOpen && me.PendingCallToArms is { } call)
        {
            double refuse = _player.Coef.AllyRefusalTension;
            CallToArmsBody = $"{call.Caller.Name} が同盟に基づき {call.Enemy.Name} への参戦を求めている。\n\n"
                + $"自軍 {Troops(me)}　{call.Enemy.Name} {Troops(call.Enemy)}\n"
                + $"断る場合:{call.Caller.Name} との緊張 +{refuse:F0}(同盟破棄の水準)";
            CallToArmsOpen = true;
            return;
        }
        // ②' 国内危機：組織運動が蜂起寸前（PendingFactionCrisis）＝**通知**（戦争ポップアップと同型・閉じるだけ）。
        // ★要求は法の経路（作者裁定2026-09-11「モーダルはあくまで通知用」・同日点火）：受諾のボタンも軍を差し向けるボタンも無い＝受諾は法律タブで同じ法の目標を置く
        //   （AI の Parliament.TargetCrisisDemands と同じ p.X・同じ GateInfo）・蜂起はエンジンが「要求した法が動いたか」で決める（Politics.CrisisThroughLawPath）。
        //   同じ運動の札が立っている間は一度だけ出す（閉じても札は残る＝危機が立っている限りエンジンが持ち、立たなくなれば自分で下ろす）。
        if (_player.PendingFactionCrisis is null) _crisisShown = null;
        if (!FactionCrisisOpen && !WarPopupOpen && _player.PendingFactionCrisis is { } fc && !ReferenceEquals(fc, _crisisShown))
        {
            _crisisShown = fc;
            FactionCrisisBody = $"{Factions.DisplayName(fc)} が蜂起の構えを見せている。\n\n"
                + $"規模 {Fmt.PopFmt(fc.Size)}　闘争度 {fc.AvgMilitancy:P0}\n"
                + $"要求:{_player.PendingFactionCrisisDemands}";
            FactionCrisisOpen = true;
            return;
        }
        // ②'' ★E3：総選挙の公示（PendingElection）＝どの思想に加担するか
        if (!ElectionPopupOpen && !WarPopupOpen && _player.PendingElection is { } pe)
        {
            ElectionAgendas.Clear();
            for (int k = 0; k < pe.AgendaIdeologies.Length; k++)
                ElectionAgendas.Add(new ElectionAgendaVm(pe.AgendaIdeologies[k], pe.AgendaLabels[k]));
            // ★E3''：レバーは「裁量余剰の何%を注ぐか」＝原資の実額を出す（データのみ）。半減点＝効きが半分になる投入額。
            ElectionPopupTitle = $"{_player.Year}年 総選挙";   // ★2026-08-31作者指示：説明行（公示・開票まで・裁量余剰・半減点）は全て撤去＝タイトルと択と金額だけ
            OnPropertyChanged(nameof(ElectionSpendText));   // 原資は四半期ごとに動く＝開いた時点の実効値を出す
            ElectionPopupOpen = true;
            return;
        }
        // ② 緊張ポップアップ：同ランク列強と緊張が高まった（閾値超え・未プロンプト・非交戦）
        if (!TensionPopupOpen && !WarPopupOpen)
            foreach (var pow in dip.Powers)
            {
                if (pow == me || pow.Nation is null) continue;
                double t = dip.Tension(me, pow);
                // ⚠再通知の解除は**発火閾値を基準に**する（2026-08-02）。旧は ConcedeTensionTarget(110) 基準だったので、
                //   発火閾値を70へ下げた途端「発火した瞬間にもう解除条件を満たしている」＝毎四半期出続ける罠になる。
                if (t < _player.Coef.TensionPopupThreshold - TensionRepromptDrop) _tensionPrompted.Remove(pow.Id);
                // 同ランク間のみ（緊張の増減操作の対象）。★AIと**同じ束**で判定する（Diplomacy.Bracket）＝
                //   4段階化しても、プレイヤーに緊張モーダルが出る相手とAIが煽る相手が食い違わない（思想⑤の対称）。
                if (dip.Bracket(pow, _player.Coef) != dip.Bracket(me, _player.Coef)) continue;
                if (dip.IsAtWar(me, pow)) continue;
                if (t >= _player.Coef.TensionPopupThreshold && !_tensionPrompted.Contains(pow.Id))
                {
                    _tensionPrompted.Add(pow.Id);
                    BuildTensionPopup(me, pow, t);
                    TensionPopupOpen = true;
                    break;
                }
            }
    }

    // 緊張ポップアップの中身（検出経路と撮影フックが共有＝二実装にしない）。時間停止中＝開いた瞬間の構成で固定。
    private void BuildTensionPopup(Power me, Power pow, double t)
    {
        var dip = _sim.Diplomacy;
        _tensionPopupFoe = pow;
        // ★2026-08-02：見出しに%を入れ、本文の同じ文言は消した（作者指摘「メインテキストとヘッダーで同じことを言っている」）
        TensionPopupTitle = $"{pow.Name} との緊張({TensionPct(t):P0})";
        // ★緊張が高まった理由（作者FB①・2026-08-10）：エンジンの内訳台帳（合計＝現在の緊張）を寄与の大きい順に。
        //   単位は見出しの緊張%と同じ物差し（TensionPct＝開戦閾値で割る）。上位4件まで＝主因だけを出す。
        // ⚠表示名は多対一（アヘン交易・植民地の競合＝どちらも「経済摩擦」）なので**名前で束ねてから**並べる。
        //   束ねは表示だけ＝台帳（enum別）は無傷、合計も不変（同じ値を足し合わせるだけ）。
        var merged = new List<(string Name, double Value)>();
        foreach (var (reason, v) in dip.TensionBreakdown(me, pow))   // 既に降順＝最初に現れた順＝束ねた行の並びも安定
        {
            string name = Diplomacy.TensionReasonJa(reason);
            int at = merged.FindIndex(x => x.Name == name);
            if (at >= 0) merged[at] = (name, merged[at].Value + v);
            else merged.Add((name, v));
        }
        var why = new List<string>();
        foreach (var (name, v) in merged.OrderByDescending(x => x.Value))
        {
            if (why.Count >= 4 || TensionPct(v) < 0.005) break;   // 降順＝0%に丸まる寄与まで来たら終わり（「高まった理由」なので緩和側も出さない）
            why.Add($"{name} +{TensionPct(v):P0}");
        }
        TensionPopupWhy = string.Join("\n", why);
        // 機能5-①-1（2026-07-28）：相手・自国の同盟関係と参戦見込み（表示のみ＝計算へは加算しない。閾値式はエンジンと単一ソース）
        double jtFoe = dip.AllyJoinThreshold(me, _player.Coef);    // 相手の同盟国が自国相手に参戦する閾値
        double jtMe = dip.AllyJoinThreshold(pow, _player.Coef);    // 自国の同盟国が相手に参戦する閾値
        var foeAl = dip.AlliesOf(pow).ToList();
        var myAl = dip.AlliesOf(me).ToList();
        // ★戦力は「自国＋同盟の合計」を先に出し、その内訳を次の行に置く（作者指摘「戦力比較しづらい」）
        string myList = string.Join("・", myAl.Select(x => $"{x.Name} {Troops(x)}(参戦 {System.Math.Clamp(dip.Tension(x, pow) / jtMe, 0, 1):P0})"));
        string foeList = string.Join("・", foeAl.Select(x => $"{x.Name} {Troops(x)}(参戦 {System.Math.Clamp(dip.Tension(x, me) / jtFoe, 0, 1):P0})"));
        TensionPopupBody = $"{me.Name} {Troops(me)}　+ 同盟 {Fmt.Men(myAl.Sum(TroopUnits))}";
        if (myAl.Count > 0) TensionPopupBody += $"\n{me.Name} の同盟:{myList}";
        TensionPopupBody += $"\n\n{pow.Name} {Troops(pow)}　+ 同盟 {Fmt.Men(foeAl.Sum(TroopUnits))}";
        if (foeAl.Count > 0) TensionPopupBody += $"\n{pow.Name} の同盟:{foeList}";
        BuildTensionActionTips(me, pow);   // 2つのボタンのホバー＝緊張量＋政治的帰結の3行（項5・2026-07-27／作者FB②・2026-08-10）
        double ramp = dip.TensionRampRemaining(me, pow);   // 自国が押してまだ反映されていない緊張（回8-1＝待ちの撤去の後はデータだけを出す）
        TensionRampText = System.Math.Abs(ramp) >= 0.5 ? $"反映待ちの緊張 {(ramp > 0 ? "+" : "-")}{System.Math.Abs(ramp):F0}" : "";
    }

    /// <summary>uishot（無人撮影）専用フック：最も緊張の高い同ランク国で緊張モーダルを実際に組んで開く。
    /// 検出経路と同じ <see cref="BuildTensionPopup"/> を通す＝撮れる画面は本番と同一。開けたら true。</summary>
    public bool OpenTensionPopupForCapture()
    {
        var dip = _sim.Diplomacy; var me = Me;
        Power? best = null; double bestT = 0;
        foreach (var pow in dip.Powers)
        {
            if (pow == me || pow.Nation is null || dip.IsAtWar(me, pow)) continue;
            double t = dip.Tension(me, pow);
            if (t > bestT) { bestT = t; best = pow; }
        }
        if (best is null || bestT <= 0) return false;
        BuildTensionPopup(me, best, bestT);
        TensionPopupOpen = true;
        return true;
    }

    [RelayCommand] private void TensionRaise() { if (_tensionPopupFoe is { } f) _sim.Diplomacy.RaiseTension(Me, f, _player.Coef); TensionPopupOpen = false; Refresh(); }
    [RelayCommand] private void TensionEase() { if (_tensionPopupFoe is { } f) _sim.Diplomacy.EaseTension(Me, f, _player.Coef); TensionPopupOpen = false; Refresh(); }
    [RelayCommand] private void TensionIgnore() => TensionPopupOpen = false;   // ✕/Esc＝何もせず閉じる（静観ボタンは廃止・項5）

    // 挑発/緩和ボタンのホバー（項5・2026-07-27）＝緊張量＋政治的帰結の3行。
    // ★作者裁定2026-08-10：3行は**ボタンに合わせたときだけ**出す（常時表示は取り止め）＝ボタンの見た目と配置は元のまま。
    //   ツールチップはポップアップ＝高さが変わってもモーダル本体のレイアウトは動かない。
    // ★式はUIで組み立てない＝エンジンの純関数 Diplomacy.PreviewTensionPolitics（本体 TensionPolitics と同じ1箇所）を読む。
    [ObservableProperty] private string _tensionProvokeTip = "";
    [ObservableProperty] private string _tensionEaseTip = "";
    // ★2026-09-28 回8-1：待ち（旧 TensionActionCooldown・18か月）の撤去＝ボタンは常に押せる。自国が押してまだ反映されていない緊張の量だけを出す。
    [ObservableProperty] private string _tensionRampText = "";
    private void BuildTensionActionTips(Power me, Power foe)
    {
        var c = _player.Coef;
        var (neighbors, spreadSum) = _sim.Diplomacy.PreviewProvocationSpread(me, foe, c);   // ★(474) 近隣への波及はエンジンの 1 本を読む（旧は距離の式を画面に書き写していた）
        TensionProvokeTip = $"緊張 +{c.ActionTensionStep:F0}({c.TensionActionRampMonths}ヶ月かけて)"
            + (neighbors > 0 ? $"　近隣 {neighbors}カ国へ波及 計+{spreadSum:F0}" : "")
            + "\n" + PoliticsLines(Diplomacy.PreviewTensionPolitics(me, foe, TensionPoliticsKind.Provoke, c));
        TensionEaseTip = $"緊張 -{c.ActionTensionStep:F0}({c.TensionActionRampMonths}ヶ月かけて)"
            + "\n" + PoliticsLines(Diplomacy.PreviewTensionPolitics(me, foe, TensionPoliticsKind.Appease, c));
    }

    // 政治的帰結の3行（作者指定の形・2026-08-10）。値が0でも行は出す＝押す前に必ず同じ3行が読める。
    private static string PoliticsLines(TensionPoliticsPreview pv)
        => $"国内{pv.HawkShare:P0}の国粋・帝国主義者からの支持:{Fmt.SignedPct1(pv.HawkDelta)}\n"
         + $"国内{pv.DoveShare:P0}の自由・共和主義者派からの支持:{Fmt.SignedPct1(pv.DoveDelta)}\n"
         + $"正統性:{Fmt.SignedPct1(pv.LegitimacyDelta)}";

    // 要求を受け入れる＝屈服（緊張を下げて開戦を回避）。総動員には触れない（新たな戦争は始まらない＝進行中の別戦争の動員を巻き込まない・レビュー2026-07-20）。講和時は WarRows.Count==0 で自動解除。
    [RelayCommand] private void WarAccept() { _sim.Diplomacy.ResolvePendingWar(Me, false, _player.Coef); WarPopupOpen = false; Refresh(); }
    // 開戦＝チェックしていれば総動員して開戦。
    [RelayCommand] private void WarDeclareGo() { _player.Mobilized = WarPopupMobilize; _sim.Diplomacy.ResolvePendingWar(Me, true, _player.Coef); WarPopupOpen = false; OnPropertyChanged(nameof(PlayerMobilized)); Refresh(); }

    [RelayCommand] private void WarEndAck() => WarEndPopupOpen = false;

    // 国内危機の通知（作者裁定2026-09-11「モーダルはあくまで通知用」）：閉じるだけ（世界には書かない）。
    //   受諾＝法律タブで同じ法の目標を置く＝AI（Parliament.TargetCrisisDemands）と同じ p.X・同じ GateInfo。蜂起はエンジンが翌四半期に「要求した法が動いたか」で決める。
    //   ⚠撤去の履歴：第4択「別の軸で譲歩する」（2026-09-03）・無血鎮圧（2026-09-08 段6）・「要求を受け入れる」「軍を差し向ける」（2026-09-11）＝AI と同じくボタンで決着する道は無い。
    [RelayCommand] private void FactionClose() { FactionCrisisOpen = false; }
    private FactionState? _crisisShown;   // 通知を出した運動（同じ札で二度出さない）

    private string WillPct(double v) => (v / _player.Coef.ExpansionWillCap).ToString("P0");   // 膨張度の％表示（上限比）

    // P4 統一戦争（プレイヤーの動詞・AIと同条件＝RNGなし・無償）：確認モーダル経由
    [RelayCommand]
    private void DiploUnifWar(string? name)
    {
        if (FindPower(name) is not { } t) return;
        bool can = _player.CanUnificationWar(t, out string reason);
        ConfirmTitle = "統一戦争";
        ConfirmBody = $"{t.Name}\n目標 {_player.UnificationRule?.NewName ?? ""}　必要州の保有国" + (can ? "" : $"\n不可:{reason}");
        ConfirmAcceptLabel = "宣戦";
        ConfirmCan = can;
        _confirmDo = () => _player.DeclareUnificationWarOn(t);
        ConfirmOpen = true;
    }

    // 通商条約ボタン（DiploTreaty）は廃止（2026-09-07作者指示「通商条約は自動化したい」）＝締結は Diplomacy.TradeTreaties が四半期に自動で行い、
    //   プレイヤー国もAIと同じ掃引に入る（同盟・禁輸の自動化と同じ型）。UIは状態（関税目標）だけを映す。締結の知らせは事件ログ。

    // ★2026-08-23作者裁定：植民地化と侵略の1本化＝確認モーダルも1つ。屈服の見込み（実効戦力/必要）と狙い先を同じ画面で読む。
    [RelayCommand]
    private void DiploInvade(string? name)
    {
        if (FindPower(name) is not { } t) return;
        bool can = _player.CanInvade(t, out string reason);
        double cost = _player.ExpansionCostFor(t);
        double force = _sim.Diplomacy.ColonizeForce(Me, t, _player.Coef);
        double need = _sim.Diplomacy.SubmitLine(t, _player.Coef);   // 屈服線＝エンジンと同じ1本（要求余剰戦力の分母と同一）
        string outlook = force >= need ? "屈服の見込み" : "拒否=開戦の見込み";
        ConfirmTitle = "侵略";
        var aimCol = _interestColony is { } ic && t.ColonyList.Contains(ic) ? ic : null;   // 02断線走査2026-07-28：狙い先が標的国の植民地なら講和の優先割譲へ
        ConfirmBody = $"{t.Name}\n余剰戦力 {WillPct(cost)} を消費　現在 {WillPct(_player.ExpansionWill)}\n実効戦力 {Fmt.Men(force * _player.Coef.SoldiersPerMilitary)}/必要 {Fmt.Men(need * _player.Coef.SoldiersPerMilitary)}　→ {outlook}"
                      + (aimCol is not null ? $"\n狙い先 {aimCol.Name}" : "")
                      + (can ? "" : $"\n不可:{reason}");
        ConfirmAcceptLabel = "侵略";
        ConfirmCan = can;
        _confirmDo = () => _player.Invade(t, aimCol);
        ConfirmOpen = true;
    }
}
