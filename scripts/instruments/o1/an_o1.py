"""(315) 線O′：借入の門を貸し手の見る能力で読む（第2条件＝利払い÷月の名目GDP）の読み手（読むだけ・挙動は変えない）。
事前登録 measurements/o1_20261014_prereg.txt の [4][5]。
  python -X utf8 an_o1.py <出力.md> A=<ins> B=<ins> [C=<ins>] [SEEDS=<n>＝種を先頭の n 個に絞る（10種の走行から5種の読みを取り出す）]
入力＝`INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins
      nation_s（全ての国×四半期）・fiscal_s（英仏蘭葡の月ごと）。腕 A が基線・B が採否の腕・C が診断の腕（採否に使わない）。
線（事前登録 [4]・B を A に対して読む）：O1 兌換停止のある種（Q1〜Q40・全期）B≦A／O2 不履行のある種（同）B≦A／O3 英の門の閉じた月（monthIndex<24）B≦0.5×A／
      O4 英の利払い÷歳入（Q5〜Q40）B≦0.70 かつ B−A≦+0.10／O5 英の Q40 末の債務÷年GDP B≦1.25×A かつ ≦2.5／O6 金利の上限（≧0.299）への張り付きの割合 全体・英・蘭 B≦2×A／
      O7 世界の不履行 0.5×A〜2×A／O8 帳尻 <1%／O9 第2条件だけで開いた月（記録）。
限界の自己申告（計器が一番バグる）：
  ・兌換停止・不履行・門の閉じた月は fiscal_s の英の月ごとの行から数える（l4w.py と同じ定義：onGold が 0.5 以上から 0.5 未満へ＝停止・defaultCount>0＝不履行）。四半期の番号は monthIndex 0,1→Q1・以後 3 か月ずつ。
  ・O3 の月は monthIndex<24（Q1〜Q8 相当）＝borrowGateOpenFlag＝0 の月（借入の門＝第1条件 ∨ 第2条件が閉）。borrowBinder＝gateClosed は払えない額がある月だけに付く＝A では 0 になりうる（事前登録 [0]④）。
  ・利払い÷歳入（Q5〜Q40）は窓の利払いの和÷歳入の和（nation_s の四半期の月平均の和）を種ごとに出して中央。債務÷年GDP は govDebtStock÷nominalGdpPerYear（四半期の行の値）。
  ・張り付きの分母は bondMonthsCount＞0 の国×四半期（an_o0.py ①と同じ）。世界の不履行は nationUid ごとの sovereignDefaultCumulativeCount の種ごとの最大の和（an_o0.py ②と同じ）。
  ・種は腕の間で共通のものだけ。NaN は〈判定不能〉で合格にしない（否定形）。O4・O5 は種ごとの値の中央（窓の中央→種の中央の二重の中央は使わない）。"""
import sys, os, statistics, collections
sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows as irows, seeds as iseeds, num

OUT = sys.argv[1]
ARMS = collections.OrderedDict()
SEEDCAP = 0
for a in sys.argv[2:]:
    k, v = a.split('=', 1)
    if k == 'SEEDS':
        SEEDCAP = int(v)
    else:
        ARMS[k] = v
UK, NL, FR, PT = 'イギリス', 'ネーデルラント', 'フランス', 'ポルトガル'
W = [8, 40, 80, 160, 240, 10 ** 6]
WN = ['Q8', 'Q40', 'Q80', 'Q160', 'Q240', '全期']
CAP = 0.299


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def mn(xs):
    xs = [x for x in xs if x == x]
    return min(xs) if xs else float('nan')


def mx(xs):
    xs = [x for x in xs if x == x]
    return max(xs) if xs else float('nan')


def f(x, nd=3):
    return '—' if x != x else f'{x:.{nd}f}'


def qof(m):
    return 0 if m < 2 else 1 + (m - 2) // 3


def load(D):
    sds = sorted(set(iseeds(D, 'nation')) & set(iseeds(D, 'fiscal')))
    if SEEDCAP:
        sds = [s for s in sds if s <= SEEDCAP]
    R = dict(seeds=sds, nat=collections.defaultdict(lambda: collections.defaultdict(dict)),
             fis=collections.defaultdict(list), dflt={}, dflt_name={}, cap=collections.Counter(), world=collections.Counter())
    for sd, r in irows(D, 'nation'):
        if sd not in sds:
            continue
        n = r['nation']
        q = int(r['quarter'])
        if n in (UK, FR, NL, PT):
            R['nat'][n][sd][q] = r
        uid = r.get('nationUid', n)
        v = num(r.get('sovereignDefaultCumulativeCount', 'nan'))
        if v == v:
            R['dflt'][(sd, uid)] = max(R['dflt'].get((sd, uid), 0), int(v))
            R['dflt_name'][(sd, uid)] = n
        bm = num(r.get('bondMonthsCount', 'nan'))
        grp = ['全体'] + ([{UK: '英', NL: '蘭', FR: '仏'}[n]] if n in (UK, NL, FR) else [])
        if bm == bm and bm > 0:
            for g in grp:
                R['cap'][(g, 'den')] += 1
                if num(r['bondRateMeanPerYear']) >= CAP:
                    R['cap'][(g, 'num')] += 1
        for c in ('borrowGateObservedMonths', 'borrowGateClosedMonths', 'gateOpenedByGdpOnlyMonthsCount', 'gateOpenedByGdpOnlyBorrowedMonthsCount'):
            v = num(r.get(c, 'nan'))
            if v == v:
                R['world'][c] += int(v)
                if n == UK:
                    R['world'][c + '_uk'] += int(v)
    for sd, r in irows(D, 'fiscal'):
        if sd in sds:
            R['fis'][(sd, r['nation'])].append(r)
    return R


def fis_uk(R, sd):
    return sorted(R['fis'].get((sd, UK), []), key=lambda r: int(r['monthIndex']))


def susp_def(R):
    out = {w: [0, 0, 0] for w in W}   # 不履行のある種, 停止のある種, 停止の総回数
    for sd in R['seeds']:
        prev = None
        dq = {w: 0 for w in W}
        sq = {w: 0 for w in W}
        for r in fis_uk(R, sd):
            q = qof(int(r['monthIndex']))
            g = num(r['onGold'])
            dcv = num(r['defaultCount'])
            for w in W:
                if q < w:
                    if dcv > 0:
                        dq[w] = 1
                    if prev is not None and prev >= .5 and g < .5:
                        sq[w] += 1
            prev = g
        for w in W:
            out[w][0] += dq[w]
            out[w][1] += 1 if sq[w] > 0 else 0
            out[w][2] += sq[w]
    return out


def gate_closed_early(R):
    per = []
    for sd in R['seeds']:
        c = o = 0
        for r in fis_uk(R, sd):
            if int(r['monthIndex']) >= 24:
                continue
            o += 1
            if r['borrowGateOpenFlag'] == '0':
                c += 1
        per.append((c, o))
    return per


def ir_q5_40(R, nat=UK):
    xs = []
    for sd in R['seeds']:
        qs = [R['nat'][nat][sd].get(q) for q in range(4, 40)]
        qs = [r for r in qs if r]
        si = sum(num(r['interestMeanPerMonth']) for r in qs)
        sr = sum(num(r['revenueBaseMeanPerMonth']) for r in qs)
        xs.append(si / sr if sr > 0 else float('nan'))
    return xs


def rg_q5_40(R):
    xs = []
    for sd in R['seeds']:
        qs = [R['nat'][UK][sd].get(q) for q in range(4, 40)]
        qs = [r for r in qs if r]
        a = sum(num(r['revenueBaseMeanPerMonth']) * 12 for r in qs)
        b = sum(num(r['nominalGdpPerYear']) for r in qs)
        xs.append(a / b if b > 0 else float('nan'))
    return xs


def debt_q40(R, q=39, nat=UK):
    xs = []
    for sd in R['seeds']:
        r = R['nat'][nat][sd].get(q)
        xs.append(num(r['govDebtStock']) / num(r['nominalGdpPerYear']) if r and num(r['nominalGdpPerYear']) > 0 else float('nan'))
    return xs


def resid_max(R):
    m = 0.0
    for sd in R['seeds']:
        for r in fis_uk(R, sd):
            rv = num(r['revenueBasePerMonth'])
            rs = abs(num(r['ledgerResidualPerMonth']))
            if rv > 0:
                if not (rs == rs):
                    return float('nan')
                m = max(m, rs / rv)
    return m


def binder_split(R, mmax=120):
    tot = collections.Counter()
    early = []
    for sd in R['seeds']:
        gc = 0
        for r in fis_uk(R, sd):
            m = int(r['monthIndex'])
            if m >= mmax:
                continue
            if num(r['borrowShortfallPerMonth']) > 0:
                tot[r['borrowBinder']] += 1
                if m < 24 and r['borrowBinder'] == 'gateClosed':
                    gc += 1
        early.append(gc)
    return tot, early


def world_defaults(R):
    nm = R['dflt_name']
    return sum(R['dflt'].values()), sum(v for k, v in R['dflt'].items() if nm.get(k) == UK)


def rev_by_q(R, q):
    return [num(R['nat'][UK][sd][q]['revenueBaseMeanPerMonth']) if q in R['nat'][UK][sd] else float('nan') for sd in R['seeds']]


D = {a: load(p) for a, p in ARMS.items()}
common = sorted(set.intersection(*[set(d['seeds']) for d in D.values()]))
for d in D.values():
    d['seeds'] = [s for s in d['seeds'] if s in common]
L = []
P = L.append
P(f'# (315) 線O′ 読み手の出力（種 {len(common)}：{common}・腕 {list(ARMS)}）\n')
S = {a: susp_def(d) for a, d in D.items()}
P('## 記録の列：英の兌換停止・不履行（窓＝四半期未満・l4w と同じ定義）')
P('| 腕 | 窓 | 不履行のある種 | 停止のある種 | 停止の総回数 | 種数 |')
P('|---|---|---|---|---|---|')
for a in D:
    for w, nm in zip(W, WN):
        P(f'| {a} | {nm} | {S[a][w][0]} | {S[a][w][1]} | {S[a][w][2]} | {len(common)} |')
P('\n## O3 英の門の閉じた月（monthIndex<24・borrowGateOpenFlag＝0）')
G = {a: gate_closed_early(d) for a, d in D.items()}
for a in D:
    P(f'- {a}: {sum(c for c, o in G[a])} / {sum(o for c, o in G[a])} 月（種ごと {[c for c, o in G[a]]}）')
P('\n## O4 英の利払い÷歳入（Q5〜Q40・種の窓和比の中央）と 歳入÷年GDP と Q40 末の債務÷年GDP')
IR = {a: ir_q5_40(d) for a, d in D.items()}
DG = {a: debt_q40(d) for a, d in D.items()}
RG = {a: rg_q5_40(d) for a, d in D.items()}
for a in D:
    P(f'- {a}: 利払い÷歳入 {f(med(IR[a]))}（{f(mn(IR[a]))}〜{f(mx(IR[a]))}）・歳入÷年GDP {f(med(RG[a]), 4)}・Q40 末の債務÷年GDP {f(med(DG[a]))}（{f(mn(DG[a]))}〜{f(mx(DG[a]))}）')
P('\n## O6 金利の上限への張り付き（bondRateMeanPerYear≧0.299 ／ 債券の月がある国×四半期）')


def pr(d, g):
    return d['cap'][(g, 'num')] / d['cap'][(g, 'den')] if d['cap'][(g, 'den')] else float('nan')


for a, d in D.items():
    P(f'- {a}: ' + '・'.join(f'{g} {d["cap"][(g, "num")]}/{d["cap"][(g, "den")]}＝{f(pr(d, g), 5)}' for g in ('全体', '英', '蘭', '仏')))
P('\n## O7 国債の不履行（nationUid ごとの累計の最大の和）')
WD = {a: world_defaults(d) for a, d in D.items()}
for a in D:
    P(f'- {a}: 世界 {WD[a][0]}・英 {WD[a][1]}')
P('\n## O8 帳尻（英・全期・全種の |ledgerResidual|÷revenueBase の最大）')
RS = {a: resid_max(d) for a, d in D.items()}
for a in D:
    P(f'- {a}: {RS[a]:.3e}')
P('\n## O9 第2条件だけで門が開いた月（nation_s の新しい列・10種の和）')
for a, d in D.items():
    w = d['world']
    P(f'- {a}: 世界 {w["gateOpenedByGdpOnlyMonthsCount"]} 月（うち借りた月 {w["gateOpenedByGdpOnlyBorrowedMonthsCount"]}）・英 {w["gateOpenedByGdpOnlyMonthsCount_uk"]}（借りた {w["gateOpenedByGdpOnlyBorrowedMonthsCount_uk"]}）・世界の門の閉じた月 {w["borrowGateClosedMonths"]}/{w["borrowGateObservedMonths"]}')
P('\n## 記録：英の払えない月の binder（monthIndex<120・払えない額がある月）と Q1〜Q8 の gateClosed の月の出た種')
for a, d in D.items():
    t, fg = binder_split(d)
    P(f'- {a}: {dict(t)}（合計 {sum(t.values())}）・Q1〜Q8 に gateClosed の月が出た種 {sum(1 for x in fg if x > 0)}/{len(fg)}（月数 {sum(fg)}）')
P('\n## 記録：英の歳入（Q1〜Q8 の四半期ごと・種の中央・月あたり）と 他の腕÷A')
base = list(D)[0]
for q in range(0, 8):
    cells = [f'{a} {f(med(rev_by_q(D[a], q)), 1)}' for a in D]
    rr = []
    for a in list(D)[1:]:
        rr.append(f'{a}÷{base} {f(med([x / y if y and y > 0 else float("nan") for x, y in zip(rev_by_q(D[a], q), rev_by_q(D[base], q))]), 3)}')
    P(f'- Q{q + 1}: ' + '・'.join(cells) + '　' + '・'.join(rr))
P('\n## 記録：仏・蘭・葡の門の閉じた月（nation_s の月数の和）と 利払い÷歳入（Q5〜Q40・種の中央）')
for a, d in D.items():
    cells = []
    for n in (FR, NL, PT):
        gc = sum(int(num(r['borrowGateClosedMonths'])) for sd in d['seeds'] for r in d['nat'][n][sd].values() if num(r['borrowGateClosedMonths']) == num(r['borrowGateClosedMonths']))
        go = sum(int(num(r['borrowGateObservedMonths'])) for sd in d['seeds'] for r in d['nat'][n][sd].values() if num(r['borrowGateObservedMonths']) == num(r['borrowGateObservedMonths']))
        cells.append(f'{n} 閉 {gc}/{go}・利払い÷歳入Q5〜Q40 {f(med(ir_q5_40(d, n)))}')
    P(f'- {a}: ' + '　'.join(cells))
P('\n## 判定（B を A に対して読む・事前登録 [4]）')
if 'A' in D and 'B' in D:
    A, B = 'A', 'B'
    ok = {}
    ok['O1'] = S[B][40][1] <= S[A][40][1] and S[B][10 ** 6][1] <= S[A][10 ** 6][1]
    ok['O2'] = S[B][40][0] <= S[A][40][0] and S[B][10 ** 6][0] <= S[A][10 ** 6][0]
    cA, cB = sum(c for c, o in G[A]), sum(c for c, o in G[B])
    ok['O3'] = cB <= 0.5 * cA and all(b[0] <= a[0] for a, b in zip(G[A], G[B]))
    irA, irB = med(IR[A]), med(IR[B])
    ok['O4'] = (irB <= 0.70) and (irB - irA <= 0.10) if irB == irB and irA == irA else False
    dA, dB = med(DG[A]), med(DG[B])
    ok['O5'] = (dB <= 1.25 * dA) and (dB <= 2.5) if dA == dA and dB == dB else False
    ok['O6'] = all(pr(D[B], g) <= 2 * pr(D[A], g) for g in ('全体', '英', '蘭'))
    ok['O7'] = 0.5 * WD[A][0] <= WD[B][0] <= 2 * WD[A][0]
    ok['O8'] = RS[B] == RS[B] and RS[B] < 0.01
    mech = D[B]['world']['gateOpenedByGdpOnlyMonthsCount_uk'] > 0
    P(f'- 機構の確認（英で第2条件だけで開いた月 ≧1）：{"働いた" if mech else "★働いていない＝以下の線は採否に使わない"}（{D[B]["world"]["gateOpenedByGdpOnlyMonthsCount_uk"]} 月）')
    for k in ('O1', 'O2', 'O3', 'O4', 'O5', 'O6', 'O7', 'O8'):
        P(f'- {k}: {"合格" if ok[k] else "外れ"}')
    bad = [k for k in ok if not ok[k]]
    P(f'- 全体: {"機構が働いていない（線は採否に使わない）" if not mech else ("O1〜O8 全て合格" if not bad else "外れ：" + "・".join(bad))}')
if 'C' in D and 'A' in D:
    c40 = S['C'][40][1]
    d40 = S['C'][40][0]
    P(f'\n## C（診断）：鎖が外れたか＝英の兌換停止のある種 Q1〜Q40 {c40}/{len(common)}（線 ≦3）・Q40 までの不履行のある種 {d40}（線 0）→ {"外れた" if c40 <= 3 and d40 == 0 else "残る"}（参考：(305) の B〔置き直しのみ〕は 10/10 と 6/10）')
open(OUT, 'wb').write(('\n'.join(L) + '\n').encode('utf-8'))
print('\n'.join(L))
