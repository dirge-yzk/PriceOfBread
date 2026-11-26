# (1395) 陸マスクで辺を作った海の図〈AI の案〉と今の図の、誤差の上限を超える組の数の比べ
import csv, sys, math
OUT = sys.argv[1]
def f(x): return float('inf') if x == 'inf' else (float('nan') if x == 'NaN' else float(x))
o = open(f'{OUT}/a8_mask_summary.txt', 'w', encoding='utf-8', newline='\n')
for name, sc in (('a8_sea_nodes.tsv', 'simKmClosed'), ('a8_sea_ports.tsv', 'simKmClosed')):
    rs = [r for r in csv.DictReader(open(f'{OUT}/{name}', encoding='utf-8'), delimiter='\t') if math.isfinite(f(r['realGridKm']))]
    cur = sum(1 for r in rs if r['over'] == '1'); msk = sum(1 for r in rs if r['maskOver'] == '1')
    rc = sorted(f(r[sc]) / f(r['realGridKm']) for r in rs if f(r['realGridKm']) > 0 and math.isfinite(f(r[sc])))
    rm = sorted(f(r['maskKm']) / f(r['realGridKm']) for r in rs if f(r['realGridKm']) > 0 and math.isfinite(f(r['maskKm'])))
    q = lambda l, p: l[min(len(l) - 1, int(p * len(l)))]
    below = sum(1 for r in rs if math.isfinite(f(r['maskKm'])) and f(r['maskKm']) < f(r['gcKm']) - 1e-6)
    o.write(f'{name}: 格子で届く組 {len(rs)}・上限超え 今の図 {cur}・陸マスクの図 {msk}｜比（図÷格子）今 中央 {q(rc,.5):.3f} p90 {q(rc,.9):.3f} max {rc[-1]:.2f}｜陸マスク 中央 {q(rm,.5):.3f} p90 {q(rm,.9):.3f} max {rm[-1]:.2f}｜陸マスクの図が大圏より短い組 {below}\n')
    worst = sorted(rs, key=lambda r: -(f(r['maskKm']) - f(r['realGridKm'])))[:8]
    for r in worst: o.write('   陸マスクの図の最悪 ' + '\t'.join([list(r.values())[0], list(r.values())[1], r['gcKm'], r['maskKm'], r['realGridKm']]) + '\n')
