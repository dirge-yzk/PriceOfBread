import csv,collections,json
d='D:/PoB_work/keep_412/m412_ins/'
out=open('a0_model.txt','w',encoding='utf-8')
gj=json.load(open('D:/PoB_work/wt_ce/PriceOfBread.SimCore/goods.json',encoding='utf-8-sig'))
G={x['name']:x for x in gj}
seed=1
ce=collections.defaultdict(lambda: collections.defaultdict(float))
for r in csv.DictReader(open(d+f'ce_s{seed}.tsv',encoding='utf-8'),delimiter='\t'):
    if r['scope']!='C' or r['nation']!='フランス': continue
    q=int(r['quarter'])
    for k in ['dailyWantPerMonth','dailySuppliedWantPerMonth']:
        if r[k]!='': ce[q][k]+=float(r[k])
rows=[r for r in csv.DictReader(open(d+f'pxgoods_s{seed}.tsv',encoding='utf-8'),delimiter='\t') if r['nation']=='フランス' and r['tier']=='Daily' and r['time'][0]=='q']
byq=collections.defaultdict(list)
for r in rows: byq[int(r['time'][1:])].append(r)
res=[]
for q in [2,6,8,10,12,20,30]:
    rs=[r for r in byq[q] if G[r['goodName']]['baseNeed']>0 and float(r['lastDemandQty'])>0]
    num=den=numq=denq=0
    for r in rs:
        g=G[r['goodName']]; p=float(r['priceMean']); ref=float(r['refPriceIndex'])
        pf=min(max((ref/p)**g['elasticity'],0.4),2.5)
        des=g['baseNeed']*pf
        ration=float(r['lastSoldQty'])/float(r['lastDemandQty']) if float(r['lastDemandQty'])>0 else 0
        num+=des*p*ration; den+=des*p
        numq+=des*ration; denq+=des
    res.append((q,ce[q]['dailySuppliedWantPerMonth']/ce[q]['dailyWantPerMonth'],num/den,numq/denq))
for x in res: print('q%d ce=%.3f model(desired*price weights)=%.3f model(desired qty weights)=%.3f'%x,file=out)
rs=[r for r in byq[8] if G[r['goodName']]['baseNeed']>0]
print('q8 detail',file=out)
for r in rs:
    g=G[r['goodName']]; p=float(r['priceMean']); ref=float(r['refPriceIndex'])
    pf=min(max((ref/p)**g['elasticity'],0.4),2.5)
    D=float(r['lastDemandQty']);S=float(r['lastSoldQty'])
    print('  %-6s baseNeed=%.3f el=%.2f pf=%.2f desiredPC=%.4f price=%.2f ration=%.2f D=%.0f'%(r['goodName'],g['baseNeed'],g['elasticity'],pf,g['baseNeed']*pf,p,S/D if D>0 else 0,D),file=out)
