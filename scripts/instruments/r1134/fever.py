# (1134) 戦争熱の項の内訳と膨張意欲の上限への張り付き（warentry・国×四半期）。読むだけ。
import csv, sys, os
csv.field_size_limit(10**9); sys.stdout.reconfigure(newline='\n')
D = sys.argv[1]
K = ['sumFMil', 'sumFMilSpend', 'sumFNoble', 'sumFSoldier', 'sumFThreat', 'sumFDiversion']
for s in range(1, 11):
    r = csv.reader(open(os.path.join(D, f'warentry_s{s}.tsv'), encoding='utf-8', newline=''), delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
    tot = {k: 0.0 for k in K}; n = cap = 0; fv = []
    for x in r:
        if len(x) < len(h): continue
        n += 1
        for k in K: tot[k] += float(x[ix[k]] or 0)
        cap += float(x[ix['willNow']] or 0) >= 9.99
        fv.append(float(x[ix['feverNow']] or 0))
    T = sum(tot.values()) or 1; fv.sort()
    print(s, 'cap%', round(100 * cap / n, 2), 'fever q10/q50/q90', round(fv[n // 10], 3), round(fv[n // 2], 3), round(fv[9 * n // 10], 3), ' '.join(f'{k[4:]}={100*tot[k]/T:.1f}%' for k in K))
