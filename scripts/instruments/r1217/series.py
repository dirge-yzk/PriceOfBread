# 使い方: python -X utf8 -I series.py <rawdir> <seed> <nation> <out>
# 年ごとの満足度（四半期平均）・日用品の充足・食料・人口・名目GDP・価格水準・戦争・内戦・州の数
import sys, math, csv
raw, seed, nat, out = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
def rows(t):
    with open(f"{raw}/{t}_s{seed}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t")
        for l in f:
            c = l.rstrip("\n").split("\t")
            yield dict(zip(h, c))
def fl(x):
    try: return float(x)
    except: return float("nan")
sat = {}
for r in rows("satneeds"):
    if r["nation"] != nat: continue
    y = int(r["year"]); sat.setdefault(y, []).append(r)
nat_s = {}
for r in rows("nation"):
    if r["nation"] != nat: continue
    y = int(r["year"]); nat_s.setdefault(y, []).append(r)
with open(out, "w", encoding="utf-8", newline="\n") as o:
    o.write("year\tnq\tsatMean\tsatMin\tdailyFill\tluxMet\tdailyTotal\tfoodFill\tpopK\tnomGdpY\trealGdpY\tpriceLevel\tatWarM\tcivWarM\tcore\tinteg\tcolon\tnoncore\tearnedPM\tgdpPerCapY\tnanCount\n")
    for y in sorted(set(sat) | set(nat_s)):
        S = sat.get(y, []); N = nat_s.get(y, [])
        sv = [fl(r["satisfaction"]) for r in S]
        dm = sum(fl(r["dailyMet"]) for r in S); dt = sum(fl(r["dailyTotal"]) for r in S)
        lm = sum(fl(r["luxuryMet"]) for r in S)
        ff = [fl(r["foodFillLastMonth"]) for r in S]
        last = N[-1] if N else {}
        g = lambda k: fl(last.get(k, "nan"))
        nanc = sum(1 for r in S for k in ("satisfaction","dailyMet","dailyTotal") if not math.isfinite(fl(r[k])))
        nanc += sum(1 for r in N for k in ("nominalGdpPerYear","priceLevel","populationK") if not math.isfinite(fl(r.get(k,"0"))))
        popk = g("populationK")
        def f(x): return "nan" if not math.isfinite(x) else f"{x:.4g}"
        o.write("\t".join([str(y), str(len(S)), f(sum(sv)/len(sv) if sv else float("nan")), f(min(sv) if sv else float("nan")),
            f(dm/dt if dt>0 else float("nan")), f(lm), f(dt), f(sum(ff)/len(ff) if ff else float("nan")), f(popk),
            f(g("nominalGdpPerYear")), f(g("realGdpPerYear")), f(g("priceLevel")),
            f(sum(fl(r["atWarMonths"]) for r in N) if N else float("nan")), f(sum(fl(r["civilWarMonths"]) for r in N) if N else float("nan")),
            f(g("coreRegions")), f(g("integratedRegions")), f(g("colonialRegions")), f(g("nonCoreRegions")),
            f(g("earnedTotalPerMonth")), f(g("nominalGdpPerYear")/popk if popk>0 else float("nan")), str(nanc)]) + "\n")
