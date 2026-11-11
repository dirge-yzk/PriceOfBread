# (852) 合流：c69eh の記録の先頭 ①（事前登録 §8〈cpop_s の資本家〔popIndex 3〕の在庫〉）の同じ盤面の新÷旧（読むだけ・反証役が B1 で数えていないと指摘した量）。
#   python -I -X utf8 cpopcap852.py <出力> <腕の ins> <基線の ins> [<ヌル腕の ins> ...]
#   cpop_s の popIndex 3 の行の savings を（種・四半期・国・州）で突き合わせ、格（同じ種の cearn_s の tier）ごとに和の比と両方が正の組の比の中央を出す。ヌル腕を渡すと〈ヌル腕÷基線〉も同じ数で出す（幅）。
import sys, os, statistics as st
from collections import defaultdict
out, arm, base = sys.argv[1], sys.argv[2], sys.argv[3]; nulls = sys.argv[4:]
def tiers(d, s):
    t = {}
    with open(os.path.join(d, f'cearn_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); iq, inn, ir, it = h.index('quarter'), h.index('nation'), h.index('region'), h.index('tier')
        for ln in f:
            x = ln.rstrip('\n').split('\t'); t[(x[iq], x[inn], x[ir])] = x[it]
    return t
def load(d, s):
    v = {}
    with open(os.path.join(d, f'cpop_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); iq, inn, ir, ip, isv = (h.index(c) for c in ('quarter', 'nation', 'region', 'popIndex', 'savings'))
        for ln in f:
            x = ln.rstrip('\n').split('\t')
            if x[ip] != '3': continue
            try: v[(x[iq], x[inn], x[ir])] = float(x[isv])
            except ValueError: pass
    return v
L = ['which\ttier\tsumRatio\tmedianRatio\tnPairs\tnSeeds']
for name, d in [('arm', arm)] + [(f'null{i+1}', n) for i, n in enumerate(nulls)]:
    sa = defaultdict(float); sb = defaultdict(float); rs = defaultdict(list); ns = 0
    for s in range(1, 11):
        if not os.path.exists(os.path.join(d, f'cpop_s{s}.tsv')): continue
        ns += 1
        tb = tiers(base, s); b = load(base, s); a = load(d, s)
        for k, va in a.items():
            if k not in b: continue
            tr = tb.get(k, '?'); vb = b[k]
            sa[tr] += va; sb[tr] += vb
            if va > 0 and vb > 0: rs[tr].append(va / vb)
    for tr in sorted(sb):
        L.append(f'{name}\t{tr}\t{(sa[tr] / sb[tr]) if sb[tr] else float("nan"):.4g}\t{st.median(rs[tr]) if rs[tr] else float("nan"):.4g}\t{len(rs[tr])}\t{ns}')
open(out, 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n')
print('\n'.join(L))
