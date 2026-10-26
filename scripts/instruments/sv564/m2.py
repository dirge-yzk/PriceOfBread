import csv,statistics as st
INV=[3,4,7,8,9,10,13]; MASS=[1,2,11,12]
T=['q0','q40','q120','q200','q400','q520']
out={}
for s in range(1,11):
    for x in csv.DictReader(open(f'D:/PoB_work/keep_560/m560_ins/money_s{s}.tsv',encoding='utf-8'),delimiter='\t'):
        if x['time'] not in T or x['nation'] not in ('イギリス','フランス','清'): continue
        P=float(x['priceLevelAfterIndex']); Yn=P*float(x['gdpRealPerMonth'])*12
        sav=[float(x[f'savingsStockClass{i}']) for i in range(14)]
        ear=[float(x[f'earnedPerMonthClass{i}']) for i in range(14)]
        pay=[float(x[f'paidPerMonthClass{i}']) for i in range(14)]
        si=sum(sav[i] for i in INV); ei=sum(ear[i] for i in INV)*12; pi=sum(pay[i] for i in INV)*12
        sm=sum(sav[i] for i in MASS); em=sum(ear[i] for i in MASS)*12; pm=sum(pay[i] for i in MASS)*12
        k=(x['nation'],x['time'],x['year'])
        out.setdefault(k,[]).append((si/Yn, si/ei if ei>0 else float('nan'), pi/ei if ei>0 else float('nan'), sum(sav)/Yn, sm/em if em>0 else float('nan'), pm/em if em>0 else float('nan'), sum(sav[i] for i in INV)/max(1,sum(sav))))
for k in sorted(out, key=lambda k:(k[0],int(k[1][1:]))):
    v=out[k]; med=lambda j: st.median([a[j] for a in v])
    print(k, 'n=%d inv/Y=%.2f inv/ownInc=%.1fy inv paid/earn=%.2f  M/Y=%.1f  mass sav/inc=%.2fy mass paid/earn=%.2f invShareOfM=%.2f'%(len(v),med(0),med(1),med(2),med(3),med(4),med(5),med(6)))
