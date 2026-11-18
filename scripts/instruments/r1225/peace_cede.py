# (1225) 読むだけ：cpeace_s の講和の件数・割譲の実の件数の和・負けた国の上位（種の範囲は引数）
import sys, csv, collections
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
n = 0; cede = 0; zero = 0; los = collections.Counter()
for s in range(s0, s1 + 1):
    with open(f"{d}/cpeace_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            n += 1; c = int(r["cedeRealCount"]); cede += c; zero += (c == 0); los[r["loser"]] += 1
print(f"peace={n}\tcedeReal_sum={cede}\tcede0={zero}\ttop_losers={los.most_common(6)}")
