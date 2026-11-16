# (1177) c116bm：越境の銀と輸入の額（nationgood_s・年末の 1 か月の清算の写し・10 年ごと＋1913）を基線・N1・N2 で並べ、幅（最大÷最小）を出す。
# 使い方：python -X utf8 -I trade1177.py <出力フォルダ> <ラベル=フォルダ>...
import sys, os, math, collections

out = sys.argv[1]; arms = [a.split("=", 1) for a in sys.argv[2:]]
YEARS = [1790, 1830, 1870, 1900, 1913, 1930]
def fnum(s):
    try:
        v = float(s); return v if math.isfinite(v) else None
    except ValueError:
        return None
res = {}
for lab, src in arms:
    agg = collections.defaultdict(float); rows = 0
    for seed in range(1, 11):
        p = os.path.join(src, f"nationgood_s{seed}.tsv")
        if not os.path.exists(p): continue
        with open(p, encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {n: i for i, n in enumerate(h)}
            for line in f:
                c = line.rstrip("\n").split("\t")
                if len(c) < len(h): continue
                y = int(c[ix["year"]])
                if y not in YEARS: continue
                rows += 1
                iv = fnum(c[ix["crossImportValue"]]) or 0.0
                agg[(y, "impV")] += iv
                if c[ix["good"]] == "銀":
                    agg[(y, "agImpQ")] += fnum(c[ix["crossImportQty"]]) or 0.0
                    agg[(y, "agImpV")] += iv
                    agg[(y, "agProd")] += fnum(c[ix["producedQty"]]) or 0.0
    res[lab] = (agg, rows)
L = ["# trade1177  " + "  ".join(f"{l}={s}(行 {res[l][1]})" for l, s in arms)]
for k, name in [("impV", "越境の輸入の額（全ての財）"), ("agImpQ", "越境の銀の輸入の量"), ("agImpV", "越境の銀の輸入の額"), ("agProd", "銀の産出（1 か月）")]:
    L.append(f"## {name}")
    for y in YEARS:
        vals = [res[l][0][(y, k)] for l, _ in arms]
        lo, hi = min(vals), max(vals)
        w = hi / lo if lo > 0 else float("inf")
        L.append(f"  {y}: " + "  ".join(f"{l} {v:.5g}" for (l, _), v in zip(arms, vals)) + f"  幅 {w:.4f}  閾値 1+2(幅-1)={1+2*(w-1):.4f}")
L.append("## 銀の輸入の額÷全ての財の輸入の額（基線）")
l0 = arms[0][0]
L.append("  " + "  ".join(f"{y} {res[l0][0][(y,'agImpV')]/res[l0][0][(y,'impV')]:.3g}" if res[l0][0][(y, 'impV')] > 0 else f"{y} nan" for y in YEARS))
L.append("## 越境の銀の輸入÷銀の産出（基線）")
L.append("  " + "  ".join(f"{y} {res[l0][0][(y,'agImpQ')]/res[l0][0][(y,'agProd')]:.3g}" if res[l0][0][(y, 'agProd')] > 0 else f"{y} nan" for y in YEARS))
with open(os.path.join(out, "trade1177.txt"), "w", encoding="utf-8", newline="\n") as f:
    f.write("\n".join(L) + "\n")
print("\n".join(L))
