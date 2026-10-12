"""F1 段0 の表（(258)・実装ログ (257) §5 の fable の設計の段1〜段5 の予測を、今の基線の数字で置き直す入力）の読み手。
  python an_f1s0_savings_chain.py <計器の出力ディレクトリ(ins)> <表の出力ディレクトリ> [自国=イギリス] [仏=フランス]
  入力＝標準の計器（`POB_INSTRUMENTS_NATIONS=イギリス,フランス INS=1 bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins）の
        class_s（挙げた国は四半期ごと）・fiscal_s（挙げた国の月ごと）・nation_s（全ての国の四半期ごと・(258) で財政の鎖の列を足した）。
  出力＝<表の出力>/ に markdown と tsv：
    t1_uk_class.md        ①英の階級ごとの Paid÷Earned・ΔSavings÷Earned（Q1〜Q8・Q1〜Q40・20年窓）＝金利生活者のポンプの大きさ
    t2_uk_gate.md         ②英の利払い÷歳入・借入の門が閉じた月・r＜1 の月・不足のある月の 借入÷不足（Q1〜Q8・Q1〜Q40・全期）
    t3_uk_revenue.md      ③英の歳入÷年GDP・間接税の割合
    t4_fr_1783_1795.md    ④仏の 1783〜1795 の年ごとの同じ列（仏 1788 の利払い÷歳入と支払い停止）
    t5_all_nations_gate.md / .tsv   ②の全 61 国の要約（国×20年窓の種の中央）
  限界の自己申告（計器が一番バグる）：
    ・ΔSavings は貯蓄の在庫の変化＝所得−支出以外の移転（税・移民に伴う貯蓄の移動・人口の増減・汚職・金利配当の受け取りなど）を含む。〈そのうち Earned−Paid で説明できない分〉を residual 列で出す。
    ・Paid÷Earned の分子と分母は階級の月平均（四半期の3か月の平均×3か月）。Earned に入らない受取（配当・利子・移転）は Paid の側にだけ出ることがある＝階級ごとに読む。
    ・〈門が閉じた月〉は月初の借入の判定（Fiscal.BorrowGateOpen）＝払えない額が無い月も数える。借入に効くのは〈不足があり、かつ門が閉じた月〉（t2 に両方を出す）。
    ・〈不足のある月〉は月初の払える額−請求（借越を含む）＜0 の月。
  種は全て中央値（と最小・最大）で要約する。10種の中央値＝〈種の中央〉。"""
import sys, os, math, statistics, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import num as fl, seeds  # noqa: E402  (261) 反証役 T1 低#5＝数の読みと seed の列挙は共有の読み口 instr.py の1本

def table(path):
    """1本の表（1試合）を行の辞書で。instr.rows は全ての seed を順に読む＝seed ごとに1本ずつ読むこの読み手では使わない（class_s は1本で数十万行）。"""
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

def f3(x, nd=3):
    if x != x: return '—'
    return f'{x:.{nd}f}'

def cell(xs, nd=3):
    """中央（最小〜最大）"""
    lo, hi = rng(xs)
    return f'{f3(med(xs), nd)} ({f3(lo, nd)}〜{f3(hi, nd)})'

D, OUT = sys.argv[1], sys.argv[2]
UK = sys.argv[3] if len(sys.argv) > 3 else 'イギリス'
FR = sys.argv[4] if len(sys.argv) > 4 else 'フランス'
os.makedirs(OUT, exist_ok=True)
SEEDS = seeds(D, 'nation')
print('種', SEEDS)

# ---------------------------------------------------------------- 読み込み
# nation_s：国×四半期（全ての国）
NAT = {}      # seed -> nation -> [row by quarter]
for sd in SEEDS:
    d = collections.defaultdict(list)
    for r in table(os.path.join(D, f'nation_s{sd}.tsv')):
        q = int(r['quarter'])
        row = {'q': q, 'year': int(r['year']), 'gdpY': fl(r['nominalGdpPerYear']), 'pop': fl(r['populationK']),
               'gateObs': fl(r['borrowGateObservedMonths']), 'gateClosed': fl(r['borrowGateClosedMonths']), 'short': fl(r['shortfallMonths']), 'rUnder': fl(r['payRateBelowOneMonths']),
               'interest': fl(r['interestMeanPerMonth']), 'revenue': fl(r['revenueBaseMeanPerMonth']), 'direct': fl(r['directTaxNetMeanPerMonth']), 'indirect': fl(r['indirectRevenueMeanPerMonth']),
               'shortfall': fl(r['shortfallMeanPerMonth']), 'borrowed': fl(r['borrowedMeanPerMonth']), 'debt': fl(r['govDebtStock']),
               # (287) 線F1の列（腕が(287)以後の木のときだけ在る＝無ければ nan）
               'landTax': fl(r.get('landTaxEndShare', 'nan')), 'landTarget': fl(r.get('landTaxTargetEndShare', 'nan')), 'polity': fl(r.get('polityCode', 'nan')),
               'priv': fl(r.get('privilegeIntactIndex', 'nan')), 'grip': fl(r.get('parliamentGripIndex', 'nan')),
               'monoEnd': fl(r.get('monopolyTierEndStage', 'nan')), 'monoBefore': fl(r.get('monopolyTierBeforeDecisionStage', 'nan'))}
        d[r['nation']].append(row)
    NAT[sd] = d

def qwin(rows, q0, q1):
    return [r for r in rows if q0 <= r['q'] <= q1]

# ---------------------------------------------------------------- ① 階級（class_s）
CLS = {}   # seed -> nation -> class -> {time: row}
for sd in SEEDS:
    p = os.path.join(D, f'class_s{sd}.tsv')
    if not os.path.exists(p): continue
    d = collections.defaultdict(lambda: collections.defaultdict(dict))
    for r in table(p):
        if r['nation'] not in (UK, FR): continue
        d[r['nation']][(int(r['classId']), r['className'])][r['time']] = {'size': fl(r['sizeK']), 'sav': fl(r['savingsStock']), 'neg': fl(r['negativeSavingsStock']),
                                                                         'earn': fl(r['earnedPerMonth']), 'paid': fl(r['paidPerMonth']), 'year': int(r['year'])}
    CLS[sd] = d

def class_stats(sd, nation, ckey, q0, q1):
    """窓 q0..q1（四半期の添字・両端を含む）の Paid÷Earned・ΔSavings÷Earned・残差。前の断面は q0-1（q0=0 なら warm）。"""
    t = CLS[sd][nation][ckey]
    prev = t.get(f'q{q0 - 1}') if q0 > 0 else t.get('warm')
    last = t.get(f'q{q1}')
    if prev is None or last is None: return None
    earn = paid = 0.0
    for q in range(q0, q1 + 1):
        r = t.get(f'q{q}')
        if r is None or r['earn'] != r['earn']: return None
        earn += r['earn'] * 3; paid += r['paid'] * 3
    dsav = last['sav'] - prev['sav']
    if not (earn > 0): return None
    return {'paid_earned': paid / earn, 'dsav_earned': dsav / earn, 'resid': (dsav - (earn - paid)) / earn, 'sav_end_earn_months': last['sav'] / (last['earn'] if last['earn'] > 0 else float('nan')),
            'size_end': last['size'], 'sav0': prev['sav'], 'earn_per_month_end': last['earn']}

def t1():
    L = []
    L.append(f'# t1 {UK} の階級ごとの Paid÷Earned・ΔSavings÷Earned（種の中央〔最小〜最大〕・{len(CLS)}種）\n')
    L.append('Paid÷Earned＝窓の支出の和÷所得の和（階級の月平均×3か月の四半期の和）。ΔSavings÷Earned＝窓の貯蓄の在庫の変化÷所得の和。'
             'residual＝(ΔSavings−(Earned−Paid))÷Earned＝所得−支出で説明できない貯蓄の変化（税・移転・人口の出入り・配当の受け取りなど。計器が自分で申告する限界）。'
             '窓は四半期の添字（Q1＝q0）。20年窓は Q1〜Q80・Q81〜Q160 …（四半期＝1/4年）。\n')
    seeds_ = [sd for sd in CLS if UK in CLS[sd]]
    classes = sorted({k for sd in seeds_ for k in CLS[sd][UK]})
    wins = [('Q1', 0, 0), ('Q1〜Q4', 0, 3), ('Q2〜Q8', 1, 7), ('Q4〜Q12', 3, 11), ('Q5〜Q40', 4, 39), ('Q1〜Q40', 0, 39), ('Q41〜Q80(1793〜1802)', 40, 79), ('Q81〜Q160(1803〜1822)', 80, 159), ('Q161〜Q240(1823〜1842)', 160, 239), ('Q241〜Q320(1843〜1862)', 240, 319)]
    for metric, label in (('paid_earned', 'Paid÷Earned'), ('dsav_earned', 'ΔSavings÷Earned'), ('resid', 'residual（ΔSavings の Earned−Paid で説明できない分÷Earned）')):
        L.append(f'\n## {label}\n')
        L.append('| 階級 | 人口(千人・開幕) | ' + ' | '.join(w[0] for w in wins) + ' |')
        L.append('|---|---|' + '---|' * len(wins))
        for ck in classes:
            vals = []
            for _, q0, q1 in wins:
                xs = []
                for sd in seeds_:
                    if ck in CLS[sd][UK]:
                        s = class_stats(sd, UK, ck, q0, q1)
                        if s: xs.append(s[metric])
                vals.append(cell(xs, 2))
            sizes = [CLS[sd][UK][ck].get('warm', {}).get('size', float('nan')) for sd in seeds_ if ck in CLS[sd][UK]]
            L.append(f'| {ck[1]}({ck[0]}) | {f3(med(sizes), 0)} | ' + ' | '.join(vals) + ' |')
    # 人口の変化（ΔSavings に人口の入れ替わりが混じる度合いを読む）
    L.append('\n## 階級の人口の変化（窓の終わり÷窓の前の断面〔Q1 の前は warm〕・ΔSavings の読みの限界：人口の入れ替わりが混じる）\n')
    L.append('| 階級 | ' + ' | '.join(w[0] for w in wins) + ' |')
    L.append('|---|' + '---|' * len(wins))
    for ck in classes:
        vals = []
        for _, q0, q1 in wins:
            xs = []
            for sd in seeds_:
                t = CLS[sd][UK].get(ck, {})
                prev = t.get(f'q{q0 - 1}') if q0 > 0 else t.get('warm'); last = t.get(f'q{q1}')
                if prev and last and prev['size'] > 0: xs.append(last['size'] / prev['size'])
            vals.append(cell(xs, 2))
        L.append(f'| {ck[1]}({ck[0]}) | ' + ' | '.join(vals) + ' |')
    # 貯蓄÷月収（開幕）＝k の表
    L.append('\n## 貯蓄の在庫÷月収（助走の直後・Q1 の終わり・Q8 の終わり・Q40 の終わり）＝何か月分の貯蓄か\n')
    L.append('| 階級 | warm | Q1末 | Q8末 | Q40末 |')
    L.append('|---|---|---|---|---|')
    for ck in classes:
        row = []
        for tm in ('warm', 'q0', 'q7', 'q39'):
            xs = []
            for sd in seeds_:
                r = CLS[sd][UK].get(ck, {}).get(tm)
                if r and r['earn'] == r['earn'] and r['earn'] > 0: xs.append(r['sav'] / r['earn'])
                elif r and tm == 'warm':
                    # warm の Earned は nan（流量を読まない断面）＝ Q1 の月収で割る
                    r1 = CLS[sd][UK].get(ck, {}).get('q0')
                    if r1 and r1['earn'] > 0: xs.append(r['sav'] / r1['earn'])
            row.append(cell(xs, 1))
        L.append(f'| {ck[1]}({ck[0]}) | ' + ' | '.join(row) + ' |')
    open(os.path.join(OUT, 't1_uk_class.md'), 'w', encoding='utf-8').write('\n'.join(L) + '\n')


# ---------------------------------------------------------------- ⑥ 段3 と 段5 の順の規則の当てはめ（AI の規則案・作者の承認は未確認）
def t6():
    """大衆＝農民・工場労働者・職人／富裕＝貴族・資本家・商人。窓は四半期の添字。Q1 は階級の人口の入れ替わりで貯蓄が跳ねる（工場労働者 +10 など）ので規則の窓は Q2 から。"""
    MASS = ('農民', '工場労働者', '職人'); RICH = ('貴族', '資本家', '商人')
    seeds_ = [sd for sd in CLS if UK in CLS[sd]]
    def agg(sd, names, q0, q1):
        e = p = ds = 0.0
        for ck, t in CLS[sd][UK].items():
            if ck[1] not in names: continue
            prev = t.get(f'q{q0 - 1}') if q0 > 0 else t.get('warm'); last = t[f'q{q1}']
            e += sum(t[f'q{q}']['earn'] * 3 for q in range(q0, q1 + 1)); p += sum(t[f'q{q}']['paid'] * 3 for q in range(q0, q1 + 1)); ds += last['sav'] - prev['sav']
        return e, p, ds
    def eall(sd, q0, q1): return sum(sum(t[f'q{q}']['earn'] * 3 for q in range(q0, q1 + 1)) for t in CLS[sd][UK].values())
    L = []
    L.append(f'# t6 段3（英の貯蓄の置き直し）と 段5（消費の予算が所得を読む）の順の規則の当てはめ（{len(seeds_)}種・種の中央〔最小〜最大〕）\n')
    L.append('⚠⚠ 規則と線は AI（このセッション）の案。作者の承認は未確認。fable の設計（実装ログ (257) §5）の反証役 F1 中#4〈順は段0 の表を見てから決める規則にする〉への答えの下書き。\n')
    L.append('大衆＝農民・工場労働者・職人／富裕＝貴族・資本家・商人。窓は Q2〜Q8（Q1 は階級間の人口の入れ替わりで貯蓄が跳ねる＝工場労働者 ΔSavings÷Earned +10 など）。\n')
    L.append('## 規則に使う量\n')
    L.append('| 量 | 定義 | Q2〜Q8 | Q4〜Q12 | Q5〜Q40 |')
    L.append('|---|---|---|---|---|')
    wins = [(1, 7), (3, 11), (4, 39)]
    def per(f): return [[f(sd, q0, q1) for sd in seeds_] for q0, q1 in wins]
    defs = [
        ('H 需要の穴', '大衆の貯蓄の取り崩し（−ΔSavings）÷国の所得（全階級の Earned）の和', lambda sd, a, b: -agg(sd, MASS, a, b)[2] / eall(sd, a, b), 3),
        ('P0 大衆の Paid÷Earned', '大衆の支出÷所得', lambda sd, a, b: agg(sd, MASS, a, b)[1] / agg(sd, MASS, a, b)[0], 2),
        ('f 取り崩しの寄与', '大衆の（−ΔSavings）÷（Paid−Earned）＝超過支出のうち貯蓄の取り崩しで賄った割合', lambda sd, a, b: (-agg(sd, MASS, a, b)[2]) / (agg(sd, MASS, a, b)[1] - agg(sd, MASS, a, b)[0]) if agg(sd, MASS, a, b)[1] > agg(sd, MASS, a, b)[0] else float('nan'), 2),
        ('R 富裕の不使用', '富裕の（Earned−Paid）÷国の所得＝金利生活者のポンプの大きさ（富裕が使わずに握った所得の割合）', lambda sd, a, b: (agg(sd, RICH, a, b)[0] - agg(sd, RICH, a, b)[1]) / eall(sd, a, b), 3),
        ('富裕の Paid÷Earned', '富裕の支出÷所得', lambda sd, a, b: agg(sd, RICH, a, b)[1] / agg(sd, RICH, a, b)[0], 2),
        ('富裕の ΔSavings÷Earned', '富裕の貯蓄の在庫の変化÷富裕の所得（段6 の線〈0.2 を超えて続く〉）', lambda sd, a, b: agg(sd, RICH, a, b)[2] / agg(sd, RICH, a, b)[0], 2),
        ('大衆の所得が国の所得に占める割合', '大衆の Earned÷全階級の Earned', lambda sd, a, b: agg(sd, MASS, a, b)[0] / eall(sd, a, b), 3),
    ]
    for lab, d, f, nd in defs:
        cols = per(f)
        L.append(f'| {lab} | {d} | ' + ' | '.join(cell(c, nd) for c in cols) + ' |')
    # 段5 の上限：min(S, Earned + S×d) → 支出の上限（所得の何倍か）= 1 + (S/月収)×d
    L.append('\n## 段5 の予算の上限は基線で拘束するか：上限（所得の何倍）＝1＋（貯蓄÷月収）×d（d は月あたりの引き出し率）\n')
    L.append('助走の直後の貯蓄÷Q1 の月収（月数）を使う。大衆の支出 P0（Q4〜Q12）が上限を超えれば拘束する（支出が下がる）。拘束しなければ単独の腕は無作用。\n')
    L.append('| 階級 | 貯蓄÷月収(warm) | P0 Paid÷Earned(Q4〜Q12) | 上限 d=1/12 | 上限 d=1/36 | 上限 d=1/120 | 拘束する d |')
    L.append('|---|---|---|---|---|---|---|')
    for cname in MASS:
        ks = []; p0s = []
        for sd in seeds_:
            for ck, t in CLS[sd][UK].items():
                if ck[1] != cname: continue
                w0 = t.get('warm'); q0r = t.get('q0')
                if w0 and q0r and q0r['earn'] > 0: ks.append(w0['sav'] / q0r['earn'])
                e, pp, _ = agg(sd, (cname,), 3, 11)
                if e > 0: p0s.append(pp / e)
        k = med(ks); p0 = med(p0s)
        caps = [1 + k / 12, 1 + k / 36, 1 + k / 120]
        binding = [lab for lab, c in zip(('1/12', '1/36', '1/120'), caps) if p0 > c]
        L.append(f'| {cname} | {f3(k, 1)} | {f3(p0, 2)} | {f3(caps[0], 2)} | {f3(caps[1], 2)} | {f3(caps[2], 2)} | {"、".join(binding) if binding else "なし（単独では無作用）"} |')
    # 階級の貯蓄の変化のうち Earned−Paid で説明できない分（残差）と、国庫の台帳の動き（国庫が配る・取る額）の比較＝残差の出どころが国庫か
    L.append('\n## 残差（ΔSavings−(Earned−Paid)）÷国の所得と、国庫の台帳の動き÷国の所得（Q5〜Q40）\n')
    L.append('大衆への残差が正で大きいとき、その出どころが国庫（福祉・年金・補助・教育・兵士の給与など）なら台帳の動きがほぼ同じ額になる。台帳の動きが小さければ出どころは国庫の外（資本市場の配当・利子、階級の移動に伴う貯蓄の移動、人口の増減など）＝未同定。' + "\n")
    def resid_group(sd, names, q0, q1):
        r = 0.0
        for ck, t in CLS[sd][UK].items():
            if ck[1] not in names: continue
            prev = t.get(f'q{q0 - 1}') if q0 > 0 else t.get('warm'); last = t[f'q{q1}']
            e = sum(t[f'q{q}']['earn'] * 3 for q in range(q0, q1 + 1)); pp = sum(t[f'q{q}']['paid'] * 3 for q in range(q0, q1 + 1))
            r += (last['sav'] - prev['sav']) - (e - pp)
        return r
    L.append('| 量 | Q5〜Q40 |')
    L.append('|---|---|')
    L.append('| 大衆の残差÷国の所得 | ' + cell([resid_group(sd, MASS, 4, 39) / eall(sd, 4, 39) for sd in seeds_], 3) + ' |')
    L.append('| 富裕の残差÷国の所得 | ' + cell([resid_group(sd, RICH, 4, 39) / eall(sd, 4, 39) for sd in seeds_], 3) + ' |')
    L.append('| 全階級の残差÷国の所得 | ' + cell([resid_group(sd, tuple(k[1] for k in CLS[sd][UK]), 4, 39) / eall(sd, 4, 39) for sd in seeds_], 3) + ' |')
    ledger = {}
    for sd in seeds_:
        rows = [r for r in table(os.path.join(D, f'fiscal_s{sd}.tsv')) if r['nation'] == UK]
        sel = rows[2 + 3 * 3: 2 + 3 * 39]   # Q5〜Q40 の月（最初の四半期は2行・以後3行ずつ）
        ea = eall(sd, 4, 39)
        for k in rows[0]:
            if k.startswith('ledger') and k.endswith('PerMonth') and k not in ('ledgerSumPerMonth', 'ledgerResidualPerMonth'):
                ledger.setdefault(k, []).append(sum(fl(r[k]) for r in sel) / ea)
    for k, v in sorted(ledger.items(), key=lambda kv: -abs(med(kv[1])))[:6]:
        L.append(f'| 国庫の台帳 {k[len("ledger"):-len("PerMonth")]}（符号つき）÷国の所得 | ' + cell(v, 3) + ' |')
    L.append('\n## 規則案（AI・未承認）と当てはめ\n')
    L.append('- 規則 A：H（Q2〜Q8）≧0.15 なら 段3 の単独は需要の穴を一度に開ける＝段5 を 段3 の前（同じ腕に載せる）。H＜0.05 なら 段3 を先。その間は同時の腕を並べる。')
    L.append('- 規則 B：段5 の上限が基線で拘束しない（P0 ≦ 上限）なら 段5 の単独の腕は基線と同じ＝順を単独の効果では決められない・段5 は 段3 と同じ腕で測る。')
    Hs = [agg(sd, MASS, 1, 7) for sd in seeds_]
    hv = [-agg(sd, MASS, 1, 7)[2] / eall(sd, 1, 7) for sd in seeds_]
    L.append(f'- 当てはめ：H(Q2〜Q8)＝{cell(hv, 3)}。規則 A の線 0.15 を{"超える" if med(hv) >= 0.15 else "超えない"}（種の最小 {f3(min(hv), 3)}）。')
    open(os.path.join(OUT, 't6_order_rule.md'), 'w', encoding='utf-8').write('\n'.join(L) + '\n')

# ---------------------------------------------------------------- ② 門（fiscal_s の月次）
FIS = {}   # seed -> nation -> [month rows]
for sd in SEEDS:
    p = os.path.join(D, f'fiscal_s{sd}.tsv')
    if not os.path.exists(p): continue
    d = collections.defaultdict(list)
    for r in table(p):
        d[r['nation']].append({'m': int(r['monthIndex']), 'year': int(float(r['year'])), 'ratio': fl(r['interestToRevenueShareNow']), 'rev': fl(r['revenueBasePerMonth']),
                               'gate': fl(r['borrowGateOpenFlag']), 'r': fl(r['payRateShare']), 'shortfall': fl(r['borrowShortfallPerMonth']), 'borrowed': fl(r['borrowedPerMonth']),
                               'binder': r['borrowBinder'], 'debt': fl(r['govDebtStock']), 'rate': fl(r['govBondRatePerYear']), 'coupon': fl(r['govAvgCouponPerYear']),
                               'defaults': fl(r['defaultCount']), 'insolv': fl(r['insolvencyMonths']), 'conv': fl(r['convertibilityShare']), 'gold': fl(r['onGold']),
                               'direct': fl(r['directTaxNetPerMonth']), 'indirect': fl(r['indirectRevenuePerMonth']), 'deval': fl(r['devaluedPerMonth']), 'claim': fl(r['claimTotalPerMonth'])})
    for n in d:
        rows = d[n]
        # 四半期の添字＝先頭の2行が q0（最初の四半期は台帳の差分の前の月が無く2行）・以後3行ずつ
        for i, r in enumerate(rows): r['q'] = 0 if i < 2 else 1 + (i - 2) // 3
    FIS[sd] = d

def month_stats(rows):
    """月次の行の集合 → 指標。"""
    n = len(rows)
    if n == 0: return None
    closed = sum(1 for r in rows if r['gate'] == 0)
    sf = [r for r in rows if r['shortfall'] == r['shortfall'] and r['shortfall'] > 0]
    sfc = [r for r in sf if r['gate'] == 0]
    rb = sum(1 for r in rows if r['r'] == r['r'] and r['r'] < 1)
    ratios = [r['ratio'] for r in rows if r['ratio'] == r['ratio']]
    sh = sum(r['shortfall'] for r in sf); bo = sum((r['borrowed'] if r['borrowed'] == r['borrowed'] else 0) for r in sf)
    return {'n': n, 'ratio_mean': (sum(ratios) / len(ratios)) if ratios else float('nan'), 'ratio_max': max(ratios) if ratios else float('nan'),
            'closed': closed, 'closed_share': closed / n, 'short': len(sf), 'short_closed': len(sfc), 'rbelow': rb, 'borrow_over_short': (bo / sh) if sh > 0 else float('nan'),
            'borrowed_months': sum(1 for r in sf if r['borrowed'] == r['borrowed'] and r['borrowed'] > 1e-9),
            'ratio_over_055': sum(1 for r in rows if r['ratio'] == r['ratio'] and r['ratio'] >= 0.55)}

def t2():
    L = []
    L.append(f'# t2 {UK} の利払い÷歳入・借入の門・r＜1・不足のある月の 借入÷不足（種の中央〔最小〜最大〕・{len(FIS)}種）\n')
    L.append('月次の表（fiscal_s）から。窓は四半期の添字（Q1〜Q8＝最初の2年・Q1〜Q40＝最初の10年）。月の数は窓の中の月（最初の月は台帳の差分が無く除く）。'
             '〈門が閉じた月〉＝月初の Fiscal.BorrowGateOpen が偽（利払い÷歳入≧0.55・冷却中のどれか）。'
             '借入÷不足＝不足のある月の 借入の和÷払えない額の和。\n')
    seeds_ = [sd for sd in FIS if UK in FIS[sd]]
    wins = [('Q1〜Q8', 0, 7), ('Q1〜Q40', 0, 39), ('Q41〜Q80', 40, 79), ('Q81〜Q160', 80, 159), ('全期', 0, 10**6)]
    L.append('| 指標 | ' + ' | '.join(w[0] for w in wins) + ' |')
    L.append('|---|' + '---|' * len(wins))
    keys = [('ratio_mean', '利払い÷歳入（月平均）', 3), ('ratio_max', '利払い÷歳入（窓の最大）', 3), ('closed', '門が閉じた月の数', 1), ('closed_share', '門が閉じた月の割合', 3),
            ('ratio_over_055', '利払い÷歳入≧0.55 の月の数', 1), ('short', '不足のある月の数', 1), ('short_closed', 'うち門が閉じていた月', 1), ('borrowed_months', 'うち実際に借りた月', 1),
            ('rbelow', 'r＜1 の月の数', 1), ('borrow_over_short', '不足のある月の 借入÷不足', 3), ('n', '窓の月数', 0)]
    S = {w[0]: [month_stats([r for r in FIS[sd][UK] if w[1] <= r['q'] <= w[2]]) for sd in seeds_] for w in wins}
    for k, label, nd in keys:
        L.append(f'| {label} | ' + ' | '.join(cell([s[k] for s in S[w[0]] if s], nd) for w in wins) + ' |')
    # 種ごとの Q1〜Q8
    L.append('\n## 種ごとの Q1〜Q8（月数 23）\n')
    L.append('| 種 | 利払い÷歳入(平均) | 門が閉じた月 | 不足のある月 | うち門が閉じていた | うち借りた月 | r＜1 の月 | 借入÷不足 |')
    L.append('|---|---|---|---|---|---|---|---|')
    for sd, s in zip(seeds_, S['Q1〜Q8']):
        if s: L.append(f"| {sd} | {f3(s['ratio_mean'])} | {s['closed']} | {s['short']} | {s['short_closed']} | {s['borrowed_months']} | {s['rbelow']} | {f3(s['borrow_over_short'])} |")
    # 四半期ごと（Q1〜Q12）の中央
    L.append('\n## 四半期ごと（Q1〜Q12・種の中央）：利払い÷歳入／門が閉じた月（3か月中）／不足のある月／r＜1 の月／借入÷不足\n')
    L.append('| 四半期 | 利払い÷歳入 | 門が閉じた月 | 不足のある月 | r＜1 の月 | 借入÷不足 |')
    L.append('|---|---|---|---|---|---|')
    for q in range(12):
        ss = [month_stats([r for r in FIS[sd][UK] if r['q'] == q]) for sd in seeds_]
        ss = [s for s in ss if s]
        L.append(f'| Q{q + 1} | ' + ' | '.join([cell([s['ratio_mean'] for s in ss], 2), cell([s['closed'] for s in ss], 0), cell([s['short'] for s in ss], 0), cell([s['rbelow'] for s in ss], 0), cell([s['borrow_over_short'] for s in ss], 2)]) + ' |')
    # 国債・不履行
    L.append('\n## 英の国債（月末）・不履行・兌換（窓の終わり・種の中央）\n')
    L.append('| 窓の終わり | 国債÷年GDP | 国債の金利(年率) | 不履行の累計 | 兌換(share) | 金本位 |')
    L.append('|---|---|---|---|---|---|')
    for lab, q in (('Q8末', 7), ('Q40末', 39), ('Q80末', 79), ('Q160末', 159), ('Q240末', 239)):
        rows = []
        for sd in seeds_:
            nrows = [r for r in NAT[sd].get(UK, []) if r['q'] == q]
            frows = [r for r in FIS[sd][UK] if r['q'] == q]
            if nrows and frows:
                rows.append((nrows[0]['debt'] / nrows[0]['gdpY'] if nrows[0]['gdpY'] > 0 else float('nan'), frows[-1]['rate'], frows[-1]['defaults'], frows[-1]['conv'], frows[-1]['gold']))
        if rows:
            L.append(f'| {lab} | ' + ' | '.join(cell([r[i] for r in rows], 3) for i in range(5)) + ' |')
    open(os.path.join(OUT, 't2_uk_gate.md'), 'w', encoding='utf-8').write('\n'.join(L) + '\n')

# ---------------------------------------------------------------- ③ 歳入÷年GDP・間接税の割合
def t3():
    L = []
    L.append(f'# t3 {UK} の歳入÷年GDP・間接税の割合（種の中央〔最小〜最大〕・{len(SEEDS)}種）\n')
    L.append('歳入＝予算の基数（Fiscal の budgetBase＝純税収＋IndirectTaxInBudgetBase×間接税）の四半期の月平均×12。年GDP＝名目の年GDP（nation_s）。'
             '間接税の割合＝間接税（関税＋専売の実収）÷（純税収＋間接税）＝係数1.0 の前提（Coefficients.IndirectTaxInBudgetBase の既定）で、番人が 予算の基数＝純税収＋係数×間接税 を確かめている。'
             '間接税＝関税と専売の実収。英の消費税の類いはここに入らない（純税収側）。\n')
    # (287) Q2〜Q8（事前登録 rev1_20261012_prereg の L1・L2'・L5 の窓）と Q3〜Q12（L3 の窓）を足す。Q1〜Q8 は残す＝黙って置き換えない（Q1 は貿易の立ち上がりの過渡を含む）。
    wins = [('Q1', 0, 0), ('Q1〜Q8', 0, 7), ('Q2〜Q8', 1, 7), ('Q3〜Q12', 2, 11), ('Q5〜Q40', 4, 39), ('Q1〜Q40', 0, 39), ('Q41〜Q80', 40, 79), ('Q81〜Q160', 80, 159), ('Q161〜Q240', 160, 239)]
    L.append('| 指標 | ' + ' | '.join(w[0] for w in wins) + ' |')
    L.append('|---|' + '---|' * len(wins))
    def per(sd, q0, q1, f):
        rows = qwin(NAT[sd].get(UK, []), q0, q1)
        return f(rows) if rows else float('nan')
    def ratio_rev(rows): return sum(r['revenue'] * 12 for r in rows) / sum(r['gdpY'] for r in rows)
    def ratio_dir(rows): return sum(r['direct'] * 12 for r in rows) / sum(r['gdpY'] for r in rows)
    def ratio_ind(rows): return sum(r['indirect'] * 12 for r in rows) / sum(r['gdpY'] for r in rows)
    def share_ind(rows):
        d = sum(r['direct'] for r in rows); i = sum(r['indirect'] for r in rows)
        return i / (d + i) if d + i > 0 else float('nan')
    def int_over_rev(rows): return sum(r['interest'] for r in rows) / sum(r['revenue'] for r in rows)
    for lab, f, nd in (('歳入÷年GDP', ratio_rev, 4), ('純税収÷年GDP', ratio_dir, 4), ('間接税÷年GDP', ratio_ind, 4), ('間接税の割合（間接税÷（純税収＋間接税））', share_ind, 3), ('利払い÷歳入（四半期の平均の比）', int_over_rev, 3)):
        L.append(f'| {lab} | ' + ' | '.join(cell([per(sd, w[1], w[2], f) for sd in SEEDS if UK in NAT[sd]], nd) for w in wins) + ' |')
    # (287) 直接税（純税収＝英では地租だけ）の割合・歳入÷年所得・土地税の段ほか
    def share_dir(rows):
        d_ = sum(r['direct'] for r in rows); i_ = sum(r['indirect'] for r in rows)
        return d_ / (d_ + i_) if d_ + i_ > 0 else float('nan')
    def inc_year(sd, q):
        t = CLS.get(sd, {}).get(UK)
        if not t: return float('nan')
        return sum(v[f'q{q}']['earn'] * 12 for v in t.values() if f'q{q}' in v and v[f'q{q}']['earn'] == v[f'q{q}']['earn'])
    def ratio_rev_inc(sd, q0, q1):
        rows = qwin(NAT[sd].get(UK, []), q0, q1)
        if not rows: return float('nan')
        return sum(r['revenue'] * 12 for r in rows) / sum(inc_year(sd, r['q']) for r in rows)
    def mean_of(key):
        def f(rows):
            xs = [r[key] for r in rows if r[key] == r[key]]
            return sum(xs) / len(xs) if xs else float('nan')
        return f
    def last_of(key):
        def f(rows):
            return rows[-1][key] if rows else float('nan')
        return f
    for lab, f, nd in (('直接税（純税収）の割合＝直接÷（直接＋間接）', share_dir, 3), ('土地税の段の率 landTaxEndShare（窓の終わり・段1＝0.133・段2＝0.267）', last_of('landTax'), 3),
                       ('土地税の目標 landTaxTargetEndShare（窓の終わり）', last_of('landTarget'), 3), ('専売の段（決定の後・窓の終わり）', last_of('monoEnd'), 1),
                       ('専売の段（決定の前・窓の終わり）', last_of('monoBefore'), 1), ('政体の番号 polityCode（窓の終わり）', last_of('polity'), 0),
                       ('PrivilegeIntact（窓の平均）', mean_of('priv'), 3), ('ParliamentGrip（窓の平均）', mean_of('grip'), 3)):
        L.append(f'| {lab} | ' + ' | '.join(cell([per(sd, w[1], w[2], f) for sd in SEEDS if UK in NAT[sd]], nd) for w in wins) + ' |')
    L.append('| 歳入÷年所得（class_s の Earned の和×12・種の中央。class_s が無い腕は—） | ' + ' | '.join(cell([ratio_rev_inc(sd, w[1], w[2]) for sd in SEEDS if UK in NAT[sd]], 4) for w in wins) + ' |')
    # 国債÷年GDP
    L.append('| 国債÷年GDP（窓の終わり） | ' + ' | '.join(cell([(lambda rows: rows[-1]['debt'] / rows[-1]['gdpY'] if rows else float('nan'))(qwin(NAT[sd].get(UK, []), w[1], w[2])) for sd in SEEDS if UK in NAT[sd]], 3) for w in wins) + ' |')
    L.append('\n（参考）歳入÷年GDP を種ごとに Q1 と Q5〜Q40 で：\n')
    L.append('| 種 | Q1 | Q5〜Q40 | Q1/Q5〜Q40 |')
    L.append('|---|---|---|---|')
    for sd in SEEDS:
        if UK not in NAT[sd]: continue
        a = per(sd, 0, 0, ratio_rev); b = per(sd, 4, 39, ratio_rev)
        L.append(f'| {sd} | {f3(a, 4)} | {f3(b, 4)} | {f3(a / b if b else float("nan"))} |')
    open(os.path.join(OUT, 't3_uk_revenue.md'), 'w', encoding='utf-8').write('\n'.join(L) + '\n')

# ---------------------------------------------------------------- ④ 仏 1783〜1795
def t4():
    L = []
    L.append(f'# t4 {FR} の 1783〜1795 の年ごとの同じ列（種の中央〔最小〜最大〕・{len(FIS)}種）\n')
    L.append('反証役の線：仏 1788 の利払い÷歳入 0.67 で支払い停止（Coefficients.cs:484-487 の出典つきの錨）＝借入の門の位置の記録。年＝暦年（国の World.Year）。'
             '月数は年あたり最大12。〈利払い÷歳入〉は月平均・括弧は種の最小〜最大。\n')
    seeds_ = [sd for sd in FIS if FR in FIS[sd]]
    L.append('| 年 | 利払い÷歳入(月平均) | 利払い÷歳入(年の最大) | 門が閉じた月 | 不足のある月 | うち借りた月 | r＜1 の月 | 借入÷不足 | 不履行の累計 | 国債の金利(年率) | 歳入÷年GDP |')
    L.append('|---|---|---|---|---|---|---|---|---|---|---|')
    for y in range(1783, 1796):
        ss = []; dfl = []; rate = []; revgdp = []
        for sd in seeds_:
            rows = [r for r in FIS[sd][FR] if r['year'] == y]
            s = month_stats(rows)
            if s:
                ss.append(s); dfl.append(rows[-1]['defaults']); rate.append(rows[-1]['rate'])
            nrows = [r for r in NAT[sd].get(FR, []) if r['year'] == y]
            if nrows: revgdp.append(sum(r['revenue'] * 12 for r in nrows) / sum(r['gdpY'] for r in nrows))
        if not ss: continue
        L.append(f'| {y} | ' + ' | '.join([cell([s['ratio_mean'] for s in ss], 2), cell([s['ratio_max'] for s in ss], 2), cell([s['closed'] for s in ss], 0), cell([s['short'] for s in ss], 0),
                                              cell([s['borrowed_months'] for s in ss], 0), cell([s['rbelow'] for s in ss], 0), cell([s['borrow_over_short'] for s in ss], 2), cell(dfl, 0), cell(rate, 3), cell(revgdp, 3)]) + ' |')
    # 1788 を種ごとに
    L.append('\n## 種ごとの 1783〜1795 全体：仏が初めて 利払い÷歳入≧0.55 の月・≧0.67 の月・最初の不履行の年\n')
    L.append('| 種 | 利払い÷歳入≧0.55 が初めて | ≧0.67 が初めて | 最初の不履行の年 | 1783〜1795 の門が閉じた月 | 1788 の利払い÷歳入(月平均) |')
    L.append('|---|---|---|---|---|---|')
    for sd in seeds_:
        rows = [r for r in FIS[sd][FR] if 1783 <= r['year'] <= 1795]
        def first(th):
            for r in rows:
                if r['ratio'] == r['ratio'] and r['ratio'] >= th: return r['year']
            return '—'
        fd = '—'
        for r in FIS[sd][FR]:
            if r['defaults'] == r['defaults'] and r['defaults'] >= 1: fd = r['year']; break
        r88 = [r['ratio'] for r in FIS[sd][FR] if r['year'] == 1788 and r['ratio'] == r['ratio']]
        L.append(f"| {sd} | {first(0.55)} | {first(0.67)} | {fd} | {sum(1 for r in rows if r['gate'] == 0)} | {f3(sum(r88) / len(r88) if r88 else float('nan'))} |")
    open(os.path.join(OUT, 't4_fr_1783_1795.md'), 'w', encoding='utf-8').write('\n'.join(L) + '\n')

# ---------------------------------------------------------------- ⑤ 全 61 国の②の要約（国×20年窓の種の中央）
def t5():
    wins = [(1783 + 20 * i, 1783 + 20 * i + 19) for i in range(9)]
    wins[-1] = (1943, 1950)
    metrics = ['gate_closed_share', 'r_below_share', 'short_share', 'interest_over_revenue', 'borrow_over_short', 'revenue_over_gdp']
    # nation -> window -> metric -> [per-seed values]
    ACC = collections.defaultdict(lambda: collections.defaultdict(lambda: collections.defaultdict(list)))
    initial = None
    for sd in SEEDS:
        names0 = {nat for nat, rows in NAT[sd].items() if any(r['q'] == 0 for r in rows)}
        initial = names0 if initial is None else initial & names0   # 開幕（q0）に居た国＝初期の国。分離独立・建国の国は種ごとに名前が違う＝別に数える
    nfounded = sorted({len([n for n in NAT[sd] if n not in initial]) for sd in SEEDS})
    print('初期の国', len(initial), '・種ごとの建国の数', nfounded)
    for sd in SEEDS:
        for nat, rows in NAT[sd].items():
            if nat not in initial: continue
            for wi, (y0, y1) in enumerate(wins):
                rs = [r for r in rows if y0 <= r['year'] <= y1 and r['gateObs'] == r['gateObs']]
                if len(rs) < 4: continue
                obs = sum(r['gateObs'] for r in rs)
                if not (obs > 0): continue
                months = sum((r['gateObs']) for r in rs)
                a = ACC[nat][wi]
                a['gate_closed_share'].append(sum(r['gateClosed'] for r in rs) / obs)
                a['r_below_share'].append(sum(r['rUnder'] for r in rs) / obs)
                a['short_share'].append(sum(r['short'] for r in rs) / obs)
                rev = sum(r['revenue'] for r in rs)
                a['interest_over_revenue'].append(sum(r['interest'] for r in rs) / rev if rev > 0 else float('nan'))
                sh = sum(r['shortfall'] for r in rs)
                a['borrow_over_short'].append(sum(r['borrowed'] for r in rs) / sh if sh > 0 else float('nan'))
                g = sum(r['gdpY'] for r in rs)
                a['revenue_over_gdp'].append(sum(r['revenue'] * 12 for r in rs) / g if g > 0 else float('nan'))
    # tsv（長い形）
    with open(os.path.join(OUT, 't5_all_nations_gate.tsv'), 'w', encoding='utf-8') as fh:
        fh.write('nation\twindow\tseeds\t' + '\t'.join(metrics) + '\n')
        for nat in sorted(ACC):
            for wi, (y0, y1) in enumerate(wins):
                a = ACC[nat].get(wi)
                if not a: continue
                fh.write(f"{nat}\t{y0}-{y1}\t{len(a['gate_closed_share'])}\t" + '\t'.join(f3(med(a[m]), 4) for m in metrics) + '\n')
    L = []
    L.append(f'# t5 初期の全 {len(initial)} 国の②の要約（国×20年窓の種の中央・分離独立で建った国は含めない＝種ごとの建国の数 {nfounded}）\n')
    L.append('各国・各窓で 種ごとの値（窓の中の月の和どうしの比）を出し、その種の中央を取る。下の表はそれを国の間で並べた分位（窓の中に 4 四半期以上ある国だけ）。'
             '詳細（国×窓）は t5_all_nations_gate.tsv。門が閉じた月の割合＝月初の借入の門が閉じていた月÷判定した月。'
             'r＜1 の割合＝率 r が1未満の月÷判定した月。借入÷不足＝借入の月平均÷払えない額の月平均（不足のある月に限らず窓の和どうし。借入は不足のある月にしか起きない）。\n')
    for m, label, nd in (('gate_closed_share', '門が閉じた月の割合', 3), ('r_below_share', 'r＜1 の月の割合', 3), ('short_share', '払えない額がある月の割合', 3), ('interest_over_revenue', '利払い÷歳入', 3), ('borrow_over_short', '借入÷不足', 3), ('revenue_over_gdp', '歳入÷年GDP', 3)):
        L.append(f'\n## {label}（国の間の p10／中央／p90 と 国の数）\n')
        L.append('| 窓 | 国の数 | p10 | 中央 | p90 | 最大 | ' + ('≧0.5 の国 |' if m in ('gate_closed_share', 'r_below_share', 'short_share') else '|'))
        L.append('|---|---|---|---|---|---|' + ('---|' if m in ('gate_closed_share', 'r_below_share', 'short_share') else ''))
        for wi, (y0, y1) in enumerate(wins):
            vals = sorted(v for v in (med(ACC[nat][wi][m]) for nat in ACC if wi in ACC[nat]) if v == v)
            if not vals: continue
            def pct(p): return vals[min(len(vals) - 1, int(p * (len(vals) - 1) + 0.5))]
            extra = f" {sum(1 for v in vals if v >= 0.5)} |" if m in ('gate_closed_share', 'r_below_share', 'short_share') else ''
            L.append(f'| {y0}〜{y1} | {len(vals)} | {f3(pct(0.1), nd)} | {f3(pct(0.5), nd)} | {f3(pct(0.9), nd)} | {f3(vals[-1], nd)} |{extra}')
    # 門が閉じた割合が大きい国の上位（全期）
    L.append('\n## 門が閉じた月の割合が高い国×窓（種の中央≧0.5）の一覧\n')
    L.append('| 国 | 窓 | 門が閉じた月の割合 | r＜1 の月の割合 | 利払い÷歳入 | 借入÷不足 | 歳入÷年GDP |')
    L.append('|---|---|---|---|---|---|---|')
    rows_ = []
    for nat in ACC:
        for wi, (y0, y1) in enumerate(wins):
            a = ACC[nat].get(wi)
            if a and med(a['gate_closed_share']) >= 0.5: rows_.append((nat, f'{y0}〜{y1}', *[med(a[m]) for m in ('gate_closed_share', 'r_below_share', 'interest_over_revenue', 'borrow_over_short', 'revenue_over_gdp')]))
    for r in sorted(rows_, key=lambda r: (r[1], -r[2])):
        L.append(f'| {r[0]} | {r[1]} | ' + ' | '.join(f3(v) for v in r[2:]) + ' |')
    open(os.path.join(OUT, 't5_all_nations_gate.md'), 'w', encoding='utf-8').write('\n'.join(L) + '\n')

if CLS: t1(); t6()
if FIS: t2(); t4()
t3(); t5()
print('done', OUT)
