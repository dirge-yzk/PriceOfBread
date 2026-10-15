"""[計測の写しだけ] (340) 計算量の単価の census：区間の時間（Stopwatch）と件数を、木の前後（基線の木と新しい木）で同じ注入で取る（読むだけ）。
環境変数 POB_CB_DIR の下に cb340.txt を書く（プロセスの終わりに `名前 値` を1行ずつ）。未設定なら区間の前後に Stopwatch の 1 命令だけ（GetTimestamp）足す。
区間：price＝MarketFlow.StepDay の全体（スレッド合算）／daily＝その中の日次の写像の3つの while（労働・役務・財）／bil＝InternationalMarket.Step（清算の全体）／
rows＝その中の買い手行の foreach（BilateralMarket）。件数：rowsSeen・fills・priceGoods・priceDailyRuns。
使い方：python inj_cb340.py <木の根>"""
import sys
root = sys.argv[1]


def edit(rel, pairs, tail=''):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    crlf = '\r\n' in s
    s = s.replace('\r\n', '\n')
    for old, new in pairs:
        assert s.count(old) == 1, (rel, old[:90], s.count(old))
        s = s.replace(old, new)
    s += tail
    if crlf: s = s.replace('\n', '\r\n')
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))


TAIL = r'''

/// <summary>[計測の写しだけ] (340) 計算量の単価の census（区間の時間と件数）。</summary>
public static class CB340
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_CB_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    public static long Price, Daily, Bil, Rows, RowsSeen, Fills, PriceGoods, PriceDailyRuns, PriceCalls;
    public static void AddPrice(long t0, long goods, long runs)
    {
        System.Threading.Interlocked.Add(ref Price, System.Diagnostics.Stopwatch.GetTimestamp() - t0);
        System.Threading.Interlocked.Add(ref PriceGoods, goods); System.Threading.Interlocked.Add(ref PriceDailyRuns, runs);
        System.Threading.Interlocked.Increment(ref PriceCalls);
    }
    public static void AddDaily(long t0) => System.Threading.Interlocked.Add(ref Daily, System.Diagnostics.Stopwatch.GetTimestamp() - t0);
    static readonly System.Diagnostics.Stopwatch Wall = System.Diagnostics.Stopwatch.StartNew();
    static CB340()
    {
        if (On) System.AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            var p = System.Diagnostics.Process.GetCurrentProcess();
            System.IO.File.WriteAllText(System.IO.Path.Combine(Dir!, "cb340.txt"),
                $"freq {System.Diagnostics.Stopwatch.Frequency}\nprice {Price}\ndaily {Daily}\nbil {Bil}\nrows {Rows}\nrowsSeen {RowsSeen}\nfills {Fills}\n" +
                $"priceGoods {PriceGoods}\npriceDailyRuns {PriceDailyRuns}\npriceCalls {PriceCalls}\ncpuSec {p.TotalProcessorTime.TotalSeconds:F3}\nwallSec {Wall.Elapsed.TotalSeconds:F3}\n");
        };
    }
}
'''

edit('PriceOfBread.SimCore/InternationalMarket.cs', [
    ("        StepBilateral(nations, c, firstClearingOfQuarter);\n",
     "        long cb340b = System.Diagnostics.Stopwatch.GetTimestamp();\n        StepBilateral(nations, c, firstClearingOfQuarter);\n        CB340.Bil += System.Diagnostics.Stopwatch.GetTimestamp() - cb340b;\n"),
], TAIL)
edit('PriceOfBread.SimCore/BilateralMarket.cs', [
    ("            foreach (var (t, lp) in buyers)\n",
     "            long cb340r = System.Diagnostics.Stopwatch.GetTimestamp();\n            foreach (var (t, lp) in buyers)\n"),
    ("            costC[CostLedger.ClearingFills] += nFill;",
     "            CB340.Rows += System.Diagnostics.Stopwatch.GetTimestamp() - cb340r; CB340.RowsSeen += buyers.Count; CB340.Fills += nFill;\n            costC[CostLedger.ClearingFills] += nFill;"),
])
edit('PriceOfBread.SimCore/MarketFlow.cs', [
    ("        int h = days;\n        double div = coef.PriceFlowDailyDiv;\n",
     "        long cb340p = System.Diagnostics.Stopwatch.GetTimestamp();\n        int h = days;\n        double div = coef.PriceFlowDailyDiv;\n"),
    ("                    double P = m.Price, RW = m.ReservationWage;\n                    int run = 0;\n",
     "                    long cb340dA = System.Diagnostics.Stopwatch.GetTimestamp();\n                    double P = m.Price, RW = m.ReservationWage;\n                    int run = 0;\n"),
    ("                    m.ReservationWage = RW;\n",
     "                    CB340.AddDaily(cb340dA);\n                    m.ReservationWage = RW;\n"),
    ("\n                    double P = P0;\n                    int run = 0;\n",
     "\n                    long cb340dB = System.Diagnostics.Stopwatch.GetTimestamp();\n                    double P = P0;\n                    int run = 0;\n"),
    ("\n                    m.Price = P;\n                    nRun += run; nSkip += h - run;\n",
     "\n                    CB340.AddDaily(cb340dB);\n                    m.Price = P;\n                    nRun += run; nSkip += h - run;\n"),
    ("\n                double P = P0;\n                int run = 0;\n",
     "\n                long cb340dC = System.Diagnostics.Stopwatch.GetTimestamp();\n                double P = P0;\n                int run = 0;\n"),
    ("\n                m.Price = P;\n                nRun += run; nSkip += h - run;\n",
     "\n                CB340.AddDaily(cb340dC);\n                m.Price = P;\n                nRun += run; nSkip += h - run;\n"),
    ("        if (cost is not null) cost[CostLedger.PriceGoods] += live.Length - nAsleep;",
     "        CB340.AddPrice(cb340p, live.Length - nAsleep, nRun);\n        if (cost is not null) cost[CostLedger.PriceGoods] += live.Length - nAsleep;"),
])
print('ok cb340', root)
