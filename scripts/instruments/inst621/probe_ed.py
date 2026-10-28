import csv,glob,re,collections,sys
sys.stdout.reconfigure(encoding="utf-8")
q=collections.defaultdict(dict)
for f in glob.glob("D:/PoB_work/keep_617/m617_ins/disc_s*.tsv"):
    s=int(re.search(r"s(\d+)\.tsv",f).group(1))
    for r in csv.DictReader(open(f,encoding="utf-8"),delimiter="\t"):
        if r["kind"]=="E" and r["row"] in("社会主義","共産主義"): q[(s,r["nation"])][r["row"]]=int(r["quarter"])
both=[v for v in q.values() if len(v)==2]
print(len(q),len(both),sum(1 for v in both if v["共産主義"]<v["社会主義"]),sum(1 for v in both if v["共産主義"]==v["社会主義"]),sum(1 for v in both if v["共産主義"]>v["社会主義"]))
print("only comm",sum(1 for v in q.values() if "共産主義" in v and "社会主義" not in v),"only soc",sum(1 for v in q.values() if "社会主義" in v and "共産主義" not in v))
