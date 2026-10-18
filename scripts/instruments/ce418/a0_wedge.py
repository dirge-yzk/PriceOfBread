import csv,collections,json
d='D:/PoB_work/keep_412/m412_ins/'
out=open('a0_wedge.txt','w',encoding='utf-8')
gj=json.load(open('D:/PoB_work/wt_ce/PriceOfBread.SimCore/goods.json',encoding='utf-8-sig'))
G={x['name']:x for x in gj}
seed=1
px=collections.defaultdict(dict)
for r in csv.DictReader(open(d+f'pxgoods_s{seed}.tsv',encoding='utf-8'),delimiter='\t'):
    if r['nation']!='フランス' or r['time'][0]!='q': continue
    px[int(r['time'][1:])][r['goodName']]=r
# wedge per good from spend_s nobles/peasants bucket (4,7) and (8,39)
wedge={}
sp={}
for r in csv.DictReader(open(d+f'spend_s{seed}.tsv',encoding='utf-8'),delimiter='\t'):
    if r['nation']!='フランス' or r['kind']!='good' or r['tier']!='Daily' or r['className']!='貴族': continue
    b=(int(r['firstQuarter']),int(r['lastQuarter']))
    if b!=(4,7): continue
    pay=float(r['payPerMonth']); rec=float(r['receiptPerMonth']); qty=float(r['qtyPerMonth'])
    sp[r['slotName']]=(pay,rec,qty)
print('nobles bucket 4-7: good pay receipt qty pay/receipt (wedge) pay/qty (buyer price)',file=out)
for g,(pay,rec,qty) in sp.items():
    print('  %-6s pay=%.2f rec=%.2f qty=%.3f wedge=%.3f buyerprice=%.2f'%(g,pay,rec,qty,pay/rec if rec>0 else float('nan'),pay/qty if qty>0 else float('nan')),file=out)
res=[]
for q in [4,5,6,7]:
    num=den=0; num0=den0=0
    for g,r in px[q].items():
        gg=G[g]
        if gg['baseNeed']<=0 or float(r['lastDemandQty'])<=0 or gg['tier']!='Daily': continue
        p=float(r['priceMean']); ref=float(r['refPriceIndex'])
        pf=min(max((ref/p)**gg['elasticity'],0.4),2.5)
        des=gg['baseNeed']*pf
        D=float(r['lastDemandQty']);S=float(r['lastSoldQty']); rat=S/D
        w=sp.get(g); wd=(w[0]/w[1]) if w and w[1]>0 else 1.0
        num+=des*p*wd*rat; den+=des*p*wd
        num0+=des*p*rat; den0+=des*p
    res.append((q,num0/den0,num/den))
for x in res: print('q%d model no-wedge=%.3f with-wedge=%.3f'%x,file=out)
