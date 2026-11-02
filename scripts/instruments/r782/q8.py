# (784) 読み手 r782 の台本（使い捨ての写し・元は Temp）・入力のパスは当時の固定の値（keep_778）
# 不履行の入口の前 8 四半期（各種の錨＝不履行の種は不履行の前の最後の〈国庫を越えた賠償〉の月・それ以外は 1785〜1815 の最大の〈越えた賠償〉の月）
import sys, csv, os, statistics as st
d=sys.argv[1]; s0=int(sys.argv[2]); nat='イギリス'
def F(x):
    try: return float(x)
    except: return 0.0
K=['revenueBasePerMonth','directTaxNetPerMonth','indirectRevenuePerMonth','ledgerStateMonopolyPerMonth','ledgerTariffPerMonth','claimInterestPerMonth','claimTotalPerMonth','treasuryAtClaimStock','borrowedPerMonth','devaluedPerMonth','payRateShare','borrowGateOpenFlag','govBondRatePerYear']
grp={'D':{},'N':{}}; ev=[]
for s in range(s0,s0+10):
    fp=os.path.join(d,f'fiscal_s{s}.tsv')
    with open(fp,encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r); ix={k:i for i,k in enumerate(h)}
        fr=[x for x in r if x[ix['nation']]==nat]
    dk=next((k for k,x in enumerate(fr) if F(x[ix['defaultCount']])>0),None)
    overs=[]
    for k,x in enumerate(fr):
        y=int(x[ix['year']]); w=-F(x[ix['ledgerWarCostPerMonth']])
        if w>1 and k>0 and w>max(0,F(fr[k-1][ix['treasuryEndStock']])) and 1785<=y<=1815 and (dk is None or k<=dk):
            mi=max(F(z[ix['insolvencyMonths']]) for z in fr[k:k+13])
            hit= dk is not None and 0<=dk-k<=12
            overs.append((k,w,mi,hit))
            T=F(fr[k-1][ix['treasuryEndStock']]); R=F(fr[k-1][ix['revenueBasePerMonth']]); I=F(fr[k-1][ix['claimInterestPerMonth']])
            # 賠償の月から 6 か月の門が閉じた月の割合
            cl=sum(1 for z in fr[k+1:k+7] if F(z[ix['borrowGateOpenFlag']])<0.5)/6
            ev.append((s,y,w,T,I/R if R>0 else 9,w/R if R>0 else 0,cl,mi,int(hit)))
    if dk is not None:
        cand=[o for o in overs if o[3]]; anchor=cand[-1][0] if cand else dk; g='D'
    else:
        if not overs: continue
        anchor=max(overs,key=lambda o:o[1])[0]; g='N'
    for q in range(-8,2):
        seg=fr[anchor+3*q:anchor+3*q+3]
        if len(seg)<3 or anchor+3*q<0: continue
        row={k:st.mean(F(z[ix[k]]) for z in seg) for k in K}
        grp[g].setdefault(q,[]).append(row)
print(f'## {d}  錨の前後の四半期の平均（種の中央値）・D=不履行の種 N=不履行でない種')
hdr=['q','n','rev','dir','ind','mono','tariff','int','i/R','claim','tre','borr','dev','r','gate']
for g in 'DN':
    print(f'-- {g}'); print(' '.join(f'{c:>7}' for c in hdr))
    for q in sorted(grp[g]):
        L=grp[g][q]; m=lambda k: st.median(x[k] for x in L)
        iR=st.median(x['claimInterestPerMonth']/x['revenueBasePerMonth'] for x in L if x['revenueBasePerMonth']>0)
        vals=[q,len(L),m('revenueBasePerMonth'),m('directTaxNetPerMonth'),m('indirectRevenuePerMonth'),m('ledgerStateMonopolyPerMonth'),m('ledgerTariffPerMonth'),m('claimInterestPerMonth'),iR,m('claimTotalPerMonth'),m('treasuryAtClaimStock'),m('borrowedPerMonth'),m('devaluedPerMonth'),m('payRateShare'),m('borrowGateOpenFlag')]
        print(' '.join(f'{v:7.3g}' if isinstance(v,float) else f'{v:>7}' for v in vals))
print('-- 〈国庫を越えた賠償〉の事象（種・年・賠償・直前の国庫・直前の i/R・賠償÷月の歳入・後 6 か月の門の閉じた割合・12 か月の未払いの最大・12 か月以内に不履行）')
for e in ev: print('ev s%d %d rep=%.0f T=%.0f iR=%.3f rep/R=%.1f closed6=%.2f insolMax=%.2f def=%d'%e)
