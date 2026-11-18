# (1199) 注：(1193) で欄（国民意識）が消えた・旧の生でだけ動く（中身は変えていない）
# (1155) 分離の圧と保持の内訳（格ごとの中央・上位 1%）。使い方: secparts.py <生のフォルダ> <種の最初> <種の最後>
import sys, os, csv, statistics as st
D, S0, S1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); sys.stdout.reconfigure(newline='\n')
cols = ['pressureIndex', 'holdShareIndex', 'holdBaseIndex', 'effHoldBeforeShareIndex', 'militancyMeanIndex', 'tariffGapIndex', 'tariffTermIndex', 'ethnicTermIndex', 'cultureDistanceIndex', 'consciousnessFactorShare', 'suffrageFactorShare', 'crowdFactorShare', 'legitimacyIndex', 'coerceIndex', 'capitalDistanceKm']
acc = {}
for s in range(S0, S1 + 1):
    p = os.path.join(D, f'secession_s{s}.tsv')
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            t = x[ix['tier']]; a = acc.setdefault(t, {c: [] for c in cols})
            for c in cols:
                try: a[c].append(float(x[ix[c]]))
                except: pass
for t, a in sorted(acc.items()):
    print(f'# tier={t}')
    for c in cols:
        v = sorted(a[c]); n = len(v)
        if n: print(f'  {c}: 中央 {v[n//2]:.4g}  上位1% {v[int(n*0.99)]:.4g}  最大 {v[-1]:.4g}')
