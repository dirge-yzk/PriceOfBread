# 国ごと: 満足度が最後まで 0.05 未満に留まる最初の四半期（satneeds_s）・NaN の行の数
import sys, math
raw = sys.argv[1]
for spec in sys.argv[2:]:
    s, n = spec.split(":")
    v = []
    with open(f"{raw}/satneeds_s{s}.tsv", encoding="utf-8") as f:
        f.readline()
        for l in f:
            c = l.rstrip("\n").split("\t")
            if c[2] == n: v.append((int(c[0]), int(c[1]), float(c[4]) if c[4] != "nan" else float("nan"), c))
    nan = sum(1 for x in v if not math.isfinite(x[2]))
    k = len(v)
    while k > 0 and v[k-1][2] < 0.05: k -= 1
    k2 = len(v)
    while k2 > 0 and v[k2-1][2] < 0.02: k2 -= 1
    print(s, n, "行", len(v), "NaN", nan, "最後まで<0.05 の最初", (v[k][0], v[k][1]) if k < len(v) else None, "最後まで<0.02", (v[k2][0], v[k2][1]) if k2 < len(v) else None)
