# (1041) c98bp 紙の台本（読むだけ・生 keep_1035/base_ins の recipeout_s）
# 案 B（自己費用 ÷ 播種の倍率＝endowFromProductivity）と案 B′（自己費用 ÷ ResourceEndowment＝鉱物 4.0・他 3.5）の単位費用を
# 級ごとに産出の重みで並べる。級 X の開いた鉱床（倍率 20＝4.0×GoldRush 5.0）は B″（÷20）と B′（÷4.0）の両方を出す。
# 使い方: python bprime_paper.py <base_ins のフォルダ> <出力 tsv>
import sys, os, math, collections, statistics

src, out = sys.argv[1], sys.argv[2]
YEARS = (1790, 1870, 1913)
MINERAL_RE = 4.0
OTHER_RE = 3.5

rows = []
minerals = set()
for s in range(1, 11):
    p = os.path.join(src, f"recipeout_s{s}.tsv")
    with open(p, "rb") as f:
        head = f.readline().decode("utf-8").rstrip("\r\n").split("\t")
        ix = {k: i for i, k in enumerate(head)}
        for raw in f:
            c = raw.decode("utf-8").rstrip("\r\n").split("\t")
            if c[0].startswith("#"):
                continue
            try:
                y = int(c[ix["year"]])
            except ValueError:
                continue
            cls = c[ix["class"]]
            if cls not in ("E", "X", "N", "S"):
                continue
            def fl(k):
                try:
                    return float(c[ix[k]])
                except ValueError:
                    return float("nan")
            e = fl("endowFromProductivity")
            if cls in ("E", "X") and e >= 3.99 and abs(e - 3.5) > 1e-6 and abs(e - 3.4999999999999996) > 1e-6:
                minerals.add(c[ix["good"]])
            if y not in YEARS:
                continue
            rows.append(dict(seed=s, year=y, nation=c[ix["nation"]], good=c[ix["good"]], cls=cls,
                             o=fl("outputSum"), unit=fl("unitCostOutW"), self=fl("selfCostOutW"),
                             ab=fl("unitCostIfAbundance"), endow=e, found=c[ix["siteFound"]]))

def re_of(g):
    return MINERAL_RE if g in minerals else OTHER_RE

def costs(r):
    inp = r["unit"] - r["self"]
    b = r["ab"]
    if r["cls"] in ("E", "X") and r["endow"] > 1.0:
        bp = r["self"] / re_of(r["good"]) + inp
    else:
        bp = r["unit"]
    return inp, b, bp

agg = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0.0, 0.0])
per_seed = collections.defaultdict(lambda: collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0.0]))
for r in rows:
    if not (r["o"] > 0) or math.isnan(r["unit"]):
        continue
    inp, b, bp = costs(r)
    k = (r["year"], r["cls"])
    a = agg[k]
    a[0] += r["o"]; a[1] += r["o"] * r["unit"]; a[2] += r["o"] * b; a[3] += r["o"] * bp; a[4] += r["o"] * r["self"]
    ps = per_seed[k][r["seed"]]
    ps[0] += r["o"]; ps[1] += r["o"] * r["unit"]; ps[2] += r["o"] * b; ps[3] += r["o"] * bp

lines = ["#(1041) 級ごとの単位費用（産出の重み・10 種の和÷和）：基線・B（÷播種の倍率）・B′（÷ResourceEndowment）",
         "year\tclass\toutput\tunitBase\tselfBase\tunitB\tunitBprime\tB_over_base\tBprime_over_base\tseedMedian_B_over_base\tseedMedian_Bp_over_base"]
for k in sorted(agg):
    a = agg[k]
    if a[0] <= 0:
        continue
    rb = [v[2] / v[1] for v in per_seed[k].values() if v[1] > 0]
    rp = [v[3] / v[1] for v in per_seed[k].values() if v[1] > 0]
    lines.append("\t".join([str(k[0]), k[1], f"{a[0]:.1f}", f"{a[1]/a[0]:.4f}", f"{a[4]/a[0]:.4f}", f"{a[2]/a[0]:.4f}", f"{a[3]/a[0]:.4f}",
                            f"{a[2]/a[1]:.4f}", f"{a[3]/a[1]:.4f}", f"{statistics.median(rb):.4f}", f"{statistics.median(rp):.4f}"]))

# 非産地÷産地（N÷E）の比
lines.append("#N÷E の単位費用の比（和÷和）")
lines.append("year\tN_over_E_base\tN_over_E_B\tN_over_E_Bprime")
for y in YEARS:
    e, n = agg.get((y, "E")), agg.get((y, "N"))
    if e and n and e[0] > 0 and n[0] > 0:
        nb = n[1] / n[0]
        lines.append(f"{y}\t{nb/(e[1]/e[0]):.3f}\t{nb/(e[2]/e[0]):.3f}\t{nb/(e[3]/e[0]):.3f}")

# 倍率が ResourceEndowment を超える行（硫黄 126.8・開いた鉱床 20）
lines.append("#倍率が ResourceEndowment を超える行（10 種の中央値）")
lines.append("year\tnation\tgood\tclass\tendow\tn_seeds\tselfBase\tunitBase\tselfB\tunitB\tselfBp\tunitBp\tselfB_over_classE_selfB\tunitB_over_classE_unitB")
big = collections.defaultdict(list)
for r in rows:
    if r["cls"] in ("E", "X") and r["endow"] > re_of(r["good"]) + 1e-6 and not math.isnan(r["unit"]):
        big[(r["year"], r["nation"], r["good"], r["cls"], round(r["endow"], 1))].append(r)
for k in sorted(big):
    rs = big[k]
    y = k[0]
    e = agg.get((y, "E"))
    selfE_B = None
    if e and e[0] > 0:
        # 級 E の自己費用（B）＝自己費用の和÷倍率（行ごと）を産出で重みづけ
        num = den = 0.0
        for r in rows:
            if r["year"] == y and r["cls"] == "E" and r["o"] > 0 and not math.isnan(r["self"]) and r["endow"] > 1:
                num += r["o"] * r["self"] / r["endow"]; den += r["o"]
        selfE_B = num / den if den > 0 else None
    unitE_B = e[2] / e[0] if e and e[0] > 0 else None
    md = lambda f: statistics.median([f(r) for r in rs])
    sB = md(lambda r: r["self"] / r["endow"])
    uB = md(lambda r: r["ab"])
    sBp = md(lambda r: r["self"] / re_of(r["good"]))
    uBp = md(lambda r: r["self"] / re_of(r["good"]) + r["unit"] - r["self"])
    lines.append("\t".join([str(y), k[1], k[2], k[3], str(k[4]), str(len(rs)), f"{md(lambda r: r['self']):.4f}", f"{md(lambda r: r['unit']):.4f}",
                            f"{sB:.5f}", f"{uB:.4f}", f"{sBp:.4f}", f"{uBp:.4f}",
                            f"{(sB/selfE_B):.3f}" if selfE_B else "nan", f"{(uB/unitE_B):.3f}" if unitE_B else "nan"]))

lines.append("#鉱物と数えた財（倍率 4.0 以上の E・X の行を持つ財）: " + "・".join(sorted(minerals)))
with open(out, "wb") as f:
    f.write(("\n".join(lines) + "\n").encode("utf-8"))
print("rows", len(rows), "out", out)
