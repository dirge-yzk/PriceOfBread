#!/usr/bin/env python3
# (868) c72tc 足した 2 列の読み（旗 0 の計器つき 10 種の生・読むだけ）。python -I cols868.py <ins のフォルダ> [種...]
#   kind ごとに importOldRuleBudgetStartPerQuarter と importBudgetStartPerQuarter の一致・食い違いの行数、
#   importBudgetExcessMaxPerClearing の分布（nan の行・正の行・最大）を数える。
import sys, os, math
d = sys.argv[1]
seeds = sys.argv[2:] or [str(i) for i in range(1, 11)]
tab = '\t'
tot = {}
exmax = -math.inf
rows = 0
for s in seeds:
    p = os.path.join(d, 'cearn_s%s.tsv' % s)
    if not os.path.exists(p):
        continue
    with open(p, 'rb') as f:
        lines = f.read().decode('utf-8').split('\n')
    h = lines[0].rstrip('\r').split(tab)
    ix = {c: i for i, c in enumerate(h)}
    ik, ib, io, ie = ix['importPayerKind'], ix['importBudgetStartPerQuarter'], ix['importOldRuleBudgetStartPerQuarter'], ix['importBudgetExcessMaxPerClearing']
    for l in lines[1:]:
        if not l.strip():
            continue
        c = l.rstrip('\r').split(tab)
        rows += 1
        k = int(c[ik]); b = float(c[ib]); o = float(c[io]); e = float(c[ie])
        same = (b == o) or (math.isinf(b) and math.isinf(o)) or abs(b - o) <= 1e-9 * max(1.0, abs(b), abs(o))
        t = tot.setdefault(k, [0, 0, 0, 0, 0])   # 行数・一致・食い違い・excess nan・excess ＞1e-9
        t[0] += 1
        t[1 if same else 2] += 1
        if math.isnan(e):
            t[3] += 1
        else:
            if e > 1e-9 * max(1.0, abs(b) if not math.isinf(b) else 1.0):
                t[4] += 1
            exmax = max(exmax, e)
print('seeds', ','.join(seeds), 'rows', rows)
for k in sorted(tot):
    t = tot[k]
    print('kind=%d rows=%d same=%d differ=%d excess_nan=%d excess_pos=%d' % (k, t[0], t[1], t[2], t[3], t[4]))
print('excess_max', exmax)
