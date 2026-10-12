"""(286) 線Q：作者の (1) 投資 (2) 使用人 (3) 奢侈品 の設計に要る数字の読み手。
  python an_q286.py <計器の出力ディレクトリ(ins)> <表の出力ディレクトリ>
  入力＝`INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins
        spend_s（窓の区切りごとの支払いの種類×階級）・svc_s（家事奉仕の市場）・class_s／flow_s／nation_s／fiscal_s。
  窓＝Q1〜Q8（q0..7）・Q5〜Q40（q4..39）・Q41〜Q160（q40..159）。種は中央値（最小〜最大）。
  財は名前で引く（instr.good_id）。数字の財の番号は書かない。
  計器の限界の自己申告：①spend_s の階級ごとの支払いの種類の和＝階級の支出（kind=class）に閉じることは番人が見張る②flow_s は四半期の月平均（挙げた国だけ）
    ③人数は四半期の末の値の月数平均④窓 Q41〜Q160 の「区切り3」は q40..159 ちょうど。Q1〜Q8＝区切り0+1・Q5〜Q40＝区切り1+2（月数で重みづけ）。"""
import sys, os, statistics, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'flow262'))
from instr import num as fl, seeds, good_id  # noqa: E402
from route_names import ROUTE_NAMES  # noqa: E402

D, OUT = sys.argv[1], sys.argv[2]
os.makedirs(OUT, exist_ok=True)
SEEDS = seeds(D, 'nation')
WIN = {'Q1-8': (0, 7, (0, 1)), 'Q5-40': (4, 39, (1, 2)), 'Q41-160': (40, 159, (3,))}
NOBLE, CAP, MERCH, SERV, PEAS, WORK = 0, 3, 10, 12, 1, 2
RICH = (NOBLE, CAP, MERCH)
CLASSNAME = {0: '貴族', 1: '農民', 2: '工場労働者', 3: '資本家', 10: '商人', 12: '使用人'}
NATIONS = ['イギリス', 'フランス', 'ネーデルラント']


def table(path):
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            yield dict(zip(head, ln.rstrip('\n').split('\t')))


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def cell(xs, nd=1, pct=False):
    xs = [x for x in xs if x == x]
    if not xs:
        return '—'
    k = 100.0 if pct else 1.0
    return f'{med(xs)*k:.{nd}f} ({min(xs)*k:.{nd}f}〜{max(xs)*k:.{nd}f})'


# ---------------------------------------------------------------- 読み込み
def load_spend(sd):
    """spend_s → {(uid, nation, bucket): {'picked':0/1,'rows':[(classId,kind,slotId,slotName,tier,months,pay,qty,rec)]}}"""
    out = collections.defaultdict(lambda: {'picked': 0, 'rows': []})
    p = os.path.join(D, f'spend_s{sd}.tsv')
    if not os.path.exists(p):
        return out
    for r in table(p):
        k = (r['nationUid'], r['nation'], int(r['bucket']))
        e = out[k]
        e['picked'] = int(r['pickedFlag'])
        e['rows'].append((int(r['classId']), r['kind'], int(r['slotId']), r['slotName'], r['tier'], float(r['monthsCount']), fl(r['payPerMonth']), fl(r['qtyPerMonth']), fl(r['receiptPerMonth'])))
    return out


def spend_window(S, nation, win, cls_set, pick=lambda kind, slot, name, tier: True, field=6):
    """窓の平均（月あたり）：階級 cls_set の合計。field＝6 pay・7 qty・8 rec。区切りは月数で重みづけ。国名が複数の個体を持つ場合は挙げた個体（pickedFlag=1）だけ。
    戻り＝{(kind, slotName, tier): value}。階級の行（kind=class）は pay＝支出・rec＝所得・qty＝人数。"""
    buckets = WIN[win][2]
    acc = collections.defaultdict(float); mo = 0.0
    uids = {k[0] for k in S if k[1] == nation and S[k]['picked']}
    for uid in uids:
        for b in buckets:
            e = S.get((uid, nation, b))
            if e is None:
                continue
            m = next((r[5] for r in e['rows'] if r[1] == 'class'), 0.0)
            mo += m
            for (c, kind, slot, name, tier, months, pay, qty, rec) in e['rows']:
                if c not in cls_set or not pick(kind, slot, name, tier):
                    continue
                acc[(kind, name, tier)] += (pay, qty, rec)[field - 6] * months if (pay, qty, rec)[field - 6] == (pay, qty, rec)[field - 6] else 0.0
    # 階級の数だけ月数が重複して数えられているので階級ごとの月数で割り直さず、行ごとの months を分母にする
    return acc, mo


def mean_rows(S, nation, win, cls_set):
    """窓の月あたりの平均を {(kind,name,tier): (pay, qty, rec)} で返す。分母＝区切りの月数（階級の行1本ぶん）の合計。"""
    buckets = WIN[win][2]
    tot = collections.defaultdict(lambda: [0.0, 0.0, 0.0]); mo = 0.0
    uids = {k[0] for k in S if k[1] == nation and S[k]['picked']}
    for uid in uids:
        for b in buckets:
            e = S.get((uid, nation, b))
            if e is None:
                continue
            ms = [r[5] for r in e['rows'] if r[1] == 'class']
            if not ms:
                continue
            m = ms[0]; mo += m
            for (c, kind, slot, name, tier, months, pay, qty, rec) in e['rows']:
                if c not in cls_set:
                    continue
                t = tot[(kind, name, tier)]
                for i, v in enumerate((pay, qty, rec)):
                    if v == v:
                        t[i] += v * months
    return ({k: [x / mo for x in v] for k, v in tot.items()} if mo > 0 else {}), mo


def class_rows(sd, nation):
    """class_s（挙げた国）→ {(time, classId): (size, savings, earned, paid)}"""
    out = {}
    for r in table(os.path.join(D, f'class_s{sd}.tsv')):
        if r['nation'] != nation:
            continue
        out[(r['time'], int(r['classId']))] = (fl(r['sizeK']), fl(r['savingsStock']), fl(r['earnedPerMonth']), fl(r['paidPerMonth']))
    return out


def flow_rows(sd, nation):
    """flow_s → {(quarter, classId, kind, route, side): amountPerMonth}"""
    out = collections.defaultdict(float)
    p = os.path.join(D, f'flow_s{sd}.tsv')
    for r in table(p):
        if r['nation'] != nation:
            continue
        f = os.path.basename(r['sourceFile']); ln = int(r['sourceLine'])
        route = ROUTE_NAMES.get((f, ln), f'{f}:{ln}')
        out[(int(r['quarter']), int(r['classId']), r['kind'], route, r['counterpartySide'])] += fl(r['amountPerMonth'])
    return out


def nation_rows(sd):
    d = collections.defaultdict(dict)
    for r in table(os.path.join(D, f'nation_s{sd}.tsv')):
        d[(r['nation'], int(r['quarter']))] = r
    return d


def qrange(win):
    a, b, _ = WIN[win]
    return a, b


def pct(x, d=1):
    return '—' if x != x else f'{x*100:.{d}f}%'


# ---------------------------------------------------------------- T1 富裕層のお金の行き先（経路ごと・年額÷年所得）
def t1_routes():
    md = ['# T1 富裕層（貴族・資本家・商人）のお金の行き先（経路ごと・各階級の所得 Earned を100%とする）', '',
          '窓の月平均÷窓の月平均の所得。種の中央（最小〜最大）。支払いの種類＝spend_s（EconomyStep の支払いの位置）・移転＝flow_s（World.Transfer の呼び手の位置＝経路名）。',
          '残差＝所得−支払い−移転の払い＋移転の受取−Δ貯蓄（人と動くお金・位相・州の移動などの未説明）。', '']
    for nation in NATIONS:
        for cls_label, cls_set in (('3階級の合計', RICH), ('貴族', (NOBLE,)), ('資本家', (CAP,)), ('商人', (MERCH,))):
            for win in WIN:
                rowsets = collections.defaultdict(list); base = []
                for sd in SEEDS:
                    S = SPEND[sd]
                    mr, mo = mean_rows(S, nation, win, set(cls_set))
                    if mo <= 0:
                        continue
                    earn = sum(v[2] for (k, n, t), v in mr.items() if k == 'class')
                    if not earn > 0:
                        continue
                    base.append(earn)
                    paid_class = sum(v[0] for (k, n, t), v in mr.items() if k == 'class')
                    items = collections.defaultdict(float)
                    for (kind, name, tier), v in mr.items():
                        if kind == 'good':
                            if name == '家事奉仕':
                                items['支出：家事奉仕（使用人への支払い）'] += v[0]
                            elif tier == 'Staple':
                                items['支出：主食'] += v[0]
                            elif tier == 'Daily':
                                items['支出：日用品'] += v[0]
                            elif tier == 'Luxury':
                                items['支出：奢侈品'] += v[0]
                            elif tier == 'Service':
                                items['支出：役務（教育・医療ほか）'] += v[0]
                            else:
                                items[f'支出：{tier}の財'] += v[0]
                        elif kind == 'tier':
                            items[f'支出：{tier}（合算）'] += v[0]
                        elif kind == 'extra':
                            items[f'投資：{name}'] += v[0]
                    items['(支出の合計・階級の Paid)'] = paid_class
                    # 移転（flow_s）
                    a, b = qrange(win)
                    nq = 0
                    fr = FLOW[sd].get(nation, {})
                    qs = sorted({q for (q, c, k, r, s) in fr if a <= q <= b})
                    nq = b - a + 1
                    taken = collections.defaultdict(float); given = collections.defaultdict(float); other = collections.defaultdict(float)
                    for (q, c, k, route, side), v in fr.items():
                        if q < a or q > b or c not in cls_set:
                            continue
                        if k == 'take':
                            taken[route] += -v / nq          # take は負の額（貯蓄からの払い）→ 払いは正で出す
                        elif k in ('give', 'giveIncome'):
                            given[route + ('〔所得に書く〕' if k == 'giveIncome' else '')] += v / nq
                        elif k in ('carryIn', 'carryOut', 'regionIn', 'regionOut', 'rebase', 'settle'):
                            other[k] += v / nq
                    for route, v in taken.items():
                        items['移転の払い：' + route] += v
                    for route, v in given.items():
                        items['移転の受取：' + route] += v
                    for k, v in other.items():
                        items['台帳：' + k] += v
                    # Δ貯蓄
                    cr = CLASS[sd].get(nation, {})
                    t0 = 'warm' if a == 0 else f'q{a-1}'; t1 = f'q{b}'
                    dsav = 0.0; ok = True
                    for c in cls_set:
                        s0 = cr.get((t0, c)); s1 = cr.get((t1, c))
                        if s0 is None or s1 is None:
                            ok = False; break
                        dsav += (s1[1] - s0[1]) / (3.0 * nq)
                    items['Δ貯蓄'] = dsav if ok else float('nan')
                    tk = sum(v for k, v in items.items() if k.startswith('移転の払い'))
                    gv = sum(v for k, v in items.items() if k.startswith('移転の受取'))
                    led = sum(v for k, v in items.items() if k.startswith('台帳：') and not k.endswith('settle'))
                    stl = items.get('台帳：settle', 0.0)
                    # 位相＝settle −(Earned − Paid)・未閉じ＝ΔSav − (settle + give + giveIncome − 払い + carry + region + rebase)
                    items['位相の差（settle −(所得−支出)）'] = stl - (earn - paid_class)
                    items['未閉じ（Δ貯蓄 − 台帳の和）'] = (dsav - (stl + gv - tk + led)) if ok else float('nan')
                    items['所得（月あたり）'] = earn
                    for k, v in items.items():
                        rowsets[k].append(v / earn if k != '所得（月あたり）' else v)
                if not base:
                    continue
                md.append(f'## {nation}・{cls_label}・{win}（所得 月あたり {cell(rowsets["所得（月あたり）"], 1)}）')
                md.append('| 項目 | 所得に対する割合 |'); md.append('|---|---|')
                order = sorted([k for k in rowsets if k != '所得（月あたり）'], key=lambda k: -abs(med(rowsets[k])) if med(rowsets[k]) == med(rowsets[k]) else 0)
                for k in order:
                    m = med(rowsets[k])
                    if m == m and abs(m) < 0.0005 and not k.startswith(('(', 'Δ', '残差')):
                        continue
                    md.append(f'| {k} | {cell(rowsets[k], 1, pct=True)}% |')
                md.append('')
    open(os.path.join(OUT, 't1_rich_routes.md'), 'w', encoding='utf-8').write('\n'.join(md))


# ---------------------------------------------------------------- T2 建設の門・T3 国債
def t2_construction():
    md = ['# T2 工場の建設：実際の額を決めているのは何か（資本家・州×月の判定の集計）', '',
          '建設 = min(採算の門が望む束, 投資枠の束)。採算の門＝限界収益（IndProductivityGain/(1+工業化/IndProductivityScale)²×技術）−資本コスト（IndCapitalCost＋クラウディング−株の過熱）。',
          '投資枠＝IndInvestmentFrac(0.05)×資本家の貯蓄（州ごと・毎月）。金額は国の通貨・名目GDPで割った年額比。', '']
    for nation in NATIONS + ['ポルトガル']:
        md.append(f'## {nation}')
        md.append('| 窓 | 判定の州×月（月あたり） | 採算の門が開いた割合 | うち投資枠が切った割合 | 限界収益の平均 | 資本コストの平均 | 投資枠（年額÷名目GDP） | 採算の門が望む額（同） | 実際の建設費（同） | 線路・機関車（同） | 資本家の貯蓄÷年所得 |')
        md.append('|---|---|---|---|---|---|---|---|---|---|---|')
        for win in WIN:
            a, b = qrange(win)
            cols = collections.defaultdict(list)
            for sd in SEEDS:
                NR = NAT[sd]
                rows = [NR.get((nation, q)) for q in range(a, b + 1)]
                rows = [r for r in rows if r is not None]
                if not rows:
                    continue
                nq = len(rows)
                judged = sum(fl(r['constructionJudgedRegionMonthsCount']) for r in rows)
                desiring = sum(fl(r['constructionDesiringRegionMonthsCount']) for r in rows)
                bound = sum(fl(r['constructionBudgetBoundRegionMonthsCount']) for r in rows)
                gdp = statistics.mean(fl(r['nominalGdpPerYear']) for r in rows)
                cols['judged'].append(judged / (3.0 * nq))
                cols['open'].append(desiring / judged if judged else float('nan'))
                cols['bound'].append(bound / desiring if desiring else float('nan'))
                cols['gain'].append(statistics.mean(fl(r['constructionMarginalGainMean']) for r in rows))
                cols['hurdle'].append(statistics.mean(fl(r['constructionHurdleMean']) for r in rows))
                cols['budget'].append(statistics.mean(fl(r['constructionBudgetMoneyPerMonth']) for r in rows) * 12 / gdp)
                cols['desired'].append(statistics.mean(fl(r['constructionDesiredMoneyPerMonth']) for r in rows) * 12 / gdp)
                mr, mo = mean_rows(SPEND[sd], nation, win, {CAP})
                cons = sum(v[0] for (k, n, t), v in mr.items() if k == 'extra' and n == '建設の材料と工賃')
                rail = sum(v[0] for (k, n, t), v in mr.items() if k == 'extra' and n == '建設に付く線路と機関車')
                cols['paid'].append(cons * 12 / gdp)
                cols['rail'].append(rail * 12 / gdp)
                cr = CLASS[sd].get(nation, {})
                sv = [cr.get((f'q{q}', CAP)) for q in range(a, b + 1)]
                sv = [x for x in sv if x and x[2] == x[2] and x[2] > 0]
                cols['sav'].append(statistics.mean(x[1] / (x[2] * 12) for x in sv) if sv else float('nan'))
            md.append(f"| {win} | {cell(cols['judged'], 1)} | {cell(cols['open'], 0, pct=True)}% | {cell(cols['bound'], 0, pct=True)}% | {cell(cols['gain'], 3)} | {cell(cols['hurdle'], 3)} | {cell(cols['budget'], 2, pct=True)}% | {cell(cols['desired'], 2, pct=True)}% | {cell(cols['paid'], 2, pct=True)}% | {cell(cols['rail'], 2, pct=True)}% | {cell(cols['sav'], 1)}年 |")
        md.append('')
    open(os.path.join(OUT, 't2_construction.md'), 'w', encoding='utf-8').write('\n'.join(md))


def t3_bonds():
    md = ['# T3 国債の購入：実際の額を決めているのは何か', '',
          '借入は月に1件・額＝min(1件の額〔名目GDP×割合〕, 払えない額〔請求−払える額〕)・国内の買い手のプール＝資本家の貯蓄×BondAppetite(0.15)＋外資。需要側（政府の不足）が決める形で、資本家の食欲は上限にすぎない。', '',
          '| 国 | 窓 | 借入（年額÷名目GDP） | 借入の判定の月（四半期あたり） | うちプールが縛った月 | 払えない額がある月（四半期あたり） | 国債の残高÷年GDP | 国内プール平均÷年GDP |', '|---|---|---|---|---|---|---|---|']
    for nation in NATIONS + ['ポルトガル']:
        for win in WIN:
            a, b = qrange(win)
            cols = collections.defaultdict(list)
            for sd in SEEDS:
                rows = [NAT[sd].get((nation, q)) for q in range(a, b + 1)]
                rows = [r for r in rows if r is not None]
                if not rows:
                    continue
                nq = len(rows)
                gdp = statistics.mean(fl(r['nominalGdpPerYear']) for r in rows)
                cols['bor'].append(statistics.mean(fl(r['borrowedMeanPerMonth']) for r in rows) * 12 / gdp)
                cols['jud'].append(sum(fl(r['borrowJudgedMonthsCount']) for r in rows) / nq)
                cols['pb'].append(sum(fl(r['borrowPoolBoundMonthsCount']) for r in rows) / nq)
                cols['sh'].append(sum(fl(r['shortfallMonths']) for r in rows) / nq)
                cols['debt'].append(statistics.mean(fl(r['govDebtStock']) for r in rows) / gdp)
                dp = [fl(r['borrowDomesticPoolMeanPerMonth']) for r in rows]
                dp = [x for x in dp if x == x]
                cols['dpool'].append(statistics.mean(dp) / gdp if dp else float('nan'))
            md.append(f"| {nation} | {win} | {cell(cols['bor'], 2, pct=True)}% | {cell(cols['jud'], 1)} | {cell(cols['pb'], 1)} | {cell(cols['sh'], 1)} | {cell(cols['debt'], 0, pct=True)}% | {cell(cols['dpool'], 1, pct=True)}% |")
    open(os.path.join(OUT, 't3_bonds.md'), 'w', encoding='utf-8').write('\n'.join(md))


# ---------------------------------------------------------------- T4 使用人
def t4_servants():
    md = ['# T4 家事奉仕（使用人）', '',
          '供給＝使用人の人数×生産性1.0×(採算 profitFactor)・需要＝全POPの BaseNeed 0.06×人数×(参照価格/価格)^0.7（クランプ 0.4〜1.4）・買う順＝役務の優先配分（ServiceRank：大衆→近代エリート→貴族）。', '']
    md += ['| 国 | 窓 | 使用人（千人） | 総人口に対する割合 | 富裕3階級に対する使用人の人数比 | 使用人1人あたりの売れた量 | 産出 | 需要 | 売れた量 | 不足 | 価格÷参照価格 |', '|---|---|---|---|---|---|---|---|---|---|---|']
    for nation in NATIONS:
        for win in WIN:
            a, b = qrange(win)
            cols = collections.defaultdict(list)
            for sd in SEEDS:
                rows = [r for r in SVC[sd] if r['nation'] == nation and a <= int(r['quarter']) <= b]
                if not rows:
                    continue
                f = lambda k: statistics.mean(fl(r[k]) for r in rows)
                tot = f('totalPopulationK')
                sv = f('servantK')
                cols['sv'].append(sv)
                cols['svs'].append(sv / tot)
                cols['rich'].append(sv / (f('nobleK') + f('capitalistK') + f('merchantK')))
                cols['per'].append(f('servicePerServant'))
                cols['out'].append(f('servantOutputPerMonth'))
                cols['dem'].append(f('demandPerMonth'))
                cols['sold'].append(f('soldPerMonth'))
                cols['short'].append(f('shortagePerMonth'))
                cols['pr'].append(f('priceSoldWeighted') / f('refPrice'))
            md.append(f"| {nation} | {win} | {cell(cols['sv'], 0)} | {cell(cols['svs'], 2, pct=True)}% | {cell(cols['rich'], 2)} | {cell(cols['per'], 2)} | {cell(cols['out'], 0)} | {cell(cols['dem'], 0)} | {cell(cols['sold'], 0)} | {cell(cols['short'], 0)} | {cell(cols['pr'], 2)} |")
    md.append('')
    md.append('## 誰が家事奉仕を買っているか（階級別）')
    md.append('| 国 | 窓 | 階級 | 購入量の割合 | 支払いの割合 | その階級の所得に対する家事奉仕への支払い |')
    md.append('|---|---|---|---|---|---|')
    for nation in NATIONS:
        for win in WIN:
            res = collections.defaultdict(lambda: collections.defaultdict(list))
            for sd in SEEDS:
                tq = tp = 0.0
                per = {}
                for c in range(0, 14):
                    mr, mo = mean_rows(SPEND[sd], nation, win, {c})
                    q = sum(v[1] for (k, n, t), v in mr.items() if k == 'good' and n == '家事奉仕')
                    pay = sum(v[0] for (k, n, t), v in mr.items() if k == 'good' and n == '家事奉仕')
                    earn = sum(v[2] for (k, n, t), v in mr.items() if k == 'class')
                    per[c] = (q, pay, earn)
                    tq += q
                    tp += pay
                for c, (q, pay, earn) in per.items():
                    if tq > 0 and q > 0:
                        res[c]['q'].append(q / tq)
                        res[c]['p'].append(pay / tp)
                        res[c]['e'].append(pay / earn if earn > 0 else float('nan'))
            for c in sorted(res, key=lambda c: -med(res[c]['q'])):
                if med(res[c]['q']) < 0.005:
                    continue
                md.append(f"| {nation} | {win} | {CLASSNAME.get(c, c)} | {cell(res[c]['q'], 0, pct=True)}% | {cell(res[c]['p'], 0, pct=True)}% | {cell(res[c]['e'], 2, pct=True)}% |")
    open(os.path.join(OUT, 't4_servants.md'), 'w', encoding='utf-8').write('\n'.join(md))


# ---------------------------------------------------------------- T5 奢侈品
def all_nation_class_year_end(sd, win):
    """年末の class_s（全ての国）の窓の平均：{nation: {classId: (size, earned, paid)}}。窓の中の年末（q%4==3）の行を等しく平均。"""
    a, b = qrange(win)
    acc = collections.defaultdict(lambda: collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0]))
    for r in table(os.path.join(D, f'class_s{sd}.tsv')):
        t = r['time']
        if not t.startswith('q'):
            continue
        q = int(t[1:])
        if q % 4 != 3 or q < a or q > b:
            continue
        e = fl(r['earnedPerMonth'])
        if e != e:
            continue
        x = acc[r['nation']][int(r['classId'])]
        x[0] += fl(r['sizeK']); x[1] += e; x[2] += fl(r['paidPerMonth']); x[3] += 1
    return {n: {c: (v[0] / v[3], v[1] / v[3], v[2] / v[3]) for c, v in d.items() if v[3] > 0} for n, d in acc.items()}


def lux_by_nation(S, nation, win, cls):
    """国・窓・階級の嗜好品（Luxury）の (pay, qty) の月平均。挙げた国は財ごとの行・他の国は財ごとの行（嗜好品は個別に出している）。"""
    buckets = WIN[win][2]
    pay = qty = mo = 0.0
    for (uid, nm, b), e in S.items():
        if nm != nation or b not in buckets:
            continue
        ms = [r[5] for r in e['rows'] if r[1] == 'class' and r[0] == cls]
        if not ms:
            continue
        m = ms[0]
        mo += m
        for (c, kind, slot, name, tier, months, p, q, rec) in e['rows']:
            if c == cls and kind == 'good' and tier == 'Luxury':
                pay += p * months; qty += q * months
    return (pay / mo, qty / mo) if mo > 0 else (float('nan'), float('nan'))


def t5_luxury():
    md = ['# T5 奢侈品（Luxury）：誰がどれだけ買っているか・所得との比・世界の1人あたり所得の分布', '',
          '買う量＝BaseNeed×人数×(参照価格/価格)^弾力（Luxury は上限 2.0・下限 0.4）で**所得に依らない**（全階級共通の束・EconomyStep の GoodNeed）。階級ごとの違いは予算（貯蓄の全額）だけ。', '']
    # --- 世界の1人あたり所得の分布（年末の class_s ＝全ての国）
    md.append('## 世界の1人あたり所得（Earned の和÷人口・月あたり・単位は各国の通貨のまま）と 実質GDP÷人口（年）の国の分布')
    md.append('人口 1000 千人以上の国×年末。窓の中の年末の行を国ごとに平均し、種の中央をとる。')
    md.append('| 窓 | 国の数（中央） | 所得/人 p10 | p25 | 中央 | p75 | p90 | 人口加重平均 | 英 | 仏 | 蘭 | 実質GDP/人 p10 | 中央 | p90 | 英 | 仏 | 蘭 |')
    md.append('|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|')
    WORLD = {}
    for win in WIN:
        a, b = qrange(win)
        qs = collections.defaultdict(list)
        for sd in SEEDS:
            cls = all_nation_class_year_end(sd, win)
            # 実質GDPと人口は nation_s の年末の行
            rg = collections.defaultdict(list)
            for (nm, q), r in NAT[sd].items():
                if q % 4 == 3 and a <= q <= b:
                    rg[nm].append((fl(r['realGdpPerYear']), fl(r['populationK']), fl(r['priceLevel'])))
            inc = {}; popw = {}
            for nm, d in cls.items():
                size = sum(v[0] for v in d.values()); earn = sum(v[1] for v in d.values())
                if size >= 1000 and earn > 0:
                    inc[nm] = earn / size; popw[nm] = size
            gp = {}
            for nm, L in rg.items():
                pop = statistics.mean(x[1] for x in L)
                if pop >= 1000:
                    gp[nm] = statistics.mean(x[0] for x in L) / pop
            WORLD[(sd, win)] = (inc, popw, gp)
            v = sorted(inc.values())
            if v:
                def qt(p, arr): return arr[min(len(arr) - 1, int(p * len(arr)))]
                qs['n'].append(len(v)); qs['p10'].append(qt(0.10, v)); qs['p25'].append(qt(0.25, v)); qs['p50'].append(qt(0.5, v)); qs['p75'].append(qt(0.75, v)); qs['p90'].append(qt(0.90, v))
                qs['wmean'].append(sum(inc[n] * popw[n] for n in inc) / sum(popw[n] for n in inc))
                for k, nm in (('uk', 'イギリス'), ('fr', 'フランス'), ('nl', 'ネーデルラント')):
                    qs[k].append(inc.get(nm, float('nan')))
            g = sorted(gp.values())
            if g:
                qs['g10'].append(qt(0.10, g)); qs['g50'].append(qt(0.5, g)); qs['g90'].append(qt(0.90, g))
                for k, nm in (('guk', 'イギリス'), ('gfr', 'フランス'), ('gnl', 'ネーデルラント')):
                    qs[k].append(gp.get(nm, float('nan')))
        md.append(f"| {win} | {cell(qs['n'], 0)} | {cell(qs['p10'], 2)} | {cell(qs['p25'], 2)} | {cell(qs['p50'], 2)} | {cell(qs['p75'], 2)} | {cell(qs['p90'], 2)} | {cell(qs['wmean'], 2)} | {cell(qs['uk'], 2)} | {cell(qs['fr'], 2)} | {cell(qs['nl'], 2)} | {cell(qs['g10'], 1)} | {cell(qs['g50'], 1)} | {cell(qs['g90'], 1)} | {cell(qs['guk'], 1)} | {cell(qs['gfr'], 1)} | {cell(qs['gnl'], 1)} |")
    md.append('')
    # --- 階級ごとの1人あたり所得÷世界平均（英仏蘭と、所得の低い国の貴族・資本家）
    md.append('## 階級の1人あたり所得 ÷ 世界の人口加重平均（r）。英仏蘭と、所得が低い4分位の国（他の国）の貴族・資本家・商人・農民')
    md.append('| 窓 | 国 | 貴族 r | 資本家 r | 商人 r | 農民 r | 使用人 r |')
    md.append('|---|---|---|---|---|---|---|')
    for win in WIN:
        res = collections.defaultdict(list)
        for sd in SEEDS:
            inc, popw, gp = WORLD[(sd, win)]
            if not inc:
                continue
            wm = sum(inc[n] * popw[n] for n in inc) / sum(popw[n] for n in inc)
            cls = all_nation_class_year_end(sd, win)
            order = sorted(inc, key=lambda n: inc[n])
            low = set(order[:max(1, len(order) // 4)])
            for nm in cls:
                if nm in NATIONS:
                    grp = nm
                elif nm in low:
                    grp = '低所得4分位の国(平均)'
                else:
                    continue
                for c, label in ((0, 'n'), (3, 'c'), (10, 'm'), (1, 'p'), (12, 's')):
                    v = cls[nm].get(c)
                    if v and v[0] > 0:
                        res[(grp, label)].append(v[1] / v[0] / wm)
        for grp in NATIONS + ['低所得4分位の国(平均)']:
            md.append(f"| {win} | {grp} | " + ' | '.join(cell(res[(grp, l)], 2) for l in 'ncmps') + ' |')
    md.append('')
    # --- 英仏蘭の嗜好品の購入：財ごと・階級ごと（上位）
    md.append('## 嗜好品の購入（挙げた国・月あたり）：階級別の合計と、所得に対する割合')
    md.append('| 国 | 窓 | 階級 | 嗜好品への支払い÷所得 | 嗜好品の数量/人（千人あたり月） | 全階級の嗜好品の支払いに占める割合 |')
    md.append('|---|---|---|---|---|---|')
    for nation in NATIONS:
        for win in WIN:
            res = collections.defaultdict(lambda: collections.defaultdict(list))
            for sd in SEEDS:
                tot = 0.0; per = {}
                for c in range(0, 14):
                    mr, mo = mean_rows(SPEND[sd], nation, win, {c})
                    pay = sum(v[0] for (k, n, t), v in mr.items() if k == 'good' and t == 'Luxury')
                    qty = sum(v[1] for (k, n, t), v in mr.items() if k == 'good' and t == 'Luxury')
                    earn = sum(v[2] for (k, n, t), v in mr.items() if k == 'class')
                    size = sum(v[1] for (k, n, t), v in mr.items() if k == 'class')
                    per[c] = (pay, qty, earn, size); tot += pay
                for c, (pay, qty, earn, size) in per.items():
                    if earn > 0 and size > 0:
                        res[c]['r'].append(pay / earn); res[c]['q'].append(qty / size); res[c]['s'].append(pay / tot if tot > 0 else float('nan'))
            for c in (0, 3, 10, 1, 2, 12):
                if c in res:
                    md.append(f"| {nation} | {win} | {CLASSNAME[c]} | {cell(res[c]['r'], 2, pct=True)}% | {cell(res[c]['q'], 4)} | {cell(res[c]['s'], 0, pct=True)}% |")
    md.append('')
    # --- 財ごと（英・Q5-40 と Q41-160）の階級別の支払い
    md.append('## 嗜好品の財ごと：英の階級別の支払い（月あたり・窓 Q5〜Q40）')
    md.append('| 財 | 全階級の支払い | 貴族 | 資本家 | 商人 | 農民 | 工場労働者 |')
    md.append('|---|---|---|---|---|---|---|')
    goods = collections.defaultdict(lambda: collections.defaultdict(list))
    for sd in SEEDS:
        for c in (0, 3, 10, 1, 2):
            mr, mo = mean_rows(SPEND[sd], 'イギリス', 'Q5-40', {c})
            for (k, n, t), v in mr.items():
                if k == 'good' and t == 'Luxury':
                    goods[n][c].append(v[0])
    rows = []
    for n, d in goods.items():
        tot = sum(med(d[c]) for c in d if med(d[c]) == med(d[c]))
        rows.append((tot, n, d))
    for tot, n, d in sorted(rows, reverse=True)[:25]:
        md.append(f"| {n} | {tot:.1f} | " + ' | '.join(f"{med(d[c]):.1f}" if c in d else '0' for c in (0, 3, 10, 1, 2)) + ' |')
    md.append('')
    # --- 静的な見込み：奢侈品だけに所得弾力 ε を入れたとき（f=max(1,r)^ε・参照＝世界の人口加重平均の1人あたり所得）
    md.append('## 静的な見込み：奢侈品の需要に f＝max(1, r)^ε を掛けたとき（r＝階級の1人あたり所得÷世界平均）')
    md.append('価格・供給は据え置き（需要が増えれば実際は価格が上がり弾力で削られる＝上限の見込み）。支払いの増加分÷その階級の所得。')
    md.append('| 国 | 窓 | 階級 | r | 今の嗜好品の支払い÷所得 | ε=0.25 | ε=0.5 | ε=1.0 |')
    md.append('|---|---|---|---|---|---|---|---|')
    for nation in NATIONS:
        for win in WIN:
            res = collections.defaultdict(lambda: collections.defaultdict(list))
            for sd in SEEDS:
                inc, popw, gp = WORLD[(sd, win)]
                if not inc:
                    continue
                wm = sum(inc[n] * popw[n] for n in inc) / sum(popw[n] for n in inc)
                cls = all_nation_class_year_end(sd, win).get(nation, {})
                for c in (0, 3, 10):
                    mr, mo = mean_rows(SPEND[sd], nation, win, {c})
                    pay = sum(v[0] for (k, n, t), v in mr.items() if k == 'good' and t == 'Luxury')
                    earn = sum(v[2] for (k, n, t), v in mr.items() if k == 'class')
                    v = cls.get(c)
                    if not (earn > 0 and v and v[0] > 0):
                        continue
                    r = v[1] / v[0] / wm
                    res[c]['r'].append(r); res[c]['now'].append(pay / earn)
                    for eps in (0.25, 0.5, 1.0):
                        res[c][eps].append(pay * max(1.0, r) ** eps / earn)
            for c in (0, 3, 10):
                if c in res:
                    md.append(f"| {nation} | {win} | {CLASSNAME[c]} | {cell(res[c]['r'], 1)} | {cell(res[c]['now'], 2, pct=True)}% | {cell(res[c][0.25], 2, pct=True)}% | {cell(res[c][0.5], 2, pct=True)}% | {cell(res[c][1.0], 2, pct=True)}% |")
    open(os.path.join(OUT, 't5_luxury.md'), 'w', encoding='utf-8').write('\n'.join(md))



if __name__ == '__main__':
    SPEND = {sd: load_spend(sd) for sd in SEEDS}
    FLOW, CLASS, NAT, SVC = {}, {}, {}, {}
    for sd in SEEDS:
        FLOW[sd] = {n: flow_rows(sd, n) for n in NATIONS}
        CLASS[sd] = {n: class_rows(sd, n) for n in NATIONS + ['ポルトガル']}
        NAT[sd] = nation_rows(sd)
        SVC[sd] = list(table(os.path.join(D, f'svc_s{sd}.tsv')))
    which = sys.argv[3:] or ['t1', 't2', 't3', 't4', 't5']
    if 't1' in which: t1_routes()
    if 't2' in which: t2_construction()
    if 't3' in which: t3_bonds()
    if 't4' in which: t4_servants()
    if 't5' in which: t5_luxury()
