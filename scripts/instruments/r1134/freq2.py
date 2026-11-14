# (1134) S24 の追加の頻度（呼び手の行・戦争熱・膨張意欲・敗戦ショック・遺恨）。読むだけ。
# 使い方: python -X utf8 -I freq2.py <生のフォルダ> <名>
import csv, sys, os, math
from collections import Counter, defaultdict
csv.field_size_limit(10**9)
D = sys.argv[1]; NAME = sys.argv[2]; SEEDS = range(1, 11)
sys.stdout.reconfigure(newline='\n')
def rows(t, s):
    p = os.path.join(D, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    r = csv.reader(open(p, encoding='utf-8', newline=''), delimiter='\t'); h = next(r)
    return {k: i for i, k in enumerate(h)}, (x for x in r if len(x) >= len(h))
def fl(x):
    try:
        v = float(x); return v if math.isfinite(v) else None
    except Exception: return None
acc = defaultdict(dict)
def add(k, s, v): acc[k][s] = v
def pc(a, b): return 100.0 * a / b if b else None
deplines = Counter()
for s in SEEDS:
    g = rows('dep', s)
    if g:
        ix, it = g
        for r in it:
            if r[ix['kindCode']] == '2': continue
            deplines[(r[ix['callerMember']], r[ix['callerLine']])] += 1
    g = rows('warentry', s)
    if g:
        ix, it = g; n = fz = fh = wz = 0
        for r in it:
            n += 1; f = fl(r[ix['feverNow']]) or 0; w = fl(r[ix['willNow']]) or 0
            fz += f > 1e-9; fh += f > 0.5; wz += w > 1e-9
        add('entry.fever>0.pct', s, pc(fz, n)); add('entry.fever>0.5.pct', s, pc(fh, n)); add('entry.will>0.pct', s, pc(wz, n))
    g = rows('scalars', s)
    if g:
        ix, it = g; n = d5 = 0
        for r in it:
            n += 1; d5 += (fl(r[ix['defeatShock']]) or 0) > 0.05
        add('sc.defeatShock>0.05.pct', s, pc(d5, n))
    g = rows('warpair', s)
    if g:
        ix, it = g; n = gr = ho = 0
        for r in it:
            n += 1; gr += (fl(r[ix['grudge']]) or 0) > 1e-9; ho += (fl(r[ix['hostility']]) or 0) > 1e-9
        add('pair.grudge>0.pct', s, pc(gr, n)); add('pair.hostility>0.pct', s, pc(ho, n))
print(f'# 生 {D}（{NAME}）')
for k in sorted(acc):
    v = sorted(x for x in acc[k].values() if x is not None)
    print(f'{k}\t{v[0]:.4g}\t{v[len(v)//2]:.4g}\t{v[-1]:.4g}\t{len(v)}' if v else f'{k}\tn/a')
print('# dep の呼び手（種 1〜10 の和）')
for (m, l), v in sorted(deplines.items()): print(f'dep\t{m}\t{l}\t{v}')
