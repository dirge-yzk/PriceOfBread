# r788：計器の 9 国の月の賠償の支払い（fiscal_s の ledgerWarCostPerMonth の負）÷ 1 回分（0.2×前年の月の名目GDP）＝同じ月に何回分を払ったかの推定（全期間）
import sys, os, csv, collections, statistics as st
d=sys.argv[1]; s0=int(sys.argv[2])
def F(x):
    try: return float(x)
    except: return 0.0
allw=multi=0.0; ev=mev=0
for s in range(s0,s0+10):
    gdp=collections.defaultdict(list)
    for line in open(os.path.join(d,f'nation_s{s}.tsv'),encoding='utf-8').read().splitlines()[1:]:
        x=line.split('\t'); gdp[(x[3],int(x[1]))].append(F(x[7]))
    r=csv.reader(open(os.path.join(d,f'fiscal_s{s}.tsv'),encoding='utf-8'),delimiter='\t'); h=next(r); ix={k:i for i,k in enumerate(h)}
    for x in r:
        w=-F(x[ix['ledgerWarCostPerMonth']]); y=int(x[2]); g=gdp.get((x[1],y-1)) or gdp.get((x[1],y))
        if w>1 and g:
            single=0.2*st.mean(g)/12; n=w/single if single>0 else 0
            allw+=w; ev+=1
            if n>1.5: multi+=w; mev+=1
print(d, f'支払いの月 {ev}・推定 1.5 回分超 {mev}・額の割合 {multi/allw if allw else 0:.2f}')
