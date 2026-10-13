"""(299) 線(3) 奢侈品の所得弾力の読み手（読むだけ・挙動は変えない）。
  ⚠(301) main には (299) のコードが入っていない（B は採らなかった＝記録だけ）＝nation_s の luxuryIncomeRatio・crimeRateWorstRegionPerMonth の列は枝 l3 の木（1e20451 以後）でだけ出る。main の木の出力では動かない。
事前登録 measurements/l3_20261013_prereg.txt の [4][5]。
  python an_l3.py A=<ins> B=<ins> [C=<ins> ...]   （先頭の腕が基線 A・採否は B を A に対して読む）
  入力＝`INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins
        nation_s（全ての国・四半期）・class_s（挙げた国は四半期ごと）・spend_s（全ての国・窓の区切りごとの階級×財）・goods_s
  窓（四半期の番号 q は 0 始まり・表の Q は q+1）：Q1-8＝q0..7・Q5-40＝q4..39・Q41-160＝q40..159・Q161-240＝q160..239・Q189-228＝q188..227・全期。
  spend_s の窓の区切り（Instruments.SpendBucketStart＝0・4・8・40・160・188・228・268・400）に合わせる：Q5-40＝区切り 1+2・Q41-160＝区切り 3・Q161-240 は区切りで揃わない＝spend_s の表は Q5-40・Q41-160・Q161-188（区切り 4）・Q189-228（区切り 5）を出す。
  ① 英の富裕層（貴族＋資本家）の貯蓄÷所得（月数）：class_s の貴族(0)と資本家(3)の savingsStock の和÷earnedPerMonth の和＝四半期ごと→窓の中央→種の中央。B÷A と B＜A の種の数。
  ② 貧国（r<1）の上位階級の奢侈品の支出：セルの集合は A の nation_s から作った r（earnedTotalPerMonth×fxRate÷priceLevel÷populationK を国の人口加重平均で割る）で窓内の平均が 1 未満。併記＝B の注入値 luxuryIncomeRatio で切った集合。
  ③ 英の専売と間接税の歳入÷年GDP：nation_s の indirectRevenueMeanPerMonth×12÷nominalGdpPerYear の窓の中央→種の中央。B÷A。
  記録：注入された r（英・全国）・奢侈品の価格・世界の奢侈品の生産・大衆の生活水準（needMetShare・foodMetShare・supplyFillShare）・窃盗（crimeRateWorstRegionPerMonth）。
  財は名前で引く（番号を書かない）。計器の限界の自己申告：①spend_s の窓の月数は四半期×3の和②class_s の savingsStock は四半期の終わりの在庫・earnedPerMonth は四半期の月平均③挙げた国（英仏蘭葡）以外の spend_s は 6 階級（貴族・農民・工場労働者・資本家・商人・使用人）だけ。"""
import sys, os, glob, re, statistics, collections, math
sys.stdout.reconfigure(encoding='utf-8')
UK = 'イギリス'
ARMS = collections.OrderedDict(a.split('=', 1) for a in sys.argv[1:])
NAMES = list(ARMS)
BASE = NAMES[0]
WIN = [('Q1-8', 0, 7), ('Q5-40', 4, 39), ('Q41-160', 40, 159), ('Q161-240', 160, 239), ('Q189-228', 188, 227), ('全期', 0, 10**6)]
SPEND_WIN = [('Q5-40', (1, 2)), ('Q41-160', (3,)), ('Q161-188', (4,)), ('Q189-228', (5,))]
NOBLE, CAPITALIST = 0, 3


def f(x):
    try:
        return float(x)
    except ValueError:
        return float('nan')


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def mean(xs):
    xs = [x for x in xs if x == x]
    return sum(xs) / len(xs) if xs else float('nan')


def cell(xs, nd=3):
    ys = [x for x in xs if x == x]
    return '—' if not ys else f'{statistics.median(ys):.{nd}f} ({min(ys):.{nd}f}〜{max(ys):.{nd}f})'


def rows(path):
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            yield dict(zip(head, ln.rstrip('\n').split('\t')))


def seed_of(p, stem):
    return int(re.search(stem + r'(\d+)\.tsv$', os.path.basename(p)).group(1))


def load_nation(D):
    """seed -> nation -> q -> {列: 数}"""
    out = {}
    for p in sorted(glob.glob(os.path.join(D, 'nation_s*.tsv'))):
        sd = seed_of(p, 'nation_s'); d = out.setdefault(sd, collections.defaultdict(dict))
        for r in rows(p):
            q = int(r['quarter'])
            d[r['nation']][q] = {k: f(v) for k, v in r.items() if k not in ('nation', 'year')}
    return out


def load_class(D):
    """seed -> q(0始まり) -> {classId: (sizeK, savingsStock, earnedPerMonth)}（英のみ・四半期ごとの行）"""
    out = {}
    for p in sorted(glob.glob(os.path.join(D, 'class_s*.tsv'))):
        sd = seed_of(p, 'class_s'); d = out.setdefault(sd, collections.defaultdict(dict))
        for r in rows(p):
            if r['nation'] != UK:
                continue
            t = r['time']
            m = re.fullmatch(r'[Qq]?(\d+)', t)
            if not m:
                continue   # 年末の行など（四半期の番号でない時刻）は飛ばす
            d[int(m.group(1))][int(r['classId'])] = (f(r['sizeK']), f(r['savingsStock']), f(r['earnedPerMonth']))
    return out


def load_spend(D):
    """seed -> (nation, bucket) -> {'months', 'lux': {classId: (pay, qty)}}（kind=good・tier=Luxury のみ）"""
    out = {}
    for p in sorted(glob.glob(os.path.join(D, 'spend_s*.tsv'))):
        sd = seed_of(p, 'spend_s'); d = out.setdefault(sd, {})
        for r in rows(p):
            if r['kind'] != 'good' or r['tier'] != 'Luxury':
                continue
            e = d.setdefault((r['nation'], int(r['bucket'])), {'months': f(r['monthsCount']), 'lux': collections.defaultdict(lambda: [0.0, 0.0])})
            c = int(r['classId']); e['lux'][c][0] += f(r['payPerMonth']); e['lux'][c][1] += f(r['qtyPerMonth'])
    return out


def window_vals(rowsq, a, b, key):
    return [rowsq[q][key] for q in sorted(rowsq) if a <= q <= b and key in rowsq[q]]


def fx_over_p(r):
    return r['fxRate'] / r['priceLevel'] if r.get('priceLevel', 0) == r.get('priceLevel', 0) and r.get('priceLevel', 0) > 0 else float('nan')


def r_from_nation(N, a, b):
    """A の nation_s から作った r：窓内の四半期ごとに（国の1人あたり実質所得）÷（人口加重の世界平均）→国ごとに窓の平均。seed の N は nation -> q -> 行。"""
    qs = sorted({q for n in N for q in N[n] if a <= q <= b})
    acc = collections.defaultdict(list)
    for q in qs:
        inc = {}; pop = {}
        for n in N:
            r = N[n].get(q)
            if not r:
                continue
            c = fx_over_p(r)
            e = r.get('earnedTotalPerMonth', float('nan')); pk = r.get('populationK', float('nan'))
            if c == c and e == e and pk == pk and pk > 0:
                inc[n] = e * c; pop[n] = pk
        S = sum(pop.values())
        if not (S > 0):
            continue
        Y = sum(inc.values()) / S
        if not (Y > 0):
            continue
        for n in inc:
            acc[n].append(inc[n] / pop[n] / Y)
    return {n: mean(v) for n, v in acc.items()}


NATD = {a: load_nation(D) for a, D in ARMS.items()}
CLSD = {a: load_class(D) for a, D in ARMS.items()}
SPD = {a: load_spend(D) for a, D in ARMS.items()}
SEEDS = sorted(set.intersection(*[set(NATD[a]) for a in ARMS]))
print(f'# (299) 線(3) 読み手の出力　腕 {NAMES}　種 {SEEDS}')
for a, D in ARMS.items():
    print(f'  {a} = {D}')

# ---------------------------------------------------------------- 注入された r（記録）
print('\n## 注入された r（nation_s の luxuryIncomeRatio・窓の中央→種の中央〔最小〜最大〕）')
for a in NAMES:
    if 'luxuryIncomeRatio' not in next(iter(next(iter(NATD[a].values())).values())).get(0, {}) and True:
        pass
print('| 腕 | 窓 | 英の r | 全国の人口加重平均の r | r<1 の国の割合（人口 100 千人以上） |')
print('|---|---|---|---|---|')
for a in NAMES:
    for name, lo, hi in WIN:
        uk, wm, lt = [], [], []
        for sd in SEEDS:
            N = NATD[a][sd]
            uk.append(med(window_vals(N[UK], lo, hi, 'luxuryIncomeRatio')))
            qs = sorted({q for n in N for q in N[n] if lo <= q <= hi})
            ws = []; ls = []
            for q in qs:
                num = den = 0.0; below = tot = 0
                for n in N:
                    r = N[n].get(q)
                    if not r or not (r.get('populationK', 0) > 0) or r.get('luxuryIncomeRatio', float('nan')) != r.get('luxuryIncomeRatio', float('nan')):
                        continue
                    num += r['luxuryIncomeRatio'] * r['populationK']; den += r['populationK']
                    if r['populationK'] >= 100:
                        tot += 1; below += r['luxuryIncomeRatio'] < 1
                if den > 0:
                    ws.append(num / den)
                if tot > 0:
                    ls.append(below / tot)
            wm.append(med(ws)); lt.append(med(ls))
        print(f'| {a} | {name} | {cell(uk)} | {cell(wm)} | {cell(lt, 2)} |')

# ---------------------------------------------------------------- ① 英の富裕層の貯蓄÷所得
print('\n## ① 英の富裕層（貴族＋資本家）の貯蓄÷所得（月数・窓の中央→種の中央〔最小〜最大〕）と、B÷A・B＜A の種の数')
print('| 窓 | ' + ' | '.join(NAMES) + ' | ' + ' | '.join(f'{b}÷{BASE}（中央）・{b}＜{BASE} の種' for b in NAMES[1:]) + ' |')
print('|---|' + '---|' * (len(NAMES) + len(NAMES) - 1))
L1 = {}
for name, lo, hi in WIN:
    per = {}
    for a in NAMES:
        v = []
        for sd in SEEDS:
            C = CLSD[a].get(sd, {})
            xs = []
            for q in sorted(C):
                if lo <= q <= hi and NOBLE in C[q] and CAPITALIST in C[q]:
                    sav = C[q][NOBLE][1] + C[q][CAPITALIST][1]; ear = C[q][NOBLE][2] + C[q][CAPITALIST][2]
                    xs.append(sav / ear if ear == ear and ear > 0 else float('nan'))
            v.append(med(xs))
        per[a] = v
    cols = [cell(per[a], 1) for a in NAMES]
    cmp_ = []
    for b in NAMES[1:]:
        ratio = med(per[b]) / med(per[BASE]) if med(per[BASE]) else float('nan')
        lower = sum(1 for x, y in zip(per[b], per[BASE]) if x == x and y == y and x < y)
        n = sum(1 for x, y in zip(per[b], per[BASE]) if x == x and y == y)
        cmp_.append(f'{ratio:.3f}・{lower}/{n}')
        L1[(name, b)] = (ratio, lower, n)
    print(f'| {name} | ' + ' | '.join(cols) + ' | ' + ' | '.join(cmp_) + ' |')

# ---------------------------------------------------------------- ③ 英の専売と間接税の歳入÷年GDP
print('\n## ③ 英の専売と間接税（関税＋専売益）÷年GDP（nation_s の indirectRevenueMeanPerMonth×12÷nominalGdpPerYear・窓の中央→種の中央）と B÷A')
print('| 窓 | ' + ' | '.join(NAMES) + ' | ' + ' | '.join(f'{b}÷{BASE}' for b in NAMES[1:]) + ' |')
print('|---|' + '---|' * (len(NAMES) + len(NAMES) - 1))
for name, lo, hi in WIN:
    per = {}
    for a in NAMES:
        v = []
        for sd in SEEDS:
            N = NATD[a][sd][UK]
            xs = [N[q]['indirectRevenueMeanPerMonth'] * 12 / N[q]['nominalGdpPerYear'] for q in sorted(N) if lo <= q <= hi and N[q].get('nominalGdpPerYear', 0) > 0]
            v.append(med(xs))
        per[a] = v
    print(f'| {name} | ' + ' | '.join(cell(per[a], 4) for a in NAMES) + ' | ' + ' | '.join(f'{med(per[b]) / med(per[BASE]):.3f}' if med(per[BASE]) else '—' for b in NAMES[1:]) + ' |')

# ---------------------------------------------------------------- ② 貧国の上位階級の奢侈品の支出
print('\n## ② 貧国（r<1）の上位階級（貴族＋資本家）の奢侈品の支出（spend_s の kind=good・tier=Luxury の payPerMonth の和）：セル（種, 国）で B＜A の割合')
for b in NAMES[1:]:
    print(f'### {b} 対 {BASE}')
    print('| 窓 | 集合の作り方 | セル数 | B＜A のセル | B＜A の割合 | 支出の変化（B÷A・セルの中央） | 数量の変化（セルの中央） |')
    print('|---|---|---|---|---|---|---|')
    for wname, buckets in SPEND_WIN:
        lo = {1: 4, 2: 4, 3: 40, 4: 160, 5: 188}[buckets[0]]; hi = {1: 7, 2: 39, 3: 159, 4: 187, 5: 227}[buckets[-1]]
        if wname == 'Q5-40':
            lo, hi = 4, 39
        for how in ('A の nation_s から作った r', 'B の注入値 luxuryIncomeRatio'):
            n_c = lower = 0; chg = []; qchg = []
            for sd in SEEDS:
                if how.startswith('A'):
                    rmap = r_from_nation(NATD[BASE][sd], lo, hi)
                else:
                    rmap = {}
                    for n, qd in NATD[b][sd].items():
                        v = [qd[q]['luxuryIncomeRatio'] for q in qd if lo <= q <= hi and qd[q].get('populationK', 0) > 0]
                        rmap[n] = mean(v)
                for n, rr in rmap.items():
                    if not (rr == rr and rr < 1.0):
                        continue
                    def tot(arm, k):
                        s = 0.0; m = 0.0; ok = False
                        for bk in buckets:
                            e = SPD[arm].get(sd, {}).get((n, bk))
                            if e is None:
                                continue
                            ok = True
                            for c in (NOBLE, CAPITALIST):
                                if c in e['lux']:
                                    s += e['lux'][c][k] * e['months']
                            m += e['months']
                        return s if ok else float('nan')
                    pa, pb = tot(BASE, 0), tot(b, 0)
                    qa, qb = tot(BASE, 1), tot(b, 1)
                    if pa == pa and pb == pb and pa > 0:
                        n_c += 1; lower += pb < pa; chg.append(pb / pa)
                        if qa == qa and qb == qb and qa > 0:
                            qchg.append(qb / qa)
            share = f'{lower / n_c:.2f}' if n_c else '—'
            print(f'| {wname} | {how} | {n_c} | {lower} | {share} | {med(chg):.3f} | {med(qchg):.3f} |')

# ---------------------------------------------------------------- 記録
print('\n## 記録：大衆の生活水準・窃盗（nation_s・窓の中央→種の中央）')
KEYS = [('needMetShare', 3), ('foodMetShare', 3), ('supplyFillShare', 3), ('crimeRateWorstRegionPerMonth', 5)]
for k, nd in KEYS:
    print(f'### {k}（英 ／ 全国の中央）')
    print('| 窓 | ' + ' | '.join(NAMES) + ' |')
    print('|---|' + '---|' * len(NAMES))
    for name, lo, hi in WIN:
        out = []
        for a in NAMES:
            ukv = []; wv = []
            for sd in SEEDS:
                N = NATD[a][sd]
                ukv.append(med(window_vals(N[UK], lo, hi, k)))
                per = [med(window_vals(N[n], lo, hi, k)) for n in N if N[n]]
                wv.append(med(per))
            out.append(f'{cell(ukv, nd)} ／ {cell(wv, nd)}')
        print(f'| {name} | ' + ' | '.join(out) + ' |')
