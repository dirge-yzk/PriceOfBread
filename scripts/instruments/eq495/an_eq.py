# (495) 線 eq の読み手：計器の表 eq_s・eqr_s と既存の表（factory_s・class_s・nation_s）から、事前登録 measurements/eq495_20261023_prereg.txt の
# C1（配りが設備になった）・C2（買わないと増えない）・V1（減るのは減価だけ）・D1（向き）・S2（発散）と後退の確かめ R1〜R3 を読む。読むだけ（シミュは回さない）。
# 使い方：PYTHONUTF8=1 python scripts/instruments/eq495/an_eq.py <計器の出力フォルダ> [dep=0.003]
# 限界：eq_s・eqr_s は旗 RecipeEquipment が正の走りにしか出ない（A の走りでは空＝このスクリプトは〈表なし〉と言う）。
import csv, glob, math, os, re, statistics as st, sys, collections

d = sys.argv[1]
dep = float(sys.argv[2].split("=")[1]) if len(sys.argv) > 2 else 0.003

def rows(path):
    with open(path, encoding="utf-8") as fh:
        return list(csv.DictReader(fh, delimiter="\t"))

def fnum(x):
    try: return float(x)
    except Exception: return float("nan")

seeds = sorted(int(re.search(r"_s(\d+)\.tsv$", p).group(1)) for p in glob.glob(os.path.join(d, "eq_s*.tsv")))
if not seeds:
    print("表なし（eq_s*.tsv が無い＝旗 RecipeEquipment が 0 の走り）"); sys.exit(0)
print(f"種 {seeds}")

# ---- C1・C3・S2（eq_s） ----
# C1：購入のあった国×四半期に設備が足された（installed＞0）。C3：適格な個体の Scale の合計の変化＝BuiltScale の合計の変化（民間の持分の合計は購入で増えない・事前登録 追記 3）。
c1_rows = c1_noinst = c1_neg = 0; c3_n = c3_viol = 0; c3_worst = 0.0; c3_rows = []; s2_ratio = 0.0; s2_nation = ''; scale_max_all = 0.0; ind_max_all = 0.0; scale_sum_max = 0.0
for sd in seeds:
    byn = collections.defaultdict(list)
    for r in rows(os.path.join(d, f"eq_s{sd}.tsv")):
        byn[r["nation"]].append(r)
    for n, rs in byn.items():
        rs.sort(key=lambda r: int(r["quarter"]))
        prev = None
        first_ss = fnum(rs[0]["scaleSum"]) if rs else float("nan")
        for r in rs:
            pts, ins = fnum(r["pointsBought"]), fnum(r["equipInstalled"])
            if first_ss > 0 and fnum(r["scaleSum"]) / first_ss > s2_ratio: s2_ratio = fnum(r["scaleSum"]) / first_ss; s2_nation = f"{n}(種{sd}・四半期{r['quarter']})"
            ss, bs = fnum(r["scaleSum"]), fnum(r["builtSum"])
            scale_max_all = max(scale_max_all, fnum(r["scaleMax"])); ind_max_all = max(ind_max_all, fnum(r["indMax"])); scale_sum_max = max(scale_sum_max, ss)
            if pts > 0:
                c1_rows += 1
                if ins < -1e-12 or not math.isfinite(ins): c1_neg += 1
                if ins <= 0: c1_noinst += 1
            if prev is not None and int(r["quarter"]) == int(prev["quarter"]) + 1:
                dS = ss - fnum(prev["scaleSum"]); dB = bs - fnum(prev["builtSum"])
                c3_n += 1
                err = abs(dS - dB) / max(1.0, abs(ss))
                c3_worst = max(c3_worst, err)
                if err > 1e-6: c3_viol += 1; c3_rows.append((err, sd, n, int(r["quarter"]), dS, dB, ss))
            prev = r
print(f"C1 配りが設備になった：購入のあった国×四半期 {c1_rows}・設備が足されなかった {c1_noinst}（{c1_noinst / max(1, c1_rows):.4%}）・負か非有限 {c1_neg}")
print(f"C3 尺度が増えない：連続四半期 {c3_n} 件のうち 設備の合計の変化が国営の持分の合計の変化と相対 1e-6 を超えて違った {c3_viol} 件（0 が合格）・最悪の相対差 {c3_worst:.3g}")
for err, sd, n, q, dS, dB, ss in sorted(c3_rows, reverse=True)[:8]:
    print(f"   C3 の外れ（相対差 {err:.3g}）：種{sd} {n} 四半期{q} ΔScaleSum={dS:.6g} ΔBuiltSum={dB:.6g} ScaleSum={ss:.6g}")
print(f"S2 発散：国の設備の合計÷最初の行の最大 {s2_ratio:.3f}（{s2_nation}・線 2 以下）・国の設備の合計の最大 {scale_sum_max:.4g}・設備の最大（個体）{scale_max_all:.4g}（記録）・州の在庫の最大 {ind_max_all:.4g}（線 10）")

# ---- V1・D1（eqr） ----
share_series = collections.defaultdict(dict)   # (seed,nation,good,recipe) -> {quarter: (share, scale)}
for sd in seeds:
    p = os.path.join(d, f"eqr_s{sd}.tsv")
    if not os.path.exists(p): continue
    for r in rows(p):
        share_series[(sd, r["nation"], r["good"], r["recipe"])][int(r["quarter"])] = (fnum(r["shareOfGood"]), fnum(r["scaleSum"]))
# D1：同じ国×財で最初に現れたレシピ以外（後から現れたレシピ）の割合の +4…+40 四半期の中央値
by_good = collections.defaultdict(list)
for (sd, n, g, rc), ser in share_series.items():
    by_good[(sd, n, g)].append((rc, ser))
groups = []
for (sd, n, g), lst in by_good.items():
    if len(lst) < 2: continue
    firsts = sorted(((min(q for q, v in ser.items() if v[1] > 1e-6), rc, ser) for rc, ser in lst if any(v[1] > 1e-6 for v in ser.values())), key=lambda t: t[0])
    if len(firsts) < 2: continue
    t0, rc, ser = firsts[-1]   # いちばん遅く立ったレシピ＝新しいレシピ
    if t0 <= 0: continue
    groups.append([ser.get(t0 + k, (float("nan"), 0))[0] for k in range(4, 44, 4)])
print(f"D1 向き：後から立ったレシピの組 {len(groups)}")
if len(groups) >= 30:
    med = []
    for i in range(10):
        vals = [g[i] for g in groups if math.isfinite(g[i])]
        med.append(st.median(vals) if vals else float("nan"))
    mono = all(not (math.isfinite(a) and math.isfinite(b)) or b >= a - 1e-3 for a, b in zip(med, med[1:]))
    print("   中央値（+4,+8,…,+40 四半期）：" + " ".join(f"{m:.3f}" for m in med) + f"　単調に増える={mono}")
else:
    print("   判定不能（組が 30 未満＝番人で代える）")

# ---- R1〜R3（既存の表） ----
def nat_year(table, col, kind="last"):
    out = {}
    for sd in seeds:
        p = os.path.join(d, f"{table}_s{sd}.tsv")
        if not os.path.exists(p): continue
        for r in rows(p):
            if "year" in r and col in r:
                out[(sd, r["nation"], int(r["year"]))] = fnum(r[col])
    return out
big = ["イギリス", "フランス", "プロイセン", "アメリカ", "日本", "清"]
out = nat_year("factory", "outputRefValuePerMonth")
pop = nat_year("nation", "totalPopulationK")
r2 = {}
for n in big:
    vals = []
    for sd in seeds:
        a, b = out.get((sd, n, 1850)), out.get((sd, n, 1950)); pa, pb = pop.get((sd, n, 1850)), pop.get((sd, n, 1950))
        if a and b and pa and pb and a > 0 and pa > 0: vals.append((b / pb) / (a / pa))
    r2[n] = st.median(vals) if vals else float("nan")
print("R2 工場の産出÷人口の 1950÷1850（中央値）：" + "　".join(f"{n} {v:.2f}" for n, v in r2.items()) + f"　→ ≧0.5 の国 {sum(1 for v in r2.values() if v >= 0.5)}/6（線 5 以上）")
bk = nat_year("factory", "factoryBankruptCount"); fc = nat_year("factory", "factoryCount")
r3 = {}
for n in big:
    vals = []
    for sd in seeds:
        mx = 0.0
        for (s2, nn, y), c in fc.items():
            if s2 == sd and nn == n and c > 0: mx = max(mx, bk.get((s2, nn, y), 0) / c)
        vals.append(mx)
    r3[n] = st.median(vals) if vals else float("nan")
print("R3 工場の閉鎖（倒産）の割合の年の最大（種の中央値）：" + "　".join(f"{n} {v:.3f}" for n, v in r3.items()) + "　（線 ≦0.10）")
print("R1 工場労働者の階級の 1950÷ピーク：class_s の階級名を使う（読み手の限界＝階級の表は四半期の末でなく年の断面）")
cls = collections.defaultdict(dict)
for sd in seeds:
    p = os.path.join(d, f"class_s{sd}.tsv")
    if not os.path.exists(p): continue
    for r in rows(p):
        if r.get("className") == "工場労働者" and r["nation"] in big and r["time"] not in ("open", "warm"):
            try: cls[(sd, r["nation"])][int(r["year"])] = fnum(r["sizeK"])
            except Exception: pass
r1 = {}
for n in big:
    vals = []
    for sd in seeds:
        ser = cls.get((sd, n), {})
        if ser and 1950 in ser and max(ser.values()) > 0: vals.append(ser[1950] / max(ser.values()))
    r1[n] = st.median(vals) if vals else float("nan")
print("   " + "　".join(f"{n} {v:.2f}" for n, v in r1.items()) + f"　→ ≧0.5 の国 {sum(1 for v in r1.values() if v >= 0.5)}/6（線 5 以上）")
