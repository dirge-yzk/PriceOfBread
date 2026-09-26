namespace PriceOfBread.SimCore;

/// <summary>純テレメトリ（2026-08-21・「そもそも相手に到達できているか」の実測）。
/// <see cref="On"/> が false の間は BilateralMarket から**1命令も呼ばれない**（分岐の中）＝挙動はビット恒等。
/// 力学は一切読まない（ここに書いた値を清算が参照する箇所は存在しない）。
/// ★2026-09-26（棚卸し econ-15）：旧経路（定数速度×自州規模＝TradeArbitrageClearing=0）専用だった計器
/// （量式分解 QWant 等・買い手の棄却理由 BuyerRows 等・枝刈り検証 ScanPruned 等・器の分解 RefScaleW 等・軽量モード Cheap）は
/// 旧経路ごと削除した（既定では常に0を黙って出していた）。残るのは州の到達性と成約の内訳（Deal/DealRef）だけ。
/// </summary>
public static class TradeReachDiag
{
    public static bool On;

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

    public static void Begin(int nn)
    {
        RegTotal = RegLinked = RegFrozen = RegNoPortLink = RegNoCoord = 0;
        if (ActiveByNation.Length != nn) { ActiveByNation = new int[nn]; LinkedByNation = new int[nn]; CrossByNation = new double[nn]; DomByNation = new double[nn]; CrossRefByNation = new double[nn]; DomRefByNation = new double[nn]; }
        else { Array.Clear(ActiveByNation); Array.Clear(LinkedByNation); Array.Clear(CrossByNation); Array.Clear(DomByNation); Array.Clear(CrossRefByNation); Array.Clear(DomRefByNation); }
        ValueCross = ValueDomestic = KmValWeighted = 0; TxCross = TxDomestic = 0;
        PairValue.Clear();
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
