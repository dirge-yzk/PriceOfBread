# (1130) c110lo：railspend_s の列ごとの 10 種の和の 腕÷旗 0（同じ種の和どうし）と、pre の月数の比（記録・門ではない）。
import sys, os
def tot(d):
    t = {}
    for s in range(1, 11):
        with open(os.path.join(d, f"railspend_s{s}.tsv"), encoding="utf-8") as f:
            h = next(f).rstrip("\n").split("\t")
            for ln in f:
                c = ln.rstrip("\n").split("\t")
                for i in range(4, len(h)): t[h[i]] = t.get(h[i], 0.0) + float(c[i])
    return t
a, b = tot(sys.argv[1]), tot(sys.argv[2])
print("col\tarm\tbase\tarm/base")
for k in b:
    print(f"{k}\t{a[k]:.6g}\t{b[k]:.6g}\t{(a[k] / b[k] if b[k] else float('nan')):.4f}")
