# (1254) 合流の注：版 J は州ごとに最初の〈4 四半期で 3 倍〉の跳ねだけを判定して抜ける（反証役 v の低）。k99B・pfS11・M・S11 は全ての跳ねで数え直しても件数が同じ。版 J は持ち主が移らない既知の件（レイキャヴィク）を拾うように置いた物差し。
# (1252) 〈膨らんでから抜ける州〉の頻度。読むだけ。
# 使い方: swell.py <名> <生のフォルダ> <種の始め> <種の終わり> [<基準の生のフォルダ>|-]
# 版 S（自分の前）: 持ち主の移り q_c の後のピーク ≥ 3×(q_c より前の最大) かつ その後の最小 < 後のピーク/10
# 版 B（基線の種）: 持ち主の移りの後のピーク ≥ 3×(基準の同じ種・同じ州の全期間のピーク) かつ その後の最小 < 後のピーク/10
# 版 J（持ち主を問わない跳ね）: 4 四半期以内に 3 倍以上へ跳ねた後のピーク、その後の最小 < ピーク/10
# 列: 膨らむ区間の内訳（州の間の流入・越境の流入・残差＝出生−死亡＋記帳されない移し）・A2 例外か
import sys,csv,os
sys.path.insert(0,os.path.join(os.path.dirname(os.path.abspath(__file__)),'..','hol'))
import hol_common
name,d,s0,s1=sys.argv[1],sys.argv[2],int(sys.argv[3]),int(sys.argv[4]); ref=sys.argv[5] if len(sys.argv)>5 and sys.argv[5]!='-' else None
def load(p):
    f=open(p,encoding='utf-8',newline=''); r=csv.reader(f,delimiter='\t'); h=next(r); ix={c:i for i,c in enumerate(h)}
    T={}
    for x in r:
        T.setdefault(x[ix['region']],[]).append((int(x[ix['quarter']]),int(x[ix['year']]),x[ix['nation']],float(x[ix['populationK']]),
            float(x[ix['migrationInKPerQuarter']]),float(x[ix['migrationOutKPerQuarter']]),float(x[ix['crossBorderInKPerQuarter']]),float(x[ix['crossBorderOutKPerQuarter']]),x[ix['tier']]))
    return T
def parts(t,a,b):  # a<q<=b の内訳
    R=[x for x in t if a<x[0]<=b]; P0=[x[3] for x in t if x[0]==a][0]
    mi=sum(x[4] for x in R); xi=sum(x[6] for x in R); out=sum(x[5]+x[7] for x in R); dP=R[-1][3]-P0 if R else 0
    return dP,mi,xi,dP-(mi+xi-out)
tot={'S':0,'B':0,'J':0}
print(f"#{name}\t種\t版\t州\t格\tA2例外\t持ち主の移り(年:前→後)\t前の最大K\t基準のピークK\t後のピーク(年)K\t後の最小(年)K\t比\t膨らむ区間ΔK\t州間流入K\t越境流入K\t残差K\t最大の1四半期の残差K(年)")
for s in range(s0,s1+1):
    T=load(f"{d}/region_s{s}.tsv"); RT=load(f"{ref}/region_s{s}.tsv") if ref else None
    cnt={'S':0,'B':0,'J':0}
    for reg,t in T.items():
        t.sort(); ex='例外' if hol_common.is_exempt('rule',reg) else ''
        rp=max(x[3] for x in RT[reg]) if RT and reg in RT else float('nan')
        chg=[i for i in range(1,len(t)) if t[i][2]!=t[i-1][2]]
        hit={}
        for i in chg:
            pre=max(x[3] for x in t[:i]); post=t[i:]; ip=max(range(len(post)),key=lambda k:post[k][3]); pk=post[ip]
            tail=post[ip+1:]
            if not tail: continue
            mn=min(tail,key=lambda x:x[3])
            if not mn[3]<pk[3]/10: continue
            for v,base in (('S',pre),('B',rp)):
                if v in hit: continue
                if base==base and pk[3]>=3*base: hit[v]=(t[i],pre,pk,mn)
        # J
        for i in range(len(t)):
            j0=max(0,i-4)
            if t[i][3]>=3*t[j0][3] and t[j0][3]>0.5:
                ip=max(range(i,len(t)),key=lambda k:t[k][3]); tail=t[ip+1:]
                if tail and min(x[3] for x in tail)<t[ip][3]/10: hit['J']=(t[j0],t[j0][3],t[ip],min(tail,key=lambda x:x[3]))
                break
        for v,(c0,pre,pk,mn) in hit.items():
            cnt[v]+=1
            a=c0[0]; dP,mi,xi,res=parts(t,a,pk[0])
            seg=[(t[k][3]-t[k-1][3]-(t[k][4]+t[k][6]-t[k][5]-t[k][7]),t[k][1]) for k in range(1,len(t)) if a<t[k][0]<=pk[0]]
            mr=max(seg) if seg else (0,0)
            prevown=[x[2] for x in t if x[0]<c0[0]]
            print(f"{name}\t{s}\t{v}\t{reg}\t{c0[8]}\t{ex}\t{c0[1]}:{prevown[-1] if prevown else ''}→{c0[2]}\t{pre:.1f}\t{rp:.1f}\t{pk[3]:.1f}({pk[1]})\t{mn[3]:.1f}({mn[1]})\t{mn[3]/pk[3]:.3f}\t{dP:.1f}\t{mi:.1f}\t{xi:.1f}\t{res:.1f}\t{mr[0]:.1f}({mr[1]})")
    for v in cnt: tot[v]+=cnt[v]
    print(f"#計\t{name}\t種{s}\tS={cnt['S']}\tB={cnt['B']}\tJ={cnt['J']}")
print(f"#合計\t{name}\t種{s0}-{s1}\tS={tot['S']}\tB={tot['B'] if ref else 'nan'}\tJ={tot['J']}")
