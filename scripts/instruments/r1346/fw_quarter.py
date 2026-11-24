# 問い 1: 州を四半期ごとに並べ、残る値打ち（食あり／なし）と同じ国の最大の機会から差 g を出す（距離の項は除く〈近似〉）
import sys
d,seed,reg,qa,qb=sys.argv[1],sys.argv[2],sys.argv[3],int(sys.argv[4]),int(sys.argv[5])
step=int(sys.argv[6]) if len(sys.argv)>6 else 1
TF=0.15; MOB=0.03
rows={}
with open(f'{d}/region_s{seed}.tsv',encoding='utf-8') as f:
    h=f.readline().rstrip('\n').split('\t'); ix={c:i for i,c in enumerate(h)}
    for line in f:
        a=line.rstrip('\n').split('\t'); q=int(a[0])
        if q<qa or q>=qb: continue
        rows.setdefault(q,[]).append(a)
g=lambda a,c: float(a[ix[c]])
print('q\tyear\towner\tpop\topp\thold\tfood\tstay(hf)\tstay(食なし)\tmaxOppDst\tdst\tgMax(hf)\tgMax(食なし)\tout\tin\thousFill\tland\tabsorb~')
for q in range(qa,qb,step):
    R=rows.get(q,[]); me=[a for a in R if a[ix['region']]==reg]
    if not me: continue
    m=me[0]; nat=m[ix['nation']]
    opp,hold,food=g(m,'opportunityIndex'),g(m,'holdingFactor'),g(m,'foodFillShare')
    stay=opp*hold*food; stay0=opp*hold
    best=max(((g(a,'opportunityIndex'),a[ix['region']]) for a in R if a[ix['nation']]==nat and a[ix['region']]!=reg),default=(0,''))
    hf=1-2*(1-g(m,'housingFactor')); land=g(m,'landFactor')
    ab=(min(1,max(0,food))*min(1,max(0,hf))*land)**2
    print(f"{q}\t{1783+q//4}\t{nat[:4]}\t{g(m,'populationK'):.2f}\t{opp:.3f}\t{hold:.3f}\t{food:.3f}\t{stay:.3f}\t{stay0:.3f}\t{best[0]:.3f}\t{best[1]}\t{best[0]-stay-TF:.3f}\t{best[0]-stay0-TF:.3f}\t{g(m,'migrationOutKPerQuarter'):.3f}\t{g(m,'migrationInKPerQuarter'):.3f}\t{hf:.3f}\t{land:.3f}\t{ab:.3f}")
