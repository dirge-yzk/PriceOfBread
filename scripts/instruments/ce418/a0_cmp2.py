import csv,collections
d='D:/PoB_work/keep_412/m412_ins/'
out=open('a0_cmp2.txt','w',encoding='utf-8')
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
print('q  ce | px qty | px priceMean-weighted | px (demand>0) priceMean-weighted excl no-need | ratio of (price/ref)',file=out)
for q in [0,4,8,12,20,30,40]:
    rs=byq[q]
    D=lambda r: float(r['lastDemandQty']); S=lambda r: float(r['lastSoldQty']); P=lambda r: float(r['priceMean'])
    qd=sum(map(D,rs)); qs=sum(map(S,rs))
    pd=sum(D(r)*P(r) for r in rs); ps=sum(S(r)*P(r) for r in rs)
    print('%2d  %.3f | %.3f | %.3f'%(q,ce[q]['dailySuppliedWantPerMonth']/ce[q]['dailyWantPerMonth'],qs/qd,ps/pd),file=out)
rs=byq[8]
print('q8 detail: good, D, S, ratio, priceMean, ref, D*price share',file=out)
tot=sum(float(r['lastDemandQty'])*float(r['priceMean']) for r in rs)
for r in sorted(rs,key=lambda r:-float(r['lastDemandQty'])*float(r['priceMean'])):
    x=float(r['lastDemandQty'])*float(r['priceMean'])
    if float(r['lastDemandQty'])>0: print('  %-6s D=%7.0f S=%7.0f ratio=%.2f price=%.2f ref=%.2f valueShare=%.3f'%(r['goodName'],float(r['lastDemandQty']),float(r['lastSoldQty']),float(r['lastSoldQty'])/float(r['lastDemandQty']),float(r['priceMean']),float(r['refPriceIndex']),x/tot),file=out)
