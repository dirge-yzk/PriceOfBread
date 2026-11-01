import sys; sys.path.insert(0,'D:/PoB_work/hol52_work/py')
from rw import *
sys.path.insert(0,'C:/Users/dirge/Documents/PriceOfBread/scripts/instruments/hol'); import hol_common
d='D:/PoB_work/keep_736/base_ins'
rows=load_mig(d,1)
tot=far=miss=0; mfar=0; names=collections.Counter()
far2=0
for (q,s),lst in rows.items():
    for dd,f in lst:
        km=hol_common.dist(s,dd); tot+=f
        if km>3000: far+=f
        e=R.get((s,dd))
        if e is None:
            miss+=f; names[(s,dd)]+=f
            if km>3000: mfar+=f
        elif e['gc']>3000: far2+=f
print(tot,far/tot,'miss',miss,'missfar',mfar, 'far(routed)',far2)
print(names.most_common(15))
