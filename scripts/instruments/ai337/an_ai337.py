"""(337) 流れF AI の応答（利払いの重さを読む）を比例の形で の読み手（読むだけ・挙動は変えない）。事前登録 measurements/ai_20261015_prereg.txt の [5][6]。
  (319) の `an_fx319_arms.py` の定義を式のまま写し、線を L1〜L3 に差し替え、抽選の記録（足した列）を足した。
  python -X utf8 an_ai337.py <出力.md> A=<ins> E=<ins> N1=<ins> N2=<ins> N3=<ins> N4=<ins> [SEEDS=<n>]
  （後半の読み＝事前登録 [10]：参照の腕 R＝E 以外の全部・腕の名はキーで引く・E は 1 本）
入力＝`INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins（nation_s・fiscal_s・class_s・pxgoods_s）
線（事前登録 [5]）：
  L1 世界の専売の段の往復（全国・全期・種の和）：E ≦ max(A, N)／L2 英の払えない月（Q1〜Q40・種の和）：E ＜ min(A, N)／
  L3 英の兌換停止の最初の四半期（全期・停止なしは ∞）：E の中央 ≧ min(A, N の中央) かつ E の最も早い種 ≧ min(A, N の最も早い種)
  N が無いときは A だけで読む（min／max が A になる＝雑音の床なし＝暫定の読み）。
記録：抽選（足した列 interestBurdenAtDecisionIndex・monopolyPushProbabilityIndex・monopolyPushDrawIndex・monopolyPushHitCount）の国×四半期の数・当たり・英と蘭の専売の段の道筋（Q1〜Q12）ほか (319) と同じ。
限界の自己申告：
  ・兌換停止＝fiscal_s の onGold が 0.5 以上から 0.5 未満へ・四半期の番号は monthIndex 0,1→Q1（0 起算で qof）・1797 年＝0 起算の 56 番目。
  ・往復＝nation_s の monopolyTierEndStage が前の四半期より上がり次の四半期に下がった件数。
  ・抽選の列は決断の瞬間の値（四半期の途中で動く量は反映しない）。抽選しなかった四半期は p・u が空（NaN）。"""
import sys, os, statistics, collections
sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows as irows, seeds as iseeds, num

OUT = sys.argv[1]
ARMS = collections.OrderedDict(); SEEDCAP = 0; REFP = None
for a in sys.argv[2:]:
    k, v = a.split('=', 1)
    if k == 'SEEDS': SEEDCAP = int(v)
    elif k == 'REF': REFP = v
    else: ARMS[k] = v
UK = 'イギリス'
W = [8, 40, 80, 160, 240, 10 ** 6]; WN = ['Q8', 'Q40', 'Q80', 'Q160', 'Q240', '全期']
MASS = ('農民', '工場労働者', '職人')
L = 0.67


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def f(x, nd=3):
    return '—' if x != x else f'{x:.{nd}f}'


def cell(xs, nd=3):
    xs = [x for x in xs if x == x]
    return '—' if not xs else f'{statistics.median(xs):.{nd}f} ({min(xs):.{nd}f}〜{max(xs):.{nd}f})'


def qof(m):
    return 0 if m < 2 else 1 + (m - 2) // 3


def load(D):
    sds = sorted(set(iseeds(D, 'nation')) & set(iseeds(D, 'fiscal')) & set(iseeds(D, 'class')))
    if SEEDCAP: sds = [s for s in sds if s <= SEEDCAP]
    R = dict(seeds=sds, nat=collections.defaultdict(dict), fis=collections.defaultdict(list), cls=collections.defaultdict(lambda: collections.defaultdict(dict)),
             expo=collections.Counter(), tw=collections.defaultdict(list), warm=collections.defaultdict(lambda: collections.defaultdict(list)), gd=collections.defaultdict(dict))
    for sd, r in irows(D, 'nation'):
        if sd not in sds: continue
        q = int(r['quarter'])
        tv = num(r.get('monopolyTierEndStage', 'nan'))
        if tv == tv: R['tw'][(sd, r.get('nationUid', r['nation']))].append((q, tv))
        if r['nation'] == UK: R['nat'][sd][q] = r
        rev, it = num(r['revenueBaseMeanPerMonth']), num(r['interestMeanPerMonth'])
        if rev == rev and it == it:
            R['expo']['den'] += 1
            if it - L * rev > 0 and not num(r['payRateBelowOneMonths']) > 0: R['expo']['burden_only'] += 1
            if it - L * rev > 0: R['expo']['burden'] += 1
    for sd, r in irows(D, 'fiscal'):
        if sd in sds and r['nation'] == UK: R['fis'][sd].append(r)
    for sd, r in irows(D, 'pxgoods'):
        if sd in sds and r['nation'] == UK and r['time'].startswith('q'):
            q = int(r['time'][1:])
            if 4 <= q < 40: R['gd'][sd][(q, r['goodName'])] = (num(r['lastSoldQty']), num(r['priceMean']))
    for sd, r in irows(D, 'class'):
        if sd in sds and r['time'] == 'warm': R['warm'][sd][r['nation']].append((r['classId'], r['sizeK'], r['savingsStock']))
        if sd in sds and r['nation'] == UK and r['time'].startswith('q') and r['className'] in MASS:
            R['cls'][sd].setdefault(int(r['time'][1:]), []).append((num(r['earnedPerMonth']), num(r['paidPerMonth'])))
    for sd in sds: R['fis'][sd].sort(key=lambda r: int(r['monthIndex']))
    return R


def susp_def(R):
    out = {w: [0, 0, 0] for w in W}   # 不履行のある種, 停止のある種, 停止の総回数
    for sd in R['seeds']:
        prev = None; dq = {w: 0 for w in W}; sq = {w: 0 for w in W}
        for r in R['fis'][sd]:
            q = qof(int(r['monthIndex'])); g = num(r['onGold']); dcv = num(r['defaultCount'])
            for w in W:
                if q < w:
                    if dcv > 0: dq[w] = 1
                    if prev is not None and prev >= .5 and g < .5: sq[w] += 1
            prev = g
        for w in W:
            out[w][0] += dq[w]; out[w][1] += 1 if sq[w] > 0 else 0; out[w][2] += sq[w]
    return out


def short_months(R, mmax):
    """(払えない月＝payRateShare<1, 払えない額のある月で binder=full) の種ごとの値"""
    a, b = [], []
    for sd in R['seeds']:
        x = y = 0
        for r in R['fis'][sd]:
            if int(r['monthIndex']) >= mmax: continue
            if num(r['payRateShare']) < 1.0: x += 1
            if num(r['borrowShortfallPerMonth']) > 0 and r['borrowBinder'] == 'full': y += 1
        a.append(x); b.append(y)
    return a, b


def per_seed(R, fn):
    return [fn(R['nat'][sd]) for sd in R['seeds']]


def rev_gdp(nat):
    xs = [num(nat[q]['revenueBaseMeanPerMonth']) * 12 / num(nat[q]['nominalGdpPerYear']) for q in range(4, 40) if q in nat]
    return statistics.mean(xs) if xs else float('nan')


def int_rev(nat):
    i = sum(num(nat[q]['interestMeanPerMonth']) for q in range(4, 40) if q in nat); v = sum(num(nat[q]['revenueBaseMeanPerMonth']) for q in range(4, 40) if q in nat)
    return i / v if v > 0 else float('nan')


def debt_gdp(nat):
    r = nat.get(39)
    return num(r['govDebtStock']) / num(r['nominalGdpPerYear']) if r else float('nan')


def mass_pe(R, sd):
    xs = []
    for q in range(4, 40):
        c = R['cls'][sd].get(q)
        if c and len(c) == len(MASS):
            e = sum(x[0] for x in c); p = sum(x[1] for x in c)
            if e > 0: xs.append(p / e)
    return statistics.mean(xs) if xs else float('nan')


def tier_stats(R):
    mx, rev, path = [], 0, []
    for sd in R['seeds']:
        t = [num(R['nat'][sd][q]['monopolyTierEndStage']) for q in range(40) if q in R['nat'][sd]]
        mx.append(max(t) if t else float('nan'))
        for i in range(2, len(t)):
            if t[i - 1] > t[i - 2] and t[i] < t[i - 1]: rev += 1
        path.append(' '.join(f'{x:.0f}' for x in t[:6]))
    return mx, rev, path


def tier_series(R, sd, lo=0, hi=40):
    return [num(R['nat'][sd][q]['monopolyTierEndStage']) for q in range(lo, hi) if q in R['nat'][sd]]


def m1_seed(R, sd):
    """(数に入る, 除外された) ＝ Q2 の末の段≧3 のうち、重さだけで上がったと読める種"""
    n = R['nat'][sd]
    if 0 not in n or 1 not in n: return (False, False)
    t1, t2 = num(n[0]['monopolyTierEndStage']), num(n[1]['monopolyTierEndStage'])
    if not t2 >= 3: return (False, False)
    early = any(num(r['payRateShare']) < 1.0 for r in R['fis'][sd] if int(r['monthIndex']) < 2)
    if t1 >= 3 or not early: return (True, False)
    return (False, True)


def tier5_q(R, sd, lo, hi):
    return sum(1 for t in tier_series(R, sd, lo, hi) if t >= 5)


def rev7(R, sd, hi=40):
    t = tier_series(R, sd, 0, hi); k = 0
    for i in range(2, len(t)):
        if t[i - 1] > t[i - 2] and t[i] < t[i - 1]: k += 1
    return k


def real_price_index(RA, RD):
    """英 Q5〜Q40・A の同じ種の同じ四半期の価格・量を重みにした量の指数（Laspeyres）と価格の指数（Laspeyres の価格版）の、種ごとの窓の平均。"""
    qs, ps = [], []
    for sd in RD['seeds']:
        if sd not in RA['gd']: continue
        qq, pp = [], []
        for q in range(4, 40):
            num_q = den_q = num_p = 0.0
            for (q_, g), (soldA, pxA) in RA['gd'][sd].items():
                if q_ != q: continue
                dd = RD['gd'][sd].get((q, g))
                if dd is None or not (soldA == soldA and pxA == pxA and dd[0] == dd[0] and dd[1] == dd[1]): continue
                num_q += dd[0] * pxA; den_q += soldA * pxA; num_p += soldA * dd[1]
            if den_q > 0: qq.append(num_q / den_q); pp.append(num_p / den_q)
        if qq: qs.append(statistics.mean(qq)); ps.append(statistics.mean(pp))
    return qs, ps


def warm_diff(RA, RD):
    ns = set(); tot = set()
    for sd in RD['seeds']:
        for nat, rows_ in RD['warm'][sd].items():
            tot.add((sd, nat))
            if RA['warm'][sd].get(nat) != rows_: ns.add((sd, nat))
    uk = sum(1 for (sd, nat) in ns if nat == UK)
    return len(ns), len(tot), uk


D = {a: load(p) for a, p in ARMS.items()}
names = list(ARMS)
lines = [f'# (319) 腕 D の読み（英・種 {D[names[0]]["seeds"]}・種の中央〔最小〜最大〕）\n']
for a in names:
    R = D[a]
    sd_ = susp_def(R)
    lines.append(f'\n## {a}\n')
    lines.append('| 窓 | ' + ' | '.join(WN) + ' |\n|---|' + '---|' * len(WN))
    lines.append('| F1 兌換停止のある種 | ' + ' | '.join(f'{sd_[w][1]}/{len(R["seeds"])}' for w in W) + ' |')
    lines.append('| 　総回数 | ' + ' | '.join(str(sd_[w][2]) for w in W) + ' |')
    lines.append('| F2 不履行のある種 | ' + ' | '.join(f'{sd_[w][0]}/{len(R["seeds"])}' for w in W) + ' |')
    sm, fm = short_months(R, 24)
    sm40, fm40 = short_months(R, 120)
    lines.append(f'\n- F3 英の払えない月（monthIndex＜24）：種の中央 {f(med(sm), 1)}・最大 {max(sm)}・月の和 {sum(sm)}（種ごと {sm}）')
    lines.append(f'- 記録：英の払えない月（Q1〜Q40）の和 {sum(sm40)}・払えない額のある月で binder＝full（満額借りても足りない）{sum(fm40)}')
    lines.append(f'- F4 英の歳入÷年GDP（Q5〜Q40・種ごとの窓の平均）：{cell(per_seed(R, rev_gdp), 4)}')
    lines.append(f'- 記録：英の利払い÷歳入（Q5〜Q40・窓の和比）：{cell(per_seed(R, int_rev))}')
    lines.append(f'- F5 英の Q40 末の債務÷年GDP：{cell(per_seed(R, debt_gdp))}')
    lines.append(f'- F6 英の大衆の Paid÷Earned（Q5〜Q40・四半期の平均の窓の平均）：{cell([mass_pe(R, sd) for sd in R["seeds"]])}')
    mx, rv, path = tier_stats(R)
    lines.append(f'- F7 英の専売の段の最大（Q1〜Q40）：{cell(mx, 1)}・往復（上げた次の四半期に下げた件数・種の和）{rv}・Q1〜Q6 の道筋（種ごと）：' + '；'.join(path))
    t5a = [tier5_q(R, sd, 4, 40) for sd in R['seeds']]; t5b = [tier5_q(R, sd, 0, 40) for sd in R['seeds']]; t5c = [tier5_q(R, sd, 0, 10 ** 6) for sd in R['seeds']]
    r7all = [rev7(R, sd, 10 ** 6) for sd in R['seeds']]
    lines.append(f'- F8 英の段5 の四半期数（Q5〜Q40・36 四半期中）：{cell(t5a, 1)}（種ごと {t5a}）／記録：Q1〜Q40 {cell(t5b, 1)}・全期 {cell(t5c, 1)}・往復の全期（種の和）{sum(r7all)}（種ごと {r7all}）')
    m1s = [m1_seed(R, sd) for sd in R['seeds']]
    lines.append(f'- M1 英の専売の段が Q2 の末までに上がった種（除外規則つき）：{sum(1 for a_, b_ in m1s if a_)}/{len(R["seeds"])}（除外 {sum(1 for a_, b_ in m1s if b_)}）・Q2 末の段（種ごと）{[num(R["nat"][sd][1]["monopolyTierEndStage"]) for sd in R["seeds"] if 1 in R["nat"][sd]]}')
    rg = per_seed(R, rev_gdp)
    lines.append(f'- 記録：英の歳入÷年GDP（Q5〜Q40）の最大の種 {f(max(x for x in rg if x == x), 4)}（種ごと {[round(x, 4) for x in rg]}）')
    ex = R['expo']
    lines.append(f'- 記録：世界で重さ＞0 の国×四半期 {ex["burden"]}／{ex["den"]}（うち r＝1 のまま〔AI の新しい逼迫〕{ex["burden_only"]}）')
    wrev = wmax = wn = 0
    for key, ser in R['tw'].items():
        ser.sort()
        for i in range(2, len(ser)):
            if ser[i - 1][1] > ser[i - 2][1] and ser[i][1] < ser[i - 1][1]: wrev += 1
        for _, tv in ser:
            wn += 1
            if tv >= 5: wmax += 1
    lines.append(f'- 記録：世界の専売の段の往復（上げた次の四半期に下げた件数・全国・種の和）{wrev}・段が最大（5）の国×四半期 {wmax}／{wn}')
def first_susp(R):
    """種ごとの英の最初の兌換停止の四半期（0 起算・停止なしは inf）"""
    out = []
    for sd in R['seeds']:
        prev = None; fq = float('inf')
        for r in R['fis'][sd]:
            g = num(r['onGold'])
            if prev is not None and prev >= .5 and g < .5:
                fq = qof(int(r['monthIndex'])); break
            prev = g
        out.append(fq)
    return out


def world_rev(R):
    k = 0
    for key, ser in R['tw'].items():
        ser.sort()
        for i in range(2, len(ser)):
            if ser[i - 1][1] > ser[i - 2][1] and ser[i][1] < ser[i - 1][1]: k += 1
    return k


def push_stats(P):
    """足した列を読む（全ての国×四半期）"""
    n = pos = hits = 0; ps = []
    for sd, r in irows(P, 'nation'):
        n += 1
        p = num(r.get('monopolyPushProbabilityIndex', 'nan'))
        if p == p and p > 0:   # 追記 [16]① 係数 1 のとき B＝0 の行は p＝0.0（有限）が書かれる＝抽選に入ったのは p＞0（u が有限）だけ
            pos += 1; ps.append(p)
            if num(r.get('monopolyPushHitCount', '0')) > 0: hits += 1
    return n, pos, hits, ps


for a in names:
    first = first_susp(D[a])
    lines.append(f'\n- 記録 {a}：英の最初の兌換停止の四半期（0 起算・種ごと・∞＝なし）{[("∞" if x == float("inf") else int(x)) for x in first]}・L3′ 1797 年（0 起算 56）より前に最初の停止をした種 {sum(1 for x in first if x < 56)}/{len(first)}')
    n_, pos_, hits_, ps_ = push_stats(ARMS[a])
    lines.append(f'- 記録 {a}：抽選した国×四半期 {pos_}／{n_}・当たり {hits_}・p の中央 {f(med(ps_), 4)}（最大 {f(max(ps_), 4) if ps_ else "—"}）')

# ---------------- 後半（事前登録 [10]）----------------
import random
EK = 'E'
REFK = [k for k in names if k != EK]


def expo_decision(P):
    """決断時の暴露（[10]12）：interestBurdenAtDecisionIndex＞0 かつ fiscalStressAtDecisionIndex＝0 の国×四半期（A でも列は読める）"""
    n = k = 0; per_nat = collections.Counter()
    for sd, r in irows(P, 'nation'):
        n += 1
        b = num(r.get('interestBurdenAtDecisionIndex', 'nan')); fs = num(r.get('fiscalStressAtDecisionIndex', 'nan'))
        if b == b and b > 0 and fs == fs and fs == 0:
            k += 1; per_nat[r['nation']] += 1
    return k, n, per_nat


def pbin(p):
    return 0 if p < 0.01 else 1 if p < 0.03 else 2 if p < 0.1 else 3


def m2(P, seeds, only=None):
    """E の中の自然実験。戻り＝(Δ, 片側 p, 層の表, 当たり数, 対象数, 別定義の Δ)。only＝国名の集合"""
    ser = collections.defaultdict(dict)
    for sd, r in irows(P, 'nation'):
        if sd not in seeds: continue
        ser[(sd, r.get('nationUid', r['nation']))][int(r['quarter'])] = r
    rows = []   # (層, 当たり, O, O01)
    for (sd, nat), qd in ser.items():
        for q, r in qd.items():
            p = num(r.get('monopolyPushProbabilityIndex', 'nan'))
            if not p > 0: continue   # 追記 [16]① p＞0（抽選に入った行だけ・B＝0 の p＝0.0 の行は当たりようがない）
            if only and r['nation'] not in only: continue
            tb = num(r.get('monopolyTierBeforeDecisionStage', 'nan'))
            if tb == tb and tb >= 5: continue
            nxt = [num(qd[q + j]['payRateBelowOneMonths']) for j in range(1, 5) if (q + j) in qd]
            if len(nxt) < 4: continue
            O = sum(nxt)
            rows.append((pbin(p), num(r.get('monopolyPushHitCount', '0')) > 0, O, 1.0 if O > 0 else 0.0))

    def stat(rs, k):
        num_ = den = 0.0
        by = collections.defaultdict(lambda: [[], []])
        for t in rs: by[t[0]][0 if t[1] else 1].append(t[k])
        for b, (h, m) in by.items():
            if h and m:
                w = len(h) * len(m) / (len(h) + len(m))
                num_ += w * (statistics.mean(h) - statistics.mean(m)); den += w
        return num_ / den if den > 0 else float('nan')
    d = stat(rows, 2); d01 = stat(rows, 3)
    rng = random.Random(20261015); cnt = 0; N = 2000
    by_idx = collections.defaultdict(list)
    for i, t in enumerate(rows): by_idx[t[0]].append(i)
    if d == d:
        for _ in range(N):
            lab = [None] * len(rows)
            for b, idxs in by_idx.items():
                hs = [rows[i][1] for i in idxs]; rng.shuffle(hs)
                for i, h in zip(idxs, hs): lab[i] = h
            sh = [(t[0], lab[i], t[2], t[3]) for i, t in enumerate(rows)]
            if stat(sh, 2) <= d: cnt += 1
    pv = (cnt + 1) / (N + 1) if d == d else float('nan')
    tab = collections.defaultdict(lambda: [0, 0, 0.0, 0.0])
    for t in rows:
        e = tab[t[0]]; e[0 if t[1] else 1] += 1; e[2 if t[1] else 3] += t[2]
    return d, pv, tab, sum(1 for t in rows if t[1]), len(rows), d01


def first_susp_str(x):
    return '∞' if x == float('inf') else str(int(x))


def uk_q18(R, sd, col):
    return [num(R['nat'][sd][q].get(col, 'nan')) if q in R['nat'][sd] else float('nan') for q in range(8)]


lines.append('\n\n## 事前登録 [10] の読み（参照の腕 R＝' + '・'.join(REFK) + '）\n')
for a in names:
    k_, n_, pn = expo_decision(ARMS[a])
    top = '・'.join(f'{nm} {c}' for nm, c in pn.most_common(6))
    lines.append(f'- 暴露（決断時：重さ＞0 かつ 1−r＝0）{a}：{k_}／{n_}（{100.0 * k_ / n_:.2f}%）・上位 {top}')
if EK in D and REFK:
    ref = [D[k] for k in REFK]
    wE = world_rev(D[EK]); wR = [world_rev(R) for R in ref]
    lines.append('\n### 線\n')
    lines.append('- L1 世界の専売の段の往復：' + '／'.join(f'{k} {v}' for k, v in zip(REFK, wR)) + f'／E {wE}（線 ≦ max_R＝{max(wR)}）→ {"合格" if wE <= max(wR) else "外れ"}')
    sE = sum(short_months(D[EK], 120)[0]); sR = [sum(short_months(R, 120)[0]) for R in ref]
    wid = max(sR) - min(sR)
    l2_undet = min(sR) - wid <= 0   # 追記 [16]③ [10]13：雑音の床が窓より広い＝判定不能
    l2 = (not l2_undet) and sE < min(sR) - wid
    lines.append('- L2 英の払えない月（Q1〜Q40・種の和）：' + '／'.join(f'{k} {v}' for k, v in zip(REFK, sR)) + f'／E {sE}（線 ＜ min_R−幅＝{min(sR)}−{wid}＝{min(sR) - wid}）→ {"判定不能" if l2_undet else "合格" if l2 else "外れ"}')
    fE = first_susp(D[EK]); fR = [first_susp(R) for R in ref]
    cE = sum(1 for x in fE if x < 56); cR = [sum(1 for x in v if x < 56) for v in fR]
    mE = min(fE); mR = [min(v) for v in fR]
    l3a = cE <= max(cR); l3b = mE >= min(mR)
    lines.append('- L3a 56 より前に最初の停止をした種：' + '／'.join(f'{k} {v}' for k, v in zip(REFK, cR)) + f'／E {cE}（線 ≦ max_R＝{max(cR)}）→ {"合格" if l3a else "外れ"}')
    lines.append('- L3b 最も早い種：' + '／'.join(f'{k} {first_susp_str(v)}' for k, v in zip(REFK, mR)) + f'／E {first_susp_str(mE)}（線 ≧ min_R＝{first_susp_str(min(mR))}）→ {"合格" if l3b else "外れ"}')
    lines.append('- 記録：最初の停止（種ごと）' + '；'.join(f'{k} {[first_susp_str(x) for x in v]}' for k, v in zip(REFK, fR)) + f'；E {[first_susp_str(x) for x in fE]}')
    lines.append('- 記録：最初の停止の全期の中央（置き換え前の L3 の量）' + '／'.join(f'{k} {statistics.median(v)}' for k, v in zip(REFK, fR)) + f'／E {statistics.median(fE)}')
    d, pv, tab, nh, nt, d01 = m2(ARMS[EK], set(D[EK]['seeds']))
    m2ok = d == d and d < 0 and pv < 0.10
    lines.append('\n### M2（E の自然実験・世界全体）\n')
    lines.append(f'- Δ（次の 4 四半期の払えない月の和：当たり−外れ・層の重み平均）{f(d, 3)}・並べ替え検定の片側 p {f(pv, 3)}・対象 {nt}・当たり {nh} → {"合格" if m2ok else "外れ"}')
    lines.append(f'- 別の定義（払えない月が 1 月でもあるか 0/1）Δ {f(d01, 3)}')
    lines.append('| p の層 | 当たり n | 外れ n | 当たりの O 平均 | 外れの O 平均 |\n|---|---|---|---|---|')
    for b in sorted(tab):
        h, m, sh, sm = tab[b]
        lines.append(f'| {["[0,0.01)", "[0.01,0.03)", "[0.03,0.1)", "[0.1,1)"][b]} | {h} | {m} | {f(sh / h if h else float("nan"), 3)} | {f(sm / m if m else float("nan"), 3)} |')
    for nm in (UK, 'ネーデルラント'):
        d2, pv2, tab2, nh2, nt2, d012 = m2(ARMS[EK], set(D[EK]['seeds']), {nm})
        lines.append(f'- 記録 {nm} だけ：Δ {f(d2, 3)}・p {f(pv2, 3)}・対象 {nt2}・当たり {nh2}')
    ser = collections.defaultdict(dict)
    for sd, r in irows(ARMS[EK], 'nation'):
        ser[(sd, r.get('nationUid', r['nation']))][int(r['quarter'])] = r
    nar = hit = 0
    for qd in ser.values():
        for q, r in qd.items():
            if num(r.get('monopolyPushHitCount', '0')) > 0:
                hit += 1
                if (q + 1) in qd and num(qd[q + 1]['monopolyTierEndStage']) < num(r['monopolyTierEndStage']): nar += 1
    lines.append(f'- 記録：当たりの次の四半期に段が下がった国×四半期 {nar}／当たり {hit}')
    # 追記 [16]② 抽選に入った行の S＝p÷B（国庫＞安全水準 ⇔ S＜0.5）。当たりの翌四半期に旧の枝が狭める余地の大きさを見る
    sl = []
    for sd_, r in irows(ARMS[EK], 'nation'):
        p_ = num(r.get('monopolyPushProbabilityIndex', 'nan')); b_ = num(r.get('interestBurdenAtDecisionIndex', 'nan'))
        if p_ == p_ and p_ > 0 and b_ == b_ and b_ > 0: sl.append((p_ / b_, num(r.get('monopolyPushHitCount', '0')) > 0))
    lo = [x for x in sl if x[0] < 0.5]
    lines.append(f'- 記録 [16]②：抽選に入った行 {len(sl)} のうち 国庫＞安全水準（S＜0.5）{len(lo)}（{100.0 * len(lo) / len(sl) if sl else float("nan"):.1f}%）・うち当たり {sum(1 for x in lo if x[1])}／全体の当たり {sum(1 for x in sl if x[1])}')
    # 追記 [16]⑥ 偏りの向き：層ごとの p の平均（当たり／外れ）
    pb = collections.defaultdict(lambda: [[], []])
    for sd_, r in irows(ARMS[EK], 'nation'):
        p_ = num(r.get('monopolyPushProbabilityIndex', 'nan'))
        if p_ == p_ and p_ > 0: pb[pbin(p_)][0 if num(r.get('monopolyPushHitCount', '0')) > 0 else 1].append(p_)
    lines.append('- 記録 [16]⑥：層内の p の平均（当たり／外れ）' + '・'.join(f'{["[0,0.01)", "[0.01,0.03)", "[0.03,0.1)", "[0.1,1)"][b_]} {f(statistics.mean(v[0]) if v[0] else float("nan"), 4)}／{f(statistics.mean(v[1]) if v[1] else float("nan"), 4)}' for b_, v in sorted(pb.items())))
    lines.append('\n### 英の Q1〜Q8（機構の表）\n')
    for k in names:
        R = D[k]
        lines.append(f'- 専売の段 {k}（種ごと・Q1〜Q8）：' + '；'.join(' '.join(f'{x:.0f}' if x == x else '–' for x in uk_q18(R, sd, 'monopolyTierEndStage')) for sd in R['seeds']))
    R = D[EK]
    for col in ('interestBurdenAtDecisionIndex', 'monopolyPushProbabilityIndex', 'monopolyPushDrawIndex'):
        lines.append(f'- E の {col}（Q1〜Q8・種ごと）：' + '；'.join(' '.join(f'{x:.3f}' if x == x else '–' for x in uk_q18(R, sd, col)) for sd in R['seeds']))
    lines.append('- E の当たり（Q1〜Q8・種ごと）：' + '；'.join(' '.join(f'{x:.0f}' if x == x else '–' for x in uk_q18(R, sd, 'monopolyPushHitCount')) for sd in R['seeds']))
    for k in names:
        sm8, _ = short_months(D[k], 24)
        lines.append(f'- 英の払えない月（Q1〜Q8）{k}：和 {sum(sm8)}（種ごと {sm8}）')
    lines.append(f'\n- 線の総括：L1 {"○" if wE <= max(wR) else "×"}・L2 {"判定不能" if l2_undet else "○" if l2 else "×"}・L3a {"○" if l3a else "×"}・L3b {"○" if l3b else "×"}・M2 {"○" if m2ok else "×"}（止める線・計算量・バランスの物差しは別に読む）')
open(OUT, 'w', encoding='utf-8', newline='\n').write('\n'.join(lines) + '\n')
print('done', OUT)
