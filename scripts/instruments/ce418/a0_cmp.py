import csv,collections
d='D:/PoB_work/keep_412/m412_ins/'
out=open('a0_cmp.txt','w',encoding='utf-8')
for seed in [1,2]:
    ce=collections.defaultdict(lambda: collections.defaultdict(float))
    for r in csv.DictReader(open(d+f'ce_s{seed}.tsv',encoding='utf-8'),delimiter='\t'):
        if r['scope']!='C' or r['nation']!='フランス': continue
        q=int(r['quarter']); 
        for k in ['dailyWantPerMonth','dailyDemandPerMonth','dailySuppliedWantPerMonth','dailyPaidPerMonth']:
            if r[k]!='': ce[q][k]+=float(r[k])
    px=collections.defaultdict(lambda: collections.defaultdict(float))
    for r in csv.DictReader(open(d+f'pxgoods_s{seed}.tsv',encoding='utf-8'),delimiter='\t'):
        if r['nation']!='フランス' or r['tier']!='Daily' or r['time'][0]!='q': continue
        q=int(r['time'][1:]); D=float(r['lastDemandQty']); S=float(r['lastSoldQty']); p=float(r['refPriceIndex'])
        px[q]['D']+=D; px[q]['S']+=S; px[q]['pD']+=D*p; px[q]['pS']+=S*p
    print('seed',seed,file=out)
    print('q  ce:sup/want  ce:paid/dem  | px:S/D(qty)  px:S/D(refprice)',file=out)
    for q in range(0,48,2):
        a=ce[q]; b=px[q]
        if a['dailyWantPerMonth']>0 and b['D']>0:
            print('%2d  %.3f  %.3f  | %.3f  %.3f'%(q,a['dailySuppliedWantPerMonth']/a['dailyWantPerMonth'],a['dailyPaidPerMonth']/max(1e-9,a['dailyDemandPerMonth']),b['S']/b['D'],b['pS']/b['pD']),file=out)
