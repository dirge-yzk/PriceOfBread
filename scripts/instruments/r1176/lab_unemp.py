# 読むだけ (1176)：labor_s の工場労働の失業（plainUnemploymentShare）を 2 つの母集合で数える。
#  (a) 国×年の最後の行（全ての国＝11-12 の母集合）(b) 年の最後の四半期の行だけ（(1169) s02_freq.py の母集合＝計器の国だけ）
# 使い方: python -X utf8 -I lab_unemp.py <生のフォルダ> <名>
import sys, os
B, NAME = sys.argv[1:3]
A = [0, 0, 0]; Bc = [0, 0, 0]
for s in range(1, 11):
    p = os.path.join(B, f'labor_s{s}.tsv')
    if not os.path.exists(p): continue
    f = open(p, encoding='utf-8'); h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    L = [l.rstrip('\r\n').split('\t') for l in f]
    last = {}; lastq = {}
    for c in L:
        k = (c[ix['nation']], int(c[ix['year']])); q = int(c[ix['quarter']])
        if k not in last or q > last[k][0]: last[k] = (q, float(c[ix['plainUnemploymentShare']]))
        y = int(c[ix['year']]); lastq[y] = max(lastq.get(y, -1), q)
    for q, u in last.values(): A[0] += 1; A[1] += u > 0.05; A[2] += u > 0.20
    endq = set(lastq.values())
    for c in L:
        if int(c[ix['quarter']]) in endq:
            u = float(c[ix['plainUnemploymentShare']]); Bc[0] += 1; Bc[1] += u > 0.05; Bc[2] += u > 0.20
print(f'{NAME} 全ての国 {A[0]}：5% 超 {A[1] / A[0]:.1%}・20% 超 {A[2] / A[0]:.1%}／計器の国だけ {Bc[0]}：5% 超 {Bc[1] / Bc[0]:.1%}・20% 超 {Bc[2] / Bc[0]:.1%}')
