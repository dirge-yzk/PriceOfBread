# (1347) 追い：賃金÷主食の値段 z の開幕（四半期 0〜3）と 1913 の比（露・清・全て）と、v_f÷w の中の ρTf̄ の動き。読むだけ。
# 使い方：python -I paper1347b.py <base_ins> <出力>
import sys, statistics
BASE, OUT = sys.argv[1], sys.argv[2]
def rows(path, need):
    with open(path, encoding="utf-8") as fh:
        h = fh.readline().rstrip("\n").split("\t"); ix = [h.index(c) for c in need]
        for line in fh:
            v = line.rstrip("\n").split("\t"); yield [v[i] for i in ix]
def fl(s):
    try: return float(s)
    except ValueError: return float("nan")
res = {}
for sd in range(1, 11):
    wb = {}
    for q, yr, nat, sold, bill in rows(f"{BASE}/labor_s{sd}.tsv", ["quarter", "year", "nation", "farmSoldPerMonth", "farmWageBillPerMonth"]):
        per = "開幕" if int(q) <= 3 else ("1913" if yr == "1913" else None)
        if per is None: continue
        for sc in ("全て", nat):
            a = wb.setdefault((sc, per), [0.0, 0.0]); s, b = fl(sold), fl(bill)
            if s > 0 and b == b: a[0] += b; a[1] += s
    pp = {}
    for q, yr, nat, kind, sold, p, rho, T, f in rows(f"{BASE}/subst_s{sd}.tsv", ["quarter", "year", "nation", "s2BasisKind", "s2SoldEnd", "stapleNeedWeightedPrice", "s2BasisEngine", "s2TechEnd", "s3HarvestInst"]):
        per = "開幕" if int(q) <= 3 else ("1913" if yr == "1913" else None)
        if per is None or kind != "2": continue
        s, p, rho, T, f = fl(sold), fl(p), fl(rho), fl(T), fl(f)
        if not (s > 0) or not (p == p and rho == rho and T == T and f == f): continue
        for sc in ("全て", nat):
            a = pp.setdefault((sc, per), [0.0, 0.0, 0.0]); a[0] += s * p; a[1] += s; a[2] += s * rho * T * f
    for key in pp:
        if key not in wb or not (wb[key][1] > 0): continue
        w = wb[key][0] / wb[key][1]; p = pp[key][0] / pp[key][1]; rtf = pp[key][2] / pp[key][1]
        res.setdefault(key, []).append((w / p, rtf, rtf * p / w))
out = ["# 国×期：z＝賃金÷主食の値段（売れた加重）・ρTf̄（売れた加重）・v_f÷w＝ρTf̄÷z（国の集計）・種の中央〔最小〜最大〕"]
for sc in ("全て", "ロシア", "清", "イギリス", "フランス"):
    for per in ("開幕", "1913"):
        v = res.get((sc, per))
        if not v: continue
        cols = []
        for i, nm in enumerate(("z", "ρTf̄", "v_f÷w")):
            xs = [t[i] for t in v]; cols.append(f"{nm} {statistics.median(xs):.3f}〔{min(xs):.3f}〜{max(xs):.3f}〕")
        out.append(f"{sc}\t{per}\t" + "\t".join(cols))
with open(OUT, "wb") as fh: fh.write(("\n".join(out) + "\n").encode("utf-8"))
