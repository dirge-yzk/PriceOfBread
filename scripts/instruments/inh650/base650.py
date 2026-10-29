import csv,sys,statistics
tot=0;hit=0;byseed=[]
for s in range(1,11):
    rows={}
    with open(f'D:/PoB_work/keep_645/m645_ins/nation_s{s}.tsv',encoding='utf-8') as f:
        r=csv.DictReader(f,delimiter='\t')
        for d in r:
            rows.setdefault(d['nation'],[]).append((int(d['quarter']),float(d['civilWarMonths']),d['polityCode']))
    t=h=0
    for n,v in rows.items():
        v.sort()
        col=[0]*len(v)
        for i,(q,c,p) in enumerate(v):
            if c>0: col[i]=1
            if i>0 and v[i-1][2]!=p: col[i]=1
        pre=[0]
        for x in col: pre.append(pre[-1]+x)
        for i in range(20,len(v)):
            t+=1
            if pre[i]-pre[i-20]>0: h+=1   # window of 20 quarters before quarter i
    byseed.append(h/t); tot+=t; hit+=h
print('windows',tot,'base rate',hit/tot, 'by seed',[round(x,3) for x in byseed])
