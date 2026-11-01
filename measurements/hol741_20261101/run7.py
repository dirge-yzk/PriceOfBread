import sys; sys.path.insert(0,'D:/PoB_work/hol52_work/py')
from run6 import *
base='D:/PoB_work/keep_736'
ev=[]; arm=None
for ln in open('C:/Users/dirge/Documents/PriceOfBread/measurements/rdh_20261101/tail_far.txt',encoding='utf-8'):
    ln=ln.rstrip('\n')
    if ln.startswith('### '): arm=ln[4:]; continue
    if not ln or ln.startswith('#'): continue
    c=ln.split('\t'); y0,y1=map(int,c[4].split('-')); ev.append((arm,int(c[0][1:]),c[1],y0,y1))
rat=collections.defaultdict(list)
print('arm\tseed\tregion\tnow\tadd_gc\tadd_route\tadd_route_nodecay')
for arm,sd,reg,y0,y1 in ev:
    r,_=run(f'{base}/{arm}',sd,sel=lambda q,s,d,reg=reg,y0=y0,y1=y1: s==reg and y0<=1783+q//4<=y1)
    b=r['now'][0]
    print(arm,sd,reg,f'{b:.1f}','\t'.join(f'{r[k][0]/b:.3f}' if b else 'nan' for k in ['add_gc','add_route','add_route_nodecay']),sep='\t')
    for k in ['add_gc','add_route','add_route_nodecay']:
        if b: rat[k].append(r[k][0]/b)
for k,v in rat.items(): print('# 尾の出の比 中央値',k,f'{st.median(v):.3f}',f'{min(v):.3f}〜{max(v):.3f}')
HOME={'イギリス','フランス','プロイセン'}
agg={n:collections.defaultdict(float) for n in HOME}
for sd in range(1,11):
    ot=opp_table(f'{base}/base_ins',sd)
    for n in HOME:
        r,_=run(f'{base}/base_ins',sd,sel=lambda q,s,d,n=n,ot=ot: ot.get((q,s),(0,0,'',''))[2]==n and ot[(q,s)][3]=='Core')
        for k,v in r.items(): agg[n][k]+=v[0]
for n in sorted(HOME): print('#',n,'今',f"{agg[n]['now']:.0f}",' '.join(f'{k} {agg[n][k]/agg[n]["now"]:.3f}' for k in ['add_gc','add_route','add_route_nodecay']))
