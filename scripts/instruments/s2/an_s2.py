"""(302) 線(2)(3) 奢侈品と家事奉仕の所得弾力＋使用人の産出の読み手（読むだけ・挙動は変えない）。(299) の l3/an_l3.py から作った。
  ⚠(304) main には (302) のコードが入っていない（D は採らなかった＝記録だけ）＝nation_s の worldRealIncomePerCapitaPerMonth・crimeRateWorstRegionPerMonth の列は枝 s2 の木（5784ca9 以後）でだけ出る。main の木の出力では動かない。
事前登録 measurements/s2_20261013_prereg.txt の [4][5]。
  python an_s2.py A=<ins> D=<ins> [B=<ins> ...]   （先頭の腕が基線 A・採否は 2 番目の腕 D を A に対して読む）
  入力＝`INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins
        nation_s（全ての国・四半期）・class_s（挙げた国は四半期ごと）・svc_s（挙げた国の家事奉仕の市場）・spend_s（全ての国・窓の区切りごとの階級×財）
  窓（四半期の番号 q は 0 始まり・表の Q は q+1）：Q1-8＝q0..7・Q5-40＝q4..39・Q41-160＝q40..159・Q161-240＝q160..239・Q189-228＝q188..227・全期。
  ① 英の富裕層（貴族＋資本家）の貯蓄（分子・名目）＝class_s の savingsStock の和・分母（同じ窓の earnedPerMonth の和）・実質の分子（÷priceLevel）・貯蓄÷所得（月数）を分けて出す。D＜A の種の数と D÷A。
  ② 英の使用人 POP の数÷人口＝svc_s の servantK÷totalPopulationK（四半期の末の値）。D＞A の種の数。
  ③ 英の専売と間接税÷年GDP＝nation_s の indirectRevenueMeanPerMonth×12÷nominalGdpPerYear。D÷A。
  記録：貧国（r<1）の上位階級の奢侈品の支出（セルの集合は A の nation_s から作った r＝実質〔earnedTotalPerMonth÷priceLevel〕÷人口加重平均・為替は掛けない＝コードと同じ換算）・
        英の階級ごとの r（貴族・資本家・農民・使用人を別に）・使用人の賃金（人あたりの所得）と人数・大衆の生活水準・窃盗。
  財は名前で引く（番号を書かない）。計器の限界の自己申告：①spend_s の窓の月数は四半期×3の和②class_s の savingsStock は四半期の終わりの在庫・earnedPerMonth は四半期の月平均
        ③階級ごとの r は国×階級の集計（コードは州×階級の POP ごと・前の月の所得）の近似④ Y は D の nation_s の worldRealIncomePerCapitaPerMonth（四半期の終わりの月の配られた値）。
"""
import sys, os, glob, re, statistics, collections, math
sys.stdout.reconfigure(encoding='utf-8')
UK = 'イギリス'
ARMS = collections.OrderedDict(a.split('=', 1) for a in sys.argv[1:])
NAMES = list(ARMS)
BASE = NAMES[0]
WIN = [('Q1-8', 0, 7), ('Q5-40', 4, 39), ('Q41-160', 40, 159), ('Q161-240', 160, 239), ('Q189-228', 188, 227), ('全期', 0, 10**6)]
SPEND_WIN = [('Q5-40', (1, 2), 4, 39), ('Q41-160', (3,), 40, 159), ('Q161-188', (4,), 160, 187), ('Q189-228', (5,), 188, 227)]
NOBLE, PEASANT, CAPITALIST, SERVANT = 0, 1, 3, 12


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
            m = re.fullmatch(r'[Qq]?(\d+)', r['time'])
            if not m:
                continue   # 年末の行など（四半期の番号でない時刻）は飛ばす
            d[int(m.group(1))][int(r['classId'])] = (f(r['sizeK']), f(r['savingsStock']), f(r['earnedPerMonth']))
    return out


def load_svc(D):
    """seed -> q -> {列: 数}（英の家事奉仕の市場・四半期の末の月）"""
    out = {}
    for p in sorted(glob.glob(os.path.join(D, 'svc_s*.tsv'))):
        sd = seed_of(p, 'svc_s'); d = out.setdefault(sd, {})
        for r in rows(p):
            if r['nation'] != UK:
                continue
            d[int(r['quarter'])] = {k: f(v) for k, v in r.items() if k not in ('nation', 'year')}
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


def real_factor(r):
    """実質＝名目÷物価の 1 本（為替は掛けない＝コードの DistributeWorldRealIncome と同じ換算）。"""
    p = r.get('priceLevel', float('nan'))
    return 1.0 / p if p == p and p > 0 else float('nan')


def r_from_nation(N, a, b):
    """A の nation_s から作った r（国単位の近似）：窓内の四半期ごとに（国の1人あたり実質所得）÷（人口加重の世界平均）→国ごとに窓の平均。"""
    qs = sorted({q for n in N for q in N[n] if a <= q <= b})
    acc = collections.defaultdict(list)
    for q in qs:
        inc = {}; pop = {}
        for n in N:
            r = N[n].get(q)
            if not r:
                continue
            c = real_factor(r)
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


def per_seed(fn, arm):
    return [fn(sd) for sd in SEEDS]


def compare(xs, ys, higher=False):
    """種ごとの比較：(中央どうしの比, 腕が基線より低い〔higher なら高い〕種の数, 比べられた種の数)"""
    ok = [(x, y) for x, y in zip(xs, ys) if x == x and y == y]
    n = len(ok)
    cnt = sum(1 for x, y in ok if (x > y if higher else x < y))
    mx, my = med(xs), med(ys)
    return (mx / my if my else float('nan')), cnt, n


NATD = {a: load_nation(D) for a, D in ARMS.items()}
CLSD = {a: load_class(D) for a, D in ARMS.items()}
SVCD = {a: load_svc(D) for a, D in ARMS.items()}
SPD = {a: load_spend(D) for a, D in ARMS.items()}
SEEDS = sorted(set.intersection(*[set(NATD[a]) for a in ARMS]))
print(f'# (302) 線(2)(3) 読み手の出力　腕 {NAMES}　種 {SEEDS}（{len(SEEDS)} 種）')
for a, D in ARMS.items():
    print(f'  {a} = {D}')
OTH = NAMES[1:]

# ---------------------------------------------------------------- 配られた世界の値（記録）
print('\n## 配られた世界の1人あたり実質所得（nation_s の worldRealIncomePerCapitaPerMonth・英の行・窓の中央→種の中央〔最小〜最大〕）')
print('| 腕 | ' + ' | '.join(n for n, _, _ in WIN) + ' |')
print('|---|' + '---|' * len(WIN))
for a in NAMES:
    cells = []
    for name, lo, hi in WIN:
        cells.append(cell([med(window_vals(NATD[a][sd][UK], lo, hi, 'worldRealIncomePerCapitaPerMonth')) for sd in SEEDS], 3))
    print(f'| {a} | ' + ' | '.join(cells) + ' |')

# ---------------------------------------------------------------- 英の階級ごとの r（記録）
print('\n## 記録：英の階級ごとの r の近似＝（class_s の earnedPerMonth÷priceLevel÷sizeK）÷Y（Y は各腕の worldRealIncomePerCapitaPerMonth・0 の腕は —）。窓の中央→種の中央')
print('| 腕 | 窓 | 貴族 | 資本家 | 農民 | 使用人 |')
print('|---|---|---|---|---|---|')
for a in OTH:
    for name, lo, hi in WIN:
        cells = []
        for cid in (NOBLE, CAPITALIST, PEASANT, SERVANT):
            v = []
            for sd in SEEDS:
                C = CLSD[a].get(sd, {}); N = NATD[a][sd][UK]
                xs = []
                for q in sorted(C):
                    if lo <= q <= hi and cid in C[q] and q in N:
                        sz, _, ear = C[q][cid]; pl = N[q].get('priceLevel', float('nan')); Y = N[q].get('worldRealIncomePerCapitaPerMonth', 0.0)
                        if sz > 0 and pl > 0 and Y > 0:
                            xs.append(ear / pl / sz / Y)
                v.append(med(xs))
            cells.append(cell(v, 2))
        print(f'| {a} | {name} | ' + ' | '.join(cells) + ' |')

# ---------------------------------------------------------------- ① 英の富裕層の貯蓄（分子）・所得（分母）・貯蓄÷所得
def l1_series(a, sd, lo, hi, kind):
    C = CLSD[a].get(sd, {}); N = NATD[a][sd][UK]
    xs = []
    for q in sorted(C):
        if not (lo <= q <= hi and NOBLE in C[q] and CAPITALIST in C[q]):
            continue
        sav = C[q][NOBLE][1] + C[q][CAPITALIST][1]; ear = C[q][NOBLE][2] + C[q][CAPITALIST][2]
        pl = N.get(q, {}).get('priceLevel', float('nan'))
        if kind == 'sav':
            xs.append(sav)
        elif kind == 'earn':
            xs.append(ear)
        elif kind == 'savReal':
            xs.append(sav / pl if pl == pl and pl > 0 else float('nan'))
        elif kind == 'ratio':
            xs.append(sav / ear if ear == ear and ear > 0 else float('nan'))
    return med(xs)


print('\n## ① 英の富裕層（貴族＋資本家）：貯蓄（分子・名目）／所得（分母・月の流れ）／貯蓄（実質＝÷priceLevel）／貯蓄÷所得（月数）。窓の中央→種の中央〔最小〜最大〕。右端＝腕÷A（種ごとの窓の中央の中央どうしの比）・腕＜A の種')
L1 = {}
for kind, label, nd in (('sav', '貯蓄（分子・名目）', 0), ('earn', '所得（分母・earnedPerMonth の和）', 1), ('savReal', '貯蓄（実質）', 0), ('ratio', '貯蓄÷所得（月数）', 1)):
    print(f'### {label}')
    print('| 窓 | ' + ' | '.join(NAMES) + ' | ' + ' | '.join(f'{b}÷{BASE}・{b}＜{BASE} の種' for b in OTH) + ' |')
    print('|---|' + '---|' * (len(NAMES) + len(OTH)))
    for name, lo, hi in WIN:
        per = {a: [l1_series(a, sd, lo, hi, kind) for sd in SEEDS] for a in NAMES}
        cmp_ = []
        for b in OTH:
            ratio, cnt, n = compare(per[b], per[BASE])
            cmp_.append(f'{ratio:.3f}・{cnt}/{n}')
            L1[(kind, name, b)] = (ratio, cnt, n)
        print(f'| {name} | ' + ' | '.join(cell(per[a], nd) for a in NAMES) + ' | ' + ' | '.join(cmp_) + ' |')

# ---------------------------------------------------------------- ② 英の使用人 POP の数÷人口
def svc_share(a, sd, lo, hi):
    S = SVCD[a].get(sd, {})
    xs = [S[q]['servantK'] / S[q]['totalPopulationK'] for q in sorted(S) if lo <= q <= hi and S[q].get('totalPopulationK', 0) > 0]
    return med(xs)


print('\n## ② 英の使用人 POP の数÷人口（svc_s の servantK÷totalPopulationK・窓の中央→種の中央〔最小〜最大〕）。右端＝腕÷A・腕＞A の種。参照＝イングランド・ウェールズ 1871 年 約 5.4%（(286)・【推】）')
print('| 窓 | ' + ' | '.join(NAMES) + ' | ' + ' | '.join(f'{b}÷{BASE}・{b}＞{BASE} の種' for b in OTH) + ' |')
print('|---|' + '---|' * (len(NAMES) + len(OTH)))
L2 = {}
for name, lo, hi in WIN:
    per = {a: [svc_share(a, sd, lo, hi) for sd in SEEDS] for a in NAMES}
    cmp_ = []
    for b in OTH:
        ratio, cnt, n = compare(per[b], per[BASE], higher=True)
        cmp_.append(f'{ratio:.3f}・{cnt}/{n}')
        L2[(name, b)] = (ratio, cnt, n)
    print(f'| {name} | ' + ' | '.join(cell(per[a], 4) for a in NAMES) + ' | ' + ' | '.join(cmp_) + ' |')

# ---------------------------------------------------------------- 使用人の賃金・人数・価格（記録）
print('\n## 記録：英の家事奉仕の市場（svc_s）と使用人の所得（class_s）')
for col, label, nd in (('servantK', '使用人の人数（千人）', 1), ('servantOutputPerMonth', '使用人の産出（月）', 1), ('soldPerMonth', '売れた量（月）', 1), ('shortagePerMonth', '不足（月）', 2)):
    print(f'### {label}')
    print('| 窓 | ' + ' | '.join(NAMES) + ' |')
    print('|---|' + '---|' * len(NAMES))
    for name, lo, hi in WIN:
        print(f'| {name} | ' + ' | '.join(cell([med(window_vals(SVCD[a].get(sd, {}), lo, hi, col)) for sd in SEEDS], nd) for a in NAMES) + ' |')
print('### 使用人の人あたりの所得（class_s の earnedPerMonth÷sizeK・名目 ／ 実質＝÷priceLevel）')
print('| 窓 | ' + ' | '.join(NAMES) + ' |')
print('|---|' + '---|' * len(NAMES))
for name, lo, hi in WIN:
    out = []
    for a in NAMES:
        nom = []; real = []
        for sd in SEEDS:
            C = CLSD[a].get(sd, {}); N = NATD[a][sd][UK]
            xn = []; xr = []
            for q in sorted(C):
                if lo <= q <= hi and SERVANT in C[q] and C[q][SERVANT][0] > 0:
                    w = C[q][SERVANT][2] / C[q][SERVANT][0]; xn.append(w)
                    pl = N.get(q, {}).get('priceLevel', float('nan')); xr.append(w / pl if pl == pl and pl > 0 else float('nan'))
            nom.append(med(xn)); real.append(med(xr))
        out.append(f'{cell(nom, 2)} ／ {cell(real, 2)}')
    print(f'| {name} | ' + ' | '.join(out) + ' |')

# ---------------------------------------------------------------- ③ 英の専売と間接税の歳入÷年GDP
print('\n## ③ 英の専売と間接税（関税＋専売益）÷年GDP（nation_s の indirectRevenueMeanPerMonth×12÷nominalGdpPerYear・窓の中央→種の中央）と 腕÷A')
print('| 窓 | ' + ' | '.join(NAMES) + ' | ' + ' | '.join(f'{b}÷{BASE}' for b in OTH) + ' |')
print('|---|' + '---|' * (len(NAMES) + len(OTH)))
L3 = {}
for name, lo, hi in WIN:
    per = {}
    for a in NAMES:
        v = []
        for sd in SEEDS:
            N = NATD[a][sd][UK]
            v.append(med([N[q]['indirectRevenueMeanPerMonth'] * 12 / N[q]['nominalGdpPerYear'] for q in sorted(N) if lo <= q <= hi and N[q].get('nominalGdpPerYear', 0) > 0]))
        per[a] = v
    for b in OTH:
        L3[(name, b)] = med(per[b]) / med(per[BASE]) if med(per[BASE]) else float('nan')
    print(f'| {name} | ' + ' | '.join(cell(per[a], 4) for a in NAMES) + ' | ' + ' | '.join(f'{L3[(name, b)]:.3f}' for b in OTH) + ' |')

# ---------------------------------------------------------------- 英の奢侈品の支払い（貴族＋資本家）
print('\n## 記録：英の貴族＋資本家の奢侈品の支払い（spend_s・kind=good・tier=Luxury の payPerMonth の和）と、英の全階級の奢侈品の支払いに占める割合')
print('| 窓 | ' + ' | '.join(NAMES) + ' | ' + ' | '.join(f'{b}÷{BASE}（支払い・数量）' for b in OTH) + ' |')
print('|---|' + '---|' * (len(NAMES) + len(OTH)))
for wname, buckets, lo, hi in SPEND_WIN:
    def tot(arm, sd, k, classes):
        s = 0.0; ok = False
        for bk in buckets:
            e = SPD[arm].get(sd, {}).get((UK, bk))
            if e is None:
                continue
            ok = True
            for c, v in e['lux'].items():
                if classes is None or c in classes:
                    s += v[k] * e['months']
        return s if ok else float('nan')
    per = {a: [tot(a, sd, 0, (NOBLE, CAPITALIST)) for sd in SEEDS] for a in NAMES}
    perq = {a: [tot(a, sd, 1, (NOBLE, CAPITALIST)) for sd in SEEDS] for a in NAMES}
    cmp_ = [f'{med([y / x for x, y in zip(per[BASE], per[b]) if x == x and y == y and x > 0]):.3f}・{med([y / x for x, y in zip(perq[BASE], perq[b]) if x == x and y == y and x > 0]):.3f}' for b in OTH]
    print(f'| {wname} | ' + ' | '.join(cell(per[a], 0) for a in NAMES) + ' | ' + ' | '.join(cmp_) + ' |')

# ---------------------------------------------------------------- 記録：貧国（r<1）の上位階級の奢侈品の支出
print('\n## 記録（採否の理由にしない・r^ε<1 を掛けた作りの上の結果）：貧国（r<1）の上位階級（貴族＋資本家）の奢侈品の支出　セル（種, 国）で 腕＜A の割合')
for b in OTH:
    print(f'### {b} 対 {BASE}')
    print('| 窓 | セル数 | 腕＜A のセル | 割合 | 支出の変化（腕÷A・セルの中央） | 数量の変化（セルの中央） |')
    print('|---|---|---|---|---|---|')
    for wname, buckets, lo, hi in SPEND_WIN:
        n_c = lower = 0; chg = []; qchg = []
        for sd in SEEDS:
            rmap = r_from_nation(NATD[BASE][sd], lo, hi)
            for n, rr in rmap.items():
                if not (rr == rr and rr < 1.0):
                    continue
                def tot(arm, k):
                    s = 0.0; ok = False
                    for bk in buckets:
                        e = SPD[arm].get(sd, {}).get((n, bk))
                        if e is None:
                            continue
                        ok = True
                        for c in (NOBLE, CAPITALIST):
                            if c in e['lux']:
                                s += e['lux'][c][k] * e['months']
                    return s if ok else float('nan')
                pa, pb = tot(BASE, 0), tot(b, 0)
                qa, qb = tot(BASE, 1), tot(b, 1)
                if pa == pa and pb == pb and pa > 0:
                    n_c += 1; lower += pb < pa; chg.append(pb / pa)
                    if qa == qa and qb == qb and qa > 0:
                        qchg.append(qb / qa)
        share = f'{lower / n_c:.2f}' if n_c else '—'
        print(f'| {wname} | {n_c} | {lower} | {share} | {med(chg):.3f} | {med(qchg):.3f} |')

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
                wv.append(med([med(window_vals(N[n], lo, hi, k)) for n in N if N[n]]))
            out.append(f'{cell(ukv, nd)} ／ {cell(wv, nd)}')
        print(f'| {name} | ' + ' | '.join(out) + ' |')

# ---------------------------------------------------------------- 事前登録の線の判定の要約
print('\n## 事前登録の線の判定（主窓 Q41-160・D＝' + (OTH[0] if OTH else '—') + '）')
if OTH:
    b = OTH[0]
    r1, c1, n1 = L1[('sav', 'Q41-160', b)]
    r2, c2, n2 = L2[('Q41-160', b)]
    r3a, r3b = L3[('Q5-40', b)], L3[('Q41-160', b)]
    ok1 = (r1 == r1 and r1 <= 0.97 and c1 >= 7)
    ok2 = (r2 == r2 and r2 >= 1.10 and c2 >= 7)
    ok3 = (r3a == r3a and 0.90 <= r3a <= 1.50 and 0.90 <= r3b <= 1.50)
    print(f'- ① 貯蓄（分子）D÷A {r1:.3f}（線 ≦0.97）・D＜A の種 {c1}/{n1}（線 ≧7）→ {"合格" if ok1 else "不合格"}')
    print(f'- ② 使用人÷人口 D÷A {r2:.3f}（線 ≧1.10）・D＞A の種 {c2}/{n2}（線 ≧7）→ {"合格" if ok2 else "不合格"}')
    print(f'- ③ 専売と間接税÷年GDP D÷A　Q5-40 {r3a:.3f}・Q41-160 {r3b:.3f}（線 0.90〜1.50）→ {"合格" if ok3 else "不合格"}')
