# (1238) c125rd：プローブ用に SimCore の写し（リポジトリの外）の BilateralMarket.cs へ、値を読むだけの呼び口と段ごとの出口の数え（TsExit 相当）を足す（バイトで読み書き）。
# c124pf の probe_pf_hook.py の ProbeDeal に加え、ジャマイカ（PrJam）×コーヒー（PrGood）だけを数える。エンジンの値は 1 つも書かない。
# 使い方: python -I probe_rd_hook.py <写しの BilateralMarket.cs>
import sys
p = sys.argv[1]
b = open(p, 'rb').read()
def ins_after(anchor, add):
    global b
    assert b.count(anchor) == 1, anchor[:60]
    b = b.replace(anchor, anchor + add)
def ins_before(anchor, add):
    global b
    assert b.count(anchor) == 1, anchor[:60]
    b = b.replace(anchor, add + anchor)

ins_after(b'    internal static bool DealConstAlwaysStale;\n',
    b'    internal static System.Action<Region, Region, int, double, double, double, double, double>? ProbeDeal;\n'
    b'    internal static Region? PrJam; internal static int PrGood = -1; internal static int PrJt = -1;\n'
    b'    internal static readonly long[] PrC = new long[32]; internal static readonly double[] PrS = new double[16];\n'
    b'    internal static readonly double[] PrGs = new double[512], PrGb = new double[512];\n')

# 段 1：入口（売り手の一覧に入ったか）
ins_before(b'            if (sellers.Count == 0) { ts?.NoSellers(g); continue; }\n',
    b'            if (g == PrGood && PrJam is not null)\n'
    b'            {\n'
    b'                PrJt = -1; for (int t9 = 0; t9 < tc; t9++) if (ReferenceEquals(tr[t9].r, PrJam)) { PrJt = t9; break; }\n'
    b'                PrC[0]++;\n'
    b'                if (PrJt < 0) PrC[1]++;\n'
    b'                else\n'
    b'                {\n'
    b'                    var m9 = mktOf[PrJt];\n'
    b'                    if (!tr[PrJt].linked) PrC[2]++;\n'
    b'                    else if (lpOf[PrJt] <= 1e-9) PrC[3]++;\n'
    b'                    else if (m9.Stock <= 1e-9) PrC[4]++;\n'
    b'                    else if (!(m9.Stock - m9.TargetStock > 1e-12)) PrC[5]++;\n'
    b'                    else { bool in9 = false; foreach (var s9 in sellers) if (s9.Node == PrJt) in9 = true; if (in9) PrC[6]++; else PrC[7]++; }\n'
    b'                    PrS[0] += capLeft[PrJt]; PrS[1] += tr[PrJt].cap; if (capLeft[PrJt] <= 1e-12) PrC[8]++;\n'
    b'                    PrS[2] += sellers.Count; int rk9 = 0; foreach (var s9 in sellers) { if (s9.Node == PrJt) break; rk9++; } PrS[6] += rk9;\n'
    b'                }\n'
    b'            }\n'
    b'            else if (g == PrGood) PrJt = -1;\n')

# 段 2：買い手の行が走査に入った・ジャマイカの下界が買い手の値段より下か・残り・容量
ins_after(b'                if (ts is not null) ts.Scan(tr, t, lp, left, capLeft, baseMul, wedge, duty, sellCheap, cornPen);\n',
    b'                if (g == PrGood && PrJt >= 0)\n'
    b'                {\n'
    b'                    PrC[9]++;\n'
    b'                    int pos9 = -1; for (int s9 = 0; s9 < sellers.Count; s9++) if (sellers[s9].Node == PrJt) { pos9 = s9; break; }\n'
    b'                    if (pos9 >= 0) { PrC[10]++; if (sellers[pos9].Price * lbMul < lp) PrC[11]++; if (left[pos9] <= 1e-12) PrC[12]++; if (capLeft[PrJt] <= 1e-12) PrC[13]++; if (PrJt == t) PrC[23]++; }\n'
    b'                }\n')

# 段 3：候補に入った（bestA の選びの前）
ins_after(b'                                candS[candN] = s2; candD![candN] = del2; candK![candN] = km2; candF![candN] = fac2;\n',
    b'                                if (g == PrGood && sn == PrJt) { PrC[14]++; if (!(del2 < lp)) PrC[15]++; if (del2 < bestDelA) PrC[16]++; }\n')

# 段 4：bestA がジャマイカの決まり直し＝X・Y・残り・予算・容量のどれが 0 か
ins_after(b'                    if (ts is not null) ts.Iter(X, Y, left[bestA], budget[t] / Math.Max(1e-12, costA), Math.Min(capLeft[t], capLeft[snA]) / Math.Max(1e-9, Wg));\n',
    b'                    if (g == PrGood && snA == PrJt && PrJt >= 0)\n'
    b'                    {\n'
    b'                        PrC[17]++;\n'
    b'                        double bq9 = budget[t] / Math.Max(1e-12, costA), cq9 = Math.Min(capLeft[t], capLeft[snA]) / Math.Max(1e-9, Wg);\n'
    b'                        if (!(X > 1e-12)) { PrC[18]++; if (xPrice <= xNeed) PrC[24]++; else PrC[25]++; }\n'
    b'                        else if (!(Y > 1e-12)) PrC[19]++;\n'
    b'                        else if (!(bq9 > 1e-12)) PrC[20]++;\n'
    b'                        else if (!(cq9 > 1e-12)) { PrC[21]++; if (capLeft[snA] <= 1e-12) PrC[26]++; }\n'
    b'                        else PrC[22]++;\n'
    b'                        PrS[7] += X; PrS[8] += Y; PrS[9] += yPrice; PrS[10] += bq9; PrS[11] += cq9;\n'
    b'                    }\n')

# 成約（c124pf の ProbeDeal と同じ）
ins_after(b'                    msA.Stock -= qA; msA.LastExport += qA;\n',
    b'                    ProbeDeal?.Invoke(tr[snA].r, tr[t].r, g, qA, unitA, frA, costA, sA);\n'
    b'                    if (g == PrGood && snA == PrJt) { PrC[27]++; PrS[12] += qA; }\n'
    b'                    if (PrJam is not null && (ReferenceEquals(tr[snA].r, PrJam) || ReferenceEquals(tr[t].r, PrJam))) { if (ReferenceEquals(tr[snA].r, PrJam)) PrGs[g] += qA * Wg; else PrGb[g] += qA * Wg; }\n')

# 段 5：潜在量→LastTradePotential・LastTradeThrottle（全ての財の清算の後の州の値）
ins_after(b'                tr[t].r.LastTradePotential = potential;\n',
    b'                if (ReferenceEquals(tr[t].r, PrJam)) { PrS[3] = potential; PrS[4] = cap; PrS[5] = tr[t].r.LastTradeThrottle; PrC[28]++; }\n')

open(p, 'wb').write(b)
print('hooked')
