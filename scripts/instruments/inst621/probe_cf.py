import csv,glob,statistics as st,collections,sys,re
sys.stdout.reconfigure(encoding="utf-8")
L=0.5
rows=["特許","株式会社","社会主義","共産主義","帝国主義","ファシズム"]
lit={}
for f in glob.glob("D:/PoB_work/keep_617/m617_ins/lawgate_s*.tsv"):
    s=int(re.search(r"s(\d+)\.tsv",f).group(1))
    for r in csv.DictReader(open(f,encoding="utf-8"),delimiter="\t"):
        lit[(s,r["nation"],int(r["quarter"]))]=float(r["literacyHome"])
tot=collections.Counter();dly=collections.Counter();ev=[]
for f in glob.glob("D:/PoB_work/keep_617/m617_ins/disc_s*.tsv"):
    s=int(re.search(r"s(\d+)\.tsv",f).group(1))
    for r in csv.DictReader(open(f,encoding="utf-8"),delimiter="\t"):
        if r["kind"]!="E" or r["row"] not in rows: continue
        q=int(r["quarter"]);n=r["nation"]
        l=lit.get((s,n,q))
        if l is None: continue
        t=70*min(1,l/L)+float(r["needTerm"])+float(r["transmitTerm"])
        tot[r["row"]]+=1
        if t<100: dly[r["row"]]+=1
        ev.append((s,n,r["row"],q,l,t))
for k in rows: print(k,tot[k],dly[k])
# majors
maj=["イギリス","フランス","プロイセン","オーストリア","ロシア","アメリカ","日本","清","オスマン"]
for k in rows:
    a=[(e[1],e[3]) for e in ev if e[2]==k and e[1] in maj]
    c=collections.Counter(n for n,q in a)
    d=collections.Counter(n for s,n,r,q,l,t in ev if r==k and n in maj and t<100)
    print(k,{n:(c[n],d[n]) for n in maj})
print("--- 1800以前の開き")
for k in rows:
    e0=[e for e in ev if e[2]==k and e[3]<=(1800-1783)*4]
    print(k,len(e0),sum(1 for e in e0 if e[4]<L), "反事実で閉じ",sum(1 for e in e0 if e[5]<100))
