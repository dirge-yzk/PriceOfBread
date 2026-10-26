import csv,statistics as st,collections,sys
d='D:/PoB_work/keep_560/m560_ins'
vals=collections.defaultdict(list); dec={}
for s in range(1,11):
    best={}
    with open(f'{d}/engineer_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            if r['kind']!='T' or r['nation']!='イギリス': continue
            row=r['row']; dc=int(r['decade']); y=int(r['year']); dec[row]=dc
            tgt=max(dc+5,1785)
            try: a=float(r['engineerPerMonth'])
            except: continue
            if a!=a: continue
            k=abs(y-tgt)
            if row not in best or k<best[row][0]: best[row]=(k,a)
    for row,(k,a) in best.items(): vals[row].append(a)
for row in sorted(vals,key=lambda r:dec[r]):
    print(f"{row}\t{dec[row]}\t{st.median(vals[row]):.3f}\t{min(vals[row]):.2f}\t{max(vals[row]):.2f}")
