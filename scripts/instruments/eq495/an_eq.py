# (495) 線 eq の読み手：計器の表 eq_s・eq_r と既存の表（factory_s・class_s・nation_s）から、事前登録 measurements/eq495_20261023_prereg.txt（追記 5）の
# C1（配りが設備になった）・C3a（配りと退場で民間の持分の合計が保たれる）・C3b（台帳の恒等）・D1（向き）・D2（配りが利幅に従う）・S2（発散）と後退の確かめ R1〜R3 を読む。読むだけ（シミュは回さない）。
# 使い方：PYTHONUTF8=1 python scripts/instruments/eq495/an_eq.py <計器の出力フォルダ>
# 限界：eq_s・eq_r は旗 RecipeEquipment が正の走りにしか出ない（A の走りでは空＝このスクリプトは〈表なし〉と言う）。eq_r は挙げた国だけ（毎四半期）。台帳の列は挙げた国の連続した四半期だけで比べる。
import csv, glob, math, os, re, statistics as st, sys, collections

d = sys.argv[1]

def rows(path):
    with open(path, encoding="utf-8") as fh:
        return list(csv.DictReader(fh, delimiter="	"))

def fnum(x):
    try: return float(x)
    except Exception: return float("nan")

seeds = sorted(int(re.search(r"_s(\d+)\.tsv$", p).group(1)) for p in glob.glob(os.path.join(d, "eq_s*.tsv")))
if not seeds:
    print("表なし（eq_s*.tsv が無い＝旗 RecipeEquipment が 0 の走り）"); sys.exit(0)
print(f"種 {seeds}")

# ---- C1・C3a・C3b・S2（eq_s） ----
c1_rows = c1_noinst = c1_neg = 0
c3a_n = 0; c3a_worst = 0.0; c3a_bad = 0; short_sum = 0
c3b_n = c3b_viol = c3b_skip = 0; c3b_worst = 0.0; c3b_rows = []
s2_ratio = 0.0; s2_nation = ''; scale_max_all = 0.0; ind_max_all = 0.0; scale_sum_max = 0.0; svc_max = 0.0
first_q = {}   # (seed, nation) -> 最初の行の四半期
eqs = {}       # (seed, nation) -> {quarter: row}
for sd in seeds:
    byn = collections.defaultdict(list)
    for r in rows(os.path.join(d, f"eq_s{sd}.tsv")):
        byn[r["nation"]].append(r)
    for n, rs in byn.items():
        rs.sort(key=lambda r: int(r["quarter"]))
        first_q[(sd, n)] = int(rs[0]["quarter"])
        eqs[(sd, n)] = {int(r["quarter"]): r for r in rs}
        prev = None
        first_ss = fnum(rs[0]["scaleSum"]) / max(1.0, fnum(rs[0]["eligibleCount"])) if rs else float("nan")
        for r in rs:
            pts, ins = fnum(r["pointsBought"]), fnum(r["equipInstalled"])
            per = fnum(r["scaleSum"]) / max(1.0, fnum(r["eligibleCount"]))   # 適格な個体 1 件あたり（領土の移り変わりで国の個体数が変わっても比べられる）
            if first_ss > 0 and per / first_ss > s2_ratio: s2_ratio = per / first_ss; s2_nation = f"{n}(種{sd}・四半期{r['quarter']})"
            ss, bs = fnum(r["scaleSum"]), fnum(r["builtSum"])
            scale_max_all = max(scale_max_all, fnum(r["scaleMax"])); ind_max_all = max(ind_max_all, fnum(r["indMax"])); scale_sum_max = max(scale_sum_max, ss)
            svc_max = max(svc_max, fnum(r["svcLaborShare"]))
            if pts > 0:
                c1_rows += 1
                if ins < -1e-12 or not math.isfinite(ins): c1_neg += 1
                if ins <= 0: c1_noinst += 1
            dm = fnum(r["distDriftMax"]); c3a_worst = max(c3a_worst, dm if math.isfinite(dm) else float("inf")); c3a_n += 1
            if not (dm <= 1e-9): c3a_bad += 1
            short_sum += int(fnum(r["renormShort"]))
            if prev is not None and int(r["quarter"]) == int(prev["quarter"]) + 1:
                if r["eligibleCount"] != prev["eligibleCount"] or r["activeRegionCount"] != prev["activeRegionCount"]:
                    c3b_skip += 1; prev = r; continue   # 領土の移り変わり（州が国をまたぐ）で個体・州の数が変わった四半期は国の合計では比べられない
                dS = ss - fnum(prev["scaleSum"])
                led = fnum(r["completedScale"]) - fnum(r["dismantledScale"]) + fnum(r["distNetScale"]) + fnum(r["retiredNetScale"])
                c3b_n += 1
                err = abs(dS - led) / max(1.0, abs(ss)); c3b_worst = max(c3b_worst, err)
                if err > 1e-6: c3b_viol += 1; c3b_rows.append((err, sd, n, int(r["quarter"]), dS, led, ss))
            prev = r
print(f"C1 配りが設備になった：購入のあった国×四半期 {c1_rows}・設備が足されなかった {c1_noinst}（{c1_noinst / max(1, c1_rows):.4%}）・負か非有限 {c1_neg}（0 が合格）")
print(f"C3a 配りと退場で民間の持分の合計が保たれる：国×四半期 {c3a_n} 行のうち 1 回ごとの相対差の最大が 1e-9 を超えた {c3a_bad} 行（0 が合格）・最悪 {c3a_worst:.3g}・薄めきれなかった回数の和 {short_sum}（記録）")
print(f"C3b 台帳の恒等：連続した四半期 {c3b_n} 件（個体・州の数が変わった {c3b_skip} 件は除く）のうち ΔscaleSum が書き手の台帳の和と相対 1e-6 を超えて違った {c3b_viol} 件（0 が合格）・最悪 {c3b_worst:.3g}")
for err, sd, n, q, dS, led, ss in sorted(c3b_rows, reverse=True)[:8]:
    print(f"   C3b の外れ（相対差 {err:.3g}）：種{sd} {n} 四半期{q} ΔScaleSum={dS:.6g} 台帳の和={led:.6g} ScaleSum={ss:.6g}")
print(f"S2 発散：適格な個体 1 件あたりの設備の合計÷最初の行の最大 {s2_ratio:.3f}（{s2_nation}・線 2 以下）・国の設備の合計の最大 {scale_sum_max:.4g}・設備の最大（個体）{scale_max_all:.4g}（記録）・州の在庫の最大 {ind_max_all:.4g}（線 10）")
print(f"記録：工員の取り合いの重みに占める役務の割合の最大 {svc_max:.3f}")

# ---- D1・D2（eq_r） ----
ser = collections.defaultdict(dict)   # (seed,nation,good,recipe) -> {quarter: (share, scale, open, marginSum)}
for sd in seeds:
    p = os.path.join(d, f"eqr_s{sd}.tsv")
    if not os.path.exists(p): continue
    for r in rows(p):
        ser[(sd, r["nation"], r["good"], r["recipe"])][int(r["quarter"])] = (fnum(r["shareOfGood"]), fnum(r["scaleSum"]), int(fnum(r["gateOpenRegions"])), fnum(r["marginSum"]))
by_good = collections.defaultdict(list)
for (sd, n, g, rc), s_ in ser.items():
    by_good[(sd, n, g)].append((rc, s_))
groups = []
for (sd, n, g), lst in by_good.items():
    fq = first_q.get((sd, n), 0)
    for rc, s_ in lst:
        opens = [q for q, v in s_.items() if v[2] > 0]
        if not opens: continue
        t0 = min(opens)
        if t0 <= fq: continue   # その国の計器の最初の行から開いているレシピは除く
        others = [v for rc2, v in lst if rc2 != rc and v.get(t0, (0, 0))[1] > 1e-6]
        if not others: continue   # 競う古いレシピがその時点に無い
        groups.append([s_.get(t0 + k, (float("nan"), 0, 0, 0))[0] for k in range(4, 44, 4)])
print(f"D1 向き：後から開いて古いレシピと競った組 {len(groups)}")
if len(groups) >= 30:
    med = []
    for i in range(10):
        vals = [g_[i] for g_ in groups if math.isfinite(g_[i])]
        med.append(st.median(vals) if vals else float("nan"))
    mono = all(not (math.isfinite(a) and math.isfinite(b)) or b >= a - 1e-3 for a, b in zip(med, med[1:]))
    rose = math.isfinite(med[0]) and math.isfinite(med[-1]) and med[-1] >= med[0] + 0.01
    print("   中央値（+4,+8,…,+40 四半期）：" + " ".join(f"{m:.3f}" for m in med) + f"　単調に増える={mono}・+40 が +4 より 0.01 以上大きい={rose}")
else:
    print("   判定不能（組が 30 未満＝番人で代える・採る判定は D2 と C1〜C3 に置く）")
# D2：購入のあった四半期（国営の完成・退場・廃棄なし）の利幅の割合と設備の割合の食い違いの向きに、次の四半期の設備の割合の変化が従う
d2_hi = d2_hi_ok = d2_lo = d2_lo_ok = 0
for (sd, n, g), lst in by_good.items():
    es = eqs.get((sd, n))
    if es is None: continue
    qs = sorted(set(q for rc, s_ in lst for q in s_))
    for q in qs:
        q1 = q + 1; r1 = es.get(q1)
        if r1 is None or fnum(r1["pointsBought"]) <= 0 or fnum(r1["equipInstalled"]) <= 0: continue
        if fnum(r1["completedScale"]) != 0 or fnum(r1["dismantledScale"]) != 0 or fnum(r1["retiredNetScale"]) != 0: continue
        cur = [(rc, s_.get(q), s_.get(q1)) for rc, s_ in lst if q in s_ and q1 in s_]
        if len(cur) < 2: continue
        mt = sum(c0[3] for _, c0, _ in cur)
        if not (mt > 1e-9): continue
        for rc, c0, c1 in cur:
            if c0[2] <= 0 or c1[2] <= 0: continue
            mshare = c0[3] / mt; dsh = c1[0] - c0[0]
            if not (math.isfinite(dsh) and math.isfinite(c0[0])): continue
            if mshare > c0[0] + 0.02:
                d2_hi += 1; d2_hi_ok += dsh > 0
            elif mshare < c0[0] - 0.02:
                d2_lo += 1; d2_lo_ok += dsh < 0
print(f"D2 配りが利幅に従う：利幅の割合＞設備の割合の行 {d2_hi}・次の四半期の設備の割合が増えた {d2_hi_ok / max(1, d2_hi):.3f}（線 0.7）／利幅の割合＜設備の割合の行 {d2_lo}・減った {d2_lo_ok / max(1, d2_lo):.3f}（線 0.7）")

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
