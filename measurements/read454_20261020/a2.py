import sys; sys.stdout.reconfigure(encoding="utf-8")
import csv,statistics as st,collections
D='D:/PoB_work/keep_451/m451_ins/'
ev=collections.defaultdict(list)
for s in range(1,11):
    with open(D+f'disc_s{s}.tsv',encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t');h=next(r)
        for x in r:
            if x[0]!='E': continue
            ev[x[4]].append((s,int(x[2]),x[3],float(x[5]),float(x[6]),float(x[7])))
order=[]
with open('C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore/Catalog.cs',encoding='utf-8') as f:
    import re
    for line in f:
        m=re.match(r'\s*new\("([^"]+)",',line)
        if m: order.append(m.group(1))
for row in order:
    L=ev.get(row,[])
    seeds=sorted(set(e[0] for e in L))
    first={}
    for e in L:
        if e[0] not in first or e[1]<first[e[0]][1]: first[e[0]]=e
    fy=sorted(v[1] for v in first.values())
    selfn=sum(1 for e in L if e[5]<1e-9 or e[3]+e[4]>=100)
    fn=collections.Counter(v[2] for v in first.values()).most_common(3)
    fneed=[round(v[4]) for v in first.values()]
    print(f"{row}\tseeds={len(seeds)}\tevents={len(L)}\tself={selfn}\tfirstYr={fy[:1]+[st.median(fy)] if fy else '-'}\tfirstBy={fn}\tfirstNeed={sorted(fneed)}")
