import csv, sys, collections, statistics as st, glob
d='D:/PoB_work/keep_407/m407_ins/'
cols=['adminFillShare','adminBureaucratCoverShare','adminNeedAtPurchasePerMonth','adminClaimCappedAtPurchasePerMonth','claimAdminPerMonth','revenueBasePerMonth','adminOverNeedRegionsCount']
yrs=[1783,1785,1790,1800,1820,1850,1880,1900,1914,1930,1945]
agg=collections.defaultdict(lambda: collections.defaultdict(list))
for f in sorted(glob.glob(d+'fiscal_s*.tsv')):
    with open(f,encoding='utf-8') as fh:
        rd=csv.DictReader(fh,delimiter='\t')
        bucket=collections.defaultdict(lambda: collections.defaultdict(list))
        for r in rd:
            y=int(r['year'])
            if y not in yrs: continue
            for c in cols:
                try: bucket[(r['nation'],y)][c].append(float(r[c]))
                except: pass
        for k,v in bucket.items():
            for c in cols:
                if v[c]: agg[k][c].append(sum(v[c])/len(v[c]))
nat=sorted({k[0] for k in agg})
for n in nat:
    print('==',n)
    print('year  fill(med,min,max)  cover  claimAdm/rev  need/rev  capped/need nseeds')
    for y in yrs:
        a=agg.get((n,y))
        if not a: continue
        m=lambda c: st.median(a[c]) if a[c] else float('nan')
        rev=m('revenueBasePerMonth')
        fl=a['adminFillShare']
        print(y, '%.3f (%.3f,%.3f) cover=%.3f claim/rev=%.3f need/rev=%.3f capped/need=%.3f n=%d'%(m('adminFillShare'),min(fl),max(fl),m('adminBureaucratCoverShare'),m('claimAdminPerMonth')/rev if rev else 0,m('adminNeedAtPurchasePerMonth')/rev if rev else 0,(m('adminClaimCappedAtPurchasePerMonth')/m('adminNeedAtPurchasePerMonth') if m('adminNeedAtPurchasePerMonth') else 0),len(fl)))
