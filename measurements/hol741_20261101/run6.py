# 入口の形の紙の計算：差 g＝機会_d−機会_s×保持_s−摩擦 0.15 を region_s の四半期の断面から作り、距離を差から引く形（摩擦×km/尺）の静的な予言
import sys; sys.path.insert(0,'D:/PoB_work/hol52_work/py')
from rw import *
FR=0.15
def opp_table(d,sd):
    m={}
    with open(f'{d}/region_s{sd}.tsv',encoding='utf-8') as fh:
        next(fh)
        for ln in fh:
            c=ln.split('\t',12); m[(int(c[0]),c[4])]=(float(c[9]),float(c[10]),c[3],c[5])
    return m
def run(d,sd,sel=None):
    ot=opp_table(d,sd); rows=load_mig(d,sd)
    res={k:[0.0,0.0] for k in ['now','add_gc','add_route','add_route_nodecay']}; bad=[0.0,0.0]
    for (q,s),lst in rows.items():
        yr=1783+q//4
        if (q,s) not in ot: continue
        os_,hs,_,_=ot[(q,s)]
        es=[]
        for dd,f in lst:
            if (q,dd) not in ot: continue
            g=ot[(q,dd)][0]-os_*hs-FR
            bad[1]+=f
            if not g>0: bad[0]+=f; continue
            e=R[(s,dd)]; es.append((dd,f,e,g,variants(e,yr)['2a_port']))
        if not es: continue
        fmax=max(x[1] for x in es); tot=sum(x[1] for x in es)
        def apply(name,wfun):
            w=[max(0.0,wfun(f,e,g,rk)) for _,f,e,g,rk in es]
            if max(w)<=0: return
            nt=tot*max(w)/fmax; W=sum(w)
            for (dd,f,e,g,rk),wi in zip(es,w):
                if sel and not sel(q,s,dd): continue
                x=nt*wi/W; res[name][0]+=x
                if e['gc']>3000: res[name][1]+=x
        apply('now',lambda f,e,g,rk: f)
        apply('add_gc',lambda f,e,g,rk: f*(g-FR*e['gc']/K)/g)
        apply('add_route',lambda f,e,g,rk: f*(g-FR*rk/K)/g)
        apply('add_route_nodecay',lambda f,e,g,rk: f*(g-FR*rk/K)/g/dec(e['gc']))
    return res,bad
if __name__=='__main__':
    base='D:/PoB_work/keep_736/base_ins'
    agg=collections.defaultdict(list)
    for sd in range(1,11):
        r,bad=run(base,sd)
        for k,v in r.items(): agg[k].append((v[0],v[1]))
        print('seed',sd,'g≦0 で流れのある量の割合',f'{bad[0]/bad[1]:.3f}', ' '.join(f'{k} {v[0]:.0f}/{v[1]/v[0]:.3f}' for k,v in r.items()))
    for k,v in agg.items():
        print('#',k,'tot med',f'{st.median(x[0] for x in v):.0f}','far share med',f'{st.median(x[1]/x[0] for x in v):.3f}','far vol med',f'{st.median(x[1] for x in v):.0f}')
