# (950) 読むだけ：分かれる前の州の secession_s の項（圧力の部品）を、腕と旗 0 の盤面で区間の平均で並べる。エンジンの関数を呼ばない。
#   python -X utf8 -I r950_sec.py <種> <州> <始め> <終わり> <名>=<ins フォルダ> ...
import sys, os
s, rg, q0, q1 = int(sys.argv[1]), sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
K = ['militancyMeanIndex', 'militancyRegionOwnIndex', 'militancyNationMeanIndex', 'crowdFactorShare', 'consciousnessMeanIndex', 'consciousnessFactorShare', 'suffrageFactorShare',
     'tariffTermIndex', 'ethnicTermIndex', 'legitimacyIndex', 'coerceIndex', 'holdBaseIndex', 'pressureIndex', 'holdShareIndex', 'marginIndex', 'restiveCount', 'dominanceShare']
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
print('名\t行\t' + '\t'.join(K))
for a in sys.argv[5:]:
    nm, d = a.split('=', 1); acc = [0.0] * len(K); n = 0
    with open(os.path.join(d, f'secession_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.rstrip('\n').split('\t')
            if x[ix['region']] != rg: continue
            q = int(x[ix['quarter']])
            if not (q0 <= q <= q1): continue
            n += 1
            for j, k in enumerate(K): v = fl(x[ix[k]]); acc[j] += v if v == v else 0.0
    print(nm, n, *(f'{v / n:.4g}' if n else '-' for v in acc), sep='\t')
