#!/usr/bin/env python
# (380) 輸送の段0 の読み手＝標準の計器 trbin_s／trroute_s／trnation_s／trstat_s（SimCore/InstrumentsTransport.cs）を読む。
#   python an_tr380.py <計器の出力ディレクトリ> [種の並び=1,2,3,4]
# 出すもの：①運賃の陸脚と海脚（時代別）・払い先（§2-7 の不具合の大きさ）②距離の分布と財の重さの帯（従価→従量で動く物）③地形の係数と両端の地形の分布（断面の年）
#   ④手段ごとの費用の見積もり（doc §2-1 の表の形・今の世界の値段で・係数は下の RECIPES＝史実から置いた【推】で、シミュの係数ではない）と分かれ目の距離
#   ⑤州の中の平均の運び距離と山／平地の州の都市の割合・工場の産出（§2-5）⑥事前登録の線の候補に足す数。
# ⚠読むだけ。RECIPES の数は計器でなくこの読み手が持つ＝直しても成績表は動かない。⚠重さは相対の輸送重量（トンではない）。
# (385) 計器の直し（列の追加）を読む：①の末尾に〈陸脚の運賃のうち、その陸脚がある州の国の運び手へ戻る割合〉（T8・成約ごとの正確な値）と陸脚の売り手側・買い手側の内訳を足した。
#   ⑤は面積の行（A）を年ごとに持つ（G 行と同じ年の同じ州の集合）。旧い形の表（380・列が無い）は新しい節を飛ばす。
import sys, os, csv, math, collections
os.environ.setdefault('PYTHONUTF8', '1')
if hasattr(sys.stdout, 'reconfigure'): sys.stdout.reconfigure(encoding='utf-8')

ERAS = [('1783-1829', 1783, 1830), ('1830-1869', 1830, 1870), ('1870-1913', 1870, 1914), ('1914-1950', 1914, 1951)]
TERR = ['平地', '大河', '支流', '丘', '森', '山', '荒地']
DIST = ['<50', '50-150', '150-400', '400-1000', '1000-2500', '2500-6000', '6000-12000', '>=12000']
WGT = ['0.4', '1.0', '2.5', '3.0', '6.0']

def era_of(y):
    for n, a, b in ERAS:
        if a <= y < b: return n
    return None

def table(path):
    if not os.path.exists(path): return []
    with open(path, encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd)
        return [dict(zip(h, r)) for r in rd if len(r) == len(h)]   # 書き途中の最後の行は読まない

def num(x):
    try: return float(x)
    except Exception: return float('nan')

def pct(a, b): return 100.0 * a / b if b else float('nan')
def fmt(x, d=2):
    if isinstance(x, float) and (math.isnan(x) or math.isinf(x)): return 'nan'
    return f'{x:.{d}f}'

# ---- 手段のレシピ（史実の概数から置いた【推】）：1トン・キロの基準の費用＝労働者の日数（基準の値段）と、その内訳の割合（財の名前→割合）と、積み下ろしの固定の費用（1トンあたりの労働日数）。
#   出所（doc §3）：荷馬車＝米 1819-22 の 15〜30 セント／トン・マイル＝日雇いの賃金（約 75 セント）で 0.25〜0.5 労働日／トン・キロ→0.30／鉄道＝米 1851 頃 2〜2.6 セント＝荷馬車の 1/6〜1/12（英 1870÷1700 は 1/20）→0.025／
#   川＝道の約 1/10（英 1700 頃）→0.075／海（帆船）＝道の約 1/60（英 1750 の石炭）→0.012／汽船＝帆船の 2/3 とした【推】／人力（担ぎ）＝30 kg×25 km／日→1.33／トラック＝1920 年代の荷馬車の 1/5 とした【推】。
#   内訳の割合はすべて【推】（史実の頁は見ていない）。
RECIPES = {
    '人力':   dict(v=1.333, fixed=0.05, mix={'労働力': 1.0},                                         terrain='horse', from_year=0),
    '馬':     dict(v=0.30,  fixed=0.20, mix={'労働力': 0.35, '馬': 0.25, '飼料': 0.40},              terrain='horse', from_year=0),
    '水路':   dict(v=0.075, fixed=0.50, mix={'労働力': 0.60, '馬': 0.20, '飼料': 0.20},              terrain='water', from_year=0),
    '鉄道':   dict(v=0.025, fixed=0.80, mix={'石炭': 0.20, '労働力': 0.15, '高度労働力': 0.15, '線路': 0.25, '機関車': 0.25}, terrain='rail', from_year=1830),
    '帆船':   dict(v=0.012, fixed=1.00, mix={'労働力': 0.50, '帆船': 0.30, '製材': 0.20},            terrain='sea',   from_year=0),
    '汽船':   dict(v=0.008, fixed=1.00, mix={'石炭': 0.35, '労働力': 0.15, '高度労働力': 0.10, '汽船': 0.40}, terrain='sea', from_year=1840),
    '自動車': dict(v=0.060, fixed=0.10, mix={'原油': 0.20, '労働力': 0.50, '自動車': 0.30},          terrain='horse', from_year=1900),
}

def main():
    ins = sys.argv[1]
    seeds = [int(s) for s in (sys.argv[2] if len(sys.argv) > 2 else '1,2,3,4').split(',')]
    bins, routes, nats, stat, nation_s, class_s = [], [], [], [], [], []
    for s in seeds:
        for t, store in (('trbin', bins), ('trroute', routes), ('trnation', nats), ('trstat', stat), ('nation', nation_s), ('class', class_s)):
            for r in table(os.path.join(ins, f'{t}_s{s}.tsv')):
                r['seed'] = s; store.append(r)
    ns = len(seeds)
    print(f'== (380) 輸送の段0 の読み手：{ins}  種 {seeds}（時代の数字は種の平均）')
    if not bins: print('trbin が無い'); return

    # ---------------- ① 運賃の陸脚／海脚と払い先 ----------------
    print('\n## ① 運賃の陸脚と海脚（年の合計を時代でならした・金建て・種の平均）')
    E = {n: collections.defaultdict(float) for n, _, _ in ERAS}
    NY = {n: len({int(r['year']) for r in bins if era_of(int(r['year'])) == n}) for n, _, _ in ERAS}   # 時代の中の年の数（年平均に直す）
    for r in bins:
        e = era_of(int(r['year']))
        if not e: continue
        d = E[e]; sea = int(r['routeCode']); cross = int(r['crossBorderFlag'])
        for k in ('valueGoldPerYear', 'weightUnitsPerYear', 'weightKmRealPerYear', 'weightKmLandRealPerYear', 'weightKmSeaRealPerYear', 'weightKmEqPerYear', 'weightKmEqLandPerYear', 'weightKmEqSeaPerYear', 'freightGoldPerYear', 'freightLandPartGoldPerYear', 'freightSeaPartGoldPerYear', 'dealsCountPerYear'):
            d[k] += num(r[k]) / ns
        if not sea: d['freightDirectLand'] += num(r['freightGoldPerYear']) / ns; d['valueDirectLand'] += num(r['valueGoldPerYear']) / ns
        if not cross: d['freightDomestic'] += num(r['freightGoldPerYear']) / ns; d['valueDomestic'] += num(r['valueGoldPerYear']) / ns; d['freightLandDomestic'] += num(r['freightLandPartGoldPerYear']) / ns
    print(f"{'時代':10} {'成約額/年':>10} {'運賃/年':>9} {'運賃/額%':>8} {'陸脚の分%':>9} {'陸直行の運賃%':>12} {'国内の成約額%':>12} {'国内の陸脚運賃/年(金)':>14} {'陸1kmの換算km':>11} {'平均の実km/重さ':>13} {'陸脚tkm%':>8}")
    for n, _, _ in ERAS:
        d = E[n]
        if not d['valueGoldPerYear']: continue
        print(f"{n:10} {d['valueGoldPerYear'] / NY[n]:10.0f} {d['freightGoldPerYear'] / NY[n]:9.1f} {pct(d['freightGoldPerYear'], d['valueGoldPerYear']):8.3f} {pct(d['freightLandPartGoldPerYear'], d['freightGoldPerYear']):9.1f} {pct(d['freightDirectLand'], d['freightGoldPerYear']):12.1f} {pct(d['valueDomestic'], d['valueGoldPerYear']):12.1f} {d['freightLandDomestic'] / NY[n]:14.1f} {d['weightKmEqLandPerYear'] / d['weightKmLandRealPerYear'] if d['weightKmLandRealPerYear'] else float('nan'):11.2f} {d['weightKmRealPerYear'] / d['weightUnitsPerYear'] if d['weightUnitsPerYear'] else float('nan'):13.0f} {pct(d['weightKmLandRealPerYear'], d['weightKmRealPerYear']):8.1f}")
    print('  ⚠陸脚の分＝運賃のうち陸脚の換算 km に比例する分（運賃は換算 km に比例）。全部が世界の船腹の比で各国の船の持ち主へ行く（BilateralMarket の freightPool）。')
    print('  ⚠陸1kmの換算km＝重さ加重の（陸脚の換算 km÷実 km）＝地形×陸÷海の比の実効（鉄道の門が開くと下がる）。')

    print('\n## ① 払い先：国ごとの（自国の買い手が払った運賃）と（自国の船の持ち主が受け取った運賃）・時代別・種の平均・上位')
    PN = {n: collections.defaultdict(lambda: collections.defaultdict(float)) for n, _, _ in ERAS}
    for r in nats:
        e = era_of(int(r['year']))
        if not e: continue
        d = PN[e][r['nation']]
        for k in ('freightPaidGoldPerYear', 'freightPaidLandPartGoldPerYear', 'freightPaidDomesticGoldPerYear', 'freightPaidLandPartDomesticGoldPerYear', 'freightReceivedGoldPerYear',
                  'freightLandSellerSideInNationGoldPerYear', 'freightLandBuyerSideInNationGoldPerYear', 'freightLandInNationToOwnCarriersGoldPerYear'):
            d[k] += num(r.get(k, 'nan')) / ns   # (385) 旧い形の表には新しい列が無い＝nan
    gdp = collections.defaultdict(lambda: collections.defaultdict(list))
    for r in nation_s:
        e = era_of(int(r['year']))
        if e and int(r['quarter']) % 4 == 3: gdp[e][r['nation']].append(num(r['nominalGdpPerYear']))
    for n, _, _ in ERAS:
        pn = PN[n]
        if not pn: continue
        tot_recv = sum(v['freightReceivedGoldPerYear'] for v in pn.values()); tot_paid = sum(v['freightPaidGoldPerYear'] for v in pn.values())
        ordered = sorted(pn.items(), key=lambda kv: -kv[1]['freightReceivedGoldPerYear'])[:6]
        print(f'-- {n}  世界の運賃の合計 {tot_paid:.1f}（受け取りの和 {tot_recv:.1f}）  [国｜払った運賃・うち陸脚・うち自国の売り手との成約・受け取った運賃の割合%・自国の陸運賃のうち世界へ漏れた割合%]')
        for nm, v in ordered:
            share = pct(v['freightReceivedGoldPerYear'], tot_recv)
            leak = 100.0 - share if v['freightPaidLandPartDomesticGoldPerYear'] > 0 else float('nan')
            print(f"   {nm:8} 払った {v['freightPaidGoldPerYear']:9.1f}（陸脚 {v['freightPaidLandPartGoldPerYear']:8.1f}・国内成約 {v['freightPaidDomesticGoldPerYear']:8.1f}・うち国内の陸脚 {v['freightPaidLandPartDomesticGoldPerYear']:8.1f}） 受取 {share:5.1f}%  自国の国内の陸脚運賃の {leak:5.1f}% が世界へ漏れる（受け取りの割合を自国の持ち分と読んだ）")
        # 陸脚の運賃の合計のうち、受け手と払い手の国が違う分＝払い手の国の受け取りの割合を引いた額（全ての国の和）
        # ⚠(382) 反証役 tr〔v〕低#2：時代（40 年前後）と種をまとめた和の比で引く近似＝清算ごとの (1−船腹の比) の積算とは一致しない（各国の取り分は最大でも約 9%＝桁は崩れない・小数 1 桁の精度は無い）
        landpaid = sum(v['freightPaidLandPartGoldPerYear'] for v in pn.values())
        wrong = sum(v['freightPaidLandPartGoldPerYear'] * (1 - v['freightReceivedGoldPerYear'] / tot_recv if tot_recv else 0) for v in pn.values())
        print(f'   陸脚の運賃の合計 {landpaid:.1f}（全運賃の {pct(landpaid, tot_paid):.1f}%）＝払い手の国の受け取りの割合（船の持ち主の持ち分）だけが自国へ戻り、残り {wrong:.1f}（{pct(wrong, landpaid):.1f}%）は他国の船の持ち主へ行く')
        # (385) T8（成約ごとの正確な値）：陸脚の運賃のうち、その陸脚がある州の国の運び手へ戻る割合。売り手側の陸脚は売り手の国・買い手側は買い手の国に積み、戻る額＝運賃×その国の船腹の比（その清算の値）。
        sIn = sum(v['freightLandSellerSideInNationGoldPerYear'] for v in pn.values()); bIn = sum(v['freightLandBuyerSideInNationGoldPerYear'] for v in pn.values()); own = sum(v['freightLandInNationToOwnCarriersGoldPerYear'] for v in pn.values())
        if not math.isnan(own):
            print(f'   (385) 陸脚の運賃（その陸脚のある国の中）{sIn + bIn:.1f}＝売り手側 {sIn:.1f}（{pct(sIn, sIn + bIn):.1f}%）＋買い手側 {bIn:.1f}（{pct(bIn, sIn + bIn):.1f}%）〔全運賃 {tot_paid:.1f} のうち陸脚の分 {landpaid:.1f}＝和は一致する〕')
            print(f'   (385) T8：うち、その陸脚のある国の運び手へ戻る額 {own:.1f}＝{pct(own, sIn + bIn):.2f}%（残り {100 - pct(own, sIn + bIn):.2f}% は他国の運び手へ）〔線＝≧ 90%〕')

    # ---------------- ② 距離の分布と財の重さの帯 ----------------
    print('\n## ② 距離の分布（成約の実 km の合計の帯・重さの割合%・時代別）')
    D = {n: [0.0] * 8 for n, _, _ in ERAS}; Dl = {n: [0.0] * 8 for n, _, _ in ERAS}; Dv = {n: [0.0] * 8 for n, _, _ in ERAS}
    for r in bins:
        e = era_of(int(r['year']))
        if not e: continue
        b = int(r['distanceBandCode']); D[e][b] += num(r['weightUnitsPerYear']); Dl[e][b] += num(r['weightKmLandRealPerYear']); Dv[e][b] += num(r['valueGoldPerYear'])
    print(f"{'時代':10} " + ' '.join(f'{x:>9}' for x in DIST) + '   （重さの割合%）')
    for n, _, _ in ERAS:
        t = sum(D[n])
        if t: print(f'{n:10} ' + ' '.join(f'{pct(x, t):9.1f}' for x in D[n]))
    print(f"{'時代':10} " + ' '.join(f'{x:>9}' for x in DIST) + '   （額の割合%）')
    for n, _, _ in ERAS:
        t = sum(Dv[n])
        if t: print(f'{n:10} ' + ' '.join(f'{pct(x, t):9.1f}' for x in Dv[n]))

    print('\n## ② 財の重さの帯ごと（輸送重量 0.4・1.0・2.5・3.0・6.0）：額・重さ・運賃の割合%と、運賃が従量（換算 km×重さに比例・合計は同じ）だったときの運賃/額の比（現行＝従価に対する倍率）')
    for n, _, _ in ERAS:
        W = collections.defaultdict(lambda: collections.defaultdict(float))
        for r in bins:
            if era_of(int(r['year'])) != n: continue
            w = W[int(r['weightBandCode'])]
            for k in ('valueGoldPerYear', 'weightUnitsPerYear', 'freightGoldPerYear', 'weightKmEqPerYear'): w[k] += num(r[k]) / ns
        tv = sum(w['valueGoldPerYear'] for w in W.values()); tw = sum(w['weightUnitsPerYear'] for w in W.values()); tf = sum(w['freightGoldPerYear'] for w in W.values()); tk = sum(w['weightKmEqPerYear'] for w in W.values())
        if not tv: continue
        print(f'-- {n}  [重さの帯｜額%・重さ%・運賃%・運賃/額（現行）%・従量にしたときの運賃/額%・倍率]')
        for b in sorted(W):
            w = W[b]; cur = pct(w['freightGoldPerYear'], w['valueGoldPerYear']); hyp = pct(tf * w['weightKmEqPerYear'] / tk, w['valueGoldPerYear']) if tk else float('nan')
            print(f"   {WGT[b]:>4}  額 {pct(w['valueGoldPerYear'], tv):5.1f}  重さ {pct(w['weightUnitsPerYear'], tw):5.1f}  運賃 {pct(w['freightGoldPerYear'], tf):5.1f}  運賃/額 {cur:6.3f}  従量 {hyp:6.3f}  倍率 {hyp / cur if cur else float('nan'):6.2f}")

    # ---------------- ③ 地形 ----------------
    zrows = [r for r in stat if r['scope'] == 'Z' and r['seed'] == seeds[0]]
    Z = {r['name']: num(r['value']) for r in zrows}
    print('\n## ③ 係数（Z）：' + '・'.join(f'{k}={v:g}' for k, v in Z.items() if not k.startswith('Terrain')))
    print('   地形の係数 馬/鉄道：' + '  '.join(f"{TERR[i]} {Z.get('TerrainHorseCost[' + ['Plain','WaterMajor','WaterMinor','Hill','Forest','Mountain','Barren'][i] + ']', float('nan')):g}/{Z.get('TerrainRailCost[' + ['Plain','WaterMajor','WaterMinor','Hill','Forest','Mountain','Barren'][i] + ']', float('nan')):g}" for i in range(7)))
    snaps = sorted({int(r['year']) for r in routes})
    print(f'   経路の断面の年：{snaps}')
    for y in snaps:
        rs = [r for r in routes if int(r['year']) == y and not r['sellerRegion'].startswith('(')]
        if not rs: continue
        tv = sum(num(r['valueGoldPerYear']) for r in rs) / ns
        tw = collections.defaultdict(float)
        for r in rs:
            v = num(r['valueGoldPerYear']) / ns; tw[int(r['sellerTerrainCode'])] += v / 2; tw[int(r['buyerTerrainCode'])] += v / 2
        seaV = sum(num(r['valueSeaRouteGoldPerYear']) for r in rs) / ns
        eqb = sum(num(r['valueTimesEqBookKmGoldPerYear']) for r in rs); eql = sum(num(r['valueTimesEqLandKmGoldPerYear']) for r in rs)
        print(f"   {y}: 経路 {len(rs) / ns:.0f} 組/種・額 {tv:.0f}・海経由の額 {pct(seaV, tv):.1f}%・換算kmのうち陸脚 {pct(eql, eqb):.1f}%・両端の地形（額で半々に数えた）" + ' '.join(f'{TERR[t]} {pct(tw[t], tv):.1f}%' for t in range(7)))

    # ---------------- ④ 手段ごとの費用の見積もり ----------------
    print('\n## ④ 手段ごとの1トン・キロの費用（労働日・基準の値段で。値段の比＝その年の世界の値段÷参照価格を内訳に掛ける）と固定の費用、分かれ目の距離')
    P = collections.defaultdict(lambda: collections.defaultdict(list))
    for r in stat:
        if r['scope'] == 'P': P[int(r['year'])][r['name']].append((num(r['value']), num(r['refValue'])))
    def ratio(y, name):
        v = P[y].get(name)
        if not v: return float('nan')
        a = sum(x[0] for x in v) / len(v); b = sum(x[1] for x in v) / len(v)
        return a / b if b else float('nan')
    def cost(m, y, terr_code=0):
        rc = RECIPES[m]; tot = 0.0
        for g, sh in rc['mix'].items():
            r = ratio(y, g); tot += sh * (r if not math.isnan(r) else 1.0)
        if rc['terrain'] == 'horse': k = Z.get('TerrainHorseCost[' + ['Plain','WaterMajor','WaterMinor','Hill','Forest','Mountain','Barren'][terr_code] + ']', 1.0)
        elif rc['terrain'] == 'rail': k = Z.get('TerrainRailCost[' + ['Plain','WaterMajor','WaterMinor','Hill','Forest','Mountain','Barren'][terr_code] + ']', 1.0)
        else: k = 1.0
        return rc['v'] * tot * k, rc['fixed'] * ratio(y, '労働力') if not math.isnan(ratio(y, '労働力')) else rc['fixed']
    for y in (1790, 1830, 1850, 1880, 1910, 1940):
        if y not in P: continue
        print(f'-- {y}  価格比（世界の値段÷参照価格）：' + ' '.join(f'{g} {ratio(y, g):.2f}' for g in ('馬', '飼料', '石炭', '線路', '機関車', '帆船', '汽船', '原油', '自動車', '労働力', '高度労働力')))
        print(f"   {'手段':6} {'解禁':>5} {'変動費/tkm':>10} {'固定/t':>8}   100km {'':>5} 500km {'':>5} 2000km {'':>3}（1トンの費用・労働日）")
        for m, rc in RECIPES.items():
            if y < rc['from_year']: continue
            v, f = cost(m, y)
            print(f"   {m:6} {rc['from_year']:5d} {v:10.4f} {f:8.3f}   " + ' '.join(f'{f + v * d:10.2f}' for d in (100, 500, 2000)))
        def breakeven(a, b, tc=0):
            va, fa = cost(a, y, tc); vb, fb = cost(b, y, tc)
            if va == vb: return float('nan')
            d = (fb - fa) / (va - vb)
            return d if d > 0 else float('nan')
        if y >= 1830:
            print('   ⚠分かれ目は荷役の固定費の差だけで決まる＝数 km（史実の鉄道が馬に勝つ距離 数十 km 以上は駅までの馬の脚〔シミュの接ぎ木 landGraft〕と積み替えが作る）。表の費用の比は史実から置いた値で、検定ではない。')
            print('   分かれ目の距離（km）：馬↔鉄道 ' + ' '.join(f'{TERR[t]}={fmt(breakeven("馬", "鉄道", t), 0)}' for t in (0, 3, 4, 5, 6)) + f'｜人力↔馬 {fmt(breakeven("人力", "馬"), 1)}')
        else:
            print(f'   分かれ目の距離（km）：人力↔馬 {fmt(breakeven("人力", "馬"), 1)}・馬↔水路 {fmt(breakeven("馬", "水路"), 0)}・馬↔帆船（港の荷役を含む）{fmt(breakeven("馬", "帆船"), 0)}')
        vh, _ = cost('馬', y); vr, _ = cost('鉄道', y) if y >= 1830 else (float('nan'), 0); vs, _ = cost('帆船', y)
        print(f'   比：鉄道÷馬 = {fmt(vr / vh, 3)}・海（帆船）÷馬 = {fmt(vs / vh, 4)}  ←  現行のシミュの運賃の係数：鉄道÷馬 = {Z.get("RailCostFraction", float("nan")):g}（鉄道の後の陸÷海の比 {Z.get("LandSeaCostRatio", float("nan")) * Z.get("RailCostFraction", float("nan")):g}）・海÷陸 = {1 / Z.get("LandSeaCostRatio", float("nan")):.4f}')

    # ---------------- ⑤ 州の中の運び・山と平地 ----------------
    print('\n## ⑤ 州の中の平均の運び距離（A）と山／平地の州の都市の割合・工場の産出（G・10年ごとの年の終わり）')
    AALL = [r for r in stat if r['scope'] == 'A' and r['seed'] == seeds[0]]
    ayears = sorted({int(r['year']) for r in AALL})
    # (385) 面積の行は断面の年（10 年ごと）ごとにその年の州で取り直す（旧は開幕の 1 回だけ＝後から現れた州は無く、その土地は隣の州に吸われた）
    for ay in ayears:
        A = [r for r in AALL if int(r['year']) == ay]
        hs = collections.defaultdict(list)
        for r in A:
            h = num(r['meanHaulKm'])
            if not math.isnan(h): hs[int(r['terrainCode'])].append(h)
        allh = sorted(h for v in hs.values() for h in v)
        if ay not in (ayears[0], 1850, 1910, ayears[-1]) and ay != 1790: continue
        print(f'   [{ay}] 州 {len(A)} 件・平均の運び距離（km）の中央値 {allh[len(allh) // 2]:.0f}・四分位 {allh[len(allh) // 4]:.0f}〜{allh[3 * len(allh) // 4]:.0f}・最大 {allh[-1]:.0f}（座標の疎な州は大きい）' if allh else f'   [{ay}] 面積の行が無い')
        print('     地形別（州の数・平均の運び距離の中央値 km）：' + ' '.join(f'{TERR[t]} {len(hs[t])}件/{sorted(hs[t])[len(hs[t]) // 2]:.0f}' for t in range(7) if hs[t]))
    G = [r for r in stat if r['scope'] == 'G']
    ys = sorted({int(r['year']) for r in G})
    print(f"   {'年':>5} {'地形':6} {'州':>5} {'人口K/州':>9} {'都市%':>6} {'工場産出/千人':>12} {'採取産出/千人':>12} {'重さ/千人':>9}  （種の平均・人口加重）")
    for y in ys:
        if y not in (1790, 1820, 1850, 1880, 1910, 1940): continue
        agg = collections.defaultdict(lambda: collections.defaultdict(float))
        for r in G:
            if int(r['year']) != y: continue
            t = int(r['terrainCode']); a = agg[t]
            a['n'] += 1.0 / ns; a['pop'] += num(r['populationK']) / ns; a['urb'] += num(r['urbanPopulationK']) / ns
            a['fac'] += num(r['factoryOutputValuePerMonth']) / ns; a['gat'] += num(r['gatherOutputValuePerMonth']) / ns; a['wt'] += num(r['outputWeightPerMonth']) / ns
        for t in (0, 3, 4, 5, 6, 1, 2):
            a = agg.get(t)
            if not a or not a['pop']: continue
            print(f"   {y:5d} {TERR[t]:6} {a['n']:5.1f} {a['pop'] / a['n']:9.0f} {pct(a['urb'], a['pop']):6.1f} {a['fac'] / a['pop']:12.3f} {a['gat'] / a['pop']:12.3f} {a['wt'] / a['pop']:9.3f}")
    # 州の中の運び（従価・シミュの運賃と同じ率）：運賃は荷の値打ち×率×換算 km÷1万 km なので、州の中の運びを同じ率で課すと
    #   荷の値打ちの 率×（陸÷海の比×地形の係数）×平均の運び距離÷1万 km になる＝物価水準に依らない比（金建ての額を年をまたいで比べる罠を避ける）。
    #   これを商人の取り分 4%（MerchantMargin）と並べる＝〈4% の中に運びが入っているか〉の読むだけの目安。州ごとの産出の額（参照価格×量）で加重。
    def tf(t, rail):
        names = ['Plain', 'WaterMajor', 'WaterMinor', 'Hill', 'Forest', 'Mountain', 'Barren']
        if t == 1 and Z.get('TerrainWaterMajorCost', 0) > 0: return Z['TerrainWaterMajorCost']
        if t == 2 and Z.get('TerrainWaterMinorCost', 0) > 0: return Z['TerrainWaterMinorCost']
        return Z.get(('TerrainRailCost[' if rail else 'TerrainHorseCost[') + names[t] + ']', 1.0)
    # (385) 年と州の名前で持つ（G 行と同じ年の面積）。★(387) 反証役 tf〔v〕低：種ごとに州の集合と格子の分け方が違う＝(種, 年, 州) の鍵にする（旧は種 1 の A 行を全ての種の G 行に当てた）
    hmap = {(r['seed'], int(r['year']), r['region']): num(r['meanHaulKm']) for r in stat if r['scope'] == 'A'}
    print('   州の中の運びを同じ率で課すと荷の値打ちの何%か（産出の額で加重・平均の運び距離は陸マスクの面積から）と、商人の取り分 4% に対する割合')
    print(f"   {'地形':6} {'州':>4} {'平均の運び距離km':>14} {'陸÷海×地形':>10} {'運び%(鉄道前)':>12} {'/4%':>6} {'運び%(鉄道後)':>12} {'/4%':>6}")
    yy = 1910 if 1910 in ys else (ys[-1] if ys else None)
    agg = collections.defaultdict(lambda: collections.defaultdict(float)); dropped = 0; seen = 0
    for r in G:
        if int(r['year']) != yy: continue
        seen += 1
        h = hmap.get((r['seed'], int(r['year']), r['region']), float('nan'))
        if math.isnan(h): dropped += 1; continue
        t = int(r['terrainCode']); v = num(r['factoryOutputValuePerMonth']) + num(r['gatherOutputValuePerMonth'])
        a = agg[t]; a['n'] += 1; a['v'] += v; a['hv'] += h * v
        a['pre'] += v * Z['FreightRatePer10Mm'] * Z['LandSeaCostRatio'] * tf(t, False) * h / Z['DistanceNormKm']
        a['post'] += v * Z['FreightRatePer10Mm'] * Z['LandSeaCostRatio'] * Z['RailCostFraction'] * tf(t, True) * h / Z['DistanceNormKm']
    allv = sum(a['v'] for a in agg.values()); allpre = sum(a['pre'] for a in agg.values()); allpost = sum(a['post'] for a in agg.values())
    mm = Z.get('MerchantMargin', 0.04)
    for t in (0, 3, 4, 5, 6, 1, 2):
        a = agg.get(t)
        if not a or not a['v']: continue
        pre = a['pre'] / a['v']; post = a['post'] / a['v']
        print(f"   {TERR[t]:6} {a['n'] / ns:4.0f} {a['hv'] / a['v']:14.0f} {Z['LandSeaCostRatio'] * tf(t, False):10.2f} {100 * pre:12.2f} {pre / mm:6.2f} {100 * post:12.2f} {post / mm:6.2f}")
    if allv: print(f"   全州   {'':4} {'':14} {'':10} {100 * allpre / allv:12.2f} {allpre / allv / mm:6.2f} {100 * allpost / allv:12.2f} {allpost / allv / mm:6.2f}   （{yy} 年の断面）")
    print(f'   （面積の行が無く落とした G 行 {dropped} 件／{seen} 件＝その種のその年の A 行に州が無い・平均の運び距離が数でない）')
    print('   ⚠水路の州（大河・支流）は陸脚の係数を 0.15・0.4 に置き換える（BilateralMarket）＝州の中の運びも同じ置き換えを仮定した。⚠荷が運び距離の平均だけ動くと置いた（実際は市場へ向かう荷と市場から来る荷が半々）。')

if __name__ == '__main__': main()
