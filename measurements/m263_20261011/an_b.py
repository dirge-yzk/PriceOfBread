#!/usr/bin/env python
# (263) 線E2 B：補給崩壊（人口100以上・supplyFillShare<0.3）の国×四半期と、ポルトガル（種10）の帰属。読むだけ。
import sys, glob, os, re, collections
d = sys.argv[1]
nat_focus = sys.argv[2] if len(sys.argv) > 2 else 'ポルトガル'
seed_focus = int(sys.argv[3]) if len(sys.argv) > 3 else 10
def load(seed):
    with open(os.path.join(d, f'nation_s{seed}.tsv'), encoding='utf-8') as fh:
        hdr = fh.readline().rstrip('\n').split('\t')
        rows = [dict(zip(hdr, l.rstrip('\n').split('\t'))) for l in fh]
    return rows
tot = 0; per = {}; bynat = collections.Counter()
for f in glob.glob(os.path.join(d, 'nation_s*.tsv')):
    seed = int(re.search(r'nation_s(\d+)', f).group(1))
    n = 0
    for r in load(seed):
        try:
            if float(r['populationK']) >= 100 and float(r['supplyFillShare']) < 0.3:
                n += 1; bynat[(seed, r['nation'])] += 1
        except ValueError: pass
    per[seed] = n; tot += n
print('補給崩壊（人口100以上）国×四半期 合計', tot, '種ごと', dict(sorted(per.items())))
print('国×種の上位', bynat.most_common(8))
rows = [r for r in load(seed_focus) if r['nation'] == nat_focus]
print(f'\n{nat_focus} 種{seed_focus}：行', len(rows))
keys = ['quarter', 'year', 'populationK', 'supplyFillShare', 'foodMetShare', 'needMetShare', 'atWarMonths', 'civilWarMonths', 'govDebtStock',
        'exportsPerClearing', 'importsPerClearing', 'stateFacilityStartedCount', 'stateFacilityCompletedCount', 'borrowGateClosedMonths', 'payRateBelowOneMonths', 'coreRegions', 'colonialRegions']
print('\t'.join(keys))
prev_collapse = False
first = None
for r in rows:
    try: c = float(r['supplyFillShare']) < 0.3
    except ValueError: c = False
    if c and first is None: first = int(r['quarter'])
print('最初の崩壊の四半期', first)
lo = (first - 12) if first is not None else 0
for r in rows:
    q = int(r['quarter'])
    if first is not None and (q < lo or q > first + 16) and q % 8: continue
    print('\t'.join(r.get(k, '')[:14] for k in keys), '|', r.get('stateFacilityStartedGoods', ''))
