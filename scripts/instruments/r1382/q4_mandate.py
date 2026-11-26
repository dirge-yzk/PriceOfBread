# (1382) 問い 4：不満度の国の集計の分布（dcont・大衆の層・人口の重み）＝D̄ の下限 0.8×Pos(livingDrop の平均)・上限 今の discontentMeanShare（D1 は声の項を抜くので層ごとに今の D 以下）
#   ⚠ D̄_u の重み（1−識字）は生に無い＝大衆の層の人口の重みで代える〈推〉。今の天命 Θ は scalars の dynMandate。
import sys, collections; sys.path.insert(0, 'D:/PoB_work/r1382_work/scripts')
from load import *
ERAS = [(1783, 1830), (1830, 1870), (1870, 1914), (1914, 1951)]
lo, hi, th = collections.defaultdict(list), collections.defaultdict(list), collections.defaultdict(list)
natlo = collections.defaultdict(list); nathi = collections.defaultdict(list)
for s in SEEDS:
    for v in rows('dcont', s):
        y = int(v['year']); e = next(i for i, (a, b) in enumerate(ERAS) if a <= y < b)
        l = 0.8 * max(0.0, fl(v['livingDropMeanIndex'])); h = fl(v['discontentMeanShare'])
        if l == l: lo[e].append(l); lo['all'].append(l); natlo[v['nation']].append(l)
        if h == h: hi[e].append(h); hi['all'].append(h); nathi[v['nation']].append(h)
    for v in rows('scalars', s):
        y = int(v['year']); e = next(i for i, (a, b) in enumerate(ERAS) if a <= y < b); t = fl(v['dynMandate'])
        if t == t and t >= 0: th[e].append(t); th['all'].append(t)
P = (.5, .9, .99)
for k in ['all', 0, 1, 2, 3]:
    nm = 'all' if k == 'all' else f'{ERAS[k][0]}-{ERAS[k][1]-1}'
    print(nm, '| D̄ 下限', pct(lo[k], P), '| 上限', pct(hi[k], P), '| 今の Θ', pct(th[k], (.1, .5, .9)))
# 国ごと：上限の p90 が大きい国の上から 8 国
top = sorted(((pct(nathi[n], (.5, .9)), n, sorted(nathi[n])[int(.9 * len(nathi[n]))]) for n in nathi), key=lambda x: -x[2])[:8]
for p_, n, _ in top: print('  国', n, '上限', p_, '下限', pct(natlo[n], (.5, .9)))
# 案ごとの e の倍率（e = 係数 × 未教育 × 媒介 の〈係数〉部分）：今 (Θ−0.5)×2・Θ3 (1−D̄)・Θ4 (1−2D̄)。D̄ は上限で
import statistics
cur = [(t - 0.5) * 2 for t in th['all']]
print('e の係数  今', pct(cur, (.1, .5, .9)), ' Θ3（上限の D̄）', pct([1 - x for x in hi['all']], (.1, .5, .9)), ' Θ4', pct([1 - 2 * x for x in hi['all']], (.1, .5, .9)))
print('上限の D̄ > 0.25 の国×四半期', f"{sum(x > 0.25 for x in hi['all'])/len(hi['all']):.2%}", ' > 0.5', f"{sum(x > 0.5 for x in hi['all'])/len(hi['all']):.3%}")
