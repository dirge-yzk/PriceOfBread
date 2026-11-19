# (1238) 読むだけ：主要国の戦争の件数（wars_s・内戦を除く・開始の年で期間分け）を種の中央で。引数：生 種0 種1
import sys, csv, collections, statistics
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
G = ["イギリス","フランス","ロシア","プロイセン","オーストリア","アメリカ","オスマン","清","日本"]
per = [(1783,1850),(1850,1900),(1900,1951)]
V = collections.defaultdict(list); W = collections.defaultdict(list)
for s in range(s0, s1 + 1):
    c = collections.Counter(); tot = collections.Counter()
    with open(f"{d}/wars_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["civilWarFlag"] != "0": continue
            y = int(r["startYear"]); p = next(i for i,(a,b) in enumerate(per) if a <= y < b); tot[p] += 1
            for n in (r["sideA"], r["sideB"]):
                if n in G: c[(n, p)] += 1
    for n in G:
        for p in range(3): V[(n, p)].append(c[(n, p)])
    for p in range(3): W[p].append(tot[p])
print("国\t" + "\t".join(f"{a}-{b-1}" for a, b in per))
for n in G: print(n + "\t" + "\t".join(f"{statistics.median(V[(n,p)]):.0f}" for p in range(3)))
print("世界\t" + "\t".join(f"{statistics.median(W[p]):.0f}" for p in range(3)))
