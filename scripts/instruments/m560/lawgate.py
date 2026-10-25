import csv,glob,os,sys,statistics as st
def rows(p):
    with open(p,encoding='utf-8',newline='') as f:
        r=csv.reader(f,delimiter='\t');h=next(r)
        for x in r:
            if len(x)==len(h): yield dict(zip(h,x))
def fl(v):
    try: return float(v)
    except: return float('nan')
Y=[1850,1880,1900,1913]
for d in sys.argv[1:]:
    print(d)
    acc={}
    for p in glob.glob(os.path.join(d,'lawgate_s*.tsv')):
        for x in rows(p):
            y=int(x['year'])
            if y in Y and int(x['quarter'])%4==3:
                k=(x['nation'],y); acc.setdefault(k,[]).append((fl(x['goldLawCode']),fl(x['goldWishNew']),fl(x['literacyHome']),fl(x['meanIndustrialization']),fl(x['urbanHome'])))
    for n in ['イギリス','フランス','プロイセン','アメリカ','日本','ロシア']:
        s=[]
        for y in Y:
            v=acc.get((n,y),[])
            if v: s.append(f'{y}: law {st.median([a[0] for a in v]):.0f} wish {st.median([a[1] for a in v]):.2f} lit {st.median([a[2] for a in v]):.2f} ind {st.median([a[3] for a in v]):.2f} urb {st.median([a[4] for a in v]):.2f}')
        print('  ',n,' | '.join(s))
