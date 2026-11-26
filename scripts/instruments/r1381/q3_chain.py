# (1381) 読む線：1 本の腕×種で、バーミンガムの鎖（線路→パンの輸入への依存→吸い込み→産出の取り分→線路の必要→食→移民）を 1 行に並べる。
# 使い方 python q3_chain.py <ins> <種> <腕の名> <q3_sink_table.tsv> >> 出力 （見出しは引数 --header で出す）
import sys, collections
if sys.argv[1] == '--header':
    print('arm\tseed\tminOverPeak\trail1830\trail1840\trail1850\trail1860\tneedSh1845\tneedSh1850\tneedSh1855\tbreadImpSh1840\tbreadImpSh1850\twheatProd1830\twheatProd1840\tbreadProd1840\tsink1840\tsink1850\tfirstFoodBelow0.9\tfoodMin1845_1865\tmig1845_1865\tfout1844\tfout1849\tfout1855'); sys.exit()
d, sd, arm, st = sys.argv[1], int(sys.argv[2]), sys.argv[3], sys.argv[4]; B = 'バーミンガム'
f = lambda s: float(s) if s not in ('', 'NaN') else 0.0
P = {}; A = collections.defaultdict(lambda: collections.defaultdict(list))
with open('%s/region_s%d.tsv' % (d, sd), encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); I = {k: i for i, k in enumerate(h)}
    for l in fh:
        c = l.rstrip('\n').split('\t')
        if c[I['region']] != B: continue
        q = int(c[0])
        if q in P: continue
        P[q] = f(c[I['populationK']]); y = int(c[I['year']])
        A[y]['food'].append(f(c[I['foodFillShare']])); A[y]['mig'].append(f(c[I['migrationOutKPerQuarter']])); A[y]['fout'].append(f(c[I['factoryOutputRefValuePerMonth']]))
mx = 0; mop = 1.0
for q in sorted(P):
    mx = max(mx, P[q]); mop = min(mop, P[q] / mx if mx > 0 else 1)
T = {}
with open('%s/trrail_s%d.tsv' % (d, sd), encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); I = {k: i for i, k in enumerate(h)}
    for l in fh:
        c = l.rstrip('\n').split('\t')
        if c[I['region']] == B: T[int(c[0])] = (f(c[I['railTrackEnd']]), f(c[I['needOutShareEnd']]))
S = {}
with open('%s/sellr_s%d.tsv' % (d, sd), encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); I = {k: i for i, k in enumerate(h)}
    for l in fh:
        c = l.rstrip('\n').split('\t')
        if c[I['region']] != B or c[I['good']] not in ('パン', '麦'): continue
        k = (int(c[0]), c[I['good']])
        if k not in S: S[k] = (f(c[I['lastDemand']]), f(c[I['lastProduced']]), f(c[I['prevImportAll']]))
SK = {}
an = {'M': 'M', 'N1': 'N1', 'N2': 'N2', 'S11': 'S11'}[arm]
for l in open(st, encoding='utf-8').readlines()[1:]:
    c = l.rstrip('\n').split('\t')
    if c[0] == an and int(c[1]) == sd and c[2] == B: SK = {1840: c[4], 1850: c[5]}
m = lambda y, k: sum(A[y][k]) / len(A[y][k]) if A[y][k] else 0.0
fb = next((y for y in range(1800, 1950) if A[y]['food'] and m(y, 'food') < 0.9), '')
fm = min(((m(y, 'food'), y) for y in range(1845, 1866) if A[y]['food']), default=(0, ''))
bi = lambda y: S[(y, 'パン')][2] / max(1, S[(y, 'パン')][0]) if (y, 'パン') in S else 0
row = [arm, str(sd), '%.3f' % mop] + ['%.0f' % T.get(y, (0, 0))[0] for y in (1830, 1840, 1850, 1860)] + ['%.3f' % T.get(y, (0, 0))[1] for y in (1845, 1850, 1855)] \
    + ['%.2f' % bi(1840), '%.2f' % bi(1850), '%.0f' % S.get((1830, '麦'), (0, 0, 0))[1], '%.0f' % S.get((1840, '麦'), (0, 0, 0))[1], '%.0f' % S.get((1840, 'パン'), (0, 0, 0))[1], SK.get(1840, ''), SK.get(1850, ''), str(fb), '%.2f@%s' % fm, '%.0f' % sum(sum(A[y]['mig']) for y in range(1845, 1866)), '%.0f' % m(1844, 'fout'), '%.0f' % m(1849, 'fout'), '%.0f' % m(1855, 'fout')]
print('\t'.join(row))
