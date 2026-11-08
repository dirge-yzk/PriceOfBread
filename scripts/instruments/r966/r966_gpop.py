# (966) G の国（<頭>_g.tsv）ごとに、G の最初の四半期の前 8 四半期の人口の最大と G の最初の人口、外資の負債の同じ 2 時点を並べる（割譲で負債が残るかの確かめ）。
import sys, os
dirs = dict(a.split('=', 1) for a in sys.argv[2:])
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
rows = [l.rstrip('\n').split('\t') for l in open(sys.argv[1], encoding='utf-8')][1:]
print('盤面\t種\t国\t分\tGの最初\t人口(前8の最大)\t人口(G)\t人口の比\tfci(前8の最大)\tfci(G)\tfci(G+40)')
for r in rows:
    b, s, n, sec, g0 = r[0], int(r[1]), r[2], r[3], int(r[6]); d = dirs[b]
    P = {}; F = {}
    with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.rstrip('\n').split('\t')
            if x[ix['nation']] == n: P[int(x[ix['quarter']])] = fl(x[ix['populationK']])
    with open(os.path.join(d, f'divhost_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.rstrip('\n').split('\t')
            if x[ix['nation']] == n: F[int(x[ix['quarter']])] = fl(x[ix['fci']])
    pb = max([P[q] for q in range(g0 - 8, g0) if q in P] or [float('nan')]); pg = P.get(g0, float('nan'))
    fb = max([F[q] for q in range(g0 - 8, g0) if q in F] or [float('nan')])
    print(b, s, n, sec, g0, f'{pb:.4g}', f'{pg:.4g}', f'{pg/pb:.3g}', f'{fb:.3g}', f'{F.get(g0, float("nan")):.3g}', f'{F.get(g0+40, float("nan")):.3g}', sep='\t')
