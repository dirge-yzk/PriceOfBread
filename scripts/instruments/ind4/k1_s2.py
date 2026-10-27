import sys,csv,collections,os,math,statistics
d=sys.argv[1]; seeds=range(1,11)
print("dir",d)
def col(h,n): return h.index(n)
tot=collections.OrderedDict()
for s in seeds:
    p=f"{d}/region_s{s}.tsv"
    if not os.path.exists(p): print("missing",p); continue
    pop=collections.defaultdict(float); rur=collections.defaultdict(float)
    foodw=collections.defaultdict(float); foodp=collections.defaultdict(float)
    with open(p,encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r)
        iq,iy,inat,ipop,irur,ia,iff=[col(h,x) for x in ('quarter','year','nation','populationK','ruralPopulationK','activeFlag','foodFillShare')]
        for row in r:
            if row[ia]!='1': continue
            y=int(row[iy]); q=int(row[iq])
            if q%4!=0 or y%10!=0: continue
            k=(row[inat],y)
            pp=float(row[ipop]); pop[k]+=pp; rur[k]+=float(row[irur])
            try: foodw[k]+=pp*float(row[iff]); foodp[k]+=pp
            except: pass
    cells={k:1-rur[k]/pop[k] for k in pop if pop[k]>0}
    hi=sum(1 for u in cells.values() if u>=0.95); lo=sum(1 for u in cells.values() if u<=0.02)
    food=sum(1 for k in foodp if foodp[k]>0 and foodw[k]/foodp[k]<0.5)
    by=collections.defaultdict(list)
    for (n,y),u in cells.items(): by[y].append(u)
    med={y:round(statistics.median(by[y]),3) for y in (1790,1850,1900,1950) if y in by}
    print(f"s{s} cells {len(cells)} stuck_hi {hi} stuck_lo {lo} frac {(hi+lo)/max(1,len(cells)):.4f} max {max(cells.values()):.3f} min {min(cells.values()):.4f} food<0.5 {food} median {med}")
# price level
for s in seeds:
    p=f"{d}/money_s{s}.tsv"
    if not os.path.exists(p): continue
    mx=0;nan=0;mxn=None
    with open(p,encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r); ip=col(h,'priceLevelAfterIndex'); inn=col(h,'nation'); iy=col(h,'year'); il=col(h,'outstandingLoansStock'); ig=col(h,'gdpRealPerMonth')
        mxl=0
        for row in r:
            try: v=float(row[ip])
            except: nan+=1; continue
            if math.isnan(v) or math.isinf(v): nan+=1; continue
            if v>mx: mx=v; mxn=(row[inn],row[iy])
            try:
                l=float(row[il]); g=float(row[ig])
                if g>0: mxl=max(mxl,l/g)
            except: pass
    print(f"s{s} priceLevel max {mx:.4g} at {mxn} nan {nan} maxLoans/gdpRealMonth {mxl:.4g}")
