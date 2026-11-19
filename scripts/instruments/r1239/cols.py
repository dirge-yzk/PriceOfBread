# 生の表の頭の行から模様に合う列を書き出す（読むだけ）。使い方: cols.py <INSRAW> <種> <模様> <出力>
import sys, os, re
ins, s, pat, outp = sys.argv[1], sys.argv[2], re.compile(sys.argv[3], re.I), sys.argv[4]
out = []
for fn in sorted(os.listdir(ins)):
    if not fn.endswith(f"_s{s}.tsv"): continue
    with open(os.path.join(ins, fn), encoding="utf-8") as f: h = f.readline().rstrip("\n").split("\t")
    m = [c for c in h if pat.search(c)]
    if m: out.append(fn[:-len(f"_s{s}.tsv")] + ": " + " ".join(m))
open(outp, "w", encoding="utf-8", newline="\n").write("\n".join(out) + "\n")
