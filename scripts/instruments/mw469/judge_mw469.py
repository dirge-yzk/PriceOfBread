"""(469) 線 mw 後半の判定の読み手（読むだけ・シミュは回さない）。
  python judge_mw469.py <腕の ins フォルダ> <基線 A の ins フォルダ（labor_s つき・5 種）> [種の数=10] [--pop=<m466_ins>]
  事前登録 measurements/mw469_20261020_prereg.txt の §4・§11・追記 3 の観測量を、labor_s（列は InstrumentsLw.cs のとおり）から出す。
  〈機構が狙いどおり働いたか〉の門
    [N0] 下限が消えた：labor_s の行（国×四半期・plain の能力>0）のうち 値段÷自己費用 <1 の行の割合 ≥ 90%（腕）
    [N1] 法が縛る：lawCode>0 の行のうち enactedNearFloorShare>0 の行が 1 つ以上（縛り＝値段が〈機構と同じ天井で削った床〉×1.05 以下）
    [N2] 法の施行行で enactedBelowFloorShare>0.05 の行（値段が床の 0.90 倍を下回る州の能力が 5% 超）が 0
    [N3] AI が法を施行：lawCode>0 に届いた種が 6 種以上（10 種のうち）
    [N4] 値段が緊張 u に比例：plainPriceOverMcIndex÷(1.5×u)（u＝plainDemand÷plainCap・u<2/3 の行だけ）の中央値が 0.8〜1.25（形 2 のとき）
  止める線（発散）
    [D1] plainTinyPriceRegionShare（または farm）≥0.05 が 8 四半期以上続く（国×種）が、挙げた国×種の 5% 超
    [D2] 実質の値段の中央値が 1941-1950 年÷1931-1940 年 <0.35（最後の十年でも年 −10% より速く落ち続けている＝不動点に着いていない）が、挙げた国×種の 5% 超
  記録：水準（実質の値段の期の中央値・腕÷A）・清÷英・失業（0.5 超の行）・人口（--pop があれば）・法の初到達の年・賃金の支払いの腕÷A
  限界の申告：①labor_s は挙げた国だけ四半期ごと（他は年末）＝D1・D2 は挙げた国（8 か国）で読む②A は 5 種（腕 B の 10 種のうち種 1〜5 だけ比べる）③N4 の u は直近の月の需要÷能力で、均衡からの遅れを含む④人口は年末の国どうし。
"""
import csv, sys, io, statistics as st, collections, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
args = [a for a in sys.argv[1:] if not a.startswith('--')]
ARM = args[0].rstrip('/') + '/'
BASE = args[1].rstrip('/') + '/'
NS = int(args[2]) if len(args) > 2 else 10
POP = next((a[6:].rstrip('/') + '/' for a in sys.argv[1:] if a.startswith('--pop=')), None)
MAIN = ['イギリス', 'フランス', 'プロイセン', 'ロシア', '清', '日本', 'オスマン', 'オーストリア']
PER = [(1790, 1809), (1810, 1839), (1840, 1869), (1870, 1899), (1900, 1950)]
PN = ['1790-1809', '1810-1839', '1840-1869', '1870-1899', '1900-1950']


def num(s):
    try:
        return float(s)
    except Exception:
        return float('nan')


def med(v):
    v = [x for x in v if x == x]
    return st.median(v) if v else float('nan')


def fmt(x, f='%.3f'):
    return f % x if x == x else '-'


def per(y):
    for i, (a, b) in enumerate(PER):
        if a <= y <= b:
            return i
    return -1


def load(B, seeds):
    L = {}
    for s in seeds:
        p = B + 'labor_s%d.tsv' % s
        if not os.path.exists(p):
            continue
        with open(p, encoding='utf-8') as f:
            L[s] = [r for r in csv.DictReader(f, delimiter='\t')]
    return L


seeds = list(range(1, NS + 1))
LA = load(ARM, seeds)
LB = load(BASE, seeds)
print('腕=%s  基線 A=%s  腕の種=%d（labor_s あり）  A の種=%d' % (ARM, BASE, len(LA), len(LB)))
if not LA:
    print('（腕に labor_s が無い）'); sys.exit(1)
rowsA = [(s, r) for s, rr in LA.items() for r in rr]
rowsB = [(s, r) for s, rr in LB.items() for r in rr]
quarterly = lambda r: r['nation'] in MAIN   # 挙げた国は毎四半期（他は年末のみ）


def cap_pos(r):
    return num(r['plainCapPerMonth']) > 0


# ---- N0
def n0(rows):
    v = [num(r['plainPriceOverMcIndex']) for s, r in rows if cap_pos(r) and quarterly(r)]
    v = [x for x in v if x == x]
    return (sum(1 for x in v if x < 1) / len(v)) if v else float('nan'), len(v)


a0, na = n0(rowsA); b0, nb = n0(rowsB)
print('== [N0] 値段÷自己費用 <1 の行の割合（挙げた国×四半期）  腕 %s（%d 行）  A %s（%d 行）  → %s（基準 ≥0.90）' % (fmt(a0), na, fmt(b0), nb, 'N0 満たす' if a0 >= 0.9 else 'N0 満たさない'))
# ---- N1・N2・N3
enacted = [(s, r) for s, r in rowsA if int(num(r['lawCode'])) > 0 and 'enactedNearFloorShare' in r]
near_pos = [(s, r) for s, r in enacted if num(r['enactedNearFloorShare']) > 0]
below_big = [(s, r) for s, r in enacted if num(r['enactedBelowFloorShare']) > 0.05]
print('== [N1] 法の施行行 %d・縛り（near>0）の行 %d（割合 %s）・near の中央値 %s・最大 %s  → %s' % (
    len(enacted), len(near_pos), fmt(len(near_pos) / len(enacted) if enacted else float('nan')),
    fmt(med([num(r['enactedNearFloorShare']) for s, r in enacted])), fmt(max([num(r['enactedNearFloorShare']) for s, r in enacted], default=float('nan'))),
    'N1 満たす' if near_pos else ('N1 満たさない（法は不活性）' if enacted else 'N1 空虚（施行行が無い）')))
below_all = [num(r['enactedBelowFloorShare']) for s, r in enacted]
print('== [N2] 施行行のうち below>0.05 の行 %d / %d  below の中央値 %s・p95 %s・最大 %s  → %s' % (
    len(below_big), len(enacted), fmt(med(below_all)), fmt(sorted(below_all)[int(0.95 * (len(below_all) - 1))] if below_all else float('nan')), fmt(max(below_all, default=float('nan'))),
    ('N2 満たす' if not below_big else 'N2 満たさない') if enacted else 'N2 空虚'))
reach = {}
for s, rr in LA.items():
    ys = [int(r['year']) for r in rr if int(num(r['lawCode'])) > 0 and r['nation'] in MAIN]
    yall = [int(r['year']) for r in rr if int(num(r['lawCode'])) > 0]
    reach[s] = (min(ys) if ys else None, min(yall) if yall else None)
n_reach = sum(1 for s, (m, a) in reach.items() if a is not None)
print('== [N3] 法が施行された種 %d / %d（基準 ≥6/10）  → %s' % (n_reach, len(LA), 'N3 満たす' if n_reach >= 6 else 'N3 満たさない'))
print('   種ごとの初施行年（挙げた 8 国 / 全ての国）： ' + ' '.join('s%d=%s/%s' % (s, m, a) for s, (m, a) in sorted(reach.items())))
byn = collections.defaultdict(list)
for s, rr in LA.items():
    first = {}
    for r in rr:
        if int(num(r['lawCode'])) > 0 and r['nation'] not in first:
            first[r['nation']] = int(r['year'])
    for n, y in first.items():
        byn[n].append(y)
print('   国別の初施行年（種の中央値・件数）： ' + ' '.join('%s=%d(%d)' % (n, med(ys), len(ys)) for n, ys in sorted(byn.items(), key=lambda kv: med(kv[1]))[:20]))
# ---- N4
ratios = []
elas = []
for s, r in rowsA:
    if not (cap_pos(r) and quarterly(r)):
        continue
    u = num(r['plainDemandPerMonth']) / num(r['plainCapPerMonth']); p = num(r['plainPriceOverMcIndex'])
    if u > 0 and u < 2 / 3 and p == p and p > 0:
        ratios.append(p / (1.5 * u))
        elas.append((u, p))
m4 = med(ratios)
import math
xs = [math.log(u) for u, p in elas]; ys_ = [math.log(p) for u, p in elas]
if len(xs) > 10:
    mx = sum(xs) / len(xs); my = sum(ys_) / len(ys_)
    sl = sum((x - mx) * (y - my) for x, y in zip(xs, ys_)) / max(1e-12, sum((x - mx) ** 2 for x in xs))
else:
    sl = float('nan')
print('== [N4] 値段÷自己費用 ÷ (1.5u)（u<2/3 の行 %d）の中央値 %s（p25 %s・p75 %s）  → %s（基準 0.8〜1.25）  log 値段 の log u に対する傾き %s（形 2 の理想 1）' % (
    len(ratios), fmt(m4), fmt(sorted(ratios)[len(ratios) // 4] if ratios else float('nan')), fmt(sorted(ratios)[3 * len(ratios) // 4] if ratios else float('nan')),
    ('N4 満たす' if 0.8 <= m4 <= 1.25 else 'N4 満たさない'), fmt(sl)))
# ---- D1・D2
def runs_tiny(rr, nat, col):
    q = sorted([(int(r['quarter']), num(r[col])) for r in rr if r['nation'] == nat])
    best = cur = 0
    for _, v in q:
        if v == v and v >= 0.05:
            cur += 1; best = max(best, cur)
        else:
            cur = 0
    return best


def ratio_end(rr, nat, col):
    a = [num(r[col]) for r in rr if r['nation'] == nat and 1931 <= int(r['year']) <= 1940]
    b = [num(r[col]) for r in rr if r['nation'] == nat and 1941 <= int(r['year']) <= 1950]
    return med(b) / med(a) if a and b and med(a) > 0 else float('nan')


tot = 0; d1 = 0; d2 = 0; d2n = 0; d1list = []; d2list = []
for s, rr in LA.items():
    for n in MAIN:
        if not any(r['nation'] == n for r in rr):
            continue
        tot += 1
        t1 = max(runs_tiny(rr, n, 'plainTinyPriceRegionShare'), runs_tiny(rr, n, 'farmTinyPriceRegionShare'))
        if t1 >= 8:
            d1 += 1; d1list.append((s, n, t1))
        r1 = min([x for x in (ratio_end(rr, n, 'plainRealPriceIndex'), ratio_end(rr, n, 'farmRealPriceIndex')) if x == x], default=float('nan'))
        if r1 == r1:
            d2n += 1
            if r1 < 0.35:
                d2 += 1; d2list.append((s, n, r1))
print('== [D1] 州の実質の値段 <1e-3×物価水準 が能力の 5% 以上を 8 四半期以上続けた 国×種： %d / %d（%s）  → %s（基準 ≤5%%）' % (d1, tot, fmt(d1 / tot if tot else float('nan')), '発散なし' if d1 <= 0.05 * tot else '発散（止める線）'))
if d1list:
    print('   ' + ' '.join('s%d:%s(%dq)' % x for x in d1list[:20]))
print('== [D2] 実質の値段の 1941-50 ÷ 1931-40 <0.35 の 国×種： %d / %d（%s）  → %s（基準 ≤5%%）' % (d2, d2n, fmt(d2 / d2n if d2n else float('nan')), '不動点に着いた' if d2 <= 0.05 * d2n else '落ち続けている（止める線）'))
if d2list:
    print('   ' + ' '.join('s%d:%s(%.2f)' % x for x in d2list[:20]))
# ---- 記録
print()
print('== [記録] 水準：plain 実質の値段の国×期の中央値（腕 | A | 腕÷A）')
print('国'.ljust(8) + ' '.join(p.center(26) for p in PN))
for n in MAIN:
    cells = []
    for i in range(len(PER)):
        a = med([num(r['plainRealPriceIndex']) for s, r in rowsA if r['nation'] == n and per(int(r['year'])) == i])
        b = med([num(r['plainRealPriceIndex']) for s, r in rowsB if r['nation'] == n and per(int(r['year'])) == i])
        cells.append(('%s|%s|%s' % (fmt(a, '%.4f'), fmt(b, '%.3f'), fmt(a / b if b == b and b > 0 else float('nan'), '%.3f'))).center(26))
    print(n.ljust(8) + ' '.join(cells))
def cn_uk(rows):
    out = []
    for i in range(len(PER)):
        c = med([num(r['plainRealPriceIndex']) for s, r in rows if r['nation'] == '清' and per(int(r['year'])) == i])
        e = med([num(r['plainRealPriceIndex']) for s, r in rows if r['nation'] == 'イギリス' and per(int(r['year'])) == i])
        out.append(c / e if e == e and e > 0 else float('nan'))
    return out
print('清÷英（plain 実質の値段）  腕： ' + ' '.join('%s=%s' % (PN[i], fmt(v, '%.2f')) for i, v in enumerate(cn_uk(rowsA))) + '   A： ' + ' '.join('%s=%s' % (PN[i], fmt(v, '%.2f')) for i, v in enumerate(cn_uk(rowsB))))
def unemp(rows):
    v = [(num(r['plainUnemploymentShare']) > 0.5) for s, r in rows if quarterly(r)]
    return sum(v) / len(v) if v else float('nan')
print('失業（plain）が 0.5 超の行の割合  腕 %s  A %s  （能力が需要の約 20 倍の尺度の人為・Q-MW3）' % (fmt(unemp(rowsA)), fmt(unemp(rowsB))))
print('失業（plain）の中央値  腕 %s  A %s' % (fmt(med([num(r['plainUnemploymentShare']) for s, r in rowsA if quarterly(r)])), fmt(med([num(r['plainUnemploymentShare']) for s, r in rowsB if quarterly(r)]))))
print('賃金の支払い（plain・月あたり）の国×期の中央値の 腕÷A：')
for n in MAIN:
    cells = []
    for i in range(len(PER)):
        a = med([num(r['plainWageBillPerMonth']) for s, r in rowsA if r['nation'] == n and per(int(r['year'])) == i and s in LB])
        b = med([num(r['plainWageBillPerMonth']) for s, r in rowsB if r['nation'] == n and per(int(r['year'])) == i])
        cells.append(fmt(a / b if b == b and b > 0 else float('nan')).center(11))
    print(n.ljust(8) + ' '.join(cells))
if POP:
    def popyear(Bd, ss):
        P = {}
        for s in ss:
            p = Bd + 'nation_s%d.tsv' % s
            if not os.path.exists(p):
                continue
            with open(p, encoding='utf-8') as f:
                for r in csv.DictReader(f, delimiter='\t'):
                    if int(r['quarter']) % 4 == 3:
                        P[(s, r['nation'], int(r['year']))] = num(r['populationK'])
        return P
    PA = popyear(ARM, seeds); PB = popyear(POP, seeds)
    print('== [記録] 人口（年末・腕÷m466・国別の中央値・期ごと）')
    print('国'.ljust(8) + ' '.join(p.center(11) for p in PN))
    for n in MAIN:
        cells = []
        for i in range(len(PER)):
            v = [PA[k] / PB[k] for k in PA if k[1] == n and per(k[2]) == i and k in PB and PB[k] > 0 and PA[k] == PA[k]]
            cells.append(fmt(med(v)).center(11) if v else '-'.center(11))
        print(n.ljust(8) + ' '.join(cells))
    tot_ = collections.defaultdict(lambda: [0.0, 0.0])
    for k in PA:
        if k in PB and PA[k] == PA[k] and PB[k] == PB[k]:
            tot_[(k[0], k[2])][0] += PA[k]; tot_[(k[0], k[2])][1] += PB[k]
    rat = [v[0] / v[1] for (s, y), v in tot_.items() if y in (1850, 1900, 1950) and v[1] > 0]
    print('世界の人口の和の比（腕÷m466・1850・1900・1950 の種ごと）： 最小 %s・中央値 %s・最大 %s' % (fmt(min(rat)) if rat else '-', fmt(med(rat)), fmt(max(rat)) if rat else '-'))
