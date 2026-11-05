# -*- coding: utf-8 -*-
# (860) c71gt 門の台本 po（読むだけ・事前登録 measurements/m860_20261105_prereg_gt.txt・gates_po853.py の写しの G2b を直した物）
# 呼び方：python -X utf8 -I gates_po860.py <腕の ins> <基線 M′の ins> <ヌル腕1> <ヌル腕2> <出力> [種の数=10] [母集合の下限の倍率=1] [閾値の表=gates_po860_thresholds.tsv]
#   形の選び：python -X utf8 -I gates_po860.py --choose-form <基線> <N1> <N2> <出力の表> <閾値の表> [種の数=10]
#   一つ抜き：python -X utf8 -I gates_po860.py --loo <基線> <N1> <N2> <出力> [種の数=10] [閾値の表]
# 入力＝計器つきの生の secession_s（末尾 10 列〔orderRawNowIndex … ruleForeignBeforeShare〕あり）と rule_s（種 1〜N）。エンジンを呼ばない・世界へ書かない。
# 門：G0 点検・G1（ΔS＞W）・G1u（ΔS ≤ T＋W）・G2a（P_ctrl の実際の目標＝旧）・G2b（持続の窓の中で無統治の割合が窓の頭の値の減衰だけで説明できる）。G3 は記録。
# 853 からの直し：G2b を〈腕と基線の C の差 ≤ W_C〉から〈窓の頭の値の減衰を引いた残りが 0〉へ。
#   無統治の割合の更新は ung ← before×(1−SPEED)＋SPEED×t（G0 が点検する式）。窓の中の全ての行が Pexact（旧の目標も新の目標も 0）なら
#   ung(窓の末) ＝ before(窓の頭)×(1−SPEED)^L になる。残り＝ung(窓の末)−before(窓の頭)×(1−SPEED)^L の絶対値の平均が TOL 以下なら通る。
#   853 の C の差は、窓の前の持ち越し（旗 1 は order_r が 1 未満だった時期に Rule[3] を積む）と集合の差で、正しい実装でも W_C を超えた（(856)）。
import sys, os, math
SPEED = 0.025; TOL = 1e-8; G2B_TOL = 1e-8

def fl(s):
    try: return float(s)
    except ValueError: return float("nan")

def read(path):
    with open(path, "rb") as f:
        head = f.readline().decode("utf-8").rstrip("\r\n").split("\t"); ix = {c: i for i, c in enumerate(head)}
        rows = [raw.decode("utf-8").rstrip("\r\n").split("\t") for raw in f]
    return ix, rows

def qdiff(vals_keys):
    """州を key の昇順（同じ値は州の名）で並べ、上位 k − 下位 k の val の平均（k＝州数÷4・850 の qdiff と同じ）。vals_keys＝[(key, region, val)]。"""
    srt = sorted(vals_keys, key=lambda t: (t[0], t[1])); k = max(1, len(srt) // 4)
    return sum(t[2] for t in srt[-k:]) / k - sum(t[2] for t in srt[:k]) / k

NEWCOLS = ("orderRawNowIndex", "orderOldIndex", "militancyNationMeanIndex", "militancyRegionOwnIndex", "orderRegionNowIndex", "reachOwnShare",
           "ungovTargetOldShare", "ungovTargetNewShare", "ruleUngovBeforeShare", "ruleForeignBeforeShare")

def arm_stats(ins, nseed, Ls, mode="exact", Amins=()):
    """mode＝exact（正しい読み）または last（窓の長さを無視して Pexact の行を全て読む・固定の盤面で G2b を赤にするための突然変異の口）。
    Ls の各 L で：Cn（窓の行数）・C（ung の平均＝853 の C・記録）・R（残りの絶対値の平均）・Rmax・Rsg（残りの平均）。
    Amins の各 m で：An・A（形 a＝州の最初の行から Pexact が途切れない行のうち run≥m の ung の平均・記録）。"""
    S, T = [], []; g0n = g0ok = g2n = g2ok = foreign = mrok = mrn = 0
    csum = {L: 0.0 for L in Ls}; cn = {L: 0 for L in Ls}; rabs = {L: 0.0 for L in Ls}; rsg = {L: 0.0 for L in Ls}; rmax = {L: 0.0 for L in Ls}
    asum = {m: 0.0 for m in Amins}; an = {m: 0 for m in Amins}
    G3 = []; yearly_rows = 0; quarterly_rows = 0
    for seed in range(1, int(nseed) + 1):
        sp, rp = os.path.join(ins, "secession_s%d.tsv" % seed), os.path.join(ins, "rule_s%d.tsv" % seed)
        if not (os.path.exists(sp) and os.path.exists(rp)): raise SystemExit("欠け：%s／%s" % (sp, rp))
        rix, rrows = read(rp); six, srows = read(sp)
        if "ungovTargetNewShare" not in six: raise SystemExit("secession_s に末尾 10 列が無い：" + sp)
        rule = {}; natp = {}
        for a in rrows:
            pop, ung, fsh = fl(a[rix["popK"]]), fl(a[rix["ruleUngovShare"]]), fl(a[rix["ruleForeignShare"]])
            rule[(a[rix["quarter"]], a[rix["nation"]], a[rix["region"]])] = (ung, fsh)
            t = natp.setdefault((a[rix["quarter"]], a[rix["nation"]]), [0.0, 0.0]); t[0] += pop * ung; t[1] += pop
        for num, den in natp.values():
            if den > 0: G3.append(num / den)
        groups = {}; byreg = {}
        for a in srows:
            k = (a[six["quarter"]], a[six["nation"]], a[six["region"]])
            if k not in rule: continue
            ung, fsh = rule[k]
            r = {c: fl(a[six[c]]) for c in NEWCOLS}
            r.update(q=int(a[six["quarter"]]), region=a[six["region"]], mil=fl(a[six["militancyMeanIndex"]]), ung=ung, fsh=fsh)
            groups.setdefault((a[six["quarter"]], a[six["nation"]]), []).append(r)
            byreg.setdefault((a[six["nation"]], a[six["region"]]), []).append(r)
        for rows in groups.values():
            if len(rows) >= 4:
                S.append(qdiff([(r["mil"], r["region"], r["ung"]) for r in rows]))
                T.append(qdiff([(r["mil"], r["region"], r["ungovTargetNewShare"] - r["ungovTargetOldShare"]) for r in rows]))
            for r in rows:
                mrn += 1
                if r["militancyRegionOwnIndex"] == r["mil"] or abs(r["militancyRegionOwnIndex"] - r["mil"]) <= 1e-12 * max(1.0, abs(r["mil"])): mrok += 1
        for rows in byreg.values():
            rows.sort(key=lambda r: r["q"]); run = 0; prevq = None; unbroken = True
            for i, r in enumerate(rows):
                nanb = r["ruleUngovBeforeShare"] != r["ruleUngovBeforeShare"]
                pex = (not nanb and r["orderOldIndex"] >= 1 and r["orderRawNowIndex"] >= 1 and r["orderRegionNowIndex"] >= 1 and r["militancyRegionOwnIndex"] > 0
                       and r["ruleForeignBeforeShare"] == 0 and r["fsh"] == 0)
                consecutive = prevq is not None and r["q"] - prevq == 1
                if consecutive: quarterly_rows += 1
                else: yearly_rows += 1
                run = (run + 1 if consecutive else 1) if pex else 0
                unbroken = pex if i == 0 else (unbroken and pex and consecutive)
                prevq = r["q"]
                if not nanb:
                    if r["ruleForeignBeforeShare"] > 0 or r["fsh"] > 0: foreign += 1
                    else:
                        t_eng = (r["ung"] - r["ruleUngovBeforeShare"] * (1 - SPEED)) / SPEED
                        g0n += 1
                        if abs(t_eng - r["ungovTargetNewShare"]) <= TOL: g0ok += 1
                        if pex:
                            g2n += 1
                            if abs(t_eng - r["ungovTargetOldShare"]) <= TOL: g2ok += 1
                if pex:
                    for L in Ls:
                        if (run >= L) if mode == "exact" else (i - L + 1 >= 0):
                            res = r["ung"] - rows[i - L + 1]["ruleUngovBeforeShare"] * (1 - SPEED) ** L
                            csum[L] += r["ung"]; cn[L] += 1; rabs[L] += abs(res); rsg[L] += res
                            if abs(res) > rmax[L]: rmax[L] = abs(res)
                    if unbroken:
                        for m in Amins:
                            if run >= m: asum[m] += r["ung"]; an[m] += 1
    mean = lambda v: sum(v) / len(v) if v else float("nan")
    return dict(S=mean(S), T=mean(T), nS=len(S), g0n=g0n, g0ok=g0ok, g2n=g2n, g2ok=g2ok, foreign=foreign, mrok=mrok, mrn=mrn,
                C={L: (csum[L] / cn[L] if cn[L] else float("nan")) for L in Ls}, Cn=dict(cn),
                R={L: (rabs[L] / cn[L] if cn[L] else float("nan")) for L in Ls}, Rsg={L: (rsg[L] / cn[L] if cn[L] else float("nan")) for L in Ls}, Rmax=dict(rmax),
                A={m: (asum[m] / an[m] if an[m] else float("nan")) for m in Amins}, An=dict(an),
                G3=mean(G3), nG3=len(G3), yearly=yearly_rows, quarterly=quarterly_rows)

def read_thr(path):
    T = {}
    if path and os.path.exists(path):
        for line in open(path, encoding="utf-8"):
            if line.startswith("#") or "\t" not in line: continue
            k, v = line.rstrip("\r\n").split("\t")[:2]
            try: T[k] = float(v)
            except ValueError: pass
    return T

def run_gates(arm, base, n1, n2, nseed=10, scale=1.0, L=120, mode="exact", cache=None, g2b_tol=G2B_TOL, null_base=None):
    """門を数えて (ok, 行) を返す。cache＝{(ins, mode, L): arm_stats の結果}（旗 0 の 3 本を使い回す口）。
    null_base＝ヌル腕 n1・n2 の比べ先（既定は base）。旗 1 の実の生は盤面が m839・ヌル腕は m852 なので、ヌル腕の幅は m852 の M′ との差で数える（盤面が違うと名乗る）。"""
    nseed = int(nseed); scale = float(scale); L = int(L); cache = cache if cache is not None else {}
    nb = base if null_base is None else null_base
    def st(d, mm="exact"):
        k = (d, mm, L)
        if k not in cache: cache[k] = arm_stats(d, nseed, [L], mm)
        return cache[k]
    sb, s1, s2, sn = st(base), st(n1), st(n2), st(nb); sa = st(arm, mode)
    W = max(abs(s1["S"] - sn["S"]), abs(s2["S"] - sn["S"])); WC = max(abs(s1["C"][L] - sn["C"][L]), abs(s2["C"][L] - sn["C"][L]))
    WG = max(abs(s1["G3"] - sn["G3"]), abs(s2["G3"] - sn["G3"]))
    dS = sa["S"] - sb["S"]; dC = sa["C"][L] - sb["C"][L]; dG = sa["G3"] - sb["G3"]
    ok = {}; O = []; P = O.append
    r0 = sa["g0ok"] / sa["g0n"] if sa["g0n"] else float("nan")
    ok["G0"] = sa["g0n"] > 0 and r0 >= 0.9999
    ok["G1"] = dS > W; ok["G1u"] = dS <= sa["T"] + W
    r2 = sa["g2ok"] / sa["g2n"] if sa["g2n"] else float("nan")
    ok["G2a"] = sa["g2n"] >= 1000 * scale and r2 >= 0.9999
    ra, rb = sa["R"][L], sb["R"][L]
    ok["G2b"] = sa["Cn"][L] >= 1000 * scale and sb["Cn"][L] >= 1000 * scale and ra <= g2b_tol and rb <= g2b_tol
    P("(860) 門 po（腕 %s・基線 %s・ヌル腕の比べ先 %s・種 %d・母集合の下限の倍率 %.4g・窓 L＝%d・読み %s）" % (arm, base, nb, nseed, scale, L, mode))
    P("G0 点検 t_eng＝tNew %d/%d（%.6f）＝%s（外国の区分で除いた行 %d・m_r の一致 %d/%d）" % (sa["g0ok"], sa["g0n"], r0, "通る" if ok["G0"] else "落ちる", sa["foreign"], sa["mrok"], sa["mrn"]))
    P("G1  ΔS %.6g ＞ W %.6g ＝%s" % (dS, W, "通る" if ok["G1"] else "落ちる"))
    P("G1u ΔS %.6g ≤ T %.6g ＋ W %.6g ＝ %.6g ＝%s" % (dS, sa["T"], W, sa["T"] + W, "通る" if ok["G1u"] else "落ちる"))
    P("G2a P_ctrl %d 行（下限 %d）・t_eng＝tOld %d（%.6f）＝%s" % (sa["g2n"], int(1000 * scale), sa["g2ok"], r2, "通る" if ok["G2a"] else "落ちる"))
    P("G2b 窓 %d の正確な母集合：腕 %d 行・基線 %d 行（下限 %d）・残り（ung の末−窓の頭の before×(1−%.3g)^L）の絶対値の平均 腕 %.3g・基線 %.3g ≤ %.3g・腕の最大 %.3g ＝%s" % (
        L, sa["Cn"][L], sb["Cn"][L], int(1000 * scale), SPEED, ra, rb, g2b_tol, sa["Rmax"][L], "通る" if ok["G2b"] else "落ちる"))
    P("G2b0（記録・853 の形）C 腕 %.6g・C 基線 %.6g・|ΔC| %.6g（W_C %.6g）・母集合 腕/基線 %d/%d（持ち越しと集合の差で正しい実装でも W_C を超える〔(856)〕）" % (
        sa["C"][L], sb["C"][L], abs(dC), WC, sa["Cn"][L], sb["Cn"][L]))
    P("G3（記録）ΔG3 %.6g（ヌル腕の幅 %.6g）・四半期ごとの行 %d・間隔が 1 でない行 %d（年末の行だけの国は窓が作れず母集合に入らない）" % (dG, WG, sa["quarterly"], sa["yearly"]))
    P("門の判定：%s（%s）" % ("全て通る" if all(ok.values()) else "落ちた門あり", "・".join("%s=%s" % (k, "通" if v else "落") for k, v in ok.items())))
    return ok, O

FORM_ORDER = "c > a > b"

def choose_form(sb, s1, s2, Ls, Amins, floor, tol):
    """旗 0 の 3 本だけで形を選ぶ。規則（事前登録 [1]）：
       形 c（窓の頭の減衰を引いた残り）：3 本の全てで母集合 ≥ floor かつ残りの絶対値の平均 ≤ tol（旗 0 では目標 0 から代数で 0〔予言〕）。
       形 a（最初の行から Pexact が途切れない州×四半期）：3 本の全てで母集合 ≥ floor かつ ヌル腕の幅 W_C ÷ |C_base| ≤ 0.25。
       形 b（G2b を記録へ）：常に使える。順は c ＞ a ＞ b の最初に満たす形（c は閾値に雑音の幅が要らない＝ヌル腕の幅から閾値を合わせる必要がない）。"""
    arms = [sb, s1, s2]; rows = []
    cL = None
    for L in Ls:
        ns = [x["Cn"][L] for x in arms]; rs = [x["R"][L] for x in arms]
        okc = min(ns) >= floor and all(r == r and r <= tol for r in rs)
        rows.append(("c", L, ns, rs, okc))
        if okc: cL = L          # 満たす L の中で最大（母集合が下限を満たす限り長い窓＝持ち越しの影響が小さい側）
    aPick = None
    for m in Amins:
        ns = [x["An"][m] for x in arms]; cs = [x["A"][m] for x in arms]
        if min(ns) < floor or any(c != c for c in cs):
            rows.append(("a", m, ns, cs, False)); continue
        WCa = max(abs(cs[1] - cs[0]), abs(cs[2] - cs[0])); rel = WCa / abs(cs[0]) if cs[0] else float("inf")
        oka = rel <= 0.25
        rows.append(("a", m, ns, cs + [rel], oka))
        if oka and aPick is None: aPick = m
    if cL is not None: return ("c", cL), rows
    if aPick is not None: return ("a", aPick), rows
    return ("b", None), rows

def loo(sb, s1, s2, L, thr_tol):
    """旗 0 の 3 本を順に腕にした 6 通り（腕・基線・相方）：G1（ΔS＞W）が緑になる通り（雑音を〈働いた〉と読む空振り）と G2b が赤になる通り。"""
    dirs = {"M": sb, "N1": s1, "N2": s2}; res = []
    for a in dirs:
        for b in dirs:
            for c in dirs:
                if len({a, b, c}) != 3: continue
                dS = dirs[a]["S"] - dirs[b]["S"]; W = abs(dirs[c]["S"] - dirs[b]["S"])
                g2 = dirs[a]["Cn"][L] >= 1000 and dirs[b]["Cn"][L] >= 1000 and dirs[a]["R"][L] <= thr_tol and dirs[b]["R"][L] <= thr_tol
                res.append((a, b, c, dS > W, not g2))
    return res

def main():
    a = sys.argv[1:]
    if a and a[0] == "--choose-form":
        base, n1, n2, out, thr_out = a[1:6]; nseed = int(a[6]) if len(a) > 6 else 10
        Ls = [20, 40, 60, 80, 120]; Am = [1, 20, 40, 80]
        sb, s1, s2 = (arm_stats(d, nseed, Ls, "exact", Am) for d in (base, n1, n2))
        pick, rows = choose_form(sb, s1, s2, Ls, Am, 1000, G2B_TOL)
        O = ["(860) G2b の形の選び（旗 0 の M′・N1・N2・種 %d・規則の順＝%s・母集合の下限 1,000 行）" % (nseed, FORM_ORDER),
             "形\tL か m\t母集合（M′/N1/N2）\t値（c＝残りの絶対値の平均／a＝ung の平均と幅の比）\t満たす"]
        for f, k, ns, vs, okf in rows:
            O.append("%s\t%s\t%s\t%s\t%s" % (f, k, "/".join(map(str, ns)), "/".join("%.4g" % v for v in vs), "はい" if okf else "いいえ"))
        O.append("選んだ形：%s%s" % (pick[0], "" if pick[1] is None else "（L または m＝%d）" % pick[1]))
        O.append("粒度：形 c の判定は 3 本×10 種の全ての窓の行で |残り| の平均が 1e-8 以下かどうか（満たす／満たさないの 2 値）。形 a の幅の比は 3 本から取った 1 つの数。")
        open(out, "w", encoding="utf-8", newline="\n").write("\n".join(O) + "\n"); print("\n".join(O))
        T = ["# (860) gates_po860.py の閾値（旗 0 の M′・N1・N2〔keep_852・種 1〜%d〕から --choose-form が規則で置いた値）" % nseed]
        if pick[0] == "c":
            L = pick[1]
            T += ["form\t1\t形 c（窓の頭の減衰を引いた残り）", "persist_L\t%d\t窓の長さ（四半期）" % L, "G2b_tol\t%.3g\t残りの絶対値の平均の上限（G0 と同じ TOL）" % G2B_TOL,
                  "n_base\t%d\tM′ の母集合" % sb["Cn"][L], "R_base\t%.3g\tM′ の残りの絶対値の平均（記録）" % sb["R"][L],
                  "W_R\t%.3g\tmax(|R(N1)-R(M′)|,|R(N2)-R(M′)|)（記録）" % max(abs(s1["R"][L] - sb["R"][L]), abs(s2["R"][L] - sb["R"][L])),
                  "W_C0\t%.9g\t853 の形の幅 max(|C(N1)-C(M′)|,|C(N2)-C(M′)|)（記録）" % max(abs(s1["C"][L] - sb["C"][L]), abs(s2["C"][L] - sb["C"][L])),
                  "W_S\t%.9g\tmax(|S(N1)-S(M′)|,|S(N2)-S(M′)|)" % max(abs(s1["S"] - sb["S"]), abs(s2["S"] - sb["S"]))]
        else: T += ["no_go\t1\t形 c を満たさない（形 %s）" % pick[0]]
        open(thr_out, "w", encoding="utf-8", newline="\n").write("\n".join(T) + "\n")
        return 0 if pick[0] == "c" else 1
    if a and a[0] == "--loo":
        base, n1, n2, out = a[1:5]; nseed = int(a[5]) if len(a) > 5 else 10
        thr = read_thr(a[6] if len(a) > 6 else os.path.join(os.path.dirname(os.path.abspath(__file__)), "gates_po860_thresholds.tsv")); L = int(thr["persist_L"])
        sb, s1, s2 = (arm_stats(d, nseed, [L]) for d in (base, n1, n2))
        res = loo(sb, s1, s2, L, thr["G2b_tol"])
        O = ["(860) 旗 0 の 3 本の一つ抜き（腕・基線・相方・種 %d・窓 L＝%d）" % (nseed, L)]
        for x, y, z, g1, g2 in res: O.append("%s・%s・%s：G1（ΔS＞W）%s・G2b %s" % (x, y, z, "緑" if g1 else "赤", "赤" if g2 else "緑"))
        O.append("空振り：G1 が緑になった通り %d／6（対称な雑音 3 本では 3／6 が基準＝G1 は雑音を区別できない粒度 1/6）・G2b が赤になった通り %d／6" % (sum(1 for r in res if r[3]), sum(1 for r in res if r[4])))
        open(out, "w", encoding="utf-8", newline="\n").write("\n".join(O) + "\n"); print("\n".join(O)); return 0
    arm, base, n1, n2, out = a[:5]; nseed = a[5] if len(a) > 5 else 10; scale = a[6] if len(a) > 6 else 1.0
    thr = read_thr(a[7] if len(a) > 7 else os.path.join(os.path.dirname(os.path.abspath(__file__)), "gates_po860_thresholds.tsv"))
    if "persist_L" not in thr or "G2b_tol" not in thr: raise SystemExit("閾値の表に persist_L・G2b_tol が無い（no-go か未作成）")
    null_base = a[8] if len(a) > 8 else None
    ok, O = run_gates(arm, base, n1, n2, nseed, scale, int(thr["persist_L"]), "exact", None, thr["G2b_tol"], null_base)
    open(out, "w", encoding="utf-8", newline="\n").write("\n".join(O) + "\n"); print("\n".join(O)); return 0

if __name__ == "__main__":
    sys.exit(main())
