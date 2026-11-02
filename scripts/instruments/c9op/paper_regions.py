import glob,collections,sys
d=sys.argv[1]
c=collections.Counter(); tot=collections.Counter()
for f in sorted(glob.glob(d+'/habitcap_s*.tsv')):
    with open(f,encoding='utf-8') as fh:
        fh.readline()
        for l in fh:
            p=l.rstrip('\n').split('\t')
            if len(p)<15: continue
            new=float(p[12]); bef=float(p[9]); pc=(new-0.98*bef)/0.1
            key=p[3]+'/'+p[4]
            tot[key]+=1
            if 5*pc>=0.5: c[key]+=1
for k,v in c.most_common(25): print(k,v,tot[k])
