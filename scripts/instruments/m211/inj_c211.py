"""[計測の写しだけ] (211) 計算量の census：潤沢線の借りられる額が読む貸し手のプール（投資家の貯蓄の走査）の時間と件数（読むだけ）。
環境変数 POB_C211_DIR の下に c211.txt（プロセスの終わりに1行＝ticks calls pops frequency）。未設定なら走査の前後に1命令も足さない（On の判定だけ）。
使い方：python inj_c211.py <木の根>"""
import sys
root = sys.argv[1]


def edit(rel, pairs, tail=''):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    for old, new in pairs:
        assert s.count(old) == 1, (rel, old[:90], s.count(old))
        s = s.replace(old, new)
    s += tail
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))


TAIL = r'''

/// <summary>[計測の写しだけ] (211) 潤沢線の貸し手のプールの走査の census。</summary>
public static class C211
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_C211_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    public static long Ticks, Calls, Pops;
    static C211()
    {
        if (On) System.AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            System.IO.File.WriteAllText(System.IO.Path.Combine(Dir!, "c211.txt"), $"{Ticks} {Calls} {Pops} {System.Diagnostics.Stopwatch.Frequency}\n");
    }
}
'''
edit('PriceOfBread.SimCore/Fiscal.cs', [], TAIL)
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ("        double pool = Fiscal.LenderPool(w, c, gdp, CapitalMarket.InvestorSavings(w), out _);\n",
     "        long c211t = C211.On ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;\n"
     "        double c211inv = CapitalMarket.InvestorSavings(w);\n"
     "        if (C211.On) { System.Threading.Interlocked.Add(ref C211.Ticks, System.Diagnostics.Stopwatch.GetTimestamp() - c211t); System.Threading.Interlocked.Increment(ref C211.Calls); int c211n = 0; foreach (var c211r in w.Regions) c211n += c211r.Pops.Count; System.Threading.Interlocked.Add(ref C211.Pops, c211n); }\n"
     "        double pool = Fiscal.LenderPool(w, c, gdp, c211inv, out _);\n"),
])
print('ok c211', root)
