import csv,statistics as st,sys
INV=[3,4,7,8,9,10,13]; MASS=[1,2,11,12]
arms=sys.argv[1:]
def rd(p): return list(csv.DictReader(open(p,encoding='utf-8'),delimiter='\t'))
for nat in ('イギリス','フランス','清'):
  print('=====',nat)
  for a in arms:
    d=f'D:/PoB_work/sv_564/{a}'
    F=[x for x in rd(f'{d}/fiscal_s1.tsv') if x['nation']==nat]
    def win(lo,hi):
        R=[x for x in F if lo<=int(x['monthIndex'])<hi]
        f=lambda k:[float(x[k]) for x in R if x[k] not in('','NaN')]
        return (st.mean(f('revenueBasePerMonth')), max(f('interestToRevenueShareNow')), sum(1 for x in R if x['borrowBinder']=='gateClosed'), max(f('defaultCount')), min(f('convertibilityShare')), st.mean(f('govBondRatePerYear')))
    w1=win(0,24); w2=win(24,120)
    M={x['time']:x for x in rd(f'{d}/money_s1.tsv') if x['nation']==nat}
    def mrow(t):
        x=M[t]; P=float(x['priceLevelAfterIndex']); Yn=P*float(x['gdpRealPerMonth'])*12
        sav=[float(x[f'savingsStockClass{i}']) for i in range(14)]; ear=[float(x[f'earnedPerMonthClass{i}']) for i in range(14)]; pay=[float(x[f'paidPerMonthClass{i}']) for i in range(14)]
        si=sum(sav[i] for i in INV); em=sum(ear[i] for i in MASS); pm=sum(pay[i] for i in MASS)
        return P, sum(sav)/Yn, si/Yn, pm/em if em>0 else float('nan'), Yn
    Ns=[x for x in rd(f'{d}/nation_s1.tsv') if x['nation']==nat]
    def con(lo,hi):
        R=[x for x in Ns if x['quarter'].lstrip('-').isdigit() and lo<=int(x['quarter'])<hi]
        b=sum(float(x['constructionBudgetMoneyPerMonth']) for x in R if x['constructionBudgetMoneyPerMonth'] not in('','NaN'))
        pl=sum(float(x['constructionPlannedMoneyPerMonth']) for x in R if x['constructionPlannedMoneyPerMonth'] not in('','NaN'))
        de=sum(float(x['constructionDesiredMoneyPerMonth']) for x in R if x['constructionDesiredMoneyPerMonth'] not in('','NaN'))
        bb=sum(float(x['constructionBudgetBoundRegionsBudgetMoneyPerMonth']) for x in R if x['constructionBudgetBoundRegionsBudgetMoneyPerMonth'] not in('','NaN'))
        return de/max(1e-9,b), pl/max(1e-9,b), bb/max(1e-9,b)
    print(a,'Q1-8: rev=%.0f irMax=%.2f gateClosed=%d def=%.0f convMin=%.2f bond=%.3f | Q9-40: rev=%.0f irMax=%.2f gate=%d def=%.0f convMin=%.2f bond=%.3f'%(w1+w2))
    for t in ('w0','w19','q0','q4','q8','q12','q20','q39'):
        if t in M: print('   %s P=%.2f M/Y=%.2f inv/Y=%.2f massPaid/Earn=%.2f Ynom=%.0f'%((t,)+mrow(t)))
    print('   construction desired/budget q0-8=%.3f planned/budget=%.3f boundShare=%.3f | q8-40 %.3f %.3f %.3f'%(con(0,8)+con(8,40)))
