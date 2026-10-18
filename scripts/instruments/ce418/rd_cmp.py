import csv,glob,os,re,sys,collections,statistics as st
csv.field_size_limit(1<<30)
def fl(x):
    try: return float(x)
    except: return float('nan')
A=sys.argv[1]; B=sys.argv[2]
def load(d,prefix,cols):
    out=[]
    for f in sorted(glob.glob(os.path.join(d,prefix+'[0-9]*.tsv'))):
        s=int(re.search(r'(\d+)\.tsv$',f).group(1))
        with open(f,encoding='utf-8',newline='') as fh:
            for r in csv.DictReader(fh,delimiter='\t'):
                out.append((s,{c:r[c] for c in cols if c in r}))
    return out
NAT=['清','ロシア','オスマン','フランス','イギリス','プロイセン','オーストリア','日本']
# food fill pop-weighted by (nation, year bucket)
def food(d):
    acc=collections.defaultdict(lambda:[0,0])
    low=collections.defaultdict(int); tot=collections.defaultdict(int)
    for s,r in load(d,'region_s',['year','nation','populationK','foodFillShare']):
        n=r['nation']; y=int(fl(r['year'])); p=fl(r['populationK']); f=fl(r['foodFillShare'])
        if not(p==p and f==f and p>0): continue
        k=(n,(y//50)*50); acc[k][0]+=p*f; acc[k][1]+=p
        tot[n]+=1
        if f<0.9: low[n]+=1
    return acc,low,tot
fa,la,ta=food(A); fb,lb,tb=food(B)
print('== 主食の充足 foodFillShare（人口加重・国×50年）A→B')
for n in NAT:
    row=[]
    for y in (1750,1800,1850,1900):
        a=fa.get((n,y)); b=fb.get((n,y))
        if a and b and a[1]>0 and b[1]>0: row.append(f'{y}:{a[0]/a[1]:.3f}→{b[0]/b[1]:.3f}')
    print(n,' '.join(row),' 州×四半期で充足<0.9の割合',f'{la[n]/max(ta[n],1):.3f}→{lb[n]/max(tb[n],1):.3f}')
# population at nation level
def pop(d):
    acc=collections.defaultdict(list)
    for s,r in load(d,'nation_s',['year','quarter','nation','populationK']):
        if r['nation'] in NAT and int(fl(r['quarter']))%4==3 and int(fl(r['year'])) in (1850,1913,1950):
            acc[(r['nation'],int(fl(r['year'])))].append(fl(r['populationK']))
    return acc
pa=pop(A); pb=pop(B)
print('== 人口（千人・10種の中央値）A→B')
for n in NAT:
    print(n,' '.join(f"{y}:{st.median(pa[(n,y)]):.0f}→{st.median(pb[(n,y)]):.0f}" for y in (1850,1913,1950) if pa.get((n,y)) and pb.get((n,y))))
# prices: ratio by good
def prices(d):
    acc=collections.defaultdict(list)
    for s,r in load(d,'goods_s',['year','quarter','nation','goodName','priceMean','lastProducedQty']):
        if r['nation'] in NAT:
            acc[(r['goodName'],r['nation'])].append(fl(r['priceMean']))
    return acc
pra=prices(A); prb=prices(B)
goods=['小麦','衣料','家具','加工肉','干物','ビール','蒸留酒','チーズ','革製品','農業労働力','畜産','木材','綿花','塩','魚','毛皮','染料','麦']
names=sorted(set(k[0] for k in pra))
print('== 財の値段（国×四半期の平均の比 B/A・8国の中央値）')
for g in names:
    if g in ('小麦','衣料','家具','加工肉','干物','ビール','蒸留酒','チーズ','革製品','畜産','塩','綿花','木','製材','革','小麦粉','肉','綿布','染料','毛皮','魚','麦','農業労働力') or True:
        rs=[]
        for n in NAT:
            a=pra.get((g,n)); b=prb.get((g,n))
            if a and b:
                ma=st.mean([x for x in a if x==x] or [float('nan')]); mb=st.mean([x for x in b if x==x] or [float('nan')])
                if ma>0: rs.append(mb/ma)
        if rs and g in ('小麦','衣料','家具','加工肉','干物','ビール','蒸留酒','チーズ','革製品','畜産','塩','綿花','木材','製材','革','小麦粉','肉','綿布','染料','毛皮','漁獲','農業労働力','麦','木'):
            print(g,f'{st.median(rs):.3f}',f'(最小{min(rs):.2f}・最大{max(rs):.2f})')
print('goods:',names[:100])
