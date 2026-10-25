import csv,glob,statistics as st
from collections import defaultdict
b='D:/PoB_work/keep_552/m552_ins'
def cap(lv,full): return full if lv==2 else (full+(1-full)*0.5 if lv==1 else 1.0)
ser=defaultdict(list)
for fp in glob.glob(b+'/rs2_s*.tsv'):
    s=int(fp.split('_s')[-1][:-4])
    for r in csv.DictReader(open(fp,encoding='utf-8'),delimiter='\t'):
        ser[(s,r['nation'])].append(r)
def slope(xs,ys):
    n=len(xs);mx=sum(xs)/n;my=sum(ys)/n;den=sum((x-mx)**2 for x in xs)
    return sum((x-mx)*(y-my) for x,y in zip(xs,ys))/den if den>0 else float('nan')
out={'serf':[], 'land':[]}
for (s,n),rows in ser.items():
    rows.sort(key=lambda r:int(r['quarter']))
    for ax in ('serf','land'):
        cur=[]
        def flush():
            global cur
            if len(cur)>=10 and int(cur[-1][0])-int(cur[0][0])>=40:
                out[ax].append((s,n,len(cur),slope([int(q)/4 for q,_,_ in cur],[o for _,_,o in cur]),slope([int(q)/4 for q,_,_ in cur],[g for _,g,_ in cur])))
            cur=[]
        for r in rows:
            sl=int(r['serfLawCode']);ll=int(r['landReformLawCode']);c=float(r['customaryRentShare']);now=float(r['effRentShareNow']);ratio=float(r['lordRentToIncomeShare'])
            if ax=='serf': pn=min(c,cap(max(sl-1,0),0.5),cap(ll,0.3))
            else: pn=min(c,cap(sl,0.5),cap(max(ll-1,0),0.3))
            g=(pn-now)/now*ratio if now>1e-9 else 0
            ok=g>1e-9
            q=int(r['quarter'])
            if ok and cur and (q-int(cur[-1][0])>4 or key!=(sl,ll)): flush()
            if ok:
                key=(sl,ll); cur.append((r['quarter'],g,float(r['oppOldIndex'])))
            else: flush()
        flush()
for ax in out:
    v=out[ax]
    print(ax,'series',len(v),'nations',sorted(set(x[1] for x in v)))
    if v: print(' old(W0) slope median',st.median(x[3] for x in v),' gain slope median',st.median(x[4] for x in v))
