# (1030) c97eb：案 B（EndowmentCostFollowsAbundance 0→1）の門と記録の量を、生のフォルダごとに種別に数える（読むだけ・エンジンの関数を呼ばない）。
# 使い方: python -X utf8 -I m1030.py <生のフォルダ> <種の範囲 1-10> <world.json> <goods.json> <出力 .tsv>
# 読む物：nation_s（貿易÷GDP）・nationgood_s（越境の輸出量・値段）・prodcap_s（州×生産者の産出＝規則 B・級）・recipeout_s（級ごとの単位費用）・region_s（州の人口）・cederule_s（規則 A）。
# 規則 A＝読みの四半期（q%4==3・印の年 1784+q//4）に州を受け取った国・州が作られた国の (種,年,国) を、nationgood_s と prodcap_s の両方の和から外す（r1026/excl.py と同じ規則）。
# 規則 B＝産出は prodcap_s の Σoutput。印の年 Y の行は Y−1 年 12 月の流量（越境は最後の清算の 1 回分・産出は月の量）。比は和÷和。
import sys, os, csv, json, collections
csv.field_size_limit(1 << 30)
RAW, RNG, WJ, GJ, OUT = sys.argv[1:6]
lo, hi = map(int, RNG.split('-')); SEEDS = range(lo, hi + 1)
YEARS = (1870, 1913)
MAJ = {0, 2, 3, 4, 5, 11}   # q3_trade_gdp.py と同じ 6 大国の添字（仏・英・米・露・普・清）
G5 = ['石炭', '鉄鉱石', '銅鉱', '木', '畜産']
w = json.load(open(WJ, encoding='utf-8-sig')); goods = json.load(open(GJ, encoding='utf-8-sig'))
gname = {g['id']: g['name'] for g in goods}
COLONIAL = {21, 22, 23, 24, 25, 26, 66, 67, 83, 111, 127}
EXCL = {5, 15, 98, 93, 105, 106, 12}   # ge_geo.py と同じ（硫黄・主食の自給の組・座標に依る財）
G04 = {gname[g['id']] for g in goods if not g['inputs'] and g['tier'] != 'Service' and g['id'] not in COLONIAL and g['id'] not in EXCL}
res = {}
for n in w['nations']:
    for p in n['provinces']: res[p['city']] = {gname[x] for x in p['resources']}
    for c in n.get('initialColonies', []): res[c['name']] = {gname[x] for x in c['resources']}
NAN = float('nan')
def fl(x):
    try: return float(x)
    except: return NAN
def rd(p):
    f = open(p, encoding='utf-8', newline=''); r = csv.reader(f, delimiter='\t'); h = next(r); return {k: i for i, k in enumerate(h)}, r
def ng(y): return y % 10 == 0 or y == 1913
o = open(OUT, 'w', encoding='utf-8', newline='\n')
o.write('seed\tkey\tvalue\n')
def put(s, k, v): o.write(f'{s}\t{k}\t{v!r}\n')
for s in SEEDS:
    # 規則 A
    ex = set()
    ix, r = rd(f'{RAW}/cederule_s{s}.tsv')
    for row in r:
        q = int(row[ix['quarter']])
        if q % 4 == 3 and ng(1784 + q // 4): ex.add((1784 + q // 4, row[ix['receiver']]))
    first = {}; pop = {}
    qY = {(Y - 1784) * 4 + 3: Y for Y in YEARS}
    ix, r = rd(f'{RAW}/region_s{s}.tsv')
    for row in r:
        rg = row[ix['region']]; q = int(row[ix['quarter']])
        if rg not in first: first[rg] = (q, row[ix['nation']])
        if q in qY: pop[(qY[q], rg)] = (int(row[ix['nationIndex']]), fl(row[ix['populationK']]))
    for rg, (q, nat) in first.items():
        if q > 0 and q % 4 == 3 and ng(1784 + q // 4): ex.add((1784 + q // 4, nat))
    put(s, 'exclN', len([k for k in ex if k[0] in YEARS]))
    # prodcap：国×財の産出（規則 B）と、その財の級 E/X の州を持つ国
    out = collections.defaultdict(float); enat = collections.defaultdict(set)
    ix, r = rd(f'{RAW}/prodcap_s{s}.tsv')
    iy, inat, ig, icl, io = ix['year'], ix['ownerNation'], ix['good'], ix['class'], ix['output']
    for row in r:
        y = int(row[iy])
        if y not in YEARS: continue
        nat = row[inat]; g = row[ig]
        if row[icl] in ('E', 'X'): enat[(y, g)].add(nat)
        if (y, nat) in ex: continue
        v = fl(row[io])
        if v == v: out[(y, nat, g)] += v
    # nationgood：越境の輸出量・値段
    expq = collections.defaultdict(float); price = {}
    ix, r = rd(f'{RAW}/nationgood_s{s}.tsv')
    for row in r:
        y = int(row[ix['year']])
        if y not in YEARS: continue
        nat = row[ix['nation']]
        if (y, nat) in ex: continue
        g = row[ix['good']]
        expq[(y, nat, g)] += fl(row[ix['crossExportQty']]); price[(y, nat, g)] = fl(row[ix['priceMeanGold']])
    for Y in YEARS:
        for g in G5 + ['硫黄', '麦', '金', '鉛鉱']:
            EN = enat[(Y, g)]
            O = OE = X = XE = pw = ww = 0.0
            for (y, n, gg), v in out.items():
                if y != Y or gg != g: continue
                O += v
                if n in EN: OE += v
                p = price.get((y, n, gg), NAN)
                if v > 0 and p == p: pw += v * p; ww += v
            for (y, n, gg), v in expq.items():
                if y != Y or gg != g: continue
                X += v
                if n in EN: XE += v
            put(s, f'out|{Y}|{g}', O); put(s, f'exp|{Y}|{g}', X); put(s, f'outE|{Y}|{g}', OE); put(s, f'expE|{Y}|{g}', XE)
            put(s, f'pxw|{Y}|{g}', pw / ww if ww > 0 else NAN)
    # recipeout：級 E と N の単位費用（産出の重みの和を書く＝集計側で和÷和）
    acc = collections.defaultdict(float)
    ix, r = rd(f'{RAW}/recipeout_s{s}.tsv')
    for row in r:
        y = int(row[ix['year']]); cl = row[ix['class']]
        if y not in YEARS or cl not in ('E', 'N'): continue
        ov = fl(row[ix['outputSum']]); u = fl(row[ix['unitCostOutW']]); a = fl(row[ix['unitCostIfAbundance']]); sc = fl(row[ix['selfCostOutW']])
        if not (ov > 0) or u != u or a != a or sc != sc: continue
        acc[(y, cl, 'o')] += ov; acc[(y, cl, 'u')] += ov * u; acc[(y, cl, 'a')] += ov * a; acc[(y, cl, 's')] += ov * sc
    for (y, cl, k), v in sorted(acc.items()): put(s, f'ro|{y}|{cl}|{k}', v)
    # 州の人口：6 大国の非産地の州（0.4 の財の産地が 1 つも無い州）
    for Y in YEARS:
        P0 = PA = 0.0; unk = 0
        for (y, rg), (ni, pk) in pop.items():
            if y != Y or ni not in MAJ or pk != pk: continue
            PA += pk
            if rg not in res: unk += 1; continue
            if not (res[rg] & G04): P0 += pk
        put(s, f'popNon|{Y}', P0); put(s, f'popMaj|{Y}', PA); put(s, f'popUnk|{Y}', unk)
    # 貿易÷GDP（q3_trade_gdp.py の T2・ALL と同じ式）
    t = collections.defaultdict(float)
    ix, r = rd(f'{RAW}/nation_s{s}.tsv')
    for row in r:
        y = int(row[ix['year']])
        if y not in YEARS: continue
        e, i_, cl, gd = fl(row[ix['exportsPerClearing']]), fl(row[ix['importsPerClearing']]), fl(row[ix['clearingsPerYear']]), fl(row[ix['nominalGdpPerYear']])
        tr = (e + i_) * cl
        if not (gd == gd and tr == tr): continue
        if gd > 0: t[(y, 'ALLt')] += tr; t[(y, 'ALLg')] += gd
        if int(row[ix['nationIndex']]) in MAJ: t[(y, 'T2t')] += tr; t[(y, 'T2g')] += gd
    for Y in YEARS:
        put(s, f'T2|{Y}', t[(Y, 'T2t')] / t[(Y, 'T2g')] if t[(Y, 'T2g')] > 0 else NAN)
        put(s, f'ALL|{Y}', t[(Y, 'ALLt')] / t[(Y, 'ALLg')] if t[(Y, 'ALLg')] > 0 else NAN)
    o.flush()
o.close()
