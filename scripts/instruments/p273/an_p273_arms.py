"""(273) 線P 後半：腕（B・R・S・S′）の計器つきの出力から、事前登録の線 P1′・P2〜P6 と記録の列を英で読む読み手（読むだけ）。
  python an_p273_arms.py <出力ファイル.md> B=<ins> R=<ins> S=<ins> S2=<ins> [Q=<窓の終わりの四半期の番号＝既定 40] [SEEDS=<n>＝種を先頭の n 個に絞る]
  入力＝標準の計器の出力（`INS=1 … run` の <出力>/ins か、canary の POB_INSTRUMENTS のフォルダ）。必要な表＝class_s・nation_s・stock_s・fiscal_s（英を NATIONS に挙げた走行）。
  線（実装ログ (273) §9 の §8 の表）：
    P1′ 谷＝英・大衆の Paid÷Earned の Q1〜Q8（q0〜q7）の四半期ごとの最小の種の中央 ≧0.50（R・S・A。S′ は記録）
    P2  大衆の Paid÷Earned の Q5〜Q40 の窓の平均の中央 ≧0.90
    P3  (a) 英の不履行の累計 Q40 の終わりが全種で 0 (b) 英の Q1〜Q40 に兌換停止（onGold 1→0）の出た種の数 ≦3/10（5種は ≦2/5）
    P4  英の実質GDP（stock_s の gdpRealPerYear）Q40 の終わりの中央 ≧3.12e5
    P5  英の歳入÷年GDP（Q5〜Q40 の四半期の平均）の中央 ≧0.0851
    P6  帳尻＝fiscal_s の ledgerResidualPerMonth の絶対値÷revenueBasePerMonth の最大 <1%
  限界の自己申告（計器が一番バグる）：
    ・class_s の paid／earned は四半期の月平均（月次ではない）。谷の月（置き直しの直後の第1月）は Q1 の平均に薄まる。
    ・「大衆」は農民 1・工場労働者 2・職人 11（Instruments.IsMass と同じ・兵士を除く）。富裕＝貴族 0・資本家 3・商人 10。
    ・兌換停止＝fiscal_s の onGold の 1→0（英の月ごとの行・monthIndex＜Q×3）。不履行＝nation_s の sovereignDefaultCumulativeCount。
    ・歳入＝nation_s の revenueBaseMeanPerMonth×12÷nominalGdpPerYear。物価水準の代理＝nominalGdpPerYear÷gdpRealPerYear（年末の行 q3・q7・q39 だけ）。
    ・種は交差集合（腕の間で共通の種）。B が無い腕の比（Q1 の歳入／B）は B の種だけ。"""
import sys, os, statistics, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import num as fl, seeds  # noqa: E402

OUTPATH = sys.argv[1]
ARMS = collections.OrderedDict()
QEND = 40
SEEDCAP = 0
for a in sys.argv[2:]:
    k, v = a.split('=', 1)
    if k == 'Q':
        QEND = int(v)
    elif k == 'SEEDS':   # (276) 種を先頭の n 個に絞る（10種の走行から 5種の読みを取り出す）
        SEEDCAP = int(v)
    else:
        ARMS[k] = v
UK = 'イギリス'
MASS = {1, 2, 11}
RICH = {0, 3, 10}
QE = QEND - 1   # 窓の終わりの行（0始まり）


def table(path):
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            yield dict(zip(head, ln.rstrip('\n').split('\t')))


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def f(x, nd=3):
    return '—' if x != x else f'{x:.{nd}f}'


def cell(xs, nd=3):
    xs2 = [x for x in xs if x == x]
    if not xs2:
        return '—'
    return f'{f(med(xs2), nd)} ({f(min(xs2), nd)}〜{f(max(xs2), nd)})'


def load(D):
    """seed -> dict の束（英のみ）。"""
    R = {}
    sd_all = sorted(set(seeds(D, 'class')) & set(seeds(D, 'nation')) & set(seeds(D, 'stock')) & set(seeds(D, 'fiscal')))
    for sd in sd_all:
        cl = collections.defaultdict(dict)   # time -> classId -> (sav, earned, paid)
        for r in table(os.path.join(D, f'class_s{sd}.tsv')):
            if r['nation'] == UK:
                cl[r['time']][int(r['classId'])] = (fl(r['savingsStock']), fl(r['earnedPerMonth']), fl(r['paidPerMonth']))
        nat = {}
        world_def = {}
        for r in table(os.path.join(D, f'nation_s{sd}.tsv')):
            q = int(r['quarter'])
            if r['nation'] == UK:
                nat[q] = r
            if 'sovereignDefaultCumulativeCount' in r:
                world_def.setdefault(r['nation'], {})[q] = fl(r['sovereignDefaultCumulativeCount'])
        stk = {}
        for r in table(os.path.join(D, f'stock_s{sd}.tsv')):
            if r['nation'] == UK and r['time'].startswith('q'):
                stk[int(r['time'][1:])] = (fl(r['gdpRealPerYear']), fl(r['techLevelIndex']))
        fis = [r for r in table(os.path.join(D, f'fiscal_s{sd}.tsv')) if r['nation'] == UK]
        R[sd] = dict(cl=cl, nat=nat, stk=stk, fis=fis, world_def=world_def)
    return R


def ratio(d, cls, i, j):
    a = sum(v[i] for c, v in d.items() if c in cls); b = sum(v[j] for c, v in d.items() if c in cls)
    return a / b if b > 0 else float('nan')


def metrics(S):
    cl, nat, stk, fis = S['cl'], S['nat'], S['stk'], S['fis']
    M = {}
    pe = lambda cls, q: ratio(cl[f'q{q}'], cls, 2, 1) if f'q{q}' in cl else float('nan')
    p1 = [pe(MASS, q) for q in range(0, 8)]
    M['P1p_min_Q1_Q8'] = min([x for x in p1 if x == x], default=float('nan'))
    M['p1_by_q'] = p1
    w = [pe(MASS, q) for q in range(4, QEND)]
    w = [x for x in w if x == x]
    M['P2_mass_paid_earned'] = sum(w) / len(w) if w else float('nan')
    wr = [pe(RICH, q) for q in range(4, QEND)]
    wr = [x for x in wr if x == x]
    M['rich_paid_earned'] = sum(wr) / len(wr) if wr else float('nan')
    M['mass_months_Q40'] = ratio(cl.get(f'q{QE}', {}), MASS, 0, 1) if f'q{QE}' in cl else float('nan')
    M['rich_months_Q40'] = ratio(cl.get(f'q{QE}', {}), RICH, 0, 1) if f'q{QE}' in cl else float('nan')
    d0 = cl.get('q0', {})
    s0 = sum(v[0] for v in d0.values()); e0 = sum(v[1] for v in d0.values())
    M['q0_savings_to_income_years'] = s0 / (12 * e0) if e0 > 0 else float('nan')
    last = nat.get(QE)
    M['P3a_default_Q40'] = fl(last['sovereignDefaultCumulativeCount']) if last and 'sovereignDefaultCumulativeCount' in last else float('nan')
    n_susp = 0; prev = None
    for r in fis:
        og = fl(r['onGold']); mi = int(r['monthIndex'])
        if prev == 1 and og == 0 and mi < QEND * 3:
            n_susp += 1
        prev = og
    M['P3b_suspensions_Q1_Q40'] = n_susp
    # (276) 借入の門（Q1〜Q8＝monthIndex 0〜23）と利払い÷歳入の最大。borrowGateOpenFlag の NaN（不足なし）は閉じたと数えない。
    gate_first = float('nan'); max_ir = float('nan')
    for r in fis:
        mi = int(r['monthIndex'])
        if mi >= 24:
            continue
        og = fl(r['borrowGateOpenFlag']) if 'borrowGateOpenFlag' in r else float('nan')
        if og == 0 and not (gate_first == gate_first):
            gate_first = mi
        ir_ = fl(r['interestToRevenueShareNow']) if 'interestToRevenueShareNow' in r else float('nan')
        if ir_ == ir_ and not (max_ir >= ir_):
            max_ir = ir_
    M['gate_first_closed_month_Q1_Q8'] = gate_first
    M['gate_closed_Q1_Q8'] = 1 if gate_first == gate_first else 0
    M['max_interest_to_revenue_Q1_Q8'] = max_ir
    M['P4_gdp_real_Q40'] = stk.get(QE, (float('nan'), float('nan')))[0]
    M['tech_Q40'] = stk.get(QE, (float('nan'), float('nan')))[1]
    rg = []; ir = []
    for q in range(4, QEND):
        r = nat.get(q)
        if not r: continue
        g = fl(r['nominalGdpPerYear']); rb = fl(r['revenueBaseMeanPerMonth'])
        if g > 0: rg.append(rb * 12 / g)
        if rb > 0: ir.append(fl(r['interestMeanPerMonth']) / rb)
    M['P5_revenue_to_gdp'] = sum(rg) / len(rg) if rg else float('nan')
    M['interest_to_revenue'] = sum(ir) / len(ir) if ir else float('nan')
    resid = 0.0
    for r in fis:
        rv = fl(r['revenueBasePerMonth']); rs = abs(fl(r['ledgerResidualPerMonth']))
        if rv > 0:
            if not (rs == rs):   # ★(275) 否定形＝NaN の残差は素通りさせない（反証役 bv：fail-open）
                resid = float('nan'); break
            resid = max(resid, rs / rv)
    M['P6_ledger_residual'] = resid
    q0 = nat.get(0)
    M['rev_Q1'] = fl(q0['revenueBaseMeanPerMonth']) if q0 else float('nan')
    for q in (3, 7, QE):
        r = nat.get(q); g = stk.get(q, (float('nan'),))[0]
        M[f'deflator_Q{q + 1}'] = fl(r['nominalGdpPerYear']) / g if r and g == g and g > 0 else float('nan')
    # 世界の不履行の総数（最後の四半期の累計の和）
    wd = 0.0
    for n, dct in S['world_def'].items():
        if dct: wd += dct[max(dct)]
    M['world_defaults_total'] = wd
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
out = [f'# (273) 腕の読み（英・種 {common}・窓 Q5〜Q{QEND}）\n\n']
LINES = [
    ('P1′ 谷：Q1〜Q8 の四半期ごとの最小（大衆の Paid÷Earned）', 'P1p_min_Q1_Q8', 3),
    ('P2 大衆の Paid÷Earned の窓の平均', 'P2_mass_paid_earned', 3),
    ('P3a 英の不履行の累計 Q40', 'P3a_default_Q40', 0),
    ('P3b 英の Q1〜Q40 の兌換停止の回数', 'P3b_suspensions_Q1_Q40', 0),
    ('P4 英の実質GDP Q40', 'P4_gdp_real_Q40', 0),
    ('P5 英の歳入÷年GDP（窓の平均）', 'P5_revenue_to_gdp', 4),
    ('P6 帳尻の残差÷歳入の最大', 'P6_ledger_residual', 12),
]
RECS = [
    ('記録：大衆の貯蓄の月数 Q40', 'mass_months_Q40', 1),
    ('記録：富裕の貯蓄の月数 Q40', 'rich_months_Q40', 1),
    ('記録：富裕の Paid÷Earned（窓の平均）', 'rich_paid_earned', 3),
    ('記録：q0 の全階級の貯蓄÷年所得（年）', 'q0_savings_to_income_years', 3),
    ('記録：利払い÷歳入（窓の平均）', 'interest_to_revenue', 3),
    ('記録：技術水準 Q40', 'tech_Q40', 3),
    ('記録：GDP デフレーター Q4', 'deflator_Q4', 3),
    ('記録：GDP デフレーター Q8', 'deflator_Q8', 3),
    ('記録：GDP デフレーター Q40', f'deflator_Q{QEND}', 3),
    ('記録：Q1 の歳入（月あたり）', 'rev_Q1', 0),
    ('記録：世界の不履行の総数（最後の四半期の累計の和）', 'world_defaults_total', 0),
]
arms = list(DATA)
for title, rows_ in (('線', LINES), ('記録の列', RECS)):
    out.append(f'## {title}（種の中央〔最小〜最大〕）\n\n| 量 | ' + ' | '.join(arms) + ' |\n|---|' + '---|' * len(arms) + '\n')
    for lab, key, nd in rows_:
        out.append(f'| {lab} | ' + ' | '.join(cell([MET[a][sd][key] for sd in common], nd) for a in arms) + ' |\n')
    out.append('\n')
# Q1 の歳入／B
if 'B' in MET:
    out.append('## 記録：Q1 の歳入／B（種ごとの比の中央）\n\n| 腕 | 中央〔最小〜最大〕 |\n|---|---|\n')
    for a in arms:
        if a == 'B': continue
        out.append(f'| {a} | ' + cell([MET[a][sd]['rev_Q1'] / MET['B'][sd]['rev_Q1'] for sd in common], 3) + ' |\n')
    out.append('\n')
# 線の判定
mode5 = N <= 5
susp_max = 2 if mode5 else 3
out.append(f'## 線の判定（{N}種・兌換停止の許容 ≦{susp_max}/{N}）\n\n| 腕 | P1′ | P2 | P3a | P3b | P4 | P5 | P6 |\n|---|---|---|---|---|---|---|---|\n')
for a in arms:
    if a == 'B':
        continue
    m = {k: [MET[a][sd][k] for sd in common] for k in MET[a][common[0]] if k != 'p1_by_q'}
    p1 = med(m['P1p_min_Q1_Q8']); p2 = med(m['P2_mass_paid_earned'])
    p3a = all(x == 0 for x in m['P3a_default_Q40'])   # ★(275) NaN（Q40 の行の欠け）は 0 と等しくない＝合格にしない（旧は NaN を落としてから all＝fail-open）
    nsusp = sum(1 for x in m['P3b_suspensions_Q1_Q40'] if x > 0)
    p4 = med(m['P4_gdp_real_Q40']); p5 = med(m['P5_revenue_to_gdp'])
    p6 = float('nan') if any(not (x == x) for x in m['P6_ledger_residual']) else max(m['P6_ledger_residual'])
    # ★(275) 線の量に NaN（行の欠け・0 割り）の種が1つでもあれば「判定不能」（med は NaN を落として中央を取る＝欠けを隠す）
    nanof = lambda k: sum(1 for x in m[k] if not (x == x))
    mark0 = lambda ok: '合格' if ok else '外れ'
    mark = lambda ok, k=None: (f'判定不能（NaN {nanof(k)}種）' if k is not None and nanof(k) else mark0(ok))
    p1txt = ('記録（S′）' if a.startswith('S2') else mark(p1 >= 0.5, 'P1p_min_Q1_Q8')) + f' {f(p1, 3)}'
    out.append(f'| {a} | {p1txt} | {mark(p2 >= 0.9, "P2_mass_paid_earned")} {f(p2, 3)} | {mark(p3a, "P3a_default_Q40")}（不履行のあった種 {sum(1 for x in m["P3a_default_Q40"] if x > 0)}/{N}） | {mark(nsusp <= susp_max)}（{nsusp}/{N}種） | {mark(p4 >= 3.12e5, "P4_gdp_real_Q40")} {f(p4, 0)} | {mark(p5 >= 0.0851, "P5_revenue_to_gdp")} {f(p5, 4)} | {mark(p6 < 0.01, "P6_ledger_residual")} {p6:.2e} |\n')
# (276) P7・P8・P0（腕 B＝基線を基準にする）
if 'B' in MET:
    nB = sum(MET['B'][sd]['gate_closed_Q1_Q8'] for sd in common)
    gate_tol = 1 if mode5 else 2
    out.append(f'\n## (276) P7・P8・P0 と記録（{N}種・P8 の許容＝腕 B の数 {nB} ＋{gate_tol}）\n\n| 腕 | P7 Q1 の歳入／基線の同じ種（中央） | P8 Q1〜Q8 に門が閉じた種 | P0 q0 の貯蓄÷年所得（中央・線 0.78〜1.46） | q0÷0.5 | 利払い÷歳入 Q1〜Q8 の最大の中央〔最小〜最大〕 |\n|---|---|---|---|---|---|\n')
    for a in arms:
        rat = [MET[a][sd]['rev_Q1'] / MET['B'][sd]['rev_Q1'] if MET['B'][sd]['rev_Q1'] > 0 else float('nan') for sd in common]
        nr = sum(1 for x in rat if not (x == x))
        p7 = med(rat)
        ncl = sum(MET[a][sd]['gate_closed_Q1_Q8'] for sd in common)
        q0v = [MET[a][sd]['q0_savings_to_income_years'] for sd in common]
        q0m = med(q0v)
        p7txt = ('基準' if a == 'B' else (f'判定不能（NaN {nr}種）' if nr else ('合格' if p7 >= 0.8 else '外れ'))) + f' {f(p7, 3)}'
        p8txt = ('基準' if a == 'B' else ('合格' if ncl <= nB + gate_tol else '外れ')) + f' {ncl}/{N}'
        p0txt = ('基準' if a == 'B' else ('判定不能（NaN）' if not (q0m == q0m) else ('内' if 0.78 <= q0m <= 1.46 else '外（積み戻し）'))) + f' {f(q0m, 3)}'
        out.append(f'| {a} | {p7txt} | {p8txt} | {p0txt} | {f(q0m / 0.5, 2)} | {cell([MET[a][sd]["max_interest_to_revenue_Q1_Q8"] for sd in common], 3)} |\n')
    out.append('\n門が閉じた最初の月（monthIndex・種ごと・—＝閉じず）：\n\n')
    for a in arms:
        out.append(f'- {a}: ' + '；'.join(f's{sd} {f(MET[a][sd]["gate_first_closed_month_Q1_Q8"], 0)}' for sd in common) + '\n')
    out.append('\n')
out.append('\n## 種ごとの値（P1′・P2・不履行・兌換停止・Q1 の歳入）\n\n')
for a in arms:
    out.append(f'- {a}: ' + '；'.join(f's{sd} P1′={f(MET[a][sd]["P1p_min_Q1_Q8"], 2)} P2={f(MET[a][sd]["P2_mass_paid_earned"], 2)} 不履行={f(MET[a][sd]["P3a_default_Q40"], 0)} 停止={MET[a][sd]["P3b_suspensions_Q1_Q40"]} 歳入Q1={f(MET[a][sd]["rev_Q1"], 0)}' for sd in common) + '\n')
out.append('\n## 大衆の Paid÷Earned の Q1〜Q8（種の中央）\n\n| 腕 | ' + ' | '.join(f'Q{q + 1}' for q in range(8)) + ' |\n|---|' + '---|' * 8 + '\n')
for a in arms:
    out.append(f'| {a} | ' + ' | '.join(f(med([MET[a][sd]['p1_by_q'][q] for sd in common]), 2) for q in range(8)) + ' |\n')
open(OUTPATH, 'w', encoding='utf-8', newline='\n').write(''.join(out))
print('done', OUTPATH)
