# (1238) 読むだけ：英の 1914-18 の兵÷人口の頂点%（nation_s の四半期・mil180 ⑥ と同じ形〈推〉）と、その期に交戦の月がある四半期の数。引数：生 種0 種1 札
import sys, csv
d, s0, s1, tag = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
for s in range(s0, s1 + 1):
    pk = 0.0; aw = 0
    with open(f"{d}/nation_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["nation"] == "イギリス" and 1914 <= int(r["year"]) <= 1918 and float(r["totalPopulationK"]) > 0:
                pk = max(pk, 100 * float(r["soldierK"]) / float(r["totalPopulationK"])); aw += float(r["atWarMonths"]) > 0
    print(f"{tag}\t{s}\t{pk:.2f}\t{aw}")
