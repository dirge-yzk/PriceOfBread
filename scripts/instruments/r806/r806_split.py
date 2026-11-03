# (806) c12rd：州の人口の減りを期間に分け、移民の純の流出と残り（出生−死亡）・戦場の強さ・主食の充足を並べる（読むだけ）。
#   python -I r806_split.py <ins> <種> <州> <年の区切り,カンマ区切り>
import csv, sys
sys.stdout.reconfigure(encoding='utf-8')
d, s, reg, cuts = sys.argv[1], sys.argv[2], sys.argv[3], [int(x) for x in sys.argv[4].split(',')]
r = csv.reader(open(f'{d}/region_s{s}.tsv', encoding='utf-8'), delimiter='\t'); h = {k: i for i, k in enumerate(next(r))}
R = {}
for row in r:
    if row[h['region']] == reg: R[int(row[h['quarter']])] = row
g = lambda q, c: float(R[q][h[c]]); qs = sorted(R)
for y0, y1 in zip(cuts, cuts[1:]):
    w = [q for q in qs if y0 <= int(R[q][h['year']]) < y1]
    if not w: continue
    p0, p1 = g(w[0], 'populationK'), g(w[-1], 'populationK')
    mig = sum(g(q, a) - g(q, b) for q in w[1:] for a, b in (('migrationOutKPerQuarter', 'migrationInKPerQuarter'), ('settlerOutKPerQuarter', 'settlerInKPerQuarter'), ('crossBorderOutKPerQuarter', 'crossBorderInKPerQuarter')))
    war = sum(g(q, 'warIntensity') for q in w) / len(w); fm = sum(g(q, 'foodFillShare') for q in w) / len(w); fmin = min(g(q, 'foodFillShare') for q in w)
    em = sum(g(q, 'earnedPerMonth') for q in w) / len(w)
    print(f'{y0}-{y1}\t{p0:.0f}→{p1:.0f}\t減り {p0 - p1:.0f}\t移民の純 {mig:.0f}\t残り {p0 - p1 - mig:.0f}\t戦場の強さの平均 {war:.3f}\t主食の充足 平均 {fm:.3f} 最小 {fmin:.3f}\t州の稼ぎ（月）平均 {em:.3g}\t持ち主 {R[w[-1]][h["nation"]]}')
