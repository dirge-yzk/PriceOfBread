import re,csv,sys,collections,statistics as st
sys.stdout.reconfigure(encoding="utf-8")
I="D:/PoB_work/dx_488/out_dxB_10/ins"
ev=[]
for ln in open("D:/PoB_work/wt_dx/measurements/dx488_20261021/B_hol.txt",encoding="utf-8"):
    m=re.match(r"\s*\[(飢え|その他|移民|戦争)[^\]]*1/10\] s(\d+) (\S+?)（(\S+?)・(\w+)）\s+([\d.e+]+)\((\d+)\)→([\d.e+]+)\((\d+)\)",ln)
    if m: ev.append((m.group(1),int(m.group(2)),m.group(3),m.group(4),int(m.group(7)),int(m.group(9))))
bys=collections.defaultdict(list)
for e in ev: bys[e[1]].append(e)
res=[]
for s,es in bys.items():
    ser=collections.defaultdict(list)
    with open(f"{I}/region_s{s}.tsv",encoding="utf-8") as f:
        for r in csv.DictReader(f,delimiter="\t"):
            ser[(r["nation"],r["region"])].append((int(r["quarter"]),float(r["populationK"]),float(r["foodFillShare"]),float(r["earnedPerMonth"])))
    for e in es:
        l=sorted(ser.get((e[3],e[2]),[]))
        if len(l)<8: continue
        y0=(e[4]-1783)*4; y1=(e[5]-1783)*4
        w=[x for x in l if y0<=x[0]<=y1]
        if len(w)<8: continue
        k=max(2,len(w)//5)
        f0=sum(x[2] for x in w[:k])/k; f1=sum(x[2] for x in w[-k:])/k
        p0=sum(x[1] for x in w[:k])/k; p1=sum(x[1] for x in w[-k:])/k
        i0=sum(x[3]/x[1] for x in w[:k])/k; i1=sum(x[3]/x[1] for x in w[-k:])/k
        res.append((e[0],e[3],e[2],f0,f1,p1/p0,i0,i1))
print("events",len(res))
fs=[r for r in res if r[0]=="飢え"]
print("飢え",len(fs),"fill early median",round(st.median(r[3] for r in fs),3),"fill late median",round(st.median(r[4] for r in fs),3),"rose in",sum(1 for r in fs if r[4]>r[3]+0.02),"fell in",sum(1 for r in fs if r[4]<r[3]-0.02),"flat",sum(1 for r in fs if abs(r[4]-r[3])<=0.02))
print("earned per capita early median",round(st.median(r[6] for r in fs),3),"late",round(st.median(r[7] for r in fs),3))
for r in fs[:8]: print(r[1],r[2],"fill",round(r[3],2),"->",round(r[4],2),"pop ratio late/early",round(r[5],2))
