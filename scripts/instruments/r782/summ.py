# (784) 読み手 r782 の台本（使い捨ての写し・元は Temp）・入力のパスは当時の固定の値（keep_778）
import sys, csv, os, statistics as st
d=sys.argv[1]; s0=int(sys.argv[2]) if len(sys.argv)>2 else 1; Y0,Y1=1784,1815; nat='イギリス'
def rows(p):
    with open(p,encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r)
        for x in r:
            if len(x)<len(h): x+=['']*(len(h)-len(x))
            yield dict(zip(h,x))
def F(x):
    try: return float(x)
    except: return 0.0
print('seed defY lostPeace coreCeded colCeded core0->min(yr) repSum repMax1 repN gdp1790 debt/gdpY1790 i/R_med9099 closedShare9099 monoShare9099 ind/rev dir/rev tre_med9099 revCV9099')
for s in range(s0,s0+10):
    fp=os.path.join(d,f'fiscal_s{s}.tsv')
    if not os.path.exists(fp): continue
    fr=[x for x in rows(fp) if x['nation']==nat]
    defY=None; rep=[]; iR=[]; closed=0; nmon=0; mono=[]; ind=[]; dirr=[]; tre=[]; rev=[]
    for x in fr:
        y=int(x['year'])
        if defY is None and F(x['defaultCount'])>0: defY=y
        if Y0<=y<=Y1 and (defY is None or y<=defY):
            w=-F(x['ledgerWarCostPerMonth'])
            if w>1: rep.append(w)
        if 1790<=y<=1799 and (defY is None or y<defY):
            R=F(x['revenueBasePerMonth']); I=F(x['claimInterestPerMonth'])
            if R>0: iR.append(I/R); mono.append(F(x['ledgerStateMonopolyPerMonth'])/R); ind.append(F(x['indirectRevenuePerMonth'])/R); dirr.append(F(x['directTaxNetPerMonth'])/R)
            nmon+=1; closed+= (F(x['borrowGateOpenFlag'])<0.5); tre.append(F(x['treasuryAtClaimStock'])); rev.append(R)
    lost=sum(1 for x in rows(os.path.join(d,f'cessp_s{s}.tsv')) if x['loser']==nat and Y0<=int(x['year'])<=Y1)
    cc=sum(1 for x in rows(os.path.join(d,f'cess_s{s}.tsv')) if x['loser']==nat and Y0<=int(x['year'])<=Y1 and x['tier']=='Core')
    ccol=sum(1 for x in rows(os.path.join(d,f'cess_s{s}.tsv')) if x['loser']==nat and Y0<=int(x['year'])<=Y1 and x['tier']!='Core')
    nr=[x for x in rows(os.path.join(d,f'nation_s{s}.tsv')) if x['nation']==nat]
    core0=F(nr[0]['coreRegions']); mn=min((F(x['coreRegions']),int(x['year'])) for x in nr if Y0<=int(x['year'])<=Y1)
    g90=[F(x['nominalGdpPerYear']) for x in nr if int(x['year'])==1790]; d90=[F(x['govDebtStock']) for x in nr if int(x['year'])==1790]
    cv=st.pstdev(rev)/st.mean(rev) if rev else 0
    print(s, defY, lost, cc, ccol, f'{core0:g}->{mn[0]:g}({mn[1]})', f'{sum(rep):.0f}', f'{max(rep) if rep else 0:.0f}', len(rep), f'{st.mean(g90):.0f}', f'{st.mean(d90)/st.mean(g90):.2f}', f'{st.median(iR):.3f}', f'{closed/max(1,nmon):.2f}', f'{st.median(mono):.2f}', f'{st.median(ind):.2f}', f'{st.median(dirr):.2f}', f'{st.median(tre):.0f}', f'{cv:.2f}')
