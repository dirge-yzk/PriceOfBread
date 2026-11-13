# (1101) 峠の線の旗 0 の数：trroute の陸直行（routeCode 0）で、両端の地形の組ごとの 換算 km÷実 km（額で重みづけた中央）と額の割合。
# 呼び方：python -X utf8 -I passbase.py <出力> <腕:フォルダ>...
import csv, sys, os
out = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
def wmed(xs):
    xs = sorted(xs); t = sum(w for _, w in xs); c = 0
    for v, w in xs:
        c += w
        if c >= t / 2: return v
    return float('nan')
for spec in sys.argv[2:]:
    arm, root = spec.split(':', 1)
    grp = {'mountainEnd': [], 'plainBoth': [], 'other': []}; val = {k: 0.0 for k in grp}
    for f in sorted(os.listdir(root)):
        if not f.startswith('trroute_s'): continue
        with open(os.path.join(root, f), encoding='utf-8', newline='') as fh:
            r = csv.reader(fh, delimiter='\t'); h = next(r); H = {k: i for i, k in enumerate(h)}
            for row in r:
                if row[H['sellerRegion']].startswith('(') or row[H['routeCode']] != '0': continue
                rk, ek, v = float(row[H['realLandKm']]), float(row[H['eqLandKm']]), float(row[H['valueGoldPerYear']])
                if not rk > 0: continue
                ts, tb = row[H['sellerTerrainCode']], row[H['buyerTerrainCode']]
                g = 'mountainEnd' if '5' in (ts, tb) else ('plainBoth' if ts == tb == '0' else 'other')
                grp[g].append((ek / rk, v)); val[g] += v
    tv = sum(val.values())
    for g in grp:
        out.write('\t'.join([arm, g, f'valueShare={100*val[g]/tv:.2f}%', f'eqOverRealWeightedMedian={wmed(grp[g]):.4g}', f'n={len(grp[g])}']) + '\n')
out.close()
