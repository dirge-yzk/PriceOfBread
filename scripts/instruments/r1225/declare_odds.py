# (1225) 読むだけ：統一の宣戦の攻め手÷標的の兵士の数（nation_s の soldierK・宣戦の四半期 q の行＝軍事力 Military の近似）（種の範囲は引数）
import sys, csv, collections
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
ratios = []; ge12 = 0; n = 0
for s in range(s0, s1 + 1):
    sk = {}
    with open(f"{d}/nation_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            try: sk[(r["nation"], int(r["quarter"]))] = float(r["soldierK"])
            except ValueError: pass
    with open(f"{d}/wars_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["civilWarFlag"] != "0" or r["entryCode"] != "declare": continue
            q = int(r["q"]); a = sk.get((r["sideA"], q)); b = sk.get((r["sideB"], q))
            if a is None or b is None or not (b > 0): continue
            n += 1; ratios.append(a / b); ge12 += a >= 1.2 * b
ratios.sort(); p = lambda x: ratios[int(x * (len(ratios) - 1))] if ratios else float("nan")
print(f"n={n}\tratio_med={p(.5):.2f}\tp10={p(.1):.2f}\tp90={p(.9):.2f}\tshare_ge_1.2={ge12/max(1,n):.3f}")
