# (1299) 問い2の材料：首都が植民地格の主権の国×四半期（q1c の class が orig_sov）を、本国格の州の有無で分け、首都の人口の割合を並べる。
# 使い方: python -X utf8 -I q2_runsum.py <q1 rows.tsv> <q1c tsv> <出力>
import sys, csv, collections
rowsp, q1cp, out = sys.argv[1], sys.argv[2], sys.argv[3]
def rd(p):
    with open(p, encoding='utf-8', newline='') as f:
        lines = [l for l in f if l.strip() and not l.startswith('#')]
    r = csv.reader(lines, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
    return [{k: row[i] for k, i in ix.items()} for row in r]
rows = {(r['seed'], r['nation'], r['quarter']): r for r in rd(rowsp)}
cls = rd(q1cp)
agg = collections.OrderedDict()
cnt = collections.Counter()
for c in cls:
    r = rows[(c['seed'], c['nation'], c['quarter'])]
    home = int(r['homeRegions'])
    key = (c['seed'], c['nation'], c['capital'], c['path'], c['class'])
    a = agg.setdefault(key, dict(n=0, y0=c['year'], y1=c['year'], capShare=[], homeShare=[], homeMin=99, homeMax=0))
    a['n'] += 1; a['y1'] = c['year']
    tp = float(r['totalPopK']); a['capShare'].append(float(r['capitalPopK']) / tp if tp > 0 else 0)
    a['homeShare'].append(float(r['homePopK']) / tp if tp > 0 else 0)
    a['homeMin'] = min(a['homeMin'], home); a['homeMax'] = max(a['homeMax'], home)
    cnt[(c['class'], 'home>0' if home > 0 else 'home=0')] += 1
with open(out, 'w', encoding='utf-8', newline='\n') as f:
    f.write('seed\tnation\tcapital\tpath\tclass\tquarters\ty0\ty1\tcapitalPopShareMedian\thomePopShareMedian\thomeRegionsMin\thomeRegionsMax\n')
    for (s, n, cap, p, cl), a in agg.items():
        cs = sorted(a['capShare']); hs = sorted(a['homeShare'])
        f.write(f"{s}\t{n}\t{cap}\t{p}\t{cl}\t{a['n']}\t{a['y0']}\t{a['y1']}\t{cs[len(cs)//2]:.3f}\t{hs[len(hs)//2]:.3f}\t{a['homeMin']}\t{a['homeMax']}\n")
    f.write('\n# 区分×本国格の州の有無の国×四半期\n')
    for k, v in sorted(cnt.items()): f.write(f'# {k[0]}\t{k[1]}\t{v}\n')
for k, v in sorted(cnt.items()): print(k[0], k[1], v)
