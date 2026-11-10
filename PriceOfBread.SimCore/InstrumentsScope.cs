namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (403) 清算の見る範囲の絞り（旗 ClearingIdleSearchQuarters・ClearingPartnerScope）の計器（読むだけ） ----------------------------------------------------------------
// 部分クラス＝InstrumentsTransport.cs の先例（蓄えは ConditionalWeakTable・セーブに載らない）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。
// 出力は INS=1（POB_INSTRUMENTS）のときだけ。偽の間は本体の hook が 1 命令も走らない。旗 0 の腕（A）でも同じ分類で出す＝反事実の読み（旗 0 のとき、眠らせる対象の行は本当はどれだけ成約していたか）。
// 出すもの（1 試合＝csc_s<seed>.tsv）：年×国×役（B＝買い手の国・S＝売り手の国）×行の種類 の行。
//   行の種類 classCode：0＝四半期の最初の清算・眠らせる対象でない行／1＝最初の清算・眠らせる対象の行（前の月に輸入が無く、不足も無いか主食でない財の不足だけ。旗に依らない定義）で走査した行
//                       ＝旗 ② の腕では番の行だけ／2＝2・3 月目・覚えで絞らなかった行のうち前の月に輸入があった行／3＝2・3 月目・覚えで絞った行／4＝番でなくて飛ばした行（走査していない・rowsCount だけ）／5＝2・3 月目・覚えで絞らなかった行のうち前の月に輸入が無く不足（か自州価格の上昇）で起きた行。
//                       ＝旗 0 の腕の 2 が、腕 B の 3 の反事実（覚えのある行は最初の清算で成約した行＝2・3 月目には前の月に輸入がある行）。5 は①の対象外。
//   rowsCount＝その種類で走査した行（種類 4 は飛ばした行）・dealtRowsCount＝うち 1 件でも成約した行・dealsCount＝成約の件数・fallbackRowsCount＝覚えが尽きて全ての売り手を探し直した行。
//   qtyPerYear＝成約の数量の和・qtyTimes…＝数量で重みを付けた和：UnitPrice＝実際に払う単価 unitA（次善の着値と自州価格の小さい方）・BestDelivered＝最良の着値・LocalPrice＝買い手の自州価格・
//   SellerShipPrice＝売り手の受取（積出し価格 sA）。qtyAfterFallbackPerYear＝探し直しの後の成約の数量。
//   単価の比（unitA÷最良の着値）が覚えで絞った行（3）で旗 0 の行（2）より上がるなら、①は輸入国から輸出国へ価格の移転を起こしている（事前登録 追記3）。
// ★(434) 清算の片方向の錠（旗 ClearingIdleThrottledScan・ClearingIdlePotentialReweight）で末尾に足した列（既存の 14 列と並びは不変）：
//   throttledRowsCount＝買い手の州が律速（前の四半期の最初の清算で LastTradeThrottle＜1）の行（種類 4 では飛ばした行のうち律速の州の行）・throttledDealtRowsCount・throttledQtyPerYear＝うち成約の行と数量・
//   fillsCount・throttledFillsCount＝その種類の行が行った着値の充填の件数（台帳の`清算：着値の充填`の内訳）・potRawPerYear＝行が潜在量へ足そうとした素の量（Σ(doneQ+newUnmet)×Wg・重みを掛ける前）・
//   potAddedPerYear＝実際に買い手の州の潜在量へ足した量（前後の差・重みを含む）・throttledPotRawPerYear＝うち律速の州の素の量。潜在量を書くのは最初の清算（種類 0・1）だけ。種類 1 の素の量×(N−1) が種類 4 の飛ばした分の不偏な見積り。★(436) 計器の限界（反証役 cl〔bv〕低#5）：これは旗 ClearingIdleThrottledScan＝0 のときだけ成り立つ。既定 1 では種類 1 に律速の州の起こした行が混ざり種類 4 から抜ける＝見積りは throttledRowsCount の行を除いて読む。
// ⚠限界：①種類 4 の行数は走査した行と同じ母集団ではない（1a の門で飛ばした行は数えない）。②数量は財の単位が混ざる（国×年の和＝財の構成の変化を拾う。比は財の構成の違いも含む）。③ 助走の月は数えない（TrClear と同じ）。
public static partial class Instruments
{
    sealed class CscAcc
    {
        public long Rows, DealtRows, Deals, FallbackRows;
        public double Qty, QUnit, QBest, QLp, QSell, QAfterFallback;
        // ★(434) 清算の片方向の錠の計器：買い手の州が律速（前の四半期の最初の清算で LastTradeThrottle＜1）の行・着値の充填の件数・潜在量（素の量と実際に足した量）。買い手の役（role 0）だけが持つ。
        public long ThrRows, ThrDealtRows, Fills, ThrFills;
        public double ThrQty, PotRaw, PotAdded, ThrPotRaw;
    }
    sealed class CscState
    {
        public readonly Dictionary<(string Nation, int Role, int Cls), CscAcc> Map = new();
        // (1024) K1 cscx_s：年×国×格×払い手（csc_s の鍵と列は変えない＝別の表）。書き手は清算の 1 本のスレッド（csc_s の Map と同じ）。
        public readonly Dictionary<(string Nation, int Tier, int Payer), CscxAcc> MapX = new();
        public System.IO.StreamWriter? XW; public bool XHeader;
    }
    sealed class CscxAcc { public long Rows, Fills, Invalid, ToBest; }

    /// <summary>(1024) 払い手の区分（買い手の州から計器が自分で読む・Cearn の ImpKind と同じ述語）：0＝手形あり商人（人数＞1e-12 かつ債務が正でない）・1＝手形なし商人（人数＞1e-12 かつ債務が正）・2＝空の商人（商人の POP はあるが人数 ≦1e-12）・3＝商人の POP 無し（全 POP が払い手）。
    /// 限界：行の終わりの時点の状態（エンジンの手形は清算の頭で決まる）。</summary>
    internal static int CscxPayer(Region r)
    {
        int mi = r.MerchantPopIndex;
        if (mi < 0 || mi >= r.Pops.Count) return 3;
        var m = r.Pops[mi];
        if (!(m.Size > 1e-12)) return 2;
        return m.Debt.Value > 0 ? 1 : 0;
    }

    /// <summary>(1024) K1：走査した買い手行の終わり（<c>BilateralMarket</c>・読むだけ）。<paramref name="fills"/>＝この行の着値の充填・<paramref name="invalid"/>＝うち着値≧自州価格・<paramref name="toBest"/>＝うち最良が替わった。</summary>
    internal static void ScopeRowX(InternationalMarket m, World buyer, Region r, long fills, long invalid, long toBest)
    {
        var s = CscAccs.GetValue(m, _ => new CscState());
        var key = (NameOf(buyer), (int)r.Tier, CscxPayer(r));
        if (!s.MapX.TryGetValue(key, out var a)) s.MapX[key] = a = new CscxAcc();
        a.Rows++; a.Fills += fills; a.Invalid += invalid; a.ToBest += toBest;
    }

    /// <summary>(1024) 試験の口：cscx の蓄え（全ての国・格・払い手の和）。</summary>
    internal static (long Rows, long Fills, long Invalid, long ToBest) ScopeXGet(MultiNationSim sim)
    {
        long rows = 0, f = 0, iv = 0, tb = 0;
        if (CscAccs.TryGetValue(sim.Market, out var s)) foreach (var a in s.MapX.Values) { rows += a.Rows; f += a.Fills; iv += a.Invalid; tb += a.ToBest; }
        return (rows, f, iv, tb);
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<InternationalMarket, CscState> CscAccs = new();
    static CscAcc CscOf(InternationalMarket m, World w, int role, int cls)
    {
        var s = CscAccs.GetValue(m, _ => new CscState());
        string name = NameOf(w);
        var key = (name, role, cls);
        if (!s.Map.TryGetValue(key, out var a)) s.Map[key] = a = new CscAcc();
        return a;
    }

    /// <summary>走査した買い手行の終わり（<c>BilateralMarket</c>・読むだけ）。<paramref name="cls"/>＝行の種類（0〜3）・<paramref name="dealt"/>＝1 件でも成約したか・<paramref name="fell"/>＝覚えが尽きて探し直したか。</summary>
    internal static void ScopeRow(InternationalMarket m, World buyer, int cls, bool dealt, bool fell, bool thr, long fills)
    {
        var a = CscOf(m, buyer, 0, cls);
        a.Rows++; if (dealt) a.DealtRows++; if (fell) a.FallbackRows++;
        a.Fills += fills;
        if (thr) { a.ThrRows++; if (dealt) a.ThrDealtRows++; a.ThrFills += fills; }
    }
    /// <summary>番でなくて飛ばした行（種類 4）。<paramref name="thr"/>＝買い手の州が律速か（飛ばした行のうち律速の州の行が (ii) の起こす母集団）。</summary>
    internal static void ScopeSkip(InternationalMarket m, World buyer, bool thr)
    {
        var a = CscOf(m, buyer, 0, 4);
        a.Rows++; if (thr) a.ThrRows++;
    }
    /// <summary>潜在量の記帳 1 件（買い手の役）。<paramref name="raw"/>＝行が足そうとした素の量（重みを掛ける前）・<paramref name="added"/>＝実際に買い手の州の潜在量へ足した量（重みを含む・前後の差）。</summary>
    internal static void ScopePot(InternationalMarket m, World buyer, int cls, bool thr, double raw, double added)
    {
        var a = CscOf(m, buyer, 0, cls);
        a.PotRaw += raw; a.PotAdded += added;
        if (thr) a.ThrPotRaw += raw;
    }
    /// <summary>成約 1 件（買い手の国の役 B と売り手の国の役 S）。</summary>
    internal static void ScopeDeal(InternationalMarket m, World buyer, World seller, int cls, bool afterFallback, double qty, double unit, double bestDelivered, double localPrice, double sellerShipPrice, bool thr)
    {
        var b = CscOf(m, buyer, 0, cls);
        if (thr) b.ThrQty += qty;
        b.Deals++; b.Qty += qty; b.QUnit += qty * unit; b.QBest += qty * bestDelivered; b.QLp += qty * localPrice; b.QSell += qty * sellerShipPrice;
        if (afterFallback) b.QAfterFallback += qty;
        var s = CscOf(m, seller, 1, cls);
        s.Deals++; s.Qty += qty; s.QUnit += qty * unit; s.QBest += qty * bestDelivered; s.QLp += qty * localPrice; s.QSell += qty * sellerShipPrice;
        if (afterFallback) s.QAfterFallback += qty;
    }

    /// <summary>番人用：買い手の役（role 0）の行の種類 <paramref name="cls"/> の蓄えの全ての国の和（走査した行・探し直した行・成約の件数・数量・数量×単価・数量×最良の着値・数量×自州価格・探し直しの後の数量）。</summary>
    internal static (long Rows, long FallbackRows, long Deals, double Qty, double QUnit, double QBest, double QLp, double QAfterFallback) ScopeGet(MultiNationSim sim, int cls)
    {
        long rows = 0, fb = 0, deals = 0; double qty = 0, qu = 0, qb = 0, ql = 0, qf = 0;
        if (CscAccs.TryGetValue(sim.Market, out var s))
            foreach (var kv in s.Map) if (kv.Key.Role == 0 && kv.Key.Cls == cls) { var a = kv.Value; rows += a.Rows; fb += a.FallbackRows; deals += a.Deals; qty += a.Qty; qu += a.QUnit; qb += a.QBest; ql += a.QLp; qf += a.QAfterFallback; }
        return (rows, fb, deals, qty, qu, qb, ql, qf);
    }

    /// <summary>番人用（(434)）：買い手の役（role 0）の行の種類 <paramref name="cls"/> の蓄えの全ての国の和（律速の州の行・着値の充填・律速の州の充填・律速の州の成約の数量・素の潜在量・実際に足した潜在量・律速の州の素の潜在量）。</summary>
    internal static (long ThrRows, long Fills, long ThrFills, double ThrQty, double PotRaw, double PotAdded, double ThrPotRaw) ScopeGetThr(MultiNationSim sim, int cls)
    {
        long tr = 0, f = 0, tf = 0; double tq = 0, pr = 0, pa = 0, tpr = 0;
        if (CscAccs.TryGetValue(sim.Market, out var s))
            foreach (var kv in s.Map) if (kv.Key.Role == 0 && kv.Key.Cls == cls) { var a = kv.Value; tr += a.ThrRows; f += a.Fills; tf += a.ThrFills; tq += a.ThrQty; pr += a.PotRaw; pa += a.PotAdded; tpr += a.ThrPotRaw; }
        return (tr, f, tf, tq, pr, pa, tpr);
    }

    /// <summary>助走の直後：助走の月の成約を数えない。</summary>
    internal static void ScopeClear(MultiNationSim sim) { if (CscAccs.TryGetValue(sim.Market, out var s)) { s.Map.Clear(); s.MapX.Clear(); } PxClear(sim); }   // (1024) cscx_s・pxstep_s の助走の蓄えも空にする

    const string CscHeader = "year\tnation\troleCode\tclassCode\trowsCount\tdealtRowsCount\tdealsCount\tfallbackRowsCount\tqtyPerYear\tqtyTimesUnitPrice\tqtyTimesBestDelivered\tqtyTimesLocalPrice\tqtyTimesSellerShipPrice\tqtyAfterFallbackPerYear\tthrottledRowsCount\tthrottledDealtRowsCount\tthrottledQtyPerYear\tfillsCount\tthrottledFillsCount\tpotRawPerYear\tpotAddedPerYear\tthrottledPotRawPerYear";

    /// <summary>年の終わり（と試合の終わりの残り）：蓄えを行にして書き、0 へ戻す。</summary>
    static void ScopeFold(Sink sk, ulong seed, int q, MultiNationSim sim, bool force = false)
    {
        bool yearEnd = q % Calendar.QuartersPerYear == Calendar.QuartersPerYear - 1;
        if (!yearEnd && !force) return;
        if (!CscAccs.TryGetValue(sim.Market, out var s) || s.Map.Count == 0) return;
        int year = sim.Nations[0].World.Year;
        sk.Csc ??= Open("csc", seed);
        if (!sk.CscHeader) { sk.Csc.WriteLine(CscHeader); sk.CscHeader = true; }
        foreach (var kv in s.Map.OrderBy(k => k.Key.Nation, StringComparer.Ordinal).ThenBy(k => k.Key.Role).ThenBy(k => k.Key.Cls))
        {
            var a = kv.Value;
            sk.Csc.WriteLine(string.Join('\t', TrI(year), kv.Key.Nation, TrI(kv.Key.Role), TrI(kv.Key.Cls),
                a.Rows.ToString(System.Globalization.CultureInfo.InvariantCulture), a.DealtRows.ToString(System.Globalization.CultureInfo.InvariantCulture), a.Deals.ToString(System.Globalization.CultureInfo.InvariantCulture), a.FallbackRows.ToString(System.Globalization.CultureInfo.InvariantCulture),
                F(a.Qty), F(a.QUnit), F(a.QBest), F(a.QLp), F(a.QSell), F(a.QAfterFallback),
                a.ThrRows.ToString(System.Globalization.CultureInfo.InvariantCulture), a.ThrDealtRows.ToString(System.Globalization.CultureInfo.InvariantCulture), F(a.ThrQty),
                a.Fills.ToString(System.Globalization.CultureInfo.InvariantCulture), a.ThrFills.ToString(System.Globalization.CultureInfo.InvariantCulture), F(a.PotRaw), F(a.PotAdded), F(a.ThrPotRaw)));
        }
        s.Map.Clear();
        CscxFold(s, seed, year);
    }

    const string CscxHeader = "year\tnation\ttierCode\tpayerCode\trowsCount\tfillsCount\tfillsInvalidCount\tfillsToBestCount";

    /// <summary>(1024) K1 cscx_s の年の終わり（と試合の終わりの残り）：csc_s と同じ呼びで行にして書き、0 へ戻す。tierCode＝Core／Integrated／Colonial の名・payerCode＝<see cref="CscxPayer"/>。</summary>
    static void CscxFold(CscState s, ulong seed, int year)
    {
        if (s.MapX.Count == 0) return;
        s.XW ??= Open("cscx", seed);
        if (!s.XHeader) { s.XW.WriteLine(CscxHeader); s.XHeader = true; }
        foreach (var kv in s.MapX.OrderBy(k => k.Key.Nation, StringComparer.Ordinal).ThenBy(k => k.Key.Tier).ThenBy(k => k.Key.Payer))
        {
            var a = kv.Value;
            s.XW.WriteLine(string.Join('\t', TrI(year), kv.Key.Nation, ((RegionTier)kv.Key.Tier).ToString(), TrI(kv.Key.Payer),
                a.Rows.ToString(System.Globalization.CultureInfo.InvariantCulture), a.Fills.ToString(System.Globalization.CultureInfo.InvariantCulture),
                a.Invalid.ToString(System.Globalization.CultureInfo.InvariantCulture), a.ToBest.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }
        s.MapX.Clear();
    }

    /// <summary>(1024) cscx_s の書き手を閉じる（<see cref="PxstepClose"/> から呼ぶ）。</summary>
    static void CscxClose(MultiNationSim sim)
    {
        if (CscAccs.TryGetValue(sim.Market, out var s)) { s.XW?.Dispose(); s.XW = null; }
    }
}
