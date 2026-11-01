# 使い捨て（hol52）：migration_s の組を経路 km で重み直す静的な予言（読むだけ）
import sys, os, math, collections, statistics as st
RD='D:/PoB_work/hol53_out/routedump_344.tsv'
K=10000.0
def dec(km): return 1.0/(1.0+km/K)
R={}
for ln in open(RD,encoding='utf-8'):
    c=ln.rstrip('\n').split('\t')
    if c[0]=='a': continue
    v=[float(x) for x in c[4:]]
    R[(c[0],c[1])]=dict(gc=v[0],land=v[1],sg=(v[2],v[3],v[4]),legA=v[5],legB=v[6],sp=(v[7],v[8],v[9]))
def canal(year): return 0 if year<1869 else (1 if year<1914 else 2)
def variants(e,year):
    k=canal(year); land=e['land']; sg=e['sg'][k]; sp=e['sp'][k]
    rail = 12.0 if year<1840 else 3.0   # 推：鉄道の門は国ごと・ここは年で粗く
    return {
      'gc':e['gc'],
      '2a_min':min(land,sg),
      '2a_port':min(land,sp),
      'landfirst':land if math.isfinite(land) else sg,
      '2b_pre':min(12*land,12*e['legA']+(sp-e['legA']-e['legB'])+12*e['legB']),
      '2b_rail':min(rail*land,rail*e['legA']+(sp-e['legA']-e['legB'])+rail*e['legB']),
    }
VN=['gc','2a_min','2a_port','landfirst','2b_pre','2b_rail']
def load_mig(d,sd):
    rows=collections.defaultdict(list)
    for ln in open(os.path.join(d,f'migration_s{sd}.tsv'),encoding='utf-8'):
        c=ln.rstrip('\n').split('\t')
        if len(c)!=4 or not c[0].isdigit(): continue
        rows[(int(c[0]),c[1])].append((c[2],float(c[3])))
    return rows
def predict(rows, sel=None):
    """sel(q,s,d)->bool で数える組を絞る。返り値＝変種ごとの（総量・3000km超〔大圏〕の量）"""
    out={v:[0.0,0.0] for v in VN}; miss=0
    for (q,s),lst in rows.items():
        yr=1783+q//4
        es=[]
        for d,f in lst:
            e=R.get((s,d))
            if e is None: miss+=f; continue
            es.append((d,f,e,variants(e,yr)))
        if not es: continue
        fmax=max(f for _,f,_,_ in es); tot=sum(f for _,f,_,_ in es)
        for v in VN:
            w=[f*dec(vv[v])/dec(e['gc']) for _,f,e,vv in es]
            newtot=tot*(max(w)/fmax); W=sum(w)
            for (d,f,e,vv),wi in zip(es,w):
                if sel and not sel(q,s,d): continue
                x=newtot*wi/W
                out[v][0]+=x
                if e['gc']>3000: out[v][1]+=x
    return out,miss
