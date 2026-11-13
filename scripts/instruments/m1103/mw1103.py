# (1103) 記録（採否に使わない）：最低賃金法が施行されている国×四半期の行（monlaw_s の MinimumWage＞0）。c105gp の反証役 v2 の mw.py の写し（種の範囲を引数に）。
# 使い方: python -X utf8 -I mw1103.py <種の始め-種の終わり> <生 ins> [<生 ins> ...]
import sys, csv, collections, os
a, b = map(int, sys.argv[1].split('-'))
for d in sys.argv[2:]:
    per = []; bynat = collections.Counter()
    for s in range(a, b + 1):
        p = os.path.join(d, f"monlaw_s{s}.tsv")
        if not os.path.exists(p): per.append(None); continue
        n = 0
        with open(p, encoding="utf-8", newline="") as f:
            r = csv.reader(f, delimiter="\t"); h = next(r); iq, inn, im = h.index("quarter"), h.index("nation"), h.index("MinimumWage")
            for row in r:
                if float(row[im]) > 0: n += 1; bynat[row[inn]] += 1
        per.append(n)
    print(d, "MinimumWage>0 の行（種ごと）", per, "和", sum(x for x in per if x is not None))
    print("   上位", bynat.most_common(6))
