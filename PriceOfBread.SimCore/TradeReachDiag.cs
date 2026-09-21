namespace PriceOfBread.SimCore;

/// <summary>純テレメトリ（2026-08-21・「そもそも相手に到達できているか」の実測）。
/// <see cref="On"/> が false の間は BilateralMarket から**1命令も呼ばれない**（分岐の中）＝挙動はビット恒等。
/// 力学は一切読まない（ここに書いた値を清算が参照する箇所は存在しない）。
/// ⚠量式分解（QWant等）・買い手棄却理由（BuyerRows等）・枝刈り検証（ScanPruned等）は旧経路（TradeArbitrageClearing=0）専用＝
/// 既定 ON（S4裁定清算）ではこれらの経路を通らず常に0。ON で有効なのは Deal/DealRef と州の到達性のみ。
/// </summary>
public static class TradeReachDiag
{
    public static bool On;
    /// <summary>軽量モード（2026-08-21・器の反証）。true のとき O(買い手×売り手) の全探索（枝刈り検証）だけを飛ばす。
    /// 積算する統計は全て O(行数) の加算＝同じ走行で全四半期を測れる。力学は読まない＝挙動不変。</summary>
    public static bool Cheap;

    // ---- 器（scaleT）の分解＝RefPrice建て（2026-08-21・「器が6倍縮んだ」主張の反証）----
    //   scaleT = max(LastDemand, LastProduced, TargetStock) のどれが最大値を与えているかと、各成分の総額。
    public static double RefDemandW, RefProducedW, RefTargetW, RefScaleW; public static long WinD, WinP, WinT;
    // ---- 成約行の厳密分解（RefPrice建て・掛け算で閉じる）----
    //   q = access × TradeSpeed × gap × scaleT なので
    //   Σq×Ref = TradeSpeed × [Σ(access×gap×scaleT×Ref)] × クリップ率  ＝**残差なしの恒等式**。
    public static double QRefWant, QRefGot, ScaleRefDealt, AccGapScaleRefDealt, AccScaleRefDealt, GapScaleRefDealt;

    // ---- 州の到達性 ----
    public static int RegTotal, RegLinked, RegFrozen, RegNoPortLink, RegNoCoord;
    public static int[] ActiveByNation = [], LinkedByNation = [];

    // ---- 成約の内訳（同国＝統計から除外される取引 / 越境＝統計に載る取引）----
    public static double ValueCross, ValueDomestic;
    public static long TxCross, TxDomestic;
    public static double KmValWeighted;                      // 額加重の平均距離を出すための分子
    public static readonly Dictionary<long, double> PairValue = new();   // (売り手国*1000+買い手国) → 額（越境のみ）
    public static double[] CrossByNation = [], DomByNation = [];
    // 同じ取引を**参照価格**で評価した額（2026-08-21・numeraire反証用）。GDP＝Σ産出×RefPrice と同じ物差しに揃えた分子。
    public static double[] CrossRefByNation = [], DomRefByNation = [];

    // ---- 買い手ごとの棄却理由（1件＝1買い手×1財）----
    public static long BuyerRows, NoSeller, NoMargin, Dealt;
    // ---- 量の律速 ----
    public static double QWant, QGot;                        // left[best] で切られる前／後
    public static long QClipped;

    // ---- 量式の分解（2026-08-21・「1件あたりの量」主張の反証）----
    //   value = access × TradeSpeed × gap × scaleT × unit（BilateralMarket の OFF 経路の式）と書けるので
    //   Value/GDP = (市場額/GDP) × (成約が押さえる市場額シェア) × (成約市場額あたりの成約額) に厳密分解して各因子を別々に測る。
    //   ★世界＝avg算出ループの全(州,財)／全＝買い手帯に入った(買い手,財)行／成約＝実際に取引が立った行。
    public static double MarketValWorld, MarketRefWorld; public static long RowsWorld;   // 全(州×財)＝取引可能な市場の総額（RefWorld＝同じ量を参照価格で評価＝数量側だけの物差し）
    public static double LpOverAvgDealt;      // 成約行の lp/avg（gap＝lp/avg − 着値/avg の分解用）
    public static double MarketValAll, ScaleTAll; public static long RowsAll;      // 買い手候補行
    public static double MarketValDealt, ScaleTDealt, AccessDealt, GapDealt, UnitOverLp, ValueDealt; public static long RowsDealt;
    public static double SellOfferVal; public static long SellRows, SellFlowBound;   // 売り手が「出せる」額（=min(在庫,流量)×価格）

    // ---- 枝刈り（相手の絞り込み）の実測 ----
    public static long ScanPruned, ScanFull;                 // 実際に評価した売り手件数（枝刈りあり／全探索）
    public static long PruneWorseBest;                       // 枝刈りが最良を取り逃した回数
    public static long PruneKilledDeal;                      // 枝刈りのせいで**取引そのものが立たなかった**回数
    public static double PruneDeliverExcess;                 // 取り逃した分の着値の超過（相対・合計）
    public static double PruneQLost;                         // 取り逃しによる希望量の目減り（合計）

    public static void Begin(int nn)
    {
        RegTotal = RegLinked = RegFrozen = RegNoPortLink = RegNoCoord = 0;
        if (ActiveByNation.Length != nn) { ActiveByNation = new int[nn]; LinkedByNation = new int[nn]; CrossByNation = new double[nn]; DomByNation = new double[nn]; CrossRefByNation = new double[nn]; DomRefByNation = new double[nn]; }
        else { Array.Clear(ActiveByNation); Array.Clear(LinkedByNation); Array.Clear(CrossByNation); Array.Clear(DomByNation); Array.Clear(CrossRefByNation); Array.Clear(DomRefByNation); }
        ValueCross = ValueDomestic = KmValWeighted = 0; TxCross = TxDomestic = 0;
        PairValue.Clear();
        BuyerRows = NoSeller = NoMargin = Dealt = 0;
        QWant = QGot = 0; QClipped = 0;
        ScanPruned = ScanFull = 0; PruneWorseBest = PruneKilledDeal = 0; PruneDeliverExcess = PruneQLost = 0;
        MarketValWorld = MarketRefWorld = 0; RowsWorld = 0; LpOverAvgDealt = 0;
        MarketValAll = ScaleTAll = 0; RowsAll = 0;
        MarketValDealt = ScaleTDealt = AccessDealt = GapDealt = UnitOverLp = ValueDealt = 0; RowsDealt = 0;
        SellOfferVal = 0; SellRows = SellFlowBound = 0;
        RefDemandW = RefProducedW = RefTargetW = RefScaleW = 0; WinD = WinP = WinT = 0;
        QRefWant = QRefGot = ScaleRefDealt = AccGapScaleRefDealt = AccScaleRefDealt = GapScaleRefDealt = 0;
    }

    public static void Deal(int seller, int buyer, double value, double km)
    {
        KmValWeighted += value * km;
        // 越境は買い手側と売り手側の両方に積む（＝ExportsByGood+ImportsByGood と同じ両建て）。
        // 同国取引も比較可能にするため 2×（買い＝売りが同じ国に立つ）で積む。
        if (seller == buyer) { ValueDomestic += value; TxDomestic++; DomByNation[buyer] += 2 * value; return; }
        ValueCross += value; TxCross++; CrossByNation[buyer] += value; CrossByNation[seller] += value;
        long k = (long)seller * 1000 + buyer;
        PairValue[k] = PairValue.TryGetValue(k, out double v) ? v + value : value;
    }

    /// <summary>同じ成約を参照価格で評価して積む（純テレメトリ・Deal と同じ両建て規約）。</summary>
    public static void DealRef(int seller, int buyer, double refValue)
    {
        if (seller == buyer) { DomRefByNation[buyer] += 2 * refValue; return; }
        CrossRefByNation[buyer] += refValue; CrossRefByNation[seller] += refValue;
    }
}
