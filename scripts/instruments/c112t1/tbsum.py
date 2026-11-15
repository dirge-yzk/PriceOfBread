import sys,glob,collections
cols=None;tot=collections.Counter();byyear=collections.defaultdict(collections.Counter)
for f in sorted(glob.glob(sys.argv[1]+'/tb_s*.tsv')):
    with open(f,encoding='utf-8') as fh:
        h=fh.readline().rstrip('\n').split('\t')
        for line in fh:
            v=line.rstrip('\n').split('\t'); d=dict(zip(h,v))
            for k in ['rows','unclassified','sk0','sk1','sk2','ps0','ps1','ps2','a1Close','evalSkip','evalAll','evalDone','u1Deals','u1Rel','u1Abs','smallD','smallDSkip','u1SmallD','u1SmallDRel','u1Gt1pct','u1Gt10pct']:
                try: x=int(d[k])
                except: continue
                tot[k]+=x; byyear[int(d['year'])//10*10][k]+=x
for k in tot: print(k,tot[k])
for y in sorted(byyear):
    b=byyear[y]; print(y,'rows',b['rows'],'sk1',b['sk1'],'a1Close',b['a1Close'],'evalSkip',b['evalSkip'],'u1Deals',b['u1Deals'],'u1Rel',b['u1Rel'])
