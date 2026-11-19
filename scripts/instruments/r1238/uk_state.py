# (1238) 読むだけ：英の状態を年ごと（指定年の最初の四半期）に種の中央で。引数：生 種0 種1 [国名]
import sys, csv, statistics, collections
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); nat = sys.argv[4] if len(sys.argv) > 4 else "イギリス"
years = [1790,1820,1850,1870,1890,1900,1910,1914,1920,1930,1940]
V = collections.defaultdict(list); pol = collections.defaultdict(collections.Counter)
for s in range(s0, s1 + 1):
    seen = set()
    with open(f"{d}/nation_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            y = int(r["year"])
            if r["nation"] != nat or y not in years or y in seen: continue
            seen.add(y)
            for c in ("populationK","soldierK","realGdpPerYear","atWarMonths","sovereignDefaultCumulativeCount"):
                V[(y,c)].append(float(r[c]))
            pol[y][r["polityCode"]] += 1
    seen = set()
    with open(f"{d}/ideology_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            y = int(r["year"])
            if r["nation"] != nat or y not in years or y in seen: continue
            seen.add(y)
            for c in ("ideoNationalLiberalismShare","ideoNationalNationalismShare","ideoNationalImperialismShare","ideoNationalRepublicanismShare"):
                V[(y,c)].append(float(r[c]))
def m(y,c): x = V[(y,c)]; return f"{statistics.median(x):.3g}" if x else "-"
print("年\t人口K\t兵K\t実GDP\t交戦月\t不履行累計\t自由\t民族\t帝国\t共和\t政体(種数)")
for y in years:
    print(f"{y}\t{m(y,'populationK')}\t{m(y,'soldierK')}\t{m(y,'realGdpPerYear')}\t{m(y,'atWarMonths')}\t{m(y,'sovereignDefaultCumulativeCount')}\t{m(y,'ideoNationalLiberalismShare')}\t{m(y,'ideoNationalNationalismShare')}\t{m(y,'ideoNationalImperialismShare')}\t{m(y,'ideoNationalRepublicanismShare')}\t{dict(pol[y])}")
