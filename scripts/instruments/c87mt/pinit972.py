# (972) pinit_s を種ごとに数える（init の行・defer の行・init で実質 GDP ≦1e-9 の行・旗の列の値）。python -I pinit972.py <ins> <種の起点>
import os, sys, collections
d, sb = sys.argv[1], int(sys.argv[2])
tot = collections.Counter()
for s in range(sb, sb + 10):
    p = os.path.join(d, f'pinit_s{s}.tsv')
    if not os.path.exists(p): print(s, 'pinit_s なし'); tot['noFile'] += 1; continue
    c = collections.Counter(); fl = set(); dn = []
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for l in f:
            x = l.rstrip('\n').split('\t'); e = x[ix['event']]; c[e] += 1; fl.add(x[ix['moTrendInitNeedsGdpFlag']])
            g = float(x[ix['gdpRealPerMonth']])
            if e == 'init' and not (g > 1e-9): c['initGdpLe0'] += 1
            if e == 'defer': dn.append((x[ix['nation']], x[ix['time']], x[ix['year']]))
    tot.update(c)
    print(s, 'init', c['init'], 'defer', c['defer'], 'init で GDP≦1e-9', c['initGdpLe0'], '旗の列', sorted(fl), 'defer の国', dn[:5], sep='\t')
print('計', dict(tot))
