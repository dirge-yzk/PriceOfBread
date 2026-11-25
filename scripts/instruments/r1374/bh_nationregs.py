# (1374) 読む線：1 国の州ごとに人口（年の印）と食の充足の平均・戦場の強さの平均（期間）。使い方 python bh_nationregs.py <ins> <種> <国> [年…]
import sys, collections, math
d, sd, nat = sys.argv[1], int(sys.argv[2]), sys.argv[3]
YS = [int(y) for y in sys.argv[4:]] or [1783, 1843, 1863, 1883, 1913, 1950]
P = collections.defaultdict(dict); F = collections.defaultdict(lambda: [0.0, 0]); W = collections.defaultdict(lambda: [0.0, 0]); T = {}; SEEN = set()
with open('%s/region_s%d.tsv' % (d, sd), encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
    for line in fh:
        c = line.rstrip('\n').split('\t')
        if c[ix['nation']] != nat: continue
        q = int(c[0]); reg = c[ix['region']]
        if (q, reg) in SEEN: continue
        SEEN.add((q, reg)); yr = int(c[1]); T[reg] = c[ix['tier']]
        if q % 4 == 3 or q == 667: P[reg][yr] = float(c[ix['populationK']])
        if yr >= 1860:
            fv = float(c[ix['foodFillShare']]); wv = float(c[ix['warIntensity']])
            if math.isfinite(fv): F[reg][0] += fv; F[reg][1] += 1
            if math.isfinite(wv): W[reg][0] += wv; W[reg][1] += 1
print('州\t格\t' + '\t'.join(str(y) for y in YS) + '\t食の充足(1860-)\t戦場の強さ(1860-)')
for reg in sorted(P, key=lambda r: -P[r].get(1783, 0)):
    print('%s\t%s\t' % (reg, T[reg]) + '\t'.join('%.0f' % P[reg].get(y, float('nan')) for y in YS) + '\t%.3f\t%.3f' % (F[reg][0] / max(1, F[reg][1]), W[reg][0] / max(1, W[reg][1])))
