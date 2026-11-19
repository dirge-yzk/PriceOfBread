# (1229) c124pf：プローブ用に SimCore の写し（リポジトリの外）の BilateralMarket.cs へ値を読むだけの呼び口 ProbeDeal を 2 行足す（バイトで読み書き）。
# 使い方: python -I probe_pf_hook.py <写しの BilateralMarket.cs>
import sys
p = sys.argv[1]
b = open(p, 'rb').read()
a1 = b'    internal static bool DealConstAlwaysStale;\n'
a2 = b'                    msA.Stock -= qA; msA.LastExport += qA;\n'
assert b.count(a1) == 1 and b.count(a2) == 1, 'anchor'
b = b.replace(a1, a1 + b'    internal static System.Action<Region, Region, int, double, double, double, double, double>? ProbeDeal;\n')
b = b.replace(a2, a2 + b'                    ProbeDeal?.Invoke(tr[snA].r, tr[t].r, g, qA, unitA, frA, costA, sA);\n')
open(p, 'wb').write(b)
print('hooked')
