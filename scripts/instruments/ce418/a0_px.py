import csv,collections,sys
d='D:/PoB_work/keep_412/m412_ins/'
out=open('a0_px.txt','w',encoding='utf-8')
rows=list(csv.DictReader(open(d+'pxgoods_s1.tsv',encoding='utf-8'),delimiter='\t'))
ts=[]
for r in rows:
    if r['time'] not in ts: ts.append(r['time'])
print(ts, file=out)
fr=[r for r in rows if r['nation']=='フランス' and r['tier']=='Daily']
print(len(fr), file=out)
bytime=collections.defaultdict(list)
for r in fr: bytime[r['time']].append(r)
for t in ts[:3]+ts[-4:]:
    rs=bytime[t]
    if not rs: continue
    print(t, rs[0]['year'], len(rs), file=out)
    D=sum(float(r['lastDemandQty']) for r in rs); S=sum(float(r['lastSoldQty']) for r in rs)
    pD=sum(float(r['lastDemandQty'])*float(r['refPriceIndex']) for r in rs); pS=sum(float(r['lastSoldQty'])*float(r['refPriceIndex']) for r in rs)
    print('  qty-sum S/D', S/D, ' refprice-weighted', pS/pD, ' mean per good', sum(float(r['lastSoldQty'])/float(r['lastDemandQty']) for r in rs if float(r['lastDemandQty'])>0)/len(rs), file=out)
    for r in rs: print('   ',r['goodName'], r['lastProducedQty'][:8], r['lastDemandQty'][:8], r['lastSoldQty'][:8], 'ratio %.2f'%(float(r['lastSoldQty'])/max(1e-9,float(r['lastDemandQty']))), file=out)
