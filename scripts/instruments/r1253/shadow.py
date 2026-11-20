# (1253) c127dc 設計の材料：発見の時計の紙の影（読むだけ・エンジンの関数を呼ばない）
# 使い方: python -X utf8 -I shadow.py <data/base.json> <data/alB.json> <出力 .txt>
# 紙の影の定め：行ごと・種ごとに、engineer_s の T 行（8 四半期ごと・国×行の A と必要性）で
#   〈技術力′＋必要性 ≧ 100〉となる国が最初に現れた標本の年を、その種のその行の〈影の年〉とする（伝播は 0 と置く＝世界で最初の発見者の形）。
#   技術力′＝70×S′×C（d＞0）・70（d≦0）・C＝A÷(A＋Am)。S′ は候補ごとの時計。未到達は 1951。
#   限界：①8 四半期の格子＝影は観測より最大 2 年遅れる側に偏る ②軌道は観測のまま（候補で発見が遅れた後の盤面は基線の発見の後の物）。
import sys, json, statistics as st, math

W0, WID, AM, TERM, GATE = 7854.08, 0.1, 12.5677, 70.0, 100.0
BASE_DEC = 1780

def ramp(x):
    s = (x - 1.0) / WID + 0.5
    return 0.0 if not (s > 0) else 1.0 if s > 1 else s

def load(p):
    with open(p, 'rb') as f:
        d = json.loads(f.read().decode('utf-8'))
    out = {}
    for s, v in d.items():
        v['W'] = {int(k): x for k, x in v['W'].items()}
        v['P'] = {int(k): x for k, x in v['P'].items()}
        v['G'] = {int(k): x for k, x in v['G'].items()}
        out[int(s)] = v
    return out

def year_of(q): return 1783 + q // 4

def integral(Wq):
    # 人×年（W は月あたりの人＝雇用の量）：標本の間を台形で
    qs = sorted(Wq); K = {}; acc = 0.0; prev = None
    for q in qs:
        if prev is not None:
            acc += 0.5 * (Wq[q] + Wq[prev]) * (q - prev) / 4.0
        K[q] = acc; prev = q
    return K

def shadow(seed, clock):
    # clock(q, decade, A) -> S（d＞0 の行だけ呼ぶ）
    first = {}
    for q, nat, row, dec, A, known, need, tech in seed['T']:
        d = (dec - BASE_DEC) / 10.0
        if d > 0:
            S = clock(q, dec)
            C = A / (A + AM) if A > 0 else 0.0
            t = TERM * S * C
        else:
            t = TERM
        if t + (need if need == need else 0) >= GATE:
            y = year_of(q)
            if y < first.get(row, 9999): first[row] = y
    return first

def observed(seed):
    f = {}
    for q, y, nat, row, tech, need, tr in seed['D']:
        if y < f.get(row, 9999): f[row] = y
    return f

def table(arm, name_fn):
    return [name_fn(s, arm[s]) for s in sorted(arm)]

def med(lst, row): return st.median([x.get(row, 1951) for x in lst])
def cnt(lst, row): return sum(x.get(row, 1951) < 1951 for x in lst)

B = load(sys.argv[1]); A = load(sys.argv[2]); out = open(sys.argv[3], 'w', encoding='utf-8', newline='\n')
def pr(*a): print(*a, file=out)

dec_of = {}
for arm in (B, A):
    for s in arm.values():
        for q, nat, row, dec, *_ in s['T']: dec_of[row] = dec
rows = sorted(dec_of, key=lambda r: (dec_of[r], r))

# 世界の量の軌道
pr('== 世界の技師の和 W・世界の人口 P（千人）・W÷P の軌道（種の中央）と al÷基準')
for q in (8, 32, 64, 128, 200, 272, 400, 536, 664):
    wb = st.median([B[s]['W'].get(q, math.nan) for s in B]); wa = st.median([A[s]['W'].get(q, math.nan) for s in A])
    pb = st.median([B[s]['P'].get(q, math.nan) for s in B]); pa = st.median([A[s]['P'].get(q, math.nan) for s in A])
    rat = st.median([A[s]['W'][q] / B[s]['W'][q] for s in B if q in A[s]['W'] and q in B[s]['W'] and B[s]['W'][q] > 0])
    pr(f' {year_of(q)} W 基準 {wb:.0f} al {wa:.0f} 種ごとの al÷基準の中央 {rat:.3f} ・P 基準 {pb:.0f} al {pa:.0f} ・W÷P 基準 {wb/pb:.4f} al {wa/pa:.4f}')
pr(' 閾値 W0×d×1.05（S＝1）：' + ' '.join(f'd={d}:{W0*d*1.05:.0f}' for d in (1, 2, 3, 4, 5, 7, 10, 12, 14)))

# 候補の時計
Pst_base = st.median([B[s]['P'][8] for s in B]); Wst_base = st.median([B[s]['W'][8] for s in B])
R0 = W0 / Wst_base   # 丁：伸びの倍率の物差し（基準の開幕の W の中央で W0 を割った物＝基線の開幕で今の式と同じ）
r0 = W0 / Pst_base   # 甲：人口あたり

def mk(kind, arm, s):
    seed = arm[s]
    WQ = seed['W']
    def Wq(q):
        # engineer_s の四半期 q（7, 15, …）は clock_s の W の標本 q＋1 と同じ判定の段
        return WQ[q + 1] if q + 1 in WQ else WQ[min(WQ, key=lambda k: abs(k - q))]
    if kind == 'cur':
        return lambda q, dec: ramp(Wq(q) / (W0 * (dec - BASE_DEC) / 10.0))
    if kind == 'none':
        return lambda q, dec: 1.0
    if kind == 'pc':
        return lambda q, dec: ramp((Wq(q) / seed['P'][q]) / (r0 * (dec - BASE_DEC) / 10.0))
    if kind == 'grow':
        w0s = seed['W'][8]
        return lambda q, dec: ramp((Wq(q) / w0s) / (R0 * (dec - BASE_DEC) / 10.0))
    if kind.startswith('int'):
        K = integral(seed['W']); k0 = float(kind[3:])
        return lambda q, dec: ramp(K[q + 1 if q + 1 in K else max(K)] / (k0 * (dec - BASE_DEC) / 10.0))
    raise ValueError(kind)

# 乙の k0 は基線の影の年の中央が今の式の影と最も近い値を格子で選ぶ（1 本の定数・基線だけで選ぶ）
def run(kind, arm):
    return [shadow(arm[s], mk(kind, arm, s)) for s in sorted(arm)]

obsB = [observed(B[s]) for s in sorted(B)]; obsA = [observed(A[s]) for s in sorted(A)]
curB = run('cur', B); curA = run('cur', A)
best = None
for k0 in (50000, 100000, 200000, 300000, 400000, 600000, 800000, 1000000, 1500000, 2000000):
    ib = run(f'int{k0}', B)
    err = st.median([abs(med(ib, r) - med(curB, r)) for r in rows if dec_of[r] > BASE_DEC])
    if best is None or err < best[0]: best = (err, k0)
pr(f'\n乙の k0（人×年×d）を基線で選んだ：{best[1]}（基線の今の式の影との年の差の中央 {best[0]}）')
kinds = [('cur', '今の式'), ('pc', '甲 W÷人口'), (f'int{best[1]}', '乙 W の積分'), ('none', '丙 世界の時計なし（S＝1）'), ('grow', '丁 W÷開幕の W（伸びの倍率）')]
res = {k: (run(k, B) if k != 'cur' else curB, run(k, A) if k != 'cur' else curA) for k, _ in kinds}

pr('\n== 校正：観測（disc_s の E 行）と今の式の影（種の中央の年・発見した種の数）')
pr('行\t年代\t観測 基準\t影 基準\t観測 al\t影 al')
for r in rows:
    pr(f'{r}\t{dec_of[r]}\t{med(obsB,r)}({cnt(obsB,r)})\t{med(curB,r)}({cnt(curB,r)})\t{med(obsA,r)}({cnt(obsA,r)})\t{med(curA,r)}({cnt(curA,r)})')
pr('発見した行の数（種の中央）：観測 基準 %s al %s ・影 基準 %s al %s' % (
    st.median([len(x) for x in obsB]), st.median([len(x) for x in obsA]),
    st.median([sum(1 for r in x if x[r] < 1951) for x in curB]), st.median([sum(1 for r in x if x[r] < 1951) for x in curA])))

pr('\n== 候補ごとの影（種の中央の年〔到達した種〕）：基準の盤面／al の盤面')
pr('行\t年代\t' + '\t'.join(n for _, n in kinds))
for r in rows:
    cells = []
    for k, _ in kinds:
        b, a = res[k]
        cells.append(f'{med(b,r):g}({cnt(b,r)})/{med(a,r):g}({cnt(a,r)})')
    pr(f'{r}\t{dec_of[r]}\t' + '\t'.join(cells))
pr('\n== 候補ごとの要約')
for k, n in kinds:
    b, a = res[k]
    nb = st.median([sum(1 for r in x if x[r] < 1951) for x in b]); na = st.median([sum(1 for r in x if x[r] < 1951) for x in a])
    dv = [med(a, r) - med(b, r) for r in rows if r != '甜菜糖']
    keep = [med(b, r) - med(curB, r) for r in rows]
    pr(f'{n}: 行の数 基準 {nb} al {na} ・al−基準で 10 年超ずれた行（甜菜糖を除く）{sum(abs(x)>10 for x in dv)}/{len(dv)}（遅れ {sum(x>10 for x in dv)}・早まり {sum(x<-10 for x in dv)}）'
       f' ・基準の盤面で今の式の影から 10 年超動いた行 {sum(abs(x)>10 for x in keep)}（早まり {sum(x<-10 for x in keep)}） ・1790 年までに開く行（基準の中央）{sum(med(b,r)<=1790 for r in rows)}')
out.close()
