# r789: 財ごとの州の値段÷参照（年平均）を州の間で並べる（競争相手の読み）。引数：<ins> <種> <財,...> <州,...> <年0> <年1> [年の刻み]
import sys, collections
sys.stdout.reconfigure(encoding='utf-8')
D, sd, goods, regs, y0, y1 = sys.argv[1], sys.argv[2], sys.argv[3].split(','), sys.argv[4].split(','), int(sys.argv[5]), int(sys.argv[6])
st = int(sys.argv[7]) if len(sys.argv) > 7 else 1
A = collections.defaultdict(lambda: [0.0, 0])
with open(f'{D}/r789g_s{sd}.tsv', encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t')
    for l in fh:
        d = dict(zip(h, l.rstrip('\n').split('\t')))
        if d['good'] not in goods or d['region'] not in regs: continue
        y = int(d['year'])
        if not (y0 <= y <= y1): continue
        a = A[(d['good'], d['region'], y)]; a[0] += float(d['price']) / float(d['refPrice']); a[1] += 1
for g in goods:
    print('==', g, '値段÷参照'); print('year\t' + '\t'.join(regs))
    for y in range(y0, y1 + 1, st):
        print(y, *[f'{A[(g, r, y)][0]/A[(g, r, y)][1]:.3f}' if A[(g, r, y)][1] else '-' for r in regs], sep='\t')
