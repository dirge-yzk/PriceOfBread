# ★(377) 開幕の貯蓄の置き直しの機構（OpeningSavingsRebaseMode・SavingsYearsByClass）は (375) で削除した＝再走は削除の前の木で
"""(319) 流れF 財政の鎖を1本の腕で：段0（必要性の確認・読むだけ）の読み手。
  python an_fx319.py A=<ins> [B=<ins>] [Q=8] [国=イギリス]
  入力＝`INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins
        （nation_s・class_s・flow_s・spend_s）。A＝基線 r314 の木（置き直しなし）・B＝置き直しだけ（OpeningSavingsRebaseMode=1・(305) の腕 B と同じ）。
  出す物（英・Q1〜Q<Q>・種の中央）：
    T0 財政の鎖の頭（歳入・直接税・間接税等・利払い・利払い÷歳入・払える率 r・専売の段・逼迫・人頭税）
    T1 階級ごとの Earned・Paid・Paid÷Earned（大衆＝農民・工場労働者・職人／富裕＝貴族・資本家・商人／使用人）
    T2 大衆の Paid を何が支えているか（Paid−Earned の不足を、貯蓄の取り崩し・窃盗の純受け取り・その他に分ける）
    T3 間接税の基数の静的な計算（spend_s の専売の取り分の階級×財）：Paid≒Earned になったら大衆の専売の取り分はいくらか
    T4 AI が専売の段を動かす速さ（最初の r＜1 の四半期・最初の B＞限界 の四半期・その間隔・段の道筋）
  限界の自己申告：
    ・nation_s の月平均の列は四半期の平均。種の中央は列ごとに取るので、比は中央どうしの比（種ごとの比の中央ではない）。
    ・T2 の分解は class_s の savingsStock の四半期末の差（÷3か月）と flow_s の窃盗（Crime.cs の give＋take）。その他＝不足−取り崩し−窃盗＝位相の差と移動・給付の和（名指ししない）。
    ・T3 の spend_s は窓の区切り（Q1〜Q4・Q5〜Q8）の月平均＝四半期ごとではない。静的な計算は〈大衆の各階級の専売の取り分を Earned÷Paid（その窓の比）で比例して縮める〉だけ
      （財の組成が変わる効果〔予算が尽きると最後の優先度の財から切る〕は入らない＝B の実測との差がそのまま組成の効果の見積もり）。"""
import sys, os, statistics, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import num as fl, seeds  # noqa: E402

ARGS = collections.OrderedDict()
QN = 8
NAT = 'イギリス'
for a in sys.argv[1:]:
    k, v = a.split('=', 1)
    if k == 'Q': QN = int(v)
    elif k == '国': NAT = v
    else: ARGS[k] = v
MASS = ('農民', '工場労働者', '職人'); RICH = ('貴族', '資本家', '商人'); SERV = ('使用人',)


def table(path):
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            yield dict(zip(head, ln.rstrip('\n').split('\t')))


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def f(x, nd=0):
    return 'nan' if x != x else f'{x:.{nd}f}'


def load_nation(d):
    out = {}
    for sd in seeds(d, 'nation'):
        for r in table(os.path.join(d, f'nation_s{sd}.tsv')):
            if r['nation'] == NAT and int(r['quarter']) < QN:
                out.setdefault(sd, {})[int(r['quarter'])] = r
    return out


def load_class(d):
    """{seed: {quarter: {className: (earned, paid, savingsStock, sizeK)}}}（time が q<番号> の行だけ）"""
    out = {}
    for sd in seeds(d, 'class'):
        for r in table(os.path.join(d, f'class_s{sd}.tsv')):
            if r['nation'] != NAT or not r['time'].startswith('q'):
                continue
            q = int(r['time'][1:])
            if q < QN:
                out.setdefault(sd, {}).setdefault(q, {})[r['className']] = (fl(r['earnedPerMonth']), fl(r['paidPerMonth']), fl(r['savingsStock']), fl(r['sizeK']))
    return out


def load_flow(d):
    """{seed: {quarter: {(className, kind, file): amountPerMonth の和}}}"""
    out = {}
    for sd in seeds(d, 'flow'):
        for r in table(os.path.join(d, f'flow_s{sd}.tsv')):
            if r['nation'] != NAT:
                continue
            q = int(r['quarter'])
            if q >= QN:
                continue
            k = (r['classId'], r['kind'], r['sourceFile'])
            e = out.setdefault(sd, {}).setdefault(q, {})
            e[k] = e.get(k, 0.0) + fl(r['amountPerMonth'])
    return out


CLASSID = {'貴族': '0', '農民': '1', '工場労働者': '2', '資本家': '3', '技師': '4', '兵士': '5', '聖職者': '6', '教師': '7', '官僚': '8', '医者': '9', '商人': '10', '職人': '11', '使用人': '12', '研究者': '13'}


def grp_sum(cl, names, i):
    return sum(cl[n][i] for n in names if n in cl and cl[n][i] == cl[n][i])


NS = {a: load_nation(d) for a, d in ARGS.items()}
CL = {a: load_class(d) for a, d in ARGS.items()}
FL = {a: load_flow(d) for a, d in ARGS.items()}
SD = {a: sorted(set(NS[a]) & set(CL[a])) for a in ARGS}
print(f'# (319) 段0 英の Q1〜Q{QN}（種の中央・A の種 {SD.get("A")}・B の種 {SD.get("B")}）\n')


def row(lab, fn, nd, sdl, q):
    return med([fn(s, q) for s in sdl])


# ---------------------------------------------------------------- T0
for a in ARGS:
    sl = SD[a]
    print(f'## T0 財政の鎖の頭（{a}）\n| 量 | ' + ' | '.join(f'Q{q + 1}' for q in range(QN)) + ' |\n|---|' + '---|' * QN)
    def N(c):
        return lambda s, q: fl(NS[a][s][q][c])
    for lab, c, nd in (('歳入（月・予算の基数）', 'revenueBaseMeanPerMonth', 0), ('直接税', 'directTaxNetMeanPerMonth', 0), ('間接税等（関税＋専売）', 'indirectRevenueMeanPerMonth', 0),
                       ('利払い（月）', 'interestMeanPerMonth', 0), ('借入（月）', 'borrowedMeanPerMonth', 0), ('払えない月の数', 'shortfallMonths', 1), ('門の閉じた月の数', 'borrowGateClosedMonths', 1),
                       ('r＜1 の月の数', 'payRateBelowOneMonths', 1), ('払える率 r（月平均）', 'payRateMeanShare', 3), ('専売の段（決断の前）', 'monopolyTierBeforeDecisionStage', 1),
                       ('専売の段（決断の目標）', 'monopolyTierTargetDecisionStage', 1), ('専売の段（四半期末）', 'monopolyTierEndStage', 1), ('逼迫（決断の時点）', 'fiscalStressAtDecisionIndex', 3),
                       ('人頭税（四半期末）', 'pollTaxEndShare', 3)):
        print(f'| {lab} | ' + ' | '.join(f(row(lab, N(c), nd, sl, q), nd) for q in range(QN)) + ' |')
    print('| 利払い÷歳入（種の中央） | ' + ' | '.join(f(med([fl(NS[a][s][q]['interestMeanPerMonth']) / fl(NS[a][s][q]['revenueBaseMeanPerMonth']) if fl(NS[a][s][q]['revenueBaseMeanPerMonth']) > 0 else float('nan') for s in sl]), 3) for q in range(QN)) + ' |')
    print('| 間接税÷歳入 | ' + ' | '.join(f(med([fl(NS[a][s][q]['indirectRevenueMeanPerMonth']) / fl(NS[a][s][q]['revenueBaseMeanPerMonth']) if fl(NS[a][s][q]['revenueBaseMeanPerMonth']) > 0 else float('nan') for s in sl]), 3) for q in range(QN)) + ' |\n')
if len(ARGS) == 2 and 'A' in ARGS and 'B' in ARGS:
    sl = sorted(set(SD['A']) & set(SD['B']))
    print('## T0′ B÷A（種ごとの比の中央）\n| 量 | ' + ' | '.join(f'Q{q + 1}' for q in range(QN)) + ' |\n|---|' + '---|' * QN)
    for lab, c in (('歳入', 'revenueBaseMeanPerMonth'), ('直接税', 'directTaxNetMeanPerMonth'), ('間接税等', 'indirectRevenueMeanPerMonth'), ('利払い', 'interestMeanPerMonth')):
        print(f'| {lab} | ' + ' | '.join(f(med([fl(NS['B'][s][q][c]) / fl(NS['A'][s][q][c]) if fl(NS['A'][s][q][c]) else float('nan') for s in sl]), 3) for q in range(QN)) + ' |')
    print()

# ---------------------------------------------------------------- T1
for a in ARGS:
    sl = SD[a]
    print(f'## T1 階級ごとの Earned・Paid（{a}・月）\n| 階級群 | 量 | ' + ' | '.join(f'Q{q + 1}' for q in range(QN)) + ' |\n|---|---|' + '---|' * QN)
    for gl, names in (('大衆', MASS), ('富裕', RICH), ('使用人', SERV)):
        E = lambda s, q: grp_sum(CL[a][s][q], names, 0)
        P = lambda s, q: grp_sum(CL[a][s][q], names, 1)
        print(f'| {gl} | Earned | ' + ' | '.join(f(med([E(s, q) for s in sl])) for q in range(QN)) + ' |')
        print(f'| {gl} | Paid | ' + ' | '.join(f(med([P(s, q) for s in sl])) for q in range(QN)) + ' |')
        print(f'| {gl} | Paid÷Earned | ' + ' | '.join(f(med([P(s, q) / E(s, q) if E(s, q) > 0 else float('nan') for s in sl]), 3) for q in range(QN)) + ' |')
    print('| 全階級 | Earned | ' + ' | '.join(f(med([sum(v[0] for v in CL[a][s][q].values() if v[0] == v[0]) for s in sl])) for q in range(QN)) + ' |')
    print('| 大衆 | 貯蓄÷月収（月数） | ' + ' | '.join(f(med([grp_sum(CL[a][s][q], MASS, 2) / grp_sum(CL[a][s][q], MASS, 0) if grp_sum(CL[a][s][q], MASS, 0) > 0 else float('nan') for s in sl]), 1) for q in range(QN)) + ' |\n')

# ---------------------------------------------------------------- T2
for a in ARGS:
    sl = SD[a]
    print(f'## T2 大衆の Paid を何が支えているか（{a}・月・Q1 の取り崩しは q0 の終わりと開幕の貯蓄の差ではなく q0→q1 から）\n| 量 | ' + ' | '.join(f'Q{q + 1}' for q in range(QN)) + ' |\n|---|' + '---|' * QN)
    def mass(s, q, i): return grp_sum(CL[a][s][q], MASS, i)
    def theft(s, q):
        t = 0.0
        for n in MASS:
            for kind in ('give', 'take'):
                t += FL[a].get(s, {}).get(q, {}).get((CLASSID[n], kind, 'Crime.cs'), 0.0)
        return t
    def draw(s, q):
        if q == 0: return float('nan')
        return -(mass(s, q, 2) - mass(s, q - 1, 2)) / 3.0
    for lab, fn, nd in (('Paid', lambda s, q: mass(s, q, 1), 0), ('Earned', lambda s, q: mass(s, q, 0), 0), ('不足 Paid−Earned', lambda s, q: mass(s, q, 1) - mass(s, q, 0), 0),
                        ('貯蓄の取り崩し', draw, 0), ('窃盗の純受け取り', theft, 0)):
        print(f'| {lab} | ' + ' | '.join(f(med([fn(s, q) for s in sl]), nd) for q in range(QN)) + ' |')
    print('| 不足÷Paid | ' + ' | '.join(f(med([(mass(s, q, 1) - mass(s, q, 0)) / mass(s, q, 1) for s in sl]), 3) for q in range(QN)) + ' |')
    print('| 取り崩し÷Paid | ' + ' | '.join(f(med([draw(s, q) / mass(s, q, 1) for s in sl]), 3) for q in range(QN)) + ' |')
    print('| 窃盗÷Paid | ' + ' | '.join(f(med([theft(s, q) / mass(s, q, 1) for s in sl]), 3) for q in range(QN)) + ' |\n')

# ---------------------------------------------------------------- T3
def load_spend(d):
    out = {}
    for sd in seeds(d, 'spend'):
        for r in table(os.path.join(d, f'spend_s{sd}.tsv')):
            if r['nation'] != NAT or int(r['bucket']) > 1:
                continue
            out.setdefault(sd, {}).setdefault(int(r['bucket']), []).append(r)
    return out


SP = {a: load_spend(d) for a, d in ARGS.items()}
if any(SP[a] for a in SP):
    print('## T3 間接税の基数の静的な計算（spend_s の専売の取り分 stateTakePerMonth・窓＝Q1〜Q4 と Q5〜Q8 の月平均）')
    for a in ARGS:
        if not SP[a]: continue
        sl = sorted(SP[a])
        print(f'\n### {a}\n| 窓 | 量 | 値 |\n|---|---|---|')
        for b, wl in ((0, 'Q1〜Q4'), (1, 'Q5〜Q8')):
            def tk(s, names):
                t = 0.0
                for r in SP[a][s].get(b, []):
                    if r['kind'] == 'good' and r['className'] in names:
                        v = fl(r['stateTakePerMonth']); t += v if v == v else 0.0
                return t
            def cls(s, names, col):
                t = 0.0
                for r in SP[a][s].get(b, []):
                    if r['kind'] == 'class' and r['className'] in names:
                        v = fl(r[col]); t += v if v == v else 0.0
                return t
            tot = lambda s: sum(fl(r['stateTakePerMonth']) for r in SP[a][s].get(b, []) if r['kind'] == 'good' and fl(r['stateTakePerMonth']) == fl(r['stateTakePerMonth']))
            print(f'| {wl} | 専売の取り分の和（全階級） | {f(med([tot(s) for s in sl]))} |')
            print(f'| {wl} | うち大衆 | {f(med([tk(s, MASS) for s in sl]))} |')
            print(f'| {wl} | うち富裕 | {f(med([tk(s, RICH) for s in sl]))} |')
            print(f'| {wl} | うち使用人 | {f(med([tk(s, SERV) for s in sl]))} |')
            print(f'| {wl} | 大衆の Paid÷Earned（階級ごとの和） | {f(med([cls(s, MASS, "payPerMonth") / cls(s, MASS, "receiptPerMonth") if cls(s, MASS, "receiptPerMonth") > 0 else float("nan") for s in sl]), 3)} |')
            # 静的：大衆の各階級の取り分を Earned÷Paid で縮める
            def static(s):
                t_all = tot(s); red = 0.0
                for n in MASS:
                    p = cls(s, (n,), 'payPerMonth'); e = cls(s, (n,), 'receiptPerMonth')
                    if p > 0 and e > 0 and p > e:
                        red += tk(s, (n,)) * (1.0 - e / p)
                return t_all - red
            print(f'| {wl} | ★静的：大衆が Paid＝Earned まで縮んだときの専売の取り分の和 | {f(med([static(s) for s in sl]))} |')
            print(f'| {wl} | 静的÷今 | {f(med([static(s) / tot(s) if tot(s) > 0 else float("nan") for s in sl]), 3)} |')

# ---------------------------------------------------------------- T4
print('\n## T4 AI が専売の段を動かす速さ（各腕・種ごと）\n')
LIMIT = 0.67
for a in ARGS:
    print(f'### {a}\n| 種 | 最初の B＞{LIMIT} の四半期 | 最初の r＜1 の四半期 | 間隔（四半期） | 段の道筋（四半期末・Q1〜Q{QN}） |\n|---|---|---|---|---|')
    lags = []
    for s in SD[a]:
        qb = qr = None
        for q in range(QN):
            r = NS[a][s][q]
            rev, itr = fl(r['revenueBaseMeanPerMonth']), fl(r['interestMeanPerMonth'])
            if qb is None and rev > 0 and itr / rev > LIMIT: qb = q + 1
            if qr is None and fl(r['payRateBelowOneMonths']) > 0: qr = q + 1
        path = ' '.join(f(fl(NS[a][s][q]['monopolyTierEndStage'])) for q in range(QN))
        lag = (qr - qb) if (qb and qr) else None
        if lag is not None: lags.append(lag)
        print(f'| {s} | {qb} | {qr} | {lag} | {path} |')
    print(f'\n間隔の中央 {statistics.median(lags) if lags else "—"}（r＜1 の出た種 {len(lags)}/{len(SD[a])}）\n')
