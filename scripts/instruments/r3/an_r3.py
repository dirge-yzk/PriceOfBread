"""(305) 線 r3：英の貯蓄の置き直し（段3・OpeningSavingsRebaseMode=1）の測り直しの読み手（読むだけ・挙動は変えない）。
事前登録 measurements/r3_20261013_prereg.txt の [2][3]。p273/an_p273_arms.py（P1′〜P6）と s2/an_s2.py（富裕層の分子・分母）の型を写した。
  python an_r3.py <出力ファイル.md> A=<ins> B=<ins> [SEEDS=<n>＝種を先頭の n 個に絞る（10種の走行から5種の読みを取り出す）]
  入力＝`INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins
        class_s・nation_s・stock_s・fiscal_s（英を NATIONS に挙げた走行）。腕 A が基線・B が採否の腕。
  線（事前登録 [2]）：P1′ 谷≧0.50／P2 窓の平均≧0.90／P3 (a)不履行 全種 0 (b)停止の種 ≦3/10／P4 実質GDP Q40 ≧0.6×A／P5 歳入÷年GDP ≧0.6×A かつ ≧0.0851／
        P6 帳尻<1%／P7 Q1 の歳入÷A ≧0.80／P8′ Q1〜Q8 に borrowBinder＝gateClosed の月が出た種の数 ≦A＋2／P9 全階級の貯蓄÷年所得 Q40 ≦5.0 年かつ ≦0.5×A／
        P10 富裕層（貴族＋資本家）の貯蓄÷所得（月数）が Q5〜Q40 と Q41〜Q160 の両方で B＜A の種 ≧8 かつ B÷A の中央 ≦0.5。
  限界の自己申告（計器が一番バグる）：
    ・class_s の paid／earned は四半期の月平均（谷の第1月は Q1 に薄まる）。savingsStock は四半期の終わりの在庫。
    ・〈大衆〉は農民 1・工場労働者 2・職人 11（Instruments.IsMass と同じ）。富裕層は貴族 0＋資本家 3（商人 10 は含めない＝s2 と同じ）。
    ・窓の中央→種の中央の二重の中央。種は交差集合（腕の間で共通の種）。NaN は〈判定不能〉で合格にしない（否定形）。
    ・全階級の貯蓄÷年所得＝Σ savingsStock ÷ (12×Σ earnedPerMonth)（(273) の q0_savings_to_income_years と同じ定義）。史実の参照は【推】。
    ・P8′ の月は fiscal_s の英の月ごとの行（borrowBinder の文字列）。不足のある月だけ binder が入る（不足の無い月は noShortfall）。"""
import sys, os, re, glob, statistics, collections
sys.stdout.reconfigure(encoding='utf-8')
OUTPATH = sys.argv[1]
ARMS = collections.OrderedDict()
SEEDCAP = 0
for a in sys.argv[2:]:
    k, v = a.split('=', 1)
    if k == 'SEEDS':
        SEEDCAP = int(v)
    else:
        ARMS[k] = v
UK = 'イギリス'
MASS = {1, 2, 11}
NOBLE, CAPITALIST = 0, 3
WIN = [('Q1-8', 0, 7), ('Q5-40', 4, 39), ('Q41-160', 40, 159), ('Q161-240', 160, 239), ('Q189-228', 188, 227), ('全期', 0, 10 ** 6)]


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def table(path):
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            yield dict(zip(head, ln.rstrip('\n').split('\t')))


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def mean(xs):
    xs = [x for x in xs if x == x]
    return sum(xs) / len(xs) if xs else float('nan')


def f(x, nd=3):
    return '—' if x != x else f'{x:.{nd}f}'


def cell(xs, nd=3):
    ys = [x for x in xs if x == x]
    return '—' if not ys else f'{statistics.median(ys):.{nd}f} ({min(ys):.{nd}f}〜{max(ys):.{nd}f})'


def seeds_of(D, stem):
    out = set()
    for p in glob.glob(os.path.join(D, stem + '_s*.tsv')):
        m = re.search(stem + r'_s(\d+)\.tsv$', os.path.basename(p))
        if m:
            out.add(int(m.group(1)))
    return out


def load(D):
    R = {}
    sds = sorted(seeds_of(D, 'class') & seeds_of(D, 'nation') & seeds_of(D, 'stock') & seeds_of(D, 'fiscal'))
    for sd in sds:
        cl = collections.defaultdict(dict)   # q -> classId -> (sizeK, sav, earned, paid)
        for r in table(os.path.join(D, f'class_s{sd}.tsv')):
            if r['nation'] != UK:
                continue
            m = re.fullmatch(r'[Qq](\d+)', r['time'])
            if not m:
                continue
            cl[int(m.group(1))][int(r['classId'])] = (fl(r['sizeK']), fl(r['savingsStock']), fl(r['earnedPerMonth']), fl(r['paidPerMonth']))
        nat = {}
        for r in table(os.path.join(D, f'nation_s{sd}.tsv')):
            if r['nation'] == UK:
                nat[int(r['quarter'])] = {k: fl(v) for k, v in r.items() if k not in ('nation', 'year')}
        stk = {}
        for r in table(os.path.join(D, f'stock_s{sd}.tsv')):
            if r['nation'] == UK and re.fullmatch(r'q\d+', r['time']):
                stk[int(r['time'][1:])] = (fl(r['gdpRealPerYear']), fl(r['techLevelIndex']))
        fis = [r for r in table(os.path.join(D, f'fiscal_s{sd}.tsv')) if r['nation'] == UK]
        R[sd] = dict(cl=cl, nat=nat, stk=stk, fis=fis)
    return R


def ratio(d, cls, i, j):
    a = sum(v[i] for c, v in d.items() if c in cls)
    b = sum(v[j] for c, v in d.items() if c in cls)
    return a / b if b > 0 else float('nan')


def metrics(S):
    cl, nat, stk, fis = S['cl'], S['nat'], S['stk'], S['fis']
    M = {}
    pe = lambda q: ratio(cl[q], MASS, 3, 2) if q in cl else float('nan')
    p1 = [pe(q) for q in range(0, 8)]
    M['p1_by_q'] = p1
    M['P1p'] = min([x for x in p1 if x == x], default=float('nan')) if all(x == x for x in p1) else float('nan')
    w = [pe(q) for q in range(4, 40)]
    M['P2'] = mean(w) if all(x == x for x in w) else float('nan')
    # 不履行・停止
    last = nat.get(39)
    M['P3a'] = last['sovereignDefaultCumulativeCount'] if last and 'sovereignDefaultCumulativeCount' in last else float('nan')
    n_susp = 0; prev = None
    for r in fis:
        og = fl(r['onGold']); mi = int(r['monthIndex'])
        if prev == 1 and og == 0 and mi < 120:
            n_susp += 1
        prev = og
    M['P3b'] = n_susp
    M['P4'] = stk.get(39, (float('nan'), float('nan')))[0]
    M['tech40'] = stk.get(39, (float('nan'), float('nan')))[1]
    for q in (79, 159, 239):
        M[f'tech{q + 1}'] = stk.get(q, (float('nan'), float('nan')))[1]
    rg = []
    for q in range(4, 40):
        r = nat.get(q)
        if not r:
            rg.append(float('nan')); continue
        g = r['nominalGdpPerYear']
        rg.append(r['revenueBaseMeanPerMonth'] * 12 / g if g > 0 else float('nan'))
    M['P5'] = mean(rg) if all(x == x for x in rg) else float('nan')
    ir = []
    for q in range(4, 40):
        r = nat.get(q)
        if r and r['revenueBaseMeanPerMonth'] > 0:
            ir.append(r['interestMeanPerMonth'] / r['revenueBaseMeanPerMonth'])
    M['interest_to_rev'] = mean(ir)
    resid = 0.0
    for r in fis:
        rv = fl(r['revenueBasePerMonth']); rs = abs(fl(r['ledgerResidualPerMonth']))
        if rv > 0:
            if not (rs == rs):
                resid = float('nan'); break
            resid = max(resid, rs / rv)
    M['P6'] = resid
    q0 = nat.get(0)
    M['rev_by_q'] = [nat[q]['revenueBaseMeanPerMonth'] if q in nat else float('nan') for q in range(0, 8)]
    M['rev_Q1'] = M['rev_by_q'][0]
    # P8′
    first = float('nan'); maxir = float('nan'); ncl = 0
    for r in fis:
        mi = int(r['monthIndex'])
        if mi >= 24:
            continue
        if r.get('borrowBinder') == 'gateClosed':
            ncl += 1
            if first != first:
                first = mi
        v = fl(r.get('interestToRevenueShareNow', 'nan'))
        if v == v and not (maxir >= v):
            maxir = v
    M['P8_first'] = first; M['P8_n'] = ncl; M['P8_hit'] = 1 if ncl > 0 else 0; M['maxir_Q1_Q8'] = maxir
    # P9：全階級の貯蓄÷年所得（年）
    def sy(q):
        d = cl.get(q)
        if not d:
            return float('nan')
        s = sum(v[1] for v in d.values()); e = sum(v[2] for v in d.values())
        return s / (12 * e) if e > 0 else float('nan')
    M['P9_q39'] = sy(39); M['P9_q0'] = sy(0)
    M['sy_Q5_40'] = med([sy(q) for q in range(4, 40)])
    M['sy_Q41_160'] = med([sy(q) for q in range(40, 160)])
    M['mass_months_q39'] = ratio(cl.get(39, {}), MASS, 1, 2)
    # 富裕層（貴族＋資本家）の分子・分母・実質の分子・月数（窓ごと）
    for name, lo, hi in WIN:
        sav = []; ear = []; sreal = []; rat = []
        for q in sorted(cl):
            if not (lo <= q <= hi):
                continue
            d = cl[q]
            if NOBLE not in d or CAPITALIST not in d:
                continue
            s = d[NOBLE][1] + d[CAPITALIST][1]; e = d[NOBLE][2] + d[CAPITALIST][2]
            pl = nat.get(q, {}).get('priceLevel', float('nan'))
            sav.append(s); ear.append(e)
            sreal.append(s / pl if pl == pl and pl > 0 else float('nan'))
            rat.append(s / e if e > 0 else float('nan'))
        M[f'rich_sav_{name}'] = med(sav); M[f'rich_earn_{name}'] = med(ear)
        M[f'rich_savreal_{name}'] = med(sreal); M[f'rich_months_{name}'] = med(rat)
    for q in (3, 7, 39):
        r = nat.get(q); g = stk.get(q, (float('nan'),))[0]
        M[f'defl_Q{q + 1}'] = r['nominalGdpPerYear'] / g if r and g == g and g > 0 else float('nan')
    return M


DATA = {a: load(d) for a, d in ARMS.items()}
common = None
for a in DATA:
    common = set(DATA[a]) if common is None else (common & set(DATA[a]))
common = sorted(common or [])
if SEEDCAP:
    common = common[:SEEDCAP]
MET = {a: {sd: metrics(DATA[a][sd]) for sd in common} for a in DATA}
N = len(common)
names = list(DATA)
BASE, ARM = names[0], names[1]
out = []
P = out.append
P(f'# (305) 線 r3 の読み（英・種 {common}（{N}種）・腕 {names}・基線＝{BASE}）\n\n')
for a, d in ARMS.items():
    P(f'- {a} = {d}\n')
P('\n')


def col(a, k):
    return [MET[a][sd][k] for sd in common]


# ---- 置き直しが起きた証拠（先に読む）
P('## 0. 置き直しが起きた証拠（数字の前に）\n\n| 腕 | q0 の全階級の貯蓄÷年所得（年・中央〔最小〜最大〕） | 判定（0.78〜1.46 の内＝置き直しが起きた） |\n|---|---|---|\n')
for a in names:
    q0 = med(col(a, 'P9_q0'))
    verdict = '基線' if a == BASE else ('内' if 0.78 <= q0 <= 1.46 else ('判定不能（NaN）' if q0 != q0 else '外（置き直しが起きていない／積み戻った）'))
    P(f'| {a} | {cell(col(a, "P9_q0"), 3)} | {verdict} |\n')
P('\n')

# ---- 判定の線
LINES = [
    ('P1′ 谷：大衆の Paid÷Earned の Q1〜Q8 の四半期ごとの最小', 'P1p', 3),
    ('P2 大衆の Paid÷Earned の Q5〜Q40 の窓の平均', 'P2', 3),
    ('P3a 英の不履行の累計 Q40', 'P3a', 0),
    ('P3b 英の Q1〜Q40 の兌換停止の回数', 'P3b', 0),
    ('P4 英の実質GDP Q40', 'P4', 0),
    ('P5 英の歳入÷年GDP（Q5〜Q40 の平均）', 'P5', 4),
    ('P6 帳尻の残差÷歳入の最大', 'P6', 12),
    ('P7 の入力 Q1 の歳入（月あたり）', 'rev_Q1', 0),
    ('P8′ Q1〜Q8 の gateClosed の月の数', 'P8_n', 0),
    ('P8′ 最初に閉じた月（monthIndex）', 'P8_first', 0),
    ('P9 全階級の貯蓄÷年所得 Q40 の終わり（年）', 'P9_q39', 3),
    ('記録：Q1〜Q8 の利払い÷歳入の最大', 'maxir_Q1_Q8', 3),
]
P('## 1. 線の値（種の中央〔最小〜最大〕）\n\n| 量 | ' + ' | '.join(names) + ' |\n|---|' + '---|' * len(names) + '\n')
for lab, k, nd in LINES:
    P(f'| {lab} | ' + ' | '.join(cell(col(a, k), nd) for a in names) + ' |\n')
P('\n')

# ---- 判定
susp_max = 2 if N <= 5 else 3
gate_tol = 2
b = {k: col(ARM, k) for k in MET[ARM][common[0]] if k not in ('p1_by_q', 'rev_by_q')}
a_ = {k: col(BASE, k) for k in MET[BASE][common[0]] if k not in ('p1_by_q', 'rev_by_q')}
nanof = lambda xs: sum(1 for x in xs if not (x == x))


def verdict(ok, xs=None):
    if xs is not None and nanof(xs):
        return f'判定不能（NaN {nanof(xs)}種）'
    return '合格' if ok else '外れ'


rows = []
p1 = med(b['P1p']); rows.append(('P1′ 谷の最小の中央', f(p1), '≧0.50', verdict(p1 >= 0.5, b['P1p'])))
p2 = med(b['P2']); rows.append(('P2 窓の平均の中央', f(p2), '≧0.90', verdict(p2 >= 0.9, b['P2'])))
ndef = sum(1 for x in b['P3a'] if x > 0)
rows.append(('P3a 不履行（Q40 に累計>0 の種）', f'{ndef}/{N}', '全種 0', verdict(all(x == 0 for x in b['P3a']), b['P3a'])))
nsusp = sum(1 for x in b['P3b'] if x > 0)
rows.append(('P3b 兌換停止の出た種', f'{nsusp}/{N}（A {sum(1 for x in a_["P3b"] if x > 0)}/{N}）', f'≦{susp_max}/{N}', verdict(nsusp <= susp_max)))
p4 = med(b['P4']); p4a = med(a_['P4']); rows.append(('P4 実質GDP Q40 の中央', f'{p4:.0f}（A {p4a:.0f}・比 {f(p4 / p4a)}）', '≧0.6×A', verdict(p4 >= 0.6 * p4a, b['P4'])))
p5 = med(b['P5']); p5a = med(a_['P5']); rows.append(('P5 歳入÷年GDP の中央', f'{f(p5, 4)}（A {f(p5a, 4)}・比 {f(p5 / p5a)}）', '≧0.6×A かつ ≧0.0851', verdict(p5 >= 0.6 * p5a and p5 >= 0.0851, b['P5'])))
p6 = float('nan') if nanof(b['P6']) else max(b['P6']); rows.append(('P6 帳尻の最大', f(p6, 12), '<1%', verdict(p6 < 0.01, b['P6'])))
rat7 = [MET[ARM][sd]['rev_Q1'] / MET[BASE][sd]['rev_Q1'] if MET[BASE][sd]['rev_Q1'] > 0 else float('nan') for sd in common]
p7 = med(rat7); rows.append(('P7 Q1 の歳入÷A の中央', f'{f(p7)}（{f(min(rat7))}〜{f(max(rat7))}）', '≧0.80', verdict(p7 >= 0.8, rat7)))
nB8 = sum(b['P8_hit']); nA8 = sum(a_['P8_hit']); rows.append(('P8′ Q1〜Q8 に gateClosed の月の出た種', f'{nB8}/{N}（A {nA8}/{N}）', f'≦A＋{gate_tol}', verdict(nB8 <= nA8 + gate_tol)))
p9 = med(b['P9_q39']); p9a = med(a_['P9_q39']); rows.append(('P9 全階級の貯蓄÷年所得 Q40 の中央（年）', f'{f(p9)}（A {f(p9a)}・比 {f(p9 / p9a)}）', '≦5.0 かつ ≦0.5×A', verdict(p9 <= 5.0 and p9 <= 0.5 * p9a, b['P9_q39'])))
p10 = []
for name in ('Q5-40', 'Q41-160'):
    xb = b[f'rich_months_{name}']; xa = a_[f'rich_months_{name}']
    ok = [(x, y) for x, y in zip(xb, xa) if x == x and y == y]
    cnt = sum(1 for x, y in ok if x < y)
    r_ = med(xb) / med(xa) if med(xa) else float('nan')
    p10.append((name, cnt, len(ok), r_))
okp10 = all(c >= 8 * N // 10 and r_ == r_ and r_ <= 0.5 and n == N for _, c, n, r_ in p10)
rows.append(('P10 富裕層の貯蓄÷所得（月数）B＜A の種／B÷A の中央', '；'.join(f'{nm} {c}/{n}・{f(r_)}' for nm, c, n, r_ in p10), f'各窓 ≧{8 * N // 10}/{N} かつ ≦0.5', '合格' if okp10 else '外れ'))
P(f'## 2. 判定（{N}種・{ARM} を {BASE} に対して読む）\n\n| 線 | {ARM} の値 | 線の値 | 判定 |\n|---|---|---|---|\n')
for r in rows:
    P(f'| {r[0]} | {r[1]} | {r[2]} | {r[3]} |\n')
P('\n')
P('門が閉じた最初の月（monthIndex・種ごと・—＝閉じず）：\n\n')
for a in names:
    P(f'- {a}: ' + '；'.join(f's{sd} {f(MET[a][sd]["P8_first"], 0)}' for sd in common) + '\n')
P('\n種ごと（P1′・P2・不履行・停止・Q1 の歳入）：\n\n')
for a in names:
    P(f'- {a}: ' + '；'.join(f's{sd} P1′={f(MET[a][sd]["P1p"], 2)} P2={f(MET[a][sd]["P2"], 2)} 不履行={f(MET[a][sd]["P3a"], 0)} 停止={MET[a][sd]["P3b"]} 歳入Q1={f(MET[a][sd]["rev_Q1"], 0)}' for sd in common) + '\n')
P('\n')

# ---- Q1〜Q8 の歳入 B÷A
P('## 3. Q1〜Q8 の四半期ごとの歳入（月あたり）B÷A（種ごとの比の中央〔最小〜最大〕）\n\n| 四半期 | ' + ' | '.join(f'Q{q + 1}' for q in range(8)) + ' |\n|---|' + '---|' * 8 + '\n')
cells = []
for q in range(8):
    rr = [MET[ARM][sd]['rev_by_q'][q] / MET[BASE][sd]['rev_by_q'][q] if MET[BASE][sd]['rev_by_q'][q] > 0 else float('nan') for sd in common]
    cells.append(cell(rr, 3))
P(f'| {ARM}÷{BASE} | ' + ' | '.join(cells) + ' |\n')
for a in names:
    P(f'| {a} の歳入 | ' + ' | '.join(f(med([MET[a][sd]['rev_by_q'][q] for sd in common]), 0) for q in range(8)) + ' |\n')
P('\n大衆の Paid÷Earned の Q1〜Q8（種の中央）：\n\n| 腕 | ' + ' | '.join(f'Q{q + 1}' for q in range(8)) + ' |\n|---|' + '---|' * 8 + '\n')
for a in names:
    P(f'| {a} | ' + ' | '.join(f(med([MET[a][sd]['p1_by_q'][q] for sd in common]), 2) for q in range(8)) + ' |\n')
P('\n')

# ---- 富裕層の貯蓄（分子）・所得（分母）・月数
P('## 4. 英の富裕層（貴族＋資本家）の貯蓄（分子）・所得（分母）・実質の分子・貯蓄÷所得（月数）。窓の中央→種の中央〔最小〜最大〕と B÷A・B＜A の種の数\n\n')
for key, label, nd in (('rich_sav_', '貯蓄（分子・名目）', 0), ('rich_earn_', '所得（分母・earnedPerMonth の和）', 1), ('rich_savreal_', '貯蓄（実質＝÷priceLevel）', 0), ('rich_months_', '貯蓄÷所得（月数）', 1)):
    P(f'### {label}\n\n| 窓 | ' + ' | '.join(names) + f' | {ARM}÷{BASE}（中央どうし）・{ARM}＜{BASE} の種 |\n|---|' + '---|' * (len(names) + 1) + '\n')
    for name, lo, hi in WIN:
        xb = col(ARM, f'{key}{name}'); xa = col(BASE, f'{key}{name}')
        ok = [(x, y) for x, y in zip(xb, xa) if x == x and y == y]
        cnt = sum(1 for x, y in ok if x < y)
        r_ = med(xb) / med(xa) if med(xa) else float('nan')
        P(f'| {name} | ' + ' | '.join(cell(col(a, f'{key}{name}'), nd) for a in names) + f' | {f(r_)}・{cnt}/{len(ok)} |\n')
    P('\n')

# ---- 全階級の貯蓄÷年所得
P('## 5. 全階級の貯蓄÷年所得（年）：q0・Q5〜Q40 の中央・Q41〜Q160 の中央・Q40 の終わり（種の中央〔最小〜最大〕）。史実の参照＝貨幣÷年GDP 0.4〜0.6【推】\n\n| 腕 | q0 | Q5-40 | Q41-160 | Q40 の終わり |\n|---|---|---|---|---|\n')
for a in names:
    P(f'| {a} | {cell(col(a, "P9_q0"))} | {cell(col(a, "sy_Q5_40"))} | {cell(col(a, "sy_Q41_160"))} | {cell(col(a, "P9_q39"))} |\n')
P('\n')

# ---- 記録
RECS = [
    ('記録：大衆の貯蓄の月数 Q40 の終わり', 'mass_months_q39', 1),
    ('記録：利払い÷歳入（Q5〜Q40 の平均）', 'interest_to_rev', 3),
    ('記録：技術水準 Q40', 'tech40', 3), ('記録：技術水準 Q80', 'tech80', 3), ('記録：技術水準 Q160', 'tech160', 3), ('記録：技術水準 Q240', 'tech240', 3),
    ('記録：GDP デフレーター Q4', 'defl_Q4', 3), ('記録：GDP デフレーター Q8', 'defl_Q8', 3), ('記録：GDP デフレーター Q40', 'defl_Q40', 3),
]
P('## 6. 記録の列（線にしない）\n\n| 量 | ' + ' | '.join(names) + ' |\n|---|' + '---|' * len(names) + '\n')
for lab, k, nd in RECS:
    P(f'| {lab} | ' + ' | '.join(cell(col(a, k), nd) for a in names) + ' |\n')
P('\n')
open(OUTPATH, 'w', encoding='utf-8', newline='\n').write(''.join(out))
print('done', OUTPATH)
