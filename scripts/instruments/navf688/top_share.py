import csv, sys, statistics as st, collections
arms=dict(a.split('=') for a in sys.argv[1:])
for lab,(d,sb) in ((k,(v.split(',')[0],int(v.split(',')[1]))) for k,v in arms.items()):
    res={1850:[],1900:[],1950:[]}; tops=collections.Counter()
    for s in range(sb,sb+10):
        by=collections.defaultdict(dict)
        for x in csv.DictReader(open(f'{d}/stock_s{s}.tsv',encoding='utf-8-sig'),delimiter='\t'):
            t=x['time']
            if t.startswith('q') and t[1:].isdigit() and int(t[1:])%4==3:
                try: by[int(x['year'])][x['nation']]=float(x['gdpRealPerYear'])
                except: pass
        for y in res:
            if y in by:
                tot=sum(by[y].values()); n,v=max(by[y].items(),key=lambda kv:kv[1]); res[y].append(v/tot)
                if y==1950: tops[n]+=1
    print(lab,{y:(round(st.median(v),3),round(max(v),3)) for y,v in res.items()},dict(tops.most_common(3)))
