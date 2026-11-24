# (1352) 州の人口の増減を 10 年ごとに分ける：州間の出・入・越境の出・入・残り（自然増減と兵ほか）
# 使い方: python q1_decomp.py <腕フォルダ> <種> <州>
import sys, csv, os
sys.stdout.reconfigure(encoding="utf-8")
d, seed, reg = sys.argv[1], sys.argv[2], sys.argv[3]
p = os.path.join(d, 'region_s%s.tsv' % seed)
rows = []
with open(p, encoding='utf-8') as f:
    for r in csv.DictReader(f, delimiter='\t'):
        if r['region'] == reg: rows.append(r)
rows.sort(key=lambda r: int(r['quarter']))
print('arm=%s seed=%s region=%s' % (os.path.basename(d), os.path.basename(p).split('_s')[-1].split('.')[0], reg))
print('yr0\tpop0\tpop1\tdPop\tout\tin\txout\txin\tresid\tfood\tearnPc\tfacOut\tsoldK\tnation')
def emit(a):
    n = a['n']; dp = a['pop1'] - a['pop0']; net = a['inn'] - a['out'] + a['xi'] - a['xo']
    print('%d\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.3f\t%.2f\t%.0f\t%.0f\t%s' % (a['yr0'], a['pop0'], a['pop1'], dp, a['out'], a['inn'], a['xo'], a['xi'], dp - net, a['food']/n, a['earn']/n, a['fac']/n, a['sold']/n, a['nat']))
acc = None
def fl(r, c): return float(r[c])
for i, r in enumerate(rows):
    y = int(r['year'])
    if acc is None or y // 10 != acc['dec']:
        if acc is not None:
            acc['pop1'] = fl(rows[i-1], 'populationK'); emit(acc)
        acc = dict(dec=y // 10, yr0=y, pop0=fl(r, 'populationK'), out=0, inn=0, xo=0, xi=0, food=0, earn=0, fac=0, sold=0, n=0, nat=r['nation'])
    acc['out'] += fl(r,'migrationOutKPerQuarter'); acc['inn'] += fl(r,'migrationInKPerQuarter')
    acc['xo'] += fl(r,'crossBorderOutKPerQuarter'); acc['xi'] += fl(r,'crossBorderInKPerQuarter')
    acc['food'] += fl(r,'foodFillShare'); pop = max(1e-9, fl(r,'populationK'))
    acc['earn'] += fl(r,'earnedPerMonth')/pop; acc['fac'] += fl(r,'factoryOutputRefValuePerMonth'); acc['sold'] += fl(r,'soldierK'); acc['n'] += 1
    acc['nat'] = r['nation']
if acc is not None:
    acc['pop1'] = fl(rows[-1], 'populationK'); emit(acc)
