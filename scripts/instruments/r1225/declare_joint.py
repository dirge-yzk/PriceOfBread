# (1225) 読むだけ：統一の宣戦のうち〈攻め手の厭戦（前の年末）≦0.5〉と〈兵士の比≧1.2〉を同時に満たす割合（静的な反実仮想＝軌道は変わる・近似）（種の範囲は引数）
import sys, csv, collections
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
n = w = m = both = 0; pairs_both = collections.Counter(); att_both = collections.Counter()
for s in range(s0, s1 + 1):
    sc = {}; sk = {}
    with open(f"{d}/scalars_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            try: sc[(r["nation"], int(r["year"]))] = float(r["warExhaustion"])
            except ValueError: pass
    with open(f"{d}/nation_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            try: sk[(r["nation"], int(r["quarter"]))] = float(r["soldierK"])
            except ValueError: pass
    with open(f"{d}/wars_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["civilWarFlag"] != "0" or r["entryCode"] != "declare": continue
            ex = sc.get((r["sideA"], int(r["startYear"]) - 1)); q = int(r["q"])
            a = sk.get((r["sideA"], q)); b = sk.get((r["sideB"], q))
            if ex is None or ex != ex or a is None or b is None or not (b > 0): continue
            n += 1; okw = ex <= 0.5; okm = a >= 1.2 * b
            w += okw; m += okm
            if okw and okm: both += 1; pairs_both[(s, r["sideA"], r["sideB"])] += 1; att_both[r["sideA"]] += 1
print(f"n={n}\tweary_ok={w}\tmil_ok={m}\tboth={both}\tboth_per_seed={both/(s1-s0+1):.1f}\tmax_pair={pairs_both.most_common(3)}")
print("attackers_both", att_both.most_common(8))
