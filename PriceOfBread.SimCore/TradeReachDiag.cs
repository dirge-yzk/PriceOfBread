namespace PriceOfBread.SimCore;

public static class TradeReachDiag
{
    public static bool On;

    public static int RegTotal, RegLinked, RegFrozen, RegNoPortLink, RegNoCoord;
    public static int[] ActiveByNation = [], LinkedByNation = [];

    public static double ValueCross, ValueDomestic;
    public static long TxCross, TxDomestic;
    public static double KmValWeighted;
    public static readonly Dictionary<long, double> PairValue = new();
    public static double[] CrossByNation = [], DomByNation = [];
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
        if (seller == buyer) { ValueDomestic += value; TxDomestic++; DomByNation[buyer] += 2 * value; return; }
        ValueCross += value; TxCross++; CrossByNation[buyer] += value; CrossByNation[seller] += value;
        long k = (long)seller * 1000 + buyer;
        PairValue[k] = PairValue.TryGetValue(k, out double v) ? v + value : value;
    }

    public static void DealRef(int seller, int buyer, double refValue)
    {
        if (seller == buyer) { DomRefByNation[buyer] += 2 * refValue; return; }
        CrossRefByNation[buyer] += refValue; CrossRefByNation[seller] += refValue;
    }
}
