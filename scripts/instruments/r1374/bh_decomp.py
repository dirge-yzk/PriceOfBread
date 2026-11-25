# (1374) 読む線：1 州の人口の変化を 10 年ごとに分ける＝州間の移民・入植・国境越え・疫病の死（epi_s）・残り（自然増減の推）。
# 使い方 python bh_decomp.py <ins> <種> <州>
import sys, collections, math
d, sd, reg = sys.argv[1], int(sys.argv[2]), sys.argv[3]
def rows(p):
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t')
        for line in fh:
            yield dict(zip(h, line.rstrip('\n').split('\t')))
def f(x):
    try: v = float(x); return v if math.isfinite(v) else 0.0
    except ValueError: return 0.0
P = {}; M = collections.Counter(); C = collections.Counter(); X = collections.Counter(); W = collections.defaultdict(list); F = collections.defaultdict(list)
for r in rows('%s/region_s%d.tsv' % (d, sd)):
    if r['region'] != reg: continue
    q = int(r['quarter']); b = (int(r['year']) - 1783) // 10
    if q in P: continue
    P[q] = (int(r['year']), f(r['populationK']), f(r['ruralPopulationK']))
    M[b] += f(r['migrationOutKPerQuarter']) - f(r['migrationInKPerQuarter'])
    C[b] += f(r.get('settlerOutKPerQuarter', 0)) - f(r.get('settlerInKPerQuarter', 0))
    X[b] += f(r['crossBorderOutKPerQuarter']) - f(r['crossBorderInKPerQuarter'])
    W[b].append(f(r['warIntensity'])); F[b].append(f(r['foodFillShare']))
E = collections.Counter(); En = collections.Counter()
for r in rows('%s/epi_s%d.tsv' % (d, sd)):
    if r['region'] != reg: continue
    b = (int(r['year']) - 1783) // 10; E[b] += f(r['deadK']); En[b] += 1
qs = sorted(P); byb = collections.defaultdict(list)
for q in qs: byb[(P[q][0] - 1783) // 10].append(q)
print('期間\t始め\t終わり\t変化\t州間の純流出\t入植の純流出\t国境越えの純流出\t疫病の死\t疫病の件\t残り(自然の推)\t食の充足の平均\t戦場の強さの最大')
prev = None
for b in sorted(byb):
    q0, q1 = byb[b][0], byb[b][-1]
    p0 = P[prev][1] if prev is not None else P[q0][1]; p1 = P[q1][1]
    dP = p1 - p0; res = dP + M[b] + C[b] + X[b] + E[b]
    print('%d-%d\t%.1f\t%.1f\t%.1f\t%.1f\t%.1f\t%.1f\t%.1f\t%d\t%.1f\t%.3f\t%.3f' % (1783 + 10 * b, min(1950, 1792 + 10 * b), p0, p1, dP, M[b], C[b], X[b], E[b], En[b], res, sum(F[b]) / len(F[b]), max(W[b])))
    prev = q1
