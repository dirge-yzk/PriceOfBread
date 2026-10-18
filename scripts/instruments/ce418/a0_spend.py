import csv,collections,json
d='D:/PoB_work/keep_412/m412_ins/'
out=open('a0_spend.txt','w',encoding='utf-8')
gj=json.load(open('D:/PoB_work/wt_ce/PriceOfBread.SimCore/goods.json',encoding='utf-8-sig'))
G={x['name']:x for x in gj}
seed=1
px=collections.defaultdict(dict)
for r in csv.DictReader(open(d+f'pxgoods_s{seed}.tsv',encoding='utf-8'),delimiter='\t'):
    if r['nation']!='フランス' or r['time'][0]!='q': continue
    q=int(r['time'][1:]); D=float(r['lastDemandQty']); S=float(r['lastSoldQty'])
    px[q][r['goodName']]=(S,D)
sp=collections.defaultdict(lambda: collections.defaultdict(dict))
buckets=set()
for r in csv.DictReader(open(d+f'spend_s{seed}.tsv',encoding='utf-8'),delimiter='\t'):
    if r['nation']!='フランス' or r['kind']!='good' or r['tier']!='Daily': continue
    b=(int(r['firstQuarter']),int(r['lastQuarter']))
    sp[b][r['className']][r['slotName']]=(float(r['payPerMonth']),float(r['qtyPerMonth']))
for b in sorted(sp)[:10]:
    qs=range(b[0],b[1]+1)
    rat={}
    for g in G:
        S=sum(px[q].get(g,(0,0))[0] for q in qs if q in px); D=sum(px[q].get(g,(0,0))[1] for q in qs if q in px)
        if D>0: rat[g]=S/D
    line='bucket %s'%(b,)
    for cls in ['貴族','農民','工場労働者','職人']:
        if cls not in sp[b]: continue
        pays=sp[b][cls]
        num=sum(v[0] for g,v in pays.items() if g in rat and rat[g]>0)
        den=sum(v[0]/rat[g] for g,v in pays.items() if g in rat and rat[g]>0)
        line+=' | %s harmonic(nation px ration)=%.3f'%(cls,num/den if den>0 else float('nan'))
    print(line,file=out)
