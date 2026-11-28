import sys,os,csv
csv.field_size_limit(1<<30)
d=sys.argv[1]; rows=0; fac=0; strong=0; strongRows=0; gate=0
for s in range(1,11):
    p=os.path.join(d,f"dcont_s{s}.tsv")
    with open(p,encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r); i=h.index('factionsCount'); j=h.index('facStrongCount'); g=h.index('facCohGateCount')
        for x in r:
            rows+=1; fac+=int(float(x[i])); v=int(float(x[j])); strong+=v; strongRows+= v>0; gate+=int(float(x[g]))>0
print(f"dcont 行 {rows} 勢力の数の平均 {fac/rows:.3f} 強い行(fStrong>0) {strongRows} 結束の門の行 {gate}")
