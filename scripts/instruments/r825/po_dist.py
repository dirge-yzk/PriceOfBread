import sys,glob
vals=[];one=0
for f in sorted(glob.glob('D:/PoB_work/keep_818/base_ins/rule_s*.tsv')):
    seen=set()
    with open(f,'rb') as fh:
        h=fh.readline().decode().rstrip('\r\n').split('\t')
        iq,inn,ip=h.index('quarter'),h.index('nation'),h.index('publicOrderIndex')
        for line in fh:
            c=line.decode('utf-8').rstrip('\r\n').split('\t')
            k=(c[iq],c[inn])
            if k in seen: continue
            seen.add(k)
            try: v=float(c[ip])
            except: continue
            vals.append(v)
vals.sort();n=len(vals)
print('nation-quarters',n,'share==1',sum(1 for v in vals if v>=0.999999)/n)
for q in (0.1,0.25,0.5,0.75,0.9):print(q,round(vals[int(q*(n-1))],4))
