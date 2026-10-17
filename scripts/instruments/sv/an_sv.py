"""(306) 線(2) 使用人の試し直し（作者の設計 (2)・使用人の産出の倍率）の読み手（読むだけ・挙動は変えない）。(302) の s2/an_s2.py から作った（弾力の節を外し、能力・採算・階級ごとの家事奉仕を足した）。
事前登録 measurements/sv_20261013_prereg.txt の [1][2][5][6]。
  python an_sv.py A=<ins> [B=<ins> ...]   （先頭の腕が基線 A・採否は 2 番目の腕 B を A に対して読む）
  入力＝`INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins
        nation_s（全ての国・四半期）・class_s（挙げた国は四半期ごと）・svc_s（挙げた国の家事奉仕の市場・末尾 2 列 servantCapacityPerMonth・servantDesiredPerMonth は (306) で足した）・spend_s（窓の区切りごとの階級×財）
  窓（四半期の番号 q は 0 始まり・表の Q は q+1）：Q1-8＝q0..7・Q5-40＝q4..39・Q41-160＝q40..159・Q161-240＝q160..239・Q189-228＝q188..227・全期。
  段0：挙げた 4 国の 利用率（産出÷能力）・採算（DesiredOutput÷能力）・売れた量・不足・価格÷参照価格・使用人÷人口、家事奉仕の買い手の階級ごとの量と支払い（spend_s）。倍率 m の規則（u_A・s_A・m_bind・m_hist・m）。
  判定② 英の富裕層（貴族＋資本家）の貯蓄（分子・名目）＝class_s の savingsStock の和・分母（同じ窓の earnedPerMonth の和）・実質の分子・貯蓄÷所得（月数）。B＜A の種の数と B÷A。
  判定① 英の使用人 POP の数÷人口＝svc_s の servantK÷totalPopulationK（四半期の末の値）。B＞A の種の数。
  記録：使用人の賃金・家事奉仕の価格・不足・大衆の家事奉仕の量・大衆の生活水準・窃盗。
  財は名前で引く（番号を書かない）。計器の限界の自己申告：①spend_s の窓の月数は四半期×3の和②class_s の savingsStock は四半期の終わりの在庫・earnedPerMonth は四半期の月平均
        ③svc_s は四半期の末の月 1 点（月平均ではない）④spend_s の階級の量は挙げた国だけ全ての財・階級（他の国は 6 階級×嗜好品と役務）。
"""
import sys, os, glob, re, statistics, collections, math
sys.stdout.reconfigure(encoding='utf-8')
UK = 'イギリス'
FOUR = ['イギリス', 'フランス', 'ネーデルラント', 'ポルトガル']
SVC_NAME = '家事奉仕'
ARMS = collections.OrderedDict(a.split('=', 1) for a in sys.argv[1:])
NAMES = list(ARMS)
BASE = NAMES[0]
WIN = [('Q1-8', 0, 7), ('Q5-40', 4, 39), ('Q41-160', 40, 159), ('Q161-240', 160, 239), ('Q189-228', 188, 227), ('全期', 0, 10**6)]
STAGE0_WIN = [('Q5-40', 4, 39), ('Q41-160', 40, 159), ('Q161-240', 160, 239)]
SPEND_WIN = [('Q5-40', (1, 2)), ('Q41-160', (3,)), ('Q161-228', (4, 5)), ('Q229-400', (6, 7))]
NOBLE, PEASANT, WORKER, CAPITALIST, MERCHANT, SERVANT = 0, 1, 2, 3, 10, 12
HIST = 0.054


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
    out = {}
    for p in sorted(glob.glob(os.path.join(D, 'nation_s*.tsv'))):
        sd = seed_of(p, 'nation_s'); d = out.setdefault(sd, collections.defaultdict(dict))
        for r in rows(p):
            q = int(r['quarter'])
            d[r['nation']][q] = {k: f(v) for k, v in r.items() if k not in ('nation', 'year')}
    return out


def load_class(D):
    """seed -> q -> {classId: (sizeK, savingsStock, earnedPerMonth)}（英のみ・四半期ごとの行）"""
    out = {}
    for p in sorted(glob.glob(os.path.join(D, 'class_s*.tsv'))):
        sd = seed_of(p, 'class_s'); d = out.setdefault(sd, collections.defaultdict(dict))
        for r in rows(p):
            if r['nation'] != UK:
                continue
            m = re.fullmatch(r'[Qq]?(\d+)', r['time'])
            if not m:
                continue
            d[int(m.group(1))][int(r['classId'])] = (f(r['sizeK']), f(r['savingsStock']), f(r['earnedPerMonth']))
    return out


def load_svc(D):
    """seed -> nation -> q -> {列: 数}（挙げた国の家事奉仕の市場・四半期の末の月）"""
    out = {}
    for p in sorted(glob.glob(os.path.join(D, 'svc_s*.tsv'))):
        sd = seed_of(p, 'svc_s'); d = out.setdefault(sd, collections.defaultdict(dict))
        for r in rows(p):
            d[r['nation']][int(r['quarter'])] = {k: f(v) for k, v in r.items() if k not in ('nation', 'year')}
    return out


def load_spend(D):
    """seed -> (nation, bucket) -> {'months', 'svc': {classId: [pay, qty]}, 'pay': {classId: 階級の支出}}（kind=good・財＝家事奉仕）"""
    out = {}
    for p in sorted(glob.glob(os.path.join(D, 'spend_s*.tsv'))):
        sd = seed_of(p, 'spend_s'); d = out.setdefault(sd, {})
        for r in rows(p):
            if r['kind'] == 'class':
                e = d.setdefault((r['nation'], int(r['bucket'])), {'months': f(r['monthsCount']), 'svc': collections.defaultdict(lambda: [0.0, 0.0]), 'paid': {}, 'size': {}})
                e['paid'][int(r['classId'])] = f(r['payPerMonth']); e['size'][int(r['classId'])] = f(r['qtyPerMonth'])
            elif r['kind'] == 'good' and r['slotName'] == SVC_NAME:
                e = d.setdefault((r['nation'], int(r['bucket'])), {'months': f(r['monthsCount']), 'svc': collections.defaultdict(lambda: [0.0, 0.0]), 'paid': {}, 'size': {}})
                c = int(r['classId']); e['svc'][c][0] += f(r['payPerMonth']); e['svc'][c][1] += f(r['qtyPerMonth'])
    return out


def window_vals(rowsq, a, b, key):
    return [rowsq[q][key] for q in sorted(rowsq) if a <= q <= b and key in rowsq[q]]


def compare(xs, ys, higher=False):
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
print(f'# (306) 線(2) 読み手の出力　腕 {NAMES}　種 {SEEDS}（{len(SEEDS)} 種）')
for a, D in ARMS.items():
    print(f'  {a} = {D}')
OTH = NAMES[1:]


def svcq(a, sd, nat):
    return SVCD[a].get(sd, {}).get(nat, {})


def ratio_col(a, sd, nat, lo, hi, num, den):
    S = svcq(a, sd, nat)
    return med([S[q][num] / S[q][den] for q in sorted(S) if lo <= q <= hi and S[q].get(den, 0) > 0 and num in S[q]])


# ---------------------------------------------------------------- 段0
print('\n## 段0 ① 家事奉仕の利用率と採算（svc_s・四半期の末の月・窓の中央→種の中央〔最小〜最大〕）。利用率＝servantOutputPerMonth÷servantCapacityPerMonth・採算＝servantDesiredPerMonth÷servantCapacityPerMonth（＝profitFactor の使用人の重み付き）')
print('| 腕 | 国 | 窓 | 利用率（産出÷能力） | 採算（DesiredOutput÷能力） | 不足÷売れた量 | 価格÷参照価格 | 使用人÷人口 |')
print('|---|---|---|---|---|---|---|---|')
for a in NAMES:
    for nat in FOUR:
        for name, lo, hi in STAGE0_WIN:
            util = [ratio_col(a, sd, nat, lo, hi, 'servantOutputPerMonth', 'servantCapacityPerMonth') for sd in SEEDS]
            prof = [ratio_col(a, sd, nat, lo, hi, 'servantDesiredPerMonth', 'servantCapacityPerMonth') for sd in SEEDS]
            shortr = [ratio_col(a, sd, nat, lo, hi, 'shortagePerMonth', 'soldPerMonth') for sd in SEEDS]
            pr = []
            for sd in SEEDS:
                S = svcq(a, sd, nat)
                pr.append(med([S[q]['priceSoldWeighted'] / S[q]['refPrice'] for q in sorted(S) if lo <= q <= hi and S[q].get('refPrice', 0) > 0]))
            sh = [ratio_col(a, sd, nat, lo, hi, 'servantK', 'totalPopulationK') for sd in SEEDS]
            print(f'| {a} | {nat} | {name} | {cell(util, 3)} | {cell(prof, 3)} | {cell(shortr, 4)} | {cell(pr, 3)} | {cell(sh, 4)} |')

print('\n## 段0 ② 家事奉仕の買い手の階級ごとの量と支払い（spend_s・窓の区切りの月平均・種の中央）。量＝qtyPerMonth・支払い＝payPerMonth・割合＝その階級が全階級の量に占める割合。階級の人数÷人口は参考')
CLS_NAME = {NOBLE: '貴族', PEASANT: '農民', WORKER: '工場労働者', CAPITALIST: '資本家', MERCHANT: '商人', SERVANT: '使用人'}
print('| 腕 | 国 | 窓 | 階級 | 量（月） | 量の割合 | 支払い（月・名目） | 支払いの割合 |')
print('|---|---|---|---|---|---|---|---|')
for a in NAMES:
    for nat in FOUR:
        for wname, buckets in SPEND_WIN:
            per = collections.defaultdict(lambda: ([], []))
            tq = []; tp = []
            for sd in SEEDS:
                qs = collections.defaultdict(float); ps = collections.defaultdict(float); mo = 0.0
                for bk in buckets:
                    e = SPD[a].get(sd, {}).get((nat, bk))
                    if e is None or not (e['months'] > 0):
                        continue
                    mo += e['months']
                    for c, v in e['svc'].items():
                        ps[c] += v[0] * e['months']; qs[c] += v[1] * e['months']
                if mo <= 0:
                    continue
                tqs = sum(qs.values()); tps = sum(ps.values())
                tq.append(tqs / mo); tp.append(tps / mo)
                for c in set(qs) | set(ps):
                    per[c][0].append(qs[c] / mo); per[c][1].append(ps[c] / mo)
            if not tq:
                continue
            mq, mp = med(tq), med(tp)
            for c in sorted(per, key=lambda c: -med(per[c][0])):
                q_, p_ = med(per[c][0]), med(per[c][1])
                if not (q_ == q_) or (mq > 0 and q_ / mq < 0.005 and (mp <= 0 or p_ / mp < 0.005)):
                    continue
                print(f'| {a} | {nat} | {wname} | {CLS_NAME.get(c, c)} | {q_:.3f} | {q_ / mq if mq else float("nan"):.3f} | {p_:.3f} | {p_ / mp if mp else float("nan"):.3f} |')

print('\n## 段0 ③ 倍率 m の規則（事前登録 [2]）：u_A＝A の英 Q41-160 の利用率・s_A＝A の英 Q41-160 の使用人÷人口（種の中央）')
uA = med([ratio_col(BASE, sd, UK, 40, 159, 'servantOutputPerMonth', 'servantCapacityPerMonth') for sd in SEEDS])
sA = med([ratio_col(BASE, sd, UK, 40, 159, 'servantK', 'totalPopulationK') for sd in SEEDS])
m_bind = 0.8 * uA
m_hist = uA * sA / HIST
m_raw = min(m_bind, m_hist)
m = math.floor(m_raw / 0.05 + 1e-9) * 0.05
print(f'- u_A={uA:.4f}　s_A={sA:.4f}　m_bind=0.8×u_A={m_bind:.4f}　m_hist=u_A×s_A÷{HIST}={m_hist:.4f}　min={m_raw:.4f}　0.05 刻みへ切り下げ **m={m:.2f}**' + ('　（0.2 を下回る＝記録）' if m < 0.2 else ''))


# ---------------------------------------------------------------- ① 英の富裕層の貯蓄
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


print('\n## 判定② 英の富裕層（貴族＋資本家）：貯蓄（分子・名目）／所得（分母・月の流れ）／貯蓄（実質＝÷priceLevel）／貯蓄÷所得（月数）。窓の中央→種の中央〔最小〜最大〕。右端＝腕÷A（種ごとの窓の中央の中央どうしの比）・腕＜A の種')
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

# 貴族と資本家を別に（貯蓄の分子）
print('### 貴族・資本家を別に（貯蓄・名目）')
print('| 窓 | 階級 | ' + ' | '.join(NAMES) + ' | ' + ' | '.join(f'{b}÷{BASE}・{b}＜{BASE} の種' for b in OTH) + ' |')
print('|---|---|' + '---|' * (len(NAMES) + len(OTH)))
for name, lo, hi in WIN:
    for cid, cn in ((NOBLE, '貴族'), (CAPITALIST, '資本家')):
        per = {}
        for a in NAMES:
            v = []
            for sd in SEEDS:
                C = CLSD[a].get(sd, {})
                v.append(med([C[q][cid][1] for q in sorted(C) if lo <= q <= hi and cid in C[q]]))
            per[a] = v
        cmp_ = []
        for b in OTH:
            ratio, cnt, n = compare(per[b], per[BASE])
            cmp_.append(f'{ratio:.3f}・{cnt}/{n}')
        print(f'| {name} | {cn} | ' + ' | '.join(cell(per[a], 0) for a in NAMES) + ' | ' + ' | '.join(cmp_) + ' |')


# ---------------------------------------------------------------- ② 英の使用人 POP の数÷人口
def svc_share(a, sd, lo, hi):
    return ratio_col(a, sd, UK, lo, hi, 'servantK', 'totalPopulationK')


print(f'\n## 判定① 英の使用人 POP の数÷人口（svc_s の servantK÷totalPopulationK・窓の中央→種の中央〔最小〜最大〕）。右端＝腕÷A・腕＞A の種。参照＝イングランド・ウェールズ 1871 年 約 {HIST * 100:.1f}%（二次資料の誌面の数・【推】）')
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

# ---------------------------------------------------------------- 記録：英の家事奉仕の市場と使用人の賃金
print('\n## 記録：英の家事奉仕の市場（svc_s）と使用人の所得（class_s）')
for col, label, nd in (('servantK', '使用人の人数（千人）', 1), ('servantOutputPerMonth', '使用人の産出（月）', 1), ('servantCapacityPerMonth', '能力（月）', 1), ('soldPerMonth', '売れた量（月）', 1), ('shortagePerMonth', '不足（月）', 2), ('priceSoldWeighted', '価格（売れた量で重み付け・名目）', 3)):
    print(f'### {label}')
    print('| 窓 | ' + ' | '.join(NAMES) + ' |')
    print('|---|' + '---|' * len(NAMES))
    for name, lo, hi in WIN:
        print(f'| {name} | ' + ' | '.join(cell([med(window_vals(svcq(a, sd, UK), lo, hi, col)) for sd in SEEDS], nd) for a in NAMES) + ' |')
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

# ---------------------------------------------------------------- 記録：大衆の家事奉仕の量
print('\n## 記録：英の家事奉仕の買い手＝大衆（農民＋工場労働者）と富裕層（貴族＋資本家＋商人）の量・支払い（spend_s・窓の区切りの月平均・種の中央）と 腕÷A')
print('| 窓 | 買い手 | ' + ' | '.join(f'{a}（量／支払い）' for a in NAMES) + ' | ' + ' | '.join(f'{b}÷{BASE}（量・支払い）' for b in OTH) + ' |')
print('|---|---|' + '---|' * (len(NAMES) + len(OTH)))
for wname, buckets in SPEND_WIN:
    for gname, gcls in (('大衆（農民＋工場労働者）', (PEASANT, WORKER)), ('富裕層（貴族＋資本家＋商人）', (NOBLE, CAPITALIST, MERCHANT)), ('使用人', (SERVANT,))):
        per = {}
        for a in NAMES:
            qv = []; pv = []
            for sd in SEEDS:
                qs = ps = mo = 0.0
                for bk in buckets:
                    e = SPD[a].get(sd, {}).get((UK, bk))
                    if e is None or not (e['months'] > 0):
                        continue
                    mo += e['months']
                    for c in gcls:
                        if c in e['svc']:
                            ps += e['svc'][c][0] * e['months']; qs += e['svc'][c][1] * e['months']
                qv.append(qs / mo if mo > 0 else float('nan')); pv.append(ps / mo if mo > 0 else float('nan'))
            per[a] = (qv, pv)
        cmp_ = []
        for b in OTH:
            rq = med([y / x for x, y in zip(per[BASE][0], per[b][0]) if x == x and y == y and x > 0])
            rp = med([y / x for x, y in zip(per[BASE][1], per[b][1]) if x == x and y == y and x > 0])
            cmp_.append(f'{rq:.3f}・{rp:.3f}')
        print(f'| {wname} | {gname} | ' + ' | '.join(f'{med(per[a][0]):.3f} ／ {med(per[a][1]):.3f}' for a in NAMES) + ' | ' + ' | '.join(cmp_) + ' |')

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
print('\n## 事前登録の線の判定（主窓 Q41-160・B＝' + (OTH[0] if OTH else '—') + '）')
if OTH:
    b = OTH[0]
    r1, c1, n1 = L1[('sav', 'Q41-160', b)]
    r2, c2, n2 = L2[('Q41-160', b)]
    ok1 = (r1 == r1 and r1 <= 0.97 and c1 >= 7)
    ok2 = (r2 == r2 and r2 >= 1.10 and c2 >= 7)
    print(f'- ① 使用人÷人口 B÷A {r2:.3f}（線 ≧1.10）・B＞A の種 {c2}/{n2}（線 ≧7）→ {"合格" if ok2 else "不合格"}')
    print(f'- ② 富裕層の貯蓄（分子）B÷A {r1:.3f}（線 ≦0.97）・B＜A の種 {c1}/{n1}（線 ≧7）→ {"合格" if ok1 else "不合格"}')
