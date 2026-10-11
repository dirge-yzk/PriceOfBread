"""(262) お金の流れの3つの異常の出どころ（線R）の表の読み手。
  python an_flow262.py <計器の出力ディレクトリ(ins)> <表の出力ディレクトリ> [自国=イギリス] [仏=フランス] [蘭=ネーデルラント]
  入力＝標準の計器（`INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins）の
        class_s（階級の貯蓄の在庫・所得・支出）・flow_s（★(262) 貯蓄の書き手の台帳）・fiscal_s（月ごとの財政の鎖）・nation_s（全ての国の四半期の財政の鎖）。
  出力＝<表の出力>/ に markdown と tsv：
    t1_residual_decomp.md   ①階級の貯蓄の増減 ΔSavings が〈所得−支出〉(Earned−Paid) で説明できない残差を、書き手の種類で分解（英・仏。大衆／富裕／全階級／使用人）
    t2_routes.md            ①の経路ごとの額（国の所得に対する割合）＝ソースの呼び出しの位置ごと・相手の側つき
    t3_servants.md          ②使用人（階級12）の Paid÷Earned の出どころ（書き手ごと）
    t4_nl_credit.md         ③ネーデルラントの借入の門（利払い÷歳入・歳入の内訳・初期の国債）を英・仏と並べる
    *.tsv                   同じ数字の機械可読版
  計器が自分で申告する限界：
    ・flow_s の額は四半期の月平均（和÷月数）。窓の和は×3（四半期の月数）で戻す。件数は四半期の和。
    ・残差 R＝ΔSavings−(Earned−Paid)。分解 R＝位相の差(settle−(Earned−Paid))＋give＋giveIncome−take＋carry(in−out)＋region(in−out)＋rebase＋未閉じ。
      〈位相の差〉＝settle−(Earned−Paid)。月末の精算(SettleMonth・EconomyStep の中)の後〜MonthClose の前に書かれた所得(income:true・例 十分の一税 Fiscal.cs:224・封建貢租 :174)は Earned には載るが精算には入らない(貯蓄へは直接入る)＝その分だけ負。
      MonthClose の後に書かれる所得(月次の清算 InternationalMarket.cs:125・四半期の末の共有層)は Earned に載らない(次の月の EconomyStep が0へ戻す)＝位相の差には出ず、giveIncome の全額が残差の正の項になる。
    ・未閉じ＝ΔSavings−Σ(全ての書き手)。建国された国の最初の月は新しい世界の構築が POP の貯蓄を直接立てる＝英・仏・蘭の名前（開幕からいる国）では0に近い。
    ・rebase と regionOut／regionIn：州が為替の違う国へ移るとき、換算(Rebase)は州の移動の記帳(RegionMoved)より先に走る＝rebase の額は〈出ていく側の POP〉の換算差で、regionOut は換算後の貯蓄。
      送り手の国で rebase＋regionOut＝−(換算前の貯蓄)・受け手の国の regionIn は換算後。英の rebase が正になる種は為替が1より小さい国へ州を渡した種。
    ・第1の位相の限界（f1s0 の読み手にも当たる）：class_s の earnedPerMonth は月の決算の終わり(MonthClose)で読む＝その後に書かれる所得(月次の清算の受け取り・四半期の末の共有層)は Earned に載らない。載らない所得は貯蓄へ直接入る(giveIncome)＝残差の正の項。
    ・建国された国(例 統領制イギリス)の最初の数か月は台帳が閉じない(原因未特定・(262) の保留)＝表は開幕からいる国(英・仏・蘭)だけ。
    ・種は全て中央値（と最小・最大）。窓は四半期の添字（Q1＝q0）。国の所得＝全階級の Earned（階級の月平均×3か月）の和。"""
import sys, os, math, statistics, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import num as fl, seeds  # noqa: E402

D, OUT = sys.argv[1], sys.argv[2]
UK = sys.argv[3] if len(sys.argv) > 3 else 'イギリス'
FR = sys.argv[4] if len(sys.argv) > 4 else 'フランス'
NL = sys.argv[5] if len(sys.argv) > 5 else 'ネーデルラント'
os.makedirs(OUT, exist_ok=True)
MONTHS = 3
MASS = ('農民', '工場労働者', '職人'); RICH = ('貴族', '資本家', '商人'); SERV = ('使用人',)
KINDS = ['settle', 'give', 'giveIncome', 'take', 'carryIn', 'carryOut', 'regionIn', 'regionOut', 'rebase']


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


def f3(x, nd=3):
    return '—' if x != x else f'{x:.{nd}f}'


def cell(xs, nd=3):
    lo, hi = rng(xs)
    return f'{f3(med(xs), nd)} ({f3(lo, nd)}〜{f3(hi, nd)})'


SEEDS = seeds(D, 'class')
print('種', SEEDS)

# ---------------------------------------------------------------- class_s
CLS = {}    # seed -> nation -> classId -> time -> dict
NAMES = {}  # classId -> className
for sd in SEEDS:
    d = collections.defaultdict(lambda: collections.defaultdict(dict))
    for r in table(os.path.join(D, f'class_s{sd}.tsv')):
        if r['nation'] not in (UK, FR, NL): continue
        c = int(r['classId']); NAMES[c] = r['className']
        d[r['nation']][c][r['time']] = {'size': fl(r['sizeK']), 'sav': fl(r['savingsStock']), 'earn': fl(r['earnedPerMonth']), 'paid': fl(r['paidPerMonth'])}
    CLS[sd] = d
IDS = {k: [c for c, n in NAMES.items() if n in v] for k, v in (('mass', MASS), ('rich', RICH), ('serv', SERV))}
IDS['all'] = sorted(NAMES)
GROUPS = [('大衆(農民・工場労働者・職人)', 'mass'), ('富裕(貴族・資本家・商人)', 'rich'), ('全階級', 'all'), ('使用人', 'serv')]

# ---------------------------------------------------------------- flow_s
# FLOW[seed][nation][q][(cls, kind, file, line, cp)] = (amountPerMonth, count)
FLOW = {}
for sd in SEEDS:
    p = os.path.join(D, f'flow_s{sd}.tsv')
    if not os.path.exists(p): continue
    d = collections.defaultdict(lambda: collections.defaultdict(dict))
    for r in table(p):
        d[r['nation']][int(r['quarter'])][(int(r['classId']), r['kind'], r['sourceFile'], int(r['sourceLine']), r['counterpartySide'])] = (fl(r['amountPerMonth']), fl(r['eventCount']))
    FLOW[sd] = d


def stock(sd, nation, c, q):
    """四半期 q の終わりの在庫（q＝-1 は warm）"""
    return CLS[sd][nation][c].get('warm' if q < 0 else f'q{q}')


def win_vals(sd, nation, cls_ids, q0, q1):
    """窓 q0..q1 の（国の所得 Y・群の ΔSavings・群の Earned−Paid・書き手ごとの和 dict）。無ければ None。"""
    if nation not in CLS[sd] or nation not in FLOW.get(sd, {}): return None
    Y = 0.0
    for c in CLS[sd][nation]:
        for q in range(q0, q1 + 1):
            r = CLS[sd][nation][c].get(f'q{q}')
            if r is None or r['earn'] != r['earn']: return None
            Y += r['earn'] * MONTHS
    dS = EP = 0.0
    for c in cls_ids:
        if c not in CLS[sd][nation]: continue
        a, b = stock(sd, nation, c, q0 - 1), stock(sd, nation, c, q1)
        if a is None or b is None: return None
        dS += b['sav'] - a['sav']
        for q in range(q0, q1 + 1):
            r = CLS[sd][nation][c][f'q{q}']
            EP += (r['earn'] - r['paid']) * MONTHS
    k = collections.defaultdict(float)
    for q in range(q0, q1 + 1):
        for (c, kind, f, ln, cp), (amt, n) in FLOW[sd][nation].get(q, {}).items():
            if c in cls_ids: k[kind] += amt * MONTHS
    return Y, dS, EP, k


def decomp(sd, nation, cls_ids, q0, q1):
    w = win_vals(sd, nation, cls_ids, q0, q1)
    if w is None: return None
    Y, dS, EP, k = w
    if not (Y > 0): return None
    settle = k['settle']
    parts = collections.OrderedDict()
    parts['R 残差 ΔSavings−(Earned−Paid)'] = dS - EP
    parts['位相の差 settle−(Earned−Paid)'] = settle - EP
    parts['give 受け取り(所得に書かない)'] = k['give']
    parts['giveIncome 受け取り(所得にも書く)'] = k['giveIncome']
    parts['take 払い(支出に書かない)'] = k['take']
    parts['carry 運搬(in−out)'] = k['carryIn'] + k['carryOut']   # out は負の額で記帳済み
    parts['region 州の移動(in−out)'] = k['regionIn'] + k['regionOut']
    parts['rebase 国が替わる換算'] = k['rebase']
    closed = settle + k['give'] + k['giveIncome'] + k['take'] + k['carryIn'] + k['carryOut'] + k['regionIn'] + k['regionOut'] + k['rebase']
    parts['未閉じ ΔSavings−Σ書き手'] = dS - closed
    return Y, {a: b / Y for a, b in parts.items()}, dS / Y, EP / Y


WINS = [('Q2〜Q8', 1, 7), ('Q5〜Q40', 4, 39), ('Q41〜Q80', 40, 79), ('Q81〜Q160', 80, 159), ('Q161〜Q240', 160, 239)]


def t1():
    L = [f'# t1 階級の貯蓄の増減の残差の分解（国の所得に対する割合・種の中央〔最小〜最大〕）\n',
         '残差 R＝ΔSavings−(Earned−Paid)。書き手の台帳（flow_s）で R を足し算に分解する。分解の行の和は R に一致（未閉じが0に近いこと）。'
         '正＝その経路が貯蓄を増やした方向。位相の差＝settle−(Earned−Paid)＝精算の後〜MonthClose の前に書かれた所得(十分の一税・封建貢租)は Earned に載るが精算に入らない分（負）。MonthClose の後の所得(月次の清算)は Earned に載らず、giveIncome の全額が残差の正の項。\n']
    tsv = ['nation\twindow\tgroup\tterm\tmedian\tmin\tmax\tseeds']
    for nation in (UK, FR):
        L.append(f'\n## {nation}\n')
        for gname, gk in GROUPS:
            L.append(f'\n### {gname}\n')
            L.append('| 項 | ' + ' | '.join(w[0] for w in WINS) + ' |')
            L.append('|---|' + '---|' * len(WINS))
            terms = None; cols = []
            for wname, q0, q1 in WINS:
                per = [decomp(sd, nation, IDS[gk], q0, q1) for sd in SEEDS]
                per = [x for x in per if x]
                if terms is None and per: terms = list(per[0][1].keys())
                cols.append(per)
            if terms is None: continue
            rows = [('ΔSavings÷国の所得', lambda x: x[2]), ('(Earned−Paid)÷国の所得', lambda x: x[3])] + [(t, (lambda t: lambda x: x[1][t])(t)) for t in terms]
            for lab, f in rows:
                cells = []
                for (wname, q0, q1), per in zip(WINS, cols):
                    xs = [f(x) for x in per]
                    cells.append(cell(xs, 3))
                    lo, hi = rng(xs)
                    tsv.append(f'{nation}\t{wname}\t{gname}\t{lab}\t{med(xs):.6g}\t{lo:.6g}\t{hi:.6g}\t{len(xs)}')
                L.append(f'| {lab} | ' + ' | '.join(cells) + ' |')
    open(os.path.join(OUT, 't1_residual_decomp.md'), 'w', encoding='utf-8').write('\n'.join(L) + '\n')
    open(os.path.join(OUT, 't1_residual_decomp.tsv'), 'w', encoding='utf-8').write('\n'.join(tsv) + '\n')


def route_name(f, ln):
    return ROUTE_NAMES.get((f, ln), f'{f}:{ln}')


try:
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    from route_names import ROUTE_NAMES  # noqa: E402
except Exception:
    ROUTE_NAMES = {}


def route_sums(sd, nation, cls_ids, q0, q1):
    out = collections.defaultdict(float)
    for q in range(q0, q1 + 1):
        for (c, kind, f, ln, cp), (amt, n) in FLOW[sd][nation].get(q, {}).items():
            if c in cls_ids: out[(kind, f, ln, cp)] += amt * MONTHS
    return out


def t2():
    L = ['# t2 書き手の経路ごとの額（国の所得に対する割合・種の中央〔最小〜最大〕）\n',
         '経路＝書き手の種類(kind)＋ソースの呼び出しの位置(ファイル:行)＋相手の側(P＝POP・T＝国庫・B＝国境の出入り・M＝造幣局・F＝不換の発行者・C＝企業の与信・H＝家計の与信・-＝相手なし)。'
         '額は窓の和÷国の所得（正＝貯蓄を増やす方向・take／carryOut／regionOut は負）。|中央|の大きい順に上位。\n']
    tsv = ['nation\twindow\tgroup\tkind\tfile\tline\tname\tcp\tmedian\tmin\tmax\tseeds']
    for nation in (UK, FR):
        for wname, q0, q1 in ((WINS[1]), (WINS[0])):
            for gname, gk in GROUPS[:3]:
                per = []
                for sd in SEEDS:
                    w = win_vals(sd, nation, IDS[gk], q0, q1)
                    if w is None or not (w[0] > 0): continue
                    per.append({k: v / w[0] for k, v in route_sums(sd, nation, IDS[gk], q0, q1).items()})
                if not per: continue
                keys = {k for p in per for k in p}
                stats = {k: [p.get(k, 0.0) for p in per] for k in keys}
                top = sorted(keys, key=lambda k: (-abs(med(stats[k])), k))[:18]
                L.append(f'\n## {nation}・{wname}・{gname}（{len(per)}種）\n')
                L.append('| 順 | 種類 | 経路 | 相手 | 国の所得比 |')
                L.append('|---|---|---|---|---|')
                for i, k in enumerate(top, 1):
                    kind, f, ln, cp = k
                    L.append(f'| {i} | {kind} | {route_name(f, ln)} ({f}:{ln}) | {cp} | {cell(stats[k], 4)} |')
                for k in sorted(keys):
                    lo, hi = rng(stats[k])
                    if abs(med(stats[k])) >= 1e-5 or max(abs(lo), abs(hi)) >= 1e-4:
                        tsv.append(f'{nation}\t{wname}\t{gname}\t{k[0]}\t{k[1]}\t{k[2]}\t{route_name(k[1], k[2])}\t{k[3]}\t{med(stats[k]):.6g}\t{lo:.6g}\t{hi:.6g}\t{len(per)}')
    open(os.path.join(OUT, 't2_routes.md'), 'w', encoding='utf-8').write('\n'.join(L) + '\n')
    open(os.path.join(OUT, 't2_routes.tsv'), 'w', encoding='utf-8').write('\n'.join(tsv) + '\n')


def t3():
    """使用人の Paid÷Earned の出どころ。使用人の貯蓄の在庫は〈支出が所得を上回った分〉を何が支えたか（書き手ごと）。"""
    L = ['# t3 使用人（階級12）の Paid÷Earned の出どころ\n',
         'Paid÷Earned＝窓の支出の和÷所得の和。支出が所得を大きく上回る分は、貯蓄の取り崩しか、所得に書かれない受け取り（give・carryIn・regionIn など）で賄われる。'
         '下の表は使用人の貯蓄の増減を書き手で分けたもの（国の所得でなく〈使用人の Earned〉に対する割合）。\n']
    sids = IDS['serv']
    for nation in (UK, FR):
        L.append(f'\n## {nation}\n')
        L.append('| 量 | ' + ' | '.join(w[0] for w in WINS) + ' |')
        L.append('|---|' + '---|' * len(WINS))
        rows = collections.OrderedDict()
        for wname, q0, q1 in WINS:
            xsd = collections.defaultdict(list)
            for sd in SEEDS:
                w = win_vals(sd, nation, sids, q0, q1)
                if w is None: continue
                Y, dS, EP, k = w
                E = P = 0.0
                for c in sids:
                    for q in range(q0, q1 + 1):
                        r = CLS[sd][nation][c][f'q{q}']; E += r['earn'] * MONTHS; P += r['paid'] * MONTHS
                if not (E > 0): continue
                xsd['Paid÷Earned'].append(P / E); xsd['ΔSavings÷Earned'].append(dS / E)
                xsd['人口(千人・窓の終わり)'].append(stock(sd, nation, sids[0], q1)['size'])
                xsd['Earned÷国の所得'].append(E / Y)
                xsd['貯蓄の在庫÷月収(窓の終わり)'].append(stock(sd, nation, sids[0], q1)['sav'] / max(1e-12, CLS[sd][nation][sids[0]][f'q{q1}']['earn']))
                for kind in ('settle', 'give', 'giveIncome', 'take', 'carryIn', 'carryOut', 'regionIn', 'regionOut', 'rebase'):
                    xsd[f'{kind}÷Earned'].append(k[kind] / E)
                xsd['位相の差÷Earned'].append((k['settle'] - (E - P)) / E)
            for lab, xs in xsd.items(): rows.setdefault(lab, {})[wname] = xs
        for lab, d in rows.items():
            L.append(f'| {lab} | ' + ' | '.join(cell(d.get(w[0], []), 3) for w in WINS) + ' |')
        # 経路
        wname, q0, q1 = WINS[1]
        per = []
        for sd in SEEDS:
            w = win_vals(sd, nation, sids, q0, q1)
            if w is None: continue
            E = sum(CLS[sd][nation][c][f'q{q}']['earn'] * MONTHS for c in sids for q in range(q0, q1 + 1))
            if not (E > 0): continue
            per.append({k: v / E for k, v in route_sums(sd, nation, sids, q0, q1).items()})
        if per:
            keys = {k for p in per for k in p}
            stats = {k: [p.get(k, 0.0) for p in per] for k in keys}
            top = sorted(keys, key=lambda k: (-abs(med(stats[k])), k))[:14]
            L.append(f'\n### {nation}・{wname} の使用人の経路（使用人の Earned に対する割合・{len(per)}種）\n')
            L.append('| 順 | 種類 | 経路 | 相手 | 割合 |')
            L.append('|---|---|---|---|---|')
            for i, k in enumerate(top, 1):
                L.append(f'| {i} | {k[0]} | {route_name(k[1], k[2])} ({k[1]}:{k[2]}) | {k[3]} | {cell(stats[k], 3)} |')
    open(os.path.join(OUT, 't3_servants.md'), 'w', encoding='utf-8').write('\n'.join(L) + '\n')


def t4():
    """③ネーデルラントの借入の門。nation_s（全ての国）と fiscal_s（挙げた国）で、利払い÷歳入・歳入÷年GDP・国債÷年GDP・債務の動きを英・仏と並べる。"""
    NAT = {}
    for sd in seeds(D, 'nation'):
        d = collections.defaultdict(dict)
        for r in table(os.path.join(D, f'nation_s{sd}.tsv')):
            if r['nation'] not in (UK, FR, NL): continue
            d[r['nation']][int(r['quarter'])] = {
                'year': int(r['year']), 'gdpY': fl(r['nominalGdpPerYear']), 'pop': fl(r['populationK']), 'gateObs': fl(r['borrowGateObservedMonths']), 'gateClosed': fl(r['borrowGateClosedMonths']),
                'short': fl(r['shortfallMonths']), 'rUnder': fl(r['payRateBelowOneMonths']), 'interest': fl(r['interestMeanPerMonth']), 'revenue': fl(r['revenueBaseMeanPerMonth']),
                'direct': fl(r['directTaxNetMeanPerMonth']), 'indirect': fl(r['indirectRevenueMeanPerMonth']), 'shortfall': fl(r['shortfallMeanPerMonth']), 'borrowed': fl(r['borrowedMeanPerMonth']), 'debt': fl(r['govDebtStock']),
                'borrowCum': fl(r['ledgerBorrowCumulative']), 'devalueCum': fl(r['ledgerDevalueCumulative'])}
        NAT[sd] = d
    # 年窓（20年＝80四半期）
    wins = [('Q1〜Q8(1783〜84)', 0, 7), ('Q9〜Q40(1785〜92)', 8, 39), ('Q41〜Q80(1793〜1802)', 40, 79), ('Q81〜Q120(1803〜12)', 80, 119), ('Q121〜Q160(1813〜22)', 120, 159), ('Q161〜Q200(1823〜32)', 160, 199), ('Q201〜Q240(1833〜42)', 200, 239), ('Q241〜Q320(1843〜62)', 240, 319)]
    L = ['# t4 ネーデルラントの借入の門（英・仏と並べる・種の中央〔最小〜最大〕）\n',
         '門＝直近の利払い＜MaxInterestShareOfRevenue(0.55)×直近の歳入(予算の基数)・冷却中でない・助走でない（Fiscal.BorrowGateOpen）。利払い＝国債×平均クーポン。歳入の基数＝純税収＋IndirectTaxInBudgetBase×間接税。\n']
    tsv = ['nation\twindow\tquantity\tmedian\tmin\tmax\tseeds']
    for nation in (NL, UK, FR):
        L.append(f'\n## {nation}\n')
        L.append('| 量 | ' + ' | '.join(w[0] for w in wins) + ' |')
        L.append('|---|' + '---|' * len(wins))
        defs = [
            ('利払い÷歳入(月平均の比)', lambda rs: sum(r['interest'] for r in rs) / max(1e-12, sum(r['revenue'] for r in rs)), 3),
            ('歳入の基数÷年GDP', lambda rs: sum(r['revenue'] for r in rs) / len(rs) * 12 / max(1e-12, sum(r['gdpY'] for r in rs) / len(rs)), 4),
            ('利払い÷年GDP', lambda rs: sum(r['interest'] for r in rs) / len(rs) * 12 / max(1e-12, sum(r['gdpY'] for r in rs) / len(rs)), 4),
            ('国債÷年GDP(窓の終わり)', lambda rs: rs[-1]['debt'] / max(1e-12, rs[-1]['gdpY']), 3),
            ('平均クーポン(利払い×12÷国債)', lambda rs: sum(r['interest'] for r in rs) / len(rs) * 12 / max(1e-12, sum(r['debt'] for r in rs) / len(rs)), 4),
            ('間接税の割合(歳入の基数のうち)', lambda rs: sum(r['indirect'] for r in rs) / max(1e-12, sum(r['direct'] + r['indirect'] for r in rs)), 3),
            ('門が閉じた月の割合', lambda rs: sum(r['gateClosed'] for r in rs) / max(1, sum(r['gateObs'] for r in rs)), 3),
            ('払えない額のある月の割合', lambda rs: sum(r['short'] for r in rs) / max(1, 3 * len(rs)), 3),
            ('借入÷払えない額', lambda rs: sum(r['borrowed'] for r in rs) / max(1e-12, sum(r['shortfall'] for r in rs)) if sum(r['shortfall'] for r in rs) > 0 else float('nan'), 3),
            ('r<1 の月の割合', lambda rs: sum(r['rUnder'] for r in rs) / max(1, 3 * len(rs)), 3),
            ('国債の変化÷年GDP(窓の終わり−始め)', lambda rs: (rs[-1]['debt'] - rs[0]['debt']) / max(1e-12, rs[0]['gdpY']), 3),
        ]
        for lab, f, nd in defs:
            cells = []
            for wname, q0, q1 in wins:
                xs = []
                for sd in NAT:
                    rs = [NAT[sd][nation][q] for q in range(q0, q1 + 1) if nation in NAT[sd] and q in NAT[sd][nation]]
                    if not rs: continue
                    try: xs.append(f(rs))
                    except ZeroDivisionError: pass
                cells.append(cell(xs, nd))
                lo, hi = rng(xs)
                tsv.append(f'{nation}\t{wname}\t{lab}\t{med(xs):.6g}\t{lo:.6g}\t{hi:.6g}\t{len(xs)}')
            L.append(f'| {lab} | ' + ' | '.join(cells) + ' |')
    open(os.path.join(OUT, 't4_nl_credit.md'), 'w', encoding='utf-8').write('\n'.join(L) + '\n')
    open(os.path.join(OUT, 't4_nl_credit.tsv'), 'w', encoding='utf-8').write('\n'.join(tsv) + '\n')


def t4b():
    """③の続き：挙げた国の月次の財政の鎖（fiscal_s）から、国債金利の中身（兌換・金本位）と請求の内訳を窓ごとに。"""
    wins = [('1783〜84', 1783, 1784), ('1785〜92', 1785, 1792), ('1793〜1802', 1793, 1802), ('1803〜22', 1803, 1822), ('1823〜42', 1823, 1842), ('1843〜62', 1843, 1862)]
    rows = {}   # (seed, nation) -> list of dict
    for sd in seeds(D, 'fiscal'):
        for r in table(os.path.join(D, f'fiscal_s{sd}.tsv')):
            if r['nation'] not in (NL, UK, FR): continue
            rows.setdefault((sd, r['nation']), []).append({k: (v if k in ('nation', 'borrowBinder') else fl(v)) for k, v in r.items()})
    L = ['# t4b 国債金利の中身と請求の内訳（月次の財政の鎖〔fiscal_s〕・種の中央〔最小〜最大〕）\n',
         '金利の式（Fiscal.Step）＝(基礎0.04＋0.018×国債÷年GDP＋0.5×期待インフレ＋0.02×政策金利)×兌換割引(1−兌換×0.4)×議会割引(1−0.1×議会の統制)。\n']
    rev = lambda rs: max(1e-12, sum(r['revenueBasePerMonth'] for r in rs))
    defs = [
        ('国債金利(年率)', lambda rs: sum(r['govBondRatePerYear'] for r in rs) / len(rs), 4),
        ('既往債の平均クーポン(年率)', lambda rs: sum(r['govAvgCouponPerYear'] for r in rs) / len(rs), 4),
        ('兌換の割合', lambda rs: sum(r['convertibilityShare'] for r in rs) / len(rs), 3),
        ('金本位の月の割合', lambda rs: sum(r['onGold'] for r in rs) / len(rs), 3),
        ('請求の合計÷歳入の基数', lambda rs: sum(r['claimTotalPerMonth'] for r in rs) / rev(rs), 3),
        ('利払い請求÷歳入の基数', lambda rs: sum(r['claimInterestPerMonth'] for r in rs) / rev(rs), 3),
        ('陸軍・海軍の請求÷歳入の基数', lambda rs: sum(r['claimArmyPerMonth'] + r['claimNavyPerMonth'] for r in rs) / rev(rs), 3),
        ('福祉・年金の請求÷歳入の基数', lambda rs: sum(r['claimWelfarePerMonth'] + r['claimPensionPerMonth'] for r in rs) / rev(rs), 3),
        ('行政・インフラの請求÷歳入の基数', lambda rs: sum(r['claimAdminPerMonth'] + r['claimInfraPerMonth'] + r['claimElecInfraPerMonth'] for r in rs) / rev(rs), 3),
        ('国庫(月末)÷歳入の基数', lambda rs: sum(r['treasuryEndStock'] for r in rs) / rev(rs), 2),
        ('不履行の累計(窓の終わり)', lambda rs: rs[-1]['defaultCount'], 1),
        ('支払い停止の月の累計(窓の終わり)', lambda rs: rs[-1]['insolvencyMonths'], 1),
    ]
    for nation in (NL, UK, FR):
        L.append(f'\n## {nation}\n')
        L.append('| 量 | ' + ' | '.join(w[0] for w in wins) + ' |')
        L.append('|---|' + '---|' * len(wins))
        for lab, f, nd in defs:
            cells = []
            for wname, y0, y1 in wins:
                xs = []
                for (sd, nat), rr in rows.items():
                    if nat != nation: continue
                    rs = [r for r in rr if y0 <= r['year'] <= y1]
                    if rs: xs.append(f(rs))
                cells.append(cell(xs, nd))
            L.append(f'| {lab} | ' + ' | '.join(cells) + ' |')
    open(os.path.join(OUT, 't4b_credit_monthly.md'), 'w', encoding='utf-8').write('\n'.join(L) + '\n')


def t7():
    """窃盗(Crime.cs:65)の大きさ：貯蓄のストックが年所得の何倍か・窃盗の年額がストックの何%か・窃盗の年額が年所得の何%か（四半期 q の断面・全階級の合計）。"""
    qs = [4, 20, 39, 79, 159, 239]
    L = ['# t7 窃盗の大きさ（全階級の合計・種の中央〔最小〜最大〕）\n',
         '窃盗の年額＝その四半期の flow_s の Crime.cs:65 の give（月平均）×12。貯蓄＝全階級の貯蓄の在庫（四半期の末）。年所得＝全階級の Earned（月平均）×12。\n']
    for nation in (UK, FR, NL):
        L.append(f'\n## {nation}\n')
        L.append('| 量 | ' + ' | '.join(f'Q{q + 1}' for q in qs) + ' |')
        L.append('|---|' + '---|' * len(qs))
        rows = collections.OrderedDict((k, collections.defaultdict(list)) for k in ('貯蓄の在庫÷年所得', '窃盗の年額÷貯蓄の在庫', '窃盗の年額÷年所得'))
        for sd in SEEDS:
            if nation not in CLS.get(sd, {}) or nation not in FLOW.get(sd, {}): continue
            for q in qs:
                sav = earn = 0.0
                for c in CLS[sd][nation]:
                    r = CLS[sd][nation][c].get(f'q{q}')
                    if r is None or r['earn'] != r['earn']: sav = float('nan'); break
                    sav += r['sav']; earn += r['earn']
                if not (sav == sav) or not (earn > 0): continue
                crime = sum(amt for (c, kind, f, ln, cp), (amt, n) in FLOW[sd][nation].get(q, {}).items() if f == 'Crime.cs' and ln == 65 and kind == 'give')
                rows['貯蓄の在庫÷年所得'][q].append(sav / (earn * 12))
                rows['窃盗の年額÷貯蓄の在庫'][q].append(crime * 12 / sav if sav > 0 else float('nan'))
                rows['窃盗の年額÷年所得'][q].append(crime / earn)
        for lab, d in rows.items():
            L.append(f'| {lab} | ' + ' | '.join(cell(d.get(q, []), 3) for q in qs) + ' |')
    open(os.path.join(OUT, 't7_crime_scale.md'), 'w', encoding='utf-8').write('\n'.join(L) + '\n')


def t5():
    """① 全階級の残差が貨幣の保存の漏れか：全階級の書き手の和を相手の側ごとに足す。P＝同じ国の POP どうし（全階級の和では打ち消し合う＝0）・T＝国庫・B＝国境の出入り・C／H＝銀行の与信・F＝不換の発行者・M＝造幣局。
    国庫と世界の外の相手はそれぞれ台帳を持つ（保存量 D は leakhunt が見る）＝全階級の残差は〈国庫と世界の外との出入り＋州の移動＋換算＋位相の差〉の足し算。"""
    L = ['# t5 全階級の残差の相手の側ごとの分解（国の所得に対する割合・種の中央〔最小〜最大〕）\n',
         '全階級の和では POP どうし(P)の受け取りと払い・運搬は打ち消し合う(0)。残るのは国庫(T)・世界の外(B／M／F／C／H)との出入りと、州の移動・国が替わる換算・位相の差。'
         '世界の外との出入りは各台帳(国境の出入り・鋳造・不換の発行・銀行の与信)が同額を反対側へ記帳する＝保存量 D の側で閉じる(leakhunt)。\n']
    for nation in (UK, FR):
        L.append(f'\n## {nation}\n')
        L.append('| 項 | ' + ' | '.join(w[0] for w in WINS) + ' |')
        L.append('|---|' + '---|' * len(WINS))
        rows = collections.OrderedDict()
        for wname, q0, q1 in WINS:
            for sd in SEEDS:
                w = win_vals(sd, nation, IDS['all'], q0, q1)
                if w is None or not (w[0] > 0): continue
                Y, dS, EP, k = w
                by = collections.defaultdict(float)
                for q in range(q0, q1 + 1):
                    for (c, kind, f, ln, cp), (amt, n) in FLOW[sd][nation].get(q, {}).items():
                        if kind in ('give', 'giveIncome', 'take'): by[(kind, cp)] += amt * MONTHS
                vals = collections.OrderedDict()
                vals['R 残差'] = (dS - EP) / Y
                vals['位相の差'] = (k['settle'] - EP) / Y
                for (kind, cp) in sorted(by): vals[f'{kind} 相手={cp}'] = by[(kind, cp)] / Y
                vals['carry'] = (k['carryIn'] + k['carryOut']) / Y
                vals['region 州の移動'] = (k['regionIn'] + k['regionOut']) / Y
                vals['rebase 換算'] = k['rebase'] / Y
                for lab, v in vals.items(): rows.setdefault(lab, {}).setdefault(wname, []).append(v)
        order = ['R 残差', '位相の差'] + sorted(l for l in rows if l.startswith(('give', 'take'))) + ['carry', 'region 州の移動', 'rebase 換算']
        for lab in order:
            if lab in rows: L.append(f'| {lab} | ' + ' | '.join(cell(rows[lab].get(w[0], []), 3) for w in WINS) + ' |')
    open(os.path.join(OUT, 't5_all_class_by_counterparty.md'), 'w', encoding='utf-8').write('\n'.join(L) + '\n')


def t6():
    """F1 の規則A（需要の穴 H の窓）への答え：大衆の貯蓄の取り崩し H＝−ΔSavings÷国の所得 を、窃盗・清算の受け取り・州の移動を除いて読み直す。
    窃盗(Crime.cs:65)は全 POP の貯蓄×犯罪率を困窮した大衆へ流す純移転（貯蓄のストックに比例）＝大衆の貯蓄を後から押し上げ、富裕から抜く。"""
    L = ['# t6 規則A の窓（需要の穴 H）を窃盗などの純移転を除いて読み直す（国の所得に対する割合・種の中央〔最小〜最大〕）\n',
         'H＝−ΔSavings÷国の所得（大衆）。H(窃盗を除く)＝H＋大衆の窃盗の受け取り−被害(Crime.cs:65 の give＋take)÷国の所得。H(窃盗・清算・州の移動を除く)＝さらに大衆の清算の受け取り(InternationalMarket.cs:125 の giveIncome)と州の移動(regionIn＋regionOut)を除いた物。'
         '−(Earned−Paid)÷国の所得＝市場の取引だけが貯蓄を動かした場合の取り崩し（残差の項を全て除いた物）。AI の規則案(未承認)＝H≧0.15 なら 段5 を 段3 の前。\n']
    wins = [('Q2〜Q8', 1, 7), ('Q4〜Q12', 3, 11), ('Q5〜Q40', 4, 39), ('Q41〜Q80', 40, 79)]
    for nation in (UK, FR):
        L.append(f'\n## {nation}（大衆＝農民・工場労働者・職人）\n')
        L.append('| 量 | ' + ' | '.join(w[0] for w in wins) + ' |')
        L.append('|---|' + '---|' * len(wins))
        cols = collections.OrderedDict()
        for wname, q0, q1 in wins:
            for sd in SEEDS:
                w = win_vals(sd, nation, IDS['mass'], q0, q1)
                if w is None or not (w[0] > 0): continue
                Y, dS, EP, k = w
                rs = route_sums(sd, nation, IDS['mass'], q0, q1)
                crime = sum(v for (kind, f, ln, cp), v in rs.items() if f == 'Crime.cs' and ln == 65)
                clear = sum(v for (kind, f, ln, cp), v in rs.items() if f == 'InternationalMarket.cs' and ln == 125 and kind == 'giveIncome')
                reg = k['regionIn'] + k['regionOut']
                vals = collections.OrderedDict()
                vals['H＝−ΔSavings÷Y'] = -dS / Y
                vals['窃盗の純受け取り÷Y'] = crime / Y
                vals['H(窃盗を除く)'] = -(dS - crime) / Y
                vals['清算の受け取り(giveIncome)÷Y'] = clear / Y
                vals['州の移動÷Y'] = reg / Y
                vals['H(窃盗・清算・州の移動を除く)'] = -(dS - crime - clear - reg) / Y
                vals['−(Earned−Paid)÷Y'] = -EP / Y
                for lab, v in vals.items(): cols.setdefault(lab, {}).setdefault(wname, []).append(v)
        for lab, d in cols.items():
            L.append(f'| {lab} | ' + ' | '.join(cell(d.get(w[0], []), 3) for w in wins) + ' |')
    open(os.path.join(OUT, 't6_rule_a_window.md'), 'w', encoding='utf-8').write('\n'.join(L) + '\n')


if __name__ == '__main__':
    which = sys.argv[6] if len(sys.argv) > 6 else 'all'
    if which in ('all', 't1'): t1()
    if which in ('all', 't2'): t2()
    if which in ('all', 't3'): t3()
    if which in ('all', 't4'): t4(); t4b()
    if which in ('all', 't5'): t5()
    if which in ('all', 't6'): t6()
    if which in ('all', 't7'): t7()
    print('done', OUT)
