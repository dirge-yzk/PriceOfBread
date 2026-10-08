import sys
sys.path.insert(0,'.')
from an_g214 import *
d=sys.argv[1]; R=load(d)
for seed in map(int,sys.argv[2:]):
    W,O,N=R[seed]
    print('seed',seed)
    for yr in (1803,1823,1843,1853,1863,1873,1883,1893,1913):
        qs=[q for q,c in W.items() if int(c[2])==yr]
        if not qs: continue
        # average over the year's 4 quarters for flows
        def avg(i): return sum(f(W[q][i]) for q in qs)/len(qs)
        print(f" {yr} R全{avg(15):.2f} 産出Ag{avg(19):.3g} Au{avg(20):.3g} 需要Ag{avg(21):.3g} 在庫Ag{avg(23):.3g} 造幣買Ag{avg(25):.3g} 売りAg{avg(27):.3g} 準備Ag{avg(29):.3g} 金{W[qs[-1]][32]}銀{W[qs[-1]][33]}")
