import sys, os, collections
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
Ls = [0.04, 0.05, 0.06, 0.07, 0.08, 0.10, 0.12]
n = 0; oldclosed = 0
openToClosed = collections.Counter(); closedToOpen = collections.Counter(); closedAll = collections.Counter()
byn = collections.defaultdict(collections.Counter)
dist = []
for sd in range(1, 11):
    for r in table(os.path.join(D, f'nation_s{sd}.tsv')):
        if 'revenueBaseMeanPerMonth' not in r or 'interestMeanPerMonth' not in r: continue
        rev = fl(r['revenueBaseMeanPerMonth']); it = fl(r['interestMeanPerMonth']); g = fl(r['nominalGdpPerYear'])/12
        if not (rev > 0 and g > 0 and it > 0): continue
        n += 1
        old = not (it < 0.67*rev)
        oldclosed += old
        dist.append(it/g)
        for L in Ls:
            new = not (it < L*g)
            closedAll[L] += new
            if old and not new: closedToOpen[L] += 1; byn[L][r['nation']] += 1
            if (not old) and new: openToClosed[L] += 1
print('N', n, 'old closed', oldclosed)
for L in Ls:
    print(f'L={L:.2f} 純GDP門 閉 {closedAll[L]} ({closedAll[L]/n:.4f})  閉→開 {closedToOpen[L]}  開→閉 {openToClosed[L]}')
dist.sort()
print('interest/GDP 分位 50/90/99/99.9:', [round(dist[int(len(dist)*p)],4) for p in (0.5,0.9,0.99,0.999)])
