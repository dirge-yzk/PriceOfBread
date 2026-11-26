# (1395) A8：誤差の上限 max(850 km, 実際の 10%) を超える組の数（格子＝陸マスク 0.1°・16 方向）
import csv, sys, math, collections
OUT = sys.argv[1]
def load(n): return list(csv.DictReader(open(f'{OUT}/{n}', encoding='utf-8'), delimiter='\t'))
def f(x):
    return float('inf') if x == 'inf' else (float('nan') if x == 'NaN' else float(x))
res = open(f'{OUT}/a8_summary.txt', 'w', encoding='utf-8', newline='\n')
def P(*a): print(*a, file=res)
for name, simc, realc in (('a8_sea_ports.tsv', 'simKmClosed', 'realGridKm'), ('a8_sea_nodes.tsv', 'simKmClosed', 'realGridKm'), ('a8_land.tsv', 'simLandKm', 'realGridKm'), ('a8_route.tsv', 'simSeaRouteKm', 'real2SameExitsKm')):
    rs = load(name); n = len(rs)
    both = [r for r in rs if math.isfinite(f(r[simc])) and math.isfinite(f(r[realc]))]
    simOnly = sum(1 for r in rs if math.isfinite(f(r[simc])) and not math.isfinite(f(r[realc])))
    realOnly = sum(1 for r in rs if not math.isfinite(f(r[simc])) and math.isfinite(f(r[realc])))
    over = [r for r in both if abs(f(r[simc]) - f(r[realc])) > max(850, 0.1 * f(r[realc]))]
    overHi = [r for r in over if f(r[simc]) > f(r[realc])]
    ratio = sorted(f(r[simc]) / f(r[realc]) for r in both if f(r[realc]) > 0)
    q = lambda p: ratio[min(len(ratio) - 1, int(p * len(ratio)))] if ratio else float('nan')
    P(f'== {name}: 組 {n}・両方有限 {len(both)}・シミュだけ有限 {simOnly}・格子だけ有限 {realOnly}・上限超え {len(over)}（シミュが長い {len(overHi)}・短い {len(over) - len(overHi)}）・比 シミュ÷格子 中央 {q(0.5):.3f} p10 {q(0.1):.3f} p90 {q(0.9):.3f} p99 {q(0.99):.3f}')
    if 'simBelowGc' in rs[0]: P(f'   シミュ＜大圏 {sum(1 for r in rs if r["simBelowGc"] == "1")}')
    if name in ('a8_sea_ports.tsv', 'a8_land.tsv', 'a8_route.tsv'):
        key = ('portA', 'portB') if name == 'a8_sea_ports.tsv' else ('regionA', 'regionB')
        cnt = collections.Counter()
        for r in over: cnt[r[key[0]]] += 1; cnt[r[key[1]]] += 1
        P('   上限超えに多く出る名：' + '・'.join(f'{k} {v}' for k, v in cnt.most_common(15)))
    top = sorted(over, key=lambda r: -abs(f(r[simc]) - f(r[realc])))[:12]
    for r in top: P('   ' + '\t'.join(r[k] for k in list(r.keys())[:9]))
