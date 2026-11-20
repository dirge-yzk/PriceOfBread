# (1249) 読むだけ：英（と墺）の 1914-18 の兵÷人口の種ごとの平均%と、1914-18 に交戦した（nation_s の atWarMonths＞0 の四半期がある）種の数。
#  H3 の門そのものは magn.py（mil180 の種の中央の頂点）＝この表は〈交戦の種の数〉を並べる記録（H3 と読み替えない）。引数：生 種0 種1 札
import sys, csv, statistics
d, s0, s1, tag = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
for nat in ("イギリス", "オーストリア"):
    vals = []; war = []
    for s in range(s0, s1 + 1):
        rat = []; aw = 0
        with open(f"{d}/nation_s{s}.tsv", encoding="utf-8", newline="") as f:
            for r in csv.DictReader(f, delimiter="\t"):
                if r["nation"] != nat or not (1914 <= int(r["year"]) <= 1918): continue
                if float(r["populationK"]) > 0: rat.append(float(r["soldierK"]) / float(r["populationK"]))
                if float(r["atWarMonths"]) > 0: aw = 1
        vals.append(100 * sum(rat) / len(rat) if rat else float("nan")); war.append(aw)
    print(f"{tag}\t{nat}\t兵÷人口%(種ごと) " + ",".join(f"{v:.2f}" for v in vals) + f"\t交戦の種 {sum(war)}/{len(war)}（" + ",".join(str(s0 + i) for i, x in enumerate(war) if x) + "）")
