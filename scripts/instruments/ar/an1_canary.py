import csv, collections
A='D:/PoB_work/ar/id/ins_form/'; B='D:/PoB_work/ar/id/ins_B/'
def load(d,f,key):
    out={}
    for r in csv.DictReader(open(d+f,encoding='utf-8'),delimiter='\t'):
        out[key(r)]=r
    return out
a=load(A,'admrule_s1.tsv',lambda r:(r['nation'],int(r['year']),int(r['quarter'])))
b=load(B,'admrule_s1.tsv',lambda r:(r['nation'],int(r['year']),int(r['quarter'])))
nats=['フランス','イギリス','清','ロシア','オスマン','プロイセン','オーストリア','日本']
print('B vs A  deliveryShare(B)/deliveryOld(B) ; fill A/B ; stateReach A->B   (seed1, last row of listed year)')
for n in nats:
    row=[]
    for y in (1783,1790,1800,1820,1850):
        ks=[k for k in b if k[0]==n and k[1]==y]
        if not ks: continue
        k=max(ks,key=lambda k:k[2])
        rb=b[k]; ra=a.get(k)
        row.append('%d f %s/%s dlv %.3f/%.3f(x%.2f) law %.3f->%.3f'%(y,'%.2f'%float(ra['adminFillShare']) if ra else '-','%.2f'%float(rb['adminFillShare']),float(rb['deliveryShare']),float(rb['deliveryOldShare']),float(rb['deliveryShare'])/float(rb['deliveryOldShare']),float(rb['stateReachOldShare']),float(rb['stateReachShare'])))
    print(n,' | '.join(row))
# revenue/GDP from rule_s
ra=load(A,'rule_s1.tsv',lambda r:(r['nation'],int(r['quarter']),r['region']))
rb=load(B,'rule_s1.tsv',lambda r:(r['nation'],int(r['quarter']),r['region']))
print('tax/GDP (rule_s revenuePerMonth*12/gdpPerMonth/12) A -> B , ruleCut A->B, at year-end rows')
for n in nats:
    row=[]
    for y in (1784,1790,1800,1820,1850):
        k=None
        for kk in ra:
            if kk[0]==n and int(ra[kk]['year'])==y: k=kk
        if k is None or k not in rb: continue
        x=ra[k]; z=rb[k]
        row.append('%d %.3f->%.3f cut %.0f->%.0f'%(y,float(x['revenuePerMonth'])/float(x['gdpPerMonth']),float(z['revenuePerMonth'])/float(z['gdpPerMonth']),float(x['ruleCutPerMonth']),float(z['ruleCutPerMonth'])))
    print(n,' | '.join(row))
