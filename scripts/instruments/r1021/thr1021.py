# (1021) c95cf：csc_s の行の種類ごとの律速の州の行と充填（読むだけ）
# 使い方：python -X utf8 -I thr1021.py <出力ファイル> <名:生のフォルダ:種の始め:種の終わり> ...
import os, sys
from collections import defaultdict
out = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
for spec in sys.argv[2:]:
    name, rest = spec.split(':', 1); d, s0, s1 = rest.rsplit(':', 2); s0, s1 = int(s0), int(s1); n = s1 - s0 + 1
    A = defaultdict(lambda: [0.0] * 6)   # rows thrRows fills thrFills dealtRows thrDealtRows
    for s in range(s0, s1 + 1):
        with open(os.path.join(d, f'csc_s{s}.tsv'), 'rb') as f:
            h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for ln in f:
                x = ln.decode('utf-8').rstrip('\r\n').split('\t')
                if x[ix['roleCode']] != '0': continue
                a = A[int(x[ix['classCode']])]
                for i, c in enumerate(['rowsCount', 'throttledRowsCount', 'fillsCount', 'throttledFillsCount', 'dealtRowsCount', 'throttledDealtRowsCount']):
                    a[i] += float(x[ix[c]])
    tf = sum(a[2] for a in A.values()); tt = sum(a[3] for a in A.values())
    print(f'== {name}：充填のうち律速の州の行の割合 {tt / tf:.4f}', file=out)
    for cl in sorted(A):
        r, tr, fl, tfl, dr, tdr = A[cl]
        print(f'  種類 {cl}: 律速の行の割合 {tr / r if r else 0:.4f} 律速の充填の割合 {tfl / fl if fl else 0:.4f} 律速の行の充填÷行 {tfl / tr if tr else 0:.2f} 非律速の充填÷行 {(fl - tfl) / (r - tr) if r - tr else 0:.2f} 律速の行の成約率 {tdr / tr if tr else 0:.3f}', file=out)
out.close()
