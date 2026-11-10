import csv, collections
ins='D:/PoB_work/c94ro_work/ident/out_ib_10/ins'
def rd(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for row in r: yield dict(zip(h, row))
tot_mis=0; mis_new=0; ctl=0; ctl_new=0
for s in range(1,11):
    osum=collections.defaultdict(float)
    for r in rd(f'{ins}/recipeout_s{s}.tsv'): osum[(r['year'],r['nation'],r['good'])]+=float(r['outputSum'])
    mis=set(); allg=set()
    for r in rd(f'{ins}/nationgood_s{s}.tsv'):
        k=(r['year'],r['nation'],r['good']); w=float(r['producedQty']); g=osum.get(k,0.0)
        allg.add(k[:2])
        if abs(g-w)>1e-9*max(abs(w),1e-300)+1e-12: mis.add(k[:2])
    regs=collections.defaultdict(set); qy={}
    for r in rd(f'{ins}/region_s{s}.tsv'):
        q=int(r['quarter']); regs[(q,r['nation'])].add(r['region']); qy[q]=r['year']
    ymap={}
    for q,y in qy.items():
        if q%4==3: ymap[y]=q
    for (y,n) in allg:
        q=ymap.get(y)
        if q is None: continue
        cur=regs.get((q,n),set()); prev=regs.get((q-1,n),set())
        new = bool(cur-prev) or (len(prev)==0 and len(cur)>0)
        if (y,n) in mis: tot_mis+=1; mis_new+=new
        else: ctl+=1; ctl_new+=new
print('mismatch groups',tot_mis,'with a region new to the nation this quarter',mis_new)
print('control groups',ctl,'with new region',ctl_new)
