# (1010) c93ge 問い1 後半と問い3 の紙：6 大国の財の層ごとの自給率（額の重み・和÷和）と、0.4 の財の非産地の産出の額÷GDP（案 A・B が輸入へ回しうる量の〈推〉の上限）。
# 使い方: python -X utf8 -I ge_self.py <world.json> <goods.json> <生のフォルダ> <種の範囲> <出力のファイル>
# 単位：nationgood の producedQty・demandQty は四半期末の州の LastProduced・LastDemand（月 1 回分）・crossImport* は清算 1 回分（月 1 回分）・priceMeanGold は州の値段の単純平均×FxRate。
#      GDP は nation_s の nominalGdpPerYear×fxRate÷clearingsPerYear（月 1 回分の金建て）。行は nationgood と同じ年の印・q%4==3。
# S1＝Σ min(産出,需要)×p ÷ Σ 需要×p ・ IMP＝Σ crossImportValue ÷ Σ(産出×p＋crossImportValue)・EXP＝Σ crossExportValue ÷ Σ 産出×p。
# NE04＝0.4 の財（ge_geo.py と同じ組）について、国の産出×p×〈非産地の能力の割合〉（州の農村人口×倍率の重み・〈推〉）÷GDP。
import sys, os, json, statistics as st
WJ, GJ, RAW, RNG, OUT = sys.argv[1:6]
lo, hi = map(int, RNG.split('-')); SEEDS = range(lo, hi + 1)
YEARS = [1790, 1830, 1870, 1913, 1950]
MAJ = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '清']
w = json.load(open(WJ, encoding='utf-8-sig')); goods = json.load(open(GJ, encoding='utf-8-sig'))
gid = {g['name']: g['id'] for g in goods}
COLONIAL = {21, 22, 23, 24, 25, 26, 66, 67, 83, 111, 127}
MINERAL = {0, 1, 2, 3, 4, 5, 7, 8, 9, 10, 89, 107, 108, 110}
EXCL = {5, 15, 98, 93, 105, 106, 12}
G04 = {g['id'] for g in goods if not g['inputs'] and g['tier'] != 'Service' and g['id'] not in COLONIAL and g['id'] not in EXCL}
res = {}
for n in w['nations']:
    for p in n['provinces']: res[p['city']] = set(p['resources'])
    for c in n.get('initialColonies', []): res[c['name']] = set(c['resources'])
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except: return 0.0
def rows(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for line in f: yield ix, line.rstrip('\n').split('\t')
def endow(g): return 4.0 if g in MINERAL else 3.5
TIERS = ['Staple', 'Daily', 'Luxury', 'Intermediate', 'Producer', 'Service']
R = {}; NE = {}; NEG = {}
for s in SEEDS:
    pn, pr, pg = (os.path.join(RAW, f'{t}_s{s}.tsv') for t in ('nationgood', 'region', 'nation'))
    if not all(os.path.exists(x) for x in (pn, pr, pg)): continue
    gdp = {}
    for ix, r in rows(pg):
        q = int(r[ix['quarter']]); y = int(r[ix['year']])
        if q % 4 != 3 or y not in YEARS or r[ix['nation']] not in MAJ: continue
        cl = fl(r[ix['clearingsPerYear']])
        if cl > 0: gdp[(y, r[ix['nation']])] = fl(r[ix['nominalGdpPerYear']]) * fl(r[ix['fxRate']]) / cl
    nef = {}   # (y,nation,g) -> 非産地の能力の割合
    reg = {}
    for ix, r in rows(pr):
        q = int(r[ix['quarter']]); y = int(r[ix['year']])
        if q % 4 != 3 or y not in YEARS or r[ix['nation']] not in MAJ or r[ix['activeFlag']] != '1': continue
        reg.setdefault((y, r[ix['nation']]), []).append((r[ix['region']], fl(r[ix['ruralPopulationK']])))
    for (y, nat), lst in reg.items():
        for g in G04:
            ce = sum(ru * endow(g) for c, ru in lst if g in res.get(c, ())); cn = sum(ru * 0.4 for c, ru in lst if g not in res.get(c, ()))
            nef[(y, nat, g)] = cn / (ce + cn) if ce + cn > 0 else 0.0
    a = {}; ne = {}; neg = {}
    for ix, r in rows(pn):
        y = int(r[ix['year']]); nat = r[ix['nation']]
        if y not in YEARS or nat not in MAJ: continue
        g = gid.get(r[ix['good']], -1); t = r[ix['tier']]
        p = fl(r[ix['priceMeanGold']]); pq, dq = fl(r[ix['producedQty']]), fl(r[ix['demandQty']])
        iv, ev = fl(r[ix['crossImportValue']]), fl(r[ix['crossExportValue']])
        for k in ((y, t), (y, 'ALL' if t != 'Service' else 'SVC')):
            x = a.setdefault(k, [0.0] * 5); x[0] += min(pq, dq) * p; x[1] += dq * p; x[2] += iv; x[3] += pq * p; x[4] += ev
        if g in G04:
            v = pq * p * nef.get((y, nat, g), 0.0)
            ne[(y, nat)] = ne.get((y, nat), 0.0) + v
            ne[(y, 'MAJ6')] = ne.get((y, 'MAJ6'), 0.0) + v
            neg[(y, r[ix['good']])] = neg.get((y, r[ix['good']]), 0.0) + v
            x = ne.setdefault((y, nat, 'imp04'), [0.0]); x[0] += iv
    for k, x in a.items():
        R.setdefault(k, []).append((x[0] / x[1] if x[1] > 0 else float('nan'), x[2] / (x[3] + x[2]) if x[3] + x[2] > 0 else float('nan'), x[4] / x[3] if x[3] > 0 else float('nan')))
    for y in YEARS:
        gm = sum(gdp.get((y, n), 0.0) for n in MAJ)
        for n in MAJ + ['MAJ6']:
            gg = gm if n == 'MAJ6' else gdp.get((y, n), 0.0)
            if gg > 0 and (y, n) in ne: NE.setdefault((y, n), []).append(ne[(y, n)] / gg)
        tot = sum(v for (yy, _), v in neg.items() if yy == y)
        for (yy, gn), v in neg.items():
            if yy == y and tot > 0: NEG.setdefault((y, gn), []).append(v / gm if gm > 0 else float('nan'))
def md(v): v = [x for x in v if x == x]; return (st.median(v), min(v), max(v)) if v else (float('nan'),) * 3
with open(OUT, 'w', encoding='utf-8', newline='\n') as o:
    o.write(f'# ge_self raw={RAW} seeds={RNG}・6 大国の和÷和・種の中央値 [最小,最大]\n## 財の層ごと\ntier\tyear\tS1_med\tS1_min\tS1_max\tIMP_med\tIMP_min\tIMP_max\tEXP_med\n')
    for t in TIERS + ['ALL', 'SVC']:
        for y in YEARS:
            v = R.get((y, t))
            if not v: continue
            s1, im, ex = md([x[0] for x in v]), md([x[1] for x in v]), md([x[2] for x in v])
            o.write(f'{t}\t{y}\t{s1[0]:.3f}\t{s1[1]:.3f}\t{s1[2]:.3f}\t{im[0]:.4f}\t{im[1]:.4f}\t{im[2]:.4f}\t{ex[0]:.4f}\n')
    o.write('\n## 0.4 の財の非産地の産出の額÷GDP（〈推〉＝能力の重みの割合を掛けた・案 A・B が輸入へ回しうる量の上限の目安）\nnation\tyear\tmed\tmin\tmax\n')
    for n in MAJ + ['MAJ6']:
        for y in YEARS:
            v = NE.get((y, n))
            if v: m = md(v); o.write(f'{n}\t{y}\t{m[0]:.4f}\t{m[1]:.4f}\t{m[2]:.4f}\n')
    o.write('\n## 6 大国の 0.4 の財ごと（非産地の産出の額÷6 大国の GDP・1913 と 1870 の上位）\ngood\tyear\tmed\n')
    for y in (1870, 1913):
        lst = sorted(((md(v)[0], gn) for (yy, gn), v in NEG.items() if yy == y), reverse=True)[:12]
        for m, gn in lst: o.write(f'{gn}\t{y}\t{m:.4f}\n')
print('ok', OUT)
