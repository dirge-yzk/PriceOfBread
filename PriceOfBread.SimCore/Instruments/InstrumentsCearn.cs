using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（計器の欄は ConditionalWeakTable と ThreadStatic だけ）。〔履歴 InstrumentsCearn.cs#1〕
public static partial class Instruments
{
    internal const string CearnHeaderText = "quarter\tyear\tnation\tregion\ttier\tpopK\tsavingsStart\tsavings\t"
        + "inMarketPerQuarter\tinExportPerQuarter\tinStatePerQuarter\tinCreditPerQuarter\tinFromPopsPerQuarter\tinMigrationPerQuarter\tinOtherPerQuarter\t"
        + "outMarketPerQuarter\toutTaxPerQuarter\toutBondPerQuarter\toutDuesPerQuarter\toutColonialRentPerQuarter\toutCapitalPerQuarter\toutImportPerQuarter\toutMigrationPerQuarter\toutOtherPerQuarter\t"
        + "residualPerQuarter\tlateRegisteredPops\tstateMarketSpendPerQuarter\tstateMarketTakePerQuarter\t"
        + "outTop1Route\toutTop1PerQuarter\toutTop2Route\toutTop2PerQuarter\tinTop1Route\tinTop1PerQuarter\t"
        + "localPriceMultiplier\tlocalPriceAtFloorFlag\tregionPriceLevel\tcostPriceLevel\tsavingsPerCapitaOverNation\tsavingsPerCapitaOverMainland\t"
        + "stapleGood\tstaplePrice\tstapleMarginalCost\ttop1Good\ttop1Price\ttop1MarginalCost\ttop2Good\ttop2Price\ttop2MarginalCost\ttop3Good\ttop3Price\ttop3MarginalCost\t"
        + "belowSelfCostShare\tfactoryOutputRefValuePerMonth\textractionOutputRefValuePerMonth\tfrozenFlag\tpollClaimPerMonth\tearnedGrossPerMonth\ttaxRate\tpollTaxLevel\t"
        + "emptyPopSavings\testateDuePerQuarter\testateNoHeirPerQuarter\testateMovedPerQuarter\testateInPerQuarter\testateOutFromLivePerQuarter\testateInToEmptyPerQuarter\testateRecvPerCapitaSpread"   // testateOutFromLivePerQuarter〔履歴 InstrumentsCearn.cs#2〕
        + "\timportPayerKind\timportBudgetStartPerQuarter\timportPaidPerQuarter\timportCrossRegionPerQuarter"   // timportBudgetStartPerQuarter〔履歴 InstrumentsCearn.cs#3〕
        + "\timportOldRuleBudgetStartPerQuarter\timportBudgetExcessMaxPerClearing"   // timportOldRuleBudgetStartPerQuarter〔履歴 InstrumentsCearn.cs#4〕
        + "\timportBudgetExcessMaxPerClearingSub\timportPaidPayerSizeLe1e12PerQuarter\timportPaidPayerSizeMidPerQuarter\timportPaidPayerSizeGt1e9PerQuarter"   // timportBudgetExcessMaxPerClearingSub〔履歴 InstrumentsCearn.cs#5〕
        + "\timportBudgetExcessMaxPerClearingSubLive"   // timportBudgetExcessMaxPerClearingSubLive〔履歴 InstrumentsCearn.cs#6〕
        + "\tmerchantDebt\testateDebtSettledPerQuarter"   // testateDebtSettledPerQuarter〔履歴 InstrumentsCearn.cs#7〕
        + "\tbillBlockedClearings\tbillBlockedDustClearings\temptyDebtAfterEstate\testateDebtMovedPerQuarter\theirDebtSettledPerQuarter"   // testateDebtMovedPerQuarter〔履歴 InstrumentsCearn.cs#8〕
        + "\theirDebtAfterSettleSum\tbillWithHeirDebtClearings";   // tbillWithHeirDebtClearings〔履歴 InstrumentsCearn.cs#9〕

    /// <summary>台帳の列（流入 7・流出 9）。<see cref="CearnColumn"/> の返り値。</summary>
    internal enum CearnCol : sbyte
    {
        Skip = -1,
        InMarket, InExport, InState, InCredit, InFromPops, InMigration, InOther,
        OutMarket, OutTax, OutBond, OutDues, OutColonialRent, OutCapital, OutImport, OutMigration, OutOther,
    }
    internal const int CearnInCols = 7, CearnCols = 16;

    internal sealed class CearnAcc
    {
        public readonly double[] Flow = new double[CearnCols];   // 流入は増分の和・流出は減りの和（正）
        public double Start = double.NaN; public bool Started;
        public int Late;
        public double StateSpend, StateTake;
        public double EstDue, EstNoHeir, EstMoved, EstIn, EstOutLive, EstInEmpty, EstSpread, EstMin = double.PositiveInfinity, EstMax = double.NegativeInfinity;   // EstMin〔履歴 InstrumentsCearn.cs#10〕
        public int ImpKind; public double ImpBudget, ImpPaid, ImpCross;   // ImpKind〔履歴 InstrumentsCearn.cs#11〕
        public double ImpOldBudget, ImpExMax = double.NegativeInfinity, ImpHeadStock = double.NaN, ImpPaidClr; public bool ImpHeadOn;   // ImpExMax〔履歴 InstrumentsCearn.cs#12〕
        public double ImpHeadStockSub = double.NaN, ImpExMaxSub = double.NegativeInfinity; public readonly double[] ImpPaidBand = new double[3];   // ImpHeadStockSub〔履歴 InstrumentsCearn.cs#13〕
        public int BillBlocked, BillBlockedDust, EmptyDebtAfter;   // EmptyDebtAfter〔履歴 InstrumentsCearn.cs#14〕
        public double EstDebtMoved, HeirDebtSettled, EstDebtBeforeSnap, EstSettledSnap;   // EstSettledSnap〔履歴 InstrumentsCearn.cs#15〕
        public double HeirDebtAfterSettleSum; public int BillWithHeirDebtClearings;   // HeirDebtAfterSettleSum〔履歴 InstrumentsCearn.cs#16〕
        public double EstDebtSettled;   // EstDebtSettled〔履歴 InstrumentsCearn.cs#17〕
        public double ImpExMaxSubLive = double.NegativeInfinity; public bool ImpClrDead;   // ImpExMaxSubLive〔履歴 InstrumentsCearn.cs#18〕
        public readonly Dictionary<(string File, int Line, SavKind Kind, char Cp), double> Out = new(), In = new();
        public readonly CgAcc[] G = [new(), new(), new(), new()];   // G〔履歴 InstrumentsCearn.cs#19〕
        public void Clear() { System.Array.Clear(Flow); Late = 0; StateSpend = 0; StateTake = 0; Out.Clear(); In.Clear(); EstDue = EstNoHeir = EstMoved = EstIn = EstOutLive = EstInEmpty = EstSpread = 0; EstMin = double.PositiveInfinity; EstMax = double.NegativeInfinity; ImpKind = 0; ImpBudget = ImpPaid = ImpCross = 0; ImpOldBudget = 0; ImpExMax = double.NegativeInfinity; ImpHeadStock = double.NaN; ImpPaidClr = 0; ImpHeadOn = false; ImpHeadStockSub = double.NaN; ImpExMaxSub = double.NegativeInfinity; System.Array.Clear(ImpPaidBand); ImpExMaxSubLive = double.NegativeInfinity; ImpClrDead = false; EstDebtSettled = 0; BillBlocked = BillBlockedDust = EmptyDebtAfter = 0; EstDebtMoved = HeirDebtSettled = 0; HeirDebtAfterSettleSum = 0; BillWithHeirDebtClearings = 0; }
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, CearnAcc> CearnAccs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Pop, CearnAcc> CearnPopAcc = new();
    sealed class CearnSink { public System.IO.StreamWriter? W, G, P; public bool Header, GHeader, PHeader; }   // CearnSink〔履歴 InstrumentsCearn.cs#20〕
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, CearnSink> CearnSinks = new();
    [System.ThreadStatic] static TreasuryCat _cearnCat;

    /// <summary>試験の口：州の台帳（無ければ null）。</summary>
    internal static CearnAcc? CearnAccOf(Region r) => CearnAccs.TryGetValue(r, out var a) ? a : null;

    /// <summary>World.Transfer の頭（計器の行）：国庫の側の区分（受け手が国庫なら toCat ?? cat・払い手が国庫なら cat＝どちらも Pay に渡る値）を写す。</summary>
    internal static void CearnRoute(TreasuryCat cat, TreasuryCat? toCat) => _cearnCat = toCat ?? cat;

    /// <summary>列の振り分け（純関数）。<paramref name="fileName"/>＝呼び手のファイル名（パスを除く）。精算（Settle）は 2 列に分けるので呼ばない（<see cref="CearnCol.Skip"/>）。</summary>
    internal static CearnCol CearnColumn(SavKind kind, char cp, TreasuryCat cat, string fileName, double delta)
    {
        switch (kind)
        {
            case SavKind.Settle: case SavKind.RegionOut: case SavKind.RegionIn: return CearnCol.Skip;
            case SavKind.Rebase: return delta > 0 ? CearnCol.InOther : CearnCol.OutOther;
            case SavKind.CarryIn: return CearnCol.InMigration;
            case SavKind.CarryOut: return CearnCol.OutMigration;
            case SavKind.Give: case SavKind.GiveIncome:
                return cp switch
                {
                    'T' => CearnCol.InState,
                    'C' or 'H' => CearnCol.InCredit,
                    'P' => CearnCol.InFromPops,
                    'B' or 'M' or 'F' => fileName == "CapitalMarket.cs" ? CearnCol.InOther : CearnCol.InExport,
                    _ => CearnCol.InOther,
                };
            case SavKind.Take:
                return cp switch
                {
                    'T' => cat == TreasuryCat.Tax ? CearnCol.OutTax : cat == TreasuryCat.Borrow ? CearnCol.OutBond : CearnCol.OutOther,
                    'P' => fileName == "Fiscal.cs" ? CearnCol.OutDues : fileName == "Colonial.cs" ? CearnCol.OutColonialRent : CearnCol.OutOther,
                    'B' => fileName == "CapitalMarket.cs" ? CearnCol.OutCapital : fileName == "BilateralMarket.cs" ? CearnCol.OutImport : CearnCol.OutOther,
                    _ => CearnCol.OutOther,
                };
            default: return delta > 0 ? CearnCol.InOther : CearnCol.OutOther;
        }
    }

    /// <summary>Sav の頭：登録された POP の書き込みを、その州の台帳の列へ 1 件足す。</summary>
    static void CearnSav(Pop p, SavKind kind, double delta)
    {
        if (!CearnOn) return;   // CearnOn〔履歴 InstrumentsCearn.cs#21〕
        if (kind is SavKind.RegionOut or SavKind.RegionIn) return;   // 州の移し替えの記帳（お金は動かない）
        if (!CearnPopAcc.TryGetValue(p, out var a)) return;
        var ga = a.G[CgGroupOf(p)];   // ga〔履歴 InstrumentsCearn.cs#22〕
        CgPops.TryGetValue(p, out var cw);
        if (cw is not null && cw.InWin) cw.Win += delta;   // InWin〔履歴 InstrumentsCearn.cs#23〕
        if (kind == SavKind.Settle)
        {
            double earned = Calendar.OneMonthOf(p.EarnedPerMonth).Value;   // 精算の所得の側（同じ月の値・精算の書き込みの直後に読む）
            a.Flow[(int)CearnCol.InMarket] += earned;
            a.Flow[(int)CearnCol.OutMarket] += earned - delta;             // 支払いの側＝所得−増分（精算の 1 行に閉じる）
            ga.Flow[(int)CearnCol.InMarket] += earned;
            ga.Flow[(int)CearnCol.OutMarket] += earned - delta;
            if (cw is not null)   // null〔履歴 InstrumentsCearn.cs#24〕
            {
                cw.QFlow[(int)CearnCol.InMarket] += earned;
                cw.QFlow[(int)CearnCol.OutMarket] += earned - delta;
                cw.QEarned += earned; cw.QPaid += earned - delta;
                cw.QLand += Calendar.OneMonthOf(p.LandRentPerMonth).Value; cw.QInd += Calendar.OneMonthOf(p.IndustrialRentPerMonth).Value;
                cw.QSub += Calendar.OneMonthOf(p.SubsistenceValuePerMonth).Value;
                if (cw.InWin) cw.WinEarned += earned;
            }
            return;
        }
        bool routed = kind is SavKind.Give or SavKind.GiveIncome or SavKind.Take or SavKind.CarryOut or SavKind.CarryIn;
        char cp = !routed ? '-' : kind is SavKind.Give or SavKind.GiveIncome ? _routeFrom : kind == SavKind.Take ? _routeTo : 'P';
        string file = _routeFile ?? "";
        var col = CearnColumn(kind, cp, _cearnCat, CearnFileName(file), delta);
        if (kind is SavKind.Give or SavKind.GiveIncome && cp == 'T' && CearnFileName(file) == "BilateralMarket.cs")   // BilateralMarket〔履歴 InstrumentsCearn.cs#25〕
        {
            CearnImportSubsidyInto(a, delta);
            if (_cearnCat == TreasuryCat.Subsidy) CearnImportSubsidySubInto(a, delta);   // CearnImportSubsidySubInto〔履歴 InstrumentsCearn.cs#26〕
        }
        if (kind == SavKind.Take && CearnFileName(file) == "BilateralMarket.cs") CearnImportPaid(a, -delta, p.Size);   // BilateralMarket〔履歴 InstrumentsCearn.cs#27〕
        if (_estRepay && kind == SavKind.Take) a.EstDebtSettled -= delta;   // EstDebtSettled〔履歴 InstrumentsCearn.cs#28〕
        if (col == CearnCol.Skip) return;
        bool isIn = (int)col < CearnInCols;
        a.Flow[(int)col] += isIn ? delta : -delta;
        ga.Flow[(int)col] += isIn ? delta : -delta;
        if (cw is not null)   // null〔履歴 InstrumentsCearn.cs#29〕
        {
            cw.QFlow[(int)col] += isIn ? delta : -delta;
            if (col == CearnCol.OutOther) { var oq = cw.QOutOther ??= new(); var ok = (routed ? file : "", routed ? _routeLine : 0, kind, cp); oq.TryGetValue(ok, out double ov); oq[ok] = ov - delta; }   // TryGetValue〔履歴 InstrumentsCearn.cs#30〕
            if (col == CearnCol.OutTax && cw.InWin) cw.WinTax -= delta;
        }
        if (routed && CearnFileName(file) == "EmptyPopEstate.cs") EstateSav(a, cw, p, kind, delta);   // EmptyPopEstate〔履歴 InstrumentsCearn.cs#31〕
        if (!routed) return;
        var key = (file, _routeLine, kind, cp);
        var d = isIn ? a.In : a.Out;
        d.TryGetValue(key, out double v);
        d[key] = v + (isIn ? delta : -delta);
    }

    [System.ThreadStatic] static string? _cearnLastPath, _cearnLastName;
    /// <summary>呼び手のファイルの名前（パスを除く）。直前と同じ文字列の参照（呼び手の位置の定数）なら写しを使う＝呼ぶたびに文字列を作らない。</summary>
    static string CearnFileName(string path)
    {
        if (!ReferenceEquals(path, _cearnLastPath)) { _cearnLastPath = path; _cearnLastName = System.IO.Path.GetFileName(path); }
        return _cearnLastName ?? "";
    }

    static double CearnSavings(Region r) { double s = 0; foreach (var p in r.Pops) s += p.Savings.Value; return s; }

    /// <summary>月初（Begin）：全ての国の州の POP を州の台帳へ登録する。初めての州は始まりの在庫をここで取る。四半期の途中で初めて登録した POP は lateRegisteredPops に数える。</summary>
    static void CearnRegister(World w)
    {
        if (!CearnOn) return;   // CearnOn〔履歴 InstrumentsCearn.cs#32〕
        foreach (var r in w.Regions)
        {
            var a = CearnAccs.GetValue(r, _ => new CearnAcc());
            bool fresh = !a.Started;
            if (fresh) { a.Start = CearnSavings(r); a.Started = true; CgReset(a, r); }
            foreach (var p in r.Pops)
            {
                var cw = CgPops.GetValue(p, _ => new CgPop());   // cw〔履歴 InstrumentsCearn.cs#33〕
                cw.InWin = true; cw.Begin = p.Savings.Value; cw.Win = 0;
                cw.WinTax = 0; cw.WinEarned = 0;   // WinTax〔履歴 InstrumentsCearn.cs#34〕
                if (CearnPopAcc.TryGetValue(p, out var a0) && ReferenceEquals(a0, a)) continue;
                CearnPopAcc.AddOrUpdate(p, a);
                if (!fresh) { a.Late++; a.G[CgGroupOf(p)].Late++; cw.QLate++; }
            }
        }
    }

    /// <summary>World.SettleStateSpent の頭：州の国家の仮勘定（区分つき・正＝国庫→生産者・負＝国庫へ）を読む。</summary>
    internal static void CearnStateSpent(Region r)
    {
        if (!CearnOn || !CearnAccs.TryGetValue(r, out var a)) return;
        foreach (double v in r.StateSpent) { if (v > 0) a.StateSpend += v; else if (v < 0) a.StateTake -= v; }
    }

    // importPayerKind＝四半期の最後の清算の頭の州の状態（0 清算の頭なし・1 商人の POP が人数 ＞1e-12・2 商人の POP の人数 ≦1e-12・3 商人の添字なし）。〔履歴 InstrumentsCearn.cs#35〕
    [System.ThreadStatic] static Region? _impRegion;
    internal static void CearnImportHead(Region r)
    {
        if (!CearnOn) return;
        CearnImportHeadInto(CearnAccs.GetValue(r, _ => new CearnAcc()), r);
    }
    /// <summary>
    /// 清算ごとの超過：頭で前の清算の〈払った額−頭の在庫〉を畳んでから今の清算を始める（手形〔在庫 ∞〕の清算は畳まない）。〔履歴 InstrumentsCearn.cs#36〕
    /// </summary>
    internal static void CearnImportHeadInto(CearnAcc a, Region r)
    {
        int mi = r.MerchantPopIndex; Pop? m = mi >= 0 && mi < r.Pops.Count ? r.Pops[mi] : null;
        double b = 0, ob = 0;
        if (m is not null && m.Size > 1e-12) { a.ImpKind = 1; b = m.Debt.Value > 0 ? System.Math.Max(0, m.Savings.Value) : double.PositiveInfinity; }
        else { a.ImpKind = m is null ? 3 : 2; foreach (var p in r.Pops) if (p.Size > 1e-12) b += System.Math.Max(0, p.Savings.Value); }
        if (m is not null) ob = m.Debt.Value > 0 ? System.Math.Max(0, m.Savings.Value) : double.PositiveInfinity;
        else foreach (var p in r.Pops) ob += System.Math.Max(0, p.Savings.Value);
        a.ImpBudget += b; a.ImpOldBudget += ob;
        double heirDebt = 0; foreach (var p in r.Pops) if (p.Size > 1e-12) heirDebt += p.Debt.Value;   // heirDebt〔履歴 InstrumentsCearn.cs#37〕
        a.HeirDebtAfterSettleSum += heirDebt;
        if (m is not null && !(m.Debt.Value > 0) && heirDebt > 1e-9) a.BillWithHeirDebtClearings++;   // BillWithHeirDebtClearings〔履歴 InstrumentsCearn.cs#38〕
        if (m is not null && m.Debt.Value > 0) { a.BillBlocked++; if (!(m.Debt.Value > 1e-12)) a.BillBlockedDust++; }   // BillBlockedDust〔履歴 InstrumentsCearn.cs#39〕
        CearnImportFoldInto(a);
        a.ImpHeadOn = true; a.ImpHeadStock = a.ImpHeadStockSub = double.IsPositiveInfinity(b) ? double.NaN : b; a.ImpPaidClr = 0;
        a.ImpClrDead = false;   // ImpClrDead〔履歴 InstrumentsCearn.cs#40〕
    }
    /// <summary>
    /// 〔履歴 InstrumentsCearn.cs#41〕
    /// </summary>
    internal static void CearnImportFoldInto(CearnAcc a)
    {
        if (!a.ImpHeadOn) return;
        a.ImpHeadOn = false;
        if (double.IsNaN(a.ImpHeadStock)) return;
        double ex = a.ImpPaidClr - a.ImpHeadStock;
        if (ex > a.ImpExMax) a.ImpExMax = ex;
        double exSub = a.ImpPaidClr - a.ImpHeadStockSub;   // exSub〔履歴 InstrumentsCearn.cs#42〕
        if (exSub > a.ImpExMaxSub) a.ImpExMaxSub = exSub;
        if (!a.ImpClrDead && exSub > a.ImpExMaxSubLive) a.ImpExMaxSubLive = exSub;   // ImpExMaxSubLive〔履歴 InstrumentsCearn.cs#43〕
    }
    /// <summary>
    /// 〔履歴 InstrumentsCearn.cs#44〕
    /// </summary>
    internal static void CearnImportSubsidyInto(CearnAcc a, double amount) { if (a.ImpHeadOn && !double.IsNaN(a.ImpHeadStock)) a.ImpHeadStock += amount; }
    /// <summary>
    /// 〔履歴 InstrumentsCearn.cs#45〕
    /// </summary>
    internal static void CearnImportSubsidySubInto(CearnAcc a, double amount) { if (a.ImpHeadOn && !double.IsNaN(a.ImpHeadStockSub)) a.ImpHeadStockSub += amount; }
    /// <summary>
    /// 試験の口。〔履歴 InstrumentsCearn.cs#46〕
    /// </summary>
    internal static void CearnImportPaidBandInto(CearnAcc a, double amount, double payerSize) { a.ImpPaidBand[payerSize <= 1e-12 ? 0 : payerSize <= 1e-9 ? 1 : 2] += amount; }
    /// <summary>払いの段の行の州を置く（null で外す）。</summary>
    internal static void CearnImportPayRegion(Region? r) { if (CearnOn) _impRegion = r; }
    /// <summary>
    /// 〔履歴 InstrumentsCearn.cs#47〕
    /// </summary>
    internal static void EstateRepay(bool on) { _estRepay = on && CearnOn; }
    [System.ThreadStatic] static bool _estRepay;
    internal static void CearnImportPaidInto(CearnAcc a, double amount) { a.ImpPaidClr += amount; }   // CearnImportPaidInto〔履歴 InstrumentsCearn.cs#48〕
    /// <summary>
    /// 試験の口。〔履歴 InstrumentsCearn.cs#49〕
    /// </summary>
    internal static void CearnImportPaidDeadInto(CearnAcc a, double amount, double payerSize) { if (payerSize <= 1e-12 && amount != 0) a.ImpClrDead = true; }
    static void CearnImportPaid(CearnAcc a, double amount, double payerSize)
    {
        a.ImpPaid += amount; CearnImportPaidInto(a, amount); CearnImportPaidBandInto(a, amount, payerSize); CearnImportPaidDeadInto(a, amount, payerSize);
        if (_impRegion is { } ir && CearnAccs.TryGetValue(ir, out var ra) && !ReferenceEquals(ra, a)) a.ImpCross += amount;
    }

    /// <summary>助走の直後（Phase warm）：助走の月の流れを捨て、始まりの在庫を取り直す。</summary>
    static void CearnWarm(MultiNationSim sim)
    {
        if (!CearnOn) return;   // CearnOn〔履歴 InstrumentsCearn.cs#50〕
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                var a = CearnAccs.GetValue(r, _ => new CearnAcc());
                a.Clear(); a.Start = CearnSavings(r); a.Started = true; CgReset(a, r);
            }
    }

    /// <summary>四半期の末の断面の列（値段と床・主食と上位 3 財・自己費用の下限・凍結・人頭税の物差し）。試験が州から直に呼べる。</summary>
    internal readonly record struct CearnSnap(double PopK, double Savings, double Lpm, int AtFloor, double RegionPriceLevel, double CostPriceLevel, double SavPerCapOverNation, double SavPerCapOverMainland,
        int StapleGood, double StaplePrice, double StapleMc, int[] TopGood, double[] TopPrice, double[] TopMc, double BelowSelfCostShare,
        double FacOut, double ExtOut, int Frozen, double PollClaimPerMonth, double EarnedGrossPerMonth);

    /// <summary>国の 1 人あたりの貯蓄の 2 つの物差し：Mainland＝州の乗数の分母と同じ本土（植民地格を除く）の州の和（本土の人口が 0 なら全ての州）・All＝全ての州の和（事前登録の定義）。分母が正でなければ NaN。</summary>
    internal static (double Mainland, double All) CearnNationPerCap(World w)
    {
        double ns = 0, np = 0, ms = 0, mp = 0;
        foreach (var x in w.Regions)
        {
            double rs = 0, rp = 0;
            foreach (var p in x.Pops) { rs += p.Savings.Value; rp += p.Size; }
            ns += rs; np += rp;
            if (x.Tier != RegionTier.Colonial) { ms += rs; mp += rp; }
        }
        double all = np > 0 ? ns / np : double.NaN;
        return (mp > 1e-9 ? ms / mp : all, all);
    }

    internal static CearnSnap CearnSnapshot(World w, Region r, double natSavPerCap, double mainSavPerCap)
    {
        var c = w.Coef;
        double pop = 0, sav = 0, poll = 0, earned = 0;
        bool pollOn = w.PollTax > 1e-9;
        foreach (var p in r.Pops)
        {
            pop += p.Size; sav += p.Savings.Value;
            double e = p.LastEarnedPerMonth.Value; if (e > 0) earned += e;
            if (pollOn && p.IsCommoner && !p.IsCriminal) poll += c.PollTaxRate * w.PollTax * p.Size;   // 査定の全額（届く割合と払える割合を掛けない）
        }
        double lpm = r.LocalPriceMultiplier, cpl = w.CostPriceLevel;
        int atFloor = lpm <= c.RegionalPriceLevelMin + 1e-9 ? 1 : 0;
        double perCap = pop > 0 && natSavPerCap > 0 ? sav / pop / natSavPerCap : double.NaN;
        double perCapMain = pop > 0 && mainSavPerCap > 0 ? sav / pop / mainSavPerCap : double.NaN;
        // 主食＝層 Staple の財のうち州の LastDemand が最大の財
        int staple = -1; double stapleDem = -1;
        int ng = System.Math.Min(r.Markets.Length, Catalog.Goods.Length);
        for (int g = 0; g < ng; g++)
            if (Catalog.Goods[g].Tier == GoodTier.Staple && r.Markets[g].LastDemand > stapleDem) { stapleDem = r.Markets[g].LastDemand; staple = g; }
        // 上位 3 財＝州の生産者（労働の財と倒産を除く）の Output×参照価格の和の大きい順
        var val = new Dictionary<int, double>();
        int below = 0, belowDen = 0;
        foreach (var s in r.Producers)
        {
            int g = s.GoodId;
            if (Catalog.IsLaborGood(g) || s.Bankrupt || g < 0 || g >= ng) continue;
            double rp = g < Catalog.RefPrices.Length ? Catalog.RefPrices[g] : 0;
            val.TryGetValue(g, out double v0); val[g] = v0 + s.Output * rp;
            if (s.Scale > 0) { belowDen++; if (r.Markets[g].Price <= s.MarginalCost * cpl * lpm) below++; }
        }
        var top = new List<KeyValuePair<int, double>>(val);
        top.Sort((x, y) => y.Value != x.Value ? y.Value.CompareTo(x.Value) : x.Key.CompareTo(y.Key));
        var tg = new int[3]; var tp = new double[3]; var tm = new double[3];
        for (int k = 0; k < 3; k++)
        {
            if (k < top.Count && top[k].Value > 0) { tg[k] = top[k].Key; tp[k] = r.Markets[tg[k]].Price; tm[k] = CearnMc(r, tg[k]); }
            else { tg[k] = -1; tp[k] = double.NaN; tm[k] = double.NaN; }
        }
        var prod = RegionProdRow(r);   // region_s と同じ定義の工場と採取の産出（計器の関数）
        int frozen = prod[0] == 0 && prod[1] == 0 && prod[2] + prod[4] > 0 ? 1 : 0;
        return new CearnSnap(pop, sav, lpm, atFloor, r.PriceLevel, cpl, perCap, perCapMain,
            staple, staple >= 0 ? r.Markets[staple].Price : double.NaN, staple >= 0 ? CearnMc(r, staple) : double.NaN, tg, tp, tm,
            belowDen > 0 ? (double)below / belowDen : double.NaN, prod[0], prod[1], frozen, poll, earned);
    }

    /// <summary>州の生産者のうちその財の Capacity が最大の 1 件の MarginalCost（参照価格の単位）。無ければ NaN。</summary>
    static double CearnMc(Region r, int g)
    {
        Producer? best = null;
        foreach (var s in r.Producers) if (s.GoodId == g && !s.Bankrupt && (best is null || s.Capacity > best.Capacity)) best = s;
        return best is null ? double.NaN : best.MarginalCost;
    }

    static string CearnTop(Dictionary<(string File, int Line, SavKind Kind, char Cp), double> d, int rank, out double amount)
    {
        amount = double.NaN;
        if (d.Count <= rank) return "-";
        var l = new List<KeyValuePair<(string File, int Line, SavKind Kind, char Cp), double>>(d);
        l.Sort((x, y) =>
        {
            int c = y.Value.CompareTo(x.Value); if (c != 0) return c;
            c = string.CompareOrdinal(x.Key.File, y.Key.File); if (c != 0) return c;
            c = x.Key.Line.CompareTo(y.Key.Line); if (c != 0) return c;
            c = x.Key.Kind.CompareTo(y.Key.Kind); return c != 0 ? c : x.Key.Cp.CompareTo(y.Key.Cp);
        });
        var k = l[rank].Key; amount = l[rank].Value;
        return System.IO.Path.GetFileName(k.File) + ":" + k.Line.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + SavKindNames[(int)k.Kind] + ":" + k.Cp;
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：全ての国の全ての州の行を書き、台帳を戻して始まりの在庫を取り直す。</summary>
    static void CearnTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!CearnOn) return;   // CearnOn〔履歴 InstrumentsCearn.cs#51〕
        var sk = CearnSinks.GetValue(sim, _ => new CearnSink());
        sk.W ??= Open("cearn", seed);
        if (!sk.Header) { sk.W.WriteLine(CearnHeaderText); sk.Header = true; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            var w = n.World; string nation = NameOf(w);
            var (mainPerCap, natPerCap) = CearnNationPerCap(w);
            foreach (var r in w.Regions)
            {
                var a = CearnAccs.GetValue(r, _ => new CearnAcc());
                var sn = CearnSnapshot(w, r, natPerCap, mainPerCap);
                double inSum = 0, outSum = 0;
                for (int k = 0; k < CearnInCols; k++) inSum += a.Flow[k];
                for (int k = CearnInCols; k < CearnCols; k++) outSum += a.Flow[k];
                double resid = a.Started ? sn.Savings - a.Start - (inSum - outSum) : double.NaN;
                var cols = new List<string>(64) { I(q), I(w.Year), nation, r.Name, r.Tier.ToString(), F(sn.PopK), F(a.Started ? a.Start : double.NaN), F(sn.Savings) };
                foreach (double v in a.Flow) cols.Add(F(v));
                cols.Add(F(resid)); cols.Add(I(a.Late)); cols.Add(F(a.StateSpend)); cols.Add(F(a.StateTake));
                cols.Add(CearnTop(a.Out, 0, out double o1)); cols.Add(F(o1)); cols.Add(CearnTop(a.Out, 1, out double o2)); cols.Add(F(o2)); cols.Add(CearnTop(a.In, 0, out double i1)); cols.Add(F(i1));
                cols.Add(F(sn.Lpm)); cols.Add(I(sn.AtFloor)); cols.Add(F(sn.RegionPriceLevel)); cols.Add(F(sn.CostPriceLevel)); cols.Add(F(sn.SavPerCapOverNation)); cols.Add(F(sn.SavPerCapOverMainland));
                cols.Add(sn.StapleGood >= 0 ? Catalog.Goods[sn.StapleGood].Name : "-"); cols.Add(F(sn.StaplePrice)); cols.Add(F(sn.StapleMc));
                for (int k = 0; k < 3; k++) { cols.Add(sn.TopGood[k] >= 0 ? Catalog.Goods[sn.TopGood[k]].Name : "-"); cols.Add(F(sn.TopPrice[k])); cols.Add(F(sn.TopMc[k])); }
                cols.Add(F(sn.BelowSelfCostShare)); cols.Add(F(sn.FacOut)); cols.Add(F(sn.ExtOut)); cols.Add(I(sn.Frozen));
                cols.Add(F(sn.PollClaimPerMonth)); cols.Add(F(sn.EarnedGrossPerMonth)); cols.Add(F(w.TaxRate.Value)); cols.Add(F(w.PollTax));
                EstateFold(a);   // EstateFold〔履歴 InstrumentsCearn.cs#52〕
                cols.Add(F(EstateEmptySavings(r))); cols.Add(F(a.EstDue)); cols.Add(F(a.EstNoHeir)); cols.Add(F(a.EstMoved)); cols.Add(F(a.EstIn)); cols.Add(F(a.EstOutLive)); cols.Add(F(a.EstInEmpty)); cols.Add(F(a.EstSpread));
                CearnImportFoldInto(a);   // CearnImportFoldInto〔履歴 InstrumentsCearn.cs#53〕
                cols.Add(I(a.ImpKind)); cols.Add(F(a.ImpBudget)); cols.Add(F(a.ImpPaid)); cols.Add(F(a.ImpCross));   // Add〔履歴 InstrumentsCearn.cs#54〕
                cols.Add(F(a.ImpOldBudget)); cols.Add(F(double.IsNegativeInfinity(a.ImpExMax) ? double.NaN : a.ImpExMax));   // Add〔履歴 InstrumentsCearn.cs#55〕
                cols.Add(F(double.IsNegativeInfinity(a.ImpExMaxSub) ? double.NaN : a.ImpExMaxSub)); cols.Add(F(a.ImpPaidBand[0])); cols.Add(F(a.ImpPaidBand[1])); cols.Add(F(a.ImpPaidBand[2]));   // Add〔履歴 InstrumentsCearn.cs#56〕
                cols.Add(F(double.IsNegativeInfinity(a.ImpExMaxSubLive) ? double.NaN : a.ImpExMaxSubLive));   // Add〔履歴 InstrumentsCearn.cs#57〕
                cols.Add(F(r.MerchantPop is { } mpd ? mpd.Debt.Value : double.NaN)); cols.Add(F(a.EstDebtSettled));   // Add〔履歴 InstrumentsCearn.cs#58〕
                cols.Add(I(a.BillBlocked)); cols.Add(I(a.BillBlockedDust)); cols.Add(I(a.EmptyDebtAfter)); cols.Add(F(a.EstDebtMoved)); cols.Add(F(a.HeirDebtSettled));   // Add〔履歴 InstrumentsCearn.cs#59〕
                cols.Add(F(a.HeirDebtAfterSettleSum)); cols.Add(I(a.BillWithHeirDebtClearings));   // Add〔履歴 InstrumentsCearn.cs#60〕
                sk.W.WriteLine(string.Join('\t', cols));
                if (r.Tier == RegionTier.Colonial) CgRows(sk, seed, q, w, nation, r, a, sn.Savings, inSum - outSum);   // RegionTier〔履歴 InstrumentsCearn.cs#61〕
                a.Clear(); a.Start = sn.Savings; a.Started = true; CgReset(a, r);
            }
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void CearnClose(MultiNationSim sim)
    {
        if (CearnSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; sk.G?.Dispose(); sk.G = null; sk.P?.Dispose(); sk.P = null; }
    }

    // cearn_s と同じ Sav の口で、同じ列へ POP の群の台帳にも足す。〔履歴 InstrumentsCearn.cs#62〕
    internal const string CgrpHeaderText = "quarter\tyear\tnation\tregion\tgroup\tpopK\tnPops\tsavingsStart\tsavings\t"
        + "inMarketPerQuarter\tinExportPerQuarter\tinStatePerQuarter\tinCreditPerQuarter\tinFromPopsPerQuarter\tinMigrationPerQuarter\tinOtherPerQuarter\t"
        + "outMarketPerQuarter\toutTaxPerQuarter\toutBondPerQuarter\toutDuesPerQuarter\toutColonialRentPerQuarter\toutCapitalPerQuarter\toutImportPerQuarter\toutMigrationPerQuarter\toutOtherPerQuarter\t"
        + "residualPerQuarter\tlateRegisteredPops\t"
        + "monthsClosed\tpopKMonths\tsavingsMonthStartSum\tsavingsMonthEndSum\twindowNetSum\tmonthResidualSum\temptiedPopKMonths\tclaimedPopKMonths\tpollClaimPerQuarter\tincomeTaxUpperPerQuarter\t"
        + "regionSavingsGap\tregionNetFlowGap";

    internal const int CgGroups = 4;
    internal static readonly string[] CgGroupNames = ["mass", "modernElite", "closed", "soldier"];
    /// <summary>空の印の割合（月末の在庫 ≦ これ×max(月初の在庫, 請求)）。計器の物差し（係数ではない）。</summary>
    internal const double CgEmptyShare = 0.01;

    /// <summary>
    /// 〔履歴 InstrumentsCearn.cs#63〕
    /// </summary>
    internal static bool CearnOn = System.Environment.GetEnvironmentVariable("POB_CEARN") != "0" && EnvWantsAny("cearn_s", "cgrp_s", "cpop_s");   // CearnOn〔履歴 InstrumentsCearn.cs#64〕

    /// <summary>群の台帳（州×群）。流れは州の台帳と同じ列。</summary>
    internal sealed class CgAcc
    {
        public readonly double[] Flow = new double[CearnCols];
        public double Start; public int Late;
        public int Months; public long Stamp = -1;
        public double PopKMonths, MonthStart, MonthEnd, Win, Emptied, Claimed, Poll, IncTax;
        public void Clear() { System.Array.Clear(Flow); Late = 0; Months = 0; PopKMonths = MonthStart = MonthEnd = Win = Emptied = Claimed = Poll = IncTax = 0; }
    }
    /// <summary>
    /// POP の月の窓（月初の在庫・窓の中の書き込みの和）。〔履歴 InstrumentsCearn.cs#65〕
    /// </summary>
    internal sealed class CgPop
    {
        public bool InWin; public double Begin, Win;
        public double WinTax, WinEarned;   // WinEarned〔履歴 InstrumentsCearn.cs#66〕
        public readonly double[] QFlow = new double[CearnCols];
        public double QStart = double.NaN; public int QLate, QMonths;
        public double QMonthStart, QMonthEnd, QWin, QEarned, QLand, QInd, QPaid, QSub, QPoll, QInc;
        public int QClaimed, QEmptied, QPos, QEmptiedPos;
        public double QEstOut, QEstIn;   // QEstIn〔履歴 InstrumentsCearn.cs#67〕
        public double QDebtCarry; public Dictionary<(string File, int Line, SavKind Kind, char Cp), double>? QOutOther;   // QDebtCarry〔履歴 InstrumentsCearn.cs#68〕
        public readonly int[] QStuck = new int[CpStuckKinds];
        /// <summary>四半期の始まり（州の台帳と同じ時点）：POP の四半期の台帳を戻し、始まりの在庫を取る。</summary>
        public void QReset(double start)
        {
            System.Array.Clear(QFlow); System.Array.Clear(QStuck);
            QStart = start; QLate = 0; QMonths = 0;
            QMonthStart = QMonthEnd = QWin = QEarned = QLand = QInd = QPaid = QSub = QPoll = QInc = 0;
            QClaimed = QEmptied = QPos = QEmptiedPos = 0;
            QEstOut = QEstIn = 0; QDebtCarry = 0; QOutOther?.Clear();
        }
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Pop, CgPop> CgPops = new();
    static long _cgStamp;

    /// <summary>群の振り分け（純関数・旗だけを読む）：3 soldier・2 closed・1 modernElite・0 mass。</summary>
    internal static int CgGroupOf(Pop p) => p.IsSoldier ? 3 : p.Closed ? 2 : p.IsModernElite ? 1 : 0;

    /// <summary>州の群の台帳を戻し、群ごとの始まりの在庫を取り直す。</summary>
    static void CgReset(CearnAcc a, Region r)
    {
        foreach (var g in a.G) { g.Clear(); g.Start = 0; }
        foreach (var p in r.Pops) a.G[CgGroupOf(p)].Start += p.Savings.Value;
        foreach (var p in r.Pops) CgPops.GetValue(p, _ => new CgPop()).QReset(p.Savings.Value);   // GetValue〔履歴 InstrumentsCearn.cs#69〕
    }

    /// <summary>月の終わり（<see cref="MonthClose"/> の頭）：国の州の POP の月の窓を閉じ、群の台帳へ月初と月末の在庫・窓の和・請求・空の印を足す。</summary>
    internal static void CgMonthClose(World w)
    {
        if (!CearnOn) return;
        var c = w.Coef;
        double pollLevel = w.PollTax, taxRate = w.TaxRate.Value;
        long stamp = ++_cgStamp;
        foreach (var r in w.Regions)
            foreach (var p in r.Pops)
            {
                if (!CgPops.TryGetValue(p, out var cw) || !cw.InWin) continue;
                cw.InWin = false;
                if (!CearnPopAcc.TryGetValue(p, out var a)) continue;
                var g = a.G[CgGroupOf(p)];
                if (g.Stamp != stamp) { g.Stamp = stamp; g.Months++; }
                double end = p.Savings.Value;
                double poll = CgPollClaim(p, c.PollTaxRate, pollLevel);
                double inc = taxRate * System.Math.Max(0, p.LastEarnedPerMonth.Value);
                double claim = poll + inc;
                g.PopKMonths += p.Size; g.MonthStart += cw.Begin; g.MonthEnd += end; g.Win += cw.Win; g.Poll += poll; g.IncTax += inc;
                bool emptied = CgEmptied(end, cw.Begin, claim);
                if (claim > 0)
                {
                    g.Claimed += p.Size;
                    if (emptied) g.Emptied += p.Size;
                }
                // Begin〔履歴 InstrumentsCearn.cs#70〕
                cw.QMonths++; cw.QMonthStart += cw.Begin; cw.QMonthEnd += end; cw.QWin += cw.Win; cw.QPoll += poll; cw.QInc += inc;
                if (claim > 0) { cw.QClaimed++; if (emptied) cw.QEmptied++; }
                if (cw.Begin > 0 && cw.Begin >= claim)
                {
                    cw.QPos++;
                    if (emptied) cw.QEmptiedPos++;
                    int k = CpStuckKind(end, cw.Begin, cw.Win, claim, cw.WinEarned, cw.WinTax);
                    if (k >= 0) cw.QStuck[k]++;
                }
            }
    }

    // cpop_s の末尾 2 列＝POP ごとの出と入り（四半期の和）。〔履歴 InstrumentsCearn.cs#71〕
    static void EstateSav(CearnAcc a, CgPop? cw, Pop p, SavKind kind, double delta)
    {
        if (kind == SavKind.Take)
        {
            a.EstMoved -= delta;
            if (p.Size > 1e-12) a.EstOutLive -= delta;
            if (cw is not null) cw.QEstOut -= delta;
        }
        else if (kind is SavKind.Give or SavKind.GiveIncome)
        {
            a.EstIn += delta;
            if (!(p.Size > 1e-12)) a.EstInEmpty += delta;
            else { double x = delta / p.Size; if (x < a.EstMin) a.EstMin = x; if (x > a.EstMax) a.EstMax = x; }
            if (cw is not null) cw.QEstIn += delta;
        }
    }

    /// <summary>月の受けた額÷人数の広がりを四半期の最大へ畳む（<see cref="EstateDue"/> の頭と四半期の行の前）。</summary>
    static void EstateFold(CearnAcc a)
    {
        if (a.EstMax >= a.EstMin)
        {
            double d = (a.EstMax - a.EstMin) / System.Math.Max(System.Math.Abs(a.EstMax), 1e-300);
            if (d > a.EstSpread) a.EstSpread = d;
        }
        a.EstMin = double.PositiveInfinity; a.EstMax = double.NegativeInfinity;
    }

    /// <summary>EmptyPopEstate.Step の頭（旗に依らない）：その月に移るはずの額を計器が自分で数える。</summary>
    internal static void EstateDue(World w)
    {
        if (!CearnOn) return;
        foreach (var r in w.Regions)
        {
            if (!CearnAccs.TryGetValue(r, out var a)) continue;
            EstateFold(a);
            a.EstDebtBeforeSnap = EmptyDebtSum(r, out _); a.EstSettledSnap = a.EstDebtSettled;   // EstDebtBeforeSnap〔履歴 InstrumentsCearn.cs#72〕
            double e = 0, live = 0;
            foreach (var p in r.Pops)
            {
                if (p.Size > 1e-12) live += p.Size;
                else if (p.Savings.Value > 0) e += p.Savings.Value;
            }
            if (!(e > 0)) continue;
            if (live > 1e-9) a.EstDue += e; else a.EstNoHeir += e;
        }
    }

    /// <summary>
    /// 〔履歴 InstrumentsCearn.cs#73〕
    /// </summary>
    static double EmptyDebtSum(Region r, out int count)
    {
        double s = 0; count = 0;
        foreach (var p in r.Pops) if (!(p.Size > 1e-12) && p.Debt.Value != 0) { s += p.Debt.Value; count++; }
        return s;
    }

    /// <summary>
    /// 旗を読まない・エンジンの関数を呼ばない。〔履歴 InstrumentsCearn.cs#74〕
    /// </summary>
    internal static void EstateAfter(World w)
    {
        if (!CearnOn) return;
        foreach (var r in w.Regions)
        {
            if (!CearnAccs.TryGetValue(r, out var a)) continue;
            double after = EmptyDebtSum(r, out int n);
            a.EmptyDebtAfter += n;
            a.EstDebtMoved += (a.EstDebtBeforeSnap - after) - (a.EstDebtSettled - a.EstSettledSnap);
        }
    }

    /// <summary>
    /// 商人 POP と人の居ない POP は数えない。〔履歴 InstrumentsCearn.cs#75〕
    /// </summary>
    internal static void CearnBillSettle(Region r, Pop p, double back)
    {
        if (!CearnOn || !(p.Size > 1e-12) || ReferenceEquals(p, r.MerchantPop)) return;
        if (CearnAccs.TryGetValue(r, out var a)) a.HeirDebtSettled += back;
    }

    /// <summary>
    /// Bank.Carry（銀行の与信の側）は数えない。〔履歴 InstrumentsCearn.cs#76〕
    /// </summary>
    internal static void CearnCarryDebt(Pop to, double debtIn)
    {
        if (CearnOn && CgPops.TryGetValue(to, out var cw)) cw.QDebtCarry += debtIn;
    }

    /// <summary>四半期の末の断面：人数 ≦1e-12 で在庫が正の POP の在庫の和。</summary>
    static double EstateEmptySavings(Region r)
    {
        double e = 0;
        foreach (var p in r.Pops) if (!(p.Size > 1e-12) && p.Savings.Value > 0) e += p.Savings.Value;
        return e;
    }

    // 行＝四半期の末に格が Colonial の州×その州の POP（州の Pops の並び・人数 0 の POP も書く）。〔履歴 InstrumentsCearn.cs#77〕
    internal const string CpopHeaderText = "quarter\tyear\tnation\tregion\tpopIndex\tclassId\tclassName\tgroup\tpopK\tsavingsStart\tsavings\t"
        + "inMarketPerQuarter\tinExportPerQuarter\tinStatePerQuarter\tinCreditPerQuarter\tinFromPopsPerQuarter\tinMigrationPerQuarter\tinOtherPerQuarter\t"
        + "outMarketPerQuarter\toutTaxPerQuarter\toutBondPerQuarter\toutDuesPerQuarter\toutColonialRentPerQuarter\toutCapitalPerQuarter\toutImportPerQuarter\toutMigrationPerQuarter\toutOtherPerQuarter\t"
        + "residualPerQuarter\tlateRegistered\t"
        + "monthsClosed\tsavingsMonthStartSum\tsavingsMonthEndSum\twindowNetSum\tmonthResidualSum\t"
        + "earnedSettlePerQuarter\tlandRentSettlePerQuarter\tindustrialRentSettlePerQuarter\tpaidSettlePerQuarter\tsubsistenceValuePerQuarter\tpollClaimPerQuarter\tincomeTaxUpperPerQuarter\t"
        + "claimedMonths\temptiedMonths\tposMonths\temptiedPosMonths\tstuckOffPortMonths\tstuckBothZeroMonths\tstuckClaimZeroMonths\tstuckIncomeZeroMonths\tstuckUnpaidMonths\tstuckOtherMonths\t"
        + "unemploymentRate\tsubsistenceRate\tfoodFill\t"
        + "laborLines\tlaborBankruptLines\tlaborCapacity\tlaborDesiredOutput\tlaborOutput\tlaborRevenuePerMonth\tlaborZeroDesiredCapacity\t"
        + "topLaborGood\ttopLaborCapacity\ttopLaborDesiredOutput\ttopLaborOutput\ttopLaborPrice\ttopLaborMarginalCost\ttopLaborRevenuePerMonth\ttopLaborCostPerMonth\t"
        + "ownerLines\townerRevenuePerMonth\tgroupStartGap\tgroupNetFlowGap\testateOutPerQuarter\testateInPerQuarter"   // townerRevenuePerMonth〔履歴 InstrumentsCearn.cs#78〕
        + "\tdebt"   // tdebt〔履歴 InstrumentsCearn.cs#79〕
        + "\toutOtherTop1Route\toutOtherTop1PerQuarter\tdebtInCarryPerQuarter";   // toutOtherTop1PerQuarter〔履歴 InstrumentsCearn.cs#80〕

    internal const int CpStuckKinds = 6;
    /// <summary>止まった月の割合（|月末−月初| ≦ これ×月初）。計器の物差し（係数ではない）。</summary>
    internal const double CpStuckShare = 0.001;

    /// <summary>止まった月の振り分け（純関数・排他・この順）：-1 止まっていない／0 口の外（|月末−月初−窓の和| ＞ 1e-9×規模）／1 請求 0 かつ所得 0／2 請求 0／3 所得 0／4 払っていない（窓の税 0）／5 その他。
    /// 呼び手は月初の在庫が正で begin≧claim の月だけを渡す。</summary>
    internal static int CpStuckKind(double end, double begin, double win, double claim, double earned, double tax)
    {
        if (!(System.Math.Abs(end - begin) <= CpStuckShare * begin)) return -1;
        if (System.Math.Abs(end - begin - win) > 1e-9 * System.Math.Max(1, System.Math.Abs(begin) + System.Math.Abs(end) + System.Math.Abs(win))) return 0;
        if (!(claim > 0) && !(earned > 0)) return 1;
        if (!(claim > 0)) return 2;
        if (!(earned > 0)) return 3;
        if (!(tax > 0)) return 4;
        return 5;
    }

    /// <summary>四半期の末：植民地格の州の POP の行を書く（<paramref name="gStart"/>・<paramref name="gNet"/>＝群の台帳の始まりの在庫と流れの差＝POP の和の確かめ）。</summary>
    static void CpopRows(CearnSink sk, ulong seed, int q, World w, string nation, Region r, double[] gStart, double[] gNet)
    {
        if (!Wants("cpop_s")) return;   // cpop_s〔履歴 InstrumentsCearn.cs#81〕
        sk.P ??= Open("cpop", seed);
        if (!sk.PHeader) { sk.P.WriteLine(CpopHeaderText); sk.PHeader = true; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int n = r.Pops.Count;
        var cws = new CgPop?[n];
        var net = new double[n];
        var sumStart = new double[CgGroups]; var sumNet = new double[CgGroups];
        for (int i = 0; i < n; i++)
        {
            var p = r.Pops[i];
            CgPops.TryGetValue(p, out cws[i]);
            double inS = 0, outS = 0;
            if (cws[i] is { } cw0) { for (int k = 0; k < CearnInCols; k++) inS += cw0.QFlow[k]; for (int k = CearnInCols; k < CearnCols; k++) outS += cw0.QFlow[k]; }
            net[i] = inS - outS;
            int gi = CgGroupOf(p);
            sumStart[gi] += cws[i]?.QStart ?? double.NaN; sumNet[gi] += net[i];
        }
        // 賃金の受け手と地代の受け手の行（州の生産者を 1 周）
        var lLines = new int[n]; var lBank = new int[n]; var lCap = new double[n]; var lDes = new double[n]; var lOut = new double[n]; var lRev = new double[n]; var lZero = new double[n];
        var top = new Producer?[n]; var oLines = new int[n]; var oRev = new double[n];
        foreach (var s in r.Producers)
        {
            int li = s.LaborPopIndex, oi = s.OwnerPopIndex;
            if (li >= 0 && li < n)
            {
                lLines[li]++;
                if (s.Bankrupt) lBank[li]++;
                else
                {
                    lCap[li] += s.Capacity; lDes[li] += s.DesiredOutput; lOut[li] += s.Output; lRev[li] += s.LastRevenue;
                    if (s.Capacity > 0 && !(s.DesiredOutput > 0)) lZero[li] += s.Capacity;
                    if (top[li] is null || s.Capacity > top[li]!.Capacity) top[li] = s;
                }
            }
            if (s.LandShares is { } lsc)   // LandShares〔履歴 InstrumentsCearn.cs#82〕
            {
                long fbc = 0;
                for (int k = 0, nk = LsSlots(w, lsc); k < nk; k++)
                {
                    if (!(lsc[k] > 0)) continue;
                    int ri = LsRecipient(r, s, k, ref fbc);
                    bool seen = false;
                    for (int j = 0; j < k; j++) if (lsc[j] > 0 && LsRecipient(r, s, j, ref fbc) == ri) { seen = true; break; }
                    if (seen || ri == li || ri < 0 || ri >= n) continue;
                    double wr = 0;
                    for (int j = k; j < nk; j++) if (lsc[j] > 0 && LsRecipient(r, s, j, ref fbc) == ri) wr += lsc[j];
                    oLines[ri]++; oRev[ri] += s.LastRevenue * wr;
                }
            }
            else if (oi >= 0 && oi < n && oi != li) { oLines[oi]++; oRev[oi] += s.LastRevenue; }
        }
        int ng = System.Math.Min(r.Markets.Length, Catalog.Goods.Length);
        for (int i = 0; i < n; i++)
        {
            var p = r.Pops[i]; var cw = cws[i]; int gi = CgGroupOf(p);
            var cols = new List<string>(80) { I(q), I(w.Year), nation, r.Name, I(i), I(p.ClassId), p.ClassName, CgGroupNames[gi], F(p.Size), F(cw?.QStart ?? double.NaN), F(p.Savings.Value) };
            for (int k = 0; k < CearnCols; k++) cols.Add(F(cw?.QFlow[k] ?? 0));
            cols.Add(F(p.Savings.Value - (cw?.QStart ?? double.NaN) - net[i])); cols.Add(I(cw?.QLate ?? 0));
            cols.Add(I(cw?.QMonths ?? 0)); cols.Add(F(cw?.QMonthStart ?? 0)); cols.Add(F(cw?.QMonthEnd ?? 0)); cols.Add(F(cw?.QWin ?? 0));
            cols.Add(F(cw is null ? 0 : cw.QMonthEnd - cw.QMonthStart - cw.QWin));
            cols.Add(F(cw?.QEarned ?? 0)); cols.Add(F(cw?.QLand ?? 0)); cols.Add(F(cw?.QInd ?? 0)); cols.Add(F(cw?.QPaid ?? 0)); cols.Add(F(cw?.QSub ?? 0)); cols.Add(F(cw?.QPoll ?? 0)); cols.Add(F(cw?.QInc ?? 0));
            cols.Add(I(cw?.QClaimed ?? 0)); cols.Add(I(cw?.QEmptied ?? 0)); cols.Add(I(cw?.QPos ?? 0)); cols.Add(I(cw?.QEmptiedPos ?? 0));
            for (int k = 0; k < CpStuckKinds; k++) cols.Add(I(cw?.QStuck[k] ?? 0));
            cols.Add(F(p.UnemploymentRate.Value)); cols.Add(F(p.SubsistenceBasisKind == SubsistenceBasisKind.Productivity ? p.SubsistenceShare : p.SubsistenceBasis)); cols.Add(F(p.FoodFill));
            cols.Add(I(lLines[i])); cols.Add(I(lBank[i])); cols.Add(F(lCap[i])); cols.Add(F(lDes[i])); cols.Add(F(lOut[i])); cols.Add(F(lRev[i])); cols.Add(F(lZero[i]));
            if (top[i] is { } t)
            {
                cols.Add(t.GoodId >= 0 && t.GoodId < Catalog.Goods.Length ? Catalog.Goods[t.GoodId].Name : "-");
                cols.Add(F(t.Capacity)); cols.Add(F(t.DesiredOutput)); cols.Add(F(t.Output));
                cols.Add(F(t.GoodId >= 0 && t.GoodId < ng ? r.Markets[t.GoodId].Price : double.NaN)); cols.Add(F(t.MarginalCost)); cols.Add(F(t.LastRevenue)); cols.Add(F(t.LastCost));
            }
            else { cols.Add("-"); for (int k = 0; k < 7; k++) cols.Add("nan"); }
            cols.Add(I(oLines[i])); cols.Add(F(oRev[i]));
            cols.Add(F(sumStart[gi] - gStart[gi])); cols.Add(F(sumNet[gi] - gNet[gi]));
            cols.Add(F(cw?.QEstOut ?? 0)); cols.Add(F(cw?.QEstIn ?? 0));   // Add〔履歴 InstrumentsCearn.cs#83〕
            cols.Add(F(p.Debt.Value));   // Add〔履歴 InstrumentsCearn.cs#84〕
            string oot = "-"; double oov = double.NaN; if (cw?.QOutOther is { } oqd) oot = CearnTop(oqd, 0, out oov);
            cols.Add(oot); cols.Add(F(oov)); cols.Add(F(cw?.QDebtCarry ?? 0));   // Add〔履歴 InstrumentsCearn.cs#85〕
            sk.P.WriteLine(string.Join('\t', cols));
        }
    }

    /// <summary>人頭税の請求（純関数・計器が自分で書く式）：段が正で、閉鎖階級でも兵士でも犯罪者でもない POP の 率×段×人数。</summary>
    internal static double CgPollClaim(Pop p, double rate, double level) => level > 1e-9 && !p.Closed && !p.IsSoldier && p.ClassId != PopClass.Criminal ? rate * level * p.Size : 0;

    /// <summary>空の印（純関数）：請求が正で、月末の在庫 ≦ <see cref="CgEmptyShare"/>×max(月初の在庫, 請求)。</summary>
    internal static bool CgEmptied(double end, double begin, double claim) => claim > 0 && end <= CgEmptyShare * System.Math.Max(begin, claim);

    /// <summary>四半期の末：植民地格の州の群の行を書く（<paramref name="regionSav"/>・<paramref name="regionNet"/>＝州の行の在庫と流れの差＝群の和の確かめ）。</summary>
    static void CgRows(CearnSink sk, ulong seed, int q, World w, string nation, Region r, CearnAcc a, double regionSav, double regionNet)
    {
        sk.G ??= Open("cgrp", seed);
        if (!sk.GHeader) { sk.G.WriteLine(CgrpHeaderText); sk.GHeader = true; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var pk = new double[CgGroups]; var sv = new double[CgGroups]; var np = new int[CgGroups];
        foreach (var p in r.Pops) { int gi = CgGroupOf(p); pk[gi] += p.Size; sv[gi] += p.Savings.Value; np[gi]++; }
        var net = new double[CgGroups];
        double savSum = 0, netSum = 0;
        for (int gi = 0; gi < CgGroups; gi++)
        {
            var g = a.G[gi];
            double inS = 0, outS = 0;
            for (int k = 0; k < CearnInCols; k++) inS += g.Flow[k];
            for (int k = CearnInCols; k < CearnCols; k++) outS += g.Flow[k];
            net[gi] = inS - outS; savSum += sv[gi]; netSum += net[gi];
        }
        double gapSav = savSum - regionSav, gapNet = netSum - regionNet;
        for (int gi = 0; gi < CgGroups; gi++)
        {
            var g = a.G[gi];
            bool any = np[gi] > 0 || g.Start != 0 || g.PopKMonths != 0 || g.Late != 0;
            foreach (double v in g.Flow) if (v != 0) { any = true; break; }
            if (!any) continue;
            // cearn_s と同じに始まりと残差は nan で名乗る（行の数は変えない）。〔履歴 InstrumentsCearn.cs#86〕
            bool started = a.Started;
            var cols = new List<string>(48) { I(q), I(w.Year), nation, r.Name, CgGroupNames[gi], F(pk[gi]), I(np[gi]), F(started ? g.Start : double.NaN), F(sv[gi]) };
            foreach (double v in g.Flow) cols.Add(F(v));
            cols.Add(F(started ? sv[gi] - g.Start - net[gi] : double.NaN)); cols.Add(I(g.Late));
            cols.Add(I(g.Months)); cols.Add(F(g.PopKMonths)); cols.Add(F(g.MonthStart)); cols.Add(F(g.MonthEnd)); cols.Add(F(g.Win)); cols.Add(F(g.MonthEnd - g.MonthStart - g.Win));
            cols.Add(F(g.Emptied)); cols.Add(F(g.Claimed)); cols.Add(F(g.Poll)); cols.Add(F(g.IncTax));
            cols.Add(F(gapSav)); cols.Add(F(gapNet));
            sk.G.WriteLine(string.Join('\t', cols));
        }
        var gStart = new double[CgGroups];
        for (int gi = 0; gi < CgGroups; gi++) gStart[gi] = a.G[gi].Start;
        CpopRows(sk, seed, q, w, nation, r, gStart, net);   // CpopRows〔履歴 InstrumentsCearn.cs#87〕
    }
}
