# (1043) 下賜の行（explainCode 6）の跳びを前の四半期の政体で分ける（立憲の中の正の力の読み）
import sys, glob, os, csv
from collections import Counter
csv.field_size_limit(1 << 30)
POL = ["絶対", "専制", "幕府", "立憲", "共和", "ブル独裁", "プロ独裁", "軍政", "無政府"]
cnt = Counter(); amt = Counter()
for p in sorted(glob.glob(os.path.join(sys.argv[1], "crownpc_s*.tsv"))):
    with open(p, encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["explainCode"] != "6" or r["firstRowFlag"] == "1": continue
            k = POL[int(r["polityPrevCode"])]
            cnt[k] += 1; amt[k] += max(0.0, float(r["pcNowShare"]) - float(r["pcPrevShare"]))
t = sum(amt.values())
for k in sorted(cnt, key=lambda k: -amt[k]):
    print(f"前の政体 {k}\t行 {cnt[k]}\t跳びの和 {amt[k]:.4g}\t{100*amt[k]/t:.1f}%")
