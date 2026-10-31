# (707) u4z：U4（私的な建設の予算を流れへ）の段0 の基線の値（国×十年×区分）＝consf_s（InstrumentsU4.cs）を読むだけ。
# 使い方：python scripts/instruments/u4z/base.py <生のフォルダ（consf_s1..N）> <出力> [種の数=10] [種の起点=1]
# 列の定義（州×月の判定の単位・助走の後）：
#   bind＝今の規則で予算が希望を切った州×月÷希望が正の州×月／cut＝切られた額÷希望の額
#   fbind＝流れの影（max(0, 前月の所得−前月の消費の支払い)×(1−萎縮)）で切る州×月÷影の判定で希望が正の州×月／fcut＝(影の希望−影の計画)÷影の希望
#   fav＝影の判定がある州×月÷判定の州×月（写しの無い月の割合の補数）
#   flow/paid＝Σ影の予算÷Σ実際の建設の支払い（設計書 §4-4 の〈資本家の流れ÷今の建設の額〉・支払いが 0 の国×十年は nan）
#   fplan/plan＝Σ影の計画÷Σ今の計画（同じ州×月＝影の判定があり希望が正の月で比べる・静的な反事実の計画の比）
#   paid/plan＝Σ実際の支払い÷Σ今の計画（資材と労働の配給・決済の値段で計画が削られた割合）／fplan/paid＝Σ影の計画÷Σ実際の支払い（1 以上なら影の予算は今の配給の後の建設の額を縛らない見込み〈推〉）
#   rho＝Σd_m d_{m-1}÷√(Σd_m²×Σd_{m-1}²)（月の建設の支払いの差の 1 つ遅れの自己相関・純な周期 2 で −1・白色雑音 −0.5・滑らか 0〜正）／amp＝Σ|d|÷Σx（振れ幅÷支払い）
#   ind＝四半期の末の工業化度の人口加重の平均（区分の州・十年の四半期の平均）
# 値は種の中央値（括弧は種の最大・rho は最小）。区分：home＝Core と Integrated の州・Colonial＝植民地格の州。
# 限界（計器の頭の申告を写す）：影は静的な反事実（希望・所得・消費は今の規則の軌道のまま）・今の消費の支払いは投資の取り置き（貯蓄の 5%）の後の値・判定は資本の区画と投資家 POP がある州だけ。
import csv, sys, statistics as st, collections as C, math
R, OUT = sys.argv[1], sys.argv[2]
NS = int(sys.argv[3]) if len(sys.argv) > 3 else 10
SB = int(sys.argv[4]) if len(sys.argv) > 4 else 1
NAT = ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "オスマン", "清", "日本", "アメリカ"]
SC = ("home", "Colonial")
K = ["judgedRegionMonthsCount", "desiringRegionMonthsCount", "budgetBoundRegionMonthsCount", "desiredMoneySum", "budgetMoneySum", "plannedMoneySum", "budgetBoundCutMoneySum",
     "flowAvailableRegionMonthsCount", "flowDesiringRegionMonthsCount", "flowBoundRegionMonthsCount", "flowDesiredMoneySum", "flowBudgetMoneySum", "flowPlannedMoneySum",
     "paidMoneySum", "oscRegionMonthsCount", "oscPaidMoneySum", "oscAbsFirstDiffMoneySum", "oscFirstDiffLagProductSum", "oscFirstDiffSquareSum", "oscFirstDiffLagSquareSum", "flowCurrentPlannedMoneySum"]
def dec(y): return 1783 if y < 1790 else (y // 10) * 10
def div(a, b): return a / b if b > 0 else float("nan")
res = C.defaultdict(lambda: C.defaultdict(list))   # (nation, decade, scope) -> 指標 -> 種ごとの値
allp = C.defaultdict(lambda: C.defaultdict(list))  # (nation, scope) -> 指標 -> 種ごとの全期間の値
rows_seen = 0
for s in range(SB, SB + NS):
    a = C.defaultdict(lambda: C.defaultdict(float)); ind = C.defaultdict(list)
    with open(f"{R}/consf_s{s}.tsv", encoding="utf-8") as f:
        for x in csv.DictReader(f, delimiter="\t"):
            n = x["nation"]
            if n not in NAT: continue
            if any(x.get(k) in (None, "") for k in K): continue   # 書きかけの行（走っている途中の生）を飛ばす
            rows_seen += 1
            y = int(x["year"]); sc = x["scope"]
            for key in ((n, dec(y), sc), (n, "all", sc)):
                t = a[key]
                for k in K: t[k] += float(x[k])
                v = float(x["industrializationPopWeightedIndex"])
                if v == v: ind[key].append(v)
    for key, t in a.items():
        m = {
            "bind": div(t["budgetBoundRegionMonthsCount"], t["desiringRegionMonthsCount"]),
            "cut": div(t["budgetBoundCutMoneySum"], t["desiredMoneySum"]),
            "fbind": div(t["flowBoundRegionMonthsCount"], t["flowDesiringRegionMonthsCount"]),
            "fcut": div(t["flowDesiredMoneySum"] - t["flowPlannedMoneySum"], t["flowDesiredMoneySum"]),
            "fav": div(t["flowAvailableRegionMonthsCount"], t["judgedRegionMonthsCount"]),
            "flow/paid": div(t["flowBudgetMoneySum"], t["paidMoneySum"]),
            "fplan/plan": div(t["flowPlannedMoneySum"], t["flowCurrentPlannedMoneySum"]),
            "paid/plan": div(t["paidMoneySum"], t["plannedMoneySum"]),
            "fplan/paid": div(t["flowPlannedMoneySum"], t["paidMoneySum"]),
            "rho": div(t["oscFirstDiffLagProductSum"], math.sqrt(t["oscFirstDiffSquareSum"] * t["oscFirstDiffLagSquareSum"])),
            "amp": div(t["oscAbsFirstDiffMoneySum"], t["oscPaidMoneySum"]),
            "ind": st.mean(ind[key]) if ind[key] else float("nan"),
            "rm": t["desiringRegionMonthsCount"],
        }
        tgt = allp if key[1] == "all" else res
        k2 = (key[0], key[2]) if key[1] == "all" else key
        for k, v in m.items(): tgt[k2][k].append(v)
def med(v):
    v = [x for x in v if x == x]; return st.median(v) if v else float("nan")
def mx(v, lo=False):
    v = [x for x in v if x == x]; return (min(v) if lo else max(v)) if v else float("nan")
COLS = ["bind", "fbind", "cut", "fcut", "fav", "flow/paid", "fplan/plan", "paid/plan", "fplan/paid", "rho", "amp", "ind"]
def fmt(r, c):
    return f"{med(r[c]):.3g}({mx(r[c], c == 'rho'):.3g})"
L = [f"# (707) u4z U4 の段0 の基線の値（consf_s・種 {SB}〜{SB + NS - 1}・挙げた 9 国・州×月の単位）。値は種の中央値（括弧は種の最大・rho は種の最小）。列の定義は読み手 scripts/instruments/u4z/base.py の頭。",
     "# 区分：home＝Core と Integrated の州（U4 の門はこちらで読む）・Colonial＝植民地格の州（投資家の貯蓄がほぼ 0＝記録）。rm＝希望が正の州×月の数（種の中央値）。",
     "nation\tdecade\tscope\trm\t" + "\t".join(COLS)]
for n in NAT:
    for d in sorted({k[1] for k in res if k[0] == n}):
        for sc in SC:
            if (n, d, sc) not in res: continue
            r = res[(n, d, sc)]
            L.append(f"{n}\t{d}\t{sc}\t{med(r['rm']):.0f}\t" + "\t".join(fmt(r, c) for c in COLS))
L.append("## 全期間（1783〜1950・国×区分）")
L.append("nation\tperiod\tscope\trm\t" + "\t".join(COLS))
for n in NAT:
    for sc in SC:
        if (n, sc) not in allp: continue
        r = allp[(n, sc)]
        L.append(f"{n}\tall\t{sc}\t{med(r['rm']):.0f}\t" + "\t".join(fmt(r, c) for c in COLS))
# 要約：home の国×十年の中央値（種の中央値の中央値）
for sc in SC:
    for c in ("bind", "fbind", "fplan/paid", "rho"):
        v = [med(res[k][c]) for k in res if k[2] == sc]
        v = [x for x in v if x == x]
        if v: L.append(f"# 要約 {sc} {c}：国×十年 {len(v)} 件・中央 {st.median(v):.3g}・最小 {min(v):.3g}・最大 {max(v):.3g}")
L.append(f"# 読んだ行 {rows_seen}")
open(OUT, "wb").write(("\n".join(L) + "\n").encode("utf-8"))
print("\n".join(L[-8:]))
