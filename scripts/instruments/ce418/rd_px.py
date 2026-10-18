import csv,glob,os,re,sys,collections,statistics as st
csv.field_size_limit(1<<30)
def fl(x):
    try: return float(x)
    except: return float('nan')
A=sys.argv[1]; B=sys.argv[2]
G=['衣料','家具','加工肉','干物','ビール','蒸留酒','チーズ','革製品','小麦','パン','米','ジャガイモ','住居','医薬品','石鹸','紙']
bands=[(0,20),(20,40),(40,80),(80,160),(160,320),(320,668)]
def load(d):
    acc=collections.defaultdict(list)  # (good,band)->list of log price
    sold=collections.defaultdict(float); dem=collections.defaultdict(float)
    import math
    for f in sorted(glob.glob(os.path.join(d,'goods_s[0-9]*.tsv'))):
        with open(f,encoding='utf-8',newline='') as fh:
            for r in csv.DictReader(fh,delimiter='\t'):
                g=r['goodName']
                if g not in G: continue
                q=int(fl(r['quarter'])); p=fl(r['priceMean'])
                for i,(a,b) in enumerate(bands):
                    if a<=q<b:
                        if p==p and p>0: acc[(g,i)].append(math.log(p))
                        sold[(g,i)]+=fl(r['lastSoldQty']) if fl(r['lastSoldQty'])==fl(r['lastSoldQty']) else 0
                        dem[(g,i)]+=fl(r['lastDemandQty']) if fl(r['lastDemandQty'])==fl(r['lastDemandQty']) else 0
    return acc,sold,dem
import math
a,sa,da=load(A); b,sb,db=load(B)
print('財の値段の幾何平均の比 B/A（全国×全種・四半期の帯）／販売÷需要 A→B')
print('帯 '+' '.join(f'{x}-{y}' for x,y in bands))
for g in G:
    row=[]
    for i in range(len(bands)):
        x=a.get((g,i)); y=b.get((g,i))
        if x and y:
            row.append(f'{math.exp(st.mean(y)-st.mean(x)):.2f}[{sa[(g,i)]/max(da[(g,i)],1e-9):.2f}→{sb[(g,i)]/max(db[(g,i)],1e-9):.2f}]')
        else: row.append('-')
    print(g,' '.join(row))
