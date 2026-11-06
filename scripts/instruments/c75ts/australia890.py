# (890) 読む線（記録・旗との因果は確かめない）：小国の名目 GDP が四半期ごとに半分へ減ってから跳ぶ型。1 つの国・1 つの種の q の範囲を nation_s と region_s から並べる（読むだけ）。
#   python -X utf8 australia890.py <ins> <種> <国の名前> <q0> <q1>
import csv, sys, os, collections
d, s, nm, q0, q1 = sys.argv[1], int(sys.argv[2]), sys.argv[3], int(sys.argv[4]), int(sys.argv[5])
NC = ("quarter", "year", "populationK", "nominalGdpPerYear", "realGdpPerYear", "priceLevel", "atWarMonths", "civilWarMonths", "exportsPerClearing", "importsPerClearing",
      "sovereignDefaultCumulativeCount", "govDebtStock", "coreRegions", "colonialRegions", "nonCoreRegions")
with open(os.path.join(d, "nation_s%d.tsv" % s), encoding="utf-8", newline="") as f:
    r = csv.reader(f, delimiter="\t"); h = next(r); ix = {c: i for i, c in enumerate(h)}
    cols = [c for c in NC if c in ix]
    print("# nation_s%d %s q%d〜q%d" % (s, nm, q0, q1)); print("\t".join(cols))
    for x in r:
        if x[ix["nation"]] != nm: continue
        q = int(x[ix["quarter"]])
        if q0 <= q <= q1: print("\t".join(x[ix[c]] for c in cols))
RC = ("populationK", "warIntensity", "foodFillShare", "earnedPerMonth", "factoryOutputRefValuePerMonth", "extractionOutputRefValuePerMonth", "factoryActiveCount", "extractionActiveCount", "extractionBankruptCount", "activeFlag")
with open(os.path.join(d, "region_s%d.tsv" % s), encoding="utf-8", newline="") as f:
    r = csv.reader(f, delimiter="\t"); h = next(r); ix = {c: i for i, c in enumerate(h)}
    cols = [c for c in RC if c in ix]
    print("# region_s%d：持ち主が %s の州（q%d〜q%d）" % (s, nm, q0, q1)); print("quarter\tregion\ttier\t" + "\t".join(cols))
    for x in r:
        if x[ix["nation"]] != nm: continue
        q = int(x[ix["quarter"]])
        if q0 <= q <= q1: print("%d\t%s\t%s\t" % (q, x[ix["region"]], x[ix["tier"]]) + "\t".join(x[ix[c]] for c in cols))
