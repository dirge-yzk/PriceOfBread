# (1172) c116t1 記録（案 B）：越境の輸入の量と額の年ごとの 10 種の和を、腕・基線・ヌル腕で並べる（採否に使わない・列名で読む）。
# 使い方: python -X utf8 -I impratio.py <腕の ins> <基線の ins> <n1> <n2>
import sys, os, csv, math, glob
csv.field_size_limit(1 << 30)
YEARS = (1830, 1870, 1913)
def sums(d, table, col):
    out = {y: 0.0 for y in YEARS}; out['all'] = 0.0; n = 0
    for p in sorted(glob.glob(os.path.join(d, f'{table}[0-9]*.tsv'))):
        with open(p, encoding='utf-8', newline='') as f:
            rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
            if col not in ix or 'year' not in ix: return None
            for r in rd:
                if len(r) < len(h): continue
                try: v = float(r[ix[col]])
                except ValueError: continue
                if not math.isfinite(v): continue
                y = int(r[ix['year']]); out['all'] += v; n += 1
                if y in out: out[y] += v
    return out
arm, base, n1, n2 = sys.argv[1:5]
for table, col in (('tb_s', 'impQ'), ('nationgood_s', 'crossImportQty'), ('nationgood_s', 'crossImportValue')):
    b = sums(base, table, col); a = sums(arm, table, col); x1 = sums(n1, table, col); x2 = sums(n2, table, col)
    print(f'{table}.{col}')
    if b is None or a is None: print('  列が無い'); continue
    for k in YEARS + ('all',):
        def r(v): return f'{v[k] / b[k]:.4f}' if v is not None and b[k] else '無し'
        print(f'  {k}: base {b[k]:.6g}・腕÷base {r(a)}・n1÷base {r(x1)}・n2÷base {r(x2)}')
