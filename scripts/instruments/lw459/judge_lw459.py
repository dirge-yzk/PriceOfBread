"""(459) 線 lw 後半の判定の読み手（読むだけ・シミュは回さない）。
  python judge_lw459.py <腕の ins フォルダ> <基線の ins フォルダ（例 D:/PoB_work/keep_457/m457_ins）> [種の数=10] [--null=<ヌル腕 ins フォルダ>,<…>]
  事前登録 measurements/lw459_20261020_prereg.txt の §4・§12 の観測量を、腕の計器の生（engineer_s・labor_s・nation_s・class_s・fm_s）から出す。
  [G1] 清÷英の労働の実質の値段（engineer_s の蒸気機関の T 行の replaced）が 3 期（1810-1839・1840-1869・1900-1950）とも 0.6 以下
  [G2] 主要 8 国の最大÷最小（replaced）が 3 期のうち 2 期以上で 1.5 以上
  [G3] 英の蒸気機関の必要性の項が正の T 行の割合（1810-1839）が基線より高く、清では英より低い
  [P1] 水準：labor_s の plain/farm の 値段÷自己費用（mc）・実質の値段の国×期の中央値（腕と基線の両方に labor_s がある種だけ）
  [P5] 止める線の目安：人口（nation_s の populationK の年末の国別の比〔腕÷基線〕の中央値）・失業（labor_s の plainUnemploymentShare が 0.5 を超えた国×四半期の数）
  [W]  段0b：農業労働の賃金の支払い（labor_s farmWageBillPerMonth）÷農民の階級の所得（class_s の農民の earnedPerMonth）の国×期の中央値
  [L]  法（C0・C1 の読み）：labor_s の lawCode が 1・2・3 以上に初めて届いた年（国別・種別）と、enactedBindShare・床÷値段の読み
  限界の申告：①T 行は 8 四半期ごと・②labor_s は挙げた国だけ四半期ごと（他は年末）・③基線の ins に labor_s は無い（A の ins か、腕 A を基線として渡す）・④population の比は同じ種の同じ年末の国どうし。
"""
import csv, sys, io, statistics as st, collections, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
args = [a for a in sys.argv[1:] if not a.startswith('--')]
ARM = args[0].rstrip('/') + '/'
BASE = (args[1] if len(args) > 1 else 'D:/PoB_work/keep_457/m457_ins').rstrip('/') + '/'
NS = int(args[2]) if len(args) > 2 else 10
NAT = ['イギリス', 'フランス', 'プロイセン', 'アメリカ', 'ロシア', '清', '日本', 'オスマン']
G_PER = [(1810, 1839), (1840, 1869), (1900, 1950)]
PER = [(1790, 1809), (1810, 1839), (1840, 1869), (1870, 1899), (1900, 1950)]
PN = ['1790-1809', '1810-1839', '1840-1869', '1870-1899', '1900-1950']


def rows(path):
    if not os.path.exists(path):
        return
    with open(path, encoding='utf-8') as f:
        yield from csv.DictReader(f, delimiter='\t')


def per(y, table=PER):
    for i, (a, b) in enumerate(table):
        if a <= y <= b:
            return i
    return -1


def med(v):
    return st.median(v) if v else float('nan')


def fmt(x, f='%.3f'):
    return f % x if x == x else '-'


def num(s):
    try:
        return float(s)
    except Exception:
        return float('nan')


def engineer(B, seeds):
    R = collections.defaultdict(list); N = collections.defaultdict(list)
    for s in seeds:
        for r in rows(B + 'engineer_s%d.tsv' % s):
            if r['kind'] != 'T' or r['row'] != '蒸気機関' or r['nation'] not in NAT:
                continue
            i = per(int(r['year']))
            if i < 0:
                continue
            R[(r['nation'], i)].append(num(r['replaced'])); N[(r['nation'], i)].append(1 if num(r['needTerm']) > 0 else 0)
    return R, N


seeds = range(1, NS + 1)
RA, NA = engineer(ARM, seeds)
RB, NB = engineer(BASE, seeds)
print('腕=%s  基線=%s  種=%d' % (ARM, BASE, NS))
print()
print('== [G1] 清÷英（replaced・期の中央値）  目標 0.6 以下（3 期とも）')
for lab, R in (('腕', RA), ('基線', RB)):
    vals = []
    for (a, b) in G_PER:
        i = [k for k, p in enumerate(PER) if p == (a, b)][0]
        vals.append(med(R[('清', i)]) / med(R[('イギリス', i)]))
    print('%s： ' % lab + ' '.join('%s=%s' % ('%d-%d' % G_PER[k], fmt(vals[k], '%.3f')) for k in range(3)) + ('  → G1 %s' % ('満たす' if all(v <= 0.6 for v in vals) else '満たさない') if lab == '腕' else ''))
print()
print('== [G2] 主要 8 国の最大÷最小（replaced）  目標 1.5 以上（3 期のうち 2 期以上）')
for lab, R in (('腕', RA), ('基線', RB)):
    vals = []
    for (a, b) in G_PER:
        i = [k for k, p in enumerate(PER) if p == (a, b)][0]
        ms = [med(R[(n, i)]) for n in NAT if R[(n, i)]]
        vals.append(max(ms) / min(ms))
    print('%s： ' % lab + ' '.join('%s=%s' % ('%d-%d' % G_PER[k], fmt(vals[k], '%.2f')) for k in range(3)) + ('  → G2 %s' % ('満たす' if sum(v >= 1.5 for v in vals) >= 2 else '満たさない') if lab == '腕' else ''))
print()
print('== [G3] 蒸気機関の必要性の項が正の T 行の割合（国×期）')
print('国'.ljust(8) + ' '.join(p.center(14) for p in PN))
for lab, N_ in (('腕', NA), ('基線', NB)):
    for n in ('イギリス', '清'):
        print((lab + n).ljust(8) + ' '.join(('%d%% (%d)' % (round(100 * sum(N_[(n, i)]) / len(N_[(n, i)])), len(N_[(n, i)]))).center(14) if N_[(n, i)] else '-'.center(14) for i in range(len(PER))))
uk_a = sum(NA[('イギリス', 1)]) / max(1, len(NA[('イギリス', 1)])); uk_b = sum(NB[('イギリス', 1)]) / max(1, len(NB[('イギリス', 1)]))
cn_a = sum(NA[('清', 1)]) / max(1, len(NA[('清', 1)]))
print('G3（1810-1839 の英が基線より高く、清が英より低い）： 英 腕 %.1f%% 基線 %.1f%%・清 腕 %.1f%% → %s' % (100 * uk_a, 100 * uk_b, 100 * cn_a, '満たす' if (uk_a > uk_b and cn_a < uk_a) else '満たさない（悪化＝英が基線より低い）' if uk_a < uk_b else '満たさない（同じ）'))

# ---------------------------------------------------------------- labor_s
def labor(B, seeds):
    L = collections.defaultdict(list)
    for s in seeds:
        for r in rows(B + 'labor_s%d.tsv' % s):
            L[s].append(r)
    return L


LA = labor(ARM, seeds)
LB = labor(BASE, seeds)
print()
if LA:
    print('== [P1] 水準（labor_s・国×期の中央値・腕）  plain 値段÷mc | farm 値段÷mc | plain 実質の値段 | plain 失業')
    print('国'.ljust(8) + ' '.join(p.center(32) for p in PN))
    for n in NAT:
        cells = []
        for i in range(len(PER)):
            v = collections.defaultdict(list)
            for s, rr in LA.items():
                for r in rr:
                    if r['nation'] == n and per(int(r['year'])) == i:
                        v['p'].append(num(r['plainPriceOverMcIndex'])); v['f'].append(num(r['farmPriceOverMcIndex'])); v['r'].append(num(r['plainRealPriceIndex'])); v['u'].append(num(r['plainUnemploymentShare']))
            cells.append(('  %s %s %s %s' % (fmt(med(v['p'])), fmt(med(v['f'])), fmt(med(v['r']), '%.3f'), fmt(med(v['u']), '%.3f'))).ljust(32) if v['p'] else '-'.center(32))
        print(n.ljust(8) + ' '.join(cells))
    if LB:
        print('（基線の labor_s が渡されたので比：腕÷基線の plain 値段÷mc の国×期の中央値）')
        for n in NAT:
            cells = []
            for i in range(len(PER)):
                a = [num(r['plainPriceOverMcIndex']) for s, rr in LA.items() for r in rr if r['nation'] == n and per(int(r['year'])) == i]
                b = [num(r['plainPriceOverMcIndex']) for s, rr in LB.items() for r in rr if r['nation'] == n and per(int(r['year'])) == i]
                cells.append(fmt(med(a) / med(b), '%.3f').center(11) if a and b else '-'.center(11))
            print(n.ljust(8) + ' '.join(cells))
    # 清÷英の実質の値段（labor_s 由来・G1 の別の読み）
    print('清÷英（plain 実質の値段 labor_s）： ' + ' '.join('%s=%s' % (PN[i], fmt(
        med([num(r['plainRealPriceIndex']) for s, rr in LA.items() for r in rr if r['nation'] == '清' and per(int(r['year'])) == i]) /
        med([num(r['plainRealPriceIndex']) for s, rr in LA.items() for r in rr if r['nation'] == 'イギリス' and per(int(r['year'])) == i]), '%.2f')) for i in range(len(PER))))
    # 失業の目安（P5）
    over = sum(1 for s, rr in LA.items() for r in rr if num(r['plainUnemploymentShare']) > 0.5 or num(r['farmUnemploymentShare']) > 0.5)
    tot = sum(1 for s, rr in LA.items() for r in rr)
    print('[P5] 失業率が 0.5 を超えた国×四半期の行： %d / %d' % (over, tot))
    # 段0b
    print()
    print('== [W] 段0b：農業労働の賃金の支払い ÷ 農民の階級の所得（国×期の中央値）')
    W = collections.defaultdict(list)
    for s in seeds:
        inc = {}
        for r in rows(ARM + 'class_s%d.tsv' % s):
            if r['className'] != '農民' or not r['time'].startswith('q'):
                continue
            inc[(r['nation'], int(r['time'][1:]))] = num(r['earnedPerMonth'])
        for r in LA.get(s, []):
            k = (r['nation'], int(r['quarter']))
            if k in inc and inc[k] > 0:
                W[(r['nation'], per(int(r['year'])))].append(num(r['farmWageBillPerMonth']) / inc[k])
    print('国'.ljust(8) + ' '.join(p.center(11) for p in PN))
    for n in NAT:
        print(n.ljust(8) + ' '.join(fmt(med(W[(n, i)])).center(11) for i in range(len(PER))))
    # 法 [L]
    print()
    print('== [L] 法（lawCode）が 1・2・3 以上に初めて届いた年（国別・種別）と床の読み')
    reach = collections.defaultdict(list)
    for s, rr in LA.items():
        for lvl in (1, 2, 3):
            first = {}
            for r in rr:
                if int(num(r['lawCode'])) >= lvl and r['nation'] not in first:
                    first[r['nation']] = int(r['year'])
            for n, y in first.items():
                reach[lvl].append((s, n, y))
    for lvl, nm in ((1, '生存'), (2, '生活'), (3, '充足')):
        by = collections.defaultdict(list)
        for s, n, y in reach[lvl]:
            by[n].append(y)
        print('%s以上に届いた国×種： %d 件  国別の初到達年（種の中央値）： %s' % (nm, len(reach[lvl]), ' '.join('%s=%d(%d種)' % (n, med(ys), len(ys)) for n, ys in sorted(by.items(), key=lambda kv: med(kv[1])))))
    bind = [num(r['enactedBindShare']) for s, rr in LA.items() for r in rr if int(num(r['lawCode'])) > 0]
    print('施行された国×四半期の数： %d・床が縛った州の割合の中央値： %s・最大： %s' % (len(bind), fmt(med(bind)), fmt(max(bind)) if bind else '-'))
    for k, nm in (('plainFloor0OverSubsIndex', '能力ベース 生存'), ('plainFloor0OverSuffIndex', '能力ベース 充足'), ('plainFloor1OverSubsIndex', '実雇用ベース 生存'), ('plainFloor1OverSuffIndex', '実雇用ベース 充足')):
        v = [num(r[k]) for s, rr in LA.items() for r in rr]
        v = [x for x in v if x == x]
        print('床÷値段 %s： 中央値 %s・最大 %s' % (nm, fmt(med(v)), fmt(max(v)) if v else '-'))
else:
    print('（この腕の ins に labor_s が無い）')

# ---------------------------------------------------------------- 人口の比（P5）
def popyear(B, seeds):
    P = {}
    for s in seeds:
        for r in rows(B + 'nation_s%d.tsv' % s):
            if int(r['quarter']) % 4 == 3:
                P[(s, r['nation'], int(r['year']))] = num(r['populationK'])
    return P


PA = popyear(ARM, seeds); PB = popyear(BASE, seeds)
print()
print('== [P5] 人口（年末・腕÷基線の国別の中央値・期ごと）')
print('国'.ljust(8) + ' '.join(p.center(11) for p in PN))
for n in NAT:
    cells = []
    for i in range(len(PER)):
        v = [PA[k] / PB[k] for k in PA if k[1] == n and per(k[2]) == i and k in PB and PB[k] > 0 and PA[k] == PA[k]]
        cells.append(fmt(med(v)).center(11) if v else '-'.center(11))
    print(n.ljust(8) + ' '.join(cells))
# 全体：世界の人口の和の比（1900-1950）
tot = collections.defaultdict(lambda: [0.0, 0.0])
for k in PA:
    if k in PB and PA[k] == PA[k] and PB[k] == PB[k]:
        tot[(k[0], k[2])][0] += PA[k]; tot[(k[0], k[2])][1] += PB[k]
rat = [v[0] / v[1] for (s, y), v in tot.items() if y in (1850, 1900, 1950) and v[1] > 0]
print('世界の人口の和の比（腕÷基線・1850・1900・1950 年の種ごと）： 最小 %s・中央値 %s・最大 %s' % (fmt(min(rat)) if rat else '-', fmt(med(rat)), fmt(max(rat)) if rat else '-'))
