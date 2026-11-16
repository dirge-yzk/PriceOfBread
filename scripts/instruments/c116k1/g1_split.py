# (1173) G1 の分け（読むだけ・記録）：植民地格（中核＞0）の余り＜0 を holdShare＜0.01（保持が潰れた行）と ≧0.01 に分け、国ごとの上位も出す。
import sys, os, csv
from collections import Counter
RAW, S0, S1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
lo = hi = 0; nat = Counter(); press = 0
for s in range(S0, S1 + 1):
    with open(os.path.join(RAW, f"secession_s{s}.tsv"), encoding="utf-8") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            if x[ix["tier"]] != "Colonial" or not (float(x[ix["coreShareMeanShare"]]) > 1e-9): continue
            p = float(x[ix["pressureIndex"]]); hs = float(x[ix["holdShareIndex"]])
            if hs - p < 0:
                if hs < 0.01: lo += 1
                else: hi += 1
                nat[x[ix["nation"]]] += 1
print(f"余り<0：holdShare<0.01 {lo}・≧0.01 {hi}・国の上位 " + "・".join(f"{k} {v}" for k, v in nat.most_common(6)))
