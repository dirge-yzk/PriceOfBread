# (1238) 読むだけ：英の失った人口の経路＝内戦・分離の戦争（wars_s）と講和の割譲（cessp_s の loser）を期間ごとに。引数：生 種0 種1
import sys, csv, collections
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); nat = "イギリス"
per = [(1783,1820),(1820,1850),(1850,1900),(1900,1951)]
P = lambda y: next(i for i,(a,b) in enumerate(per) if a <= y < b)
civ = collections.Counter(); sec = collections.Counter(); ced = collections.Counter(); cedr = collections.Counter(); cedwin = collections.defaultdict(collections.Counter)
for s in range(s0, s1 + 1):
    with open(f"{d}/wars_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if nat not in (r["sideA"], r["sideB"]): continue
            p = P(int(r["startYear"]))
            if r["civilWarFlag"] != "0": civ[p] += 1
            if r["entryCode"] in ("secession","civil"): sec[(p, r["entryCode"])] += 1
    with open(f"{d}/cessp_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["loser"] != nat: continue
            p = P(int(r["year"])); ced[p] += 1; cedr[p] += int(float(r["loserRegionCount"] or 0)); cedwin[p][r["winner"]] += 1
n = s1 - s0 + 1
for i,(a,b) in enumerate(per):
    print(f"{a}-{b-1}\t内戦(英が側)={civ[i]/n:.1f}/種\t分離={sec[(i,'secession')]/n:.1f}\tcivil札={sec[(i,'civil')]/n:.1f}\t英が負けた講和={ced[i]/n:.1f}/種\t勝者上位={cedwin[i].most_common(5)}")
