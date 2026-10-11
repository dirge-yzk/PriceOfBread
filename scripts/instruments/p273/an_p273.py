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
    ・置き直しの予測は SavingsYearsByClass（Coefficients.cs の宣言から読む）と助走の直後の warm の行の earnedPerMonth から作る式の写し＝助走の直後の LastEarned とは月の位相が違う（四半期の平均）ので近似。
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
        if 'warm' not in tl: continue
        d = tl['warm']
        inc = sum(v[1] for v in d.values() if v[1] > 0)
        shape = sum(K[c] * v[1] for c, v in d.items() if v[1] > 0 and c < len(K))
        if not (shape > 0): continue
        l = m * inc / shape
        lam.append(l)
        mm_pred.append(l * K[1] * 12)   # 農民の貯蓄の月数 ＝ λ×k×12（月収あたり）
        sav_before = sum(v[0] for v in d.values())
        sav_after = sum(l * K[c] * 12 * v[1] for c, v in d.items() if v[1] > 0 and c < len(K))
        rho.append(sav_after / sav_before if sav_before > 0 else float('nan'))
    out.append(f'\n## 置き直しの予測（{n}・m={m}・助走の直後の warm の行から）\n\n- λ（m×Σ所得÷Σk×所得）{cell(lam, 3)}\n- 農民の貯蓄の月数（λ×k×12）{cell(mm_pred, 2)} 月（大衆の k は農民・工場労働者 0.2・職人 0.5）\n- ρ（置いた後÷置く前のΣ貯蓄）{cell(rho, 4)}\n')
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
print('done', OUT)
