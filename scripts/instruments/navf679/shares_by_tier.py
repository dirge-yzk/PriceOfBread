import csv,sys,statistics as st,collections
D='D:/PoB_work/keep_675/m675_ins/'
YMAX=int(sys.argv[1]); majors=['イギリス','フランス','プロイセン','ロシア','オーストリア','アメリカ','日本']
res=collections.defaultdict(lambda: collections.defaultdict(list))
nports=collections.defaultdict(lambda: collections.defaultdict(list))
for s in range(1,11):
    tier={}
    cap={}
    with open(D+f'region_s{s}.tsv',encoding='utf-8') as f:
        r=csv.DictReader(f,delimiter='\t')
        for row in r:
            q=int(row['quarter']); n=row['nation']
            tier[(q,n,row['region'])]=row['tier']
            if q==0 and n not in cap: cap[n]=row['region']
    tot=collections.defaultdict(lambda:[0,0,0,0,0])  # cap, homeOther, colonial ; counts
    with open(D+f'port_s{s}.tsv',encoding='utf-8') as f:
        r=csv.DictReader(f,delimiter='\t')
        agg=collections.defaultdict(lambda:[0.0,0.0,0.0,0,0])
        for row in r:
            q=int(row['quarter']); y=int(row['year']); n=row['nation']
            if n not in majors or y>YMAX: continue
            t=tier.get((q,n,row['region']))
            thr=float(row['portThroughput'])
            if not thr>0: continue
            a=agg[(n,q)]
            if row['region']==cap.get(n): a[0]+=thr
            elif t=='Colonial': a[2]+=thr; a[4]+=1
            else: a[1]+=thr; a[3]+=1
    for (n,q),a in agg.items():
        T=a[0]+a[1]+a[2]
        if T<=0: continue
        res[n]['cap'].append(a[0]/T); res[n]['homeOther'].append(a[1]/T); res[n]['col'].append(a[2]/T)
        H=a[0]+a[1]
        res[n]['capOfHome'].append(a[0]/H if H>0 else float('nan'))
        nports[n]['home'].append(a[3]); nports[n]['col'].append(a[4])
for n in majors:
    d=res[n]
    print(n,'共通(全港) 首都/本国他/植民地 中央値 %.2f %.2f %.2f'%(st.median(d['cap']),st.median(d['homeOther']),st.median(d['col'])),
          '| 本国の港の内の首都 %.2f'%st.median([x for x in d['capOfHome'] if x==x]),
          '| 本国他の港の数(中央) %s 植民地の港の数 %s'%(st.median(nports[n]['home']),st.median(nports[n]['col'])))
