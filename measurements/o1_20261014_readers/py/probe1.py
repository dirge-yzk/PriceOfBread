import sys, os, statistics, collections
sys.stdout.reconfigure(encoding='utf-8')
D = sys.argv[1]
def table(p):
    with open(p, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            yield dict(zip(head, ln.rstrip('\n').split('\t')))
def fl(x):
    try: return float(x)
    except: return float('nan')
KAPPA = [0.0, 0.06, 0.08, 0.10, 0.12]
tot = collections.Counter(); closed = {k: collections.Counter() for k in KAPPA}
bynat = collections.defaultdict(lambda: collections.Counter())
rows = 0
for sd in range(1, 11):
    for r in table(os.path.join(D, f'nation_s{sd}.tsv')):
        q = int(r['quarter'])
        if 'revenueBaseMeanPerMonth' not in r or 'interestMeanPerMonth' not in r: continue
        rev = fl(r['revenueBaseMeanPerMonth']); it = fl(r['interestMeanPerMonth']); g = fl(r['nominalGdpPerYear'])/12
        if not (rev > 0 and g > 0 and it > 0): continue
        n = r['nation']
        tot['all'] += 1; bynat[n]['n'] += 1
        for k in KAPPA:
            cap = max(rev, k*g)
            if not (it < 0.67*cap):
                closed[k]['all'] += 1; bynat[n][k] += 1
        if q < 8:
            tot['q8'] += 1
            for k in KAPPA:
                cap = max(rev, k*g)
                if not (it < 0.67*cap): closed[k]['q8'] += 1
print('国×四半期（債務あり）', tot['all'], ' Q1-8:', tot['q8'])
for k in KAPPA:
    print(f'kappa={k:.2f} 閉: 全 {closed[k]["all"]} ({closed[k]["all"]/tot["all"]:.4f})  Q1-8 {closed[k]["q8"]}')
print('-- 国別: kappa 0 と 0.10 の閉の数')
for n, c in sorted(bynat.items(), key=lambda kv: -(kv[1][0.0]-kv[1][0.10]))[:20]:
    print(n, c['n'], c[0.0], c[0.06], c[0.08], c[0.10], c[0.12])
