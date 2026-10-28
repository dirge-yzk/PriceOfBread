# -*- coding: utf-8 -*-
"""(640) 線 idn の読み手（事前登録 measurements/idn640_20261028_prereg.txt の [4] と 追記 3 の全ての判定の段）。
使い方：
  python -X utf8 -I an_idn640.py A <腕 A の ins>                     … 一貫性・G1（空でない門）・G2b（旧 G2 の復活）・旗の組の決定・R4・R7・部品の分布
  python -X utf8 -I an_idn640.py B <腕 B の ins> <基線 ins> <N1 ins> <N2 ins> <腕 A の ins> <旗の行 例 共産主義,社会主義>  … G2（新）・G3・G5・G6・G7 と R1〜R3・R5
すべて標準出力。国ごとの数・G7 は基線 m627・N1・N2 の同じ統計と並べる（RULE36）。
"""
import csv, glob, math, re, sys

sys.stdout.reconfigure(encoding="utf-8")
MAJ = ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "アメリカ", "日本", "清", "オスマン"]
HALF = 0.2
GATE = 0.15
NEAR0 = 10.0
Q_OPEN = 31
ROWS = [("社会主義", "wageUnmet", "needSoc", "Socialism"), ("共産主義", "sComNew", "needCom", "Communism"), ("ファシズム", "sFasNew", "needFas", "Fascism")]


def fl(v):
    try:
        return float(v)
    except Exception:
        return float("nan")


def med(xs):
    xs = sorted(x for x in xs if not math.isnan(x))
    return xs[len(xs) // 2] if xs else float("nan")


def seed_of(path):
    return int(re.search(r"s(\d+)\.tsv$", path).group(1))


def load(d, stem):
    out = {}
    for f in sorted(glob.glob(d + "/" + stem + "_s*.tsv")):
        rows = []
        with open(f, encoding="utf-8") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                rows.append(r)
        out[seed_of(f)] = rows
    return out


def pearson(xs, ys):
    n = len(xs)
    if n < 3:
        return float("nan")
    mx, my = sum(xs) / n, sum(ys) / n
    sxx = sum((x - mx) ** 2 for x in xs); syy = sum((y - my) ** 2 for y in ys)
    sxy = sum((x - mx) * (y - my) for x, y in zip(xs, ys))
    if sxx <= 1e-18 or syy <= 1e-18:
        return float("nan")
    return sxy / math.sqrt(sxx * syy)


def idn_rows(d):
    data = {}
    for s, rows in load(d, "idn").items():
        rr = []
        for r in rows:
            x = {k: fl(v) for k, v in r.items() if k != "nation"}
            x["nation"] = r["nation"]
            rr.append(x)
        data[s] = rr
    return data


def need(strength):
    return 70.0 * strength / (strength + HALF) if strength > 0 else 0.0


def opens(d):
    """disc_s の E 行（開いた）：{(seed, nation, row): quarter}（最初の1件）"""
    out = {}
    for s, rows in load(d, "disc").items():
        for r in rows:
            if r["kind"] != "E":
                continue
            k = (s, r["nation"], r["row"])
            q = int(float(r["quarter"]))
            if k not in out or q < out[k]:
                out[k] = q
    return out


def mode_a(d):
    data = idn_rows(d)
    if not data:
        print("idn_s が無い:", d); return
    seeds = sorted(data)
    print("# 腕 A:", d, "種:", seeds, "行数:", {s: len(v) for s, v in data.items()})
    print("\n== 一貫性：旗 0 のとき needSoc は 70×workerMil÷(workerMil+%g) と一致するか（解禁済み・前提待ちの行は除く）" % HALF)
    bad = tot = 0
    for s in seeds:
        for r in data[s]:
            if r["knownSoc"] == 1 or (r["techSoc"] == 0 and r["needSoc"] == 0 and r["transSoc"] == 0):
                continue
            tot += 1
            if abs(need(r["workerMil"]) - r["needSoc"]) > 1e-6:
                bad += 1
    print(f"  不一致 {bad}/{tot}（0 のはず）")
    print("\n== G1（空でない門）：全ての国×全ての四半期の新しい強度の最大（括弧＝今の強度の最大）と 0.15 に届いた種の数（通る＝3 種以上）")
    g1 = {}
    for name, col, _, _ in ROWS:
        reach = 0; line = []
        for s in seeds:
            vals = [r[col] for r in data[s] if not math.isnan(r[col])]
            mx = max(vals, default=float("nan"))
            oldc = "workerMil" if col != "sFasNew" else "sFasOld"
            old = max((r[oldc] for r in data[s] if not math.isnan(r[oldc])), default=float("nan"))
            if mx >= GATE:
                reach += 1
            line.append(f"s{s}:{mx:.3f}({old:.3f})")
        g1[name] = reach >= 3
        print(f"  {name}: 届いた種 {reach}/{len(seeds)} → G1 {'通る' if g1[name] else '通らない（空の門）'}   " + " ".join(line))
    print("\n== G2b（旧 G2 の復活・追記 3）：q0〜q%d の主要 9 国の候補の強度から作る必要性の項 70×s÷(s+%g) が %g 未満のセルの割合（通る＝50%% 以上）" % (Q_OPEN, HALF, NEAR0))
    g2b = {}
    for name, col, _, _ in ROWS:
        cells = [need(r[col]) for s in seeds for r in data[s] if r["nation"] in MAJ and r["quarter"] <= Q_OPEN and not math.isnan(r[col])]
        if not cells:
            g2b[name] = False; print(f"  {name}: セル無し"); continue
        frac = sum(1 for x in cells if x < NEAR0) / len(cells)
        g2b[name] = frac >= 0.5
        print(f"  {name}: {sum(1 for x in cells if x < NEAR0)}/{len(cells)} = {frac:.3f}  中央 {med(cells):.2f}  最大 {max(cells):.2f} → G2b {'通る' if g2b[name] else '通らない'}")
    print("\n== 旗の組の決定（追記 3 の規則：IdeologyDevTermOff=1 ＋ G1 と G2b の両方を通った行の旗）")
    flag_of = {"社会主義": "SpreadNeedSocialismBoard", "共産主義": "SpreadNeedCommunismBoard", "ファシズム": "SpreadNeedFascismDecline"}
    chosen = [flag_of[n] for n, _, _, _ in ROWS if g1[n] and g2b[n]]
    print("  通った行:", [n for n, _, _, _ in ROWS if g1[n] and g2b[n]])
    print("  POB_COEF=\"" + ";".join([c + "=1" for c in chosen] + ["IdeologyDevTermOff=1"]) + "\"")   # ★(645) 注：IdeologyDevTermOff は (640) の採用で消した旗＝再走するときはこの行を外す
    print("\n== G3 の基線の読み（記録）：主要 9 国×全四半期の s_soc と s_com の相関（種ごと）")
    for s in seeds:
        xs = [r["wageUnmet"] for r in data[s] if r["nation"] in MAJ]; ys = [r["sComNew"] for r in data[s] if r["nation"] in MAJ]
        print(f"  s{s}: r={pearson(xs, ys):.3f} (n={len(xs)})")
    print("\n== 部品の分布（主要 9 国の中央・年別）")
    hdr = ["wageShare", "wageUnmet", "workerSupplyFill", "ownerTake", "sComNew", "workerMil", "colonialShare", "crisis", "popMil", "decline", "sFasNew"]
    print("  年\t" + "\t".join(hdr))
    for y in [1783, 1790, 1800, 1820, 1840, 1860, 1880, 1900, 1920, 1940]:
        vals = []
        for h in hdr:
            xs = [r[h] for s in seeds for r in data[s] if r["nation"] in MAJ and int(r["year"]) == y]
            m = med(xs)
            vals.append("-" if math.isnan(m) else f"{m:.3f}")
        print(f"  {y}\t" + "\t".join(vals))
    print("\n== 共産主義の取り分の感度（記録・追記 3）：取り分を定数 0.5 に固定した強度 0.5×wageShare の最大と、実の sComNew の最大（種ごと）")
    for s in seeds:
        a = max((0.5 * r["wageShare"] for r in data[s] if not math.isnan(r["wageShare"])), default=float("nan"))
        b = max((r["sComNew"] for r in data[s] if not math.isnan(r["sComNew"])), default=float("nan"))
        print(f"  s{s}: 取り分固定 0.5 の最大 {a:.3f}  実 {b:.3f}")
    print("\n== R7：主要 9 国の工場労働者の充足の中央（国全体の supplyFillShare は nation_s と並べる）: %.3f" % med([r["workerSupplyFill"] for s in seeds for r in data[s] if r["nation"] in MAJ]))
    print("\n== R4 帝国主義：開幕（q≤3）の colonialShare＝0 の国の割合（全国）")
    q0 = [r for s in seeds for r in data[s] if r["quarter"] <= 3]
    if q0:
        z = sum(1 for r in q0 if r["colonialShare"] <= 0)
        print(f"  q≤3 の行 {len(q0)} 件のうち colonialShare＝0： {z} 件 ({z / len(q0):.3f})")


def first_open_stats(op, rowname, seeds):
    qs = []
    for s in seeds:
        c = [(q, k[1]) for k, q in op.items() if k[0] == s and k[2] == rowname]
        if c:
            qs.append(min(c))
    return qs


def price_stats(d):
    """nation_s の priceLevel：戦争・内戦の外の最大・非有限の数・1 四半期で 100 倍以上・1e10 倍以上の跳ねの数"""
    mx = 0.0; nonfin = 0; j100 = 0; j1e10 = 0
    for s, rows in load(d, "nation").items():
        prev = {}
        for r in rows:
            p = fl(r.get("priceLevel", "nan"))
            n = r["nation"]
            if math.isnan(p) or math.isinf(p):
                nonfin += 1
            else:
                if fl(r["civilWarMonths"]) == 0 and fl(r["atWarMonths"]) == 0 and p > mx:
                    mx = p
                if n in prev and prev[n] > 0:
                    ratio = p / prev[n]
                    if ratio >= 100: j100 += 1
                    if ratio >= 1e10: j1e10 += 1
            prev[n] = p if not (math.isnan(p) or math.isinf(p)) else prev.get(n, 0)
    return mx, nonfin, j100, j1e10


def mode_b(dB, dBase, dN1, dN2, dA, flagrows):
    flags = [x for x in flagrows.split(",") if x]
    data = idn_rows(dB)
    seedsB = sorted(data) if data else []
    print("# 腕 B:", dB, " 基線:", dBase, " N1:", dN1, " N2:", dN2, " A:", dA, " 旗の行:", flags)
    opB, opBase, opN1, opN2 = opens(dB), opens(dBase), opens(dN1), opens(dN2)
    S = list(range(1, 11))
    print("\n== G2（新・門が閉じている）：q≤%d に社会主義・共産主義・ファシズムの行を開いた 国×種（全ての国）  ※効くのは社会主義の旗だけ（共産主義は前提待ち・ファシズムの新しい強度は旧以下で作りの上で自明）" % Q_OPEN)
    cnt = {}
    for label, op in [("B", opB), ("m627", opBase), ("N1", opN1), ("N2", opN2)]:
        cnt[label] = sum(1 for k, q in op.items() if k[2] in ("社会主義", "共産主義", "ファシズム") and q <= Q_OPEN)
    print("  ", cnt, "→ G2", "通る" if cnt["B"] <= max(cnt["m627"], cnt["N1"], cnt["N2"]) else "通らない")
    print("\n== G3（別の量で動く）：主要 9 国の s_soc と s_com の相関（社会主義が未開の間・種ごと）通る＝10 種のうち 8 種以上で 0.9 未満")
    low = 0
    for s in seedsB:
        xs = []; ys = []
        for r in data[s]:
            if r["nation"] in MAJ and r["knownSoc"] == 0:
                xs.append(r["wageUnmet"]); ys.append(r["sComNew"])
        rr = pearson(xs, ys)
        if rr < 0.9: low += 1
        print(f"  s{s}: r={rr:.3f} (n={len(xs)})")
    print(f"  0.9 未満の種 {low}/{len(seedsB)} → G3 {'通る' if low >= 8 else '通らない'}")
    print("\n== G5（生きた門）：旗を入れた行ごとに、主要 9 国のどれかが行を開いた種の数（通る＝10 種のうち 5 種以上）／基線・N1・N2")
    for name in flags:
        c = {}
        for label, op in [("B", opB), ("m627", opBase), ("N1", opN1), ("N2", opN2)]:
            c[label] = sum(1 for s in S if any(k[0] == s and k[2] == name and k[1] in MAJ for k in op))
        print(f"  {name}: {c} → G5 {'通る' if c['B'] >= 5 else '通らない'}")
    print("\n== G6（順序）：共産主義が社会主義より先に開いた 国×種（0 のはず）と、ファシズムを開いた国の危機の履歴")
    for label, op in [("B", opB), ("m627", opBase)]:
        bad = sum(1 for k, q in op.items() if k[2] == "共産主義" and (k[0], k[1], "社会主義") in op and q < op[(k[0], k[1], "社会主義")])
        print(f"  {label}: 共産主義が社会主義より先 {bad} 件")
    nocr = 0; totf = 0
    for k, q in opB.items():
        if k[2] != "ファシズム" or k[0] not in data:
            continue
        rs = [r for r in data[k[0]] if r["nation"] == k[1] and r["quarter"] <= q]
        if not rs: continue
        totf += 1
        if not any(r["crisis"] > 0 for r in rs): nocr += 1
    print(f"  B のファシズムを開いた {totf} 件のうち、開くまでに危機が正だった四半期が無い {nocr} 件（記録）")
    print("\n== G7（止める線の機構の門）：戦争・内戦の外の価格水準の最大（腕 B ≤ max(基線, N1, N2)）・非有限 0・1e10 倍の跳ね 0")
    ps = {l: price_stats(d) for l, d in [("B", dB), ("m627", dBase), ("N1", dN1), ("N2", dN2)]}
    for l, (mx, nf, j100, j1e10) in ps.items():
        print(f"  {l}: 最大 {mx:.4g} 非有限 {nf} 100倍跳ね {j100} 1e10倍跳ね {j1e10}")
    ok = ps["B"][0] <= max(ps["m627"][0], ps["N1"][0], ps["N2"][0]) and ps["B"][1] == 0 and ps["B"][3] == 0
    print("  → G7（価格水準）", "通る" if ok else "通らない")
    print("\n== R1：思想 4 行の世界で最初に開く四半期（種ごとの最初の件の中央・暦の年は記録）")
    for name in ["社会主義", "共産主義", "帝国主義", "ファシズム"]:
        line = []
        for label, op in [("B", opB), ("m627", opBase), ("N1", opN1), ("N2", opN2)]:
            qs = first_open_stats(op, name, S)
            line.append(f"{label}: 種 {len(qs)}/10 最初の q の中央 {med([float(q) for q, _ in qs]):.0f}")
        print(f"  {name}: " + " | ".join(line))
    print("\n== R2：主要 9 国が行を開いた種の数（国ごと）  B／m627／N1／N2")
    for name in ["社会主義", "共産主義", "帝国主義", "ファシズム"]:
        print(f"  [{name}]")
        for n in MAJ:
            c = [sum(1 for s in S if (s, n, name) in op) for op in (opB, opBase, opN1, opN2)]
            print(f"    {n}: " + "／".join(str(x) for x in c))
    print("\n== R3：発見の四半期から国民の思想の割合が 10% に届くまでの四半期（国×種の中央）  B／m627／N1／N2")
    colmap = {"社会主義": "ideoNationalSocialismShare", "共産主義": "ideoNationalCommunismShare", "ファシズム": "ideoNationalFascismShare"}
    ideo = {l: load(d, "ideology") for l, d in [("B", dB), ("m627", dBase), ("N1", dN1), ("N2", dN2)]}
    ops = {"B": opB, "m627": opBase, "N1": opN1, "N2": opN2}
    for name, col in colmap.items():
        res = []
        for l in ["B", "m627", "N1", "N2"]:
            lags = []; never = 0
            for (s, n, rn), q in ops[l].items():
                if rn != name or n not in MAJ or s not in ideo[l]:
                    continue
                reached = None
                for r in ideo[l][s]:
                    if r["nation"] == n and int(float(r["quarter"])) >= q and fl(r[col]) >= 0.10:
                        reached = int(float(r["quarter"])) - q; break
                if reached is None: never += 1
                else: lags.append(float(reached))
            res.append(f"{l}: 中央 {med(lags):.0f} (届いた {len(lags)}・届かず {never})")
        print(f"  {name}: " + " | ".join(res))
    print("\n== R5：ファシズムの支持（ideoNationalFascismShare）の主要 9 国×全四半期の平均と最大（種の平均）  腕 B／基線 m627／N1／N2（IdeologyDevTermOff の効き）")
    for l in ["B", "m627", "N1", "N2"]:
        vals = []; mxs = []
        for s, rows in ideo[l].items():
            v = [fl(r["ideoNationalFascismShare"]) for r in rows if r["nation"] in MAJ]
            v = [x for x in v if not math.isnan(x)]
            if v:
                vals.append(sum(v) / len(v)); mxs.append(max(v))
        print(f"  {l}: 平均 {sum(vals) / max(1, len(vals)):.5f}  最大の中央 {med(mxs):.4f}")
    print("\n（R7・R8・R4・G1・G2b は 腕 A の読み＝mode A）")


if __name__ == "__main__":
    if len(sys.argv) >= 3 and sys.argv[1] == "A":
        mode_a(sys.argv[2])
    elif len(sys.argv) >= 8 and sys.argv[1] == "B":
        mode_b(*sys.argv[2:8])
    else:
        print(__doc__)
