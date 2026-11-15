# (1144) 事前登録 [6] の同じ盤面の新÷旧（記録）：10 種の和の比を年ごとに。読むだけ・エンジンの関数を呼ばない。
#   使い方: python -X utf8 -I newold.py 旧=<ins> 新=<ins> [--years 1830,1870,1913]
import sys, glob, os, collections, statistics as st

def rows(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for l in f:
            v = l.rstrip('\n').split('\t')
            if len(v) >= len(h): yield dict(zip(h, v))

def fl(x):
    try:
        v = float(x); return v if v == v and abs(v) != float('inf') else None
    except Exception: return None

def one(d, years):
    out = collections.defaultdict(float); price = collections.defaultdict(list)
    for p in glob.glob(os.path.join(d, 'nationgood_s*.tsv')):
        for r in rows(p):
            y = int(r['year'])
            if y not in years: continue
            for k in ('crossImportValue', 'crossImportQty', 'stockQty', 'dutyTariffPaid', 'producedQty'):
                v = fl(r.get(k))
                if v is not None: out[(k, y)] += v
            v = fl(r.get('priceMeanGold'))
            if v is not None and v > 0: price[(r['good'], y)].append(v)
    for p in glob.glob(os.path.join(d, 'trbin_s*.tsv')):
        for r in rows(p):
            y = int(r['year'])
            if y not in years: continue
            v = fl(r.get('freightGoldPerYear'))
            if v is not None: out[('freightGoldPerYear', y)] += v
    for p in glob.glob(os.path.join(d, 'disccontact_s*.tsv')):
        for r in rows(p):
            try: y = int(r['year'])
            except Exception: continue
            if y not in years: continue
            for k, v in r.items():
                if k in ('year', 'nation', 'q', 'seed'): continue
                x = fl(v)
                if x is not None and k.lower().find('import') >= 0: out[('disccontact.' + k, y)] += x
    return out, price

def main(a):
    years = [1830, 1870, 1913]; arms = []
    i = 0
    while i < len(a):
        if a[i] == '--years': years = [int(x) for x in a[i + 1].split(',')]; i += 2; continue
        arms.append(a[i].split('=', 1)); i += 1
    (n0, d0), (n1, d1) = arms
    o0, p0 = one(d0, years); o1, p1 = one(d1, years)
    keys = sorted(set(k for k, _ in o0) | set(k for k, _ in o1))
    for k in keys:
        print(k, ' '.join(f'{y} {o1[(k, y)] / o0[(k, y)]:.4f}' if o0[(k, y)] else f'{y} nan' for y in years))
    for y in years:
        r = [st.median(p1[(g, y)]) / st.median(p0[(g, y)]) for (g, yy) in p0 if yy == y and (g, y) in p1]
        if r: print('priceMeanGold 財ごとの中央の比の中央', y, f'{st.median(r):.4f}', '財の数', len(r))

if __name__ == '__main__':
    main(sys.argv[1:])
