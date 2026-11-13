# (1093) 上がった行の κ の分布（一次の反実仮想の大きさ・読むだけ）
import sys, math, statistics as st
from collections import defaultdict
o = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
o.write('arm\tyear\tnRise\tkMed\tkP90\tkP99\tvWmeanK\tshareK>2\tshareK>10\trhoMed\trhoP90\tvWmeanK_fall\tcbRise_kMed\n')
for a in sys.argv[2:]:
    f = open(a + '_rows.tsv', encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    K = defaultdict(list); V = defaultdict(float); VK = defaultdict(float); R = defaultdict(list); FV = defaultdict(float); FVK = defaultdict(float); CB = defaultdict(list)
    for ln in f:
        v = ln.rstrip('\n').split('\t')
        so = float(v[ix['sOld']]); sb = float(v[ix['sBoth']]); y = v[ix['year']]
        k = float(v[ix['kappa']]); out = float(v[ix['out']]); cap = float(v[ix['cap']]); px = float(v[ix['px']]) if v[ix['px']] else 0.0
        val = out * px
        if so > 0 and (sb - so) / so > 0.01:
            K[y].append(k); R[y].append(sb / so); V[y] += val; VK[y] += val * k
            if cap > 0 and out / cap >= 0.9: CB[y].append(k)
        elif sb < so * 0.99:
            FV[y] += val; FVK[y] += val * k
    f.close()
    for y in sorted(K):
        ks = sorted(K[y]); rs = sorted(R[y]); n = len(ks)
        q = lambda xs, p: xs[min(len(xs) - 1, int(p * len(xs)))]
        o.write('%s\t%s\t%d\t%.3f\t%.3f\t%.3f\t%.3f\t%.4f\t%.4f\t%.3f\t%.3f\t%.3f\t%.3f\n' % (a, y, n, st.median(ks), q(ks, .9), q(ks, .99), VK[y] / V[y] if V[y] > 0 else float('nan'),
                sum(1 for x in ks if x > 2) / n, sum(1 for x in ks if x > 10) / n, st.median(rs), q(rs, .9), FVK[y] / FV[y] if FV[y] > 0 else float('nan'), st.median(CB[y]) if CB[y] else float('nan')))
o.close()
