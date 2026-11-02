# (784) 読み手 r782 の台本（使い捨ての写し・元は Temp）・入力のパスは当時の固定の値（keep_778）
# 賠償の月の額 ÷ 1 回の講和の額の目安（0.2×前年の月の名目GDP）＝同じ月に何回分の講和を払ったか（推定）・英が敗者・1784〜1815
import sys, csv, os, collections, statistics as st
d=sys.argv[1]; s0=int(sys.argv[2]); nat='イギリス'
def F(x):
    try: return float(x)
    except: return 0.0
allw=multi=0.0; ev=mev=0; ov=mov=0; peaceyrs=[]
for s in range(s0,s0+10):
    gdp=collections.defaultdict(list)
    with open(os.path.join(d,f'nation_s{s}.tsv'),encoding='utf-8') as f:
        for line in f:
            x=line.split('\t')
            if len(x)>8 and x[3]==nat: gdp[int(x[1])].append(F(x[7]))
    with open(os.path.join(d,f'fiscal_s{s}.tsv'),encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r); ix={k:i for i,k in enumerate(h)}
        fr=[x for x in r if x[1]==nat]
    for k,x in enumerate(fr):
        y=int(x[2]); w=-F(x[ix['ledgerWarCostPerMonth']])
        if w>1 and 1784<=y<=1815:
            g=st.mean(gdp.get(y-1) or gdp[y])/12; single=0.2*g
            n=w/single if single>0 else 0
            allw+=w; ev+=1
            if n>1.5: multi+=w; mev+=1
            T=F(fr[k-1][ix['treasuryEndStock']]) if k>0 else 0
            if w>max(0,T) and T>=0: ov+=1; mov+= n>1.5
    c=collections.Counter(int(z.split('\t')[1]) for z in open(os.path.join(d,f'cessp_s{s}.tsv'),encoding='utf-8').read().splitlines()[1:] if z.split('\t')[3]==nat and 1784<=int(z.split('\t')[1])<=1815)
    peaceyrs+= list(c.values())
print(d, f'賠償の月 {ev}・うち推定 1.5 回分超 {mev}・額の割合 {multi/allw if allw else 0:.2f}・〈国庫を越えた〉事象 {ov}・うち複数 {mov}・負けた講和のある年の講和の数 中央値 {st.median(peaceyrs) if peaceyrs else 0}・最大 {max(peaceyrs) if peaceyrs else 0}・3 以上の年 {sum(1 for v in peaceyrs if v>=3)}')
