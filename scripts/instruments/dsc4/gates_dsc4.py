# 発見の式の作り直し（dsc4）：腕の計器の生（dscx_s<種>.tsv）から、事前登録 measurements/dsc4_20261026/prereg_dsc4.txt の狙いの数字 G1〜G4 と記録を読む。
# 使い方：PYTHONUTF8=1 python gates_dsc4.py <ins ディレクトリ> <種の起点> <種の数>   （例：… out/ins 1 10）
# ⚠限界（自己申告）：標本は偶数の四半期（解禁した四半期の 1 つ手前〜その標本の間の幅 2 四半期）・国は計器の NATIONS に挙げた 9 国だけ（主要国）・
#   G4 の〈技師の総数が最大の国〉は 9 国の中での最大（全ての国の総数は計器に無い）・飛び越えの判定は標本の時点の前提の断面。
import sys, os, collections, statistics as st
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from load2 import *
MAJOR=['イギリス','フランス','プロイセン','アメリカ','ロシア','オーストリア','オスマン','清','日本']
FOLLOW=[m for m in MAJOR if m!='イギリス']
def analyse(path):
    N,R,P,Q=load2(path)
    qs=sorted({q for (q,_,_) in R}); year={}
    for (q,n,r),d in R.items(): year[q]=d['year']
    rows=sorted({r for (_,_,r) in R}); dec={r:d['decade'] for (q,n,r),d in R.items() for _ in [0]}
    first={}   # (n,r)->最初に known=1 を見た標本の四半期（q0 で既に known なら 開幕の既知）
    prev_open={}  # (n,r)->直前の標本の前提の断面
    jump=[]
    q0=qs[0]
    for q in qs:
        for n in MAJOR:
            for r in rows:
                d=R.get((q,n,r))
                if d is None: continue
                if d['known']==1 and (n,r) not in first:
                    first[(n,r)]=q
                    if q!=q0 and prev_open.get((n,r),1)==0: jump.append((n,r,year[q]))
                prev_open[(n,r)]=d['open']
    start={(n,r) for (n,r),q in first.items() if q==q0}
    opened={k:q for k,q in first.items() if k not in start}
    return dict(N=N,R=R,qs=qs,year=year,rows=rows,dec=dec,opened=opened,start=start,jump=jump)
def gates(a):
    y=a['year']; op=a['opened']; out={}
    out['G1']=sum(1 for (n,r),q in op.items() if a['dec'][r]>=1800 and y[q]<=1786)
    out['G1b_opened_by_1790']=sum(1 for (n,r),q in op.items() if y[q]<=1790)
    def cnt(row,yr): return sum(1 for n in FOLLOW if (n,row) in op and y[op[(n,row)]]<=yr)
    out['steam_by1850']=cnt('蒸気機関',1850); out['steam_by1900']=cnt('蒸気機関',1900); out['loco_by1850']=cnt('機関車',1850)
    sim=[]
    for r in a['rows']:
        qq=sorted(op[(n,r)] for n in MAJOR if (n,r) in op)
        if len(qq)>=2: sim.append(sum(1 for x in qq if x<=qq[0]+4))
    out['G3_sim']=sim
    # 追記2 G3b：国ごとの固まり＝主要国ごとに、4 四半期（1 年）の窓の中に開いた行の数の最大
    clump={}
    for n in MAJOR:
        qq=sorted(q for (m,r),q in op.items() if m==n)
        best=0
        for i,q0 in enumerate(qq):
            best=max(best,sum(1 for x in qq[i:] if x<=q0+4))
        clump[n]=best
    out['G3b']=clump
    rows_spread=['特許','株式会社','社会主義','共産主義','帝国主義','ファシズム']
    out['spread_open']={r:sorted((y[op[(n,r)]],n[:3]) for n in MAJOR if (n,r) in op) for r in rows_spread}
    notmax=0; tot=0; cn=[]
    firsts={}
    start_rows={r for (n,r) in a['start']}   # 追記2：開幕に主要国の誰かが知っている行（蒸気機関・特許）は G4 から外す（最初の発見者が〈追う国〉になるため）
    for (n,r),q in op.items():
        if r in start_rows: continue
        if r not in firsts or q<firsts[r][0]: firsts[r]=(q,{n})
        elif q==firsts[r][0]: firsts[r][1].add(n)
    for r,(q,ns) in firsts.items():
        eng={m:a['N'].get((q,m),{}).get('engAll',0) for m in MAJOR}
        best=max(eng,key=lambda m:eng[m])
        tot+=1; notmax+= (best not in ns)
        if '清' in ns: cn.append(r)
    out['G4']=(notmax,tot); out['cn_first']=cn
    out['firsts']={r:(y[q],sorted(ns)) for r,(q,ns) in firsts.items()}
    out['jump']=a['jump']
    # 追記2 記録：積みの割合（前提が開いていて未解禁の標本の技術・必要性・伝わりの点の和）と、必要性が効いた行（その行の標本のどこかで必要性の点が 5 以上）の数
    sT=sN=sX=0.0; needy=set()
    for (q,n,r),d in a['R'].items():
        if n not in MAJOR or d['known']==1 or d['open']!=1: continue
        sT+=d['tech'] if d['tech']==d['tech'] else 0; sN+=d['need'] if d['need']==d['need'] else 0; sX+=d['trans'] if d['trans']==d['trans'] else 0
        if d['need']==d['need'] and d['need']>=5: needy.add(r)
    tot=sT+sN+sX
    out['share']=(sT/tot,sN/tot,sX/tot) if tot>0 else None
    out['needy_rows']=len(needy)
    out['key']={r:sorted((y[op[(n,r)]],n[:3]) for n in MAJOR if (n,r) in op) for r in ('蒸気機関','機関車','転炉鋼','石炭火力','電動機')}
    return out
def main():
    d=sys.argv[1]; s0=int(sys.argv[2]); ns=int(sys.argv[3])
    res=[]
    for s in range(s0,s0+ns):
        p=os.path.join(d,'dscx_s%d.tsv'%s)
        if not os.path.exists(p): print('seed',s,'なし'); continue
        a=analyse(p); g=gates(a); res.append((s,g))
        print('== seed',s)
        print(' G1(1783-86 に開いた d>=2 の国×行)=',g['G1'],' 1790 年までに開いた国×行=',g['G1b_opened_by_1790'])
        print(' G2 蒸気機関(英を除く主要国)1850=',g['steam_by1850'],'1900=',g['steam_by1900'],' 機関車 1850=',g['loco_by1850'])
        print(' G3 一斉の数 中央=',st.median(g['G3_sim']) if g['G3_sim'] else None,'最大=',max(g['G3_sim']) if g['G3_sim'] else None,'行の数=',len(g['G3_sim']))
        print(' G3b 国ごとの 1 年の窓の最大=',g['G3b'])
        print(' G4 最初の発見者が技師最大の国でない行(開幕の既知の行を除く)=',g['G4'])
        print(' 記録 清が最初の行=',g['cn_first'])
        print(' 記録 積みの割合(技術,必要性,伝わり)=',g['share'],' 必要性の点が 5 以上になった行の数=',g['needy_rows'])
        print(' 記録 飛び越え(国×行×年)=',len(g['jump']),g['jump'][:12])
        for r,v in g['key'].items(): print('  記録',r,v)
        for r,v in g['spread_open'].items(): print('  記録(思想・制度)',r,v)
    if res:
        n=len(res)
        print('\n## 集計（%d 種）'%n)
        print(' G1 全ての種で 0:',all(g['G1']==0 for s,g in res))
        print(' G2 蒸気機関1850>=3 の種の割合',sum(g['steam_by1850']>=3 for s,g in res)/n,'(>=0.6)  1900>=6 の割合',sum(g['steam_by1900']>=6 for s,g in res)/n,'(>=0.8)  機関車1850>=4 の割合',sum(g['loco_by1850']>=4 for s,g in res)/n,'(>=0.6)')
        allsim=[x for s,g in res for x in g['G3_sim']]
        print(' G3 中央=',st.median(allsim) if allsim else None,'(<=4)  最大=',max(allsim) if allsim else None,'(<=6)')
        cl=[v for s,g in res for v in g['G3b'].values()]
        print(' G3b 国×種ごとの 1 年の窓の最大 中央=',st.median(cl),'(<=3)  最大=',max(cl),'(<=8)')
        nm=sum(g['G4'][0] for s,g in res); tt=sum(g['G4'][1] for s,g in res)
        print(' G4 割合=',nm/tt if tt else None,'(>=0.5)')
if __name__=='__main__': main()
