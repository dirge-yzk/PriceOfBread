# (1273) 正規化の定数の紙：開幕の無統治のデータ RuleInit[3] と、今の P1-1・候補 S の目標の水準を並べる（読むだけ）
# 呼び方：python -X utf8 -I p1273_calib.py <base_ins> <出力>
# ①最初の四半期：首都以外の人口加重の 今の目標・RuleInit[3]・候補 S の目標（qS を倍率 k で動かす）・目標の平均が RuleInit[3] の平均に合う k
# ②期間ごとの実際の Rule[3]（nowRule3Share・全ての州・人口加重）
import sys, math, collections
BASE = sys.argv[1].rstrip('/') + '/'
OUT = sys.argv[2]


def rows(t, s):
    with open(BASE + f'{t}_s{s}.tsv', 'rb') as f:
        head = f.readline().decode('utf-8').rstrip('\r\n').split('\t')
        ix = {k: i for i, k in enumerate(head)}
        for line in f:
            yield ix, line.decode('utf-8').rstrip('\r\n').split('\t')


def fl(x):
    try:
        return float(x)
    except ValueError:
        return float('nan')


def c01(x):
    return 0.0 if not (x > 0) else (1.0 if x > 1 else x)


def order(R, mbar, mr):
    return 1.0 if not (mr > 0) else c01(R * mbar / mr)


def period(y):
    return '1783-1815' if y <= 1815 else '1816-1870' if y <= 1870 else '1871-1913' if y <= 1913 else '1914-1950'


out = []
KS = [0.25, 0.35, 0.5, 0.7, 1.0, 1.4, 2.0, 2.8, 4.0]
agg = collections.defaultdict(float)
per = collections.defaultdict(lambda: [0.0, 0.0, 0.0])
for s in range(1, 11):
    nat = {}
    for ix, r in rows('nation', s):
        nat[(r[ix['quarter']], r[ix['nation']])] = (fl(r[ix['soldierK']]), fl(r[ix['totalPopulationK']]))
    q0 = min(nat, key=lambda k: int(k[0]))[0]
    qS = sum(v[0] for k, v in nat.items() if k[0] == q0) / sum(v[1] for k, v in nat.items() if k[0] == q0)
    init3 = {}
    for ix, r in rows('rulepower', s):
        y = int(r[ix['year']]); pk = fl(r[ix['popK']]); u = fl(r[ix['nowRule3Share']])
        if r[ix['quarter']] == q0:
            init3[(r[ix['nation']], r[ix['region']])] = fl(r[ix['ruleInit3Share']])
        if pk > 0 and math.isfinite(u):
            a = per[period(y)]; a[0] += pk * u; a[1] += pk
    for ix, r in rows('secession', s):
        if r[ix['quarter']] != q0:
            continue
        nn, rg = r[ix['nation']], r[ix['region']]
        pk = fl(r[ix['popK']]); reach = fl(r[ix['reachOwnShare']]); tnew = fl(r[ix['ungovTargetNewShare']])
        mbar = fl(r[ix['militancyNationMeanIndex']]); mr = fl(r[ix['militancyRegionOwnIndex']])
        i3 = init3.get((nn, rg), float('nan'))
        sk, pkn = nat.get((q0, nn), (float('nan'), float('nan')))
        if not all(math.isfinite(x) for x in (pk, reach, tnew, i3, sk, pkn)) or not (pkn > 0):
            agg['skip'] += 1; continue
        agg['pop'] += pk; agg['cur'] += pk * tnew; agg['init'] += pk * i3; agg['n'] += 1
        agg['reachgap'] += pk * (1 - reach)
        for k in KS:
            RS = (sk / pkn) / (qS * k)
            agg[('S', k)] += pk * (1 - order(RS, mbar, mr)) * (1 - reach)
    out.append(f'seed {s} qS={qS:.6f}')

P = agg['pop']
out.append(f'# 最初の四半期・首都以外・種 1〜10 を合わせた人口加重（行 {int(agg["n"])}・飛ばした {int(agg["skip"])}）')
out.append(f'今の P1-1 の無統治の目標 {agg["cur"] / P:.4f}・開幕のデータ RuleInit[3] {agg["init"] / P:.4f}・到達の欠け (1−到達) {agg["reachgap"] / P:.4f}（＝秩序 0 の時の目標の上限）')
for k in KS:
    out.append(f'候補 S・qS×{k}: 目標 {agg[("S", k)] / P:.4f}')
out.append('# 実際の Rule[3]（全ての州・人口加重・nowRule3Share）')
for pr in ('1783-1815', '1816-1870', '1871-1913', '1914-1950'):
    a = per[pr]
    out.append(f'{pr}\t{a[0] / a[1]:.4f}')
with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
    f.write('\n'.join(out) + '\n')
