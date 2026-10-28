import sys,collections
f=sys.argv[1]
prev=None
byyr=collections.defaultdict(lambda: collections.defaultdict(float))
big=[]
for line in open(f,encoding='utf-8'):
    c=line.rstrip('\n').split('\t')
    if len(c)<4: continue
    label=c[0]; tick=int(c[1]); s1=float(c[2])
    if prev is not None and tick>=prev[1]:
        d=s1-prev[2]
        key=(prev[0],label)
        if key in (('日次信用','月次経済'),('四半期末','外部層'),('月次犯罪','月次軍事')) and abs(d)>1e-12:
            yr=1783+tick//360
            byyr[key][yr//10*10]+=d
            if key[0]=='日次信用' and abs(d)>5: big.append((yr,round(d,1),round(s1,1)))
    prev=(label,tick,s1)
for k,v in byyr.items():
    print(k,{y:round(x,1) for y,x in sorted(v.items())})
print('big events (credit->monthly econ):',big[:30])
