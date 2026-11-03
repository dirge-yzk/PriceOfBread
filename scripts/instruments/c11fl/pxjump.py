# (801) c11fl：止める線〈1 四半期に 100 倍以上・1e10 倍以上〉の価格の側の読み（読むだけ）。基準とヌル腕と腕を同じ台本で数え直して並べる（★(797)）。
#   ①pxmonth_s の priceLevelIndex の 3 行（3 か月）前との比の最大（挙げた 9 国・助走 wm と試合の頭 qm）  ②nation_s の priceLevel の 1 四半期の比（全ての国）。
#   使い方: python -I -X utf8 pxjump.py <名前>=<ins フォルダ> ...
import csv, glob, math, os, sys
K = 4   # 建国の直後の型＝その国（nationUid）の系列の最初の行から 4 四半期以内
for arg in sys.argv[1:]:
    name, d = arg.split('=', 1)
    seeds = sorted(int(os.path.basename(p)[len('nation_s'):-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))
    pxmax = 1.0; px100 = px1e10 = 0
    for s in seeds:
        p = os.path.join(d, f'pxmonth_s{s}.tsv')
        if not os.path.exists(p): continue
        with open(p, encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            hist = {}
            for x in r:
                try: v = float(x[ix['priceLevelIndex']])
                except ValueError: continue
                lst = hist.setdefault(x[ix['nation']], []); lst.append(v)
                if len(lst) > 3 and lst[-4] > 0 and math.isfinite(v):
                    ra = v / lst[-4]
                    pxmax = max(pxmax, ra)
                    if ra >= 100: px100 += 1
                    if ra >= 1e10: px1e10 += 1
    j100 = j1e10 = nonf = 0; found = 0; other = []
    for s in seeds:
        with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            iu, inm, iy, iq, ip = ix['nationUid'], ix['nation'], ix['year'], ix['quarter'], ix['priceLevel']
            prev = {}; first = {}
            for x in r:
                try: g = float(x[ip])
                except ValueError: g = float('nan')
                u = x[iu]; q = int(x[iq])
                if u not in first: first[u] = q
                if not math.isfinite(g): nonf += 1; continue
                if u in prev and prev[u] > 0:
                    ra = g / prev[u]
                    if ra >= 1e10: j1e10 += 1
                    if ra >= 100:
                        j100 += 1
                        if q - first[u] <= K: found += 1
                        else: other.append((s, x[inm], int(x[iy]), q, round(ra, 1)))
                prev[u] = g
    print(f'{name}\t種 {len(seeds)}\tpxmonth 3 か月比の最大 {pxmax:.4g}（100 倍以上 {px100}・1e10 倍以上 {px1e10}）\tnation_s priceLevel 四半期比：100 倍以上 {j100}（建国の直後 {found}・それ以外 {len(other)}）・1e10 倍以上 {j1e10}・非有限 {nonf}')
    for b in other[:8]: print('   それ以外', b)
