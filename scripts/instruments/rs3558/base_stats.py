import csv,glob,sys,statistics as st
from collections import Counter,defaultdict
d=sys.argv[1]
def cap(lv,full): return full if lv==2 else (full+(1-full)*0.5 if lv==1 else 1.0)
tot=Counter(); floor=Counter(); gainpos=Counter(); gains=defaultdict(list); rows_n=0
bynat=defaultdict(Counter)
for fp in glob.glob(d+'/rs2_s*.tsv'):
    for r in csv.DictReader(open(fp,encoding='utf-8'),delimiter='\t'):
        rows_n+=1
        s=int(r['serfLawCode']); l=int(r['landReformLawCode']); c=float(r['customaryRentShare']); now=float(r['effRentShareNow']); ratio=float(r['lordRentToIncomeShare'])
        used=float(r['oppSerfNewIndex'])
        key=('serf',s)
        tot[key]+=1
        if used<=1.0+1e-9: floor[key]+=1
        prev=max(s-1,0)
        pnow=min(c,cap(prev,0.5),cap(l,0.3))
        g=(pnow-now)/now*ratio if now>1e-9 else 0
        if g>1e-9: gainpos[key]+=1; gains[key].append(g)
        bynat[(r['nation'],s)]['n']+=1
        if g>1e-9: bynat[(r['nation'],s)]['gain']+=1
print('rows',rows_n)
for k in sorted(tot): print(k,'n',tot[k],'floor',floor[k],round(floor[k]/tot[k],3),'gain>0',gainpos[k],round(gainpos[k]/tot[k],3),'gain median',round(st.median(gains[k]),3) if gains[k] else None)
for k in sorted(bynat):
    if bynat[k]['n']>200: print(k,dict(bynat[k]))
