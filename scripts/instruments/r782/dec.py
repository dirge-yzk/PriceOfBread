# (784) 読み手 r782 の台本（使い捨ての写し・元は Temp）・入力のパスは当時の固定の値（keep_778）
# 1790〜1799 の英の歳入の内訳（種ごとの平均→種の中央値）・腕ごと
import sys, csv, os, statistics as st
nat='イギリス'
def F(x):
    try: return float(x)
    except: return 0.0
K=['revenueBasePerMonth','ledgerTaxPerMonth','ledgerTariffPerMonth','ledgerStateMonopolyPerMonth','claimInterestPerMonth','claimTotalPerMonth','claimArmyPerMonth','claimNavyPerMonth','claimSoldierPayPerMonth','claimAdminPerMonth','govDebtStock','govBondRatePerYear','treasuryAtClaimStock','convertibilityShare']
nm=['rev','tax','tariff','mono','int','claim','army','navy','spay','admin','debt','bond','tre','conv']
y0,y1=int(sys.argv[1]),int(sys.argv[2])
print('arm '+' '.join(f'{n:>8}' for n in nm)+'      i/R')
for spec in sys.argv[3:]:
    name,d,s0=spec.split(':'); s0=int(s0); per=[]
    for s in range(s0,s0+10):
        acc={k:[] for k in K}
        with open(os.path.join(d,f'fiscal_s{s}.tsv'),encoding='utf-8') as f:
            r=csv.reader(f,delimiter='\t'); h=next(r); ix={k:i for i,k in enumerate(h)}
            for x in r:
                if x[1]!=nat: continue
                y=int(x[2])
                if F(x[ix['defaultCount']])>0: break
                if y0<=y<=y1:
                    for k in K: acc[k].append(F(x[ix[k]]))
        if acc['revenueBasePerMonth']: per.append({k:st.mean(v) for k,v in acc.items()})
    med={k:st.median(p[k] for p in per) for k in K}
    iR=st.median(p['claimInterestPerMonth']/p['revenueBasePerMonth'] for p in per)
    print(f'{name:>4} '+' '.join(f'{med[k]:8.4g}' for k in K)+f' {iR:8.3f}')
