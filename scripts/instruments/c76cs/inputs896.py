# (896) 周辺の点検の材料：召集の新しい入力（利払い÷歳入 x・DebtGrip）の時期ごとの中央値（crownpc_s・読むだけ）
import csv, sys, os, statistics
from collections import defaultdict
base, out = sys.argv[1], sys.argv[2]
NAT = ["イギリス", "清", "フランス", "ロシア", "プロイセン", "オスマン", "オーストリア", "日本", "アメリカ"]
PER = [(1783, 1830), (1830, 1880), (1880, 1914), (1914, 1951)]
xs = defaultdict(list); dg = defaultdict(list); au = defaultdict(list); hi = defaultdict(int); n = defaultdict(int)
for k in range(1, 11):
    with open(os.path.join(base, f"crownpc_s{k}.tsv"), encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {a: i for i, a in enumerate(h)}
        for l in r:
            nat = l[ix["nation"]]
            if nat not in NAT or l[ix["firstRowFlag"]] == "1": continue
            yr = int(l[ix["year"]])
            p = next((a for a, b in PER if a <= yr < b), None)
            rev = float(l[ix["revenuePerMonth"]]); it = float(l[ix["interestPerMonth"]])
            x = 0.0 if not (rev > 0) else it / rev
            key = (nat, p); n[key] += 1
            xs[key].append(x); dg[key].append(float(l[ix["debtGripShare"]])); au[key].append(float(l[ix["austerityIndex"]]))
            if x >= 0.67: hi[key] += 1
o = ["(896) 召集の入力の時期ごとの中央値（base＝keep_892/base_ins・種 1〜10・最初の行を除く）", "国\t時期\t行\tx 中央\tx≥0.67 の割合\tDebtGrip 中央\tDebtGrip>0.45 の割合\tAusterity 中央\tAusterity>0.5 の割合"]
for nat in NAT:
    for a, b in PER:
        key = (nat, a)
        if not n[key]: continue
        o.append(f"{nat}\t{a}–{b-1}\t{n[key]}\t{statistics.median(xs[key]):.3f}\t{hi[key]/n[key]*100:.1f}%\t{statistics.median(dg[key]):.3f}\t{sum(1 for v in dg[key] if v > 0.45)/n[key]*100:.1f}%\t{statistics.median(au[key]):.3f}\t{sum(1 for v in au[key] if v > 0.5)/n[key]*100:.1f}%")
open(out, "wb").write(("\n".join(o) + "\n").encode("utf-8"))
