# 3000km超（大圏）の国内の移民を 出る州の格×入る州の格 で分ける（base_ins・10種の和）
import sys; sys.path.insert(0,'D:/PoB_work/hol52_work/py')
from rw import *
base='D:/PoB_work/keep_736/base_ins'
cnt=collections.Counter(); tot=collections.Counter(); top=collections.Counter()
for sd in range(1,11):
    own={}
    with open(f'{base}/region_s{sd}.tsv',encoding='utf-8') as fh:
        next(fh)
        for ln in fh:
            c=ln.split('\t',6); own[(int(c[0]),c[4])]=(c[3],c[5])
    for (q,s),lst in load_mig(base,sd).items():
        ts=own.get((q,s),('?','?'))
        for d,f in lst:
            td=own.get((q,d),('?','?'))[1]
            key=(ts[1],td); tot[key]+=f
            if R[(s,d)]['gc']>3000:
                cnt[key]+=f
                if ts[0]=='イギリス' and ts[1]=='Core': top[(s,d)]+=f
T=sum(tot.values()); F=sum(cnt.values())
print('総量',f'{T:.0f}','3000km超',f'{F:.0f}')
for k in sorted(tot,key=lambda k:-cnt[k]): print(k,f'全 {tot[k]/T:.3f}',f'遠 {cnt[k]/F:.3f}',f'遠÷全 {cnt[k]/tot[k]:.3f}')
print('英の本国→3000km超の上位', [(a,b,round(v)) for (a,b),v in top.most_common(8)])
