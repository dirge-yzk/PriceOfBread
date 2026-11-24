# 問い 1・4: 1 つの州への流入と流出を、3 つの規則（R0 旧・R1 今の hf・R2 対称）で静的に数え直す〈近似〉（受け入れの上限は除く）
import sys,json,math,collections
d,seed,reg,qa,qb,qc=sys.argv[1],int(sys.argv[2]),sys.argv[3],int(sys.argv[4]),int(sys.argv[5]),int(sys.argv[6])
P={p['name']:(p['lat'],p['lon']) for p in json.load(open('C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore/places.json',encoding='utf-8'))}
def km(a,b):
    (la1,lo1),(la2,lo2)=P.get(a,(0,0)),P.get(b,(0,0))
    dl=math.radians(la2-la1); dn=math.radians(lo2-lo1)
    x=math.sin(dl/2)**2+math.cos(math.radians(la1))*math.cos(math.radians(la2))*math.sin(dn/2)**2
    x=min(1,max(0,x)); return 6371*2*math.atan2(math.sqrt(x),math.sqrt(1-x))
byq=collections.defaultdict(lambda: collections.defaultdict(list))
with open(f'{d}/region_s{seed}.tsv',encoding='utf-8') as f:
    h=f.readline().rstrip('\n').split('\t'); ix={c:i for i,c in enumerate(h)}
    for line in f:
        a=line.rstrip('\n').split('\t'); q=int(a[0])
        if qa<=q<qc: byq[q][a[ix['nation']]].append((a[ix['region']],float(a[ix['opportunityIndex']]),float(a[ix['holdingFactor']]),float(a[ix['foodFillShare']]),float(a[ix['populationK']])))
IN=collections.Counter(); OUT=collections.Counter()
for q in range(qa,qc):
    ph='膨' if q<qb else '抜'
    for nat,L in byq[q].items():
        if not any(x[0]==reg for x in L): continue
        for (sn,so,sh,sf,sp) in L:
            if not (sp>=1.0): continue
            for rule in (0,1,2):
                stay=so*sh*(sf if rule else 1.0); gs={}
                for (dn,do,dh,df,dp) in L:
                    if dn==sn: continue
                    g=do*(df if rule==2 else 1.0)-stay-0.15
                    if g<=0: continue
                    g-=0.15*km(sn,dn)/10000
                    if g>0: gs[dn]=g
                if not gs: continue
                fr=min(0.03*max(gs.values()),0.2); W=sum(gs.values())
                if sn==reg: OUT[(ph,rule)]+=fr*sp
                elif reg in gs: IN[(ph,rule)]+=fr*sp*gs[reg]/W
for ph in ('膨','抜'):
    print(f'{ph}の段: 流入 R0 {IN[(ph,0)]:.2f} R1 {IN[(ph,1)]:.2f} R2 {IN[(ph,2)]:.2f}｜流出 R0 {OUT[(ph,0)]:.2f} R1 {OUT[(ph,1)]:.2f} R2 {OUT[(ph,2)]:.2f}（月の割合×K の和＝相対だけを読む）')
