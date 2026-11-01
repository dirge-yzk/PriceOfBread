# 参考（確証なし）：尺 K を変えた場合の静的な予言（大圏と 2a_port）＝km を替えるだけでは届かない幅を測る
import sys; sys.path.insert(0,'D:/PoB_work/hol52_work/py')
import rw
from rw import *
base='D:/PoB_work/keep_736/base_ins'
rows={sd:load_mig(base,sd) for sd in range(1,11)}
def pred(rows,Knew,v):
    out=[0.0,0.0]
    for (q,s),lst in rows.items():
        yr=1783+q//4
        es=[(d,f,R[(s,d)]) for d,f in lst]
        fmax=max(f for _,f,_ in es); tot=sum(f for _,f,_ in es)
        w=[f*(1/(1+variants(e,yr)[v]/Knew))/dec(e['gc']) for _,f,e in es]
        nt=tot*max(w)/fmax; W=sum(w)
        for (d,f,e),wi in zip(es,w):
            x=nt*wi/W; out[0]+=x
            if e['gc']>3000: out[1]+=x
    return out
for v in ['gc','2a_port']:
    for Kn in [10000,5000,3000,2000,1000]:
        fs=[];fv=[];tt=[]
        for sd in range(1,11):
            o=pred(rows[sd],Kn,v); fs.append(o[1]/o[0]); fv.append(o[1]); tt.append(o[0])
        print(v,Kn,f'far {st.median(fs):.3f}',f'farVol {st.median(fv):.0f}',f'tot {st.median(tt):.0f}')
