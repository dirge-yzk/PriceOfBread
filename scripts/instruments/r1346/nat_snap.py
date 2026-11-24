# 同じ国の州を 2 つの四半期で並べる（人口・機会・雇用・工場の産出・食）
import sys
d,seed,nat,q1,q2=sys.argv[1],sys.argv[2],sys.argv[3],int(sys.argv[4]),int(sys.argv[5])
S={}
with open(f'{d}/region_s{seed}.tsv',encoding='utf-8') as f:
    h=f.readline().rstrip('\n').split('\t'); ix={c:i for i,c in enumerate(h)}
    for line in f:
        a=line.rstrip('\n').split('\t'); q=int(a[0])
        if q not in (q1,q2) or a[ix['nation']]!=nat: continue
        S.setdefault(a[ix['region']],{})[q]=a
v=lambda a,c: float(a[ix[c]])
print('region\ttier\tpop1\tpop2\topp1\topp2\temp1\temp2\tfact1\tfact2\tfood1\tfood2\tout2\tin2')
for r,t in sorted(S.items(),key=lambda kv:-v(kv[1].get(q1,kv[1].get(q2)),'populationK')):
    a=t.get(q1); b=t.get(q2)
    if a is None or b is None: continue
    print(f"{r}\t{a[ix['tier']]}\t{v(a,'populationK'):.0f}\t{v(b,'populationK'):.0f}\t{v(a,'opportunityIndex'):.2f}\t{v(b,'opportunityIndex'):.2f}\t{v(a,'employmentShare'):.2f}\t{v(b,'employmentShare'):.2f}\t{v(a,'factoryOutputRefValuePerMonth'):.0f}\t{v(b,'factoryOutputRefValuePerMonth'):.0f}\t{v(a,'foodFillShare'):.2f}\t{v(b,'foodFillShare'):.2f}\t{v(b,'migrationOutKPerQuarter'):.2f}\t{v(b,'migrationInKPerQuarter'):.2f}")
