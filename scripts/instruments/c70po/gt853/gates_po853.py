# -*- coding: utf-8 -*-
# (853) c70gt 門の台本 po（読むだけ・事前登録 measurements/m853_20261105_prereg_gt.txt [2]・gates850.py と p1_paper850.py〔(850) c69po〕の写しを直した物）
# 呼び方：python -X utf8 -I gates_po853.py <腕の ins> <基線 M′の ins> <ヌル腕1> <ヌル腕2> <出力> [種の数=10] [母集合の下限の倍率=1] [閾値の表=gates_po853_thresholds.tsv]
#   窓の長さの選び：python -X utf8 -I gates_po853.py --choose-L <基線> <N1> <N2> <出力の表> <閾値の表> [種の数=10]
# 入力＝計器つきの生の secession_s（末尾 10 列〔orderRawNowIndex … ruleForeignBeforeShare〕あり）と rule_s（種 1〜N）。エンジンを呼ばない・世界へ書かない。
# 門：G0 点検・G1（ΔS＞W）・G1u（ΔS ≤ T＋W）・G2a（P_ctrl の実際の目標＝旧）・G2b（正確な母集合の持続の窓の ruleUngovShare の平均）。G3 は記録。
# 850 からの直し：G2b の対照の母集合を近似（publicOrderIndex・R・m̄_a）でなく、同じ瞬間の正確な列 Pexact（G2a と同じ述語）で作り、持続の窓 L の全ての四半期（1 刻み）で Pexact を満たす州×四半期だけにする。
import sys, os, math, collections
SPEED = 0.025; TOL = 1e-8

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

def arm_stats(ins, nseed, Ls, mode="exact"):
    """mode＝exact（正しい読み）または last（最後の四半期だけ Pexact・固定の盤面で G2b を赤にするための突然変異の口）。"""
    S, T = [], []; g0n = g0ok = g2n = g2ok = foreign = mrok = mrn = 0
    csum = {L: 0.0 for L in Ls}; cn = {L: 0 for L in Ls}; G3 = []; yearly_rows = 0; quarterly_rows = 0
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
            rows.sort(key=lambda r: r["q"]); run = 0; prevq = None
            for r in rows:
                nanb = r["ruleUngovBeforeShare"] != r["ruleUngovBeforeShare"]
                pex = (not nanb and r["orderOldIndex"] >= 1 and r["orderRawNowIndex"] >= 1 and r["orderRegionNowIndex"] >= 1 and r["militancyRegionOwnIndex"] > 0
                       and r["ruleForeignBeforeShare"] == 0 and r["fsh"] == 0)
                consecutive = prevq is not None and r["q"] - prevq == 1
                if consecutive: quarterly_rows += 1
                else: yearly_rows += 1
                run = (run + 1 if consecutive else 1) if pex else 0
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
                        if (run >= L) if mode == "exact" else True:
                            csum[L] += r["ung"]; cn[L] += 1
    mean = lambda v: sum(v) / len(v) if v else float("nan")
    return dict(S=mean(S), T=mean(T), nS=len(S), g0n=g0n, g0ok=g0ok, g2n=g2n, g2ok=g2ok, foreign=foreign, mrok=mrok, mrn=mrn,
                C={L: (csum[L] / cn[L] if cn[L] else float("nan")) for L in Ls}, Cn=dict(cn), G3=mean(G3), nG3=len(G3), yearly=yearly_rows, quarterly=quarterly_rows)

def choose_L(sb, s1, s2, Ls, floor):
    """各 L で旗 0 の 3 本の C(L) から、（腕・基線・相方）6 通りの空振りの数を数え、下限を満たす L の中で最小・同数は長い方。"""
    trip = [(a, b, c) for a in range(3) for b in range(3) for c in range(3) if len({a, b, c}) == 3]
    arms = [sb, s1, s2]; table = []
    for L in Ls:
        cs = [x["C"][L] for x in arms]; ns = [x["Cn"][L] for x in arms]
        alarm = sum(1 for a, b, c in trip if abs(cs[a] - cs[b]) > abs(cs[c] - cs[b])) if all(c == c for c in cs) else None
        table.append((L, ns, cs, alarm, min(ns) >= floor and alarm is not None))
    cand = [t for t in table if t[4]]
    if not cand: return None, table
    best = min(t[3] for t in cand); pick = max(t[0] for t in cand if t[3] == best)
    return pick, table

def read_thr(path):
    T = {}
    if path and os.path.exists(path):
        for line in open(path, encoding="utf-8"):
            if line.startswith("#") or "\t" not in line: continue
            k, v = line.rstrip("\r\n").split("\t")[:2]
            try: T[k] = float(v)
            except ValueError: pass
    return T

def run_gates(arm, base, n1, n2, nseed=10, scale=1.0, L=40, mode="exact", cache=None):
    """門を数えて (ok, 行) を返す。cache＝{ins: arm_stats の結果}（旗 0 の 3 本を使い回す口）。"""
    nseed = int(nseed); scale = float(scale); L = int(L); cache = cache if cache is not None else {}
    def st(d, mm="exact"):
        k = (d, mm, L)
        if k not in cache: cache[k] = arm_stats(d, nseed, [L], mm)
        return cache[k]
    sb, s1, s2 = st(base), st(n1), st(n2); sa = st(arm, mode)
    W = max(abs(s1["S"] - sb["S"]), abs(s2["S"] - sb["S"])); WC = max(abs(s1["C"][L] - sb["C"][L]), abs(s2["C"][L] - sb["C"][L]))
    WG = max(abs(s1["G3"] - sb["G3"]), abs(s2["G3"] - sb["G3"]))
    dS = sa["S"] - sb["S"]; dC = sa["C"][L] - sb["C"][L]; dG = sa["G3"] - sb["G3"]
    ok = {}; O = []; P = O.append
    r0 = sa["g0ok"] / sa["g0n"] if sa["g0n"] else float("nan")
    ok["G0"] = sa["g0n"] > 0 and r0 >= 0.9999
    ok["G1"] = dS > W; ok["G1u"] = dS <= sa["T"] + W
    r2 = sa["g2ok"] / sa["g2n"] if sa["g2n"] else float("nan")
    ok["G2a"] = sa["g2n"] >= 1000 * scale and r2 >= 0.9999
    ok["G2b"] = sa["Cn"][L] >= 1000 * scale and sb["Cn"][L] >= 1000 * scale and abs(dC) <= WC
    P("(853) 門 po（腕 %s・基線 %s・種 %d・母集合の下限の倍率 %.4g・窓 L＝%d・読み %s）" % (arm, base, nseed, scale, L, mode))
    P("G0 点検 t_eng＝tNew %d/%d（%.6f）＝%s（外国の区分で除いた行 %d・m_r の一致 %d/%d）" % (sa["g0ok"], sa["g0n"], r0, "通る" if ok["G0"] else "落ちる", sa["foreign"], sa["mrok"], sa["mrn"]))
    P("G1  ΔS %.6g ＞ W %.6g ＝%s" % (dS, W, "通る" if ok["G1"] else "落ちる"))
    P("G1u ΔS %.6g ≤ T %.6g ＋ W %.6g ＝ %.6g ＝%s" % (dS, sa["T"], W, sa["T"] + W, "通る" if ok["G1u"] else "落ちる"))
    P("G2a P_ctrl %d 行（下限 %d）・t_eng＝tOld %d（%.6f）＝%s" % (sa["g2n"], int(1000 * scale), sa["g2ok"], r2, "通る" if ok["G2a"] else "落ちる"))
    P("G2b 窓 %d の正確な母集合：腕 %d 行・基線 %d 行（下限 %d）・C 腕 %.6g・C 基線 %.6g・|ΔC| %.6g ≤ W_C %.6g ＝%s" % (
        L, sa["Cn"][L], sb["Cn"][L], int(1000 * scale), sa["C"][L], sb["C"][L], abs(dC), WC, "通る" if ok["G2b"] else "落ちる"))
    P("G3（記録）ΔG3 %.6g（ヌル腕の幅 %.6g）・四半期ごとの行 %d・間隔が 1 でない行 %d（年末の行だけの国は窓が作れず母集合に入らない）" % (dG, WG, sa["quarterly"], sa["yearly"]))
    P("門の判定：%s（%s）" % ("全て通る" if all(ok.values()) else "落ちた門あり", "・".join("%s=%s" % (k, "通" if v else "落") for k, v in ok.items())))
    return ok, O

def main():
    a = sys.argv[1:]
    if a and a[0] == "--choose-L":
        base, n1, n2, out, thr_out = a[1:6]; nseed = int(a[6]) if len(a) > 6 else 10
        Ls = [20, 40, 60, 80, 120]
        sb, s1, s2 = (arm_stats(d, nseed, Ls) for d in (base, n1, n2))
        pick, table = choose_L(sb, s1, s2, Ls, 1000)
        O = ["(853) 窓の長さ L の選び（旗 0 の M′・N1・N2・種 %d・一つ抜き 6 通り・母集合の下限 1,000 行・規則＝空振りの数が最小の L・同数は長い方）" % nseed,
             "L\t母集合（M′/N1/N2）\tC（M′/N1/N2）\t空振りの数／6\t下限を満たす"]
        for L, ns, cs, al, okf in table:
            O.append("%d\t%s\t%s\t%s\t%s" % (L, "/".join(map(str, ns)), "/".join("%.6g" % c for c in cs), "-" if al is None else al, "はい" if okf else "いいえ"))
        if pick is None: O.append("選んだ L：なし（no-go＝G2b を門にしない）")
        else:
            WCs = max(abs(s1["C"][pick] - sb["C"][pick]), abs(s2["C"][pick] - sb["C"][pick]))
            O.append("選んだ L：%d・W_C＝%.6g" % (pick, WCs))
        O.append("粒度：3 本しか無いので空振りの数は 0〜6 の 7 値。年末の行だけの国は窓が作れず入らない（M′ の間隔が 1 でない行 %d・1 の行 %d）。" % (sb["yearly"], sb["quarterly"]))
        open(out, "w", encoding="utf-8", newline="\n").write("\n".join(O) + "\n"); print("\n".join(O))
        T = ["# (853) gates_po853.py の閾値（旗 0 の M′・N1・N2〔keep_852・種 1〜%d〕から --choose-L が規則で置いた値）" % nseed]
        if pick is not None:
            T += ["persist_L\t%d\t窓の長さ（四半期）" % pick, "W_C\t%.9g\tmax(|C(N1)-C(M′)|,|C(N2)-C(M′)|)" % WCs, "C_base\t%.9g\tM′ の C" % sb["C"][pick],
                  "n_base\t%d\tM′ の母集合" % sb["Cn"][pick], "W_S\t%.9g\tmax(|S(N1)-S(M′)|,|S(N2)-S(M′)|)" % max(abs(s1["S"] - sb["S"]), abs(s2["S"] - sb["S"]))]
        else: T += ["no_go\t1\t下限を満たす L が無い"]
        open(thr_out, "w", encoding="utf-8", newline="\n").write("\n".join(T) + "\n")
        return 0
    arm, base, n1, n2, out = a[:5]; nseed = a[5] if len(a) > 5 else 10; scale = a[6] if len(a) > 6 else 1.0
    thr = read_thr(a[7] if len(a) > 7 else os.path.join(os.path.dirname(os.path.abspath(__file__)), "gates_po853_thresholds.tsv"))
    if "persist_L" not in thr: raise SystemExit("閾値の表に persist_L が無い（no-go か未作成）")
    ok, O = run_gates(arm, base, n1, n2, nseed, scale, int(thr["persist_L"]))
    open(out, "w", encoding="utf-8", newline="\n").write("\n".join(O) + "\n"); print("\n".join(O)); return 0

if __name__ == "__main__":
    sys.exit(main())
