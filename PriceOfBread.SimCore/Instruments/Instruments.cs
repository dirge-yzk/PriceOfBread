namespace PriceOfBread.SimCore;

/// <summary>
/// 既定は出さない：環境変数 POB_INSTRUMENTS=＆lt;出力ディレクトリ＆gt; を立てたときだけ、On が真になる（偽の間は本体から1命令も走らない＝分岐の中だけ）。〔履歴 Instruments.cs#1〕
/// </summary>
public static partial class Instruments
{
    /// <summary>計器を出すか。偽の間は本体の分岐の中へ入らない。</summary>
    public static bool On = Environment.GetEnvironmentVariable("POB_INSTRUMENTS") is { Length: > 0 };
    static string Dir = Environment.GetEnvironmentVariable("POB_INSTRUMENTS") ?? "";
    /// <summary>
    /// 月ごとの財政の行と四半期ごとの階級・在庫の行を出す国（国名＝BaseName をカンマで区切る・*＝全て）。〔履歴 Instruments.cs#2〕
    /// </summary>
    internal static double PxObserveCeil = 4.0;
    static string[] Picked = SplitNames(Environment.GetEnvironmentVariable("POB_INSTRUMENTS_NATIONS"));
    /// <summary>
    /// 未設定・空＝null＝全表（今と同じ）。〔履歴 Instruments.cs#3〕
    /// </summary>
    static HashSet<string>? TablesSel = ParseTables(Environment.GetEnvironmentVariable("POB_INSTRUMENTS_TABLES"));
    static HashSet<string>? ParseTables(string? s) { var l = SplitNames(s); return l.Length == 0 ? null : new HashSet<string>(l, StringComparer.Ordinal); }
    /// <summary>この表（<c>nation_s</c> の形）を出すか。</summary>
    internal static bool Wants(string table) => TablesSel is null || TablesSel.Contains(table);
    /// <summary>表の選びの環境変数を今読んだ値で見て、この中の 1 つでも選ばれるか（静的な初期化子から使う＝他の static 欄の初期化の順に依らない）。</summary>
    static bool EnvWantsAny(params string[] tables) { var t = ParseTables(Environment.GetEnvironmentVariable("POB_INSTRUMENTS_TABLES")); return t is null || tables.Any(x => t.Contains(x)); }
    /// <summary>試験用：出す表の選びを替える（null・空＝全表）。</summary>
    internal static void SelectTables(string? csv)
    {
        TablesSel = ParseTables(csv);
        CearnOn = System.Environment.GetEnvironmentVariable("POB_CEARN") != "0" && (TablesSel is null || Wants("cearn_s") || Wants("cgrp_s") || Wants("cpop_s"));   // 3 表とも選ばれなければ累積ごと飛ぶ（POB_CEARN=0 と同じ既存の切り口）
    }
    static string[] SplitNames(string? s) => s is { Length: > 0 } ? s.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) : [];

    /// <summary>番人（テスト）が計器を入れ替えるための口。出力ディレクトリと国の選び方を渡す（本番は環境変数）。</summary>
    internal static void Configure(string? dir, string? nations)
    {
        Dir = dir ?? ""; Picked = SplitNames(nations); On = Dir.Length > 0;
        if (On) System.IO.Directory.CreateDirectory(Dir);
    }

    internal enum Item
    {
        Interest, Welfare, Pension, ColonialUpkeep, TributeOutQ, RecruitQ, TradePolicy, PortQ,
        Army, Navy, Admin, Infra, ElecInfra, SoldierPay, HigherEdu,
        LastEducation, LastWelfare, LastFactoryBuild, LastSubsidy, StateConstruction, StateLoss, Count,
    }
    static readonly string[] ItemNames =
    [
        "claimInterestPerMonth", "claimWelfarePerMonth", "claimPensionPerMonth", "claimColonialUpkeepPerMonth", "claimTributeOutAtQuarterEnd", "claimRecruitAtQuarterEnd",
        "claimTradePolicyPerMonth", "claimPortAtQuarterEnd",
        "claimArmyPerMonth", "claimNavyPerMonth", "claimAdminPerMonth", "claimInfraPerMonth", "claimElecInfraPerMonth", "claimSoldierPayPerMonth", "claimHigherEduPerMonth",
        "claimLastMonthEducationPerMonth", "claimLastMonthWelfareSubsidyPerMonth", "claimLastMonthFactoryBuildPerMonth", "claimLastMonthSubsidyPerMonth", "claimStateConstructionPerMonth", "claimStateLossPerMonth",
    ];
    const int NC = PopClass.Count;
    const int NCat = (int)TreasuryCat.Count;

    sealed class WState
    {
        public double FleetMovedOutCeded, FleetReceivedCeded, FleetReturned;   // FleetReturned〔履歴 Instruments.cs#4〕
        public double FleetMovedOutHome, FleetMovedOutColonial;   // FleetMovedOutColonial〔履歴 Instruments.cs#5〕
        public long BurTicks; public int BurCalls;         // BurTicks〔履歴 Instruments.cs#6〕
        public string? BurRow;                             // BurRow〔履歴 Instruments.cs#7〕
        public int ModStance = -1; public double ModGapUsed = double.NaN;   // ModStance〔履歴 Instruments.cs#8〕
        public List<string>? SecRows;                      // SecRows〔履歴 Instruments.cs#9〕
        public List<string>? EpiRows;                      // EpiRows〔履歴 Instruments.cs#10〕
        public string? CoupRow; public int CoupRoute, CoupNanBranch;      // CoupRow〔履歴 Instruments.cs#11〕
        public bool Active, Armed;                         // 今月の写しが有効か／月初の Begin が走ったか
        public readonly double[] Claim = new double[(int)Item.Count];
        public double Treasury0, Payable0, Spare0, Payable1, Borrowed, Devalued, ClaimsTotal, PayRate;
        public int BorrowCode = -1;                        // -1＝借入の判定に来なかった（払えない額なし）0＝門が閉じた 1＝満額（払えない額を全部借りた）2＝1件の額が縛った 3＝プール（国内＋外資）が縛った 4＝借りられなかった（額が1e-9以下）
        public double Shortfall, Ticket, DomPool, ForPool;
        public bool HaveLedger; public double PrevTreasury; public readonly double[] PrevLedger = new double[NCat];
        public readonly List<string> Rows = new();
        public readonly List<string> SiteRows = new();     // SiteRows〔履歴 Instruments.cs#12〕
        public readonly List<string> DiscRows = new();     // DiscRows〔履歴 Instruments.cs#13〕
        public readonly List<string> EngRows = new();      // EngRows〔履歴 Instruments.cs#14〕
        public readonly List<string> ClockRows = new();    // ClockRows〔履歴 Instruments.cs#15〕
        public readonly List<string> DiscFlowRows = new();   // DiscFlowRows〔履歴 Instruments.cs#16〕
        public readonly Dictionary<(int Row, string Seller, World SellerWorld, int Good), double> DiscContact = new();   // DiscContact〔履歴 Instruments.cs#17〕
        public readonly double[] ClsEarn = new double[NC], ClsPaid = new double[NC]; public int ClsMonths;
        public double OpTensionAdded, OpTensionLevelMax; public int OpTensionPairMonths;   // OpTensionLevelMax〔履歴 Instruments.cs#18〕
        public double LuxPopMonths, LuxNonPosPopMonths, LuxInvalidPopMonths, LuxNonPosSizeSum;   // LuxNonPosSizeSum〔履歴 Instruments.cs#19〕
        public PopAcc? PopA;                               // PopA〔履歴 Instruments.cs#20〕
        public FmAcc? FmA;                                 // FmA〔履歴 Instruments.cs#21〕
        public int Sel = -1;                               // -1＝未判定 0＝挙げていない 1＝挙げた国
        public int WarMonths, CivilWarMonths;              // 今の四半期の交戦の月・内戦の月（軍の更新の直前に数える・四半期の末に0へ戻す・助走の直後にも0へ戻す）
        public int GateOpenNow = -1;                       // 今月の借入の門（-1＝判定に来なかった 0＝閉じていた 1＝開いていた）＝月初の PreBorrow が Borrow と同じ1本（Fiscal.BorrowGateOpen）で読む
        public double RevDirectNet = double.NaN, RevIndirect = double.NaN;   // 今月の純税収（汚職漏れ控除後・Fiscal.Step の revenue）と間接税（関税＋専売の実収・予算の基数へ係数を掛けて算入する前）
        public int QGateObs, QGateClosed, QShortMonths, QRUnder1Months;      // QRUnder1Months〔履歴 Instruments.cs#22〕
        public double QInterest, QRevenue, QDirect, QIndirect, QShortfall, QBorrowed;   // 今の四半期の月ごとの量の和（利払い・予算の基数・純税収・間接税・払えない額・借入）
        // AdmRBelow1Months〔履歴 Instruments.cs#23〕
        public int AdmMonths, AdmPaidMonths, AdmRBelow1Months;
        public double AdmSimClaim, AdmCatClaim, AdmCatPaid, AdmFiscClaim, AdmBase, AdmSpendable, AdmLastRev, AdmSimMul, AdmCover, AdmFiscMul, AdmPayRate, AdmFill, AdmFillRBelow1;
        // 月の予算の位置（州ごとの AdminBudget の代入）で溜め、月の決算の終わりで四半期の和へ畳む：基数 SpendableRevenue・先月の歳入・官僚の居る州の（人口の比×距離の割増）の和と人口の比の和
        public double BudSpendable = double.NaN, BudLastRev = double.NaN, BudMul, BudCover;
        // DecScale〔履歴 Instruments.cs#24〕
        public double DecScale = double.NaN, DecLosing = double.NaN, DecShare = double.NaN;
        public int DecAtWar = -1, DecMobilized = -1, DecPicked, DecOpen = -1, DecTarget = -1, DecEnforced = -1;
        public int FacStarted, FacStartedClosed, FacDone, FacDoneClosed, FacDoneClosedAtStart;   // 今の四半期の国営の棟の着工・完成（門が閉じていた分つき・四半期の末に0へ戻す）
        // SavLedger〔履歴 Instruments.cs#25〕
        public readonly Dictionary<(int Cls, int Kind, string File, int Line, char Cp), double[]> SavLedger = new();
        // SubMismatchOff〔履歴 Instruments.cs#26〕
        public int FsRun, FsSeenMismatch, FsFired, FsFiredMismatch, FsFiredOver, SubMismatch, SubMismatchOff;
        public readonly List<string> WheelGoods = new(), FacStartedGoods = new();   // 食い違いの財の名前（「投げ売り:」「補助:」を前置）・着工した財の名前
        // BondMonths〔履歴 Instruments.cs#27〕
        public int BondMonths;
        public double BondBase, BondDebt, BondFisher, BondDistrust, BondPolicy, BondGold, BondRepr, BondClamp, BondRate, BondDebtRatio, BondConvMul, BondReprMul;
        // AdminClaims〔履歴 Instruments.cs#28〕
        public readonly Dictionary<Region, double> AdminClaims = new();
        public double BudClaim, BudNeedB, BudMinB, BudNeedP, BudMinP; public int BudOverRegions;
        public double QAdmClaim, QAdmNeedB, QAdmMinB, QAdmNeedP, QAdmMinP, QCfBorrowed; public int QAdmOverRegionMonths, QCfShortMonths, QCfRBelow1Months, QCfObs;
        // CapSnap〔履歴 Instruments.cs#29〕
        public readonly Dictionary<Region, double[]> CapSnap = new();
        public double QCapPaid, QCapPopK;
        public readonly double[] QCapBoundPaid = new double[CapDs.Length], QCapCut = new double[CapDs.Length], QCapBoundPopK = new double[CapDs.Length];
        // MonoBefore〔履歴 Instruments.cs#30〕
        public int FactorySold, MonoBefore = -1, MonoTarget = -1; public double FiscalStressAtDecision = double.NaN;
        public double PushBurden = double.NaN, PushP = double.NaN, PushU = double.NaN; public int PushHit;   // PushBurden〔履歴 Instruments.cs#31〕
        // Uid〔履歴 Instruments.cs#32〕
        public int Uid;                                    // 国の個体の番号（0＝未採番）＝World の個体に付く＝改名でも変わらず、建国は新しい番号（四半期の行を書く順に採番＝決定論）
        public long CnRows, CnBound; public double CnDesired, CnDemand;   // 消費の (POP×財) の行：行数・予算で切られた行数（需要＜希望）・希望額と立った需要額（金額建て・月の和）
        public int ConDesiring, ConBound; public double ConDesiredUnits, ConCutUnits;   // 建設：希望が正の（州×月）・うち投資枠で切られた数・希望の束・切られた束
        public int BrJudged, BrPoolBound, BrForeignCut; public double BrDomPool, BrForRaw, BrFor;   // 借入の判定の月：判定した月・プールが縛った月・うち外資のプールが他国の投資家の貯蓄で切られていた月・各プールの和
        public int CrDays, CrRepayBoundDays, CrFiniteDays; public double CrReserveCap, CrRepayable;   // 与信の上限（日数で重みづけ）：判定の日数・返せる額が準備の側より小さかった日数・有限の日数・各上限の日数つきの和
        // -1／NaN＝この四半期は来なかった〔履歴 Instruments.cs#33〕
        public int MmOutcome = -1, MmFronts = -1; public bool MmIncreasing;
        public double MmCur = double.NaN, MmTarget = double.NaN, MmNet = double.NaN, MmResist = double.NaN, MmSpeed = double.NaN, MmPush = double.NaN;
        public double MmOppTrad = double.NaN, MmOppModern = double.NaN, MmOppMass = double.NaN, MmStock = double.NaN, MmStockMax = double.NaN, MmEnvelopeAfterGov = double.NaN;
        // Government.CountOpenFronts が数えた開いた戦線（数えた順）と、Step がその四半期に出した結果。〔履歴 Instruments.cs#34〕
        public readonly List<GFront> Gf = new(); public bool GfCounted, GfMilExpired; public int GfLevelIdx = -1, GfGateWhy; public double GfGateNet = double.NaN;
        // 段の法のループの頭／Step の末尾の目減り S と最大量 b、その四半期に払った額（段の法・税と支出の段 R・軍事費 M・関税 T・治安維持力 C）〔履歴 Instruments.cs#35〕
        public double GfSHead = double.NaN, GfMaxHead = double.NaN, GfSEnd = double.NaN, GfPendCost = double.NaN, GfPendS = double.NaN, GfGateCost = double.NaN, GfGateS = double.NaN;
        public double GfPaidL, GfPaidR, GfPaidM, GfPaidT, GfPaidC;
        public double RfSpendApplied, RfSpendLegitOnly, RfSpendShareMax, RfConcede;   // RfConcede〔履歴 Instruments.cs#36〕
        public int RfWallLaw, RfWallSlider, RfPrevQ = -1, RfBlkLaw, RfBlkSlider, RfBlkFrozen, RfBlkLegit; public double RfWallShareMax;   /* RfPrevQ〔履歴 Instruments.cs#37〕 */ public double RfWallCost, RfCumEduPrev = double.NaN; public string RfWallAxes = "";   // RfPrevQ〔履歴 Instruments.cs#38〕
        // PxMonthGameN〔履歴 Instruments.cs#39〕
        public int PxWarmN, PxGameN, PxMonthWarmN, PxMonthGameN;                     // 助走／試合の何回目か（四半期の更新・月の決算の数）
        public readonly List<string> PxRows = new(), PxGoodsRows = new(), PxMonthRows = new(); public string? PxHdr, PxMonthHdr;
        public readonly List<string> PfixRows = new(), PjumpRows = new();   // PfixRows〔履歴 Instruments.cs#40〕
        public int PfixMonths; public double PfixOld, PfixNew, PfixBroadMin = double.PositiveInfinity;   // PfixMonths〔履歴 Instruments.cs#41〕
        public readonly List<string> FsxRows = new();   // FsxRows〔履歴 Instruments.cs#42〕
        public int FsxDevMonths, FsxGateMonths, FsxOverMonths; public double FsxPForceSum, FsxPForceMax, FsxPMSum, FsxPSSum, FsxPSMax, FsxFloorSum, FsxDoneSum, FsxAboveFloorSum, FsxExcessWouldSum;   // FsxOverMonths〔履歴 Instruments.cs#43〕
        public readonly List<string> DcRows = new();   // DcRows〔履歴 Instruments.cs#44〕
        public int PcWarmN, PcGameN; public readonly List<string> PcRows = new();   // PcGameN〔履歴 Instruments.cs#45〕
        public readonly List<string> PinitRows = new();   // PinitRows〔履歴 Instruments.cs#46〕
        public double PxResetSince; public int PxResetCount;   // 国庫を助走の前の値へ戻した額の和（戻した値 − 戻す前の国庫＝正は基礎貨幣の創出・負は消滅）と回数。四半期の更新の行で読んで0へ戻す
        public readonly double[] PxEarn = new double[NC], PxPaid = new double[NC]; public int PxMonths;   // 今の四半期の月ごとの所得と支出の和（四半期の更新で読んで0へ戻す）
        // LimitPrev＝前の月の「利払いが限界に届いたか」は四半期をまたぐ（助走の月は -1 へ戻す）〔履歴 Instruments.cs#47〕
        public double GateInt = double.NaN, GateRev = double.NaN;   // PreBorrow で門が読んだ直近の利払いと歳入（月の決算の終わりの値とは別＝Fiscal.Step が月末に書き換える前）
        public int LimitNow = -1, Over067Now = -1, LimitPrev = -1;  // 今月の門の入力で 利払い÷歳入が限界に届いたか／仏1788 の線 0.67 を超えたか（-1＝判定に来なかった 0＝いいえ 1＝はい）・前の月の LimitNow。〔履歴 Instruments.cs#48〕
        public bool WarNow;                                         // 今月交戦中か（WarMonth が軍の更新の直前に立てる・Begin で下ろす）
        public int QLimitMonths, QLimitCrossings, QLimitBorrowMonths, QZombieMonths, QWarGateClosedMonths, QOver067Months, QOver067BorrowedMonths, QOver067RBelow1Months;
        public int QUnpaidObs, QUnpaidMonths;
        public double QUnpaidSum, QUnpaidMax, QDevalueShareSum, QDevalueShareMax;
        // QSpend〔履歴 Instruments.cs#49〕
        public double[]? QSpend;                           // 今の四半期の月ごとの支払いの和：[(階級 × SpendW + 種類) × 3 + {支払い額, 数量, 生産者の受取}]（EconomyStep の支払いの位置が積む）
        // CeBought〔履歴 Instruments.cs#50〕
        public double[]? CeC, CeNum, CeDen, CeBasket, CeBought; public EntryCtx? Ent;
        public int ConRegionMonths;                        // 建設の判定に来た州×月
        public double ConPlannedMoney, ConBudgetBoundMoney, ConBuiltUnits, ConPlannedUnits; public readonly double[] ConRationW = new double[5];   // 計画した（枠で切った後の）金額／枠が切った州×月の枠の金額／建った束／計画した束／計画した束で重みづけた配給率（鋼・機械・セメント・製材・労働）の和
        public double ConGainSum, ConHurdleSum, ConBudgetMoney, ConDesiredMoney;   // 限界収益・資本コストの州×月の和／投資枠の金額／希望の金額（希望の束×束の費用×(1+工賃)）
    }
    /// <summary>
    /// Kind＝L 段の法〔Idx＝LevelLaws の表の行〕／R 税・支出の段〔Idx＝RateLaws の行〕／M 軍事費の合計／C 治安維持力／T 関税。〔履歴 Instruments.cs#51〕
    /// </summary>
    internal sealed class GFront { public char Kind; public int Idx; public PolicyAxis Axis; public double Cur, Target, Net = double.NaN; public int Code = -1; public bool Expired;
        /// <summary>
        /// 無償で通った＝0・門の手前で止まった＝NaN）と、払う前（決める時）の目減り S。〔履歴 Instruments.cs#52〕
        /// </summary>
        public double Cost = double.NaN, SDec = double.NaN; public bool Structural; }
    /// <summary>
    /// 段5 の上限の引き出し率 d（月あたり）。〔履歴 Instruments.cs#53〕
    /// </summary>
    internal static readonly double[] CapDs = [1.0 / 120, 1.0 / 240, 1.0 / 480, 1.0 / 36, 1.0 / 12];   // CapDs〔履歴 Instruments.cs#54〕
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
        public int NextUid;  // NextUid〔履歴 Instruments.cs#55〕
        public readonly Dictionary<string, (int Quarter, int Year)> CanalOpened = new();   // CanalOpened〔履歴 Instruments.cs#56〕
        public readonly Dictionary<(int Uid, int Bucket), SpendAcc> Spend = new();         // Spend〔履歴 Instruments.cs#57〕
        public System.IO.StreamWriter? PfixW, PjumpW, FsxW; public bool PfixHeader, PjumpHeader, FsxHeader;   // FsxW〔履歴 Instruments.cs#58〕
        public System.IO.StreamWriter? DevCapW; public bool DevCapHeader;   // DevCapW〔履歴 Instruments.cs#59〕
        public System.IO.StreamWriter? PClamp; public bool PClampHeader;   // PClamp〔履歴 Instruments.cs#60〕
        public System.IO.StreamWriter? Pinit; public bool PinitHeader;   // Pinit〔履歴 Instruments.cs#61〕
        public System.IO.StreamWriter? Site, Metal, MetalNat; public bool SiteHeader, MetalHeader, MetalNatHeader;   // MetalNat〔履歴 Instruments.cs#62〕
        public System.IO.StreamWriter? Eng; public bool EngHeader;                         // Eng〔履歴 Instruments.cs#63〕
        public System.IO.StreamWriter? Clock; public bool ClockHeader;                     // Clock〔履歴 Instruments.cs#64〕
        public System.IO.StreamWriter? Disc; public bool DiscHeader;                     // Disc〔履歴 Instruments.cs#65〕
        public System.IO.StreamWriter? DiscFlow; public bool DiscFlowHeader;               // DiscFlow〔履歴 Instruments.cs#66〕
        public System.IO.StreamWriter? DiscContact; public bool DiscContactHeader;         // DiscContact〔履歴 Instruments.cs#67〕
        public System.IO.StreamWriter? Svc; public bool SvcHeader;                         // Svc〔履歴 Instruments.cs#68〕
        public System.IO.StreamWriter? Cons; public bool ConsHeader;                       // Cons〔履歴 Instruments.cs#69〕
        public System.IO.StreamWriter? Consf; public bool ConsfHeader;                     // Consf〔履歴 Instruments.cs#70〕
        public System.IO.StreamWriter? Port; public bool PortHeader;                       // Port〔履歴 Instruments.cs#71〕
        public System.IO.StreamWriter? Opium; public bool OpiumHeader;                     // Opium〔履歴 Instruments.cs#72〕
        public System.IO.StreamWriter? Ce; public bool CeHeader;                           // Ce〔履歴 Instruments.cs#73〕
        public System.IO.StreamWriter? PopS; public bool PopSHeader;                       // PopS〔履歴 Instruments.cs#74〕
        public System.IO.StreamWriter? AgeS; public bool AgeSHeader;                       // AgeS〔履歴 Instruments.cs#75〕
        public System.IO.StreamWriter? Fm; public bool FmHeader;                           // Fm〔履歴 Instruments.cs#76〕
        public System.IO.StreamWriter? Sca; public bool ScaHeader;                         // Sca〔履歴 Instruments.cs#77〕
        public System.IO.StreamWriter? Ivf; public bool IvfHeader;                         // Ivf〔履歴 Instruments.cs#78〕
        public System.IO.StreamWriter? TrBin, TrRoute, TrNat, TrStat; public bool TrHeader; public System.IO.StreamWriter? TrRail;   /* TrStat〔履歴 Instruments.cs#79〕 */   // TrStat〔履歴 Instruments.cs#80〕
        public System.IO.StreamWriter? Bur; public bool BurHeader;                         // Bur〔履歴 Instruments.cs#81〕
        public System.IO.StreamWriter? Coup; public bool CoupHeader;                       // Coup〔履歴 Instruments.cs#82〕
        public System.IO.StreamWriter? Anc; public bool AncHeader;                         // Anc〔履歴 Instruments.cs#83〕
        public System.IO.StreamWriter? Cap; public bool CapHeader; public System.IO.StreamWriter? Ide; public bool IdeHeader;   // Cap〔履歴 Instruments.cs#84〕
        public System.IO.StreamWriter? Sec; public bool SecHeader;                         // Sec〔履歴 Instruments.cs#85〕
        public System.IO.StreamWriter? UrxRt; public bool UrxRtHeader;   // UrxRt〔履歴 Instruments.cs#86〕
        public System.IO.StreamWriter? Xmig; public bool XmigHeader;                        // Xmig〔履歴 Instruments.cs#87〕
        public System.IO.StreamWriter? Urx, UrxPdW; public bool UrxHeader, UrxPdHeader;   // UrxPdW〔履歴 Instruments.cs#88〕
        public System.IO.StreamWriter? Inpf, Inpb; public bool InpHeader;                   // Inpb〔履歴 Instruments.cs#89〕
        public System.IO.StreamWriter? Epi; public bool EpiHeader;                         // Epi〔履歴 Instruments.cs#90〕
        public System.IO.StreamWriter? Csc; public bool CscHeader;                         // Csc〔履歴 Instruments.cs#91〕
        public System.IO.StreamWriter? Rule; public bool RuleHeader;                       // Rule〔履歴 Instruments.cs#92〕
        public System.IO.StreamWriter? Reform; public bool ReformHeader;                   // Reform〔履歴 Instruments.cs#93〕
        public System.IO.StreamWriter? AdmRule; public bool AdmRuleHeader;                 // AdmRule〔履歴 Instruments.cs#94〕
        public System.IO.StreamWriter? Ed; public bool EdHeader;                           // Ed〔履歴 Instruments.cs#95〕
        public System.IO.StreamWriter? Fd; public bool FdHeader;                           // Fd〔履歴 Instruments.cs#96〕
        public System.IO.StreamWriter? EqS, EqR; public bool EqSHeader, EqRHeader;          // EqR〔履歴 Instruments.cs#97〕
        public System.IO.StreamWriter? Sf; public bool SfHeader;                           // Sf〔履歴 Instruments.cs#98〕
        public System.IO.StreamWriter? Aib; public bool AibHeader;                         // Aib〔履歴 Instruments.cs#99〕
        public System.IO.StreamWriter? Lab; public bool LabHeader;                         // Lab〔履歴 Instruments.cs#100〕
        public System.IO.StreamWriter? SbU, SbR, SbRReg; public bool SbUHeader, SbRHeader, SbRRegHeader;                  // SbRReg〔履歴 Instruments.cs#101〕
        public System.IO.StreamWriter? Pay; public bool PayHeader;                         // Pay〔履歴 Instruments.cs#102〕
        public System.IO.StreamWriter? House; public bool HouseHeader;                     // House〔履歴 Instruments.cs#103〕
        public System.IO.StreamWriter? Lwk; public bool LwkHeader;                         // Lwk〔履歴 Instruments.cs#104〕
        public System.IO.StreamWriter? Chem, ChemGood; public bool ChemHeader, ChemGoodHeader;   // ChemGood〔履歴 Instruments.cs#105〕
        public System.IO.StreamWriter? Fb; public bool FbHeader;   // Fb〔履歴 Instruments.cs#106〕
        public System.IO.StreamWriter? Wp; public bool WpHeader;   // Wp〔履歴 Instruments.cs#107〕
        public System.IO.StreamWriter? ModSig, ScoreS; public bool ModSigHeader, ScoreSHeader;   // ScoreS〔履歴 Instruments.cs#108〕
        public System.IO.StreamWriter? Lawgate2; public bool Lawgate2Header;                // Lawgate2〔履歴 Instruments.cs#109〕
        public System.IO.StreamWriter? LawGate; public bool LawGateHeader;                 // LawGate〔履歴 Instruments.cs#110〕
        public System.IO.StreamWriter? SvcBuy; public bool SvcBuyHeader;                   // SvcBuy〔履歴 Instruments.cs#111〕
        public System.IO.StreamWriter? Dcont; public bool DcontHeader;                       // Dcont〔履歴 Instruments.cs#112〕
        public readonly Dictionary<(int Uid, int Win), CeAcc> CeN = new();                 // CeN〔履歴 Instruments.cs#113〕
    }
    /// <summary>
    /// 区切り（四半期の番号 q）＝[0,4) [4,8) [8,40) [40,160) [160,∞)。〔履歴 Instruments.cs#114〕
    /// </summary>
    internal sealed class SpendAcc
    {
        public string Nation = ""; public bool Picked; public int Months;
        public readonly double[] Spend = new double[NC * SpendW * SpendStride];
        public readonly double[] Earn = new double[NC], Paid = new double[NC], SizeMonths = new double[NC];   // 階級の所得・支出（月の和）・人数の月数の和（千人×月）
        public readonly string?[] Names = new string?[NC];
    }
    /// <summary>
    /// 区切り4 は「Q161〜」から「Q161〜Q188」になった）。〔履歴 Instruments.cs#115〕
    /// </summary>
    internal static readonly int[] SpendBucketStart = [0, 4, 8, 40, 160, 188, 228, 268, 400];
    static int SpendBucketOf(int q) { int b = 0; for (int i = 1; i < SpendBucketStart.Length; i++) if (q >= SpendBucketStart[i]) b = i; return b; }
    /// <summary>支払いの種類の幅＝財の数＋下の「財でない支払い」。</summary>
    internal static readonly int SpendW = Catalog.Goods.Length + 8;
    /// <summary>
    /// 〔履歴 Instruments.cs#116〕
    /// </summary>
    const int SpendStride = 4;
    /// <summary>財でない支払い（POP の <c>PaidPerMonth</c> に入る物）の種類。財の番号の後ろに並べる。</summary>
    internal enum SpendExtra { Construction = 0, ConstructionRail = 1, InfraMaintenance = 2, Grid = 3, ReignitionOrder = 4, RebelArms = 5 }
    static readonly string[] SpendExtraNames = ["建設の材料と工賃", "建設に付く線路と機関車", "インフラ保守", "送電網", "止まった工場の再開の発注", "反乱勢力の武器"];
    /// <summary>外交の記録（戦争の始まりと戦死の月ごとの和）。外交は MultiNationSim に1つ＝世界ごとの状態（<see cref="WState"/>）とは別に持つ。</summary>
    sealed class DipState
    {
        public readonly List<(int Year, War War)> Wars = new();
        public readonly Dictionary<(int Year, string Nation), double[]> Cas = new();   // [0]＝月数 [1]＝兵士の月和（千人×月）[2]＝戦死（千人）
        public readonly List<(int Year, string Invader, string Target, bool TargetIsNation, bool Submitted, double Added, double AddedToTarget)> Invades = new();   // Invades〔履歴 Instruments.cs#117〕
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
        CearnRegister(w);   // CearnRegister〔履歴 Instruments.cs#118〕
        if (IsPicked(w, st)) RegisterPops(w, st);   // RegisterPops〔履歴 Instruments.cs#119〕
        st.Active = !w.WarmupFrozen; st.Armed = true;
        if (!st.Active) { st.HaveLedger = false; st.LimitPrev = -1; return; }   // HaveLedger〔履歴 Instruments.cs#120〕
        Array.Clear(st.Claim);
        st.Treasury0 = w.Treasury.Value; st.Payable0 = st.Spare0 = st.Payable1 = st.Borrowed = st.Devalued = st.ClaimsTotal = st.PayRate = double.NaN;
        st.BorrowCode = -1; st.Shortfall = st.Ticket = st.DomPool = st.ForPool = double.NaN;
        st.GateOpenNow = -1; st.RevDirectNet = st.RevIndirect = double.NaN;
        st.LimitNow = st.Over067Now = -1; st.GateInt = st.GateRev = double.NaN; st.WarNow = false;   // LimitNow〔履歴 Instruments.cs#121〕
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
        c[(int)Item.SoldierPay] += r.SoldierPayBudget; c[(int)Item.HigherEdu] += r.HigherEduFunding;
        c[(int)Item.LastEducation] += regrow * Math.Max(0, r.StateSpent[(int)TreasuryCat.Education]);
        c[(int)Item.LastWelfare] += regrow * Math.Max(0, r.StateSpent[(int)TreasuryCat.Welfare]);
        c[(int)Item.LastFactoryBuild] += regrow * Math.Max(0, r.StateSpent[(int)TreasuryCat.FactoryBuild]);
        c[(int)Item.LastSubsidy] += regrow * Math.Max(0, r.StateSpent[(int)TreasuryCat.Subsidy]);
        // AdminBudget〔履歴 Instruments.cs#122〕
        st.AdminClaims[r] = r.AdminBudget; st.BudClaim += r.AdminBudget;
        if (AdminNeedOf(w, r) is { } nb) { st.BudNeedB += nb; st.BudMinB += Math.Min(r.AdminBudget, nb); if (r.AdminBudget > nb) st.BudOverRegions++; }
    }

    /// <summary>
    /// 官僚の居ない州は購買が無い＝null。〔履歴 Instruments.cs#123〕
    /// </summary>
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

    /// <summary>
    /// 項は w.Explain の "BondRate" の各項と同じ式（Explain は立てない＝全域の Explain 経路を動かさない）。〔履歴 Instruments.cs#124〕
    /// </summary>
    internal static void BondRate(World w, double baseTerm, double debtTerm, double fisherTerm, double distrustTerm, double policyTerm, double goldMemo, double reprTerm, double clampTerm, double debtRatio, double convMul, double reprMul)
    {
        if (w.WarmupFrozen) return;
        var st = S(w);
        st.BondMonths++; st.BondBase += baseTerm; st.BondDebt += debtTerm; st.BondFisher += fisherTerm; st.BondDistrust += distrustTerm; st.BondPolicy += policyTerm;
        st.BondGold += goldMemo; st.BondRepr += reprTerm; st.BondClamp += clampTerm; st.BondRate += w.GovBondRatePerYear.Value; st.BondDebtRatio += debtRatio; st.BondConvMul += convMul; st.BondReprMul += reprMul;
    }

    /// <summary>
    /// 上限の前月の所得 E は Pop.LastEarnedPerMonth（EconomyStep が S の直前に写す物）を月の決算の終わりに読む。〔履歴 Instruments.cs#125〕
    /// </summary>
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
    /// <summary>
    /// 〔履歴 Instruments.cs#126〕
    /// </summary>
    internal static void MonopolyPush(World w, double burden, double p, double u, bool hit) { var st = S(w); st.PushBurden = burden; st.PushP = p; st.PushU = u; st.PushHit = hit ? 1 : 0; }

    /// <summary>借りる前（月初の払える額と余剰が決まった所）。</summary>
    internal static void PreBorrow(World w, double payable, double spare)
    {
        if (Live(w) is not { } st) return;
        st.Payable0 = payable; st.Spare0 = spare;
        st.GateOpenNow = Fiscal.BorrowGateOpen(w, w.Coef) ? 1 : 0;   // 次の行の Fiscal.Borrow が読むのと同じ門（純粋な関数＝読むだけ）
        // 歳入が正の月だけ判定〔履歴 Instruments.cs#127〕
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
        // domesticPool〔履歴 Instruments.cs#128〕
        st.BrJudged++; st.BrDomPool += domesticPool; st.BrFor += foreignPool; st.BrForRaw += foreignPoolRaw;
        if (st.BorrowCode >= 3) { st.BrPoolBound++; if (foreignPool < foreignPoolRaw) st.BrForeignCut++; }
    }
    /// <summary>
    /// BudgetBindDiag と同じ行・同じ述語を標準の計器へ畳んだ（あちらは On の間は各国の日次を逐次へ倒す診断＝バッチでは使えない）。〔履歴 Instruments.cs#129〕
    /// </summary>
    internal static void ConsumeMonth(World w, long rows, long bound, double desiredVal, double demandVal)
    {
        var st = S(w);
        st.CnRows += rows; st.CnBound += bound; st.CnDesired += desiredVal; st.CnDemand += demandVal;
    }
    /// <summary>
    /// 希望が正の州×月だけ数える。〔履歴 Instruments.cs#130〕
    /// </summary>
    internal static void Construction(World w, double desiredUnits, double budgetUnits, double gain = 0, double hurdle = 0, double unitMoney = 0, double planned = 0)
    {
        var st = S(w);
        // gain〔履歴 Instruments.cs#131〕
        st.ConRegionMonths++; st.ConGainSum += gain; st.ConHurdleSum += hurdle;
        st.ConBudgetMoney += budgetUnits * unitMoney; if (desiredUnits > 0) st.ConDesiredMoney += desiredUnits * unitMoney;
        st.ConPlannedMoney += planned * unitMoney; if (desiredUnits > 0 && budgetUnits < desiredUnits) st.ConBudgetBoundMoney += budgetUnits * unitMoney;
        if (!(desiredUnits > 0)) return;
        st.ConDesiring++; st.ConDesiredUnits += desiredUnits;
        if (budgetUnits < desiredUnits) { st.ConBound++; st.ConCutUnits += desiredUnits - budgetUnits; }
    }
    // ConstructionFill〔履歴 Instruments.cs#132〕

    /// <summary>
    /// 計画の束で重みづけて積む。〔履歴 Instruments.cs#133〕
    /// </summary>
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

    // CeSize〔履歴 Instruments.cs#134〕

    // 階級ごとの欄（州と月をまたいで足した月の和。四半期の行で月数で割る）。欄の番号と CeColNames の並びは一致させる。
    const int CeSize = 0, CeEarnLast = 1, CeEarnLastPos = 2, CeSavStart = 3, CeBudStart = 4,
        CeWant = 5 /* 5..8＝食料・日用品・奢侈品・役務その他 */, CeDem = 9 /* 9..11＝食料・日用品・奢侈品 */, CeSWant = 12 /* 12..14＝食料・日用品・奢侈品 */, CePay = 15 /* 15..18＝食料・日用品・奢侈品・役務その他 */,
        CeConsPaid = 19, CeUnfilled = 20, CeRemainder = 21, CeRemainderBasket = 22, CeBasketGap = 23, CeFoodGap = 24, CeRestStock = 25, CeShadowBud = 26, CeShadowCut = 27,
        CeDraw = 28, CeFoodDraw = 29, CeBasketDraw = 30, CeCoveredPopK = 31, CeFoodUnaffordPopK = 32, CeNegDemand = 33, CeNegBudgetPopK = 34, CeK = 35;
    /// <summary>
    /// 〔履歴 Instruments.cs#135〕
    /// </summary>
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

    /// <summary>
    /// EconomyStep.Run の消費の財のループの口から呼ぶ（Instruments.On の中だけ・読むだけ＝力学の値を1つも書かない）。〔履歴 Instruments.cs#136〕
    /// </summary>
    internal sealed class EntryCtx
    {
        readonly double[] cC, cNum, cDen, cBasket, cBought; Region reg = null!; int np;
        int[] cls = [];
        internal bool U4;   // U4〔履歴 Instruments.cs#137〕
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
                if (U4 && i == reg.InvestorPopIndex) U4ConsStash(reg, i, cons);   // InvestorPopIndex〔履歴 Instruments.cs#138〕
            }
        }
    }
    /// <summary>
    /// 助走の月は null（何も積まない）。〔履歴 Instruments.cs#139〕
    /// </summary>
    internal static EntryCtx? EntryBegin(World w, Region r)
    {
        if (w.WarmupFrozen) return null;
        var st = S(w);
        int ng = Catalog.Goods.Length;
        st.CeC ??= new double[NC * CeK];
        st.CeNum ??= new double[ng]; st.CeDen ??= new double[ng]; st.CeBasket ??= new double[ng]; st.CeBought ??= new double[ng];
        var e = st.Ent ??= new EntryCtx(st.CeC, st.CeNum, st.CeDen, st.CeBasket, st.CeBought);
        e.Begin(r);
        e.U4 = IsPicked(w, st);   // U4〔履歴 Instruments.cs#140〕
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

    /// <summary>
    /// 〔履歴 Instruments.cs#141〕
    /// </summary>
    static void SvcRow(Sink sk, int q, World w)
    {
        int id = Catalog.DomesticServiceId;
        double capacity = 0, desired = 0;   // capacity〔履歴 Instruments.cs#142〕
        double servantK = 0, noble = 0, cap = 0, merch = 0, totalK = 0, output = 0, sold = 0, shortage = 0, demand = 0, priceW = 0, produced = 0, rationW = 0;
        foreach (var r in w.Regions)
        {
            if (id >= r.Markets.Length) continue;
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

    /// <summary>
    /// 助走の月は数えない。〔履歴 Instruments.cs#143〕
    /// </summary>
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
    /// <summary>
    /// 指数 0（旧の切り）では切り下げ額＝切りで止まった月、指数が正なら（既定 1＝切り無し）切りを超えて取れた月。〔履歴 Instruments.cs#144〕
    /// </summary>
    internal static void DevCap(World w, double room, double cap, double devalued)
    {
        if (!(cap > 0) || !(room > cap)) return;
        var st = S(w);
        st.DcRows.Add(string.Join('\t', (w.WarmupFrozen ? "w" + st.PcWarmN : "q" + st.PcGameN), NameOf(w), F(w.Year), F(room), F(cap), F(devalued), F(room / cap), F(w.Coef.DevaluationCapReliefExp)));
    }
    /// <summary>
    /// 〔履歴 Instruments.cs#145〕
    /// </summary>
    internal static double FiatPressureM(double devalue, double broadAfter) => devalue / Math.Max(broadAfter, devalue);
    /// <summary>
    /// 旧の式（絶対値の床 1.0）と M 案（FiatPressureM）の分母が違う月だけ、その四半期の和へ溜める（旧の式なら圧が割合を越えた月＝焼き込んだ分母が効いた月。〔履歴 Instruments.cs#146〕
    /// </summary>
    internal static void PfixMonth(World w, double devalue, double broadAfter)
    {
        double oldDen = Math.Max(1.0, broadAfter), newDen = Math.Max(broadAfter, devalue);
        if (oldDen == newDen) return;
        var st = S(w);
        st.PfixMonths++; st.PfixOld += devalue / oldDen; st.PfixNew += FiatPressureM(devalue, broadAfter);   if (broadAfter < st.PfixBroadMin) st.PfixBroadMin = broadAfter;
    }
    /// <summary>
    /// pForce＝実際に積んだ圧（S 案）。〔履歴 Instruments.cs#147〕
    /// </summary>
    internal static void FsxPressure(World w, double devalue, double totalSav, double issue, double broadAfter, double pForce)
    {
        var st = S(w);
        double pM = FiatPressureM(devalue, broadAfter), pS = Fiscal.FiatPressureOfLedger(devalue, totalSav, issue);
        st.FsxDevMonths++; st.FsxPForceSum += pForce; st.FsxPMSum += pM; st.FsxPSSum += pS;
        if (pForce > st.FsxPForceMax) st.FsxPForceMax = pForce;
        if (pS > st.FsxPSMax) st.FsxPSMax = pS;
    }
    /// <summary>
    /// 読むだけ。〔履歴 Instruments.cs#148〕
    /// </summary>
    internal static void FsxRepay(World w, double floorRepay, double done, double excess)
    {
        var st = S(w);
        st.FsxGateMonths++; st.FsxFloorSum += floorRepay; st.FsxDoneSum += done; st.FsxAboveFloorSum += done - floorRepay; st.FsxExcessWouldSum += excess;
        if (excess > 0) st.FsxOverMonths++;
    }
    /// <summary>率 r が決まった所（請求の合計・払える額〔借入と切り下げの後〕・r・この月の切り下げ）。</summary>
    internal static void Rate(World w, double claims, double payable, double payRate, double devalued)
    {
        if (Live(w) is not { } st) return;
        st.ClaimsTotal = claims; st.Payable1 = payable; st.PayRate = payRate; st.Devalued = devalued;
    }

    // AdminBase〔履歴 Instruments.cs#149〕

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
        if (!double.IsNaN(st.PayRate)) st.AdmPayRate += st.PayRate;   // AdmPayRate〔履歴 Instruments.cs#150〕
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
    /// <summary>
    /// outcome：0＝目標＝現在で動かず／1＝純支持が余白に届かず固定／2＝政治資本の在庫が代価に届かず固定／3＝無償で動いた／4＝政治資本を払って動いた。〔履歴 Instruments.cs#151〕
    /// </summary>
    internal static void MilitaryMove(World w, double cur, double target, double net, double resist, double speed, int outcome, in PolicyPreferences.Profile e, bool increasing)
    {
        var st = S(w);
        st.MmOutcome = outcome; st.MmCur = cur; st.MmTarget = target; st.MmNet = net; st.MmResist = resist; st.MmSpeed = speed; st.MmPush = w.PoliticalPushBudget;
        st.MmStock = w.MandateSpent; st.MmStockMax = w.PoliticalCapitalMax; st.MmIncreasing = increasing;
        if (e.Power is null) return;
        FmNet(w, e, increasing);   // FmNet〔履歴 Instruments.cs#152〕
        double sign = increasing ? 1 : -1;
        double p0 = PolicyPreferences.Pref(PolicyAxis.Coercion, 0, e, w) * sign, p1 = PolicyPreferences.Pref(PolicyAxis.Coercion, 1, e, w) * sign, p2 = PolicyPreferences.Pref(PolicyAxis.Coercion, 2, e, w) * sign;
        st.MmOppTrad = p0 < 0 ? e.Power[0] * -p0 : 0; st.MmOppModern = p1 < 0 ? e.Power[1] * -p1 : 0; st.MmOppMass = p2 < 0 ? e.Power[2] * -p2 : 0;
    }
    /// <summary>
    /// 〔履歴 Instruments.cs#153〕
    /// </summary>
    internal static void PushFronts(World w, int fronts) => S(w).MmFronts = fronts;
    // FrontsBegin〔履歴 Instruments.cs#154〕
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
        // 払って通った額は四半期の台帳へ（Enact 経由で同じ行が StepLevel を複数回呼んでも落とさない）〔履歴 Instruments.cs#155〕
        if (why is 2 or 4) { st.GfGateCost = st.GfPendCost; st.GfGateS = st.GfPendS; if (why == 4) st.GfPaidL += st.GfPendCost; }
        else { st.GfGateCost = why == 3 ? 0 : double.NaN; st.GfGateS = double.NaN; }
        st.GfPendCost = st.GfPendS = double.NaN;
    }
    /// <summary>
    /// 〔履歴 Instruments.cs#156〕
    /// </summary>
    static bool IsStructural(World w, int code, double cost) => code == 2 && cost > w.PoliticalCapitalMax;
    /// <summary>
    /// 〔履歴 Instruments.cs#157〕
    /// </summary>
    internal static void Priced(World w, double cost) { var st = S(w); st.GfPendCost = cost; st.GfPendS = w.MandateSpent; }
    /// <summary>
    /// 〔履歴 Instruments.cs#158〕
    /// </summary>
    internal static void StockAtLevelHead(World w) { var st = S(w); st.GfSHead = w.MandateSpent; st.GfMaxHead = w.PoliticalCapitalMax; }
    /// <summary>
    /// 〔履歴 Instruments.cs#159〕
    /// </summary>
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
        bool gated = code == -1;   // gated〔履歴 Instruments.cs#160〕
        if (code == -1) code = st.GfGateWhy switch { 1 => 1, 2 => 2, 4 => result == ti ? 8 : 4, _ => result == ti ? 7 : 3 };
        if (FindFront(st, 'L', st.GfLevelIdx) is { } f)
        {
            f.Code = code; f.Net = code is 0 or 5 or 6 ? double.NaN : st.GfGateNet;
            if (gated) { f.Cost = st.GfGateCost; f.SDec = st.GfGateS; f.Structural = IsStructural(w, code, f.Cost); }
        }
        Lawgate2Note(w, code, ci, ti);   // Lawgate2Note〔履歴 Instruments.cs#161〕
    }
    /// <summary><c>Government.Move</c> の結果（outcome は <see cref="MilitaryMove"/> と同じ 0 閉じていた／1 純支持／2 政治資本／3 無償で動いた／4 払って動いた）。軸と歩幅の尺度から戦線の種類を決める。</summary>
    internal static void MoveResult(World w, PolicyAxis a, double spendScale, int outcome, double cur, double target, double next, double net, double speed)
    {
        var st = S(w);
        // 払って動いた額は種類ごとの四半期の台帳へ〔履歴 Instruments.cs#162〕
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
        st.MmEnvelopeAfterGov = Military.WarStageShare(w, w.MilitarySpending.Value);   // MmEnvelopeAfterGov〔履歴 Instruments.cs#163〕
    }
    static void ResetQuarter(WState st)
    {
        st.AdmMonths = st.AdmPaidMonths = st.AdmRBelow1Months = 0;
        st.AdmSimClaim = st.AdmCatClaim = st.AdmCatPaid = st.AdmFiscClaim = st.AdmBase = st.AdmSpendable = st.AdmLastRev = st.AdmSimMul = st.AdmCover = st.AdmFiscMul = st.AdmPayRate = st.AdmFill = st.AdmFillRBelow1 = 0;
        st.DecScale = st.DecLosing = st.DecShare = double.NaN;
        st.DecAtWar = st.DecMobilized = st.DecOpen = st.DecTarget = st.DecEnforced = -1; st.DecPicked = 0;
        st.FsRun = st.FsSeenMismatch = st.FsFired = st.FsFiredMismatch = st.FsFiredOver = st.SubMismatch = st.SubMismatchOff = 0; st.WheelGoods.Clear(); st.FacStartedGoods.Clear();   // FsRun〔履歴 Instruments.cs#164〕
        // BondMonths〔履歴 Instruments.cs#165〕
        st.BondMonths = 0; st.BondBase = st.BondDebt = st.BondFisher = st.BondDistrust = st.BondPolicy = st.BondGold = st.BondRepr = st.BondClamp = st.BondRate = st.BondDebtRatio = st.BondConvMul = st.BondReprMul = 0;
        st.QAdmClaim = st.QAdmNeedB = st.QAdmMinB = st.QAdmNeedP = st.QAdmMinP = st.QCfBorrowed = 0; st.QAdmOverRegionMonths = st.QCfShortMonths = st.QCfRBelow1Months = st.QCfObs = 0;
        st.QCapPaid = st.QCapPopK = 0; Array.Clear(st.QCapBoundPaid); Array.Clear(st.QCapCut); Array.Clear(st.QCapBoundPopK);
        st.FactorySold = 0; st.MonoBefore = st.MonoTarget = -1; st.FiscalStressAtDecision = double.NaN; st.PushBurden = st.PushP = st.PushU = double.NaN; st.PushHit = 0;
        // CnRows〔履歴 Instruments.cs#166〕
        st.CnRows = st.CnBound = 0; st.CnDesired = st.CnDemand = 0;
        st.ConDesiring = st.ConBound = 0; st.ConDesiredUnits = st.ConCutUnits = 0;
        st.ConRegionMonths = 0; st.ConGainSum = st.ConHurdleSum = st.ConBudgetMoney = st.ConDesiredMoney = 0; st.ConPlannedMoney = st.ConBudgetBoundMoney = st.ConBuiltUnits = st.ConPlannedUnits = 0; Array.Clear(st.ConRationW); if (st.QSpend is not null) Array.Clear(st.QSpend);   // ConRegionMonths〔履歴 Instruments.cs#167〕
        st.BrJudged = st.BrPoolBound = st.BrForeignCut = 0; st.BrDomPool = st.BrForRaw = st.BrFor = 0;
        st.CrDays = st.CrRepayBoundDays = st.CrFiniteDays = 0; st.CrReserveCap = st.CrRepayable = 0;
        st.MmOutcome = st.MmFronts = -1; st.MmIncreasing = false;
        st.MmCur = st.MmTarget = st.MmNet = st.MmResist = st.MmSpeed = st.MmPush = st.MmOppTrad = st.MmOppModern = st.MmOppMass = st.MmStock = st.MmStockMax = st.MmEnvelopeAfterGov = double.NaN;
        st.Gf.Clear(); st.GfCounted = st.GfMilExpired = false; st.GfLevelIdx = -1; st.GfGateWhy = 0; st.GfGateNet = double.NaN;   // Clear〔履歴 Instruments.cs#168〕
        st.GfSHead = st.GfMaxHead = st.GfSEnd = st.GfPendCost = st.GfPendS = st.GfGateCost = st.GfGateS = double.NaN; st.GfPaidL = st.GfPaidR = st.GfPaidM = st.GfPaidT = st.GfPaidC = 0;   // GfSHead〔履歴 Instruments.cs#169〕
        st.QLimitMonths = st.QLimitCrossings = st.QLimitBorrowMonths = st.QZombieMonths = st.QWarGateClosedMonths = st.QOver067Months = st.QOver067BorrowedMonths = st.QOver067RBelow1Months = 0;   // QLimitMonths〔履歴 Instruments.cs#170〕
        st.QUnpaidObs = st.QUnpaidMonths = 0; st.QUnpaidSum = st.QUnpaidMax = st.QDevalueShareSum = st.QDevalueShareMax = 0;
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
        st.WarNow = atWar;   // WarNow〔履歴 Instruments.cs#171〕
        if (w.InCivilWar) st.CivilWarMonths++;
        CwMonth(w);   // CwMonth〔履歴 Instruments.cs#172〕
    }
    /// <summary>戦争を立てた所（<c>Diplomacy.AddBelligerent</c> の <c>Wars.Add</c> の直後）：試合の終わりに戦死と月数を読む。</summary>
    internal static void WarStarted(Diplomacy dip, War war, int year) { Dips.GetValue(dip, _ => new DipState()).Wars.Add((year, war)); WarNoteStart(dip, war); }
    /// <summary>
    /// 読むだけ。〔履歴 Instruments.cs#173〕
    /// </summary>
    internal static void Invaded(Diplomacy dip, int year, Power invader, Power target, bool submitted, double added, double addedToTarget)
        { Dips.GetValue(dip, _ => new DipState()).Invades.Add((year, NameOf(invader), NameOf(target), target.Nation is not null, submitted, added, addedToTarget)); WarNoteInvade(dip); }
    /// <summary>月の戦死（<c>Diplomacy.WarCasualties</c>）：(年, 国)ごとの月数・兵士の月和・戦死の和（戦死は兵士の上限をかけた後の量）。</summary>
    internal static void Casualty(Diplomacy dip, int year, Power p, double soldierPop, double dead)
    {
        var d = Dips.GetValue(dip, _ => new DipState());
        var key = (year, NameOf(p));
        if (!d.Cas.TryGetValue(key, out var a)) d.Cas[key] = a = new double[3];
        a[0] += 1; a[1] += soldierPop; a[2] += dead;
    }

    // GateAtStart〔履歴 Instruments.cs#174〕

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
        InheritParStarted(w, r, p);   // InheritParStarted〔履歴 Instruments.cs#175〕
    }

    // FireSaleRun〔履歴 Instruments.cs#176〕

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
        InheritParCompleted(w, p);   // InheritParCompleted〔履歴 Instruments.cs#177〕
        var st = S(w);
        st.FacDone++;
        if (Discovery.Gate(r, p) > 0) return;
        st.FacDoneClosed++;
        if (StartGates.TryGetValue(p, out var g) && g.Closed) st.FacDoneClosedAtStart++;
    }

    // GiveIncome〔履歴 Instruments.cs#178〕

    /// <summary>
    /// 貯蓄 Pop._savings の書き手の種類。〔履歴 Instruments.cs#179〕
    /// </summary>
    internal enum SavKind : byte { Settle, Give, GiveIncome, Take, CarryOut, CarryIn, Rebase, RegionOut, RegionIn }
    static readonly string[] SavKindNames = ["settle", "give", "giveIncome", "take", "carryOut", "carryIn", "rebase", "regionOut", "regionIn"];
    [ThreadStatic] static string? _routeFile;
    [ThreadStatic] static int _routeLine;
    [ThreadStatic] static char _routeFrom, _routeTo;   // 払い手の側・受け手の側（P＝POP・T＝国庫・B／M／F／C／H＝世界の外の台帳の種類）
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Pop, WState> PopOwner = new();

    /// <summary>移し方の1本（<see cref="World.Transfer"/>）と人の移し方の1本（<see cref="Pop.Blend"/>）の入口：呼び出しの位置（コンパイル時の定数・[CallerFilePath]／[CallerLineNumber]）を、続く貯蓄の書き込みの経路名にする。</summary>
    internal static void Route(string file, int line, char fromSide, char toSide) { _routeFile = file; _routeLine = line; _routeFrom = fromSide; _routeTo = toSide; }

    internal static void FleetReturned(World from, double ships) => S(from).FleetReturned += ships;   // FleetReturned〔履歴 Instruments.cs#180〕

    /// <summary>州が別の国へ移る（<c>Territory.TransferRegion</c>・換算の後）：送り手の台帳から州の POP の貯蓄を引き（regionOut）、受け手が挙げた国なら POP を受け手の台帳へ登録して足す（regionIn）。建国（to＝null）と挙げていない国へ移った POP は台帳の外へ出る。</summary>
    internal static void RegionMoved(World from, World? to, Region r, Territory.Rehoming how)
    {
        NgRegionMoved(from, r);   // NgRegionMoved〔履歴 Instruments.cs#181〕
        CedeBefore(from, to, r, how);   // CedeBefore〔履歴 Instruments.cs#182〕
        if (r.FleetUnits is { } fu)   // FleetUnits〔履歴 Instruments.cs#183〕
        {
            double sum = 0; foreach (double x in fu) sum += x;
            var sf = S(from);
            if (r.Tier == RegionTier.Colonial) sf.FleetMovedOutColonial += sum; else sf.FleetMovedOutHome += sum;
            if (how == Territory.Rehoming.Ceded && from.Regions.Count > 0) { sf.FleetMovedOutCeded += sum; if (to is not null) S(to).FleetReceivedCeded += sum; }   // Remove の後なので Count は残る州の数）〔履歴 Instruments.cs#184〕
        }
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
        CearnSav(p, kind, delta);   // CearnSav〔履歴 Instruments.cs#185〕
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

    // SuspensionInterestShare〔履歴 Instruments.cs#186〕

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

    /// <summary>
    /// 月数は四半期の和・割合は観測した月の平均と最大。〔履歴 Instruments.cs#187〕
    /// </summary>
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
        return cols;
    }

    /// <summary>
    /// earnedTotalPerMonth＝全階級の Earned の和（四半期の月平均・自国通貨の名目＝class_s の earnedPerMonth の和と同じ量を全ての国×四半期で）。〔履歴 Instruments.cs#188〕
    /// </summary>
    static List<(string Name, string Value)> Extras289(WState st)
    {
        var cols = new List<(string, string)>();
        double cm = st.ClsMonths > 0 ? st.ClsMonths : double.NaN;
        double earn = 0; for (int c = 0; c < NC; c++) earn += st.ClsEarn[c];
        cols.Add(("earnedTotalPerMonth", F(earn / cm)));
        return cols;
    }

    /// <summary>
    /// 読むだけ。〔履歴 Instruments.cs#189〕
    /// </summary>
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

    /// <summary>
    /// 累計は Extras329 が nation_s の末尾 4 列へ書く（ε＝0 では呼ばれない＝0）。〔履歴 Instruments.cs#190〕
    /// </summary>
    internal static void LuxuryRatioDiag(World w, double pops, double nonPos, double invalid, double nonPosSize)
    {
        var st = S(w);
        st.LuxPopMonths += pops; st.LuxNonPosPopMonths += nonPos; st.LuxInvalidPopMonths += invalid; st.LuxNonPosSizeSum += nonPosSize;
    }

    // ConAcc〔履歴 Instruments.cs#191〕

    sealed class ConAcc { public int Judged; public double Desired, Budget, Planned, PlannedUnits, BuiltUnits, Paid; public int TermMonths, DesiringMonths; public double TermGain, TermSat, TermTech, TermCrowd, TermHeat, TermHurdle, TermNet, TermAbsorb, TermJoint, TermUnits, TermUnitMoney; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, ConAcc> ConAccs = new();
    /// <summary>
    /// 挙げた国だけ。〔履歴 Instruments.cs#192〕
    /// </summary>
    internal static void ConsRegion(World w, Region r, double desiredUnits, double budgetUnits, double unitMoney, double plannedUnits)
    {
        if (!IsPicked(w, S(w))) return;
        var a = ConAccs.GetValue(r, _ => new ConAcc());
        a.Judged++; if (desiredUnits > 0) a.Desired += desiredUnits * unitMoney;
        a.Budget += budgetUnits * unitMoney; a.Planned += plannedUnits * unitMoney; a.PlannedUnits += plannedUnits;
        U4Decide(r, desiredUnits, budgetUnits, unitMoney, plannedUnits);   // U4Decide〔履歴 Instruments.cs#193〕
    }
    /// <summary>
    /// 挙げた国だけ。〔履歴 Instruments.cs#194〕
    /// </summary>
    internal static void ConsTerms(World w, Region r, double gainTech, double sat, double techLev, double hurdle, double netReturn, double joint, double absorb, double units, double unitMoney)
    {
        if (!IsPicked(w, S(w))) return;
        var a = ConAccs.GetValue(r, _ => new ConAcc());
        a.TermMonths++; if (units > 0) a.DesiringMonths++;
        a.TermGain += gainTech; a.TermSat += sat; a.TermTech += techLev; a.TermCrowd += r.CrowdingPremium; a.TermHeat += r.FinancialHeat; a.TermHurdle += hurdle; a.TermNet += netReturn;
        a.TermAbsorb += absorb; a.TermJoint += joint; a.TermUnits += units; a.TermUnitMoney += unitMoney;
    }
    /// <summary>
    /// 〔履歴 Instruments.cs#195〕
    /// </summary>
    internal static void ConsRegionPaid(World w, Region r, double builtUnits, double cost)
    {
        if (!IsPicked(w, S(w))) return;
        var a = ConAccs.GetValue(r, _ => new ConAcc());
        a.BuiltUnits += builtUnits; a.Paid += cost;
        U4Paid(r, builtUnits, cost);   // U4Paid〔履歴 Instruments.cs#196〕
    }
    /// <summary>
    /// 外さないと、戻った四半期に古い積算が戻った国の行として出る。〔履歴 Instruments.cs#197〕
    /// </summary>
    internal static void ConsDepart(World from, Region r)
    {
        if (IsPicked(from, S(from))) U4Depart(from, r);   // IsPicked〔履歴 Instruments.cs#198〕
        if (!ConAccs.TryGetValue(r, out var a)) return;
        ConAccs.Remove(r);
        if (!IsPicked(from, S(from))) return;
        ConsDeparted.GetValue(from, _ => new List<(Region, ConAcc)>()).Add((r, a));
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, List<(Region Reg, ConAcc Acc)>> ConsDeparted = new();
    /// <summary>
    /// 〔履歴 Instruments.cs#199〕
    /// </summary>
    static void ClearCons(World w)
    {
        ConsDeparted.Remove(w);   // Remove〔履歴 Instruments.cs#200〕
        foreach (var r in w.Regions) if (ConAccs.TryGetValue(r, out var a)) { a.Judged = 0; a.Desired = a.Budget = a.Planned = a.PlannedUnits = a.BuiltUnits = a.Paid = 0; ClearTerms(a); }
        U4ClearWorld(w);   // U4ClearWorld〔履歴 Instruments.cs#201〕
    }
    static string TermMean(ConAcc? a, double? sum) => a is null || a.TermMonths == 0 || sum is null ? "nan" : F(sum.Value / a.TermMonths);
    static void ClearTerms(ConAcc a) { a.TermMonths = a.DesiringMonths = 0; a.TermGain = a.TermSat = a.TermTech = a.TermCrowd = a.TermHeat = a.TermHurdle = a.TermNet = a.TermAbsorb = a.TermJoint = a.TermUnits = a.TermUnitMoney = 0; }
    internal const int ConsMaxQuarter = 160;
    /// <summary>
    /// 資本家の貯蓄の在庫（四半期の末）・希望・投資枠・計画・実際に払った額（月あたり）と、州の人口・工業化度・工場の規模。〔履歴 Instruments.cs#202〕
    /// </summary>
    static void ConsRows(Sink sk, ulong seed, int q, World w, WState st)
    {
        List<(Region Reg, ConAcc Acc)>? departed = null;
        if (ConsDeparted.TryGetValue(w, out var dl)) { departed = new(dl); ConsDeparted.Remove(w); }   // ConsDeparted〔履歴 Instruments.cs#203〕
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
            sk.Cons.WriteLine(string.Join('\t', I(q), I(w.Year), nation, r.Name, r.Tier.ToString(), "1", F(pop), F(invSize), F(invSav), F(capSav), F(invEarn), F(r.Industrialization), F(r.ProductiveIndustrialization), F(scale),
                I(a?.Judged ?? 0), F(cm), F((a?.Desired ?? 0) / cm), F((a?.Budget ?? 0) / cm), F((a?.Planned ?? 0) / cm), F((a?.PlannedUnits ?? 0) / cm), F((a?.BuiltUnits ?? 0) / cm), F((a?.Paid ?? 0) / cm),
                I(a?.TermMonths ?? 0), I(a?.DesiringMonths ?? 0), TermMean(a, a?.TermGain), TermMean(a, a?.TermSat), TermMean(a, a?.TermTech), TermMean(a, a?.TermCrowd), TermMean(a, a?.TermHeat), TermMean(a, a?.TermHurdle), TermMean(a, a?.TermNet), TermMean(a, a?.TermAbsorb), TermMean(a, a?.TermJoint), TermMean(a, a?.TermUnits), TermMean(a, a?.TermUnitMoney)));
            if (a is not null) { a.Judged = 0; a.Desired = a.Budget = a.Planned = a.PlannedUnits = a.BuiltUnits = a.Paid = 0; ClearTerms(a); }
        }
        if (departed is null) return;
        // 活動の列（activeFlag）は 0・州の在庫の列は 0（いまの持ち主の物なので載せない）・月あたりの列は積算÷この国の月数。〔履歴 Instruments.cs#204〕
        foreach (var (r, a) in departed)
            sk.Cons.WriteLine(string.Join('	', I(q), I(w.Year), nation, r.Name, r.Tier.ToString(), "0", F(0), F(0), F(0), F(0), F(double.NaN), F(double.NaN), F(double.NaN), F(0),
                I(a.Judged), F(cm), F(a.Desired / cm), F(a.Budget / cm), F(a.Planned / cm), F(a.PlannedUnits / cm), F(a.BuiltUnits / cm), F(a.Paid / cm),
                I(a.TermMonths), I(a.DesiringMonths), TermMean(a, a.TermGain), TermMean(a, a.TermSat), TermMean(a, a.TermTech), TermMean(a, a.TermCrowd), TermMean(a, a.TermHeat), TermMean(a, a.TermHurdle), TermMean(a, a.TermNet), TermMean(a, a.TermAbsorb), TermMean(a, a.TermJoint), TermMean(a, a.TermUnits), TermMean(a, a.TermUnitMoney)));
    }

    // PortAcc〔履歴 Instruments.cs#205〕

    sealed class PortAcc { public bool Gate; public double InvSav, Need, Intended, StateIntended, PaidCap, PaidState, Built; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, PortAcc> PortAccs = new();
    /// <summary>
    /// 挙げた国だけ。〔履歴 Instruments.cs#206〕
    /// </summary>
    internal static void PortGate(World w, Region r, double investorSavings, double need, double intendedCap, double intendedState)
    {
        if (!IsPicked(w, S(w))) return;
        var a = PortAccs.GetValue(r, _ => new PortAcc());
        a.Gate = true; a.InvSav = investorSavings; a.Need = need; a.Intended = intendedCap; a.StateIntended = intendedState;
    }
    /// <summary>
    /// 〔履歴 Instruments.cs#207〕
    /// </summary>
    internal static void PortPaid(World w, Region r, double paidCap, double paidState, double builtCapacity)
    {
        if (!IsPicked(w, S(w))) return;
        var a = PortAccs.GetValue(r, _ => new PortAcc());
        a.PaidCap = paidCap; a.PaidState = paidState; a.Built = builtCapacity;
    }
    /// <summary>
    /// 律速率・潜在量・取扱高・港の容量は四半期の終わりの断面（StepPorts が見た値と同じ瞬間＝次の四半期の最初の清算まで書き換わらない）。〔履歴 Instruments.cs#208〕
    /// </summary>
    static void PortRows(Sink sk, ulong seed, int q, World w)
    {
        sk.Port ??= Open("port", seed);
        if (!sk.PortHeader) { sk.Port.WriteLine("quarter	year	nation	region	tradeThrottleIndex	tradePotentialWeight	portThroughput	portCapacityEnd	investorSavingsStock	congestedGate	needMoneyAtQuarterEnd	capitalIntendedMoneyAtQuarterEnd	capitalPaidMoneyAtQuarterEnd	stateIntendedMoneyAtQuarterEnd	statePaidMoneyAtQuarterEnd	builtCapacity"); sk.PortHeader = true; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string nation = NameOf(w);
        foreach (var r in w.Regions)
        {
            if (!r.IsPort) continue;
            PortAccs.TryGetValue(r, out var a);
            bool gate = a is not null && a.Gate;
            double sav = gate ? a!.InvSav : (r.InvestorPopIndex >= 0 && r.InvestorPopIndex < r.Pops.Count ? r.Pops[r.InvestorPopIndex].Savings.Value : double.NaN);
            sk.Port.WriteLine(string.Join('	', I(q), I(w.Year), nation, r.Name, F(r.LastTradeThrottle), F(r.LastTradePotential), F(r.LastPortThroughput), F(r.PortCapacity), F(sav), gate ? "1" : "0",
                F(a?.Need ?? 0), F(a?.Intended ?? 0), F(a?.PaidCap ?? 0), F(a?.StateIntended ?? 0), F(a?.PaidState ?? 0), F(a?.Built ?? 0)));
            if (a is not null) { a.Gate = false; a.InvSav = a.Need = a.Intended = a.StateIntended = a.PaidCap = a.PaidState = a.Built = 0; }
        }
    }

    /// <summary>
    /// 読むだけ：どちらの関数も World の列挙値と数値を読む純粋な式（書く経路・乱数・メモ化の新しい位置は無い。〔履歴 Instruments.cs#209〕
    /// </summary>
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
        DivhostMonth(w);   // DivhostMonth〔履歴 Instruments.cs#210〕
        CgMonthClose(w);   // CgMonthClose〔履歴 Instruments.cs#211〕
        LandsaleMonth(w);   // LandsaleMonth〔履歴 Instruments.cs#212〕
        LandshareMonth(w);   // LandshareMonth〔履歴 Instruments.cs#213〕
        LandbaseMonth(w);   // LandbaseMonth〔履歴 Instruments.cs#214〕
        BasisxMonth(w);   // BasisxMonth〔履歴 Instruments.cs#215〕
        var st = S(w);
        if (st.Armed && IsPicked(w, st)) PxMonth(w, st);   // IsPicked〔履歴 Instruments.cs#216〕
        if (!st.Active || !st.Armed) { st.Armed = false; return; }
        st.Armed = false;
        foreach (var r in w.Regions) foreach (var p in r.Pops) { int c = (uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1; st.ClsEarn[c] += p.EarnedPerMonth.Value; st.ClsPaid[c] += p.PaidPerMonth.Value; }
        st.ClsMonths++;
        if (st.GateOpenNow >= 0) { st.QGateObs++; if (st.GateOpenNow == 0) st.QGateClosed++; }
        if (st.Spare0 < 0) { st.QShortMonths++; st.QShortfall += -st.Spare0; }
        if (st.PayRate < 1) st.QRUnder1Months++;
        AccumulateGateAround(st);   // AccumulateGateAround〔履歴 Instruments.cs#217〕
        st.QBorrowed += double.IsNaN(st.Borrowed) ? 0 : st.Borrowed; st.QInterest += w.LastInterestPerMonth.Value; st.QRevenue += w.LastRevenuePerMonth.Value;
        st.QDirect += double.IsNaN(st.RevDirectNet) ? 0 : st.RevDirectNet; st.QIndirect += double.IsNaN(st.RevIndirect) ? 0 : st.RevIndirect;
        var led = w.TreasuryLedger;
        AccumulateAdmin(w, st);
        AccumulateAdminNeed(st);   // AccumulateAdminNeed〔履歴 Instruments.cs#218〕
        AccumulateCap(st);          // AccumulateCap〔履歴 Instruments.cs#219〕
        if (IsPicked(w, st) && st.HaveLedger)
        {
            var sb = new System.Text.StringBuilder();
            FiscalRow(w, st, sb, header: false);
            st.Rows.Add(sb.ToString());
        }
        st.PrevTreasury = w.Treasury.Value; Array.Copy(led, st.PrevLedger, NCat); st.HaveLedger = true;
        st.BudMul = st.BudCover = 0; st.BudSpendable = st.BudLastRev = double.NaN;   // 月の予算の位置で溜めた基数を次の月へ持ち越さない
        st.BudClaim = st.BudNeedB = st.BudMinB = st.BudNeedP = st.BudMinP = 0; st.BudOverRegions = 0; st.AdminClaims.Clear();   // BudClaim〔履歴 Instruments.cs#220〕
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
        // C〔履歴 Instruments.cs#221〕
        C("adminClaimFiscalPerMonth", w.LastAdminClaimPerMonth.Value); C("adminSpendableAtBudgetPerMonth", st.BudSpendable); C("adminLastRevenueAtBudgetPerMonth", st.BudLastRev);
        C("adminSimMeanMulShare", st.BudMul); C("adminBureaucratCoverShare", st.BudCover); C("adminFiscalMeanMul", Fiscal.AdminDistanceMean(w)); C("adminFillShare", AdminFillShare(w));
        // A′ の静的な反事実はこの4列と spareBeforeBorrowPerMonth・borrowedPerMonth から読み手が出す〔履歴 Instruments.cs#222〕
        C("adminNeedAtBudgetPerMonth", st.BudNeedB); C("adminClaimCappedAtBudgetPerMonth", st.BudMinB); C("adminNeedAtPurchasePerMonth", st.BudNeedP); C("adminClaimCappedAtPurchasePerMonth", st.BudMinP); C("adminOverNeedRegionsCount", st.BudOverRegions);
        C("treasuryAtClaimStock", st.Treasury0); C("revenueBasePerMonth", w.LastRevenuePerMonth.Value); C("payableBeforeBorrowStock", st.Payable0); C("spareBeforeBorrowPerMonth", st.Spare0);
        C("borrowShortfallPerMonth", st.Shortfall); C("borrowTicketPerMonth", st.Ticket); C("borrowDomesticPoolStock", st.DomPool); C("borrowForeignPoolStock", st.ForPool);
        C("borrowedPerMonth", st.Borrowed);
        T("borrowBinder", st.BorrowCode switch { 0 => "gateClosed", 1 => "full", 2 => "ticket", 3 => "pool", 4 => "none", _ => "noShortfall" });
        C("borrowCooldownMonths", w.BorrowCooldownMonths); C("interestToRevenueShareNow", w.LastRevenuePerMonth.Value > 0 ? w.LastInterestPerMonth / w.LastRevenuePerMonth : double.NaN);
        C("devaluedPerMonth", st.Devalued); C("payableAfterBorrowStock", st.Payable1); C("payRateShare", st.PayRate);
        C("borrowGateOpenFlag", st.GateOpenNow < 0 ? double.NaN : st.GateOpenNow); C("directTaxNetPerMonth", st.RevDirectNet); C("indirectRevenuePerMonth", st.RevIndirect);
        C("convertibilityShare", w.Convertibility); C("onGold", w.OnGoldStandard ? 1 : 0); C("govDebtStock", w.GovDebt.Value);
        C("govBondRatePerYear", w.GovBondRatePerYear.Value); C("govAvgCouponPerYear", w.GovAvgCouponPerYear.Value); C("insolvencyMonths", w.InsolvencyMonths); C("defaultCount", w.SovereignDefaultCount);
        var led = w.TreasuryLedger; double dSum = 0;
        for (int k = 0; k < NCat; k++) { double d = led[k] - st.PrevLedger[k]; dSum += d; C("ledger" + ((TreasuryCat)k) + "PerMonth", d); }
        double dT = w.Treasury.Value - st.PrevTreasury;
        C("treasuryEndStock", w.Treasury.Value); C("treasuryDeltaPerMonth", dT); C("ledgerSumPerMonth", dSum); C("ledgerResidualPerMonth", dT - dSum);
        // unpaid〔履歴 Instruments.cs#223〕
        double unpaid = UnpaidBeforeDevaluationShare(st.ClaimsTotal, st.Payable0, st.Borrowed);
        C("interestToRevenueShareAtGate", st.GateRev > 0 ? st.GateInt / st.GateRev : double.NaN); C("interestAtLimitFlag", st.LimitNow < 0 ? double.NaN : st.LimitNow);
        C("unpaidBeforeDevaluationShare", unpaid); C("devaluedToClaimShare", double.IsNaN(unpaid) ? double.NaN : (st.Devalued > 0 ? st.Devalued : 0) / st.ClaimsTotal);
        if (sb.Length > 0) sb.Length--;   // 末尾のタブ
    }

    // ---------------------------------------------------------------- 四半期の末・断面（SelfPlay.Run が呼ぶ） ----------------------------------------------------------------

    // PxMonthGameMax〔履歴 Instruments.cs#224〕

    const int PxMonthGameMax = 24, PxGoodsGameMax = 48;

    static string PxI(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
    // 犯罪者（階級 14）の列は CriminalClassEnabled>0 の世界にだけ出す＝犯罪者の POP が無い世界の出力（表の頭の列も）は NC を 14→15 にしても 1 バイトも変わらない。〔履歴 Instruments.cs#225〕
    static int PxCols(World w) => w.Coef.CriminalClassEnabled > 0 ? NC : NC - 1;

    /// <summary>
    /// 名前で引く（番号でなく Catalog の財から・README §1-9）。〔履歴 Instruments.cs#226〕
    /// </summary>
    static bool PxGoodPicked(int g) => Catalog.Goods[g].Tier == GoodTier.Staple || Catalog.Goods[g].Tier == GoodTier.Daily || System.Array.IndexOf(World.ExciseGoods, g) >= 0;

    /// <summary>
    /// 物価水準 P の動きと需給の動きを分ける。〔履歴 Instruments.cs#227〕
    /// </summary>
    static void PxRel(World w, out double staple, out double daily)
    {
        var lS = new List<double>(); var lD = new List<double>();
        foreach (var r in w.Regions)
        {
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

    /// <summary>
    /// 州の因子の平均は価格の天井に張り付いた少数の州で引かれる＝中央で読む。〔履歴 Instruments.cs#228〕
    /// </summary>
    static double Median(List<double> xs)
    {
        if (xs.Count == 0) return double.NaN;
        xs.Sort(); int m = xs.Count / 2;
        return xs.Count % 2 == 1 ? xs[m] : 0.5 * (xs[m - 1] + xs[m]);
    }

    /// <summary>
    /// 読むだけ（GdpPerMonth は読まない）。〔履歴 Instruments.cs#229〕
    /// </summary>
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

    /// <summary>
    /// early＝趨勢の基準を取るだけで物価は動かなかった最初の1回。〔履歴 Instruments.cs#230〕
    /// </summary>
    internal static void PriceStep(World w, double broad, double baseM, double money, double gdp, double ratio, double trend0, double cyclical, double pBefore, double pEma, double pFiat, double convTarget, double fiatP, bool early)
    {
        var st = S(w);
        if (!early) BsBiasRead(w, gdp);   // T0a：為替の偏りの再計算（読むだけ）
        {   // 読むだけ（早い呼び〔趨勢の基準を取るだけ〕は数えない）。〔履歴 Instruments.cs#231〕
            bool wm = w.WarmupFrozen; int pc = wm ? st.PcWarmN++ : st.PcGameN++;
            if (early || !(w.MoTrendForInstruments > 1e-9))   // 読むだけ〔履歴 Instruments.cs#232〕
                st.PinitRows.Add(string.Join('	', (wm ? "w" : "q") + PxI(pc), NameOf(w), F(w.Year), early ? "init" : "defer", F(gdp), F(ratio), F(w.MoTrendForInstruments), F(w.Coef.MoTrendInitNeedsGdp)));
            if (!early)
            {   // 注〔履歴 Instruments.cs#233〕
                string pt = (wm ? "w" : "q") + PxI(pc), pnm = NameOf(w), pyr = F(w.Year);
                if (st.PfixMonths > 0)
                    st.PfixRows.Add(string.Join('	', pt, pnm, pyr, "1", PxI(st.PfixMonths), F(st.PfixOld), F(st.PfixNew), F(st.PfixBroadMin), F(gdp), F(pBefore), F(w.PriceLevel)));
                if (!(gdp > 1e-9))
                    st.PfixRows.Add(string.Join('	', pt, pnm, pyr, "2", "0", "nan", "nan", "nan", F(gdp), F(pBefore), F(w.PriceLevel)));
                if (pBefore > 0 && w.PriceLevel >= pBefore * 100.0 || !(w.PriceLevel > 0) || !double.IsFinite(w.PriceLevel))
                    st.PjumpRows.Add(string.Join('	', pt, pnm, pyr, F(pBefore), F(w.PriceLevel), F(fiatP), F(cyclical)));
            }
            if (!early)
            {   // 注〔履歴 Instruments.cs#234〕
                double lineEnd = UtilityAgent.OpulenceLineOf(w), treEnd = w.Treasury.Value;
                st.FsxRows.Add(string.Join('	', (wm ? "w" : "q") + PxI(pc), NameOf(w), F(w.Year), PxI(st.FsxDevMonths), F(st.FsxPForceSum), F(st.FsxPForceMax), F(st.FsxPMSum), F(st.FsxPSSum), F(st.FsxPSMax),
                    PxI(st.FsxGateMonths), F(st.FsxFloorSum), F(st.FsxDoneSum), F(st.FsxAboveFloorSum), F(st.FsxExcessWouldSum), PxI(st.FsxOverMonths), F(treEnd), F(lineEnd), F(w.GovDebt.Value), treEnd > lineEnd ? "1" : "0"));
            }
            st.FsxDevMonths = 0; st.FsxGateMonths = 0; st.FsxOverMonths = 0; st.FsxPForceSum = 0; st.FsxPForceMax = 0; st.FsxPMSum = 0; st.FsxPSSum = 0; st.FsxPSMax = 0; st.FsxFloorSum = 0; st.FsxDoneSum = 0; st.FsxAboveFloorSum = 0; st.FsxExcessWouldSum = 0;
            st.PfixMonths = 0; st.PfixOld = 0; st.PfixNew = 0; st.PfixBroadMin = double.PositiveInfinity;
            if (!early && (pFiat > PxObserveCeil || pFiat < w.Coef.PriceLevelFloor))
                st.PcRows.Add(string.Join('\t', (wm ? "w" : "q") + PxI(pc), NameOf(w), F(w.Year), pFiat > PxObserveCeil ? "ceil" : "floor", F(pFiat), F(w.PriceLevel), F(fiatP), F(cyclical)));
        }
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
                if (g >= r.Markets.Length) continue;
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

    /// <summary>
    /// 戻す前の国庫 before と戻した値 to の差＝基礎貨幣の創出（正）か消滅（負）。〔履歴 Instruments.cs#235〕
    /// </summary>
    internal static void TreasuryRebased(World w, double before, double to)
    {
        var st = S(w);
        if (!IsPicked(w, st)) return;
        st.PxResetSince += to - before; st.PxResetCount++;
    }

    /// <summary>
    /// 〔履歴 Instruments.cs#236〕
    /// </summary>
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
        if (st.PcRows.Count > 0)   // PcRows〔履歴 Instruments.cs#237〕
        {
            sk.PClamp ??= Open("pclamp", seed);
            if (!sk.PClampHeader) { sk.PClamp.WriteLine("time\tnation\tyear\tkind\tpriceLevelAfterFiatIndex\tpriceLevelAfterIndex\tfiatPressureBeforeShare\tcyclicalRatio"); sk.PClampHeader = true; }
            foreach (var row in st.PcRows) sk.PClamp.WriteLine(row);
            st.PcRows.Clear();
        }
        if (st.PinitRows.Count > 0)   // PinitRows〔履歴 Instruments.cs#238〕
        {
            sk.Pinit ??= Open("pinit", seed);
            if (!sk.PinitHeader) { sk.Pinit.WriteLine("time	nation	year	event	gdpRealPerMonth	moneyToGdpMonths	moTrendAfterMonths	moTrendInitNeedsGdpFlag"); sk.PinitHeader = true; }
            foreach (var row in st.PinitRows) sk.Pinit.WriteLine(row);
            st.PinitRows.Clear();
        }
        if (st.PfixRows.Count > 0)   // PfixRows〔履歴 Instruments.cs#239〕
        {
            sk.PfixW ??= Open("pfix", seed);
            if (!sk.PfixHeader) { sk.PfixW.WriteLine("time	nation	year	kind	devalueMonths	pressureOld	pressureNew	broadMin	gdpRealPerMonth	priceLevelBefore	priceLevelAfter"); sk.PfixHeader = true; }
            foreach (var row in st.PfixRows) sk.PfixW.WriteLine(row);
            st.PfixRows.Clear();
        }
        if (st.PjumpRows.Count > 0)   // PjumpRows〔履歴 Instruments.cs#240〕
        {
            sk.PjumpW ??= Open("pjump", seed);
            if (!sk.PjumpHeader) { sk.PjumpW.WriteLine("time	nation	year	priceLevelBefore	priceLevelAfter	fiatPressureBeforeShare	cyclicalRatio"); sk.PjumpHeader = true; }
            foreach (var row in st.PjumpRows) sk.PjumpW.WriteLine(row);
            st.PjumpRows.Clear();
        }
        if (st.FsxRows.Count > 0)   // FsxRows〔履歴 Instruments.cs#241〕
        {
            sk.FsxW ??= Open("fsx", seed);
            if (!sk.FsxHeader) { sk.FsxW.WriteLine("time	nation	year	devalueMonths	pressureForceSum	pressureForceMax	pressureMSum	pressureSSum	pressureSMax	repayGateMonths	repayFloorSum	repayDoneSum	repayAboveFloorSum	excessWouldSum	overLineMonths	treasuryEnd	lineEnd	govDebtEnd	treasuryOverLineFlag"); sk.FsxHeader = true; }
            foreach (var row in st.FsxRows) sk.FsxW.WriteLine(row);
            st.FsxRows.Clear();
        }
        if (st.DcRows.Count > 0)   // DcRows〔履歴 Instruments.cs#242〕
        {
            sk.DevCapW ??= Open("devcap", seed);
            if (!sk.DevCapHeader) { sk.DevCapW.WriteLine("time\tnation\tyear\troomPerMonth\tcapPerMonth\tdevaluedPerMonth\troomOverCapRatio\treliefExp"); sk.DevCapHeader = true; }
            foreach (var row in st.DcRows) sk.DevCapW.WriteLine(row);
            st.DcRows.Clear();
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
        if (!Wants(kind + "_s")) return new System.IO.StreamWriter(System.IO.Stream.Null);   // StreamWriter〔履歴 Instruments.cs#243〕
        System.IO.Directory.CreateDirectory(Dir);   // CreateDirectory〔履歴 Instruments.cs#244〕
        return new(System.IO.Path.Combine(Dir, $"{kind}_s{seed}.tsv"), false, new System.Text.UTF8Encoding(false), 1 << 16);
    }

    /// <summary>断面（開幕 <c>open</c>・助走の直後 <c>warm</c>）：全ての国の階級と在庫の行。開幕は GDP を読まない（構築直後に和を取らない）。</summary>
    internal static void Phase(MultiNationSim sim, ulong seed, string time)
    {
        var sk = SinkOf(sim, seed);
        if (time == "warm") foreach (var n in sim.Nations) { var wst = S(n.World); wst.WarMonths = 0; wst.CivilWarMonths = 0; ResetQuarter(wst); wst.SavLedger.Clear(); ClearCons(n.World); }   // 助走の月は数えない（旧の注入も助走の直後に0へ戻していた）
        foreach (var n in sim.Nations) { ClassRows(sk, seed, time, n.World, S(n.World), flows: false); StockRow(sk, seed, time, n.World, readGdp: time != "open"); }
        if (time == "warm") foreach (var n in sim.Nations) { foreach (var rg in n.World.Regions) FlowOf(rg).HabitSoldSum = 0; var wst2 = S(n.World); wst2.OpTensionAdded = 0; wst2.OpTensionPairMonths = 0; wst2.OpTensionLevelMax = 0; CeClear(wst2); PAcc(n.World).Clear(n.World); if (wst2.FmA is { } fma) { fma.ClearNet(); fma.ClearArmy(); } }   // OpTensionPairMonths〔履歴 Instruments.cs#245〕
        if (time == "warm") InpWorldTable(sim);   // InpWorldTable〔履歴 Instruments.cs#246〕
        if (time == "warm") CwWarm(sim);   // CwWarm〔履歴 Instruments.cs#247〕
        if (time == "warm") CearnWarm(sim);   // CearnWarm〔履歴 Instruments.cs#248〕
        if (time == "warm") { TrClear(sim); ScopeClear(sim); AibClear(sim); }   // ScopeClear〔履歴 Instruments.cs#249〕
        foreach (var n in sim.Nations) FlushPx(sk, seed, S(n.World));   // Nations〔履歴 Instruments.cs#250〕
        RdhPhase(sim, seed, time);   // RdhPhase〔履歴 Instruments.cs#251〕
    }

    /// <summary>四半期の末：挙げた国は毎四半期・全ての国は年末（q%4==3）。財政の月次の行を書き出し、階級の所得と支出の四半期の合計をゼロへ戻す。</summary>
    internal static void Quarter(MultiNationSim sim, ulong seed, int q)
    {
        var sk = SinkOf(sim, seed);
        InpWorldTable(sim);   // InpWorldTable〔履歴 Instruments.cs#252〕
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
            if (picked) FlowRows(sk, seed, q, w, st);   // FlowRows〔履歴 Instruments.cs#253〕
            FlushPx(sk, seed, st);   // FlushPx〔履歴 Instruments.cs#254〕
            NationRow(sk, seed, q, i, w, st, yearEnd);
            SpendFold(sk, w, st, q, picked);   // SpendFold〔履歴 Instruments.cs#255〕
            PopFold(sk, seed, w, st, q, picked, yearEnd);   // PopFold〔履歴 Instruments.cs#256〕
            FmFold(sk, seed, w, st, q, picked, yearEnd);    // FmFold〔履歴 Instruments.cs#257〕
            ScalarsFold(sk, seed, w, q, picked, yearEnd);   // ScalarsFold〔履歴 Instruments.cs#258〕
            CrownFold(sim, seed, w, q);                     // CrownFold〔履歴 Instruments.cs#259〕
            CenterFold(sim, seed, w, q);                    // CenterFold〔履歴 Instruments.cs#260〕
            ArmsFold(sim, seed, w, q, picked);              // ArmsFold〔履歴 Instruments.cs#261〕
            CwFold(sim, seed, w, q);   // CwFold〔履歴 Instruments.cs#262〕
            IvfFold(sk, seed, w, q, picked, yearEnd);       // IvfFold〔履歴 Instruments.cs#263〕
            BurFold(sk, seed, w, st, q);   // BurFold〔履歴 Instruments.cs#264〕
            CoupFold(sk, seed, w, st, q);   // CoupFold〔履歴 Instruments.cs#265〕
            AnchorFold(sk, seed, w, q, picked, yearEnd);    // AnchorFold〔履歴 Instruments.cs#266〕
            CapFold(sk, seed, w, st, q, picked, yearEnd);   // CapFold〔履歴 Instruments.cs#267〕
            IdeologyFold(sk, seed, w, st, q, picked, yearEnd);   // IdeologyFold〔履歴 Instruments.cs#268〕
            SecFold(sk, seed, w, st, q, picked, yearEnd);   // SecFold〔履歴 Instruments.cs#269〕
            UrxFold(sk, seed, w, st, q);                    // UrxFold〔履歴 Instruments.cs#270〕
            EpiFold(sk, seed, w, st, q);                    // EpiFold〔履歴 Instruments.cs#271〕
            RuleFold(sk, seed, w, q, picked, yearEnd);      // RuleFold〔履歴 Instruments.cs#272〕
            MonFold(sim, seed, w, st, q, i, yearEnd);       // MonFold〔履歴 Instruments.cs#273〕
            ReformFold(sk, seed, w, st, q, picked, yearEnd);   // ReformFold〔履歴 Instruments.cs#274〕
            AdmRuleFold(sk, seed, w, q, picked, yearEnd);   // AdmRuleFold〔履歴 Instruments.cs#275〕
            EdFold(sk, seed, w, q, picked, yearEnd);        // EdFold〔履歴 Instruments.cs#276〕
            FdFold(sk, seed, w, q, picked);                 // FdFold〔履歴 Instruments.cs#277〕
            EqFold(sk, seed, w, q, picked, yearEnd);        // EqFold〔履歴 Instruments.cs#278〕
            SfFold(sk, seed, w, q, picked, yearEnd);        // SfFold〔履歴 Instruments.cs#279〕
            AibFold(sk, seed, w, q, picked, yearEnd);       // AibFold〔履歴 Instruments.cs#280〕
            LwFold(sk, seed, w, q, picked, yearEnd);        // LwFold〔履歴 Instruments.cs#281〕
            LwkFold(sk, seed, w, q, yearEnd);               // LwkFold〔履歴 Instruments.cs#282〕
            SbFold(sk, seed, w, q, picked);                 // SbFold〔履歴 Instruments.cs#283〕
            InpFold(sk, seed, w, st, q, picked, yearEnd);   // InpFold〔履歴 Instruments.cs#284〕
            PayFold(sk, seed, w, q, picked, yearEnd);       // PayFold〔履歴 Instruments.cs#285〕
            HouseFold(sk, seed, w, q, picked, yearEnd);     // HouseFold〔履歴 Instruments.cs#286〕
            ChemFold(sk, seed, w, q, picked);               // ChemFold〔履歴 Instruments.cs#287〕
            FbFold(sk, seed, w, q, picked, yearEnd);        // FbFold〔履歴 Instruments.cs#288〕
            WpFold(sk, seed, w, q);                         // WpFold〔履歴 Instruments.cs#289〕
            ModFold(sk, seed, w, st, q, picked, yearEnd);   // ModFold〔履歴 Instruments.cs#290〕
            LawGateFold(sk, seed, w, q, picked, yearEnd);   // LawGateFold〔履歴 Instruments.cs#291〕
            Lawgate2Fold(sk, seed, w, q);                   // Lawgate2Fold〔履歴 Instruments.cs#292〕
            SvcBuyFold(sk, seed, w, q, picked);   // SvcBuyFold〔履歴 Instruments.cs#293〕
            DcontFold(sk, seed, w, q, picked, yearEnd);     // DcontFold〔履歴 Instruments.cs#294〕
            CeFold(sk, seed, w, st, q, picked, yearEnd);   // CeFold〔履歴 Instruments.cs#295〕
            if (picked) SvcRow(sk, q, w);
            if (picked) ConsRows(sk, seed, q, w, st);   // ConsRows〔履歴 Instruments.cs#296〕
            if (picked) ConsfRows(sk, seed, q, w);      // ConsfRows〔履歴 Instruments.cs#297〕
            if (picked) PortRows(sk, seed, q, w);       // PortRows〔履歴 Instruments.cs#298〕
            Array.Clear(st.ClsEarn); Array.Clear(st.ClsPaid); st.ClsMonths = 0; st.WarMonths = 0; st.CivilWarMonths = 0; ResetQuarter(st);
            st.QGateObs = st.QGateClosed = st.QShortMonths = st.QRUnder1Months = 0; st.QInterest = st.QRevenue = st.QDirect = st.QIndirect = st.QShortfall = st.QBorrowed = 0;
            st.FacStarted = st.FacStartedClosed = st.FacDone = st.FacDoneClosed = st.FacDoneClosedAtStart = 0;
        }
        RegionRows(sk, seed, q, sim);
        OpiumRows(sk, seed, q, sim);    // OpiumRows〔履歴 Instruments.cs#299〕
        HabitlgTable(sim, seed, q);     // HabitlgTable〔履歴 Instruments.cs#300〕
        SiteTables(sk, seed, q, sim);   // SiteTables〔履歴 Instruments.cs#301〕
        SilverParityTable(sim, seed, q);   // SilverParityTable〔履歴 Instruments.cs#302〕
        DiscTable(sk, seed, q, sim);    // DiscTable〔履歴 Instruments.cs#303〕
        EngineerTable(sk, seed, q, sim);   // EngineerTable〔履歴 Instruments.cs#304〕
        ClockTable(sk, seed, sim);         // ClockTable〔履歴 Instruments.cs#305〕
        IdnTable(sim, seed);               // IdnTable〔履歴 Instruments.cs#306〕
        PortchTable(sim, seed, q);         // PortchTable〔履歴 Instruments.cs#307〕
        PortlmTable(sim, seed, q);         // PortlmTable〔履歴 Instruments.cs#308〕
        InheritIntentTable(sim, seed, q);  // InheritIntentTable〔履歴 Instruments.cs#309〕
        PrdfTable(sim, seed);              // PrdfTable〔履歴 Instruments.cs#310〕
        DivhostTable(sim, seed, q); RdhTable(sim, seed, q);            // DivhostTable〔履歴 Instruments.cs#311〕
        PrdsTable(sim, seed);              // PrdsTable〔履歴 Instruments.cs#312〕
        CpwdTable(sim, seed, q);           // CpwdTable〔履歴 Instruments.cs#313〕
        CearnTable(sim, seed, q);          // CearnTable〔履歴 Instruments.cs#314〕
        CedeTable(sim, q);                 // CedeTable〔履歴 Instruments.cs#315〕
        RulePowerTable(sim, seed, q);      // RulePowerTable〔履歴 Instruments.cs#316〕
        MinecapTable(sim, seed, q);        // MinecapTable〔履歴 Instruments.cs#317〕
        LandsaleTable(sim, seed, q);       // LandsaleTable〔履歴 Instruments.cs#318〕
        LandshareTable(sim, seed, q);      // LandshareTable〔履歴 Instruments.cs#319〕
        if (DscOn) DscTable(sim, seed, q); // DscTable〔履歴 Instruments.cs#320〕
        NationgoodTable(sim, seed, q); NgEventsRoll(sim); TradeskipTable(sim, seed, q); RecipeoutTable(sim, seed, q); ProdcapTable(sim, seed, q);   // NationgoodTable〔履歴 Instruments.cs#321〕
        SellrTable(sim, seed, q);          // SellrTable〔履歴 Instruments.cs#322〕
        BuyShadowTable(sim, seed, q);      // BuyShadowTable（T0a・s4cost_s・s7staple_s）
        TbTable(sim, seed, q);             // TbTable（T0b・tb_s）
        TransportFold(sk, seed, q, sim);   // TransportFold〔履歴 Instruments.cs#323〕
        ScopeFold(sk, seed, q, sim);       // ScopeFold〔履歴 Instruments.cs#324〕
        ScoreFold(sk, seed, q, sim);       // ScoreFold〔履歴 Instruments.cs#325〕
        SubstTable(sim, seed, q); CornintTable(sim, seed, q); MerchTable(sim, seed, q); PxstepTable(sim, seed, q); CorncfTable(sim, seed, q); LordstatTable(sim, seed, q);   // SubstTable〔履歴 Instruments.cs#326〕
        LandbaseTable(sim, seed, q);   // LandbaseTable〔履歴 Instruments.cs#327〕
        BasisxTable(sim, seed, q); WarQuarter(sim, seed, q);   // BasisxTable〔履歴 Instruments.cs#328〕
        foreach (var cp in sim.Diplomacy.Chokepoints)   // Chokepoints〔履歴 Instruments.cs#329〕
            if (cp.IsCanal && cp.IsOpen && !sk.CanalOpened.ContainsKey(cp.Id)) sk.CanalOpened[cp.Id] = (q, sim.Nations.Count > 0 ? sim.Nations[0].World.Year : 0);
    }

    /// <summary>試合の終わり（例外の経路も）：書き出しを閉じる。</summary>
    internal static void Close(MultiNationSim sim)
    {
        if (!Sinks.TryGetValue(sim, out var sk)) return;
        if (Dips.TryGetValue(sim.Diplomacy, out var dip)) WarTables(sk, dip, sim.Diplomacy);
        CpTable(sk, sim.Diplomacy);   // CpTable〔履歴 Instruments.cs#330〕
        CmTable(sk, sim.Diplomacy);   // CmTable〔履歴 Instruments.cs#331〕
        ImpTable(sk, sim.Diplomacy);   // ImpTable〔履歴 Instruments.cs#332〕
        DepTable(sk, sim.Diplomacy);   // DepTable〔履歴 Instruments.cs#333〕
        CanalTable(sk, sim);
        SpendTable(sk);   // SpendTable〔履歴 Instruments.cs#334〕
        CeTable(sk);      // CeTable〔履歴 Instruments.cs#335〕
        BurScanTable(sk, sim);   // BurScanTable〔履歴 Instruments.cs#336〕
        TransportClose(sk, sim);   // TransportClose〔履歴 Instruments.cs#337〕
        ScopeFold(sk, sk.Seed, 0, sim, force: true); sk.Csc?.Dispose(); sk.Csc = null;   // ScopeFold〔履歴 Instruments.cs#338〕
        MonClose(sim, sk.Seed);   // MonClose〔履歴 Instruments.cs#339〕
        sk.Site?.Dispose(); sk.Site = null; sk.Metal?.Dispose(); sk.Metal = null; sk.MetalNat?.Dispose(); sk.MetalNat = null; sk.PClamp?.Dispose(); sk.PClamp = null; sk.DevCapW?.Dispose(); sk.DevCapW = null; sk.PfixW?.Dispose(); sk.PfixW = null; sk.PjumpW?.Dispose(); sk.PjumpW = null; sk.FsxW?.Dispose(); sk.FsxW = null;   // Dispose〔履歴 Instruments.cs#340〕
        sk.Pinit?.Dispose(); sk.Pinit = null;   // Dispose〔履歴 Instruments.cs#341〕
        sk.Disc?.Dispose(); sk.Disc = null;   // Dispose〔履歴 Instruments.cs#342〕
        sk.Eng?.Dispose(); sk.Eng = null;     // Dispose〔履歴 Instruments.cs#343〕
        sk.Lwk?.Dispose(); sk.Lwk = null;     // Dispose〔履歴 Instruments.cs#344〕
        sk.Clock?.Dispose(); sk.Clock = null;   // Dispose〔履歴 Instruments.cs#345〕
        IdnClose(sim);   // IdnClose〔履歴 Instruments.cs#346〕
        SubstClose(sim); CornintClose(sim); MerchClose(sim); PxstepClose(sim); CorncfClose(sim); LordstatClose(sim);   // SubstClose〔履歴 Instruments.cs#347〕
        LandbaseClose(sim);   // LandbaseClose〔履歴 Instruments.cs#348〕
        BasisxClose(sim); NationgoodClose(sim); TradeskipClose(sim); RecipeoutClose(sim); ProdcapClose(sim); SellrClose(sim);   // BasisxClose〔履歴 Instruments.cs#349〕
        BuyShadowClose(sim);   // BuyShadowClose（T0a）
        TbClose(sim);          // TbClose（T0b）
        CrownClose(sim);   // CrownClose〔履歴 Instruments.cs#350〕
        CenterClose(sim);  // CenterClose〔履歴 Instruments.cs#351〕
        ArmsClose(sim);    // ArmsClose〔履歴 Instruments.cs#352〕
        CwClose(sim); WarClose(sim);   // CwClose〔履歴 Instruments.cs#353〕
        LandsaleClose(sim);   // LandsaleClose〔履歴 Instruments.cs#354〕
        LandshareClose(sim);   // LandshareClose〔履歴 Instruments.cs#355〕
        SilverParityClose(sim);   // SilverParityClose〔履歴 Instruments.cs#356〕
        SilverWeightClose(sim, sk.Seed);   // SilverWeightClose〔履歴 Instruments.cs#357〕
        DivhostClose(sim); PortchClose(sim);    // DivhostClose〔履歴 Instruments.cs#358〕
        PortlmClose(sim);    // PortlmClose〔履歴 Instruments.cs#359〕
        CpwdClose(sim);      // CpwdClose〔履歴 Instruments.cs#360〕
        CearnClose(sim);     // CearnClose〔履歴 Instruments.cs#361〕
        InheritIntentClose(sim);   // InheritIntentClose〔履歴 Instruments.cs#362〕
        PrdfClose(sim);  // PrdfClose〔履歴 Instruments.cs#363〕
        RdhClose(sim);   // RdhClose〔履歴 Instruments.cs#364〕
        PrdsClose(sim);  // PrdsClose〔履歴 Instruments.cs#365〕
        CessClose(sim, sk.Seed);   // CessClose〔履歴 Instruments.cs#366〕
        CedeClose(sim, sk.Seed);   // CedeClose〔履歴 Instruments.cs#367〕
        DscClose(sim);   // DscClose〔履歴 Instruments.cs#368〕
        sk.DiscContact?.Dispose(); sk.DiscContact = null;   // Dispose〔履歴 Instruments.cs#369〕
        sk.DiscFlow?.Dispose(); sk.DiscFlow = null;   // Dispose〔履歴 Instruments.cs#370〕
        sk.Svc?.Dispose(); sk.Svc = null;
        sk.Cons?.Dispose(); sk.Cons = null;   // Dispose〔履歴 Instruments.cs#371〕
        sk.Consf?.Dispose(); sk.Consf = null;   // Dispose〔履歴 Instruments.cs#372〕
        sk.Port?.Dispose(); sk.Port = null;   // Dispose〔履歴 Instruments.cs#373〕
        sk.Opium?.Dispose(); sk.Opium = null;   // Dispose〔履歴 Instruments.cs#374〕
        sk.Ce?.Dispose(); sk.Ce = null;   // Dispose〔履歴 Instruments.cs#375〕
        sk.PopS?.Dispose(); sk.PopS = null;   // Dispose〔履歴 Instruments.cs#376〕
        sk.AgeS?.Dispose(); sk.AgeS = null;   // Dispose〔履歴 Instruments.cs#377〕
        sk.Fm?.Dispose(); sk.Fm = null;       // Dispose〔履歴 Instruments.cs#378〕
        sk.Sca?.Dispose(); sk.Sca = null;     // Dispose〔履歴 Instruments.cs#379〕
        sk.Ivf?.Dispose(); sk.Ivf = null;     // Dispose〔履歴 Instruments.cs#380〕
        sk.TrBin?.Dispose(); sk.TrBin = null; sk.TrRoute?.Dispose(); sk.TrRoute = null; sk.TrNat?.Dispose(); sk.TrNat = null; sk.TrStat?.Dispose(); sk.TrStat = null; sk.TrRail?.Dispose(); sk.TrRail = null;   /* Dispose〔履歴 Instruments.cs#381〕 */   // Dispose〔履歴 Instruments.cs#382〕
        sk.Xmig?.Dispose(); sk.Xmig = null;   // Dispose〔履歴 Instruments.cs#383〕
        sk.Bur?.Dispose(); sk.Bur = null;   // Dispose〔履歴 Instruments.cs#384〕
        sk.Coup?.Dispose(); sk.Coup = null;   // Dispose〔履歴 Instruments.cs#385〕
        sk.Anc?.Dispose(); sk.Anc = null;   // Dispose〔履歴 Instruments.cs#386〕
        sk.Cap?.Dispose(); sk.Cap = null; sk.Ide?.Dispose(); sk.Ide = null;   // Dispose〔履歴 Instruments.cs#387〕
        sk.Sec?.Dispose(); sk.Sec = null;   // Dispose〔履歴 Instruments.cs#388〕
        sk.Urx?.Dispose(); sk.Urx = null; sk.UrxPdW?.Dispose(); sk.UrxPdW = null; sk.UrxRt?.Dispose(); sk.UrxRt = null;   // Dispose〔履歴 Instruments.cs#389〕
        sk.Epi?.Dispose(); sk.Epi = null;   // Dispose〔履歴 Instruments.cs#390〕
        sk.Inpf?.Dispose(); sk.Inpf = null; sk.Inpb?.Dispose(); sk.Inpb = null;   // Dispose〔履歴 Instruments.cs#391〕
        sk.Rule?.Dispose(); sk.Rule = null;   // Dispose〔履歴 Instruments.cs#392〕
        sk.Reform?.Dispose(); sk.Reform = null;   // Dispose〔履歴 Instruments.cs#393〕
        sk.AdmRule?.Dispose(); sk.AdmRule = null;   // Dispose〔履歴 Instruments.cs#394〕
        sk.Ed?.Dispose(); sk.Ed = null;   // Dispose〔履歴 Instruments.cs#395〕
        sk.Fd?.Dispose(); sk.Fd = null;   // Dispose〔履歴 Instruments.cs#396〕
        sk.EqS?.Dispose(); sk.EqS = null; sk.EqR?.Dispose(); sk.EqR = null;   // Dispose〔履歴 Instruments.cs#397〕
        sk.Sf?.Dispose(); sk.Sf = null;   // Dispose〔履歴 Instruments.cs#398〕
        sk.Aib?.Dispose(); sk.Aib = null;   // Dispose〔履歴 Instruments.cs#399〕
        sk.Lab?.Dispose(); sk.Lab = null;   // Dispose〔履歴 Instruments.cs#400〕
        sk.SbU?.Dispose(); sk.SbU = null; sk.SbR?.Dispose(); sk.SbR = null; sk.SbRReg?.Dispose(); sk.SbRReg = null;   // Dispose〔履歴 Instruments.cs#401〕
        sk.Pay?.Dispose(); sk.Pay = null;   // Dispose〔履歴 Instruments.cs#402〕
        sk.House?.Dispose(); sk.House = null;   // Dispose〔履歴 Instruments.cs#403〕
        sk.Chem?.Dispose(); sk.Chem = null; sk.ChemGood?.Dispose(); sk.ChemGood = null;   // Dispose〔履歴 Instruments.cs#404〕
        sk.Fb?.Dispose(); sk.Fb = null;   // Dispose〔履歴 Instruments.cs#405〕
        sk.Wp?.Dispose(); sk.Wp = null;   // Dispose〔履歴 Instruments.cs#406〕
        sk.ModSig?.Dispose(); sk.ModSig = null; sk.ScoreS?.Dispose(); sk.ScoreS = null;   // Dispose〔履歴 Instruments.cs#407〕
        sk.LawGate?.Dispose(); sk.LawGate = null;   // Dispose〔履歴 Instruments.cs#408〕
        sk.Lawgate2?.Dispose(); sk.Lawgate2 = null;   // Dispose〔履歴 Instruments.cs#409〕
        sk.SvcBuy?.Dispose(); sk.SvcBuy = null;   // Dispose〔履歴 Instruments.cs#410〕
        sk.Dcont?.Dispose(); sk.Dcont = null;   // Dispose〔履歴 Instruments.cs#411〕
        sk.Fiscal?.Dispose(); sk.Class?.Dispose(); sk.Stock?.Dispose(); sk.Nation?.Dispose(); sk.Region?.Dispose(); sk.Migration?.Dispose(); sk.Flow?.Dispose(); sk.Goods?.Dispose(); sk.Money?.Dispose(); sk.PxGoods?.Dispose(); sk.PxMonth?.Dispose(); sk.Fiscal = sk.Class = sk.Stock = sk.Nation = sk.Region = sk.Migration = sk.Flow = sk.Goods = sk.Money = sk.PxGoods = sk.PxMonth = null;
    }

    // RegionFlow〔履歴 Instruments.cs#412〕

    /// <summary>州ごとの四半期の人口の流れ（千人）。四半期の末に <c>region</c> と <c>migration</c> の行へ書いて0へ戻す。</summary>
    sealed class RegionFlow
    {
        public double MigrationOutK, MigrationInK, SettlerInK, CrossBorderOutK, CrossBorderInK;
        public double[]? SvcBuyAcc;   // SvcBuyAcc〔履歴 Instruments.cs#413〕
        public double HabitSoldSum;   // HabitSoldSum〔履歴 Instruments.cs#414〕
        public double TheftPoolSum, TheftRecipientsEarnedSum;   // TheftRecipientsEarnedSum〔履歴 Instruments.cs#415〕
        public Dictionary<Region, double>? MigrationTo;   // 出る州→入る州ごとの量（四半期の合計・州の間の移民だけ）
        public Dictionary<Region, double>? CrossBorderTo; // CrossBorderTo〔履歴 Instruments.cs#416〕
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, RegionFlow> Flows = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Pop, double[]> SettlerOut = new();   // 入植で本国の pop から出た量（四半期の合計）＝出た pop が居る州の行に書く
    static RegionFlow FlowOf(Region r) => Flows.GetValue(r, _ => new RegionFlow());

    /// <summary>
    /// 州の中で閉じる移転（盗む側も受ける側も同じ州）なので、州ごとの行で〈窃盗の受け取り÷受け手の所得〉が読める（flow_s は国×階級で州を分けない）。〔履歴 Instruments.cs#417〕
    /// </summary>
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
    /// <summary>
    /// 越境の移民（MultiNationSim.MoveMigrants の運搬の前・読むだけ）。〔履歴 Instruments.cs#418〕
    /// </summary>
    internal static void CrossBorder(Region src, Region dst, double flowK)
    {
        var f = FlowOf(src); f.CrossBorderOutK += flowK; FlowOf(dst).CrossBorderInK += flowK;
        var to = f.CrossBorderTo ??= new();   // region_s の越境の列と同じ量を組に分けただけ〔履歴 Instruments.cs#419〕
        to[dst] = to.GetValueOrDefault(dst) + flowK;
    }

    /// <summary>州ごとの行（<c>region</c>＝全ての国の全ての州・四半期の末）と州の間の移民の組（<c>migration</c>＝出る州→入る州の量）。州の空洞化（F1 の線）の読み手 <c>scripts/instruments/hol/an_h2.py</c> が読む。
    /// 機会の因子（賃金・雇用・押し出し・住居）は旧の注入（<c>u4e/inj_ghost.py</c>）の定義のまま＝雇用は兵士を含む全 pop の失業・押し出しの不穏は全 pop の闘争度の人口加重で、
    /// 実際の機会（<see cref="Territory.Opportunity"/>＝兵士を除く・<c>Politics.AvgRegionMilitancy</c>）とは違う＝因子の積は <c>opportunityIndex</c> と一致しない（機会そのものは本物）。</summary>
    static void RegionRows(Sink sk, ulong seed, int q, MultiNationSim sim)
    {
        sk.Region ??= Open("region", seed);
        sk.Migration ??= Open("migration", seed);
        sk.Xmig ??= Open("xmig", seed);   // Open〔履歴 Instruments.cs#420〕
        if (!sk.RegionHeader)
        {
            sk.Region.WriteLine("quarter\tyear\tnationIndex\tnation\tregion\ttier\tactiveFlag\tpopulationK\truralPopulationK\topportunityIndex\tholdingFactor\twageFactor\tlandFactor\temploymentShare\tpushFactor\thousingFactor\twagePremium\tarableLand\t"
                + "migrationOutKPerQuarter\tmigrationInKPerQuarter\tsettlerOutKPerQuarter\tsettlerInKPerQuarter\tcrossBorderOutKPerQuarter\tcrossBorderInKPerQuarter\twarIntensity\tfoodFillShare\t"
                + "theftPoolPerMonth\ttheftRecipientsEarnedPerMonth\tearnedPerMonth\t" + RegionProdHeaderText);   // ttheftRecipientsEarnedPerMonth〔履歴 Instruments.cs#421〕
            sk.Migration.WriteLine("quarter\tfromRegion\ttoRegion\tmovedKPerQuarter\trouteKm");   // WriteLine〔履歴 Instruments.cs#422〕
            sk.RegionHeader = true;
        }
        if (!sk.XmigHeader) { sk.Xmig.WriteLine("quarter\tfromRegion\ttoRegion\tfromNation\ttoNation\tmovedKPerQuarter\tgreatCircleKm\trouteKm"); sk.XmigHeader = true; }   // tmovedKPerQuarter〔履歴 Instruments.cs#423〕
        // 表に無い州の名前は nan。〔履歴 Instruments.cs#424〕
        string RouteText(Region a, Region b, int cfg, Coefficients cf)
        {
            int ia = RouteKm.IndexOf(a.Name), ib = RouteKm.IndexOf(b.Name);
            return ia < 0 || ib < 0 ? "nan" : F(RouteKm.Km(ia, ib, cfg, cf));
        }
        Dictionary<Region, (int Index, World W)>? ownerOf = null;   // 越境の移民の行き先の州の持ち主（四半期の末・初めて要るとき 1 度だけ作る）
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
                    unemp += p.Size * p.UnemploymentRate.Value; unrest += p.Size * p.Militancy; foodMet += p.FoodMet; foodNeed += p.FoodNeed;   earned += Calendar.OneMonthOf(p.LastEarnedPerMonth).Value;   // Value〔履歴 Instruments.cs#425〕
                    if (SettlerOut.TryGetValue(p, out var po)) { settlerOut += po[0]; po[0] = 0; }
                }
                double floor = Territory.OpportunityFactorFloor;
                double wage = Math.Max(floor, 1.0 + c.OpportunityWageWeight * (r.WagePremium - 1.0));
                double emp = pop >= 1.0 ? 1.0 - unemp / pop : 1.0;
                double push = pop >= 1.0 ? Math.Max(floor, 1.0 - c.PushUnrestWeight * (unrest / pop) - c.CrimePushWeight * r.CrimeRate) : 1.0;
                double hous = pop >= 1.0 ? Math.Max(floor, 1.0 - c.HousingMigrationFriction * (1.0 - r.HousingFill)) : 1.0;
                var f = FlowOf(r);
                sk.Region.WriteLine(string.Join('\t', I(q), I(year), I(i), nation, r.Name, r.Tier.ToString(), "1", F(pop), F(rural),
                    F(Territory.Opportunity(r, c)), F(Territory.Holding(r, c)), F(wage), F(Territory.LandFactor(r, rural, c)), F(emp), F(push), F(hous), F(r.WagePremium), F(r.ArableLand),
                    F(f.MigrationOutK), F(f.MigrationInK), F(settlerOut), F(f.SettlerInK), F(f.CrossBorderOutK), F(f.CrossBorderInK), F(r.LastWarIntensity), F(foodNeed > 0 ? foodMet / foodNeed : 1),
                    F(Calendar.PerQuarterToPerMonth(f.TheftPoolSum)), F(Calendar.PerQuarterToPerMonth(f.TheftRecipientsEarnedSum)), F(earned)) + "\t" + string.Join('\t', RegionProdRow(r).Select(F)));   // F〔履歴 Instruments.cs#426〕
                if (f.MigrationTo is { } to)
                {
                    int cfgW = RouteKm.CfgOf(w);
                    foreach (var kv in to) if (kv.Value > 0) sk.Migration.WriteLine(string.Join('\t', I(q), r.Name, kv.Key.Name, F(kv.Value), RouteText(r, kv.Key, cfgW, c)));
                    to.Clear();
                }
                if (f.CrossBorderTo is { } xto)   // CrossBorderTo〔履歴 Instruments.cs#427〕
                {
                    if (ownerOf is null)
                    {
                        ownerOf = new();
                        for (int oi = 0; oi < sim.Nations.Count; oi++) foreach (var orr in sim.Nations[oi].World.Regions) ownerOf[orr] = (oi, sim.Nations[oi].World);
                    }
                    int cfgS = RouteKm.CfgOf(w);
                    var (sla, slo) = Places.Coord(r.Name);
                    foreach (var kv in xto)
                    {
                        if (!(kv.Value > 0)) continue;
                        bool known = ownerOf.TryGetValue(kv.Key, out var ow);
                        var (dla, dlo) = Places.Coord(kv.Key.Name);
                        sk.Xmig.WriteLine(string.Join('\t', I(q), r.Name, kv.Key.Name, nation, known ? NameOf(ow.W) : "(なし)", F(kv.Value), F(Geo.DistanceKm(sla, slo, dla, dlo)),
                            RouteText(r, kv.Key, known ? cfgS & RouteKm.CfgOf(ow.W) : cfgS, c)));
                    }
                    xto.Clear();
                }
                f.MigrationOutK = f.MigrationInK = f.SettlerInK = f.CrossBorderOutK = f.CrossBorderInK = 0;   f.TheftPoolSum = f.TheftRecipientsEarnedSum = 0;
            }
        }
    }

    // OpiumRows〔履歴 Instruments.cs#428〕

    /// <summary>
    /// 読むだけ＝力学の値を書かない・乱数を引かない。〔履歴 Instruments.cs#429〕
    /// </summary>
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
                double pop = r.SumPopulation();
                popSum += pop; addSum += r.OpiumAddiction * pop; soldSum += sold;
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

    /// <summary>
    /// 〔履歴 Instruments.cs#430〕
    /// </summary>
    internal static void HabitSold(Region r, double sold) => FlowOf(r).HabitSoldSum += sold;

    /// <summary>
    /// 〔履歴 Instruments.cs#431〕
    /// </summary>
    internal static void OpiumTension(World w, double rate, double level)
    {
        var st = S(w); st.OpTensionAdded += rate; st.OpTensionPairMonths++;
        if (level > st.OpTensionLevelMax) st.OpTensionLevelMax = level;
    }

    /// <summary>
    /// 戦争1本ごと（wars）と(年, 国)ごとの戦死（casualty）。〔履歴 Instruments.cs#432〕
    /// </summary>
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

    // DiscoveryEvent〔履歴 Instruments.cs#433〕

    /// <summary>
    /// ratio＝代替する物の値段（実質）と新しいレシピで作る費用（物を生む行だけ・無ければ NaN）。〔履歴 Instruments.cs#434〕
    /// </summary>
    internal static void DiscoveryEvent(World w, int row, Discovery.Terms t, (double Replaced, double Cost) ratio)
        => S(w).DiscRows.Add(DiscRowText("E", w, row, t, ratio));

    /// <summary>
    /// 〔履歴 Instruments.cs#435〕
    /// </summary>
    internal static void DiscoveryTrace(World w, int row, Discovery.Terms t, (double Replaced, double Cost) ratio)
        => S(w).DiscRows.Add(DiscRowText("T", w, row, t, ratio));

    /// <summary>
    /// 蒸気機関と機関車の行だけ、買い手の国ごとに〈どの国から運び手の財を買ったか〉を四半期の和で貯める（述語が買い手の未解禁を要るので解禁後は貯まらない＝小さい表）。〔履歴 Instruments.cs#436〕
    /// </summary>
    internal static void DiscoveryContact(World buyer, World seller, int row, int good, double value)
    {
        if (!(value > 0) || Catalog.Discoveries[row].Name is not ("蒸気機関" or "機関車")) return;
        var acc = S(buyer).DiscContact; var key = (row, NameOf(seller), seller, good);   // acc〔履歴 Instruments.cs#437〕
        acc[key] = acc.TryGetValue(key, out double cur) ? cur + value : value;
    }

    /// <summary>
    /// 分子の内訳（disccontact_s の carrier）と並べて、運び手を広げた分の希釈（分母だけが太る側）を読む。〔履歴 Instruments.cs#438〕
    /// </summary>
    internal static void DiscoveryFlow(World w0, double[] gf, int[][] carriers)
    {
        var goods = new SortedSet<int>();
        for (int r = 0; r < carriers.Length; r++)
            if (Catalog.Discoveries[r].Name is "蒸気機関" or "機関車") foreach (int g in carriers[r]) goods.Add(g);
        var st = S(w0);
        foreach (int g in goods)
            if (g < gf.Length) st.DiscFlowRows.Add(string.Join('	', w0.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), Catalog.Goods[g].Name, F(gf[g])));
    }

    /// <summary>
    /// kind＝E 解禁した行／T 全ての行の軌道（8四半期ごと・解禁済みの行も＝knownFlag 1）。〔履歴 Instruments.cs#439〕
    /// </summary>
    internal static void EngineerRow(string kind, World w, int row, double a, double techTerm, bool known, double needTerm, (double Replaced, double Cost) ratio)
        => S(w).EngRows.Add(string.Join('\t', kind, w.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), NameOf(w), Catalog.Discoveries[row].Name,
            Catalog.Discoveries[row].Decade.ToString(System.Globalization.CultureInfo.InvariantCulture), F(a), F(techTerm), known ? "1" : "0",
            F(needTerm), ratio is (0, 0) ? "nan" : F(ratio.Replaced), ratio is (0, 0) ? "nan" : F(ratio.Cost)));   // F〔履歴 Instruments.cs#440〕

    /// <summary>
    /// census は T の行（8四半期ごと）と E の行（解禁した四半期）から A0 を決める（事前登録 [6]）。〔履歴 Instruments.cs#441〕
    /// </summary>
    static void EngineerTable(Sink sk, ulong seed, int q, MultiNationSim sim)
    {
        foreach (var n in sim.Nations)
        {
            var st = S(n.World);
            if (st.EngRows.Count == 0) continue;
            sk.Eng ??= Open("engineer", seed);
            if (!sk.EngHeader) { sk.Eng.WriteLine("kind\tquarter\tyear\tnation\trow\tdecade\tengineerPerMonth\ttechTerm\tknownFlag\tneedTerm\treplaced\tcost"); sk.EngHeader = true; }
            foreach (var row in st.EngRows)
            {
                int tab = row.IndexOf('\t');
                sk.Eng.WriteLine(row.Substring(0, tab + 1) + q.ToString(System.Globalization.CultureInfo.InvariantCulture) + row.Substring(tab));
            }
            st.EngRows.Clear();
        }
    }

    static string DiscRowText(string kind, World w, int row, Discovery.Terms t, (double Replaced, double Cost) ratio)
        => string.Join('\t', kind, w.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), NameOf(w), Catalog.Discoveries[row].Name,
            F(t.Tech), F(t.Need), F(t.Transmit), ratio is (0, 0) ? "nan" : F(ratio.Replaced), ratio is (0, 0) ? "nan" : F(ratio.Cost));   // 書かれなかった行（新兵器・制度・思想など）は nan

    /// <summary>
    /// replaced／cost は物を生む行（製法・新製品）だけ（それ以外は nan）。〔履歴 Instruments.cs#442〕
    /// </summary>
    static void DiscTable(Sink sk, ulong seed, int q, MultiNationSim sim)
    {
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Disc ??= Open("disc", seed);
        if (!sk.DiscHeader) { sk.Disc.WriteLine("kind\tquarter\tyear\tnation\trow\ttechTerm\tneedTerm\ttransmitTerm\treplaced\tcost\tnationUid"); sk.DiscHeader = true; }
        foreach (var n in sim.Nations)
        {
            var st = S(n.World);
            foreach (var row in st.DiscRows) { int tab = row.IndexOf('\t'); sk.Disc.WriteLine(row.Substring(0, tab + 1) + I(q) + row.Substring(tab) + '\t' + I(st.Uid)); }   // WriteLine〔履歴 Instruments.cs#443〕
            st.DiscRows.Clear();
            if (ReferenceEquals(n, sim.Nations[0]) && st.DiscFlowRows.Count > 0)   // ReferenceEquals〔履歴 Instruments.cs#444〕
            {
                sk.DiscFlow ??= Open("discflow", seed);
                if (!sk.DiscFlowHeader) { sk.DiscFlow.WriteLine("quarter	year	good	valueFlowQ"); sk.DiscFlowHeader = true; }
                foreach (var row in st.DiscFlowRows) sk.DiscFlow.WriteLine(I(q) + "	" + row);
                st.DiscFlowRows.Clear();
            }
            if (st.DiscContact.Count > 0)   // DiscContact〔履歴 Instruments.cs#445〕
            {
                sk.DiscContact ??= Open("disccontact", seed);
                if (!sk.DiscContactHeader) { sk.DiscContact.WriteLine("quarter	year	buyer	row	seller	carrier	valuePerQuarter	buyerUid	sellerUid"); sk.DiscContactHeader = true; }
                foreach (var ((crow, seller, sellerW, cgood), v) in st.DiscContact.OrderBy(kv => kv.Key.Row).ThenBy(kv => kv.Key.Seller, StringComparer.Ordinal).ThenBy(kv => kv.Key.Good).ThenBy(kv => S(kv.Key.SellerWorld).Uid))
                    sk.DiscContact.WriteLine(string.Join('	', I(q), I(n.World.Year), NameOf(n.World), Catalog.Discoveries[crow].Name, seller, Catalog.Goods[cgood].Name, F(v), I(st.Uid), I(S(sellerW).Uid)));   // 記帳の鍵も個体で分ける＝同じ名の別の国の売りが 1 行に合算されない）。〔履歴 Instruments.cs#446〕
                st.DiscContact.Clear();
            }
        }
    }

    // SiteOpened〔履歴 Instruments.cs#447〕

    /// <summary>
    /// parts＝[0] 人の数 N・[1] その財の値段・[2] 今のレシピで掘る費用。〔履歴 Instruments.cs#448〕
    /// </summary>
    internal static void SiteOpened(World w, Region r, Producer p, Discovery.Terms t, double[] parts) => S(w).SiteRows.Add(SiteRowText("O", w, r, p, t, parts));

    static string SiteRowText(string kind, World w, Region r, Producer p, Discovery.Terms t, double[] parts)
    {
        var site = Catalog.Sites[p.SiteRow];
        return string.Join('\t', kind, w.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), NameOf(w), r.Name, site.Name, site.Kind == Catalog.SiteKind.Deposit ? "D" : "P",
            F(Discovery.DifficultyOf(site.Decade, w.Coef)), F(parts[0]), F(parts[1]), F(parts[2]), F(t.Tech), F(t.Need));
    }

    /// <summary>
    /// 読むだけ。〔履歴 Instruments.cs#449〕
    /// </summary>
    static void SiteTables(Sink sk, ulong seed, int q, MultiNationSim sim)
    {
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Site ??= Open("site", seed);
        if (!sk.SiteHeader) { sk.Site.WriteLine("kind\tquarter\tyear\tnation\tregion\tsite\tsiteKind\tdifficulty\tpeopleK\tprice\tcost\ttechTerm\tneedTerm"); sk.SiteHeader = true; }
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
                if (Catalog.SilverId >= r.Markets.Length || Catalog.GoldOreId >= r.Markets.Length) continue;
                var mAg = r.Markets[Catalog.SilverId]; var mAu = r.Markets[Catalog.GoldOreId];
                prodAg += mAg.LastProduced; prodAu += mAu.LastProduced; demAg += mAg.LastDemand; demAu += mAu.LastDemand; stAg += mAg.Stock; stAu += mAu.Stock;
                if (!(mAg.LastDemand > 1e-9 || mAg.LastProduced > 1e-9) || !(mAu.LastDemand > 1e-9 || mAu.LastProduced > 1e-9)) continue;
                if (!(mAg.Price > 1e-9) || !(mAu.Price > 1e-9)) continue;
                lnSum += System.Math.Log(mAg.Price / mAu.Price / refRatio); nRatio++;
            }
        }
        sk.Metal.WriteLine(string.Join('\t', I(q), I(yr), F(prodAu), F(prodAg), F(demAu), F(demAg), F(stAu), F(stAg), I(nGold), I(nSilver), I(nFiat), nRatio > 0 ? F(lnSum / nRatio) : "nan", I(nRatio), names.ToString()));
        // 造幣局の標準の買い手が立つ条件（市場の値段≦造幣平価＝参照価格・Simulation.MintGold）と、   産出の跳ねがどの国で起きるかを読む。〔履歴 Instruments.cs#450〕
        sk.MetalNat ??= Open("metalnat", seed);
        if (!sk.MetalNatHeader) { sk.MetalNat.WriteLine("quarter\tyear\tnation\tlaw\tAuProduced\tAgProduced\tAuDemand\tAgDemand\tAuStock\tAgStock\tAuPriceOverParityProdWeighted\tAgPriceOverParityProdWeighted\tAuRegionsProducing\tAgRegionsProducing\tAuRegionsAtOrBelowParity\tAgRegionsAtOrBelowParity\tAuProducerOutput\tAgProducerOutput\tAuProducerCount\tAgProducerCount"); sk.MetalNatHeader = true; }
        foreach (var n in sim.Nations)
        {
            var w = n.World; var stn = S(w);
            if (!IsPicked(w, stn)) continue;
            double[] prod = new double[2], dem = new double[2], stk = new double[2], pw = new double[2], pOut = new double[2]; int[] rp = new int[2], rb = new int[2], pc = new int[2];
            int[] ids = [Catalog.GoldOreId, Catalog.SilverId];
            foreach (var r in w.Regions)
                for (int k = 0; k < 2; k++)
                {
                    int g = ids[k]; if (g >= r.Markets.Length) continue;
                    var m = r.Markets[g]; double par = Catalog.RefPrices[g];
                    prod[k] += m.LastProduced; dem[k] += m.LastDemand; stk[k] += m.Stock;
                    if (m.LastProduced > 1e-9 && m.Price > 1e-9 && par > 1e-9) { pw[k] += m.LastProduced * (m.Price / par); rp[k]++; }
                    if (m.Stock > 1e-12 && m.Price <= par) rb[k]++;
                    foreach (var pr in r.Producers) if (pr.GoodId == g) { pOut[k] += pr.Output; pc[k]++; }
                }
            int law = w.MonetaryLaw == MonetaryStandardLaw.Gold ? 2 : w.MonetaryLaw == MonetaryStandardLaw.Silver ? 1 : 0;
            sk.MetalNat.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), I(law), F(prod[0]), F(prod[1]), F(dem[0]), F(dem[1]), F(stk[0]), F(stk[1]),
                prod[0] > 1e-9 ? F(pw[0] / prod[0]) : "nan", prod[1] > 1e-9 ? F(pw[1] / prod[1]) : "nan", I(rp[0]), I(rp[1]), I(rb[0]), I(rb[1]), F(pOut[0]), F(pOut[1]), I(pc[0]), I(pc[1])));
        }
    }

    static void WarTables(Sink sk, DipState dip, Diplomacy dp)
    {
        using (var wr = Open("wars", sk.Seed))
        {
            wr.WriteLine("startYear\tcivilWarFlag\tsideA\tsideB\tmonths\tdeadK\tq\tentryCode");
            int wi = 0;
            foreach (var (year, war) in dip.Wars)
                wr.WriteLine(string.Join('\t', year.ToString(System.Globalization.CultureInfo.InvariantCulture), war.CivilWarParent is not null ? "1" : "0", NameOf(war.A), NameOf(war.B), F(war.Months), F(war.DeadA + war.DeadB), WarMetaCols(dp, wi++)));
        }
        using (var iv = Open("invade", sk.Seed))
        {
            iv.WriteLine("year	invader	target	targetIsNation	submitted	proximityAdded	addedToTarget	q");
            int ii = 0;
            foreach (var (year, invader, target, targetIsNation, submitted, added, addedToTarget) in dip.Invades)
                iv.WriteLine(string.Join('	', year.ToString(System.Globalization.CultureInfo.InvariantCulture), invader, target, targetIsNation ? "1" : "0", submitted ? "1" : "0", F(added), F(addedToTarget), WarInvadeQ(dp, ii++)));
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
            foreach (var p in r.Pops)
            {
                popK += p.Size; if (p.IsSoldier) soldierK += p.Size; supplyFillSum += p.SupplyFill * p.Size;
                foodMetSum += p.FoodMet; foodNeedSum += p.FoodNeed; needMetSum += p.NeedMet; needTotalSum += p.NeedTotal;   // FoodMet〔履歴 Instruments.cs#451〕
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
        var extras = NationExtras(w, st);   // extras〔履歴 Instruments.cs#452〕
        foreach (var ex in extras) cols.Add(ex.Value);
        cols.Add(F(threat + own > 0 ? threat / (threat + own) : 0)); cols.Add(F(army + own > 0 ? army / (army + own) : 0)); cols.Add(I((int)w.SerfEmancipation));
        cols.Add(I(core)); cols.Add(I(integrated)); cols.Add(I(colonial)); cols.Add(nonCore.ToString());
        cols.Add(F(led[(int)TreasuryCat.Borrow])); cols.Add(F(led[(int)TreasuryCat.Devalue])); cols.Add(F(led[(int)TreasuryCat.Army])); cols.Add(F(led[(int)TreasuryCat.Navy]));
        cols.Add(F(w.GovDebt.Value)); cols.Add(F(exports)); cols.Add(F(imports)); cols.Add(I(w.Coef.ClearingsPerYear));
        // 量は四半期の月平均（月数で割る）・月の数は整数〔履歴 Instruments.cs#453〕
        double mo = st.ClsMonths > 0 ? st.ClsMonths : double.NaN;
        cols.Add(I(st.QGateObs)); cols.Add(I(st.QGateClosed)); cols.Add(I(st.QShortMonths)); cols.Add(I(st.QRUnder1Months));
        cols.Add(F(st.QInterest / mo)); cols.Add(F(st.QRevenue / mo)); cols.Add(F(st.QDirect / mo)); cols.Add(F(st.QIndirect / mo)); cols.Add(F(st.QShortfall / mo)); cols.Add(F(st.QBorrowed / mo));
        cols.Add(I(st.FacStarted)); cols.Add(I(st.FacStartedClosed)); cols.Add(I(st.FacDone)); cols.Add(I(st.FacDoneClosed)); cols.Add(I(st.FacDoneClosedAtStart));
        cols.Add(F(popK > 0 ? supplyFillSum / popK : 0));   // 人口加重の供給の充足（補給崩壊の物差し＝この値が0.3 を割る国×四半期を数える・旧の注入 inj_s3a の fill と同じ式）
        // 台帳の月の和は四半期の末の POP の値（月の途中でない）〔履歴 Instruments.cs#454〕
        var stakeGoods = new List<string>();
        int stakeAll = StakeMismatch(w, out int stakeToInv, out int stakeToUninv, out int staleSub, stakeGoods);
        cols.Add(I(st.FsRun)); cols.Add(I(st.FsSeenMismatch)); cols.Add(I(st.FsFired)); cols.Add(I(st.FsFiredMismatch)); cols.Add(I(st.FsFiredOver)); cols.Add(I(st.SubMismatch)); cols.Add(I(st.SubMismatchOff));
        cols.Add(I(stakeAll)); cols.Add(I(stakeToInv)); cols.Add(I(stakeToUninv));
        cols.Add(string.Join(';', st.WheelGoods)); cols.Add(string.Join(';', st.FacStartedGoods));
        cols.Add(F(foodNeedSum > 0 ? foodMetSum / foodNeedSum : double.NaN)); cols.Add(F(needTotalSum > 0 ? needMetSum / needTotalSum : double.NaN));
        cols.Add(I(staleSub)); cols.Add(string.Join(';', stakeGoods));
        var ext265 = Extras265(w, st);   // ext265〔履歴 Instruments.cs#455〕
        foreach (var ex in ext265) cols.Add(ex.Value);
        if (st.Uid == 0) st.Uid = ++sk.NextUid;   // NextUid〔履歴 Instruments.cs#456〕
        var ext271 = Extras271(w, st);   // ext271〔履歴 Instruments.cs#457〕
        foreach (var ex in ext271) cols.Add(ex.Value);
        var ext270 = Extras270(w, st);   // ext270〔履歴 Instruments.cs#458〕
        foreach (var ex in ext270) cols.Add(ex.Value);
        var ext274 = Extras274(w, st);   // ext274〔履歴 Instruments.cs#459〕
        foreach (var ex in ext274) cols.Add(ex.Value);
        var ext278 = Extras278(w, st);   // ext278〔履歴 Instruments.cs#460〕
        foreach (var ex in ext278) cols.Add(ex.Value);
        var ext281 = Extras281(st);   // ext281〔履歴 Instruments.cs#461〕
        foreach (var ex in ext281) cols.Add(ex.Value);
        var ext286 = Extras286(w, st);   // ext286〔履歴 Instruments.cs#462〕
        foreach (var ex in ext286) cols.Add(ex.Value);
        var ext287 = Extras287(w);   // ext287〔履歴 Instruments.cs#463〕
        foreach (var ex in ext287) cols.Add(ex.Value);
        var ext289 = Extras289(st);   // ext289〔履歴 Instruments.cs#464〕
        foreach (var ex in ext289) cols.Add(ex.Value);
        var ext329 = Extras329(w);   // ext329〔履歴 Instruments.cs#465〕
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

    /// <summary>
    /// 出す国を絞らないと 61国×668四半期×財の数の行になる。〔履歴 Instruments.cs#466〕
    /// </summary>
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
                if (g >= r.Markets.Length) continue;
                var m = r.Markets[g];
                prod += m.LastProduced; dem += m.LastDemand; sold += m.LastSold; shortage += m.LastShortage; imp += m.LastImport; exp += m.LastExport; stock += m.Stock; price += m.Price; n++;
            }
            if (n == 0) continue;
            double ev = w.ExportsByGood is { } eg && g < eg.Length ? eg[g] : double.NaN, iv = w.ImportsByGood is { } ig && g < ig.Length ? ig[g] : double.NaN;
            sk.Goods.WriteLine(string.Join('\t', I(q), I(w.Year), nm, good.Name, good.Tier.ToString(), I(n), F(prod), F(dem), F(sold), F(shortage), F(imp), F(exp), F(stock), F(price / n), F(iv), F(ev)));
        }
    }

    /// <summary>
    /// 月ごとの量は四半期の月の平均（PerMonth・PerYear）・数は四半期の和。〔履歴 Instruments.cs#467〕
    /// </summary>
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

    /// <summary>
    /// nationUid＝国の個体の番号（nationIndex は国の消滅で詰まり、nation〔BaseName〕は建国で再利用・継承で変わる＝個体を指さない。〔履歴 Instruments.cs#468〕
    /// </summary>
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

    /// <summary>
    /// 建設の実際の額は spend_s の kind=extra「建設の材料と工賃」（資本家の階級）。〔履歴 Instruments.cs#469〕
    /// </summary>
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

    /// <summary>
    /// 来なかった四半期は結果の符号 −1・量は空（NaN）。〔履歴 Instruments.cs#470〕
    /// </summary>
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

    /// <summary>
    /// Government.Step の頭で数えた開いた戦線 N（政治予算の係数 (1/N)^0.5 の分母）それぞれが、その四半期の施行で何になったか。〔履歴 Instruments.cs#471〕
    /// </summary>
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

    /// <summary>
    /// 件数は符号 2 の戦線を代価＞b か否かで分ける（L＝段の法・連続＝税と支出の段 R・軍事費 M・治安維持力 C・関税 T）。〔履歴 Instruments.cs#472〕
    /// </summary>
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
        double nvPortStock = 0, nvInlandStock = 0, nvColStock = 0, nvOutCap = 0, nvOutOther = 0, nvBudCap = 0, nvBudOther = 0, nvBudColonial = 0, nvPaid = 0; int nvOrders = 0;   // nvPortStock〔履歴 Instruments.cs#473〕
        var tier = new double[3];
        foreach (var r in w.Regions)
        {
            double sz = r.SumPopulation(); popK += sz; if (r.Tier != RegionTier.Colonial) homePopK += sz;
            {
                bool isCap = r.Id == 0;
                var fgs = w.Coef.FleetGoodIds;   // 戦闘艦の3財（軍艦・装甲艦・弩級戦艦）の和
                foreach (var s in r.Producers) if (System.Array.IndexOf(fgs, s.GoodId) >= 0) { if (isCap) nvOutCap += s.Output; else nvOutOther += s.Output; }
                double stk = 0;
                foreach (int fg in fgs) if (fg >= 0 && fg < r.Markets.Length) stk += Math.Max(0, r.Markets[fg].Stock);
                if (!isCap) { if (r.Tier == RegionTier.Colonial) nvColStock += stk; else if (r.IsPort) nvPortStock += stk; else nvInlandStock += stk; }   // nvInlandStock〔履歴 Instruments.cs#474〕
                if (isCap) nvBudCap += r.NavalBudget; else nvBudOther += r.NavalBudget;
                if (!isCap && r.Tier == RegionTier.Colonial) nvBudColonial += r.NavalBudget;   // nvBudColonial〔履歴 Instruments.cs#475〕
                nvPaid += r.StateSpent[(int)TreasuryCat.Navy];   // 追記 2：今月の海軍の区分の支払い（予算の使い道＝使い残しの見え方）
                if (r.NavalOrder is not null) nvOrders++;
            }
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
        C("railCapacityStock", w.RailCapacity); C("steamFillShare", steamW > 0 ? steamV / steamW : 0);
        C("portFleetGoodsStock", nvPortStock); C("colonialFleetGoodsStock", nvColStock); C("inlandFleetGoodsStock", nvInlandStock); C("capitalFleetGoodsOutputPerMonth", nvOutCap); C("otherFleetGoodsOutputPerMonth", nvOutOther);   // C〔履歴 Instruments.cs#476〕
        C("navalBudgetCapitalPerMonth", nvBudCap); C("navalBudgetOtherPerMonth", nvBudOther); C("navalOrderStatesCount", nvOrders);
        C("navalBudgetColonialPerMonth", nvBudColonial); C("navyPaidPerMonth", nvPaid); C("fleetUnitsMovedOutHomeCum", S(w).FleetMovedOutHome); C("fleetUnitsMovedOutColonialCum", S(w).FleetMovedOutColonial); C("fleetUnitsMovedOutCededCum", S(w).FleetMovedOutCeded); C("fleetUnitsReceivedCededCum", S(w).FleetReceivedCeded); C("fleetUnitsReturnedToCapitalCum", S(w).FleetReturned);   // C〔履歴 Instruments.cs#477〕
          // Open〔履歴 Instruments.cs#478〕
        sk.Stock ??= Open("stock", seed);
        if (!sk.StockHeader) { sk.Stock.WriteLine(string.Join('\t', cols.Select(c => c.Name))); sk.StockHeader = true; }
        sk.Stock.WriteLine(string.Join('\t', cols.Select(c => c.Value)));
    }
}
