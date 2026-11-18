# (1225) 読むだけ：統一の宣戦（wars_s の entryCode=declare）の攻め手の厭戦を、宣戦の年の前の年末の scalars_s の行で読む（種の範囲は引数）
import sys, csv, collections, statistics
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
ex_a = []; ex_b = []; byatt = collections.Counter(); above = 0; n = 0
for s in range(s0, s1 + 1):
    sc = {}
    with open(f"{d}/scalars_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            sc[(r["nation"], int(r["year"]))] = float(r["warExhaustion"]) if r["warExhaustion"] not in ("", "nan") else float("nan")
    with open(f"{d}/wars_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["civilWarFlag"] != "0" or r["entryCode"] != "declare": continue
            y = int(r["startYear"]) - 1
            a = sc.get((r["sideA"], y)); b = sc.get((r["sideB"], y))
            byatt[r["sideA"]] += 1
            if a is None or a != a: continue
            n += 1; ex_a.append(a); above += a > 0.5
            if b is not None and b == b: ex_b.append(b)
q = lambda v, p: sorted(v)[int(p * (len(v) - 1))] if v else float("nan")
print(f"declare_with_row={n}\tattacker_ex_med={q(ex_a,.5):.3f}\tp25={q(ex_a,.25):.3f}\tp75={q(ex_a,.75):.3f}\tshare_gt_0.5={above/max(1,n):.3f}\ttarget_ex_med={q(ex_b,.5):.3f}")
print("attackers_top", byatt.most_common(10))
