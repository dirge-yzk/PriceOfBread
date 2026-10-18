import csv,collections,sys
d='D:/PoB_work/keep_412/m412_ins/'
out=open('a0_ce.txt','w',encoding='utf-8')
def load(seed):
    return list(csv.DictReader(open(d+f'ce_s{seed}.tsv',encoding='utf-8'),delimiter='\t'))
for seed in [1,2,3]:
    rows=[r for r in load(seed) if r['scope']=='C']
    nations=collections.Counter(r['nation'] for r in rows)
    print('seed',seed,dict(nations.most_common(10)),file=out)
    for nat in ['フランス','イギリス']:
        agg=collections.defaultdict(lambda: collections.defaultdict(float))
        for r in rows:
            if r['nation']!=nat: continue
            q=int(r['quarter'])
            if not (5<=q<=40): continue
            w=float(r['monthsCount'] or 0)
            c=r['className']
            for k in ['popK','dailyWantPerMonth','dailyDemandPerMonth','dailySuppliedWantPerMonth','dailyPaidPerMonth','earnedLastPositivePerMonth','foodWantPerMonth','foodSuppliedWantPerMonth','foodPaidPerMonth','foodDemandPerMonth']:
                v=r[k]
                if v=='' : continue
                agg[c][k]+=float(v)
                agg['ALL'][k]+=float(v)
        print(' ',nat,file=out)
        for c,a in agg.items():
            f=lambda n,d: (a[n]/a[d]) if a[d]>0 else float('nan')
            print('   %-6s popK=%8.0f  dailyRatio(supplied/want)=%.3f  paid/demand=%.3f  demand/want(cut)=%.3f  paid/want=%.3f | food ratio sup/want=%.3f paid/dem=%.3f  cut=%.3f'%(c,a['popK']/36,f('dailySuppliedWantPerMonth','dailyWantPerMonth'),f('dailyPaidPerMonth','dailyDemandPerMonth'),f('dailyDemandPerMonth','dailyWantPerMonth'),f('dailyPaidPerMonth','dailyWantPerMonth'),f('foodSuppliedWantPerMonth','foodWantPerMonth'),f('foodPaidPerMonth','foodDemandPerMonth'),f('foodDemandPerMonth','foodWantPerMonth')),file=out)
