# (1010) c93ge 問い1：採取の財の産地（賦存>1）と非産地（0.4）の作った量の割合を、今の生（国×財）で出せる範囲で数える。
# 使い方: python -X utf8 -I ge_geo.py <world.json> <goods.json> <生のフォルダ> <種の範囲 1-10> <出力のファイル>
# 読む物：world.json（州と植民地の resources）・region_s（州→国の対応と農村人口・調べる年の q%4==3 の行）・nationgood_s（国×財の producedQty）。
# 出す物：
#  (P) 紙：財ごとの産地の州の数 k・州の総数 N・能力の重み（農村人口×倍率）での産地の割合（1783 の q0・種1・開幕の盤面）。
#  (N) 生：調べる年ごとに、国を〈その財の産地の州を 1 つ以上持つ国（E 国）〉と〈持たない国（0 国）〉に分け、producedQty の和の割合。
#      0 国の量＝確実に非産地の州が作った量（下限）。E 国の中の非産地の州の量は分けられない（州×財の産出の列が無い＝C3 へ）。
#      E 国の中は能力の重み（農村人口×倍率・採算と労働の配分を無視）で割った〈推〉を併記。
# 0.4 が当たる財＝採取（投入なし・役務でない）・植民地の財でない・硫黄でない（非産地は 0）・麦と野菜でない（主食の自給の組）・米でない（適地の外は除去・適地は主食）。
# ⚠金は産地の発見の行（Catalog.cs:798〜 Sites）があり、resources に無い州でも通常の賦存で作る＝金の share0 は下限でない。
# ジャガイモ・トウモロコシ・綿花は州の座標に依る（PotatoBelt・MaizeBelt・CottonSuitable）＝この台本は座標を持たない＝除く（名乗る）。
import sys, os, json, statistics as st
WJ, GJ, RAW, RNG, OUT = sys.argv[1:6]
STAPLE = len(sys.argv) > 6 and sys.argv[6] == 'staple'   # 第 6 引数 staple＝主食の組（麦・野菜・米・ジャガイモ・玉蜀黍）を、非産地の倍率 1.0 で数える（帯の外の 0.4 と米の除去は無視＝近似）
lo, hi = map(int, RNG.split('-')); SEEDS = range(lo, hi + 1)
YEARS = [1790, 1830, 1870, 1913, 1950]
w = json.load(open(WJ, encoding='utf-8-sig'))
goods = json.load(open(GJ, encoding='utf-8-sig'))
gname = {g['id']: g['name'] for g in goods}
COLONIAL = {21, 22, 23, 24, 25, 26, 66, 67, 83, 111, 127}   # Catalog.cs:373 IsColonial
MINERAL = {0, 1, 2, 3, 4, 5, 7, 8, 9, 10, 89, 107, 108, 110}  # Catalog.cs:344 IsMineral（id は goods.json と照合）
EXCL = {5: '硫黄＝非産地 0', 15: '主食の自給', 98: '主食の自給', 93: '米＝適地外は除去・適地は主食', 105: '座標に依る', 106: '座標に依る', 12: '座標に依る'}
rgo = [g['id'] for g in goods if not g['inputs'] and g['tier'] != 'Service']
G04 = [g for g in rgo if g not in COLONIAL and g not in EXCL]
LOWM = 0.4
if STAPLE: G04 = [15, 98, 93, 105, 106]; LOWM = 1.0
res = {}   # 州・植民地の名→resources
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
def endow(g): return 4.0 if g in MINERAL else 3.5   # Simulation.cs:685
o = open(OUT, 'w', encoding='utf-8', newline='\n')
o.write(f'# ge_geo raw={RAW} seeds={RNG}\n# 除いた財: ' + '・'.join(f'{gname[g]}({v})' for g, v in EXCL.items()) + '\n')
# (P) 開幕の紙（種 lo の q0）
rp = {}; unmatched = 0
for ix, r in rows(os.path.join(RAW, f'region_s{lo}.tsv')):
    if r[ix['quarter']] != '0': continue
    nm = r[ix['region']]
    if nm not in res: unmatched += 1
    rp[nm] = (r[ix['nation']], fl(r[ix['ruralPopulationK']]))
o.write(f'\n## (P) 開幕の紙（q0・種{lo}・州 {len(rp)}・world.json に名の無い州 {unmatched}）\n')
o.write('good\tk_endowed\tN_regions\tcapShareEndowed_rural\tnationsWithEndowed\n')
for g in G04:
    ke = [nm for nm in rp if g in res.get(nm, ())]
    ce = sum(rp[nm][1] * endow(g) for nm in ke); cn = sum(rp[nm][1] * LOWM for nm in rp if nm not in ke and nm in res)
    o.write(f'{gname[g]}\t{len(ke)}\t{len(rp)}\t{ce / (ce + cn) if ce + cn > 0 else float("nan"):.3f}\t{len({rp[nm][0] for nm in ke})}\n')
# (N) 生：年×財の 0 国の割合と E 国の中の推
acc = {}   # (g,y) -> list per seed of (share0, shareEst)
for s in SEEDS:
    pr = os.path.join(RAW, f'region_s{s}.tsv'); pn = os.path.join(RAW, f'nationgood_s{s}.tsv')
    if not (os.path.exists(pr) and os.path.exists(pn)): continue
    reg = {}   # y -> nation -> list of (region, rural)
    for ix, r in rows(pr):
        q = int(r[ix['quarter']]); y = int(r[ix['year']])
        if q % 4 != 3 or y not in YEARS or r[ix['activeFlag']] != '1': continue
        reg.setdefault(y, {}).setdefault(r[ix['nation']], []).append((r[ix['region']], fl(r[ix['ruralPopulationK']])))
    prod = {}
    for ix, r in rows(pn):
        y = int(r[ix['year']])
        if y not in YEARS: continue
        prod[(y, r[ix['nation']], r[ix['good']])] = fl(r[ix['producedQty']])
    for y in YEARS:
        if y not in reg: continue
        for g in G04:
            nm = gname[g]; tot = z = est = 0.0
            for nat, lst in reg[y].items():
                p = prod.get((y, nat, nm), 0.0); tot += p
                ce = sum(ru * endow(g) for c, ru in lst if g in res.get(c, ()))
                cn = sum(ru * LOWM for c, ru in lst if g not in res.get(c, ()))
                if ce <= 0: z += p; est += p
                else: est += p * cn / (ce + cn)
            if tot > 0: acc.setdefault((g, y), []).append((z / tot, est / tot, tot))
o.write('\n## (N) 生：非産地の作った量の割合（producedQty の和÷和・種の中央値 [最小,最大]）\n')
o.write('# share0＝産地の州を持たない国の量÷世界（確実に非産地＝下限）・shareEst＝E 国の中を能力の重みで割った〈推〉\n')
o.write('good\tyear\tshare0_med\tshare0_min\tshare0_max\tshareEst_med\tworldProd_med\tseeds\n')
for (g, y) in sorted(acc, key=lambda k: (gname[k[0]], k[1])):
    v = acc[(g, y)]; a = [x[0] for x in v]
    o.write(f'{gname[g]}\t{y}\t{st.median(a):.3f}\t{min(a):.3f}\t{max(a):.3f}\t{st.median([x[1] for x in v]):.3f}\t{st.median([x[2] for x in v]):.1f}\t{len(v)}\n')
o.close(); print('ok', OUT)
