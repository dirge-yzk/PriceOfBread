import sys,collections
f=sys.argv[1]
prev=None
delta=collections.defaultdict(float)
cnt=collections.Counter()
last_label_order=[]
for line in open(f,encoding='utf-8'):
    c=line.rstrip('\n').split('\t')
    if len(c)<4: continue
    label=c[0]; tick=int(c[1]); s1=float(c[2]); s2=float(c[3])
    if prev is not None and tick>=prev[1]:
        d1=s1-prev[2]; 
        if abs(d1)>1e-12:
            delta[(prev[0],label)]+=d1; cnt[(prev[0],label)]+=1
    prev=(label,tick,s1,s2)
tot=sum(delta.values())
print('total civilian size change over run',round(tot,1))
for k,v in sorted(delta.items(),key=lambda kv:kv[1])[:12]:
    print('%s -> %s : %+.1f (n=%d)'%(k[0],k[1],v,cnt[k]))
print('...')
for k,v in sorted(delta.items(),key=lambda kv:-kv[1])[:6]:
    print('%s -> %s : %+.1f (n=%d)'%(k[0],k[1],v,cnt[k]))
