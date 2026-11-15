# (1149) c112il 読み手：機関車の行の〈輸入で学ぶ道〉。読むだけ・書くのは標準出力だけ。
#   python -X utf8 -I loco_import.py <ins の生> <種の始め> <種の終わり>
# 読む表：disc_s（T・E 行）・disccontact_s（機関車の行）・discflow_s（運び手の世界の越境の額）・railspend_s（discEnd）・nationgood_s（越境の輸入の額）。
# 列は名で引く（★(749)）。計器が書かない物は数えない（0 と数えない）。
import sys, os, math
from collections import defaultdict

ins, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
LOCO, STEAM = '機関車', '蒸気機関'
CARR = ('機関車', '蒸気機関', '汽船')
HALF, TMAX = 0.05, 70.0   # Coefficients.p06:109 DiscoveryContactHalf・Discovery.cs:11 TermMax（ソースから写した）

def rd(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            if len(c) < len(h): continue
            yield ix, c

def fl(x):
    try: return float(x)
    except ValueError: return float('nan')

T = defaultdict(float)   # 合計の数え
carr_sum = defaultdict(float); carr_rows = defaultdict(int)
route = defaultdict(int)
match = defaultdict(int)
imp_dec = defaultdict(float)     # (財, 年代) → 発見の前の国の越境の輸入の額（nationgood_s・売り手を問わない）
dem_pre = defaultdict(float)
p3_lines = []
e_terms = []
EX = defaultdict(float)
wimp = defaultdict(float)   # (財, 年代) → 全ての国の越境の輸入の額（分母の太り方の見込み）
for s in range(s0, s1 + 1):
    pd = os.path.join(ins, f'disc_s{s}.tsv')
    if not os.path.exists(pd): print(f'種 {s}：disc_s が無い'); continue
    eq_loco = {}; eq_steam = {}; t_loco = {}; t_steam_seen = set(); names = {}
    for ix, c in rd(pd):
        k, q, nat, row, uid = c[ix['kind']], int(c[ix['quarter']]), c[ix['nation']], c[ix['row']], c[ix['nationUid']]
        names[uid] = nat
        if row == LOCO:
            tt = (fl(c[ix['techTerm']]), fl(c[ix['needTerm']]), fl(c[ix['transmitTerm']]))
            if k == 'E': eq_loco[uid] = (q, tt)
            elif k == 'T': t_loco[(q, uid)] = tt[2]
        elif row == STEAM:
            if k == 'E': eq_steam[uid] = q
            elif k == 'T': t_steam_seen.add(uid)
    # 機関車を発見の時に知らなかった国＝T 行のある国（開幕から知る国は T 行が無い）
    unknown0 = {u for (_, u) in t_loco}
    steam_q = {}
    for u in names:
        if u in eq_steam: steam_q[u] = eq_steam[u]
        elif u not in t_steam_seen: steam_q[u] = -1   # 開幕から知る（T 行も E 行も無い）
    # 経路（E 行）：自国＝tech+need≥100
    for u, (q, (te, ne, tr)) in eq_loco.items():
        route['E 行'] += 1
        if te + ne >= 100.0: route['自国の項だけで 100 に届く'] += 1
        elif te + tr >= 100.0 and ne + tr < 100.0: route['tech＋伝わり'] += 1
        elif ne + tr >= 100.0: route['need＋伝わり（tech＋need＜100）'] += 1
        else: route['3 項の和で届く'] += 1
        e_terms.append((te, ne, tr))
    # 運び手の世界の越境の額
    den = defaultdict(float)
    for ix, c in rd(os.path.join(ins, f'discflow_s{s}.tsv')):
        if c[ix['good']] in CARR: den[int(c[ix['quarter']])] += fl(c[ix['valueFlowQ']])
    imp = defaultdict(float)   # (q, buyerUid) → 知る国から買った運び手の額
    buyers = set()
    for ix, c in rd(os.path.join(ins, f'disccontact_s{s}.tsv')):
        if c[ix['row']] != LOCO: continue
        q = int(c[ix['quarter']]); v = fl(c[ix['valuePerQuarter']]); g = c[ix['carrier']]; b = c[ix['buyerUid']]
        carr_sum[g] += v; carr_rows[g] += 1
        imp[(q, b)] += v; buyers.add(b)
    # 組み直しとエンジンの値の突き合わせ（T 行と E 行の伝わりの項）
    eng = dict(t_loco)
    e_keys = {(q, u): (tt[0], tt[1]) for u, (q, tt) in eq_loco.items()}
    for u, (q, tt) in eq_loco.items(): eng[(q, u)] = tt[2]
    for key, ev in eng.items():
        q, u = key
        x = imp.get(key, 0.0); d = den.get(q, 0.0)
        r = TMAX * (x / d) / (x / d + HALF) if (x > 0 and d > 1e-12) else 0.0
        if not math.isfinite(ev): match['エンジンの値が非有限'] += 1; continue
        if x > 0:
            if abs(r - ev) <= 1e-6 * max(1.0, abs(ev)): match['[輸入あり] 一致'] += 1
            elif r < ev: match['[輸入あり] 組み直し＜エンジン'] += 1
            else: match['[輸入あり] 組み直し＞エンジン'] += 1
        elif ev > 0: match['[輸入 0] エンジン＞0（技師の移動だけ〈推〉）'] += 1
        if key in e_keys and ev < TMAX:
            xe = HALF * ev / (TMAX - ev); xi = (x / d) if (x > 0 and d > 1e-12) else 0.0
            te, ne = e_keys[key]
            if te + ne < 100.0:
                EX['伝わりが要った E 行'] += 1
                EX['x の輸入の分'] += min(xi, xe); EX['x の技師の分'] += max(0.0, xe - xi)
                if te + ne + r >= 100.0: EX['輸入の分だけで 100 に届いた'] += 1
                if xi > 0: EX['輸入が 0 でない'] += 1
        if r == 0 and ev == 0: match['両方 0'] += 1
        elif abs(r - ev) <= 1e-6 * max(1.0, abs(ev)): match['一致（相対 1e-6）'] += 1
        elif r < ev: match['組み直し＜エンジン（技師の移動の分〈推〉）'] += 1
        else: match['組み直し＞エンジン（合わない）'] += 1
    # P3：q400 に知らない国のうち、q228〔1840〕〜q400 に知る国から運び手を 1 度も買わなかった国
    known400 = {}
    for ix, c in rd(os.path.join(ins, f'railspend_s{s}.tsv')):
        q = int(c[ix['q']])
        if q <= 400: known400[c[ix['nation']]] = (q, c[ix['discEnd']])
    unk400 = {n for n, (q, d) in known400.items() if d == '0' and q >= 396}
    uid_of = {}
    for u, n in names.items(): uid_of.setdefault(n, set()).add(u)
    for n in sorted(unk400):
        us = uid_of.get(n, set())
        got = sum(v for (q, b), v in imp.items() if b in us and 228 <= q <= 400)
        trmax = max([ev for (q, u), ev in t_loco.items() if u in us and 228 <= q <= 400] or [float('nan')])
        st = min([steam_q[u] for u in us if u in steam_q] or [99999])
        T['q400 に知らない国'] += 1
        if got > 0: T['そのうち 1840〜1883 に運び手を買った国'] += 1
        else:
            T['そのうち買わなかった国'] += 1
            if st > 400: T['買わなかった国のうち蒸気機関も q400 に知らない'] += 1
            p3_lines.append(f'種 {s} {n}：運び手の買い 0・伝わりの項の最大 {trmax:.3g}・蒸気機関の発見 q{st if st < 99999 else "なし"}')
    T['種'] += 1
    T['運び手を買った国（発見の前・延べ）'] += len(buyers)
    # nationgood_s：発見の前の国（年の終わりに知らない）の越境の輸入の額と需要
    lq = {}
    for u in names:
        if u in eq_loco: lq[names[u]] = min(lq.get(names[u], 99999), eq_loco[u][0])
        elif u in unknown0: lq.setdefault(names[u], 99999)
    for ix, c in rd(os.path.join(ins, f'nationgood_s{s}.tsv')):
        g = c[ix['good']]
        if g not in CARR and g != '線路': continue
        y = int(c[ix['year']]); n = c[ix['nation']]
        dec = (y // 10) * 10
        v = fl(c[ix['crossImportValue']]); dq = fl(c[ix['demandQty']])
        if math.isfinite(v): wimp[(g, dec)] += v
        if n not in lq or lq[n] <= (y - 1783) * 4 + 3: continue
        if math.isfinite(v): imp_dec[(g, dec)] += v
        if math.isfinite(dq): dem_pre[(g, dec)] += dq

print(f'== {ins} 種 {s0}〜{s1}（読めた種 {int(T["種"])}）')
tot = sum(carr_sum.values())
print('知る国から買った運び手の額（機関車の行・発見の前の国・全ての四半期の和）')
for g in CARR: print(f'  {g}：額 {carr_sum[g]:.4g}（{carr_sum[g] / tot if tot else float("nan"):.4%}）・行 {carr_rows[g]}')
print(f'  運び手を買った国（延べ・種の和）{int(T["運び手を買った国（発見の前・延べ）"])}')
print('機関車の発見の経路（E 行）'); [print(f'  {k}：{v}') for k, v in route.items()]
if e_terms:
    import statistics as st
    for i, nm in enumerate(('tech', 'need', '伝わり')): print(f'  E 行の {nm} の中央 {st.median(t[i] for t in e_terms):.3g}')
print('伝わりの項の組み直し（輸入だけ）とエンジンの値（T・E 行）'); [print(f'  {k}：{v}') for k, v in sorted(match.items())]
print('伝わりが要った E 行（tech＋need＜100）の接触 x の内訳（輸入＝組み直し・技師＝エンジンの x−輸入〈推〉）'); [print(f'  {k}：{v:.4g}') for k, v in EX.items()]
print('全ての国の越境の輸入の額（nationgood_s・年代の和）＝運び手に線路を足したときの分母の太り方')
for g in CARR + ('線路',):
    print('  ' + g + '：' + '・'.join(f'{d}年代 {wimp[(g, d)]:.3g}' for d in range(1820, 1910, 10)))
print('P3（q400 に機関車を知らない国）'); [print(f'  {k}：{int(v)}') for k, v in T.items() if k.startswith('q400') or k.startswith('そのうち') or k.startswith('買わなかった')]
for l in p3_lines[:40]: print('   ', l)
if len(p3_lines) > 40: print(f'    …ほか {len(p3_lines) - 40} 行')
print('発見の前の国の越境の輸入の額（nationgood_s・売り手を問わない・年代の和）と需要の量')
for g in CARR + ('線路',):
    print('  ' + g + '：' + '・'.join(f'{d}年代 {imp_dec[(g, d)]:.3g}／需要 {dem_pre[(g, d)]:.3g}' for d in range(1820, 1910, 10)))
