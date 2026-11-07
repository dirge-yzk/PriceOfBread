using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (822) 植民地の稼ぎの表 cearn_s（読むだけ・恒等・事前登録 measurements/m822_20261104_prereg_ce.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCpwd.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（計器の欄は ConditionalWeakTable と ThreadStatic だけ）。
// 目的＝(814) の凍結（植民地の州のお金が月 1.5〜2.3% で縮み、工場と採取の産出が 0 で止まる）の出口を、州の POP の貯蓄の台帳から読む。
// 行＝全ての国の全ての州×四半期（region_s と同じ母集合）。額は州の持ち主の国の通貨・四半期の和（…PerQuarter）。助走の月は数えない（Phase の warm で捨てる）。
// 台帳の出所＝貯蓄の書き手の 1 本の計器の口 Sav（Pop の中の書き手がそこを呼ぶ）。POP の州は計器が自分で持つ登録（月初の Begin で全ての国の全ての州の POP）。
//   列は書き手の種類・相手の側（P＝POP・T＝国庫・B／M／F／C／H＝世界の外）・国庫の区分（World.Transfer の頭で写す）・呼び手のファイル名で決める（行番号は列の鍵にしない）。
//   POP→POP は払い手の州で流出・受け手の州で流入。州の中の運搬（社会移動・徴兵・州の中の移動）は同じ州の出と入りが打ち消す。
// 流入 7 列：inMarket（月末の精算の所得の側）・inExport（世界の外〔B・M・F〕から・CapitalMarket.cs 以外）・inState（国庫から）・inCredit（与信 C・H から）・inFromPops（他の POP から）・
//   inMigration（運搬の受け取り）・inOther（CapitalMarket.cs の世界の外から・換算の正・どれでもない受け取り）。
// 流出 9 列（正の額）：outMarket（精算の支払いの側＝所得の側−精算の増分）・outTax（国庫へ・区分 Tax）・outBond（国庫へ・区分 Borrow）・outDues（Fiscal.cs の POP→POP＝十分の一税と仲介者の取り分）・
//   outColonialRent（Colonial.cs の POP→POP＝植民地の取り分）・outCapital（CapitalMarket.cs の POP→世界の外＝外資の配当・資本の送り出し・逃避）・outImport（BilateralMarket.cs の POP→世界の外＝輸入の代金と運賃）・
//   outMigration（運搬の送り出し）・outOther（それ以外）。
// 閉じ：residual＝savings−savingsStart−(Σ流入−Σ流出)。経路の上位＝精算と換算を除く書き込みの鍵（ファイル:行:種類:相手の側）の和の上位（行番号は読み手が測った木で名前へ引く）。
// 値段と床・凍結・人頭税の物差しの列は四半期の末の断面（州の生産者・市場・POP を計器が自分で読む・エンジンの関数を呼ばない・自己費用は読み手が MarginalCost×costPriceLevel×localPriceMultiplier で組む）。
// ⚠限界（計器が自分で申告する）：①登録は月初＝月の途中で作られた POP の書き込みは次の月まで台帳の外（残差と lateRegisteredPops に出る）②並列の国の日次の中で別の国の POP へ払う書き込みは無い前提（flow_s と同じ）
//   ③精算の所得と支払いは州の中の市場の循環を含む（州の外への純の払いは outMarket−inMarket）④国庫の区分は World.Transfer の頭で写した値（運搬の書き込みは区分を読まない）
//   ⑤格・乗数・値段は四半期の末の値（savingsPerCapitaOverNation＝全ての州の 1 人あたりで割る〔事前登録の定義〕・savingsPerCapitaOverMainland＝州の乗数の分母と同じ本土の 1 人あたりで割る〔腕の前の追記〕）⑥国家の市場を通る出入り（stateMarket*）は POP の台帳の外（StateSpent の月の和）⑦鉱脈の品位の器 MineralCostFollowsGrade（既定 0）は自己費用に掛けない。
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
        + "emptyPopSavings\testateDuePerQuarter\testateNoHeirPerQuarter\testateMovedPerQuarter\testateInPerQuarter\testateOutFromLivePerQuarter\testateInToEmptyPerQuarter\testateRecvPerCapitaSpread"   // (848) 末尾 8 列＝相続 R1
        + "\timportPayerKind\timportBudgetStartPerQuarter\timportPaidPerQuarter\timportCrossRegionPerQuarter"   // (863) 末尾 4 列＝輸入の払い手（CearnImportHead）
        + "\timportOldRuleBudgetStartPerQuarter\timportBudgetExcessMaxPerClearing"   // (868) 末尾 2 列＝旧の規則の予算（旗に依らない）・清算ごとの超過の最大
        + "\timportBudgetExcessMaxPerClearingSub\timportPaidPayerSizeLe1e12PerQuarter\timportPaidPayerSizeMidPerQuarter\timportPaidPayerSizeGt1e9PerQuarter"   // (874) 末尾 4 列＝超過の最大（補助は TreasuryCat.Subsidy だけ足す）・払った額を払いの時点の払い手の人数（≦1e-12／1e-12 超〜1e-9／1e-9 超）で分けた和
        + "\timportBudgetExcessMaxPerClearingSubLive"   // (880) 末尾 1 列＝Sub と同じ超過の最大を、人数 ≦1e-12 の払い手の 0 でない払いが無かった清算だけで畳む（旗 0 の人の居ない商人の手形の清算を外す）
        + "\tmerchantDebt\testateDebtSettledPerQuarter"   // (910) 末尾 2 列＝四半期の末の商人の POP の債務（添字なし＝NaN）・相続の前の返しの段で貯蓄から世界の外へ移った額（台帳・旗を読まない）
        + "\tbillBlockedClearings\tbillBlockedDustClearings\temptyDebtAfterEstate\testateDebtMovedPerQuarter\theirDebtSettledPerQuarter";   // (920) 末尾 5 列＝手形を振り出せなかった清算の数・その中の塵・相続の段の直後の人の居ない債務者の数・相続の段で返し以外で債務が移った額・人の居る非商人 POP が決済 ① で返した額

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
        public double EstDue, EstNoHeir, EstMoved, EstIn, EstOutLive, EstInEmpty, EstSpread, EstMin = double.PositiveInfinity, EstMax = double.NegativeInfinity;   // (848) 相続 R1 の列
        public int ImpKind; public double ImpBudget, ImpPaid, ImpCross;   // (863) 輸入の払い手の列（CearnImportHead・CearnImportPaid）
        public double ImpOldBudget, ImpExMax = double.NegativeInfinity, ImpHeadStock = double.NaN, ImpPaidClr; public bool ImpHeadOn;   // (868) 旧の規則の予算・清算ごとの超過の最大（未定義は -∞）・今の清算の頭の在庫（NaN＝手形）・今の清算の払い・頭があるか
        public double ImpHeadStockSub = double.NaN, ImpExMaxSub = double.NegativeInfinity; public readonly double[] ImpPaidBand = new double[3];   // (874) 補助を Subsidy だけに絞った頭の在庫と超過の最大・払った額の人数の帯別の和
        public int BillBlocked, BillBlockedDust, EmptyDebtAfter;   // (920) 手形を振り出せなかった清算の数・その中の塵（0＜債務≦1e-12）・相続の段の直後に人数 ≦1e-12 で債務 ≠0 の POP の数（月ごとの和）
        public double EstDebtMoved, HeirDebtSettled, EstDebtBeforeSnap, EstSettledSnap;   // (920) 相続の段で人の居ない POP の債務が返し以外で減った額・人の居る非商人 POP が決済 ① で返した額・相続の段の頭の断面（人の居ない POP の債務の和・返した額の累計）
        public double EstDebtSettled;   // (910) 相続の前の返しの段で移った額（四半期の和）
        public double ImpExMaxSubLive = double.NegativeInfinity; public bool ImpClrDead;   // (880) 人数 ≦1e-12 の払い手の払いが無かった清算だけの超過の最大・今の清算に人数 ≦1e-12 の払い手の 0 でない払いがあったか
        public readonly Dictionary<(string File, int Line, SavKind Kind, char Cp), double> Out = new(), In = new();
        public readonly CgAcc[] G = [new(), new(), new(), new()];   // (829) 階級の群の台帳（群の番号＝CgGroupOf）
        public void Clear() { System.Array.Clear(Flow); Late = 0; StateSpend = 0; StateTake = 0; Out.Clear(); In.Clear(); EstDue = EstNoHeir = EstMoved = EstIn = EstOutLive = EstInEmpty = EstSpread = 0; EstMin = double.PositiveInfinity; EstMax = double.NegativeInfinity; ImpKind = 0; ImpBudget = ImpPaid = ImpCross = 0; ImpOldBudget = 0; ImpExMax = double.NegativeInfinity; ImpHeadStock = double.NaN; ImpPaidClr = 0; ImpHeadOn = false; ImpHeadStockSub = double.NaN; ImpExMaxSub = double.NegativeInfinity; System.Array.Clear(ImpPaidBand); ImpExMaxSubLive = double.NegativeInfinity; ImpClrDead = false; EstDebtSettled = 0; BillBlocked = BillBlockedDust = EmptyDebtAfter = 0; EstDebtMoved = HeirDebtSettled = 0; }
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, CearnAcc> CearnAccs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Pop, CearnAcc> CearnPopAcc = new();
    sealed class CearnSink { public System.IO.StreamWriter? W, G, P; public bool Header, GHeader, PHeader; }   // G＝(829) cgrp_s・P＝(836) cpop_s
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
        if (!CearnOn) return;   // (829) 環境変数の門（POB_CEARN=0）
        if (kind is SavKind.RegionOut or SavKind.RegionIn) return;   // 州の移し替えの記帳（お金は動かない）
        if (!CearnPopAcc.TryGetValue(p, out var a)) return;
        var ga = a.G[CgGroupOf(p)];   // (829) 同じ書き込みを POP の群の台帳にも足す
        CgPops.TryGetValue(p, out var cw);
        if (cw is not null && cw.InWin) cw.Win += delta;   // (829) 月の窓の中の書き込みの和
        if (kind == SavKind.Settle)
        {
            double earned = Calendar.OneMonthOf(p.EarnedPerMonth).Value;   // 精算の所得の側（同じ月の値・精算の書き込みの直後に読む）
            a.Flow[(int)CearnCol.InMarket] += earned;
            a.Flow[(int)CearnCol.OutMarket] += earned - delta;             // 支払いの側＝所得−増分（精算の 1 行に閉じる）
            ga.Flow[(int)CearnCol.InMarket] += earned;
            ga.Flow[(int)CearnCol.OutMarket] += earned - delta;
            if (cw is not null)   // (836) POP の四半期の台帳と精算の内訳（同じ月の値）
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
        if (kind is SavKind.Give or SavKind.GiveIncome && cp == 'T' && CearnFileName(file) == "BilateralMarket.cs")   // (868) 国庫から払い手への入り（清算の頭の後・払いの前）
        {
            CearnImportSubsidyInto(a, delta);
            if (_cearnCat == TreasuryCat.Subsidy) CearnImportSubsidySubInto(a, delta);   // (874) 売買方針の補助だけ（輸出の関税の戻し〔TreasuryCat.Tariff・払いの後〕は足さない）
        }
        if (kind == SavKind.Take && CearnFileName(file) == "BilateralMarket.cs") CearnImportPaid(a, -delta, p.Size);   // (863) 輸入の払い（関税・代金・運賃・港湾料・保険料＝清算の払いの段）
        if (_estRepay && kind == SavKind.Take) a.EstDebtSettled -= delta;   // (910) 相続の前の返しの段の中の貯蓄の出（Bank.Lend の負の側＝世界の外へ・台帳の書き込み＝移った額）
        if (col == CearnCol.Skip) return;
        bool isIn = (int)col < CearnInCols;
        a.Flow[(int)col] += isIn ? delta : -delta;
        ga.Flow[(int)col] += isIn ? delta : -delta;
        if (cw is not null)   // (836) POP の四半期の台帳（同じ列）と窓の中の税
        {
            cw.QFlow[(int)col] += isIn ? delta : -delta;
            if (col == CearnCol.OutOther) { var oq = cw.QOutOther ??= new(); var ok = (routed ? file : "", routed ? _routeLine : 0, kind, cp); oq.TryGetValue(ok, out double ov); oq[ok] = ov - delta; }   // (920) outOther の経路別（cpop_s の outOtherTop1）
            if (col == CearnCol.OutTax && cw.InWin) cw.WinTax -= delta;
        }
        if (routed && CearnFileName(file) == "EmptyPopEstate.cs") EstateSav(a, cw, p, kind, delta);   // (848) 相続 R1 の列（実際に移った額＝台帳の書き込み）
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
        if (!CearnOn) return;   // (829) 環境変数の門
        foreach (var r in w.Regions)
        {
            var a = CearnAccs.GetValue(r, _ => new CearnAcc());
            bool fresh = !a.Started;
            if (fresh) { a.Start = CearnSavings(r); a.Started = true; CgReset(a, r); }
            foreach (var p in r.Pops)
            {
                var cw = CgPops.GetValue(p, _ => new CgPop());   // (829) 月の窓の始まり＝月初の在庫を写す
                cw.InWin = true; cw.Begin = p.Savings.Value; cw.Win = 0;
                cw.WinTax = 0; cw.WinEarned = 0;   // (836)
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

    // ---------------------------------------------------------------- (863) 輸入の払い手の列（cearn_s の末尾 4 列・読むだけ・恒等・事前登録 measurements/m863_20261105_prereg_ip.txt [2][4]） ----------------------------------------------------------------
    // 清算の頭（BilateralMarket の払い手の選びの直後）に州だけを受け取り、払い手と予算の局所変数は受け取らない＝州の状態と予算は計器が自分で読む（旗を読まない）。
    //   importPayerKind＝四半期の最後の清算の頭の州の状態（0 清算の頭なし・1 商人の POP が人数 ＞1e-12・2 商人の POP の人数 ≦1e-12・3 商人の添字なし）。
    //   importBudgetStartPerQuarter＝清算の頭ごとの予算の和（商人が人数 ＞1e-12 なら、債務 ＞0 でその商人の max(0, 貯蓄)・それ以外〔債務 0・NaN〕は ∞／それ以外の州は人数 ＞1e-12 の POP の Σmax(0, 貯蓄)）。
    //   importPaidPerQuarter＝その州の POP が BilateralMarket.cs の移し方で払った額（台帳の書き込み＝実際に移った額）。importCrossRegionPerQuarter＝払いの段の行の州と別の州に登録された POP がその行で払った額。
    // ⚠限界：①登録は月初＝月の途中に作られた POP の払いは数えない（cearn_s の限界①）②州の状態は清算の頭の 1 時点 ③cpop_s の U1 は植民地格の州だけ（cpop_s の母集合）。
    [System.ThreadStatic] static Region? _impRegion;
    internal static void CearnImportHead(Region r)
    {
        if (!CearnOn) return;
        CearnImportHeadInto(CearnAccs.GetValue(r, _ => new CearnAcc()), r);
    }
    /// <summary>(868) 頭の本体（試験の口）。ImpOldBudget＝旧の規則の予算（旗に依らない）：商人の POP があればその 1 POP〔債務 ＞0 なら max(0, 貯蓄)・債務が 0 や NaN なら手形＝+∞〕／商人の添字が無い州は全 POP の Σmax(0, 貯蓄)。
    /// 清算ごとの超過：頭で前の清算の〈払った額−頭の在庫〉を畳んでから今の清算を始める（手形〔在庫 ∞〕の清算は畳まない）。旗 0 の kind 2 の州では実際の払い手と計器の在庫が別（旗を読まない限界）。</summary>
    internal static void CearnImportHeadInto(CearnAcc a, Region r)
    {
        int mi = r.MerchantPopIndex; Pop? m = mi >= 0 && mi < r.Pops.Count ? r.Pops[mi] : null;
        double b = 0, ob = 0;
        if (m is not null && m.Size > 1e-12) { a.ImpKind = 1; b = m.Debt.Value > 0 ? System.Math.Max(0, m.Savings.Value) : double.PositiveInfinity; }
        else { a.ImpKind = m is null ? 3 : 2; foreach (var p in r.Pops) if (p.Size > 1e-12) b += System.Math.Max(0, p.Savings.Value); }
        if (m is not null) ob = m.Debt.Value > 0 ? System.Math.Max(0, m.Savings.Value) : double.PositiveInfinity;
        else foreach (var p in r.Pops) ob += System.Math.Max(0, p.Savings.Value);
        a.ImpBudget += b; a.ImpOldBudget += ob;
        if (m is not null && m.Debt.Value > 0) { a.BillBlocked++; if (!(m.Debt.Value > 1e-12)) a.BillBlockedDust++; }   // (920) 手形を振り出せない（決済の後も商人の債務が正＝:580 の門が偽・旗を読まない）清算の頭・その中の塵
        CearnImportFoldInto(a);
        a.ImpHeadOn = true; a.ImpHeadStock = a.ImpHeadStockSub = double.IsPositiveInfinity(b) ? double.NaN : b; a.ImpPaidClr = 0;
        a.ImpClrDead = false;   // (880) 今の清算の印を戻す
    }
    /// <summary>(868) 今の清算の超過〈払った額−頭の在庫〉を最大に畳んで清算を閉じる（頭が無い・手形の清算は何もしない）。</summary>
    internal static void CearnImportFoldInto(CearnAcc a)
    {
        if (!a.ImpHeadOn) return;
        a.ImpHeadOn = false;
        if (double.IsNaN(a.ImpHeadStock)) return;
        double ex = a.ImpPaidClr - a.ImpHeadStock;
        if (ex > a.ImpExMax) a.ImpExMax = ex;
        double exSub = a.ImpPaidClr - a.ImpHeadStockSub;   // (874) 補助を Subsidy だけに絞った超過（2 つの在庫は同じ頭から作る＝NaN も同じ清算）
        if (exSub > a.ImpExMaxSub) a.ImpExMaxSub = exSub;
        if (!a.ImpClrDead && exSub > a.ImpExMaxSubLive) a.ImpExMaxSubLive = exSub;   // (880) 人数 ≦1e-12 の払い手の払いが無かった清算だけ
    }
    /// <summary>(868) 清算の頭の後に国庫から払い手へ入った補助を、清算内の在庫に足す（手形の清算は足さない）。</summary>
    internal static void CearnImportSubsidyInto(CearnAcc a, double amount) { if (a.ImpHeadOn && !double.IsNaN(a.ImpHeadStock)) a.ImpHeadStock += amount; }
    /// <summary>(874) 清算の頭の後に国庫から払い手へ入った額のうち、売買方針の補助（TreasuryCat.Subsidy）だけを、絞った在庫に足す（手形の清算は足さない）。</summary>
    internal static void CearnImportSubsidySubInto(CearnAcc a, double amount) { if (a.ImpHeadOn && !double.IsNaN(a.ImpHeadStockSub)) a.ImpHeadStockSub += amount; }
    /// <summary>(874) 払いを払いの時点の払い手の人数で帯に分ける（≦1e-12／1e-12 超〜1e-9／1e-9 超）。試験の口。</summary>
    internal static void CearnImportPaidBandInto(CearnAcc a, double amount, double payerSize) { a.ImpPaidBand[payerSize <= 1e-12 ? 0 : payerSize <= 1e-9 ? 1 : 2] += amount; }
    /// <summary>払いの段の行の州を置く（null で外す）。</summary>
    internal static void CearnImportPayRegion(Region? r) { if (CearnOn) _impRegion = r; }
    /// <summary>(910) EmptyPopEstate.Step の返しの段の前後（true＝入る・false＝出る）：その間の貯蓄の出を estateDebtSettledPerQuarter へ畳む文脈の印（旗を読まない）。</summary>
    internal static void EstateRepay(bool on) { _estRepay = on && CearnOn; }
    [System.ThreadStatic] static bool _estRepay;
    internal static void CearnImportPaidInto(CearnAcc a, double amount) { a.ImpPaidClr += amount; }   // (868) 今の清算の払い（試験の口）
    /// <summary>(880) 払いの時点の払い手の人数が ≦1e-12（帯の列と同じ述語）で額が 0 でなければ、今の清算に印を付ける（その清算は importBudgetExcessMaxPerClearingSubLive に畳まない）。試験の口。</summary>
    internal static void CearnImportPaidDeadInto(CearnAcc a, double amount, double payerSize) { if (payerSize <= 1e-12 && amount != 0) a.ImpClrDead = true; }
    static void CearnImportPaid(CearnAcc a, double amount, double payerSize)
    {
        a.ImpPaid += amount; CearnImportPaidInto(a, amount); CearnImportPaidBandInto(a, amount, payerSize); CearnImportPaidDeadInto(a, amount, payerSize);
        if (_impRegion is { } ir && CearnAccs.TryGetValue(ir, out var ra) && !ReferenceEquals(ra, a)) a.ImpCross += amount;
    }

    /// <summary>助走の直後（Phase warm）：助走の月の流れを捨て、始まりの在庫を取り直す。</summary>
    static void CearnWarm(MultiNationSim sim)
    {
        if (!CearnOn) return;   // (829) 環境変数の門
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
        if (!CearnOn) return;   // (829) 環境変数の門（POB_CEARN=0 で cearn_s と cgrp_s を書かない）
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
                EstateFold(a);   // (848) 相続 R1 の末尾 8 列
                cols.Add(F(EstateEmptySavings(r))); cols.Add(F(a.EstDue)); cols.Add(F(a.EstNoHeir)); cols.Add(F(a.EstMoved)); cols.Add(F(a.EstIn)); cols.Add(F(a.EstOutLive)); cols.Add(F(a.EstInEmpty)); cols.Add(F(a.EstSpread));
                CearnImportFoldInto(a);   // (868) 行を書く前に最後の清算を畳む
                cols.Add(I(a.ImpKind)); cols.Add(F(a.ImpBudget)); cols.Add(F(a.ImpPaid)); cols.Add(F(a.ImpCross));   // (863)
                cols.Add(F(a.ImpOldBudget)); cols.Add(F(double.IsNegativeInfinity(a.ImpExMax) ? double.NaN : a.ImpExMax));   // (868)
                cols.Add(F(double.IsNegativeInfinity(a.ImpExMaxSub) ? double.NaN : a.ImpExMaxSub)); cols.Add(F(a.ImpPaidBand[0])); cols.Add(F(a.ImpPaidBand[1])); cols.Add(F(a.ImpPaidBand[2]));   // (874)
                cols.Add(F(double.IsNegativeInfinity(a.ImpExMaxSubLive) ? double.NaN : a.ImpExMaxSubLive));   // (880)
                cols.Add(F(r.MerchantPop is { } mpd ? mpd.Debt.Value : double.NaN)); cols.Add(F(a.EstDebtSettled));   // (910)
                cols.Add(I(a.BillBlocked)); cols.Add(I(a.BillBlockedDust)); cols.Add(I(a.EmptyDebtAfter)); cols.Add(F(a.EstDebtMoved)); cols.Add(F(a.HeirDebtSettled));   // (920)
                sk.W.WriteLine(string.Join('\t', cols));
                if (r.Tier == RegionTier.Colonial) CgRows(sk, seed, q, w, nation, r, a, sn.Savings, inSum - outSum);   // (829) 植民地格の州だけ群の行
                a.Clear(); a.Start = sn.Savings; a.Started = true; CgReset(a, r);
            }
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void CearnClose(MultiNationSim sim)
    {
        if (CearnSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; sk.G?.Dispose(); sk.G = null; sk.P?.Dispose(); sk.P = null; }
    }

    // ---------------------------------------------------------------- (829) 植民地格の州の階級の群の表 cgrp_s（読むだけ・恒等・事前登録 measurements/m829_20261104_prereg_cg.txt） ----------------------------------------------------------------
    // 目的＝仮説〈定額の請求（人頭税）が払う群の貯蓄を毎月空にする〉を、凍結の州の群ごとの台帳で読む（(822) の次の推奨）。cearn_s と同じ Sav の口で、同じ列へ POP の群の台帳にも足す。
    // 行＝四半期の末に格が Colonial の州×群（0 mass 平民の大衆＝!Closed・!IsSoldier・!IsModernElite〔犯罪者を含む〕・1 modernElite・2 closed〔Closed・!IsSoldier〕・3 soldier〔IsSoldier〕）。
    //   述語は計器の中で旗から書く（エンジンの関数を呼ばない）。人数・在庫・流れ・月の和が全て 0 の群の行は書かない。
    // 月の窓＝月初（Begin＝CearnRegister）に POP ごとに在庫を写し、月の終わり（MonthClose の頭＝Fiscal.Step の徴税の後）に月末の在庫を読む。窓の中の書き込みの和を POP ごとに持つ（閉じ＝monthResidualSum）。
    // 請求は計器が自分で書く式：人頭税＝PollTaxRate×段×Size（段が正・!Closed・!IsSoldier・犯罪者でない）・所得税の上界＝TaxRate×max(0, LastEarnedPerMonth)（身分の免税と課税への同意を掛けない）。
    // 空の印（POP×月）＝請求が正で、月末の在庫 ≦ CgEmptyShare×max(月初の在庫, 請求)。
    // 環境変数の門 POB_CEARN=0 で cearn_s と cgrp_s を書かない（既定＝書く＝基準の表は変わらない）。
    // ⚠限界（計器が自分で申告する）：①登録は月初＝月の途中で作られた POP は次の月まで群の台帳の外（lateRegisteredPops と残差に出る）②月の窓は月初に登録した POP だけ
    //   ③所得税は上界＝人頭税の割合は下界 ④国の段と率は月の終わりの値 ⑤州が月の途中で別の国へ移った月の窓は数えない ⑥月初の在庫は〈月初の予算〉の代わり（EconomyStep が RemainingBudget に写すのは Begin の後）。
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

    /// <summary>環境変数の門：POB_CEARN=0 で cearn_s と cgrp_s と cpop_s〔(836)〕を書かない（試験が切り替える）。</summary>
    internal static bool CearnOn = System.Environment.GetEnvironmentVariable("POB_CEARN") != "0";

    /// <summary>群の台帳（州×群）。流れは州の台帳と同じ列。</summary>
    internal sealed class CgAcc
    {
        public readonly double[] Flow = new double[CearnCols];
        public double Start; public int Late;
        public int Months; public long Stamp = -1;
        public double PopKMonths, MonthStart, MonthEnd, Win, Emptied, Claimed, Poll, IncTax;
        public void Clear() { System.Array.Clear(Flow); Late = 0; Months = 0; PopKMonths = MonthStart = MonthEnd = Win = Emptied = Claimed = Poll = IncTax = 0; }
    }
    /// <summary>POP の月の窓（月初の在庫・窓の中の書き込みの和）。(836) POP の四半期の台帳（Q…・cpop_s）と窓の中の税と精算の所得（止まった月の振り分け）も持つ。</summary>
    internal sealed class CgPop
    {
        public bool InWin; public double Begin, Win;
        public double WinTax, WinEarned;   // (836) 窓の中の outTax と精算の所得
        public readonly double[] QFlow = new double[CearnCols];
        public double QStart = double.NaN; public int QLate, QMonths;
        public double QMonthStart, QMonthEnd, QWin, QEarned, QLand, QInd, QPaid, QSub, QPoll, QInc;
        public int QClaimed, QEmptied, QPos, QEmptiedPos;
        public double QEstOut, QEstIn;   // (848) 相続 R1 の出と入り
        public double QDebtCarry; public Dictionary<(string File, int Line, SavKind Kind, char Cp), double>? QOutOther;   // (920) Pop.Carry で受けた債務の和・outOther の経路別の和（遅延確保）
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
        foreach (var p in r.Pops) CgPops.GetValue(p, _ => new CgPop()).QReset(p.Savings.Value);   // (836) POP の四半期の台帳も同じ時点で戻す
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
                // (836) POP の四半期の台帳：月の窓・請求・空の印・月初が正で begin≧claim の月と、その中の止まった月の振り分け
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

    // ---------------------------------------------------------------- (848) 相続 R1 の列（読むだけ・恒等・事前登録 measurements/m848_20261105_prereg_eh.txt §4 G2） ----------------------------------------------------------------
    // cearn_s の末尾 8 列（全ての国の全ての州×四半期）：emptyPopSavings＝四半期の末の断面の人数 ≦1e-12 で在庫が正の POP の在庫の和／estateDue・estateNoHeir＝EmptyPopEstate.Step の頭で計器が自分で数えた
    //   〈その月に移るはずの額〉（旗に依らない・受け手の人数の和 ＞1e-9 の州＝due・それ以外＝noHeir）の四半期の和／estateMoved・estateIn＝移し方の 1 本の呼び手が EmptyPopEstate.cs の
    //   書き込み（払い手の出・受け手の入り）／estateOutFromLive＝人数 ＞1e-12 の払い手の出／estateInToEmpty＝人数 ≦1e-12 の受け手の入り／estateRecvPerCapitaSpread＝月ごとの受けた額÷受け手の人数の
    //   (最大−最小)÷最大の四半期の最大。cpop_s の末尾 2 列＝POP ごとの出と入り（四半期の和）。
    // ⚠限界：①登録は月初＝月の途中で現れた POP の書き込みは台帳の外 ②広がりは受け手の書き込みの時点の人数で割る ③due は旗 0 でも数える（反実仮想）＝旗 1 の腕では前の月の移しの後に入った額。
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
            a.EstDebtBeforeSnap = EmptyDebtSum(r, out _); a.EstSettledSnap = a.EstDebtSettled;   // (920) 相続の段の頭の断面
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

    /// <summary>(920) 人数 ≦1e-12 の POP の債務の和（符号つき）と、債務 ≠0 の POP の数。</summary>
    static double EmptyDebtSum(Region r, out int count)
    {
        double s = 0; count = 0;
        foreach (var p in r.Pops) if (!(p.Size > 1e-12) && p.Debt.Value != 0) { s += p.Debt.Value; count++; }
        return s;
    }

    /// <summary>(920) 相続の段（<see cref="EmptyPopEstate.Step"/>）の直後：人数 ≦1e-12 で債務 ≠0 の POP の数（月ごとの和）と、段の頭から直後までに人の居ない POP の債務が減った額から返した額（estateDebtSettledPerQuarter の段の中の増分）を引いた残り＝返し以外で債務が移った額。
    /// 旗を読まない・エンジンの関数を呼ばない。今の実装では人の居ない POP の債務を動かすのは返しの段だけ＝estateDebtMovedPerQuarter は常に 0（債務を相続人へ移す案 B1 が入ったときに初めて正になる）。</summary>
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

    /// <summary>(920) 決済 ①（BilateralMarket.cs の清算の頭）で、人数 ＞1e-12 の商人でない POP が債務を返した額。商人 POP と人の居ない POP は数えない。</summary>
    internal static void CearnBillSettle(Region r, Pop p, double back)
    {
        if (!CearnOn || !(p.Size > 1e-12) || ReferenceEquals(p, r.MerchantPop)) return;
        if (CearnAccs.TryGetValue(r, out var a)) a.HeirDebtSettled += back;
    }

    /// <summary>(920) Pop.Carry の 7 引数の形で受け手が受けた債務（受け手の通貨・cpop_s の debtInCarryPerQuarter）。Bank.Carry（銀行の与信の側）は数えない。</summary>
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

    // ---------------------------------------------------------------- (836) 植民地格の州の POP の行 cpop_s（読むだけ・恒等・作業の事前登録 measurements/m836_20261105/plan.txt） ----------------------------------------------------------------
    // 目的＝〈人数がほぼ 0 の POP に在庫が止まる根〉と〈人の居る POP の所得が 0 の根〉（(829) の反証役の直し）を、群でなく POP の単位で読む。
    // 行＝四半期の末に格が Colonial の州×その州の POP（州の Pops の並び・人数 0 の POP も書く）。cgrp_s と同じ Sav の口で、同じ列へ POP の四半期の台帳にも足す。
    // 列：識別（popIndex＝州の Pops の添字・classId・className・group）・人数・四半期の始まりの在庫（その州の台帳が戻された時点の値・戻される前に現れた POP は nan）・末の在庫・流入 7・流出 9・残差・遅れの印・
    //   月の窓（cgrp_s と同じ定義の POP の値）・精算の内訳（所得・その中の地代〔LandRentPerMonth〕と工業レント〔IndustrialRentPerMonth〕・支払い＝所得−増分・自給の値〔SubsistenceValuePerMonth〕＝精算の時点の月の値の和）・
    //   請求（cgrp_s と同じ式）・月の数え（請求の立つ月・空の印・月初の在庫が正で begin≧claim の月・その中の空の印・その中の止まった月の 6 区分＝CpStuckKind）・
    //   四半期の末の断面（失業率・自給率・主食の充足・その POP が賃金の受け手〔LaborPopIndex〕の行と地代の受け手〔OwnerPopIndex〕の行）・群の和の確かめ（州×群の POP の和−群の台帳）。
    //   賃金の行の採算の率はエンジンの関数を呼ばず、供給の段が書いた DesiredOutput（＝Capacity×採算の率）と Capacity を写す（読み手が割る）。
    // 環境変数の門 POB_CEARN=0 で書かない（cearn_s・cgrp_s と同じ門）。
    // ⚠限界（計器が自分で申告する）：①cgrp_s の限界①〜⑥と同じ ②払った税の種類（人頭税と所得税）は口では分けられない（国庫への移しは 1 本）＝払った額は outTax・種類は請求の式だけ
    //   ③自給の〈量〉は POP ごとの欄が無い＝自給の値と自給率 ④DesiredOutput は供給の段を通った月の値（眠った生産者・倒産の行は古い値）⑤断面は四半期の末の 1 時点。
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
        + "ownerLines\townerRevenuePerMonth\tgroupStartGap\tgroupNetFlowGap\testateOutPerQuarter\testateInPerQuarter"   // (848) 末尾 2 列＝相続 R1
        + "\tdebt"   // (863) 末尾 1 列＝四半期の末の Pop.Debt
        + "\toutOtherTop1Route\toutOtherTop1PerQuarter\tdebtInCarryPerQuarter";   // (920) 末尾 3 列＝outOther の上位の経路・その額・Pop.Carry で受けた債務

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
            if (s.LandShares is { } lsc)   // ★(895) 土地の持ち分：割合が正の枠の受け手のうち担い手でない者に行を 1（同じ受け手は 1 回）・売上×その受け手の割合の和（null は今の持ち主の 1 行）
            {
                long fbc = 0;
                for (int k = 0; k < lsc.Length; k++)
                {
                    if (!(lsc[k] > 0)) continue;
                    int ri = LsRecipient(r, s, k, ref fbc);
                    bool seen = false;
                    for (int j = 0; j < k; j++) if (lsc[j] > 0 && LsRecipient(r, s, j, ref fbc) == ri) { seen = true; break; }
                    if (seen || ri == li || ri < 0 || ri >= n) continue;
                    double wr = 0;
                    for (int j = k; j < lsc.Length; j++) if (lsc[j] > 0 && LsRecipient(r, s, j, ref fbc) == ri) wr += lsc[j];
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
            cols.Add(F(cw?.QEstOut ?? 0)); cols.Add(F(cw?.QEstIn ?? 0));   // (848)
            cols.Add(F(p.Debt.Value));   // (863)
            string oot = "-"; double oov = double.NaN; if (cw?.QOutOther is { } oqd) oot = CearnTop(oqd, 0, out oov);
            cols.Add(oot); cols.Add(F(oov)); cols.Add(F(cw?.QDebtCarry ?? 0));   // (920)
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
            // (842) 州の台帳が Started でない四半期（四半期の途中で州が足されて月初の登録の前）は、始まりの在庫 g.Start が既定の 0 のまま＝残差が在庫の全額に見える。cearn_s と同じに始まりと残差は nan で名乗る（行の数は変えない）。
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
        CpopRows(sk, seed, q, w, nation, r, gStart, net);   // (836) 同じ州の POP の行
    }
}
