# 使い方: python -X utf8 -I q1_static_f.py <腕の生> <基線の生> <種 a-b> <出力>
# (1231) 問い 1 の三：consf（home・国×四半期の州×月の和）で、c123iv の規則の静的な見込みの上界 Σ_q min(希望_基線, 稼ぎ_基線) を基線の計画と並べ、
# 腕の計画・希望・支払いの比と分ける（cons_s は 1823 年で切れる＝その後はこの上界だけ）。上界＝州ごとの min の和 ≦ 和どうしの min。
import sys
def fl(x):
    try: return float(x)
    except: return float("nan")
def rd(raw, s0, s1):
    out = {}
    for s in range(s0, s1 + 1):
        with open(f"{raw}/consf_s{s}.tsv", encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            for l in f:
                c = l.rstrip("\n").split("\t")
                if c[ix["scope"]] != "home": continue
                out[(s, int(c[0]), c[2])] = (int(c[1]), fl(c[ix["desiredMoneySum"]]), fl(c[ix["flowEarnedLastMoneySum"]]), fl(c[ix["plannedMoneySum"]]), fl(c[ix["paidMoneySum"]]), fl(c[ix["budgetMoneySum"]]))
    return out
s0, s1 = map(int, sys.argv[3].split("-"))
A = rd(sys.argv[1], s0, s1); B = rd(sys.argv[2], s0, s1)
agg = {}
for k, b in B.items():
    a = A.get(k)
    if a is None or not all(x == x for x in b[1:] + a[1:]): continue
    g = agg.setdefault((k[2], b[0] // 10 * 10), [0.0] * 8)
    g[0] += min(b[1], max(b[2], 0)); g[1] += b[3]; g[2] += a[3]; g[3] += b[4]; g[4] += a[4]; g[5] += b[1]; g[6] += a[1]; g[7] += a[5]
with open(sys.argv[4], "w", encoding="utf-8", newline="\n") as w:
    w.write("nation\tdecade\tstaticUpper/plan_base\tplan_arm/plan_base\tpaid_arm/paid_base\tdes_arm/des_base\tbudget_arm/plan_arm\tpaid/plan_base\tpaid/plan_arm\n")
    r = lambda x, y: x / y if y else float("nan")
    for (n, d), g in sorted(agg.items()):
        if 1790 <= d <= 1940:
            w.write(f"{n}\t{d}\t{r(g[0],g[1]):.4f}\t{r(g[2],g[1]):.4f}\t{r(g[4],g[3]):.4f}\t{r(g[6],g[5]):.4f}\t{r(g[7],g[2]):.3f}\t{r(g[3],g[1]):.4f}\t{r(g[4],g[2]):.4f}\n")
