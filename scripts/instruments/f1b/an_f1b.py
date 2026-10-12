"""(288) Fork1 測り直しの腕の読み手（事前登録 measurements/f1b_20261012_prereg.txt の [5][6][7][15]）＝読むだけ・挙動は変えない。
  python an_f1b.py <出力のmarkdown> [--seeds 1-5] 腕名=<計器の出力ディレクトリ(ins)> [腕名=…]
  入力＝標準の計器の nation_s<seed>.tsv（全ての国・四半期ごと）。腕名は A（基線）・B（主の腕）・C（記録専用）など。--seeds で種を絞る（5種の読み＝10種の走行の種 1〜5）。
  出す物（英＝イギリス・仏＝フランスの種の中央〔最小〜最大〕）：
    ① 水準と構成（P1〜P3・P5）：歳入÷年GDP／純税収÷年GDP／間接税÷年GDP／直接税の割合／利払い÷歳入 を窓 Q1・Q2〜Q8・Q3〜Q12・Q5〜Q40 で。
    ② 土地税の推移（P4・h2）：landTaxEndShare と landTaxTargetEndShare の Q1〜Q12・Q16・Q20・Q40 の四半期の終わりの中央。
       教義が目標を戻す四半期（定義は [7] で固定）：(a) landTaxTargetEndShare が 0.2667 以上に初めて達する四半期 (b) landTaxEndShare が 0.25 以上に初めて達する四半期（Q40 までに届かない種の数を併記）。
    ③ 戦線の薄め（[15] (i)・P9）：開いた戦線の数・R.LandTax の戦線が開いている種の数・pushBudgetFactor・ratePaidInQuarter・politicalCapitalSpentIndex・frontCapitalFixedCount・frontNetSupportFixedCount・militaryMoveCurrentShare を Q1〜Q12 の四半期ごとの中央。
       P9＝pushBudgetFactor の腕÷A（種ごとの比の中央・Q1〜Q4）。
    ④ 仏の漏れの確認（P8）：仏の歳入÷年GDP と直接税の割合 Q2〜Q8。
  限界の自己申告：
    ・土地税の率 landTaxEndShare は四半期の終わりの値（Move の後）。四半期の途中の動きは見えない。
    ・pushBudgetFactor は四半期の頭に数えた戦線 N の (1/N)^0.5（Instruments.Extras274）。施行の中で変わった戦線は反映されない。
    ・列が無い腕（古い木）は nan（—）と出す。種の中央は nan を除いて取る（全て nan なら —）。
"""
import sys, os, statistics, collections, re
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import num as fl, seeds  # noqa: E402

UK, FR = 'イギリス', 'フランス'
args = sys.argv[1:]
OUTP = args.pop(0)
SEL = None
if '--seeds' in args:
    i = args.index('--seeds'); a, b = args[i + 1].split('-'); SEL = set(range(int(a), int(b) + 1)); del args[i:i + 2]
ARMS = collections.OrderedDict()
for a in args:
    k, v = a.split('=', 1); ARMS[k] = v

COLS = ['nominalGdpPerYear', 'interestMeanPerMonth', 'revenueBaseMeanPerMonth', 'directTaxNetMeanPerMonth', 'indirectRevenueMeanPerMonth',
        'landTaxEndShare', 'landTaxTargetEndShare', 'openFrontsCount', 'openFrontStates', 'pushBudgetFactor', 'ratePaidInQuarter', 'politicalCapitalSpentIndex',
        'frontCapitalFixedCount', 'frontNetSupportFixedCount', 'militaryMoveCurrentShare', 'monopolyTierEndStage']

def load(D):
    """seed -> nation -> {quarter(0始まり): 行の辞書(必要な列だけ)}"""
    out = {}
    for sd in seeds(D, 'nation'):
        if SEL is not None and sd not in SEL: continue
        d = collections.defaultdict(dict)
        with open(os.path.join(D, f'nation_s{sd}.tsv'), encoding='utf-8') as fh:
            head = fh.readline().rstrip('\n').split('\t'); ix = {h: i for i, h in enumerate(head)}
            iq, inat = ix['quarter'], ix['nation']
            for ln in fh:
                f = ln.rstrip('\n').split('\t')
                if f[inat] not in (UK, FR): continue
                d[f[inat]][int(f[iq])] = {c: f[ix[c]] for c in COLS if c in ix}
        out[sd] = d
    return out

def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')

def cell(xs, nd=3):
    ys = [x for x in xs if x == x]
    if not ys: return '—'
    return f'{statistics.median(ys):.{nd}f} ({min(ys):.{nd}f}〜{max(ys):.{nd}f})'

def val(r, c): return fl(r.get(c, 'nan'))

def win(rows, q0, q1): return [rows[q] for q in range(q0, q1 + 1) if q in rows]

def ratio(rows, num_c, mult=12.0):
    s = sum(val(r, num_c) * mult for r in rows); g = sum(val(r, 'nominalGdpPerYear') for r in rows)
    return s / g if g else float('nan')

def share_dir(rows):
    d = sum(val(r, 'directTaxNetMeanPerMonth') for r in rows); i = sum(val(r, 'indirectRevenueMeanPerMonth') for r in rows)
    return d / (d + i) if d + i > 0 else float('nan')

def int_rev(rows):
    r_ = sum(val(r, 'revenueBaseMeanPerMonth') for r in rows)
    return sum(val(r, 'interestMeanPerMonth') for r in rows) / r_ if r_ else float('nan')

def first_q(rows, col, th, qmax=39):
    for q in range(0, qmax + 1):
        if q in rows and val(rows[q], col) >= th: return q + 1
    return float('nan')

DATA = {k: load(v) for k, v in ARMS.items()}
L = []
sds_note = f'（種 {min(SEL)}〜{max(SEL)} のみ）' if SEL else ''
L.append(f'# f1b 読み手の出力 {sds_note}\n')
for k, d in DATA.items(): L.append(f'- 腕 {k}：{ARMS[k]}（種 {sorted(d)}）')
L.append('')

WINS = [('Q1', 0, 0), ('Q2〜Q8', 1, 7), ('Q3〜Q12', 2, 11), ('Q5〜Q40', 4, 39)]
for nat, label in ((UK, '英'), (FR, '仏')):
    L.append(f'## ① {label}の水準と構成（種の中央〔最小〜最大〕）\n')
    L.append('| 腕 | 指標 | ' + ' | '.join(w[0] for w in WINS) + ' |'); L.append('|---|---|' + '---|' * len(WINS))
    items = [('歳入÷年GDP', lambda rs: ratio(rs, 'revenueBaseMeanPerMonth'), 4), ('純税収（直接税）÷年GDP', lambda rs: ratio(rs, 'directTaxNetMeanPerMonth'), 4),
             ('間接税÷年GDP', lambda rs: ratio(rs, 'indirectRevenueMeanPerMonth'), 4), ('直接税の割合', share_dir, 3), ('利払い÷歳入', int_rev, 3)]
    for k, d in DATA.items():
        for lab, f, nd in items:
            xs = []
            for w in WINS:
                v = []
                for sd, dn in d.items():
                    rs = win(dn.get(nat, {}), w[1], w[2])
                    v.append(f(rs) if rs else float('nan'))
                xs.append(cell(v, nd))
            L.append(f'| {k} | {lab} | ' + ' | '.join(xs) + ' |')
    L.append('')

QS = list(range(0, 12)) + [15, 19, 39]
L.append('## ② 英の土地税の推移（四半期の終わり・種の中央〔最小〜最大〕）\n')
L.append('| 腕 | 量 | ' + ' | '.join(f'Q{q+1}' for q in QS) + ' |'); L.append('|---|---|' + '---|' * len(QS))
for k, d in DATA.items():
    for lab, col in (('landTaxEndShare（率）', 'landTaxEndShare'), ('landTaxTargetEndShare（目標）', 'landTaxTargetEndShare')):
        L.append(f'| {k} | {lab} | ' + ' | '.join(cell([val(dn[UK][q], col) if q in dn.get(UK, {}) else float('nan') for dn in d.values()], 3) for q in QS) + ' |')
L.append('')
L.append('教義が目標を戻す四半期（定義は事前登録 [7]）：\n')
L.append('| 腕 | (a) 目標が 0.2667 以上に初めて届く Qn（届いた種の中央・届かない種の数） | (b) 率が 0.25 以上に初めて届く Qn（同） | 率 Q8 末 | 率 Q40 末 |'); L.append('|---|---|---|---|---|')
for k, d in DATA.items():
    qa = [first_q(dn.get(UK, {}), 'landTaxTargetEndShare', 0.2666) for dn in d.values()]
    qb = [first_q(dn.get(UK, {}), 'landTaxEndShare', 0.25) for dn in d.values()]
    na = sum(1 for x in qa if x != x); nb = sum(1 for x in qb if x != x)
    r8 = [val(dn[UK][7], 'landTaxEndShare') if 7 in dn.get(UK, {}) else float('nan') for dn in d.values()]
    r40 = [val(dn[UK][39], 'landTaxEndShare') if 39 in dn.get(UK, {}) else float('nan') for dn in d.values()]
    L.append(f'| {k} | {f"{med(qa):.1f}" if med(qa) == med(qa) else "—"}（届かない {na}/{len(qa)}） | {f"{med(qb):.1f}" if med(qb) == med(qb) else "—"}（届かない {nb}/{len(qb)}） | {cell(r8)} | {cell(r40)} |')
L.append('\n種ごとの (a)(b)（Qn・届かない＝—）：\n')
def qn(x): return '—' if x != x else str(int(x))
for k, d in DATA.items():
    L.append(f'- {k}：' + '、'.join(f's{sd}=({qn(first_q(dn.get(UK, {}), "landTaxTargetEndShare", 0.2666))}, {qn(first_q(dn.get(UK, {}), "landTaxEndShare", 0.25))})' for sd, dn in d.items()))
L.append('')

Q12 = list(range(0, 12))
L.append('## ③ 英の戦線の薄め（四半期の終わりの行・種の中央）\n')
L.append('| 腕 | 量 | ' + ' | '.join(f'Q{q+1}' for q in Q12) + ' |'); L.append('|---|---|' + '---|' * len(Q12))
for k, d in DATA.items():
    for lab, col, nd in (('開いた戦線 openFrontsCount', 'openFrontsCount', 1), ('pushBudgetFactor', 'pushBudgetFactor', 3), ('ratePaidInQuarter（R の政治資本の払い）', 'ratePaidInQuarter', 3),
                         ('politicalCapitalSpentIndex', 'politicalCapitalSpentIndex', 3), ('frontCapitalFixedCount', 'frontCapitalFixedCount', 1),
                         ('frontNetSupportFixedCount', 'frontNetSupportFixedCount', 1), ('militaryMoveCurrentShare', 'militaryMoveCurrentShare', 4)):
        L.append(f'| {k} | {lab} | ' + ' | '.join(cell([val(dn[UK][q], col) if q in dn.get(UK, {}) else float('nan') for dn in d.values()], nd) for q in Q12) + ' |')
    cnt = []
    for q in Q12:
        c = 0; n = 0
        for dn in d.values():
            r = dn.get(UK, {}).get(q)
            if r is None: continue
            n += 1; c += 1 if 'R.LandTax' in r.get('openFrontStates', '') else 0
        cnt.append(f'{c}/{n}')
    L.append(f'| {k} | R.LandTax の戦線が開いている種 | ' + ' | '.join(cnt) + ' |')
    mono = [cell([val(dn[UK][q], 'monopolyTierEndStage') if q in dn.get(UK, {}) else float('nan') for dn in d.values()], 1) for q in Q12]
    L.append(f'| {k} | 専売の段（決定の後） | ' + ' | '.join(mono) + ' |')
L.append('')
if 'A' in DATA:
    L.append('P9：pushBudgetFactor の 腕÷A（種ごとの比の中央〔最小〜最大〕）\n')
    L.append('| 腕 | Q1〜Q4 の平均の比 | Q5〜Q8 の平均の比 | Q9〜Q12 の平均の比 |'); L.append('|---|---|---|---|')
    for k, d in DATA.items():
        if k == 'A': continue
        cols = []
        for q0, q1 in ((0, 3), (4, 7), (8, 11)):
            rs = []
            for sd, dn in d.items():
                if sd not in DATA['A']: continue
                a = [val(DATA['A'][sd][UK][q], 'pushBudgetFactor') for q in range(q0, q1 + 1) if q in DATA['A'][sd].get(UK, {})]
                b = [val(dn[UK][q], 'pushBudgetFactor') for q in range(q0, q1 + 1) if q in dn.get(UK, {})]
                a = [x for x in a if x == x]; b = [x for x in b if x == x]
                rs.append((sum(b) / len(b)) / (sum(a) / len(a)) if a and b and sum(a) else float('nan'))
            cols.append(cell(rs, 3))
        L.append(f'| {k} | ' + ' | '.join(cols) + ' |')
    L.append('')
open(OUTP, 'w', encoding='utf-8').write('\n'.join(L) + '\n')
print('done', OUTP)
