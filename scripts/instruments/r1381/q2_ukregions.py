# (1381) 読む線：英の本国の州の工場の産出（region_s）と工場労働者（merch_s）の年の平均。使い方 python q2_ukregions.py <ins> <種> <年の始め> <年の終わり> <出力> [州,州,...]
import sys, collections
d, sd, y0, y1, out = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), int(sys.argv[4]), sys.argv[5]
regs = (sys.argv[6] if len(sys.argv) > 6 else 'バーミンガム,ロンドン,マンチェスター,グラスゴー,ダブリン').split(',')
f = lambda s: float(s) if s not in ('', 'NaN') else 0.0
A = collections.defaultdict(lambda: collections.defaultdict(list)); seen = set()
with open('%s/region_s%d.tsv' % (d, sd), encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); I = {k: i for i, k in enumerate(h)}
    for l in fh:
        c = l.rstrip('\n').split('\t'); r = c[I['region']]
        if r not in regs: continue
        y = int(c[I['year']])
        if not (y0 <= y <= y1) or (c[0], r) in seen: continue
        seen.add((c[0], r))
        A[(y, r)]['fout'].append(f(c[I['factoryOutputRefValuePerMonth']])); A[(y, r)]['pop'].append(f(c[I['populationK']])); A[(y, r)]['food'].append(f(c[I['foodFillShare']]))
seen = set()
with open('%s/merch_s%d.tsv' % (d, sd), encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); I = {k: i for i, k in enumerate(h)}
    for l in fh:
        c = l.rstrip('\n').split('\t'); r = c[I['region']]
        if r not in regs: continue
        y = int(c[I['year']])
        if not (y0 <= y <= y1) or (c[0], r) in seen: continue
        seen.add((c[0], r)); A[(y, r)]['work'].append(f(c[I['workerK']]))
m = lambda v: sum(v) / len(v) if v else 0.0
with open(out, 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('year\t' + '\t'.join('%s_%s' % (r, k) for r in regs for k in ('fout', 'work', 'perW', 'pop', 'food')) + '\n')
    for y in range(y0, y1 + 1):
        row = []
        for r in regs:
            a = A[(y, r)]; fo_, wk = m(a['fout']), m(a['work'])
            row += ['%.0f' % fo_, '%.0f' % wk, '%.1f' % (fo_ / wk if wk > 0 else 0), '%.0f' % m(a['pop']), '%.2f' % m(a['food'])]
        fo.write('%d\t' % y + '\t'.join(row) + '\n')
