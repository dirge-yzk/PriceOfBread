"""(322) 合流：反証役 fx〔bv〕#2 の処置＝同じ生の計器から読み手だけで出す（読むだけ・シミュは回さない）。
  python -X utf8 bv322.py <出力.md> A=<ins> D=<ins>
出す物：
  (1) 専売の段の往復（nation_s の monopolyTierEndStage が前の四半期より上がり次の四半期に下がった件数）を国ごとに・Q1〜Q40 と全期・D−A の大きい順。蘭の段の道筋（Q1〜Q40・種ごとの段の列）。
  (2) 英の大衆（農民・工場労働者・職人）の窃盗の純受け取り（flow_s の Crime.cs の give＋take）÷大衆の Paid（class_s の paidPerMonth）・Q5〜Q40 と Q41〜Q160・種ごと→中央。
限界の自己申告：往復の数え方は an_fx319_arms.py の F7 と同じ（国の個体は nationUid）。flow_s の額は四半期の月平均・class_s の paidPerMonth も月平均＝四半期ごとに和を取って比にする。
  窃盗の経路は sourceFile＝Crime.cs の全ての行（A と D とも 65 行だけ）。"""
import sys, os, statistics, collections
sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows as irows, seeds as iseeds, num

OUT = sys.argv[1]
ARMS = collections.OrderedDict(a.split('=', 1) for a in sys.argv[2:])
UK, NL = 'イギリス', 'ネーデルラント'
MASS = ('農民', '工場労働者', '職人')


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def rng(xs, nd=4):
    xs = [x for x in xs if x == x]
    return '—' if not xs else f'{statistics.median(xs):.{nd}f}（{min(xs):.{nd}f}〜{max(xs):.{nd}f}）'


def trips(seq, qmax):
    s = sorted(p for p in seq if p[0] < qmax)
    n = 0
    for i in range(1, len(s) - 1):
        if s[i][1] > s[i - 1][1] and s[i + 1][1] < s[i][1]: n += 1
    return n


res = {}
for arm, D in ARMS.items():
    sds = sorted(set(iseeds(D, 'nation')))
    tw = collections.defaultdict(list); name = {}
    for sd, r in irows(D, 'nation'):
        tv = num(r.get('monopolyTierEndStage', 'nan'))
        if tv == tv:
            k = (sd, r.get('nationUid', r['nation'])); tw[k].append((int(r['quarter']), tv)); name[k] = r['nation']
    byn40 = collections.Counter(); bynall = collections.Counter(); nlpath = {}
    for k, seq in tw.items():
        byn40[name[k]] += trips(seq, 40); bynall[name[k]] += trips(seq, 10 ** 6)
        if name[k] == NL: nlpath.setdefault(k[0], []).append(sorted(seq))
    cid = {}
    for sd, r in irows(D, 'class'):
        if r['nation'] == UK: cid[(sd, r['classId'])] = r['className']
    paid = collections.defaultdict(float); theft = collections.defaultdict(float)
    for sd, r in irows(D, 'class'):
        if r['nation'] == UK and r['className'] in MASS and r['time'].startswith('q'):
            q = int(r['time'][1:]); paid[(sd, q)] += num(r['paidPerMonth'])
    for sd, r in irows(D, 'flow'):
        if r['nation'] != UK or r['sourceFile'] != 'Crime.cs': continue
        if cid.get((sd, r['classId'])) not in MASS: continue
        theft[(sd, int(r['quarter']))] += num(r['amountPerMonth'])
    win = {}
    for wn, (a, b) in (('Q5〜Q40', (4, 40)), ('Q41〜Q160', (40, 160))):
        per = []
        for sd in sds:
            p = sum(paid[(sd, q)] for q in range(a, b)); t = sum(theft[(sd, q)] for q in range(a, b))
            per.append(t / p if p > 0 else float('nan'))
        win[wn] = per
    res[arm] = dict(byn40=byn40, bynall=bynall, nl=nlpath, win=win, seeds=sds)

arms = list(ARMS)
A, Dm = arms[0], arms[-1]
L = [f'# (322) 反証役 fx〔bv〕#2 の処置（読むだけ）・{" / ".join(f"{k}={v}" for k, v in ARMS.items())}', '']
L.append('## (1) 専売の段の往復（国ごと・種の和）'); L.append('')
for k in arms: L.append(f'- {k}：世界 Q1〜Q40 {sum(res[k]["byn40"].values())}・全期 {sum(res[k]["bynall"].values())}')
L.append(''); L.append(f'| 国 | {A} Q1〜Q40 | {Dm} Q1〜Q40 | {A} 全期 | {Dm} 全期 | 全期の差 |'); L.append('|---|---|---|---|---|---|')
ns = set(res[A]['bynall']) | set(res[Dm]['bynall'])
for n in sorted(ns, key=lambda n: -(res[Dm]['bynall'][n] - res[A]['bynall'][n]))[:15]:
    L.append(f'| {n} | {res[A]["byn40"][n]} | {res[Dm]["byn40"][n]} | {res[A]["bynall"][n]} | {res[Dm]["bynall"][n]} | {res[Dm]["bynall"][n] - res[A]["bynall"][n]:+d} |')
L.append(''); L.append(f'## 蘭の専売の段の道筋（Q1〜Q40・種ごと・段の列）'); L.append('')
for k in arms:
    L.append(f'### {k}')
    for sd in sorted(res[k]['nl']):
        for seq in res[k]['nl'][sd]:
            L.append(f'- s{sd}：' + ''.join(str(int(v)) for q, v in seq if q < 40) + f'（往復 Q1〜Q40 {trips(seq, 40)}・全期 {trips(seq, 10 ** 6)}）')
    L.append('')
L.append('## (2) 英の大衆の窃盗の純受け取り÷大衆の Paid（種の中央〔最小〜最大〕）'); L.append('')
L.append(f'| 窓 | ' + ' | '.join(arms) + ' |'); L.append('|---|' + '---|' * len(arms))
for wn in ('Q5〜Q40', 'Q41〜Q160'):
    L.append(f'| {wn} | ' + ' | '.join(rng(res[k]['win'][wn]) for k in arms) + ' |')
open(OUT, 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n')
print('\n'.join(L))
