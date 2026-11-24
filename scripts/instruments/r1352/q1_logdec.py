# (1352) 残る値打ち stay＝機会×持ち分×食 の対数の変化を項ごとに分ける（年平均で 2 点を比べる）
# 使い方: python q1_logdec.py <q1_series.tsv> <腕> <州> <年,年,...>
import sys, csv, math
sys.stdout.reconfigure(encoding="utf-8")
src, arm, reg, yrs = sys.argv[1], sys.argv[2], sys.argv[3], [int(x) for x in sys.argv[4].split(',')]
terms = ['wageFactor','landFactor','employmentShare','pushFactor','housingFactor','holdingFactor','foodFillShare']
acc = {}
with open(src, encoding='utf-8') as f:
    for r in csv.DictReader(f, delimiter='\t'):
        if r['arm'] != arm or r['region'] != reg: continue
        y = int(r['year'])
        if y not in yrs: continue
        a = acc.setdefault(y, {t: 0.0 for t in terms + ['opportunityIndex','stayApprox','populationK','migrationOutKPerQuarter','wagePremium']} | {'n': 0})
        for t in a:
            if t != 'n': a[t] += float(r[t])
        a['n'] += 1
print('arm=%s region=%s' % (arm, reg))
print('year\t' + '\t'.join(['pop','opp','stay','out/q','wPrem'] + terms))
for y in yrs:
    a = acc[y]; n = a['n']
    print('%d\t' % y + '\t'.join('%.3f' % (a[t]/n) for t in ['populationK','opportunityIndex','stayApprox','migrationOutKPerQuarter','wagePremium'] + terms))
print('Δln（隣の年との差）')
for y0, y1 in zip(yrs, yrs[1:]):
    a, b = acc[y0], acc[y1]
    d = {t: math.log((b[t]/b['n'])/(a[t]/a['n'])) for t in terms + ['stayApprox','opportunityIndex']}
    print('%d→%d\tstay %+.3f\topp %+.3f\t' % (y0, y1, d['stayApprox'], d['opportunityIndex']) + '\t'.join('%s %+.3f' % (t[:5], d[t]) for t in terms))
