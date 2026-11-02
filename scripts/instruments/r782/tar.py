# (784) 読み手 r782 の台本（使い捨ての写し・元は Temp）・入力のパスは当時の固定の値（keep_778）
import sys, csv, os, statistics as st, collections
nat='イギリス'
def F(x):
    try: return float(x)
    except: return 0.0
for spec in sys.argv[1:]:
    name,d=spec.split(':'); byy=collections.defaultdict(list); corn=collections.defaultdict(list)
    for s in range(1,11):
        acc=collections.defaultdict(list)
        with open(os.path.join(d,f'fiscal_s{s}.tsv'),encoding='utf-8') as f:
            r=csv.reader(f,delimiter='\t'); h=next(r); ix={k:i for i,k in enumerate(h)}
            for x in r:
                if x[1]==nat: acc[int(x[2])].append(F(x[ix['ledgerTariffPerMonth']]))
        for y in (1784,1787,1790,1793,1796,1799,1802,1805): byy[y].append(st.mean(acc[y]) if acc[y] else 0)
        with open(os.path.join(d,f'monlaw_s{s}.tsv'),encoding='utf-8') as f:
            r=csv.reader(f,delimiter='\t'); h=next(r); ix={k:i for i,k in enumerate(h)}
            for x in r:
                if x[ix['nation']]==nat and x[ix['quarter']] and int(x[ix['year']]) in (1784,1790,1796,1802) and int(x[ix['quarter']])%4==0:
                    corn[int(x[ix['year']])].append(x[ix['CornLaws']])
    print(name,'関税の月の額（年の平均・種の中央値）', ' '.join(f'{y}:{st.median(v):.0f}' for y,v in byy.items()))
    print(name,'穀物法の段', {y:collections.Counter(v).most_common(3) for y,v in corn.items()})
