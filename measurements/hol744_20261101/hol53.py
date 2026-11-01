# 波53 段0 (744)：入口の形 (a)(b) の紙の計算（読むだけ・使い捨て）。rw.py の経路の表＋region_s の機会・保持・格＋migration_s の組。
# 時代の係数 era(年)＝1＋clamp((年−1850)/63)【推：TransportRevolution のランプ onset1.7≈1850・満了 2.9≈1913 の注から年へ写した】
import sys, os, collections, statistics as st, math
sys.path.insert(0,'D:/PoB_work/hol53_out')
from rw import R, variants, K
sys.path.insert(0,'C:/Users/dirge/Documents/PriceOfBread/scripts/instruments/hol'); import hol_common
MISS=collections.Counter()
def E(s,d):
    e=R.get((s,d))
    if e is None:
        g=hol_common.dist(s,d); MISS[(s,d)]+=1
        e=dict(gc=g,land=float('inf'),sg=(g,g,g),legA=0.0,legB=0.0,sp=(g,g,g)); R[(s,d)]=e
    return e
FR=0.15
def era(y): return 1.0+min(1.0,max(0.0,(y-1850)/63.0))
def decnow(km,y): return 1.0/(1.0+km/(K*era(y)))
NAT=['イギリス','フランス','プロイセン','スペイン','オランダ','ロシア','アメリカ']
def km_of(v,e,yr):
    vv=variants(e,yr); k=0 if yr<1869 else (1 if yr<1914 else 2)
    if v=='a_gc' or v=='a_gc_noera': return e['gc']
    if v=='a_rt': return vv['2a_port']
    sea=e['sp'][k]-e['legA']-e['legB']; land=e['land']
    if v.startswith('b1'):   # 陸の尺＝K・海の尺＝r×K
        r=12.0 if v=='b1_12' else 3.6
        via=e['legA']+sea/r+e['legB']; return min(land,via) if math.isfinite(land) else via
    if v=='b2':              # 海の尺＝K・陸の尺＝K/r
        r=3.6; via=r*e['legA']+sea+r*e['legB']; return min(r*land,via) if math.isfinite(land) else via
VN=['a_gc','a_gc_noera','a_rt','b1','b1_12','b2']
def opp_table(d,sd):
    m={}
    with open(f'{d}/region_s{sd}.tsv',encoding='utf-8') as fh:
        next(fh)
        for ln in fh:
            c=ln.split('\t',12); m[(int(c[0]),c[4])]=(float(c[9]),float(c[10]),c[3],c[5])
    return m
def load_mig(d,sd):
    rows=collections.defaultdict(list)
    for ln in open(os.path.join(d,f'migration_s{sd}.tsv'),encoding='utf-8'):
        c=ln.rstrip('\n').split('\t')
        if len(c)!=4 or not c[0].isdigit(): continue
        rows[(int(c[0]),c[1])].append((c[2],float(c[3])))
    return rows
def run(d,sd,cats):
    """cats: 名前→ 関数(q,s,d,ot)->bool。返り値 res[cat][var]=[総量, 3000km超の量, 流れのある組の数]・全体の再構成の外（g≦0）"""
    ot=opp_table(d,sd); rows=load_mig(d,sd)
    res={c:{v:[0.0,0.0,0] for v in ['act','now']+VN} for c in cats}
    lost=[0.0,0.0]; negpair={v:[0,0] for v in ['now']+VN}
    for (q,s),lst in rows.items():
        yr=1783+q//4
        for dd,f in lst:
            for c,fn in cats.items():
                if fn(q,s,dd,ot):
                    a=res[c]['act']; a[0]+=f; a[2]+=1
                    if E(s,dd)['gc']>3000: a[1]+=f
        if (q,s) not in ot: continue
        os_,hs,_,_=ot[(q,s)]
        es=[]
        for dd,f in lst:
            lost[1]+=f
            if (q,dd) not in ot: lost[0]+=f; continue
            g=ot[(q,dd)][0]-os_*hs-FR
            if not g>0: lost[0]+=f; continue
            e=E(s,dd); es.append((dd,f,e,g))
        if not es: continue
        fmax=max(x[1] for x in es); tot=sum(x[1] for x in es)
        def apply(name,wfun):
            w=[max(0.0,wfun(f,e,g)) for _,f,e,g in es]
            negpair[name][0]+=sum(1 for x in w if x<=0); negpair[name][1]+=len(w)
            if max(w)<=0: return
            nt=tot*max(w)/fmax; W=sum(w)
            for (dd,f,e,g),wi in zip(es,w):
                x=nt*wi/W
                for c,fn in cats.items():
                    if fn(q,s,dd,ot):
                        r=res[c][name]; r[0]+=x; r[2]+=(1 if x>0 else 0)
                        if e['gc']>3000: r[1]+=x
        apply('now',lambda f,e,g: f)
        for v in VN:
            if v=='a_gc_noera': apply(v,lambda f,e,g: f*(g-FR*e['gc']/K)/g/(1.0/(1.0+e['gc']/K)))
            else: apply(v,lambda f,e,g,v=v: f*(g-FR*km_of(v,e,yr)/(K*era(yr)))/g/decnow(e['gc'],yr))
    return res,lost,negpair
def tier(ot,q,r): return ot.get((q,r),('','','',''))
CATS={
 'world': lambda q,s,d,ot: True,
 'colsrc': lambda q,s,d,ot: tier(ot,q,s)[3]=='Colonial',
}
for n in NAT:
    CATS[n+'_core→col']=lambda q,s,d,ot,n=n: tier(ot,q,s)[2]==n and tier(ot,q,s)[3]=='Core' and tier(ot,q,d)[2]==n and tier(ot,q,d)[3]=='Colonial'
    CATS[n+'_core→core']=lambda q,s,d,ot,n=n: tier(ot,q,s)[2]==n and tier(ot,q,s)[3]=='Core' and tier(ot,q,d)[2]==n and tier(ot,q,d)[3]=='Core'
if __name__=='__main__':
    import pickle
    arm=sys.argv[1]; out=sys.argv[2]
    allr={}
    for sd in range(1,11):
        allr[sd]=run(arm,sd,CATS)
        print('seed',sd,'done',flush=True)
    pickle.dump(allr,open(out,'wb')); print('missing pairs',len(MISS),MISS.most_common(5))
