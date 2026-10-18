namespace PriceOfBread.SimCore;

/// <summary>★読むだけの標準の計器（2026-10-10 (242) 線I）＝回ごとに写しへ注入していた計器（inj_*.py）の代わりに、本体へ1回だけ置いた出力。
/// 既定は出さない：環境変数 <c>POB_INSTRUMENTS=&lt;出力ディレクトリ&gt;</c> を立てたときだけ、<see cref="On"/> が真になる（偽の間は本体から1命令も走らない＝分岐の中だけ）。
/// 出すもの（1試合＝seed ごとに3本の TSV）：
/// <list type="bullet">
/// <item><c>fiscal_s&lt;seed&gt;.tsv</c> 財政の鎖（<c>POB_INSTRUMENTS_NATIONS</c> に挙げた国の月ごと）：請求の項目別（r を掛ける前）・借入（払えない額・門・1件の額・国内プール・外資プール・どれが縛ったか）・切り下げ・r・
/// 兌換・国債の利率・クーポン・実際の収支（国庫の台帳の区分ごとの月の差分）・国庫の帳尻（Δ国庫−Σ台帳）。</item>
/// <item><c>class_s&lt;seed&gt;.tsv</c> 階級ごとの人数・貯蓄・所得（Earned）・支出（Paid）（挙げた国は四半期ごと・全ての国は年末）。</item>
/// <item><c>stock_s&lt;seed&gt;.tsv</c> 在庫と即応度と装備（財の在庫の価値・首都の軍需品の在庫・即応度・装備の充足・兵士・艦）。年末の行には史実比較の桁の読み手（NG）が読む量
/// （艦隊のゲージ・技術水準・鉄道の容量・工場の蒸気動力の割合・いま交戦中か）も出る。</item>
/// <item><c>nation_s&lt;seed&gt;.tsv</c> 国×四半期（全ての国・死んだ国も）：人口・兵士・名目GDP・交戦と内戦の月の数・徴兵法・総動員・安全保障の不安の割合・州の格の数・台帳の累計（借入・切り下げ・陸・海）・国債・
/// 貿易（清算1回分の輸出と輸入）。史実比較の桁の読み手（NG の読み手の入力 11 種）の入力＝旧の注入（scripts/instruments の inj_*.py）の代わり。</item>
/// <item><c>flow_s&lt;seed&gt;.tsv</c> ★(262) 貯蓄の書き手の台帳（挙げた国だけ・四半期ごと・階級×書き手の種類×呼び出しの位置×相手の側の月平均の額と件数）。階級の貯蓄の増減が書き手の和に閉じる。</item>
/// <item><c>region_s&lt;seed&gt;.tsv</c> 州×四半期（全ての国の全ての州）：人口・農村人口・機会とその因子・耕地・四半期の人口の流れ（州の間の移民・入植・越境の出入り）・戦場の強さ・主食の充足・★(323) 窃盗の列（<c>theftPoolPerMonth</c>＝州のその四半期の盗まれた額の月平均・<c>theftRecipientsEarnedPerMonth</c>＝受け手〔困窮大衆〕の前の月の所得の月平均・<c>earnedPerMonth</c>＝州の全 POP の前の月の所得の和〔四半期の末〕）。
/// <c>migration_s&lt;seed&gt;.tsv</c> 州の間の移民の組（出る州→入る州）の四半期の量。州の空洞化（F1 の線）の読み手 <c>hol/an_h2.py</c> の入力＝旧の注入 <c>u4e/inj_ghost.py</c> の代わり。</item>
/// <item><c>opium_s&lt;seed&gt;.tsv</c> ★(342) アヘンの表（<c>scope</c> 列で2種類の行）：<c>R</c>＝州×四半期（中毒度・販売量・闇栽培の係数のどれかが動いている州だけ）・<c>N</c>＝国×四半期（禁令・輸出入・緊張の積みのどれかが有る国だけ）。読み手 <c>scripts/instruments/op/an_op.py</c>。列は <see cref="OpiumRows"/> の注。</item>
/// <item><c>ce_s&lt;seed&gt;.tsv</c> ★(345) 消費の入口 段A：予算の3層の材料（<c>scope</c> 列で2種類の行）：<c>C</c>＝国×四半期×階級（挙げた国は毎四半期・他の国は年末の7階級）の月の和を月数で割った額＝使える所得（前の月の確定した所得）・月初の貯蓄と消費の予算・食料／日用品／奢侈品／役務その他の籠の費用（欲しい量×買い手の値段）と予算で切った後の需要と実際の支払い・所得を超えた支払い（貯蓄の取り崩し）・余り（所得−食料の籠−日用品の籠）ほか。<c>N</c>＝国×窓（40四半期）×財の必需の割合の影（導いた値を書くだけ＝力学は読まない・読み手は <c>an_ce.py</c> の N 行の表）。読み手 <c>scripts/instruments/ce/an_ce.py</c>。列は <see cref="CeColNames"/> の注。</item>
/// <item><c>pop_s&lt;seed&gt;.tsv</c> ★(361) 人口の作り替え 段A・B（<c>scope</c> 列で2種類の行・実装は <c>InstrumentsPop.cs</c>）：<c>N</c>＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）の粗出生・粗死亡（平常・飢え・疫病ほか・戦死は別）・層別（10歳刻み8本）の死と死の率・10歳までの生存・60歳以後の余命・合計出生率の代理・回復期（平常の死が0の月）の数・子の価値 <c>v</c>（ψ＝0）とその項（稼ぎ・費用・老後の扶養）・結婚の割合 <c>m</c> と制限 <c>c</c> と合計出生率の影・影の出生。<c>C</c>＝国×四半期×階級（挙げた国は全階級・他は年末の7階級）の影の項。籠は <c>ce_s</c> の積算を読む。読み手 <c>scripts/instruments/pop/an_pop.py</c>。新しい数（θ・m_max・合計出生率7.5）は計器の const＝力学の係数ではない。</item>
/// <item><c>fm_s&lt;seed&gt;.tsv</c> ★(371) 流れM の計器 2 本（実装は <c>InstrumentsFm.cs</c>・国×四半期＝挙げた国は毎四半期・他の全ての国は年末）：①戦時の軍事費の純支持（<c>militaryMoveNetSupportIndex</c>）の項への分解（街頭の 3 ブロック・議会・軍）と、戦争の規模・存亡・戦場の強さ・兵士の人口比。②海の金の割合の分母 max(陸の必要量＋内の守り, 自国の軍の大きさ) の組成（陸の必要量を本国どうしの国境・植民地を含む国境・国境でない投射に、内の守りを人口・不穏度・質に分け、どちらが分母を決めているか）。読み手 <c>scripts/instruments/fm/an_fm.py</c>。</item>
/// <item><c>canal_s&lt;seed&gt;.tsv</c> ★(271) 運河ごとに1行（開通した四半期の番号・開通しなかった運河は <c>-1</c>。試合の終わりに書く）。<c>nation_s</c> の末尾の21列（国の個体の番号 <c>nationUid</c>・消費の予算の拘束・建設の投資枠・国債の借入のプール・与信の上限）も (271)。</item>
/// <item>★(274) <c>nation_s</c> の末尾の列（<c>openFrontsCount</c> ほか17列）＝政治予算の係数 (1/N)^0.5 の N に数えた開いた戦線のそれぞれが、その四半期の施行で動いたか・何で固定されたか（幽霊戦線）。符号は <see cref="GFront"/>・読み手は <c>scripts/instruments/g274/an_ghost.py</c>。</item>
/// <item><c>wars_s&lt;seed&gt;.tsv</c> 戦争1本ごと（開戦の年・内戦か・当事者・月数・戦死）／<c>casualty_s&lt;seed&gt;.tsv</c> (年, 国)ごとの兵士の月と戦死（試合の終わりに書く）。</item>
/// </list>
/// 開幕（構築直後 <c>open</c>）と助走の直後（<c>warm</c>）の断面は class と stock に全ての国で出る。初期値の一覧は Runner の <c>initdump</c>。
/// <para>⚠<b>読むだけ</b>＝力学の値を1つも書かない・乱数を引かない・メモ化された量を新しい位置で読まない（<see cref="World.GdpPerMonth"/> は四半期の末と助走の直後にだけ読む）。
/// ⚠(248) 但し書き：<c>nation_s</c> は全ての国の名目GDP を毎四半期読む＝メモ化の経路（<see cref="World.GdpPerMonth"/>）が生産者の <c>_siteRegion</c> を書く。書く値は同じ（冪等）＝結果は変わらないが、読む回数は増える。
/// 戦死の表の鍵は <c>BaseName</c>＝同じ BaseName の国（建国・継承）は1行にまとまる（世界の和を読む読み手には効かない）。
/// 守りの証明＝計器を全て出した10種と出さない10種の帯の報告が全行バイト一致（README §1-3）。番人＝<c>InstrumentsTests</c>。</para>
/// <para>⚠<b>この計器が自分で申告する限界</b>：①請求の項目は「請求の足し算の各項を、足している行の横で写したもの」＝項目の和と請求の合計の差（<c>claimItemGapPerMonth</c>）が0でなければ写し漏れ（請求の式に項が足されたのに
/// 計器が追いついていない）。②月は「前の月の決算の終わりから今の月の決算の終わりまで」の台帳の差分＝日々の支払いと四半期末の共有層の支払いは、それが起きた区間の月に入る。
/// ③単位は列名の接尾辞（<c>PerMonth</c>＝月の流れ・<c>Stock</c>＝在庫・<c>Share</c>＝0〜1 の割合・<c>PerYear</c>＝年率・<c>K</c>＝千人）。④自国通貨の名目。</para></summary>
public static partial class Instruments
{
    /// <summary>計器を出すか。偽の間は本体の分岐の中へ入らない。</summary>
    public static bool On = Environment.GetEnvironmentVariable("POB_INSTRUMENTS") is { Length: > 0 };
    static string Dir = Environment.GetEnvironmentVariable("POB_INSTRUMENTS") ?? "";
    /// <summary>月ごとの財政の行と四半期ごとの階級・在庫の行を出す国（国名＝BaseName をカンマで区切る・<c>*</c>＝全て）。未指定＝なし（全ての国の年末の行だけ）。</summary>
    static string[] Picked = SplitNames(Environment.GetEnvironmentVariable("POB_INSTRUMENTS_NATIONS"));
    static string[] SplitNames(string? s) => s is { Length: > 0 } ? s.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) : [];

    /// <summary>番人（テスト）が計器を入れ替えるための口。出力ディレクトリと国の選び方を渡す（本番は環境変数）。</summary>
    internal static void Configure(string? dir, string? nations)
    {
        Dir = dir ?? ""; Picked = SplitNames(nations); On = Dir.Length > 0;
        if (On) System.IO.Directory.CreateDirectory(Dir);
    }

    internal enum Item
    {
        Interest, Welfare, Pension, ColonialUpkeep, TributeOutQ, RecruitQ, MegaForeignYieldQ, TradePolicy, PortQ,
        Army, Navy, Admin, Infra, ElecInfra, SoldierPay, StateBuild, MonumentBuild, HigherEdu,
        LastEducation, LastWelfare, LastFactoryBuild, LastSubsidy, StateConstruction, StateLoss, Count,
    }
    static readonly string[] ItemNames =
    [
        "claimInterestPerMonth", "claimWelfarePerMonth", "claimPensionPerMonth", "claimColonialUpkeepPerMonth", "claimTributeOutAtQuarterEnd", "claimRecruitAtQuarterEnd",
        "claimMegaForeignYieldAtQuarterEnd", "claimTradePolicyPerMonth", "claimPortAtQuarterEnd",
        "claimArmyPerMonth", "claimNavyPerMonth", "claimAdminPerMonth", "claimInfraPerMonth", "claimElecInfraPerMonth", "claimSoldierPayPerMonth", "claimStateBuildPerMonth", "claimMonumentBuildPerMonth", "claimHigherEduPerMonth",
        "claimLastMonthEducationPerMonth", "claimLastMonthWelfareSubsidyPerMonth", "claimLastMonthFactoryBuildPerMonth", "claimLastMonthSubsidyPerMonth", "claimStateConstructionPerMonth", "claimStateLossPerMonth",
    ];
    const int NC = PopClass.Count;
    const int NCat = (int)TreasuryCat.Count;

    sealed class WState
    {
        public long BurTicks; public int BurCalls;         // (389) 走査の実時間（Stopwatch の刻み）と呼びの数
        public string? BurRow;                             // (389) 官僚制度の判断の表の行（GateAllLawTargets が置き BurFold が書いて消す）
        public List<string>? SecRows;                      // (395) 州の分離の内訳の行（CheckSecession が置き SecFold が書いて消す）
        public string? CoupRow; public int CoupRoute;      // (393) 軍の力の入口の表の行と第 1 の門の後の経路（Politics.Transition が置き CoupFold が書いて消す）
        public bool Active, Armed;                         // 今月の写しが有効か／月初の Begin が走ったか
        public readonly double[] Claim = new double[(int)Item.Count];
        public double Treasury0, Payable0, Spare0, Payable1, Borrowed, Devalued, ClaimsTotal, PayRate;
        public int BorrowCode = -1;                        // -1＝借入の判定に来なかった（払えない額なし）0＝門が閉じた 1＝満額（払えない額を全部借りた）2＝1件の額が縛った 3＝プール（国内＋外資）が縛った 4＝借りられなかった（額が1e-9以下）
        public double Shortfall, Ticket, DomPool, ForPool;
        public bool HaveLedger; public double PrevTreasury; public readonly double[] PrevLedger = new double[NCat];
        public readonly List<string> Rows = new();
        public readonly List<string> SiteRows = new();     // (316) 産地の判定の行（開いた行 O・開く前の軌道 T）＝四半期の末に site_s へ書く
        public readonly List<string> DiscRows = new();     // (320) 発見の表の行の出来事（E）と蒸気機関・機関車の軌道（T）＝四半期の末に disc_s へ書く
        public readonly List<string> DiscFlowRows = new();   // (360) 世界の越境の取引額（接触の分母）：蒸気機関・機関車の行の運び手の財ごと・四半期の末に discflow_s へ（Nations[0] の世界に貯める）
        public readonly Dictionary<(int Row, string Seller, int Good), double> DiscContact = new();   // (328) 伝わりの接触の内訳：買い手のこの国が未解禁の蒸気機関・機関車の運び手の財を、解禁済みの売り手の国から買った額（金建て・四半期の和）＝四半期の末に disccontact_s へ書く
        public readonly double[] ClsEarn = new double[NC], ClsPaid = new double[NC]; public int ClsMonths;
        public double OpTensionAdded, OpTensionLevelMax; public int OpTensionPairMonths;   // ★(342) 禁令中の国が輸出国との組へ積んだアヘンの緊張（四半期の和・件数・積んだ直後の組の値の最大）
        public double LuxPopMonths, LuxNonPosPopMonths, LuxInvalidPopMonths, LuxNonPosSizeSum;   // (329) 奢侈品の所得弾力：f＝1 に倒れた POP×月の累計（読むだけ・四半期の終わりに nation_s へ）
        public PopAcc? PopA;                               // (361) 人口の作り替え 段A・B の蓄え（InstrumentsPop.cs）
        public FmAcc? FmA;                                 // (371) 流れM の計器の蓄え（InstrumentsFm.cs）
        public int Sel = -1;                               // -1＝未判定 0＝挙げていない 1＝挙げた国
        public int WarMonths, CivilWarMonths;              // 今の四半期の交戦の月・内戦の月（軍の更新の直前に数える・四半期の末に0へ戻す・助走の直後にも0へ戻す）
        public int GateOpenNow = -1;                       // 今月の借入の門（-1＝判定に来なかった 0＝閉じていた 1＝開いていた）＝月初の PreBorrow が Borrow と同じ1本（Fiscal.BorrowGateOpen）で読む
        public double RevDirectNet = double.NaN, RevIndirect = double.NaN;   // 今月の純税収（汚職漏れ控除後・Fiscal.Step の revenue）と間接税（関税＋専売の実収・予算の基数へ係数を掛けて算入する前）
        public int QGateObs, QGateClosed, QShortMonths, QRUnder1Months;      // 今の四半期の月数：門を判定した月・門が閉じていた月・払えない額がある月（月初の spare＜0）・率 r＜1 の月（(258) F1 段0）
        public double QInterest, QRevenue, QDirect, QIndirect, QShortfall, QBorrowed;   // 今の四半期の月ごとの量の和（利払い・予算の基数・純税収・間接税・払えない額・借入）
        // ---- (259) 行政の請求の2つの式（Simulation の州の予算の和／Fiscal の adminClaim）・国家の支払い率・行政の充足：今の四半期の月ごとの和（四半期の末に0へ戻す）
        public int AdmMonths, AdmPaidMonths, AdmRBelow1Months;
        public double AdmSimClaim, AdmCatClaim, AdmCatPaid, AdmFiscClaim, AdmBase, AdmSpendable, AdmLastRev, AdmSimMul, AdmCover, AdmFiscMul, AdmPayRate, AdmFill, AdmFillRBelow1;
        // 月の予算の位置（州ごとの AdminBudget の代入）で溜め、月の決算の終わりで四半期の和へ畳む：基数 SpendableRevenue・先月の歳入・官僚の居る州の（人口の比×距離の割増）の和と人口の比の和
        public double BudSpendable = double.NaN, BudLastRev = double.NaN, BudMul, BudCover;
        // ---- (259) 戦時段（四半期の頭の AI の判断と Government.Step の後・NaN／-1＝この四半期は来なかった）
        public double DecScale = double.NaN, DecLosing = double.NaN, DecShare = double.NaN;
        public int DecAtWar = -1, DecMobilized = -1, DecPicked, DecOpen = -1, DecTarget = -1, DecEnforced = -1;
        public int FacStarted, FacStartedClosed, FacDone, FacDoneClosed, FacDoneClosedAtStart;   // 今の四半期の国営の棟の着工・完成（門が閉じていた分つき・四半期の末に0へ戻す）
        // ---- (262) 貯蓄の書き手の台帳（挙げた国だけ・四半期の和・四半期の末に0へ戻す）：(階級, 書き手の種類, 呼び出しの位置) → [額の和, 件数]
        public readonly Dictionary<(int Cls, int Kind, string File, int Line, char Cp), double[]> SavLedger = new();
        // ---- (263) 輪と動詞の個体の食い違い（投げ売りの輪・補助の輪が掴む生産者 pr と、動詞〔CanSellFactory・SetStateTarget・SetSubsidyRate〕が World.FindFactory で掴む個体）：今の四半期の和（四半期の末に0へ戻す）
        public int FsRun, FsSeenMismatch, FsFired, FsFiredMismatch, FsFiredOver, SubMismatch, SubMismatchOff;
        public readonly List<string> WheelGoods = new(), FacStartedGoods = new();   // 食い違いの財の名前（「投げ売り:」「補助:」を前置）・着工した財の名前
        // ---- (265) 線I：国債金利の内訳（Fiscal.Step の月ごとの値の四半期の和・月数で割って平均にする）
        public int BondMonths;
        public double BondBase, BondDebt, BondFisher, BondDistrust, BondPolicy, BondGold, BondRepr, BondClamp, BondRate, BondDebtRatio, BondConvMul, BondReprMul;
        // ---- (265) 行政の必要額：州ごとの請求（r を掛ける前）の写し〔月の決算で空にする〕・今月の和〔MonthClose で四半期へ畳む〕・四半期の和
        public readonly Dictionary<Region, double> AdminClaims = new();
        public double BudClaim, BudNeedB, BudMinB, BudNeedP, BudMinP; public int BudOverRegions;
        public double QAdmClaim, QAdmNeedB, QAdmMinB, QAdmNeedP, QAdmMinP, QCfBorrowed; public int QAdmOverRegionMonths, QCfShortMonths, QCfRBelow1Months, QCfObs;
        // ---- (265) 段5 の上限（大衆の Paid が d ごとの上限 min(S, E＋S×d) を超えるか）：月初の貯蓄 S と前月の所得 E の写し（州→POP ごとの [S, E]）・四半期の和
        public readonly Dictionary<Region, double[]> CapSnap = new();
        public double QCapPaid, QCapPopK;
        public readonly double[] QCapBoundPaid = new double[CapDs.Length], QCapCut = new double[CapDs.Length], QCapBoundPopK = new double[CapDs.Length];
        // ---- (265) 専売の段・国営工場の売却・財政の逼迫（AI の判断の四半期の頭）
        public int FactorySold, MonoBefore = -1, MonoTarget = -1; public double FiscalStressAtDecision = double.NaN;
        public double PushBurden = double.NaN, PushP = double.NaN, PushU = double.NaN; public int PushHit;   // (337) 専売の段の確率の押し上げ：決断の時点の重さ・確率・抽選の目・当たり
        // ---- (271) 線K：国の個体・消費の予算の拘束・建設の投資枠・国債のプール・与信の上限（読むだけ・四半期の和・四半期の末に0へ戻す）
        public int Uid;                                    // 国の個体の番号（0＝未採番）＝World の個体に付く＝改名でも変わらず、建国は新しい番号（四半期の行を書く順に採番＝決定論）
        public long CnRows, CnBound; public double CnDesired, CnDemand;   // 消費の (POP×財) の行：行数・予算で切られた行数（需要＜希望）・希望額と立った需要額（金額建て・月の和）
        public int ConDesiring, ConBound; public double ConDesiredUnits, ConCutUnits;   // 建設：希望が正の（州×月）・うち投資枠で切られた数・希望の束・切られた束
        public int BrJudged, BrPoolBound, BrForeignCut; public double BrDomPool, BrForRaw, BrFor;   // 借入の判定の月：判定した月・プールが縛った月・うち外資のプールが他国の投資家の貯蓄で切られていた月・各プールの和
        public int CrDays, CrRepayBoundDays, CrFiniteDays; public double CrReserveCap, CrRepayable;   // 与信の上限（日数で重みづけ）：判定の日数・返せる額が準備の側より小さかった日数・有限の日数・各上限の日数つきの和
        // ---- (270) 線B2：軍事費の合計の施行の一歩（Government.Step の milTotal＝Move の1回）の内訳。-1／NaN＝この四半期は来なかった
        public int MmOutcome = -1, MmFronts = -1; public bool MmIncreasing;
        public double MmCur = double.NaN, MmTarget = double.NaN, MmNet = double.NaN, MmResist = double.NaN, MmSpeed = double.NaN, MmPush = double.NaN;
        public double MmOppTrad = double.NaN, MmOppModern = double.NaN, MmOppMass = double.NaN, MmStock = double.NaN, MmStockMax = double.NaN, MmEnvelopeAfterGov = double.NaN;
        // ---- (274) 線G：幽霊戦線の計器。Government.CountOpenFronts が数えた開いた戦線（数えた順）と、Step がその四半期に出した結果。-1＝施行の結果が来なかった
        public readonly List<GFront> Gf = new(); public bool GfCounted, GfMilExpired; public int GfLevelIdx = -1, GfGateWhy; public double GfGateNet = double.NaN;
        // ---- (278) 線G2：政治資本の在庫の台帳。段の法のループの頭／Step の末尾の目減り S と最大量 b、その四半期に払った額（段の法・税と支出の段 R・軍事費 M・関税 T・治安維持力 C）
        public double GfSHead = double.NaN, GfMaxHead = double.NaN, GfSEnd = double.NaN, GfPendCost = double.NaN, GfPendS = double.NaN, GfGateCost = double.NaN, GfGateS = double.NaN;
        public double GfPaidL, GfPaidR, GfPaidM, GfPaidT, GfPaidC;
        // ---- (280) 線X：物価の更新の鎖（助走を含む四半期ごと）・財ごとの価格・月ごとの貨幣と所得（挙げた国だけ・書き出しは Phase warm／Quarter で行う）
        public int PxWarmN, PxGameN, PxMonthWarmN, PxMonthGameN;                     // 助走／試合の何回目か（四半期の更新・月の決算の数）
        public readonly List<string> PxRows = new(), PxGoodsRows = new(), PxMonthRows = new(); public string? PxHdr, PxMonthHdr;
        public double PxResetSince; public int PxResetCount;   // 国庫を助走の前の値へ戻した額の和（戻した値 − 戻す前の国庫＝正は基礎貨幣の創出・負は消滅）と回数。四半期の更新の行で読んで0へ戻す
        public readonly double[] PxEarn = new double[NC], PxPaid = new double[NC]; public int PxMonths;   // 今の四半期の月ごとの所得と支出の和（四半期の更新で読んで0へ戻す）
        // ---- (281) 線O0：借入の門の周り（読むだけ・月ごとの印と四半期の和）。LimitPrev＝前の月の「利払いが限界に届いたか」は四半期をまたぐ（助走の月は -1 へ戻す）
        public double GateInt = double.NaN, GateRev = double.NaN;   // PreBorrow で門が読んだ直近の利払いと歳入（月の決算の終わりの値とは別＝Fiscal.Step が月末に書き換える前）
        public int LimitNow = -1, Over067Now = -1, LimitPrev = -1;  // 今月の門の入力で 利払い÷歳入が限界に届いたか／仏1788 の線 0.67 を超えたか（-1＝判定に来なかった 0＝いいえ 1＝はい）・前の月の LimitNow。⚠(294) から既定の限界も 0.67＝限界を動かさない国では Over067Now（超える）は LimitNow（届く＝以上）とちょうど等しい月だけ違う＝別の物差しとしてはほぼ働かない（列名は変えない）
        public bool WarNow;                                         // 今月交戦中か（WarMonth が軍の更新の直前に立てる・Begin で下ろす）
        public int QLimitMonths, QLimitCrossings, QLimitBorrowMonths, QZombieMonths, QWarGateClosedMonths, QOver067Months, QOver067BorrowedMonths, QOver067RBelow1Months;
        public int QUnpaidObs, QUnpaidMonths, QMegaPass, QMegaOpen;
        public double QUnpaidSum, QUnpaidMax, QDevalueShareSum, QDevalueShareMax;
        // ---- (286) 線Q：富裕層のお金の行き先（階級×支払いの種類）と建設の門
        public double[]? QSpend;                           // 今の四半期の月ごとの支払いの和：[(階級 × SpendW + 種類) × 3 + {支払い額, 数量, 生産者の受取}]（EconomyStep の支払いの位置が積む）
        // ---- (345) 消費の入口 段A（読むだけ）：階級×欄の今の四半期の月の和（欄は CeColNames）と、財ごとの必需の割合の影の和（州×月の和・四半期の末に ce_s へ畳んで 0 へ戻す）
        public double[]? CeC, CeNum, CeDen, CeBasket, CeBought; public EntryCtx? Ent;
        public int ConRegionMonths;                        // 建設の判定に来た州×月
        public double ConPlannedMoney, ConBudgetBoundMoney, ConBuiltUnits, ConPlannedUnits; public readonly double[] ConRationW = new double[5];   // 計画した（枠で切った後の）金額／枠が切った州×月の枠の金額／建った束／計画した束／計画した束で重みづけた配給率（鋼・機械・セメント・製材・労働）の和
        public double ConGainSum, ConHurdleSum, ConBudgetMoney, ConDesiredMoney;   // 限界収益・資本コストの州×月の和／投資枠の金額／希望の金額（希望の束×束の費用×(1+工賃)）
        public readonly double[] QStPaid = new double[2];  // (289) 線D：国庫が実際に払った建設の額（材料＋工賃）の月の和：[0]＝国の建設〔Modernization の stateConstruction の決済〕[1]＝巨大建造物〔Megaproject の決済〕
    }
    /// <summary>(274) 開いた戦線1本。Kind＝L 段の法〔Idx＝LevelLaws の表の行〕／R 税・支出の段〔Idx＝RateLaws の行〕／M 軍事費の合計／C 治安維持力／T 関税。
    /// Code：0 施行のとき目標＝現在で閉じていた／1 純支持で固定／2 政治資本で固定／3 無償で動いた〔未到達〕／4 政治資本を払って動いた〔未到達〕／5 不可逆で固定／6 前提で固定／
    /// 7 無償で動いて目標に届いた／8 払って動いて目標に届いた／9 軍事費の解禁の外の失効だけで動いた（M のみ）／-1 結果が来なかった。</summary>
    internal sealed class GFront { public char Kind; public int Idx; public PolicyAxis Axis; public double Cur, Target, Net = double.NaN; public int Code = -1; public bool Expired;
        /// <summary>(278) 線G2：この一歩の代価（門が計算した額。無償で通った＝0・門の手前で止まった＝NaN）と、払う前（決める時）の目減り S。<see cref="Structural"/>＝符号 2 のうち代価が政治資本の最大量そのものを超える（その四半期の b では在庫が空〔満タン〕でも届かない・b と純支持は四半期ごとに動く）。</summary>
        public double Cost = double.NaN, SDec = double.NaN; public bool Structural; }
    /// <summary>段5 の上限の引き出し率 d（月あたり）。(264) 実装ログ §7 の事前登録の格子＝1/120・1/240・1/480（AI の設計・作者の承認は未確認）。</summary>
    internal static readonly double[] CapDs = [1.0 / 120, 1.0 / 240, 1.0 / 480, 1.0 / 36, 1.0 / 12];   // (271) 末尾の2つ＝(268) 申し送りの推奨（格子に 1/36・1/12 を足す）
    static readonly string[] CapDNames = ["D120", "D240", "D480", "D36", "D12"];
    /// <summary>「大衆」＝農民・工場労働者・職人（階級の表を読む読み手 f1s0／flow262 と同じ定義）。兵士は除く。</summary>
    static bool IsMass(Pop p) => !p.IsSoldier && (p.ClassId == PopClass.Peasant || p.ClassId == PopClass.Worker || p.ClassId == PopClass.Artisan);
    /// <summary>段5 の上限（月の消費の予算の上限）＝min(max(0,S), max(0,E)＋max(0,S)×d)。</summary>
    internal static double CapOf(double savings, double lastEarned, double d)
    {
        double s = Math.Max(0, savings), e = Math.Max(0, lastEarned);
        return Math.Min(s, e + s * d);
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, WState> States = new();
    static WState S(World w) => States.GetValue(w, _ => new WState());

    sealed class Sink
    {
        public System.IO.StreamWriter? Fiscal, Class, Stock, Nation, Region, Migration, Flow, Goods, Money, PxGoods, PxMonth;
        public bool FiscalHeader, ClassHeader, StockHeader, NationHeader, RegionHeader, FlowHeader, GoodsHeader, MoneyHeader, PxGoodsHeader, PxMonthHeader;
        public ulong Seed;   // 試合の終わり（Close）に戦争の表を書くときのファイル名
        public int NextUid;  // (271) 国の個体の番号の次
        public readonly Dictionary<string, (int Quarter, int Year)> CanalOpened = new();   // (271) 運河の開通の四半期（最初に開通を見た四半期の末）
        public readonly Dictionary<(int Uid, int Bucket), SpendAcc> Spend = new();         // (286) 国の個体×窓の区切りごとの支払いの積算
        public System.IO.StreamWriter? Site, Metal; public bool SiteHeader, MetalHeader;   // (316) 産地の判定の行・世界の金銀の行
        public System.IO.StreamWriter? Disc; public bool DiscHeader;                       // (320) 発見の表の行の出来事と蒸気機関・機関車の軌道
        public System.IO.StreamWriter? DiscFlow; public bool DiscFlowHeader;               // (360) 接触の分母（運び手の財ごとの世界の越境の取引額）
        public System.IO.StreamWriter? DiscContact; public bool DiscContactHeader;         // (328) 伝わりの接触の内訳（買い手×行×売り手）
        public System.IO.StreamWriter? Svc; public bool SvcHeader;                         // (286) 家事奉仕の市場（挙げた国×四半期）
        public System.IO.StreamWriter? Cons; public bool ConsHeader;                       // (289) 資本家の建設の州ごとの表（挙げた国×州×四半期）
        public System.IO.StreamWriter? Port; public bool PortHeader;                       // (292) 港の建設の州ごとの表（挙げた国×活きた港の州×四半期）
        public System.IO.StreamWriter? Opium; public bool OpiumHeader;                     // (342) アヘンの表（州×四半期の行 R と国×四半期の行 N）
        public System.IO.StreamWriter? Ce; public bool CeHeader;                           // (345) 消費の入口の表（国×四半期×階級の行 C と国×窓×財の行 N）
        public System.IO.StreamWriter? PopS; public bool PopSHeader;                       // (361) 人口の表（国×四半期の行 N と国×四半期×階級の行 C）
        public System.IO.StreamWriter? Fm; public bool FmHeader;                           // (371) 流れM の計器（国×四半期の行）
        public System.IO.StreamWriter? Sca; public bool ScaHeader;                         // (373) 政治のスカラーの表（国×四半期）
        public System.IO.StreamWriter? TrBin, TrRoute, TrNat, TrStat; public bool TrHeader;   // (380) 輸送の段0 の計器（年×距離の帯の行・経路の行・国の行・州と値段の行）
        public System.IO.StreamWriter? Bur; public bool BurHeader;                         // (389) AI の官僚制度の判断の表（国×四半期）
        public System.IO.StreamWriter? Coup; public bool CoupHeader;                       // (393) 軍の力の入口の表（国×四半期・第 1 の門を通った行だけ）
        public System.IO.StreamWriter? Anc; public bool AncHeader;                         // (369) 扶養力の錨の表（国×四半期）
        public System.IO.StreamWriter? Cap; public bool CapHeader; public System.IO.StreamWriter? Ide; public bool IdeHeader;   // (390) 工場の能力の内訳の表・思想の割合の表（国×四半期）
        public System.IO.StreamWriter? Sec; public bool SecHeader;                         // (395) 州の分離の内訳の表（国×四半期×州）
        public readonly Dictionary<(int Uid, int Win), CeAcc> CeN = new();                 // (345) 国の個体×窓（40四半期）ごとの必需の割合の影の積算
    }
    /// <summary>(286) 線Q：窓の区切りごとの積算。区切り（四半期の番号 q）＝[0,4) [4,8) [8,40) [40,160) [160,∞)。窓 Q1〜Q8＝区切り0+1・Q5〜Q40＝1+2・Q41〜Q160＝3。</summary>
    internal sealed class SpendAcc
    {
        public string Nation = ""; public bool Picked; public int Months;
        public readonly double[] Spend = new double[NC * SpendW * SpendStride];
        public readonly double[] Earn = new double[NC], Paid = new double[NC], SizeMonths = new double[NC];   // 階級の所得・支出（月の和）・人数の月数の和（千人×月）
        public readonly string?[] Names = new string?[NC];
    }
    /// <summary>(289) 線D：区切りに 188（1830年）・228（1840年）・268（1850年）・400 を足した（区切り0〜3 は (286) のまま＝窓 Q1〜Q8・Q5〜Q40・Q41〜Q160 の読み手はそのまま読める。区切り4 は「Q161〜」から「Q161〜Q188」になった）。</summary>
    internal static readonly int[] SpendBucketStart = [0, 4, 8, 40, 160, 188, 228, 268, 400];
    static int SpendBucketOf(int q) { int b = 0; for (int i = 1; i < SpendBucketStart.Length; i++) if (q >= SpendBucketStart[i]) b = i; return b; }
    /// <summary>支払いの種類の幅＝財の数＋下の「財でない支払い」。</summary>
    internal static readonly int SpendW = Catalog.Goods.Length + 8;
    /// <summary>(289) 1つの (階級, 種類) の積算の幅：{買い手の実支払い, 数量, 生産者の受取, 国庫の取り分〔専売の上乗せ＋国庫へ行く独占レント〕}。</summary>
    const int SpendStride = 4;
    /// <summary>財でない支払い（POP の <c>PaidPerMonth</c> に入る物）の種類。財の番号の後ろに並べる。</summary>
    internal enum SpendExtra { Construction = 0, ConstructionRail = 1, InfraMaintenance = 2, Grid = 3, ReignitionOrder = 4, RebelArms = 5 }
    static readonly string[] SpendExtraNames = ["建設の材料と工賃", "建設に付く線路と機関車", "インフラ保守", "送電網", "止まった工場の再開の発注", "反乱勢力の武器"];
    /// <summary>外交の記録（戦争の始まりと戦死の月ごとの和）。外交は MultiNationSim に1つ＝世界ごとの状態（<see cref="WState"/>）とは別に持つ。</summary>
    sealed class DipState
    {
        public readonly List<(int Year, War War)> Wars = new();
        public readonly Dictionary<(int Year, string Nation), double[]> Cas = new();   // [0]＝月数 [1]＝兵士の月和（千人×月）[2]＝戦死（千人）
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, DipState> Dips = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, Sink> Sinks = new();
    static Sink SinkOf(MultiNationSim sim, ulong seed) => Sinks.GetValue(sim, _ => new Sink { Seed = seed });

    static string NameOf(Power p) => p.BaseName.Length > 0 ? p.BaseName : p.Name;
    public static string NameOf(World w) => w.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "(単国)";
    static bool IsPicked(World w, WState st)
    {
        if (st.Sel < 0) { var n = NameOf(w); st.Sel = Array.IndexOf(Picked, "*") >= 0 || Array.IndexOf(Picked, n) >= 0 ? 1 : 0; }
        return st.Sel == 1;
    }
    static string F(double v) => double.IsNaN(v) ? "nan" : double.IsInfinity(v) ? (v > 0 ? "inf" : "-inf") : v.ToString("G10", System.Globalization.CultureInfo.InvariantCulture);

    // ---------------------------------------------------------------- 月初（Simulation.MonthSettle の請求の足し算の位置）の写し ----------------------------------------------------------------

    /// <summary>請求の足し算の直前＝今月の写しを始める。助走（時間が経たない）の月は写さない。</summary>
    internal static void Begin(World w)
    {
        var st = S(w);
        if (IsPicked(w, st)) RegisterPops(w, st);   // (262) 貯蓄の書き手の台帳：挙げた国の POP を台帳の持ち主へ登録（月初・新しい POP は次の月から）
        st.Active = !w.WarmupFrozen; st.Armed = true;
        if (!st.Active) { st.HaveLedger = false; st.LimitPrev = -1; return; }   // (281) 助走の月は限界の縁の往復を数えない
        Array.Clear(st.Claim);
        st.Treasury0 = w.Treasury.Value; st.Payable0 = st.Spare0 = st.Payable1 = st.Borrowed = st.Devalued = st.ClaimsTotal = st.PayRate = double.NaN;
        st.BorrowCode = -1; st.Shortfall = st.Ticket = st.DomPool = st.ForPool = double.NaN;
        st.GateOpenNow = -1; st.RevDirectNet = st.RevIndirect = double.NaN;
        st.LimitNow = st.Over067Now = -1; st.GateInt = st.GateRev = double.NaN; st.WarNow = false;   // (281)
    }
    static WState? Live(World w) { var st = S(w); return st.Active && st.Armed ? st : null; }

    /// <summary>請求の最初の4項（直近の利払い・社会保障・年金・植民地の維持費）。</summary>
    internal static void ClaimsBase(World w)
    {
        if (Live(w) is not { } st) return;
        st.Claim[(int)Item.Interest] = w.LastInterestPerMonth.Value; st.Claim[(int)Item.Welfare] = w.LastWelfareCostPerMonth.Value; st.Claim[(int)Item.Pension] = w.LastPensionCostPerMonth.Value; st.Claim[(int)Item.ColonialUpkeep] = w.LastColonialUpkeepPerMonth.Value;
    }
    /// <summary>四半期の締め月の請求（貢納・招聘の支度金）。</summary>
    internal static void ClaimsQuarter(World w)
    {
        if (Live(w) is not { } st) return;
        st.Claim[(int)Item.TributeOutQ] = w.LastTributeOutPerQuarter; st.Claim[(int)Item.RecruitQ] = w.RecruitBudget;
    }
    /// <summary>請求の1項（足している行の横で、足した額をそのまま写す）。</summary>
    internal static void Claim(World w, Item it, double amount)
    {
        if (Live(w) is not { } st) return;
        st.Claim[(int)it] += amount;
    }
    /// <summary>州ごとの請求の足し算（予算・工事の列・先月の区分つきの支出を先月の r で割り戻した額）。</summary>
    internal static void ClaimRegion(World w, Region r, double regrow, bool payQuarter, double portThr)
    {
        if (Live(w) is not { } st) return;
        var c = st.Claim;
        if (payQuarter) c[(int)Item.PortQ] += w.PortStateRequest(r, portThr);
        c[(int)Item.Army] += r.MilitaryBudget; c[(int)Item.Navy] += r.NavalBudget; c[(int)Item.Admin] += r.AdminBudget; c[(int)Item.Infra] += r.InfraBudget; c[(int)Item.ElecInfra] += r.ElecInfraBudget;
        c[(int)Item.SoldierPay] += r.SoldierPayBudget; c[(int)Item.StateBuild] += r.StateBuildQueue; c[(int)Item.MonumentBuild] += r.MonumentBuildQueue; c[(int)Item.HigherEdu] += r.HigherEduFunding;
        c[(int)Item.LastEducation] += regrow * Math.Max(0, r.StateSpent[(int)TreasuryCat.Education]);
        c[(int)Item.LastWelfare] += regrow * Math.Max(0, r.StateSpent[(int)TreasuryCat.Welfare]);
        c[(int)Item.LastFactoryBuild] += regrow * Math.Max(0, r.StateSpent[(int)TreasuryCat.FactoryBuild]);
        c[(int)Item.LastSubsidy] += regrow * Math.Max(0, r.StateSpent[(int)TreasuryCat.Subsidy]);
        // (265) 行政の請求（r を掛ける前）の写し＝この州の必要額（予算の時点の価格）で切った額と並べる
        st.AdminClaims[r] = r.AdminBudget; st.BudClaim += r.AdminBudget;
        if (AdminNeedOf(w, r) is { } nb) { st.BudNeedB += nb; st.BudMinB += Math.Min(r.AdminBudget, nb); if (r.AdminBudget > nb) st.BudOverRegions++; }
    }

    /// <summary>★(265) 州の行政の必要額＝<c>EconomyStep.Run</c> の行政調達の見積り <c>est</c> と同じ式（<c>AdminNeedPerPop</c>×人口×max(行政の価格, Eps)×距離の割増）。官僚の居ない州は購買が無い＝null。
    /// 掛ける順も EconomyStep のとおり（((係数×人口)×価格)×割増）。<paramref name="w"/> が購買の見積りと同じ価格を読む時点（予算＝月初）と、購買の時点（<see cref="AdminNeedPurchase"/>）の2か所で呼ぶ。</summary>
    internal static double? AdminNeedOf(World w, Region r)
    {
        if (r.BureaucratPopIndex < 0 || Catalog.AdminId >= r.Markets.Length) return null;
        double totalPop = 0; foreach (var p in r.Pops) totalPop += p.Size;
        return w.Coef.AdminNeedPerPop * totalPop * Math.Max(r.Markets[Catalog.AdminId].Price, 1e-9) * Fiscal.AdminDistanceMul(w, r);
    }
    /// <summary>行政の購買の見積り <paramref name="est"/>（<c>EconomyStep.Run</c>・購買の時点の値）と、月初に写した請求の小さい方。予算の時点の必要額（<see cref="AdminNeedOf"/>）と食い違えば価格が月の途中で動いている。</summary>
    internal static void AdminNeedPurchase(World w, Region r, double est)
    {
        if (Live(w) is not { } st) return;
        st.BudNeedP += est; st.BudMinP += st.AdminClaims.TryGetValue(r, out var claim) ? Math.Min(claim, est) : 0;
    }
    /// <summary>月の決算の終わり（国債金利・行政の請求の月の和を四半期へ畳む前）：行政の必要額で切る反事実 A′（静的）を月ごとに数える。
    /// 余り X＝Σ州 max(0, 請求−必要額)（予算の時点）。払える額の余剰 spare′＝spare＋X・借入 b′＝min(b, max(0, −spare′))（門が開いている月は min(券面, プール, 不足) の不足だけが動く）・
    /// 払える額 payable′＝payable−(b−b′)・請求 claims′＝claims−X・率 r′＝clamp(payable′÷claims′)。切り下げの額は動かさない（上限の見積り）。</summary>
    static void AccumulateAdminNeed(WState st)
    {
        st.QAdmClaim += st.BudClaim; st.QAdmNeedB += st.BudNeedB; st.QAdmMinB += st.BudMinB; st.QAdmNeedP += st.BudNeedP; st.QAdmMinP += st.BudMinP; st.QAdmOverRegionMonths += st.BudOverRegions;
        if (double.IsNaN(st.Spare0)) return;
        st.QCfObs++;
        var cf = AdminCf(st.Spare0, st.BudClaim - st.BudMinB, double.IsNaN(st.Borrowed) ? 0 : st.Borrowed, st.ClaimsTotal, st.Payable1);
        if (cf.Short) st.QCfShortMonths++;
        st.QCfBorrowed += cf.Borrowed;
        if (cf.RBelow1) st.QCfRBelow1Months++;
    }
    /// <summary>A′ の静的な反事実の1か月分（純関数・番人が手で確かめる）。<paramref name="x"/>＝請求から減る額。払えない＝spare′＜0・借入 b′＝min(b, max(0, −spare′))・率 r′＝clamp((payable−(b−b′))÷(claims−x))＜1。</summary>
    internal static (bool Short, double Borrowed, bool RBelow1) AdminCf(double spare0, double x, double borrowed, double claimsTotal, double payable1)
    {
        double sp = spare0 + x, b2 = Math.Min(borrowed, Math.Max(0, -sp));
        bool rBelow1 = false;
        if (!double.IsNaN(claimsTotal) && !double.IsNaN(payable1))
        {
            double c2 = claimsTotal - x, p2 = payable1 - (borrowed - b2), r2 = c2 > 0 ? p2 / c2 : 1.0;
            if (!(r2 >= 0)) r2 = 0; else if (r2 > 1) r2 = 1;
            rBelow1 = r2 < 1;
        }
        return (sp < 0, b2, rBelow1);
    }

    /// <summary>★(265) 国債金利の内訳（<c>Fiscal.Step</c> の金利の式・月ごと）。項は <c>w.Explain</c> の "BondRate" の各項と同じ式（Explain は立てない＝全域の Explain 経路を動かさない）。
    /// 閉じ方：基礎＋債務比＋期待インフレ＋信認喪失＋政策金利（各々 兌換割引済み）＋議会の信認割引＋上限クランプ＝金利。金本位の信認割引は**割引ずみの5項の上に重ねた説明の項**（足すと二重＝Explain の並びのまま記録するが閉じには入れない）。</summary>
    internal static void BondRate(World w, double baseTerm, double debtTerm, double fisherTerm, double distrustTerm, double policyTerm, double goldMemo, double reprTerm, double clampTerm, double debtRatio, double convMul, double reprMul)
    {
        if (w.WarmupFrozen) return;
        var st = S(w);
        st.BondMonths++; st.BondBase += baseTerm; st.BondDebt += debtTerm; st.BondFisher += fisherTerm; st.BondDistrust += distrustTerm; st.BondPolicy += policyTerm;
        st.BondGold += goldMemo; st.BondRepr += reprTerm; st.BondClamp += clampTerm; st.BondRate += w.GovBondRatePerYear.Value; st.BondDebtRatio += debtRatio; st.BondConvMul += convMul; st.BondReprMul += reprMul;
    }

    /// <summary>★(265) 段5 の上限の写し（<c>EconomyStep.Run</c> の直前）：大衆の月初の貯蓄 S。上限の前月の所得 E は <c>Pop.LastEarnedPerMonth</c>（EconomyStep が S の直前に写す物）を月の決算の終わりに読む。挙げた国だけ。</summary>
    internal static void PreEconomy(World w, Region r)
    {
        if (Live(w) is not { } st || !IsPicked(w, st)) return;
        if (!st.CapSnap.TryGetValue(r, out var buf) || buf.Length != r.Pops.Count) st.CapSnap[r] = buf = new double[r.Pops.Count];
        for (int i = 0; i < buf.Length; i++) buf[i] = r.Pops[i].Savings.Value;
    }
    /// <summary>月の決算の終わり：大衆の Paid が d ごとの上限 <see cref="CapOf"/> を超えるか。拘束＝Paid＞上限。削られる額＝Paid−上限。</summary>
    static void AccumulateCap(WState st)
    {
        foreach (var kv in st.CapSnap)
        {
            var r = kv.Key; var buf = kv.Value;
            if (buf.Length != r.Pops.Count) continue;
            for (int i = 0; i < buf.Length; i++)
            {
                var p = r.Pops[i];
                if (!IsMass(p)) continue;
                double paid = p.PaidPerMonth.Value, e = p.LastEarnedPerMonth.Value;
                st.QCapPaid += paid; st.QCapPopK += p.Size;
                for (int k = 0; k < CapDs.Length; k++)
                {
                    double cap = CapOf(buf[i], e, CapDs[k]);
                    if (paid > cap) { st.QCapBoundPaid[k] += paid; st.QCapCut[k] += paid - cap; st.QCapBoundPopK[k] += p.Size; }
                }
            }
        }
        st.CapSnap.Clear();
    }

    /// <summary>国営工場の売却が成立した（<c>World.SellFactory</c> の成功の直前）。AI の投げ売りも人の売却も同じ口。</summary>
    internal static void FactorySold(World w) => S(w).FactorySold++;
    /// <summary>AI の財政規律の判断（<c>SelfPlay.Apply</c> の逼迫の位置）：逼迫の指標。</summary>
    internal static void FiscalStress(World w, double stress) => S(w).FiscalStressAtDecision = stress;
    /// <summary>専売の段の書き込みの直前：今の段と AI が置く段。</summary>
    internal static void MonopolyDecision(World w, int before, int target) { var st = S(w); st.MonoBefore = before; st.MonoTarget = target; }
    /// <summary>(337) 専売の段の確率の押し上げ（<c>SelfPlay.Apply</c>）：決断の時点の利払いの重さ・確率 p・抽選の目 u（抽選しなかったら NaN）・当たり。</summary>
    internal static void MonopolyPush(World w, double burden, double p, double u, bool hit) { var st = S(w); st.PushBurden = burden; st.PushP = p; st.PushU = u; st.PushHit = hit ? 1 : 0; }

    /// <summary>借りる前（月初の払える額と余剰が決まった所）。</summary>
    internal static void PreBorrow(World w, double payable, double spare)
    {
        if (Live(w) is not { } st) return;
        st.Payable0 = payable; st.Spare0 = spare;
        st.GateOpenNow = Fiscal.BorrowGateOpen(w, w.Coef) ? 1 : 0;   // 次の行の Fiscal.Borrow が読むのと同じ門（純粋な関数＝読むだけ）
        // (281) 門が読むのと同じ2つの量（直近の利払いと歳入）で、利払い÷歳入が限界 MaxInterestShareOfRevenue に届いたか（門の式の否定＝割り算にしない）・仏1788 の線を超えたか。歳入が正の月だけ判定
        double li = w.LastInterestPerMonth.Value, lr = w.LastRevenuePerMonth.Value;
        st.GateInt = li; st.GateRev = lr;
        st.LimitNow = lr > 0 && !(w.LastInterestPerMonth < w.Coef.MaxInterestShareOfRevenue * w.LastRevenuePerMonth) ? 1 : 0;
        st.Over067Now = lr > 0 && li > SuspensionInterestShare * lr ? 1 : 0;
    }
    /// <summary>歳入の内訳（Fiscal.Step が予算の基数を書いた所）：純税収と間接税の実収。予算の基数＝純税収＋間接税。</summary>
    internal static void Revenue(World w, double directNet, double indirect)
    {
        if (Live(w) is not { } st) return;
        st.RevDirectNet = directNet; st.RevIndirect = indirect;
    }
    /// <summary>借入の門が閉じていた（払えない額がある月）。冷却中・助走・利払いの限界のどれかは列に出す。</summary>
    internal static void Gate(World w, double shortfall)
    {
        if (Live(w) is not { } st) return;
        st.BorrowCode = 0; st.Shortfall = shortfall;
    }
    /// <summary>借入の判定（門が開いていた）：何が借入の額を縛ったか。</summary>
    internal static void Borrow(World w, double shortfall, double ticket, double domesticPool, double foreignPool, double borrowed, double foreignPoolRaw)
    {
        if (Live(w) is not { } st) return;
        st.Shortfall = shortfall; st.Ticket = ticket; st.DomPool = domesticPool; st.ForPool = foreignPool;
        double want = Math.Min(ticket, shortfall);
        st.BorrowCode = !(borrowed > 1e-9) ? 4 : borrowed < want - 1e-9 ? 3 : ticket < shortfall ? 2 : 1;
        // (271) 国内と外資のプールのどちらが縛ったか：判定の月の数・プールが縛った月（コード3か4）・うち外資のプールが他国の投資家の貯蓄の上限で切られていた月・各プールの和
        st.BrJudged++; st.BrDomPool += domesticPool; st.BrFor += foreignPool; st.BrForRaw += foreignPoolRaw;
        if (st.BorrowCode >= 3) { st.BrPoolBound++; if (foreignPool < foreignPoolRaw) st.BrForeignCut++; }
    }
    /// <summary>★(271) 消費の予算の拘束（州×月）：<c>EconomyStep.Run</c> の消費ループで数えた (POP×財) の行数・予算で切られた行数（需要＜希望）・希望額と立った需要額（金額建て）。
    /// <c>BudgetBindDiag</c> と同じ行・同じ述語を標準の計器へ畳んだ（あちらは On の間は各国の日次を逐次へ倒す診断＝バッチでは使えない）。</summary>
    internal static void ConsumeMonth(World w, long rows, long bound, double desiredVal, double demandVal)
    {
        var st = S(w);
        st.CnRows += rows; st.CnBound += bound; st.CnDesired += desiredVal; st.CnDemand += demandVal;
    }
    /// <summary>★(271) 建設の側で投資枠（投資家の貯蓄×<c>IndInvestmentFrac</c>）が希望を切ったか（州×月）：希望の束 <paramref name="desiredUnits"/> と枠で買える束 <paramref name="budgetUnits"/>（<c>EconomyStep.Run</c> の
    /// <c>construction = min(希望, 枠)</c> の2つの引数）。希望が正の州×月だけ数える。</summary>
    internal static void Construction(World w, double desiredUnits, double budgetUnits, double gain = 0, double hurdle = 0, double unitMoney = 0, double planned = 0)
    {
        var st = S(w);
        // (286) 線Q：判定に来た州×月の数・限界収益と資本コストの和・投資枠と希望の金額（希望が0の月も数える＝「採算の門が閉じている月」を分母に残す）
        st.ConRegionMonths++; st.ConGainSum += gain; st.ConHurdleSum += hurdle;
        st.ConBudgetMoney += budgetUnits * unitMoney; if (desiredUnits > 0) st.ConDesiredMoney += desiredUnits * unitMoney;
        st.ConPlannedMoney += planned * unitMoney; if (desiredUnits > 0 && budgetUnits < desiredUnits) st.ConBudgetBoundMoney += budgetUnits * unitMoney;
        if (!(desiredUnits > 0)) return;
        st.ConDesiring++; st.ConDesiredUnits += desiredUnits;
        if (budgetUnits < desiredUnits) { st.ConBound++; st.ConCutUnits += desiredUnits - budgetUnits; }
    }
    // ---------------------------------------------------------------- (286) 線Q：富裕層のお金の行き先 ----------------------------------------------------------------

    /// <summary>(286) 線Q：今の四半期の支払いの積算の配列（EconomyStep が1回の Run で受け取って使い回す＝支払い1行ごとに辞書を引かない）。</summary>
    /// <summary>(286) 建設の決済：計画した束 <paramref name="planned"/>（投資枠と採算の門を通った後）に対し、実際に建った束 <paramref name="built"/> と、5つの投入（鋼・機械・セメント・製材・労働）の配給率。計画の束で重みづけて積む。</summary>
    internal static void ConstructionFill(World w, double planned, double built, double rSteel, double rMachine, double rCement, double rLumber, double rLabor)
    {
        var st = S(w);
        st.ConPlannedUnits += planned; st.ConBuiltUnits += built;
        st.ConRationW[0] += planned * rSteel; st.ConRationW[1] += planned * rMachine; st.ConRationW[2] += planned * rCement; st.ConRationW[3] += planned * rLumber; st.ConRationW[4] += planned * rLabor;
    }
    internal static double[] SpendBuf(World w) => S(w).QSpend ??= new double[NC * SpendW * SpendStride];
    /// <summary>支払い1行を積む：階級 <paramref name="cls"/>・種類 <paramref name="slot"/>（財の番号、または <see cref="ExtraSlot"/>）・買い手の実支払い <paramref name="pay"/>・数量・生産者の受取（税と流通マージンを除く）。読むだけ。</summary>
    internal static void AddSpend(double[] buf, int cls, int slot, double pay, double qty, double receipt, double stateTake = 0)
    {
        if ((uint)slot >= (uint)SpendW) return;
        int c = (uint)cls < (uint)NC ? cls : NC - 1;
        int k = (c * SpendW + slot) * SpendStride;
        buf[k] += pay; buf[k + 1] += qty; buf[k + 2] += receipt; buf[k + 3] += stateTake;
    }
    /// <summary>(289) 線D：国庫が実際に払った建設の額（材料＋工賃）。kind 0＝国の建設・1＝巨大建造物。月の和で積み、四半期の行（<c>nation_s</c> の末尾）で月数で割る。</summary>
    internal static void StatePaid(World w, int kind, double amount) => S(w).QStPaid[kind] += amount;
    internal static int ExtraSlot(SpendExtra e) => Catalog.Goods.Length + (int)e;

    /// <summary>四半期の末：今の四半期の支払いの積算を窓の区切りの積算へ足す（<see cref="Quarter"/> が国ごとに呼ぶ・<c>st.Uid</c> が採番された後）。</summary>
    static void SpendFold(Sink sk, World w, WState st, int q, bool picked)
    {
        if (st.ClsMonths <= 0 || st.Uid == 0) { if (st.QSpend is not null) Array.Clear(st.QSpend); return; }
        int b = SpendBucketOf(q);
        if (!sk.Spend.TryGetValue((st.Uid, b), out var a)) sk.Spend[(st.Uid, b)] = a = new SpendAcc();
        a.Nation = NameOf(w); a.Picked |= picked; a.Months += st.ClsMonths;
        var size = new double[NC];
        foreach (var r in w.Regions) foreach (var p in r.Pops) { int c = (uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1; size[c] += p.Size; a.Names[c] ??= p.ClassName; }
        for (int c = 0; c < NC; c++) { a.Earn[c] += st.ClsEarn[c]; a.Paid[c] += st.ClsPaid[c]; a.SizeMonths[c] += size[c] * st.ClsMonths; }
        if (st.QSpend is { } qs) { for (int i = 0; i < qs.Length; i++) a.Spend[i] += qs[i]; Array.Clear(qs); }
    }

    /// <summary>試合の終わり：窓の区切りごとの支払いの表 <c>spend_s</c>。挙げた国＝全ての財と全ての階級／他の国＝階級 {貴族・農民・工場労働者・資本家・商人・使用人}×（嗜好品と役務の財ごと＋他の層の合算）。
    /// 階級の行（kind=class）は全ての階級：payPerMonth＝階級の支出（Paid）・receiptPerMonth＝所得（Earned）・qtyPerMonth＝人数（千人）の平均。
    /// 計器の限界：①窓の区切りの月の数は <c>monthsCount</c>（四半期×3）②人数は四半期の末の値×月数の平均（月の途中の増減を見ない）③kind=good／extra の payPerMonth の階級ごとの和が kind=class の payPerMonth と一致しなければ写し漏れ。</summary>
    static void SpendTable(Sink sk)
    {
        if (sk.Spend.Count == 0) return;
        using var wr = Open("spend", sk.Seed);
        wr.WriteLine("bucket\tfirstQuarter\tlastQuarter\tnationUid\tnation\tpickedFlag\tclassId\tclassName\tkind\tslotId\tslotName\ttier\tmonthsCount\tpayPerMonth\tqtyPerMonth\treceiptPerMonth\tstateTakePerMonth");
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int[] otherClasses = [PopClass.Noble, PopClass.Peasant, PopClass.Worker, PopClass.Capitalist, 10, 12];
        int ng = Catalog.Goods.Length;
        foreach (var kv in sk.Spend.OrderBy(k => k.Key.Uid).ThenBy(k => k.Key.Bucket))
        {
            var a = kv.Value; int b = kv.Key.Bucket;
            string head = string.Join('\t', I(b), I(SpendBucketStart[b]), b + 1 < SpendBucketStart.Length ? I(SpendBucketStart[b + 1] - 1) : "-1", I(kv.Key.Uid), a.Nation, a.Picked ? "1" : "0");
            double mo = a.Months > 0 ? a.Months : double.NaN;
            for (int c = 0; c < NC; c++)
            {
                if (a.Names[c] is null) continue;
                string ch = head + "\t" + I(c) + "\t" + a.Names[c];
                wr.WriteLine(string.Join('\t', ch, "class", "-1", "階級", "", I(a.Months), F(a.Paid[c] / mo), F(a.SizeMonths[c] / mo), F(a.Earn[c] / mo), F(double.NaN)));
                bool full = a.Picked || Array.IndexOf(otherClasses, c) >= 0;
                if (!full) continue;
                var tierSum = new double[6 * SpendStride];
                for (int g = 0; g < ng; g++)
                {
                    int k = (c * SpendW + g) * SpendStride;
                    double pay = a.Spend[k], qty = a.Spend[k + 1], rec = a.Spend[k + 2], take = a.Spend[k + 3];
                    if (pay == 0 && qty == 0 && rec == 0 && take == 0) continue;
                    var gd = Catalog.Goods[g]; int t = (int)gd.Tier;
                    if (a.Picked || gd.Tier == GoodTier.Luxury || gd.Tier == GoodTier.Service)
                        wr.WriteLine(string.Join('\t', ch, "good", I(g), gd.Name, gd.Tier.ToString(), I(a.Months), F(pay / mo), F(qty / mo), F(rec / mo), F(take / mo)));
                    else { tierSum[t * SpendStride] += pay; tierSum[t * SpendStride + 1] += qty; tierSum[t * SpendStride + 2] += rec; tierSum[t * SpendStride + 3] += take; }
                }
                if (!a.Picked)
                    for (int t = 0; t < 6; t++)
                        if (tierSum[t * SpendStride] != 0 || tierSum[t * SpendStride + 1] != 0 || tierSum[t * SpendStride + 2] != 0 || tierSum[t * SpendStride + 3] != 0)
                            wr.WriteLine(string.Join('\t', ch, "tier", I(-2 - t), "階層の合算", ((GoodTier)t).ToString(), I(a.Months), F(tierSum[t * SpendStride] / mo), F(tierSum[t * SpendStride + 1] / mo), F(tierSum[t * SpendStride + 2] / mo), F(tierSum[t * SpendStride + 3] / mo)));
                for (int e = 0; e < SpendExtraNames.Length; e++)
                {
                    int k = (c * SpendW + ng + e) * SpendStride;
                    if (a.Spend[k] == 0 && a.Spend[k + 1] == 0 && a.Spend[k + 2] == 0 && a.Spend[k + 3] == 0) continue;
                    wr.WriteLine(string.Join('\t', ch, "extra", I(ng + e), SpendExtraNames[e], "", I(a.Months), F(a.Spend[k] / mo), F(a.Spend[k + 1] / mo), F(a.Spend[k + 2] / mo), F(a.Spend[k + 3] / mo)));
                }
            }
        }
    }

    // ---------------------------------------------------------------- (345) 消費の入口 段A：予算の3層の材料（読むだけ） ----------------------------------------------------------------

    // 階級ごとの欄（州と月をまたいで足した月の和。四半期の行で月数で割る）。欄の番号と CeColNames の並びは一致させる。
    const int CeSize = 0, CeEarnLast = 1, CeEarnLastPos = 2, CeSavStart = 3, CeBudStart = 4,
        CeWant = 5 /* 5..8＝食料・日用品・奢侈品・役務その他 */, CeDem = 9 /* 9..11＝食料・日用品・奢侈品 */, CeSWant = 12 /* 12..14＝食料・日用品・奢侈品 */, CePay = 15 /* 15..18＝食料・日用品・奢侈品・役務その他 */,
        CeConsPaid = 19, CeUnfilled = 20, CeRemainder = 21, CeRemainderBasket = 22, CeBasketGap = 23, CeFoodGap = 24, CeRestStock = 25, CeShadowBud = 26, CeShadowCut = 27,
        CeDraw = 28, CeFoodDraw = 29, CeBasketDraw = 30, CeCoveredPopK = 31, CeFoodUnaffordPopK = 32, CeNegDemand = 33, CeNegBudgetPopK = 34, CeK = 35;
    /// <summary>★(345) <c>ce_s</c> の C 行の列（階級ごと・月の平均）。<c>earnedLastPerMonth</c>＝Σ <see cref="Pop.LastEarnedPerMonth"/>（EconomyStep が月の頭に前の月の確定値へ写した欄＝消費の予算の層の <c>Y</c>・資本家は負になりうる）／
    /// <c>earnedLastPositivePerMonth</c>＝Σ max(0, Y)（以下の派生量はこちらを使う）／<c>savingsStartStock</c>＝月初の貯蓄（負の債務を含む）／<c>budgetStartStock</c>＝Σ max(0, 消費の財のループの直前の残予算)（投資家の控除・再点火の発注・反乱の武器を引いた後＝設計 doc §1-2 の <c>S</c>）／
    /// <c>…WantPerMonth</c>＝Σ 欲しい量×買い手の値段（予算で切る前・供給で切る前＝奢侈品と家事奉仕は所得の弾力・主食はパニック買いを掛けた後）／<c>…DemandPerMonth</c>＝予算で切った後（min(欲しい量, 予算÷値段)）×買い手の値段／
    /// <c>…SuppliedWantPerMonth</c>＝Σ 欲しい量×その財の配給率×買い手の値段（〈籠の費用〉の実用の定義＝市場に無い財〔配給率が低い〕の欲しい量を数えない。主食の弾力 0 は値段が上がっても欲しい量が減らない＝供給の無い主食の欲しい量×品薄の値段が WantPerMonth を何倍にも膨らます）／
    /// <c>…PaidPerMonth</c>＝実際の支払い（配給の後・補助を引いた額）／<c>consumptionPaidPerMonth</c>＝Σ(消費の財のループの前後の <see cref="Pop.PaidPerMonth"/> の差)＝4つの層の支払いの和と一致しなければ写し漏れ（<c>class_s</c> の <c>paidPerMonth</c> は建設・保守なども含む全 Paid＝分母が違う）／
    /// <c>foodUnfilledHeldPerMonth</c>＝食料が在庫切れで買えず予算から外した額／<c>remainderPerMonth</c>＝Σ max(0, Y−食料の支払い−日用品の支払い)（設計 doc §1-2 の余り <c>X</c> の影＝層1・2 の支払いは今の実際の支払いで代用）／
    /// <c>remainderByBasketPerMonth</c>＝Σ max(0, Y−食料の供給後の籠−日用品の供給後の籠)（所得−籠）／<c>basketGapPerMonth</c>＝Σ max(0, 供給後の籠の和−Y)／<c>foodGapPerMonth</c>＝Σ max(0, 供給後の食料の籠−Y)／
    /// <c>restStockStock</c>＝Σ max(0, 予算−Y)（設計 doc の <c>S′</c>）／<c>shadowDailyBudgetPerMonth</c>＝Σ max(0, min(Y,S)−min(食料の支払い,S))（層2 の予算の影＝設計 doc §1-2 の式）／<c>shadowDailyCutPerMonth</c>＝Σ max(0, 供給後の日用品の籠−その予算)（層2 で削られる額の影）／
    /// <c>savingsDrawPerMonth</c>＝Σ max(0, 消費の支払い−Y)（所得を超えて貯蓄から出した額）／<c>foodSavingsDrawPerMonth</c>＝Σ max(0, 食料の支払い−Y)／<c>basketSavingsDrawPerMonth</c>＝Σ max(0, 食料と日用品の支払い−Y)／
    /// <c>basketCoveredPopK</c>＝供給後の食料の籠＋日用品の籠 ≦ Y の POP の人口／<c>foodUnaffordablePopK</c>＝供給後の食料の籠 ＞ 予算 の POP の人口／
    /// <c>negativeDemandPerMonth</c>＝需要が負（残予算が負＝在庫切れの食料を予算から外して貯蓄を超えた POP の、後の財）の行の需要×買い手の値段の和（≦0）／<c>budgetEndNegativePopK</c>＝消費の財のループの後に残予算が負の POP の人口。
    /// ⚠限界：①Y の定義は〈前の月の Earned〉（移転を含むかは Earned に入る物に従う）②各 POP の欲しい量の和は、財の間の予算の食い合いを見ない③PerMonth は州と月の和を四半期の月数で割った国の月平均④役務の優先配分の財は供給後の籠を持たない。</summary>
    internal static readonly string[] CeColNames =
    [
        "popK", "earnedLastPerMonth", "earnedLastPositivePerMonth", "savingsStartStock", "budgetStartStock",
        "foodWantPerMonth", "dailyWantPerMonth", "luxuryWantPerMonth", "otherWantPerMonth",
        "foodDemandPerMonth", "dailyDemandPerMonth", "luxuryDemandPerMonth",
        "foodSuppliedWantPerMonth", "dailySuppliedWantPerMonth", "luxurySuppliedWantPerMonth",
        "foodPaidPerMonth", "dailyPaidPerMonth", "luxuryPaidPerMonth", "otherPaidPerMonth",
        "consumptionPaidPerMonth", "foodUnfilledHeldPerMonth", "remainderPerMonth", "remainderByBasketPerMonth", "basketGapPerMonth", "foodGapPerMonth", "restStockStock",
        "shadowDailyBudgetPerMonth", "shadowDailyCutPerMonth", "savingsDrawPerMonth", "foodSavingsDrawPerMonth", "basketSavingsDrawPerMonth",
        "basketCoveredPopK", "foodUnaffordablePopK", "negativeDemandPerMonth", "budgetEndNegativePopK",
    ];
    /// <summary>非挙げ国が年末の行に出す階級（貴族・農民・工場労働者・資本家・職人・商人・家事使用人）。</summary>
    static readonly int[] CeOtherClasses = [PopClass.Noble, PopClass.Peasant, PopClass.Worker, PopClass.Capitalist, PopClass.Artisan, 10, 12];
    /// <summary>国の個体×窓（40四半期）ごとの積算。N 行（必需の割合の影）の元。</summary>
    internal sealed class CeAcc
    {
        public string Nation = ""; public int Months;
        public readonly double[] Num, Den, Basket, Bought;
        public CeAcc(int ng) { Num = new double[ng]; Den = new double[ng]; Basket = new double[ng]; Bought = new double[ng]; }
    }
    const int CeWindowQuarters = 40;
    /// <summary>階級ごとの欄の数（番人が <c>EntryCtx</c> の配列を作るため）。</summary>
    internal static int CeWidth => CeK;

    /// <summary>★(345) 州×月の消費の入口の作業欄（世界ごとに1つを使い回す）。<c>EconomyStep.Run</c> の消費の財のループの口から呼ぶ（<c>Instruments.On</c> の中だけ・読むだけ＝力学の値を1つも書かない）。
    /// 助走の月は作らない（<see cref="EntryBegin"/> が null を返す）。必要の無い財は呼ばない（<c>needless</c> の外）。</summary>
    internal sealed class EntryCtx
    {
        readonly double[] cC, cNum, cDen, cBasket, cBought; Region reg = null!; int np;
        int[] cls = [];
        double[] size = [], y = [], sav0 = [], bud0 = [], paid0 = [], cur = [], want = [], pay = [];
        double sizeTot; int gi, grp; double d0Sum, num, cMax, b, bought;
        internal EntryCtx(double[] c, double[] num, double[] den, double[] basket, double[] bought) { cC = c; cNum = num; cDen = den; cBasket = basket; cBought = bought; }

        internal void Begin(Region r)
        {
            reg = r; np = r.Pops.Count;
            if (cls.Length < np) { cls = new int[np]; size = new double[np]; y = new double[np]; sav0 = new double[np]; bud0 = new double[np]; paid0 = new double[np]; cur = new double[np]; }
            if (want.Length < 3 * np) { want = new double[3 * np]; pay = new double[3 * np]; }
            Array.Clear(want, 0, 3 * np); Array.Clear(pay, 0, 3 * np);
            sizeTot = 0;
            for (int i = 0; i < np; i++)
            {
                var p = r.Pops[i];
                cls[i] = (uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1; size[i] = p.Size; sizeTot += p.Size; y[i] = p.LastEarnedPerMonth.Value;
            }
        }
        /// <summary>消費の財のループの直前（予算の上限・投資家の控除・発注と武器の控除の後）：残予算・貯蓄・その時点の支払いを撮る。</summary>
        internal void Start()
        {
            for (int i = 0; i < np; i++) { var p = reg.Pops[i]; sav0[i] = p.Savings.Value; bud0[i] = p.RemainingBudget; paid0[i] = p.PaidPerMonth.Value; }
        }
        /// <summary>財 <paramref name="g"/>（層 <paramref name="group"/>＝0 食料・1 日用品・2 奢侈品・3 役務その他）の需要のループの前。</summary>
        internal void GoodBegin(int g, int group) { gi = g; grp = group; d0Sum = 0; num = 0; cMax = 0; b = 0; bought = 0; Array.Clear(cur, 0, np); }
        /// <summary>需要のループの1行（必要量が正の POP）：<paramref name="d0"/>＝欲しい量（価格の弾力まで・所得の弾力の前）・<paramref name="desired"/>＝予算で切る前の欲しい量・<paramref name="demand"/>＝切った後。</summary>
        internal void Want(int i, double d0, double desired, double demand, double price)
        {
            d0Sum += d0;
            var C = cC; int k = cls[i] * CeK;
            double wv = desired * price;
            C[k + CeWant + grp] += wv; cur[i] = desired;
            if (demand < 0) C[k + CeNegDemand] += demand * price;
            if (grp < 3) C[k + CeDem + grp] += demand * price;
        }
        /// <summary>支払いのループの前：籠の1人あたり <c>b</c>＝Σ欲しい量÷州の人口（必需の割合の影の分母の上限）。</summary>
        internal void PayBegin() { b = sizeTot > 0 ? d0Sum / sizeTot : 0; }
        /// <summary>支払いのループの1行（<paramref name="i"/>＝POP の添字・<paramref name="ration"/>＝この財の配給率・<paramref name="price"/>＝買い手の値段）。</summary>
        internal void Pay(int i, double qty, double payv, double ration, double price)
        {
            int k = cls[i] * CeK;
            cC[k + CePay + grp] += payv;
            if (grp < 3) { double sw = cur[i] * ration * price; cC[k + CeSWant + grp] += sw; want[grp * np + i] += sw; pay[grp * np + i] += payv; }
            double s = size[i], cap = b * s, qp = qty > 0 ? qty : 0;   // 負の需要（残予算が負の POP）は必需の割合の影に数えない（negativeDemandPerMonth に別に出す）
            num += qp < cap ? qp : cap;
            if (s > 0) { double cc = qp / s; if (cc > cMax) cMax = cc; }
            bought += qty;
        }
        /// <summary>役務の優先配分のループの1行（添字を持たない＝階級だけに積む・必需の割合は出さない）。</summary>
        internal void PayService(int classId, double qty, double payv)
        {
            int c = (uint)classId < (uint)NC ? classId : NC - 1;
            cC[c * CeK + CePay + grp] += payv; bought += qty;
        }
        /// <summary>食料が在庫切れで買えず、予算から外した額。</summary>
        internal void Unfilled(int i, double v) => cC[cls[i] * CeK + CeUnfilled] += v;
        /// <summary>財のループの終わり。<paramref name="nValid"/>＝比例配給の財（役務の優先配分の財は必需の割合を出さない）。
        /// 必需の割合の影 <c>n</c>＝Σ_p min(買った量_p, 籠の1人あたり×人口_p) ÷ (Σ人口 × min(最大の1人あたり購入, 籠の1人あたり))（設計 doc §4-1・min(c_p, min(c_max, b)) = min(c_p, b) で1回の走査で出る）。誰も買えない州は 0。</summary>
        internal void GoodEnd(bool nValid)
        {
            if (!(d0Sum > 0) || gi >= cNum.Length) return;
            cBasket[gi] += d0Sum; cBought[gi] += bought;
            if (nValid && sizeTot > 0)
            {
                double nn = b > 0 && cMax > 0 ? num / (sizeTot * (cMax < b ? cMax : b)) : 0;
                cNum[gi] += sizeTot * nn; cDen[gi] += sizeTot;
            }
        }
        /// <summary>消費の財のループの後：POP ごとの非線形な派生量（max の中が POP ごと＝階級の和から導けない）を階級へ畳む。</summary>
        internal void Finish()
        {
            var C = cC;
            for (int i = 0; i < np; i++)
            {
                var p = reg.Pops[i]; int k = cls[i] * CeK; double s = size[i];
                double Y = y[i], Yp = Y > 0 ? Y : 0, S0 = bud0[i] > 0 ? bud0[i] : 0;
                double F = want[i], D = want[np + i], fp = pay[i], dp = pay[np + i];   // F・D＝供給後の籠（食料・日用品）・fp・dp＝実際の支払い
                double cons = p.PaidPerMonth.Value - paid0[i];
                double X = Yp - fp - dp; if (!(X > 0)) X = 0;
                double Xb = Yp - F - D; if (!(Xb > 0)) Xb = 0;
                double gap = F + D - Yp; if (!(gap > 0)) gap = 0;
                double fgap = F - Yp; if (!(fgap > 0)) fgap = 0;
                double rest = S0 - Yp; if (!(rest > 0)) rest = 0;
                double l2 = (Yp < S0 ? Yp : S0) - (fp < S0 ? fp : S0); if (!(l2 > 0)) l2 = 0;
                double l2cut = D - l2; if (!(l2cut > 0)) l2cut = 0;
                double draw = cons - Yp; if (!(draw > 0)) draw = 0;
                double fdraw = fp - Yp; if (!(fdraw > 0)) fdraw = 0;
                double bdraw = fp + dp - Yp; if (!(bdraw > 0)) bdraw = 0;
                C[k + CeSize] += s; C[k + CeEarnLast] += Y; C[k + CeEarnLastPos] += Yp; C[k + CeSavStart] += sav0[i]; C[k + CeBudStart] += S0; C[k + CeConsPaid] += cons;
                C[k + CeRemainder] += X; C[k + CeRemainderBasket] += Xb; C[k + CeBasketGap] += gap; C[k + CeFoodGap] += fgap; C[k + CeRestStock] += rest; C[k + CeShadowBud] += l2; C[k + CeShadowCut] += l2cut;
                C[k + CeDraw] += draw; C[k + CeFoodDraw] += fdraw; C[k + CeBasketDraw] += bdraw;
                if (p.RemainingBudget < 0) C[k + CeNegBudgetPopK] += s;
                if (F + D <= Yp) C[k + CeCoveredPopK] += s;
                if (F > S0) C[k + CeFoodUnaffordPopK] += s;
            }
        }
    }
    /// <summary>★(345) 州×月の入口。助走の月は null（何も積まない）。</summary>
    internal static EntryCtx? EntryBegin(World w, Region r)
    {
        if (w.WarmupFrozen) return null;
        var st = S(w);
        int ng = Catalog.Goods.Length;
        st.CeC ??= new double[NC * CeK];
        st.CeNum ??= new double[ng]; st.CeDen ??= new double[ng]; st.CeBasket ??= new double[ng]; st.CeBought ??= new double[ng];
        var e = st.Ent ??= new EntryCtx(st.CeC, st.CeNum, st.CeDen, st.CeBasket, st.CeBought);
        e.Begin(r);
        return e;
    }
    static void CeClear(WState st)
    {
        if (st.CeC is not null) Array.Clear(st.CeC);
        if (st.CeNum is not null) { Array.Clear(st.CeNum); Array.Clear(st.CeDen!); Array.Clear(st.CeBasket!); Array.Clear(st.CeBought!); }
    }

    /// <summary>四半期の末：C 行（挙げた国は毎四半期・他の国は年末の7階級）を書き、N 行の元（必需の割合の影）を窓へ足して、今の四半期の積算を戻す。<c>st.Uid</c> が採番された後（<see cref="Quarter"/> の <see cref="SpendFold"/> の後）で呼ぶ。</summary>
    static void CeFold(Sink sk, ulong seed, World w, WState st, int q, bool picked, bool yearEnd)
    {
        var c = st.CeC; if (c is null) return;
        if (st.ClsMonths > 0 && st.Uid != 0)
        {
            double mo = st.ClsMonths; string nation = NameOf(w);
            string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (picked || yearEnd)
            {
                var names = new string?[NC];
                foreach (var r in w.Regions) foreach (var p in r.Pops) { int cc = (uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1; names[cc] ??= p.ClassName; }
                sk.Ce ??= Open("ce", seed);
                if (!sk.CeHeader) { sk.Ce.WriteLine(CeHeaderLine()); sk.CeHeader = true; }
                string tail = new string('\t', 3);   // N 行の3列（necessityShadowShare・basketQtyPerMonth・boughtQtyPerMonth）は C 行では空
                for (int cl = 0; cl < NC; cl++)
                {
                    if (!picked && Array.IndexOf(CeOtherClasses, cl) < 0) continue;
                    int k = cl * CeK;
                    if (!(c[k + CeSize] > 0)) continue;
                    var sb = new System.Text.StringBuilder();
                    sb.Append("C\t").Append(I(q)).Append('\t').Append(I(q)).Append('\t').Append(I(w.Year)).Append('\t').Append(I(st.Uid)).Append('\t').Append(nation).Append('\t').Append(I(cl)).Append('\t').Append(names[cl] ?? "").Append("\t\t\t\t").Append(I(st.ClsMonths));
                    for (int f = 0; f < CeK; f++)
                    {
                        // 欄 f の値：人口と 〈人口の月数〉 は欄 CeSize（人口×月の和）。Stock 系も月の平均＝月の和÷月数
                        sb.Append('\t').Append(F(c[k + f] / mo));
                    }
                    sb.Append(tail);
                    sk.Ce.WriteLine(sb.ToString());
                }
            }
            int win = q / CeWindowQuarters;
            if (!sk.CeN.TryGetValue((st.Uid, win), out var a)) sk.CeN[(st.Uid, win)] = a = new CeAcc(Catalog.Goods.Length);
            a.Nation = nation; a.Months += st.ClsMonths;
            if (st.CeNum is { } nm)
                for (int g = 0; g < nm.Length; g++) { a.Num[g] += nm[g]; a.Den[g] += st.CeDen![g]; a.Basket[g] += st.CeBasket![g]; a.Bought[g] += st.CeBought![g]; }
        }
        CeClear(st);
    }
    static string CeHeaderLine() => "scope\tquarter\tlastQuarter\tyear\tnationUid\tnation\tclassId\tclassName\tgoodId\tgoodName\ttier\tmonthsCount\t" + string.Join('\t', CeColNames) + "\tnecessityShadowShare\tbasketQtyPerMonth\tboughtQtyPerMonth";

    /// <summary>試合の終わり：N 行（国×窓×財の必需の割合の影）。<c>necessityShadowShare</c>＝人口で重みづけた州×月の平均（比例配給の財だけ・役務の優先配分の財は nan）・<c>basketQtyPerMonth</c>＝欲しい量（価格の弾力まで）の月の和・
    /// <c>boughtQtyPerMonth</c>＝買った量の月の和・<c>popK</c> 欄（C 行の popK の位置）＝その財の籠が立つ州の人口の月平均。窓は 40 四半期（<c>quarter</c>～<c>lastQuarter</c>）。</summary>
    static void CeTable(Sink sk)
    {
        if (sk.CeN.Count == 0) return;
        sk.Ce ??= Open("ce", sk.Seed);
        if (!sk.CeHeader) { sk.Ce.WriteLine(CeHeaderLine()); sk.CeHeader = true; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string mid = new string('\t', CeK - 1);   // popK 以外の C 列は空
        foreach (var kv in sk.CeN.OrderBy(k => k.Key.Uid).ThenBy(k => k.Key.Win))
        {
            var a = kv.Value; int win = kv.Key.Win; double mo = a.Months > 0 ? a.Months : double.NaN;
            for (int g = 0; g < a.Basket.Length; g++)
            {
                if (!(a.Basket[g] > 0)) continue;
                var gd = Catalog.Goods[g];
                sk.Ce.WriteLine(string.Join('\t', "N", I(win * CeWindowQuarters), I(win * CeWindowQuarters + CeWindowQuarters - 1), "", I(kv.Key.Uid), a.Nation, "", "", I(g), gd.Name, gd.Tier.ToString(), I(a.Months),
                    F(a.Den[g] / mo) + mid, F(a.Den[g] > 0 ? a.Num[g] / a.Den[g] : double.NaN), F(a.Basket[g] / mo), F(a.Bought[g] / mo)));
            }
        }
    }

    /// <summary>(286) 線Q：家事奉仕の市場（挙げた国×四半期の末の月の値）：使用人の数・産出・売れた量・不足・価格と参照価格・富裕層の人数。</summary>
    static void SvcRow(Sink sk, int q, World w)
    {
        int id = Catalog.DomesticServiceId;
        double capacity = 0, desired = 0;   // ★(306) 能力（Size×fraction×Productivity×boost）と、能力×採算（DesiredOutput）＝四半期の末の月の値
        double servantK = 0, noble = 0, cap = 0, merch = 0, totalK = 0, output = 0, sold = 0, shortage = 0, demand = 0, priceW = 0, produced = 0, rationW = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active || id >= r.Markets.Length) continue;
            foreach (var p in r.Pops)
            {
                totalK += p.Size;
                if (p.ClassId == 12) servantK += p.Size; else if (p.ClassId == PopClass.Noble) noble += p.Size; else if (p.ClassId == PopClass.Capitalist) cap += p.Size; else if (p.ClassId == 10) merch += p.Size;
            }
            foreach (var s in r.Producers) if (s.GoodId == id) { output += s.Output; capacity += s.Capacity; desired += s.DesiredOutput; }
            var m = r.Markets[id];
            produced += m.LastProduced; sold += m.LastSold; shortage += m.LastShortage; demand += m.LastDemand; priceW += m.Price * Math.Max(m.LastSold, 1e-9); rationW += Math.Max(m.LastSold, 1e-9);
        }
        sk.Svc ??= Open("svc", sk.Seed);
        if (!sk.SvcHeader) { sk.Svc.WriteLine("quarter\tyear\tnation\ttotalPopulationK\tservantK\tnobleK\tcapitalistK\tmerchantK\tservantOutputPerMonth\tproducedPerMonth\tsoldPerMonth\tdemandPerMonth\tshortagePerMonth\tpriceSoldWeighted\trefPrice\tservicePerServant\tservantCapacityPerMonth\tservantDesiredPerMonth"); sk.SvcHeader = true; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Svc.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), F(totalK), F(servantK), F(noble), F(cap), F(merch), F(output), F(produced), F(sold), F(demand), F(shortage), F(priceW / rationW), F(id < Catalog.RefPrices.Length ? Catalog.RefPrices[id] : double.NaN), F(servantK > 0 ? sold / servantK : double.NaN), F(capacity), F(desired)));
    }

    /// <summary>★(271) 与信の上限のどちらが縛ったか（<c>Credit.Step</c>・旬の初日の呼び＝<paramref name="days"/> 日ぶん）：準備の側の上限 <paramref name="reserveCap"/>（準備×信用乗数×金のカバー率の抑制）と、
    /// 借り手が返せる額 <paramref name="repayable"/>（無限＝投資家のいない小シナリオ）。助走の月は数えない。</summary>
    internal static void CreditCap(World w, double reserveCap, double repayable, int days)
    {
        if (w.WarmupFrozen) return;
        var st = S(w);
        st.CrDays += days; st.CrReserveCap += reserveCap * days;
        if (!double.IsInfinity(repayable)) { st.CrFiniteDays += days; st.CrRepayable += repayable * days; }
        if (repayable < reserveCap) st.CrRepayBoundDays += days;
    }
    internal static void PostBorrow(World w, double borrowed)
    {
        if (Live(w) is not { } st) return;
        st.Borrowed = borrowed;
    }
    /// <summary>率 r が決まった所（請求の合計・払える額〔借入と切り下げの後〕・r・この月の切り下げ）。</summary>
    internal static void Rate(World w, double claims, double payable, double payRate, double devalued)
    {
        if (Live(w) is not { } st) return;
        st.ClaimsTotal = claims; st.Payable1 = payable; st.PayRate = payRate; st.Devalued = devalued;
    }

    // ---------------------------------------------------------------- (259) 行政の請求の2つの式・戦時段（0g の必要性の確認の計器・読むだけ） ----------------------------------------------------------------

    /// <summary>月初の予算（<c>Simulation</c> の州ごとの <c>AdminBudget</c> の代入の直後）：行政の予算の基数と、官僚の居る州の（人口の比・人口の比×距離の割増）の和。
    /// 基数 <see cref="World.SpendableRevenue"/> は先月の歳入＋潤沢の上乗せ（<c>Fiscal</c> の <c>adminClaim</c> の基数＝当月の予算基数とは別）。助走の月は写さない。</summary>
    internal static void AdminBase(World w, Region r, double nationPop)
    {
        if (w.WarmupFrozen) return;
        var st = S(w);
        st.BudSpendable = w.SpendableRevenue(); st.BudLastRev = w.LastRevenuePerMonth.Value;
        if (r.BureaucratPopIndex >= 0 && nationPop > 1e-9)
        {
            double share = r.SumPopulation() / nationPop;
            st.BudCover += share; st.BudMul += share * Fiscal.AdminDistanceMul(w, r);
        }
    }
    /// <summary>国の行政の充足（活きた州の人口加重・前の月の決算で EconomyStep が書いた <see cref="Region.AdminFill"/>）。</summary>
    static double AdminFillShare(World w)
    {
        double s = 0, n = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            double p = r.SumPopulation();
            if (!(p > 0)) continue;
            s += p * r.AdminFill; n += p;
        }
        return n > 0 ? s / n : double.NaN;
    }
    /// <summary>月の決算の終わり：行政の請求の2つの式と支払い率・行政の充足を四半期の和へ足す。</summary>
    static void AccumulateAdmin(World w, WState st)
    {
        st.AdmMonths++;
        var led = w.TreasuryLedger;
        st.AdmSimClaim += st.Claim[(int)Item.Admin];
        st.AdmCatClaim += st.Claim[(int)Item.Admin] + st.Claim[(int)Item.Infra] + st.Claim[(int)Item.ElecInfra];
        st.AdmFiscClaim += w.LastAdminClaimPerMonth.Value;
        st.AdmBase += w.LastRevenuePerMonth.Value;
        st.AdmSpendable += st.BudSpendable; st.AdmLastRev += st.BudLastRev;
        st.AdmSimMul += st.BudMul; st.AdmCover += st.BudCover;
        st.AdmFiscMul += Fiscal.AdminDistanceMean(w);
        if (st.HaveLedger) { st.AdmCatPaid += -(led[(int)TreasuryCat.Admin] - st.PrevLedger[(int)TreasuryCat.Admin]); st.AdmPaidMonths++; }
        double fill = AdminFillShare(w);
        if (!double.IsNaN(st.PayRate)) st.AdmPayRate += st.PayRate;   // (261) CS1718 の書き直し（NaN を除く・意味は同じ）
        if (st.PayRate < 1) { st.AdmRBelow1Months++; st.AdmFillRBelow1 += fill; }
        st.AdmFill += fill;
    }

    /// <summary>AI の総動員の判断（<c>UtilityAgent.DecideQuarter</c> の総動員の分岐の終わり）：戦争の規模と存亡（<see cref="Diplomacy.WarScale"/> の戻り値と out）・交戦中か・判断の後の総動員。四半期の頭に1回。</summary>
    internal static void Mobilization(World w, bool atWar, double scale, double losing)
    {
        var st = S(w);
        st.DecAtWar = atWar ? 1 : 0; st.DecScale = scale; st.DecLosing = losing; st.DecMobilized = w.Mobilized ? 1 : 0;
    }
    /// <summary>AI が戦時段（最上段）を目標に置いた（<c>WarStageOpen</c> が真のとき無条件）。</summary>
    internal static void WarStagePicked(World w) => S(w).DecPicked = 1;
    /// <summary>★(270) 軍事費の合計の施行の一歩（<c>Government.Move</c> の呼びのうち軸が Coercion・歩幅の尺度が 1.0 の1本＝<c>Step</c> の milTotal）。
    /// outcome：0＝目標＝現在で動かず／1＝純支持が余白に届かず固定／2＝政治資本の在庫が代価に届かず固定／3＝無償で動いた／4＝政治資本を払って動いた。
    /// 抵抗は <see cref="PolicyPreferences.OpposingPower"/> の合計とブロックごとの内訳（力×反対の強さ＝旧支配層・近代エリート・大衆。残りは議会・小営業・軍）。読むだけ。</summary>
    internal static void MilitaryMove(World w, double cur, double target, double net, double resist, double speed, int outcome, in PolicyPreferences.Profile e, bool increasing)
    {
        var st = S(w);
        st.MmOutcome = outcome; st.MmCur = cur; st.MmTarget = target; st.MmNet = net; st.MmResist = resist; st.MmSpeed = speed; st.MmPush = w.PoliticalPushBudget;
        st.MmStock = w.MandateSpent; st.MmStockMax = w.PoliticalCapitalMax; st.MmIncreasing = increasing;
        if (e.Power is null) return;
        FmNet(w, e, increasing);   // (371) 流れM の計器：純支持の項への分解
        double sign = increasing ? 1 : -1;
        double p0 = PolicyPreferences.Pref(PolicyAxis.Coercion, 0, e, w) * sign, p1 = PolicyPreferences.Pref(PolicyAxis.Coercion, 1, e, w) * sign, p2 = PolicyPreferences.Pref(PolicyAxis.Coercion, 2, e, w) * sign;
        st.MmOppTrad = p0 < 0 ? e.Power[0] * -p0 : 0; st.MmOppModern = p1 < 0 ? e.Power[1] * -p1 : 0; st.MmOppMass = p2 < 0 ? e.Power[2] * -p2 : 0;
    }
    /// <summary>★(270) 開いた戦線の数（<c>Government.Step</c> の政治予算の分母）。</summary>
    internal static void PushFronts(World w, int fronts) => S(w).MmFronts = fronts;
    // ---------------------------------------------------------------- (274) 線G：幽霊戦線（読むだけ）----------------------------------------------------------------
    /// <summary>戦線の数え上げの始まり（<c>Government.CountOpenFronts</c> の頭）。前の四半期の印を捨てる。</summary>
    internal static void FrontsBegin(World w) { var st = S(w); st.Gf.Clear(); st.GfCounted = true; st.GfMilExpired = false; }
    /// <summary>開いた戦線1本（<c>CountOpenFronts</c> が <c>n++</c> する場所そのもの＝述語を計器へ書き写さない）。</summary>
    internal static void FrontCounted(World w, char kind, int idx, PolicyAxis axis, double cur, double target)
        => S(w).Gf.Add(new GFront { Kind = kind, Idx = idx, Axis = axis, Cur = cur, Target = target });
    /// <summary>施行のループがいま回している段の法の表の行（<c>StepLevel</c> の結果をその戦線へ付ける）。</summary>
    internal static void LevelBegin(World w, int idx) => S(w).GfLevelIdx = idx;
    /// <summary>解禁の外で軍事費の施行が段3へ戻された四半期（<c>Step</c> の失効の行）。</summary>
    internal static void MilitaryExpiry(World w) => S(w).GfMilExpired = true;
    /// <summary><c>MandateGate</c> の結果：1 純支持の門で否決／2 政治資本の在庫で否決／3 無償で通った／4 政治資本を払って通った。</summary>
    internal static void GateResult(World w, int why, double net)
    {
        var st = S(w); st.GfGateWhy = why; st.GfGateNet = net;
        // (278) 代価：払う枝（2 在庫で否決・4 払って通った）は門が計算した額（Priced が置いた）／3 無償＝0／1 門の手前＝なし。払って通った額は四半期の台帳へ（Enact 経由で同じ行が StepLevel を複数回呼んでも落とさない）
        if (why is 2 or 4) { st.GfGateCost = st.GfPendCost; st.GfGateS = st.GfPendS; if (why == 4) st.GfPaidL += st.GfPendCost; }
        else { st.GfGateCost = why == 3 ? 0 : double.NaN; st.GfGateS = double.NaN; }
        st.GfPendCost = st.GfPendS = double.NaN;
    }
    /// <summary>(278) 符号 2 のうち代価が政治資本の最大量 b そのものを超える（その四半期の b では在庫が空〔満タン〕でも届かない・b と純支持は四半期ごとに動く）＝段の法と連続の軸で同じ1つの定義。</summary>
    static bool IsStructural(World w, int code, double cost) => code == 2 && cost > w.PoliticalCapitalMax;
    /// <summary>(278) 政治資本の代価の計算の直後（<c>MandateGate</c> の押し切りの枝・<c>Move</c> の押し切りの枝）＝払う前の目減り S を一緒に写す。</summary>
    internal static void Priced(World w, double cost) { var st = S(w); st.GfPendCost = cost; st.GfPendS = w.MandateSpent; }
    /// <summary>(278) <c>Government.Step</c> の段の法のループの頭：目減り S と最大量 b（四半期に1回導出して World に置いた値）。</summary>
    internal static void StockAtLevelHead(World w) { var st = S(w); st.GfSHead = w.MandateSpent; st.GfMaxHead = w.PoliticalCapitalMax; }
    /// <summary>(278) <c>Government.Step</c> の末尾：目減り S。</summary>
    internal static void StockAtStepEnd(World w) => S(w).GfSEnd = w.MandateSpent;
    /// <summary>番人（テスト）が1回の <c>Government.Step</c> の後の政治資本の台帳を読む口：段の法のループの頭の S・Step の末尾の S・最大量 b・段の法の払い・連続の軸（R・M・T・C）の払いの和。</summary>
    internal static (double SHead, double SEnd, double Max, double PaidLevel, double PaidContinuous) LedgerOf(World w)
    { var st = S(w); return (st.GfSHead, st.GfSEnd, st.GfMaxHead, st.GfPaidL, st.GfPaidR + st.GfPaidM + st.GfPaidT + st.GfPaidC); }
    /// <summary>番人（テスト）が1回の <c>Government.Step</c> の後の開いた戦線を読む口。</summary>
    internal static IReadOnlyList<GFront> FrontsOf(World w) => S(w).Gf;
    static GFront? FindFront(WState st, char kind, int idx)
    {
        foreach (var f in st.Gf) if (f.Kind == kind && f.Idx == idx) return f;
        return null;
    }
    /// <summary><c>StepLevel</c> の結果。code −1＝<c>MandateGate</c> を通った（門の結果と、一歩で目標に届いたかから符号を決める）。</summary>
    internal static void LevelResult(World w, int code, int ci, int ti, int result)
    {
        var st = S(w);
        bool gated = code == -1;   // (278) 門まで来た一歩だけが代価を持つ
        if (code == -1) code = st.GfGateWhy switch { 1 => 1, 2 => 2, 4 => result == ti ? 8 : 4, _ => result == ti ? 7 : 3 };
        if (FindFront(st, 'L', st.GfLevelIdx) is { } f)
        {
            f.Code = code; f.Net = code is 0 or 5 or 6 ? double.NaN : st.GfGateNet;
            if (gated) { f.Cost = st.GfGateCost; f.SDec = st.GfGateS; f.Structural = IsStructural(w, code, f.Cost); }
        }
    }
    /// <summary><c>Government.Move</c> の結果（outcome は <see cref="MilitaryMove"/> と同じ 0 閉じていた／1 純支持／2 政治資本／3 無償で動いた／4 払って動いた）。軸と歩幅の尺度から戦線の種類を決める。</summary>
    internal static void MoveResult(World w, PolicyAxis a, double spendScale, int outcome, double cur, double target, double next, double net, double speed)
    {
        var st = S(w);
        // (278) 代価：払う枝（2 在庫で否決・4 払って動いた）は Priced が置いた額／3 無償＝0／0・1 は門の手前＝なし。払って動いた額は種類ごとの四半期の台帳へ
        double cost = outcome is 2 or 4 ? st.GfPendCost : outcome == 3 ? 0 : double.NaN, sDec = outcome is 2 or 4 ? st.GfPendS : double.NaN;
        st.GfPendCost = st.GfPendS = double.NaN;
        char kind; int idx = 0;
        if (a == PolicyAxis.Coercion) kind = spendScale == 1.0 ? 'M' : 'C';
        else if (a == PolicyAxis.Tariff) kind = 'T';
        else { idx = Array.FindIndex(Government.RateLaws, l => l.Axis == a); kind = 'R'; }
        if (outcome == 4) { switch (kind) { case 'M': st.GfPaidM += cost; break; case 'T': st.GfPaidT += cost; break; case 'C': st.GfPaidC += cost; break; default: st.GfPaidR += cost; break; } }
        if (kind == 'R' && idx < 0) return;
        if (FindFront(st, kind, idx) is not { } f) return;
        int code = outcome >= 3 && Math.Abs(target - next) <= 0.026 ? (outcome == 4 ? 8 : 7) : outcome;
        if (kind == 'M' && st.GfMilExpired) { f.Expired = true; if (code == 0) code = 9; }
        f.Code = code; f.Net = net; f.Cost = cost; f.SDec = sDec; f.Structural = IsStructural(w, code, cost);
    }
    /// <summary>四半期の頭の <c>Government.Step</c> の後：戦時段の解禁・軍事費の段の目標（保存）と施行。</summary>
    internal static void AfterGovernment(World w)
    {
        var st = S(w);
        st.DecOpen = Government.WarStageOpen(w) ? 1 : 0;
        st.DecTarget = Government.MilitaryStage(w.Policy.MilitarySpendingTarget); st.DecEnforced = Government.MilitaryStage(w.MilitarySpending.Value); st.DecShare = w.MilitarySpending.Value;
        st.MmEnvelopeAfterGov = Military.WarStageShare(w, w.MilitarySpending.Value);   // (270) 戦時段の器の割合（エンジンの量そのもの）
    }
    static void ResetQuarter(WState st)
    {
        st.AdmMonths = st.AdmPaidMonths = st.AdmRBelow1Months = 0;
        st.AdmSimClaim = st.AdmCatClaim = st.AdmCatPaid = st.AdmFiscClaim = st.AdmBase = st.AdmSpendable = st.AdmLastRev = st.AdmSimMul = st.AdmCover = st.AdmFiscMul = st.AdmPayRate = st.AdmFill = st.AdmFillRBelow1 = 0;
        st.DecScale = st.DecLosing = st.DecShare = double.NaN;
        st.DecAtWar = st.DecMobilized = st.DecOpen = st.DecTarget = st.DecEnforced = -1; st.DecPicked = 0;
        st.FsRun = st.FsSeenMismatch = st.FsFired = st.FsFiredMismatch = st.FsFiredOver = st.SubMismatch = st.SubMismatchOff = 0; st.WheelGoods.Clear(); st.FacStartedGoods.Clear();   // (263) 助走の分は数えない
        // (265) 助走の分は数えない
        st.BondMonths = 0; st.BondBase = st.BondDebt = st.BondFisher = st.BondDistrust = st.BondPolicy = st.BondGold = st.BondRepr = st.BondClamp = st.BondRate = st.BondDebtRatio = st.BondConvMul = st.BondReprMul = 0;
        st.QAdmClaim = st.QAdmNeedB = st.QAdmMinB = st.QAdmNeedP = st.QAdmMinP = st.QCfBorrowed = 0; st.QAdmOverRegionMonths = st.QCfShortMonths = st.QCfRBelow1Months = st.QCfObs = 0;
        st.QCapPaid = st.QCapPopK = 0; Array.Clear(st.QCapBoundPaid); Array.Clear(st.QCapCut); Array.Clear(st.QCapBoundPopK);
        st.FactorySold = 0; st.MonoBefore = st.MonoTarget = -1; st.FiscalStressAtDecision = double.NaN; st.PushBurden = st.PushP = st.PushU = double.NaN; st.PushHit = 0;
        // (271) 助走の分は数えない
        st.CnRows = st.CnBound = 0; st.CnDesired = st.CnDemand = 0;
        st.ConDesiring = st.ConBound = 0; st.ConDesiredUnits = st.ConCutUnits = 0;
        st.ConRegionMonths = 0; st.ConGainSum = st.ConHurdleSum = st.ConBudgetMoney = st.ConDesiredMoney = 0; st.ConPlannedMoney = st.ConBudgetBoundMoney = st.ConBuiltUnits = st.ConPlannedUnits = 0; Array.Clear(st.ConRationW); if (st.QSpend is not null) Array.Clear(st.QSpend);   // (286) 助走の分は数えない
        st.BrJudged = st.BrPoolBound = st.BrForeignCut = 0; st.BrDomPool = st.BrForRaw = st.BrFor = 0;
        st.CrDays = st.CrRepayBoundDays = st.CrFiniteDays = 0; st.CrReserveCap = st.CrRepayable = 0;
        st.MmOutcome = st.MmFronts = -1; st.MmIncreasing = false;
        st.MmCur = st.MmTarget = st.MmNet = st.MmResist = st.MmSpeed = st.MmPush = st.MmOppTrad = st.MmOppModern = st.MmOppMass = st.MmStock = st.MmStockMax = st.MmEnvelopeAfterGov = double.NaN;
        st.Gf.Clear(); st.GfCounted = st.GfMilExpired = false; st.GfLevelIdx = -1; st.GfGateWhy = 0; st.GfGateNet = double.NaN;   // (274) 助走の分は数えない
        st.GfSHead = st.GfMaxHead = st.GfSEnd = st.GfPendCost = st.GfPendS = st.GfGateCost = st.GfGateS = double.NaN; st.GfPaidL = st.GfPaidR = st.GfPaidM = st.GfPaidT = st.GfPaidC = 0;   // (278) 助走の分は数えない
        st.QLimitMonths = st.QLimitCrossings = st.QLimitBorrowMonths = st.QZombieMonths = st.QWarGateClosedMonths = st.QOver067Months = st.QOver067BorrowedMonths = st.QOver067RBelow1Months = 0;   // (281) 助走の分は数えない（LimitPrev は四半期をまたぐ＝ここでは戻さない）
        st.QUnpaidObs = st.QUnpaidMonths = st.QMegaPass = st.QMegaOpen = 0; st.QUnpaidSum = st.QUnpaidMax = st.QDevalueShareSum = st.QDevalueShareMax = 0;
        Array.Clear(st.QStPaid);   // (289) 助走の分は数えない
    }
    /// <summary>国×四半期の行に足す列（行政の請求の2つの式・戦時段）。月ごとの量は四半期の月の平均（<c>PerMonth</c>）。</summary>
    static List<(string Name, string Value)> NationExtras(World w, WState st)
    {
        var cols = new List<(string, string)>();
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        void C(string name, double v) => cols.Add((name, F(v)));
        void N(string name, int v) => cols.Add((name, I(v)));
        double m = st.AdmMonths > 0 ? st.AdmMonths : double.NaN, mp = st.AdmPaidMonths > 0 ? st.AdmPaidMonths : double.NaN;
        // 戦時段：四半期の頭（AI の判断と Government.Step の後）と四半期の末
        N("warStageOpenAfterGovFlag", st.DecOpen); C("warScaleRatioAtDecision", st.DecScale); C("homeLosingScoreAtDecision", st.DecLosing);
        N("atWarAtDecisionFlag", st.DecAtWar); N("mobilizedAtDecisionFlag", st.DecMobilized); N("aiPickedWarStageFlag", st.DecPicked);
        N("militaryStageTargetAfterGovStage", st.DecTarget); N("militaryStageEnforcedAfterGovStage", st.DecEnforced); C("militarySpendingAfterGovShare", st.DecShare);
        N("warStageOpenEndFlag", Government.WarStageOpen(w) ? 1 : 0); N("militaryStageTargetEndStage", Government.MilitaryStage(w.Policy.MilitarySpendingTarget));
        N("militaryStageEnforcedEndStage", Government.MilitaryStage(w.MilitarySpending.Value)); C("militarySpendingEndShare", w.MilitarySpending.Value);
        // 行政の請求：Simulation の州の予算の和（claimAdmin）と Fiscal の adminClaim（LastAdminClaim）・その基数と距離の割増・実際の支払い（台帳の Admin＝行政＋上下水道＋電化）
        N("adminMonthsCount", st.AdmMonths); N("adminRBelow1MonthsCount", st.AdmRBelow1Months);
        C("adminClaimSimPerMonth", st.AdmSimClaim / m); C("adminClaimFiscalPerMonth", st.AdmFiscClaim / m);
        C("adminCatClaimSimPerMonth", st.AdmCatClaim / m); C("adminCatPaidPerMonth", st.AdmCatPaid / mp);
        C("adminSpendableAtBudgetPerMonth", st.AdmSpendable / m); C("adminLastRevenueAtBudgetPerMonth", st.AdmLastRev / m); C("adminRevenueBasePerMonth", st.AdmBase / m);
        C("adminSimMeanMulShare", st.AdmSimMul / m); C("adminBureaucratCoverShare", st.AdmCover / m); C("adminFiscalMeanMul", st.AdmFiscMul / m);
        C("payRateMeanShare", st.AdmPayRate / m); C("adminFillMeanShare", st.AdmFill / m);
        C("adminFillMeanInRBelow1Share", st.AdmRBelow1Months > 0 ? st.AdmFillRBelow1 / st.AdmRBelow1Months : double.NaN);
        return cols;
    }

    // ---------------------------------------------------------------- 軍の更新の直前・外交（史実比較の桁の読み手の入力） ----------------------------------------------------------------

    /// <summary>軍の更新（<c>Military.Step</c>）の直前：この四半期の交戦の月と内戦の月を数える。月の決算の終わり（<see cref="MonthClose"/>）で数えると外交の更新の後＝内戦の始まりと終わりの月で1か月ずれる。</summary>
    internal static void WarMonth(World w, bool atWar)
    {
        var st = S(w);
        if (atWar) st.WarMonths++;
        st.WarNow = atWar;   // (281) 交戦×門閉の月を MonthClose で数える
        if (w.InCivilWar) st.CivilWarMonths++;
    }
    /// <summary>戦争を立てた所（<c>Diplomacy.AddBelligerent</c> の <c>Wars.Add</c> の直後）：試合の終わりに戦死と月数を読む。</summary>
    internal static void WarStarted(Diplomacy dip, War war, int year) => Dips.GetValue(dip, _ => new DipState()).Wars.Add((year, war));
    /// <summary>月の戦死（<c>Diplomacy.WarCasualties</c>）：(年, 国)ごとの月数・兵士の月和・戦死の和（戦死は兵士の上限をかけた後の量）。</summary>
    internal static void Casualty(Diplomacy dip, int year, Power p, double soldierPop, double dead)
    {
        var d = Dips.GetValue(dip, _ => new DipState());
        var key = (year, NameOf(p));
        if (!d.Cas.TryGetValue(key, out var a)) d.Cas[key] = a = new double[3];
        a[0] += 1; a[1] += soldierPop; a[2] += dead;
    }

    // ---------------------------------------------------------------- 国営の棟の着工と完成（(260) 線E の合否の量） ----------------------------------------------------------------

    /// <summary>着工の時点の門（発見の門が閉じていたか）を生産者ごとに覚える計器の側の表＝シミュの状態を足さない（完成の月に「着工の時点でも閉じていたか」を読むため）。</summary>
    sealed class GateAtStart { public bool Closed; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Producer, GateAtStart> StartGates = new();

    /// <summary>国営の棟の着工（<c>World.BuildFactory</c> が <c>ConstructionLeft</c> を置いた直後）：門が閉じた生産者への着工を数える。読むだけ（<see cref="Discovery.Gate(Region, Producer)"/> は純関数）。</summary>
    internal static void FacilityStarted(World w, Region r, Producer p)
    {
        var st = S(w);
        bool closed = !(Discovery.Gate(r, p) > 0);
        st.FacStarted++; if (closed) st.FacStartedClosed++;
        st.FacStartedGoods.Add(Catalog.Goods[p.GoodId].Name);
        StartGates.GetValue(p, _ => new GateAtStart()).Closed = closed;
    }

    // ---------------------------------------------------------------- 輪と動詞の個体の食い違い（(263) 線E2 A） ----------------------------------------------------------------

    /// <summary>投げ売りの輪が回った（財政の逼迫が <c>FireSaleFiscalStress</c> を超えた四半期の頭）。</summary>
    internal static void FireSaleRun(World w) => S(w).FsRun++;

    /// <summary>投げ売りの輪が国営の持分のある生産者 <paramref name="pr"/> を見た：動詞（<see cref="World.CanSellFactory"/>・<see cref="World.SetStateTarget"/>）が掴む個体（<see cref="World.FindFactory"/>）が <paramref name="pr"/> と別なら数える。
    /// <see cref="World.FindFactory"/>・<see cref="Discovery.Gate(Region, Producer)"/> は純関数（書かない・メモ化しない）。</summary>
    internal static void FireSaleSeen(World w, Region rg, Producer pr)
    {
        if (ReferenceEquals(World.FindFactory(rg, pr.GoodId), pr)) return;
        S(w).FsSeenMismatch++;
    }

    /// <summary>投げ売りの輪が売ると決めた：①火が付いた回数 ②その個体が FindFactory の個体と別（食い違い）③食い違いのうち、輪が置く目標〔pr の棟数−1〕が動詞が掴む個体の現在の棟数を超える（＝着工になる向き）。
    /// 食い違いの財の名前は <c>投げ売り:財名</c>（<c>旧棟数→掴まれる個体の棟数/掴まれる個体の門の開閉/pr の門の開閉</c> を括弧に）。</summary>
    internal static void FireSaleFired(World w, Region rg, Producer pr)
    {
        var st = S(w);
        st.FsFired++;
        var p = World.FindFactory(rg, pr.GoodId);
        if (p is null || ReferenceEquals(p, pr)) return;
        st.FsFiredMismatch++;
        int prUnits = World.StateUnits(pr, w.Coef), pUnits = World.StateUnits(p, w.Coef);
        int target = Math.Max(0, prUnits - 1);
        if (target > pUnits) st.FsFiredOver++;
        st.WheelGoods.Add($"投げ売り:{Catalog.Goods[pr.GoodId].Name}({prUnits}→{pUnits}/{(Discovery.Gate(rg, p) > 0 ? "開" : "閉")}/{(Discovery.Gate(rg, pr) > 0 ? "開" : "閉")}/{(target > pUnits ? "着工" : target < pUnits ? "売却" : "同じ")})");
    }

    /// <summary>補助の輪が補助を書き換える（<c>pr.Subsidized != want</c>）：動詞（<see cref="World.SetSubsidy"/>）が掴む個体が <paramref name="pr"/> と別なら数える。</summary>
    internal static void SubsidyWrite(World w, Region rg, Producer pr, bool want)
    {
        var p = World.FindFactory(rg, pr.GoodId);
        if (p is null || ReferenceEquals(p, pr)) return;
        var st = S(w);
        st.SubMismatch++; if (!want) st.SubMismatchOff++;
        st.WheelGoods.Add($"補助:{Catalog.Goods[pr.GoodId].Name}");
    }

    /// <summary>四半期の末の断面：国営の持分がある生産者のうち、FindFactory が別の個体を返すもの（動詞が掴めない持分）の数。
    /// 返る個体も持分があるなら <paramref name="toInvested"/>（持分が2つの個体に分かれている）・無いなら <paramref name="toUninvested"/>（持分は売れない＝CanSellFactory が「国営でない」を返す）。</summary>
    static int StakeMismatch(World w, out int toInvested, out int toUninvested, out int staleSubsidy, List<string> goods)
    {
        int all = 0; toInvested = toUninvested = staleSubsidy = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var pr in r.Producers)
            {
                bool stake = pr.BuiltScale > 1e-9;
                if (!stake && !pr.Subsidized) continue;
                var p = World.FindFactory(r, pr.GoodId);
                if (p is null || ReferenceEquals(p, pr)) continue;
                if (pr.Subsidized && Discovery.Gate(r, pr) > 0) staleSubsidy++;   // 補助が付いたまま動詞が掴まない（門が開いた）個体＝補助の輪が外せない補助
                if (!stake) continue;
                all++;
                bool inv = p.BuiltScale > 1e-9;
                if (inv) toInvested++; else toUninvested++;
                goods.Add($"{Catalog.Goods[pr.GoodId].Name}({(Discovery.Gate(r, pr) > 0 ? "開" : "閉")}{World.StateUnits(pr, w.Coef)}→{(inv ? "投資済" : "未投資")}{World.StateUnits(p, w.Coef)})");
            }
        }
        return all;
    }

    /// <summary>国営の棟の完成（<c>World.StepConstruction</c> が <c>BuiltScale</c> に1棟足した直後）：完成の月に門が閉じている棟（＝操業規模が0に置かれる不活性の設備）を数える。
    /// 着工の時点でも閉じていた分と、工期の途中で（後継の発見により）閉じた分は、最後の列が分ける。</summary>
    internal static void FacilityCompleted(World w, Region r, Producer p)
    {
        var st = S(w);
        st.FacDone++;
        if (Discovery.Gate(r, p) > 0) return;
        st.FacDoneClosed++;
        if (StartGates.TryGetValue(p, out var g) && g.Closed) st.FacDoneClosedAtStart++;
    }

    // ---------------------------------------------------------------- (262) 貯蓄の書き手の台帳（階級の ΔSavings の出どころ・読むだけ） ----------------------------------------------------------------

    /// <summary>貯蓄 <c>Pop._savings</c> の書き手の種類。書き手は Pop の中の5本だけ（<c>Pop.Distribute</c>・<c>Take</c>・<c>Carry</c>・<c>Rebase</c>・<c>SettleMonth</c>。開幕の貯蓄の置き直し〔旧 RebaseSavingsTo〕は削除した）。
    /// Settle＝月末の精算（所得−支出）／Give＝受け取り（所得に書かない）／GiveIncome＝受け取り（所得にも書く）／Take＝払い（支出に書かない）／CarryOut・CarryIn＝人と動くお金（階級・州・国をまたぐ運搬）／Rebase＝国が替わる換算。</summary>
    internal enum SavKind : byte { Settle, Give, GiveIncome, Take, CarryOut, CarryIn, Rebase, RegionOut, RegionIn }
    static readonly string[] SavKindNames = ["settle", "give", "giveIncome", "take", "carryOut", "carryIn", "rebase", "regionOut", "regionIn"];
    [ThreadStatic] static string? _routeFile;
    [ThreadStatic] static int _routeLine;
    [ThreadStatic] static char _routeFrom, _routeTo;   // 払い手の側・受け手の側（P＝POP・T＝国庫・B／M／F／C／H＝世界の外の台帳の種類）
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Pop, WState> PopOwner = new();

    /// <summary>移し方の1本（<see cref="World.Transfer"/>）と人の移し方の1本（<see cref="Pop.Blend"/>）の入口：呼び出しの位置（コンパイル時の定数・[CallerFilePath]／[CallerLineNumber]）を、続く貯蓄の書き込みの経路名にする。</summary>
    internal static void Route(string file, int line, char fromSide, char toSide) { _routeFile = file; _routeLine = line; _routeFrom = fromSide; _routeTo = toSide; }

    /// <summary>州が別の国へ移る（<c>Territory.TransferRegion</c>・換算の後）：送り手の台帳から州の POP の貯蓄を引き（regionOut）、受け手が挙げた国なら POP を受け手の台帳へ登録して足す（regionIn）。建国（to＝null）と挙げていない国へ移った POP は台帳の外へ出る。</summary>
    internal static void RegionMoved(World? to, Region r)
    {
        foreach (var p in r.Pops) { Sav(p, SavKind.RegionOut, -p.Savings.Value); PopOwner.Remove(p); }
        if (to is null) return;
        var st = S(to);
        if (!IsPicked(to, st)) return;
        foreach (var p in r.Pops) { PopOwner.AddOrUpdate(p, st); Sav(p, SavKind.RegionIn, p.Savings.Value); }
    }

    static void RegisterPops(World w, WState st) { foreach (var r in w.Regions) foreach (var p in r.Pops) PopOwner.AddOrUpdate(p, st); }

    /// <summary>貯蓄の書き込み1件（<c>Pop</c> の中の書き手の隣から呼ぶ・額は符号つきの増分）。挙げた国の POP だけ記帳する（登録が無ければ何もしない）。</summary>
    internal static void Sav(Pop p, SavKind kind, double delta)
    {
        if (!PopOwner.TryGetValue(p, out var st)) return;
        bool routed = kind is SavKind.Give or SavKind.GiveIncome or SavKind.Take or SavKind.CarryOut or SavKind.CarryIn;
        // 相手の側：受け取り（Give・GiveIncome）は払い手の側・払い（Take）は受け手の側。運搬は POP どうし
        char cp = !routed ? '-' : kind is SavKind.Give or SavKind.GiveIncome ? _routeFrom : kind == SavKind.Take ? _routeTo : 'P';
        var key = ((uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1, (int)kind, routed ? (_routeFile ?? "") : "", routed ? _routeLine : 0, cp);
        if (!st.SavLedger.TryGetValue(key, out var a)) st.SavLedger[key] = a = new double[2];
        a[0] += delta; a[1] += 1;
    }

    /// <summary>四半期の末：挙げた国の台帳を <c>flow_s&lt;seed&gt;.tsv</c> へ書いて0へ戻す。額は四半期の月平均（<c>PerMonth</c>＝和÷月数）・件数は四半期の和。</summary>
    static void FlowRows(Sink sk, ulong seed, int q, World w, WState st)
    {
        if (st.SavLedger.Count == 0) return;
        sk.Flow ??= Open("flow", seed);
        if (!sk.FlowHeader) { sk.Flow.WriteLine("quarter	year	nation	classId	kind	sourceFile	sourceLine	counterpartySide	amountPerMonth	eventCount"); sk.FlowHeader = true; }
        double mo = st.ClsMonths > 0 ? st.ClsMonths : double.NaN;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var kv in st.SavLedger.OrderBy(k => k.Key.Cls).ThenBy(k => k.Key.Kind).ThenBy(k => k.Key.File, StringComparer.Ordinal).ThenBy(k => k.Key.Line).ThenBy(k => k.Key.Cp))
            sk.Flow.WriteLine(string.Join('	', I(q), I(w.Year), NameOf(w), I(kv.Key.Cls), SavKindNames[kv.Key.Kind], System.IO.Path.GetFileName(kv.Key.File), I(kv.Key.Line), kv.Key.Cp.ToString(), F(kv.Value[0] / mo), F(kv.Value[1])));
        st.SavLedger.Clear();
    }

    // ---------------------------------------------------------------- (281) 線O0：借入の門の周り（読むだけ） ----------------------------------------------------------------

    /// <summary>仏1788 の支払停止の線＝利払い÷歳入（3.18億÷4.72億リーヴル≒0.67・Fiscal.Borrow の注の史実の錨）。係数ではない＝計器の物差し（借入の門の限界 <see cref="Coefficients.MaxInterestShareOfRevenue"/> は係数から読む）。</summary>
    internal const double SuspensionInterestShare = 0.67;

    /// <summary>切り下げで埋める前の払えない額÷請求＝max(0, 請求−(max(0,国庫)＋歳入＋借入))÷請求（[0,1]）。<paramref name="payable0"/>＝月初の払える額 max(0,国庫)＋歳入（借越は含めない＝当座借越を数えない）。
    /// 請求が正でない月・入力が NaN の月は NaN（数えない）。</summary>
    internal static double UnpaidBeforeDevaluationShare(double claims, double payable0, double borrowed)
    {
        if (!(claims > 0) || double.IsNaN(payable0) || double.IsNaN(borrowed)) return double.NaN;
        return Math.Max(0, claims - (payable0 + borrowed)) / claims;
    }

    /// <summary>月の決算の終わり：借入の門の周りの月数を四半期の和へ足す。
    /// 限界に届いた月（<see cref="WState.LimitNow"/>）・前の月との反転（窓の縁の往復＝反転の数）・限界に届いたまま率 r＝1 で借りた月・ゾンビの月（門に依らない定義＝限界に届いた・r＝1・(借入＋切り下げ)＞0）・
    /// 交戦×門閉・仏1788 の線を超えた月と、その月に借りたか・r＜1 か・切り下げで埋める前の払えない額÷請求と切り下げ÷請求。</summary>
    static void AccumulateGateAround(WState st)
    {
        if (st.LimitNow < 0) return;   // 借入の判定の位置に来なかった月（財政が無い国）は数えない
        double borrowed = st.Borrowed > 0 ? st.Borrowed : 0, dev = st.Devalued > 0 ? st.Devalued : 0;   // 否定形＝NaN は0
        bool rOne = st.PayRate >= 1;
        if (st.LimitNow == 1)
        {
            st.QLimitMonths++;
            if (rOne && borrowed > 0) st.QLimitBorrowMonths++;
            if (rOne && borrowed + dev > 0) st.QZombieMonths++;
        }
        if (st.LimitPrev >= 0 && st.LimitPrev != st.LimitNow) st.QLimitCrossings++;
        st.LimitPrev = st.LimitNow;
        if (st.GateOpenNow == 0 && st.WarNow) st.QWarGateClosedMonths++;
        if (st.Over067Now == 1)
        {
            st.QOver067Months++;
            if (borrowed > 0) st.QOver067BorrowedMonths++;
            if (st.PayRate < 1) st.QOver067RBelow1Months++;
        }
        double unpaid = UnpaidBeforeDevaluationShare(st.ClaimsTotal, st.Payable0, st.Borrowed);
        if (!double.IsNaN(unpaid))
        {
            double devShare = dev / st.ClaimsTotal;
            st.QUnpaidObs++; st.QUnpaidSum += unpaid; st.QDevalueShareSum += devShare;
            if (unpaid > 0) st.QUnpaidMonths++;
            if (unpaid > st.QUnpaidMax) st.QUnpaidMax = unpaid;
            if (devShare > st.QDevalueShareMax) st.QDevalueShareMax = devShare;
        }
    }

    /// <summary>巨大建造物の恐慌の拠出の判定（<c>Megaprojects.Step</c> の反景気循環の項）：恐慌（GDP の収縮が閾値の上）の判定に来た回数と、うち「まだ借りられる」（借入の門が開いている）が真だった回数。国×事業×四半期。</summary>
    internal static void MegaCrisis(World w, bool gateOpen)
    {
        var st = S(w);
        st.QMegaPass++; if (gateOpen) st.QMegaOpen++;
    }

    /// <summary>(281) 国×四半期の行の末尾に足す列。月数は四半期の和・割合は観測した月の平均と最大。</summary>
    static List<(string Name, string Value)> Extras281(WState st)
    {
        var cols = new List<(string, string)>();
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        double uo = st.QUnpaidObs > 0 ? st.QUnpaidObs : double.NaN;
        cols.Add(("interestAtLimitMonthsCount", I(st.QLimitMonths))); cols.Add(("interestLimitCrossingsCount", I(st.QLimitCrossings)));
        cols.Add(("interestAtLimitBorrowedMonthsCount", I(st.QLimitBorrowMonths))); cols.Add(("zombieMonthsCount", I(st.QZombieMonths)));
        cols.Add(("warGateClosedMonthsCount", I(st.QWarGateClosedMonths)));
        cols.Add(("interestShareOver067MonthsCount", I(st.QOver067Months))); cols.Add(("interestShareOver067BorrowedMonthsCount", I(st.QOver067BorrowedMonths))); cols.Add(("interestShareOver067RBelow1MonthsCount", I(st.QOver067RBelow1Months)));
        cols.Add(("unpaidBeforeDevaluationObservedMonthsCount", I(st.QUnpaidObs))); cols.Add(("unpaidBeforeDevaluationMonthsCount", I(st.QUnpaidMonths)));
        cols.Add(("unpaidBeforeDevaluationMeanShare", F(st.QUnpaidSum / uo))); cols.Add(("unpaidBeforeDevaluationMaxShare", st.QUnpaidObs > 0 ? F(st.QUnpaidMax) : "nan"));
        cols.Add(("devaluedToClaimMeanShare", F(st.QDevalueShareSum / uo))); cols.Add(("devaluedToClaimMaxShare", st.QUnpaidObs > 0 ? F(st.QDevalueShareMax) : "nan"));
        cols.Add(("megaCrisisPassCount", I(st.QMegaPass))); cols.Add(("megaCrisisGateOpenCount", I(st.QMegaOpen)));
        return cols;
    }

    /// <summary>★(289) 線D（作者の設計の材料）：国×四半期の行の末尾。<c>earnedTotalPerMonth</c>＝全階級の Earned の和（四半期の月平均・自国通貨の名目＝<c>class_s</c> の <c>earnedPerMonth</c> の和と同じ量を全ての国×四半期で）。
    /// <c>stateConstructionPaidPerMonth</c>＝国庫が実際に払った国の建設（材料＋工賃・<c>EconomyStep</c> の stateConstruction の決済）・<c>monumentPaidPerMonth</c>＝同じく巨大建造物（請求ベースの <c>fiscal_s</c> の claim でなく払えた額）。
    /// 読むだけ：支払いの位置の直後に決済で確定した額を積むだけ（値を書き換えない）。</summary>
    static List<(string Name, string Value)> Extras289(WState st)
    {
        var cols = new List<(string, string)>();
        double cm = st.ClsMonths > 0 ? st.ClsMonths : double.NaN;
        double earn = 0; for (int c = 0; c < NC; c++) earn += st.ClsEarn[c];
        cols.Add(("earnedTotalPerMonth", F(earn / cm)));
        cols.Add(("stateConstructionPaidPerMonth", F(st.QStPaid[0] / cm)));
        cols.Add(("monumentPaidPerMonth", F(st.QStPaid[1] / cm)));
        return cols;
    }

    /// <summary>★(329) 線(2)(3)（取り込み元 (299)(302)）：国×四半期の行の末尾の 2 列。読むだけ。
    /// <c>worldRealIncomePerCapitaPerMonth</c>＝<see cref="World.WorldRealIncomePerCapitaPerMonth"/>（世界の1人あたり実質所得〔名目÷物価・人数は千人の単位〕・MultiNationSim が月次決算の前に毎月配る値の、四半期の終わりの時点の値。全ての国で同じ値）。ε＝0（既定）は配りが走らない＝全ての国と四半期で 0。
    /// <c>luxuryExportsPerClearing</c>＝その国の <see cref="World.ExportsByGood"/>（直近の清算1回分・世界価格建て）のうち Luxury 階層の財〔アヘンを除く＝所得弾力の対象と同じ集合〕の和。事前登録の記録の列 H4〔奢侈品の輸出が稼ぎ頭の国〕を全ての国で読む（goods_s は残存国の主食・日用品だけ・px_goods は挙げた国だけ）。</summary>
    internal static List<(string Name, string Value)> Extras329(World w)
    {
        double luxExp = 0;
        if (w.ExportsByGood is { } eg)
            for (int g = 0; g < eg.Length && g < Catalog.Goods.Length; g++)
                if (Catalog.Goods[g].Tier == GoodTier.Luxury && !Habit.Of(w.Coef, g).Forms) luxExp += eg[g];
        var st = S(w);
        return [("worldRealIncomePerCapitaPerMonth", F(w.WorldRealIncomePerCapitaPerMonth)), ("luxuryExportsPerClearing", F(luxExp)),
            ("luxuryRatioPopMonths", F(st.LuxPopMonths)), ("luxuryRatioNonPositivePopMonths", F(st.LuxNonPosPopMonths)), ("luxuryRatioInvalidPopMonths", F(st.LuxInvalidPopMonths)), ("luxuryRatioNonPositiveSizeSum", F(st.LuxNonPosSizeSum))];
    }

    /// <summary>★(329) 奢侈品の所得弾力の計器（読むだけ）：EconomyStep が 1 州×1 か月ごとに呼ぶ。pops＝その州の POP の数（総数）・nonPos＝Size・世界の値・物価が正で r が非正の POP の数・invalid＝Size・世界の値・物価が正でない／r が非有限の数（どちらも f＝1 へ倒れた）・nonPosSize＝非正の POP の Size の和。累計は Extras329 が nation_s の末尾 4 列へ書く（ε＝0 では呼ばれない＝0）。</summary>
    internal static void LuxuryRatioDiag(World w, double pops, double nonPos, double invalid, double nonPosSize)
    {
        var st = S(w);
        st.LuxPopMonths += pops; st.LuxNonPosPopMonths += nonPos; st.LuxInvalidPopMonths += invalid; st.LuxNonPosSizeSum += nonPosSize;
    }

    // ---------------------------------------------------------------- (289) 線D：資本家の建設の州ごとの表 ----------------------------------------------------------------

    sealed class ConAcc { public int Judged; public double Desired, Budget, Planned, PlannedUnits, BuiltUnits, Paid; public int TermMonths, DesiringMonths; public double TermGain, TermSat, TermTech, TermCrowd, TermHeat, TermHurdle, TermNet, TermAbsorb, TermJoint, TermUnits, TermUnitMoney; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, ConAcc> ConAccs = new();
    /// <summary>(289) 建設の判定（州×月）：希望・投資枠・計画の金額（<see cref="Construction"/> と同じ定義〔束×束の費用×(1+工賃)〕）を州ごとに積む。挙げた国だけ。</summary>
    internal static void ConsRegion(World w, Region r, double desiredUnits, double budgetUnits, double unitMoney, double plannedUnits)
    {
        if (!IsPicked(w, S(w))) return;
        var a = ConAccs.GetValue(r, _ => new ConAcc());
        a.Judged++; if (desiredUnits > 0) a.Desired += desiredUnits * unitMoney;
        a.Budget += budgetUnits * unitMoney; a.Planned += plannedUnits * unitMoney; a.PlannedUnits += plannedUnits;
    }
    /// <summary>(300) 線 i1 段 0：希望の束を決める項を州×月で積む（読むだけ）。<paramref name="gainTech"/>＝限界収益×(1+技術の梃子×技術の水準)・<paramref name="sat"/>＝飽和の分母の底（1+生産的工業化度÷スケール）・
    /// <paramref name="techLev"/>＝技術の水準・<paramref name="hurdle"/>＝資本コスト＋クラウディング−金融の過熱・<paramref name="netReturn"/>＝gainTech−hurdle・<paramref name="joint"/>＝株式会社の速さの倍率・
    /// <paramref name="absorb"/>＝私的投資の吸収の因子（本体が希望の伸びに掛けた値そのもの・掛けない州は 1.0＝本体から引数で受ける・ここで計算し直さない）・
    /// <paramref name="units"/>＝希望の束（吸収の因子を掛けた後）・<paramref name="unitMoney"/>＝束の費用×(1+工賃)。挙げた国だけ。</summary>
    internal static void ConsTerms(World w, Region r, double gainTech, double sat, double techLev, double hurdle, double netReturn, double joint, double absorb, double units, double unitMoney)
    {
        if (!IsPicked(w, S(w))) return;
        var a = ConAccs.GetValue(r, _ => new ConAcc());
        a.TermMonths++; if (units > 0) a.DesiringMonths++;
        a.TermGain += gainTech; a.TermSat += sat; a.TermTech += techLev; a.TermCrowd += r.CrowdingPremium; a.TermHeat += r.FinancialHeat; a.TermHurdle += hurdle; a.TermNet += netReturn;
        a.TermAbsorb += absorb; a.TermJoint += joint; a.TermUnits += units; a.TermUnitMoney += unitMoney;
    }
    /// <summary>(289) 建設の決済（州×月）：建った束と、投資家が実際に払った額（材料＋工賃・<c>PaidPerMonth</c> へ足すのと同じ <c>cost</c>）。</summary>
    internal static void ConsRegionPaid(World w, Region r, double builtUnits, double cost)
    {
        if (!IsPicked(w, S(w))) return;
        var a = ConAccs.GetValue(r, _ => new ConAcc());
        a.BuiltUnits += builtUnits; a.Paid += cost;
    }
    /// <summary>(289) 助走の月の分は数えない（<see cref="ResetQuarter"/> と同じ扱い）。</summary>
    static void ClearCons(World w)
    {
        foreach (var r in w.Regions) if (ConAccs.TryGetValue(r, out var a)) { a.Judged = 0; a.Desired = a.Budget = a.Planned = a.PlannedUnits = a.BuiltUnits = a.Paid = 0; ClearTerms(a); }
    }
    static string TermMean(ConAcc? a, double? sum) => a is null || a.TermMonths == 0 || sum is null ? "nan" : F(sum.Value / a.TermMonths);
    static void ClearTerms(ConAcc a) { a.TermMonths = a.DesiringMonths = 0; a.TermGain = a.TermSat = a.TermTech = a.TermCrowd = a.TermHeat = a.TermHurdle = a.TermNet = a.TermAbsorb = a.TermJoint = a.TermUnits = a.TermUnitMoney = 0; }
    internal const int ConsMaxQuarter = 160;
    /// <summary>(289) 線D：挙げた国×州×四半期（q が <see cref="ConsMaxQuarter"/> まで）。資本家の貯蓄の在庫（四半期の末）・希望・投資枠・計画・実際に払った額（月あたり）と、州の人口・工業化度・工場の規模。
    /// 限界の自己申告：①貯蓄は四半期の末の値・希望と枠と支払いは四半期の月平均（÷ClsMonths）②<c>investorSavingsStock</c>＝<c>InvestorPopIndex</c> の POP・<c>capitalistSavingsStock</c>＝州の資本家階級の全 POP の和
    /// ③州の人口は全 POP の和（兵士を含む）④工場の規模＝RGO でない生産者の <c>BuiltScale</c> の和。</summary>
    static void ConsRows(Sink sk, ulong seed, int q, World w, WState st)
    {
        if (q > ConsMaxQuarter) return;
        sk.Cons ??= Open("cons", seed);
        if (!sk.ConsHeader) { sk.Cons.WriteLine("quarter\tyear\tnation\tregion\ttier\tactiveFlag\tpopulationK\tinvestorSizeK\tinvestorSavingsStock\tcapitalistSavingsStock\tinvestorEarnedPerMonth\tindustrializationIndex\tproductiveIndustrializationIndex\tfactoryScale\tjudgedMonthsCount\tmonthsCount\tdesiredMoneyPerMonth\tbudgetMoneyPerMonth\tplannedMoneyPerMonth\tplannedUnitsPerMonth\tbuiltUnitsPerMonth\tpaidPerMonth	termMonthsCount	desiringMonthsCount	gainWithTechMean	satMean	techLevelMean	crowdingPremiumMean	financialHeatMean	hurdleMean	netReturnMean	absorbFactorMean	jointStockFactorMean	desiredUnitsMean	unitMoneyMean"); sk.ConsHeader = true; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        double cm = st.ClsMonths > 0 ? st.ClsMonths : double.NaN;
        string nation = NameOf(w);
        foreach (var r in w.Regions)
        {
            double pop = 0, capSav = 0;
            foreach (var p in r.Pops) { pop += p.Size; if (p.ClassId == PopClass.Capitalist) capSav += p.Savings.Value; }
            double invSize = double.NaN, invSav = double.NaN, invEarn = double.NaN;
            if (r.InvestorPopIndex >= 0 && r.InvestorPopIndex < r.Pops.Count) { var ip = r.Pops[r.InvestorPopIndex]; invSize = ip.Size; invSav = ip.Savings.Value; invEarn = ip.EarnedPerMonth.Value; }
            double scale = 0; foreach (var pr in r.Producers) if (!pr.IsRgo) scale += pr.BuiltScale;
            ConAccs.TryGetValue(r, out var a);
            sk.Cons.WriteLine(string.Join('\t', I(q), I(w.Year), nation, r.Name, r.Tier.ToString(), r.State == RegionState.Active ? "1" : "0", F(pop), F(invSize), F(invSav), F(capSav), F(invEarn), F(r.Industrialization), F(r.ProductiveIndustrialization), F(scale),
                I(a?.Judged ?? 0), F(cm), F((a?.Desired ?? 0) / cm), F((a?.Budget ?? 0) / cm), F((a?.Planned ?? 0) / cm), F((a?.PlannedUnits ?? 0) / cm), F((a?.BuiltUnits ?? 0) / cm), F((a?.Paid ?? 0) / cm),
                I(a?.TermMonths ?? 0), I(a?.DesiringMonths ?? 0), TermMean(a, a?.TermGain), TermMean(a, a?.TermSat), TermMean(a, a?.TermTech), TermMean(a, a?.TermCrowd), TermMean(a, a?.TermHeat), TermMean(a, a?.TermHurdle), TermMean(a, a?.TermNet), TermMean(a, a?.TermAbsorb), TermMean(a, a?.TermJoint), TermMean(a, a?.TermUnits), TermMean(a, a?.TermUnitMoney)));
            if (a is not null) { a.Judged = 0; a.Desired = a.Budget = a.Planned = a.PlannedUnits = a.BuiltUnits = a.Paid = 0; ClearTerms(a); }
        }
    }

    // ---------------------------------------------------------------- (292) 線P：港の建設の州ごとの表 ----------------------------------------------------------------

    sealed class PortAcc { public bool Gate; public double InvSav, Need, Intended, StateIntended, PaidCap, PaidState, Built; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, PortAcc> PortAccs = new();
    /// <summary>(292) 港の建設の門を通った州（StepPorts・四半期に1回）：資本家の貯蓄（建設の前）・混雑を解くのに要る分（<see cref="MultiNationSim.PortCapitalNeed"/>・旧式の腕では反事実）・資本家ルートの意図の額・国家ルートの請求×r。挙げた国だけ。</summary>
    internal static void PortGate(World w, Region r, double investorSavings, double need, double intendedCap, double intendedState)
    {
        if (!IsPicked(w, S(w))) return;
        var a = PortAccs.GetValue(r, _ => new PortAcc());
        a.Gate = true; a.InvSav = investorSavings; a.Need = need; a.Intended = intendedCap; a.StateIntended = intendedState;
    }
    /// <summary>(292) 実際に払えた額（PaySales の返り値）と、足された容量。</summary>
    internal static void PortPaid(World w, Region r, double paidCap, double paidState, double builtCapacity)
    {
        if (!IsPicked(w, S(w))) return;
        var a = PortAccs.GetValue(r, _ => new PortAcc());
        a.PaidCap = paidCap; a.PaidState = paidState; a.Built = builtCapacity;
    }
    /// <summary>(292) 線P：挙げた国×四半期×活きた港の州。律速率・潜在量・取扱高・港の容量は四半期の終わりの断面（StepPorts が見た値と同じ瞬間＝次の四半期の最初の清算まで書き換わらない）。
    /// 建設の列は四半期に1回払う額なので <c>…AtQuarterEnd</c>（月あたりにしない）。<c>congestedGate</c>＝StepPorts の建設の門（混雑・蒸気機関・投資家 POP・助走でない）を通った州。門の外の行の建設の列は 0。
    /// 限界の自己申告：①<c>needMoneyAtQuarterEnd</c>は門を通った州だけ（外は 0）②<c>investorSavingsStock</c>は建設の前の投資家 POP の貯蓄③旧式の腕では要る分は反事実（払った額の決定には使っていない）。</summary>
    static void PortRows(Sink sk, ulong seed, int q, World w)
    {
        sk.Port ??= Open("port", seed);
        if (!sk.PortHeader) { sk.Port.WriteLine("quarter	year	nation	region	tradeThrottleIndex	tradePotentialWeight	portThroughput	portCapacityEnd	investorSavingsStock	congestedGate	needMoneyAtQuarterEnd	capitalIntendedMoneyAtQuarterEnd	capitalPaidMoneyAtQuarterEnd	stateIntendedMoneyAtQuarterEnd	statePaidMoneyAtQuarterEnd	builtCapacity"); sk.PortHeader = true; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string nation = NameOf(w);
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active || !r.IsPort) continue;
            PortAccs.TryGetValue(r, out var a);
            bool gate = a is not null && a.Gate;
            double sav = gate ? a!.InvSav : (r.InvestorPopIndex >= 0 && r.InvestorPopIndex < r.Pops.Count ? r.Pops[r.InvestorPopIndex].Savings.Value : double.NaN);
            sk.Port.WriteLine(string.Join('	', I(q), I(w.Year), nation, r.Name, F(r.LastTradeThrottle), F(r.LastTradePotential), F(r.LastPortThroughput), F(r.PortCapacity), F(sav), gate ? "1" : "0",
                F(a?.Need ?? 0), F(a?.Intended ?? 0), F(a?.PaidCap ?? 0), F(a?.StateIntended ?? 0), F(a?.PaidState ?? 0), F(a?.Built ?? 0)));
            if (a is not null) { a.Gate = false; a.InvSav = a.Need = a.Intended = a.StateIntended = a.PaidCap = a.PaidState = a.Built = 0; }
        }
    }

    /// <summary>★(287) 線F1（英の開幕の歳入を史実へ）：土地税の段が差し戻されたかと、土地税が触る権力の取り分を見る列（国×四半期の行の末尾・四半期の終わりの断面）。
    /// <c>landTaxEndShare</c>＝<see cref="World.LandTax"/>（段1＝0.133・段2＝0.267）・<c>landTaxTargetEndShare</c>＝政策の目標・<c>polityCode</c>＝<c>(int)Polity</c>・
    /// <c>privilegeIntactIndex</c>＝<see cref="PowerShares.PrivilegeIntact"/>（免税の項が土地税で動く）・<c>parliamentGripIndex</c>＝<see cref="PowerShares.ParliamentGrip"/>（常設歳入の分母が土地税で動く）。
    /// 読むだけ：どちらの関数も World の列挙値と数値を読む純粋な式（書く経路・乱数・メモ化の新しい位置は無い。ParliamentGrip が読む名目GDP は、同じ行の nominalGdpPerYear が同じ時点で読んだ値）。</summary>
    static List<(string Name, string Value)> Extras287(World w)
    {
        var cols = new List<(string, string)>();
        cols.Add(("landTaxEndShare", F(w.LandTax))); cols.Add(("landTaxTargetEndShare", F(w.Policy.LandTaxTarget)));
        cols.Add(("polityCode", ((int)w.Polity).ToString(System.Globalization.CultureInfo.InvariantCulture)));
        cols.Add(("privilegeIntactIndex", F(PowerShares.PrivilegeIntact(w)))); cols.Add(("parliamentGripIndex", F(PowerShares.ParliamentGrip(w))));
        return cols;
    }

    // ---------------------------------------------------------------- 月末（Simulation.MonthSettle の終わり） ----------------------------------------------------------------

    /// <summary>月の決算の終わり：階級ごとの所得と支出を四半期の合計へ足し、挙げた国は財政の1行を溜める（台帳の差分は前の月の決算の終わりから）。</summary>
    internal static void MonthClose(World w)
    {
        var st = S(w);
        if (st.Armed && IsPicked(w, st)) PxMonth(w, st);   // (280) 助走の月も読む（下の Active の判定の前）
        if (!st.Active || !st.Armed) { st.Armed = false; return; }
        st.Armed = false;
        foreach (var r in w.Regions) foreach (var p in r.Pops) { int c = (uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1; st.ClsEarn[c] += p.EarnedPerMonth.Value; st.ClsPaid[c] += p.PaidPerMonth.Value; }
        st.ClsMonths++;
        if (st.GateOpenNow >= 0) { st.QGateObs++; if (st.GateOpenNow == 0) st.QGateClosed++; }
        if (st.Spare0 < 0) { st.QShortMonths++; st.QShortfall += -st.Spare0; }
        if (st.PayRate < 1) st.QRUnder1Months++;
        AccumulateGateAround(st);   // (281) 借入の門の周り
        st.QBorrowed += double.IsNaN(st.Borrowed) ? 0 : st.Borrowed; st.QInterest += w.LastInterestPerMonth.Value; st.QRevenue += w.LastRevenuePerMonth.Value;
        st.QDirect += double.IsNaN(st.RevDirectNet) ? 0 : st.RevDirectNet; st.QIndirect += double.IsNaN(st.RevIndirect) ? 0 : st.RevIndirect;
        var led = w.TreasuryLedger;
        AccumulateAdmin(w, st);
        AccumulateAdminNeed(st);   // (265) 行政の必要額の反事実 A′
        AccumulateCap(st);          // (265) 段5 の上限の拘束
        if (IsPicked(w, st) && st.HaveLedger)
        {
            var sb = new System.Text.StringBuilder();
            FiscalRow(w, st, sb, header: false);
            st.Rows.Add(sb.ToString());
        }
        st.PrevTreasury = w.Treasury.Value; Array.Copy(led, st.PrevLedger, NCat); st.HaveLedger = true;
        st.BudMul = st.BudCover = 0; st.BudSpendable = st.BudLastRev = double.NaN;   // 月の予算の位置で溜めた基数を次の月へ持ち越さない
        st.BudClaim = st.BudNeedB = st.BudMinB = st.BudNeedP = st.BudMinP = 0; st.BudOverRegions = 0; st.AdminClaims.Clear();   // (265)
    }

    static void FiscalRow(World w, WState st, System.Text.StringBuilder sb, bool header)
    {
        void C(string name, double v) { if (header) sb.Append(name).Append('\t'); else sb.Append(F(v)).Append('\t'); }
        void T(string name, string v) { if (header) sb.Append(name).Append('\t'); else sb.Append(v).Append('\t'); }
        T("monthIndex", ((int)(w.Tick / Calendar.DaysPerMonth)).ToString(System.Globalization.CultureInfo.InvariantCulture));
        T("nation", NameOf(w)); C("year", w.Year);
        for (int i = 0; i < (int)Item.Count; i++) C(ItemNames[i], st.Claim[i]);
        double itemSum = 0; for (int i = 0; i < (int)Item.Count; i++) itemSum += st.Claim[i];
        C("claimTotalPerMonth", st.ClaimsTotal); C("claimItemGapPerMonth", st.ClaimsTotal - itemSum);
        // (259) 行政の請求の2つの式：Fiscal の adminClaim（claimAdminPerMonth＝Simulation の州の予算の和とは別の式）・その基数・官僚の居る州の割増・行政の充足
        C("adminClaimFiscalPerMonth", w.LastAdminClaimPerMonth.Value); C("adminSpendableAtBudgetPerMonth", st.BudSpendable); C("adminLastRevenueAtBudgetPerMonth", st.BudLastRev);
        C("adminSimMeanMulShare", st.BudMul); C("adminBureaucratCoverShare", st.BudCover); C("adminFiscalMeanMul", Fiscal.AdminDistanceMean(w)); C("adminFillShare", AdminFillShare(w));
        // (265) 行政の必要額（予算の時点・購買の時点）と、請求を必要額で切った額（州ごとに min してから和）。A′ の静的な反事実はこの4列と spareBeforeBorrowPerMonth・borrowedPerMonth から読み手が出す
        C("adminNeedAtBudgetPerMonth", st.BudNeedB); C("adminClaimCappedAtBudgetPerMonth", st.BudMinB); C("adminNeedAtPurchasePerMonth", st.BudNeedP); C("adminClaimCappedAtPurchasePerMonth", st.BudMinP); C("adminOverNeedRegionsCount", st.BudOverRegions);
        C("treasuryAtClaimStock", st.Treasury0); C("revenueBasePerMonth", w.LastRevenuePerMonth.Value); C("payableBeforeBorrowStock", st.Payable0); C("spareBeforeBorrowPerMonth", st.Spare0);
        C("borrowShortfallPerMonth", st.Shortfall); C("borrowTicketPerMonth", st.Ticket); C("borrowDomesticPoolStock", st.DomPool); C("borrowForeignPoolStock", st.ForPool);
        C("borrowedPerMonth", st.Borrowed);
        T("borrowBinder", st.BorrowCode switch { 0 => "gateClosed", 1 => "full", 2 => "ticket", 3 => "pool", 4 => "none", _ => "noShortfall" });
        C("borrowCooldownMonths", w.BorrowCooldownMonths); C("interestToRevenueShareNow", w.LastRevenuePerMonth.Value > 0 ? w.LastInterestPerMonth / w.LastRevenuePerMonth : double.NaN);
        C("devaluedPerMonth", st.Devalued); C("payableAfterBorrowStock", st.Payable1); C("payRateShare", st.PayRate);
        C("borrowGateOpenFlag", st.GateOpenNow < 0 ? double.NaN : st.GateOpenNow); C("directTaxNetPerMonth", st.RevDirectNet); C("indirectRevenuePerMonth", st.RevIndirect);
        C("convertibilityShare", w.Convertibility); C("onGold", w.OnGoldStandard ? 1 : 0); C("govDebtStock", w.GovDebt.Value);
        C("govBondRatePerYear", w.GovBondRatePerYear.Value); C("govAvgCouponPerYear", w.GovAvgCouponPerYear.Value); C("insolvencyMonths", w.InsolvencyQuarters); C("defaultCount", w.SovereignDefaultCount);
        var led = w.TreasuryLedger; double dSum = 0;
        for (int k = 0; k < NCat; k++) { double d = led[k] - st.PrevLedger[k]; dSum += d; C("ledger" + ((TreasuryCat)k) + "PerMonth", d); }
        double dT = w.Treasury.Value - st.PrevTreasury;
        C("treasuryEndStock", w.Treasury.Value); C("treasuryDeltaPerMonth", dT); C("ledgerSumPerMonth", dSum); C("ledgerResidualPerMonth", dT - dSum);
        // (281) 線O0：門が読んだ利払い÷歳入（月の決算の終わりの interestToRevenueShareNow とは別＝月初の値）・限界に届いたか・切り下げで埋める前の払えない額÷請求・切り下げ÷請求
        double unpaid = UnpaidBeforeDevaluationShare(st.ClaimsTotal, st.Payable0, st.Borrowed);
        C("interestToRevenueShareAtGate", st.GateRev > 0 ? st.GateInt / st.GateRev : double.NaN); C("interestAtLimitFlag", st.LimitNow < 0 ? double.NaN : st.LimitNow);
        C("unpaidBeforeDevaluationShare", unpaid); C("devaluedToClaimShare", double.IsNaN(unpaid) ? double.NaN : (st.Devalued > 0 ? st.Devalued : 0) / st.ClaimsTotal);
        if (sb.Length > 0) sb.Length--;   // 末尾のタブ
    }

    // ---------------------------------------------------------------- 四半期の末・断面（SelfPlay.Run が呼ぶ） ----------------------------------------------------------------

    // ---------------------------------------------------------------- (280) 線X：物価の更新の鎖 ----------------------------------------------------------------

    const int PxMonthGameMax = 24, PxGoodsGameMax = 48;

    static string PxI(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
    // (366) 貯蓄・所得・支出の階級別の列の数。犯罪者（階級 14）の列は CriminalClassEnabled>0 の世界にだけ出す＝犯罪者の POP が無い世界の出力（表の頭の列も）は NC を 14→15 にしても 1 バイトも変わらない。読み手（an_e7・an_x280）は 0..13 を名前で引く。
    static int PxCols(World w) => w.Coef.CriminalClassEnabled > 0 ? NC : NC - 1;

    /// <summary>(280) 物価の財の表の対象：主食・日用品と、専売の梯子の最初の群（World.ExciseGoods＝ビール・蒸留酒・ワイン・煙草・茶・コーヒー・石鹸）。名前で引く（番号でなく Catalog の財から・README §1-9）。</summary>
    static bool PxGoodPicked(int g) => Catalog.Goods[g].Tier == GoodTier.Staple || Catalog.Goods[g].Tier == GoodTier.Daily || System.Array.IndexOf(World.ExciseGoods, g) >= 0;

    /// <summary>(280) 州ごとの 市場価格 ÷ (参照価格 × 州の物価水準) の平均（主食／日用品）＝需給の因子 S/D（ProductionDiag の恒等式1）。物価水準 P の動きと需給の動きを分ける。</summary>
    static void PxRel(World w, out double staple, out double daily)
    {
        var lS = new List<double>(); var lD = new List<double>();
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            for (int g = 0; g < r.Markets.Length && g < Catalog.Goods.Length; g++)
            {
                var t = Catalog.Goods[g].Tier; if (t != GoodTier.Staple && t != GoodTier.Daily) continue;
                double den = Catalog.RefPrices[g] * r.PriceLevel; if (!(den > 1e-12)) continue;
                double rel = r.Markets[g].Price / den;
                if (t == GoodTier.Staple) lS.Add(rel); else lD.Add(rel);
            }
        }
        staple = Median(lS); daily = Median(lD);
    }

    /// <summary>(280) 中央値（空は NaN）。州の因子の平均は価格の天井に張り付いた少数の州で引かれる＝中央で読む。</summary>
    static double Median(List<double> xs)
    {
        if (xs.Count == 0) return double.NaN;
        xs.Sort(); int m = xs.Count / 2;
        return xs.Count % 2 == 1 ? xs[m] : 0.5 * (xs[m - 1] + xs[m]);
    }

    /// <summary>(280) 月の決算で、挙げた国の貨幣と所得の月次の行（助走の月を含む）を溜め、四半期の更新が読む所得・支出の和を足す。読むだけ（GdpPerMonth は読まない）。</summary>
    static void PxMonth(World w, WState st)
    {
        var sav = new double[NC]; var earn = new double[NC]; var paid = new double[NC]; double total = 0;
        foreach (var r in w.Regions) foreach (var p in r.Pops)
        {
            int c = (uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1;
            sav[c] += p.Savings.Value; total += p.Savings.Value; earn[c] += p.EarnedPerMonth.Value; paid[c] += p.PaidPerMonth.Value;
        }
        for (int c = 0; c < NC; c++) { st.PxEarn[c] += earn[c]; st.PxPaid[c] += paid[c]; }
        st.PxMonths++;
        bool warm = w.WarmupFrozen;
        if (!warm && st.PxMonthGameN >= PxMonthGameMax) return;   // 月ごとの行は助走の全部と試合の最初の2年だけ（出力の大きさ）
        PxRel(w, out double stapleRel, out double dailyRel);
        var cols = new List<(string Name, string Value)>();
        void C(string name, double v) => cols.Add((name, F(v)));
        cols.Add(("time", (warm ? "wm" : "qm") + PxI(warm ? st.PxMonthWarmN++ : st.PxMonthGameN++))); cols.Add(("nation", NameOf(w))); C("year", w.Year);
        C("priceLevelIndex", w.PriceLevel); C("broadMoneyStock", total); C("treasuryStock", w.Treasury.Value); C("outstandingLoansStock", w.OutstandingLoans.Value);
        C("convertibilityShare", w.Convertibility); C("stapleRelPriceMedianRatio", stapleRel); C("dailyRelPriceMedianRatio", dailyRel);
        for (int c = 0; c < PxCols(w); c++) C("savingsStockClass" + PxI(c), sav[c]);
        for (int c = 0; c < PxCols(w); c++) C("earnedPerMonthClass" + PxI(c), earn[c]);
        for (int c = 0; c < PxCols(w); c++) C("paidPerMonthClass" + PxI(c), paid[c]);
        st.PxMonthHdr ??= string.Join('\t', cols.Select(x => x.Name));
        st.PxMonthRows.Add(string.Join('\t', cols.Select(x => x.Value)));
    }

    /// <summary>(280) <see cref="World.UpdatePriceLevel"/> の1回（助走の四半期を含む）：物価の更新の鎖の全項と、更新の直後の貨幣の構成・階級ごとの貯蓄と所得・財ごとの価格を溜める。
    /// <paramref name="early"/>＝趨勢の基準を取るだけで物価は動かなかった最初の1回。読むだけ。</summary>
    internal static void PriceStep(World w, double broad, double baseM, double money, double gdp, double ratio, double trend0, double cyclical, double pBefore, double pEma, double pFiat, double convTarget, double fiatP, bool early)
    {
        var st = S(w);
        if (!IsPicked(w, st)) return;
        bool warm = w.WarmupFrozen;
        string time = (warm ? "w" : "q") + PxI(warm ? st.PxWarmN++ : st.PxGameN++);
        var sav = new double[NC];
        foreach (var r in w.Regions) foreach (var p in r.Pops) { int c = (uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1; sav[c] += p.Savings.Value; }
        var cols = new List<(string Name, string Value)>();
        void C(string name, double v) => cols.Add((name, F(v)));
        cols.Add(("time", time)); cols.Add(("nation", NameOf(w))); C("year", w.Year); cols.Add(("event", early ? "first" : "update"));
        C("priceLevelBeforeIndex", pBefore); C("priceLevelAfterEmaIndex", pEma); C("priceLevelAfterFiatIndex", pFiat); C("priceLevelAfterIndex", w.PriceLevel); C("costPriceLevelAfterIndex", w.CostPriceLevel);
        C("expectedInflationPerYear", w.ExpectedInflationPerYear.Value); C("fiatPressureBeforeShare", fiatP);
        C("broadMoneyStock", broad); C("baseMoneyStock", baseM); C("treasuryStock", w.Treasury.Value); C("outstandingLoansStock", w.OutstandingLoans.Value);
        C("convertibilityTargetShare", convTarget); C("convertibilityShare", w.Convertibility); C("moneyUsedStock", money); C("gdpRealPerMonth", gdp);
        C("moneyToGdpMonths", ratio); C("moTrendBeforeMonths", trend0); C("moTrendAfterMonths", w.MoTrendForInstruments); C("cyclicalRatio", cyclical);
        C("cumulativeTradeFlowStock", w.CumulativeTradeFlow.Value); C("cumulativeGoldMintedStock", w.CumulativeGoldMinted.Value); C("cumulativeFiatIssuedStock", w.CumulativeFiatIssued.Value); C("treasuryResetSinceLastRowStock", st.PxResetSince); C("treasuryResetCount", st.PxResetCount);
        st.PxResetSince = 0; st.PxResetCount = 0;
        C("monthsInQuarterCount", st.PxMonths);
        for (int c = 0; c < PxCols(w); c++) C("savingsStockClass" + PxI(c), sav[c]);
        for (int c = 0; c < PxCols(w); c++) C("earnedPerMonthClass" + PxI(c), st.PxMonths > 0 ? st.PxEarn[c] / st.PxMonths : double.NaN);
        for (int c = 0; c < PxCols(w); c++) C("paidPerMonthClass" + PxI(c), st.PxMonths > 0 ? st.PxPaid[c] / st.PxMonths : double.NaN);
        st.PxHdr ??= string.Join('\t', cols.Select(x => x.Name));
        st.PxRows.Add(string.Join('\t', cols.Select(x => x.Value)));
        Array.Clear(st.PxEarn); Array.Clear(st.PxPaid); st.PxMonths = 0;
        // 財ごとの価格：国の平均（州の単純平均）・参照価格×州の物価水準で割った需給の因子・四半期の量
        string nm = NameOf(w);
        for (int g = 0; g < Catalog.Goods.Length; g++)
        {
            if (!warm && st.PxGameN > PxGoodsGameMax) break;   // 財ごとの行は助走の全部と試合の最初の12年だけ（出力の大きさ）
            if (!PxGoodPicked(g)) continue;
            double prod = 0, dem = 0, sold = 0, shortage = 0, imp = 0, exp = 0, stock = 0, price = 0, lvl = 0; int n = 0; var rels = new List<double>();
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active || g >= r.Markets.Length) continue;
                var m = r.Markets[g];
                prod += m.LastProduced; dem += m.LastDemand; sold += m.LastSold; shortage += m.LastShortage; imp += m.LastImport; exp += m.LastExport; stock += m.Stock; price += m.Price; lvl += r.PriceLevel; n++;
                double den = Catalog.RefPrices[g] * r.PriceLevel; if (den > 1e-12) rels.Add(m.Price / den);
            }
            if (n == 0) continue;
            bool excise = System.Array.IndexOf(World.ExciseGoods, g) >= 0;
            st.PxGoodsRows.Add(string.Join('\t', time, nm, PxI(w.Year), Catalog.Goods[g].Name, Catalog.Goods[g].Tier.ToString(), excise ? "1" : "0", F(g < w.StateMonopolyMargins.Length ? w.StateMonopolyMargins[g] : double.NaN), PxI(n),
                F(Catalog.RefPrices[g]), F(price / n), F(lvl / n), F(Median(rels)), F(rels.Count > 0 ? rels.Max() : double.NaN), F(prod), F(dem), F(sold), F(shortage), F(imp), F(exp), F(stock)));
        }
    }

    /// <summary>(280) 助走の四半期の終わりに国庫を助走の前の値へ戻した額（呼び手＝<see cref="World.RebaseTreasury"/>）。戻す前の国庫 <paramref name="before"/> と戻した値 <paramref name="to"/> の差＝基礎貨幣の創出（正）か消滅（負）。</summary>
    internal static void TreasuryRebased(World w, double before, double to)
    {
        var st = S(w);
        if (!IsPicked(w, st)) return;
        st.PxResetSince += to - before; st.PxResetCount++;
    }

    /// <summary>(280) 溜めた行を3本の表（<c>money_s</c>・<c>pxgoods_s</c>・<c>pxmonth_s</c>）へ書く。</summary>
    static void FlushPx(Sink sk, ulong seed, WState st)
    {
        if (st.PxRows.Count > 0)
        {
            sk.Money ??= Open("money", seed);
            if (!sk.MoneyHeader) { sk.Money.WriteLine(st.PxHdr); sk.MoneyHeader = true; }
            foreach (var row in st.PxRows) sk.Money.WriteLine(row);
            st.PxRows.Clear();
        }
        if (st.PxGoodsRows.Count > 0)
        {
            sk.PxGoods ??= Open("pxgoods", seed);
            if (!sk.PxGoodsHeader) { sk.PxGoods.WriteLine("time\tnation\tyear\tgoodName\ttier\texciseFlag\tmonopolyMarginShare\tactiveRegions\trefPriceIndex\tpriceMean\tregionPriceLevelMeanIndex\tpriceOverRefTimesLevelMedianRatio\tpriceOverRefTimesLevelMaxRatio\tlastProducedQty\tlastDemandQty\tlastSoldQty\tlastShortageQty\tlastImportQty\tlastExportQty\tstockQty"); sk.PxGoodsHeader = true; }
            foreach (var row in st.PxGoodsRows) sk.PxGoods.WriteLine(row);
            st.PxGoodsRows.Clear();
        }
        if (st.PxMonthRows.Count > 0)
        {
            sk.PxMonth ??= Open("pxmonth", seed);
            if (!sk.PxMonthHeader) { sk.PxMonth.WriteLine(st.PxMonthHdr); sk.PxMonthHeader = true; }
            foreach (var row in st.PxMonthRows) sk.PxMonth.WriteLine(row);
            st.PxMonthRows.Clear();
        }
    }

    static System.IO.StreamWriter Open(string kind, ulong seed)
    {
        System.IO.Directory.CreateDirectory(Dir);   // 環境変数が存在しないディレクトリを指しても試合ごとに落ちない（(244) 反証役 vi1#7）
        return new(System.IO.Path.Combine(Dir, $"{kind}_s{seed}.tsv"), false, new System.Text.UTF8Encoding(false), 1 << 16);
    }

    /// <summary>断面（開幕 <c>open</c>・助走の直後 <c>warm</c>）：全ての国の階級と在庫の行。開幕は GDP を読まない（構築直後に和を取らない）。</summary>
    internal static void Phase(MultiNationSim sim, ulong seed, string time)
    {
        var sk = SinkOf(sim, seed);
        if (time == "warm") foreach (var n in sim.Nations) { var wst = S(n.World); wst.WarMonths = 0; wst.CivilWarMonths = 0; ResetQuarter(wst); wst.SavLedger.Clear(); ClearCons(n.World); }   // 助走の月は数えない（旧の注入も助走の直後に0へ戻していた）
        foreach (var n in sim.Nations) { ClassRows(sk, seed, time, n.World, S(n.World), flows: false); StockRow(sk, seed, time, n.World, readGdp: time != "open"); }
        if (time == "warm") foreach (var n in sim.Nations) { foreach (var rg in n.World.Regions) FlowOf(rg).HabitSoldSum = 0; var wst2 = S(n.World); wst2.OpTensionAdded = 0; wst2.OpTensionPairMonths = 0; wst2.OpTensionLevelMax = 0; CeClear(wst2); PAcc(n.World).Clear(n.World); if (wst2.FmA is { } fma) { fma.ClearNet(); fma.ClearArmy(); } }   // (342) 助走の月は数えない
        if (time == "warm") TrClear(sim);   // (380) 助走の月の成約を数えない
        foreach (var n in sim.Nations) FlushPx(sk, seed, S(n.World));   // (280) 助走の物価の鎖の行
    }

    /// <summary>四半期の末：挙げた国は毎四半期・全ての国は年末（q%4==3）。財政の月次の行を書き出し、階級の所得と支出の四半期の合計をゼロへ戻す。</summary>
    internal static void Quarter(MultiNationSim sim, ulong seed, int q)
    {
        var sk = SinkOf(sim, seed);
        bool yearEnd = q % Calendar.QuartersPerYear == Calendar.QuartersPerYear - 1;
        string time = "q" + q.ToString(System.Globalization.CultureInfo.InvariantCulture);
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World; var st = S(w);
            bool picked = IsPicked(w, st);
            if (st.Rows.Count > 0)
            {
                sk.Fiscal ??= Open("fiscal", seed);
                if (!sk.FiscalHeader) { var h = new System.Text.StringBuilder(); FiscalRow(w, st, h, header: true); sk.Fiscal.WriteLine(h.ToString()); sk.FiscalHeader = true; }
                foreach (var row in st.Rows) sk.Fiscal.WriteLine(row);
                st.Rows.Clear();
            }
            if (picked || yearEnd) { ClassRows(sk, seed, time, w, st, flows: true); StockRow(sk, seed, time, w, readGdp: true); }
            if (picked) FlowRows(sk, seed, q, w, st);   // (262) 貯蓄の書き手の台帳（階級の読む位置＝同じ瞬間）
            FlushPx(sk, seed, st);   // (280) 物価の鎖の行（挙げた国だけが溜めている）
            NationRow(sk, seed, q, i, w, st, yearEnd);
            SpendFold(sk, w, st, q, picked);   // (286) 線Q：支払いの積算を窓の区切りへ（階級の所得と支出を戻す前）
            PopFold(sk, seed, w, st, q, picked, yearEnd);   // (361) 人口の表（籠は ce_s の積算を読む＝CeFold が戻す前）
            FmFold(sk, seed, w, st, q, picked, yearEnd);    // (371) 流れM の計器（純支持の項と陸の必要量の組成・MmNet などを ResetQuarter が戻す前に読む）
            ScalarsFold(sk, seed, w, q, picked, yearEnd);   // (373) 政治のスカラーの表
            BurFold(sk, seed, w, st, q);   // (389) AI の官僚制度の判断の表
            CoupFold(sk, seed, w, st, q);   // (393) 軍の力の入口の表
            AnchorFold(sk, seed, w, q, picked, yearEnd);    // (369) 扶養力の錨の表
            CapFold(sk, seed, w, st, q, picked, yearEnd);   // (390) 工場の能力の内訳の表（四半期の末の断面・読むだけ）
            IdeologyFold(sk, seed, w, st, q, picked, yearEnd);   // (390) 思想の割合の表（National と素の平均）
            SecFold(sk, seed, w, st, q, picked, yearEnd);   // (395) 州の分離の内訳の表（CheckSecession が置いた行）
            CeFold(sk, seed, w, st, q, picked, yearEnd);   // (345) 消費の入口の表（階級の所得と支出を戻す前）
            if (picked) SvcRow(sk, q, w);
            if (picked) ConsRows(sk, seed, q, w, st);   // (289) 線D：資本家の建設の州ごとの表（階級の所得と支出を戻す前）
            if (picked) PortRows(sk, seed, q, w);       // (292) 線P：港の建設の州ごとの表
            Array.Clear(st.ClsEarn); Array.Clear(st.ClsPaid); st.ClsMonths = 0; st.WarMonths = 0; st.CivilWarMonths = 0; ResetQuarter(st);
            st.QGateObs = st.QGateClosed = st.QShortMonths = st.QRUnder1Months = 0; st.QInterest = st.QRevenue = st.QDirect = st.QIndirect = st.QShortfall = st.QBorrowed = 0;
            st.FacStarted = st.FacStartedClosed = st.FacDone = st.FacDoneClosed = st.FacDoneClosedAtStart = 0;
        }
        RegionRows(sk, seed, q, sim);
        OpiumRows(sk, seed, q, sim);    // (342) アヘンの表
        SiteTables(sk, seed, q, sim);   // (316) 産地の判定の行と世界の金銀の行
        DiscTable(sk, seed, q, sim);    // (320) 発見の表の行の出来事と蒸気機関・機関車の軌道
        TransportFold(sk, seed, q, sim);   // (380) 輸送の段0 の計器（成約の蓄えを年の終わりに行にする）
        foreach (var cp in sim.Diplomacy.Chokepoints)   // (271) 運河の開通の四半期（最初に開通を見た四半期の末）
            if (cp.IsCanal && cp.IsOpen && !sk.CanalOpened.ContainsKey(cp.Id)) sk.CanalOpened[cp.Id] = (q, sim.Nations.Count > 0 ? sim.Nations[0].World.Year : 0);
    }

    /// <summary>試合の終わり（例外の経路も）：書き出しを閉じる。</summary>
    internal static void Close(MultiNationSim sim)
    {
        if (!Sinks.TryGetValue(sim, out var sk)) return;
        if (Dips.TryGetValue(sim.Diplomacy, out var dip)) WarTables(sk, dip);
        CanalTable(sk, sim);
        SpendTable(sk);   // (286)
        CeTable(sk);      // (345)
        BurScanTable(sk, sim);   // (389)
        TransportClose(sk, sim);   // (380) 年の途中で終わった残り
        sk.Site?.Dispose(); sk.Site = null; sk.Metal?.Dispose(); sk.Metal = null;   // (316)
        sk.Disc?.Dispose(); sk.Disc = null;   // (320)
        sk.DiscContact?.Dispose(); sk.DiscContact = null;   // (328)
        sk.DiscFlow?.Dispose(); sk.DiscFlow = null;   // (360)
        sk.Svc?.Dispose(); sk.Svc = null;
        sk.Cons?.Dispose(); sk.Cons = null;   // (289)
        sk.Port?.Dispose(); sk.Port = null;   // (292)
        sk.Opium?.Dispose(); sk.Opium = null;   // (342)
        sk.Ce?.Dispose(); sk.Ce = null;   // (345)
        sk.PopS?.Dispose(); sk.PopS = null;   // (361)
        sk.Fm?.Dispose(); sk.Fm = null;       // (371)
        sk.Sca?.Dispose(); sk.Sca = null;     // (373)
        sk.TrBin?.Dispose(); sk.TrBin = null; sk.TrRoute?.Dispose(); sk.TrRoute = null; sk.TrNat?.Dispose(); sk.TrNat = null; sk.TrStat?.Dispose(); sk.TrStat = null;   // (380)
        sk.Bur?.Dispose(); sk.Bur = null;   // (389)
        sk.Coup?.Dispose(); sk.Coup = null;   // (393)
        sk.Anc?.Dispose(); sk.Anc = null;   // (369)
        sk.Cap?.Dispose(); sk.Cap = null; sk.Ide?.Dispose(); sk.Ide = null;   // (390)
        sk.Sec?.Dispose(); sk.Sec = null;   // (395)
        sk.Fiscal?.Dispose(); sk.Class?.Dispose(); sk.Stock?.Dispose(); sk.Nation?.Dispose(); sk.Region?.Dispose(); sk.Migration?.Dispose(); sk.Flow?.Dispose(); sk.Goods?.Dispose(); sk.Money?.Dispose(); sk.PxGoods?.Dispose(); sk.PxMonth?.Dispose(); sk.Fiscal = sk.Class = sk.Stock = sk.Nation = sk.Region = sk.Migration = sk.Flow = sk.Goods = sk.Money = sk.PxGoods = sk.PxMonth = null;
    }

    // ---------------------------------------------------------------- 州の人口と移動（州の空洞化の計器・(255)）----------------------------------------------------------------

    /// <summary>州ごとの四半期の人口の流れ（千人）。四半期の末に <c>region</c> と <c>migration</c> の行へ書いて0へ戻す。</summary>
    sealed class RegionFlow
    {
        public double MigrationOutK, MigrationInK, SettlerInK, CrossBorderOutK, CrossBorderInK;
        public double HabitSoldSum;   // ★(342) 州のアヘンの月ごとの販売量の和（四半期の末に opium_s へ書いて 0 へ戻す）
        public double TheftPoolSum, TheftRecipientsEarnedSum;   // ★(323) 四半期の月ごとの窃盗の額の和・その月の受け手（困窮大衆）の前の月の所得の和（Crime.Step の Transfer の直後・読むだけ）
        public Dictionary<Region, double>? MigrationTo;   // 出る州→入る州ごとの量（四半期の合計・州の間の移民だけ）
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, RegionFlow> Flows = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Pop, double[]> SettlerOut = new();   // 入植で本国の pop から出た量（四半期の合計）＝出た pop が居る州の行に書く
    static RegionFlow FlowOf(Region r) => Flows.GetValue(r, _ => new RegionFlow());

    /// <summary>★(323) 窃盗（<see cref="Crime.Step"/> の移し方の直後・読むだけ）：その州のその月の盗まれた額 <paramref name="pool"/> と、受け手（困窮大衆＝闘争度×(1−満足度) の重みが正の POP）の前の月の所得の和を、四半期の和として積む。
    /// 州の中で閉じる移転（盗む側も受ける側も同じ州）なので、州ごとの行で〈窃盗の受け取り÷受け手の所得〉が読める（<c>flow_s</c> は国×階級で州を分けない）。</summary>
    internal static void Theft(Region r, double pool, List<Pop> recipients)
    {
        var f = FlowOf(r); f.TheftPoolSum += pool;
        double e = 0; foreach (var p in recipients) e += Calendar.OneMonthOf(p.LastEarnedPerMonth).Value;
        f.TheftRecipientsEarnedSum += e;
    }

    /// <summary>州の間の移民（<c>Territory.MoveRegion</c> の運搬の直後・読むだけ）。</summary>
    internal static void Migrated(Region src, Region dst, double sizeK)
    {
        var f = FlowOf(src); f.MigrationOutK += sizeK; FlowOf(dst).MigrationInK += sizeK;
        var to = f.MigrationTo ??= new();
        to[dst] = to.GetValueOrDefault(dst) + sizeK;
    }
    /// <summary>入植（本国の pop → 植民地格の州・<c>Colonial</c> の運搬の直後・読むだけ）。</summary>
    internal static void Settled(Pop from, Region dst, double sizeK)
    {
        SettlerOut.GetValue(from, _ => new double[1])[0] += sizeK; FlowOf(dst).SettlerInK += sizeK;
    }
    /// <summary>越境の移民（<c>MultiNationSim.MoveMigrants</c> の運搬の前・読むだけ）。★(257) 反証役 C1 低#4：足すのは**求めた流量 flow**＝実際に動いた量（Pop.Blend の戻り値 moved）ではない（旧の注入と同じ定義・moved が flow より小さい月は差が出る）。</summary>
    internal static void CrossBorder(Region src, Region dst, double flowK)
    {
        FlowOf(src).CrossBorderOutK += flowK; FlowOf(dst).CrossBorderInK += flowK;
    }

    /// <summary>州ごとの行（<c>region</c>＝全ての国の全ての州・四半期の末）と州の間の移民の組（<c>migration</c>＝出る州→入る州の量）。州の空洞化（F1 の線）の読み手 <c>scripts/instruments/hol/an_h2.py</c> が読む。
    /// 機会の因子（賃金・雇用・押し出し・住居）は旧の注入（<c>u4e/inj_ghost.py</c>）の定義のまま＝雇用は兵士を含む全 pop の失業・押し出しの不穏は全 pop の闘争度の人口加重で、
    /// 実際の機会（<see cref="Territory.Opportunity"/>＝兵士を除く・<c>Politics.AvgRegionMilitancy</c>）とは違う＝因子の積は <c>opportunityIndex</c> と一致しない（機会そのものは本物）。</summary>
    static void RegionRows(Sink sk, ulong seed, int q, MultiNationSim sim)
    {
        sk.Region ??= Open("region", seed);
        sk.Migration ??= Open("migration", seed);
        if (!sk.RegionHeader)
        {
            sk.Region.WriteLine("quarter\tyear\tnationIndex\tnation\tregion\ttier\tactiveFlag\tpopulationK\truralPopulationK\topportunityIndex\tholdingFactor\twageFactor\tlandFactor\temploymentShare\tpushFactor\thousingFactor\twagePremium\tarableLand\t"
                + "migrationOutKPerQuarter\tmigrationInKPerQuarter\tsettlerOutKPerQuarter\tsettlerInKPerQuarter\tcrossBorderOutKPerQuarter\tcrossBorderInKPerQuarter\twarIntensity\tfoodFillShare\t"
                + "theftPoolPerMonth\ttheftRecipientsEarnedPerMonth\tearnedPerMonth");
            sk.Migration.WriteLine("quarter\tfromRegion\ttoRegion\tmovedKPerQuarter");
            sk.RegionHeader = true;
        }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int year = sim.Nations[0].World.Year;
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World; var c = w.Coef; string nation = NameOf(w);
            foreach (var r in w.Regions)
            {
                double pop = 0, rural = 0, unemp = 0, unrest = 0, foodMet = 0, foodNeed = 0, settlerOut = 0, earned = 0;
                foreach (var p in r.Pops)
                {
                    pop += p.Size; if (!p.IsUrban) rural += p.Size;
                    unemp += p.Size * p.UnemploymentRate.Value; unrest += p.Size * p.Militancy; foodMet += p.FoodMet; foodNeed += p.FoodNeed;   earned += Calendar.OneMonthOf(p.LastEarnedPerMonth).Value;   // ★(323)
                    if (SettlerOut.TryGetValue(p, out var po)) { settlerOut += po[0]; po[0] = 0; }
                }
                bool active = r.State == RegionState.Active;
                double floor = Territory.OpportunityFactorFloor;
                double wage = Math.Max(floor, 1.0 + c.OpportunityWageWeight * (r.WagePremium - 1.0));
                double emp = pop >= 1.0 ? 1.0 - unemp / pop : 1.0;
                double push = pop >= 1.0 ? Math.Max(floor, 1.0 - c.PushUnrestWeight * (unrest / pop) - c.CrimePushWeight * r.CrimeRate) : 1.0;
                double hous = pop >= 1.0 ? Math.Max(floor, 1.0 - c.HousingMigrationFriction * (1.0 - r.HousingFill)) : 1.0;
                var f = FlowOf(r);
                sk.Region.WriteLine(string.Join('\t', I(q), I(year), I(i), nation, r.Name, r.Tier.ToString(), active ? "1" : "0", F(pop), F(rural),
                    F(active ? Territory.Opportunity(r, c) : double.NaN), F(Territory.Holding(r, c)), F(wage), F(Territory.LandFactor(r, rural, c)), F(emp), F(push), F(hous), F(r.WagePremium), F(r.ArableLand),
                    F(f.MigrationOutK), F(f.MigrationInK), F(settlerOut), F(f.SettlerInK), F(f.CrossBorderOutK), F(f.CrossBorderInK), F(r.LastWarIntensity), F(foodNeed > 0 ? foodMet / foodNeed : 1),
                    F(Calendar.PerQuarterToPerMonth(f.TheftPoolSum)), F(Calendar.PerQuarterToPerMonth(f.TheftRecipientsEarnedSum)), F(earned)));   // ★(323) 窃盗の列（四半期の月平均・窃盗の無い州は 0）
                if (f.MigrationTo is { } to)
                {
                    foreach (var kv in to) if (kv.Value > 0) sk.Migration.WriteLine(string.Join('\t', I(q), r.Name, kv.Key.Name, F(kv.Value)));
                    to.Clear();
                }
                f.MigrationOutK = f.MigrationInK = f.SettlerInK = f.CrossBorderOutK = f.CrossBorderInK = 0;   f.TheftPoolSum = f.TheftRecipientsEarnedSum = 0;
            }
        }
    }

    // ---------------------------------------------------------------- (342) アヘンの表（段1・読むだけ）----------------------------------------------------------------

    /// <summary>★(342) 段1：アヘンの表 <c>opium_s</c>（<c>scope</c> 列で2種類の行）。<c>R</c>＝州×四半期（中毒度が正・販売量が正・闇栽培の係数が 1 未満のどれかの州だけ）：中毒度 <c>addictionIndex</c>（<see cref="Region.OpiumAddiction"/>）・
    /// 四半期の月平均の販売量 <c>soldPerMonth</c>・1人あたり <c>soldPerCapitaPerMonth</c>・州の市場の値段 <c>unitPrice</c>・労働の倍率 <c>laborFactorIndex</c>（<see cref="Habit.LaborFactor"/>＝1 で蝕まない）・闇栽培の係数 <c>productionFactorIndex</c>（全面禁止の州で 1 未満）。
    /// <c>N</c>＝国×四半期（法・緊張・輸出入のどれかが有る国だけ）：保有の法の段 <c>possessionLawCode</c>（0＝許可・1＝輸入禁止・2＝禁止）・輸出の法 <c>exportBanFlag</c>・全国の人口加重の中毒度 <c>nationalAddictionIndex</c>・
    /// 全州の販売量の和 <c>soldTotalPerMonth</c>・直近の清算1回分の輸出 <c>exportsPerClearing</c>／輸入 <c>importsPerClearing</c>（世界価格建て）・
    /// 緊張の積み：四半期に積んだ率の和 <c>tensionAddedPerQuarter</c>・積んだ組×月の数 <c>tensionPairMonthsCount</c>・積んだ直後の組のアヘン由来の緊張の四半期の最大 <c>tensionLevelMaxIndex</c>。
    /// 読むだけ＝力学の値を書かない・乱数を引かない。販売量と緊張の積みは本体の位置でわずかに積む（<c>Instruments.On</c> の中だけ）。</summary>
    static void OpiumRows(Sink sk, ulong seed, int q, MultiNationSim sim)
    {
        sk.Opium ??= Open("opium", seed);
        if (!sk.OpiumHeader)
        {
            sk.Opium.WriteLine("scope	quarter	year	nationIndex	nation	region	populationK	addictionIndex	soldPerMonth	soldPerCapitaPerMonth	unitPrice	laborFactorIndex	productionFactorIndex	"
                + "possessionLawCode	exportBanFlag	nationalAddictionIndex	soldTotalPerMonth	exportsPerClearing	importsPerClearing	tensionAddedPerQuarter	tensionPairMonthsCount	tensionLevelMaxIndex");
            sk.OpiumHeader = true;
        }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int year = sim.Nations[0].World.Year;
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World; var c = w.Coef; string nation = NameOf(w); var st = S(w);
            int gid = c.OpiumGoodId;
            double popSum = 0, addSum = 0, soldSum = 0;
            foreach (var r in w.Regions)
            {
                var f = FlowOf(r);
                double sold = Calendar.PerQuarterToPerMonth(f.HabitSoldSum); f.HabitSoldSum = 0;
                bool active = r.State == RegionState.Active;
                double pop = active ? r.SumPopulation() : 0;
                if (active) { popSum += pop; addSum += r.OpiumAddiction * pop; soldSum += sold; }
                if (!(r.OpiumAddiction > 0 || sold > 0 || r.ContrabandProductionFactor < 1.0)) continue;
                double price = gid >= 0 && gid < r.Markets.Length && r.Markets[gid] is { } mk ? mk.Price : double.NaN;
                sk.Opium.WriteLine(string.Join('	', "R", I(q), I(year), I(i), nation, r.Name, F(pop), F(r.OpiumAddiction), F(sold), F(pop > 0 ? sold / pop : double.NaN), F(price),
                    F(Habit.LaborFactor(c, r)), F(r.ContrabandProductionFactor), "", "", "", "", "", "", "", "", ""));
            }
            double ex = w.ExportsByGood is { } eg && gid >= 0 && gid < eg.Length ? eg[gid] : 0, im = w.ImportsByGood is { } ig && gid >= 0 && gid < ig.Length ? ig[gid] : 0;
            if (w.OpiumPossession != ReformLevel.None || w.OpiumExportBan || addSum > 0 || soldSum > 0 || ex > 0 || im > 0 || st.OpTensionPairMonths > 0)
                sk.Opium.WriteLine(string.Join('	', "N", I(q), I(year), I(i), nation, "", "", "", "", "", "", "", "",
                    I((int)w.OpiumPossession), w.OpiumExportBan ? "1" : "0", F(popSum > 0 ? addSum / popSum : 0), F(soldSum), F(ex), F(im), F(st.OpTensionAdded), I(st.OpTensionPairMonths), F(st.OpTensionLevelMax)));
            st.OpTensionAdded = 0; st.OpTensionPairMonths = 0; st.OpTensionLevelMax = 0;
        }
    }

    /// <summary>★(342) 州のその月のアヘンの販売量（<c>EconomyStep</c> の中毒度の更新の直前・読むだけ）。</summary>
    internal static void HabitSold(Region r, double sold) => FlowOf(r).HabitSoldSum += sold;

    /// <summary>★(342) 禁令中の国 <paramref name="w"/> がアヘンの輸出国との組へ緊張を積んだ直後（<c>Diplomacy.OpiumTension</c>・読むだけ）：積んだ率 <paramref name="rate"/>・その組のアヘン由来の緊張の今の値 <paramref name="level"/>。</summary>
    internal static void OpiumTension(World w, double rate, double level)
    {
        var st = S(w); st.OpTensionAdded += rate; st.OpTensionPairMonths++;
        if (level > st.OpTensionLevelMax) st.OpTensionLevelMax = level;
    }

    /// <summary>戦争1本ごと（<c>wars</c>）と(年, 国)ごとの戦死（<c>casualty</c>）。戦争の戦死と月数は試合の終わりの値（戦争が続いていればその時点まで）。</summary>
    /// <summary>★(271) 運河の表 <c>canal_s</c>：運河ごとに1行。<c>openedQuarter</c>＝最初に開通を見た四半期（<c>quarter</c> と同じ数え方）・開通しなかった運河は <c>-1</c>。試合の終わりに書く。</summary>
    static void CanalTable(Sink sk, MultiNationSim sim)
    {
        using var wr = Open("canal", sk.Seed);
        wr.WriteLine("canalId\topenedQuarter\topenedYear");
        foreach (var cp in sim.Diplomacy.Chokepoints)
        {
            if (!cp.IsCanal) continue;
            bool opened = sk.CanalOpened.TryGetValue(cp.Id, out var oq);
            wr.WriteLine(string.Join('\t', cp.Id, opened ? oq.Quarter.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-1", opened ? oq.Year.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-1"));
        }
    }

    // ---------------------------------------------------------------- 発見の表の行の出来事（流れG・(320)）----------------------------------------------------------------

    /// <summary>★(320) 発見の表の行が解禁した（<see cref="Discovery.Step"/> の解禁の所・読むだけ）。<paramref name="ratio"/>＝代替する物の値段（実質）と新しいレシピで作る費用（物を生む行だけ・無ければ NaN）。</summary>
    internal static void DiscoveryEvent(World w, int row, Discovery.Terms t, (double Replaced, double Cost) ratio)
        => S(w).DiscRows.Add(DiscRowText("E", w, row, t, ratio));

    /// <summary>★(320) まだ解禁していない蒸気機関と機関車の行の軌道（<see cref="Discovery.Step"/> の判定の頭・読むだけ・四半期ごと）。</summary>
    internal static void DiscoveryTrace(World w, int row, Discovery.Terms t, (double Replaced, double Cost) ratio)
        => S(w).DiscRows.Add(DiscRowText("T", w, row, t, ratio));

    /// <summary>★(328) 伝わりの接触の内訳（<see cref="BilateralMarket"/> の発見の記帳 <c>AccumulateDiscovery</c> が <c>DiscoveryImportQ</c> へ足す所と同じ述語〔売り手が解禁済み・買い手が未解禁〕・読むだけ）。
    /// 蒸気機関と機関車の行だけ、買い手の国ごとに〈どの国から運び手の財を買ったか〉を四半期の和で貯める（述語が買い手の未解禁を要るので解禁後は貯まらない＝小さい表）。
    /// 分母（世界のその運び手の財の取引額）は書かない＝売り手の国どうしの割合を読む用。四半期の区切りは disc_s と同じ表の書き出しの位置。</summary>
    internal static void DiscoveryContact(World buyer, World seller, int row, int good, double value)
    {
        if (!(value > 0) || Catalog.Discoveries[row].Name is not ("蒸気機関" or "機関車")) return;
        var acc = S(buyer).DiscContact; var key = (row, NameOf(seller), good);   // (360) 運び手の財（買った財）も名乗る＝伝わりの道で機関車・汽船が蒸気機関の行の運び手になる分を読む
        acc[key] = acc.TryGetValue(key, out double cur) ? cur + value : value;
    }

    /// <summary>★(360) 接触の分母（読むだけ）：蒸気機関・機関車の行の運び手の財ごとの、この四半期の世界の越境の取引額（<c>DiscoveryGoodFlowQ</c>＝接触の分母が読む配列そのもの）。
    /// 分子の内訳（disccontact_s の carrier）と並べて、運び手を広げた分の希釈（分母だけが太る側）を読む。四半期の末に discflow_s へ。</summary>
    internal static void DiscoveryFlow(World w0, double[] gf, int[][] carriers)
    {
        var goods = new SortedSet<int>();
        for (int r = 0; r < carriers.Length; r++)
            if (Catalog.Discoveries[r].Name is "蒸気機関" or "機関車") foreach (int g in carriers[r]) goods.Add(g);
        var st = S(w0);
        foreach (int g in goods)
            if (g < gf.Length) st.DiscFlowRows.Add(string.Join('	', w0.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), Catalog.Goods[g].Name, F(gf[g])));
    }

    static string DiscRowText(string kind, World w, int row, Discovery.Terms t, (double Replaced, double Cost) ratio)
        => string.Join('\t', kind, w.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), NameOf(w), Catalog.Discoveries[row].Name,
            F(t.Tech), F(t.Need), F(t.Transmit), ratio is (0, 0) ? "nan" : F(ratio.Replaced), ratio is (0, 0) ? "nan" : F(ratio.Cost), F(w.TechLevel));   // 書かれなかった行（新兵器・制度・思想など）は nan

    /// <summary>★(320) 発見の表 <c>disc_s</c>（読むだけ）。列＝kind（E 解禁した行／T 蒸気機関と機関車の軌道〔まだ解禁していない四半期ごと〕）・quarter・year・nation・row・techTerm・needTerm・transmitTerm・
    /// replaced（代替する物の値段・実質）・cost（新しいレシピで作る費用）・techLevel（★(322) 合流：枝 dg の列 ownOpen は外した＝解禁した後の盤面を読むので製法の行で常に 1 になる・反証役 dg〔bv〕#1）。replaced／cost は物を生む行（製法・新製品）だけ（それ以外は nan）。</summary>
    static void DiscTable(Sink sk, ulong seed, int q, MultiNationSim sim)
    {
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Disc ??= Open("disc", seed);
        if (!sk.DiscHeader) { sk.Disc.WriteLine("kind\tquarter\tyear\tnation\trow\ttechTerm\tneedTerm\ttransmitTerm\treplaced\tcost\ttechLevel"); sk.DiscHeader = true; }
        foreach (var n in sim.Nations)
        {
            var st = S(n.World);
            foreach (var row in st.DiscRows) { int tab = row.IndexOf('\t'); sk.Disc.WriteLine(row.Substring(0, tab + 1) + I(q) + row.Substring(tab)); }
            st.DiscRows.Clear();
            if (ReferenceEquals(n, sim.Nations[0]) && st.DiscFlowRows.Count > 0)   // (360) 接触の分母（世界の値＝1 国の世界に貯めた行を 1 度だけ）
            {
                sk.DiscFlow ??= Open("discflow", seed);
                if (!sk.DiscFlowHeader) { sk.DiscFlow.WriteLine("quarter	year	good	valueFlowQ"); sk.DiscFlowHeader = true; }
                foreach (var row in st.DiscFlowRows) sk.DiscFlow.WriteLine(I(q) + "	" + row);
                st.DiscFlowRows.Clear();
            }
            if (st.DiscContact.Count > 0)   // (328) 接触の内訳（買い手の国の個体×行×売り手の国）
            {
                sk.DiscContact ??= Open("disccontact", seed);
                if (!sk.DiscContactHeader) { sk.DiscContact.WriteLine("quarter	year	buyer	row	seller	carrier	valuePerQuarter"); sk.DiscContactHeader = true; }
                foreach (var ((crow, seller, cgood), v) in st.DiscContact.OrderBy(kv => kv.Key.Row).ThenBy(kv => kv.Key.Seller, StringComparer.Ordinal).ThenBy(kv => kv.Key.Good))
                    sk.DiscContact.WriteLine(string.Join('	', I(q), I(n.World.Year), NameOf(n.World), Catalog.Discoveries[crow].Name, seller, Catalog.Goods[cgood].Name, F(v)));
                st.DiscContact.Clear();
            }
        }
    }

    // ---------------------------------------------------------------- 産地の判定と世界の金銀（鉱床の必要性の回・(316)）----------------------------------------------------------------

    /// <summary>★(316) 産地が開いた（<see cref="Discovery.Step"/> の産地のループ・読むだけ）。<paramref name="parts"/>＝[0] 人の数 N・[1] その財の値段・[2] 今のレシピで掘る費用。</summary>
    internal static void SiteOpened(World w, Region r, Producer p, Discovery.Terms t, double[] parts) => S(w).SiteRows.Add(SiteRowText("O", w, r, p, t, parts));

    static string SiteRowText(string kind, World w, Region r, Producer p, Discovery.Terms t, double[] parts)
    {
        var site = Catalog.Sites[p.SiteRow];
        return string.Join('\t', kind, w.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), NameOf(w), r.Name, site.Name, site.Kind == Catalog.SiteKind.Deposit ? "D" : "P",
            F(w.TechLevel), F(Discovery.DifficultyOf(site.Decade, w.Coef)), F(parts[0]), F(parts[1]), F(parts[2]), F(t.Tech), F(t.Need));
    }

    /// <summary>★(316) 産地の判定の表 <c>site_s</c>（行の種類 kind＝O 開いた行／T まだ開かない産地の軌道〔四半期ごと〕）と世界の金銀の表 <c>metal_s</c>（四半期ごとに1行）。読むだけ。
    /// site_s の列＝kind・quarter・year・nation・region・site・siteKind〔D 鉱床／P 植林〕・techLevel・difficulty・peopleK・price・cost・techTerm・needTerm（kind=O の quarter は書いた四半期の末の番号）。
    /// metal_s の列＝quarter・year・goldProduced・silverProduced・goldDemand・silverDemand・goldStock・silverStock（州の市場の直近の月の和）・nGoldLaw・nSilverLaw・nFiatLaw（人の居る国）・
    /// lnAgAuMean＝金銀の両方の市場が生きた州の ln(銀の値段÷金の値段÷参照の比) の平均・nRatio（その州の数）・goldNations＝金本位の国の名前。</summary>
    static void SiteTables(Sink sk, ulong seed, int q, MultiNationSim sim)
    {
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Site ??= Open("site", seed);
        if (!sk.SiteHeader) { sk.Site.WriteLine("kind\tquarter\tyear\tnation\tregion\tsite\tsiteKind\ttechLevel\tdifficulty\tpeopleK\tprice\tcost\ttechTerm\tneedTerm"); sk.SiteHeader = true; }
        foreach (var n in sim.Nations)
        {
            var w = n.World; var st = S(w);
            Discovery.VisitSiteTerms(w, w.Coef, (r, p, t, parts) => st.SiteRows.Add(SiteRowText("T", w, r, p, t, parts)));
            foreach (var row in st.SiteRows) { int tab = row.IndexOf('\t'); sk.Site.WriteLine(row.Substring(0, tab + 1) + I(q) + row.Substring(tab)); }
            st.SiteRows.Clear();
        }
        sk.Metal ??= Open("metal", seed);
        if (!sk.MetalHeader) { sk.Metal.WriteLine("quarter\tyear\tgoldProduced\tsilverProduced\tgoldDemand\tsilverDemand\tgoldStock\tsilverStock\tnGoldLaw\tnSilverLaw\tnFiatLaw\tlnAgAuMean\tnRatio\tgoldNations"); sk.MetalHeader = true; }
        double prodAg = 0, prodAu = 0, demAg = 0, demAu = 0, stAg = 0, stAu = 0, lnSum = 0;
        int nGold = 0, nSilver = 0, nFiat = 0, nRatio = 0, yr = 0;
        var names = new System.Text.StringBuilder();
        double refRatio = Catalog.RefPrices[Catalog.SilverId] / Catalog.RefPrices[Catalog.GoldOreId];
        foreach (var n in sim.Nations)
        {
            var w = n.World; yr = w.Year;
            if (w.TotalPopulation > 0)
            {
                if (w.MonetaryLaw == MonetaryStandardLaw.Gold) { nGold++; names.Append(NameOf(w)).Append(';'); } else if (w.MonetaryLaw == MonetaryStandardLaw.Silver) nSilver++; else nFiat++;
            }
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                if (Catalog.SilverId >= r.Markets.Length || Catalog.GoldOreId >= r.Markets.Length) continue;
                var mAg = r.Markets[Catalog.SilverId]; var mAu = r.Markets[Catalog.GoldOreId];
                prodAg += mAg.LastProduced; prodAu += mAu.LastProduced; demAg += mAg.LastDemand; demAu += mAu.LastDemand; stAg += mAg.Stock; stAu += mAu.Stock;
                if (!(mAg.LastDemand > 1e-9 || mAg.LastProduced > 1e-9) || !(mAu.LastDemand > 1e-9 || mAu.LastProduced > 1e-9)) continue;
                if (!(mAg.Price > 1e-9) || !(mAu.Price > 1e-9)) continue;
                lnSum += System.Math.Log(mAg.Price / mAu.Price / refRatio); nRatio++;
            }
        }
        sk.Metal.WriteLine(string.Join('\t', I(q), I(yr), F(prodAu), F(prodAg), F(demAu), F(demAg), F(stAu), F(stAg), I(nGold), I(nSilver), I(nFiat), nRatio > 0 ? F(lnSum / nRatio) : "nan", I(nRatio), names.ToString()));
    }

    static void WarTables(Sink sk, DipState dip)
    {
        using (var wr = Open("wars", sk.Seed))
        {
            wr.WriteLine("startYear\tcivilWarFlag\tsideA\tsideB\tmonths\tdeadK");
            foreach (var (year, war) in dip.Wars)
                wr.WriteLine(string.Join('\t', year.ToString(System.Globalization.CultureInfo.InvariantCulture), war.CivilWarParent is not null ? "1" : "0", NameOf(war.A), NameOf(war.B), F(war.Months), F(war.DeadA + war.DeadB)));
        }
        using (var cs = Open("casualty", sk.Seed))
        {
            cs.WriteLine("year\tnation\tmonths\tsoldierMonthsK\tdeadK");
            foreach (var kv in dip.Cas)
                cs.WriteLine(string.Join('\t', kv.Key.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), kv.Key.Nation, F(kv.Value[0]), F(kv.Value[1]), F(kv.Value[2])));
        }
    }

    /// <summary>国×四半期の行（全ての国・死んだ国も）。読み手は年と国の名前で引く。台帳は試合の始まりからの累計（四半期の差分は読み手が引く）。
    /// 読む位置は旧の注入と同じ＝四半期の末（<c>StepQuarter</c> の直後）。名目GDP は四半期の末にしか読まない（メモ化された量を新しい位置で読まない）。</summary>
    static void NationRow(Sink sk, ulong seed, int q, int index, World w, WState st, bool yearEnd)
    {
        double popK = 0, soldierK = 0, supplyFillSum = 0, foodMetSum = 0, foodNeedSum = 0, needMetSum = 0, needTotalSum = 0;
        int core = 0, integrated = 0, colonial = 0;
        var nonCore = new System.Text.StringBuilder();
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Pops)
            {
                popK += p.Size; if (p.IsSoldier) soldierK += p.Size; supplyFillSum += p.SupplyFill * p.Size;
                foodMetSum += p.FoodMet; foodNeedSum += p.FoodNeed; needMetSum += p.NeedMet; needTotalSum += p.NeedTotal;   // (263) 補給の充足の内訳（主食の台帳・満足度の台帳）
            }
            if (r.Tier == RegionTier.Core) core++; else if (r.Tier == RegionTier.Integrated) integrated++; else colonial++;
            if (yearEnd && r.Tier != RegionTier.Core) nonCore.Append(r.Tier == RegionTier.Integrated ? "I:" : "C:").Append(r.Name).Append(';');
        }
        double exports = 0, imports = 0;
        if (w.ExportsByGood is { } eg) foreach (var v in eg) exports += v;
        if (w.ImportsByGood is { } ig) foreach (var v in ig) imports += v;
        double threat = w.SecurityThreat, own = w.SecurityThreatOwnMass, army = w.SecurityThreatArmy;
        var led = w.TreasuryLedger;
        var cols = new List<string>(32);
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        cols.Add(I(q)); cols.Add(I(w.Year)); cols.Add(I(index)); cols.Add(NameOf(w));
        cols.Add(F(w.TotalPopulation)); cols.Add(F(popK)); cols.Add(F(soldierK)); cols.Add(F(Calendar.PerMonthToPerYear(w.NominalGdpPerMonth).Value));
        cols.Add(I(st.WarMonths)); cols.Add(I(st.CivilWarMonths)); cols.Add(I((int)w.ConscriptionLaw)); cols.Add(w.Mobilized ? "1" : "0");
        var extras = NationExtras(w, st);   // (259) 戦時段・行政の請求の2つの式（総動員の列の直後＝他の線が末尾に足す列と行が重ならない）
        foreach (var ex in extras) cols.Add(ex.Value);
        cols.Add(F(threat + own > 0 ? threat / (threat + own) : 0)); cols.Add(F(army + own > 0 ? army / (army + own) : 0)); cols.Add(I((int)w.SerfEmancipation));
        cols.Add(I(core)); cols.Add(I(integrated)); cols.Add(I(colonial)); cols.Add(nonCore.ToString());
        cols.Add(F(led[(int)TreasuryCat.Borrow])); cols.Add(F(led[(int)TreasuryCat.Devalue])); cols.Add(F(led[(int)TreasuryCat.Army])); cols.Add(F(led[(int)TreasuryCat.Navy]));
        cols.Add(F(w.GovDebt.Value)); cols.Add(F(exports)); cols.Add(F(imports)); cols.Add(I(w.Coef.ClearingsPerYear));
        // (258) F1 段0：財政の鎖を全ての国の四半期で（借入の門・払えない額・率 r・歳入の内訳）。量は四半期の月平均（月数で割る）・月の数は整数
        double mo = st.ClsMonths > 0 ? st.ClsMonths : double.NaN;
        cols.Add(I(st.QGateObs)); cols.Add(I(st.QGateClosed)); cols.Add(I(st.QShortMonths)); cols.Add(I(st.QRUnder1Months));
        cols.Add(F(st.QInterest / mo)); cols.Add(F(st.QRevenue / mo)); cols.Add(F(st.QDirect / mo)); cols.Add(F(st.QIndirect / mo)); cols.Add(F(st.QShortfall / mo)); cols.Add(F(st.QBorrowed / mo));
        cols.Add(I(st.FacStarted)); cols.Add(I(st.FacStartedClosed)); cols.Add(I(st.FacDone)); cols.Add(I(st.FacDoneClosed)); cols.Add(I(st.FacDoneClosedAtStart));
        cols.Add(F(popK > 0 ? supplyFillSum / popK : 0));   // 人口加重の供給の充足（補給崩壊の物差し＝この値が0.3 を割る国×四半期を数える・旧の注入 inj_s3a の fill と同じ式）
        // (263) 線E2：輪と動詞の個体の食い違い（A）・補給の充足の内訳と着工した財（B）。台帳の月の和は四半期の末の POP の値（月の途中でない）
        var stakeGoods = new List<string>();
        int stakeAll = StakeMismatch(w, out int stakeToInv, out int stakeToUninv, out int staleSub, stakeGoods);
        cols.Add(I(st.FsRun)); cols.Add(I(st.FsSeenMismatch)); cols.Add(I(st.FsFired)); cols.Add(I(st.FsFiredMismatch)); cols.Add(I(st.FsFiredOver)); cols.Add(I(st.SubMismatch)); cols.Add(I(st.SubMismatchOff));
        cols.Add(I(stakeAll)); cols.Add(I(stakeToInv)); cols.Add(I(stakeToUninv));
        cols.Add(string.Join(';', st.WheelGoods)); cols.Add(string.Join(';', st.FacStartedGoods));
        cols.Add(F(foodNeedSum > 0 ? foodMetSum / foodNeedSum : double.NaN)); cols.Add(F(needTotalSum > 0 ? needMetSum / needTotalSum : double.NaN));
        cols.Add(I(staleSub)); cols.Add(string.Join(';', stakeGoods));
        var ext265 = Extras265(w, st);   // (265) 線I：末尾に足す列（名前と値を同じ表から出す＝見出しと行がずれない）
        foreach (var ex in ext265) cols.Add(ex.Value);
        if (st.Uid == 0) st.Uid = ++sk.NextUid;   // (271) 国の個体の番号（四半期の行を書く順に採番＝決定論・World の個体に付く）
        var ext271 = Extras271(w, st);   // (271) 線K：国の個体・消費の予算の拘束・建設の投資枠・国債のプール・与信の上限
        foreach (var ex in ext271) cols.Add(ex.Value);
        var ext270 = Extras270(w, st);   // (270) 線B2：軍事費の合計の施行の一歩の内訳（末尾）
        foreach (var ex in ext270) cols.Add(ex.Value);
        var ext274 = Extras274(w, st);   // (274) 線G：幽霊戦線（開いた戦線の施行の結果）
        foreach (var ex in ext274) cols.Add(ex.Value);
        var ext278 = Extras278(w, st);   // (278) 線G2：政治資本の在庫の台帳・符号 2 の分割
        foreach (var ex in ext278) cols.Add(ex.Value);
        var ext281 = Extras281(st);   // (281) 線O0：借入の門の周り
        foreach (var ex in ext281) cols.Add(ex.Value);
        var ext286 = Extras286(w, st);   // (286) 線Q：建設の門と実質GDP・為替・物価水準
        foreach (var ex in ext286) cols.Add(ex.Value);
        var ext287 = Extras287(w);   // (287) 線F1：土地税の段・政体・特権の度合い・議会の掌握（四半期の終わりの断面）
        foreach (var ex in ext287) cols.Add(ex.Value);
        var ext289 = Extras289(st);   // (289) 線D：国の所得の合計・国庫が実際に払った建設の額
        foreach (var ex in ext289) cols.Add(ex.Value);
        var ext329 = Extras329(w);   // (329) 線(2)(3)：世界の1人あたり実質所得（配られた値）と奢侈品の輸出額
        foreach (var ex in ext329) cols.Add(ex.Value);
        sk.Nation ??= Open("nation", seed);
        if (!sk.NationHeader)
        {
            sk.Nation.WriteLine("quarter\tyear\tnationIndex\tnation\ttotalPopulationK\tpopulationK\tsoldierK\tnominalGdpPerYear\tatWarMonths\tcivilWarMonths\tconscriptionLaw\tmobilizedFlag\t"
                + string.Join('\t', extras.Select(e => e.Name)) + "\t"
                + "securityThreatShare\tarmyThreatShare\tserfEmancipation\tcoreRegions\tintegratedRegions\tcolonialRegions\tnonCoreRegions\t"
                + "ledgerBorrowCumulative\tledgerDevalueCumulative\tledgerArmyCumulative\tledgerNavyCumulative\tgovDebtStock\texportsPerClearing\timportsPerClearing\tclearingsPerYear\t"
                + "borrowGateObservedMonths\tborrowGateClosedMonths\tshortfallMonths\tpayRateBelowOneMonths\t"
                + "interestMeanPerMonth\trevenueBaseMeanPerMonth\tdirectTaxNetMeanPerMonth\tindirectRevenueMeanPerMonth\tshortfallMeanPerMonth\tborrowedMeanPerMonth\t"
                + "stateFacilityStartedCount\tstateFacilityStartedGateClosedCount\tstateFacilityCompletedCount\tstateFacilityCompletedGateClosedCount\tstateFacilityCompletedGateClosedAtStartCount\tsupplyFillShare\t"
                + "fireSaleRunCount\tfireSaleSeenMismatchCount\tfireSaleFiredCount\tfireSaleFiredMismatchCount\tfireSaleFiredOverTargetCount\tsubsidyWriteMismatchCount\tsubsidyWriteMismatchWantOffCount\t"
                + "stakeMismatchProducerCount\tstakeMismatchToInvestedCount\tstakeMismatchToUninvestedCount\twheelMismatchGoods\tstateFacilityStartedGoods\tfoodMetShare\tneedMetShare\tstaleSubsidyNotGrabbedCount\tstakeMismatchGoods\t"
                + string.Join('\t', ext265.Select(e => e.Name)) + "\t"
                + string.Join('\t', ext271.Select(e => e.Name)) + "\t"
                + string.Join('\t', ext270.Select(e => e.Name)) + "\t"
                + string.Join('\t', ext274.Select(e => e.Name)) + "\t"
                + string.Join('\t', ext278.Select(e => e.Name)) + "\t"
                + string.Join('\t', ext281.Select(e => e.Name)) + "\t"
                + string.Join('\t', ext286.Select(e => e.Name)) + "\t"
                + string.Join('\t', ext287.Select(e => e.Name)) + "\t"
                + string.Join('\t', ext289.Select(e => e.Name)) + "\t"
                + string.Join('\t', ext329.Select(e => e.Name)));
            sk.NationHeader = true;
        }
        sk.Nation.WriteLine(string.Join('\t', cols));
        if (IsRemnant(core + integrated + colonial, popK, popK > 0 ? supplyFillSum / popK : 0)) GoodsRows(sk, seed, q, w);
    }

    /// <summary>★(265) 残存国（本国を失った1〜2州の国）の財ごとの表 <c>goods_s</c> を出す条件：活きた州が2以下・人口100千人以上・人口加重の供給の充足が <see cref="RemnantSupplyFillMax"/> 未満（補給崩壊の物差しの片側）。
    /// 出す国を絞らないと 61国×668四半期×財の数の行になる。条件は計器の自己申告の限界＝残存国でも補給が足りている四半期は出ない。</summary>
    internal static bool IsRemnant(int activeRegions, double popK, double supplyFill) => activeRegions <= 2 && popK >= 100 && supplyFill < RemnantSupplyFillMax;
    internal static double RemnantSupplyFillMax = 0.5;

    /// <summary>残存国の財ごとの需給（主食・日用品）：州の市場の直近の月の値の国の和。<c>lastProducedQty</c>＝今期の産出・<c>lastDemandQty</c>＝前期の総需要（消費＋投入）・<c>lastSoldQty</c>・<c>lastShortageQty</c>・<c>lastImportQty</c>／<c>lastExportQty</c>＝今期の輸入・輸出量・
    /// <c>stockQty</c>＝在庫・<c>importsValuePerClearing</c>／<c>exportsValuePerClearing</c>＝国の財別の輸入額・輸出額（直近の清算1回分・世界価格建て）。四半期の末の断面（月平均ではない）。</summary>
    static void GoodsRows(Sink sk, ulong seed, int q, World w)
    {
        sk.Goods ??= Open("goods", seed);
        if (!sk.GoodsHeader) { sk.Goods.WriteLine("quarter\tyear\tnation\tgoodName\ttier\tactiveRegions\tlastProducedQty\tlastDemandQty\tlastSoldQty\tlastShortageQty\tlastImportQty\tlastExportQty\tstockQty\tpriceMean\timportsValuePerClearing\texportsValuePerClearing"); sk.GoodsHeader = true; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string nm = NameOf(w);
        for (int g = 0; g < Catalog.Goods.Length; g++)
        {
            var good = Catalog.Goods[g];
            if (good.Tier != GoodTier.Staple && good.Tier != GoodTier.Daily) continue;
            double prod = 0, dem = 0, sold = 0, shortage = 0, imp = 0, exp = 0, stock = 0, price = 0; int n = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active || g >= r.Markets.Length) continue;
                var m = r.Markets[g];
                prod += m.LastProduced; dem += m.LastDemand; sold += m.LastSold; shortage += m.LastShortage; imp += m.LastImport; exp += m.LastExport; stock += m.Stock; price += m.Price; n++;
            }
            if (n == 0) continue;
            double ev = w.ExportsByGood is { } eg && g < eg.Length ? eg[g] : double.NaN, iv = w.ImportsByGood is { } ig && g < ig.Length ? ig[g] : double.NaN;
            sk.Goods.WriteLine(string.Join('\t', I(q), I(w.Year), nm, good.Name, good.Tier.ToString(), I(n), F(prod), F(dem), F(sold), F(shortage), F(imp), F(exp), F(stock), F(price / n), F(iv), F(ev)));
        }
    }

    /// <summary>★(265) 線I：国×四半期の行の末尾に足す列。月ごとの量は四半期の月の平均（<c>PerMonth</c>・<c>PerYear</c>）・数は四半期の和。
    /// 国債金利の内訳／行政の必要額と反事実 A′／段5 の上限の拘束／専売の段・売却・逼迫・人頭税・福祉の目標。</summary>
    static List<(string Name, string Value)> Extras265(World w, WState st)
    {
        var cols = new List<(string, string)>();
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        void C(string name, double v) => cols.Add((name, F(v)));
        void N(string name, int v) => cols.Add((name, I(v)));
        double bm = st.BondMonths > 0 ? st.BondMonths : double.NaN, m = st.AdmMonths > 0 ? st.AdmMonths : double.NaN;
        N("bondMonthsCount", st.BondMonths);
        C("bondRateMeanPerYear", st.BondRate / bm); C("bondBaseTermPerYear", st.BondBase / bm); C("bondDebtTermPerYear", st.BondDebt / bm); C("bondInflationExpectationTermPerYear", st.BondFisher / bm);
        C("bondDistrustTermPerYear", st.BondDistrust / bm); C("bondPolicyRateTermPerYear", st.BondPolicy / bm); C("bondGoldDiscountMemoPerYear", st.BondGold / bm);
        C("bondRepresentationTermPerYear", st.BondRepr / bm); C("bondClampTermPerYear", st.BondClamp / bm);
        C("bondDebtToGdpYearMeanRatio", st.BondDebtRatio / bm); C("bondConvertibilityDiscountMeanFactor", st.BondConvMul / bm); C("bondRepresentationDiscountMeanFactor", st.BondReprMul / bm);
        C("adminNeedAtBudgetPerMonth", st.QAdmNeedB / m); C("adminClaimCappedAtBudgetPerMonth", st.QAdmMinB / m); C("adminNeedAtPurchasePerMonth", st.QAdmNeedP / m); C("adminClaimCappedAtPurchasePerMonth", st.QAdmMinP / m);
        N("adminOverNeedRegionMonthsCount", st.QAdmOverRegionMonths);
        N("adminCfObservedMonths", st.QCfObs); N("adminCfShortMonthsCount", st.QCfShortMonths); C("adminCfBorrowedMeanPerMonth", st.QCfBorrowed / m); N("adminCfRBelow1MonthsCount", st.QCfRBelow1Months);
        double cm = st.ClsMonths > 0 ? st.ClsMonths : double.NaN;
        bool capOn = st.QCapPaid > 0 && st.QCapPopK > 0;
        C("massPaidPerMonth", capOn ? st.QCapPaid / cm : double.NaN);
        for (int k = 0; k < CapDs.Length; k++) C("capBoundPaidShare" + CapDNames[k], capOn ? st.QCapBoundPaid[k] / st.QCapPaid : double.NaN);
        for (int k = 0; k < CapDs.Length; k++) C("capCutShare" + CapDNames[k], capOn ? st.QCapCut[k] / st.QCapPaid : double.NaN);
        for (int k = 0; k < CapDs.Length; k++) C("capBoundPopShare" + CapDNames[k], capOn ? st.QCapBoundPopK[k] / st.QCapPopK : double.NaN);
        N("monopolyTierBeforeDecisionStage", st.MonoBefore); N("monopolyTierTargetDecisionStage", st.MonoTarget); N("monopolyTierEndStage", w.StateMonopolyTier);
        C("fiscalStressAtDecisionIndex", st.FiscalStressAtDecision); N("stateFactorySoldCount", st.FactorySold);
        C("interestBurdenAtDecisionIndex", st.PushBurden); C("monopolyPushProbabilityIndex", st.PushP); C("monopolyPushDrawIndex", st.PushU); N("monopolyPushHitCount", st.PushHit);
        C("pollTaxEndShare", w.PollTax); C("pollTaxTargetEndShare", w.Policy.PollTaxTarget); C("socialSecurityEndShare", w.SocialSecurity); C("socialSecurityTargetEndShare", w.Policy.SocialSecurityTarget); N("socialSecurityLawEndStage", (int)w.SocialSecurityLaw);
        return cols;
    }

    /// <summary>★(271) 線K：国×四半期の行の末尾に足す列。<c>nationUid</c>＝国の個体の番号（<c>nationIndex</c> は国の消滅で詰まり、<c>nation</c>〔BaseName〕は建国で再利用・継承で変わる＝個体を指さない。この番号は World の個体に付く＝改名でも変わらず、建国は新しい番号）。
    /// 消費の予算の拘束（BudgetBindDiag の行と同じ述語を標準の計器へ）／建設の側で投資枠が希望を切った州×月／国債の借入の判定の月でプールが縛った月と各プール／与信の上限（準備の側と返せる額）。
    /// 金額は四半期の月の平均（<c>PerMonth</c>）・借入の各プールは判定した月あたりの平均（<c>MeanPerMonth</c>＝判定した月の和÷判定した月の数）・与信の上限は判定の日数で重みづけた平均（<c>MeanStock</c>）。</summary>
    static List<(string Name, string Value)> Extras271(World w, WState st)
    {
        var cols = new List<(string, string)>();
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        void C(string name, double v) => cols.Add((name, F(v)));
        void N(string name, long v) => cols.Add((name, v.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        N("nationUid", st.Uid);
        double cm = st.ClsMonths > 0 ? st.ClsMonths : double.NaN;
        N("consumeRowsCount", st.CnRows); N("consumeBudgetBoundRowsCount", st.CnBound); C("consumeBudgetBoundRowShare", st.CnRows > 0 ? (double)st.CnBound / st.CnRows : double.NaN);
        C("consumeDesiredValuePerMonth", st.CnDesired / cm); C("consumeCutValuePerMonth", (st.CnDesired - st.CnDemand) / cm); C("consumeCutShare", st.CnDesired > 0 ? (st.CnDesired - st.CnDemand) / st.CnDesired : double.NaN);
        N("constructionDesiringRegionMonthsCount", st.ConDesiring); N("constructionBudgetBoundRegionMonthsCount", st.ConBound);
        C("constructionDesiredUnitsPerMonth", st.ConDesiredUnits / cm); C("constructionCutUnitsPerMonth", st.ConCutUnits / cm);
        double bj = st.BrJudged > 0 ? st.BrJudged : double.NaN;
        N("borrowJudgedMonthsCount", st.BrJudged); N("borrowPoolBoundMonthsCount", st.BrPoolBound); N("borrowPoolBoundForeignCutByInvestorsMonthsCount", st.BrForeignCut);
        C("borrowDomesticPoolMeanPerMonth", st.BrDomPool / bj); C("borrowForeignPoolRawMeanPerMonth", st.BrForRaw / bj); C("borrowForeignPoolMeanPerMonth", st.BrFor / bj);
        N("creditJudgedDaysCount", st.CrDays); N("creditRepayableBoundDaysCount", st.CrRepayBoundDays);
        C("creditReserveCapMeanStock", st.CrDays > 0 ? st.CrReserveCap / st.CrDays : double.NaN); C("creditRepayableCapMeanStock", st.CrFiniteDays > 0 ? st.CrRepayable / st.CrFiniteDays : double.NaN);
        return cols;
    }

    /// <summary>★(286) 線Q：建設の門（<c>EconomyStep.Run</c> の投資家の建設の判定＝州×月）と、国をまたいで比べる量。
    /// 建設の実際の額は <c>spend_s</c> の kind=extra「建設の材料と工賃」（資本家の階級）。ここの <c>constructionBudgetMoneyPerMonth</c> は投資枠の金額（<c>IndInvestmentFrac</c>×資本家の貯蓄×(1−接収の萎縮)＝州の和）・
    /// <c>constructionDesiredMoneyPerMonth</c> は採算の門が望んだ金額（希望の束×束の費用×(1+工賃)・希望が正の州×月だけの和）。実際の額＝min(2つ)を配給で割った物。
    /// <c>constructionMarginalGainMean</c>／<c>constructionHurdleMean</c> は判定した州×月の平均の限界収益（技術の係数込み）と資本コスト（金利のクラウディングと株のバブルを含む）＝差が採算の門。
    /// 実質GDP（参照価格の産出・中間財を重ねる総産出）・為替・物価水準は国をまたぐ比較のため。</summary>
    static List<(string Name, string Value)> Extras286(World w, WState st)
    {
        var cols = new List<(string, string)>();
        double cm = st.ClsMonths > 0 ? st.ClsMonths : double.NaN;
        double cj = st.ConRegionMonths > 0 ? st.ConRegionMonths : double.NaN;
        cols.Add(("constructionJudgedRegionMonthsCount", st.ConRegionMonths.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        cols.Add(("constructionMarginalGainMean", F(st.ConGainSum / cj))); cols.Add(("constructionHurdleMean", F(st.ConHurdleSum / cj)));
        cols.Add(("constructionBudgetMoneyPerMonth", F(st.ConBudgetMoney / cm))); cols.Add(("constructionDesiredMoneyPerMonth", F(st.ConDesiredMoney / cm)));
        cols.Add(("constructionPlannedMoneyPerMonth", F(st.ConPlannedMoney / cm))); cols.Add(("constructionBudgetBoundRegionsBudgetMoneyPerMonth", F(st.ConBudgetBoundMoney / cm)));
        cols.Add(("constructionPlannedUnitsPerMonth", F(st.ConPlannedUnits / cm))); cols.Add(("constructionBuiltUnitsPerMonth", F(st.ConBuiltUnits / cm)));
        string[] rn = ["Steel", "Machine", "Cement", "Lumber", "Labor"];
        for (int i = 0; i < 5; i++) cols.Add(("constructionRation" + rn[i] + "Mean", F(st.ConPlannedUnits > 0 ? st.ConRationW[i] / st.ConPlannedUnits : double.NaN)));
        cols.Add(("realGdpPerYear", F(Calendar.PerMonthToPerYear(w.GdpPerMonth.Value))));
        cols.Add(("fxRate", F(w.FxRate))); cols.Add(("priceLevel", F(w.PriceLevel)));
        return cols;
    }

    /// <summary>★(270) 線B2：軍事費の合計の施行の一歩の内訳（国×四半期の行の末尾）。来なかった四半期は結果の符号 −1・量は空（NaN）。</summary>
    static List<(string Name, string Value)> Extras270(World w, WState st)
    {
        var cols = new List<(string, string)>();
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        cols.Add(("militaryMoveOutcomeCode", I(st.MmOutcome))); cols.Add(("militaryMoveIncreasingFlag", I(st.MmOutcome >= 0 && st.MmIncreasing ? 1 : 0)));
        cols.Add(("militaryMoveCurrentShare", F(st.MmCur))); cols.Add(("militaryMoveTargetShare", F(st.MmTarget)));
        cols.Add(("militaryMoveNetSupportIndex", F(st.MmNet))); cols.Add(("militaryMoveResistIndex", F(st.MmResist)));
        cols.Add(("militaryMoveResistTradIndex", F(st.MmOppTrad))); cols.Add(("militaryMoveResistModernIndex", F(st.MmOppModern))); cols.Add(("militaryMoveResistMassIndex", F(st.MmOppMass)));
        cols.Add(("militaryMovePushBudgetFactor", F(st.MmPush))); cols.Add(("militaryMoveOpenFrontsCount", I(st.MmFronts)));
        cols.Add(("militaryMoveStepFraction", F(st.MmSpeed)));
        cols.Add(("politicalCapitalSpentIndex", F(st.MmStock))); cols.Add(("politicalCapitalMaxIndex", F(st.MmStockMax)));
        cols.Add(("warStageEnvelopeShareAfterGov", F(st.MmEnvelopeAfterGov))); cols.Add(("warStageEnvelopeShareEnd", F(Military.WarStageShare(w, w.MilitarySpending.Value))));
        cols.Add(("sovereignDefaultCumulativeCount", I(w.SovereignDefaultCount)));
        return cols;
    }

    /// <summary>★(274) 線G：幽霊戦線。<c>Government.Step</c> の頭で数えた開いた戦線 N（政治予算の係数 (1/N)^0.5 の分母）それぞれが、その四半期の施行で何になったか。
    /// 件数の列は符号ごと（符号の意味は <see cref="GFront"/>）。<c>openFrontStates</c>＝数えた順の「種類.軸:符号」（種類 L 段の法・R 税と支出の段・M 軍事費・C 治安維持力・T 関税。M の失効は末尾 x）。
    /// 政治予算の係数が薄めるのは連続の軸（R・M・C・T）の歩幅だけ＝段の法（L）は <c>StepLevel</c> が一歩ずつ進み係数を掛けない（それでも L の開いた戦線も N に数えられる）。
    /// ⚠限界：①施行の結果は四半期の頭の1回の施行のもの（数えた時と施行の時で状態が変わった戦線は符号 0 か −1）②「動く戦線だけで数えた N′」は読み手が導く静的な反事実（係数が変われば歩幅が変わり政治資本の払いも変わる）。</summary>
    static List<(string Name, string Value)> Extras274(World w, WState st)
    {
        var cols = new List<(string, string)>();
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var cnt = new int[11];   // 符号 −1..9 を +1 して数える
        int level = 0, cont = 0; bool mil = false;
        var sb = new System.Text.StringBuilder();
        foreach (var f in st.Gf)
        {
            cnt[f.Code + 1]++;
            if (f.Kind == 'L') level++; else cont++;
            if (f.Kind == 'M') mil = true;
            if (sb.Length > 0) sb.Append(';');
            sb.Append(f.Kind).Append('.').Append(f.Axis).Append(':').Append(f.Code); if (f.Expired) sb.Append('x');
        }
        cols.Add(("openFrontsCount", I(st.GfCounted ? st.Gf.Count : -1)));
        cols.Add(("openFrontsLevelLawCount", I(level))); cols.Add(("openFrontsContinuousCount", I(cont)));
        cols.Add(("frontClosedAtStepCount", I(cnt[1]))); cols.Add(("frontNetSupportFixedCount", I(cnt[2]))); cols.Add(("frontCapitalFixedCount", I(cnt[3])));
        cols.Add(("frontMovedFreeCount", I(cnt[4]))); cols.Add(("frontMovedPaidCount", I(cnt[5])));
        cols.Add(("frontIrreversibleFixedCount", I(cnt[6]))); cols.Add(("frontPrerequisiteFixedCount", I(cnt[7])));
        cols.Add(("frontReachedFreeCount", I(cnt[8]))); cols.Add(("frontReachedPaidCount", I(cnt[9])));
        cols.Add(("frontMilitaryExpiredCount", I(cnt[10]))); cols.Add(("frontUnrecordedCount", I(cnt[0])));
        cols.Add(("militaryFrontCountedFlag", I(mil ? 1 : 0))); cols.Add(("pushBudgetFactor", F(w.PoliticalPushBudget)));
        cols.Add(("openFrontStates", sb.ToString()));
        return cols;
    }

    /// <summary>★(278) 線G2：政治資本の在庫の台帳と、符号 2（政治資本で固定）の分割。(274) の符号 2 は〈代価が最大量 b を超える＝その四半期の b では在庫が空でも届かない（構造）〉と〈代価は b 以内だが残り a＝max(0,b−S) が足りない（飢え）〉を混ぜていた。
    /// 件数は符号 2 の戦線を代価＞b か否かで分ける（L＝段の法・連続＝税と支出の段 R・軍事費 M・治安維持力 C・関税 T）。和＝既存の <c>frontCapitalFixedCount</c>。
    /// 台帳：<c>politicalCapitalSpentAtLevelHead</c>（段の法のループの頭の目減り S）・<c>politicalCapitalMaxAtLevelHead</c>（最大量 b）・<c>politicalCapitalSpentAtStepEnd</c>（Step の末尾の S）。
    /// その四半期に払った額＝<c>levelLawPaidInQuarter</c>（段の法）・<c>ratePaidInQuarter</c>（R）・<c>militaryPaidInQuarter</c>（M）・<c>tariffPaidInQuarter</c>（T）・<c>securityPaidInQuarter</c>（C）・<c>continuousPaidInQuarter</c>（R+M+T+C）。
    /// 恒等：S の末尾−S の頭＝段の法の払い＋連続の軸の払い（Step の中で S を書くのは MandateGate と Move の2本だけ）。
    /// <c>openFrontCosts</c>・<c>openFrontSpentAtDecision</c>＝<c>openFrontStates</c> と同じ順の、各戦線の代価と払う前の S（門の手前で止まった＝nan・無償＝代価 0）。
    /// ⚠限界：①政治資本の最大量 b は Step の頭で導出した値（決める時に変わらない）②Politics.Step の回復と内戦講和の譲歩の支出は Step の外＝列は四半期の間（末尾→次の頭）の差で読む。</summary>
    static List<(string Name, string Value)> Extras278(World w, WState st)
    {
        var cols = new List<(string, string)>();
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int sl = 0, tl = 0, sc = 0, tc = 0;   // 構造（Structural）／飢え × 段の法／連続
        var cost = new System.Text.StringBuilder(); var spent = new System.Text.StringBuilder();
        foreach (var f in st.Gf)
        {
            if (f.Code == 2) { if (f.Kind == 'L') { if (f.Structural) sl++; else tl++; } else { if (f.Structural) sc++; else tc++; } }
            if (cost.Length > 0) { cost.Append(';'); spent.Append(';'); }
            cost.Append(F(f.Cost)); spent.Append(F(f.SDec));
        }
        cols.Add(("frontCapitalStructuralLevelCount", I(sl))); cols.Add(("frontCapitalStarvedLevelCount", I(tl)));
        cols.Add(("frontCapitalStructuralContinuousCount", I(sc))); cols.Add(("frontCapitalStarvedContinuousCount", I(tc)));
        cols.Add(("politicalCapitalSpentAtLevelHead", F(st.GfSHead))); cols.Add(("politicalCapitalMaxAtLevelHead", F(st.GfMaxHead))); cols.Add(("politicalCapitalSpentAtStepEnd", F(st.GfSEnd)));
        cols.Add(("levelLawPaidInQuarter", F(st.GfPaidL))); cols.Add(("ratePaidInQuarter", F(st.GfPaidR))); cols.Add(("militaryPaidInQuarter", F(st.GfPaidM)));
        cols.Add(("tariffPaidInQuarter", F(st.GfPaidT))); cols.Add(("securityPaidInQuarter", F(st.GfPaidC)));
        cols.Add(("continuousPaidInQuarter", F(st.GfPaidR + st.GfPaidM + st.GfPaidT + st.GfPaidC)));
        cols.Add(("openFrontCosts", cost.ToString())); cols.Add(("openFrontSpentAtDecision", spent.ToString()));
        return cols;
    }

    static void ClassRows(Sink sk, ulong seed, string time, World w, WState st, bool flows)
    {
        var size = new double[NC]; var sav = new double[NC]; var negSav = new double[NC]; var names = new string[NC];
        foreach (var r in w.Regions) foreach (var p in r.Pops)
        {
            int c = (uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1;
            size[c] += p.Size; sav[c] += p.Savings.Value; if (p.Savings.Value < 0) negSav[c] += p.Savings.Value; if (names[c] is null) names[c] = p.ClassName;
        }
        sk.Class ??= Open("class", seed);
        if (!sk.ClassHeader) { sk.Class.WriteLine("time\tnation\tyear\tclassId\tclassName\tsizeK\tsavingsStock\tnegativeSavingsStock\tearnedPerMonth\tpaidPerMonth\tsavingsToEarnedMonths"); sk.ClassHeader = true; }
        string nm = NameOf(w);
        for (int c = 0; c < NC; c++)
        {
            if (!(size[c] > 0) && names[c] is null) continue;
            double earn = flows && st.ClsMonths > 0 ? st.ClsEarn[c] / st.ClsMonths : double.NaN;
            double paid = flows && st.ClsMonths > 0 ? st.ClsPaid[c] / st.ClsMonths : double.NaN;
            sk.Class.WriteLine(string.Join('\t', time, nm, w.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), c.ToString(System.Globalization.CultureInfo.InvariantCulture), names[c] ?? "", F(size[c]), F(sav[c]), F(negSav[c]), F(earn), F(paid), F(earn > 0 ? sav[c] / earn : double.NaN)));
        }
    }

    static void StockRow(Sink sk, ulong seed, string time, World w, bool readGdp)
    {
        double popK = 0, homePopK = 0, soldierK = 0, stockValue = 0, ships = 0, steamW = 0, steamV = 0;
        var tier = new double[3];
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            double sz = r.SumPopulation(); popK += sz; if (r.Tier != RegionTier.Colonial) homePopK += sz;
            foreach (var p in r.Pops) if (p.IsSoldier) soldierK += p.Size;
            foreach (var s in r.Producers)   // 工場の蒸気動力の割合の母集団＝労働を買う工場（石炭と電力を除く）の産出×参照価格
            {
                if (!s.BuysLabor || s.GoodId == Catalog.CoalId || s.GoodId == Catalog.ElectricityId) continue;
                double v = s.Output * (s.GoodId >= 0 && s.GoodId < Catalog.RefPrices.Length ? Catalog.RefPrices[s.GoodId] : 0);
                if (!(v > 0)) continue;
                steamW += v; steamV += v * s.SteamFill;
            }
            if (r.FleetUnits is { } fa) foreach (var x in fa) ships += x;
            for (int g = 0; g < r.Markets.Length && g < Catalog.Goods.Length; g++)
            {
                var good = Catalog.Goods[g]; if (good.Tier == GoodTier.Service) continue;
                double v = Math.Max(0, r.Markets[g].Stock) * Catalog.RefPrices[g]; stockValue += v;
                tier[good.Tier == GoodTier.Staple ? 0 : good.Tier == GoodTier.Daily ? 1 : 2] += v;
            }
        }
        var cap = w.Regions.Count > 0 ? w.Regions[0] : null; var sp = w.SelfPower;
        double Stk(int g) => cap is not null && g < cap.Markets.Length ? cap.Markets[g].Stock : double.NaN;
        double gdpYear = readGdp ? Calendar.PerMonthToPerYear(w.GdpPerMonth.Value) : double.NaN;
        var cols = new List<(string Name, string Value)>();
        void C(string name, double v) => cols.Add((name, F(v)));
        cols.Add(("time", time)); cols.Add(("nation", NameOf(w))); C("year", w.Year);
        C("populationK", popK); C("homePopulationK", homePopK); C("soldierK", soldierK); C("soldierToPopulationShare", popK > 0 ? soldierK / popK : double.NaN);
        C("totalPopulationK", w.TotalPopulation); C("totalMoneyStock", w.TotalMoney.Value);   // 階級の表の合計との突き合わせ用（合計＝分母）
        C("treasuryStock", w.Treasury.Value); C("govDebtStock", w.GovDebt.Value); C("gdpRealPerYear", gdpYear);
        C("goodsStockValueStock", stockValue); C("goodsStockToGdpYear", gdpYear > 0 ? stockValue / gdpYear : double.NaN);
        C("stapleStockValueStock", tier[0]); C("dailyStockValueStock", tier[1]); C("luxuryStockValueStock", tier[2]);
        C("capitalArtilleryStock", Stk(Catalog.ArtilleryId)); C("capitalRifleStock", Stk(Catalog.RifleId)); C("capitalAmmunitionStock", Stk(Catalog.AmmunitionId)); C("capitalWarshipStock", Stk(Catalog.WarshipId));
        C("fleetUnitsStock", ships);
        C("militaryReadinessShare", w.MilitaryReadiness); C("capitalEquipFillShare", cap?.MilitaryEquipFill ?? double.NaN); C("trainingFillShare", w.MilitaryTrainingFill);
        C("armyUpkeepShare", w.ArmyUpkeep.Value); C("navyUpkeepShare", w.NavyUpkeep.Value);
        C("landPowerIndex", sp?.LandPower ?? double.NaN); C("navalPowerIndex", sp?.NavalPower ?? double.NaN);
        bool atWar = false;
        if (sp is not null && w.Diplomacy is { } dip) foreach (var war in dip.Wars) if (war.A == sp || war.B == sp) { atWar = true; break; }
        C("fleetGaugeIndex", sp?.FleetGauge ?? double.NaN); C("atWarFlag", atWar ? 1 : 0);
        C("techLevelIndex", w.TechLevel); C("railCapacityStock", w.RailCapacity); C("steamFillShare", steamW > 0 ? steamV / steamW : 0);
        sk.Stock ??= Open("stock", seed);
        if (!sk.StockHeader) { sk.Stock.WriteLine(string.Join('\t', cols.Select(c => c.Name))); sk.StockHeader = true; }
        sk.Stock.WriteLine(string.Join('\t', cols.Select(c => c.Value)));
    }
}
