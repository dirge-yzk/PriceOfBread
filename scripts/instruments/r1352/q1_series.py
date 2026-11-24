# (1352) 問い1・2：ある種の州の四半期の系列を腕ごとに並べる（読むだけ）
# 使い方: python q1_series.py <出力tsv> <種> <州,州,...> <腕名=フォルダ> ...
import sys, os, csv
out, seed, regs = sys.argv[1], sys.argv[2], sys.argv[3].split(',')
arms = [a.split('=', 1) for a in sys.argv[4:]]
cols = ['populationK','ruralPopulationK','opportunityIndex','holdingFactor','wageFactor','landFactor','employmentShare','pushFactor','housingFactor','wagePremium','arableLand','migrationOutKPerQuarter','migrationInKPerQuarter','crossBorderOutKPerQuarter','crossBorderInKPerQuarter','foodFillShare','earnedPerMonth','factoryOutputRefValuePerMonth','factoryActiveCount','soldierK','warIntensity']
with open(out, 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('\t'.join(['arm','seed','quarter','year','region']+cols+['pbImplied','stayApprox'])+'\n')
    for name, d in arms:
        p = os.path.join(d, 'region_s%s.tsv' % seed)
        sd = os.path.basename(p).split('_s')[-1].split('.')[0]
        with open(p, encoding='utf-8') as f:
            rd = csv.DictReader(f, delimiter='\t')
            for r in rd:
                if r['region'] not in regs: continue
                v = [r.get(c, '') for c in cols]
                try:
                    den = 1.0
                    for c in ['wageFactor','landFactor','employmentShare','pushFactor','housingFactor']: den *= float(r[c])
                    pb = float(r['opportunityIndex'])/den if den else float('nan')
                    st = float(r['opportunityIndex'])*float(r['holdingFactor'])*float(r['foodFillShare'])
                except Exception:
                    pb = st = float('nan')
                fo.write('\t'.join([name, sd, r['quarter'], r['year'], r['region']]+v+['%.4f'%pb,'%.4f'%st])+'\n')
