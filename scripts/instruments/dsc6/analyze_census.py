# dsc6 段0（dsc5 の続き・h を足した）：census（旗0・種 2・3）から、事前登録 [10] の規則で D0・A0・K を決め、再生の指標を出す。
# 使い方：PYTHONUTF8=1 python analyze_census.py <dscx_s2.tsv> <dscx_s3.tsv> [--k <K>] [--d0 <D0>] [--a0 <A0>]
import sys, os, statistics as st
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from replay3 import *
set_catalog_order([l.strip() for l in open(os.path.join(os.path.dirname(os.path.abspath(__file__)),'catalog_order.txt'),encoding='utf-8') if l.strip()])
def medians(Ds):
    dens=[]; scale=[]
    for D in Ds:
        for (q,n,r),d in D.R.items():
            if n not in MAJOR or r in SPREAD: continue
            if D.year.get(q,9999)>1850: continue
            a=d['aHome']
            if a!=a or not a>0: continue
            Nn=D.N.get((q,n))
            if not Nn or not Nn['popCore']>1e-9: continue
            dens.append(a/Nn['popCore']); scale.append(a)
    ss=[]
    for D in Ds:
        for (q,n,r),d in D.R.items():
            if n not in MAJOR or r in SPREAD or r in WEAPON or r=='機関車': continue
            if D.year.get(q,9999)>1850 or d['known']!=0: continue
            Nn=D.N.get((q,n))
            if not Nn: continue
            Y=Nn.get('Y',float('nan'))
            sv=savings(d,d['dem'])
            if Y==Y and Y>0 and sv>0: ss.append(sv/Y)
    return (st.median(dens),len(dens)),(st.median(scale),len(scale)),(st.median(ss) if ss else float('nan'),len(ss))
def kscan(Ds,J,D0,A0,lo=20,hi=600,step=20,h=None):
    best=None; rows=[]
    for K in range(lo,hi+1,step):
        gaps=[]
        for D in Ds:
            op,_,_=run(D,K,J=J,D0=D0,A0=A0,h=h)
            g=anchor_gap(D,op)
            if g is None: gaps=None; break
            gaps.append(g)
        rows.append((K,gaps))
    return rows
def main():
    args=sys.argv[1:]
    paths=[a for a in args if not a.startswith('--')]
    opt={}
    for i,a in enumerate(args):
        if a.startswith('--') and i+1<len(args): opt[a]=args[i+1]
    Ds=[Data(p) for p in paths[:2]]
    (d0,nd),(a0,na),(h0,nh)=medians(Ds)
    print('D0（密度の中央値・正の値・主要 9 国・~1850 年）=',d0,' 標本',nd,'  A0（規模の中央値）=',a0,' 標本',na)
    print('h（s＝S÷Y の中央値・正の値・未解禁の物を生む行・主要 9 国・~1850 年）=',h0,' 標本',nh)
    D0=float(opt.get('--d0',d0)); A0=float(opt.get('--a0',a0)); H=float(opt['--h']) if '--h' in opt else (h0 if '--noh' not in opt else None)
    J=int(opt.get('--j',4))
    if '--k' in opt:
        K=int(opt['--k'])
    else:
        coarse=kscan(Ds,J,D0,A0,20,600,20,H)
        print('K の粗い走査（K, 2 種の 内燃機関→戦車 の四半期数）：')
        for K,g in coarse: print(' ',K,g)
        okc=[(abs(sum(g)/len(g)-124),K) for K,g in coarse if g]
        if not okc: print('錨が再生で成立しない'); return
        K0=min(okc)[1]
        fine=kscan(Ds,J,D0,A0,max(2,K0-20),K0+20,1,H)
        cand=[(abs(sum(g)/len(g)-124),K,g) for K,g in fine if g]
        K=min(cand)[1]
        print('K（規則1・同点は小さい方）=',K,[c for c in cand if c[1]==K][0][2])
    for D,p in zip(Ds,paths):
        op,known,comp=run(D,K,J=J,D0=D0,A0=A0,h=H)
        print('==',os.path.basename(p),' 開いた国×行',len(op))
        # T1：主要国の 1 年の窓（4 四半期）の最大
        cl={}
        for n in MAJOR:
            qq=sorted(q for (m,r),q in op.items() if m==n and (m,r) not in {(m,r2) for r2 in D.known0[m]})
            best=0
            for i,q0 in enumerate(qq): best=max(best,sum(1 for x in qq[i:] if x<=q0+3))
            cl[n]=best
        print(' T1 国ごとの 1 年の窓の最大',cl,' 中央',st.median(cl.values()))
        def cnt(row,y): return sum(1 for n in MAJOR if n!='イギリス' and (n,row) in op and D.year.get(op[(n,row)],1783+op[(n,row)]//4)<=y)
        print(' T3 蒸気機関 1850=',cnt('蒸気機関',1850),'1900=',cnt('蒸気機関',1900),' 機関車 1850=',cnt('機関車',1850))
        sim=[]
        for r in D.rows:
            qq=sorted(op[(n,r)] for n in MAJOR if (n,r) in op)
            if len(qq)>=2: sim.append(sum(1 for x in qq if x<=qq[0]+3))
        print(' T4 中央',st.median(sim) if sim else None,' 最大',max(sim) if sim else None)
        firsts={}
        for (n,r),q in op.items():
            if r in D.known0.get('イギリス',set()) or r in SPREAD: continue
            if r not in firsts or q<firsts[r][0]: firsts[r]=(q,{n})
            elif q==firsts[r][0]: firsts[r][1].add(n)
        nm=0;tot=0;cn=[]
        for r,(q,ns) in firsts.items():
            qe=q if q%2==0 else q-1
            eng={m:D.N.get((qe,m),{}).get('engAll',0) for m in MAJOR}
            best=max(eng,key=lambda m:eng[m]); tot+=1; nm+=(best not in ns)
            if '清' in ns: cn.append(r)
        print(' T5',nm,'/',tot,' 清が最初の行',cn)
        def yr_(r,n):
            q=op.get((n,r)); return None if q is None else D.year.get(q,1783+q//4)
        print(' 解禁の年（英/米/仏/清）：',{r:(yr_(r,'イギリス'),yr_(r,'アメリカ'),yr_(r,'フランス'),yr_(r,'清')) for r in ['蒸気機関','機関車','転炉鋼','石炭火力','電動機','内燃機関','戦車']})
        tot_=[sum(v[i] for v in comp.values()) for i in range(3)]; s=sum(tot_)
        print(' 積みの割合（技術・必要性・伝わり）',[round(x/s,3) for x in tot_] if s>0 else None)
if __name__=='__main__': main()
