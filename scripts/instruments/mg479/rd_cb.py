# (479) 段0の読み：国をまたぐ移民（年あたり・人口比%）の国×期の種の中央値。使い方：python rd_cb.py <計器の出力フォルダ> 1,2,...,10
import csv, sys, statistics as st, collections
base = sys.argv[1]; seeds = [int(x) for x in sys.argv[2].split(',')]
want = ['イギリス','アメリカ','清','ロシア','フランス','プロイセン','オランダ','ポルトガル','アイルランド']
periods = {'P1':(0,67),'P2':(68,267),'P3':(268,467),'P4':(468,667)}
res = collections.defaultdict(lambda: collections.defaultdict(list))  # (nation,period)-> list of per-seed (outRate,inRate)
names=set()
for sd in seeds:
    acc = collections.defaultdict(lambda:[0.0,0.0,0.0])  # (nation,period)->[out,in,popsum]
    nq = collections.defaultdict(set)
    with open(f'{base}/region_s{sd}.tsv', encoding='utf-8') as f:
        rd = csv.DictReader(f, delimiter='\t')
        for r in rd:
            q = int(r['quarter']); n = r['nation']; names.add(n)
            for p,(a,b) in periods.items():
                if a<=q<=b:
                    k=(n,p); x=acc[k]
                    x[0]+=float(r['crossBorderOutKPerQuarter']); x[1]+=float(r['crossBorderInKPerQuarter']); x[2]+=float(r['populationK']); nq[k].add(q); break
    for k,x in acc.items():
        if x[2]>0: res[k][sd] = (x[0]/x[2]*4*100, x[1]/x[2]*4*100)   # annual % of population (sum out / sum pop over quarters = mean quarterly rate)
print('# annual pct of pop out/in, median over seeds')
for n in [x for x in sorted(names) if x in want]:
    row=[]
    for p in periods:
        v=res.get((n,p))
        if v: row.append(f"{p}: out {st.median([a for a,b in v.values()]):.3f} in {st.median([b for a,b in v.values()]):.3f}")
    if any(('out' in s) for s in row): print(n, ' | '.join(row))
