# r789: 棟ごとの表（r789p）を 2 つの窓（年の範囲・四半期の断面の平均）で比べる。財×(数・稼働・倒産・規模・産出・頭数・売上・費用・単位の自己費用・投入費・労働費・値段)。
# 引数：<ins> <種> <州> <A0> <A1> <B0> <B1>
import sys, collections
sys.stdout.reconfigure(encoding='utf-8')
D, sd, REG = sys.argv[1], sys.argv[2], sys.argv[3]
A0, A1, B0, B1 = map(int, sys.argv[4:8])
acc = {w: collections.defaultdict(lambda: collections.defaultdict(float)) for w in 'AB'}
nq = {'A': set(), 'B': set()}
with open(f'{D}/r789p_s{sd}.tsv', encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t')
    for l in fh:
        d = dict(zip(h, l.rstrip('\n').split('\t')))
        if d['region'] != REG: continue
        y = int(d['year']); w = 'A' if A0 <= y <= A1 else 'B' if B0 <= y <= B1 else None
        if not w: continue
        nq[w].add(d['quarter'])
        k = d['good'] + ('' if d['recipe'] == '-' else '/' + d['recipe']) + ('(RGO)' if d['isRgo'] == '1' else '')
        a = acc[w][k]
        g = lambda c: (lambda v: v if v == v else 0.0)(float(d[c]))
        o = g('output')
        a['n'] += 1; a['bk'] += 1 if d['bankruptFlag'] == '1' else 0; a['act'] += 1 if (d['bankruptFlag'] == '0' and (g('scale') != 0 or o != 0)) else 0
        a['scale'] += g('scale'); a['built'] += g('builtScale'); a['cap'] += g('capacity'); a['out'] += o; a['heads'] += g('heads')
        a['rev'] += g('lastRevenue'); a['cost'] += g('lastCost'); a['mc'] += o * g('mcSelf'); a['ic'] += o * g('inputCostPerUnit'); a['lc'] += o * g('laborCostPerUnit'); a['pr'] += g('price')
        a['fill'] += o * g('inputFill'); a['steam'] += g('steamFill')
for w in 'AB':
    n = len(nq[w])
    for k in acc[w]:
        for c in acc[w][k]: acc[w][k][c] /= n
print(f'窓A {A0}-{A1}（{len(nq["A"])} 四半期）・窓B {B0}-{B1}（{len(nq["B"])} 四半期）・四半期あたりの平均')
Z = collections.defaultdict(float)
keys = sorted(set(acc['A']) | set(acc['B']), key=lambda k: -(acc['A'].get(k, Z)['rev']))
T = {w: collections.defaultdict(float) for w in 'AB'}
print('good\tn_A\tn_B\tact_A\tact_B\tbk_A\tbk_B\tbuilt_A\tbuilt_B\tout_A\tout_B\theads_A\theads_B\trev_A\trev_B\tcost_A\tcost_B\tp_A\tp_B\tmc/u_A\tmc/u_B\tic/u_A\tic/u_B\tlab/u_A\tlab/u_B\tfill_A\tfill_B')
for k in keys:
    a, b = acc['A'].get(k, Z), acc['B'].get(k, Z)
    for w, x in (('A', a), ('B', b)):
        for c in ('n', 'act', 'bk', 'out', 'heads', 'rev', 'cost', 'built'): T[w][c] += x[c]
    u = lambda x, c: x[c] / x['out'] if x['out'] > 0 else float('nan')
    pr = lambda x: x['pr'] / x['n'] if x['n'] else float('nan')
    print(k, *[f'{v:.1f}' for v in (a['n'], b['n'], a['act'], b['act'], a['bk'], b['bk'], a['built'], b['built'], a['out'], b['out'], a['heads'], b['heads'], a['rev'], b['rev'], a['cost'], b['cost'])],
          *[f'{v:.3f}' for v in (pr(a), pr(b), u(a, 'mc'), u(b, 'mc'), u(a, 'ic'), u(b, 'ic'), u(a, 'lc'), u(b, 'lc'), u(a, 'fill'), u(b, 'fill'))], sep='\t')
print('TOTAL', *[f"{T['A'][c]:.1f}\t{T['B'][c]:.1f}" for c in ('n', 'act', 'bk', 'built', 'out', 'heads', 'rev', 'cost')], sep='\t')
