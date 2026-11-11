# (1037) c98lw 記録の段（読むだけ）：同じ種・国・年の labor_s（(459) の lw の表）の列を腕÷基準で並べる（工員と農民の賃金の支払い・失業率・労働の値段）。
# 使い方: python record_lw.py <基準の計器の生> <腕の計器の生>
import sys, glob, os, statistics, collections
base, arm = sys.argv[1], sys.argv[2]
COLS = ['plainWageBillPerMonth', 'plainUnemploymentShare', 'plainRealPriceIndex', 'plainCapPerMonth', 'farmWageBillPerMonth', 'farmUnemploymentShare', 'farmRealPriceIndex', 'farmCapPerMonth']
def load(p):
    d = {}
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            if int(c[ix['quarter']]) % 4 != 3: continue
            d[(c[ix['nation']], int(c[ix['year']]))] = {k: c[ix[k]] for k in COLS if k in ix}
    return d
era = lambda y: '1783-1849' if y < 1850 else '1850-1899' if y < 1900 else '1900-1950'
acc = collections.defaultdict(list); diff = collections.defaultdict(list)
for pa in sorted(glob.glob(arm + '/labor_s*.tsv')):
    pb = os.path.join(base, os.path.basename(pa))
    if not os.path.exists(pb): continue
    A, B = load(pa), load(pb)
    for key, ra in A.items():
        rb = B.get(key)
        if rb is None: continue
        for k in COLS:
            try: va, vb = float(ra[k]), float(rb[k])
            except (KeyError, ValueError): continue
            if va != va or vb != vb: continue
            if k.endswith('UnemploymentShare'): diff[(k, era(key[1]))].append(va - vb)
            elif vb > 0 and va >= 0: acc[(k, era(key[1]))].append(va / vb)
print('# 腕÷基準（同じ種・国・年末の行の比の中央値と十分位の 1・9）。失業率は差（腕−基準）')
for k in COLS:
    for e in ('1783-1849', '1850-1899', '1900-1950'):
        v = acc.get((k, e)) or diff.get((k, e))
        if not v: continue
        q = statistics.quantiles(v, n=10) if len(v) >= 10 else [float('nan')] * 9
        print(f'{k}\t{e}\tN={len(v)}\t中央 {statistics.median(v):.4f}\t十分位1 {q[0]:.4f}\t十分位9 {q[8]:.4f}')
