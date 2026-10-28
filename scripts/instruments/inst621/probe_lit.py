import csv,glob,statistics as st,collections,sys
sys.stdout.reconfigure(encoding="utf-8")
nat=["イギリス","フランス","プロイセン","オーストリア","ロシア","アメリカ","日本","清","オスマン"]
yrs=[1783,1800,1830,1860,1900,1930]
d=collections.defaultdict(list)
for f in glob.glob("D:/PoB_work/keep_617/m617_ins/lawgate_s*.tsv"):
    for r in csv.DictReader(open(f,encoding="utf-8"),delimiter="\t"):
        if r["nation"] in nat and int(r["year"]) in yrs and int(r["quarter"])==(int(r["year"])-1783)*4:
            d[(r["nation"],int(r["year"]))].append(float(r["literacyHome"]))
for n in nat:
    print(n,[round(st.median(d[(n,y)]),2) if d[(n,y)] else None for y in yrs])
