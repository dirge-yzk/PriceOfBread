import csv, collections, statistics as st, glob
d='D:/PoB_work/keep_407/m407_ins/'
tot=collections.defaultdict(float); bur={}
for r in csv.DictReader(open(d+'class_s1.tsv',encoding='utf-8'),delimiter='\t'):
    if r['time']!='open': continue
    n=r['nation']
    tot[n]+=float(r['sizeK'])
    if r['className']=='官僚': bur[n]=float(r['sizeK'])
fill={}
for f in glob.glob(d+'fiscal_s*.tsv'):
    b=collections.defaultdict(list)
    for r in csv.DictReader(open(f,encoding='utf-8'),delimiter='\t'):
        if int(r['year'])==1783: b[r['nation']].append(float(r['adminFillShare']))
    for n,v in b.items(): fill.setdefault(n,[]).append(sum(v)/len(v))
print('nation totalPopK burK bur/pop need(K units,0.04*pop) fill83 impliedUnitsPerBur')
for n in tot:
    if n not in fill or n not in bur: continue
    need=0.04*tot[n]; fl=st.median(fill[n])
    print('%-8s %9.0f %8.1f %.3f%% %9.0f %.3f %.2f'%(n,tot[n],bur[n],100*bur[n]/tot[n],need,fl,fl*need/bur[n]))
