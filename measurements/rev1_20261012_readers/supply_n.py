"""(270) 線B2：補給崩壊（supplyFillShare<0.3）の国×四半期。python supply.py <計器の出力ディレクトリ>。人口100以上の種ごとの数・千件級の種・合計・同じ国の連続の最長。"""
import csv,collections,sys
D=sys.argv[1]
mx=0;res={}
tot_all=0
longrun=0
for s in range(1,int(sys.argv[2])+1):
    cnt=0;cnt_all=0
    run=collections.defaultdict(int); best=0
    with open(f'{D}/nation_s{s}.tsv',encoding='utf-8',errors='replace') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            try: fill=float(r['supplyFillShare']); pop=float(r['populationK'])
            except: continue
            col=fill<0.3
            if col: cnt_all+=1
            k=r['nationIndex']
            if col and pop>=100:
                cnt+=1; run[k]+=1; best=max(best,run[k])
            else: run[k]=0
    res[s]=(cnt,cnt_all,best)
print({s:v for s,v in res.items()})
print('seeds with >=1000 (pop>=100):',sum(1 for v in res.values() if v[0]>=1000),'max per seed',max(v[0] for v in res.values()),'total',sum(v[0] for v in res.values()),'all-pop total',sum(v[1] for v in res.values()),'longest run',max(v[2] for v in res.values()))
