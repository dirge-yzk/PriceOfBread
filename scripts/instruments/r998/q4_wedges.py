# (998) 問い4の読める断片：(b) 運賃÷額（越境・国内・距離帯）・(c) 実効の関税（関税の払い÷輸入額）・越境の額の割合・成約のある国の組の数。
# 使い方: python -X utf8 -I q4_wedges.py <生のフォルダ> <ラベル> <出力のファイル> <種の範囲>
import sys, os, statistics as st
RAW, LABEL, OUT, RNG = sys.argv[1:5]
lo, hi = map(int, RNG.split('-')); SEEDS = range(lo, hi + 1)
YEARS = [1790, 1830, 1870, 1913, 1948]
DEC = {1790: 1790, 1830: 1830, 1870: 1870, 1913: 1910, 1948: 1950}
MAJ = {'イギリス', 'フランス', 'ロシア', 'プロイセン', '清', 'アメリカ'}
def rows(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for line in f: yield ix, line.rstrip('\n').split('\t')
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except: return 0.0
def med(xs):
    xs = [x for x in xs if x == x]; return st.median(xs) if xs else float('nan')
R = {}
def put(k, v): R.setdefault(k, []).append(v)
for s in SEEDS:
    # trbin：越境と国内の額・運賃・距離帯
    p = os.path.join(RAW, f'trbin_s{s}.tsv')
    if os.path.exists(p):
        acc = {}
        for ix, r in rows(p):
            y = int(r[ix['year']])
            if y not in DEC.values(): continue
            cb, db = r[ix['crossBorderFlag']], r[ix['distanceBandCode']]
            v, fr = fl(r[ix['valueGoldPerYear']]), fl(r[ix['freightGoldPerYear']])
            for k in ((y, 'cb' + cb, 'all'), (y, 'cb' + cb, 'd' + db), (y, 'any', 'all')):
                a = acc.setdefault(k, [0.0, 0.0]); a[0] += v; a[1] += fr
        for k, a in acc.items():
            put(('bin_fr/val',) + k, a[1] / a[0] if a[0] > 0 else float('nan'))
            put(('bin_valshare',) + k, a[0] / acc[(k[0], 'any', 'all')][0] if acc.get((k[0], 'any', 'all'), [0])[0] > 0 else float('nan'))
    # trroute：国の組（上位 3000 の州の組から）
    p = os.path.join(RAW, f'trroute_s{s}.tsv')
    if os.path.exists(p):
        pairs = {}; alive = {}; rest = {}; tot = {}
        for ix, r in rows(p):
            y = int(r[ix['year']])
            if y not in DEC.values(): continue
            sn, bn = r[ix['sellerNation']], r[ix['buyerNation']]
            v, fr = fl(r[ix['valueGoldPerYear']]), fl(r[ix['freightGoldPerYear']])
            tot[y] = tot.get(y, 0.0) + v
            if r[ix['sellerRegion']].startswith('('): rest[y] = rest.get(y, 0.0) + v; continue
            if sn != bn and v > 0: pairs.setdefault(y, set()).add(tuple(sorted((sn, bn))))
            alive.setdefault(y, set()).update((sn, bn))
        for y in tot:
            n = len(alive.get(y, ())); np_ = len(pairs.get(y, ()))
            put(('route_pairs', y), np_); put(('route_nations_seen', y), n)
            put(('route_pairfrac_of_seen', y), np_ / (n * (n - 1) / 2) if n > 1 else float('nan'))
            put(('route_restshare', y), rest.get(y, 0.0) / tot[y] if tot[y] > 0 else float('nan'))
    # 実効の関税＝fiscal_s の ledgerTariffPerMonth（関税の台帳・月）の年の和 ÷ 年の輸入額（nation_s の importsPerClearing×clearingsPerYear の 4 四半期の平均）
    #  ⚠nation_s の tariffPaidInQuarter は政治資本の払い（Instruments.cs:2642）＝関税の額ではない＝使わない
    p = os.path.join(RAW, f'nation_s{s}.tsv'); pf = os.path.join(RAW, f'fiscal_s{s}.tsv')
    if os.path.exists(p) and os.path.exists(pf):
        imp = {}
        for ix, r in rows(p):
            y = int(r[ix['year']])
            if y not in YEARS: continue
            a = imp.setdefault((r[ix['nation']], y), [0.0, 0]); a[0] += fl(r[ix['importsPerClearing']]) * fl(r[ix['clearingsPerYear']]); a[1] += 1
        rev = {}; neg = {}
        for ix, r in rows(pf):
            y = int(r[ix['year']])
            if y not in YEARS: continue
            t = fl(r[ix['ledgerTariffPerMonth']]); k = (r[ix['nation']], y)
            rev[k] = rev.get(k, 0.0) + t
            if t < 0: neg[y] = neg.get(y, 0) + 1
        acc = {}
        for (n, y), (isum, cnt) in imp.items():
            if (n, y) not in rev or cnt == 0: continue
            iy = isum / cnt
            for sc in (['MAJ6'] if n in MAJ else []) + ['ALL'] + ([n] if n in MAJ else []):
                a = acc.setdefault((sc, y), [0.0, 0.0]); a[0] += rev[(n, y)]; a[1] += iy
        for (sc, y), a in acc.items(): put(('tariff_eff', sc, y), a[0] / a[1] if a[1] > 0 else float('nan'))
        for y in YEARS: put(('tariff_negmonths', y), neg.get(y, 0))
with open(OUT, 'w', encoding='utf-8', newline='\n') as o:
    o.write(f'# q4 {LABEL} raw={RAW} seeds={RNG}・種の中央値 [最小,最大]・和÷和\n')
    o.write('# 単位の注：ledgerTariffPerMonth は自国の通貨、importsPerClearing は金建て（fxRate≈1 は q3 で確かめた）\n')
    for k in sorted(R, key=str):
        v = [x for x in R[k] if x == x]
        if not v: continue
        o.write('\t'.join(map(str, k)) + f'\t{med(v):.4f}\t[{min(v):.4f},{max(v):.4f}]\tn={len(v)}\n')
print('ok', OUT)
