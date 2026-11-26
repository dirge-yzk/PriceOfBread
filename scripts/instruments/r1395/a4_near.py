# (1395) A4：海の点の組で〈格子の最短がほぼ大圏（陸を跨がない）〉なのに表が遠回りの組＝近い辺が無い候補
import csv, sys, math, collections
OUT = sys.argv[1]
def f(x): return float('inf') if x == 'inf' else (float('nan') if x == 'NaN' else float(x))
rs = list(csv.DictReader(open(f'{OUT}/a8_sea_nodes.tsv', encoding='utf-8'), delimiter='\t'))
o = open(f'{OUT}/a4_near_missing.tsv', 'w', encoding='utf-8', newline='\n')
o.write('nodeA\tnodeB\tgcKm\tsimKm\tgridKm\tsimOverGrid\n')
hits = []
for r in rs:
    gc, sm, gr = f(r['gcKm']), f(r['simKmClosed']), f(r['realGridKm'])
    if not (math.isfinite(sm) and math.isfinite(gr)) or gc <= 0: continue
    if gr / gc < 1.08 and gc < 3000 and sm / gr > 1.5: hits.append((sm - gr, r['nodeA'], r['nodeB'], gc, sm, gr))
hits.sort(reverse=True)
for d, a, b, gc, sm, gr in hits: o.write(f'{a}\t{b}\t{gc:.0f}\t{sm:.0f}\t{gr:.0f}\t{sm/gr:.2f}\n')
c = collections.Counter()
for h in hits: c[h[1]] += 1; c[h[2]] += 1
print(f'近い組（大圏 3000 km 未満・格子÷大圏 1.08 未満）で 表÷格子 1.5 超：{len(hits)} 組')
print('多く出る点：' + '・'.join(f'{k} {v}' for k, v in c.most_common(16)))
