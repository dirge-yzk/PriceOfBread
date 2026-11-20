# (1259) c128dc 設計の材料 2 本目：案 A（供給の側の時計・帰還込み）と案 B（時計の廃止＋木の前提＋足し算の技術力）を同じ物差しで紙の影にする（読むだけ・エンジンの関数を呼ばない）
# 使い方: python -X utf8 -I shadow3.py <data フォルダ> <出力 .txt>
# 影の定め（r1253/shadow2.py と同じ）：engineer_s の T 行（8 四半期ごと・国×行の A と必要性 N）で 技術力′＋N ≧ 100 の国が最初に出た標本の年（伝播 0・未到達 1951）。
# この版で足した物：①今ある前提の辺（Catalog.cs の After 10 本）を全ての候補に当てる（前提の行が前の標本までに開いた国だけ）②案 A の帰還込みの al の盤面（基準の供給×al÷基準の軌道・(1254) の追記の 1794〜1797 年の値）③案 B の木（AI の案の辺〈推〉）④N1 の盤面。
import sys, json, statistics as st

W0, WID, AM, TERM, GATE, BASE_DEC = 7854.08, 0.1, 12.5677, 70.0, 100.0, 1780
HIST = {'蒸気機関': 1783, '甜菜糖': 1801, '魚の缶詰': 1810, 'キニーネの単離': 1820, '黄鉄鉱法': 1838, '木材パルプ製紙': 1844, '過リン酸石灰': 1842,
        '転炉鋼': 1856, '石油灯油': 1859, '石油ケロシン': 1859, '鉄筋コンクリート': 1867, 'クロムなめし': 1884, '無煙火薬': 1884, '水力発電': 1882,
        '蒸気タービン': 1884, '電動機': 1888, '人絹': 1891, '石油火力': 1905, '窒素固定': 1913, '合成ゴム': 1916,
        'ガス灯': 1812, '缶詰': 1810, '汽船': 1812, '機関車': 1825, '加硫ゴム': 1839, 'ダイナマイト': 1867, '冷蔵': 1877, '石炭火力': 1882, '送電網': 1882,
        '電化製品': 1885, '自転車': 1885, '内燃機関': 1876, '自動車': 1886, 'ラジオ': 1920,
        '装甲艦': 1859, '機関銃': 1884, '弩級戦艦': 1906, '戦車': 1916, '軍用機': 1912, '化学兵器': 1915,
        'ルブラン法': 1791, '鋳鉄の骨組み': 1796, 'ソルベー法': 1863, '合成染料': 1856, '電解': 1886}   # need159/an_q11.py の表＋r1253 で足した 5 行（全て AI の概数〈推〉）
# 今ある前提の辺（PriceOfBread.SimCore/Catalog.cs の Discoveries の After・main f442e96a0）
EDGE_NOW = {'自転車': '加硫ゴム', '内燃機関': '蒸気機関', '自動車': '内燃機関', 'ラジオ': '電化製品', '装甲艦': '汽船', '弩級戦艦': '装甲艦',
            '戦車': '内燃機関', '軍用機': '内燃機関', '電解': '石炭火力'}
# 案 B の木（AI の案・確証なし〈推〉）：今の辺に足す 1 本ずつ（直線系・前提は 1 つ）。根は 甜菜糖・缶詰・ガス灯・ルブラン法・鋳鉄の骨組み・キニーネの単離・加硫ゴム・汽船（前提は開幕の既知の蒸気機関＝遅らせない）。
EDGE_B = {'魚の缶詰': '缶詰', '黄鉄鉱法': 'ルブラン法', '過リン酸石灰': '黄鉄鉱法', 'ソルベー法': 'ルブラン法', 'ダイナマイト': '黄鉄鉱法',
          '無煙火薬': 'ダイナマイト', '合成染料': 'ガス灯', 'クロムなめし': '合成染料', '化学兵器': '合成染料', '石油灯油': 'ガス灯',
          '石油ケロシン': '石油灯油', '機関車': '汽船', '転炉鋼': '機関車', '鉄筋コンクリート': '転炉鋼', '機関銃': '転炉鋼',
          '木材パルプ製紙': '鋳鉄の骨組み', '人絹': '木材パルプ製紙', '冷蔵': '缶詰', '石炭火力': '鉄筋コンクリート', '送電網': '石炭火力',
          '蒸気タービン': '石炭火力', '水力発電': '送電網', '電動機': '送電網', '電化製品': '送電網', '石油火力': '蒸気タービン',
          '窒素固定': '過リン酸石灰', '合成ゴム': '加硫ゴム'}

DEC0 = {}   # 行の年代（年代≦ 1780 の前提は開幕から既知）

def ramp(x):
    s = (x - 1.0) / WID + 0.5
    return 0.0 if not (s > 0) else 1.0 if s > 1 else s

def load(p):
    with open(p, 'rb') as f:
        d = {int(k): v for k, v in json.loads(f.read().decode('utf-8')).items()}
    for v in d.values():
        for k in ('W', 'SUP', 'SPK'): v[k] = {int(a): b for a, b in v[k].items()}
        v['T'].sort(key=lambda r: r[0])
    return d

def yq(q): return 1783 + q // 4
def near(m, q): return m[q + 1] if q + 1 in m else m[q] if q in m else m[min(m, key=lambda k: abs(k - q))]
C = lambda A: A / (A + AM) if A > 0 else 0.0

def shadow(seed, tech_fn, edges):
    first = {}; firstq = {}   # 国ごとの開いた四半期（前提の判定）と世界で最初の年
    nat_open = {}
    for q, nat, row, dec, A, known, need, tt in seed['T']:
        if (nat, row) in nat_open: continue
        pre = edges.get(row)
        if pre is not None and DEC0.get(pre, 9999) > BASE_DEC:
            pq = nat_open.get((nat, pre))
            if pq is None or not (pq < q): continue
        d = (dec - BASE_DEC) / 10.0
        t = tech_fn(q, nat, d, A, tt) if d > 0 else TERM
        if t + (need if need == need else 0) >= GATE:
            nat_open[(nat, row)] = q
            y = yq(q)
            if y < first.get(row, 9999): first[row] = y
    return first

def interp(knots, q):
    if q <= knots[0][0]: return knots[0][1]
    for (q0, v0), (q1, v1) in zip(knots, knots[1:]):
        if q <= q1: return v0 + (v1 - v0) * (q - q0) / (q1 - q0)
    return knots[-1][1]

# al÷基準の軌道（供給の能力）：1783〜1791 は m1253 supply.txt、1794〜1797 は (1254) の追記の中央（生 alB2.json・今は消えた）
K_EARLY = [(0, 0.991), (8, 0.987), (32, 0.977), (44, 0.918), (48, 0.855), (52, 0.773), (56, 0.714)]
PROF = {
    'P1 1797 で止まる（0.714 のまま）': K_EARLY,
    'P1lo 1797 の種の最小 0.644 のまま': K_EARLY[:-1] + [(56, 0.644)],
    'P1hi 1797 の種の最大 0.775 のまま': K_EARLY[:-1] + [(56, 0.775)],
    'P2 al の生の軌道（今の式の錠の帰還を含む）': K_EARLY + [(64, 0.651), (128, 0.624), (200, 0.482), (272, 0.490), (400, 0.362), (536, 0.361), (664, 0.492)],
}
# 今の式の al の盤面（W の al÷基準・m1253 材料 §4-1 の種の中央＝錠を含む生の軌道）
W_AL = [(8, 0.757), (32, 0.794), (64, 0.583), (128, 0.504), (200, 0.464), (272, 0.331), (400, 0.310), (536, 0.324), (664, 0.314)]

def tech(kind, seed, par, prof=None):
    W = seed['W']; SUP = seed['SUP']; POP = seed['POP']; NSUP = seed['NSUP']
    sc = (lambda q: interp(prof, q)) if prof else (lambda q: 1.0)
    if kind == 'obs':   # 記録された技術力の項（T 行の techTerm）＝今の式の写し・N1 でも読める
        return lambda q, n, d, A, tt: tt if tt == tt else 0.0
    if kind == 'cur':
        return lambda q, n, d, A, tt: TERM * ramp(sc(q) * near(W, q) / (W0 * d)) * C(A)
    if kind == 'sup':
        return lambda q, n, d, A, tt: TERM * ramp(sc(q) * SUP[q] / (par * d)) * C(A)
    if kind == 'none':
        return lambda q, n, d, A, tt: TERM * C(A)
    if kind == 'dens':   # 丙′：35×識字＋35×密度÷(密度＋h)（密度＝その行の親の産業の技師 A÷国の人口〔千人〕・分野別）
        def f(q, n, d, A, tt):
            pl = POP.get(f'{q}|{n}')
            if pl is None or not (pl[0] > 0): return 0.0
            lit = pl[1] if pl[1] == pl[1] else 0.0
            den = A / pl[0] if A > 0 else 0.0
            return 35.0 * lit + 35.0 * den / (den + par)
        return f
    if kind == 'lit':    # 丙‴：識字だけ（重み par・上限 70 の置き換え）＝技師の行の表にも技師の階級にも依らない
        def f(q, n, d, A, tt):
            pl = POP.get(f'{q}|{n}')
            if pl is None: return 0.0
            return par * (pl[1] if pl[1] == pl[1] else 0.0)
        return f
    if kind == 'cls':    # 丙″：35×識字＋35×e÷(e＋h)（e＝国の技師の階級の人数÷国の人口・分野別でない）
        def f(q, n, d, A, tt):
            pl = POP.get(f'{q}|{n}'); ns = NSUP.get(f'{q}|{n}')
            if pl is None or not (pl[0] > 0) or ns is None: return 0.0
            lit = pl[1] if pl[1] == pl[1] else 0.0
            e = sc(q) * ns[1] / pl[0]
            return 35.0 * lit + 35.0 * e / (e + par)
        return f
    raise ValueError(kind)

def run(kind, arm, par, edges, prof=None):
    return [shadow(arm[s], tech(kind, arm[s], par, prof), edges) for s in sorted(arm)]

def med(l, r): return st.median([x.get(r, 1951) for x in l])
def nrows(l): return st.median([sum(1 for r in x if x[r] < 1951) for x in l])

dd = sys.argv[1]
B = load(f'{dd}/base.json'); N = load(f'{dd}/n1.json')
out = open(sys.argv[2], 'w', encoding='utf-8', newline='\n')
def pr(*a): print(*a, file=out, flush=True)

dec_of = {}
for s in B.values():
    for q, nat, row, dec, *_ in s['T']: dec_of[row] = dec
rows = sorted(dec_of, key=lambda r: (dec_of[r], r))
rows_d = [r for r in rows if dec_of[r] > BASE_DEC]
DEC0.update(dec_of)
E1 = dict(EDGE_NOW); E2 = dict(EDGE_NOW); E2.update(EDGE_B)

def obs_first(arm):
    res = []
    for s in sorted(arm):
        f = {}
        for q, y, nat, row, *_ in arm[s]['D']:
            if y < f.get(row, 9999): f[row] = y
        res.append(f)
    return res
obsB, obsN = obs_first(B), obs_first(N)
curB = run('cur', B, None, E1); obsTB = run('obs', B, None, E1); obsTN = run('obs', N, None, E1)
pr(f'行の数 {len(rows)}（d＞0 は {len(rows_d)}）・今ある前提の辺 {len(EDGE_NOW)}・案 B で足す辺 {len(EDGE_B)}〈推〉')
pr(f'校正：観測（disc_s の E）の行の数 基準 {nrows(obsB)}・N1 {nrows(obsN)}／影（W の今の式）基準 {nrows(curB)}／影（techTerm の写し）基準 {nrows(obsTB)}・N1 {nrows(obsTN)}')
dd_ = [med(curB, r) - med(obsB, r) for r in rows_d]
pr(f'  基準の影（W の今の式）と観測の差：中央 {st.median(dd_)}・10 年超 {sum(abs(x)>10 for x in dd_)} 行')
dd2 = [med(curB, r) - med(obsTB, r) for r in rows_d]
pr(f'  W の今の式と techTerm の写しの差：10 年超 {sum(abs(x)>10 for x in dd2)} 行（0 なら N1 の今の式は techTerm の写しで読める）')

def calib(kind, grid, edges):
    best = None
    for p in grid:
        b = run(kind, B, p, edges)
        e = st.median([abs(med(b, r) - med(curB, r)) for r in rows_d]); m = st.mean([abs(med(b, r) - med(curB, r)) for r in rows_d])
        if best is None or (e, m) < best[:2]: best = (e, m, p)
    return best

def moved(a, b, rs, skip=('甜菜糖',)):
    x = [med(a, r) - med(b, r) for r in rs if r not in skip]
    return sum(abs(v) > 10 for v in x), sum(v > 10 for v in x), sum(v < -10 for v in x)

def hist_cols(b):
    hd = [med(b, r) - HIST[r] for r in rows if r in HIST and med(b, r) < 1951]
    early = sum(1 for r in rows if r in HIST and med(b, r) < HIST[r] - 10)
    return (st.median(hd) if hd else float('nan')), early, sum(med(b, r) <= 1790 for r in rows)

s_sup = calib('sup', list(range(2000, 12001, 500)), E1)
pr(f'\n戊（案 A）の定数 s0＝{s_sup[2]}（基準 m1254 の盤面で今の式の影との年の差 中央 {s_sup[0]}・平均 {s_sup[1]:.1f}・格子 500）')
supB = run('sup', B, s_sup[2], E1)

# ── 問い 1：案 A の帰還込みの見積もり
pr('\n== 問い 1：案 A（戊）の al の盤面＝基準の供給×al÷基準の軌道（甜菜糖を除く・基準の戊の影との差）')
pr('軌道\t行の数\t10 年超の遅れ\t0〜10 年の遅れ\t遅れの中央（遅れた行）\t遅れの最大（未到達を除く）\t未到達になる行')
A_res = {}
for name, prof in PROF.items():
    a = run('sup', B, s_sup[2], E1, prof); A_res[name] = a
    dl = [(r, med(a, r) - med(supB, r)) for r in rows_d if r != '甜菜糖' and med(supB, r) < 1951]
    late = [v for r, v in dl if v > 0]
    gone = [r for r, v in dl if med(a, r) >= 1951]
    fin = [v for r, v in dl if med(a, r) < 1951]
    pr(f'{name}\t{nrows(a)}\t{sum(v>10 for _,v in dl)}\t{sum(0<v<=10 for _,v in dl)}\t{st.median(late) if late else 0}\t{max(fin) if fin else 0}\t{len(gone)}')
for name, prof in (('今の式 W×al の生の軌道', W_AL), ('今の式 W×0.757（一次・帰還なし）', [(0, 0.757)])):
    a = run('cur', B, None, E1, prof); A_res[name] = a
    dl = [(r, med(a, r) - med(curB, r)) for r in rows_d if r != '甜菜糖' and med(curB, r) < 1951]
    late = [v for r, v in dl if v > 0]
    pr(f'{name}\t{nrows(a)}\t{sum(v>10 for _,v in dl)}\t{sum(0<v<=10 for _,v in dl)}\t{st.median(late) if late else 0}\t-\t{sum(med(a,r)>=1951 for r,_ in dl)}')
pr('\n行ごと（種の中央の年）：行\t年代\t史実〈推〉\t基準 今の式\t基準 戊\t戊×P1\t戊×P1lo\t戊×P2\t今の式×al の生の W')
k1 = list(PROF)
for r in rows_d:
    pr(f'{r}\t{dec_of[r]}\t{HIST.get(r,"")}\t{med(curB,r):g}\t{med(supB,r):g}\t{med(A_res[k1[0]],r):g}\t{med(A_res[k1[1]],r):g}\t{med(A_res[k1[3]],r):g}\t{med(A_res["今の式 W×al の生の軌道"],r):g}')

# ── 問い 2：案 B
pr('\n== 問い 2：案 B（時計なし）・今の辺だけ／案 B の辺を足す')
cand = {}
cand['今の式'] = (curB, run('obs', N, None, E1), None)
cand['案 A 戊'] = (supB, None, A_res[k1[0]])
for en, ed in (('今の辺', E1), ('木', E2)):
    cand[f'丙 時計なし・{en}'] = (run('none', B, None, ed), run('none', N, None, ed), None)
    h1 = calib('dens', [1e-5, 3e-5, 1e-4, 3e-4, 1e-3, 3e-3, 1e-2], ed)
    cand[f'丙′ 識字＋分野の技師密度・{en}（h={h1[2]}）'] = (run('dens', B, h1[2], ed), run('dens', N, h1[2], ed), None)
    h2 = calib('cls', [1e-4, 3e-4, 1e-3, 2e-3, 3e-3, 5e-3, 1e-2, 3e-2, 0.1, 0.3, 1.0], ed)
    cand[f'丙″ 識字＋国の技師の階級の比・{en}（h={h2[2]}）'] = (run('cls', B, h2[2], ed), None, run('cls', B, h2[2], ed, K_EARLY))
    w3 = calib('lit', [30, 40, 50, 60, 70, 80, 90, 100, 120], ed)
    cand[f'丙‴ 識字だけ・{en}（重み={w3[2]}）'] = (run('lit', B, w3[2], ed), run('lit', N, w3[2], ed), 'same')
    if en == '木':
        hh = None
        for h in [1e-4, 3e-4, 1e-3, 3e-3, 1e-2, 3e-2, 0.1, 0.3, 1.0]:
            bb = run('cls', B, h, ed); v = [abs(med(bb, r) - HIST[r]) for r in rows_d if r in HIST]
            k = (st.median(v), st.mean(v))
            if hh is None or k < hh[0]: hh = (k, h, bb)
        cand[f'丙″ 木・史実で選んだ h={hh[1]}（参考）'] = (hh[2], None, run('cls', B, hh[1], ed, K_EARLY))
pr('候補\t行の数 基準\t基準の今の式から 10 年超（計・遅/早）\tal の盤面で 10 年超（計・遅/早）\tN1 の幅 10 年超（計・遅/早・記録だけ）\t1790 年までに開く行\t史実との差の中央\t史実の 10 年前より早い行')
for k, (b, n, a) in cand.items():
    m1 = moved(b, curB, rows_d, ())
    ma = (0, 0, 0, '作りの上') if a == 'same' else moved(a, b, rows_d) if a is not None else ('読めない',)
    mn = moved(obsN, obsB, rows_d, ()) + ('観測',) if k == '今の式' else moved(n, b, rows_d, ()) if n is not None else ('読めない',)
    hc = hist_cols(b)
    pr(f'{k}\t{nrows(b)}\t{m1}\t{ma}\t{mn}\t{hc[2]}\t{hc[0]}\t{hc[1]}')
obs_n = moved(obsN, obsB, rows_d, ())
pr(f'参考：観測（disc_s の E）の N1 の幅 10 年超 {obs_n}')
pr('\n行ごと（基準の種の中央）：行\t年代\t史実〈推〉\t' + '\t'.join(cand))
for r in rows_d:
    pr(f'{r}\t{dec_of[r]}\t{HIST.get(r,"")}\t' + '\t'.join(f'{med(v[0], r):g}' for v in cand.values()))
out.close()
