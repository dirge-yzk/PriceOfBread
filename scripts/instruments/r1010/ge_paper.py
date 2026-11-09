# (1010) c93ge 問い3 の紙：案 A・B の単位費用の比（定数だけから）・硫黄の E・6 大国ごとに〈産地の州を持たない 0.4 の財〉の産出の額÷GDP と、その財の産地を持つ国。
# 使い方: python -X utf8 -I ge_paper.py <world.json> <goods.json> <生のフォルダ> <種の範囲> <年> <出力のファイル>
# 定数（ソースの行）：RgoVa 0.34（Catalog.cs:536）・FarmLaborPerOutput 0.4（Catalog.cs:534）・農業労働の参照の値段 0.5（Catalog.cs:517）・
#   賦存 4.0／3.5（Simulation.cs:685）・非産地 0.4（Simulation.cs:684）・E＝(k×4.0＋(N−k)×0.4)÷k（Simulation.cs:744・ScarceCountsIncludeColonies 1＝植民地も数える）。
# 単位費用（参照の単位）＝RgoVa÷倍率^指数 ＋ FarmLaborPerOutput×0.5（ApplyEndowment は MarginalCost だけを割る＝農業労働の投入は割らない・Simulation.cs:759〜:764）。
import sys, os, json, statistics as st
WJ, GJ, RAW, RNG, YEAR, OUT = sys.argv[1:7]; YEAR = int(YEAR)
lo, hi = map(int, RNG.split('-')); SEEDS = range(lo, hi + 1)
RGOVA, FLPO, FLP = 0.34, 0.4, 0.5
MAJ = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '清']
w = json.load(open(WJ, encoding='utf-8-sig')); goods = json.load(open(GJ, encoding='utf-8-sig'))
gname = {g['id']: g['name'] for g in goods}; gid = {v: k for k, v in gname.items()}
COLONIAL = {21, 22, 23, 24, 25, 26, 66, 67, 83, 111, 127}
MINERAL = {0, 1, 2, 3, 4, 5, 7, 8, 9, 10, 89, 107, 108, 110}
EXCL = {5, 15, 98, 93, 105, 106, 12}
G04 = [g['id'] for g in goods if not g['inputs'] and g['tier'] != 'Service' and g['id'] not in COLONIAL and g['id'] not in EXCL]
def uc(m, e): return RGOVA / (m ** e) + FLPO * FLP
o = open(OUT, 'w', encoding='utf-8', newline='\n')
o.write(f'# ge_paper raw={RAW} seeds={RNG} year={YEAR}\n## 単位費用（参照の単位）\ncase\tendowment\tunitCost\tratio_to_ref\n')
ref = uc(1.0, 0)
for lab, m, e in [('今（指数0）', 1.0, 0), ('産地 3.5・指数1', 3.5, 1), ('産地 4.0（鉱物）・指数1', 4.0, 1), ('非産地 0.4・指数1', 0.4, 1)]:
    o.write(f'{lab}\t{m}\t{uc(m, e):.4f}\t{uc(m, e) / ref:.3f}\n')
o.write(f'比 案B（非産地÷産地）\t3.5\t{ref / uc(3.5, 1):.3f}\t鉱物 {ref / uc(4.0, 1):.3f}\n')
o.write(f'比 案A（非産地÷産地）\t3.5\t{uc(0.4, 1) / uc(3.5, 1):.3f}\t鉱物 {uc(0.4, 1) / uc(4.0, 1):.3f}\n')
# 硫黄の E（植民地を数える）
k = N = 0
for n in w['nations']:
    for p in n['provinces']: N += 1; k += 5 in p['resources']
    for c in n.get('initialColonies', []): N += 1; k += 5 in c['resources']
E = (k * 4.0 + (N - k) * 0.4) / k
o.write(f'\n## 硫黄の総量保存の E（k={k}・N={N}）＝{E:.2f}・指数1 で産地の単位費用 {uc(E, 1):.4f}（参照の {uc(E, 1) / ref:.3f} 倍・自己費用の部分は 1/{E:.1f}）\n')
o.write(f'## 量の門だけ E を読み、費用の門は 4.0 を読む形なら {uc(4.0, 1):.4f}\n')
# 6 大国ごとの 0 国の財
res = {}; owner0 = {}
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
A = {}; HOLD = {}
for s in SEEDS:
    pr, pn, pg = (os.path.join(RAW, f'{t}_s{s}.tsv') for t in ('region', 'nationgood', 'nation'))
    if not all(os.path.exists(x) for x in (pr, pn, pg)): continue
    reg = {}
    for ix, r in rows(pr):
        if int(r[ix['quarter']]) % 4 != 3 or int(r[ix['year']]) != YEAR or r[ix['activeFlag']] != '1': continue
        reg.setdefault(r[ix['nation']], []).append(r[ix['region']])
    gdp = {}
    for ix, r in rows(pg):
        if int(r[ix['quarter']]) % 4 != 3 or int(r[ix['year']]) != YEAR or r[ix['nation']] not in MAJ: continue
        cl = fl(r[ix['clearingsPerYear']])
        if cl > 0: gdp[r[ix['nation']]] = fl(r[ix['nominalGdpPerYear']]) * fl(r[ix['fxRate']]) / cl
    for g in G04:
        hs = sorted(n for n, lst in reg.items() if any(g in res.get(c, ()) for c in lst))
        for h in hs: HOLD.setdefault(gname[g], {}).setdefault(h, 0); HOLD[gname[g]][h] += 1
    for ix, r in rows(pn):
        if int(r[ix['year']]) != YEAR or r[ix['nation']] not in MAJ: continue
        g = gid.get(r[ix['good']], -1)
        if g not in G04: continue
        n = r[ix['nation']]
        ke = sum(1 for c in reg.get(n, []) if g in res.get(c, ()))
        v = fl(r[ix['producedQty']]) * fl(r[ix['priceMeanGold']])
        if gdp.get(n, 0) > 0:
            A.setdefault((n, r[ix['good']]), []).append((v / gdp[n], ke, fl(r[ix['crossImportValue']]) / gdp[n], fl(r[ix['crossExportValue']]) / gdp[n]))
o.write(f'\n## {YEAR} 6 大国×0.4 の財：産出の額÷GDP・その国の産地の州の数（種の中央値）・越境の輸入÷GDP・輸出÷GDP（上位 8 財）\nnation\tgood\tprodOverGdp\tkInNation\timpOverGdp\texpOverGdp\n')
for n in MAJ:
    lst = sorted(((st.median([x[0] for x in v]), g, st.median([x[1] for x in v]), st.median([x[2] for x in v]), st.median([x[3] for x in v])) for (nn, g), v in A.items() if nn == n), reverse=True)[:8]
    for m, g, ke, im, ex in lst: o.write(f'{n}\t{g}\t{m:.4f}\t{ke:.0f}\t{im:.5f}\t{ex:.5f}\n')
o.write(f'\n## {YEAR} 産地の州を持つ国（国名=その国が産地を持った種の数）\n')
for g in sorted(HOLD): o.write(f'{g}\t' + ' '.join(f'{h}={c}' for h, c in sorted(HOLD[g].items(), key=lambda x: -x[1])) + '\n')
o.close(); print('ok', OUT)
