import sys, collections, glob, os
base = sys.argv[1]
# nation-quarter farmer sat
rows = []  # (seed, q, year, nation, popK, sat)
seen=set()
for s in range(1,11):
    f = os.path.join(base, f"svcbuy_s{s}.tsv")
    with open(f, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t')
        ix = {k:i for i,k in enumerate(h)}
        for line in fh:
            c = line.rstrip('\n').split('\t')
            if c[ix['classId']] != '1': continue
            key=(s,c[ix['quarter']],c[ix['nation']])
            if key in seen: continue
            seen.add(key)
            rows.append((s,int(c[ix['quarter']]),int(c[ix['year']]),c[ix['nation']],float(c[ix['popK']]),float(c[ix['satisfactionMean']])))
print("rows", len(rows))
TH=[0.02,0.1,0.3]
# overall by 25-year era: popK-weighted share of farmer pop with sat<th, and quarter-count share
eras=[(1783,1800),(1800,1825),(1825,1850),(1850,1875),(1875,1900),(1900,1925),(1925,1951)]
print("era: farmerPop-weighted share below TH (0.02,0.1,0.3); mean sat popweighted; nation-quarters")
for a,b in eras:
    sel=[r for r in rows if a<=r[2]<b]
    tot=sum(r[4] for r in sel)
    if tot==0: continue
    sh=[sum(r[4] for r in sel if r[5]<t)/tot for t in TH]
    ms=sum(r[4]*r[5] for r in sel)/tot
    print(f"{a}-{b}: "+" ".join(f"{x:.4f}" for x in sh)+f" mean={ms:.3f} n={len(sel)}")
# by nation: share of nation-quarters with sat<0.02, farmer pop mean
by=collections.defaultdict(list)
for r in rows: by[r[3]].append(r)
out=[]
for n,rs in by.items():
    tot=sum(r[4] for r in rs)
    f=[sum(r[4] for r in rs if r[5]<t)/tot for t in TH]
    out.append((f[0],n,f,len(rs),tot/len(rs)))
out.sort(reverse=True)
print("nation: share<0.02,<0.1,<0.3 (pop-weighted over all seeds/quarters); nQ; mean farmerK")
for f0,n,f,nq,mp in out[:25]:
    print(f"{n}: {f[0]:.3f} {f[1]:.3f} {f[2]:.3f} nQ={nq} meanK={mp:.0f}")
# sat==~0 runs: longest consecutive quarters below 0.02 per (seed,nation)
runs=[]
g=collections.defaultdict(list)
for r in rows: g[(r[0],r[3])].append(r)
for k,rs in g.items():
    rs.sort(key=lambda r:r[1])
    cur=0;best=0;startY=None;bestY=None
    for r in rs:
        if r[5]<0.02:
            if cur==0: startY=r[2]
            cur+=1
            if cur>best: best=cur;bestY=startY
        else: cur=0
    if best>=4: runs.append((best,k,bestY))
runs.sort(reverse=True)
print("runs >=4 quarters below 0.02:",len(runs))
for b,k,y in runs[:25]: print(b,k,y)
