# (1014) c94pe 計算量の差の帰属（読むだけ）：tradeskip_s の清算の行の数（rows・rowIdle・rowNoNeed・rowBudget0・rowScan・xPairs）の 10 種の和を腕と基準で並べる。
# 使い方：python costattr1014.py <腕の生> <基準の生>
import os, sys
cols = ['rows', 'rowIdle', 'rowNoNeed', 'rowBudget0', 'rowScan', 'rowDealtCross', 'rowNoDeal', 'xPairs']
def tot(d):
    T = dict.fromkeys(cols, 0.0)
    for s in range(1, 11):
        with open(os.path.join(d, f'tradeskip_s{s}.tsv'), 'rb') as f:
            h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for ln in f:
                x = ln.decode('utf-8').rstrip('\r\n').split('\t')
                for c in cols: T[c] += float(x[ix[c]])
    return T
a, b = tot(sys.argv[1]), tot(sys.argv[2])
for c in cols: print(f"{c}: 基準 {b[c]:.4g} 腕 {a[c]:.4g} 腕÷基準 {a[c]/b[c] if b[c] else float('nan'):.4f}")
