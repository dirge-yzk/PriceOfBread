namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsScope.cs#1〕
public static partial class Instruments
{
    sealed class CscAcc
    {
        public long Rows, DealtRows, Deals, FallbackRows;
        public double Qty, QUnit, QBest, QLp, QSell, QAfterFallback;
        // 買い手の役（role 0）だけが持つ。〔履歴 InstrumentsScope.cs#2〕
        public long ThrRows, ThrDealtRows, Fills, ThrFills;
        public double ThrQty, PotRaw, PotAdded, ThrPotRaw;
    }
    sealed class CscState
    {
        public readonly Dictionary<(string Nation, int Role, int Cls), CscAcc> Map = new();
        // 書き手は清算の 1 本のスレッド（csc_s の Map と同じ）。〔履歴 InstrumentsScope.cs#3〕
        public readonly Dictionary<(string Nation, int Tier, int Payer), CscxAcc> MapX = new();
        public System.IO.StreamWriter? XW; public bool XHeader;
    }
    sealed class CscxAcc { public long Rows, Fills, Invalid, ToBest, SkippedLB; }

    /// <summary>
    /// 限界：行の終わりの時点の状態（エンジンの手形は清算の頭で決まる）。〔履歴 InstrumentsScope.cs#4〕
    /// </summary>
    internal static int CscxPayer(Region r)
    {
        int mi = r.MerchantPopIndex;
        if (mi < 0 || mi >= r.Pops.Count) return 3;
        var m = r.Pops[mi];
        if (!(m.Size > 1e-12)) return 2;
        return m.Debt.Value > 0 ? 1 : 0;
    }

    /// <summary>
    /// fills＝この行の着値の充填・invalid＝うち着値≧自州価格・toBest＝うち最良が替わった。〔履歴 InstrumentsScope.cs#5〕
    /// </summary>
    internal static void ScopeRowX(InternationalMarket m, World buyer, Region r, long fills, long invalid, long toBest, long skippedLB)
    {
        var s = CscAccs.GetValue(m, _ => new CscState());
        var key = (NameOf(buyer), (int)r.Tier, CscxPayer(r));
        if (!s.MapX.TryGetValue(key, out var a)) s.MapX[key] = a = new CscxAcc();
        a.Rows++; a.Fills += fills; a.Invalid += invalid; a.ToBest += toBest; a.SkippedLB += skippedLB;
    }

    /// <summary>
    /// 〔履歴 InstrumentsScope.cs#6〕
    /// </summary>
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

    /// <summary>
    /// 〔履歴 InstrumentsScope.cs#7〕
    /// </summary>
    internal static (long ThrRows, long Fills, long ThrFills, double ThrQty, double PotRaw, double PotAdded, double ThrPotRaw) ScopeGetThr(MultiNationSim sim, int cls)
    {
        long tr = 0, f = 0, tf = 0; double tq = 0, pr = 0, pa = 0, tpr = 0;
        if (CscAccs.TryGetValue(sim.Market, out var s))
            foreach (var kv in s.Map) if (kv.Key.Role == 0 && kv.Key.Cls == cls) { var a = kv.Value; tr += a.ThrRows; f += a.Fills; tf += a.ThrFills; tq += a.ThrQty; pr += a.PotRaw; pa += a.PotAdded; tpr += a.ThrPotRaw; }
        return (tr, f, tf, tq, pr, pa, tpr);
    }

    /// <summary>助走の直後：助走の月の成約を数えない。</summary>
    internal static void ScopeClear(MultiNationSim sim) { if (CscAccs.TryGetValue(sim.Market, out var s)) { s.Map.Clear(); s.MapX.Clear(); } PxClear(sim); }   // ScopeClear〔履歴 InstrumentsScope.cs#8〕

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

    const string CscxHeader = "year\tnation\ttierCode\tpayerCode\trowsCount\tfillsCount\tfillsInvalidCount\tfillsToBestCount\tfillsSkippedLB";

    /// <summary>
    /// tierCode＝Core／Integrated／Colonial の名・payerCode＝CscxPayer。〔履歴 InstrumentsScope.cs#9〕
    /// </summary>
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
                a.Invalid.ToString(System.Globalization.CultureInfo.InvariantCulture), a.ToBest.ToString(System.Globalization.CultureInfo.InvariantCulture),
                a.SkippedLB.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }
        s.MapX.Clear();
    }

    /// <summary>
    /// 〔履歴 InstrumentsScope.cs#10〕
    /// </summary>
    static void CscxClose(MultiNationSim sim)
    {
        if (CscAccs.TryGetValue(sim.Market, out var s)) { s.XW?.Dispose(); s.XW = null; }
    }
}
