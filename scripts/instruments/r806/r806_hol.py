# (806) c12rd：1 州の空洞化の窓（ピーク→1/10 未満へ初めて落ちた四半期）の内訳（読むだけ・an_h2.py と独立に数える）。
#   python -I r806_hol.py <ins> <種> <州>
#   減り D＝ピークの人口−落ちた四半期の人口・移民の純の流出 Mig＝Σ(州の間の出−入＋入植の出−入＋越境の出−入)・残り＝D−Mig（出生−死亡＝戦争・飢え・病気・自然の減り）
#   持ち主の移り変わり（国・格）と、窓の中の戦場の強さ・主食の充足・雇用の 5 年ごとの値も出す。
import sys, csv
sys.stdout.reconfigure(encoding='utf-8')
d, seed, reg = sys.argv[1], sys.argv[2], sys.argv[3]
f = open(f'{d}/region_s{seed}.tsv', encoding='utf-8'); r = csv.reader(f, delimiter='\t'); h = {k: i for i, k in enumerate(next(r))}
R = {}
for row in r:
    if row[h['region']] == reg: R[int(row[h['quarter']])] = row
qs = sorted(R)
g = lambda q, c: float(R[q][h[c]])
peak = -1; pq = None; hit = None
for q in qs:
    p = g(q, 'populationK')
    if p > peak: peak, pq = p, q
    if hit is None and peak >= 1 and p < peak / 10: hit = q; break
print(f'種 {seed} {reg}：ピーク {peak:.1f} 千人（{R[pq][h["year"]]}・q{pq}）', end='')
if hit is None:
    lo = min(qs[qs.index(pq):], key=lambda q: g(q, 'populationK'))
    print(f'・1/10 未満に落ちない（ピークの後の最小 {g(lo, "populationK"):.1f}・{R[lo][h["year"]]}）'); hit = lo
else:
    print(f'→ {g(hit, "populationK"):.1f} 千人（{R[hit][h["year"]]}・q{hit}）')
win = [q for q in qs if pq < q <= hit]
D = peak - g(hit, 'populationK')
mig = sum(g(q, 'migrationOutKPerQuarter') - g(q, 'migrationInKPerQuarter') for q in win)
col = sum(g(q, 'settlerOutKPerQuarter') - g(q, 'settlerInKPerQuarter') for q in win)
xb = sum(g(q, 'crossBorderOutKPerQuarter') - g(q, 'crossBorderInKPerQuarter') for q in win)
print(f'  減り D {D:.1f}・州の間の純の流出 {mig:.1f}・入植の純 {col:.1f}・越境の純 {xb:.1f}・移民の計 {mig + col + xb:.1f}（D の {(mig + col + xb) / D:.1%}）・残り（出生−死亡）{D - mig - col - xb:.1f}')
own = []
for q in qs:
    k = (R[q][h['nation']], R[q][h['tier']])
    if not own or own[-1][0] != k: own.append((k, R[q][h['year']], q))
print('  持ち主の移り変わり：' + ' → '.join(f'{n}・{t}（{y}）' for (n, t), y, q in own))
print('  year\tpop\tnetMigQ\twar\tfood\temp\twage\thous\tpush\tearnedM\tfacA\tfacB\textA\textB\towner')
for q in qs:
    if q < pq - 8 or q > hit + 4: continue
    if not (q % 20 == pq % 20 or q == hit or abs(q - hit) <= 2): continue
    nm = sum(g(q, a) - g(q, b) for a, b in (('migrationOutKPerQuarter', 'migrationInKPerQuarter'), ('settlerOutKPerQuarter', 'settlerInKPerQuarter'), ('crossBorderOutKPerQuarter', 'crossBorderInKPerQuarter')))
    print('  ' + '\t'.join([R[q][h['year']], f'{g(q, "populationK"):.1f}', f'{nm:.2f}'] + [f'{g(q, c):.3g}' for c in ('warIntensity', 'foodFillShare', 'employmentShare', 'wageFactor', 'housingFactor', 'pushFactor', 'earnedPerMonth', 'factoryActiveCount', 'factoryBankruptCount', 'extractionActiveCount', 'extractionBankruptCount')] + [R[q][h['nation']]]))
