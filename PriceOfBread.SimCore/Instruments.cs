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
/// <item><c>region_s&lt;seed&gt;.tsv</c> 州×四半期（全ての国の全ての州）：人口・農村人口・機会とその因子・耕地・四半期の人口の流れ（州の間の移民・入植・越境の出入り）・戦場の強さ・主食の充足。
/// <c>migration_s&lt;seed&gt;.tsv</c> 州の間の移民の組（出る州→入る州）の四半期の量。州の空洞化（F1 の線）の読み手 <c>hol/an_h2.py</c> の入力＝旧の注入 <c>u4e/inj_ghost.py</c> の代わり。</item>
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
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, WState> States = new();
    static WState S(World w) => States.GetValue(w, _ => new WState());

    sealed class Sink
    {
        public System.IO.StreamWriter? Fiscal, Class, Stock, Nation, Region, Migration;
        public bool FiscalHeader, ClassHeader, StockHeader, NationHeader, RegionHeader;
        public ulong Seed;   // 試合の終わり（Close）に戦争の表を書くときのファイル名
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
    }

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
    internal static void Borrow(World w, double shortfall, double ticket, double domesticPool, double foreignPool, double borrowed)
    {
        if (Live(w) is not { } st) return;
        st.Shortfall = shortfall; st.Ticket = ticket; st.DomPool = domesticPool; st.ForPool = foreignPool;
        double want = Math.Min(ticket, shortfall);
        st.BorrowCode = !(borrowed > 1e-9) ? 4 : borrowed < want - 1e-9 ? 3 : ticket < shortfall ? 2 : 1;
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
        if (st.PayRate == st.PayRate) st.AdmPayRate += st.PayRate;
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
    /// <summary>四半期の頭の <c>Government.Step</c> の後：戦時段の解禁・軍事費の段の目標（保存）と施行。</summary>
    internal static void AfterGovernment(World w)
    {
        var st = S(w);
        st.DecOpen = Government.WarStageOpen(w) ? 1 : 0;
        st.DecTarget = Government.MilitaryStage(w.Policy.MilitarySpendingTarget); st.DecEnforced = Government.MilitaryStage(w.MilitarySpending); st.DecShare = w.MilitarySpending;
    }
    static void ResetQuarter(WState st)
    {
        st.AdmMonths = st.AdmPaidMonths = st.AdmRBelow1Months = 0;
        st.AdmSimClaim = st.AdmCatClaim = st.AdmCatPaid = st.AdmFiscClaim = st.AdmBase = st.AdmSpendable = st.AdmLastRev = st.AdmSimMul = st.AdmCover = st.AdmFiscMul = st.AdmPayRate = st.AdmFill = st.AdmFillRBelow1 = 0;
        st.DecScale = st.DecLosing = st.DecShare = double.NaN;
        st.DecAtWar = st.DecMobilized = st.DecOpen = st.DecTarget = st.DecEnforced = -1; st.DecPicked = 0;
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
        StartGates.GetValue(p, _ => new GateAtStart()).Closed = closed;
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
        if (IsPicked(w, st) && st.HaveLedger)
        {
            var sb = new System.Text.StringBuilder();
            FiscalRow(w, st, sb, header: false);
            st.Rows.Add(sb.ToString());
        }
        st.PrevTreasury = w.Treasury.Value; Array.Copy(led, st.PrevLedger, NCat); st.HaveLedger = true;
        st.BudMul = st.BudCover = 0; st.BudSpendable = st.BudLastRev = double.NaN;   // 月の予算の位置で溜めた基数を次の月へ持ち越さない
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
        if (time == "warm") foreach (var n in sim.Nations) { var wst = S(n.World); wst.WarMonths = 0; wst.CivilWarMonths = 0; ResetQuarter(wst); }   // 助走の月は数えない（旧の注入も助走の直後に0へ戻していた）
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
            NationRow(sk, seed, q, i, w, st, yearEnd);
            Array.Clear(st.ClsEarn); Array.Clear(st.ClsPaid); st.ClsMonths = 0; st.WarMonths = 0; st.CivilWarMonths = 0; ResetQuarter(st);
            st.QGateObs = st.QGateClosed = st.QShortMonths = st.QRUnder1Months = 0; st.QInterest = st.QRevenue = st.QDirect = st.QIndirect = st.QShortfall = st.QBorrowed = 0;
            st.FacStarted = st.FacStartedClosed = st.FacDone = st.FacDoneClosed = st.FacDoneClosedAtStart = 0;
        }
        RegionRows(sk, seed, q, sim);
    }

    /// <summary>試合の終わり（例外の経路も）：書き出しを閉じる。</summary>
    internal static void Close(MultiNationSim sim)
    {
        if (!Sinks.TryGetValue(sim, out var sk)) return;
        if (Dips.TryGetValue(sim.Diplomacy, out var dip)) WarTables(sk, dip);
        sk.Fiscal?.Dispose(); sk.Class?.Dispose(); sk.Stock?.Dispose(); sk.Nation?.Dispose(); sk.Region?.Dispose(); sk.Migration?.Dispose(); sk.Fiscal = sk.Class = sk.Stock = sk.Nation = sk.Region = sk.Migration = null;
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
        double popK = 0, soldierK = 0, supplyFillSum = 0;
        int core = 0, integrated = 0, colonial = 0;
        var nonCore = new System.Text.StringBuilder();
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Pops) { popK += p.Size; if (p.IsSoldier) soldierK += p.Size; supplyFillSum += p.SupplyFill * p.Size; }
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
        sk.Nation ??= Open("nation", seed);
        if (!sk.NationHeader)
        {
            sk.Nation.WriteLine("quarter\tyear\tnationIndex\tnation\ttotalPopulationK\tpopulationK\tsoldierK\tnominalGdpPerYear\tatWarMonths\tcivilWarMonths\tconscriptionLaw\tmobilizedFlag\t"
                + string.Join('\t', extras.Select(e => e.Name)) + "\t"
                + "securityThreatShare\tarmyThreatShare\tserfEmancipation\tcoreRegions\tintegratedRegions\tcolonialRegions\tnonCoreRegions\t"
                + "ledgerBorrowCumulative\tledgerDevalueCumulative\tledgerArmyCumulative\tledgerNavyCumulative\tgovDebtStock\texportsPerClearing\timportsPerClearing\tclearingsPerYear\t"
                + "borrowGateObservedMonths\tborrowGateClosedMonths\tshortfallMonths\tpayRateBelowOneMonths\t"
                + "interestMeanPerMonth\trevenueBaseMeanPerMonth\tdirectTaxNetMeanPerMonth\tindirectRevenueMeanPerMonth\tshortfallMeanPerMonth\tborrowedMeanPerMonth\t"
                + "stateFacilityStartedCount\tstateFacilityStartedGateClosedCount\tstateFacilityCompletedCount\tstateFacilityCompletedGateClosedCount\tstateFacilityCompletedGateClosedAtStartCount\tsupplyFillShare");
            sk.NationHeader = true;
        }
        sk.Nation.WriteLine(string.Join('\t', cols));
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
