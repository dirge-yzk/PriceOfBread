# dsc5：census（旗0・種 2・3）の系列を外から与えて、新しい形（行ごとの技術の項 FieldTech・行ごとに年 1 回の判定・J 倍の積み）の積み上げを再生する（紙の上の計算・盤面の応答は無い）。
# 事前登録 measurements/dsc5_20261027/prereg_dsc5.txt [10] の規則（D0・A0・K の決め方）を実行する道具。
import sys, os, math, collections, statistics as st
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from load3 import *
TERMMAX=70.0
MAJOR=['イギリス','フランス','プロイセン','アメリカ','ロシア','オーストリア','オスマン','清','日本']
TREE={'魚の缶詰':['缶詰'],'鉄筋コンクリート':['鋳鉄の骨組み'],'無煙火薬':['ダイナマイト'],'水力発電':['石炭火力'],'蒸気タービン':['石炭火力'],'石油火力':['石炭火力'],
 '窒素固定':['過リン酸石灰'],'合成ゴム':['加硫ゴム'],'送電網':['石炭火力'],'内燃機関':['蒸気機関'],'装甲艦':['汽船'],'弩級戦艦':['装甲艦','蒸気タービン'],'戦車':['内燃機関'],
 '軍用機':['内燃機関'],'ソルベー法':['ルブラン法'],'電解':['ソルベー法']}
SPREAD={'特許','株式会社','社会主義','共産主義','帝国主義','ファシズム'}
WEAPON={'装甲艦','機関銃','弩級戦艦','戦車','軍用機','化学兵器'}
def sat(x,h): return TERMMAX*x/(x+h) if x==x and x>0 and h>0 else 0.0
def fieldtech(lit,popk,a,wL=0.4,wS=0.2,D0=0.03,A0=100.0,e=0.5):
    lit=min(1.0,max(0.0,lit if lit==lit else 0.0))
    dens=a/popk if (popk==popk and popk>1e-9 and a==a and a>0) else 0.0
    satD=dens/(dens+D0) if dens>0 and D0>0 else 0.0
    z=(a/A0)**e if (a==a and a>0 and A0>0 and e>0) else 0.0
    satS=z/(1+z) if z>0 else 0.0
    if wL+wS>1: wS=1-wL
    wD=1-wL-wS
    return min(TERMMAX,max(0.0,TERMMAX*(wL*lit+wD*satD+wS*satS)))
def savings(d,qty):
    v,c,ru=d['v'],d['cost'],d['RU']
    if not(v==v and c==c and v>0 and c>0 and v>c and qty==qty and qty>0 and ru>0): return 0.0
    return (v-c)/ru*qty
class Data:
    def __init__(self,path):
        self.N,self.R,self.P,self.Q=load3(path)
        self.qs=sorted({q for (q,_) in self.N})
        self.nats=sorted({n for (_,n) in self.N})
        self.rows=sorted({r for (_,_,r) in self.R})
        self.rowidx={r:i for i,r in enumerate(self.rows)}
        self.rec=collections.defaultdict(list)
        for (q,y,dst,src,h,pool) in self.Q: self.rec[(q,dst)].append((src,h,pool))
        q0=self.qs[0]
        self.known0={n:{r for r in self.rows if self.R.get((q0,n,r),{}).get('known')==1} for n in self.nats}
        self.year={q:d['year'] for (q,n),d in self.N.items()}
        # 制度と思想の行の開いた四半期（census の実走から・今の門のまま）
        self.spread_open={}
        for q in self.qs:
            for n in self.nats:
                for r in SPREAD:
                    d=self.R.get((q,n,r))
                    if d and d['known']==1 and (n,r) not in self.spread_open and r not in self.known0[n]: self.spread_open[(n,r)]=q
        # catalog 順の行の添字（位相は Catalog.Discoveries の添字）。census の行名の順は辞書順なので、位相の添字は別に与える
CATALOG_ORDER=None
def set_catalog_order(names):
    global CATALOG_ORDER
    CATALOG_ORDER={n:i for i,n in enumerate(names)}
def phase_judged(row,q,J):
    """この標本の四半期 q（偶数）で、行 row の判定を使うか（実際の判定の四半期 q*≡−row mod J が q か q+1 のとき）。使うときの q* を返す。"""
    if J<=1: return q
    i=CATALOG_ORDER[row]
    d=(-i-q)%J
    if d==0: return q
    if d==1: return q+1
    return None
def run(D,K,J=4,wL=0.4,wS=0.2,D0=0.03,A0=100.0,e=0.5,Hr=0.005,Hs=100.0,Ha=0.25,tree=TREE):
    wt=J if J>1 else 2   # 標本は偶数の四半期＝J＝1 のときは 1 標本が 2 四半期ぶん
    known={n:set(D.known0[n]) for n in D.nats}
    prog={}; opened={}; comp=collections.defaultdict(lambda:[0.0,0.0,0.0])
    for q in D.qs:
        newly=[]
        for n in D.nats:
            Nn=D.N.get((q,n))
            if not Nn: continue
            for r in D.rows:
                if r in known[n] or r in SPREAD: continue
                d=D.R.get((q,n,r))
                if d is None: continue
                a=d['aHome']
                if a!=a: continue   # 代表の生産者が居ない行は積まない
                qs_=phase_judged(r,q,J)
                if qs_ is None: continue
                par=tree.get(r,[])
                locked=any(p not in known[n] for p in par)
                y=0.0
                for (src,h,pool) in D.rec.get((q,n),[]):
                    if r in known.get(src,()) and pool>1e-9: y+=h/pool
                X_=0.0
                xa=sum(sh for sname,sh in D.P.get((q,n),{}).items() if sname in known and r in known[sname])
                z=(xa/Ha if xa>0 else 0.0)+(y/Hr if y>0 else 0.0)
                X=TERMMAX*z/(1+z) if z>0 else 0.0
                if locked: inc=X; tt=nd=0.0
                else:
                    tt=fieldtech(Nn['lit'],Nn['popCore'],a,wL,wS,D0,A0,e)
                    if r in WEAPON or r=='機関車': nd=d['need']
                    else: nd=sat(savings(d,d['dem']),Hs)
                    inc=tt+nd+X
                    c=comp[r]; c[0]+=tt*wt/K; c[1]+=nd*wt/K; c[2]+=X*wt/K
                if not(inc>0): continue
                prog[(n,r)]=prog.get((n,r),0.0)+inc*wt/K
                if prog[(n,r)]>=100.0: newly.append((n,r,qs_))
        for (n,r,qq) in newly:
            known[n].add(r); opened[(n,r)]=qq
    for (n,r),q in D.spread_open.items(): opened[(n,r)]=q
    return opened,known,comp
def anchor_gap(D,op):
    def first(r): 
        v=[q for (n,rr),q in op.items() if rr==r]
        return min(v) if v else None
    a,b=first('内燃機関'),first('戦車')
    return (b-a) if (a is not None and b is not None) else None
