# (1238) 読むだけ：種ごとの英の表（握る人口 1900・1914〔region_s〕・1900-50 の組の数と開戦〔warpair_s〕・1914-18 の兵÷人口の平均〔nation_s〕・1914-18 の兵月〔casualty_s〕）。引数：生 種0 種1 札
import sys, csv, collections
d, s0, s1, tag = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]; nat = "イギリス"
for s in range(s0, s1 + 1):
    tot = collections.Counter()
    with open(f"{d}/region_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["nation"] == nat and int(r["quarter"]) % 4 == 0 and r["year"] in ("1850","1900","1914"): tot[r["year"]] += float(r["populationK"])
    pairs = set(); starts = 0.0
    with open(f"{d}/warpair_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if nat in (r["nationA"], r["nationB"]) and 1900 <= 1783 + int(r["quarter"]) // 4 <= 1950:
                pairs.add((r["nationA"], r["nationB"])); starts += float(r["starts"])
    rat = []
    with open(f"{d}/nation_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["nation"] == nat and 1914 <= int(r["year"]) <= 1918 and float(r["populationK"]) > 0: rat.append(float(r["soldierK"]) / float(r["populationK"]))
    sm = 0.0
    with open(f"{d}/casualty_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["nation"] == nat and 1914 <= int(r["year"]) <= 1918: sm += float(r["soldierMonthsK"])
    h3 = 100 * sum(rat) / len(rat) if rat else float('nan')
    print(f"{tag}\t{s}\t{tot['1850']/1000:.1f}\t{tot['1900']/1000:.1f}\t{tot['1914']/1000:.1f}\t{len(pairs)}\t{starts:.0f}\t{h3:.2f}\t{sm:.0f}")
