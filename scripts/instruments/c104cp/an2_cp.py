import csv,collections,sys
d=sys.argv[1]
tot=0;up=0;byp=collections.Counter();byt=collections.Counter()
da=[];dl0=[]
for s in range(1,11):
    for r in csv.DictReader(open(f'{d}/center_s{s}.tsv'),delimiter='\t'):
        try: k=float(r['safetyKIndex']);u=float(r['safetyKPlusIndex'])
        except: continue
        if k!=k or u!=u: continue
        tot+=1; byt[r['polityCode']]+=1
        if u<k: up+=1; byp[r['polityCode']]+=1
        if r['quarter']=='0': da.append(float(r['armyShare']))
da.sort()
print('K+1 で下がる行',up,'/',tot,{k:(byp[k],byt[k]) for k in byt})
print('q0 armyShare 分位 p10/p50/p90',da[len(da)//10],da[len(da)//2],da[9*len(da)//10],'>0.65:',sum(x>0.65 for x in da),'/',len(da))
