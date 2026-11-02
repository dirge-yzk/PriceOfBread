import sys,json
BOX=(59.0,61.5,21.0,31.0)
P={p['name']:(p['lat'],p['lon']) for p in json.load(open('C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore/places.json',encoding='utf-8'))}
def inbox(n):
    la,lo=P.get(n,(0,0)); return BOX[0]<=la<=BOX[1] and BOX[2]<=lo<=BOX[3]
def load(p):
    R={}
    for ln in open(p,encoding='utf-8'):
        c=ln.rstrip('\n').split('\t')
        if c[0]=='a': continue
        R[(c[0],c[1])]=c
    return R
A=load(sys.argv[1]);B=load(sys.argv[2])
assert A.keys()==B.keys()
cols={'land':[5],'sea_gc':[6,7,8],'sea_port':[11,12,13],'port':[2,3]}
res={k:[0,0] for k in cols}; samp={k:[] for k in cols}
for key in A:
    a=A[key];b=B[key]
    ib=inbox(key[0]) or inbox(key[1])
    for k,ix in cols.items():
        if any(a[i]!=b[i] for i in ix):
            res[k][0 if ib else 1]+=1
            if not ib and len(samp[k])<12: samp[k].append((key, [a[i] for i in ix],[b[i] for i in ix]))
tot=len(A); nin=sum(1 for k in A if inbox(k[0]) or inbox(k[1]))
print(f"pairs {tot} (box-touching {nin})")
for k in cols: print(f"  {k:9s} changed: box-touching {res[k][0]}  outside {res[k][1]}")
for k in cols:
    for s in samp[k][:12]: print('   out',k,s)
print('box places',sorted(n for n in P if inbox(n)))
