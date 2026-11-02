# (776) 腕 B と基線の nation_s から ロシア・スウェーデン・フィンランドの年末の行を 1 回の読みで比べる（種ごとに 1 ファイル 1 回）
import csv,sys,collections,statistics as st
sys.stdout.reconfigure(encoding='utf-8')
NAT=['ロシア','スウェーデン','フィンランド']; YEARS=[1800,1850,1880,1913,1930,1950]
def scan(root):
    out=collections.defaultdict(list)   # (nat,year)->[(pop,gdp,export,import)]
    cnt=collections.Counter()
    for s in range(1,11):
        with open(f'{root}/nation_s{s}.tsv',encoding='utf-8') as f:
            r=csv.reader(f,delimiter='\t'); h=next(r); i={n:k for k,n in enumerate(h)}
            ex=[n for n in h if n.lower().startswith('export')][:1]+[n for n in h if n.lower().startswith('import')][:1]
            cols=[i['totalPopulationK'],i['nominalGdpPerYear']]+[i[c] for c in ex]
            last={}
            for row in r:
                nat=row[i['nation']]
                if nat in NAT:
                    y=int(row[i['year']])
                    if y in YEARS: last[(nat,y)]=row
            for (nat,y),row in last.items():
                out[(nat,y)].append([float(row[c]) if row[c] not in ('','NaN') else float('nan') for c in cols])
    return out,ex
A,exA=scan('D:/PoB_work/c7hl_arm/out_c7hlB_10/ins'); B,exB=scan('D:/PoB_work/keep_773/base_ins')
print('cols: pop(K), nominalGdpPerYear,',exA)
for nat in NAT:
    for y in YEARS:
        a=A.get((nat,y),[]); b=B.get((nat,y),[])
        f=lambda v:[round(st.mean(x[k] for x in v),1) if v else None for k in range(4)]
        print(nat,y,'seeds arm/base',len(a),len(b),'arm',f(a),'base',f(b))
