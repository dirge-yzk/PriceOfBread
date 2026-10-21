import csv, sys, statistics as st, collections
D=sys.argv[1]; sys.stdout.reconfigure(encoding="utf-8")
N=["イギリス","フランス","プロイセン","ロシア","日本","清"]
eras=[(1783,1799),(1800,1849),(1850,1899),(1900,1949)]
acc=collections.defaultdict(list)
for s in range(1,11):
    per=collections.defaultdict(lambda:[0.0,0.0])
    with open(f"{D}/age_s{s}.tsv",encoding="utf-8") as f:
        for row in csv.DictReader(f,delimiter="\t"):
            if row["scope"]=="B" and row["bandIndex"]=="0":
                y=int(row["year"]); n=row["nation"]
                for i,(a,b) in enumerate(eras):
                    if a<=y<=b:
                        per[(n,i)][0]+=float(row["deathsK"] or 0); per[(n,i)][1]+=float(row["birthsK"] or 0)
    for k,(d,b) in per.items():
        if b>0: acc[k].append(1000*d/b)
for n in N:
    print(n, " ".join(f"{a}-{b}:{st.median(acc[(n,i)]):.0f}" for i,(a,b) in enumerate(eras) if acc[(n,i)]))
