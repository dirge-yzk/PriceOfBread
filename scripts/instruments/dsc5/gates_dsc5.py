# dsc5：腕の計器の生（disc_s<種>.tsv の DiscoveryEvent＝毎四半期・dscx_s<種>.tsv の N と R＝偶数の四半期の標本）から、事前登録 measurements/dsc5_20261027/prereg_dsc5.txt [6] の
# 狙いの数字 T1〜T5・T8 と記録を読む。
# 使い方：PYTHONUTF8=1 python gates_dsc5.py <ins ディレクトリ> <種の起点> <種の数> [--rows]
# ⚠限界（自己申告）：国は計器の NATIONS に挙げた 9 国だけ（主要国）／T5 の〈技師の総数が最大の国〉は 9 国の中での最大で、標本（偶数の四半期）の engAll／T8 は偶数の四半期の標本／
#   開幕の既知の行は〈最初の標本で主要国のどれかが知っていた行〉。制度と思想の行は DiscoveryEvent に載る（今の門）。
import sys, os, csv, collections, random, statistics as st
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from load3 import *
MAJOR=['イギリス','フランス','プロイセン','アメリカ','ロシア','オーストリア','オスマン','清','日本']
FOLLOW=[m for m in MAJOR if m!='イギリス']
SPREAD={'特許','株式会社','社会主義','共産主義','帝国主義','ファシズム'}
ORDER=[l.strip() for l in open(os.path.join(os.path.dirname(os.path.abspath(__file__)),'catalog_order.txt'),encoding='utf-8') if l.strip()]
DEC={}
def events(path):
    ev={}   # (nation,row)->quarter
    year={}
    with open(path,encoding='utf-8') as fh:
        rd=csv.reader(fh,delimiter='\t'); next(rd)
        for r in rd:
            if r[0]!='E': continue
            q=int(r[1]); n=r[3]; row=r[4]
            year[q]=int(r[2])
            if (n,row) not in ev: ev[(n,row)]=q
    return ev,year
def analyse(ins,seed):
    ev,year=events(os.path.join(ins,'disc_s%d.tsv'%seed))
    N,R,P,Q=load3(os.path.join(ins,'dscx_s%d.tsv'%seed))
    for (q,n,r),d in R.items(): DEC[r]=d['decade']; year.setdefault(q,d['year'])
    qs=sorted({q for (q,_,_) in R})
    q0=qs[0]
    start={(n,r) for (q,n,r),d in R.items() if q==q0 and d['known']==1}
    start_rows={r for (n,r) in start if n in MAJOR}
    op={k:q for k,q in ev.items() if k[0] in MAJOR and k not in start}
    return dict(N=N,R=R,qs=qs,year=year,ev=op,start_rows=start_rows)
def _rank(v):
    o=sorted(range(len(v)),key=lambda i:v[i]); r=[0.0]*len(v); i=0
    while i<len(o):
        j=i
        while j+1<len(o) and v[o[j+1]]==v[o[i]]: j+=1
        for k in range(i,j+1): r[o[k]]=(i+j)/2.0
        i=j+1
    return r
def spearman(x,y):
    if len(x)<5: return None
    rx,ry=_rank(x),_rank(y); mx,my=sum(rx)/len(rx),sum(ry)/len(ry)
    sx=sum((a-mx)**2 for a in rx); sy=sum((b-my)**2 for b in ry)
    if sx<=0 or sy<=0: return None
    return sum((a-mx)*(b-my) for a,b in zip(rx,ry))/(sx*sy)**0.5
def yr(a,q):
    # 四半期 q の年（標本が無ければ 1783+q//4）
    return a['year'].get(q,1783+q//4)
def gates(a):
    op=a['ev']; out={}
    out['T2']=sum(1 for (n,r),q in op.items() if DEC.get(r,0)>=1800 and yr(a,q)<=1786)
    def cnt(row,y): return sum(1 for n in FOLLOW if (n,row) in op and yr(a,op[(n,row)])<=y)
    out['steam1850']=cnt('蒸気機関',1850); out['steam1900']=cnt('蒸気機関',1900); out['loco1850']=cnt('機関車',1850)
    sim=[]
    rows=sorted({r for (n,r) in op})
    for r in rows:
        qq=sorted(op[(n,r)] for n in MAJOR if (n,r) in op)
        if len(qq)>=2: sim.append(sum(1 for x in qq if x<=qq[0]+3))
    out['T4']=sim
    clump={}; clump_nospread={}
    for n in MAJOR:
        for tag,store,skip in (('all',clump,False),('nospread',clump_nospread,True)):
            qq=sorted(q for (m,r),q in op.items() if m==n and not (skip and r in SPREAD))
            best=0
            for i,q0 in enumerate(qq):
                best=max(best,sum(1 for x in qq[i:] if x<=q0+3))
            store[n]=best
    out['T1']=clump; out['T1_nospread']=clump_nospread
    # 記録 T1r：固まりが〈ランダムな散りより〉強いか＝同じ国の同じ件数の開く四半期を、最初〜最後の幅に一様ランダムに散らした 4 四半期の窓の最大の平均（乱数は固定）
    rng=random.Random(20261027); nullm={}
    for n in MAJOR:
        qq=[q for (m,r),q in op.items() if m==n]
        if len(qq)<5: continue
        lo,hi=min(qq),max(qq); v=[]
        for _ in range(200):
            xs=sorted(rng.randint(lo,hi) for _ in qq); best=0
            for i,q0 in enumerate(xs): best=max(best,sum(1 for x in xs[i:] if x<=q0+3))
            v.append(best)
        nullm[n]=sum(v)/len(v)
    out['T1_null']=nullm
    # T5
    firsts={}
    for (n,r),q in op.items():
        if r in a['start_rows']: continue
        if r not in firsts or q<firsts[r][0]: firsts[r]=(q,{n})
        elif q==firsts[r][0]: firsts[r][1].add(n)
    notmax=0; tot=0; cn=[]; fl={}
    for r,(q,ns) in firsts.items():
        qe=q if q%2==0 else q-1
        eng={m:a['N'].get((qe,m),{}).get('engAll',0) for m in MAJOR}
        best=max(eng,key=lambda m:eng[m])
        tot+=1; notmax+=(best not in ns)
        if '清' in ns: cn.append(r)
        fl[r]=(yr(a,q),sorted(ns))
    out['T5']=(notmax,tot); out['cn_first']=cn; out['firsts']=fl
    # T8：同じ国・同じ標本で、前提が開いていて未解禁の（制度と思想でない）行の技術の項の最大−最小
    spread=[]
    byqn=collections.defaultdict(list)
    for (q,n,r),d in a['R'].items():
        if n not in MAJOR or r in SPREAD or d['known']==1 or d['open']!=1: continue
        t=d['tech']
        if t==t and d['aHome']==d['aHome']: byqn[(q,n)].append(t)
    for k,v in byqn.items():
        if len(v)>=3: spread.append(max(v)-min(v))
    out['T8']=st.median(spread) if spread else None
    # 記録：二重計上の確かめ＝同じ国・同じ標本で、前提が開いていて未解禁の行の（需要 dem）と（その行の分野の本国の技術者 aHome）の順位相関の中央値（事前登録 [6]・追記 3）
    byqn2=collections.defaultdict(lambda:([],[]))
    for (q,n,r),d in a['R'].items():
        if n not in MAJOR or r in SPREAD or d['known']==1 or d['open']!=1: continue
        x,y=d['dem'],d['aHome']
        if x==x and y==y and x>0 and y>0: byqn2[(q,n)][0].append(x); byqn2[(q,n)][1].append(y)
    rho=[spearman(v[0],v[1]) for v in byqn2.values()]
    rho=[x for x in rho if x is not None]
    out['rho']=st.median(rho) if rho else None
    # 記録
    key={r:sorted((yr(a,op[(n,r)]),n[:3]) for n in MAJOR if (n,r) in op) for r in ('蒸気機関','機関車','転炉鋼','石炭火力','電動機','内燃機関','戦車')}
    out['key']=key
    out['spread_open']={r:sorted((yr(a,op[(n,r)]),n[:3]) for n in MAJOR if (n,r) in op) for r in SPREAD}
    # 飛び越え：前提の行（積み上げの木）を自分で開く前に開いた国×行
    return out
def main():
    ins=sys.argv[1]; s0=int(sys.argv[2]); ns=int(sys.argv[3])
    res=[]
    for s in range(s0,s0+ns):
        if not os.path.exists(os.path.join(ins,'disc_s%d.tsv'%s)): print('seed',s,'なし'); continue
        a=analyse(ins,s); g=gates(a); res.append((s,g))
        print('== seed',s)
        print(' T1 国ごとの 1 年の窓の最大（制度と思想を含む）=',g['T1'])
        print(' T1 同（制度と思想を除く）=',g['T1_nospread'])
        print(' 記録 T1r 一様ランダムに散らした帰無の平均（国ごと）=',{k:round(v,2) for k,v in g['T1_null'].items()})
        print(' T2 1783-86 に d>=2 の行が開いた国×行=',g['T2'])
        print(' T3 蒸気機関(英を除く主要国)1850=',g['steam1850'],'1900=',g['steam1900'],' 機関車 1850=',g['loco1850'])
        print(' T4 一斉 中央=',st.median(g['T4']) if g['T4'] else None,'最大=',max(g['T4']) if g['T4'] else None,'行の数=',len(g['T4']))
        print(' T5 最初の発見者が技師最大の国でない行=',g['T5'])
        print(' T8 同じ国・同じ標本の技術の項の最大−最小の中央値=',g['T8'])
        print(' 記録 需要と A_f の順位相関（国×標本の中央値）=',g['rho'])
        print(' 記録 清が最初の行=',g['cn_first'])
        for r,v in g['key'].items(): print('  記録',r,v)
        for r,v in g['spread_open'].items(): print('  記録(思想・制度)',r,v)
    if res:
        n=len(res)
        print('\n## 集計（%d 種）'%n)
        cl=[v for s,g in res for v in g['T1'].values()]
        print(' T1 国×種ごとの 1 年の窓の最大 中央=',st.median(cl),'(<=3)  最大=',max(cl),'(<=8)')
        cl2=[v for s,g in res for v in g['T1_nospread'].values()]
        print(' T1（制度と思想を除く）中央=',st.median(cl2),' 最大=',max(cl2))
        nl=[v for s,g in res for v in g['T1_null'].values()]
        print(' 記録 T1r 帰無（ランダム）の窓の最大の平均 =',round(st.mean(nl),2),' 観測の平均 =',round(st.mean(cl),2),' 観測−帰無 =',round(st.mean(cl)-st.mean(nl),2))
        print(' T2 全ての種で 0:',all(g['T2']==0 for s,g in res))
        print(' T3 蒸気機関1850>=3 の割合',sum(g['steam1850']>=3 for s,g in res)/n,'(>=0.6)  1900>=6 の割合',sum(g['steam1900']>=6 for s,g in res)/n,'(>=0.8)  機関車1850>=4 の割合',sum(g['loco1850']>=4 for s,g in res)/n,'(>=0.6)')
        allsim=[x for s,g in res for x in g['T4']]
        print(' T4 中央=',st.median(allsim) if allsim else None,'(<=4)  最大=',max(allsim) if allsim else None,'(<=6)')
        nm=sum(g['T5'][0] for s,g in res); tt=sum(g['T5'][1] for s,g in res)
        print(' T5 割合=',nm/tt if tt else None,'(>=0.5)')
        t8=[g['T8'] for s,g in res if g['T8'] is not None]
        print(' T8 中央=',st.median(t8) if t8 else None,'(>=5)')
        rh=[g['rho'] for s,g in res if g['rho'] is not None]
        print(' 記録 需要と A_f の順位相関 中央=',round(st.median(rh),3) if rh else None)
        nc=[len(g['cn_first']) for s,g in res]
        print(' 記録 清が最初の発見者の行の数 種ごと=',nc,' 中央=',st.median(nc))
if __name__=='__main__': main()
