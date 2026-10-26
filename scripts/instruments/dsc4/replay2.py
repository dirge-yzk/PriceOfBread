# 発見の式の作り直し：census の系列を外から与えた量として積み上げを再生する（紙の上の計算・盤面の応答は無い）
import sys, os, math, collections
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
from load2 import *
TERMMAX=70.0
def sat(x,h): return TERMMAX*x/(x+h) if x==x and x>0 and h>0 else 0.0

# 新しい木（Catalog.cs の DscAfter / DscAfter2 と同じ・番人が突き合わせる）
TREE={'魚の缶詰':['缶詰'],'鉄筋コンクリート':['鋳鉄の骨組み'],'無煙火薬':['ダイナマイト'],'水力発電':['石炭火力'],'蒸気タービン':['石炭火力'],'石油火力':['石炭火力'],
 '窒素固定':['過リン酸石灰'],'合成ゴム':['加硫ゴム'],'送電網':['石炭火力'],'内燃機関':['蒸気機関'],'装甲艦':['汽船'],'弩級戦艦':['装甲艦','蒸気タービン'],'戦車':['内燃機関'],
 '軍用機':['内燃機関'],'共産主義':['社会主義'],'ソルベー法':['ルブラン法'],'電解':['ソルベー法']}
SPREAD={'特許','株式会社','社会主義','共産主義','帝国主義','ファシズム'}
WEAPON={'装甲艦','機関銃','弩級戦艦','戦車','軍用機','化学兵器'}

class Data:
    def __init__(self,path):
        self.N,self.R,self.P,self.Q=load2(path)
        self.qs=sorted({q for (q,_) in self.N})
        self.nats=sorted({n for (_,n) in self.N})
        self.rows=sorted({r for (_,_,r) in self.R})
        self.rec=collections.defaultdict(list)
        for (q,y,dst,src,h,pool) in self.Q: self.rec[(q,dst)].append((src,h,pool))
        q0=self.qs[0]
        self.known0={n:{r for r in self.rows if self.R.get((q0,n,r),{}).get('known')==1} for n in self.nats}
        self.year={q:d['year'] for (q,n),d in self.N.items()}

def savings(d,qty):
    v,c,ru=d['v'],d['cost'],d['RU']
    if not(v==v and c==c and v>0 and c>0 and v>c and qty==qty and qty>0 and ru>0): return 0.0
    return (v-c)/ru*qty

def run(D,K,Hr=0.005,Ht=0.5,Hs=100.0,wl=0.5,D0=0.03,tree=TREE,needmode='sav',cap=TERMMAX,transmode='rec+trade',Ha=0.25):
    known={n:set(D.known0[n]) for n in D.nats}
    prog={}
    opened={}
    comp=collections.defaultdict(lambda:[0.0,0.0,0.0])   # (row)->tech,need,trans の積み
    for q in D.qs:
        dt=2.0
        newly=[]
        for n in D.nats:
            Nn=D.N.get((q,n))
            if not Nn: continue
            lit=Nn['lit']; dens=Nn['engCore']/max(Nn['popCore'],1e-9)
            T=TERMMAX*(wl*lit+(1-wl)*(dens/(dens+D0)))
            for r in D.rows:
                if r in known[n]: continue
                d=D.R.get((q,n,r))
                if d is None: continue
                par=tree.get(r,[])
                locked=any(p not in known[n] for p in par)
                # 伝わり
                X=0.0
                if 'rec' in transmode:
                    y=0.0
                    for (src,h,pool) in D.rec.get((q,n),[]):
                        if r in known.get(src,()) and pool>1e-9: y+=h/pool
                    X+=sat(y,Hr)
                if 'trade' in transmode:
                    X+=sat(d['xN'],Ht)
                if 'any' in transmode:
                    xa=sum(sh for sname,sh in D.P.get((q,n),{}).items() if sname in known and r in known[sname])
                    X+=sat(xa,Ha)
                X=min(X,cap)
                if locked: inc=X
                else:
                    if r in WEAPON or r in SPREAD or needmode=='old': Nd=d['need']
                    elif r=='機関車': Nd=d['need']   # 鉄道の節約は census に無い＝旧の値で代用（記録）
                    else: Nd=sat(savings(d,d['dem']),Hs)
                    inc=T+Nd+X
                    c=comp[r]; c[0]+=T*dt/K; c[1]+=Nd*dt/K; c[2]+=X*dt/K
                prog[(n,r)]=prog.get((n,r),0.0)+inc*dt/K
                if prog[(n,r)]>=100.0: newly.append((n,r))
        for (n,r) in newly:
            known[n].add(r); opened[(n,r)]=q
    return opened,known
