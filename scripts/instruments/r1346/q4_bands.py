# 問い 4: 国内の移民の量を食の充足の帯（出どころ×行き先）で分け、腕どうしで比べる。
# あわせて hf の腕の盤面の上で、国内の移民の計画の量を 3 つの規則で静的に数え直す（1 ステップの〈近似〉・受け入れの上限は除く）:
#   R0＝旧（stay=opp×hold・行き先=opp）  R1＝今の hf（stay=opp×hold×food・行き先=opp）  R2＝対称（stay=opp×hold×food・行き先=opp×food）
# 食は計器の foodFillShare（平滑の前の生の比）＝ Region.FoodFill（平滑 0.2）の〈近似〉。
import sys,json,math,collections
arm,seeds=sys.argv[1],[int(s) for s in sys.argv[2].split(',')]
static=len(sys.argv)>3 and sys.argv[3]=='static'
P={p['name']:(p['lat'],p['lon']) for p in json.load(open('C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore/places.json',encoding='utf-8'))}
def km(a,b):
    (la1,lo1),(la2,lo2)=P.get(a,(0,0)),P.get(b,(0,0))
    dl=math.radians(la2-la1); dn=math.radians(lo2-lo1)
    x=math.sin(dl/2)**2+math.cos(math.radians(la1))*math.cos(math.radians(la2))*math.sin(dn/2)**2
    x=min(1,max(0,x)); return 6371*2*math.atan2(math.sqrt(x),math.sqrt(1-x))
KM={}
def band(f): return '<0.5' if f<0.5 else '0.5-0.9' if f<0.9 else '0.9-0.99' if f<0.99 else '>=0.99'
B=['<0.5','0.5-0.9','0.9-0.99','>=0.99']
tot=collections.Counter(); worse=0.0; allf=0.0
plan=collections.Counter()
for s in seeds:
    R={}
    byq=collections.defaultdict(lambda: collections.defaultdict(list))
    with open(f'{arm}/region_s{s}.tsv',encoding='utf-8') as f:
        h=f.readline().rstrip('\n').split('\t'); ix={c:i for i,c in enumerate(h)}
        io,ih,ifd,ip,iN,ir=ix['opportunityIndex'],ix['holdingFactor'],ix['foodFillShare'],ix['populationK'],ix['nation'],ix['region']
        for line in f:
            a=line.rstrip('\n').split('\t'); q=int(a[0])
            rec=(float(a[io]),float(a[ih]),float(a[ifd]),float(a[ip]))
            R[(q,a[ir])]=rec
            if static: byq[q][a[iN]].append((a[ir],)+rec)
    with open(f'{arm}/migration_s{s}.tsv',encoding='utf-8') as f:
        f.readline()
        for line in f:
            a=line.split('\t'); q=int(a[0]); m=float(a[3])
            src=R.get((q,a[1])); dst=R.get((q,a[2]))
            if src is None or dst is None: continue
            tot[(band(src[2]),band(dst[2]))]+=m; allf+=m
            if dst[2]<src[2]: worse+=m
    if static:
        for q,nats in byq.items():
            for nat,L in nats.items():
                if len(L)<2: continue
                for (sn,so,sh,sf,sp) in L:
                    if not (sp>=1.0): continue
                    for rule in (0,1,2):
                        stay=so*sh*(sf if rule else 1.0)
                        gs=[]
                        for (dn,do,dh,df,dp) in L:
                            if dn==sn: continue
                            dv=do*(df if rule==2 else 1.0)
                            g=dv-stay-0.15
                            if g<=0: continue
                            k=(sn,dn)
                            d=KM.get(k)
                            if d is None: d=KM[k]=km(sn,dn)
                            g-=0.15*d/10000
                            if g>0: gs.append(g)
                        if gs:
                            fr=min(0.03*max(gs),0.2)
                            plan[(rule,band(sf))]+=fr*sp
    print(f'# seed {s} done',file=sys.stderr)
print(f'## 腕 {arm} 種 {seeds}')
print(f'国内の移民の和 {allf:.0f}K・行き先の食が出どころより低い流れ {worse:.0f}K（{worse/allf:.1%}）')
print('出どころ\行き先\t'+'\t'.join(B)+'\t和')
for sb in B:
    print(sb+'\t'+'\t'.join(f'{tot[(sb,db)]:.0f}' for db in B)+f'\t{sum(tot[(sb,db)] for db in B):.0f}')
print('和\t'+'\t'.join(f'{sum(tot[(sb,db)] for sb in B):.0f}' for db in B))
if static:
    print('## 静的な数え直し（出どころの食の帯ごとの計画の量の和・単位は K×四半期の割合）')
    print('帯\tR0旧\tR1今のhf\tR2対称')
    for sb in B+['和']:
        g=lambda r: sum(plan[(r,b)] for b in B) if sb=='和' else plan[(r,sb)]
        print(f'{sb}\t{g(0):.0f}\t{g(1):.0f}\t{g(2):.0f}')
