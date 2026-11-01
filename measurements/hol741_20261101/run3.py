# 英仏普の本国の州から出る国内の移民（region_s で出る州の国と格を引く）・距離の帯の紙の計算
import sys; sys.path.insert(0,'D:/PoB_work/hol52_work/py')
from rw import *
base='D:/PoB_work/keep_736/base_ins'
HOME={'イギリス','フランス','プロイセン'}
def owner(sd):
    m={}
    with open(f'{base}/region_s{sd}.tsv',encoding='utf-8') as fh:
        next(fh)
        for ln in fh:
            c=ln.split('\t',6)
            m[(int(c[0]),c[4])]=(c[3],c[5])
    return m
agg={n:{v:[0.0,0.0] for v in VN} for n in HOME}
band=collections.defaultdict(lambda: collections.defaultdict(list))   # 帯→変種→(重み, km 比)
for sd in range(1,11):
    own=owner(sd); rows=load_mig(base,sd)
    for n in HOME:
        sub={k:v for k,v in rows.items() if own.get(k,('',''))[0]==n and own.get(k)[1]=='Core'}
        o,_=predict(sub)
        for v in VN: agg[n][v][0]+=o[v][0]; agg[n][v][1]+=o[v][1]
    # 帯ごと：全ての組（流れで重み）の 経路km÷大圏 の比
    for (q,s),lst in rows.items():
        yr=1783+q//4; on=own.get((q,s),('',''))
        grp='home' if on[0] in HOME and on[1]=='Core' else 'all'
        for d,f in lst:
            e=R[(s,d)]; vv=variants(e,yr); g=e['gc']
            for c0 in (500,1000,3000,6000,9000):
                if c0*0.8<=g<=c0*1.25:
                    for v in VN: band[(grp,c0)][v].append((f,vv[v]/g))
print('## 英仏普の本国の州から出る国内の移民（10 種の和）：変種ごとの総量の比（新÷今）・3000km超の割合')
for n in sorted(HOME):
    b=agg[n]['gc'][0]
    print(n, f'今 {b:.0f}千人', '\t'.join(f'{v} {agg[n][v][0]/b:.3f}/{agg[n][v][1]/agg[n][v][0]:.3f}' for v in VN))
def wmed(pairs):
    pairs=sorted(pairs,key=lambda x:x[1]); W=sum(p[0] for p in pairs); acc=0
    for f,r in pairs:
        acc+=f
        if acc>=W/2: return r
print('## 帯（大圏の ±20%）ごとの 経路km÷大圏 の流れ加重の中央値 と 減衰（今→新）')
for (grp,c0) in sorted(band):
    cells=[]
    for v in VN:
        r=wmed(band[(grp,c0)][v]); cells.append(f'{v} ×{r:.2f} 減衰 {dec(c0*r):.3f}')
    n=len(band[(grp,c0)]['gc'])
    print(grp, c0, f'組×四半期 {n}', ' | '.join(cells))
