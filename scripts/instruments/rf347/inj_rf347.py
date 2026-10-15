"""[計測の写しだけ] (347) 清算の割り当てと時間：StepBilateral の1回ごとの時間（Stopwatch）と割り当てたバイト数（スレッドごと）、
試合全体の割り当て量・GC の回数と止まった時間を、木の前後で同じ注入で取る（読むだけ）。
環境変数 POB_RF_DIR の下に rf347.txt を書く（プロセスの終わりに `名前 値` を1行ずつ）。未設定なら Stopwatch と割り当ての読みを足すだけ。
使い方：python inj_rf347.py <木の根>"""
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

/// <summary>[計測の写しだけ] (347) 清算の割り当てと時間。</summary>
public static class RF347
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_RF_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    public static long BilTicks, BilBytes, BilCalls;
    static readonly System.Diagnostics.Stopwatch Wall = System.Diagnostics.Stopwatch.StartNew();
    static RF347()
    {
        if (On) System.AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            var p = System.Diagnostics.Process.GetCurrentProcess();
            System.IO.File.WriteAllText(System.IO.Path.Combine(Dir!, "rf347.txt"),
                $"freq {System.Diagnostics.Stopwatch.Frequency}\nbilTicks {BilTicks}\nbilBytes {BilBytes}\nbilCalls {BilCalls}\n" +
                $"totalAllocBytes {System.GC.GetTotalAllocatedBytes(false)}\ngen0 {System.GC.CollectionCount(0)}\ngen1 {System.GC.CollectionCount(1)}\ngen2 {System.GC.CollectionCount(2)}\n" +
                $"gcPauseSec {System.GC.GetTotalPauseDuration().TotalSeconds:F3}\ncpuSec {p.TotalProcessorTime.TotalSeconds:F3}\nwallSec {Wall.Elapsed.TotalSeconds:F3}\n");
        };
    }
}
'''

edit('PriceOfBread.SimCore/InternationalMarket.cs', [
    ("        StepBilateral(nations, c, firstClearingOfQuarter);\n",
     "        long rf347t = System.Diagnostics.Stopwatch.GetTimestamp(); long rf347b = System.GC.GetAllocatedBytesForCurrentThread();\n"
     "        StepBilateral(nations, c, firstClearingOfQuarter);\n"
     "        RF347.BilTicks += System.Diagnostics.Stopwatch.GetTimestamp() - rf347t; RF347.BilBytes += System.GC.GetAllocatedBytesForCurrentThread() - rf347b; RF347.BilCalls++;\n"),
], TAIL)
print('INJ_OK')
