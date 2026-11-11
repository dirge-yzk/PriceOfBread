using System.Collections.Generic;
using TrEntry = (int nation, PriceOfBread.SimCore.Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk);
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (1003) 貿易の計器 C2 tradeskip_s（成約 0 の理由・読むだけ・恒等・事前登録 measurements/m1003_20261110_prereg_tg.txt）
// 部分クラス。力学の値を 1 つも書かない・乱数を引かない・距離の表（KmOf・PairRisk）を呼ばない（表に副作用を作らない）。
// 読む清算＝調べる年（NgEveryYears の倍数と 1913）の第 4 四半期の**最初の清算**（全ての行が走る清算）。C1（nationgood_s）は四半期の最後の清算を読む＝別の清算。
// 行＝年×買い手の国×財（＋国＝ALL の財ごとの行と合計の行）。買い手の行（BilateralMarket の buyers の 1 行＝CostLedger.ClearingRows に数える行）ごとにちょうど 1 つの結果：
//   rowIdle（番の標本の飛ばし）・rowNoNeed（需要なし＝xNeed≦0・参照価格≦1e-9・需要 0）・rowBudget0（予算 0）・rowScan（走査に入った行）。
//   走査に入った行は rowDealtCross（越境の成約あり）・rowDealtDomOnly（国内の成約だけ）・rowNoDeal に分ける。
//   走査に入った行ごとに、その財の売り手の表の越境の売り手を最初に当たった門で 1 つに数える：xSoldOut（残り 0）・xCap（輸送の容量）・xWedge（楔で飛ばす）・xPass（楔を通った）。
//   xWedge は互いに排他の 4 区分：xWedgeA 関税を抜けば通る・xWedgeB A でなく運賃を抜けば通る・xWedgeC A でも B でもないが両方抜けば通る・xWedgeD 両方抜いても通らない。
//     式＝selP×(baseMul＋wS2)≧lp（現物）・A＝selP×(baseMul＋wS2−duty)＜lp・B＝selP×(1＋wS2)＜lp・C＝selP×(1＋wS2−duty)＜lp。距離・危険料は引かない。
//   ratioBin0〜9＝越境の売り手の selP÷lp（金建て）の度数（境 0.5・0.8・0.9・1.0・1.1・1.25・1.5・2・4）。domPairs＝国内の売り手の数。
//   売り手側（財ごとに 1 回・売り手の国の行）：sellerUnlinked（連結なし）・sellerNoStock（lp≦1e-9 か在庫≦1e-9）・sellerOtherGate（それ以外の門で売り手の表に載らない）・sellerListed。
//   国＝ALL・財＝名前の行：goodNoSellers（その財に売り手が 0 人＝ClearingRows に数えない）。国＝ALL・財＝ALL の行：rowsLedger＝この清算の Cost[ClearingRows] の増分（エンジンの台帳）。
// ⚠限界（計器が自己申告する）：呼び口の引数でエンジンの局所（lp・capLeft・left・売り手の表・baseMul・wS2・duty）を受け取る（ScopeSkip・ScopeDeal の先例）＝再導出ではない。
//   数え上げ（行の結果の排他・和・台帳との突き合わせ）は計器のコード。着値（距離）の判定はしない＝xPass は距離を含めた着値が lp 未満かを見ていない。
//   売り手の国の次元は行に持たない。国内の売り手は xPairs に数えない。
public static partial class Instruments
{
    internal static readonly string TradeskipHeaderText = "year\tnation\tgood\trows\trowIdle\trowNoNeed\trowBudget0\trowScan\trowDealtCross\trowDealtDomOnly\trowNoDeal\t"
        + "xPairs\txSoldOut\txCap\txWedge\txWedgeA\txWedgeB\txWedgeC\txWedgeD\txPass\tdomPairs\t"
        + "ratioBin0\tratioBin1\tratioBin2\tratioBin3\tratioBin4\tratioBin5\tratioBin6\tratioBin7\tratioBin8\tratioBin9\t"
        + "sellerUnlinked\tsellerNoStock\tsellerOtherGate\tsellerListed\tgoodNoSellers\trowsLedger\t"
        + "dutyMean\tdutyTariffMean\tdutyCornMean\txWedgeAT\txWedgeAC\txWedgeAB\t" + TsGateHeader + "ratioNaN\ttarEff\ttariffNow\ttariffTarget\ttreatyScale\texRate"
        + "\tsogStockLeTarget\tsogSellAcc0\tsogMilitary\tsogOther\txpEndPriceHigh\txpEndXPriceSmall\txpEndXNeedSmall\txpEndYZero\txpEndBudget\txpEndCapMid\txpEndDealt\txpEndGuard"
        + "\tubRows\tubInf\trpIter\trpDiff\trpCtrl\trpCtrlDiff\tgdRows\tgdIterX\tgdIterY\tgdIterLeft\tgdIterBudget\tgdIterCap\tgdIterRemove\tubD\trpSumXOld\trpSumXBb";   // (1045) C4d・ガード終わりの内訳（末尾に足す・旧の列は同じ順・同じバイト）   // (1008) 末尾の列（旧の列は同じ順・同じバイト）・(1036) C4a：売り手の〈その他の門〉の分け（在庫≦目標・sellAcc 0・軍需・その他＝和は sellerOtherGate）・C4b：xPass の組の行の終わりの理由（組の数で重みづけ＝和は xPass）
    static string TsGateHeader => string.Concat(new[] { "xSoldBin", "xCapBin", "xWedgeBin", "xPassBin" }.Select(g => string.Concat(Enumerable.Range(0, 10).Select(k => g + k + "\t"))));
    internal const int TsNewCols = 3 + 3 + 40 + 1 + 5 + 12 + 13 + 3;
    const int DStride = TsAcc.DStride;   // (1045) 走査した行の実数の和の幅（旧の 3＋C4d の 3）   // (1008) ALL の行を同じ幅で埋める数

    internal enum TsCol { Rows, Idle, NoNeed, Budget0, Scan, DealtCross, DealtDom, NoDeal, XPairs, XSold, XCap, XWedge, WA, WB, WC, WD, XPass, DomPairs,
        Bin0, Bin1, Bin2, Bin3, Bin4, Bin5, Bin6, Bin7, Bin8, Bin9, SUnlinked, SNoStock, SLive, SListed, WAT, WAC, WAB, RNaN, GBin0, OgStock = GBin0 + 40, OgAcc0, OgMil, OgOther, XeHigh, XeXPrice, XeXNeed, XeY, XeBudget, XeCap, XeDeal, XeGuard,
        UbRows, UbInf, RpIter, RpDiff, RpCtrl, RpCtrlDiff, GdRows, GdX, GdY, GdLeft, GdBudget, GdCap, GdRemove, Count }   // (1045) 末尾の 13 列
    internal enum TsExit { None = 0, PriceHigh, XPriceSmall, XNeedSmall, YZero, Budget, CapMid, Dealt, Guard }
    /// <summary>(1036) 試験の呼び口：買い手が満ちた行の終わり（X≦0）で、その行の xPrice・xNeed と計器が付けた理由を渡す（null＝何もしない）。</summary>
    [System.ThreadStatic] internal static Action<double, double, TsExit>? TsExitProbe;
    internal const int TsOutIdle = 1, TsOutNoNeed = 2, TsOutBudget0 = 3;
    static readonly double[] TsRatioEdges = [0.5, 0.8, 0.9, 1.0, 1.1, 1.25, 1.5, 2.0, 4.0];

    internal sealed class TsAcc
    {
        internal int Nn, Gc; internal string[] Names = []; internal int[] C = []; internal int[] NoSell = [];
        internal long Ledger0, LedgerDelta; internal bool Filled, Ended;
        internal double[] D = [], TarEff = [], TariffNow = [], Target = [], Treaty = [], ExRate = [];   // (1008) D＝国×財×{duty・関税の分・穀物法の分}の和（走査した行）・国の値は清算の頭で蓄える
        int[] sNode = []; double[] sPrice = []; int sN; bool[] listedMark = []; int curXPass;
        int cur = -1, curBn, curG; bool scanned, dealtCross, dealtDom;

        internal int At(int bn, int g, TsCol c) => C[(bn * Gc + g) * (int)TsCol.Count + (int)c];
        void Add(int bn, int g, TsCol c) => C[(bn * Gc + g) * (int)TsCol.Count + (int)c]++;

        internal void GoodBegin(int cnt) { sN = 0; if (sNode.Length < cnt) { sNode = new int[Math.Max(64, cnt * 2)]; sPrice = new double[sNode.Length]; } }
        internal void AddSeller(int node, double price) { sNode[sN] = node; sPrice[sN] = price; sN++; }
        internal void NoSellers(int g) { CloseRow(); NoSell[g]++; }

        /// <summary>売り手側（その財に 1 回）：全ての州を連結の有無・在庫で数える（売り手の表は AddSeller で渡した物）。</summary>
        internal void SellerSide(TrEntry[] tr, int tc, double[] lpOf, Market[] mktOf, int g, bool milGood, double[] milExportAcc)
        {
            if (listedMark.Length < tc) listedMark = new bool[Math.Max(64, tc * 2)]; else Array.Clear(listedMark, 0, tc);
            for (int i = 0; i < sN; i++) listedMark[sNode[i]] = true;
            for (int s = 0; s < tc; s++)
            {
                int sn = tr[s].nation;
                if (!tr[s].linked) Add(sn, g, TsCol.SUnlinked);
                else if (!(lpOf[s] > 1e-9) || !(mktOf[s].Stock > 1e-9)) Add(sn, g, TsCol.SNoStock);
                else
                {
                    Add(sn, g, TsCol.SLive);
                    if (listedMark[s]) continue;
                    // (1045) C4a の順をエンジンの門の順へ（BilateralMarket.cs の売り手の表の構築＝出し手の係数 sellAcc≦1e-12〔州の access → 軍需 → 鎖国・アヘン〕→ 在庫−目標≦1e-12）。
                    //   計器は鎖国とアヘンの係数を再導出しない（エンジンの関数を呼ばない）＝その 2 つで落ちた州は、在庫−目標≦1e-12 でもあれば OgStock、無ければ OgOther に数える（宣言した限界）。
                    if (tr[s].access <= 1e-12) Add(sn, g, TsCol.OgAcc0);
                    else if (milGood && tr[s].access * milExportAcc[sn] <= 1e-12) Add(sn, g, TsCol.OgMil);
                    else if (!(mktOf[s].Stock - mktOf[s].TargetStock > 1e-12)) Add(sn, g, TsCol.OgStock);
                    else Add(sn, g, TsCol.OgOther);
                }
            }
            for (int i = 0; i < sN; i++) Add(tr[sNode[i]].nation, g, TsCol.SListed);
        }

        void CloseRow()
        {
            if (cur < 0) return;
            if (scanned) Add(curBn, curG, dealtCross ? TsCol.DealtCross : dealtDom ? TsCol.DealtDom : TsCol.NoDeal);
            cur = -1; scanned = dealtCross = dealtDom = false;
        }

        internal void Row(int bn, int g) { CloseRow(); cur = 1; curBn = bn; curG = g; curXPass = 0; Add(bn, g, TsCol.Rows); }
        /// <summary>(1036) C4b：走査した行の終わりの理由。その行の xPass の組の数だけ該当の列へ足す（組ごとの追跡ではなく行の終わりの理由を組の数で重みづけ）。</summary>
        internal void Exit(TsExit why)
        {
            if (cur >= 0 && scanned && why == TsExit.Guard)   // (1045) ガードで終わった走査の行の反復の内訳（xPass が 0 の行も数える＝行の数 gdRows）
            {
                int o = (curBn * Gc + curG) * (int)TsCol.Count;
                C[o + (int)TsCol.GdRows]++;
                for (int k = 0; k < GdLen; k++) C[o + (int)TsCol.GdX + k] += gd[k];
            }
            ExitXPass(why);
        }

        // (1045) C4d・ガード終わりの内訳：三つの呼び口（計器は Market の欄と売り手の表 sPrice を自分で読む・エンジンの関数を呼ばない）。
        internal const int DStride = 6, GdLen = 6;
        readonly int[] gd = new int[GdLen];   // 行の中の反復の内訳＝X・Y・left・予算・容量が縛った反復、売り手を外した反復

        /// <summary>:887 の xNeed≦0 の飛ばし：案 B-b の上界 TS＋D·(rpT/先頭の売り手の値段)^ε−P−S ＞ 0 の行を数える（先頭の売り手が居ない・値段が正でない・NaN は ubInf）。</summary>
        internal void SkipNeed(Market m, double refPrice, double priceLevel, double exRate, int headS, double eps)
        {
            if (cur < 0) return;
            double p = headS >= 0 && headS < sN ? sPrice[headS] : double.NaN;
            if (!(p > 0)) { Add(curBn, curG, TsCol.UbInf); return; }
            double rpT = refPrice * priceLevel * exRate;
            double ub = m.TargetStock + m.LastDemand * Math.Pow(rpT / p, eps) - m.LastProduced - m.Stock;
            if (!(ub > 0)) return;
            Add(curBn, curG, TsCol.UbRows);
            if (D.Length > 0) D[(curBn * Gc + curG) * DStride + 3] += m.LastDemand;
        }

        /// <summary>最良と次善が決まり直した反復（dealConstStale）：旧の X と案 B-b の X を同じ盤面で両方記帳する（不透明の漏れ＝アヘンの係数は掛けない）。
        /// 統制の行＝r≦1 かつ S−I＜TS（紙で X が一致する側）。</summary>
        internal void Repick(Market m, double access, double refPrice, double priceLevel, double exRate, double unitA, double eps)
        {
            if (cur < 0 || !scanned) return;
            double rpT = refPrice * priceLevel * exRate;
            bool on = rpT > 1e-9 && unitA > 1e-9;
            double r = rpT / unitA;
            double xPrice = on ? m.LastDemand * Math.Pow(r, 2.0 * eps) - (m.LastProduced + m.LastImport) : 0.0;
            double xNeed = m.TargetStock + m.LastDemand - m.LastProduced - m.Stock;
            double xNeedB = on ? m.TargetStock + m.LastDemand * Math.Pow(r, eps) - m.LastProduced - m.Stock : xNeed;
            double xOld = access * Math.Max(0, Math.Min(xPrice, xNeed));
            double xBb = access * Math.Max(0, Math.Min(xPrice, xNeedB));
            bool diff = xOld != xBb && !(double.IsNaN(xOld) && double.IsNaN(xBb));
            Add(curBn, curG, TsCol.RpIter); if (diff) Add(curBn, curG, TsCol.RpDiff);
            if (on && r <= 1.0 && m.Stock - m.LastImport < m.TargetStock) { Add(curBn, curG, TsCol.RpCtrl); if (diff) Add(curBn, curG, TsCol.RpCtrlDiff); }
            if (D.Length > 0) { int di = (curBn * Gc + curG) * DStride; D[di + 4] += xOld; D[di + 5] += xBb; }
        }

        /// <summary>成約の判定の反復：X・Y・売り手の残り・予算の項・容量の項のうち最小のもの（同値は左）を数える。NaN は選ばない。</summary>
        internal void Iter(double x, double y, double left, double budgetTerm, double capTerm)
        {
            if (cur < 0 || !scanned) return;
            int k = 0; double v = x;
            if (y < v || double.IsNaN(v)) { k = 1; v = y; }
            if (left < v || double.IsNaN(v)) { k = 2; v = left; }
            if (budgetTerm < v || double.IsNaN(v)) { k = 3; v = budgetTerm; }
            if (capTerm < v || double.IsNaN(v)) { k = 4; v = capTerm; }
            gd[k]++;
        }
        internal void IterRemove() { if (cur < 0 || !scanned) return; gd[5]++; }

        void ExitXPass(TsExit why) { if (cur < 0 || !scanned || curXPass == 0 || why == TsExit.None) return; C[(curBn * Gc + curG) * (int)TsCol.Count + (int)TsCol.XeHigh + (int)why - 1] += curXPass; }
        internal void Outcome(int which) { if (cur < 0) return; Add(curBn, curG, which == TsOutIdle ? TsCol.Idle : which == TsOutNoNeed ? TsCol.NoNeed : TsCol.Budget0); }
        internal void Deal(bool cross) { if (cur < 0) return; if (cross) dealtCross = true; else dealtDom = true; }

        /// <summary>走査に入る直前（行の門を全て通った）：売り手の表の越境の売り手を最初に当たった門で数える。</summary>
        internal void Scan(TrEntry[] tr, int t, double lp, double[] left, double[] capLeft, double baseMul, double wedge, double duty, double[]? sellCheap, double cornPen = 0.0)
        {
            if (cur < 0) return;
            scanned = true; Add(curBn, curG, TsCol.Scan);
            double protect = TarEff.Length > curBn ? Math.Max(0, TarEff[curBn]) : duty - cornPen;   // (1008) 関税の分＝エンジンの protect の写し（tar の局所から）・合成の盤面だけ差で出す
            for (int k = 0; k < GdLen; k++) gd[k] = 0;   // (1045) 行ごとの反復の内訳を空にする
            if (D.Length > 0) { int di = (curBn * Gc + curG) * DStride; D[di] += duty; D[di + 1] += protect; D[di + 2] += cornPen; }
            for (int i = 0; i < sN; i++)
            {
                int sn = sNode[i]; int sNat = tr[sn].nation; double selP = sPrice[i];
                if (sNat == curBn) { Add(curBn, curG, TsCol.DomPairs); continue; }
                Add(curBn, curG, TsCol.XPairs);
                double ratio = selP / lp; int b = 0; while (b < TsRatioEdges.Length && ratio >= TsRatioEdges[b]) b++;
                bool nan = double.IsNaN(ratio);
                if (!nan) Add(curBn, curG, TsCol.Bin0 + b); else Add(curBn, curG, TsCol.RNaN);
                if (left[i] <= 1e-12) { Add(curBn, curG, TsCol.XSold); if (!nan) Add(curBn, curG, TsCol.GBin0 + b); continue; }
                if (capLeft[sn] <= 1e-12 || capLeft[t] <= 1e-12) { Add(curBn, curG, TsCol.XCap); if (!nan) Add(curBn, curG, TsCol.GBin0 + 10 + b); continue; }
                double wS2 = wedge - (sellCheap is not null ? sellCheap[sNat] : 0.0);
                if (selP * (baseMul + wS2) >= lp)
                {
                    Add(curBn, curG, TsCol.XWedge); if (!nan) Add(curBn, curG, TsCol.GBin0 + 20 + b);
                    if (selP * (baseMul + wS2 - duty) < lp)
                    {
                        Add(curBn, curG, TsCol.WA);
                        if (selP * (baseMul + wS2 - protect) < lp) Add(curBn, curG, TsCol.WAT);
                        else if (selP * (baseMul + wS2 - cornPen) < lp) Add(curBn, curG, TsCol.WAC);
                        else Add(curBn, curG, TsCol.WAB);
                    }
                    else if (selP * (1.0 + wS2) < lp) Add(curBn, curG, TsCol.WB);
                    else if (selP * (1.0 + wS2 - duty) < lp) Add(curBn, curG, TsCol.WC);
                    else Add(curBn, curG, TsCol.WD);
                }
                else { Add(curBn, curG, TsCol.XPass); curXPass++; if (!nan) Add(curBn, curG, TsCol.GBin0 + 30 + b); }
            }
        }

        internal void End(long ledger) { CloseRow(); LedgerDelta = ledger - Ledger0; Ended = true; }
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<InternationalMarket, TsAcc> TsAccs = new();
    sealed class TsSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, TsSink> TsSinks = new();

    /// <summary>清算の頭：調べる年の第 4 四半期の最初の清算なら蓄えを返す（それ以外・計器が切れていれば null）。呼び口は null の確かめだけを熱い所に置く。</summary>
    internal static TsAcc? TsBegin(InternationalMarket m, IReadOnlyList<Simulation> nations, int gc, bool first, long ledger, double[]? tar = null, double[]? exRate = null)
    {
        if (!On || !first || nations.Count == 0) return null;
        var w0 = nations[0].World;
        if (w0.Quarter != Calendar.QuartersPerYear || !NgYear(w0.Year + 1)) return null;   // Quarter の年末の呼び（q%4==3）の直前に終わった四半期＝第 4 四半期（World.Year はその四半期の末で進む＝呼びの時点の年は +1・表の年は C1 と同じ呼びの時点の年）
        var a = TsAccs.GetValue(m, _ => new TsAcc());
        int nn = nations.Count, len = nn * gc * (int)TsCol.Count;
        if (a.C.Length != len) a.C = new int[len]; else Array.Clear(a.C);
        if (a.NoSell.Length != gc) a.NoSell = new int[gc]; else Array.Clear(a.NoSell);
        a.Names = new string[nn]; for (int i = 0; i < nn; i++) a.Names[i] = NameOf(nations[i].World);
        if (a.D.Length != nn * gc * DStride) a.D = new double[nn * gc * DStride]; else Array.Clear(a.D);   // (1045) 3 → DStride（先頭の 3 つは旧のまま・後ろ 3 つは C4d の和）
        a.TarEff = new double[nn]; a.TariffNow = new double[nn]; a.Target = new double[nn]; a.Treaty = new double[nn]; a.ExRate = new double[nn];   // (1008) 国の値は清算の頭の写し（tar・exRate はエンジンの局所・他は World から計器が自分で写す）
        for (int i = 0; i < nn; i++)
        {
            var wi = nations[i].World; a.TarEff[i] = tar is not null ? tar[i] : double.NaN; a.ExRate[i] = exRate is not null ? exRate[i] : double.NaN;
            a.TariffNow[i] = wi.Tariff; a.Treaty[i] = 1.0 - Politics.ForeignPortShare(wi);
            double tg = Government.Q5(wi.Policy.TariffTarget) + wi.TariffRetaliationBias; a.Target[i] = tg < wi.Coef.TariffFloor ? wi.Coef.TariffFloor : tg > 1 ? 1 : tg;
        }
        a.Nn = nn; a.Gc = gc; a.Ledger0 = ledger; a.LedgerDelta = 0; a.Filled = true; a.Ended = false;
        return a;
    }

    /// <summary>番人用：蓄え（無ければ null）。</summary>
    internal static TsAcc? TsSnapshot(InternationalMarket m) => TsAccs.TryGetValue(m, out var a) && a.Filled ? a : null;

    internal static string FR(double v) => double.IsNaN(v) ? "nan" : double.IsInfinity(v) ? (v > 0 ? "inf" : "-inf") : v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);   // (1008) 往復の桁
    static void TradeskipTable(MultiNationSim sim, ulong seed, int q)
    {
        bool yearEnd = q % Calendar.QuartersPerYear == Calendar.QuartersPerYear - 1;
        if (!yearEnd || !TsAccs.TryGetValue(sim.Market, out var a) || !a.Filled || !a.Ended) return;   // 行は清算に居た国（TsBegin の時点の名簿・名前も写し）だけ
        var sk = TsSinks.GetValue(sim, _ => new TsSink());
        if (sk.W is null) { sk.W = Open("tradeskip", seed); sk.W.WriteLine(TradeskipHeaderText); }
        string I(long v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int nc = (int)TsCol.Count; int year = sim.Nations[0].World.Year;
        var cols = new List<string>(48);
        for (int i = 0; i < a.Nn; i++)
        {
            string nm = a.Names[i];
            for (int g = 0; g < Catalog.Goods.Length && g < a.Gc; g++)
            {
                int o = (i * a.Gc + g) * nc;
                int live = a.C[o + (int)TsCol.SLive], listed = a.C[o + (int)TsCol.SListed];
                cols.Clear(); cols.Add(I(year)); cols.Add(nm); cols.Add(Catalog.Goods[g].Name);
                for (int c = (int)TsCol.Rows; c <= (int)TsCol.Bin9; c++) cols.Add(I(a.C[o + c]));
                cols.Add(I(a.C[o + (int)TsCol.SUnlinked])); cols.Add(I(a.C[o + (int)TsCol.SNoStock])); cols.Add(I(live - listed)); cols.Add(I(listed)); cols.Add("0"); cols.Add("0");
                double ns = a.C[o + (int)TsCol.Scan]; int di = (i * a.Gc + g) * DStride;   // (1008) 末尾の列：走査した行の平均（走査 0 の行は 0）・小数は往復の桁（門の 1e-12 のため）
                for (int k = 0; k < 3; k++) cols.Add(ns > 0 ? FR(a.D[di + k] / ns) : "0");
                cols.Add(I(a.C[o + (int)TsCol.WAT])); cols.Add(I(a.C[o + (int)TsCol.WAC])); cols.Add(I(a.C[o + (int)TsCol.WAB]));
                for (int c = (int)TsCol.GBin0; c < (int)TsCol.GBin0 + 40; c++) cols.Add(I(a.C[o + c]));
                cols.Add(I(a.C[o + (int)TsCol.RNaN])); cols.Add(FR(a.TarEff[i])); cols.Add(FR(a.TariffNow[i])); cols.Add(FR(a.Target[i])); cols.Add(FR(a.Treaty[i])); cols.Add(FR(a.ExRate[i]));
                for (int c = (int)TsCol.OgStock; c < (int)TsCol.Count; c++) cols.Add(I(a.C[o + c]));   // (1036) C4a・C4b・(1045) C4d の整数の列
                for (int k = 3; k < DStride; k++) cols.Add(FR(a.D[di + k]));   // (1045) C4d の和（ubD・rpSumXOld・rpSumXBb）
                sk.W.WriteLine(string.Join('\t', cols));
            }
        }
        string zeros = string.Join('\t', Enumerable.Repeat("0", 32)), zerosNew = string.Join('\t', Enumerable.Repeat("0", TsNewCols));
        for (int g = 0; g < Catalog.Goods.Length && g < a.Gc; g++)
            sk.W.WriteLine(string.Join('\t', I(year), "ALL", Catalog.Goods[g].Name) + "\t" + zeros + "\t" + I(a.NoSell[g]) + "\t0\t" + zerosNew);
        sk.W.WriteLine(string.Join('\t', I(year), "ALL", "ALL") + "\t" + zeros + "\t0\t" + I(a.LedgerDelta) + "\t" + zerosNew);
        a.Filled = false; a.Ended = false;
    }

    static void TradeskipClose(MultiNationSim sim) { if (TsSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; } }
}
