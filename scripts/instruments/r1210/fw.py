# (1210) ② 1 つの州の系列を腕ごとに並べる（読むだけ）。用法: fw.py <州> <種> <出力> <名=生のフォルダ> ...
# region_s（人口・雇用・工場と採取の産出の参照値・移民）と mintreg_s（銀の産出・値段・雇用・品位）を年の最後の行で並べる。
import sys, csv, os
reg, seed, out = sys.argv[1], int(sys.argv[2]), sys.argv[3]
arms = [a.split('=', 1) for a in sys.argv[4:]]
YEARS = [1783, 1790, 1800, 1820, 1840, 1850, 1860, 1870, 1875, 1880, 1883, 1890, 1900, 1913, 1930, 1950]
RC = ['nation', 'tier', 'populationK', 'employmentShare', 'wageFactor', 'opportunityIndex', 'factoryOutputRefValuePerMonth', 'extractionOutputRefValuePerMonth',
      'factoryActiveCount', 'extractionActiveCount', 'extractionBankruptCount', 'migrationOutKPerQuarter', 'migrationInKPerQuarter', 'foodFillShare', 'earnedPerMonth']
MC = ['prodAg', 'priceAg', 'employed', 'grade', 'depositFrac', 'nProdLp', 'wLaborAg']

def last_by_year(path, key_col, key, cols):
    d = {}
    if not os.path.exists(path): return d
    with open(path, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            if x[ix[key_col]] != key: continue
            d[int(x[ix['year']])] = [x[ix[c]] if c in ix and ix[c] < len(x) else '' for c in cols]
    return d

def fmt(v):
    try:
        z = float(v)
        return f'{z:.4g}'
    except Exception:
        return v

with open(out, 'w', encoding='utf-8', newline='\n') as o:
    o.write(f'# 州={reg} 種={seed} 腕=' + ' '.join(f'{n}={p}' for n, p in arms) + '\n')
    o.write('arm\tyear\t' + '\t'.join(RC) + '\t' + '\t'.join(MC) + '\n')
    for n, p in arms:
        R = last_by_year(f'{p}/region_s{seed}.tsv', 'region', reg, RC)
        M = last_by_year(f'{p}/mintreg_s{seed}.tsv', 'region', reg, MC)
        pk = max(((float(v[2]), y) for y, v in R.items() if v[2] not in ('', 'NaN')), default=(None, None))
        o.write(f'# {n}: 年の行 {len(R)}・人口のピーク {pk[0]} ({pk[1]})・1950 {R.get(1950, ["", "", ""])[2]}\n')
        for y in YEARS:
            if y not in R and y not in M: continue
            o.write(f'{n}\t{y}\t' + '\t'.join(fmt(v) for v in R.get(y, [''] * len(RC))) + '\t' + '\t'.join(fmt(v) for v in M.get(y, [''] * len(MC))) + '\n')
print('ok', out)
