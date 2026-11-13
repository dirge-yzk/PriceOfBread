using System.Collections.Generic;
using TrEntry = (int nation, PriceOfBread.SimCore.Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk);
namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・距離の表（KmOf・PairRisk）を呼ばない（表に副作用を作らない）。〔履歴 InstrumentsTradeskip.cs#1〕
public static partial class Instruments
{
    internal static readonly string TradeskipHeaderText = "year\tnation\tgood\trows\trowIdle\trowNoNeed\trowBudget0\trowScan\trowDealtCross\trowDealtDomOnly\trowNoDeal\t"
        + "xPairs\txSoldOut\txCap\txWedge\txWedgeA\txWedgeB\txWedgeC\txWedgeD\txPass\tdomPairs\t"
        + "ratioBin0\tratioBin1\tratioBin2\tratioBin3\tratioBin4\tratioBin5\tratioBin6\tratioBin7\tratioBin8\tratioBin9\t"
        + "sellerUnlinked\tsellerNoStock\tsellerOtherGate\tsellerListed\tgoodNoSellers\trowsLedger\t"
        + "dutyMean\tdutyTariffMean\tdutyCornMean\txWedgeAT\txWedgeAC\txWedgeAB\t" + TsGateHeader + "ratioNaN\ttarEff\ttariffNow\ttariffTarget\ttreatyScale\texRate"
        + "\tsogStockLeTarget\tsogSellAcc0\tsogMilitary\tsogOther\txpEndPriceHigh\txpEndXPriceSmall\txpEndXNeedSmall\txpEndYZero\txpEndBudget\txpEndCapMid\txpEndDealt\txpEndGuard"
        + "\tubRows\tubInf\trpIter\trpDiff\trpCtrl\trpCtrlDiff\tgdRows\tgdIterX\tgdIterY\tgdIterLeft\tgdIterBudget\tgdIterCap\tgdIterRemove\tubD\trpSumXOld\trpSumXBb";   // tgdIterBudget〔履歴 InstrumentsTradeskip.cs#2〕
    static string TsGateHeader => string.Concat(new[] { "xSoldBin", "xCapBin", "xWedgeBin", "xPassBin" }.Select(g => string.Concat(Enumerable.Range(0, 10).Select(k => g + k + "\t"))));
    internal const int TsNewCols = 3 + 3 + 40 + 1 + 5 + 12 + 13 + 3;
    const int DStride = TsAcc.DStride;   // DStride〔履歴 InstrumentsTradeskip.cs#3〕

    internal enum TsCol { Rows, Idle, NoNeed, Budget0, Scan, DealtCross, DealtDom, NoDeal, XPairs, XSold, XCap, XWedge, WA, WB, WC, WD, XPass, DomPairs,
        Bin0, Bin1, Bin2, Bin3, Bin4, Bin5, Bin6, Bin7, Bin8, Bin9, SUnlinked, SNoStock, SLive, SListed, WAT, WAC, WAB, RNaN, GBin0, OgStock = GBin0 + 40, OgAcc0, OgMil, OgOther, XeHigh, XeXPrice, XeXNeed, XeY, XeBudget, XeCap, XeDeal, XeGuard,
        UbRows, UbInf, RpIter, RpDiff, RpCtrl, RpCtrlDiff, GdRows, GdX, GdY, GdLeft, GdBudget, GdCap, GdRemove, Count }   // RpCtrlDiff〔履歴 InstrumentsTradeskip.cs#4〕
    internal enum TsExit { None = 0, PriceHigh, XPriceSmall, XNeedSmall, YZero, Budget, CapMid, Dealt, Guard }
    /// <summary>
    /// 〔履歴 InstrumentsTradeskip.cs#5〕
    /// </summary>
    [System.ThreadStatic] internal static Action<double, double, TsExit>? TsExitProbe;
    /// <summary>
    /// 〔履歴 InstrumentsTradeskip.cs#6〕
    /// </summary>
    [System.ThreadStatic] internal static bool TsPairAllYears;
    internal const int TsOutIdle = 1, TsOutNoNeed = 2, TsOutBudget0 = 3;
    static readonly double[] TsRatioEdges = [0.5, 0.8, 0.9, 1.0, 1.1, 1.25, 1.5, 2.0, 4.0];

    internal sealed class TsAcc
    {
        internal int Nn, Gc; internal string[] Names = []; internal int[] C = []; internal int[] NoSell = [];
        internal long Ledger0, LedgerDelta; internal bool Filled, Ended;
        internal double[] D = [], TarEff = [], TariffNow = [], Target = [], Treaty = [], ExRate = [];   // D〔履歴 InstrumentsTradeskip.cs#7〕
        int[] sNode = []; double[] sPrice = []; int sN; bool[] listedMark = []; int curXPass;
        int cur = -1, curBn, curG; bool scanned, dealtCross, dealtDom;

        // PairOn〔履歴 InstrumentsTradeskip.cs#8〕
        internal bool PairOn; internal int[] Pair = []; internal double[] Sh = []; internal World[] Worlds = [];
        internal const int PairCols = 8, ShCols = 11, ShCls = 8;
        int[] pcStamp = []; string[] pcCls = []; double[] pcNow = [], pcBp = []; int stamp;
        readonly bool[] shRowShort = new bool[ShCls];
        static int ClsIx(string c) => c switch { "E" => 0, "N" => 1, "S" => 2, "F" => 3, "X" => 4, "Z" => 5, "U" => 6, _ => 7 };
        internal static readonly string[] ClsNames = ["E", "N", "S", "F", "X", "Z", "U", "-"];
        void P(int sn, int k) => Pair[((curBn * Nn + sn) * Gc + curG) * PairCols + k]++;
        /// <summary>その州のその財の最安の生産者の級・今の単位費用・案 B′ の単位費用（名目・RoRead と同じ式＝計器が自分で組む）。無ければ級 "-"・NaN。財ごとに 1 度だけ引く（GoodBegin で札を替える）。</summary>
        (string Cls, double Now, double Bp) CostOf(int node, TrEntry[] tr)
        {
            if (pcStamp.Length < tr.Length) { pcStamp = new int[tr.Length]; pcCls = new string[tr.Length]; pcNow = new double[tr.Length]; pcBp = new double[tr.Length]; }
            if (pcStamp[node] == stamp) return (pcCls[node], pcNow[node], pcBp[node]);
            string cls = "-"; double now = double.NaN, bp = double.NaN; var r = tr[node].r; var w = Worlds[tr[node].nation];
            foreach (var p in r.Producers)
            {
                if (p.GoodId != curG) continue;
                var x = RoRead(w, r, p);
                if (!(x.Mc >= 0) || !(x.IfBprime >= 0)) continue;
                if (cls == "-" || x.Mc < now) { cls = x.Class; now = x.Mc; bp = x.IfBprime; }
            }
            pcStamp[node] = stamp; pcCls[node] = cls; pcNow[node] = now; pcBp[node] = bp;
            return (cls, now, bp);
        }

        internal int At(int bn, int g, TsCol c) => C[(bn * Gc + g) * (int)TsCol.Count + (int)c];
        void Add(int bn, int g, TsCol c) => C[(bn * Gc + g) * (int)TsCol.Count + (int)c]++;

        internal void GoodBegin(int cnt) { sN = 0; stamp++; if (sNode.Length < cnt) { sNode = new int[Math.Max(64, cnt * 2)]; sPrice = new double[sNode.Length]; } }
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
                    // 計器は鎖国とアヘンの係数を再導出しない（エンジンの関数を呼ばない）＝その 2 つで落ちた州は、在庫−目標≦1e-12 でもあれば OgStock、無ければ OgOther に数える（宣言した限界）。〔履歴 InstrumentsTradeskip.cs#9〕
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
        /// <summary>
        /// その行の xPass の組の数だけ該当の列へ足す（組ごとの追跡ではなく行の終わりの理由を組の数で重みづけ）。〔履歴 InstrumentsTradeskip.cs#10〕
        /// </summary>
        internal void Exit(TsExit why)
        {
            if (cur >= 0 && scanned && why == TsExit.Guard)   // scanned〔履歴 InstrumentsTradeskip.cs#11〕
            {
                int o = (curBn * Gc + curG) * (int)TsCol.Count;
                C[o + (int)TsCol.GdRows]++;
                for (int k = 0; k < GdLen; k++) C[o + (int)TsCol.GdX + k] += gd[k];
            }
            ExitXPass(why);
        }

        // DStride〔履歴 InstrumentsTradeskip.cs#12〕
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
            double protect = TarEff.Length > curBn ? Math.Max(0, TarEff[curBn]) : duty - cornPen;   // protect〔履歴 InstrumentsTradeskip.cs#13〕
            for (int k = 0; k < GdLen; k++) gd[k] = 0;   // GdLen〔履歴 InstrumentsTradeskip.cs#14〕
            if (D.Length > 0) { int di = (curBn * Gc + curG) * DStride; D[di] += duty; D[di + 1] += protect; D[di + 2] += cornPen; }
            double bCostNow = double.NaN, bCostBp = double.NaN;
            if (PairOn) { var bc = CostOf(t, tr); bCostNow = bc.Now; bCostBp = bc.Bp; Array.Clear(shRowShort); }
            for (int i = 0; i < sN; i++)
            {
                int sn = sNode[i]; int sNat = tr[sn].nation; double selP = sPrice[i];
                if (sNat == curBn) { Add(curBn, curG, TsCol.DomPairs); continue; }
                Add(curBn, curG, TsCol.XPairs);
                if (PairOn) P(sNat, 0);
                double ratio = selP / lp; int b = 0; while (b < TsRatioEdges.Length && ratio >= TsRatioEdges[b]) b++;
                bool nan = double.IsNaN(ratio);
                if (!nan) Add(curBn, curG, TsCol.Bin0 + b); else Add(curBn, curG, TsCol.RNaN);
                if (left[i] <= 1e-12) { Add(curBn, curG, TsCol.XSold); if (PairOn) P(sNat, 1); if (!nan) Add(curBn, curG, TsCol.GBin0 + b); continue; }
                if (capLeft[sn] <= 1e-12 || capLeft[t] <= 1e-12) { Add(curBn, curG, TsCol.XCap); if (PairOn) P(sNat, 2); if (!nan) Add(curBn, curG, TsCol.GBin0 + 10 + b); continue; }
                double wS2 = wedge - (sellCheap is not null ? sellCheap[sNat] : 0.0);
                bool wedgeD = false;
                if (selP * (baseMul + wS2) >= lp)
                {
                    Add(curBn, curG, TsCol.XWedge); if (!nan) Add(curBn, curG, TsCol.GBin0 + 20 + b);
                    if (selP * (baseMul + wS2 - duty) < lp)
                    {
                        Add(curBn, curG, TsCol.WA); if (PairOn) P(sNat, 3);
                        if (selP * (baseMul + wS2 - protect) < lp) Add(curBn, curG, TsCol.WAT);
                        else if (selP * (baseMul + wS2 - cornPen) < lp) Add(curBn, curG, TsCol.WAC);
                        else Add(curBn, curG, TsCol.WAB);
                    }
                    else if (selP * (1.0 + wS2) < lp) { Add(curBn, curG, TsCol.WB); if (PairOn) P(sNat, 4); }
                    else if (selP * (1.0 + wS2 - duty) < lp) { Add(curBn, curG, TsCol.WC); if (PairOn) P(sNat, 5); }
                    else { Add(curBn, curG, TsCol.WD); wedgeD = true; if (PairOn) P(sNat, 6); }
                }
                else { Add(curBn, curG, TsCol.XPass); curXPass++; if (!nan) Add(curBn, curG, TsCol.GBin0 + 30 + b); if (PairOn) P(sNat, 7); }
                if (PairOn) Shadow(tr, t, sn, sNat, selP, baseMul, wS2, lp, wedgeD, bCostNow, bCostBp);
            }
        }

        /// <summary>
        /// 案 B′ の売り手の値段＝selP×（売り手の最安の生産者の B′ 費用÷今の費用）・買い手の lp も同じ比で動かす（値段が費用に従う近似＝計器の読み・確証なし）。〔履歴 InstrumentsTradeskip.cs#15〕
        /// </summary>
        void Shadow(TrEntry[] tr, int t, int sn, int sNat, double selP, double baseMul, double wS2, double lp, bool wedgeD, double bCostNow, double bCostBp)
        {
            var sc = CostOf(sn, tr); int k = ClsIx(sc.Cls);
            int o = ((curBn * Gc + curG) * ShCls + k) * ShCols;
            double landed = selP * (baseMul + wS2);
            double sRatio = sc.Now > 0 && sc.Bp >= 0 ? sc.Bp / sc.Now : 1.0, bRatio = bCostNow > 0 && bCostBp >= 0 ? bCostBp / bCostNow : 1.0;
            double landedBp = landed * sRatio, lpBp = lp * bRatio;
            bool nowPass = !(landed >= lp), bpPass = !(landedBp >= lpBp);
            Sh[o]++; if (nowPass) Sh[o + 1]++; if (wedgeD) Sh[o + 2]++; if (bpPass) Sh[o + 3]++;
            if (wedgeD && bpPass)
            {
                Sh[o + 4]++;
                if (!shRowShort[k])
                {
                    shRowShort[k] = true; var m = tr[t].r.Markets[curG];
                    double need = m.TargetStock + m.LastDemand - m.LastProduced - m.Stock;
                    if (need > 0) Sh[o + 5] += need * lp;
                }
            }
            double ex = ExRate.Length > curBn ? ExRate[curBn] : double.NaN, cn = bCostNow * ex, cb = bCostBp * ex;
            if (cn > 0 && cb > 0 && landed >= 0)
            {
                double rn = landed / cn, rb = landedBp / cb;
                Sh[o + 6]++; Sh[o + 7] += rn; Sh[o + 8] += rb; if (rn < 1.0) Sh[o + 9]++; if (rb < 1.0) Sh[o + 10]++;
            }
        }

        internal void End(long ledger) { CloseRow(); LedgerDelta = ledger - Ledger0; Ended = true; }
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<InternationalMarket, TsAcc> TsAccs = new();
    sealed class TsSink { public System.IO.StreamWriter? W, WPair, WShadow; }
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
        if (a.D.Length != nn * gc * DStride) a.D = new double[nn * gc * DStride]; else Array.Clear(a.D);   // DStride〔履歴 InstrumentsTradeskip.cs#16〕
        a.TarEff = new double[nn]; a.TariffNow = new double[nn]; a.Target = new double[nn]; a.Treaty = new double[nn]; a.ExRate = new double[nn];   // TarEff〔履歴 InstrumentsTradeskip.cs#17〕
        for (int i = 0; i < nn; i++)
        {
            var wi = nations[i].World; a.TarEff[i] = tar is not null ? tar[i] : double.NaN; a.ExRate[i] = exRate is not null ? exRate[i] : double.NaN;
            a.TariffNow[i] = wi.Tariff; a.Treaty[i] = 1.0 - Politics.ForeignPortShare(wi);
            double tg = Government.Q5(wi.Policy.TariffTarget) + wi.TariffRetaliationBias; a.Target[i] = tg < wi.Coef.TariffFloor ? wi.Coef.TariffFloor : tg > 1 ? 1 : tg;
        }
        a.Nn = nn; a.Gc = gc;
        a.PairOn = TsPairAllYears || w0.Year + 1 == 1870 || w0.Year + 1 == 1913;   // PairOn〔履歴 InstrumentsTradeskip.cs#18〕
        if (a.PairOn)
        {
            a.Worlds = new World[nn]; for (int i = 0; i < nn; i++) a.Worlds[i] = nations[i].World;
            a.Pair = new int[nn * nn * gc * TsAcc.PairCols]; a.Sh = new double[nn * gc * TsAcc.ShCls * TsAcc.ShCols];
        }
        else { a.Worlds = []; a.Pair = []; a.Sh = []; }
        a.Ledger0 = ledger; a.LedgerDelta = 0; a.Filled = true; a.Ended = false;
        return a;
    }

    /// <summary>番人用：蓄え（無ければ null）。</summary>
    internal static TsAcc? TsSnapshot(InternationalMarket m) => TsAccs.TryGetValue(m, out var a) && a.Filled ? a : null;

    internal static string FR(double v) => double.IsNaN(v) ? "nan" : double.IsInfinity(v) ? (v > 0 ? "inf" : "-inf") : v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);   // FR〔履歴 InstrumentsTradeskip.cs#19〕
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
                double ns = a.C[o + (int)TsCol.Scan]; int di = (i * a.Gc + g) * DStride;   // ns〔履歴 InstrumentsTradeskip.cs#20〕
                for (int k = 0; k < 3; k++) cols.Add(ns > 0 ? FR(a.D[di + k] / ns) : "0");
                cols.Add(I(a.C[o + (int)TsCol.WAT])); cols.Add(I(a.C[o + (int)TsCol.WAC])); cols.Add(I(a.C[o + (int)TsCol.WAB]));
                for (int c = (int)TsCol.GBin0; c < (int)TsCol.GBin0 + 40; c++) cols.Add(I(a.C[o + c]));
                cols.Add(I(a.C[o + (int)TsCol.RNaN])); cols.Add(FR(a.TarEff[i])); cols.Add(FR(a.TariffNow[i])); cols.Add(FR(a.Target[i])); cols.Add(FR(a.Treaty[i])); cols.Add(FR(a.ExRate[i]));
                for (int c = (int)TsCol.OgStock; c < (int)TsCol.Count; c++) cols.Add(I(a.C[o + c]));   // OgStock〔履歴 InstrumentsTradeskip.cs#21〕
                for (int k = 3; k < DStride; k++) cols.Add(FR(a.D[di + k]));   // DStride〔履歴 InstrumentsTradeskip.cs#22〕
                sk.W.WriteLine(string.Join('\t', cols));
            }
        }
        string zeros = string.Join('\t', Enumerable.Repeat("0", 32)), zerosNew = string.Join('\t', Enumerable.Repeat("0", TsNewCols));
        for (int g = 0; g < Catalog.Goods.Length && g < a.Gc; g++)
            sk.W.WriteLine(string.Join('\t', I(year), "ALL", Catalog.Goods[g].Name) + "\t" + zeros + "\t" + I(a.NoSell[g]) + "\t0\t" + zerosNew);
        sk.W.WriteLine(string.Join('\t', I(year), "ALL", "ALL") + "\t" + zeros + "\t0\t" + I(a.LedgerDelta) + "\t" + zerosNew);
        if (a.PairOn && a.Pair.Length > 0) TradeskipPairTables(sk, a, year, seed);
        a.Filled = false; a.Ended = false;
    }

    internal const string TskpairHeaderText = "year\tnation\tseller\tgood\txPairs\txSoldOut\txCap\txWedgeA\txWedgeB\txWedgeC\txWedgeD\txPass";
    internal const string TskshadowHeaderText = "year\tnation\tgood\tsellerClass\tpairs\tnowPass\tnowWedgeD\tbpPass\tdToBpPass\tdToBpShort\townN\townNowSum\townBpSum\townNowLt1\townBpLt1";

    /// <summary>
    /// 年が 1870 と 1913 の清算だけ。〔履歴 InstrumentsTradeskip.cs#23〕
    /// </summary>
    static void TradeskipPairTables(TsSink sk, TsAcc a, int year, ulong seed)
    {
        if (sk.WPair is null) { sk.WPair = Open("tskpair", seed); sk.WPair.WriteLine(TskpairHeaderText); }
        if (sk.WShadow is null) { sk.WShadow = Open("tskshadow", seed); sk.WShadow.WriteLine(TskshadowHeaderText); }
        string I(long v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int pc = TsAcc.PairCols;
        for (int b = 0; b < a.Nn; b++)
            for (int s = 0; s < a.Nn; s++)
                for (int g = 0; g < Catalog.Goods.Length && g < a.Gc; g++)
                {
                    int o = ((b * a.Nn + s) * a.Gc + g) * pc;
                    if (a.Pair[o] == 0) continue;
                    var sb = new System.Text.StringBuilder();
                    sb.Append(I(year)).Append('\t').Append(a.Names[b]).Append('\t').Append(a.Names[s]).Append('\t').Append(Catalog.Goods[g].Name);
                    for (int k = 0; k < pc; k++) sb.Append('\t').Append(I(a.Pair[o + k]));
                    sk.WPair.WriteLine(sb.ToString());
                }
        for (int b = 0; b < a.Nn; b++)
            for (int g = 0; g < Catalog.Goods.Length && g < a.Gc; g++)
                for (int k = 0; k < TsAcc.ShCls; k++)
                {
                    int o = ((b * a.Gc + g) * TsAcc.ShCls + k) * TsAcc.ShCols;
                    if (a.Sh[o] == 0) continue;
                    var sb = new System.Text.StringBuilder();
                    sb.Append(I(year)).Append('\t').Append(a.Names[b]).Append('\t').Append(Catalog.Goods[g].Name).Append('\t').Append(TsAcc.ClsNames[k]);
                    for (int j = 0; j < TsAcc.ShCols; j++) sb.Append('\t').Append(FR(a.Sh[o + j]));
                    sk.WShadow.WriteLine(sb.ToString());
                }
        a.Pair = []; a.Sh = [];
    }

    static void TradeskipClose(MultiNationSim sim) { if (TsSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; sk.WPair?.Dispose(); sk.WPair = null; sk.WShadow?.Dispose(); sk.WShadow = null; } }
}
