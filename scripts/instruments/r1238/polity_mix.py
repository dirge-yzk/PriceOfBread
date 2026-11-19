# (1238) 読むだけ：1900-1950 の四半期で、英の政体の割合と世界（人口 1 万以上の国）の政体の割合（nation_s の polityCode・0 絶対 1 専制 2 幕府 3 立憲 4 共和 5 ブルジョワ独裁 6 プロレタリア独裁 7 軍事独裁 8 無政府）。引数：生 種0 種1
import sys, csv, collections
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
uk = collections.Counter(); wd = collections.Counter()
for s in range(s0, s1 + 1):
    with open(f"{d}/nation_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if not (1900 <= int(r["year"]) <= 1950) or float(r["totalPopulationK"]) < 10: continue
            wd[r["polityCode"]] += 1
            if r["nation"] == "イギリス": uk[r["polityCode"]] += 1
f = lambda c: {k: round(v / sum(c.values()), 3) for k, v in sorted(c.items())}
print("英", f(uk)); print("世界", f(wd))
