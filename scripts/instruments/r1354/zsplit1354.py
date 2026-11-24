# (1354) z＝w÷p̄ の帰属の続き：mc÷p̄ を 物価の水準（nation_s の priceLevel）÷p̄ と mc÷物価 に分ける。読むだけ。
# 使い方：python -I zsplit1354.py <base_ins> <出力>
import sys, statistics
BASE, OUT = sys.argv[1], sys.argv[2]
NATS = ("ロシア", "清", "イギリス")
def rows(path, need):
    with open(path, encoding="utf-8") as fh:
        h = fh.readline().rstrip("\n").split("\t"); ix = [h.index(c) for c in need]
        for line in fh:
            v = line.rstrip("\n").split("\t"); yield [v[i] for i in ix]
def fl(s):
    try: return float(s)
    except ValueError: return float("nan")
def per(q, y): return "開幕" if int(q) <= 3 else ("1913" if y == "1913" else None)
res = {}
for sd in range(1, 11):
    pl = {}
    for q, y, nat, p in rows(f"{BASE}/nation_s{sd}.tsv", ["quarter", "year", "nation", "priceLevel"]):
        pe = per(q, y)
        if nat in NATS and pe and fl(p) > 0: pl.setdefault((nat, pe), []).append(fl(p))
    lab = {}
    for q, y, nat, sold, bill, pmc in rows(f"{BASE}/labor_s{sd}.tsv", ["quarter", "year", "nation", "farmSoldPerMonth", "farmWageBillPerMonth", "farmPriceOverMcIndex"]):
        pe = per(q, y)
        if nat in NATS and pe and fl(sold) > 0 and fl(pmc) > 0:
            a = lab.setdefault((nat, pe), [0.0, 0.0, 0.0]); a[0] += fl(bill); a[1] += fl(sold); a[2] += fl(sold) * fl(bill) / fl(sold) / fl(pmc)
    pb = {}
    for q, y, nat, kind, sold, p in rows(f"{BASE}/subst_s{sd}.tsv", ["quarter", "year", "nation", "s2BasisKind", "s2SoldEnd", "stapleNeedWeightedPrice"]):
        pe = per(q, y)
        if nat in NATS and pe and kind == "2" and fl(sold) > 0 and fl(p) > 0:
            a = pb.setdefault((nat, pe), [0.0, 0.0]); a[0] += fl(sold) * fl(p); a[1] += fl(sold)
    for k in pl:
        if k not in lab or k not in pb: continue
        P = statistics.mean(pl[k]); w = lab[k][0] / lab[k][1]; mc = lab[k][2] / lab[k][1]; p = pb[k][0] / pb[k][1]
        for nm, v in (("物価の水準 P", P), ("主食の値段 p̄", p), ("賃金 w", w), ("mc", mc), ("p̄÷P", p / P), ("mc÷P", mc / P), ("w÷P", w / P)):
            res.setdefault((k[0], nm, k[1]), []).append(v)
out = ["# 国×量：開幕（四半期 0〜3）と 1913 の種の中央・比＝1913÷開幕（種ごとの比の中央）"]
for nat in NATS:
    for nm in ("物価の水準 P", "主食の値段 p̄", "賃金 w", "mc", "p̄÷P", "mc÷P", "w÷P"):
        a, b = res.get((nat, nm, "開幕")), res.get((nat, nm, "1913"))
        if not a or not b: continue
        r = sorted(y / x for x, y in zip(a, b))
        out.append(f"{nat} {nm}: 開幕 {statistics.median(a):.4g}・1913 {statistics.median(b):.4g}・比 {statistics.median(r):.3g}〔{r[0]:.3g}〜{r[-1]:.3g}〕")
open(OUT, "wb").write(("\n".join(out) + "\n").encode("utf-8"))
print("書いた", len(out))
