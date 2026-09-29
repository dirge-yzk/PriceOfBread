// GameViewModel.Fiscal.cs ＝ 財政（経済）タブ＝税/支出のレバー・上限・金融（金本位/金利/近代化投資）・予算予想。
// ★2026-08-10のUI分割：GameViewModel.cs から**メンバーを移しただけ**（ロジック・順序とも不変）。
//   同じ partial class GameViewModel の一部＝どのファイルからでも全メンバーに触れる。
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.SimCore;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    // 経済タブの予算予想（左＝支出／右＝収入）：Fiscal と同式で毎tick再計算＝段ボタンを押した瞬間に追従し常に現在の予想を表示（四半期を待たない）。
    // 各行＝「項目 目標%（実効%）　予想額」。合計は各列の下部・収支は最下部（バーは廃止＝2026-07-04指示）。
    [ObservableProperty] private string _interestLine = "";
    [ObservableProperty] private string _adminLine = "";
    [ObservableProperty] private string _eduLine = "";
    [ObservableProperty] private string _welLine = "";
    [ObservableProperty] private string _pensionLine = "";
    [ObservableProperty] private string _indLine = "";
    public string IndTip { get; private set; } = "";   // ★近代化投資のコスト内訳（2026-08-04・ホバー＝データのみ）
    [ObservableProperty] private string _incomeLine = "";
    [ObservableProperty] private string _progLine = "";
    [ObservableProperty] private string _pollLine = "";
    [ObservableProperty] private string _landLine = "";
    [ObservableProperty] private string _monopolyLine = "";
    [ObservableProperty] private string _tariffLine = "";
    // ⚠撤去2026-08-23（読み手ゼロ）: TariffThrottleText（U6の輸送律速の州数）＝VM だけ実装され View へ未配線のまま。律速の内訳は貿易タブの州別物流表が持つ。
    // 見えない出費の可視化（監査2026-07-11 接1）：植民地維持費・属国貢納・汚職根絶費＝国庫を実際に動かすのに予算表に無かった行。
    [ObservableProperty] private string _colonialLine = "";
    [ObservableProperty] private string _colonialAmt = "";
    [ObservableProperty] private bool _colonialRowVisible;
    [ObservableProperty] private string _tributeLine = "";
    [ObservableProperty] private string _tributeAmt = "";
    [ObservableProperty] private bool _tributeRowVisible;
    [ObservableProperty] private string _tributeInAmt = "";
    [ObservableProperty] private bool _tributeInRowVisible;
    // 金額（右寄せ列）。ラベルと分離＝数値が縦に揃う。
    [ObservableProperty] private string _armyAmt = "";
    [ObservableProperty] private string _navyAmt = "";
    [ObservableProperty] private string _adminAmt = "";
    [ObservableProperty] private string _eduAmt = "";
    [ObservableProperty] private string _welAmt = "";
    [ObservableProperty] private string _indAmt = "";
    [ObservableProperty] private string _interestAmt = "";
    [ObservableProperty] private string _pensionAmt = "";
    [ObservableProperty] private string _incomeAmt = "";
    [ObservableProperty] private string _progAmt = "";
    [ObservableProperty] private string _pollAmt = "";
    [ObservableProperty] private string _landAmt = "";
    [ObservableProperty] private string _monopolyAmt = "";
    [ObservableProperty] private string _tariffAmt = "";
    private double _eduMonthly, _stateMonopMonthly;                 // 実績（直近四半期の月平均）＝Regionの累積の差分から
    private double _prevEduCum, _prevStateMonopCum;
    private int _prevQuarterStamp = -1;   // 四半期境界の検出（年×4＋四半期）。-1=未捕捉＝初回は基準化のみ（他7）
    // 外資の純フロー（流入なら収入側・流出なら支出側に計上＝片方は£0）
    private double _prevForeignNet;
    private double _fInMonthly, _fOutMonthly;
    [ObservableProperty] private string _foreignInAmt = "";
    [ObservableProperty] private string _foreignOutAmt = "";
    // 実効額（現在の実効レート×現在のpop基盤＝いま実際に流れている額）。予想額＝目標（段ボタンで置いた値）ベースと並記。
    [ObservableProperty] private string _armyAct = "";
    [ObservableProperty] private string _navyAct = "";
    [ObservableProperty] private string _adminAct = "";
    [ObservableProperty] private string _eduAct = "";
    [ObservableProperty] private string _welAct = "";
    [ObservableProperty] private string _indAct = "";
    [ObservableProperty] private string _interestAct = "";
    [ObservableProperty] private string _pensionAct = "";
    [ObservableProperty] private string _incomeAct = "";
    [ObservableProperty] private string _progAct = "";
    [ObservableProperty] private string _pollAct = "";
    [ObservableProperty] private string _landAct = "";
    [ObservableProperty] private string _monopolyAct = "";
    [ObservableProperty] private string _tariffAct = "";
    [ObservableProperty] private string _spendTotalAct = "";
    [ObservableProperty] private string _revenueTotalAct = "";
    // ★国庫の台帳（2026-08-13・作者指示「国庫に触るものは全て財政タブに入れる」）。
    //   既存の行は**経常予算の予想**で、国債の起債・償還／通貨切下げ／賠償金／通行料／国営工場／巨大建造物／
    //   工場建設／戦費 が1つも出ていなかった＝ヘッダーの収支（国庫の実績変化）と一致しなかった。
    //   台帳（World.TreasuryLedger）はエンジン側で**全ての書き手を通した**ので、その月次差分を出せば
    //   合計はヘッダーと厳密に一致する（テスト TreasuryLedgerTests が Σ台帳 ≡ ΔTreasury を固定）。
    public System.Collections.ObjectModel.ObservableCollection<LedgerRow> LedgerOut { get; } = new();
    public System.Collections.ObjectModel.ObservableCollection<LedgerRow> LedgerIn { get; } = new();
    [ObservableProperty] private string _ledgerBalanceText = "";

    // ==== 通貨改革（2026-08-13・作者採用）====
    // 累積切下げを減らす経路が1つも無く、切下げ→資本逃避→GDP縮小→不信上昇が吸収状態になっていた。
    // 史実の脱出口＝レンテンマルク1923／ポワンカレ・フラン1926。**旧平価に戻すか実力の平価で戻すか**が決断になる
    //（英1925の旧平価復帰はデフレ不況を招いた失敗例）。UIは2つのボタンだけ＝説明文は書かない。
    [ObservableProperty] private bool _currencyReformVisible;
    [ObservableProperty] private string _currencyReformParityText = "";

    [RelayCommand] private void ReformAtMarket() => _player.RequestCurrencyReform(_player.ExchangeRate);
    [RelayCommand] private void ReformAtPar() => _player.RequestCurrencyReform(1.0);

    private static string LedgerName(TreasuryCat c) => c switch
    {
        TreasuryCat.Tax => "税収",                     TreasuryCat.Tariff => "関税",
        TreasuryCat.StateMonopoly => "専売益",      TreasuryCat.Borrow => "国債の起債",
        TreasuryCat.Devalue => "通貨切下げ",           TreasuryCat.StateFactory => "国営工場",
        TreasuryCat.TributeIn => "貢納の受取",         TreasuryCat.OtherIn => "その他の収入",
        TreasuryCat.Army => "陸軍",                    TreasuryCat.Navy => "海軍",
        TreasuryCat.Admin => "行政",                   TreasuryCat.Education => "教育",
        TreasuryCat.Welfare => "社会保障",             TreasuryCat.Pension => "年金",
        TreasuryCat.Modernization => "近代化投資",     TreasuryCat.AntiCorruption => "汚職の根絶",
        TreasuryCat.Interest => "国債の利払い",        TreasuryCat.DebtRepay => "国債の償還",
        TreasuryCat.FactoryBuild => "工場の建設",      TreasuryCat.Subsidy => "補助金",
        TreasuryCat.Megaproject => "巨大建造物",       TreasuryCat.ColonialUpkeep => "植民地の維持",
        TreasuryCat.TributeOut => "貢納の支払",        TreasuryCat.WarCost => "戦費",
        _ => "その他の支出",
    };

    /// <summary>台帳の月次差分を取り、非ゼロの区分だけを行にする（0の行は出さない＝既存の作法）。</summary>
    private void SampleTreasuryLedger()
    {
        var now = _player.TreasuryLedger;
        double bal = 0;
        LedgerOut.Clear(); LedgerIn.Clear();
        for (int i = 0; i < now.Length && i < _ledgerPrev.Length; i++)
        {
            double d = now[i] - _ledgerPrev[i];
            bal += d;
            if (System.Math.Abs(d) < 50.0) continue;   // 0の行は出さない＝**表示解像度（£0.1k刻み）で0になる額**は行にしない
            var row = new LedgerRow { Name = LedgerName((TreasuryCat)i), Amount = Money(System.Math.Abs(d)) };
            (d >= 0 ? LedgerIn : LedgerOut).Add(row);
        }
        _ledgerPrev = (double[])now.Clone();
        // ★2026-08-15作者フィードバック(6)：開幕直後（最初の月次差分が立つ前）は両列が空で「機能していない」ように見える
        //   →空のときは「—」を1行置く（機能は生きている＝月が進めば実額が並ぶ）
        if (LedgerOut.Count == 0) LedgerOut.Add(new LedgerRow { Name = "—", Amount = "" });
        if (LedgerIn.Count == 0) LedgerIn.Add(new LedgerRow { Name = "—", Amount = "" });
        LedgerBalanceText = (bal >= 0 ? "+" : "-") + Money(System.Math.Abs(bal));
        // 通貨改革のボタンは**発動条件を満たしたときだけ**出す（押せないボタンを置かない＝UI最小主義）
        var c0 = _player.Coef;
        CurrencyReformVisible = c0.CurrencyReformEnabled > 0.5 && !_player.OnGoldStandard
            && _player.CurrencyReformCooldown <= 0 && _player.NominalGdp > 1e-9
            && _player.CumulativeDevaluation / _player.NominalGdp > c0.CapitalFlightDistrustThreshold
            && _player.NoDevaluationMonths >= c0.CurrencyReformCalmMonths
            && _player.FiscalFundingRatio >= c0.GoldStandardMinFunding;
        CurrencyReformParityText = $"{_player.ExchangeRate:F2}";
    }

    // ⚠撤去2026-08-23（読み手ゼロ）: BudgetBalanceActText ＝収支の実効値は行別 Act 列と合計行（SpendTotalAct/RevenueTotalAct）が持つ。
    [ObservableProperty] private string _spendTotalText = "";
    [ObservableProperty] private string _revenueTotalText = "";
    [ObservableProperty] private string _budgetBalanceText = "";
    // ⚠撤去2026-09-12（R1・読み手ゼロ）: WelfareMax（社会保障スライダーの上限）／撤去2026-09-20（段の点火）: RegressiveMin（人頭税スライダーの下限＝緊縮の逆進フロア）。
    //   緊縮の上限・下限はエンジンの Government.LawPrereq（「緊縮の上限」「緊縮の下限」）が段ボタンの「押せない段」として出す。
    // レバーの定数上限（VMクランプと**段の値**が同源で参照＝限界値の直書きをしない）。
    // ⚠2026-09-23：スライダーが全廃されたので読み手は GameViewModel.Law の Lever（段の上限）とここの setter の2つだけ。
    //   これに伴い Views/SliderLimit.cs（スライダーの硬い上下限＋壁マーカーの添付プロパティ）を**削除**した＝
    //   添付プロパティ SliderLimit.Max/Min の参照が全 axaml で0件・他タブにも使い手が無かった（死蔵）。
    public const double TariffCeil = 0.75;           // 関税の上限（下限＝Coef.TariffFloor・負＝ダンピング）
    public const double InterestCeil = 0.5;          // 政策金利の上限

    // --- 予算：プレイヤーの政策レバー（World.Policy を直接読み書き＝NullAgent なので上書きされない）---
    // ★陸海の維持費（2026-08-04・作者仕様(5)「陸海の割り振りは廃止し『陸軍維持費』『海軍維持費』とする」）＝**払う金そのもの**を陸と海で別々に持つ。
    // ⚠撤去2026-09-20（段の点火）: 陸海それぞれのスライダー（ArmyUpkeep/NavyUpkeep）と戦時の下限（ArmyUpkeepFloor/NavyUpkeepFloor→NavalShareMin/Max）。
    //   合計は軍事費の段ボタン（MilitaryLaw）が置き、戦時の床はエンジンの LawPrereq（「戦争中は軍を解散できない」）が「押せない段」として出す。

    // ⚠撤去2026-09-23（作者裁定「海軍の比率のレバーを廃止し、プレイヤーもAIと同じ地理由来の配分に乗せる」）:
    //   MilTargetTotal / NavalShare / NavalShareLine（2026-09-12 R2 で入れ直した「比」のスライダー1本）。
    //   経緯＝2026-08-04 作者仕様(5)「陸海の割り振りは**廃止**し『陸軍維持費』『海軍維持費』とする」・2026-08-10 作者裁定「陸軍と海軍の
    //   **注力レバーは廃止**」で一度消えたが、2026-09-20 に軍事費が段になった副作用（陸海2本の金額スライダーの撤去）で「比」1本が復活していた。
    //   いまは**誰も比を振らない**＝地理が決める（UtilityAgent.SetNavalUpkeepShare を AI もプレイヤーも同じ引数で通る＝Simulation.QuarterStart）。
    //   動かせるのは軍事費の**合計**（段ボタン＝MilitaryLaw）だけ＝レバーが1本減り、AI・プレイヤー等価が戻る。
    // ⚠3項目を1行に詰めると折り返す（他のレバー行と同じ書式＝2項目に留め、熟練度は右列へ）
    public string ArmyUpkeepLine => $"陸軍維持費 {_player.Policy.ArmyUpkeepTarget:P0}　実効 {_player.ArmyUpkeep:P0}";
    public string NavyUpkeepLine => $"海軍維持費 {_player.Policy.NavyUpkeepTarget:P0}　実効 {_player.NavyUpkeep:P0}";

    public double Tariff
    {
        get => _player.Policy.TariffTarget;
        set
        {
            double v = System.Math.Clamp(value, _player.Coef.TariffFloor, TariffCeil);   // −25%（負＝ダンピング）〜+75%
            _player.Policy.TariffTarget = v;
            OnPropertyChanged();
        }
    }

    // --- 税・支出（作者裁定2026-09-12 R1・2026-09-20 点火）＝**段ボタンへ一本化**（GameViewModel.Law の IncomeTaxLaw ほか6行）。
    //     ⚠撤去2026-09-20: 所得税・累進・人頭税・土地税・教育費のスライダー（IncomeTax/ProgressiveTax/PollTax/LandTax(+Enabled)/EducationSpending(+Enabled)）。
    //       前提（検地台帳・教育法・緊縮の下限・官僚制度）は段ボタンの「押せない段」＝エンジンの LawPrereq 1本が出す（UIに第二の判定式を置かない）。
    //     ⚠社会保障のスライダーは 2026-09-12 に撤去（法律タブの法へ一本化）。専売の全体スライダーは 2026-07-19 に廃止（生産タブの商品ごとの率）。---

    // ⚠撤去2026-09-23: SnapBack（クランプが効いた時に**ドラッグ確定後**へ再通知して thumb を上限位置へ戻す1本・2026-07-16）。
    //   つまみが1つも無くなった＝戻す相手が居ない。段ボタンは押した直後に LawRowVM.Refresh が施行値を読み直す＝
    //   クランプの結果はその場で段の塗りに出る（遅延通知は要らない）。上限・下限そのもの（Clamp/Min）は最終権威として残置。


    public double AdminSpending
    {
        get => _player.Policy.AdminSpendingTarget;
        set { _player.Policy.AdminSpendingTarget = value; OnPropertyChanged(); }
    }

    // === ヘッダーから財政タブへ移す内訳（作者裁定2026-09-23＝ヘッダーの詳細はグラフだけにする）。
    //     ⚠**行を増やさない**＝該当する既存のセルのホバーに入れる（既存の作法）。
    //     ⚠内訳の式は1つも書き写さない＝既に組んである AdminTerms()／CreditTerms()（GameViewModel.Header.cs・同じ partial class）を**呼ぶだけ**。
    /// <summary>行政費のセルのホバー＝**行政力**（行政費レバーと行政効率の関係＝2026-08-15に作者指示でヘッダーへ足したもの）。</summary>
    public string AdminTip => $"行政力　{_player.AdminPowerSmoothed:P0}\n" + string.Join("\n", AdminTerms().Select(t => t.Text));
    /// <summary>外資流入のセルのホバー＝**外資信用**の4項（信用が何で削れているか＝4項の積）。</summary>
    public string ForeignInTip => $"外資信用　{_player.CreditRating:P0}\n" + string.Join("\n", CreditTerms().Select(t => t.Text));

    // ★汚職根絶の表示は撤去（汚職V2点火2026-08-15・8d裁定＝UIから動詞が消えるのは了承済み）：
    //   V2では根絶費の請求ごと死んでいる（監視は法＝報道/資格任用/議会予算権で買う）。旧 AntiCorruption/AntiCorruptionText は削除。

    // 阿片（主フラグONのゲームのみ表示）：★禁令のチェックボックスは廃止（禁制品doc C3/U4・2026-07-30）＝法2本（保有3段・輸出2段）へ移行し法律タブへ。
    // 中毒度の表示は残す＝中毒は財の性質であって禁令の特例ではない（麦に BaseNeed があるのと同型）。
    public bool OpiumVisible => _player.Coef.OpiumAddictionGrowth > 0;
    // ★再征服の行（HasLockedRegions／ForceUnlockStateText）は削除（2026-09-28 処理の一元化 段2 束3）：実ゲームで Locked になる州は
    //   分離独立で凍結した州だけだった（旧経路の単一州の分離＝Region.Seceded）＝その経路を削除（作者裁定 g）＝分離した州は新しい国へ移る。
    //   Locked を作るのはテストシナリオ FrontierDevelopment だけ（辺境の廃止2026-08-26）＝画面に出す対象が無い。

    [ObservableProperty] private string _opiumLine = "";
    private double OpiumAvgAddiction()
    {
        double add = 0, sz = 0;
        foreach (var r in _player.Regions) if (r.State == RegionState.Active) { double rp = r.SumPopulation(); add += r.OpiumAddiction * rp; sz += rp; }
        return sz > 1e-9 ? add / sz : 0;
    }

    // P9 国民形成費（購入モデル・主フラグONのゲームのみ表示）：学校網・教練・官製新聞＝国民意識を「買う」
    // 国民形成費レバーは廃止（2026-07-19ユーザー裁定）＝選挙を望む層が自費で国民運動を賄う（需要駆動・Fiscal）。国民意識の内訳はヘッダーの説明APIで見える。
    [ObservableProperty] private string _unifLine = "";   // P4 統一の機運（外交タブ上部・空=非表示）


    // 物価水準（インフレ/デフレ＝広義マネー/実質産出）・為替（金平価=100%基準・不換で減価<100%）。財政タブの金融セクションで表示（ヘッダーから移動2026-07-16）。
    public string PriceLevelText => !_player.MonetaryEnabled ? "—" : _player.PriceLevel.ToString("P0");
    public string ExchangeRateText => !_player.MonetaryEnabled ? "—" : _player.ExchangeRate.ToString("P0");
    // ⚠撤去2026-08-23（読み手ゼロ）: 金本位の旧スイッチ一式（GoldStandardWanted/GoldShort/GoldStandardButtonText/ToggleGoldStandard）＝
    //   通貨制度は経済タブの3ボタン（不換紙幣/銀本位/金本位＝EconTab の法の行）へ移行済みで、axaml の束縛が1つも無かった。
    //   ⚠この撤去で「UI規律の唯一の説明文の例外」（加入/離脱の確認モーダル本文）も消える＝説明文は画面から全廃された。
    /// <summary>兌換カバー率を出すか。★金本位に関わるとき（加入中・志向あり・兌換停止中）だけ意味を持つ（2026-08-02の整理）。</summary>
    public bool CoverVisible => _player.MonetaryEnabled && (_player.OnGoldStandard || _player.Policy.MonetaryTarget >= 2 || _player.GoldStandardSuspendedInCrisis);
    /// <summary>貸出金利を出すか。★年率換算（World.LoanRateAnnual）の係数が0のあいだは常に0.0%＝エンジンに量が無い＝行ごと隠す（2026-09-26・点火すれば自動で出る）。</summary>
    public bool LoanRateVisible => _player.Coef.LoanSpread > 0 || _player.Coef.PolicyRateLoanWeight > 0;
    /// <summary>家計債務を出すか。★書き手（借入と返済）と同じ門＝個人債務の主フラグが0のあいだは残高が常に0＝行ごと隠す（2026-09-26）。</summary>
    public bool HouseholdLoanVisible => _player.Bank is not null && _player.Coef.PopDebtDriven > 0;
    /// <summary>★総動員のスイッチ（戦争タブの総動員トグル）。総動員中は「復員」に変わる。</summary>
    public string MobilizeButtonText => PlayerMobilized ? "復員" : "総動員";
    [RelayCommand] private void ToggleMobilize()
    {
        PlayerMobilized = !PlayerMobilized;
        OnPropertyChanged(nameof(MobilizeButtonText));
    }

    // 金利（中央銀行の政策目標）。低＝信用拡大・高＝引締。上限50%＝段ボタンの最上段「最大」（2026-09-23にスライダーから段へ）。
    public double InterestLever
    {
        get => _player.Policy.InterestRateTarget;
        set
        {
            double v = System.Math.Min(value, InterestCeil);
            _player.Policy.InterestRateTarget = v;
            OnPropertyChanged();
        }
    }

    // ★トリレンマ（2026-08-13）：兌換を維持しながら資本も自由なら、金利は自分のものでない＝**掴めなくする**。
    //   行は消さない（いま金利がいくらかは見えている必要がある）＝2026-09-23からは段ボタンを**丸ごと無効**にする
    //   （EconTab の ContentControl に IsEnabled＝Avalonia は無効を子へ伝える）。説明文も書かない（思想⑦）——
    //   掴めないことと、この真下に並ぶ通貨制度の法・法律タブの資本移動の法が状態を示していれば足りる。
    //   ⚠エンジン側（Government の rateTarget 差し替え）と**同じ条件式**を読む＝UIとエンジンが食い違わない。
    public bool InterestLeverEnabled
        => !(_player.Coef.TrilemmaRateGate > 0 && _player.OnGoldStandard && !_player.CapitalControlLaw && (_player.Coef.SilverLightDiscipline <= 0 || _player.GoldStandardLaw));   // ★B6（実バグ21件・2026-09-07）：銀の段の軽い規律の第4条件をエンジン（Government）と揃える＝銀正貨のプレイヤーだけレバーが死んでいた

    // 近代化投資：四半期に ModernizationBudget（GDP比の凸コスト）を全資本区画の建設予約へ（World.ApplyModernizationInvestment が自動適用）。
    // ★2026-08-10 に財政タブへスライダーを復活。⚠エンジン側の対（Simulation.QuarterStart がプレイヤー国を上書きしない）と**セット**で意味を持つ
    //   ＝片方だけ戻すと、押した段が次の四半期に自動値へ戻って「段ボタンが効かない」状態になる。
    public double ModernizationInvest
    {
        get => _player.ModernizationFrac;
        set { _player.ModernizationFrac = value; OnPropertyChanged(); }
    }

    /// <summary>技術水準の年次推移（作者指示2026-08-15「技術力はヘッダーから撤去＝財政タブの技術効率へ移設」）。
    /// 置き場は近代化投資の行＝この行だけが「効率 +n%」（最先端との差）を名乗る＝技術の量はここに集める。
    /// ★新しい系列も状態変数も作らない＝毎年採取されセーブにも入っている _histTech を、ヘッダーと同じ作り手（StatG）で読むだけ。
    /// ★毎tickの通知を持たない get-only＝吹き出しが開いた瞬間にだけ評価される
    ///   （点列は SeriesView のキャッシュ＝年が変わるまで実体を作り直さない。Points は HeaderStat.SafePoints で必ず非null）。</summary>
    public HeaderStat TechHover => StatG("", "技術水準", _histTech, v => v.ToString("F2"), 0, $"技術水準 {_player.TechLevel:F2}");

    // === 予算予想（経済タブ）：Fiscal.Step と同じ式を「今の目標（段ボタンで置いた値）＋今のpop状態」で毎tick評価＝常に予想値（四半期を待たない）===
    // 収入＝各税の粗収入×汚職控除。支出＝維持費・利払い・軍需/行政（歳入シェア）・社会保障/年金（裁量余剰上限）・近代化投資（国庫割合の月割）。
    private void UpdateForecast()
    {
        var c = _player.Coef;
        double earnedAll = 0, earnedElite = 0, headNonPriv = 0, landRent = 0, popAll = 0, massSize = 0, oldSize = 0;
        foreach (var r in _player.Regions)
            foreach (var p in r.Pops)
            {
                double e = System.Math.Max(0, p.Earned);
                earnedAll += e; popAll += p.Size;
                if ((p.IsModernElite || p.Closed) && !p.IsSoldier) earnedElite += e;
                if (!p.Closed && !p.IsSoldier) headNonPriv += p.Size;
                if (p.LandRent > 0) landRent += p.LandRent;
                if (!p.Closed && !p.IsModernElite) massSize += p.Size;
                if (!p.IsSoldier) oldSize += p.Size * p.OldShare;
            }
        double leak = 1.0 - c.CorruptionMax * _player.EffectiveCorruption;   // 汚職控除＝エンジン Fiscal の leak と同じ正準定義 World.EffectiveCorruption を直読み（C2-C5撤去2026-08-16：旧式(1−行政)×(1−清廉度)はV2恒久ONで撤去＝清廉度読みのままだと監視(OversightV2)の効いた国の税収を過小表示していた）
        double gIncome = _player.Policy.IncomeTaxTarget * earnedAll * leak;
        double gProg = _player.Policy.ProgressiveTaxTarget * earnedElite * leak;
        double gPoll = c.PollTaxRate * _player.Policy.PollTaxTarget * headNonPriv * leak;
        double gLand = _player.Policy.LandTaxTarget * landRent * leak;
        double net = gIncome + gProg + gPoll + gLand;
        // 関税：直近の貿易額（世界価格建て・国際市場は清算単位（四半期清算=3ヶ月／月次清算=1ヶ月）＝MonthsPerClearing で月割）×レートで予想。正=輸入関税収入／負=輸出補助（ダンピング＝コスト）。
        double tImpVal = _player.LastImportValue / (double)c.MonthsPerClearing, tExpVal = _player.LastExportValue / (double)c.MonthsPerClearing;   // 清算単位→月割（月次清算に追随・2026-08-22）
        double TariffRev(double rate) => (rate >= 0 ? tImpVal : tExpVal) * rate;   // ★段3-1：係数 TariffRevenueRate（常に1）を削除
        double tariffGross = TariffRev(_player.Policy.TariffTarget);   // 予想（目標レート＝バーに即応）
        // 接14（監査2026-07-11・2026-07-25実装）：実効はUIの自前近似でなくエンジンの実績診断を読む（国際市場は清算単位＝MonthsPerClearing で月割。負値＝ダンピングの補助金支出）
        double tariffActMonthly = _player.LastTariffRevenue / (double)c.MonthsPerClearing;
        // 維持費の按分（表示のみ）：治安維持力維持＝軍の俸給→軍事費へ・行政コスト→行政費へ加算（合計は不変）。軍事費0%でも俸給は残る＝仕様。
        double milUpkeep = c.CoercionUpkeepRate * _player.BaseCoercion * (popAll / System.Math.Max(1.0, c.PoliticalReferencePop));
        double adminUpkeep = c.AdminCostPerPop * popAll;
        double upkeep = milUpkeep + adminUpkeep;
        double interest = _player.GovDebt * _player.GovBondRate / (double)Calendar.MonthsPerYear;
        // ★予測はエンジンが実際に施行する目標を読む（2026-09-12 反証・接14と同じ作法＝UIに第二の式を置かない）。
        //   旧は生の陸海の和（Policy.MilitarySpendingTarget）で、段（R2）では段へ丸められる前の値＝開幕0.30に対し施行は段1の0.25＝予測が**2割過大**だった。
        double mil = Government.MilitaryTotalTarget(_player, _player.Policy) * c.MilitaryRevenueShare * net;
        double admin = c.AdminBudgetFrac * _player.Policy.AdminSpendingTarget * net;
        double welCap = c.WelfareRevenueShare * _player.FiscalCapacity;   // 裁量余剰の上限（請求の上限＝エンジンの Fiscal と同じ式・借入スタックの出費上限は 2026-09-28 に撤去）
        double wel = System.Math.Min(c.WelfareCostRate * _player.Policy.SocialSecurityTarget * massSize, welCap);
        double penLevel = _player.Pension switch { ReformLevel.Full => 1.0, ReformLevel.Limited => 0.5, _ => 0.0 };
        double pen = System.Math.Min(c.PensionCostRate * penLevel * oldSize, welCap);
        // ★是正2026-08-10：ここは**休眠側の旧式**（frac×国庫×0.5）を自前で計算していて、実際に国庫から引かれる額と一致していなかった
        //   （現行は ModernizationCostGdpShare>0＝GDP比の凸コスト×制度疲労/技術の遅れ×旧体制の反対）。すぐ下の IndTip は
        //   新式の内訳を出していたので、**倍率の説明だけ新しく金額は古い**という食い違いにもなっていた。
        //   予算表の他の行と同じ作法（接14＝UIの自前近似をやめエンジンの式を読む）に揃える＝World.ModernizationBudget が唯一の出所。
        double ind = _player.ModernizationBudget() / (double)Calendar.MonthsPerQuarter;   // 四半期→月割

        // 実効額（税行のみ現在の実効レート×同じ課税ベース）。支出側の実効は接14（監査2026-07-11・2026-07-25実装）＝
        // UIの自前近似をやめ、エンジンが予算表向けに書く実績診断（World.LastMilitaryClaim等・Fiscalは月次実行＝当月値）を読む。
        // UI式とエンジン式の将来的な乖離（間接税の基数算入・裁量余剰の平滑化等を近似が追い損ねる問題）を構造的に断つ。
        double aIncome = _player.TaxRate * earnedAll * leak;
        double aProg = _player.ProgressiveTax * earnedElite * leak;
        double aPoll = c.PollTaxRate * _player.PollTax * headNonPriv * leak;
        double aLand = _player.LandTax * landRent * leak;
        double netA = aIncome + aProg + aPoll + aLand;
        double aMil = _player.LastMilitaryClaim + milUpkeep;
        double aAdmin = _player.LastAdminClaim + adminUpkeep;
        double aWel = _player.LastWelfareCost * _player.FiscalFundingRatio;   // 実効＝請求×国家の支払い率 r（エンジンの Fiscal と同じ・2026-09-28）
        double aPen = _player.LastPensionCost * _player.FiscalFundingRatio;
        double aInterest = _player.LastInterest;   // R9満期構造の実クーポン込み（UI近似はGovBondRate直＝乖離しうる）

        InterestLine = "利払い"; InterestAct = Money(aInterest); InterestAmt = Money(interest);
        // ★陸海それぞれの金額（作者指示2026-08-11）。台帳は陸海で分かれていないので、**エンジン自身の配分**
        //   （World.NavalSpendShare＝NavyUpkeep/(Army+Navy)）で総額を割る＝UIに第二の配分式を置かない。
        //   和は必ず旧「軍事費」の額と一致する（表示を分けただけで金は動いていない）。
        double navShare = _player.NavalSpendShare;
        ArmyAct = Money(aMil * (1 - navShare)); ArmyAmt = Money((mil + milUpkeep) * (1 - navShare));
        NavyAct = Money(aMil * navShare);       NavyAmt = Money((mil + milUpkeep) * navShare);
        AdminLine = $"行政費 {_player.Policy.AdminSpendingTarget:P0}　実効 {_player.AdminSpending:P0}"; AdminAct = Money(aAdmin); AdminAmt = Money(admin + adminUpkeep);  // 行政調達＋行政コスト（維持費の官僚分）
        // ★汚職根絶のラベル更新は撤去（汚職V2点火2026-08-15＝表示ごと削除）
        // 国民形成費のレバー行は廃止（2026-07-19）＝国民運動は需要駆動（内訳はヘッダー国民意識の説明API）
        if (OpiumVisible) OpiumLine = $"アヘン中毒度 {OpiumAvgAddiction():P1}";   // 中毒度は財の性質＝表示は残る（禁令は法律タブへ移行・2026-07-30）
        EduLine = $"教育費 {_player.Policy.EducationSpendingTarget:P0}　実効 {_player.EducationSpending:P0}"; EduAct = Money(_eduMonthly); EduAmt = Money(_eduMonthly);   // 実績（市場依存＝予想も実績値）
        WelLine = $"社会保障 {_player.Policy.SocialSecurityTarget:P0}　実効 {_player.SocialSecurity:P0}"; WelAct = Money(aWel); WelAmt = Money(wel);   // 02断線走査（2026-07-28）：「約束した福祉の何割が払えたか」の可視化（ビスマルク年金の財源問題）
        PensionLine = "年金"; PensionAct = Money(aPen); PensionAmt = Money(pen);
        // 見えない出費の可視化（監査2026-07-11 接1）：国庫を実際に動かすのに予算表に現れず「黒字表示なのに国庫が減る」原因だった3項目。
        double colonialUp = _player.LastColonialUpkeep * _player.FiscalFundingRatio;   // 植民地維持費（俸給＋補給財〔石炭/糧食/キニーネ〕の請求×国家の支払い率 r・2026-09-28）
        double tribOut = _player.LastTributeOut * _player.FiscalFundingRatio / (double)Calendar.MonthsPerQuarter;   // 属国貢納（支払＝請求×r・四半期→月割）
        double tribIn = _player.LastTributeIn / (double)Calendar.MonthsPerQuarter;                    // 属国貢納（受取・同）
        ColonialRowVisible = colonialUp > 1e-9;
        ColonialLine = "植民地維持費"; ColonialAmt = Money(colonialUp);
        TributeRowVisible = tribOut > 1e-9;
        TributeLine = "属国貢納(宗主へ)"; TributeAmt = Money(tribOut);
        TributeInRowVisible = tribIn > 1e-9;
        TributeInAmt = Money(tribIn);
        // ★汚職根絶費の行は撤去（汚職V2点火2026-08-15＝V2では請求ごと死んでいる＝常に0の行を出さない）
        // ★2026-08-15作者指示：技術力をヘッダーから移設＝近代化投資の欄に「技術効率」として表示。
        //   技術効率＝対最先端の遅れ（技術ゲインの残差比例式の駆動項＝遅れているほど£あたりの追いつきが大きい）。
        //   0%＝最先端＝投資しても技術は伸びない（工業化と制度疲労には効く）が一目で読める。
        double lag = _player.WorldTechLevel > 1e-9 ? System.Math.Clamp((_player.WorldTechLevel - _player.TechLevel) / _player.WorldTechLevel, 0, 1) : 0;
        IndLine = $"近代化投資 {_player.ModernizationFrac:P0}　効率 +{lag:P0}";
        IndAct = Money(ind); IndAmt = Money(ind);   // レバー直結＝実効=予想
        // ★コストの内訳ホバー（2026-08-04）：制度疲労・技術の遅れの割増と旧体制の反対＝なぜ同じ%でも高く/細くなるのかの分解（データのみ）
        {
            double dec = System.Math.Clamp(_player.InstitutionalDecay, 0, 1);
            var (trad, modern, mass) = InterestGroups.Blocs(_player);
            double tot = trad + modern + mass; double tradShare = tot > 1e-9 ? trad / tot : 0;
            IndTip = $"技術(対世界最先端) {(1.0 - lag >= 0.995 ? "最先端" : (1.0 - lag).ToString("P0"))}"
                   + System.Environment.NewLine
                   + $"コスト倍率 ×{1.0 + _player.Coef.ModernizationDecayCost * dec + _player.Coef.ModernizationTechLagCost * lag:F2}"
                   + $"(制度疲労 +{_player.Coef.ModernizationDecayCost * dec:P0}　技術の遅れ +{_player.Coef.ModernizationTechLagCost * lag:P0})"
                   + System.Environment.NewLine + $"旧体制の反対 -{System.Math.Min(1.0, _player.Coef.ModernizationOldGuardResist * tradShare):P0}(旧支配層の政治力 {tradShare:P0})";
            OnPropertyChanged(nameof(IndTip));
        }
        OnPropertyChanged(nameof(AdminTip)); OnPropertyChanged(nameof(ForeignInTip));   // ヘッダーから移した2つの内訳ホバー（IndTip と同じ追従）
        IncomeLine = $"所得税 {_player.Policy.IncomeTaxTarget:P0}　実効 {_player.TaxRate:P0}";
        IncomeAct = Money(aIncome); IncomeAmt = Money(gIncome);
        ProgLine = $"累進課税 {_player.Policy.ProgressiveTaxTarget:P0}　実効 {_player.ProgressiveTax:P0}"; ProgAct = Money(aProg); ProgAmt = Money(gProg);
        PollLine = $"人頭税 {_player.Policy.PollTaxTarget:P0}　実効 {_player.PollTax:P0}"; PollAct = Money(aPoll); PollAmt = Money(gPoll);
        LandLine = $"土地税 {_player.Policy.LandTaxTarget:P0}　実効 {_player.LandTax:P0}"; LandAct = Money(aLand); LandAmt = Money(gLand);
        // ★専売（作者裁定2026-09-20）＝率のレバーは無い（材の選択は生産タブ）。ここに出すのは対象の広さと、そこから出る実効率。
        // ★2026-09-20：「負担」は財の**数**ではなく**家計の買い物のうち専売になっている割合**（奢侈だけの国は数なら22%・負担なら6%）。
        //   掛けた「実効」は真の実効平均率＝PowerShares の常設歳入の項と同じ1本。
        MonopolyLine = $"専売 負担 {_player.StateMonopolyBreadth:P0}　実効 {_player.StateMonopolyBreadth * _player.Coef.StateMonopolyMargin:P0}";
        MonopolyAct = Money(_stateMonopMonthly); MonopolyAmt = Money(_stateMonopMonthly);   // 実績（市場依存）
        TariffLine = $"関税 {_player.Policy.TariffTarget:P0}　実効 {_player.Tariff:P0}";   // 負値＝ダンピング（輸出補助＝予想額が負）
        TariffAct = Money(tariffActMonthly); TariffAmt = Money(tariffGross);
        ForeignInAmt = Money(_fInMonthly);
        ForeignOutAmt = Money(_fOutMonthly);

        double spendTotal = upkeep + interest + mil + admin + wel + pen + ind + _eduMonthly + _fOutMonthly + colonialUp + tribOut;   // 接1：植民地維持費・貢納を合計へ（根絶費はV2撤去2026-08-15）
        double revTotal = net + tariffGross + _stateMonopMonthly + _fInMonthly + tribIn;
        double spendActT = aInterest + aMil + aAdmin + aWel + aPen + ind + _eduMonthly + _fOutMonthly + colonialUp + tribOut;
        double revActT = netA + tariffActMonthly + _stateMonopMonthly + _fInMonthly + tribIn;
        SpendTotalText = Money(spendTotal); SpendTotalAct = Money(spendActT);
        RevenueTotalText = Money(revTotal); RevenueTotalAct = Money(revActT);
        double bal = revTotal - spendTotal;
        BudgetBalanceText = (bal >= 0 ? "+" : "-") + Money(Math.Abs(bal));

        // 国債（借入）＝ヘッダーへ移動（借金ボタン廃止・2026-07-21ユーザー指示／借入はエンジンが r＜1 の月に自動・2026-09-28）。中核データ（残高・対GDP・借入数・信認）＋デフォルト予兆。ホバーで内訳。
        DebtHeaderText = $"国債:{Money(_player.GovDebt)}"   /* ★2026-08-02：借入数は非表示（ホバーの内訳で足りる） */
                       + (_player.InsolvencyQuarters > 0 ? $"　デフォルトまで{Math.Max(0, c.DefaultTriggerQuarters - _player.InsolvencyQuarters)}か月" : "");   /* ★2026-09-26：カウンタは月次で進む（dorm-29）＝旧「四半期」は3倍ずれていた */
        // 財7単位フリップ（2026-07-26）：エンジンの金本位資格/金利プレミアムが年換算GDP基準になったのに合わせ、表示も対年GDP（史実の債務比率と直接比較できる単位）
        // 分母＝**年次名目**（月次 NominalGdp×12）＝エンジン Fiscal.cs の兌換停止/国債金利の閾値と同じ単位（旧＝年次実質 Gdp×12 で表示だけ別物差し・2026-09-11 #10）
        DebtHeaderTip = $"国債 {Money(_player.GovDebt)}　対年GDP {(_player.NominalGdp > 1e-9 ? System.Math.Min(9.99, _player.GovDebt / (_player.NominalGdp * (double)Calendar.MonthsPerYear)) : 0):P0}　金利 {_player.GovBondRate:P0}　債券価格 {_player.BondPrice:P0}"
                       + (_player.ForeignDebt > 1 ? $"\nうち対外 {Money(_player.ForeignDebt)}" : "")
                       + (_player.AusterityIntensity > 0 ? $"\n緊縮 {_player.AusterityIntensity:P0}" : "")
                       + (_player.CumulativeDevaluation > 1 ? $"\n切下げ累計 {Money(_player.CumulativeDevaluation)}" : "")
                       + (_player.FiscalFundingRatio < 1 ? $"\n支払い率 {_player.FiscalFundingRatio:P0}" : "");
        DebtHeaderBrush = _player.InsolvencyQuarters > 0 ? NewsRed : TermNeutral;

        // ⚠撤去2026-08-23（読み手ゼロ）: GoldJoinEnabled（旧・金本位ボタンのグレーアウト判定）＝ボタン自体が通貨制度の3ボタンへ移行して消えていた。
    }
    // 国債＝ヘッダー表示（借金ボタン廃止・2026-07-21）。DebtHeaderText＝残高（+デフォルト予兆）／Tip＝内訳／Brush＝支払不能で赤。
    [ObservableProperty] private string _debtHeaderText = "";
    [ObservableProperty] private string _debtHeaderTip = "";
    [ObservableProperty] private HeaderStat _treasuryHover = new();   // 国庫のホバーグラフ（年次推移・2026-07-16）
    // ★2026-08-02 描画クラッシュの修正：Avalonia の Polyline は Points が null だと CreateDefiningGeometry で
    //   ArgumentNullException を投げ、**毎フレーム**描画パスが中断する（画面が更新されず「操作を受け付けない」ように見える）。
    //   バインド経路の途中が null（TreasuryHover 未設定・ProdDetail が財未選択で null）だと Points に null が届く。
    //   TargetNullValue/FallbackValue では塞げないことを実測で確認済み（固定値にすると例外0・全画面が撮れる）ため、
    //   **VM 側で必ず非nullを返すプロパティ**を用意してそこへバインドする。空の Points は例外にならない。
    public global::Avalonia.Points TreasurySparkPoints => TreasuryHover?.Points ?? new();
    partial void OnTreasuryHoverChanged(HeaderStat value) => OnPropertyChanged(nameof(TreasurySparkPoints));
    [ObservableProperty] private IBrush _debtHeaderBrush = TermNeutral;
}
