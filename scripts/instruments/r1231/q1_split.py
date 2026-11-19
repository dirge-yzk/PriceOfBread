# 使い方: python -X utf8 -I q1_split.py <腕の生> <基線の生> <種 a-b> <出力の頭>
# (1231) 読む線 c124rd 問い 1：資本家の消失の根の分岐（設計書 §12-5 の (i)〜(iii)・読む前に固定された規則をそのまま当てる）。
# 母集合＝consf の scope=home（計器 9 国の本国格の州×月の和・投資家の POP）。E＝前の月の稼ぎ・C＝前の月の消費の支払い・F⁺＝州×月の正の部分 max(0,E−C)（萎縮を掛けた値）・P＝建設の支払い・fplan＝流れの影で切った計画。
# 名目 GDP は nation_s の nominalGdpPerYear（四半期の値の平均＝年率）。比は 10 種の和どうし（種の中央値の比も並べる）。
# 縛る組＝1800〜1910 年代の home の国×十年で、基線の fplan/paid ≦ 0.8（設計書 §11-9 の機械の規則・基線は m1226 で取り直す）。
import sys, statistics as st
def fl(x):
    try: return float(x)
    except: return float("nan")
def rd(raw, s0, s1):
    out = {}   # (seed, nation, decade) -> dict
    for s in range(s0, s1 + 1):
        gdp = {}
        with open(f"{raw}/nation_s{s}.tsv", encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            for l in f:
                c = l.rstrip("\n").split("\t")
                gdp[(c[ix["nation"]], int(c[ix["quarter"]]))] = fl(c[ix["nominalGdpPerYear"]])
        with open(f"{raw}/consf_s{s}.tsv", encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            for l in f:
                c = l.rstrip("\n").split("\t")
                if c[ix["scope"]] != "home": continue
                q = int(c[0]); y = int(c[1]); n = c[2]; d = y // 10 * 10
                a = out.setdefault((s, n, d), dict(E=0.0, C=0.0, Fp=0.0, P=0.0, fplan=0.0, plan=0.0, bud=0.0, des=0.0, S=0.0, Y=0.0, nq=0, ny=0))
                a["E"] += fl(c[ix["flowEarnedLastMoneySum"]]); a["C"] += fl(c[ix["flowConsumptionPaidLastMoneySum"]])
                a["Fp"] += fl(c[ix["flowBudgetMoneySum"]]); a["P"] += fl(c[ix["paidMoneySum"]])
                a["fplan"] += fl(c[ix["flowPlannedMoneySum"]]); a["plan"] += fl(c[ix["plannedMoneySum"]])
                a["bud"] += fl(c[ix["budgetMoneySum"]]); a["des"] += fl(c[ix["desiredMoneySum"]])
                a["S"] += fl(c[ix["investorSavingsStock"]]); a["nq"] += 1
                g = gdp.get((n, q), float("nan"))
                if g == g: a["Y"] += g / 4.0; a["ny"] += 1   # 四半期の名目 GDP（年率÷4）
    return out
A = rd(sys.argv[1], *map(int, sys.argv[3].split("-"))); B = rd(sys.argv[2], *map(int, sys.argv[3].split("-")))
pre = sys.argv[4]
keys = sorted({(n, d) for (_, n, d) in B})
def pool(T, n, d, k): return sum(v[k] for (s, nn, dd), v in T.items() if nn == n and dd == d)
rows = []
for n, d in keys:
    if not (1780 <= d <= 1940): continue
    r = {k: (pool(A, n, d, k), pool(B, n, d, k)) for k in ("E", "C", "Fp", "P", "fplan", "plan", "bud", "des", "S", "Y")}
    Pb = r["P"][1]; fp = r["fplan"][1] / Pb if Pb > 0 else float("nan")
    bind = (1800 <= d <= 1910) and fp <= 0.8
    rows.append((n, d, bind, fp, r))
with open(pre + "_table.tsv", "w", encoding="utf-8", newline="\n") as w:
    w.write("nation\tdecade\tbind\tfplan_over_paid_base\tP_arm/base\tE_arm/base\tC_arm/base\tF+_arm/base\tE/Y_arm\tE/Y_base\tC/Y_arm\tC/Y_base\tF+/Y_arm\tF+/Y_base\tP/Y_arm\tP/Y_base\tS/Yq_arm\tS/Yq_base\tC/E_arm\tC/E_base\tshare_income_raw\tshare_cons_raw\tshare_income_gdp\tshare_cons_gdp\n")
    for n, d, bind, fp, r in rows:
        g = lambda k, i: r[k][i]
        rat = lambda k: g(k, 0) / g(k, 1) if g(k, 1) else float("nan")
        Ya, Yb = g("Y", 0), g("Y", 1)
        # 生の額の分け：F＝E−C（正負の和）・ΔF＝ΔE−ΔC
        Fa, Fb = g("E", 0) - g("C", 0), g("E", 1) - g("C", 1); dF = Fb - Fa
        si = (g("E", 1) - g("E", 0)) / dF if dF > 0 else float("nan"); sc = (g("C", 0) - g("C", 1)) / dF if dF > 0 else float("nan")
        # GDP で割った分け（値段の水準の差を外す）
        ea, eb, ca, cb = g("E", 0) / Ya, g("E", 1) / Yb, g("C", 0) / Ya, g("C", 1) / Yb; dfy = (eb - cb) - (ea - ca)
        siy = (eb - ea) / dfy if dfy > 0 else float("nan"); scy = (ca - cb) / dfy if dfy > 0 else float("nan")
        w.write("\t".join([n, str(d), "1" if bind else "0", f"{fp:.3f}", f"{rat('P'):.4f}", f"{rat('E'):.4f}", f"{rat('C'):.4f}", f"{rat('Fp'):.4f}",
                           f"{ea:.4f}", f"{eb:.4f}", f"{ca:.4f}", f"{cb:.4f}", f"{g('Fp',0)/Ya:.4f}", f"{g('Fp',1)/Yb:.4f}", f"{g('P',0)/Ya:.4f}", f"{g('P',1)/Yb:.4f}",
                           f"{g('S',0)/Ya:.2f}", f"{g('S',1)/Yb:.2f}", f"{g('C',0)/g('E',0):.3f}", f"{g('C',1)/g('E',1):.3f}", f"{si:.3f}", f"{sc:.3f}", f"{siy:.3f}", f"{scy:.3f}"]) + "\n")
# 縛る組の集計（読む前に固定の規則：流れの落ちのうち所得の落ちが 6 割以上＝(i)・消費の増えが 6 割以上＝(ii)・どちらも届かない＝(iii)）
bind = [x for x in rows if x[2]]
with open(pre + "_verdict.txt", "w", encoding="utf-8", newline="\n") as w:
    w.write(f"縛る組の件数 {len(bind)}（基線 m1226 の fplan/paid ≦ 0.8・1800〜1910 年代 home）\n")
    w.write("組: " + " ".join(f"{n}{d}" for n, d, *_ in bind) + "\n")
    for tag, F in (("生の額", lambda r: (r["E"][1] - r["E"][0], r["C"][0] - r["C"][1])), ("GDP で割った額", lambda r: (r["E"][1] / r["Y"][1] - r["E"][0] / r["Y"][0], r["C"][0] / r["Y"][0] - r["C"][1] / r["Y"][1]))):
        si_l = []; sc_l = []; dEs = dCs = 0.0
        for n, d, b, fp, r in bind:
            dE, dC = F(r); dF = dE + dC
            if dF > 0: si_l.append(dE / dF); sc_l.append(dC / dF)
            if tag == "GDP で割った額": dEs += dE; dCs += dC
        med = lambda v: st.median(v) if v else float("nan")
        w.write(f"[{tag}] 流れが落ちた組 {len(si_l)}/{len(bind)}・所得の落ちの割合の中央 {med(si_l):.3f}・消費の増えの割合の中央 {med(sc_l):.3f}\n")
        if tag == "GDP で割った額": w.write(f"[{tag}] 組の和：所得の落ち {dEs:.4f}・消費の増え {dCs:.4f}・所得の割合 {dEs/(dEs+dCs) if dEs+dCs>0 else float('nan'):.3f}\n")
    pr = [r["P"][0] / r["P"][1] for n, d, b, fp, r in bind if r["P"][1] > 0]
    er = [r["E"][0] / r["E"][1] for n, d, b, fp, r in bind if r["E"][1] > 0]
    cr = [r["C"][0] / r["C"][1] for n, d, b, fp, r in bind if r["C"][1] > 0]
    fr = [r["fplan"][1] / r["P"][1] for n, d, b, fp, r in bind if r["P"][1] > 0]
    w.write(f"縛る組の中央：P 腕÷基線 {st.median(pr):.4f}・E 腕÷基線 {st.median(er):.4f}・C 腕÷基線 {st.median(cr):.4f}・基線の fplan/paid {st.median(fr):.3f}\n")
