import csv, collections, statistics as st, glob
d='D:/PoB_work/keep_407/m407_ins/'
# opening rule (seed1 q0, home regions: all active regions of the nation)
agg=collections.defaultdict(lambda:[0,0,0,0,0,0,0])  # pop, R0, R1, R2, R3 weighted
with open(d+'rule_s1.tsv',encoding='utf-8') as fh:
    for r in csv.DictReader(fh,delimiter='\t'):
        if r['quarter']!='0': continue
        p=float(r['popK']); a=agg[r['nation']]
        a[0]+=p; a[1]+=p*float(r['ruleStateShare']); a[2]+=p*float(r['ruleLocalShare']); a[3]+=p*float(r['ruleForeignShare']); a[4]+=p*float(r['ruleUngovShare'])
        if r['tier']=='Colonial': a[5]+=p
        else: a[6]+=p
# fill 1783/1790 median over seeds (mean over months in year)
fill={}
for yr in (1783,1785,1790,1800):
    for f in glob.glob(d+'fiscal_s*.tsv'):
        b=collections.defaultdict(list)
        for r in csv.DictReader(open(f,encoding='utf-8'),delimiter='\t'):
            if int(r['year'])==yr: b[r['nation']].append(float(r['adminFillShare']))
        for n,v in b.items(): fill.setdefault((n,yr),[]).append(sum(v)/len(v))
print('nation  popK  R0  R1  R2  R3 | old=R0+.7R1 | fallback(fill83) | literal(fill83) | fallback(fill90) literal(fill90)  | lawreach old,fill83,fill90')
for n,a in agg.items():
    if (n,1783) not in fill: continue
    P=a[0]; R0,R1,R2,R3=[x/P for x in a[1:5]]
    out=[]
    old=R0+0.7*R1
    f83=st.median(fill[(n,1783)]); f90=st.median(fill[(n,1790)]); f00=st.median(fill[(n,1800)])
    fb=lambda f: R0*(0.7+0.3*f)+0.7*R1
    lit=lambda f: R0*f+0.7*R1
    print('%-8s %8.0f R=(%.3f %.3f %.3f %.3f) col=%.2f | old %.3f | fb83 %.3f (x%.2f) lit83 %.3f (x%.2f) | fb90 %.3f (x%.2f) lit90 %.3f (x%.2f) | fb00 x%.2f lit00 x%.2f | law %.3f -> %.3f / %.3f (f83=%.3f f90=%.3f f00=%.3f)'%(n,P,R0,R1,R2,R3,a[5]/P,old,fb(f83),fb(f83)/old,lit(f83),lit(f83)/old,fb(f90),fb(f90)/old,lit(f90),lit(f90)/old,fb(f00)/old,lit(f00)/old,R0,R0*f83,R0*f90,f83,f90,f00))
