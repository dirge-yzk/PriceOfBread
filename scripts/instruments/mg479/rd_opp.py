# (479) 段0の読み：国×期の人口加重の機会 O・残る値打ち（O×持ち分×食料充足）・持ち分・食料充足。使い方：python rd_opp.py <計器の出力フォルダ> 1,2,3,4,5
import csv, sys, statistics as st, collections
base=sys.argv[1]; seeds=[int(x) for x in sys.argv[2].split(',')]
want=['イギリス','アメリカ','清','ロシア','フランス','プロイセン','アイルランド']
periods={'P1':(0,67),'P2':(68,267),'P3':(268,467),'P4':(468,667)}
out=collections.defaultdict(list)
for sd in seeds:
    acc=collections.defaultdict(lambda:[0,0,0,0,0,0.0, 0.0])  # w,sum O, sum push, sum hold, sumO-gtpush..., 
    with open(f'{base}/region_s{sd}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            n=r['nation']
            if n not in want or r['activeFlag']!='1': continue
            q=int(r['quarter'])
            for p,(a,b) in periods.items():
                if a<=q<=b:
                    w=float(r['populationK']); O=float(r['opportunityIndex']); h=float(r['holdingFactor']); ff=float(r['foodFillShare'])
                    x=acc[(n,p)]; x[0]+=w; x[1]+=w*O; x[2]+=w*O*h*ff; x[3]+=w*h; x[4]+=w*ff; x[5]+=w*float(r['wageFactor']) ; x[6]+=w*float(r['landFactor'])
                    break
    for k,x in acc.items():
        if x[0]>0: out[k].append((x[1]/x[0],x[2]/x[0],x[3]/x[0],x[4]/x[0],x[5]/x[0],x[6]/x[0]))
print('nation period : popw mean  Opportunity | push(O*hold*food) | holding | foodFill | wageFactor | landFactor   (median over seeds)')
for n in want:
    for p in periods:
        v=out.get((n,p))
        if v: print(n,p,' | '.join(f"{st.median([t[i] for t in v]):.3f}" for i in range(6)))
