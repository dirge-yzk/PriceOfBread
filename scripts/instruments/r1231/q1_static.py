# 使い方: python -X utf8 -I q1_static.py <腕の生> <基線の生> <種 a-b> <出力>
# (1231) 問い 1 の二：c123iv の規則（予算＝min(前の月の稼ぎ, 貯蓄)）の静的な見込み（基線の稼ぎ・希望・貯蓄のままなら計画はいくらか）と腕の実際を、州×四半期（cons_s・Core と Integrated）で並べる。
# 近似：四半期の平均の月の額で min を取る（月ごとの min の和より大きく出る＝静的な見込みは上側）。
# 列：static＝Σmin(希望_基線, 稼ぎ⁺_基線, 貯蓄_基線)・plan＝計画・paid＝支払い・des＝希望・E⁺＝稼ぎの正の部分。比は 10 種の和どうし。
import sys
def fl(x):
    try: return float(x)
    except: return float("nan")
def rd(raw, s0, s1):
    out = {}
    for s in range(s0, s1 + 1):
        with open(f"{raw}/cons_s{s}.tsv", encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            for l in f:
                c = l.rstrip("\n").split("\t")
                if c[ix["tier"]] not in ("Core", "Integrated"): continue
                out[(s, int(c[0]), c[2], c[3])] = (fl(c[ix["desiredMoneyPerMonth"]]), fl(c[ix["investorEarnedPerMonth"]]), fl(c[ix["investorSavingsStock"]]),
                                                   fl(c[ix["plannedMoneyPerMonth"]]), fl(c[ix["paidPerMonth"]]), fl(c[ix["budgetMoneyPerMonth"]]), int(c[1]))
    return out
s0, s1 = map(int, sys.argv[3].split("-"))
A = rd(sys.argv[1], s0, s1); B = rd(sys.argv[2], s0, s1)
agg = {}
pos = lambda x: x if x > 0 else 0.0
for k, b in B.items():
    a = A.get(k)
    if a is None: continue
    n = k[2]; d = b[6] // 10 * 10
    g = agg.setdefault((n, d), [0.0] * 10)
    desb, eb, sb, planb, paidb, budb = b[:6]; desa, ea, sa, plana, paida, buda = a[:6]
    if not all(x == x for x in (desb, eb, sb, planb, paidb, desa, ea, plana, paida)): continue
    g[0] += min(desb, pos(eb), pos(sb)); g[1] += planb; g[2] += plana; g[3] += paidb; g[4] += paida
    g[5] += desb; g[6] += desa; g[7] += pos(eb); g[8] += pos(ea); g[9] += min(desa, pos(eb), pos(sb))   # 9＝腕の希望は使わず基線の稼ぎ（所得の落ちだけを外す）
with open(sys.argv[4], "w", encoding="utf-8", newline="\n") as w:
    w.write("nation\tdecade\tstatic/plan_base\tplan_arm/plan_base\tpaid_arm/paid_base\tpaid/plan_base\tpaid/plan_arm\tdes_arm/des_base\tE+_arm/E+_base\tstatic_withArmDesire/plan_base\n")
    for (n, d), g in sorted(agg.items()):
        if not (1790 <= d <= 1910): continue
        r = lambda x, y: x / y if y else float("nan")
        w.write(f"{n}\t{d}\t{r(g[0],g[1]):.4f}\t{r(g[2],g[1]):.4f}\t{r(g[4],g[3]):.4f}\t{r(g[3],g[1]):.4f}\t{r(g[4],g[2]):.4f}\t{r(g[6],g[5]):.4f}\t{r(g[8],g[7]):.4f}\t{r(g[9],g[1]):.4f}\n")
