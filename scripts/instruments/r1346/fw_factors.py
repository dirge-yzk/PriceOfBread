# 問い 1: 機会の因子（賃金・土地・雇用・押し・住居・生産性〔=機会÷他の積〕）を並べる
import sys
d,seed,reg,qa,qb,step=sys.argv[1],sys.argv[2],sys.argv[3],int(sys.argv[4]),int(sys.argv[5]),int(sys.argv[6])
with open(f'{d}/region_s{seed}.tsv',encoding='utf-8') as f:
    h=f.readline().rstrip('\n').split('\t'); ix={c:i for i,c in enumerate(h)}
    print('q\tyear\tpop\trural\topp\twage\twprem\tland\temp\tpush\thous\tprod\tfood\tarable\tearned/pop\tfactOut\tfactAct\twar')
    for line in f:
        a=line.rstrip('\n').split('\t'); q=int(a[0])
        if a[ix['region']]!=reg or q<qa or q>=qb or (q-qa)%step: continue
        v=lambda c: float(a[ix[c]])
        den=v('wageFactor')*v('landFactor')*v('employmentShare')*v('pushFactor')*v('housingFactor')
        print(f"{q}\t{1783+q//4}\t{v('populationK'):.1f}\t{v('ruralPopulationK'):.1f}\t{v('opportunityIndex'):.3f}\t{v('wageFactor'):.3f}\t{v('wagePremium'):.2f}\t{v('landFactor'):.3f}\t{v('employmentShare'):.3f}\t{v('pushFactor'):.3f}\t{v('housingFactor'):.3f}\t{v('opportunityIndex')/den if den>0 else 0:.3f}\t{v('foodFillShare'):.3f}\t{v('arableLand'):.1f}\t{v('earnedPerMonth')/max(1,v('populationK')):.1f}\t{v('factoryOutputRefValuePerMonth'):.0f}\t{v('factoryActiveCount'):.0f}\t{v('warIntensity'):.3f}")
