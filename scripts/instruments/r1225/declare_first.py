# (1225) 読むだけ：統一の宣戦のうち、攻め手の前の年末の敗戦の衝撃が小さい（<0.05）＝輪の外から起きた宣戦の件数と、そのうち厭戦≦0.5 の件数（種ごと・近似）（種の範囲は引数）
import sys, csv, collections
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
tot = collections.Counter(); first = collections.Counter(); first_ok = collections.Counter(); yrs = []
for s in range(s0, s1 + 1):
    sc = {}
    with open(f"{d}/scalars_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            try: sc[(r["nation"], int(r["year"]))] = (float(r["defeatShock"]), float(r["warExhaustion"]))
            except ValueError: pass
    with open(f"{d}/wars_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["civilWarFlag"] != "0" or r["entryCode"] != "declare": continue
            tot[s] += 1; v = sc.get((r["sideA"], int(r["startYear"]) - 1))
            if v is None: continue
            if v[0] < 0.05:
                first[s] += 1; yrs.append(int(r["startYear"]))
                if v[1] <= 0.5: first_ok[s] += 1
print("per_seed_total", [tot[s] for s in range(s0, s1 + 1)])
print("per_seed_first", [first[s] for s in range(s0, s1 + 1)])
print("per_seed_first_and_rested", [first_ok[s] for s in range(s0, s1 + 1)])
yrs.sort(); print("first_years_min_med_max", yrs[0] if yrs else None, yrs[len(yrs)//2] if yrs else None, yrs[-1] if yrs else None)
