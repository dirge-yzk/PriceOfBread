# (1041) c98bp 読むだけ：植民地格の州の〈労働の主の生産者〉の国内の半分の損益（LastRevenue−LastCost＝売れた分の国内収入−全ての産出の投入）
# と、その POP の輸出の受け取り（inExportPerQuarter）を基線 keep_1035/base_ins の cpop_s・cearn_s で並べる。
# 使い方: python halfledger.py <base_ins> <出力 tsv>
import sys, os, collections, statistics

src, out = sys.argv[1], sys.argv[2]
YEARS = {1790, 1870, 1913}

def rows(path):
    with open(path, "rb") as f:
        head = None
        for raw in f:
            line = raw.decode("utf-8").rstrip("\r\n")
            if line.startswith("#") or not line:
                continue
            c = line.split("\t")
            if head is None:
                head = {k: i for i, k in enumerate(c)}
                continue
            yield head, c

def fl(x):
    try:
        return float(x)
    except ValueError:
        return float("nan")

res = collections.defaultdict(lambda: collections.defaultdict(list))
for s in range(1, 11):
    tier = {}
    for h, c in rows(os.path.join(src, f"cearn_s{s}.tsv")):
        y = int(c[h["year"]])
        if y in YEARS:
            tier[(c[h["quarter"]], c[h["nation"]], c[h["region"]])] = c[h["tier"]]
    for h, c in rows(os.path.join(src, f"cpop_s{s}.tsv")):
        y = int(c[h["year"]])
        if y not in YEARS:
            continue
        t = tier.get((c[h["quarter"]], c[h["nation"]], c[h["region"]]), "?")
        g = c[h["topLaborGood"]]
        if g in ("-", ""):
            continue
        rev = fl(c[h["topLaborRevenuePerMonth"]]); cost = fl(c[h["topLaborCostPerMonth"]])
        price = fl(c[h["topLaborPrice"]]); mc = fl(c[h["topLaborMarginalCost"]])
        exp = fl(c[h["inExportPerQuarter"]])
        k = (y, t, c[h["className"]])
        res[k]["neg"].append(1.0 if rev < cost else 0.0)
        res[k]["gap"].append(rev - cost)
        res[k]["exp"].append(exp / 3.0)
        res[k]["pm"].append(price / mc if mc > 0 else float("nan"))

lines = ["#(1041) 植民地格と他の州の労働の主の生産者の国内の半分の損益（基線 m1027 の生・10 種・読みの四半期の全行）",
         "year\ttier\tclass\tn\tshareRevBelowCost\tsumRevMinusCostPerMonth\tsumExportInPerMonth\tmedianPriceOverRawMC"]
for k in sorted(res):
    d = res[k]
    if len(d["neg"]) < 20:
        continue
    pm = [v for v in d["pm"] if v == v]
    lines.append("\t".join([str(k[0]), k[1], k[2], str(len(d["neg"])), f"{sum(d['neg'])/len(d['neg']):.3f}",
                            f"{sum(d['gap']):.1f}", f"{sum(v for v in d['exp'] if v == v):.1f}",
                            f"{statistics.median(pm):.3f}" if pm else "nan"]))
with open(out, "wb") as f:
    f.write(("\n".join(lines) + "\n").encode("utf-8"))
print("done", out)
