# (1352) 州の食の内訳（fd）と人口の閉じ（popclose）を年ごとに並べる（基線の計器つきだけにある表）
# 使い方: python q1_food.py <腕フォルダ> <種> <州> <年0> <年1> <刻み年>
import sys, csv, os, collections
sys.stdout.reconfigure(encoding='utf-8')
d, seed, reg, y0, y1, st = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]), int(sys.argv[5]), int(sys.argv[6])
def rows(t):
    with open(os.path.join(d, '%s_s%s.tsv' % (t, seed)), encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            if r.get('region') == reg and y0 <= int(r['year']) <= y1: yield r
agg = collections.defaultdict(lambda: collections.defaultdict(float)); n = collections.defaultdict(lambda: collections.defaultdict(int))
for t, cols in [('fd', ['popK','stapleNeedPerMonth','marketNeedPerMonth','demandPerMonth','subPerMonth','fillNew','harvestMean','subsistenceMean','landFactor','belowNewK']),
                ('popclose', ['birthsK','deathsNormalK','deathsFamineK','migrationOutK','killedViolentK','killedOtherK','residualK']),
                ('subst', ['subPopK','farmCapPerMonth','farmSoldPerMonth','farmProducedPerMonth','marketShareOfNeed'])]:
    for r in rows(t):
        b = (int(r['year']) - y0) // st * st + y0
        for c in cols:
            try: agg[b][t + '.' + c] += float(r[c]); n[b][t + '.' + c] += 1
            except: pass
keys = sorted({k for b in agg for k in agg[b]})
sumk = {k for k in keys if k.startswith('popclose.')}
print('yr\t' + '\t'.join(k.split('.', 1)[1] for k in keys))
for b in sorted(agg):
    print('%d\t' % b + '\t'.join(('%.1f' % agg[b][k]) if k in sumk else ('%.3g' % (agg[b][k] / max(1, n[b][k]))) for k in keys))
