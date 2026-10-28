import csv, sys, os, glob, statistics as st
def rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for x in r:
            if len(x) == len(h): yield dict(zip(h, x))
def fl(x):
    try: return float(x)
    except: return float('nan')
def capser(d, seed):
    ser = {}
    for r in rows(f'{d}/factory_s{seed}.tsv'):
        y, q = int(fl(r['year'])), int(fl(r['quarter']))
        if q % 4 != 3: continue
        ser[y] = ser.get(y, 0.0) + fl(r['capacityRefValuePerMonth'])
    return ser
def bound(d, seed):
    out = {}
    for r in rows(f'{d}/nation_s{seed}.tsv'):
        y = int(fl(r['year']))
        a = out.setdefault(y//10*10, [0.0, 0.0])
        a[0] += fl(r['constructionDesiringRegionMonthsCount']); a[1] += fl(r['constructionBudgetBoundRegionMonthsCount'])
    return {k: (v[1]/v[0] if v[0] else float('nan')) for k, v in out.items()}
def slots(d, seed):
    out = {}
    p = f'{d}/slot_s{seed}.tsv'
    if not os.path.exists(p): return out
    for r in rows(p):
        y, q = int(fl(r['year'])), int(fl(r['quarter']))
        if q % 4 != 3: continue
        out[y] = out.get(y, 0.0) + fl(r['sumN'])
    return out
base, tags, seed = sys.argv[1], sys.argv[2].split(','), sys.argv[3]
A = capser(f'{base}/out_A{seed}', seed)
for t in tags:
    d = f'{base}/out_{t}'
    try: S = capser(d, seed)
    except Exception as e: print(t, 'ERR', e); continue
    ys = [y for y in sorted(S) if y in A and y % 10 == 8]
    print(t, 'cap B/A', ' '.join(f'{y}:{S[y]/A[y]:.2f}' for y in ys))
    N = slots(d, seed); 
    if N: print('   sumN', ' '.join(f'{y}:{N[y]:.0f}' for y in sorted(N) if y % 10 == 8))
    try: print('   budgetBound share by decade', ' '.join(f'{k}:{v:.2f}' for k, v in sorted(bound(d, seed).items())))
    except Exception as e: print('   bound ERR', e)
