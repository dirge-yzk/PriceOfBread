# (1306) 四半期ごとの系列（階級 c の食料の充足・参照・飢えの項・闘争度・充足）と州の予算・基盤の 3 生産者
# 使い方: python -X utf8 -I a2_series.py <out> <tag> <class>
import sys, csv, collections
out, tag, cls = sys.argv[1], sys.argv[2], sys.argv[3]
def load(p):
    with open(p, encoding='utf-8', newline='') as f: return list(csv.DictReader(f, delimiter='\t'))
pop = [r for r in load(f'{out}/pop_{tag}.tsv') if r['class'] == cls]
reg = load(f'{out}/reg_{tag}.tsv')
prod = [r for r in load(f'{out}/prod_{tag}.tsv') if r['recipe'] in ('水道局', '電灯網', 'ガス灯')]
def agg(rows, keys):
    d = collections.defaultdict(lambda: collections.defaultdict(float))
    for r in rows:
        k = (r['tax'], int(r['q']))
        sz = float(r['size']) if 'size' in r else 1.0
        d[k]['size'] += sz
        for x in keys:
            v = float(r[x]) if x != 'foodfill' else (min(1.0, float(r['foodMet']) / float(r['foodNeed'])) if float(r['foodNeed']) > 1e-9 else 1.0)
            d[k][x] += sz * v
    return d
P = agg(pop, ['foodfill', 'foodRef', 'milFamine', 'mil', 'sat', 'earned'])
R = collections.defaultdict(lambda: collections.defaultdict(float))
for r in reg:
    k = (r['tax'], int(r['q']))
    for x in ('infraBudget', 'elecInfraBudget', 'adminBudget', 'infraFill', 'elecInfraFill', 'treasury', 'pollPaid', 'pLabor', 'priceLevel'):
        R[k][x] += float(r[x])
U = collections.defaultdict(lambda: collections.defaultdict(float))
for r in prod:
    k = (r['tax'], int(r['q']))
    U[k][r['recipe'] + '_scale'] += float(r['scale']); U[k][r['recipe'] + '_rev'] += float(r['rev']); U[k][r['recipe'] + '_sm'] += float(r['smMargin'])
qs = sorted({q for (t, q) in P if t == '0'})
print(f'# {tag} class {cls}：各列 軽/重')
print(' q | foodfill      | foodRef       | famine        | mil           | infraBud            | elecBud             | 水道 scale    | ガス灯 scale  | 電灯 scale    | 水道 sm       | pollPaid')
for q in qs:
    def p(t, x): d = P.get((t, q)); return d[x] / d['size'] if d and d['size'] > 0 else float('nan')
    def g(D, t, x): d = D.get((t, q)); return d[x] if d else float('nan')
    print(f"{q:3d} | {p('0','foodfill'):.3f} {p('0.6','foodfill'):.3f} | {p('0','foodRef'):.3f} {p('0.6','foodRef'):.3f} | {p('0','milFamine'):.3f} {p('0.6','milFamine'):.3f} | {p('0','mil'):.3f} {p('0.6','mil'):.3f} | {g(R,'0','infraBudget'):9.4g} {g(R,'0.6','infraBudget'):9.4g} | {g(R,'0','elecInfraBudget'):9.4g} {g(R,'0.6','elecInfraBudget'):9.4g} | {g(U,'0','水道局_scale'):.3f} {g(U,'0.6','水道局_scale'):.3f} | {g(U,'0','ガス灯_scale'):.3f} {g(U,'0.6','ガス灯_scale'):.3f} | {g(U,'0','電灯網_scale'):.3f} {g(U,'0.6','電灯網_scale'):.3f} | {g(U,'0','水道局_sm'):+.3f} {g(U,'0.6','水道局_sm'):+.3f} | {g(R,'0','pollPaid'):.4g} {g(R,'0.6','pollPaid'):.4g}")
