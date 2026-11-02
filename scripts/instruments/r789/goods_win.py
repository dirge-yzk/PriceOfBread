# r789: 州×財の 2 つの窓（各 4 年・四半期の断面の平均）の比べ。売上(LastRevenue)・付加価値の近似(売上−産出×単位投入費)・産出・値段÷参照・販売・輸出・輸入・在庫
import sys, collections
sys.stdout.reconfigure(encoding='utf-8')
D, sd, REG = sys.argv[1], sys.argv[2], sys.argv[3]
A0, A1, B0, B1 = map(int, sys.argv[4:8])
acc = {w: collections.defaultdict(lambda: collections.defaultdict(float)) for w in 'AB'}
nq = {'A': set(), 'B': set()}
with open(f'{D}/r789g_s{sd}.tsv', encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t')
    for l in fh:
        d = dict(zip(h, l.rstrip('\n').split('\t')))
        if d['region'] != REG: continue
        y = int(d['year']); w = 'A' if A0 <= y <= A1 else 'B' if B0 <= y <= B1 else None
        if not w: continue
        nq[w].add(d['quarter'])
        f = lambda k: (lambda v: v if v == v else 0.0)(float(d[k]))
        a = acc[w][d['good']]
        o = f('outputSum')
        a['rev'] += f('lastRevenueSum'); a['cost'] += f('lastCostSum'); a['out'] += o; a['ic'] += o * f('inputCostPerUnitW'); a['lc'] += o * f('laborCostPerUnitW')
        a['pr'] += f('price'); a['ref'] = f('refPrice'); a['sold'] += f('sold'); a['dem'] += f('demand'); a['stock'] += f('stock')
        a['exp'] += f('prevExport') + f('lastExport'); a['imp'] += f('prevImport') + f('lastImport'); a['nAct'] += f('nActive'); a['bk'] += f('nBankrupt'); a['heads'] += f('headsSum')
        a['tier'] = d['goodTier']
for w in 'AB':
    n = len(nq[w])
    for g in acc[w]:
        for k in list(acc[w][g]):
            if k not in ('ref', 'tier'): acc[w][g][k] /= n
goods = set(acc['A']) | set(acc['B'])
Z = collections.defaultdict(float)
def va(a): return a['rev'] - a['ic']
rows = []
for g in goods:
    a, b = acc['A'].get(g, Z), acc['B'].get(g, Z)
    rows.append((va(b) - va(a), g, a, b))
rows.sort()
tA = sum(va(r[2]) for r in rows); tB = sum(va(r[3]) for r in rows)
print(f'窓A {A0}-{A1}（{len(nq["A"])} 四半期）・窓B {B0}-{B1}（{len(nq["B"])} 四半期）・付加価値の近似の和 A {tA:.0f} → B {tB:.0f}（差 {tB-tA:+.0f}）')
print('good\ttier\tdVA\tVA_A\tVA_B\trev_A\trev_B\tout_A\tout_B\tp/ref_A\tp/ref_B\tic/u_A\tic/u_B\tlab/u_A\tlab/u_B\tsold_A\tsold_B\tdem_A\tdem_B\texp_A\texp_B\timp_A\timp_B\tstock_A\tstock_B\theads_A\theads_B\tbk_A\tbk_B')
for d, g, a, b in rows:
    if abs(d) < 0.01 * max(1, abs(tB - tA)) and abs(va(a)) < 0.02 * abs(tA): continue
    pr = lambda x: x['pr'] / x['ref'] if x.get('ref') else float('nan')
    u = lambda x, k: x[k] / x['out'] if x['out'] > 0 else float('nan')
    print(g, a.get('tier', b.get('tier', '')), f'{d:+.0f}', f"{va(a):.0f}", f"{va(b):.0f}", f"{a['rev']:.0f}", f"{b['rev']:.0f}", f"{a['out']:.1f}", f"{b['out']:.1f}", f'{pr(a):.3f}', f'{pr(b):.3f}',
          f"{u(a,'ic'):.3f}", f"{u(b,'ic'):.3f}", f"{u(a,'lc'):.3f}", f"{u(b,'lc'):.3f}", f"{a['sold']:.1f}", f"{b['sold']:.1f}", f"{a['dem']:.1f}", f"{b['dem']:.1f}", f"{a['exp']:.1f}", f"{b['exp']:.1f}", f"{a['imp']:.1f}", f"{b['imp']:.1f}",
          f"{a['stock']:.1f}", f"{b['stock']:.1f}", f"{a['heads']:.1f}", f"{b['heads']:.1f}", f"{a['bk']:.1f}", f"{b['bk']:.1f}", sep='\t')
