# (1047) c99ov 記録の台本（採否に使わない・読むだけ）
# 使い方: python -X utf8 record_ov.py <出力> 名前=insフォルダ ...（10 種の中央値を腕ごとに並べる）
# 列：
#   tradeGdp＝(exportsPerClearing＋importsPerClearing)÷2×12÷nominalGdpPerYear（nation_s・1913 の最後の行・国の和）＝6 大国と全ての国。清算は月次（年 12 回）。
#   lwTop＝sellr_s の 1913 で（国・財）ごとに労働の重みが最大の州の割合（重みの和に対して）の中央値（6 大国・重みの和＞0 の組）。
#   farmWage＝labor_s の farmRealPriceIndex（1913 の最後の行）の 6 大国の中央値（農業労働の実質の値段）。
#   年は 1870 と 1913。
import sys, os, statistics as st
from collections import defaultdict
MAJ = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '清']
OUT = sys.argv[1]; ARMS = [a.split('=', 1) for a in sys.argv[2:]]
YEARS = ('1870', '1913')


def last_rows(p, ycol, ncol):
    rows = {}
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            v = ln.rstrip('\n').split('\t')
            if v[ix[ycol]] in YEARS: rows[(v[ix[ycol]], v[ix[ncol]])] = v
    return rows, ix


def one(d, s):
    r = {}
    rows, ix = last_rows(os.path.join(d, 'nation_s%d.tsv' % s), 'year', 'nation')
    for y in YEARS:
        for sc in ('6大国', '世界'):
            t = g = 0.0
            for (yy, nat), v in rows.items():
                if yy != y or (sc == '6大国' and nat not in MAJ): continue
                try:
                    e = float(v[ix['exportsPerClearing']]); i = float(v[ix['importsPerClearing']]); gd = float(v[ix['nominalGdpPerYear']])
                except ValueError:
                    continue
                if gd > 0 and e == e and i == i: t += (e + i) / 2 * 12; g += gd
            r['tradeGdp_%s_%s' % (sc, y)] = t / g if g > 0 else float('nan')
    lw = defaultdict(lambda: defaultdict(float))
    with open(os.path.join(d, 'sellr_s%d.tsv' % s), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix2 = {k: i for i, k in enumerate(h)}
        for ln in f:
            v = ln.rstrip('\n').split('\t')
            if v[0] not in YEARS or v[ix2['ownerNation']] not in MAJ: continue
            w = float(v[ix2['laborWeight']])
            if w > 0: lw[(v[0], v[ix2['ownerNation']], v[ix2['good']])][v[ix2['region']]] += w
    for y in YEARS:
        L = [max(m.values()) / sum(m.values()) for k, m in lw.items() if k[0] == y and len(m) >= 2]
        r['lwTop_%s' % y] = st.median(L) if L else float('nan')
    rows3, ix3 = last_rows(os.path.join(d, 'labor_s%d.tsv' % s), 'year', 'nation')
    for y in YEARS:
        L = [float(v[ix3['farmRealPriceIndex']]) for (yy, nat), v in rows3.items() if yy == y and nat in MAJ]
        L = [x for x in L if x == x]
        r['farmWage_%s' % y] = st.median(L) if L else float('nan')
    return r


res = {}
for name, d in ARMS:
    res[name] = [one(d, s) for s in range(1, 11) if os.path.exists(os.path.join(d, 'sellr_s%d.tsv' % s))]
keys = list(res[ARMS[0][0]][0].keys())
o = ['# (1047) c99ov record_ov.py（記録・採否に使わない）' + ' '.join('%s=%s' % (a, b) for a, b in ARMS), 'measure\t' + '\t'.join(n for n, _ in ARMS)]
for k in keys:
    vals = []
    for n, _ in ARMS:
        L = [x[k] for x in res[n] if x[k] == x[k]]
        vals.append('%.5g' % st.median(L) if L else 'nan')
    o.append(k + '\t' + '\t'.join(vals))
with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
    f.write('\n'.join(o) + '\n')
print('\n'.join(o))
