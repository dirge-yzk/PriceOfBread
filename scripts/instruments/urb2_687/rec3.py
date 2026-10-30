# (687) 線 urb2 後半：記録（採否に使わない）。人口の倍率 1913/1783（9 国中央値）・疫病の死・都市への流入の区間別（urx_s の dUrbanK の区間別の和・全期間・9 国の本国 Core）。読むだけ。
# 使い方：python rec3.py <ins> [種の起点=1] [種の数=10]
import sys, csv, glob, statistics as st, collections
d = sys.argv[1].rstrip("/"); s0 = int(sys.argv[2]) if len(sys.argv) > 2 else 1; n = int(sys.argv[3]) if len(sys.argv) > 3 else 10
NAT9 = ["イギリス","フランス","プロイセン","オーストリア","ロシア","オスマン","日本","清","アメリカ"]
ratios = collections.defaultdict(list); sec = collections.defaultdict(float)
for s in range(s0, s0 + n):
    first = {}; y13 = {}
    with open(f"{d}/nation_s{s}.tsv", encoding="utf-8") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ina, iy, iq, ip = h.index("nation"), h.index("year"), h.index("quarter"), h.index("populationK")
        for row in r:
            nm = row[ina]
            if nm not in NAT9: continue
            p = float(row[ip])
            if nm not in first: first[nm] = p
            if int(row[iy]) == 1913 and nm not in y13: y13[nm] = p
    for nm in y13: ratios[nm].append(y13[nm] / first[nm])
    with open(f"{d}/urx_s{s}.tsv", encoding="utf-8") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r)
        for row in r:
            if row[3] == "Core" and row[2] in NAT9 and not row[4].startswith("_"):
                sec[row[4]] += float(row[6])
print("人口の倍率 1913/1783 の国ごとの種の中央値:", {k: round(st.median(v), 2) for k, v in ratios.items()}, " 9 国の中央値", round(st.median(st.median(v) for v in ratios.values()), 3))
top = sorted(sec.items(), key=lambda x: -abs(x[1]))[:6]
print("urx_s 区間別の都市への純の増減の和（千人・Core・9 国・全種）上位:", [(k, round(v)) for k, v in top])
print("  社会移動:", round(sec.get("社会移動", 0)), " 月次人口:", round(sec.get("月次人口", 0)))
