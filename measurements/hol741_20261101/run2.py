# 尾の 23 件と英仏普の国内・距離の帯ごとの紙の計算
import sys; sys.path.insert(0,'D:/PoB_work/hol52_work/py')
from rw import *
base='D:/PoB_work/keep_736'
# 尾の事象
ev=[]; arm=None
for ln in open('C:/Users/dirge/Documents/PriceOfBread/measurements/rdh_20261101/tail_far.txt',encoding='utf-8'):
    ln=ln.rstrip('\n')
    if ln.startswith('### '): arm=ln[4:]; continue
    if not ln or ln.startswith('#'): continue
    c=ln.split('\t'); y0,y1=map(int,c[4].split('-'))
    ev.append((arm,int(c[0][1:]),c[1],c[2],c[3],y0,y1))
cache={}
def rows_of(arm,sd):
    k=(arm,sd)
    if k not in cache: cache[k]=load_mig(f'{base}/{arm}',sd)
    return cache[k]
print('## 尾の事象（窓の中の出）：変種ごとの 出の比（新÷今）・3000km超（大圏）の割合')
print('arm\tseed\tregion\tnation\ttier\twindow\tout\t'+'\t'.join(f'{v}_ratio\t{v}_far' for v in VN))
evres=collections.defaultdict(list)
for arm,sd,reg,nat,tier,y0,y1 in ev:
    rows=rows_of(arm,sd)
    sub={k:v for k,v in rows.items() if k[1]==reg and y0<=1783+k[0]//4<=y1}
    o,_=predict(sub)
    base0=o['gc'][0]
    cells=[]
    for v in VN:
        r=o[v][0]/base0 if base0 else float('nan'); fs=o[v][1]/o[v][0] if o[v][0] else float('nan')
        cells.append(f'{r:.3f}\t{fs:.3f}'); evres[v].append(r)
    print(f'{arm}\t{sd}\t{reg}\t{nat}\t{tier}\t{y0}-{y1}\t{base0:.1f}\t'+'\t'.join(cells))
for v in VN: print('# 中央値 出の比',v,f'{st.median(evres[v]):.3f}','最小',f'{min(evres[v]):.3f}','最大',f'{max(evres[v]):.3f}')
