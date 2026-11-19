# (1238) 読むだけ：warpair_s の英を含む組の期間ごとの和（monthsGe140・starts・submits・gateOpenMonths・組の数）。引数：生 種0 種1
import sys, csv, collections
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); nat = "イギリス"
per = [(1783,1850),(1850,1900),(1900,1914),(1914,1919),(1919,1951)]
S = collections.defaultdict(collections.Counter); pairs = collections.defaultdict(set)
for s in range(s0, s1 + 1):
    with open(f"{d}/warpair_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if nat not in (r["nationA"], r["nationB"]): continue
            y = 1783 + int(r["quarter"]) // 4
            p = next((i for i,(a,b) in enumerate(per) if a <= y < b), None)
            if p is None: continue
            for c in ("monthsGe140","monthsGt100","gateOpenMonths","starts","submits"): S[p][c] += float(r[c])
            pairs[p].add((s, r["nationA"], r["nationB"]))
n = s1 - s0 + 1
for i,(a,b) in enumerate(per):
    print(f"{a}-{b-1}\t組={len(pairs[i])/n:.1f}/種\t" + "\t".join(f"{c}={v/n:.1f}" for c, v in S[i].items()))
