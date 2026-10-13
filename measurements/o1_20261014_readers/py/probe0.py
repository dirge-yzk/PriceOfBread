import sys, os, statistics, collections
sys.stdout.reconfigure(encoding='utf-8')
D = sys.argv[1]
NATS = ['イギリス','フランス','ネーデルラント','ポルトガル']
def table(p):
    with open(p, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            yield dict(zip(head, ln.rstrip('\n').split('\t')))
def fl(x):
    try: return float(x)
    except: return float('nan')
med = lambda xs: statistics.median([x for x in xs if x==x]) if any(x==x for x in xs) else float('nan')
res = {n: collections.defaultdict(list) for n in NATS}
for sd in range(1, 11):
    nat = {}
    for r in table(os.path.join(D, f'nation_s{sd}.tsv')):
        if r['nation'] in NATS:
            nat[(r['nation'], int(r['quarter']))] = r
    for n in NATS:
        for q in range(0, 40):
            r = nat.get((n, q))
            if not r: continue
            gdpm = fl(r['nominalGdpPerYear'])/12
            rev = fl(r['revenueBaseMeanPerMonth']); it = fl(r['interestMeanPerMonth'])
            debt = fl(r['govDebtStock'])
            res[n][q].append((it/rev if rev>0 else float('nan'), it/gdpm if gdpm>0 else float('nan'), rev/gdpm if gdpm>0 else float('nan'), debt/(gdpm*12) if gdpm>0 else float('nan')))
for n in NATS:
    print('==', n, ' (種の中央) q: 利払い/歳入  利払い/月GDP  歳入/GDP  債務/年GDP')
    for q in list(range(0, 12)) + [19, 39]:
        v = res[n][q]
        print(f'Q{q+1:<3}', ' '.join(f'{med([x[i] for x in v]):8.3f}' for i in range(4)))
