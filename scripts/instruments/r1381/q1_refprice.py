# (1381) 読む線：prodcap_s の工場の産出と region_s の factoryOutputRefValuePerMonth から、財ごとの参照の値段を最小二乗で逆算する（RefPrices は表に無い・numpy なし）。使い方 python q1_refprice.py <ins> <種> <出力>
import sys, collections
d, sd, out = sys.argv[1], int(sys.argv[2]), sys.argv[3]
f = lambda s: float(s) if s not in ('', 'NaN') else 0.0
X = collections.defaultdict(lambda: collections.defaultdict(float)); goods = {}
with open('%s/prodcap_s%d.tsv' % (d, sd), encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); I = {k: i for i, k in enumerate(h)}
    for line in fh:
        c = line.rstrip('\n').split('\t')
        if c[I['isRgo']] != '0': continue
        g = c[I['good']]; goods.setdefault(g, len(goods))
        X[(c[I['q']], c[I['region']])][g] += f(c[I['output']])
Y = {}
with open('%s/region_s%d.tsv' % (d, sd), encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); I = {k: i for i, k in enumerate(h)}
    for line in fh:
        c = line.rstrip('\n').split('\t'); k = (c[I['quarter']], c[I['region']])
        if k in X and k not in Y: Y[k] = f(c[I['factoryOutputRefValuePerMonth']])
keys = [k for k in X if k in Y]; G = sorted(goods, key=goods.get); n = len(G)
A = [[X[k].get(g, 0.0) for g in G] for k in keys]; bb = [Y[k] for k in keys]
M = [[0.0] * n for _ in range(n)]; v = [0.0] * n
for row, y in zip(A, bb):
    nz = [(j, x) for j, x in enumerate(row) if x != 0.0]
    for j, x in nz:
        v[j] += x * y; Mj = M[j]
        for l, z in nz: Mj[l] += x * z
for j in range(n): M[j][j] += 1e-9 * (1 + M[j][j])
for c in range(n):
    piv = max(range(c, n), key=lambda r: abs(M[r][c])); M[c], M[piv] = M[piv], M[c]; v[c], v[piv] = v[piv], v[c]
    if abs(M[c][c]) < 1e-300: continue
    for r in range(c + 1, n):
        fct = M[r][c] / M[c][c]
        if fct == 0.0: continue
        Mr, Mc = M[r], M[c]
        for l in range(c, n): Mr[l] -= fct * Mc[l]
        v[r] -= fct * v[c]
sol = [0.0] * n
for c in range(n - 1, -1, -1):
    s = v[c] - sum(M[c][l] * sol[l] for l in range(c + 1, n)); sol[c] = s / M[c][c] if abs(M[c][c]) > 1e-300 else 0.0
rel = sorted(abs(sum(a * s for a, s in zip(row, sol)) - y) / max(1.0, abs(y)) for row, y in zip(A, bb))
with open(out, 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('# rows=%d goods=%d maxRelResid=%.3g medianRelResid=%.3g p95=%.3g\n' % (len(keys), n, rel[-1], rel[len(rel)//2], rel[int(len(rel)*0.95)]))
    fo.write('good\trefPriceEst\n')
    for g, s in zip(G, sol): fo.write('%s\t%.4f\n' % (g, s))
