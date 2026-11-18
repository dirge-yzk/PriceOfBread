# (1225) 読むだけ：輪の外（敗戦の衝撃<0.05）・休んだ（厭戦≦0.5）・兵士の比≧1.2 を同時に満たす統一の宣戦の種ごとの件数と、同じ組の最多（静的な近似）（種の範囲は引数）
import sys, csv, collections
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
res = []; pmax = []; att = collections.Counter()
for s in range(s0, s1 + 1):
    sc = {}; sk = {}; n = 0; pairs = collections.Counter()
    for r in csv.DictReader(open(f"{d}/scalars_s{s}.tsv", encoding="utf-8", newline=""), delimiter="\t"):
        try: sc[(r["nation"], int(r["year"]))] = (float(r["defeatShock"]), float(r["warExhaustion"]))
        except ValueError: pass
    for r in csv.DictReader(open(f"{d}/nation_s{s}.tsv", encoding="utf-8", newline=""), delimiter="\t"):
        try: sk[(r["nation"], int(r["quarter"]))] = float(r["soldierK"])
        except ValueError: pass
    for r in csv.DictReader(open(f"{d}/wars_s{s}.tsv", encoding="utf-8", newline=""), delimiter="\t"):
        if r["civilWarFlag"] != "0" or r["entryCode"] != "declare": continue
        v = sc.get((r["sideA"], int(r["startYear"]) - 1)); q = int(r["q"]); a = sk.get((r["sideA"], q)); b = sk.get((r["sideB"], q))
        if v is None or a is None or b is None or not (b > 0): continue
        if v[0] < 0.05 and v[1] <= 0.5 and a >= 1.2 * b: n += 1; pairs[(r["sideA"], r["sideB"])] += 1; att[r["sideA"]] += 1
    res.append(n); pmax.append(max(pairs.values()) if pairs else 0)
print("per_seed", res); print("pair_max", pmax); print("attackers", att.most_common(6))
