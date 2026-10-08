import sys,statistics as st
sys.path.insert(0,'.')
from an_g214 import *
arms=sys.argv[1:]
D={a:load('../out_'+a) for a in arms}
def at(W,yr,i):
    qs=[q for q,c in W.items() if int(c[2])==yr]
    return f(W[max(qs)][i]) if qs else float('nan')
def yavg(W,lo,hi,i):
    v=[f(c[i]) for q,c in W.items() if lo<=int(c[2])<=hi]
    return sum(v)/len(v) if v else float('nan')
print('指標 | '+' | '.join(arms))
for yr in (1843,1873,1893,1913):
    print(f'金の国の数 {yr} 中央(種別) | '+' | '.join(f"{st.median([at(R[s][0],yr,32) for s in R]):.0f} ({','.join(str(int(at(R[s][0],yr,32))) for s in R)})" for R in D.values()))
for yr in (1843,1873,1893,1913):
    print(f'同じ州の比 {yr} 中央(種別) | '+' | '.join(f"{st.median([at(R[s][0],yr,15) for s in R]):.2f} ({','.join(f'{at(R[s][0],yr,15):.2f}' for s in R)})" for R in D.values()))
for lo,hi in ((1783,1842),(1843,1872),(1873,1913)):
    print(f'銀の産出 年平均 {lo}-{hi} 中央 | '+' | '.join(f"{st.median([yavg(R[s][0],lo,hi,19) for s in R]):.3g}" for R in D.values()))
for lo,hi in ((1783,1842),(1843,1872),(1873,1913)):
    print(f'金の産出 年平均 {lo}-{hi} 中央 | '+' | '.join(f"{st.median([yavg(R[s][0],lo,hi,20) for s in R]):.3g}" for R in D.values()))
for a,R in D.items():
    ys=[]
    for s in R:
        for o in R[s][1]:
            if o[5]=='金' and o[4]=='オーストラリア': ys.append(int(o[2]))
    print(a,'ヴィクトリア(豪の金)の開いた年',sorted(ys))
