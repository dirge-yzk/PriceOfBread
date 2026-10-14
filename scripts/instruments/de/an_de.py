"""(329) 需要の所得弾力（奢侈品と家事奉仕・階級単位）の読み手（読むだけ・挙動は変えない）。事前登録 measurements/de_20261014_prereg.txt の [4][8]。
取り込み元は scripts/instruments/s2/an_s2.py（枝 s2・(302)）だが、線が違う：旧の線〈富裕層の貯蓄が下がる〉ではなく、作者 2026-10-14 の裁定〈史実の向き〉。
  python an_de.py A=<ins> B=<ins> N1=<ins> N2=<ins> [L=<ins> ...]   （先頭の腕が基線 A・2 番目が B・名前が N1・N2 の腕はヌル腕＝雑音の床 δ を作る）
  入力＝`INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins
        nation_s（全ての国・四半期）・class_s（挙げた国は四半期ごと）・svc_s（挙げた国の家事奉仕の市場）・spend_s（全ての国・窓の区切りごとの階級×財）
  線（事前登録 [4]・主窓 Q41-160 と Q5-40）：
    H1 大衆（工場労働者＋農民）の家事奉仕の量の割合：B ≦ A−max(0.15, 2δ)・B＜A の種 ≥8
    H2 貴族の 1 人あたりの家事奉仕の量 B÷A ≧ 1+max(0.20, 2δ) かつ 中流（商人＋資本家）の 1 人あたり B÷A ≧ 1+max(0.20, 2δ)
    H3 英の使用人÷人口 B÷A ≧ 0.85（Q5-40 と Q41-160 の両方・上限なし＝事前登録の追記 2）。史実の参照 5.7%（英・ウェールズ 1871・二次）
    判定は H1・H2 が Q41-160 だけ（Q5-40 は参考）・H3 は Q5-40 と Q41-160 の両方。最後に 1 行にまとめる
    P8（記録・追記 2）貧国〔国全体の r＜1〕の貴族＋資本家と大衆の奢侈品の支払い B÷A（spend_s の全国）／ r≤0 の fallback の計数（nation_s の luxuryRatio* の列）
    H4（記録）奢侈品（Luxury 階層・アヘンを除く）の輸出額÷全輸出額が最大の国と上位 5 国（nation_s の luxuryExportsPerClearing÷exportsPerClearing）
  窓（四半期の番号 q は 0 始まり・表の Q は q+1）：nation_s・svc_s・class_s は Q1-8・Q5-40・Q41-160・Q161-240・Q189-228・全期。
        spend_s の窓の区切りは Instruments.SpendBucketStart＝[0,4,8,40,160,188,228,…]（構成の線 H1・H2 は Q5-40〔区切り 1・2〕・Q41-160〔3〕・Q161-188〔4〕・Q189-228〔5〕で読む＝Q1-8 と全期は無い）。
  財・階級は名前／定数で引く（財の番号を書かない）。計器の限界の自己申告：
    ①spend_s の qtyPerMonth は区切りの月の平均（家事奉仕の量＝役務の単位）・kind=class の qtyPerMonth は人数（千人）の月平均＝1 人あたり＝Σ(量×月数)÷Σ(人数×月数)
    ②class_s の savingsStock は四半期の終わりの在庫・earnedPerMonth は四半期の月平均
    ③階級ごとの r は国×階級の集計（コードは州×階級の POP ごと・前の月の所得）の近似
    ④H4 の輸出額は直近の清算 1 回分の世界価格建て（四半期の終わりの断面）＝割合は窓の四半期の中央。茶・絹織物・生糸・陶磁器は Luxury 階層の外（goods.json）で、この列に入らない
    ⑤種の数の線は 1/10 刻み＝δ が 1 種を下回ることはない
"""
import sys, os, glob, re, statistics, collections, math
sys.stdout.reconfigure(encoding='utf-8')
UK = 'イギリス'
ARMS = collections.OrderedDict(a.split('=', 1) for a in sys.argv[1:])
NAMES = list(ARMS)
BASE = NAMES[0]
NULLS = [n for n in NAMES[1:] if n.startswith('N')]
OTH = [n for n in NAMES[1:] if n not in NULLS]
WIN = [('Q1-8', 0, 7), ('Q5-40', 4, 39), ('Q41-160', 40, 159), ('Q161-240', 160, 239), ('Q189-228', 188, 227), ('全期', 0, 10**6)]
SPEND_WIN = [('Q5-40', (1, 2), 4, 39), ('Q41-160', (3,), 40, 159), ('Q161-188', (4,), 160, 187), ('Q189-228', (5,), 188, 227)]
NOBLE, PEASANT, WORKER, CAPITALIST, MERCHANT, SERVANT = 0, 1, 2, 3, 10, 12
MASS = (WORKER, PEASANT)
MIDDLE = (MERCHANT, CAPITALIST)
SERVICE_NAME = '家事奉仕'
HIST_SERVANT_SHARE = 0.057   # 英・ウェールズ 1871 年 130 万超÷2270 万（二次・事前登録 [2]）


def f(x):
    try:
        return float(x)
    except ValueError:
        return float('nan')


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


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
    """seed -> bucket -> {'months', 'qty': {classId: 量}, 'size': {classId: 人数}}（英・家事奉仕〔kind=good・slotName〕と階級の人数〔kind=class〕）"""
    out = {}
    for p in sorted(glob.glob(os.path.join(D, 'spend_s*.tsv'))):
        sd = seed_of(p, 'spend_s'); d = out.setdefault(sd, {})
        for r in rows(p):
            if r['nation'] != UK:
                continue
            if r['kind'] == 'good' and r['slotName'] == SERVICE_NAME:
                e = d.setdefault(int(r['bucket']), {'months': f(r['monthsCount']), 'qty': {}, 'size': {}})
                e['qty'][int(r['classId'])] = f(r['qtyPerMonth'])
            elif r['kind'] == 'class':
                e = d.setdefault(int(r['bucket']), {'months': f(r['monthsCount']), 'qty': {}, 'size': {}})
                e['size'][int(r['classId'])] = f(r['qtyPerMonth'])
    return out


def window_vals(rowsq, a, b, key):
    return [rowsq[q][key] for q in sorted(rowsq) if a <= q <= b and key in rowsq[q]]


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
print(f'# (329) 需要の所得弾力 読み手の出力　腕 {NAMES}　種 {SEEDS}（{len(SEEDS)} 種）　基線 {BASE}・ヌル腕 {NULLS}・読む腕 {OTH}')
for a, D in ARMS.items():
    print(f'  {a} = {D}')


def floor(valfn):
    """雑音の床 δ＝max(|N−A|)（腕の値＝種の中央）"""
    a = med([valfn(BASE, sd) for sd in SEEDS])
    ds = [abs(med([valfn(n, sd) for sd in SEEDS]) - a) for n in NULLS]
    ds = [d for d in ds if d == d]
    return max(ds) if ds else float('nan')


# ---------------------------------------------------------------- 構成（spend_s）
def comp_stats(arm, sd, buckets):
    """(大衆の割合, 貴族の1人あたり, 中流の1人あたり, 全階級の1人あたり)"""
    S = SPD[arm].get(sd, {})
    qty = collections.defaultdict(float); size = collections.defaultdict(float)
    for b in buckets:
        e = S.get(b)
        if e is None:
            return (float('nan'),) * 4
        m = e['months']
        for c, v in e['qty'].items():
            qty[c] += v * m
        for c, v in e['size'].items():
            size[c] += v * m
    tot = sum(qty.values())
    if not (tot > 0):
        return (float('nan'),) * 4
    mass = sum(qty[c] for c in MASS) / tot

    def pc(cs):
        s = sum(size[c] for c in cs)
        return sum(qty[c] for c in cs) / s if s > 0 else float('nan')
    allsz = sum(size.values())
    return (mass, pc((NOBLE,)), pc(MIDDLE), tot / allsz if allsz > 0 else float('nan'))


H = {}
print('\n## 構成の表（英・家事奉仕の量・spend_s）：階級ごとの量の割合（種の中央〔最小〜最大〕）')
for wname, buckets, lo, hi in SPEND_WIN:
    print(f'### {wname}')
    print('| 腕 | ' + ' | '.join(['工場労働者', '農民', '商人', '資本家', '貴族', '大衆（工場労働者＋農民）']) + ' |')
    print('|---|' + '---|' * 6)
    for a in NAMES:
        share = collections.defaultdict(list)
        for sd in SEEDS:
            S = SPD[a].get(sd, {})
            qty = collections.defaultdict(float)
            ok = True
            for b in buckets:
                e = S.get(b)
                if e is None:
                    ok = False
                    break
                for c, v in e['qty'].items():
                    qty[c] += v * e['months']
            tot = sum(qty.values())
            if ok and tot > 0:
                for c in (WORKER, PEASANT, MERCHANT, CAPITALIST, NOBLE):
                    share[c].append(qty[c] / tot)
                share['mass'].append((qty[WORKER] + qty[PEASANT]) / tot)
        print(f'| {a} | ' + ' | '.join(cell(share[c], 3) for c in (WORKER, PEASANT, MERCHANT, CAPITALIST, NOBLE, 'mass')) + ' |')

print('\n## 1 人あたりの家事奉仕の量（量÷人数・種の中央〔最小〜最大〕）と 腕÷A（種の中央どうしの比・腕＞A の種）')
for wname, buckets, lo, hi in SPEND_WIN:
    print(f'### {wname}')
    print('| 階級 | ' + ' | '.join(NAMES) + ' | ' + ' | '.join(f'{b}÷{BASE}・{b}＞{BASE} の種' for b in OTH) + ' |')
    print('|---|' + '---|' * (len(NAMES) + len(OTH)))
    for label, idx in (('貴族', 1), ('中流（商人＋資本家）', 2), ('全階級', 3)):
        per = {a: [comp_stats(a, sd, buckets)[idx] for sd in SEEDS] for a in NAMES}
        cmp_ = []
        for b in OTH:
            ratio, cnt, n = compare(per[b], per[BASE], higher=True)
            cmp_.append(f'{ratio:.3f}・{cnt}/{n}')
            H[('pc' + str(idx), wname, b)] = (ratio, cnt, n)
        print(f'| {label} | ' + ' | '.join(cell(per[a], 4) for a in NAMES) + ' | ' + ' | '.join(cmp_) + ' |')


# ---------------------------------------------------------------- 使用人÷人口
def svc_share(a, sd, lo, hi):
    S = SVCD[a].get(sd, {})
    xs = [S[q]['servantK'] / S[q]['totalPopulationK'] for q in sorted(S) if lo <= q <= hi and S[q].get('totalPopulationK', 0) > 0]
    return med(xs)


print(f'\n## 英の使用人 POP の数÷人口（svc_s の servantK÷totalPopulationK・窓の中央→種の中央〔最小〜最大〕）。右端＝腕÷A・腕＞A の種。史実の参照＝英・ウェールズ 1871 年 約 {HIST_SERVANT_SHARE * 100:.1f}%（二次）')
print('| 窓 | ' + ' | '.join(NAMES) + ' | ' + ' | '.join(f'{b}÷{BASE}・{b}＞{BASE} の種' for b in OTH) + ' |')
print('|---|' + '---|' * (len(NAMES) + len(OTH)))
for name, lo, hi in WIN:
    per = {a: [svc_share(a, sd, lo, hi) for sd in SEEDS] for a in NAMES}
    cmp_ = []
    for b in OTH:
        ratio, cnt, n = compare(per[b], per[BASE], higher=True)
        cmp_.append(f'{ratio:.3f}・{cnt}/{n}')
        H[('svc', name, b)] = (ratio, cnt, n)
    print(f'| {name} | ' + ' | '.join(cell(per[a], 4) for a in NAMES) + ' | ' + ' | '.join(cmp_) + ' |')


# ---------------------------------------------------------------- H4 奢侈品の輸出（全ての国）
def lux_share(a, sd, nat, lo, hi):
    N = NATD[a][sd].get(nat, {})
    xs = []
    for q in sorted(N):
        if lo <= q <= hi:
            e = N[q].get('exportsPerClearing', float('nan')); l = N[q].get('luxuryExportsPerClearing', float('nan'))
            if e == e and l == l and e > 0:
                xs.append(l / e)
    return med(xs)


print('\n## H4（記録）奢侈品（Luxury 階層・アヘンを除く）の輸出額÷全輸出額（nation_s・全ての国・窓の四半期の中央→種の中央）の上位 5 国')
for wname, lo, hi in (('Q41-160', 40, 159), ('Q161-240', 160, 239)):
    print(f'### {wname}')
    for a in NAMES:
        nats = sorted({n for sd in SEEDS for n in NATD[a][sd]})
        sc = []
        for n in nats:
            v = [lux_share(a, sd, n, lo, hi) for sd in SEEDS]
            if sum(1 for x in v if x == x) >= 5:
                sc.append((med(v), n, sum(1 for x in v if x == x)))
        sc.sort(reverse=True)
        print(f'- {a}：' + '・'.join(f'{n} {s:.3f}（{k}種）' for s, n, k in sc[:5]) + f'　／　英 {med([lux_share(a, sd, UK, lo, hi) for sd in SEEDS]):.3f}')
    for b in OTH:
        names_a = sorted({n for sd in SEEDS for n in NATD[BASE][sd]})
        sa = {n: med([lux_share(BASE, sd, n, lo, hi) for sd in SEEDS]) for n in names_a}
        sb = {n: med([lux_share(b, sd, n, lo, hi) for sd in SEEDS]) for n in sorted({n for sd in SEEDS for n in NATD[b][sd]})}
        diffs = sorted((sb[n] - sa[n], n) for n in sa if n in sb and sa[n] == sa[n] and sb[n] == sb[n])
        print(f'- {b}−{BASE}（割合の差）：最小 ' + '・'.join(f'{n} {d:+.3f}' for d, n in diffs[:3]) + '　最大 ' + '・'.join(f'{n} {d:+.3f}' for d, n in diffs[-3:]))

# ---------------------------------------------------------------- 記録：英の階級ごとの r（近似）
print('\n## 記録：英の階級ごとの r の近似＝（class_s の earnedPerMonth÷priceLevel÷sizeK）÷Y（Y は各腕の worldRealIncomePerCapitaPerMonth・0 の腕は —）。窓の中央→種の中央')
print('| 腕 | 窓 | 貴族 | 資本家 | 商人 | 工場労働者 | 農民 | 使用人 |')
print('|---|---|---|---|---|---|---|---|')
for a in OTH:
    for name, lo, hi in WIN:
        cells = []
        for cid in (NOBLE, CAPITALIST, MERCHANT, WORKER, PEASANT, SERVANT):
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


# ---------------------------------------------------------------- 記録：富裕層の貯蓄（分子・分母）
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


print('\n## 記録：英の富裕層（貴族＋資本家）の貯蓄（分子・名目）／所得（分母）／貯蓄（実質）／貯蓄÷所得（月数）。窓の中央→種の中央。右端＝腕÷A・腕＜A の種')
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
        print(f'| {name} | ' + ' | '.join(cell(per[a], nd) for a in NAMES) + ' | ' + ' | '.join(cmp_) + ' |')

# ---------------------------------------------------------------- 記録：専売と間接税÷年GDP・使用人の市場・生活水準
print('\n## 記録：英の専売と間接税（nation_s の indirectRevenueMeanPerMonth×12÷nominalGdpPerYear・窓の中央→種の中央）と 腕÷A')
print('| 窓 | ' + ' | '.join(NAMES) + ' | ' + ' | '.join(f'{b}÷{BASE}' for b in OTH) + ' |')
print('|---|' + '---|' * (len(NAMES) + len(OTH)))
for name, lo, hi in WIN:
    per = {}
    for a in NAMES:
        v = []
        for sd in SEEDS:
            N = NATD[a][sd][UK]
            v.append(med([N[q]['indirectRevenueMeanPerMonth'] * 12 / N[q]['nominalGdpPerYear'] for q in sorted(N) if lo <= q <= hi and N[q].get('nominalGdpPerYear', 0) > 0]))
        per[a] = v
    rat = [med(per[b]) / med(per[BASE]) if med(per[BASE]) else float('nan') for b in OTH]
    print(f'| {name} | ' + ' | '.join(cell(per[a], 4) for a in NAMES) + ' | ' + ' | '.join(f'{x:.3f}' for x in rat) + ' |')

print('\n## 記録：英の家事奉仕の市場（svc_s）と使用人の人あたりの所得（class_s）')
for col, label, nd in (('servantK', '使用人の人数（千人）', 1), ('soldPerMonth', '売れた量（月）', 1), ('shortagePerMonth', '不足（月）', 2), ('priceSoldWeighted', '売値（加重）', 3)):
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

print('\n## 記録：大衆の生活水準（nation_s・窓の中央→種の中央・英 ／ 全国の中央）')
for k, nd in (('needMetShare', 3), ('foodMetShare', 3), ('supplyFillShare', 3)):
    print(f'### {k}')
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

# ---------------------------------------------------------------- 記録 P8：貧国の貴族＋資本家と大衆の奢侈品の支払い（spend_s の全国・事前登録 追記 2）
def load_spend_all(D):
    """seed -> bucket -> (uid, nation) -> {'months', 'size':{c}, 'receipt':{c}, 'lux':{c}}（Luxury 階層の財〔アヘンを除く〕の payPerMonth の階級ごとの和）"""
    out = {}
    for p in sorted(glob.glob(os.path.join(D, 'spend_s*.tsv'))):
        sd = seed_of(p, 'spend_s'); d = out.setdefault(sd, {})
        for r in rows(p):
            k = r['kind']
            if k == 'class':
                e = d.setdefault(int(r['bucket']), {}).setdefault((r['nationUid'], r['nation']), {'months': f(r['monthsCount']), 'size': {}, 'receipt': {}, 'lux': {}})
                c = int(r['classId']); e['size'][c] = f(r['qtyPerMonth']); e['receipt'][c] = f(r['receiptPerMonth'])
            elif k == 'good' and r['tier'] == 'Luxury' and r['slotName'] != 'アヘン':
                e = d.setdefault(int(r['bucket']), {}).setdefault((r['nationUid'], r['nation']), {'months': f(r['monthsCount']), 'size': {}, 'receipt': {}, 'lux': {}})
                c = int(r['classId']); e['lux'][c] = e['lux'].get(c, 0.0) + f(r['payPerMonth'])
    return out


def nation_median(a, sd, nat, key, lo, hi):
    N = NATD[a][sd].get(nat, {})
    return med([N[q][key] for q in sorted(N) if lo <= q <= hi and key in N[q]])


if OTH:
    b_arm = OTH[0]
    SPA = {a: load_spend_all(ARMS[a]) for a in (BASE, b_arm)}
    print(f'\n## 記録 P8：貧国〔国全体の r＜1・{b_arm} の Y と各国の priceLevel で選ぶ〕の貴族＋資本家と大衆の奢侈品〔Luxury 階層・アヘンを除く〕の 1 人あたりの実質の支払い（payPerMonth÷人数÷priceLevel）の {b_arm}÷{BASE}（種の中央〔最小〜最大〕・＞1 の種）')
    print('| 窓 | 貧国の数（種の中央） | 貴族＋資本家 | 大衆（工場労働者＋農民） | 参考：貧国の貴族＋資本家の r の中央 |')
    print('|---|---|---|---|---|')
    ELITE = (NOBLE, CAPITALIST)
    for wname, buckets, lo, hi in SPEND_WIN[:2]:
        elite_r = []; mass_r = []; npoor = []; elite_rr = []
        for sd in SEEDS:
            def acc(a, poor_set, cls):
                pay = size = 0.0
                for key in poor_set:
                    pl = nation_median(a, sd, key[1], 'priceLevel', lo, hi)
                    if not (pl == pl and pl > 0):
                        continue
                    for bk in buckets:
                        e = SPA[a].get(sd, {}).get(bk, {}).get(key)
                        if e is None:
                            continue
                        m = e['months']
                        for c in cls:
                            if c in e['size'] and c in e['lux']:
                                pay += e['lux'][c] * m / pl; size += e['size'][c] * m
                return pay / size if size > 0 else float('nan')
            poor = []; rvals = []
            Yb = med([NATD[b_arm][sd][UK][q].get('worldRealIncomePerCapitaPerMonth', 0.0) for q in range(lo, hi + 1) if q in NATD[b_arm][sd].get(UK, {})])
            for key in (SPA[b_arm].get(sd, {}).get(buckets[0], {})):
                tot_rec = tot_sz = 0.0; ok = True
                pl = nation_median(b_arm, sd, key[1], 'priceLevel', lo, hi)
                if not (pl == pl and pl > 0 and Yb > 0):
                    continue
                for bk in buckets:
                    e = SPA[b_arm].get(sd, {}).get(bk, {}).get(key)
                    if e is None:
                        ok = False; break
                    m = e['months']
                    tot_rec += sum(e['receipt'].values()) * m; tot_sz += sum(e['size'].values()) * m
                if ok and tot_sz > 0 and (tot_rec / tot_sz / pl / Yb) < 1:
                    poor.append(key)
                    e = SPA[b_arm][sd][buckets[0]][key]
                    er = sum(e['receipt'].get(c, 0) for c in ELITE); es = sum(e['size'].get(c, 0) for c in ELITE)
                    if es > 0:
                        rvals.append(er / es / pl / Yb)
            npoor.append(len(poor)); elite_rr.append(med(rvals))
            ea, eb = acc(BASE, poor, ELITE), acc(b_arm, poor, ELITE)
            ma, mb = acc(BASE, poor, MASS), acc(b_arm, poor, MASS)
            elite_r.append(eb / ea if ea == ea and ea > 0 else float('nan')); mass_r.append(mb / ma if ma == ma and ma > 0 else float('nan'))
        up_e = sum(1 for x in elite_r if x == x and x > 1); up_m = sum(1 for x in mass_r if x == x and x > 1)
        print(f'| {wname} | {med(npoor):.0f} | {cell(elite_r, 3)}・＞1 の種 {up_e}/{len([x for x in elite_r if x == x])} | {cell(mass_r, 3)}・＞1 の種 {up_m}/{len([x for x in mass_r if x == x])} | {cell(elite_rr, 2)} |')


# ---------------------------------------------------------------- 記録：r≤0 などで f＝1 に倒れた POP×月（nation_s の累計列の窓の差）
FB = ('luxuryRatioPopMonths', 'luxuryRatioNonPositivePopMonths', 'luxuryRatioInvalidPopMonths', 'luxuryRatioNonPositiveSizeSum')


def fb_window(a, sd, nats, lo, hi):
    """窓 [lo, hi] の累計列の差（lo-1 の末の値を引く・窓の終わりが試合の末より後なら最後の四半期まで）"""
    tot = [0.0] * 4
    for n in nats:
        N = NATD[a][sd].get(n, {})
        if not N:
            continue
        qs = [q for q in N if q <= hi]
        if not qs:
            continue
        h = max(qs)
        if not all(k in N[h] for k in FB):
            continue
        l0 = N.get(lo - 1) if lo >= 1 else None
        for i, k in enumerate(FB):
            tot[i] += N[h][k] - (l0[k] if l0 is not None and k in l0 else 0.0)
    return tot


for a in OTH:
    print(f'\n## 記録：{a} の r≤0 などで f＝1 に倒れた POP×月（nation_s の luxuryRatio* の累計列の窓の差・ε＝0 の腕は 0）。事前登録 追記 2：非正の POP×月が総数の 1% 以上なら f＝0 を次の腕の候補に書く')
    print('| 窓 | 範囲 | 総数（POP×月・種の中央） | 非正の割合 | 非正の Size の和 | 無効の割合 |')
    print('|---|---|---|---|---|---|')
    for name, lo, hi in WIN:
        for label, nats_of in (('英', lambda sd, a=a: [UK]), ('全国', lambda sd, a=a: list(NATD[a][sd]))):
            tots = []; npr = []; invr = []; npsz = []
            for sd in SEEDS:
                t, n_, i_, s_ = fb_window(a, sd, nats_of(sd), lo, hi)
                tots.append(t); npr.append(n_ / t if t > 0 else float('nan')); invr.append(i_ / t if t > 0 else float('nan')); npsz.append(s_)
            print(f'| {name} | {label} | {cell(tots, 0)} | {cell(npr, 4)} | {cell(npsz, 1)} | {cell(invr, 4)} |')


# ---------------------------------------------------------------- 事前登録の線の判定（追記 2 の読み方：H1・H2 の合否は Q41-160 だけ・H3 は下限だけ・Q5-40 は参考）
print('\n## 事前登録の線の判定（B＝' + (OTH[0] if OTH else '—') + '・雑音の床 δ はヌル腕 ' + str(NULLS) + ' から）')
if OTH:
    b = OTH[0]
    verdict = {}
    for wname, buckets, lo, hi in SPEND_WIN[:2]:
        main = wname == 'Q41-160'
        tag = '' if main else '〔参考・合否に使わない〕'
        d_mass = floor(lambda a, sd: comp_stats(a, sd, buckets)[0])
        d_nob = floor(lambda a, sd: comp_stats(a, sd, buckets)[1])
        d_mid = floor(lambda a, sd: comp_stats(a, sd, buckets)[2])
        ma = [comp_stats(BASE, sd, buckets)[0] for sd in SEEDS]; mb = [comp_stats(b, sd, buckets)[0] for sd in SEEDS]
        lower = sum(1 for x, y in zip(ma, mb) if x == x and y == y and y < x)
        thr1 = max(0.15, 2 * (d_mass if d_mass == d_mass else 0))
        ok1 = med(mb) <= med(ma) - thr1 and lower >= 8
        print(f'- H1 {wname}{tag}：大衆の割合 A {med(ma):.3f}→B {med(mb):.3f}（差 {med(mb) - med(ma):+.3f}・線 ≦ −{thr1:.3f}・δ={d_mass:.4f}）・B＜A の種 {lower}/{len(SEEDS)}（線 ≧8）→ {"通る" if ok1 else "外れる"}')
        for cname, cid in (('工場労働者', WORKER), ('農民', PEASANT)):
            def share(a, sd, cid=cid):
                S = SPD[a].get(sd, {}); qty = collections.defaultdict(float)
                for bk in buckets:
                    e = S.get(bk)
                    if e is None:
                        return float('nan')
                    for c, v in e['qty'].items():
                        qty[c] += v * e['months']
                tot = sum(qty.values())
                return qty[cid] / tot if tot > 0 else float('nan')
            sa = med([share(BASE, sd) for sd in SEEDS]); sb = med([share(b, sd) for sd in SEEDS])
            print(f'    内訳 {cname}：A {sa:.3f}→B {sb:.3f}（差 {sb - sa:+.3f}）')
        rn, cn, nn = H[('pc1', wname, b)]; rm, cm, nm = H[('pc2', wname, b)]
        base_n = med([comp_stats(BASE, sd, buckets)[1] for sd in SEEDS]); base_m = med([comp_stats(BASE, sd, buckets)[2] for sd in SEEDS])
        da = d_nob / base_n if d_nob == d_nob and base_n else 0.0
        dm = d_mid / base_m if d_mid == d_mid and base_m else 0.0
        t_n = 1 + max(0.20, 2 * da); t_m = 1 + max(0.20, 2 * dm)
        ok2 = rn >= t_n and rm >= t_m
        print(f'- H2 {wname}{tag}：貴族の1人あたり B÷A {rn:.3f}（線 ≧{t_n:.3f}・B＞A {cn}/{nn}）・中流 B÷A {rm:.3f}（線 ≧{t_m:.3f}・B＞A {cm}/{nm}）→ {"通る" if ok2 else "外れる"}')
        if main:
            verdict['H1'] = ok1; verdict['H2'] = ok2
    r5, c5, n5 = H[('svc', 'Q5-40', b)]; r41, c41, n41 = H[('svc', 'Q41-160', b)]
    ok3 = r5 >= 0.85 and r41 >= 0.85
    verdict['H3'] = ok3
    s41 = med([svc_share(b, sd, 40, 159) for sd in SEEDS])
    print(f'- H3：使用人÷人口 B÷A　Q5-40 {r5:.3f}（B＞A {c5}/{n5}）・Q41-160 {r41:.3f}（B＞A {c41}/{n41}）（線 ≧0.85 の両方・上限なし）→ {"通る" if ok3 else "外れる"}　／　B の Q41-160 の水準 {s41:.4f} 対 史実 {HIST_SERVANT_SHARE:.3f}（英・ウェールズ 1871）、A は {med([svc_share(BASE, sd, 40, 159) for sd in SEEDS]):.4f}')
    print('- **合否（Q41-160 の H1・H2、Q5-40 と Q41-160 の H3）：' + '・'.join(f'{k} {"通る" if v else "外れる"}' for k, v in verdict.items()) + ' → ' + ('H1〜H3 全て通る' if all(verdict.values()) else 'H1〜H3 のどれかが外れた') + '**')
