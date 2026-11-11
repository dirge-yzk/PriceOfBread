# (1029) 突然変異の適用：python -I mut.py <木> <M1..M5>（BilateralMarket.cs を直す）
import sys
tree, m = sys.argv[1], sys.argv[2]
p = tree + '/PriceOfBread.SimCore/BilateralMarket.cs'
b = open(p, 'rb').read().decode('utf-8')
M = {
 'M1': ("                mn = Math.Min(mn, k);", "                if (kmLand[ix]) mn = Math.Min(mn, k);"),   # 最小を陸路だけで取る
 'M2': ("double facL = 1 + marL + wS2 + 0.0;", "double facL = 1 + marL + wS2 + 0.3;"),   # risk を 0 にしない（危険料を足す）
 'M3': ("{ nSkipLB++; prevS = s2; s2 = nextS![s2]; continue; }", "{ nSkipLB++; break; }"),   # break にする
 'M4': ("            System.Array.Fill(lbTab, -1.0, 0, tc * nnLb);\n", ""),   # 表を作り直さない（持ち越す）
 'M5': ("internal static bool LbSkip(double delLB, double lp) => delLB >= lp;", "internal static bool LbSkip(double delLB, double lp) => delLB > lp * (1 - 1e-9);"),   # 余裕を逆に付ける
}
o, n = M[m]
assert b.count(o) == 1, (m, b.count(o))
b = b.replace(o, n)
if m == 'M4':
    o2 = "if (lbTab.Length < tc * nnLb) lbTab = ks.LbKm = new double[tc * nnLb];"
    assert b.count(o2) == 1
    b = b.replace(o2, "if (lbTab.Length < tc * nnLb) { lbTab = ks.LbKm = new double[tc * nnLb]; System.Array.Fill(lbTab, -1.0); }")
open(p, 'wb').write(b.encode('utf-8'))
