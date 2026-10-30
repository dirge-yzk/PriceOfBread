# (670) 線 edu2 段0 の読み手（読むだけ・判定ではない）。基線 m655_ins の生（教育法は 10 種・全期間で段 2 のまま＝義務教育の制度は無い世界）から、
# 義務教育の目標を置く述語 3 本の真偽の列を 9 国の四半期末で比べる：
#   stock       ＝現行の述語（国庫 ＞ 3×財政バッファ＝5 か月×(利払い＋行政＋軍の請求の月額)）。軍の請求は claimArmy＋claimNavy で代用（実装は LastMilitaryClaimPerMonth）
#   stock+cost  ＝上に義務教育の全額購入の月額（代価）を請求へ足した形（15×(請求＋代価)）
#   flow        ＝代価 ≦ 裁量余剰の流れ（Fiscal.cs の surplus の EMA〔平滑 0.15〕＝World.FiscalCapacityPerMonth の再現）
# 代価＝(ed_s の stateWantMoneyPerMonth÷fundingShare＝民間が満たさなかった教育の金額)×目標の教育費の率（0.4・0.6 の 2 本）。供給の上限は掛けていない＝過大側。
# 自動機械＝法が述語に毎四半期追従する（真で制定・偽で撤回・政治の門なし）と置いたときの制定・撤回の件数。政治の門と全額購入が国庫へ戻す効果は入らない＝上限の見積り。
# 使い方：PYTHONUTF8=1 python stage0_edu2.py <ins フォルダ>
import csv,sys
from collections import defaultdict
sys.stdout.reconfigure(encoding='utf-8')
D=(sys.argv[1] if len(sys.argv)>1 else 'D:/PoB_work/keep_655/m655_ins').rstrip('/')+'/'   # 入力＝計器つき 10 種の ins フォルダ（fiscal_s・nation_s・ed_s）
def fl(x):
    try: return float(x)
    except: return float('nan')
eras=[(1783,1810),(1810,1850),(1850,1900),(1900,1951)]
RATES=(0.4,0.6)
names=['stock','stock+cost','flow']
def new(): return {'n':0,'t':0,'up':0,'dn':0}
S={(nm,r):{e:new() for e in eras} for nm in names for r in RATES}
auto={(nm,r):{'up':0,'dn':0,'ns':0,'stand':0,'q':0} for nm in names for r in RATES}
truth_rank={(nm,r):[[],[]] for nm in names for r in RATES}
breach={r:{e:[] for e in eras} for r in RATES}   # 述語 stock が真の四半期ごとの〈代価の流出だけで閾値を割るまでの月数〉＝(国庫−15×請求)÷代価  # rank of GDP pc among true / false
for s in range(1,11):
    cap={}; Fq={}
    with open(D+f'fiscal_s{s}.tsv',encoding='utf-8') as f:
        rd=csv.reader(f,delimiter='\t'); h=next(rd)
        cols=['monthIndex','nation','year','claimInterestPerMonth','claimArmyPerMonth','claimNavyPerMonth','claimAdminPerMonth','treasuryAtClaimStock','adminLastRevenueAtBudgetPerMonth']
        ix={c:h.index(c) for c in cols}
        for row in rd:
            m=int(row[ix['monthIndex']]); n=row[ix['nation']]
            I,A,Nv,Ad,T,Rv=[fl(row[ix[c]]) for c in cols[3:]]
            sur=max(0,Rv-I-Ad-A-Nv)
            c0=cap.get(n); cap[n]=sur if (c0 is None or c0<1e-9) else c0+0.15*(sur-c0)
            if m%3==2: Fq[(m,n)]=(int(row[ix['year']]),I+A+Nv+Ad,T,cap[n])
    nat={}
    with open(D+f'nation_s{s}.tsv',encoding='utf-8') as f:
        rd=csv.reader(f,delimiter='\t'); h=next(rd); iq=h.index('quarter'); inn=h.index('nation'); ip=h.index('populationK'); ig=h.index('nominalGdpPerYear')
        for row in rd: nat[(int(row[iq]),row[inn])]=(fl(row[ip]),fl(row[ig]))
    ed={}
    with open(D+f'ed_s{s}.tsv',encoding='utf-8') as f:
        rd=csv.reader(f,delimiter='\t'); h=next(rd); ix={c:i for i,c in enumerate(h)}
        for row in rd: ed[(int(row[0]),row[2])]=(fl(row[ix['fundingShare']]),fl(row[ix['stateWantMoneyPerMonth']]))
    nations=sorted(set(n for (_,n) in ed)); prev={}; law={}
    for q in range(668):
        rows=[]
        for n in nations:
            k=(3*q+2,n)
            if k not in Fq or (q,n) not in ed or (q,n) not in nat: continue
            fs,wm=ed[(q,n)]; y,base,T,cp=Fq[k]; pop,gdp=nat[(q,n)]
            if not(fs>1e-6 and base>0 and pop>0): continue
            rows.append((n,y,gdp/pop,wm/fs,base,T,cp))
        if len(rows)<5: continue
        pcs=sorted(r[2] for r in rows)
        for (n,y,pc,unmet,base,T,cp) in rows:
            rk=sum(1 for z in pcs if z<pc)/(len(pcs)-1)
            for r in RATES:
                cost=unmet*r
                if T>15*base and cost>0:
                    for e in eras:
                        if e[0]<=y<e[1]: breach[r][e].append((T-15*base)/cost)
                P={'stock':T>15*base,'stock+cost':T>15*(base+cost),'flow':cost<=cp}
                for nm in names:
                    p=P[nm]; key=(nm,r)
                    truth_rank[key][0 if p else 1].append(rk)
                    p0=prev.get(key+(n,))
                    for e in eras:
                        if e[0]<=y<e[1]:
                            o=S[key][e]; o['n']+=1; o['t']+=p
                            if p0 is not None and p0!=p: o['up' if p else 'dn']+=1
                    prev[key+(n,)]=p
                    # automaton: law follows predicate, political gate ignored
                    a=auto[key]; L=law.get(key+(n,),0)  # 0=Public 1=Compulsory
                    a['q']+=1
                    if p and L==0: L=1; a['up']+=1
                    elif (not p) and L==1: L=0; a['dn']+=1
                    a['stand']+=L; law[key+(n,)]=L
for key in S:
    nm,r=key
    print(f'== {nm} 目標率={r}')
    for e in eras:
        o=S[key][e]; print(f"   {e} 真={o['t']/o['n']:.3f} 上げ反転/国年={o['up']/o['n']*4:.3f} 下げ反転/国年={o['dn']/o['n']*4:.3f}")
    a=auto[key]; print(f"   自動機械(政治の門なし): 制定={a['up']} 撤回={a['dn']} 比={a['dn']/max(1,a['up']):.3f} 義務の割合={a['stand']/a['q']:.3f}")
    T_,F_=truth_rank[key]; med=lambda L:sorted(L)[len(L)//2] if L else float('nan')
    print(f"   GDP順位 真のとき平均={sum(T_)/max(1,len(T_)):.3f} 偽のとき平均={sum(F_)/max(1,len(F_)):.3f}")

print('== 流出だけで述語 stock が偽になるまでの月数（真の四半期の中央値・p25・p75・件数）=(国庫−15×請求)÷代価。歳入の伸び・借入・他の請求の変化は入らない')
for r in RATES:
    for e in eras:
        L=sorted(breach[r][e])
        if L: print(f"   目標率={r} {e} 中央値={L[len(L)//2]:.0f} p25={L[len(L)//4]:.0f} p75={L[len(L)*3//4]:.0f} 件数={len(L)}")
