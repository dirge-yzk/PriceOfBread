"""(289) 線D：作者の設計の材料の読み手（読むだけ）。
  python an_d289.py <計器の出力ディレクトリ(ins)> <表の出力ディレクトリ> [a|b|c ...]
  入力＝`INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins
        spend_s（窓の区切りごとの支払いの種類×階級・stateTakePerMonth つき）・cons_s（資本家の建設の州ごとの表）・nation_s（earnedTotalPerMonth ほか）・flow_s・fiscal_s・class_s。
  窓（四半期の番号 q は 0 始まり・表の Q は q+1）：Q1-8＝q0..7（区切り0+1）・Q5-40＝q4..39（1+2）・Q41-160＝q40..159（3）・Q189-228＝q188..227（1830年代・区切り5）・Q229-268＝q228..267（1840年代・区切り6）。
  (a) 投資の総額：英・仏・蘭・葡の粗投資にあたる流れを**種ごとに全部足してから**中央値（最小〜最大）。分母＝国の所得（全階級の Earned の和）と名目GDP の両方。国債の購入・資本の輸出は別の行（合計に入れない）。
  (b) 州の偏り：cons_s の州ごとの貯蓄・希望・投資枠・実際の建設・人口・産業。偏りの数（ジニ係数・シェアの食い違い）。
  (c) 奢侈品の参照所得：国・階級の1人あたり所得（Earned÷人数×為替÷物価水準）÷世界の人口加重平均 r と、静的な見込み。
  財は名前で引く（番号を書かない）。
  計器の限界の自己申告：①spend_s の窓の月数は四半期×3の和②flow_s・fiscal_s は挙げた国だけ③fiscal_s の四半期は「行の並びを3か月ずつ」で割り当てる（monthIndex の連続を確かめる）④国庫が払った建設は nation_s の末尾の列（新規）⑤上下水道・電化インフラは請求×支払い率の見込み（払えた額の直接の列は無い）。"""
import sys, os, statistics, collections, math
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..'))
sys.path.insert(0, os.path.join(HERE, '..', 'flow262'))
from instr import num as fl, seeds  # noqa: E402
from route_names import ROUTE_NAMES  # noqa: E402

D, OUT = sys.argv[1], sys.argv[2]
WHICH = sys.argv[3:] or ['a', 'b', 'c']
os.makedirs(OUT, exist_ok=True)
SEEDS = seeds(D, 'nation')
# 窓：ラベル → (q 始め, q 終わり, 区切りの番号)
WIN = {'Q1-8': (0, 7, (0, 1)), 'Q5-40': (4, 39, (1, 2)), 'Q41-160': (40, 159, (3,)), 'Q189-228(1830年代)': (188, 227, (5,)), 'Q229-268(1840年代)': (228, 267, (6,))}
NATIONS = ['イギリス', 'フランス', 'ネーデルラント', 'ポルトガル']
CLASSNAME = {0: '貴族', 1: '農民', 2: '工場労働者', 3: '資本家', 4: '技師', 5: '兵士', 6: '聖職者', 7: '教師', 8: '官僚', 9: '医者', 10: '商人', 11: '職人', 12: '使用人', 13: '研究者'}


def table(path, cols=None, where=None):
    """タブ区切りを辞書で。cols を渡すとその列だけ残す（速くする）。where(行の辞書)＝偽なら捨てる。"""
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        idx = [(h, i) for i, h in enumerate(head) if cols is None or h in cols]
        for ln in fh:
            f = ln.rstrip('\n').split('\t')
            r = {h: f[i] for h, i in idx}
            if where is None or where(r):
                yield r


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def cell(xs, nd=1, pct=False, suffix=''):
    xs = [x for x in xs if x == x]
    if not xs:
        return '—'
    k = 100.0 if pct else 1.0
    s = '%' if pct else ''
    return f'{med(xs)*k:.{nd}f}{s} ({min(xs)*k:.{nd}f}〜{max(xs)*k:.{nd}f}){suffix}'


def gini(xs):
    """非負の量のジニ係数（0＝均等・1＝1つに集中）。全て 0 なら nan。"""
    xs = sorted(x for x in xs if x == x and x >= 0)
    n = len(xs); s = sum(xs)
    if n == 0 or s <= 0:
        return float('nan')
    cum = 0.0
    for i, x in enumerate(xs, 1):
        cum += i * x
    return (2.0 * cum) / (n * s) - (n + 1.0) / n


def tv(p, q):
    """シェアの食い違い＝0.5×Σ|p_i/Σp − q_i/Σq|（一方のシェアのうち、他方に合わせるには動かす割合）。"""
    sp, sq = sum(p), sum(q)
    if not (sp > 0 and sq > 0):
        return float('nan')
    return 0.5 * sum(abs(a / sp - b / sq) for a, b in zip(p, q))


def spearman(a, b):
    n = len(a)
    if n < 3:
        return float('nan')
    def rk(v):
        o = sorted(range(n), key=lambda i: v[i]); r = [0.0] * n; i = 0
        while i < n:
            j = i
            while j + 1 < n and v[o[j + 1]] == v[o[i]]:
                j += 1
            for k in range(i, j + 1):
                r[o[k]] = (i + j) / 2.0
            i = j + 1
        return r
    ra, rb = rk(a), rk(b)
    ma, mb = sum(ra) / n, sum(rb) / n
    sa = math.sqrt(sum((x - ma) ** 2 for x in ra)); sb = math.sqrt(sum((x - mb) ** 2 for x in rb))
    return sum((x - ma) * (y - mb) for x, y in zip(ra, rb)) / (sa * sb) if sa > 0 and sb > 0 else float('nan')


# ---------------------------------------------------------------- 読み込み
def load_spend(sd):
    """spend_s → {(nation, bucket): {'months', 'class': {c: (pay, size, earn)}, 'good': [(c, name, tier, pay, qty, rec, take)], 'extra': [(c, name, pay)], 'tier': [(c, tier, pay, take)]}}"""
    out = {}
    for r in table(os.path.join(D, f'spend_s{sd}.tsv')):
        key = (r['nation'], int(r['bucket']))
        e = out.get(key)
        if e is None:
            e = out[key] = {'months': fl(r['monthsCount']), 'class': {}, 'good': [], 'extra': [], 'tier': [], 'picked': r['pickedFlag'] == '1'}
        c = int(r['classId']); k = r['kind']
        pay, qty, rec, take = fl(r['payPerMonth']), fl(r['qtyPerMonth']), fl(r['receiptPerMonth']), fl(r['stateTakePerMonth'])
        if k == 'class':
            e['class'][c] = (pay, qty, rec)             # 支出・人数(千人)・所得
        elif k == 'good':
            e['good'].append((c, r['slotName'], r['tier'], pay, qty, rec, take))
        elif k == 'extra':
            e['extra'].append((c, r['slotName'], pay))
        else:
            e['tier'].append((c, r['tier'], pay, take))
    return out


NAT_COLS = {'quarter', 'nation', 'populationK', 'totalPopulationK', 'nominalGdpPerYear', 'earnedTotalPerMonth', 'stateConstructionPaidPerMonth', 'monumentPaidPerMonth', 'fxRate', 'priceLevel', 'revenueBaseMeanPerMonth', 'indirectRevenueMeanPerMonth', 'borrowedMeanPerMonth'}


def load_nation(sd):
    """nation_s → {(nation, q): {列: 数}}（全ての国）"""
    out = {}
    for r in table(os.path.join(D, f'nation_s{sd}.tsv'), NAT_COLS):
        out[(r['nation'], int(r['quarter']))] = {k: fl(v) for k, v in r.items() if k not in ('nation', 'quarter')}
    return out


def load_flow(sd):
    """flow_s → {nation: {(q, classId, kind, route, side): 月あたり}}（q は窓の最大まで）"""
    out = {n: collections.defaultdict(float) for n in NATIONS}
    qmax = max(w[1] for w in WIN.values())
    for r in table(os.path.join(D, f'flow_s{sd}.tsv'), {'quarter', 'nation', 'classId', 'kind', 'sourceFile', 'sourceLine', 'counterpartySide', 'amountPerMonth'}, lambda r: r['nation'] in NATIONS and int(r['quarter']) <= qmax):
        f = os.path.basename(r['sourceFile']); ln = int(r['sourceLine'])
        route = ROUTE_NAMES.get((f, ln), f'{f}:{ln}')
        out[r['nation']][(int(r['quarter']), int(r['classId']), r['kind'], route, r['counterpartySide'])] += fl(r['amountPerMonth'])
    return out


FISC_COLS = {'monthIndex', 'nation', 'claimInfraPerMonth', 'claimElecInfraPerMonth', 'payRateShare', 'ledgerFactoryBuildPerMonth', 'borrowedPerMonth'}


def load_fiscal(sd):
    """fiscal_s → {nation: [(q, 行の辞書)]}。q＝行の並びを3か月ずつ（四半期の末にまとめて書く＝先頭から3行が q0）。monthIndex の連続は別に確かめる。"""
    out = {n: [] for n in NATIONS}; cnt = {n: 0 for n in NATIONS}; last = {}
    gaps = 0
    for r in table(os.path.join(D, f'fiscal_s{sd}.tsv'), FISC_COLS, lambda r: r['nation'] in NATIONS):
        n = r['nation']; m = int(r['monthIndex'])
        if n in last and m != last[n] + 1:
            gaps += 1
        last[n] = m
        out[n].append((cnt[n] // 3, {k: fl(v) for k, v in r.items() if k not in ('nation', 'monthIndex')}))
        cnt[n] += 1
    return out, gaps


# ---------------------------------------------------------------- (a) 投資の総額
PRIV = [  # (行の名前, 種類, 名前)  種類＝extra（spend_s の財でない支払い）/ flow（flow_s の払い take）
    ('工場の建設（材料と工賃）', 'extra', '建設の材料と工賃'), ('建設に付く線路・機関車', 'extra', '建設に付く線路と機関車'),
    ('止まった工場の再開の発注', 'extra', '止まった工場の再開の発注'), ('送電網', 'extra', '送電網'), ('インフラ保守（資本家の払い）', 'extra', 'インフラ保守'),
    ('港の建設の工賃（資本家ルート）', 'flow', '港の建設の工賃・資本家ルート'), ('運河の建設の工賃', 'flow', '運河の建設の工賃'),
    ('船と線路の買い付け', 'flow', '船と線路の買い付け'), ('植民地の開発の汽船の代金', 'flow', '植民地の開発の汽船の代金'), ('植民地の開発の線路の代金', 'flow', '植民地の開発の線路の代金'),
]
PUBL = ['国の建設（払えた額）', '巨大建造物（払えた額）', '国営工場の建設（国庫の台帳）', '港の建設の工賃（国家ルート）', '上下水道・電化インフラ（請求×支払い率）']
MEMO = [('国債の国内購入（資本家→国庫）', '国債の国内購入', ('take',)), ('資本の輸出の送り出し', '資本の輸出の送り出し', ('take',)), ('企業への与信・貸し（銀行→投資家・受け取り）', '企業への与信・貸し(銀行→投資家)', ('give', 'giveIncome'))]
EXCL_FOR_B = ('止まった工場の再開の発注', 'インフラ保守（資本家の払い）')   # 「再開の発注・保守を除く」の合計から外す行


def spend_window(SP, nation, win, kind, name):
    """窓の月あたりの平均（区切りを月数で重みづけ）。kind＝extra：全階級の和。"""
    a, b, bk = WIN[win]
    tot = mo = 0.0
    for bkt in bk:
        e = SP.get((nation, bkt))
        if not e or not e['months'] == e['months']:
            continue
        mo += e['months']
        if kind == 'extra':
            tot += sum(p for (c, nm, p) in e['extra'] if nm == name and p == p) * e['months']
    return tot / mo if mo > 0 else float('nan')


def invest_seed(sd, SP, NAT, FL, FI, nation, win):
    """1種・1国・1窓の投資の流れ（年額）と分母。戻り値＝({行: 年額}, 年所得, 名目GDP)"""
    a, b, _ = WIN[win]
    qs = range(a, b + 1)
    rows = [NAT.get((nation, q)) for q in qs]
    rows = [r for r in rows if r is not None and r['earnedTotalPerMonth'] == r['earnedTotalPerMonth']]
    if not rows:
        return None
    income = statistics.mean(r['earnedTotalPerMonth'] for r in rows) * 12
    gdp = statistics.mean(r['nominalGdpPerYear'] for r in rows)
    nq = b - a + 1
    res = {}
    fl_n = FL.get(nation, {})
    for label, kind, name in PRIV:
        if kind == 'extra':
            res[label] = spend_window(SP, nation, win, 'extra', name) * 12
        else:
            v = 0.0
            for (q, c, k, route, side), x in fl_n.items():
                if a <= q <= b and k == 'take' and route.startswith(name):
                    v += -x
            res[label] = v / nq * 12
    st = statistics.mean(r['stateConstructionPaidPerMonth'] for r in rows) * 12
    mn = statistics.mean(r['monumentPaidPerMonth'] for r in rows) * 12
    res[PUBL[0]] = st; res[PUBL[1]] = mn
    fs = [(q, r) for q, r in FI.get(nation, []) if a <= q <= b]
    res[PUBL[2]] = (-sum(r['ledgerFactoryBuildPerMonth'] for q, r in fs if r['ledgerFactoryBuildPerMonth'] == r['ledgerFactoryBuildPerMonth']) / len(fs) * 12) if fs else float('nan')
    res[PUBL[4]] = (sum((r['claimInfraPerMonth'] + r['claimElecInfraPerMonth']) * (r['payRateShare'] if r['payRateShare'] == r['payRateShare'] else 1.0) for q, r in fs) / len(fs) * 12) if fs else float('nan')
    gv = 0.0
    for (q, c, k, route, side), x in fl_n.items():
        if a <= q <= b and k in ('give', 'giveIncome') and route.startswith('港の建設の工賃・国家ルート'):
            gv += x
    res[PUBL[3]] = gv / nq * 12
    for label, name, kinds in MEMO:
        v = 0.0
        for (q, c, k, route, side), x in fl_n.items():
            if a <= q <= b and k in kinds and route.startswith(name):
                v += -x if k == 'take' else x
        res[label] = v / nq * 12
    return res, income, gdp


def t_a():
    PRIV_LABELS = [p[0] for p in PRIV]
    md = ['# T-A 投資の総額（粗投資にあたる流れを種ごとに全部足してから中央値・最小〜最大）', '',
          '■ 足した物：民間＝資本家の工場建設（材料と工賃）・建設に付く線路と機関車・止まった工場の再開の発注・送電網・インフラ保守・港の建設の工賃（資本家ルート）・運河の工賃・船と線路の買い付け・植民地開発の汽船と線路。'
          '国＝国庫が実際に払った国の建設・巨大建造物・国営工場の建設（国庫の台帳）・港の建設の工賃（国家ルート）・上下水道と電化インフラ（請求×支払い率の見込み）。',
          '■ 合計に入れない物（別の行）：国債の国内購入（国の払えない額を埋める移動で、国の建設の請求と二重になる）・資本の輸出（国外への投資）・企業への与信（貯蓄の移動）。',
          '■ 合計は「種ごとに全ての行を足した値」の中央値（行の中央値の和ではない）。分母＝国の所得（全階級の Earned の和×12）と名目GDP（総産出ベース）。どちらも自国通貨の名目。',
          '■ 史実（粗投資÷国民所得）：英 1760 年 約 6%・1830〜40 年代 約 11%【推・Allen の要約経由で原典未確認】。', '']
    allres = {}
    for nation in NATIONS:
        for win in WIN:
            for sd in SEEDS:
                allres[(nation, win, sd)] = invest_seed(sd, SPEND[sd], NAT[sd], FLOW[sd], FISC[sd], nation, win)
    for den_i, den_name in ((1, '国の所得（Earned の和）'), (2, '名目GDP')):
        md.append(f'## 分母＝{den_name}')
        for nation in NATIONS:
            md.append(f'### {nation}')
            md.append('| 行 | ' + ' | '.join(WIN) + ' |')
            md.append('|---|' + '---|' * len(WIN))

            def row(label, fn):
                cells = []
                for win in WIN:
                    xs = []
                    for sd in SEEDS:
                        r = allres[(nation, win, sd)]
                        if r is None:
                            continue
                        res, inc, gdp = r
                        d = inc if den_i == 1 else gdp
                        v = fn(res)
                        xs.append(v / d if v == v and d else float('nan'))
                    cells.append(cell(xs, 2, pct=True))
                md.append(f'| {label} | ' + ' | '.join(cells) + ' |')
            def sm(res, labels):
                xs = [res[l] for l in labels]
                return sum(x for x in xs if x == x) if any(x == x for x in xs) else float('nan')
            allp = PRIV_LABELS + PUBL
            row('**合計（全部）**', lambda res: sm(res, allp))
            row('合計から再開の発注・保守を除く', lambda res: sm(res, [l for l in allp if l not in EXCL_FOR_B]))
            row('合計から再開の発注・保守・港の建設（資本家ルート）を除く', lambda res: sm(res, [l for l in allp if l not in EXCL_FOR_B and l != PRIV_LABELS[5]]))
            row('うち民間の合計', lambda res: sm(res, PRIV_LABELS))
            row('うち国の合計', lambda res: sm(res, PUBL))
            row('うち民間の工場建設だけ（材料と工賃）', lambda res: res[PRIV_LABELS[0]])
            md.append('| ― 内訳（行ごとの中央値） ― |' + ' |' * len(WIN))
            for l in allp:
                row(l, lambda res, l=l: res[l])
            md.append('| ― 合計に入れない物 ― |' + ' |' * len(WIN))
            for l, _, _k in MEMO:
                row(l, lambda res, l=l: res[l])
            md.append('')
    open(os.path.join(OUT, 't_a_invest_total.md'), 'w', encoding='utf-8').write('\n'.join(md))


# ---------------------------------------------------------------- (b) 州の偏り
CONS_COLS = {'quarter', 'nation', 'region', 'tier', 'activeFlag', 'populationK', 'investorSizeK', 'investorSavingsStock', 'capitalistSavingsStock', 'industrializationIndex', 'productiveIndustrializationIndex', 'factoryScale', 'judgedMonthsCount', 'monthsCount', 'desiredMoneyPerMonth', 'budgetMoneyPerMonth', 'plannedMoneyPerMonth', 'builtUnitsPerMonth', 'paidPerMonth'}
BWIN = {'Q5-40': (4, 39), 'Q41-160': (40, 159)}


def load_cons(sd):
    out = collections.defaultdict(list)
    p = os.path.join(D, f'cons_s{sd}.tsv')
    if not os.path.exists(p):
        return out
    for r in table(p, CONS_COLS):
        out[r['nation']].append({'q': int(r['quarter']), 'region': r['region'], 'tier': r['tier'], 'active': r['activeFlag'] == '1', **{k: fl(v) for k, v in r.items() if k not in ('quarter', 'nation', 'region', 'tier', 'activeFlag')}})
    return out


def region_window(rows, a, b):
    """州ごとの窓の平均：{州: 値の辞書}。ストックは四半期の末の平均・フローは月あたりの四半期の平均。"""
    acc = collections.defaultdict(lambda: collections.defaultdict(list))
    tier = {}
    for r in rows:
        if a <= r['q'] <= b and r['active']:
            tier[r['region']] = r['tier']
            for k in ('populationK', 'investorSavingsStock', 'capitalistSavingsStock', 'productiveIndustrializationIndex', 'factoryScale', 'desiredMoneyPerMonth', 'budgetMoneyPerMonth', 'plannedMoneyPerMonth', 'paidPerMonth', 'builtUnitsPerMonth', 'investorSizeK'):
                v = r[k]
                if v == v:
                    acc[r['region']][k].append(v)
    return {n: {k: statistics.mean(v) for k, v in d.items()} for n, d in acc.items()}, tier


def t_b(CONSX=None, fname='t_b_region_skew.md', title_note=''):
    md = ['# T-B 州の偏り：お金のある州と建てたい州の食い違い（英・仏・蘭・葡）' + title_note, '',
          '■ 州ごとの量（窓の平均）：資本家の貯蓄の在庫（投資家の POP）・希望（採算の門が望んだ額の月あたり）・投資枠（IndInvestmentFrac×その州の貯蓄の月あたり）・計画（枠で切った後）・実際の払い（材料＋工賃）・人口・工業化度・工場の規模。',
          '■ ジニ係数＝州の間の量の偏り（0＝全州で同じ・1＝1州に集中）。**シェアの食い違い**＝0.5×Σ|貯蓄のシェア−他の量のシェア|（貯蓄の何割を動かせば他の量の分布に合うか）。スピアマン＝州の順位の相関。',
          '■ 国のプールにしたときの見込み：実際の計画の和 Σ_州 min(希望,枠) に対し、国全体で min(Σ希望, Σ枠) まで建てられるとしたときの比（四半期ごとに計算して窓で平均）＝貯蓄が州をまたいで動けたときの上限の見込み（配給・価格・採算の門の変化は見ない）。', '']
    # 国×窓ごとの指標
    gather = collections.defaultdict(lambda: collections.defaultdict(list))
    topreg = collections.defaultdict(lambda: collections.defaultdict(list))
    for sd in SEEDS:
        C = (CONSX or CONS)[sd]
        for nation in NATIONS:
            rows = C.get(nation, [])
            for win, (a, b) in BWIN.items():
                rw, tier = region_window(rows, a, b)
                names = sorted(rw)
                names = [n for n in names if 'populationK' in rw[n]]
                if len(names) < 3:
                    continue
                g = lambda k: [rw[n].get(k, 0.0) for n in names]
                sav, des, bud, pln, pay, pop, ind, fac = g('investorSavingsStock'), g('desiredMoneyPerMonth'), g('budgetMoneyPerMonth'), g('plannedMoneyPerMonth'), g('paidPerMonth'), g('populationK'), g('productiveIndustrializationIndex'), g('factoryScale')
                key = (nation, win)
                gather[key]['n'].append(len(names))
                for lab, v in (('貯蓄', sav), ('希望', des), ('投資枠', bud), ('実際の払い', pay), ('人口', pop), ('工業化度', ind), ('工場の規模', fac)):
                    gather[key]['gini_' + lab].append(gini(v))
                for lab, v in (('希望', des), ('実際の払い', pay), ('人口', pop), ('工業化度', ind), ('工場の規模', fac)):
                    gather[key]['tv_' + lab].append(tv(sav, v))
                    gather[key]['sp_' + lab].append(spearman(sav, v))
                gather[key]['tv_希望_払い'].append(tv(des, pay))
                gather[key]['share_zero_sav'].append(sum(1 for n in names if rw[n].get('investorSavingsStock', 0) <= 0) / len(names))
                # 希望が正の州のうち、投資枠が希望を切った割合（金額）＝Σ max(0,希望−枠)/Σ希望（窓の平均どうしで近似）・国のプールの見込み
                gather[key]['cut_actual'].append(sum(max(0.0, d - bb) for d, bb in zip(des, bud)) / sum(des) if sum(des) > 0 else float('nan'))
                # 四半期ごと：Σ_州 min(希望, 枠)（実際の計画の近似）と min(Σ希望, Σ枠)
                byq = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0.0])
                for r in rows:
                    if a <= r['q'] <= b and r['active']:
                        d, bb, pl = r['desiredMoneyPerMonth'], r['budgetMoneyPerMonth'], r['plannedMoneyPerMonth']
                        if d == d and bb == bb:
                            x = byq[r['q']]; x[0] += d; x[1] += bb; x[2] += min(d, bb); x[3] += pl if pl == pl else 0.0
                lifts = []; planned_share = []
                for q, (D_, B_, M_, P_) in byq.items():
                    if M_ > 0:
                        lifts.append(min(D_, B_) / M_)
                        planned_share.append(P_ / M_)
                gather[key]['pool_lift'].append(statistics.mean(lifts) if lifts else float('nan'))
                gather[key]['sum_des_over_sum_bud'].append(sum(des) / sum(bud) if sum(bud) > 0 else float('nan'))
                # 上位の州（英の表）
                if nation == 'イギリス':
                    ts, td, tp, tpo = sum(sav), sum(des), sum(pay), sum(pop)
                    ti = sum(ind)
                    for n, s_, d_, p_, o_, i_ in zip(names, sav, des, pay, pop, ind):
                        topreg[win][n].append((s_ / ts if ts > 0 else float('nan'), d_ / td if td > 0 else float('nan'), p_ / tp if tp > 0 else float('nan'), o_ / tpo if tpo > 0 else float('nan'), i_ / ti if ti > 0 else float('nan'), tier.get(n, '')))
    for win in BWIN:
        md.append(f'## 窓 {win}')
        md.append('| 国 | 州の数 | ジニ：貯蓄 | 希望 | 投資枠 | 実際の払い | 人口 | 工業化度 | 工場の規模 | 貯蓄が0の州の割合 |')
        md.append('|---|---|---|---|---|---|---|---|---|---|')
        for nation in NATIONS:
            g = gather.get((nation, win))
            if not g:
                continue
            md.append(f"| {nation} | {cell(g['n'], 0)} | " + ' | '.join(cell(g['gini_' + l], 2) for l in ('貯蓄', '希望', '投資枠', '実際の払い', '人口', '工業化度', '工場の規模')) + f" | {cell(g['share_zero_sav'], 0, pct=True)} |")
        md.append('')
        md.append('シェアの食い違い（貯蓄のシェアと他の量のシェアの差・0＝同じ分布）とスピアマン（州の順位の相関・1＝同じ順位）')
        md.append('| 国 | 食い違い：貯蓄↔希望 | 貯蓄↔実際の払い | 貯蓄↔人口 | 貯蓄↔工業化度 | 貯蓄↔工場の規模 | 希望↔実際の払い | 順位の相関：貯蓄↔希望 | 貯蓄↔実際の払い | 貯蓄↔人口 | 貯蓄↔工業化度 |')
        md.append('|---|---|---|---|---|---|---|---|---|---|---|')
        for nation in NATIONS:
            g = gather.get((nation, win))
            if not g:
                continue
            md.append(f"| {nation} | " + ' | '.join(cell(g[k], 2) for k in ('tv_希望', 'tv_実際の払い', 'tv_人口', 'tv_工業化度', 'tv_工場の規模', 'tv_希望_払い')) + ' | ' + ' | '.join(cell(g[k], 2) for k in ('sp_希望', 'sp_実際の払い', 'sp_人口', 'sp_工業化度')) + ' |')
        md.append('')
        md.append('投資枠が希望を切った割合と、貯蓄を国のプールにしたときの見込み')
        md.append('| 国 | Σ希望÷Σ投資枠（国全体） | 州ごとに見て枠が希望を切った割合（金額・窓の平均どうし） | 国のプールにしたときの建設額÷実際の計画（四半期ごと・平均） |')
        md.append('|---|---|---|---|')
        for nation in NATIONS:
            g = gather.get((nation, win))
            if not g:
                continue
            md.append(f"| {nation} | {cell(g['sum_des_over_sum_bud'], 2)} | {cell(g['cut_actual'], 0, pct=True)} | {cell(g['pool_lift'], 2)} |")
        md.append('')
        md.append(f'### 英の州の表（{win}・貯蓄のシェアの上位20州・種の中央）')
        md.append('各量の「国全体に占めるシェア」。工業化度のシェアは州の工業化度の和に対して。')
        md.append('| 州 | 格 | 貯蓄 | 希望 | 実際の払い | 人口 | 工業化度 |')
        md.append('|---|---|---|---|---|---|---|')
        tr = topreg[win]
        ordered = sorted(tr, key=lambda n: -med([x[0] for x in tr[n]]))[:20]
        for n in ordered:
            xs = tr[n]
            md.append(f"| {n} | {xs[0][5]} | " + ' | '.join(f"{med([x[i] for x in xs])*100:.1f}%" for i in range(5)) + ' |')
        md.append('')
    open(os.path.join(OUT, fname), 'w', encoding='utf-8').write('\n'.join(md))


# ---------------------------------------------------------------- (c) 奢侈品の参照所得
CONV = {'raw': lambda fx, P: 1.0, 'fx': lambda fx, P: fx, 'fx/P': lambda fx, P: fx / P}


def win_nation_income(sd, win, conv_key):
    """1種・1窓の全ての国：{国: {'earn','size','fx','P','cls': {c: (earn, size)}, 'conv'}}。所得・人数は spend_s の階級の行（区切りを月数で重みづけ）・為替と物価水準は nation_s の窓の平均。"""
    a, b, bk = WIN[win]
    SP, NAT = SPEND[sd], NAT_ALL[sd]
    nations = {n for (n, bkt) in SP}
    out = {}
    for n in nations:
        tot = collections.defaultdict(lambda: [0.0, 0.0]); mo = 0.0
        for bkt in bk:
            e = SP.get((n, bkt))
            if not e:
                continue
            m = e['months']
            if not m == m:
                continue
            mo += m
            for c, (pay, size, earn) in e['class'].items():
                if earn == earn and size == size:
                    tot[c][0] += earn * m; tot[c][1] += size * m
        if mo <= 0:
            continue
        cls = {c: (v[0] / mo, v[1] / mo) for c, v in tot.items()}
        earn = sum(v[0] for v in cls.values()); size = sum(v[1] for v in cls.values())
        fx = [NAT[(n, q)]['fxRate'] for q in range(a, b + 1) if (n, q) in NAT and NAT[(n, q)]['fxRate'] == NAT[(n, q)]['fxRate']]
        pl = [NAT[(n, q)]['priceLevel'] for q in range(a, b + 1) if (n, q) in NAT and NAT[(n, q)]['priceLevel'] == NAT[(n, q)]['priceLevel']]
        if not fx or not pl or not (size > 0 and earn > 0):
            continue
        cv = CONV[conv_key](statistics.mean(fx), statistics.mean(pl))
        out[n] = {'earn': earn, 'size': size, 'cls': cls, 'conv': cv}
    return out


def world_mean(inc, floor_k=100.0):
    """世界の人口加重平均の1人あたり所得（共通の通貨に直した後）＝Σ(所得×換算)÷Σ人数。人数 floor_k 千人未満の国は除く。"""
    num = den = 0.0
    for n, v in inc.items():
        if v['size'] >= floor_k:
            num += v['earn'] * v['conv']; den += v['size']
    return num / den if den > 0 else float('nan')


def pctile(xs, p):
    xs = sorted(x for x in xs if x == x)
    return xs[min(len(xs) - 1, int(p * len(xs)))] if xs else float('nan')


def t_c():
    md = ['# T-C 奢侈品の参照所得（作者の案：参照所得＝世界平均・奢侈品だけ所得弾力）', '',
          '■ r＝（所得÷人数）×換算 ÷ 世界の人口加重平均（共通の通貨に直した1人あたり所得）。国の r は国の全階級の合計、階級の r は階級の所得と人数。窓の平均（所得・人数は spend_s の階級の行・為替と物価水準は nation_s の窓の平均）。人数 100 千人未満の国は世界平均から除く。',
          '■ 換算の3本：「名目」＝自国通貨のまま（為替も物価も掛けない）／「為替」＝×fxRate（金建てへ）／「為替÷物価」＝×fxRate÷priceLevel（AI の案＝(287) の反証役 Q〔v〕6 の処置・作者の指定ではない〔(290) で訂正〕）。',
          '■ 計器の限界〔(290) 反証役 v 低で追記〕：挙げた国（英仏蘭葡）以外の spend_s は財ごとの行を 6 階級（貴族・農民・工場労働者・資本家・商人・使用人）にしか書かない＝世界の分布の「所得に対する変化」と「専売の取り分÷歳入」は技師・聖職者・官僚などを含まない（国単位の r^ε−1 と英仏蘭葡の数字は影響しない）。', '']
    # --- r の分布（国）
    md.append('## 国単位の r（1人あたり所得÷世界の人口加重平均）の分布と英仏蘭葡')
    md.append('| 窓 | 換算 | 国の数 | p10 | p25 | 中央 | p75 | p90 | r<0.5 の国の割合 | r>1 の国の割合 | 英 | 仏 | 蘭 | 葡 |')
    md.append('|---|---|---|---|---|---|---|---|---|---|---|---|---|---|')
    for win in WIN:
        for ck in CONV:
            col = collections.defaultdict(list)
            for sd in SEEDS:
                inc = win_nation_income(sd, win, ck)
                wm = world_mean(inc)
                if not wm == wm:
                    continue
                rs = {n: v['earn'] * v['conv'] / v['size'] / wm for n, v in inc.items() if v['size'] >= 100.0}
                vals = list(rs.values())
                col['n'].append(len(vals))
                for k, p in (('p10', .1), ('p25', .25), ('p50', .5), ('p75', .75), ('p90', .9)):
                    col[k].append(pctile(vals, p))
                col['lt'].append(sum(1 for v in vals if v < 0.5) / len(vals)); col['gt'].append(sum(1 for v in vals if v > 1) / len(vals))
                for n, k in (('イギリス', 'uk'), ('フランス', 'fr'), ('ネーデルラント', 'nl'), ('ポルトガル', 'pt')):
                    col[k].append(rs.get(n, float('nan')))
            md.append(f"| {win} | {ck} | {cell(col['n'], 0)} | " + ' | '.join(cell(col[k], 2) for k in ('p10', 'p25', 'p50', 'p75', 'p90')) + f" | {cell(col['lt'], 0, pct=True)} | {cell(col['gt'], 0, pct=True)} | " + ' | '.join(cell(col[k], 2) for k in ('uk', 'fr', 'nl', 'pt')) + ' |')
    md.append('')
    # --- 階級の r
    md.append('## 階級の r（階級の1人あたり所得×換算÷世界の人口加重平均）：英仏蘭葡と、国の r が下位4分位の国の平均（換算＝為替÷物価）')
    md.append('| 窓 | 国 | 貴族 | 資本家 | 商人 | 農民 | 工場労働者 | 使用人 |')
    md.append('|---|---|---|---|---|---|---|---|')
    for win in WIN:
        res = collections.defaultdict(list)
        for sd in SEEDS:
            inc = win_nation_income(sd, win, 'fx/P'); wm = world_mean(inc)
            if not wm == wm:
                continue
            rs = {n: v['earn'] * v['conv'] / v['size'] / wm for n, v in inc.items() if v['size'] >= 100.0}
            low = set(sorted(rs, key=lambda n: rs[n])[:max(1, len(rs) // 4)])
            for n, v in inc.items():
                grp = n if n in NATIONS else ('下位4分位の国(平均)' if n in low else None)
                if not grp:
                    continue
                for c, lab in ((0, 'n'), (3, 'c'), (10, 'm'), (1, 'p'), (2, 'w'), (12, 's')):
                    x = v['cls'].get(c)
                    if x and x[1] > 0 and x[0] > 0:
                        res[(grp, lab)].append(x[0] / x[1] * v['conv'] / wm)
        for grp in NATIONS + ['下位4分位の国(平均)']:
            md.append(f"| {win} | {grp} | " + ' | '.join(cell(res[(grp, l)], 2) for l in 'ncmpws') + ' |')
    md.append('')
    # --- 静的な見込み
    md.append('## 静的な見込み：奢侈品の需要に f を掛ける（価格・供給・予算の切れを無視した**上限**の見込み）')
    md.append('f の形：「r^ε」＝平均より貧しければ減る・「max(1,r)^ε」＝平均以下は変えない。r は「国」＝国の r を全階級に当てる／「階級」＝階級ごとの r。換算＝為替÷物価。')
    md.append('指標：①奢侈品の支払いの変化（国全体・％）②国の所得に対する奢侈品の支払いの変化（ポイント）③専売の国庫の取り分の変化÷国の歳入の基数（nation_s の revenueBaseMeanPerMonth・ポイント）。')
    md.append('')
    md.append('### 英仏蘭葡')
    md.append('| 窓 | 国 | 型 | r | ε | ①奢侈品の支払いの変化 | ②所得に対する変化(pt) | ③専売の取り分の変化÷歳入(pt) | 参考：奢侈品の支払い÷所得 | 参考：専売の取り分÷歳入 |')
    md.append('|---|---|---|---|---|---|---|---|---|---|')
    worlds = collections.defaultdict(list)
    shape = {'r^ε': lambda r, e: r ** e, 'max(1,r)^ε': lambda r, e: max(1.0, r) ** e}
    for win in WIN:
        res = collections.defaultdict(list)
        for sd in SEEDS:
            inc = win_nation_income(sd, win, 'fx/P'); wm = world_mean(inc)
            if not wm == wm:
                continue
            SP = SPEND[sd]; a, b, bk = WIN[win]
            NAT = NAT_ALL[sd]
            for n, v in inc.items():
                if v['size'] < 100.0:
                    continue
                # 窓の階級ごとの奢侈品の支払い・専売の取り分（区切りを月数で重みづけ）
                lux = collections.defaultdict(float); take_lux = collections.defaultdict(float); take_all = 0.0; mo = 0.0
                for bkt in bk:
                    e = SP.get((n, bkt))
                    if not e or not e['months'] == e['months']:
                        continue
                    m = e['months']; mo += m
                    for (c, nm, tier, pay, qty, rec, take) in e['good']:
                        if take == take:
                            take_all += take * m
                            if tier == 'Luxury':
                                take_lux[c] += take * m
                        if tier == 'Luxury' and pay == pay:
                            lux[c] += pay * m
                    for (c, tier, pay, take) in e['tier']:
                        if take == take:
                            take_all += take * m
                if mo <= 0:
                    continue
                lux = {c: x / mo for c, x in lux.items()}; take_lux = {c: x / mo for c, x in take_lux.items()}; take_all /= mo
                lux_tot = sum(lux.values())
                rev = statistics.mean(NAT[(n, q)]['revenueBaseMeanPerMonth'] for q in range(a, b + 1) if (n, q) in NAT and NAT[(n, q)]['revenueBaseMeanPerMonth'] == NAT[(n, q)]['revenueBaseMeanPerMonth']) if any((n, q) in NAT and NAT[(n, q)]['revenueBaseMeanPerMonth'] == NAT[(n, q)]['revenueBaseMeanPerMonth'] for q in range(a, b + 1)) else float('nan')
                r_n = v['earn'] * v['conv'] / v['size'] / wm
                for form, fn in shape.items():
                    for eps in (0.25, 0.5, 1.0):
                        for kind in ('国', '階級'):
                            dl = dt = 0.0
                            for c in set(lux) | set(take_lux):
                                x = v['cls'].get(c)
                                if kind == '国':
                                    r = r_n
                                else:
                                    if not (x and x[1] > 0 and x[0] > 0):
                                        continue
                                    r = x[0] / x[1] * v['conv'] / wm
                                f = fn(r, eps)
                                dl += lux.get(c, 0.0) * (f - 1.0); dt += take_lux.get(c, 0.0) * (f - 1.0)
                            key = (win, form, kind, eps)
                            vals = (dl / lux_tot if lux_tot > 0 else float('nan'), dl / v['earn'], dt / rev if rev == rev and rev > 0 else float('nan'))
                            if n in NATIONS:
                                res[(n,) + key].append(vals)
                                res[(n, 'base', win)].append((lux_tot / v['earn'], take_all / rev if rev == rev and rev > 0 else float('nan'), dict(take_lux=sum(take_lux.values()) / rev if rev == rev and rev > 0 else float('nan'))))
                            worlds[key].append(vals)
                            worlds[('seed', sd) + key].append(vals)
        for n in NATIONS:
            for form in shape:
                for kind in ('国', '階級'):
                    for eps in (0.25, 0.5, 1.0):
                        vs = res.get((n, win, form, kind, eps))
                        if not vs:
                            continue
                        base = res.get((n, 'base', win), [])
                        md.append(f"| {win} | {n} | {form} | {kind} | {eps} | {cell([x[0] for x in vs], 0, pct=True)} | {cell([x[1] for x in vs], 2, pct=True)} | {cell([x[2] for x in vs], 2, pct=True)} | {cell([x[0] for x in base], 2, pct=True)} | {cell([x[1] for x in base], 2, pct=True)}（うち奢侈品 {cell([x[2]['take_lux'] for x in base], 2, pct=True)}） |")
    md.append('')
    md.append('### 世界の国（人口100千人以上・全種×全国をまとめた分布）：ε=0.5')
    md.append('| 窓 | 型 | r | ①奢侈品の支払いの変化 p10 / 中央 / p90 | ②所得に対する変化(pt) p10 / 中央 / p90 | ③専売の取り分の変化÷歳入(pt) p10 / 中央 / p90 |')
    md.append('|---|---|---|---|---|---|')
    for win in WIN:
        for form in shape:
            for kind in ('国', '階級'):
                vs = worlds.get((win, form, kind, 0.5))
                if not vs:
                    continue
                def trio(i, pctflag=True):
                    xs = [x[i] for x in vs if x[i] == x[i]]
                    return ' / '.join(f'{pctile(xs, p)*100:.1f}%' for p in (.1, .5, .9)) if xs else '—'
                md.append(f"| {win} | {form} | {kind} | {trio(0)} | {trio(1)} | {trio(2)} |")
    open(os.path.join(OUT, 't_c_luxury_reference.md'), 'w', encoding='utf-8').write('\n'.join(md))


def t_c_quarterly():
    """(c) 国×四半期の r（nation_s の earnedTotalPerMonth÷populationK×換算÷世界の人口加重平均）。全ての国・全ての四半期・種ごとに r を出し、四半期ごとに種をまたいだ中央値で表す。
    世界の平均＝人口 100 千人以上の国の Σ(所得×換算)÷Σ人数。換算＝'fx/P'（為替÷物価）と 'fx'。TSV 2本（全 668 四半期）。"""
    for ck in ('fx/P', 'fx'):
        per_q = collections.defaultdict(lambda: collections.defaultdict(list))   # q → 列 → 種ごとの値
        for sd in SEEDS:
            NAT = NAT_ALL[sd]
            byq = collections.defaultdict(dict)
            for (n, q), v in NAT.items():
                byq[q][n] = v
            for q, d in byq.items():
                num = den = 0.0; rs = {}
                for n, v in d.items():
                    pk, e = v.get('populationK', float('nan')), v.get('earnedTotalPerMonth', float('nan'))
                    cv = CONV[ck](v.get('fxRate', float('nan')), v.get('priceLevel', float('nan')))
                    if pk == pk and e == e and cv == cv and pk >= 100.0 and e > 0 and cv > 0:
                        num += e * cv; den += pk
                if den <= 0:
                    continue
                wm = num / den
                for n, v in d.items():
                    pk, e = v.get('populationK', float('nan')), v.get('earnedTotalPerMonth', float('nan'))
                    cv = CONV[ck](v.get('fxRate', float('nan')), v.get('priceLevel', float('nan')))
                    if pk == pk and e == e and cv == cv and pk >= 100.0 and e > 0 and cv > 0:
                        rs[n] = e / pk * cv / wm
                vals = sorted(rs.values())
                if not vals:
                    continue
                pc = per_q[q]
                pc['n'].append(len(vals))
                for k, pp in (('p10', .1), ('p50', .5), ('p90', .9)):
                    pc[k].append(pctile(vals, pp))
                for n, k in (('イギリス', 'uk'), ('フランス', 'fr'), ('ネーデルラント', 'nl'), ('ポルトガル', 'pt')):
                    pc[k].append(rs.get(n, float('nan')))
        with open(os.path.join(OUT, 't_c_r_by_quarter_' + ck.replace('/', '_') + '.tsv'), 'w', encoding='utf-8') as fh:
            fh.write('quarter\tyear\tseeds\tnations\tp10\tp50\tp90\tUK\tFR\tNL\tPT\n')
            for q in sorted(per_q):
                pc = per_q[q]
                fh.write(f"{q}\t{1783 + q // 4}\t{len(pc['n'])}\t{med(pc['n']):.0f}\t" + '\t'.join(f"{med(pc[k]):.3f}" for k in ('p10', 'p50', 'p90', 'uk', 'fr', 'nl', 'pt')) + '\n')


if __name__ == '__main__':
    SPEND = {sd: load_spend(sd) for sd in SEEDS}
    NAT_ALL = {sd: load_nation(sd) for sd in SEEDS}
    NAT = NAT_ALL
    if 'a' in WHICH:
        FLOW = {sd: load_flow(sd) for sd in SEEDS}
        FISC = {}
        tg = 0
        for sd in SEEDS:
            FISC[sd], g = load_fiscal(sd); tg += g
        print('fiscal_s の monthIndex の途切れ:', tg)
        t_a()
    if 'b' in WHICH:
        CONS = {sd: load_cons(sd) for sd in SEEDS}
        t_b()
        # 本土の州だけ（植民地格を除く）＝植民地は資本家の貯蓄が0で希望だけが大きい州が多いので、本土どうしの食い違いも別に出す
        HOME = {sd: {n: [r for r in rows if r['tier'] != 'Colonial'] for n, rows in CONS[sd].items()} for sd in SEEDS}
        t_b(HOME, 't_b_region_skew_home.md', '＜本土の州だけ（植民地格を除く）＞')
    if 'c' in WHICH:
        t_c()
        t_c_quarterly()
