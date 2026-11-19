# (1238) 読むだけ：英（または指定の国）の戦争の件数を期間×入口×側で数える（内戦を除く）。引数：生 種0 種1 [国名]
import sys, csv, collections
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); nat = sys.argv[4] if len(sys.argv) > 4 else "イギリス"
per = [(1783,1850),(1850,1900),(1900,1914),(1914,1919),(1919,1951)]
tot = collections.Counter(); seeds_at_war = collections.Counter(); months = collections.Counter()
for s in range(s0, s1 + 1):
    hit = set()
    with open(f"{d}/wars_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["civilWarFlag"] != "0" or nat not in (r["sideA"], r["sideB"]): continue
            y = int(r["startYear"]); side = "A" if r["sideA"] == nat else "B"
            p = next(i for i,(a,b) in enumerate(per) if a <= y < b)
            tot[(p, side, r["entryCode"])] += 1; months[(p, side)] += float(r["months"])
            y1 = y + float(r["months"])/12
            for i,(a,b) in enumerate(per):
                if y < b and y1 >= a: hit.add(i)
    for i in hit: seeds_at_war[i] += 1
n = s1 - s0 + 1
for i,(a,b) in enumerate(per):
    a_ = {k[2]: v for k, v in tot.items() if k[0]==i and k[1]=="A"}; b_ = {k[2]: v for k, v in tot.items() if k[0]==i and k[1]=="B"}
    print(f"{a}-{b-1}\t種あたり 件 A={sum(a_.values())/n:.1f} B={sum(b_.values())/n:.1f}\t月 A={months[(i,'A')]/n:.0f} B={months[(i,'B')]/n:.0f}\t交戦した種={seeds_at_war[i]}/{n}\tA{dict(sorted(a_.items()))}\tB{dict(sorted(b_.items()))}")
