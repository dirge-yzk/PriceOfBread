# 使い方: python rows.py <nation_s.tsv> <国名> <q0> <q1> <列,列,...> <出力>
import csv,sys
p,nm,q0,q1,cols,out=sys.argv[1:7]; q0=int(q0);q1=int(q1); cols=cols.split(',')
with open(p,encoding='utf-8',newline='') as f, open(out,'w',encoding='utf-8') as o:
    r=csv.reader(f,delimiter='\t'); h=next(r); ix={c:i for i,c in enumerate(h)}
    o.write('\t'.join(['quarter','year']+cols)+'\n')
    for x in r:
        if x[ix['nation']]==nm and q0<=int(x[ix['quarter']])<=q1:
            o.write('\t'.join([x[0],x[1]]+[x[ix[c]] for c in cols])+'\n')
