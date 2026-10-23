// GameViewModel.cs ＝ ゲーム本体の核＝シム駆動・時間/速度・タブ切替・地図状態・書式ユーティリティ・行の据え置き同期。
// ★2026-08-10のUI分割：GameViewModel.cs から**メンバーを移しただけ**（ロジック・順序とも不変）。
//   同じ partial class GameViewModel の一部＝どのファイルからでも全メンバーに触れる。
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.SimCore;

namespace PriceOfBread.App.ViewModels;

// ゲーム本体。ヘッドレス SimCore を駆動。プレイヤー国は NullAgent ＝ UI が World.Policy を操作（AIは介入しない）。
// 速度クロック：x1=1秒1日。経済は四半期刻みなので「画面は毎日進め、90日ごとに StepQuarter」。
// ★2026-08-10：タブ別 partial へ分割。Fiscal/Law/Politics/Diplomacy/Pop/War/Popups/Header/News/Map/SaveMenu/Refresh/Rows。
public partial class GameViewModel : ViewModelBase
{
    private readonly MultiNationSim _sim;
    private readonly int _playerIndex;
    private readonly World _player;
    // ★スレッドタイマー駆動（2026-08-03・作者報告「速度3でも時間の進みがかなり遅いが、地図を動かすと時間がちゃんと進む」）：
    //   旧 DispatcherTimer はディスパッチャの都合（描画アイドル・ウィンドウ遮蔽・入力の有無）で発火が間引かれ、
    //   ゲーム内時間そのものが減速していた（実測＝同一ビルドで 20日/秒の時間帯と 3.7日/秒の時間帯が混在）。
    //   スレッドプールのタイマーが 50ms ごとに **Post でディスパッチャを起こす**＝遮蔽やアイドルに左右されない。
    //   OnTick は従来どおり UIスレッドで走る（スレッド安全性は不変）。Post が詰まって連続実行されても
    //   壁時計の実経過（_tickWatch）で加算するので二重進行しない（elapsed≈0）。
    public double PerfStepDayMs, PerfDailyMs, PerfMonthRefreshMs, PerfQuarterRefreshMs;
    public double PerfElapsedRaw, PerfElapsedUsed, PerfDays; public int PerfTicks, PerfSpeedSeen;   // A1 計測用の累計（--perf が読む・力学非接触）
    private System.Threading.Timer? _timer;
    private volatile bool _timerStopped;
    private double _simDays, _daysSinceStep, _monthDelta;
    // _satHistory（四半期満足度＝旧SatBars用）は削除（監査2026-07-12 死16/他20）
    // イベントログ（アラート）：SimCore は外交ログしか持たないので、UI 側が四半期ごとに「状態遷移の瞬間」を検出して
    // 「yyyy/MM/dd 国名で、〇〇が発生」の形で Alerts に積む。解決法は提示しない（何が起きたかだけ）。
    private double _prevDeval; private bool _prevAusterity, _prevDepression, _prevHeatHot, _prevFamine;
    // 全国の政体・内戦の前回値（政変/内戦の発火検出）。★国の Id をキー（2026-09-28 処理の一元化 段2 束2）＝旧は Powers の添字の配列で、
    //   国の数が変わった四半期（建国・再併合）に配列ごと作り直して比べなかった＝戦争の形の革命（再併合と同じ四半期に親の政体が変わる）の政変の報せが出なかった。
    private System.Collections.Generic.Dictionary<long, Polity>? _prevPolities;
    private System.Collections.Generic.HashSet<long>? _prevCivilWarKeys;   // ★内戦一元化：タグ付き戦争のペアIdキー（新経路の内戦検出）
    private System.Collections.Generic.Dictionary<long, double>? _prevTerror;   // 国ごとの粛清累計の前回値（恐怖政治ニュース・Id基準＝count変動に強い）
    private System.Collections.Generic.HashSet<long>? _prevPowerIds;   // 前四半期に存在した実国のId集合（分離独立の建国ニュース＝新Id出現）
    private System.Collections.Generic.Dictionary<long, (bool Dep, bool Gold, bool Def, bool Fam)>? _prevEconState;   // 外国経済イベントの前回値（恐慌/金本位/デフォルト/大凶作・Id基準＝2026-07-24 §3-2）
    // 終局年代記（#129・2026-07-25）：政変・戦争・恐慌・デフォルト・建国の事実記録（発生日・全国・遅延なし）。終局画面で年表表示
    private readonly System.Collections.Generic.List<AnnalRow> _annals = new();
    private System.Collections.Generic.Dictionary<(long, long), string>? _prevWars;   // 戦争の開戦/終戦エッジ（Id対→表示名）
    private void Annal(string icon, string text) => _annals.Add(new AnnalRow(EventDate(), icon, text));
    // 自国の転機通知の前回値（法律・税解禁・与党・金本位・ランク・疫病・制度・デフォルト）
    private Suffrage _pfSuf; private EducationLaw _pfEdu; private CensorshipLaw _pfCen; private LaborUnionLaw _pfUni; private WorkHours _pfWh; private bool _pfTreatyPortInForce;   // 条約港が効いているか（自然失効＝真から偽への通知用・案A 2026-08-16・(399) で港州の外国持分から読む）
    private bool _pfChild; private WomenRightsLaw _pfWomenRights; private ReformLevel _pfCorn, _pfLand, _pfSerf, _pfPen;   // ★女性の権利（統合法・2026-08-10）＝旧・財産権/参政権の2boolを1段へ
    private SocialSecurityLaw _pfSS;   // 社会保障法の段（弧3-3・2026-07-24＝段の変わり目をニュースに残す）
    private ColonialSuffrageLaw _pfColSuf; private MigrantNativeSuffrageLaw _pfMigSuf;   // 参政権の範囲2法（2026-08-16）
    private bool _pfOrderLow;          // 治安崩壊の前回値（接15・2026-07-25＝秩序<50%のエッジ通知）
    private bool _pfGold, _pfDefault, _pfPatent, _pfJoint, _pfEpidemic;
    private int _pfParty; private PowerRank _pfRank;
    private static readonly IBrush TensionLow = new SolidColorBrush(Color.Parse("#1F5FA8"));    // 緊張 低=青
    private static readonly IBrush TensionMid = new SolidColorBrush(Color.Parse("#B8860B"));    // 緊張 中=黄
    private static readonly IBrush TensionHigh = new SolidColorBrush(Color.Parse("#A0432D"));   // 緊張 高=赤（警告赤に統一）
    private static readonly IBrush NewsBlue = new SolidColorBrush(Color.Parse("#4A6A8C"));
    private static readonly IBrush NewsGold = new SolidColorBrush(Color.Parse("#8A6A20"));
    private static readonly IBrush NewsRed = new SolidColorBrush(Color.Parse("#A0432D"));
    private static readonly IBrush NewsGreen = new SolidColorBrush(Color.Parse("#2E7D32"));
    private static readonly IBrush SelfRowHighlight = new SolidColorBrush(Color.Parse("#C7DCF2"));   // 自国行のハイライト（外交タブ・終局ランキング共用）
    // 外交タブ・国カードの関係バッジ色（GameViewModel.Refresh.cs）＝固定パレット7色。戦争中は NewsRed／同盟は NewsGreen と同値なので使い回す。
    private static readonly IBrush BadgeDefault = new SolidColorBrush(Color.Parse("#555555"));
    private static readonly IBrush BadgeOwnVassal = new SolidColorBrush(Color.Parse("#3A5A8A"));
    private static readonly IBrush BadgeOwnSuzerain = new SolidColorBrush(Color.Parse("#8A6A2A"));
    private static readonly IBrush BadgeOtherVassal = new SolidColorBrush(Color.Parse("#7A6A55"));
    private static readonly IBrush BadgeNoTerritory = new SolidColorBrush(Color.Parse("#8A8070"));
    // 政体色（外交タブの国カード・PolityColor）＝固定パレット9政体7色。立憲君主制は BadgeOwnVassal、ブルジョア独裁は
    // BadgeOwnSuzerain、既定(null/無政府)は BadgeDefault と同値なので使い回す。
    private static readonly IBrush PolityAbsolutist = new SolidColorBrush(Color.Parse("#7A3030"));   // 絶対/専制/幕府
    private static readonly IBrush PolityRepublic = new SolidColorBrush(Color.Parse("#2E6E5E"));
    private static readonly IBrush PolityMilitary = new SolidColorBrush(Color.Parse("#5A3A6A"));
    private static readonly IBrush PolityProletarian = new SolidColorBrush(Color.Parse("#9A2A2A"));
    // 報せの伝播：遠い国の事件は距離分だけ遅れて届く（配達待ちキュー）。電信の普及（年代）でラグは縮み、1870年に実質リアルタイム。
    private readonly List<PendingNews> _pending = new();
    private sealed record PendingNews(double DeliveryDay, double EventDay, string Icon, string Body, IBrush Color, bool Mine);   // 表示日付＝届いた日・遅延分は「（nヶ月前）」を付す
    private const double NewsDelayPerKm = 0.007;       // 1789年の伝播の遅さ＝1kmあたり日数（米≒5600km→約39日・東京≒9600km→約67日・パリ≒340km→約2日）
    // ★2026-09-03（AUD-05）：NewsRealtimeYear（暦年1870）は撤去＝報せの速さは前線技術のランプから導く（GameViewModel.News.cs）。
    // 経済イベント検出の閾値（基準化と検出で同値＝ズレると誤発火。UI通知のみ・エンジンとは独立）
    // ★恐慌の閾値は SelfPlayMatch の1組を読む（fis-24・2026-09-26・旧＝同じ値の写し）。入り0.20／出0.08の2値＝境界で「恐慌に入った／回復した」が交互に出る点滅を止める（2026-08-23）
    //   ★(450) 判定そのもの（入り・出の 2 値と恐慌の語）は SelfPlayMatch.InDepression／IsGreatDepression／DepressionWord の 1 組を呼ぶ（Runner の play の報告も同じ関数・旧はここに 3 本の定数の写しと判定式があった）。
    private const double FinancialHeatHot = 0.85;          // 金融過熱の警報
    private const double FamineHarvest = 0.80;             // 収穫係数がこれ未満で飢饉
    private const double AusterityActive = 0.01;           // 緊縮の発動判定
    private const double FillWarnThreshold = 0.95;         // 充足率がこれ未満で不足警告（過密/主食/住居）
    private const double TensionRepromptDrop = 20.0;       // 緊張がこの幅だけ下がると再プロンプト可
    // 階級名（3層分類の文字列比較・複数箇所で使用）
    private const string CapitalistClass = PlayText.CapitalistClass;   // ★(435) 階級名の定数は SimCore の 1 か所（Runner の報告と同じ層の分け方）
    private const string BureaucratClass = PlayText.BureaucratClass;
    private const string ArtisanClass = PlayText.ArtisanClass;
    private const string MerchantClass = PlayText.MerchantClass;

    [ObservableProperty] private string _dateText = "";
    // ★2026-09-23（作者指示「文字数固定」）：国庫と収支を**2枚に割る**。1枚のままだと箱の右端は止まっても
    //   中の収支の桁が動いて読み位置がずれる（＝指摘が消えない）。それぞれが自前の最小幅を持つ。
    [ObservableProperty] private string _treasuryText = "";
    [ObservableProperty] private string _balanceText = "";
    // 金融セクション（財政タブ）：1本の連結文字列でなく「ラベル｜値」行に分解（銀行なし＝与信残高/兌換系は「—」）。
    // 金利doc B1（2026-07-30）：金利だけが他11レバーと違い「目標／実効」の1行ペアを持たず、実効が別行に散っていた＝ペアへ統一。
    // 併せて年率換算（World.LoanRatePerYear）を1行で出す＝無次元レバーの目盛だけでは何%の金利なのかが盤面に無かった。
    // ★2026-09-26：換算の係数が0のあいだ（今の既定）は行ごと隠す（LoanRateVisible）＝点火まで 0.0% を出さない。
    [ObservableProperty] private string _interestLeverLine = "";
    [ObservableProperty] private string _loanRateText = "";
    // ★2026-09-03（キューA-03／出典 トリアージ 2-A 3-6）：慢性赤字の分子（硬性ゲート LossFinancedRate の材料）と家計債務が
    //   プレイヤーから完全に見えなかった＝ガラス箱の規律。金融ブロックに2行だけ足す（データのみ・説明文なし）。
    [ObservableProperty] private string _householdLoanText = "";
    [ObservableProperty] private string _lossFinancedText = "";
    // ⚠撤去2026-08-23（読み手ゼロ）: CreditText ＝与信残高はヘッダー金融ホバー（Header.cs の FinanceTerms）が表示。axaml 束縛なし。
    [ObservableProperty] private string _coverText = "";
    [ObservableProperty] private string _playerName = "";   // 旧DebtText/DebtVisible（未バインド）は削除＝財政劣化の連鎖はDebtLineへ統合（監査2026-07-12 死14）
    // ★2026-09-23（作者指示「文字数固定」）：地位（列強12位／準列強／中堅／弱小）を国名から割る。
    //   動くのは**地位の側**（順位・ランクは世界の側で変わる）＝ここだけ固定幅にすれば桁ズレは止まる。
    //   ⚠国名まで固定幅にはしない：最長の変体名「ポーランド＝リトアニア」は18px太字で216px あり、常時確保すると
    //     ヘッダー1行目の合計が全画面の枠を越えて WrapPanel が折り返す＝2段化（2026-08-02に却下済み）を招く。
    [ObservableProperty] private string _playerRankText = "";
    // 速度レベル（1〜3＝レート）と再生状態は分離：1/2/3は速度の選択（ハイライトのみ）・スペースで再生/停止。
    [ObservableProperty] private int _speed = 2;
    [ObservableProperty] private bool _playing;
    public bool IsSpeed1 => Speed == 1;
    public bool IsSpeed2 => Speed == 2;
    public bool IsSpeed3 => Speed == 3;
    public bool IsSpeed4 => Speed == 4;
    public string PlayIcon => Playing ? "SPACE:⏸" : "SPACE:▶";
    partial void OnSpeedChanged(int value) { if (value > 3) { Speed = 3; return; }   // ★最高速廃止＝旧状態(4)も3へ丸める
        OnPropertyChanged(nameof(IsSpeed1)); OnPropertyChanged(nameof(IsSpeed2)); OnPropertyChanged(nameof(IsSpeed3)); OnPropertyChanged(nameof(IsSpeed4)); }
    partial void OnPlayingChanged(bool value) => OnPropertyChanged(nameof(PlayIcon));


    // 地図中心UI：ラベル層と線の層。**リストごと差し替え**（要素単位のCollectionChanged千発を出さない＝ズーム/四半期の再構築が軽い）。
    // ★F7（作者裁定 2026-09-28）：州○（ProvinceDots）と山脈・砂漠・峠の○（BarrierDots）は廃止＝州は塗り分けと地名で表す。
    [ObservableProperty] private List<MapLabel> _mapLabels = new();
    [ObservableProperty] private List<MapEdgeLine> _mapEdges = new();   // 03地理 手順1（§6作者裁定）：陸グラフの辺＝陸路の骨格を地図に出す（データそのものの表示＝思想⑦に反しない）
    // 州ホバー＝地図左上の固定パネルに詳細（ツールチップより長いリストを出せる・上位品目のハイライト付き）。
    [ObservableProperty] private MapHoverInfo? _mapHover;
    // ★州パネルのピン留め（支配度 §7「ホバーで要約・クリックでピン」・2026-09-07）：州の左クリックで左上パネルを固定。同じ州でもう一度＝解除。他の州＝持ち替え（当たりは塗り分けのセル＝MapHitAt）。
    [ObservableProperty] private bool _hoverPinned;
    private Region? _pinnedRegion;   // ピンの鍵＝州（MapHoverInfo は四半期ごとに作り直される＝実体で比べると「同じ州で解除」が壊れ、表示も固まる＝レビュー是正2026-09-07）
    public void TogglePin(MapHoverInfo? hover, Region? region)
    {
        if (hover is null || region is null) return;
        if (HoverPinned && ReferenceEquals(_pinnedRegion, region)) { HoverPinned = false; _pinnedRegion = null; return; }
        MapHover = hover; _pinnedRegion = region; HoverPinned = true;
    }
    /// <summary>四半期の作り直しの後＝ピン留めの州の最新の要約へ差し替える（州が消えた＝分離/割譲ならピンを外す）。</summary>
    private void RefreshPinnedHover()
    {
        if (!HoverPinned || _pinnedRegion is null) return;
        if (_provHoverCache.TryGetValue(_pinnedRegion, out var fresh)) MapHover = fresh;
        else { HoverPinned = false; _pinnedRegion = null; MapHover = null; }
    }
    // 旧・国選択（SelectedNationName/Info/IsPlayer/DetailModalOpen）＝クリックモーダル廃止後の残骸を削除（監査2026-07-12 死35）

    // Escメニュー（設定=無効・タイトルに戻る）と終局（1949年12月のあと＝EndYear の1950年到達でランキング表示）
    [ObservableProperty] private bool _menuOpen;
    [ObservableProperty] private bool _endingOpen;
    partial void OnEndingOpenChanged(bool value) { if (value) PopupPause(); else PopupResume(); }   // ★2026-08-24：終局のランキングも時間停止（裏で1935年以降が進まない）
    [ObservableProperty] private string _endingTitle = "";
    [ObservableProperty] private List<EndingRow> _endingRows = new();
    [ObservableProperty] private List<AnnalRow> _endingAnnals = new();   // 終局年代記（#129・2026-07-25）
    private bool _endingShown;
    private readonly Action? _onExit;   // タイトルへ戻る（MainWindowViewModel が注入）
    // ヘッダー2行目：国家指標のリスト（一部はホバーで年次推移グラフ）。**行オブジェクトは据え置き＝値だけ更新**
    // （毎月のリスト差し替えは ItemsControl 全再構築＝行の高さが一瞬消え、上部バーが表示/非表示を繰り返して見えた・2026-07-19）。
    public ObservableCollection<HeaderStat> HeaderStats { get; } = new();
    // 年次時系列（毎年1点・126年ぶん）：ヘッダーのホバーグラフの素
    private readonly List<double> _histPop = new(), _histGdp = new(), _histProd = new(), _histTech = new(),
        _histLit = new(), _histSat = new(), _histMil = new(), _histCorr = new();
    private readonly List<double> _histFood = new();   // 主食充足の年次履歴（ヘッダーホバー）
    private readonly List<double> _histLeg = new(), _histCons = new();   // 正統性・国民意識の年次履歴（2026-07-15＝説明API/隠れドライバのヘッダーホバー）
    private readonly List<double> _histMandate = new();   // 政治資本（MandateSpent）の年次履歴（ヘッダーホバー・2026-07-16）
    private readonly List<double> _histTreasury = new();  // 国庫の年次履歴（ヘッダー国庫のホバーグラフ・2026-07-16）
    // ★2026-08-13（作者指摘「信用が非常に大切というのがUIからわからない」＋ヘッダー監査）：
    //   国力スコア＝軍事＋豊かさ＋金融＋技術、豊かさ＝GDP+工業化×9+満足度×12+植民地×1.5。〔★(457) この式は (449) で世界の総量に対する割合の 6 項へ置き換わり (456) で消えた＝今の国力は NationalScore.ShareScores・旧の式は計器 Instruments.OldScore だけ〕
    //   この7項のうち**工業化・金融・対外信用の3つがヘッダーに無かった**。とくに工業化はスコア最大の重み(×9)で
    //   本作の主題そのものなのに、自国の全体値を出す場所がUIのどこにも無かった（敵国は外交ホバー・州別は生産ホバーに有る）。
    private readonly List<double> _histInd = new(), _histCredit = new(), _histFin = new();
    private readonly List<double> _histGini = new();      // ジニ係数の年次履歴（2026-08-10作者要望）。★2026-09-23にヘッダー表示は撤去したが**採取は残す**（セーブの系列配列に居る＝外すと旧セーブが黙ってずれる）
    // ★2026-09-23（作者指示「識字率の右に満足度・不満度・闘争度」）：不満度の年次履歴。
    // ⚠セーブの系列配列（この下の targets と SaveMenu.cs の lists）の**末尾**に足した＝末尾追加だけが安全（途中に挿すと全系列が黙ってずれる）。
    private readonly List<double> _histDis = new();
    private int _histStartYear = Calendar.StartYear, _lastSampledYear;

    /// <summary>年次系列の点列と最小/最大の記憶（2026-08-10）。系列は**1年に1点しか伸びない**のに、
    /// ヘッダーは Refresh（月次）ごとに全系列の点列を作り直し、さらに Min/Max を別々に走査していた
    /// （1系列あたり5パス×14系列）。点数が変わるまでは同じ結果になるので、点数をキーに使い回す。
    /// 値は LINQ の Min/Max のまま求める＝**表示される数値は1ビットも変えない**（NaN の順序規約まで同じ）。
    /// キーは List の参照（系列の実体は生涯1つ）。</summary>
    private readonly Dictionary<List<double>, (int N, global::Avalonia.Points Pts, double Min, double Max)> _seriesCache = new();

    // 4タブ（地図上のドッキングパネル・暗転なし）：外交・経済・POP・法律と政治。空文字＝閉。
    [ObservableProperty] private string _activeTab = "";
    public bool PanelOpen => ActiveTab.Length > 0;
    public bool TabDiplo => ActiveTab == "外交";
    public bool TabEcon => ActiveTab == "財政";
    public bool TabPop => ActiveTab == "POP";
    public bool TabPolitics => ActiveTab == "政治";   // 政治タブ＝王権と議会・政体・政体の実績・政治影響力
    // ★2026-08-01：法律を独立タブへ分離（法が20→22本に増え、政治タブの右列に収まらなくなった）。
    // ⚠併せて「国家の変体」の表示を撤去（ユーザー指示）＝読み取り専用の目標表示で、操作も判断も生まなかった。
    public bool TabLaw => ActiveTab == "法律";      // 法律タブ＝法の一覧と議会立法（F2は下部へ統合2026-07-12）
    public bool TabWar => ActiveTab == "戦争";   // 戦争タブ（2026-07-19）＝交戦中の戦況＋終わった戦争の残置
    public bool TabTrade => ActiveTab == "貿易"; // 貿易タブ（2026-07-24）＝輸出入一覧・輸送サマリ・州別物流（外国経済の可視化doc §3-6/§3-7）
    // ★2026-08-10（作者指示）：国有化と巨大建造物は**生産タブの左下**へ戻す（2026-08-02に法律タブへ出していたものの差し戻し）。
    //   法律タブの右列は制度（自動施行の3機構）だけになる。
    // 幅（項4・2026-07-26：財政タブの右端見切れ＝範囲ラベル「＜ 100%」の窮屈と金額の桁上がりに+30の余裕）。
    // ★2026-08-10（作者裁定）：**全タブ650で統一**。外交のときだけ1280へ広げる分岐は撤去した＝
    //   フルスクリーン1920でもドックが画面の2/3を占め、地図が見えなくなっていた（外交の2段組は650の中に収める）。
    public double PanelWidth => 650;
    partial void OnActiveTabChanged(string value)
    {
        OnPropertyChanged(nameof(PanelOpen)); OnPropertyChanged(nameof(TabDiplo)); OnPropertyChanged(nameof(TabEcon));
        OnPropertyChanged(nameof(TabPop)); OnPropertyChanged(nameof(TabPolitics)); OnPropertyChanged(nameof(TabLaw)); OnPropertyChanged(nameof(TabWar)); OnPropertyChanged(nameof(TabTrade));
        if (TabWar) RefreshFactions();   // 国内勢力は戦争タブへ移設（2026-07-20）＝開いた瞬間に更新
        if (TabTrade) RefreshTrade();    // 貿易タブ（2026-07-24）＝開いた瞬間に更新（以後は四半期Refreshで追従）
        // 巨大建造物の更新は生産タブ側（ToggleProd→RefreshProduction と四半期Refreshの ProdOpen 経路）＝2026-08-10に左下へ戻したので法律タブでは組まない
    }
    [RelayCommand]
    private void SelectTab(string? tab)
    {
        ProdOpen = false;   // 生産タブ（全画面）と右ドックは同時に出さない＝QWERで生産タブから抜けられる
        DiscoveryOpen = false;   // 発見パネルは2026-09-07に右ドックと同寸へ＝同じ矩形に2枚重ねない
        ActiveTab = ActiveTab == tab ? "" : tab ?? "";   // 同名再指定＝トグルで閉じる
        if (ActiveTab == "法律") PoliticsTabAlert = false;   // 法改正の通知は法律タブを開いたら消灯
    }

    // 法律タブの通知（受動レイヤ・裁定2026-07-09「変更可能になった時点で法律タブを光らせる」）：
    // 各法の改革方向の一歩が「通らない→通る」へ変わった四半期に政治タブを点灯（Government.CanStep＝副作用なしの照会）。
    private bool _politicsTabAlert;
    public bool PoliticsTabAlert { get => _politicsTabAlert; set { _politicsTabAlert = value; OnPropertyChanged(); } }
    private ulong? _lawMovableBits;
    private void UpdateLawAlert()
    {
        ulong bits = 0; int i = 0;
        void Ax(PolicyAxis a, bool increasing) { if (Government.CanStep(_player, a, increasing)) bits |= 1UL << i; i++; }
        Ax(PolicyAxis.Suffrage, true); Ax(PolicyAxis.EducationLaw, true); Ax(PolicyAxis.Censorship, true);
        Ax(PolicyAxis.LaborUnion, true); Ax(PolicyAxis.ShorterHours, false);   // ★親軍法は2026-08-18に撤去（軍国度＝軍事費×徴兵法の導出量）
        Ax(PolicyAxis.WomenRights, true); Ax(PolicyAxis.CornLaws, false); Ax(PolicyAxis.LandReform, true); Ax(PolicyAxis.SerfEmancipation, true);
        if (_lawMovableBits is { } prev && (bits & ~prev) != 0 && ActiveTab != "政治") PoliticsTabAlert = true;   // 初回（ロード/開始直後）は基準線のみ＝点灯しない
        _lawMovableBits = bits;
    }
    // ⚠撤去2026-08-23（読み手ゼロ）: _buildableGoodIds/_buildableGoodNames ＝旧・工場建設コンボの残骸（着工は生産タブの StepStateTargetAt が現役）。


    // ★旧「海軍偏重」のレバー（戦争タブ）は撤去（2026-08-10 作者裁定「陸軍と海軍の注力レバーは廃止し、海軍維持費と陸軍維持費で表現する」）。
    //   陸海の重点は財政タブの陸軍維持費・海軍維持費の2本＝払う金そのもので置く（UI最小主義＝同じことを2箇所で操作させない）。

    // 旧UI由来の未バインド残骸を削除（監査2026-07-12 死16）＝法律の書込可能ラッパ（Militarism/WomenPropertyLaw/CornLaws/LandReform/SerfEmancipation/Pension/ReformOptions
    // ＝XAMLは*Target・LawXxxText系が現役）・ForeignCapitalText（外資は予算表の外資流入/流出の行が現役）・GoldStandardStatus（説明文＝文言規律にも抵触。状態はGoldShortが現役）。

    /// <summary>新規ゲーム＝**構築済み**のシムと助走中の価格標本を受け取る（必ずUIスレッドで呼ぶ＝InitAfterSimReady がタイマー等を起動）。
    /// ★シムの組み立てと開始前の助走（数秒）は MainWindowViewModel.BuildWarmedSim へ分離（作者指示2026-08-15）＝
    /// 裏スレッドで回して読み込み画面の点アニメを動かすため。⚠この型の静的フィールド初期化子は Avalonia のブラシを持つ
    /// ＝裏スレッドでこの型に触れてはならない（価格標本が素の List&lt;double&gt; で渡ってくるのはそのため）。
    /// 決定論の注意書き（シード1789UL・エージェント生成順・専売0・助走は StartWarmup の1本）は BuildWarmedSim 側に集約。
    /// 阿片の初期禁令（2026-07-20）は撤去（禁制品doc C3・2026-07-30）：法になった以上、初期値は他の法と同じく世界の初期状態が決める
    /// （プレイヤー国だけ最初から禁令＝AIと非対称だった）。禁令は法律タブで通す＝議会と利益集団の政治ゲートを経る。</summary>
    public MultiNationSim Sim => _sim;          // 内戦の陣営選択（P8）＝同じ世界のまま操作国を差し替える呼び手（MainWindowViewModel）
    public int PlayerIndex => _playerIndex;

    public GameViewModel(MultiNationSim sim, int playerIndex, List<double>[]? warmupPrices, Action? onExit = null, Action? onLoad = null)
    {
        _playerIndex = playerIndex;
        _onExit = onExit;
        _onLoad = onLoad;
        _sim = sim;
        _player = _sim.Nations[playerIndex].World;
        // 助走中の価格標本を引き取る＝開幕の1四半期目から価格グラフに線が引ける（作者フィードバック2026-08-10 項目1）。
        // 刻みは本編と同じ四半期＝x軸は一様。_wpVersion は標本数＝SampleWorldPrices が1標本ごとに進めるのと同じ数え方。
        _wpHist = warmupPrices;
        _wpVersion = warmupPrices is { Length: > 0 } ? warmupPrices[0].Count : 0;
        _lastSampledYear = _player.Year;
        SampleYearStats();   // 開始時点（1783）を年次グラフの起点に
        InitAfterSimReady();
    }

    // ロード用：復元済みシム＋エンベロープ（UI側の経過日数・年次グラフ・戦争UI状態）から再開。
    private GameViewModel(MultiNationSim sim, int playerIndex, double simDays, int dayInQuarterUI, int lastSampledYear,
                          List<double>[] hist, Action? onExit, Action? onLoad, WarUiSave? warUi = null)
    {
        _playerIndex = playerIndex;
        _onExit = onExit;
        _onLoad = onLoad;
        _sim = sim;
        _player = _sim.Nations[playerIndex].World;
        _simDays = simDays;
        _dayInQuarterUI = dayInQuarterUI;
        _lastSampledYear = lastSampledYear;
        // 監査2026-07-12（他20）：_histFood を追加＝ロード後も食料グラフの凶作履歴が残る（旧＝セーブ漏れで喪失＋年ラベルずれ）。
        // _satHistory（SatBars削除で読者消滅）はリストごと削除。セーブ形式はビルド内互換のみ（SimCore側と同方針）＝壊れた旧セーブはロード時catchで安全に弾かれる。
        // ⚠_histInd/_histCredit/_histFin（2026-08-13）は配列の**先頭**に挿入した＝この並びより前のセーブは全系列が3つずれて黙って読まれる（SaveMagic は POBSAVE1 のまま＝互換扱いのまま非互換）。
        List<double>[] targets = [_histInd, _histCredit, _histFin, _histPop, _histGdp, _histProd, _histTech, _histLit, _histSat, _histMil, _histCorr, _histFood, _histLeg, _histCons, _histMandate, _histTreasury, _histGini, _histDis];   // ★2026-09-23：不満度 _histDis を**末尾**に追加（旧セーブは hist.Length=17 で止まる＝不満度だけ空で再開）
        for (int i = 0; i < targets.Length && i < hist.Length; i++) targets[i].AddRange(hist[i]);
        _endingShown = PlayText.IsEnded(Calendar.StartYear + (int)_simDays / Calendar.DaysPerYear);   // 終局後のセーブでランキングを再表示しない。表示暦基準（★2026-08-10に助走が暦を進めなくなり、エンジン暦＝表示暦で一致した。旧＝助走4四半期ぶんエンジンが1年先行＝監査2026-07-11 他9）。★1950延長2026-09-22＝終局は表示暦が EndYear（1950年1月1日）に入った時＝GameQuarters 経過＝自己対戦と同じ四半期（旧＝EndYear+1で自己対戦より4四半期長かった）
        // 戦争UIの追跡状態を復元（レビュー2026-07-19）：戦費按分・勝敗基準・終わった戦争の残置＝ロードで途切れない。
        // InitAfterSimReady 内の初回 Refresh より前に適用（既存の交戦が「新規の敵」として再基準化されない）。
        if (warUi is not null)
        {
            _warCostStamp = warUi.CostStamp;
            foreach (var t in warUi.Tracks)
                _warTracks[t.Id] = new WarTrack
                {
                    Won0 = t.Won0, Lost0 = t.Lost0, LastScore = t.LastScore, LastMonths = t.LastMonths,
                    LastDeadSelf = t.DeadSelf, LastDeadEnemy = t.DeadEnemy,
                    LastCivDeadSelf = t.CivSelf, LastCivDeadEnemy = t.CivEnemy, CostShare = t.Cost,
                };
            foreach (var p in warUi.Pasts)
                PastWarRows.Add(new PastWarRow { Key = p.Key, EndDay = p.EndDay, Title = p.Title, Detail = p.Detail });
        }
        InitAfterSimReady();
    }

    // 新規/ロード共通の初期化（シムが確定した後）：通知基準・州リスト・建設可能財・タイマー。
    private void InitAfterSimReady()
    {
        foreach (var n in _sim.Nations) n.TelemetryEnabled = false;   // UIはTelemetryを読まない＝四半期スナップショットの割当/保持（セーブ肥大の一因）を止める
        _player.Explain = new ExplainLedger();   // 説明API（#63）：プレイヤー国だけに台帳を常設装着＝毎四半期のPolitics.Stepが正統性の内訳を記帳（本計算はビット恒等・ゼロコスト・SaveGameは非直列化なのでロード後もここで再装着）
        _player.ForceUnlock = true;              // ★再征服は常時ON（作者指示2026-08-24＝レバーを撤去）。実費は Territory.Step が持つ（国庫1000/月と全popの闘争度）＝国庫が尽きれば自動で止まる
        _treasuryMStart = _player.Treasury.Value;      // 月次収支の基準（新規/ロード共通）
        PlayerName = _player.SelfPower?.Name ?? _player.Region.Name;   // 国名（首都州名ではなく国名）
        foreach (var rg in _player.Regions) { _prevEduCum += rg.CumEducationCost; _prevStateMonopCum += rg.CumStateMonopolyRevenue; }   // 累積カウンタの基準化（初回表示が過去合計にならない）
        _prevForeignNet = (_player.ForeignCapitalIn - _player.CapitalAbroad).Value;   // 外資純フローの基準化
        SnapshotTurningPoints();   // 転機通知の基準値（開始/ロード時点の状態＝直後に誤発火しない）＝DetectEvents の基準更新と同じ1本（2026-09-11 #20・旧は治安と条約港が抜けていた＝治安<50%で開始/ロードすると誤発火）
        _diploLogSeen = _sim.Diplomacy.Log.Count;   // 既存の外交ログは既読（ロード直後に過去ログが再配達されない）
        BuildLaws();   // 法律タブの段階ボタン行（1934拡張E8・新規/ロード共通）

        _timerStopped = false;
        _timer = new System.Threading.Timer(_ =>
        {
            if (_timerStopped) return;
            Dispatcher.UIThread.Post(() => { if (!_timerStopped) OnTick(null, EventArgs.Empty); }, DispatcherPriority.Input);
        }, null, 50, 50);   // 50ms＝日付が滑らかに進む見た目の粒度。常時稼働（停止中も予算予想がスライダーに即応。Speed=0はOnTickでシムだけ止める）
        Refresh();
        UpdateDate();
    }

    private readonly System.Diagnostics.Stopwatch _tickWatch = System.Diagnostics.Stopwatch.StartNew();   // tick間の実経過（2026-08-03＝時間の進みをtickの疎密から独立させる）
    private static readonly int[] SpeedDays = { 0, 3, 9, 20, 45 };   // 速度レベル1..4→1秒あたりの日数（1遅/2中/3速/4最速）。index0は未使用。★4=45日/秒≝四半期2秒（作者指示2026-08-03「実行速度が遅い」）

    // 速度キー/ボタン：速度レベルを選ぶだけ（時間は動かさない＝ハイライトのみ）。再生/停止はスペース（TogglePause）に限る。
    [RelayCommand]
    private void SetSpeed(string s) { if (int.TryParse(s, out var v)) Speed = System.Math.Clamp(v, 1, 3); }   // ★2026-08-31：最高速(4=45日/秒)は廃止＝かくつくため（作者指示）

    [RelayCommand]
    private void TogglePause()
    {
        if (AnyPopupOpen) return;   // ★2026-08-24作者指示：モーダル中は時間を動かせない（ポップアップの裏で世界が進まない）
        Playing = !Playing;         // スペースキー：再生⇔停止
    }

    // 金額表示：通貨単位はポンド（£）。k（千）または M（百万）。例 200000→"£200k"、1_500_000→"£1.50M"。
    // ── 実兵数の表示（2026-08-08・作者指示「表示上は実際の兵数にする」）───────────────────
    //   旧 Force()（ゲージ×100の抽象数）は全置換で撤去済み。
    //   軍の規模は抽象ゲージでなく**人数**で見せる（代表スケール1単位＝1000人）。質（即応・熟練・技術・装備）は
    //   数に混ぜない＝勝勢%・即応%・熟練ツールチップへ分離。数は嘘をつかない（多いが弱い軍＝アヘン戦争が読める）。
    //   Nation を持たない抽象的な外国だけはゲージ×SoldiersPerMilitary で人数へ近似（実POPが無い）。
    private double TroopUnits(Power p) => p.Nation is { } nw ? nw.SoldierSizeK() : p.Military * _player.Coef.SoldiersPerMilitary;
    private string Troops(Power p) => Fmt.Men(TroopUnits(p));

    /// <summary>緊張の表示値＝**開戦までの近さ**（100%＝宣戦の閾値）。
    /// ⚠★2026-08-02の是正：旧は内部緊張を 0〜100 でクランプして「%」と呼んでいた。ところが
    ///   ポップアップの発火条件 <c>TensionPopupThreshold</c> も 100 だったので、**通知は必ず「100%」と表示された**
    ///   （作者報告「毎回100%にしか表示されない」）＝数字が情報を持っていなかった。
    ///   内部緊張の上限は200・宣戦は <c>WarDeclareTension</c>(140) なので、100 は本当は開戦の71%である。
    /// ★宣戦閾値で割れば「あと何割で戦争か」を意味する数になり、閾値を動かしても表示が自動で追随する。</summary>
    private double TensionPct(double tension)
        => System.Math.Clamp(tension / _player.Coef.WarDeclareTension, 0, 1);


    private double[] _ledgerPrev = new double[(int)TreasuryCat.Count];   // 台帳の前月値（月次差分の基準）
    private double _monthWarCost;      // 今月ぶんの戦費の実払い額（TreasuryLedger の陸軍・海軍・戦費の月次差分の和・符号反転）＝戦争タブが読む（GameViewModel.Refresh.cs）。SampleTreasuryLedgerが_ledgerPrevを上書きする前にOnTickで1回だけ取る
    private double _treasuryMStart;    // 月初の国庫（月次収支の基準・InitAfterSimReady で初期化）
    private int _dayInQuarterUI;        // 0..89（月/四半期境界の検出）

    // 三層時計に合わせた描写：日次＝価格（物価/国庫/生産詳細）、月次＝生産グラフ/POP/外交/州、四半期＝地図/法律。
    // ⚠2026-09-26 ㊽ から国内価格は旬の最終日（PriceFlowStepDays 日ごと＝2026-09-30 (114) から10日）にだけ動く＝その間の日は同じ価格を描き直す（描写は日次のまま）。
    //   建設の進捗と信用（与信・信頼）も (114) から旬の初日にまとめて動く。
    // ＝それぞれの表示頻度をその計算頻度に一致させる（シム計算は軽く、重いのはUI再構築＝各層のcadenceで最小限に描く）。
    private void OnTick(object? sender, EventArgs e)
    {
        ConsumeZoom();   // ホイールが止まった後に1回だけ地図を再構築（ズームの軽量化）
        if (!Playing) { _tickWatch.Restart(); if (TabEcon) UpdateForecast(); return; }   // 停止中も財政タブ表示中はスライダーの予想値が即応（閉じている間は走らせない＝全州×全pop走査の間引き）
        // ★実測の経過時間で進める（2026-08-03・作者報告「速度3でも時間の進みがかなり遅いが、地図を動かすと時間がちゃんと進む」）。
        //   旧＝「tick は 50ms ごとに来る」前提の固定加算（×0.05）。DispatcherTimer は描画や入力が無いと間引かれるので、
        //   放置中は tick が疎になり**ゲーム内時間そのものが減速**していた。地図を動かすとディスパッチャが活性化して
        //   tick が密になり「ちゃんと進む」＝報告の症状そのもの。壁時計の実経過で加算すれば tick の疎密と無関係に速度が真になる。
        double elapsed = System.Math.Min(_tickWatch.Elapsed.TotalSeconds, 1.0);
        PerfTicks++; PerfElapsedRaw += _tickWatch.Elapsed.TotalSeconds; PerfElapsedUsed += elapsed;   // A1 計測：tick数・実経過・採用経過   // 上限1秒＝速度3で1tick最大20日（計算burstは有界のまま、疎tickでも速度が落ちない）
        _tickWatch.Restart();
        double days = SpeedDays[System.Math.Clamp(Speed, 1, 4)] * elapsed;
        PerfDays += days; PerfSpeedSeen = Speed;
        _simDays += days;
        _daysSinceStep += days;
        int whole = (int)_daysSinceStep;   // 端数を溜めて整数日ずつ進める（低速でも1日ずつ・高速でも日単位）
        if (whole > 0)
        {
            _daysSinceStep -= whole;
            bool monthCrossed = false, quarterCrossed = false;
            for (int d = 0; d < whole; d++)
            {
                var swDay = System.Diagnostics.Stopwatch.StartNew();   // A1 計測（2026-09-05）：1四半期あたりの内訳＝StepDay／日次更新／月次・四半期更新
                bool qDone = _sim.StepDay();
                PerfStepDayMs += swDay.Elapsed.TotalMilliseconds;   // 1日進める（StepDay内で月次決算・四半期戦略が境界発火。RNG順はStepQuarterと同一＝決定論不変）
                _dayInQuarterUI = (_dayInQuarterUI + 1) % Calendar.DaysPerQuarter;
                if (_dayInQuarterUI % Calendar.DaysPerMonth == 0) monthCrossed = true;   // 30/60/0(=90日)＝月次決算の境界
                if (qDone) quarterCrossed = true;
            }
            if (monthCrossed)
            {
                _monthDelta = _player.Treasury.Value - _treasuryMStart; _treasuryMStart = _player.Treasury.Value;
                // ★戦争タブの「戦費」はエンジンが実際に払った軍の支出を読む＝UIで係数から再計算しない。
                //   2026-09-30（支払いの一元化 段3-2）から戦費の独自の支払い（旧 Diplomacy.Fund）は無い＝戦争の費用は軍需の調達と兵士の給与（区分 陸軍・海軍）＋賠償（区分 戦費）。
                //   SampleTreasuryLedger が _ledgerPrev を「今」へ上書きする前に、その直前の月次差分を取る（符号は支出が負＝反転）。
                {
                    double warOut = 0;
                    foreach (var wc in new[] { TreasuryCat.Army, TreasuryCat.Navy, TreasuryCat.WarCost })
                    {
                        int wcIdx = (int)wc;
                        if (wcIdx < _player.TreasuryLedger.Length && wcIdx < _ledgerPrev.Length) warOut += _ledgerPrev[wcIdx] - _player.TreasuryLedger[wcIdx];
                    }
                    _monthWarCost = Math.Max(0, warOut);
                }
                SampleTreasuryLedger();   // ★台帳の月次差分を**ヘッダーの収支と同じ瞬間に**取る（2026-08-13）＝両者が構成上一致する   // 月次収支（先月の国庫増減・四半期境界も月境界）
            }
            // 戦費の按分累計は Refresh 内（月スタンプで1回/月・戦線数で均等按分）＝レビュー2026-07-19で移設
            if (quarterCrossed)
            {
                // ★作者裁定 f（2026-09-28）：操作国が再併合で吸収された（反乱側に乗り換えた後に親が勝った・革命が成就した）＝エンジンは親の国へ
                //   IsPlayer と操作者を移している（MultiNationSim.ReabsorbNations）。画面を親の国へ付け直して報せる。
                //   ⚠国を除くと Nations の添字が動く＝操作国より前の反乱国が消えても _playerIndex がずれる＝同じ付け直しで直す（報せは出さない）。
                int follow = _sim.Nations.FindIndex(n => n.World.SelfPower?.IsPlayer == true);
                if (follow >= 0 && (follow != _playerIndex || !ReferenceEquals(_sim.Nations[follow].World, _player)))
                {
                    bool absorbed = !_sim.Nations.Exists(n => ReferenceEquals(n.World, _player));
                    string newName = _sim.Nations[follow].World.SelfPower!.Name;
                    string notice = absorbed ? $"{PlayerNationName}は{newName}へ再併合された。\n\n操作国:{newName}" : "";
                    OnFollowPlayer?.Invoke(follow, notice);
                    return;
                }
                // （借入はエンジンが自動で借りる＝Fiscal の1行・国家の支払い率 r＜1 の月・2026-09-28 作者裁定「借入は自動化・借金ボタンは不要」＝画面の自動起債の予約2行を削除）
                // 法律の自動決定（UtilityAgent.DecideLaws の四半期呼び出し）は廃止＝プレイヤーが政治タブで目標を置く（AIボタンで随時助言）。
                DetectEvents();
                // _satHistory（四半期満足度＝旧SatBars用）は読者消滅につき削除（監査2026-07-12 死16/他20。年次の満足度グラフ_histSatが現役）
                if (_player.Year > _lastSampledYear) { _lastSampledYear = _player.Year; SampleYearStats(); }   // 年次グラフの採点（年が変わった四半期に1回）
                if (!_endingShown && PlayText.IsEnded(Calendar.StartYear + (int)_simDays / Calendar.DaysPerYear)) ShowEnding();   // 終局：**表示暦**の1949年12月が終わったら（＝GameQuarters 経過）ランキング（以後は観戦続行可）。★2026-08-10に助走が暦を進めなくなり、エンジン暦＝表示暦で一致した（旧＝助走で+1年先行し、表示上の最終年が丸ごと失われていた＝監査2026-07-11 他9）
                { var swQ = System.Diagnostics.Stopwatch.StartNew(); Refresh(includeMap: true); PerfQuarterRefreshMs += swQ.Elapsed.TotalMilliseconds; }          // 四半期＝地図含む全再構築（政策・議会・所有権が変わる層）
            }
            else if (monthCrossed) { var swM = System.Diagnostics.Stopwatch.StartNew(); Refresh(includeMap: false); PerfMonthRefreshMs += swM.Elapsed.TotalMilliseconds; }   // 月次＝地図以外（生産グラフ・POP・外交・州＝MonthSettle の層）
            else { var swR = System.Diagnostics.Stopwatch.StartNew(); RefreshDaily(); PerfDailyMs += swR.Elapsed.TotalMilliseconds; }   // 日次＝価格のみ（物価・国庫・生産詳細＝MarketFlow の層・価格そのものは旬の最終日に動く）
        }
        UpdateDate();           // 毎tick 日付を進める（経過日だけ安く更新＝UIスレッドを塞がず入力が通る）
        if (TabEcon) UpdateForecast();   // 予想値の毎tick更新は財政タブ表示中のみ（閉じている間は月次/四半期の Refresh 内呼び出しが最新化）
        FlushPending();         // 報せの配達は毎日判定（遠い事件ほど遅れて到着＝距離分のラグ）
    }

    // 日次の描写：価格層だけ（物価・国庫・生産タブの詳細パネルの価格）。重いグラフ本体・地図・国家・POPは触らない＝軽量。
    private void RefreshDaily()
    {
        TreasuryText = $"国庫:{Fmt.Money(_player.Treasury.Value)}";
        BalanceText = $"収支:{(_monthDelta >= 0 ? "＋" : "−")}{Fmt.Money(Math.Abs(_monthDelta))}/月";
        OnPropertyChanged(nameof(PriceLevelText)); OnPropertyChanged(nameof(ExchangeRateText));   // 物価・為替（計算プロパティ）を再読込
        RefreshProdDetail();                        // 生産タブの詳細パネルの価格（選択財のみ・軽い）。グラフ本体は月次
    }

    // 経過日表示のみ（毎tickで安く呼べる）。経済状態に触れないので軽い。
    private void UpdateDate()
    {
        int t = (int)_simDays;
        DateText = $"{Calendar.StartYear + t / Calendar.DaysPerYear}/{t % Calendar.DaysPerYear / Calendar.DaysPerMonth + 1:00}/{t % Calendar.DaysPerMonth + 1:00}";   // yyyy/mm/dd（日単位で滑らかに進む・360日年/30日月）
    }

    // 行コレクションの据え置き同期（2026-07-19）：キー集合が同じなら既存行へ値だけ流す（並び替えは Move＝コンテナ温存）。
    // 集合が変わった時だけ作り直す。毎月の Clear+再構築は行の Button/ツールチップを破棄し「時間を止めないとボタンが押せない」の主因だった。
    /// <summary>
    /// 文化・宗教の構成（#62）：州ラベル(Region.Culture/Religion)を州人口で加重し、シェア降順の表にする。
    /// World.ReligiousUnity/EthnicUnity は最大シェアだけのスカラーなので、ここでその内訳を開く。読むだけ＝SimCore不変。
    /// </summary>
    private void BuildComposition()
    {
        var rel = new Dictionary<string, double>();
        var cul = new Dictionary<string, double>();
        double total = 0;
        foreach (var r in _player.Regions)
        {
            if (r.State != RegionState.Active) continue;
            double p = r.SumPopulation();
            if (p <= 0) continue;
            total += p;
            if (!string.IsNullOrEmpty(r.Religion)) rel[r.Religion] = rel.GetValueOrDefault(r.Religion) + p;
            if (!string.IsNullOrEmpty(r.Culture)) cul[r.Culture] = cul.GetValueOrDefault(r.Culture) + p;
        }
        static List<CompositionRow> Rows(Dictionary<string, double> src, double tot)
        {
            var list = new List<CompositionRow>();
            if (tot <= 1e-9) return list;
            foreach (var kv in src.OrderByDescending(kv => kv.Value))
                list.Add(new CompositionRow { Name = kv.Key, Size = Fmt.PopFmtK(kv.Value), Share = (kv.Value / tot).ToString("P0") });   // POPタブはk統一（項10）
            return list;
        }
        SyncRows(ReligionRows, Rows(rel, total), r => r.Name, (t, s) => t.CopyFrom(s));
        SyncRows(CultureRows, Rows(cul, total), r => r.Name, (t, s) => t.CopyFrom(s));
        // 02断線走査2026-07-28：児童労働率の可視化＝倍率排除④の創発量（家計困窮×合法度×就学）。法の2値でなく実態が数年かけて落ちる様子＝工場監督官の物語
        double clSum = 0, clSize = 0;
        foreach (var r in _player.Regions)
            foreach (var p in r.Pops)
                if (!p.IsSoldier && p.AgeShare(10, 15) > 1e-9) { clSum += p.ChildLaborRate.Value * p.Size; clSize += p.Size; }   // 働きうる子供（10〜14歳・年齢の層）のいる POP
        // ★2026-08-02（作者指示）：「児童労働率」→**児童の就業率**（何の率かが名前で分かる）。合法度は撤去＝
        //   法の現在値は法律タブの「児童労働」行（禁止/合法）が持っており、ここに出すと同じことを2箇所で言う。
        ChildLaborLine = clSize > 1e-9 ? $"児童の就業率 {clSum / clSize:P0}" : "";
        OnPropertyChanged(nameof(ChildLaborLine));
    }

    /// <param name="reorder">既存行を desired の並びへ Move するか。false＝**画面上の並びを動かさない**（値だけキーで突き合わせて流す）。
    /// クリックできる行（生産タブの州別＝着工/売却ボタン）は、押下と離上の間に行が動くと Avalonia が Click を捨てるので false で呼ぶ（2026-08-09）。</param>
    internal static void SyncRows<T>(ObservableCollection<T> target, IReadOnlyList<T> desired, Func<T, string> keyOf, Action<T, T> copy, bool reorder = true) where T : class
    {
        if (target.Count == desired.Count)
        {
            var keys = new HashSet<string>();
            bool same = true;
            for (int i = 0; i < target.Count; i++) if (!keys.Add(keyOf(target[i]))) { same = false; break; }   // 既存キーの重複＝作り直し
            if (same) foreach (var d in desired) if (!keys.Contains(keyOf(d))) { same = false; break; }
            if (same)
            {
                if (!reorder)
                {
                    // ⚠Move を飛ばすだけでは copy(target[i], desired[i]) が**別の行の値**を流し込む。必ずキーで突き合わせる。
                    var byKey = new Dictionary<string, T>(desired.Count);
                    foreach (var d in desired) byKey[keyOf(d)] = d;
                    foreach (var t in target) if (byKey.TryGetValue(keyOf(t), out var src)) copy(t, src);
                    return;
                }
                for (int i = 0; i < desired.Count; i++)
                {
                    string k = keyOf(desired[i]);
                    if (keyOf(target[i]) != k)
                        for (int j = i + 1; j < target.Count; j++)
                            if (keyOf(target[j]) == k) { target.Move(j, i); break; }
                    copy(target[i], desired[i]);
                }
                return;
            }
        }
        target.Clear();
        foreach (var d in desired) target.Add(d);
    }

    private static IBrush PolityColor(Polity? p) => p switch
    {
        Polity.Absolutist or Polity.Despotic or Polity.Shogunate => PolityAbsolutist,
        Polity.ConstitutionalMonarchy => BadgeOwnVassal,
        Polity.Republic => PolityRepublic,
        Polity.MilitaryDictatorship => PolityMilitary,
        Polity.BourgeoisDictatorship => BadgeOwnSuzerain,
        Polity.ProletarianDictatorship => PolityProletarian,
        _ => BadgeDefault,
    };

    // 国家ランクの和名（列強/中堅/弱小・改修2026-06-28）。
    private static string RankJa(PowerRank r) => r switch { PowerRank.Great => "列強", PowerRank.Secondary => "準列強", PowerRank.Middle => "中堅", _ => "弱小" };

    // 政体の和名（英語enumをそのまま出さない）。9政体すべてを網羅。★(435) 表は SimCore の 1 か所（Runner の報告と同じ）。
    private static string PolityRegimeJa(Polity? p) => PlayText.PolityRegimeJa(p);

    // 政体で変わる国号（イギリス王国／イギリス共和国／…）と内戦の国名（政体の頭書き）の表は SimCore の1か所（SecessionNames・2026-09-28 処理の一元化 段2 束3）。
}
