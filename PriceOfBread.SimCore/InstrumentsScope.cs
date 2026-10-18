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
// ⚠限界：①種類 4 の行数は走査した行と同じ母集団ではない（1a の門で飛ばした行は数えない）。②数量は財の単位が混ざる（国×年の和＝財の構成の変化を拾う。比は財の構成の違いも含む）。③ 助走の月は数えない（TrClear と同じ）。
public static partial class Instruments
{
    sealed class CscAcc
    {
        public long Rows, DealtRows, Deals, FallbackRows;
        public double Qty, QUnit, QBest, QLp, QSell, QAfterFallback;
    }
    sealed class CscState
    {
        public readonly Dictionary<(string Nation, int Role, int Cls), CscAcc> Map = new();
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
    internal static void ScopeRow(InternationalMarket m, World buyer, int cls, bool dealt, bool fell)
    {
        var a = CscOf(m, buyer, 0, cls);
        a.Rows++; if (dealt) a.DealtRows++; if (fell) a.FallbackRows++;
    }
    /// <summary>番でなくて飛ばした行（種類 4）。</summary>
    internal static void ScopeSkip(InternationalMarket m, World buyer) => CscOf(m, buyer, 0, 4).Rows++;
    /// <summary>成約 1 件（買い手の国の役 B と売り手の国の役 S）。</summary>
    internal static void ScopeDeal(InternationalMarket m, World buyer, World seller, int cls, bool afterFallback, double qty, double unit, double bestDelivered, double localPrice, double sellerShipPrice)
    {
        var b = CscOf(m, buyer, 0, cls);
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

    /// <summary>助走の直後：助走の月の成約を数えない。</summary>
    internal static void ScopeClear(MultiNationSim sim) { if (CscAccs.TryGetValue(sim.Market, out var s)) s.Map.Clear(); }

    const string CscHeader = "year\tnation\troleCode\tclassCode\trowsCount\tdealtRowsCount\tdealsCount\tfallbackRowsCount\tqtyPerYear\tqtyTimesUnitPrice\tqtyTimesBestDelivered\tqtyTimesLocalPrice\tqtyTimesSellerShipPrice\tqtyAfterFallbackPerYear";

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
                F(a.Qty), F(a.QUnit), F(a.QBest), F(a.QLp), F(a.QSell), F(a.QAfterFallback)));
        }
        s.Map.Clear();
    }
}
