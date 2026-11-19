# (1245) 天候の読み直しの別の取り方：年の最後の四半期（quarter%4==3）だけの国の平均（年の初めのロールの前後を混ぜない）。使い方: <生> <種の始め> <終わり>
import sys, os, csv, math, collections
raw, a, b = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
def corr(x, y):
    n = len(x); mx, my = sum(x)/n, sum(y)/n
    sx = math.sqrt(sum((v-mx)**2 for v in x)); sy = math.sqrt(sum((v-my)**2 for v in y))
    return sum((u-mx)*(v-my) for u, v in zip(x, y))/sx/sy
for s in range(a, b+1):
    ny = collections.defaultdict(list)
    with open(os.path.join(raw, f'fd_s{s}.tsv'), encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            if int(x[ix['quarter']]) % 4 != 3: continue
            try: v = float(x[ix['weatherHarvestMean']])
            except Exception: continue
            if math.isfinite(v): ny[(x[ix['nation']], int(x[ix['year']]))].append(v)
    vals = {k: sum(v)/len(v) for k, v in ny.items()}; allv = list(vals.values())
    byn = collections.defaultdict(dict)
    for (n, y), v in vals.items(): byn[n][y] = v
    p, c = [], []
    for n, d in byn.items():
        m = sum(d.values())/len(d)
        for y, v in d.items():
            if y-1 in d: p.append(d[y-1]-m); c.append(v-m)
    n = len(allv); mu = sum(allv)/n; sd = math.sqrt(sum((v-mu)**2 for v in allv)/n)
    sk = sum((v-mu)**3 for v in allv)/n/sd**3
    print(f's{s} 国×年 {n}・標準偏差 {sd:.3f}・<0.8 {sum(v<0.8 for v in allv)/n:.2%}・歪度 {sk:.3f}・前年との相関 {corr(p,c):.3f}')
