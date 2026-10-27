import sys,os,collections,statistics as st
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
import gates_dsc6 as g6, gates_dsc6b as g
from gates_dsc6 import MAJOR,SPREAD
ins=sys.argv[1]   # 使い方：python clump_composition.py <ins ディレクトリ>（種 21〜30・研究／伝わりで開いた出来事の割合を、国ごとの最大の窓〔M>=5〕の中と全体で比べる＝記録）
tot=collections.Counter(); inwin=collections.Counter(); wins=0
tech_mean=[];lit=[]
for s in range(21,31):
    a=g6.analyse(ins,s); ev=g.events_terms(os.path.join(ins,'disc_s%d.tsv'%s))
    op={k:q for k,q in a['ev'].items() if k[1] not in SPREAD}
    for k in op:
        t=ev[k]; tot['res' if t[1]+t[2]>=t[3] else 'trn']+=1
    for n in MAJOR:
        l=sorted((q,k) for k,q in op.items() if k[0]==n)
        best=0;bw=None
        for i,(q0,_) in enumerate(l):
            w=[k for q,k in l[i:] if q<=q0+3]
            if len(w)>best: best=len(w);bw=w
        if best>=5:
            wins+=1
            for k in bw:
                t=ev[k]; inwin['res' if t[1]+t[2]>=t[3] else 'trn']+=1
print('all events',dict(tot),' share research %.3f'%(tot['res']/sum(tot.values())))
print('events in nation-max windows with M>=5 (n=%d windows)'%wins,dict(inwin),' share research %.3f'%(inwin['res']/max(1,sum(inwin.values()))))
