import csv,collections,json
d='D:/PoB_work/keep_412/m412_ins/'
out=open('a0_cmp3.txt','w',encoding='utf-8')
gj=json.load(open('D:/PoB_work/wt_ce/PriceOfBread.SimCore/goods.json',encoding='utf-8-sig'))
G={x['name']:x for x in gj}
for seed in [1,2,3]:
    ce=collections.defaultdict(lambda: collections.defaultdict(float))
    for r in csv.DictReader(open(d+f'ce_s{seed}.tsv',encoding='utf-8'),delimiter='\t'):
        if r['scope']!='C' or r['nation']!='フランス': continue
        q=int(r['quarter'])
        for k in ['dailyWantPerMonth','dailySuppliedWantPerMonth']:
            if r[k]!='': ce[q][k]+=float(r[k])
    rows=[r for r in csv.DictReader(open(d+f'pxgoods_s{seed}.tsv',encoding='utf-8'),delimiter='\t') if r['nation']=='フランス' and r['tier']=='Daily' and r['time'][0]=='q']
    byq=collections.defaultdict(list)
    for r in rows: byq[int(r['time'][1:])].append(r)
    print('seed',seed,'  q | ce daily | px all-Daily qty | px grp1(no nutrition, baseNeed>0) qty | grp1 price-weighted | grp1 price-weighted, mean q5..40',file=out)
    acc=[];accce=[];accq=[]
    for q in range(0,48):
        rs=[r for r in byq[q] if G[r['goodName']]['nutrition']<=0 and G[r['goodName']]['baseNeed']>0 and float(r['lastDemandQty'])>0]
        D=lambda r: float(r['lastDemandQty']); S=lambda r: float(r['lastSoldQty']); P=lambda r: float(r['priceMean'])
        pd=sum(D(r)*P(r) for r in rs); ps=sum(S(r)*P(r) for r in rs); qd=sum(map(D,rs)); qs=sum(map(S,rs))
        allD=sum(D(r) for r in byq[q]); allS=sum(S(r) for r in byq[q])
        if 5<=q<=40: acc.append(ps/pd); accce.append(ce[q]['dailySuppliedWantPerMonth']/ce[q]['dailyWantPerMonth']); accq.append(qs/qd)
        if q in (0,4,8,12,20,30,40,44): print('%2d | %.3f | %.3f | %.3f | %.3f'%(q,ce[q]['dailySuppliedWantPerMonth']/ce[q]['dailyWantPerMonth'],allS/allD,qs/qd,ps/pd),file=out)
    print('  mean q5..40: ce %.3f  px grp1 qty %.3f  px grp1 price-weighted %.3f'%(sum(accce)/len(accce),sum(accq)/len(accq),sum(acc)/len(acc)),file=out)
print([ (n,x['nutrition'],x['baseNeed']) for n,x in G.items() if x['tier']=='Daily' and x['baseNeed']>0 and x['nutrition']>0],file=out)
print([ n for n,x in G.items() if x['tier']=='Daily' and x['baseNeed']>0 and x['nutrition']<=0],file=out)
