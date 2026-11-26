import csv, sys, math, collections
OUT = sys.argv[1]
def f(x): return float('inf') if x == 'inf' else (float('nan') if x == 'NaN' else float(x))
o = open(f'{OUT}/a8_more.txt', 'w', encoding='utf-8', newline='\n')
def P(*a): print(*a, file=o)
rs = list(csv.DictReader(open(f'{OUT}/a8_sea_ports.tsv', encoding='utf-8'), delimiter='\t'))
c = collections.Counter()
for r in rs:
    if not math.isfinite(f(r['realGridKm'])): c[r['portA']] += 1; c[r['portB']] += 1
P('海：格子で届かない港（組の数）：' + '・'.join(f'{k} {v}' for k, v in c.most_common(12)))
both = [r for r in rs if math.isfinite(f(r['realGridKm']))]
over = [r for r in both if r['over'] == '1']
b = collections.Counter()
for r in over:
    q = f(r['simKmClosed']) / f(r['realGridKm'])
    b['<1.2' if q < 1.2 else '1.2-1.5' if q < 1.5 else '1.5-2' if q < 2 else '2-4' if q < 4 else '>=4'] += 1
P('海：上限超えの比の分布：' + '・'.join(f'{k} {b[k]}' for k in ['<1.2', '1.2-1.5', '1.5-2', '2-4', '>=4']))
s = collections.Counter()
for r in over:
    rl = f(r['realGridKm']); s['<2000' if rl < 2000 else '2000-5000' if rl < 5000 else '5000-10000' if rl < 10000 else '>=10000'] += 1
P('海：上限超えの実際の距離の分布：' + '・'.join(f'{k} {s[k]}' for k in ['<2000', '2000-5000', '5000-10000', '>=10000']))
offs = sorted({(r['portA'], f(r['snapOffA'])) for r in rs} | {(r['portB'], f(r['snapOffB'])) for r in rs}, key=lambda x: -x[1])
P('海：港から格子の海の升までの距離 上位：' + '・'.join(f'{k} {v:.0f}' for k, v in offs[:8]))
# 縫い代：港から一番近い海の点まで（シミュの seaHalf）＝ sim − 表 の差を港ごとに
# 迂回の大物を除いた残り：比が 2 以上の港を含む組を除く
bad = collections.Counter()
for r in both:
    if f(r['simKmClosed']) / f(r['realGridKm']) >= 2: bad[r['portA']] += 1; bad[r['portB']] += 1
badp = {k for k, v in bad.items() if v >= 60}
rest = [r for r in both if r['portA'] not in badp and r['portB'] not in badp]
P(f'海：比 2 以上の組が 60 以上ある港 {len(badp)}：' + '・'.join(sorted(badp)))
P(f'海：それらを除いた組 {len(rest)}・上限超え {sum(1 for r in rest if r["over"] == "1")}・比の中央 {sorted(f(r["simKmClosed"]) / f(r["realGridKm"]) for r in rest)[len(rest) // 2]:.3f}')
lr = list(csv.DictReader(open(f'{OUT}/a8_land.tsv', encoding='utf-8'), delimiter='\t'))
so = collections.Counter(); go = collections.Counter()
for r in lr:
    a, g = math.isfinite(f(r['simLandKm'])), math.isfinite(f(r['realGridKm']))
    if a and not g: so[r['regionA']] += 1; so[r['regionB']] += 1
    if g and not a: go[r['regionA']] += 1; go[r['regionB']] += 1
P('陸：シミュだけ有限に多い州：' + '・'.join(f'{k} {v}' for k, v in so.most_common(10)))
P('陸：格子だけ有限に多い州：' + '・'.join(f'{k} {v}' for k, v in go.most_common(10)))
rt = list(csv.DictReader(open(f'{OUT}/a8_route.tsv', encoding='utf-8'), delimiter='\t'))
# 物理の最短（R1＝どの港でも）と、シミュの海の道の km の比
q = sorted(f(r['simSeaRouteKm']) / f(r['real1AnyPortKm']) for r in rt if math.isfinite(f(r['simSeaRouteKm'])) and math.isfinite(f(r['real1AnyPortKm'])) and f(r['real1AnyPortKm']) > 0)
P(f'道：シミュの海の道 km ÷ 物理の最短（どの港でも・陸も可）：中央 {q[len(q)//2]:.3f} p90 {q[int(.9*len(q))]:.3f}・組 {len(q)}')
