# (1374) 読む線：1 州の食の勘定（fd_s）と値段・輸入（cearn_s）を年の印ごとに（その年の 4 四半期の平均）。使い方 python bh_food.py <ins> <種> <州>
import sys, collections, math
d, sd, reg = sys.argv[1], int(sys.argv[2]), sys.argv[3]
YS = [1790, 1810, 1830, 1845, 1850, 1855, 1860, 1870, 1880, 1900, 1920, 1940, 1949]
def agg(p, cols):
    A = collections.defaultdict(lambda: collections.defaultdict(list))
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for line in fh:
            c = line.rstrip('\n').split('\t')
            if c[ix['region']] != reg: continue
            y = int(c[ix['year']])
            if y not in YS: continue
            for k in cols:
                try: v = float(c[ix[k]])
                except (ValueError, KeyError): continue
                if math.isfinite(v): A[y][k].append(v)
            for k in [k for k in cols if k.endswith('Good')]: A[y][k] = c[ix[k]]
    return A
FC = ['popK', 'stapleNeedPerMonth', 'marketNeedPerMonth', 'demandPerMonth', 'subPerMonth', 'fillNew', 'harvestMean', 'subsistenceMean', 'landFactor', 'clampBindShare']
CC = ['stapleGood', 'staplePrice', 'stapleMarginalCost', 'regionPriceLevel', 'importCrossRegionPerQuarter', 'importPaidPerQuarter', 'savingsPerCapitaOverNation', 'belowSelfCostShare', 'factoryOutputRefValuePerMonth']
A = agg('%s/fd_s%d.tsv' % (d, sd), FC); B = agg('%s/cearn_s%d.tsv' % (d, sd), CC)
m = lambda L: sum(L) / len(L) if isinstance(L, list) and L else float('nan')
print('年\t' + '\t'.join(FC) + '\t' + '\t'.join(CC))
for y in YS:
    print(str(y) + '\t' + '\t'.join('%.4g' % m(A[y][k]) for k in FC) + '\t' + '\t'.join((B[y][k] if k.endswith('Good') else '%.4g' % m(B[y][k])) for k in CC))
