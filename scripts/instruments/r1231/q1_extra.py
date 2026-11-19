# 使い方: python -X utf8 -I q1_extra.py <腕の生> <基線の生> <種 a-b>
# (1231) 問い 1 の五：spend_s の extra の枠（130 建設の材料と工賃・131 線路と機関車・132 インフラ保守・133 送電網・134 再開の発注）を全ての階級の和で、窓の区切りごとに腕÷基線（計器 9 国・10 種の和）。資本家の行（階級 3）の稼ぎと支出と消費（支出−extra）も並べる。
import sys
def fl(x):
    try: return float(x)
    except: return float("nan")
def rd(raw, s0, s1):
    out = {}
    for s in range(s0, s1 + 1):
        with open(f"{raw}/spend_s{s}.tsv", encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            for l in f:
                c = l.rstrip("\n").split("\t")
                if c[ix["pickedFlag"]] != "1": continue
                b = int(c[0]); m = fl(c[ix["monthsCount"]]); pay = fl(c[ix["payPerMonth"]])
                if not (pay == pay): continue
                g = out.setdefault(b, {})
                if c[ix["kind"]] == "extra": g[c[ix["slotId"]]] = g.get(c[ix["slotId"]], 0.0) + pay * m
                if c[ix["kind"]] == "extra" and c[ix["classId"]] == "3": g["x3"] = g.get("x3", 0.0) + pay * m
                if c[ix["kind"]] == "class" and c[ix["classId"]] == "3":
                    g["P3"] = g.get("P3", 0.0) + pay * m; g["E3"] = g.get("E3", 0.0) + fl(c[ix["receiptPerMonth"]]) * m
    return out
s0, s1 = map(int, sys.argv[3].split("-"))
A = rd(sys.argv[1], s0, s1); B = rd(sys.argv[2], s0, s1)
yrs = {0: "1783", 1: "1784", 2: "1785-92", 3: "1793-1822", 4: "1823-29", 5: "1830-39", 6: "1840-49", 7: "1850-82", 8: "1883-1950"}
print("bucket\tyears\t130建設 a/b\t131線路 a/b\t132保守 a/b\t133送電 a/b\t134再開 a/b\t資本家の稼ぎ a/b\t資本家の消費 a/b\t保守÷建設 a|b\t送電÷建設 a|b")
for b in sorted(B):
    a = A.get(b, {}); bb = B[b]
    r = lambda k: a.get(k, 0.0) / bb[k] if bb.get(k) else float("nan")
    ca = a.get("P3", 0) - a.get("x3", 0); cb = bb.get("P3", 0) - bb.get("x3", 0)
    q = lambda d, k: d.get(k, 0.0) / d["130"] if d.get("130") else float("nan")
    print(f"{b}\t{yrs[b]}\t{r('130'):.3f}\t{r('131'):.3f}\t{r('132'):.3f}\t{r('133'):.3f}\t{r('134'):.3f}\t{r('E3'):.3f}\t{ca/cb:.3f}\t{q(a,'132'):.4f}|{q(bb,'132'):.4f}\t{q(a,'133'):.4f}|{q(bb,'133'):.4f}")
