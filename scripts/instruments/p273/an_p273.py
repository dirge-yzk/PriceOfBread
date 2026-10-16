# ★(377) 開幕の貯蓄の置き直しの機構（OpeningSavingsRebaseMode・SavingsYearsByClass）は (375) で削除した＝再走は削除の前の木で
"""(273) 線P：段3（英の貯蓄の置き直し）＋段5（消費の予算の上限）の事前登録の入力を、基線の計器つき10種から出す読み手（読むだけ）。
  python an_p273.py <計器の出力ディレクトリ(ins)> <表の出力ディレクトリ> [国=イギリス,フランス,ネーデルラント,ポルトガル]
  入力＝標準の計器（`POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル INS=1 bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins）の
        class_s（挙げた国は四半期ごと＋open／warm の断面）と nation_s（全ての国の四半期ごと）。
  出力＝<表の出力>/ に markdown：
    t1_mass_savings.md   ①大衆（農民・工場労働者・職人）の貯蓄の月数〔貯蓄÷月収〕と Paid÷Earned の窓ごとの値（種の中央〔最小〜最大〕）＋置き直しの予測（英・助走の直後の所得から）
    t2_cap_grid.md       ②段5 の上限の格子 d ごとの拘束（大衆の Paid のうち上限を超える割合・削られる割合・拘束された POP の割合）
    t3_consume_bind.md   ③消費の予算の拘束（nation_s の consume… 列）
  限界の自己申告（計器が一番バグる）：
    ・「大衆」は計器の IsMass と同じ＝農民・工場労働者・職人（兵士を除く）。使用人・兵士は入らない。
    ・貯蓄の月数＝大衆の savingsStock の和 ÷ earnedPerMonth の和（窓の終わりの四半期の行〔time が q<番号>〕）。earnedPerMonth は四半期の月平均＝月の決算の終わりで読む（位相の罠・(262)）。
    ・置き直しの予測は SavingsYearsByClass（Coefficients.cs の宣言から読む）と最初の四半期 q0 の行の earnedPerMonth（四半期の月平均）から作る式の写し＝助走の最後の月の LastEarned とは違う（位相と1四半期の差）ので近似。
    ・種は全て中央値（と最小〜最大）。"""
import sys, os, re, math, statistics, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import num as fl, seeds  # noqa: E402

D, OUT = sys.argv[1], sys.argv[2]
NATIONS = (sys.argv[3] if len(sys.argv) > 3 else 'イギリス,フランス,ネーデルラント,ポルトガル').split(',')
os.makedirs(OUT, exist_ok=True)
MASS = {1, 2, 11}   # 農民・工場労働者・職人（Instruments.IsMass）
M_DATA = {'イギリス': 0.5}   # world.json の broadMoneyToIncome（今は英だけ・置き直しの予測用）

def table(path):
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            yield dict(zip(head, ln.rstrip('\n').split('\t')))

def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')

def rng(xs):
    xs = [x for x in xs if x == x]
    return (min(xs), max(xs)) if xs else (float('nan'), float('nan'))

def f(x, nd=3):
    return '—' if x != x else f'{x:.{nd}f}'

def cell(xs, nd=3):
    lo, hi = rng(xs)
    return f'{f(med(xs), nd)} ({f(lo, nd)}〜{f(hi, nd)})'

def years_by_class():
    here = os.path.dirname(os.path.abspath(__file__))
    src = open(os.path.join(here, '..', '..', '..', 'PriceOfBread.SimCore', 'Coefficients.cs'), encoding='utf-8-sig').read()
    m = re.search(r'public double\[\] SavingsYearsByClass = \[([^\]]*)\];', src)
    return [float(x) for x in m.group(1).split(',')]

SEEDS = seeds(D, 'class')
print('種', SEEDS)
K = years_by_class()

# ---- class_s：国→seed→time→クラス→(savings, earned, paid, size)
CL = {n: {} for n in NATIONS}
for sd in SEEDS:
    for r in table(os.path.join(D, f'class_s{sd}.tsv')):
        n = r['nation']
        if n not in CL: continue
        CL[n].setdefault(sd, {}).setdefault(r['time'], {})[int(r['classId'])] = (fl(r['savingsStock']), fl(r['earnedPerMonth']), fl(r['paidPerMonth']), fl(r['sizeK']))

def mass_months(d):
    s = sum(v[0] for c, v in d.items() if c in MASS); e = sum(v[1] for c, v in d.items() if c in MASS)
    return s / e if e > 0 else float('nan')

def all_savings_to_income_years(d):
    s = sum(v[0] for v in d.values()); e = sum(v[1] for v in d.values())
    return s / (12 * e) if e > 0 else float('nan')

def mass_paid_to_earned(d):
    p = sum(v[2] for c, v in d.items() if c in MASS); e = sum(v[1] for c, v in d.items() if c in MASS)
    return p / e if e > 0 else float('nan')

def window_mean(fn, tl, q0, q1):
    xs = [fn(tl[f'q{q}']) for q in range(q0, q1 + 1) if f'q{q}' in tl]
    xs = [x for x in xs if x == x]
    return sum(xs) / len(xs) if xs else float('nan')

QS = [3, 7, 19, 39, 79, 159]   # 四半期の番号（0始まり）＝Q4・Q8・Q20・Q40・Q80・Q160 の終わり
WIN = [(4, 39, 'Q5〜Q40'), (40, 79, 'Q41〜Q80'), (80, 159, 'Q81〜Q160')]
out = []
out.append('# ① 大衆の貯蓄の月数と Paid÷Earned（基線の計器つき10種・種の中央〔最小〜最大〕）\n')
out.append('限界：大衆＝農民・工場労働者・職人。貯蓄の月数＝Σ貯蓄÷Σ月収（四半期の行）。\n')
for n in NATIONS:
    if not CL[n]: continue
    out.append(f'\n## {n}\n')
    out.append('| 量 | ' + ' | '.join(f'Q{q+1} の終わり' for q in QS) + ' |\n|---|' + '---|' * len(QS) + '\n')
    out.append('| 大衆の貯蓄の月数 | ' + ' | '.join(cell([mass_months(CL[n][sd][f'q{q}']) for sd in CL[n] if f'q{q}' in CL[n][sd]], 1) for q in QS) + ' |\n')
    out.append('| 全階級の貯蓄÷年所得（年） | ' + ' | '.join(cell([all_savings_to_income_years(CL[n][sd][f'q{q}']) for sd in CL[n] if f'q{q}' in CL[n][sd]], 2) for q in QS) + ' |\n')
    out.append('\n| 量 | ' + ' | '.join(w[2] for w in WIN) + ' |\n|---|' + '---|' * len(WIN) + '\n')
    out.append('| 大衆の Paid÷Earned（窓の四半期の平均） | ' + ' | '.join(cell([window_mean(mass_paid_to_earned, CL[n][sd], a, b) for sd in CL[n]], 3) for a, b, _ in WIN) + ' |\n')
    # 開幕の断面（open・warm）
    for ph in ('open', 'warm'):
        xs = [mass_months(CL[n][sd][ph]) for sd in CL[n] if ph in CL[n][sd]]
        ys = [all_savings_to_income_years(CL[n][sd][ph]) for sd in CL[n] if ph in CL[n][sd]]
        if xs: out.append(f'\n- 断面 {ph}：大衆の貯蓄の月数 {cell(xs, 1)}・全階級の貯蓄÷年所得 {cell(ys, 2)} 年\n')
# 置き直しの予測（m の国だけ）
for n, m in M_DATA.items():
    if n not in CL or not CL[n]: continue
    lam, mm_pred, rho = [], [], []
    for sd, tl in CL[n].items():
        d = tl.get('q0')   # 助走の直後の warm の行は所得が nan（経済を回す前）＝最初の四半期 q0 の行を代わりに使う（置き直しの LastEarned は助走の最後の月＝近似）
        if d is None: continue
        inc = sum(v[1] for v in d.values() if v[1] > 0)
        shape = sum(K[c] * v[1] for c, v in d.items() if v[1] > 0 and c < len(K))
        if not (shape > 0): continue
        l = m * inc / shape
        lam.append(l)
        mass_e = sum(v[1] for c, v in d.items() if c in MASS and v[1] > 0)
        mm_pred.append(l * 12 * sum(K[c] * v[1] for c, v in d.items() if c in MASS and v[1] > 0 and c < len(K)) / mass_e if mass_e > 0 else float('nan'))   # 大衆の貯蓄の月数 ＝ λ×12×Σk×所得÷Σ所得
        sav_before = sum(v[0] for v in d.values())
        sav_after = sum(l * K[c] * 12 * v[1] for c, v in d.items() if v[1] > 0 and c < len(K))
        rho.append(sav_after / sav_before if sav_before > 0 else float('nan'))
    out.append(f'\n## 置き直しの予測（{n}・m={m}・最初の四半期 q0 の行から＝助走の直後の warm の行は所得が nan）\n\n- λ（m×Σ所得÷Σk×所得）{cell(lam, 3)}\n- 置き直し直後の大衆の貯蓄の月数（λ×12×Σk×所得÷Σ所得）{cell(mm_pred, 2)} 月（大衆の k は農民・工場労働者 0.2・職人 0.5）\n- ρ（置いた後÷置く前のΣ貯蓄）{cell(rho, 4)}\n')
open(os.path.join(OUT, 't1_mass_savings.md'), 'w', encoding='utf-8', newline='\n').write(''.join(out))

# ---- nation_s：上限の格子と消費の予算の拘束
NATC = collections.defaultdict(lambda: collections.defaultdict(list))   # nation -> seed -> [row]
head_seen = None
for sd in seeds(D, 'nation'):
    for r in table(os.path.join(D, f'nation_s{sd}.tsv')):
        if r['nation'] in NATIONS:
            NATC[r['nation']][sd].append(r)
            head_seen = r
DNAMES = ['D120', 'D240', 'D480', 'D36', 'D12']
def col_window(rows, name, a, b):
    xs = [fl(r[name]) for r in rows if a <= int(r['quarter']) <= b and name in r]
    xs = [x for x in xs if x == x]
    return sum(xs) / len(xs) if xs else float('nan')
o2 = ['# ② 段5 の上限の格子 d ごとの拘束（nation_s の cap… 列・窓の四半期の平均の種の中央〔最小〜最大〕・基線＝上限なし）\n']
for n in NATIONS:
    if not NATC[n]: continue
    o2.append(f'\n## {n}\n\n| 列 | ' + ' | '.join(w[2] for w in WIN) + ' |\n|---|' + '---|' * len(WIN) + '\n')
    for base, label in (('capBoundPaidShare', '拘束された大衆の Paid の割合'), ('capCutShare', '削られる額÷大衆の Paid'), ('capBoundPopShare', '拘束された大衆の人口の割合')):
        for dn in DNAMES:
            col = base + dn
            if col not in head_seen: continue
            o2.append(f'| {label} {dn}（d＝1/{dn[1:]}） | ' + ' | '.join(cell([col_window(NATC[n][sd], col, a, b) for sd in NATC[n]], 3) for a, b, _ in WIN) + ' |\n')
open(os.path.join(OUT, 't2_cap_grid.md'), 'w', encoding='utf-8', newline='\n').write(''.join(o2))

o3 = ['# ③ 消費の予算の拘束（nation_s の consume… 列・窓の四半期の平均の種の中央〔最小〜最大〕）\n']
CONS = ['consumeBudgetBoundRowShare', 'consumeCutShare', 'consumeDesiredValuePerMonth', 'consumeCutValuePerMonth']
for n in NATIONS:
    if not NATC[n]: continue
    o3.append(f'\n## {n}\n\n| 列 | ' + ' | '.join(w[2] for w in WIN) + ' |\n|---|' + '---|' * len(WIN) + '\n')
    for col in CONS:
        if col not in head_seen: continue
        o3.append(f'| {col} | ' + ' | '.join(cell([col_window(NATC[n][sd], col, a, b) for sd in NATC[n]], 4) for a, b, _ in WIN) + ' |\n')
open(os.path.join(OUT, 't3_consume_bind.md'), 'w', encoding='utf-8', newline='\n').write(''.join(o3))

# ---- ④ 事前登録の線の基線の数（英・Q5〜Q40 など）：実質GDP・技術・不履行・兌換停止・歳入÷年GDP・利払い÷歳入
from instr import rows  # noqa: E402
o4 = ['# ④ 事前登録の線の基線の数（英＝イギリス・種の中央〔最小〜最大〕）\n',
      '限界：実質GDP は stock_s の gdpRealPerYear（年末の行＝四半期の番号%4==3）・技術は techLevelIndex（同）。兌換停止＝fiscal_s の onGold が 1→0 になった月の数（月の添字÷3＝四半期）。'
      '不履行＝nation_s の sovereignDefaultCumulativeCount の窓の終わりの値。歳入÷年GDP＝revenueBaseMeanPerMonth×12÷nominalGdpPerYear の四半期の平均。\n']
UK = 'イギリス'
stk = collections.defaultdict(dict)   # seed -> time -> (gdpReal, tech)
for sd, r in rows(D, 'stock'):
    if r['nation'] == UK and r['time'].startswith('q'):
        stk[sd][r['time']] = (fl(r['gdpRealPerYear']), fl(r['techLevelIndex']))
QQ = (3, 19, 39, 79, 159, 239)
o4.append('\n## 実質GDP（年・stock_s）と技術水準\n\n| 量 | ' + ' | '.join(f'Q{q+1} の終わり' for q in QQ) + ' |\n|---|' + '---|' * len(QQ) + '\n')
for idx, lab, nd in ((0, '実質GDP（年）', 0), (1, '技術水準 techLevelIndex', 3)):
    o4.append(f'| {lab} | ' + ' | '.join(cell([stk[sd][f'q{q}'][idx] for sd in stk if f'q{q}' in stk[sd]], nd) for q in QQ) + ' |\n')
dflt = collections.defaultdict(dict); revgdp = collections.defaultdict(list); intrev = collections.defaultdict(list)
for sd, r in rows(D, 'nation'):
    if r['nation'] != UK: continue
    q = int(r['quarter'])
    dflt[sd][q] = fl(r['sovereignDefaultCumulativeCount'])
    if 4 <= q <= 39:
        g = fl(r['nominalGdpPerYear']); rb = fl(r['revenueBaseMeanPerMonth'])
        if g > 0: revgdp[sd].append(rb * 12 / g)
        intrev[sd].append(fl(r['interestMeanPerMonth']) / rb if rb > 0 else float('nan'))
o4.append('\n## 不履行（英・累計）・歳入÷年GDP・利払い÷歳入\n\n')
o4.append(f'- 英の不履行の累計 Q40 の終わり：10種の和 {sum(v.get(39, 0) for v in dflt.values()):.0f}・Q80：{sum(v.get(79, 0) for v in dflt.values()):.0f}・全期：{sum(max(v.values()) for v in dflt.values()):.0f}（種ごとの Q40：{[int(v.get(39, 0)) for _, v in sorted(dflt.items())]}）\n')
o4.append(f'- 歳入÷年GDP（Q5〜Q40 の四半期の平均）{cell([sum(v) / len(v) for v in revgdp.values()], 4)}\n')
o4.append(f'- 利払い÷歳入（Q5〜Q40 の四半期の平均）{cell([sum(x for x in v if x == x) / max(1, len([x for x in v if x == x])) for v in intrev.values()], 3)}\n')
susp = {}
for sd in seeds(D, 'fiscal'):
    prev = None; n40 = 0; nall = 0
    for r in table(os.path.join(D, f'fiscal_s{sd}.tsv')):
        if r['nation'] != UK: continue
        on_gold = fl(r['onGold']); mi = int(r['monthIndex'])
        if prev == 1 and on_gold == 0:
            nall += 1
            if mi < 120: n40 += 1
        prev = on_gold
    susp[sd] = (n40, nall)
if susp:
    o4.append(f'- 英の兌換停止（onGold 1→0）：Q1〜Q40 の10種の和 {sum(v[0] for v in susp.values())}（種ごと {[v[0] for _, v in sorted(susp.items())]}）・全期 {sum(v[1] for v in susp.values())}\n')
open(os.path.join(OUT, 't4_baseline_lines.md'), 'w', encoding='utf-8', newline='\n').write(''.join(o4))
print('done', OUT)
