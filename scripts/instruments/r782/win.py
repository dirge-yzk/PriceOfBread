# (784) 読み手 r782 の台本（使い捨ての写し・元は Temp）・入力のパスは当時の固定の値（keep_778）
import sys, csv, os
d=sys.argv[1]; seeds=[int(x) for x in sys.argv[2].split(',')]; nat=sys.argv[3] if len(sys.argv)>3 else 'イギリス'
cols=['year','revenueBasePerMonth','directTaxNetPerMonth','indirectRevenuePerMonth','claimInterestPerMonth','claimTotalPerMonth','claimArmyPerMonth','claimNavyPerMonth','claimSoldierPayPerMonth','treasuryAtClaimStock','payRateShare','borrowGateOpenFlag','borrowedPerMonth','interestToRevenueShareNow','govBondRatePerYear','convertibilityShare','onGold','devaluedPerMonth','insolvencyMonths','defaultCount']
short=['yr','rev','dir','ind','int','claim','army','navy','spay','tre','r','gate','borr','i/R','bond','conv','gold','dev','insol','dc']
for s in seeds:
    rows=[]
    with open(os.path.join(d,f'fiscal_s{s}.tsv'),encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r); ix={k:i for i,k in enumerate(h)}
        for x in r:
            if x[ix['nation']]!=nat: continue
            rows.append(x)
    dm=None
    for k,x in enumerate(rows):
        if float(x[ix['defaultCount']] or 0)>0: dm=k; break
    print(f'## s{s} default row {dm} month {rows[dm][0] if dm is not None else None}')
    if dm is None: continue
    print('mon '+' '.join(f'{c:>9}' for c in short))
    for x in rows[max(0,dm-26):dm+2]:
        print(f'{x[0]:>4} '+' '.join(f'{float(x[ix[c]] or 0):9.4g}' for c in cols))
