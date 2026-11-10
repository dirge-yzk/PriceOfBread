# (1015) c94rh 盤面の側：国×採取の財の産地（賦存>1 の州・産地の行）と産出・取り分・越境の輸入。
# 使い方: python -X utf8 -I rh_board.py <world.json> <goods.json> <Catalog.cs> <生のフォルダ> <種の範囲 1-10> <出力のファイル>
# 定義（prereg.txt §1）：産地＝州の world.json resources に財がある（Simulation.cs:830/:836 の枝＝賦存>1）か Catalog.Sites にその場所・財の行（:827 SiteRowFor）。
# 区分：0.4＝Simulation.cs:851／希少＝硫黄（Coefficients ScarceMineralOnlySulfur 1・:850）／植民地＝Catalog.cs:373 IsColonial／主食＝Climate.cs:38〜:42／綿花＝:819 の気候の門。
import sys, os, re, json, statistics as st
WJ, GJ, CAT, RAW, RNG, OUT = sys.argv[1:7]
lo, hi = map(int, RNG.split('-')); SEEDS = list(range(lo, hi + 1))
NINE = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '清', 'オーストリア', 'オスマン', '日本']
SUPP = ['スペイン', 'スウェーデン', 'ライン諸邦', 'ポーランド', 'ペルシャ', 'ネーデルラント']
ALLN = NINE + SUPP
w = json.load(open(WJ, encoding='utf-8-sig')); goods = json.load(open(GJ, encoding='utf-8-sig'))
gname = {g['id']: g['name'] for g in goods}; gid = {v: k for k, v in gname.items()}
COLONIAL = {gid[n] for n in ['生ゴム', 'パーム油', '象牙', 'サトウキビ', 'カカオ', '香辛料', '茶', 'コーヒー', 'アヘン', 'グアノ', '金鶏樹皮']}   # (1017) 合流：財の番号の直書きを名前へ（GoodIdGuardTests・同じ集合）
STAPLE = {gid[n] for n in ['麦', '野菜', '米', 'ジャガイモ', 'トウモロコシ']}
def cls(g):
    if g in COLONIAL: return '植民地'
    if g in STAPLE: return '主食'
    if g == gid['硫黄']: return '希少'
    if g == gid['綿花']: return '綿花'
    return '0.4'
EXT = [g['id'] for g in goods if not g['inputs'] and g['tier'] != 'Service']
# 州の名→resources（本国の州・植民地・抽象の標的の植民地）と 1783 の持ち主
RES = {}; HOME = {}; OWN0 = {}
for n in w['nations']:
    for p in n['provinces']: RES[p['city']] = set(p['resources']); HOME.setdefault(n['name'], []).append(p['city']); OWN0[p['city']] = (n['name'], 'P')
    for c in n.get('initialColonies', []): RES[c['name']] = set(c['resources']); OWN0[c['name']] = (n['name'], 'C')
for a in w.get('abstractTargets', []):
    for c in a.get('colonies', []): RES.setdefault(c['name'], set(c['resources'])); OWN0.setdefault(c['name'], (a['name'], 'A'))
# Catalog.Sites（Catalog.cs の new("名", "場所", XxxId, 年代, SiteKind.Y) の行）
IDS = {}
src = open(CAT, encoding='utf-8-sig').read()
for m in re.finditer(r'public const int (\w+Id)\s*=\s*(\d+);', src): IDS[m.group(1)] = int(m.group(2))
blk = src[src.index('public static readonly SiteRow[] Sites'):]
blk = blk[:blk.index('];')]
SITES = [(m.group(1), m.group(2), IDS[m.group(3)], int(m.group(4)), m.group(5)) for m in re.finditer(r'new\("([^"]+)",\s*"([^"]+)",\s*(\w+Id),\s*(\d+),\s*SiteKind\.(\w+)\)', blk)]
SITEAT = {(pl, g) for _, pl, g, _, _ in SITES}
def endowed(region, g):
    return (g in RES.get(region, ())) or ((region, g) in SITEAT)
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except: return 0.0
def rows(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for line in f: yield ix, line.rstrip('\n').split('\t')
YEARS = (1870, 1913)
held = {}      # (seed, year, nation) -> [region]
unmapped = {}  # region -> count
prod = {}      # (seed, year, nation, good) -> (produced, demand, crossImpQty, crossExpQty)
world = {}     # (seed, year, good) -> Σ produced（全ての国）
used = []
for s in SEEDS:
    pr, pn = (os.path.join(RAW, f'{t}_s{s}.tsv') for t in ('region', 'nationgood'))
    if not (os.path.exists(pr) and os.path.exists(pn)): continue
    used.append(s)
    for ix, r in rows(pr):
        y = int(r[ix['year']])
        if y not in YEARS or int(r[ix['quarter']]) % 4 != 3 or r[ix['activeFlag']] != '1': continue
        reg = r[ix['region']]
        if reg not in RES: unmapped[reg] = unmapped.get(reg, 0) + 1
        held.setdefault((s, y, r[ix['nation']]), []).append(reg)
    for ix, r in rows(pn):
        y = int(r[ix['year']])
        if y not in YEARS: continue
        g = gid.get(r[ix['good']], -1)
        if g not in EXT: continue
        p = fl(r[ix['producedQty']])
        world[(s, y, g)] = world.get((s, y, g), 0.0) + p
        if r[ix['nation']] in ALLN:
            prod[(s, y, r[ix['nation']], g)] = (p, fl(r[ix['demandQty']]), fl(r[ix['crossImportQty']]), fl(r[ix['crossExportQty']]))
def med(v): return st.median(v) if v else float('nan')
o = open(OUT, 'w', encoding='utf-8', newline='\n')
o.write(f'# rh_board raw={RAW} seeds={used} years={YEARS}（四半期 q%4==3・activeFlag 1）\n')
o.write('# Sites（Catalog.cs から読んだ行）: ' + ' / '.join(f'{nm}@{pl}:{gname[g]}:{d}' for nm, pl, g, d, _ in SITES) + '\n')
o.write('# 対応なしの州（world.json に名が無い・産地は 0 と数えた＝名乗り）: ' + ' '.join(sorted(unmapped)) + '\n')
o.write('nation\tgood\tclass\tP1783\tC1783\tsiteRows\theld1870_med\theld1870_rng\theld1913_med\theld1913_rng\tlost1913_seeds\tlostTo1913\tprod1870_med\tprod1913_med\twshare1870\twshare1913\timpOverDem1870\timpOverDem1913\texpOverProd1913\n')
for n in ALLN:
    homes = HOME.get(n, [])
    cols = [c for c, (nn, k) in OWN0.items() if nn == n and k == 'C']
    for g in EXT:
        P = sum(1 for c in homes if g in RES[c]); C = sum(1 for c in cols if g in RES[c])
        sr = [f'{pl}:{d}' for nm, pl, gg, d, _ in SITES if gg == g and pl in homes + cols]
        hv = {}; lost = 0; lostto = {}
        for y in YEARS:
            hv[y] = [sum(1 for reg in held.get((s, y, n), []) if endowed(reg, g)) for s in used]
        e83 = [c for c in homes + cols if endowed(c, g)]
        for s in used:
            mine = set(held.get((s, 1913, n), []))
            lst = [c for c in e83 if c not in mine]
            if lst:
                lost += 1
                for c in lst:
                    for (ss, yy, nn), regs in held.items():
                        if ss == s and yy == 1913 and c in regs: lostto[nn] = lostto.get(nn, 0) + 1
        pv = {y: [prod.get((s, y, n, g), (0, 0, 0, 0)) for s in used] for y in YEARS}
        ws = {y: med([pv[y][i][0] / world[(s, y, g)] for i, s in enumerate(used) if world.get((s, y, g), 0) > 0]) for y in YEARS}
        iod = {y: med([x[2] / x[1] for x in pv[y] if x[1] > 0]) for y in YEARS}
        eop = med([x[3] / x[0] for x in pv[1913] if x[0] > 0])
        if P == 0 and C == 0 and not sr and max(hv[1870] + hv[1913]) == 0 and med([x[0] for x in pv[1913]]) == 0: continue
        o.write(f'{n}\t{gname[g]}\t{cls(g)}\t{P}\t{C}\t{";".join(sr) or "-"}\t{med(hv[1870]):g}\t{min(hv[1870])}-{max(hv[1870])}\t{med(hv[1913]):g}\t{min(hv[1913])}-{max(hv[1913])}\t{lost}\t'
                + (','.join(f'{k}={v}' for k, v in sorted(lostto.items(), key=lambda x: -x[1])) or '-')
                + f'\t{med([x[0] for x in pv[1870]]):.4g}\t{med([x[0] for x in pv[1913]]):.4g}\t{ws[1870]:.4f}\t{ws[1913]:.4f}\t{iod[1870]:.4f}\t{iod[1913]:.4f}\t{eop:.4f}\n')
o.close(); print('ok', OUT, 'seeds', used, 'unmapped', len(unmapped))
