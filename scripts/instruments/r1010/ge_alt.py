# (1010) c93ge 問い2：劣った代替の組ごとに、(a) 紙の単位の投入費（Σ投入×国の値段・nationgood の priceMeanGold）と (b) 設備の規模の割合（eqr_s・9 カ国）。
# 使い方: python -X utf8 -I ge_alt.py <出力のファイル> <基線の生> <基線の種> [<ラベル>=<ヌル腕の生>:<種の範囲> ...]
# (a) は基線の生だけ（nationgood はヌル腕に無い）。自己費用（FactoryVa 0.06）と労働・技師の投入（LaborPerOutput 0.5・AdvLaborPerOutput(財)）は
#     同じ財の既定と代替で同じ＝差に効かないので足さない。生産性（能力）は表の右に並べる（単位費用に入らない＝EconomyStep.cs:1118 の mc は MineralSelfCost＋InputCostPerUnit）。
# (b) eqr_s の scaleSum の和÷和（財の中の割合）・marginSum÷scaleSum（規模あたりの採算・単位費用ではない）。
import sys, os, statistics as st
OUT, BASE, BRNG = sys.argv[1:4]; NULLS = sys.argv[4:]
YEARS = [1830, 1870, 1913, 1950]
MAJ = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '清']
# 組：財 → [(レシピ, 生産性（goods.json か Simulation.cs の値・FullChainProductivityScale 7 は両方に掛かる）, {投入:量}, 出典)]
PAIRS = {
    '硫酸': [('(既定)', 1.8, {'硫黄': .8, '硝石': .1, '石炭': .6}, 'goods.json id33'),
             ('黄鉄鉱法', 1.8 * .7, {'銅鉱': 1.4, '石炭': .4}, 'Simulation.cs:1226・SulfurFromPyriteProductivity 0.7')],
    '肥料': [('(既定)', 1.9, {'硫酸': .2336, '畜産': .45}, 'goods.json id99'),
             ('グアノ肥料', 2.0, {'グアノ': .6, '硫酸': .1}, 'Simulation.cs:1390'),
             ('堆肥', 2.0 * .5, {'畜産': 1.0}, 'Simulation.cs:1394・CompostProductivity 0.5'),
             ('ハーバーボッシュ', 2.7, {'硫酸': .15, '電力': .25}, 'Simulation.cs の HB の行')],
    '紙': [('(既定)', 1.8, {'木': 1.404, '硫酸': .05, 'ソーダ灰': .0406}, 'goods.json id53'),
           ('ぼろ布製紙', 1.8, {'麻': .6, '綿布': .4}, 'Simulation.cs:1404')],
    '砂糖': [('(既定)', 2.2, {'サトウキビ': 1.0}, 'goods.json id44'),
             ('甜菜糖', 1.4, {'野菜': 2.2, '石灰石': .3222}, 'Simulation.cs:1363')],
    'キニーネ': [('(既定)', 1.6, {'金鶏樹皮': .5, '硫酸': .3}, 'goods.json id70'),
                ('樹皮の粉末', 1.6, {'金鶏樹皮': 1.5}, 'Simulation.cs:1411')],
    '鋼': [('(既定)', 1.9, {'鉄': 1.0, '石炭': .659}, 'goods.json id28'),
           ('坩堝鋼', 0.9, {'鉄': 2.2, '石炭': 1.978}, 'Simulation.cs:1088')],
}
def fl(x):
    try:
        v = float(x); return v if v == v else float('nan')
    except: return float('nan')
def rows(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for line in f: yield ix, line.rstrip('\n').split('\t')
def md(v):
    v = [x for x in v if x == x]; return st.median(v) if v else float('nan')
def seeds(r): lo, hi = map(int, r.split('-')); return range(lo, hi + 1)
o = open(OUT, 'w', encoding='utf-8', newline='\n')
o.write(f'# ge_alt base={BASE}:{BRNG} nulls={NULLS}\n')
# (a) 紙の投入費
C = {}   # (good, recipe, nation, year) -> [cost per seed]
need = {g for v in PAIRS.values() for _, _, ins, _ in v for g in ins}
for s in seeds(BRNG):
    p = os.path.join(BASE, f'nationgood_s{s}.tsv')
    if not os.path.exists(p): continue
    pr = {}
    for ix, r in rows(p):
        y = int(r[ix['year']])
        if y in YEARS and r[ix['good']] in need: pr[(y, r[ix['nation']], r[ix['good']])] = fl(r[ix['priceMeanGold']])
    nats = {k[1] for k in pr}
    for g, rs in PAIRS.items():
        for rc, prod, ins, _ in rs:
            for y in YEARS:
                for n in list(nats) + ['MAJ6']:
                    ns = MAJ if n == 'MAJ6' else [n]
                    tot = 0.0; ok = True
                    for nn in ns:
                        c = sum(q * pr.get((y, nn, i), float('nan')) for i, q in ins.items())
                        if c != c: ok = False; break
                        tot += c
                    if ok: C.setdefault((g, rc, n, y), []).append(tot / len(ns))
o.write('\n## (a) 紙の投入費（Σ投入×国の値段・金建て・自己費用と労働は同じなので除く・6 大国は単純平均・種の中央値）\n')
o.write('good\trecipe\tproductivity\tsource\tyear\tMAJ6\tイギリス\tフランス\tプロイセン\tロシア\tアメリカ\t清\tratio_to_default_MAJ6\n')
for g, rs in PAIRS.items():
    for y in YEARS:
        d0 = md(C.get((g, '(既定)', 'MAJ6', y), []))
        for rc, prod, ins, src in rs:
            vals = [md(C.get((g, rc, n, y), [])) for n in ['MAJ6'] + MAJ]
            o.write(f'{g}\t{rc}\t{prod:.2f}\t{src}\t{y}\t' + '\t'.join(f'{v:.3f}' for v in vals) + f'\t{vals[0] / d0 if d0 == d0 and d0 > 0 else float("nan"):.3f}\n')
# (b) eqr の規模の割合（基線とヌル腕）
o.write('\n## (b) eqr_s の規模の割合（scope＝MAJ6 か国名・和÷和・種の中央値）と規模あたりの採算・gateOpenRegions の和\n')
o.write('arm\tscope\tgood\trecipe\tyear\tshareOfScale\tmarginPerScale\tgateOpenRegions\n')
arms = [('base', BASE, BRNG)] + [(a.split('=')[0], a.split('=')[1].rsplit(':', 1)[0], a.rsplit(':', 1)[1]) for a in NULLS]
for lab, raw, rng in arms:
    E = {}
    for s in seeds(rng):
        p = os.path.join(raw, f'eqr_s{s}.tsv')
        if not os.path.exists(p): continue
        acc = {}; tot = {}
        for ix, r in rows(p):
            y = int(r[ix['year']]); g = r[ix['good']]
            if y not in YEARS or g not in PAIRS or int(r[ix['quarter']]) % 4 != 3: continue
            n = r[ix['nation']]; rc = r[ix['recipe']]
            sc, mg, go = fl(r[ix['scaleSum']]), fl(r[ix['marginSum']]), fl(r[ix['gateOpenRegions']])
            for sc0 in ([n] + (['MAJ6'] if n in MAJ else [])):
                a = acc.setdefault((sc0, g, rc, y), [0.0, 0.0, 0.0]); a[0] += sc if sc == sc else 0; a[1] += mg if mg == mg else 0; a[2] += go if go == go else 0
                tot[(sc0, g, y)] = tot.get((sc0, g, y), 0.0) + (sc if sc == sc else 0)
        for (sc0, g, rc, y), a in acc.items():
            t = tot[(sc0, g, y)]
            E.setdefault((sc0, g, rc, y), []).append((a[0] / t if t > 0 else float('nan'), a[1] / a[0] if a[0] > 0 else float('nan'), a[2]))
    for k in sorted(E):
        if k[0] != 'MAJ6' and lab != 'base': continue
        v = E[k]
        o.write(f'{lab}\t' + '\t'.join(map(str, k)) + f'\t{md([x[0] for x in v]):.3f}\t{md([x[1] for x in v]):.3f}\t{md([x[2] for x in v]):.0f}\n')
o.close(); print('ok', OUT)
