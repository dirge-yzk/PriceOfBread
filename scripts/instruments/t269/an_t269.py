"""(269) 線T 段T 窃盗のフロー化の読み手（読むだけ）。腕A・腕B に同じスクリプトを当てる。
  python an_t269.py <計器の出力ディレクトリ(ins)> <表の出力ファイル.md> [--seeds N]
  入力＝標準の計器（INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル）の class_s・flow_s・nation_s。
        flow_s は挙げた国だけ（英・仏・蘭）。nation_s は全ての国。
  出力＝事前登録の線の量（英・10種の中央〔最小〜最大〕）。窓は四半期の添字（Q1＝q0）：W1＝Q5〜Q40・W2＝Q41〜Q80・W3＝Q81〜Q160・W4＝Q161〜Q240。
    ・大衆(農民・工場労働者・職人)の ΔSavings÷国の所得（生の H＝その符号を反転）／大衆の Paid÷Earned／大衆の貯蓄の在庫÷月収（窓の終わり）
    ・富裕(貴族・資本家・商人)の ΔSavings÷Earned／使用人の Paid÷Earned
    ・窃盗の額÷国の所得（全階級の受け取り＝flow_s の (Crime.cs, 65) の give を窓で足す÷国の所得）
    ・大衆の Paid÷Earned の四半期の最小（Q5〜Q40・種ごと）＝1.1 以下に落ちた種の数
    ・全ての国：補給崩壊（supplyFillShare＜0.3）の国×四半期のうち人口100千人以上／種ごとの最大／同じ国が連続100四半期以上の件数、不履行 sovereignDefaultCount の種ごとの最後の値の和（列が無ければ「列なし」）
  計器の限界：class_s の Earned／Paid は四半期の末の月の値（flow_s の額は四半期の月平均）＝窓の和は ×3 の近似。Y（国の所得）は class_s の earnedPerMonth の和×3。
"""
import sys, os, math, statistics, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import num as fl, seeds  # noqa: E402

D, OUT = sys.argv[1], sys.argv[2]
MAXSEED = None
if '--seeds' in sys.argv:
    MAXSEED = int(sys.argv[sys.argv.index('--seeds') + 1])
NATIONS = ['イギリス', 'フランス', 'ネーデルラント']
MONTHS = 3
MASS = ('農民', '工場労働者', '職人'); RICH = ('貴族', '資本家', '商人'); SERV = ('使用人',)
WINS = [('Q5〜Q40', 4, 39), ('Q41〜Q80', 40, 79), ('Q81〜Q160', 80, 159), ('Q161〜Q240', 160, 239)]
CRIME_ROUTE = ('Crime.cs', 65)


def table(path):
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            yield dict(zip(head, ln.rstrip('\n').split('\t')))


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def cell(xs, nd=3):
    xs = [x for x in xs if x == x]
    if not xs: return '—'
    return f'{statistics.median(xs):.{nd}f} ({min(xs):.{nd}f}〜{max(xs):.{nd}f})'


SEEDS = seeds(D, 'class')
if MAXSEED: SEEDS = [s for s in SEEDS if s <= MAXSEED]
CLS = {}; NAMES = {}
for sd in SEEDS:
    d = collections.defaultdict(lambda: collections.defaultdict(dict))
    for r in table(os.path.join(D, f'class_s{sd}.tsv')):
        if r['nation'] not in NATIONS: continue
        c = int(r['classId']); NAMES[c] = r['className']
        d[r['nation']][c][r['time']] = {'sav': fl(r['savingsStock']), 'earn': fl(r['earnedPerMonth']), 'paid': fl(r['paidPerMonth'])}
    CLS[sd] = d
IDS = {k: [c for c, n in NAMES.items() if n in v] for k, v in (('mass', MASS), ('rich', RICH), ('serv', SERV))}
FLOW = {}
for sd in SEEDS:
    p = os.path.join(D, f'flow_s{sd}.tsv')
    if not os.path.exists(p): continue
    d = collections.defaultdict(lambda: collections.defaultdict(float))
    for r in table(p):
        if r['kind'] == 'give' and (r['sourceFile'], int(r['sourceLine'])) == CRIME_ROUTE:
            d[r['nation']][int(r['quarter'])] += fl(r['amountPerMonth'])
    FLOW[sd] = d


def q(sd, nation, c, qq):
    return CLS[sd][nation][c].get('warm' if qq < 0 else f'q{qq}')


def window(sd, nation, group, q0, q1):
    """窓の（国の所得 Y・ΔSavings・群の Earned の和・群の Paid の和・窓の終わりの月収）。無ければ None。"""
    if nation not in CLS[sd]: return None
    Y = 0.0
    for c in CLS[sd][nation]:
        for qq in range(q0, q1 + 1):
            r = CLS[sd][nation][c].get(f'q{qq}')
            if r is None or r['earn'] != r['earn']: return None
            Y += r['earn'] * MONTHS
    dS = E = P = endE = endS = 0.0
    for c in IDS[group]:
        if c not in CLS[sd][nation]: continue
        a, b = q(sd, nation, c, q0 - 1), q(sd, nation, c, q1)
        if a is None or b is None: return None
        dS += b['sav'] - a['sav']; endS += b['sav']; endE += b['earn']
        for qq in range(q0, q1 + 1):
            r = CLS[sd][nation][c][f'q{qq}']
            E += r['earn'] * MONTHS; P += r['paid'] * MONTHS
    return Y, dS, E, P, endS, endE


def crime_ratio(sd, nation, q0, q1):
    w = window(sd, nation, 'mass', q0, q1)
    if w is None or not (w[0] > 0) or sd not in FLOW: return float('nan')
    return sum(FLOW[sd][nation].get(qq, 0.0) * MONTHS for qq in range(q0, q1 + 1)) / w[0]


def mass_paid_min(sd, nation, q0, q1):
    xs = []
    for qq in range(q0, q1 + 1):
        e = p = 0.0
        for c in IDS['mass']:
            r = CLS[sd][nation].get(c, {}).get(f'q{qq}')
            if r and r['earn'] == r['earn']: e += r['earn']; p += r['paid']
        if e > 0: xs.append(p / e)
    return min(xs) if xs else float('nan')


L = ['# t269 段T 窃盗のフロー化の読み（種の中央〔最小〜最大〕）\n', f'入力 `{D}`・種 {SEEDS}\n']
tsv = ['nation\twindow\tquantity\tmedian\tmin\tmax\tseeds']
QS = [
    ('大衆の ΔSavings÷国の所得（生の H＝符号を反転）', lambda w, sd, n, q0, q1: w['mass'][1] / w['mass'][0] if w['mass'] else float('nan')),
    ('大衆の Paid÷Earned', lambda w, sd, n, q0, q1: w['mass'][3] / w['mass'][2] if w['mass'] and w['mass'][2] > 0 else float('nan')),
    ('大衆の貯蓄の在庫÷月収（窓の終わり・月数）', lambda w, sd, n, q0, q1: w['mass'][4] / w['mass'][5] if w['mass'] and w['mass'][5] > 0 else float('nan')),
    ('富裕の ΔSavings÷Earned', lambda w, sd, n, q0, q1: w['rich'][1] / w['rich'][2] if w['rich'] and w['rich'][2] > 0 else float('nan')),
    ('富裕の ΔSavings÷国の所得', lambda w, sd, n, q0, q1: w['rich'][1] / w['rich'][0] if w['rich'] else float('nan')),
    ('使用人の Paid÷Earned', lambda w, sd, n, q0, q1: w['serv'][3] / w['serv'][2] if w['serv'] and w['serv'][2] > 0 else float('nan')),
    ('窃盗の額÷国の所得（全階級の受け取り）', lambda w, sd, n, q0, q1: crime_ratio(sd, n, q0, q1)),
    ('大衆の Paid÷Earned の四半期の最小', lambda w, sd, n, q0, q1: mass_paid_min(sd, n, q0, q1)),
]
for nation in NATIONS:
    L.append(f'\n## {nation}\n')
    L.append('| 量 | ' + ' | '.join(w[0] for w in WINS) + ' |')
    L.append('|---|' + '---|' * len(WINS))
    for lab, f in QS:
        cells = []
        for wname, q0, q1 in WINS:
            xs = []
            for sd in SEEDS:
                w = {g: window(sd, nation, g, q0, q1) for g in ('mass', 'rich', 'serv')}
                try: xs.append(f(w, sd, nation, q0, q1))
                except (ZeroDivisionError, KeyError): xs.append(float('nan'))
            cells.append(cell(xs))
            ys = [x for x in xs if x == x]
            if ys: tsv.append(f'{nation}\t{wname}\t{lab}\t{statistics.median(ys):.6g}\t{min(ys):.6g}\t{max(ys):.6g}\t{len(ys)}')
        L.append(f'| {lab} | ' + ' | '.join(cells) + ' |')
# 英の大衆の貯蓄が尽きた（Paid÷Earned の最小 ≦1.1）種の数
for nation in NATIONS:
    cnt = sum(1 for sd in SEEDS if mass_paid_min(sd, nation, 4, 39) <= 1.1)
    L.append(f'\n- {nation}：大衆の Paid÷Earned の四半期の最小（Q5〜Q40）が 1.1 以下の種 {cnt}/{len(SEEDS)}')

# ---------------------------------------------------------------- 全ての国（nation_s）
L.append('\n## 全ての国（nation_s）\n')
tot = 0; per = {}; runs100 = 0; big = 0; defaults = 0; have_def = True
for sd in SEEDS:
    p = os.path.join(D, f'nation_s{sd}.tsv')
    if not os.path.exists(p): continue
    n_low = 0; best_run = collections.Counter(); cur = {}; last_def = {}
    for r in table(p):
        key = (r['nationIndex'], r['nation'])
        low = fl(r['supplyFillShare']) < 0.3 and fl(r['populationK']) >= 100
        if low:
            n_low += 1; cur[key] = cur.get(key, 0) + 1
            if cur[key] > best_run[key]: best_run[key] = cur[key]
        else:
            cur[key] = 0
        if 'sovereignDefaultCount' in r: last_def[key] = fl(r['sovereignDefaultCount'])
        else: have_def = False
    per[sd] = n_low; tot += n_low
    if n_low >= 1000: big += 1
    runs100 += sum(1 for v in best_run.values() if v >= 100)
    if have_def: defaults += sum(last_def.values())
L.append(f'- 補給崩壊（supplyFillShare＜0.3 かつ人口100千人以上）の国×四半期：合計 {tot}・種ごと {dict(sorted(per.items()))}・種ごとの最大 {max(per.values()) if per else 0}・千件級 {big}/{len(per)}・同じ国が連続100四半期以上の件数 {runs100}')
L.append(f'- 不履行（sovereignDefaultCount の種ごとの最後の値の和・全ての国）：{defaults:.0f}' if have_def else '- 不履行：列なし（この入力の nation_s に sovereignDefaultCount が無い）')
open(OUT, 'w', encoding='utf-8').write('\n'.join(L) + '\n')
open(OUT.replace('.md', '.tsv'), 'w', encoding='utf-8').write('\n'.join(tsv) + '\n')
print('\n'.join(L))
