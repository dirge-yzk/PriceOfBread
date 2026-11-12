import csv,collections,sys
d=sys.argv[1]; tot=collections.Counter(); folded=0; mon=0
for s in range(1,11):
    last={}
    for r in csv.DictReader(open(f'{d}/coupgate_s{s}.tsv'),delimiter='\t'):
        rt=r['route']; q=int(r['quarter']); n=r['nation']
        if rt in('3','5'):
            tot[rt]+=1
            if last.get(n)!=q-1: folded+=1
            last[n]=q
print('route3',tot['3'],'route5',tot['5'],'folded',folded)
