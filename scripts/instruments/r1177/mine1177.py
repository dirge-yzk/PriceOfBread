# (1177) c116bm：誤りの門の幅の材料＝銀を産む州（prodAg＞0）の値段÷平価の中央（持ち主の本位ごと・時代ごと）と、銀の賦存の 6 州の雇用 0.3 未満の四半期の数。
# 使い方：python -X utf8 -I mine1177.py <出力フォルダ> <ラベル=フォルダ>...
import sys, os, math, collections, statistics
out = sys.argv[1]; arms = [a.split("=", 1) for a in sys.argv[2:]]
SILVER6 = {"オムスク", "プラハ", "ヌエバ・エスパーニャ銀地帯", "ペルー", "ラ・プラタ", "ドレスデン"}
def era(y): return "1783-1829" if y < 1830 else "1830-1869" if y < 1870 else "1870-1913" if y < 1914 else "1914-1950"
def fnum(s):
    try:
        v = float(s); return v if math.isfinite(v) else None
    except ValueError:
        return None
L = []
for lab, src in arms:
    pr = collections.defaultdict(list); low = collections.Counter(); seen = collections.Counter(); nanEmp = 0
    for seed in range(1, 11):
        p = os.path.join(src, f"mintreg_s{seed}.tsv")
        if not os.path.exists(p): continue
        with open(p, encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {n: i for i, n in enumerate(h)}
            for line in f:
                c = line.rstrip("\n").split("\t")
                if len(c) < len(h): continue
                y = int(c[ix["year"]]); law = int(c[ix["law"]])
                pa, par, pd = fnum(c[ix["priceAg"]]), fnum(c[ix["parityAg"]]), fnum(c[ix["prodAg"]])
                if pd is not None and pd > 0 and pa is not None and par:
                    pr[(era(y), law)].append(pa / par)
                if c[ix["region"]] in SILVER6:
                    e = fnum(c[ix["employed"]])
                    if e is None: nanEmp += 1; continue
                    seen[law] += 1
                    if e < 0.3: low[law] += 1
    L.append(f"# {lab} {src}")
    for e in ["1783-1829", "1830-1869", "1870-1913", "1914-1950"]:
        L.append(f"  {e}: 産む州の値段÷平価の中央 " + "  ".join(f"law{law} {statistics.median(pr[(e, law)]):.4f}(n {len(pr[(e, law)])})" for law in (0, 1, 2) if pr[(e, law)]))
    L.append("  6 州の雇用＜0.3 の四半期（持ち主の本位ごと・雇用が書かれた行だけ）：" + "  ".join(f"law{law} {low[law]}/{seen[law]}" for law in (0, 1, 2)) + f"  雇用 nan の行 {nanEmp}")
with open(os.path.join(out, "mine1177.txt"), "w", encoding="utf-8", newline="\n") as f:
    f.write("\n".join(L) + "\n")
print("\n".join(L))
