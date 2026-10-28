import csv,sys,collections,statistics
arms={'m627':'D:/PoB_work/keep_627/m627_ins','n1':'D:/PoB_work/keep_627/n1','n2':'D:/PoB_work/keep_627/n2'}
nats=['イギリス','フランス','プロイセン']
qs={1850:268,1880:388,1913:520}
res=collections.defaultdict(dict)
for a,d in arms.items():
    for s in range(1,11):
        try: f=open(f'{d}/region_s{s}.tsv',encoding='utf-8')
        except Exception as e: print('missing',a,s); continue
        acc=collections.defaultdict(lambda:[0.0,0.0])
        for row in csv.DictReader(f,delimiter='\t'):
            q=int(row['quarter'])
            if q in qs.values() and row['nation'] in nats and row['tier']=='Core':
                k=(row['nation'],q); acc[k][0]+=float(row['populationK']); acc[k][1]+=float(row['ruralPopulationK'])
        for (n,q),(p,r) in acc.items():
            if p>0: res[(a,n,q)][s]=1-r/p
for n in nats:
    for y,q in qs.items():
        out=[]
        for a in arms:
            v=list(res[(a,n,q)].values())
            if v: out.append('%s med%.3f [%.3f,%.3f] n%d'%(a,statistics.median(v),min(v),max(v),len(v)))
        print(n,y,' | '.join(out))
print('--- hull and 2SE (m627 seeds std/sqrt10 x2)')
for n in nats:
    for y,q in qs.items():
        meds=[statistics.median(res[(a,n,q)].values()) for a in arms]
        sd=statistics.pstdev(res[('m627',n,q)].values())
        se2=2*sd/10**0.5
        print(n,y,'hull [%.3f,%.3f] 2SE %.3f -> band [%.3f,%.3f]'%(min(meds),max(meds),se2,min(meds)-se2,max(meds)+se2))
