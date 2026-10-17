import sys
root = sys.argv[1] + '/PriceOfBread.SimCore/'
def rw(name, edits):
    p = root + name
    b = open(p, 'rb').read()
    t = b.decode('utf-8')
    for old, new, cnt in edits:
        n = t.count(old)
        assert n == cnt, (name, old[:60], n)
        t = t.replace(old, new)
    open(p, 'wb').write(t.encode('utf-8'))
# PT class
open(root + 'PT.cs', 'w', encoding='utf-8', newline='\n').write('''
namespace PriceOfBread.SimCore;
public static class PT
{
    public static readonly long[] Ticks = new long[96];
    public static bool FlipR, FlipB; public static double Sink;
    [System.ThreadStatic] static long[]? last;
    static PT() { System.AppDomain.CurrentDomain.ProcessExit += (_, _) => Dump(); }
    public static void Start(int ch) { last ??= new long[8]; last[ch] = System.Diagnostics.Stopwatch.GetTimestamp(); }
    public static void Seg(int ch, int i) { last ??= new long[8]; long now = System.Diagnostics.Stopwatch.GetTimestamp(); System.Threading.Interlocked.Add(ref Ticks[i], now - last[ch]); last[ch] = now; }
    public static void Add(int i, long t) { System.Threading.Interlocked.Add(ref Ticks[i], t); }
    public static void Dump()
    {
        double f = 1.0 / System.Diagnostics.Stopwatch.Frequency;
        for (int i = 0; i < Ticks.Length; i++) if (Ticks[i] != 0) System.Console.Error.WriteLine($"PT[{i}] {Ticks[i] * f:F2}s");
    }
}
''')
HEAD = '        int ng = region.Markets.Length;\n        int np = region.Producers.Count;\n'
rw('EconomyStep.cs', [
 ('    public static void Run(Region region, Coefficients coef, double productivityMultiplier = 1.0, Bank? bank = null, World? world = null)\n    {\n' + HEAD,
  '    public static void Run(Region region, Coefficients coef, double productivityMultiplier = 1.0, Bank? bank = null, World? world = null)\n    {\n        PT.FlipR = !PT.FlipR; PT.Start(0);\n' + HEAD, 1),
 ('        var scratch = RentScratch(ng, np, region.Pops.Count);\n', '        PT.Seg(0, 0); var scratch = RentScratch(ng, np, region.Pops.Count); PT.Seg(0, 1);\n', 1),
 ('        // === ① 供給：能力＝労働', '        PT.Seg(0, 2);\n        // === ① 供給：能力＝労働', 1),
 ('        // === ② 工場の投入需要 ＋', '        PT.Seg(0, 3);\n        // === ② 工場の投入需要 ＋', 1),
 ('        // === ③ 各財の取引（消費＝予算制約', '        PT.Seg(0, 4);\n        // === ③ 各財の取引（消費＝予算制約', 1),
 ('        System.Array.Sort(consumeOrder, 0, ngoods,', '        PT.Seg(0, 5);\n        System.Array.Sort(consumeOrder, 0, ngoods,', 1),
 ('        // 交差弾力性（B-2）：主食内の相対価格の基準', '        PT.Seg(0, 6);\n        // 交差弾力性（B-2）：主食内の相対価格の基準', 1),
 ('        // === 工場の投入支払い：同じ額を', '        PT.Seg(0, 7);\n        // === 工場の投入支払い：同じ額を', 1),
 ('        // === ④ 所得（§9-C）：付加価値', '        PT.Seg(0, 8);\n        // === ④ 所得（§9-C）：付加価値', 1),
])
rw('BilateralMarket.cs', [
 ('        var swSec = MultiNationSim.TimeOn ? System.Diagnostics.Stopwatch.StartNew() : null;', '        PT.Seg(1, PT.FlipB ? 79 : 9); var swSec = MultiNationSim.TimeOn ? System.Diagnostics.Stopwatch.StartNew() : null;', 1),
 ('            sellers.Clear();\n', '            PT.Seg(1, 10);\n            sellers.Clear();\n', 1),
 ('            if (sellers.Count == 0) continue;\n            sellers.Sort((a, b)', '            PT.Seg(1, PT.FlipB ? 21 : 11);\n            if (sellers.Count == 0) continue;\n            sellers.Sort((a, b)', 1),
 ('            var left = ClearScratch.Fit(ref sc.Left, sellers.Count);', '            PT.Seg(1, 12);\n            var left = ClearScratch.Fit(ref sc.Left, sellers.Count);', 1),
 ('            buyers.Sort((x, y)', '            PT.Seg(1, 13);\n            buyers.Sort((x, y)', 1),
 ('            if (swSec is not null) { TimeListsTicks', '            PT.Seg(1, 14);\n            if (swSec is not null) { TimeListsTicks', 1),
 ('            costC[CostLedger.ClearingFills] += nFill;', '            PT.Seg(1, 15);\n            costC[CostLedger.ClearingFills] += nFill;', 1),
 ('        if (swSec is not null) TimeClearTicks += swSec.ElapsedTicks;', '        PT.Seg(1, 16);\n        if (swSec is not null) TimeClearTicks += swSec.ElapsedTicks;', 1),
])
rw('InternationalMarket.cs', [
 ('        StepBilateral(nations, c, firstClearingOfQuarter);', '        PT.FlipB = !PT.FlipB; PT.Start(1); StepBilateral(nations, c, firstClearingOfQuarter); PT.Seg(1, 17);', 1),
])
rw('Simulation.cs', [
 ('                if (priceDay) MarketFlow.StepDay(r, World.Coef, hFlow, cost, monthStartSpan);', '                if (priceDay) { long ptq = System.Diagnostics.Stopwatch.GetTimestamp(); MarketFlow.StepDay(r, World.Coef, hFlow, cost, monthStartSpan); PT.Add(20, System.Diagnostics.Stopwatch.GetTimestamp() - ptq); }', 1),
])
