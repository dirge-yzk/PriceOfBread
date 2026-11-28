import sys
mc={}; 
for l in open(r'D:/PoB_work/keep_1425/base_ins/migcap_s1.tsv',encoding='utf-8'):
    v=l.split('\t')
    if len(v)>9 and v[3]=='西部': mc[v[0]]=(float(v[4]),float(v[5]),float(v[9]))
h=None
for l in open(r'D:/PoB_work/keep_1425/base_ins/region_s1.tsv',encoding='utf-8'):
    v=l.rstrip('\n').split('\t')
    if h is None: h={k:i for i,k in enumerate(v)}; continue
    if v[h['region']]=='西部' and int(v[0])%80==0:
        pop=float(v[h['populationK']]); f=float(v[h['foodFillShare']]); hf=float(v[h['housingFactor']]); lf=float(v[h['landFactor']])
        hous=1-(1-hf)/0.5
        cap=0.012/3*pop*(min(1,f)*min(1,max(0,hous))*lf)**2
        m=mc[v[0]]
        print(v[0], round(pop,1), 'food_rs',round(f,4),'food_mc',round(m[1],4),'cap_rs',round(cap,4),'cap_mc',round(m[2],4))
