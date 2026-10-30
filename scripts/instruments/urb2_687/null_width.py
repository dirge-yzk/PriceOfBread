# (687) 線 urb2 段0：ヌル腕の幅（読むだけ）。基線 m685_ins と N1・N2 の、同じ種・同じ国・同じ十年（最初の四半期）の差の中央値の最大の絶対値。
# 使い方：python null_width.py <基線> <N1> <N2> <表名=labor> <列名=plainUnemploymentShare> [国=全部]
import sys, csv, statistics as st, collections
base, n1, n2 = [x.rstrip("/") for x in sys.argv[1:4]]
tab = sys.argv[4] if len(sys.argv) > 4 else "labor"; col = sys.argv[5] if len(sys.argv) > 5 else "plainUnemploymentShare"
def load(d, s):
    out = {}
    with open(f"{d}/{tab}_s{s}.tsv", encoding="utf-8") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); iq, iy, ina, ic = [h.index(x) for x in ("quarter", "year", "nation", col)]
        for row in r:
            if int(row[iq]) % 4 != 0 or int(row[iy]) % 10 != 3 and int(row[iy]) % 10 != 0: continue
            try: v = float(row[ic])
            except: continue
            if v == v: out[(int(row[iy]), row[ina])] = v
    return out
lvl = collections.defaultdict(list)
dif = {"n1": collections.defaultdict(list), "n2": collections.defaultdict(list)}
for s in range(1, 11):
    B = load(base, s)
    for k, v in B.items(): lvl[k[1]].append(v)
    for nm, d in (("n1", n1), ("n2", n2)):
        N = load(d, s)
        for k in B:
            if k in N: dif[nm][k[1]].append(N[k] - B[k])
print(f"## {tab}.{col}：基線の水準（国ごとの全十年×種の中央値）と、ヌル腕との差（国ごとの全十年×種の中央値の絶対値・90 分位の絶対値）")
mx = 0
for n in sorted(lvl):
    row = [f"{n}\tlevel_med {st.median(lvl[n]):.4f}"]
    for nm in ("n1", "n2"):
        d = dif[nm][n]
        if d:
            ad = sorted(abs(x) for x in d); row.append(f"{nm} |med| {abs(st.median(d)):.4f} p90|d| {ad[int(0.9*(len(ad)-1))]:.4f} max|d| {ad[-1]:.4f}"); mx = max(mx, ad[int(0.9*(len(ad)-1))])
    print("\t".join(row))
print(f"## 全国の p90|d| の最大 {mx:.4f}")
