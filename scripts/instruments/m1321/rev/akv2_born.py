import sys, csv, os, collections
raw=sys.argv[1]
tot=0; match=0; unmatched=[]; extra=0; capr=0; nzero=0
SB=int(sys.argv[2]) if len(sys.argv)>2 else 1  # (1321) 種の起点を引数に（S11 で読むため・合流が足した）
for s in range(SB,SB+10):
    st=list(csv.DictReader(open(os.path.join(raw,f'sectreas_s{s}.tsv'),encoding='utf-8'),delimiter='\t'))
    born=collections.defaultdict(list)
    with open(os.path.join(raw,f'treasledger_s{s}.tsv'),encoding='utf-8') as f:
        for row in csv.DictReader(f,delimiter='\t'):
            if row['item']=='_bornTreasury': born[row['quarter']].append(float(row['value']))
    nb=sum(len(v) for v in born.values())
    for r in st:
        tot+=1; a=float(r['actualT'])
        if r['capitalRisingFlag']=='1': capr+=1
        if a==0: nzero+=1
        L=born.get(r['quarter'],[])
        hit=[x for x in L if abs(x-a)<=1e-9*max(1,abs(a))]
        if hit: match+=1; L.remove(hit[0])
        else: unmatched.append((s,r['quarter'],r['parent'],a,L[:]))
    extra+=sum(len(v) for v in born.values())
print('rows',tot,'matched_born',match,'extra_born',extra,'capitalRising',capr,'actualT==0',nzero)
for u in unmatched[:10]: print('UNMATCHED',u)
