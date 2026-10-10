namespace PriceOfBread.SimCore;

/// <summary>★読むだけの標準の計器（2026-10-10 (242) 線I）＝回ごとに写しへ注入していた計器（inj_*.py）の代わりに、本体へ1回だけ置いた出力。
/// 既定は出さない：環境変数 <c>POB_INSTRUMENTS=&lt;出力ディレクトリ&gt;</c> を立てたときだけ、<see cref="On"/> が真になる（偽の間は本体から1命令も走らない＝分岐の中だけ）。
/// 出すもの（1試合＝seed ごとに3本の TSV）：
/// <list type="bullet">
/// <item><c>fiscal_s&lt;seed&gt;.tsv</c> 財政の鎖（<c>POB_INSTRUMENTS_NATIONS</c> に挙げた国の月ごと）：請求の項目別（r を掛ける前）・借入（払えない額・門・1件の額・国内プール・外資プール・どれが縛ったか）・切り下げ・r・
/// 兌換・国債の利率・クーポン・実際の収支（国庫の台帳の区分ごとの月の差分）・国庫の帳尻（Δ国庫−Σ台帳）。</item>
/// <item><c>class_s&lt;seed&gt;.tsv</c> 階級ごとの人数・貯蓄・所得（Earned）・支出（Paid）（挙げた国は四半期ごと・全ての国は年末）。</item>
/// <item><c>stock_s&lt;seed&gt;.tsv</c> 在庫と即応度と装備（財の在庫の価値・首都の軍需品の在庫・即応度・装備の充足・兵士・艦）。</item>
/// </list>
/// 開幕（構築直後 <c>open</c>）と助走の直後（<c>warm</c>）の断面は class と stock に全ての国で出る。初期値の一覧は Runner の <c>initdump</c>。
/// <para>⚠<b>読むだけ</b>＝力学の値を1つも書かない・乱数を引かない・メモ化された量を新しい位置で読まない（<see cref="World.GdpPerMonth"/> は四半期の末と助走の直後にだけ読む）。
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
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, WState> States = new();
    static WState S(World w) => States.GetValue(w, _ => new WState());

    sealed class Sink
    {
        public System.IO.StreamWriter? Fiscal, Class, Stock;
        public bool FiscalHeader, ClassHeader, StockHeader;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, Sink> Sinks = new();

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
        st.Treasury0 = w.Treasury; st.Payable0 = st.Spare0 = st.Payable1 = st.Borrowed = st.Devalued = st.ClaimsTotal = st.PayRate = double.NaN;
        st.BorrowCode = -1; st.Shortfall = st.Ticket = st.DomPool = st.ForPool = double.NaN;
    }
    static WState? Live(World w) { var st = S(w); return st.Active && st.Armed ? st : null; }

    /// <summary>請求の最初の4項（直近の利払い・社会保障・年金・植民地の維持費）。</summary>
    internal static void ClaimsBase(World w)
    {
        if (Live(w) is not { } st) return;
        st.Claim[(int)Item.Interest] = w.LastInterestPerMonth; st.Claim[(int)Item.Welfare] = w.LastWelfareCostPerMonth; st.Claim[(int)Item.Pension] = w.LastPensionCostPerMonth; st.Claim[(int)Item.ColonialUpkeep] = w.LastColonialUpkeepPerMonth;
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

    // ---------------------------------------------------------------- 月末（Simulation.MonthSettle の終わり） ----------------------------------------------------------------

    /// <summary>月の決算の終わり：階級ごとの所得と支出を四半期の合計へ足し、挙げた国は財政の1行を溜める（台帳の差分は前の月の決算の終わりから）。</summary>
    internal static void MonthClose(World w)
    {
        var st = S(w);
        if (!st.Active || !st.Armed) { st.Armed = false; return; }
        st.Armed = false;
        foreach (var r in w.Regions) foreach (var p in r.Pops) { int c = (uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1; st.ClsEarn[c] += p.EarnedPerMonth; st.ClsPaid[c] += p.PaidPerMonth; }
        st.ClsMonths++;
        var led = w.TreasuryLedger;
        if (IsPicked(w, st) && st.HaveLedger)
        {
            var sb = new System.Text.StringBuilder();
            FiscalRow(w, st, sb, header: false);
            st.Rows.Add(sb.ToString());
        }
        st.PrevTreasury = w.Treasury; Array.Copy(led, st.PrevLedger, NCat); st.HaveLedger = true;
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
        C("treasuryAtClaimStock", st.Treasury0); C("revenueBasePerMonth", w.LastRevenuePerMonth); C("payableBeforeBorrowStock", st.Payable0); C("spareBeforeBorrowPerMonth", st.Spare0);
        C("borrowShortfallPerMonth", st.Shortfall); C("borrowTicketPerMonth", st.Ticket); C("borrowDomesticPoolStock", st.DomPool); C("borrowForeignPoolStock", st.ForPool);
        C("borrowedPerMonth", st.Borrowed);
        T("borrowBinder", st.BorrowCode switch { 0 => "gateClosed", 1 => "full", 2 => "ticket", 3 => "pool", 4 => "none", _ => "noShortfall" });
        C("borrowCooldownMonths", w.BorrowCooldownMonths); C("interestToRevenueShareNow", w.LastRevenuePerMonth > 0 ? w.LastInterestPerMonth / w.LastRevenuePerMonth : double.NaN);
        C("devaluedPerMonth", st.Devalued); C("payableAfterBorrowStock", st.Payable1); C("payRateShare", st.PayRate);
        C("convertibilityShare", w.Convertibility); C("onGold", w.OnGoldStandard ? 1 : 0); C("govDebtStock", w.GovDebt);
        C("govBondRatePerYear", w.GovBondRatePerYear); C("govAvgCouponPerYear", w.GovAvgCouponPerYear); C("insolvencyMonths", w.InsolvencyQuarters); C("defaultCount", w.SovereignDefaultCount);
        var led = w.TreasuryLedger; double dSum = 0;
        for (int k = 0; k < NCat; k++) { double d = led[k] - st.PrevLedger[k]; dSum += d; C("ledger" + ((TreasuryCat)k) + "PerMonth", d); }
        double dT = w.Treasury - st.PrevTreasury;
        C("treasuryEndStock", w.Treasury); C("treasuryDeltaPerMonth", dT); C("ledgerSumPerMonth", dSum); C("ledgerResidualPerMonth", dT - dSum);
        if (sb.Length > 0) sb.Length--;   // 末尾のタブ
    }

    // ---------------------------------------------------------------- 四半期の末・断面（SelfPlay.Run が呼ぶ） ----------------------------------------------------------------

    static Sink SinkOf(MultiNationSim sim) => Sinks.GetValue(sim, _ => new Sink());
    static System.IO.StreamWriter Open(string kind, ulong seed) => new(System.IO.Path.Combine(Dir, $"{kind}_s{seed}.tsv"), false, new System.Text.UTF8Encoding(false), 1 << 16);

    /// <summary>断面（開幕 <c>open</c>・助走の直後 <c>warm</c>）：全ての国の階級と在庫の行。開幕は GDP を読まない（構築直後に和を取らない）。</summary>
    internal static void Phase(MultiNationSim sim, ulong seed, string time)
    {
        var sk = SinkOf(sim);
        foreach (var n in sim.Nations) { ClassRows(sk, seed, time, n.World, S(n.World), flows: false); StockRow(sk, seed, time, n.World, readGdp: time != "open"); }
    }

    /// <summary>四半期の末：挙げた国は毎四半期・全ての国は年末（q%4==3）。財政の月次の行を書き出し、階級の所得と支出の四半期の合計をゼロへ戻す。</summary>
    internal static void Quarter(MultiNationSim sim, ulong seed, int q)
    {
        var sk = SinkOf(sim);
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
            Array.Clear(st.ClsEarn); Array.Clear(st.ClsPaid); st.ClsMonths = 0;
        }
    }

    /// <summary>試合の終わり（例外の経路も）：書き出しを閉じる。</summary>
    internal static void Close(MultiNationSim sim)
    {
        if (!Sinks.TryGetValue(sim, out var sk)) return;
        sk.Fiscal?.Dispose(); sk.Class?.Dispose(); sk.Stock?.Dispose(); sk.Fiscal = sk.Class = sk.Stock = null;
    }

    static void ClassRows(Sink sk, ulong seed, string time, World w, WState st, bool flows)
    {
        var size = new double[NC]; var sav = new double[NC]; var negSav = new double[NC]; var names = new string[NC];
        foreach (var r in w.Regions) foreach (var p in r.Pops)
        {
            int c = (uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1;
            size[c] += p.Size; sav[c] += p.Savings; if (p.Savings < 0) negSav[c] += p.Savings; if (names[c] is null) names[c] = p.ClassName;
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
        double popK = 0, homePopK = 0, soldierK = 0, stockValue = 0, ships = 0;
        var tier = new double[3];
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            double sz = r.SumPopulation(); popK += sz; if (r.Tier != RegionTier.Colonial) homePopK += sz;
            foreach (var p in r.Pops) if (p.IsSoldier) soldierK += p.Size;
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
        double gdpYear = readGdp ? Calendar.PerMonthToPerYear(w.GdpPerMonth) : double.NaN;
        var cols = new List<(string Name, string Value)>();
        void C(string name, double v) => cols.Add((name, F(v)));
        cols.Add(("time", time)); cols.Add(("nation", NameOf(w))); C("year", w.Year);
        C("populationK", popK); C("homePopulationK", homePopK); C("soldierK", soldierK); C("soldierToPopulationShare", popK > 0 ? soldierK / popK : double.NaN);
        C("totalPopulationK", w.TotalPopulation); C("totalMoneyStock", w.TotalMoney);   // 階級の表の合計との突き合わせ用（合計＝分母）
        C("treasuryStock", w.Treasury); C("govDebtStock", w.GovDebt); C("gdpRealPerYear", gdpYear);
        C("goodsStockValueStock", stockValue); C("goodsStockToGdpYear", gdpYear > 0 ? stockValue / gdpYear : double.NaN);
        C("stapleStockValueStock", tier[0]); C("dailyStockValueStock", tier[1]); C("luxuryStockValueStock", tier[2]);
        C("capitalArtilleryStock", Stk(Catalog.ArtilleryId)); C("capitalRifleStock", Stk(Catalog.RifleId)); C("capitalAmmunitionStock", Stk(Catalog.AmmunitionId)); C("capitalWarshipStock", Stk(Catalog.WarshipId));
        C("fleetUnitsStock", ships);
        C("militaryReadinessShare", w.MilitaryReadiness); C("capitalEquipFillShare", cap?.MilitaryEquipFill ?? double.NaN); C("trainingFillShare", w.MilitaryTrainingFill);
        C("armyUpkeepShare", w.ArmyUpkeep); C("navyUpkeepShare", w.NavyUpkeep);
        C("landPowerIndex", sp?.LandPower ?? double.NaN); C("navalPowerIndex", sp?.NavalPower ?? double.NaN);
        sk.Stock ??= Open("stock", seed);
        if (!sk.StockHeader) { sk.Stock.WriteLine(string.Join('\t', cols.Select(c => c.Name))); sk.StockHeader = true; }
        sk.Stock.WriteLine(string.Join('\t', cols.Select(c => c.Value)));
    }
}
