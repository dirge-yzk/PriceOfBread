# (701) smu1：U4（私的な建設の予算を流れへ）の段0 の材料＝今の予算が希望を切る州×四半期の割合（読むだけ）。
# 使い方：python scripts/instruments/smu1/u4base.py <生のフォルダ（cons_s1..10）> <出力>
# cons_s（(289) 州ごとの希望・枠・計画）：desiredMoneyPerMonth＝希望（予算で切る前）・budgetMoneyPerMonth＝予算（投資家の貯蓄×IndInvestmentFrac）・plannedMoneyPerMonth＝min の結果。
# 切った＝planned＜desired×(1−1e-9)（desired＞0 の州×四半期だけ）。限界：四半期の月の平均の値で判定＝月の中の 1 か月だけ切った州は拾わない。cons_s は 1783〜1823 だけ（計器の窓）。
import csv, sys, statistics as st, collections as C
R, OUT = sys.argv[1], sys.argv[2]
NAT = ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "オスマン", "清", "日本", "アメリカ"]
SC = ("all", "home", "Colonial")
def dec(y): return 1783 if y < 1790 else (y // 10) * 10
res = C.defaultdict(lambda: C.defaultdict(list))
for s in range(1, 11):
    a = C.defaultdict(lambda: [0, 0, 0.0, 0.0, 0.0])
    with open(f"{R}/cons_s{s}.tsv", encoding="utf-8") as f:
        for x in csv.DictReader(f, delimiter="\t"):
            if x["nation"] not in NAT: continue
            try: d = float(x["desiredMoneyPerMonth"]); b = float(x["budgetMoneyPerMonth"]); p = float(x["plannedMoneyPerMonth"])
            except: continue
            if not d > 0: continue
            y = dec(int(x["year"])); n = x["nation"]
            for k in ((y, n, "all"), (y, n, "Colonial" if x["tier"] == "Colonial" else "home")):
                t = a[k]; t[0] += 1; t[1] += 1 if p < d * (1 - 1e-9) else 0; t[2] += d; t[3] += b; t[4] += max(0.0, d - p)
    for k, t in a.items():
        res[k]["bind"].append(t[1] / t[0]); res[k]["bud"].append(t[3] / t[2]); res[k]["cut"].append(t[4] / t[2])
def med(v):
    v = [x for x in v if x == x]; return st.median(v) if v else float("nan")
L = ["# (701) smu1 U4 の段0 の材料（基線 m694・種 1〜10）：bind＝予算が希望を切った州×四半期の割合・cut＝切られた額÷希望の額・bud＝Σ予算÷Σ希望。値は種の中央値（括弧は種の最大）。",
     "# 区分：home＝本国格と統合の州・Colonial＝植民地格の州（投資家の貯蓄がほぼ 0）。cons_s は 1783〜1823 だけ（計器の窓）。",
     "nation\tdecade\tscope\tbind\tcut\tbud"]
for n in NAT:
    for d in sorted({k[0] for k in res if k[1] == n}):
        for sc in SC:
            if (d, n, sc) not in res: continue
            r = res[(d, n, sc)]
            L.append(f"{n}\t{d}\t{sc}\t{med(r['bind']):.3g}({max(r['bind']):.3g})\t{med(r['cut']):.3g}({max(r['cut']):.3g})\t{med(r['bud']):.3g}")
for sc in SC:
    b = [x for k in res if k[2] == sc for x in res[k]["bind"]]; c = [x for k in res if k[2] == sc for x in res[k]["cut"]]
    L.append(f"# {sc}：国×十年×種 {len(b)} 件・bind 中央 {st.median(b):.3g}・最大 {max(b):.3g}・cut 中央 {st.median(c):.3g}・最大 {max(c):.3g}")
open(OUT, "wb").write(("\n".join(L) + "\n").encode("utf-8"))
print("\n".join(L[-3:]))
