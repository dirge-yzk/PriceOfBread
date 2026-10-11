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
/// <item><c>region_s&lt;seed&gt;.tsv</c> 州×四半期（全ての国の全ての州）：人口・農村人口・機会とその因子・耕地・四半期の人口の流れ（州の間の移民・入植・越境の出入り）・戦場の強さ・主食の充足。
/// <c>migration_s&lt;seed&gt;.tsv</c> 州の間の移民の組（出る州→入る州）の四半期の量。州の空洞化（F1 の線）の読み手 <c>hol/an_h2.py</c> の入力＝旧の注入 <c>u4e/inj_ghost.py</c> の代わり。</item>
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
public static class Instruments
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
        public bool Active, Armed;                         // 今月の写しが有効か／月初の Begin が走ったか
        public readonly double[] Claim = new double[(int)Item.Count];
        public double Treasury0, Payable0, Spare0, Payable1, Borrowed, Devalued, ClaimsTotal, PayRate;
        public int BorrowCode = -1;                        // -1＝借入の判定に来なかった（払えない額なし）0＝門が閉じた 1＝満額（払えない額を全部借りた）2＝1件の額が縛った 3＝プール（国内＋外資）が縛った 4＝借りられなかった（額が1e-9以下）
        public double Shortfall, Ticket, DomPool, ForPool;
        public bool HaveLedger; public double PrevTreasury; public readonly double[] PrevLedger = new double[NCat];
        public readonly List<string> Rows = new();
        public readonly double[] ClsEarn = new double[NC], ClsPaid = new double[NC]; public int ClsMonths;
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
        public System.IO.StreamWriter? Fiscal, Class, Stock, Nation, Region, Migration, Flow, Goods;
        public bool FiscalHeader, ClassHeader, StockHeader, NationHeader, RegionHeader, FlowHeader, GoodsHeader;
        public ulong Seed;   // 試合の終わり（Close）に戦争の表を書くときのファイル名
        public int NextUid;  // (271) 国の個体の番号の次
        public readonly Dictionary<string, (int Quarter, int Year)> CanalOpened = new();   // (271) 運河の開通の四半期（最初に開通を見た四半期の末）
    }
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
        if (!st.Active) { st.HaveLedger = false; return; }
        Array.Clear(st.Claim);
        st.Treasury0 = w.Treasury.Value; st.Payable0 = st.Spare0 = st.Payable1 = st.Borrowed = st.Devalued = st.ClaimsTotal = st.PayRate = double.NaN;
        st.BorrowCode = -1; st.Shortfall = st.Ticket = st.DomPool = st.ForPool = double.NaN;
        st.GateOpenNow = -1; st.RevDirectNet = st.RevIndirect = double.NaN;
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

    /// <summary>借りる前（月初の払える額と余剰が決まった所）。</summary>
    internal static void PreBorrow(World w, double payable, double spare)
    {
        if (Live(w) is not { } st) return;
        st.Payable0 = payable; st.Spare0 = spare;
        st.GateOpenNow = Fiscal.BorrowGateOpen(w, w.Coef) ? 1 : 0;   // 次の行の Fiscal.Borrow が読むのと同じ門（純粋な関数＝読むだけ）
    }
    /// <summary>歳入の内訳（Fiscal.Step が予算の基数を書いた所）：純税収と間接税の実収。予算の基数＝純税収＋<c>IndirectTaxInBudgetBase</c>×間接税。</summary>
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
    internal static void Construction(World w, double desiredUnits, double budgetUnits)
    {
        if (!(desiredUnits > 0)) return;
        var st = S(w);
        st.ConDesiring++; st.ConDesiredUnits += desiredUnits;
        if (budgetUnits < desiredUnits) { st.ConBound++; st.ConCutUnits += desiredUnits - budgetUnits; }
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
        st.DecTarget = Government.MilitaryStage(w.Policy.MilitarySpendingTarget); st.DecEnforced = Government.MilitaryStage(w.MilitarySpending); st.DecShare = w.MilitarySpending;
        st.MmEnvelopeAfterGov = Military.WarStageShare(w, w.MilitarySpending);   // (270) 戦時段の器の割合（エンジンの量そのもの）
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
        st.FactorySold = 0; st.MonoBefore = st.MonoTarget = -1; st.FiscalStressAtDecision = double.NaN;
        // (271) 助走の分は数えない
        st.CnRows = st.CnBound = 0; st.CnDesired = st.CnDemand = 0;
        st.ConDesiring = st.ConBound = 0; st.ConDesiredUnits = st.ConCutUnits = 0;
        st.BrJudged = st.BrPoolBound = st.BrForeignCut = 0; st.BrDomPool = st.BrForRaw = st.BrFor = 0;
        st.CrDays = st.CrRepayBoundDays = st.CrFiniteDays = 0; st.CrReserveCap = st.CrRepayable = 0;
        st.MmOutcome = st.MmFronts = -1; st.MmIncreasing = false;
        st.MmCur = st.MmTarget = st.MmNet = st.MmResist = st.MmSpeed = st.MmPush = st.MmOppTrad = st.MmOppModern = st.MmOppMass = st.MmStock = st.MmStockMax = st.MmEnvelopeAfterGov = double.NaN;
        st.Gf.Clear(); st.GfCounted = st.GfMilExpired = false; st.GfLevelIdx = -1; st.GfGateWhy = 0; st.GfGateNet = double.NaN;   // (274) 助走の分は数えない
        st.GfSHead = st.GfMaxHead = st.GfSEnd = st.GfPendCost = st.GfPendS = st.GfGateCost = st.GfGateS = double.NaN; st.GfPaidL = st.GfPaidR = st.GfPaidM = st.GfPaidT = st.GfPaidC = 0;   // (278) 助走の分は数えない
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
        N("militaryStageEnforcedEndStage", Government.MilitaryStage(w.MilitarySpending)); C("militarySpendingEndShare", w.MilitarySpending);
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

    /// <summary>貯蓄 <c>Pop._savings</c> の書き手の種類。書き手は Pop の中の6本だけ（<c>Pop.Distribute</c>・<c>Take</c>・<c>Carry</c>・<c>Rebase</c>・<c>SettleMonth</c>・開幕の <c>RebaseSavingsTo</c>〔(273) 旧 ScaleSavings〕）。
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

    // ---------------------------------------------------------------- 月末（Simulation.MonthSettle の終わり） ----------------------------------------------------------------

    /// <summary>月の決算の終わり：階級ごとの所得と支出を四半期の合計へ足し、挙げた国は財政の1行を溜める（台帳の差分は前の月の決算の終わりから）。</summary>
    internal static void MonthClose(World w)
    {
        var st = S(w);
        if (!st.Active || !st.Armed) { st.Armed = false; return; }
        st.Armed = false;
        foreach (var r in w.Regions) foreach (var p in r.Pops) { int c = (uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1; st.ClsEarn[c] += p.EarnedPerMonth.Value; st.ClsPaid[c] += p.PaidPerMonth.Value; }
        st.ClsMonths++;
        if (st.GateOpenNow >= 0) { st.QGateObs++; if (st.GateOpenNow == 0) st.QGateClosed++; }
        if (st.Spare0 < 0) { st.QShortMonths++; st.QShortfall += -st.Spare0; }
        if (st.PayRate < 1) st.QRUnder1Months++;
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
        if (sb.Length > 0) sb.Length--;   // 末尾のタブ
    }

    // ---------------------------------------------------------------- 四半期の末・断面（SelfPlay.Run が呼ぶ） ----------------------------------------------------------------

    static System.IO.StreamWriter Open(string kind, ulong seed)
    {
        System.IO.Directory.CreateDirectory(Dir);   // 環境変数が存在しないディレクトリを指しても試合ごとに落ちない（(244) 反証役 vi1#7）
        return new(System.IO.Path.Combine(Dir, $"{kind}_s{seed}.tsv"), false, new System.Text.UTF8Encoding(false), 1 << 16);
    }

    /// <summary>断面（開幕 <c>open</c>・助走の直後 <c>warm</c>）：全ての国の階級と在庫の行。開幕は GDP を読まない（構築直後に和を取らない）。</summary>
    internal static void Phase(MultiNationSim sim, ulong seed, string time)
    {
        var sk = SinkOf(sim, seed);
        if (time == "warm") foreach (var n in sim.Nations) { var wst = S(n.World); wst.WarMonths = 0; wst.CivilWarMonths = 0; ResetQuarter(wst); wst.SavLedger.Clear(); }   // 助走の月は数えない（旧の注入も助走の直後に0へ戻していた）
        foreach (var n in sim.Nations) { ClassRows(sk, seed, time, n.World, S(n.World), flows: false); StockRow(sk, seed, time, n.World, readGdp: time != "open"); }
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
            NationRow(sk, seed, q, i, w, st, yearEnd);
            Array.Clear(st.ClsEarn); Array.Clear(st.ClsPaid); st.ClsMonths = 0; st.WarMonths = 0; st.CivilWarMonths = 0; ResetQuarter(st);
            st.QGateObs = st.QGateClosed = st.QShortMonths = st.QRUnder1Months = 0; st.QInterest = st.QRevenue = st.QDirect = st.QIndirect = st.QShortfall = st.QBorrowed = 0;
            st.FacStarted = st.FacStartedClosed = st.FacDone = st.FacDoneClosed = st.FacDoneClosedAtStart = 0;
        }
        RegionRows(sk, seed, q, sim);
        foreach (var cp in sim.Diplomacy.Chokepoints)   // (271) 運河の開通の四半期（最初に開通を見た四半期の末）
            if (cp.IsCanal && cp.IsOpen && !sk.CanalOpened.ContainsKey(cp.Id)) sk.CanalOpened[cp.Id] = (q, sim.Nations.Count > 0 ? sim.Nations[0].World.Year : 0);
    }

    /// <summary>試合の終わり（例外の経路も）：書き出しを閉じる。</summary>
    internal static void Close(MultiNationSim sim)
    {
        if (!Sinks.TryGetValue(sim, out var sk)) return;
        if (Dips.TryGetValue(sim.Diplomacy, out var dip)) WarTables(sk, dip);
        CanalTable(sk, sim);
        sk.Fiscal?.Dispose(); sk.Class?.Dispose(); sk.Stock?.Dispose(); sk.Nation?.Dispose(); sk.Region?.Dispose(); sk.Migration?.Dispose(); sk.Flow?.Dispose(); sk.Goods?.Dispose(); sk.Fiscal = sk.Class = sk.Stock = sk.Nation = sk.Region = sk.Migration = sk.Flow = sk.Goods = null;
    }

    // ---------------------------------------------------------------- 州の人口と移動（州の空洞化の計器・(255)）----------------------------------------------------------------

    /// <summary>州ごとの四半期の人口の流れ（千人）。四半期の末に <c>region</c> と <c>migration</c> の行へ書いて0へ戻す。</summary>
    sealed class RegionFlow
    {
        public double MigrationOutK, MigrationInK, SettlerInK, CrossBorderOutK, CrossBorderInK;
        public Dictionary<Region, double>? MigrationTo;   // 出る州→入る州ごとの量（四半期の合計・州の間の移民だけ）
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, RegionFlow> Flows = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Pop, double[]> SettlerOut = new();   // 入植で本国の pop から出た量（四半期の合計）＝出た pop が居る州の行に書く
    static RegionFlow FlowOf(Region r) => Flows.GetValue(r, _ => new RegionFlow());

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
                + "migrationOutKPerQuarter\tmigrationInKPerQuarter\tsettlerOutKPerQuarter\tsettlerInKPerQuarter\tcrossBorderOutKPerQuarter\tcrossBorderInKPerQuarter\twarIntensity\tfoodFillShare");
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
                double pop = 0, rural = 0, unemp = 0, unrest = 0, foodMet = 0, foodNeed = 0, settlerOut = 0;
                foreach (var p in r.Pops)
                {
                    pop += p.Size; if (!p.IsUrban) rural += p.Size;
                    unemp += p.Size * p.UnemploymentRate; unrest += p.Size * p.Militancy; foodMet += p.FoodMet; foodNeed += p.FoodNeed;
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
                    F(f.MigrationOutK), F(f.MigrationInK), F(settlerOut), F(f.SettlerInK), F(f.CrossBorderOutK), F(f.CrossBorderInK), F(r.LastWarIntensity), F(foodNeed > 0 ? foodMet / foodNeed : 1)));
                if (f.MigrationTo is { } to)
                {
                    foreach (var kv in to) if (kv.Value > 0) sk.Migration.WriteLine(string.Join('\t', I(q), r.Name, kv.Key.Name, F(kv.Value)));
                    to.Clear();
                }
                f.MigrationOutK = f.MigrationInK = f.SettlerInK = f.CrossBorderOutK = f.CrossBorderInK = 0;
            }
        }
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
                + string.Join('\t', ext278.Select(e => e.Name)));
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
        cols.Add(("warStageEnvelopeShareAfterGov", F(st.MmEnvelopeAfterGov))); cols.Add(("warStageEnvelopeShareEnd", F(Military.WarStageShare(w, w.MilitarySpending))));
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
        C("armyUpkeepShare", w.ArmyUpkeep); C("navyUpkeepShare", w.NavyUpkeep);
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
