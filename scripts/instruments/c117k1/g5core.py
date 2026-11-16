# (1179) 記録：G5 本国の中核の群（成人男子以上×中核≤0.5×Core）の行の数と余り<0 の国×年の内訳（読むだけ）
import sys, os, csv
from collections import Counter
def rd(p):
    with open(p, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r: yield ix, x
for arg in sys.argv[1:]:
    d, law, tag = arg.split("|")
    rows = Counter(); neg = Counter(); nrows = 0
    for s in range(1, 11):
        suf = {}
        for ix, x in rd(os.path.join(law, f"monlaw_s{s}.tsv")): suf[(int(x[ix["quarter"]]), x[ix["nation"]])] = int(float(x[ix["Suffrage"]]))
        for ix, x in rd(os.path.join(d, f"secession_s{s}.tsv")):
            if x[ix["tier"]] != "Core": continue
            k = float(x[ix["coreShareMeanShare"]])
            if not (k <= 0.5): continue
            q = int(x[ix["quarter"]]); n = x[ix["nation"]]
            if suf.get((q, n), -1) < 2: continue
            nrows += 1; rows[n] += 1
            if float(x[ix["holdShareIndex"]]) - float(x[ix["pressureIndex"]]) < 0: neg[(s, n, x[ix["year"]][:3] + "0s")] += 1
    print(tag, "群の行", nrows, "国", dict(rows.most_common(6)), "余り<0 の(種,国,年代)", dict(neg.most_common(8)))
