# 読むだけ (1176) c116rl：工場労働の実質の値段の中央（labor_s の plainRealPriceIndex）を、11-12 と (1169) の 2 つの数え方で並べ、名目と物価へ分ける。
#  plainRealPriceIndex＝州ごとの 労働の値段÷州の PriceLevel を人力の能力で重みづけた平均（InstrumentsLw.cs:121・:91）。参照の値段で割っていない。
#  11-12（ledger_S02_2026-11-12.tsv の S02-17）は〈÷参照 0.5〉＝この列÷0.5 の中央を 0.69 と書いた。(1169) s02_freq.py は列そのものの中央を 0.30 と書いた。
#  名目の値段＝plainWageBillPerMonth÷plainSoldPerMonth（売れた量で重みづけた州の労働の値段）・物価＝nation_s の priceLevel（同じ四半期・同じ国）。
#  実質の近似＝名目÷物価（州の重みが違うので列と一致しない＝一致の割合も印字する）。
# 使い方: python -X utf8 -I lab_real.py <生のフォルダ> <名>
import sys, os, statistics, collections
B, NAME = sys.argv[1:3]
REF = 0.5
MAJ = ('イギリス', 'フランス', 'プロイセン', 'アメリカ', 'ロシア', '日本', '清', 'オーストリア', 'オスマン帝国', 'スペイン')
def rd(t, s):
    p = os.path.join(B, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    f = open(p, encoding='utf-8'); h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    return ix, (l.rstrip('\r\n').split('\t') for l in f)
def fl(x):
    try: return float(x)
    except: return float('nan')
def era(y): return '1783-1849' if y < 1850 else '1850-1913' if y < 1914 else '1914-1950'
allr = []; farm = []; by_era = collections.defaultdict(list); by_nat = collections.defaultdict(list)
nom_e = collections.defaultdict(list); pl_e = collections.defaultdict(list); ok = tot = 0; ns = 0
maj_nom = collections.defaultdict(list); maj_pl = collections.defaultdict(list); maj_real = collections.defaultdict(list)
for s in range(1, 11):
    a = rd('labor', s)
    if a is None: continue
    ns += 1; ix, it = a; L = list(it)
    lastq = {}
    for c in L:
        y = int(c[ix['year']]); q = int(c[ix['quarter']]); lastq[y] = max(lastq.get(y, -1), q)
    endq = set(lastq.values())
    pl = {}
    b = rd('nation', s)
    if b is not None:
        jx, jt = b
        for c in jt:
            q = int(c[jx['quarter']])
            if q in endq: pl[(q, c[jx['nation']])] = fl(c[jx['priceLevel']])
    for c in L:
        q = int(c[ix['quarter']])
        if q not in endq: continue
        y = int(c[ix['year']]); n = c[ix['nation']]
        x = fl(c[ix['plainRealPriceIndex']]); fx = fl(c[ix['farmRealPriceIndex']])
        if x == x: allr.append(x); by_era[era(y)].append(x)
        if fx == fx: farm.append(fx)
        sold = fl(c[ix['plainSoldPerMonth']]); bill = fl(c[ix['plainWageBillPerMonth']]); p = pl.get((q, n), float('nan'))
        if sold > 0 and p > 0 and x == x:
            nom = bill / sold; tot += 1; ok += abs(nom / p - x) <= 0.05 * max(x, 1e-9)
            nom_e[era(y)].append(nom); pl_e[era(y)].append(p)
            if n in MAJ: maj_nom[(n, era(y))].append(nom); maj_pl[(n, era(y))].append(p); maj_real[(n, era(y))].append(x)
med = statistics.median
# 全ての国：国×年ごとにその国の最後の行（計器の国は四半期ごと・他の国は年 1 行＝年の最初の四半期に書かれる＝上の〈年の最後の四半期〉では落ちる）
alln = []; alln_maj = []; alln_min = []; ny = 0; farm_all = []
for s in range(1, 11):
    a = rd('labor', s)
    if a is None: continue
    ix, it = a; last = {}
    for c in it:
        k = (c[ix['nation']], int(c[ix['year']])); q = int(c[ix['quarter']])
        if k not in last or q > last[k][0]: last[k] = (q, fl(c[ix['plainRealPriceIndex']]), fl(c[ix['farmRealPriceIndex']]))
    for (n, y), (q, x, fx) in last.items():
        ny += 1
        if x == x: alln.append(x); (alln_maj if n in MAJ else alln_min).append(x)
        if fx == fx: farm_all.append(fx)
if alln:
    print(f'全ての国（国×年の最後の行 {ny}）：列の中央 {med(alln):.3f}・÷{REF} {med(alln) / REF:.3f}・上位 10% の境（÷{REF}） {sorted(alln)[int(.9 * len(alln))] / REF:.3f}・主要 10 国 {med(alln_maj):.3f}（{len(alln_maj)}）・他の国 {med(alln_min):.3f}（{len(alln_min)}）・農業 列＜0.25 {sum(v < 0.25 for v in farm_all) / len(farm_all):.1%}・列＜0.5 {sum(v < 0.5 for v in farm_all) / len(farm_all):.1%}')
print(f'== {NAME} {B} 種 {ns}・年の最後の四半期の国×四半期 {len(allr)}（＝計器の国だけ）')
if not allr: print('labor_s 表なし'); sys.exit()
print(f'列の中央 {med(allr):.3f}（(1169) の数え方）・列÷{REF} の中央 {med(allr) / REF:.3f}（11-12 の数え方）・上位 10% の境（÷{REF}） {sorted(allr)[int(.9 * len(allr))] / REF:.3f}')
print(f'農業：列＜0.5 {sum(v < 0.5 for v in farm) / len(farm):.1%}（(1169)）・列÷{REF}＜0.5（＝列＜0.25） {sum(v < 0.25 for v in farm) / len(farm):.1%}（11-12）')
print('時代別の列の中央 ' + ' '.join(f'{e}:{med(v):.3f}' for e, v in sorted(by_era.items())))
print(f'名目÷物価が列と 5% 以内で一致 {ok}/{tot}')
print('時代別の名目の中央・物価の中央 ' + ' '.join(f'{e}:{med(nom_e[e]):.3f}/{med(pl_e[e]):.3f}' for e in sorted(nom_e)))
for n in MAJ:
    print(n + ' ' + ' '.join(f'{e}:実質{med(maj_real[(n, e)]):.3f}・名目{med(maj_nom[(n, e)]):.3f}・物価{med(maj_pl[(n, e)]):.3f}' for e in ('1783-1849', '1850-1913', '1914-1950') if maj_real[(n, e)]))
