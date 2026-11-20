# (1253) c127dc 設計の材料：発見の時計の候補ごとの紙の影（読むだけ・エンジンの関数を呼ばない）
# 使い方: python -X utf8 -I shadow2.py <data フォルダ> <出力 .txt>
# 影の定め（shadow.py と同じ）：engineer_s の T 行（8 四半期ごと・国×行の A と必要性 N）で 技術力′＋N ≧ 100 の国が最初に出た標本の年（伝播 0＝世界で最初の発見者の形・未到達 1951）。
# 盤面は 3 つ：基準（keep_1233/base_ins）・al の生（keep_1247_rej/alB＝発見が止まった後の軌道を含む）・al の一次の合成（基準の軌道の W だけを種ごとの al÷基準〔1785 年〕倍にした物＝技師の行を外した直後の量の変化だけ・発見の帰還を含まない）。
import sys, json, statistics as st, math

W0, WID, AM, TERM, GATE, BASE_DEC = 7854.08, 0.1, 12.5677, 70.0, 100.0, 1780
HIST = {'蒸気機関': 1783, '甜菜糖': 1801, '魚の缶詰': 1810, 'キニーネの単離': 1820, '黄鉄鉱法': 1838, '木材パルプ製紙': 1844, '過リン酸石灰': 1842,
        '転炉鋼': 1856, '石油灯油': 1859, '石油ケロシン': 1859, '鉄筋コンクリート': 1867, 'クロムなめし': 1884, '無煙火薬': 1884, '水力発電': 1882,
        '蒸気タービン': 1884, '電動機': 1888, '人絹': 1891, '石油火力': 1905, '窒素固定': 1913, '合成ゴム': 1916,
        'ガス灯': 1812, '缶詰': 1810, '汽船': 1812, '機関車': 1825, '加硫ゴム': 1839, 'ダイナマイト': 1867, '冷蔵': 1877, '石炭火力': 1882, '送電網': 1882,
        '電化製品': 1885, '自転車': 1885, '内燃機関': 1876, '自動車': 1886, 'ラジオ': 1920,
        '装甲艦': 1859, '機関銃': 1884, '弩級戦艦': 1906, '戦車': 1916, '軍用機': 1912, '化学兵器': 1915}   # scripts/instruments/need159/an_q11.py の表（AI の概数〈推〉）
HIST2 = {'ルブラン法': 1791, '鋳鉄の骨組み': 1796, 'ソルベー法': 1863, '合成染料': 1856, '電解': 1886}   # この台本で足した〈推〉（一般の技術史の概数）

def ramp(x):
    s = (x - 1.0) / WID + 0.5
    return 0.0 if not (s > 0) else 1.0 if s > 1 else s

def load(p):
    with open(p, 'rb') as f:
        return {int(k): v for k, v in json.loads(f.read().decode('utf-8')).items()}

def prep(d, d2):
    for s, v in d.items():
        for k in ('W', 'P', 'G'): v[k] = {int(a): b for a, b in v[k].items()}
        v['SUP'] = {int(a): b for a, b in d2[s]['SUP'].items()}
        v['POP'] = d2[s]['POP']
    return d

def yq(q): return 1783 + q // 4

def near(m, q):
    return m[q + 1] if q + 1 in m else m[q] if q in m else m[min(m, key=lambda k: abs(k - q))]

def integral(Wq):
    qs = sorted(Wq); K = {}; acc = 0.0; prev = None
    for q in qs:
        if prev is not None: acc += 0.5 * (Wq[q] + Wq[prev]) * (q - prev) / 4.0
        K[q] = acc; prev = q
    return K

def shadow(seed, tech_fn):
    first = {}
    for q, nat, row, dec, A, known, need, _ in seed['T']:
        d = (dec - BASE_DEC) / 10.0
        t = tech_fn(q, nat, d, A) if d > 0 else TERM
        if t + (need if need == need else 0) >= GATE:
            y = yq(q)
            if y < first.get(row, 9999): first[row] = y
    return first

C = lambda A: A / (A + AM) if A > 0 else 0.0

def tech(kind, seed, par, wscale=1.0):
    W = seed['W']; P = seed['P']; SUP = seed['SUP']; POP = seed['POP']
    if kind == 'cur':
        return lambda q, n, d, A: TERM * ramp(wscale * near(W, q) / (W0 * d)) * C(A)
    if kind == 'pc':     # 甲：W÷世界の人口 と r0×d（r0＝W0÷基準の 1785 年の世界の人口の中央）
        return lambda q, n, d, A: TERM * ramp((wscale * near(W, q) / P[q]) / (par * d)) * C(A)
    if kind == 'int':    # 乙：W の積分（人×年）と k0×d
        K = integral({k: wscale * v for k, v in W.items()})
        return lambda q, n, d, A: TERM * ramp(near(K, q) / (par * d)) * C(A)
    if kind == 'none':   # 丙：世界の時計なし
        return lambda q, n, d, A: TERM * C(A)
    if kind == 'grow':   # 丁：W÷開幕の W と g0×d（g0＝W0÷基準の 1785 年の W の中央）
        w8 = wscale * W[8]
        return lambda q, n, d, A: TERM * ramp((wscale * near(W, q) / w8) / (par * d)) * C(A)
    if kind in ('sup', 'sup2', 'sup3'):    # 戊：世界の高度労働力の供給の能力（factory_s の和・技師の行の表に依らない）と s0×d
        return lambda q, n, d, A: TERM * ramp(SUP[q] / (par * d)) * C(A)
    if kind == 'dens':   # 丙′：時計なし・足し算＝35×識字＋35×密度÷(密度＋h)（密度＝A÷国の人口〔千人〕）
        def f(q, n, d, A):
            pl = POP.get(f'{q}|{n}')
            if pl is None or not (pl[0] > 0): return 0.0
            lit = pl[1] if pl[1] == pl[1] else 0.0
            den = A / pl[0] if A > 0 else 0.0
            return 35.0 * lit + 35.0 * den / (den + par)
        return f
    raise ValueError(kind)

def run(kind, arm, par, synth=None):
    return [shadow(arm[s], tech(kind, arm[s], par, synth[s] if synth else 1.0)) for s in sorted(arm)]

def med(l, r): return st.median([x.get(r, 1951) for x in l])
def cnt(l, r): return sum(x.get(r, 1951) < 1951 for x in l)
def nrows(l): return st.median([sum(1 for r in x if x[r] < 1951) for x in l])

dd = sys.argv[1]
B = prep(load(f'{dd}/base.json'), load(f'{dd}/base2.json'))
A = prep(load(f'{dd}/alB.json'), load(f'{dd}/alB2.json'))
out = open(sys.argv[2], 'w', encoding='utf-8', newline='\n')
def pr(*a): print(*a, file=out)

dec_of = {}
for arm in (B, A):
    for s in arm.values():
        for q, nat, row, dec, *_ in s['T']: dec_of[row] = dec
rows = sorted(dec_of, key=lambda r: (dec_of[r], r))
rows_d = [r for r in rows if dec_of[r] > BASE_DEC]
synth = {s: A[s]['W'][8] / B[s]['W'][8] for s in B}
pr('al の一次の合成の倍率（種ごとの al÷基準・1785 年の W）：' + ' '.join(f'{synth[s]:.3f}' for s in sorted(synth)))

curB = run('cur', B, None)
def calib(kind, grid):
    best = None
    for p in grid:
        b = run(kind, B, p)
        e = st.median([abs(med(b, r) - med(curB, r)) for r in rows_d])
        m = st.mean([abs(med(b, r) - med(curB, r)) for r in rows_d])
        if best is None or (e, m) < best[:2]: best = (e, m, p)
    return best
P8 = st.median([B[s]['P'][8] for s in B]); W8 = st.median([B[s]['W'][8] for s in B])
cal = {'cur': (0,0,None), 'sup': (None,None,4329.8), 'sup2': (None,None,7000), 'sup3': (None,None,9000)}
names = {'cur': '今の式', 'sup': '戊 規則の定数 4330', 'sup2': '戊 7000', 'sup3': '戊 9000'}
for k, v in cal.items():
    pr(f'{names[k]}：定数 {v[2]}' + ('' if v[0] in (0, None) and k in ('cur', 'none') else f'（基準の盤面で今の式の影との年の差 中央 {v[0]}・平均 {v[1] if v[1] is None else round(v[1],1)}）'))

R = {}
for k in names:
    p = cal[k][2]
    R[k] = (curB if k == 'cur' else run(k, B, p), run(k, A, p), run(k, B, p, synth))

pr('\n== 行ごとの影（種の中央の年〔到達した種〕）：基準／al の生／al の一次の合成')
pr('行\t年代\t史実〈推〉\t' + '\t'.join(names[k] for k in names))
for r in rows:
    h = HIST.get(r, HIST2.get(r, ''))
    cells = [f'{med(b,r):g}({cnt(b,r)})/{med(a,r):g}({cnt(a,r)})/{med(s,r):g}({cnt(s,r)})' for b, a, s in (R[k] for k in names)]
    pr(f'{r}\t{dec_of[r]}\t{h}\t' + '\t'.join(cells))

pr('\n== 候補ごとの要約（甜菜糖は al の狙いの行＝ずれの数から除く）')
pr('候補\t行の数 基準/生/合成\t基準で今の式から>10年動いた行（早/遅）\t合成で基準から>10年ずれた行（遅/早）\t生で基準から>10年ずれた行（遅/早）\t1790 年までに開く行（基準）\t史実との差の中央（基準・到達した行）\t史実の 10 年前より早い行（基準）')
for k in names:
    b, a, s = R[k]
    mv = [med(b, r) - med(curB, r) for r in rows_d]
    ds = [med(s, r) - med(b, r) for r in rows if r != '甜菜糖']
    da = [med(a, r) - med(b, r) for r in rows if r != '甜菜糖']
    hd = [med(b, r) - HIST.get(r, HIST2.get(r)) for r in rows if (r in HIST or r in HIST2) and med(b, r) < 1951]
    early = sum(1 for r in rows if (r in HIST or r in HIST2) and med(b, r) < HIST.get(r, HIST2.get(r, 0)) - 10)
    pr(f'{names[k]}\t{nrows(b)}/{nrows(a)}/{nrows(s)}\t{sum(abs(x)>10 for x in mv)}（{sum(x<-10 for x in mv)}/{sum(x>10 for x in mv)}）\t'
       f'{sum(abs(x)>10 for x in ds)}（{sum(x>10 for x in ds)}/{sum(x<-10 for x in ds)}）\t{sum(abs(x)>10 for x in da)}（{sum(x>10 for x in da)}/{sum(x<-10 for x in da)}）\t'
       f'{sum(med(b,r)<=1790 for r in rows)}\t{st.median(hd) if hd else "nan"}\t{early}')

pr('\n== 供給の能力 SUP と W の比（種の中央）・al の生÷基準')
for q in (8, 32, 64, 128, 200, 272, 400, 536, 664):
    sb = st.median([B[s]['SUP'][q] for s in B]); wb = st.median([B[s]['W'][q] for s in B])
    ra = st.median([A[s]['SUP'][q] / B[s]['SUP'][q] for s in B])
    pr(f' {yq(q)} SUP {sb:.0f} W {wb:.0f} SUP÷W {sb/wb:.3f} SUP の al÷基準 {ra:.3f}')
out.close()
