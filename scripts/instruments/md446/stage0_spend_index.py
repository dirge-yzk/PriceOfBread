import csv,statistics as st,collections,sys
sys.stdout.reconfigure(encoding='utf-8')
D='D:/PoB_work/keep_443/m443_ins/'
NAT=['イギリス','清','プロイセン','ロシア','日本','フランス']
CL=['農民','工場労働者','使用人','貴族','資本家']
W={2:'1785-92',3:'1793-1822',4:'1823-49',5:'1823-49',6:'1823-49',7:'1850-82',8:'1883-1949'}
res=collections.defaultdict(lambda: collections.defaultdict(list))
for s in range(1,11):
    size=collections.defaultdict(list)
    with open(D+'class_s%d.tsv'%s,encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r); ix={n:i for i,n in enumerate(h)}
        for row in r:
            try: y=int(row[ix['year']])
            except: continue
            size[(row[ix['nation']],row[ix['className']],y)].append(float(row[ix['sizeK']]))
    sz={k:st.mean(v) for k,v in size.items()}
    tmp=collections.defaultdict(list)
    with open(D+'spend_s%d.tsv'%s,encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r); ix={n:i for i,n in enumerate(h)}
        for row in r:
            if row[ix['slotName']]!='医療' or row[ix['kind']]!='good': continue
            n=row[ix['nation']]; c=row[ix['className']]; b=int(row[ix['bucket']])
            if n not in NAT or c not in CL or b not in W: continue
            fq=int(row[ix['firstQuarter']]); lq=int(row[ix['lastQuarter']]); lq=667 if lq<0 else lq
            ys=range(1783+fq//4,1783+lq//4+1)
            pk=st.mean([sz[(n,c,y)] for y in ys if (n,c,y) in sz]) if any((n,c,y) in sz for y in ys) else None
            if not pk: continue
            tmp[(n,c,W[b])].append(float(row[ix['qtyPerMonth']])/(0.05*pk))
    for k,v in tmp.items(): res[k[0]+'|'+k[1]][k[2]].append(st.mean(v))
order=['1785-92','1793-1822','1823-49','1850-82','1883-1949']
for n in NAT:
    print(n)
    for c in CL:
        d=res[n+'|'+c]
        print('  ',c,' '.join(f'{p}:{st.median(d[p]):.3f}' if d[p] else f'{p}:-' for p in order))
