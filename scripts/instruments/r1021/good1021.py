# (1021) c95cf：tradeskip_s の財ごとの走査行・楔を通った越境の売り手・越境の成約の行（10 種の和・ALL の行を除く・読むだけ）
# 使い方：python -X utf8 -I good1021.py <出力ファイル> <生のフォルダ> <種の始め> <種の終わり>
import os, sys
from collections import defaultdict
outp, d, s0, s1 = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
G = defaultdict(lambda: [0.0, 0.0, 0.0, 0.0]); Y = defaultdict(lambda: [0.0, 0.0])
for s in range(s0, s1 + 1):
    with open(os.path.join(d, f'tradeskip_s{s}.tsv'), 'rb') as f:
        h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            x = ln.decode('utf-8').rstrip('\r\n').split('\t')
            if x[ix['nation']] == 'ALL' or x[ix['good']] == 'ALL': continue
            a = G[x[ix['good']]]
            for i, c in enumerate(('rowScan', 'xPass', 'rowDealtCross', 'rowIdle')): a[i] += float(x[ix[c]] or 0)
            Y[x[ix['year']]][0] += float(x[ix['rowScan']] or 0); Y[x[ix['year']]][1] += float(x[ix['xPass']] or 0)
T = [sum(v[i] for v in G.values()) for i in range(4)]
o = open(outp, 'w', encoding='utf-8', newline='\n')
print(f'生 {d} 種 {s0}〜{s1}・財 {len(G)}・和 rowScan {T[0]:.6g} xPass {T[1]:.6g} rowDealtCross {T[2]:.6g} rowIdle {T[3]:.6g}', file=o)
print('楔を通った越境の売り手（xPass）の上位 15 財：xPass の割合・rowScan の割合・xPass÷rowScan・越境の成約の行÷rowScan', file=o)
for g, v in sorted(G.items(), key=lambda kv: -kv[1][1])[:15]:
    print(f'  {g} {v[1] / T[1]:.4f} {v[0] / T[0]:.4f} {v[1] / v[0] if v[0] else 0:.1f} {v[2] / v[0] if v[0] else 0:.3f}', file=o)
print('調べる年ごとの rowScan・xPass（10 種の和）', file=o)
for y in sorted(Y): print(f'  {y} {Y[y][0]:.6g} {Y[y][1]:.6g}', file=o)
o.close()
