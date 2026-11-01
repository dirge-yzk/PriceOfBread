import collections, statistics as st
from hol53 import *
base='D:/PoB_work/keep_736'
ev=[]; arm=None
for ln in open('C:/Users/dirge/Documents/PriceOfBread/measurements/rdh_20261101/tail_far.txt',encoding='utf-8'):
    ln=ln.rstrip('\n')
    if ln.startswith('### '): arm=ln[4:]; continue
    if not ln or ln.startswith('#'): continue
    c=ln.split('\t'); y0,y1=map(int,c[4].split('-')); ev.append((arm,int(c[0][1:]),c[1],y0,y1))
byk=collections.defaultdict(list)
for i,(a,sd,reg,y0,y1) in enumerate(ev): byk[(a,sd)].append((i,reg,y0,y1))
rat=collections.defaultdict(list); rows=[]
for (a,sd),L in byk.items():
    cats={f'e{i}':(lambda q,s,d,ot,reg=reg,y0=y0,y1=y1: s==reg and y0<=1783+q//4<=y1) for i,reg,y0,y1 in L}
    res,_,_=run(f'{base}/{a}',sd,cats)
    for i,reg,y0,y1 in L:
        r=res[f'e{i}']; b=r['now'][0]
        rows.append((a,sd,reg,b,{v:(r[v][0]/b if b else float('nan')) for v in VN}))
        for v in VN:
            if b: rat[v].append(r[v][0]/b)
for a,sd,reg,b,d in rows: print(a,sd,reg,f'{b:.1f}',' '.join(f'{v} {x:.3f}' for v,x in d.items()),sep='\t')
for v in VN: print('# 尾の出の比',v,f'中央値 {st.median(rat[v]):.3f}',f'{min(rat[v]):.3f}〜{max(rat[v]):.3f}', '1未満',sum(1 for x in rat[v] if x<1),'/',len(rat[v]))
