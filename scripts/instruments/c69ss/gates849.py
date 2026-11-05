# (849) c69ss 門の台本（読むだけ・事前登録 measurements/m849_20261105_prereg_ss.txt [3] の G1〜G5 と記録）
# 呼び方：python -X utf8 gates849.py <腕の ins> <基線の ins> <ヌル腕1> <ヌル腕2> <出力> [種の数=10] [母集合の下限の倍率=1]
#   旗 0 の側＝基線の同じ（種・四半期・国/州）・ヌル腕の幅 w＝基線・N1・N2 の s0 の最大−最小・s＝engineSub÷(need×担い手)。
#   突然変異の 1 種の腕は 種の数 1・倍率 0.1 で読む（門の式は同じ）。
import sys, os, collections

def fnum(s):
    try: return float(s)
    except ValueError: return float("nan")

def load(base, seed, s2):
    D = {}
    p = os.path.join(base, "subst_s%d.tsv" % seed)
    with open(p, encoding="utf-8") as f:
        hdr = f.readline().rstrip("\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
        has2 = "s2SigmaEngine" in ix
        for line in f:
            c = line.rstrip("\n").split("\t")
            sp = fnum(c[ix["subPopK"]])
            if not (sp > 0): continue
            need = fnum(c[ix["stapleNeedPerCapita"]]); eng = fnum(c[ix["engineSubOutputPerMonth"]]); nom = fnum(c[ix["nominalSubPerMonth"]])
            cap = fnum(c[ix["farmCapPerMonth"]]); sold = fnum(c[ix["farmSoldPerMonth"]])
            r = {"y": int(c[ix["year"]]), "sp": sp, "s": eng / (need * sp) if need * sp > 0 and eng == eng else float("nan"),
                 "d1": fnum(c[ix["unsoldD1Share"]]), "cap": cap, "sold": sold, "hf": eng / nom if nom > 0 else float("nan"),
                 "ms": fnum(c[ix["marketShareOfNeed"]])}
            if s2 and has2:
                for k in ("s2BasisKind", "s2RhoSource"): r[k] = int(c[ix[k]])
                for k in ("s2BasisEngine", "s2D0Inst", "s2RhoInst", "s2SigmaEngine", "s2SigmaInst", "s2TechHead", "s2Tech0Inst", "s2EffFillHead", "s2EffFill0Inst",
                          "s2BakeSigmaDiff", "s2BakeRhoRelDiff", "s2EEnd", "s2UnempEngine", "s2UnempB", "s2NewOverOld", "s2CapEnd"):
                    r[k] = fnum(c[ix[k]])
            D[(int(c[ix["quarter"]]), c[ix["nation"]] + "/" + c[ix["region"]])] = r
    return D, has2

def med(xs):
    xs = sorted(x for x in xs if x == x); n = len(xs)
    return float("nan") if n == 0 else xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])

def win(y):
    return "1783-85" if y <= 1785 else "1786-99" if y <= 1799 else "1800-49" if y <= 1849 else "1850-99" if y <= 1899 else "1900-50"

def main(arm, b, n1, n2, out, nseed=10, scale=1.0):
    nseed = int(nseed); scale = float(scale)
    C = collections.Counter(); L = collections.defaultdict(list); has2all = True
    for seed in range(1, nseed + 1):
        A, has2 = load(arm, seed, True); has2all = has2all and has2
        B = [load(x, seed, False)[0] for x in (b, n1, n2)]
        open0 = {}
        for (q, reg), r in A.items():
            if q == 0: open0[reg] = r
        baked = set()
        for (q, reg), r in A.items():
            # G3a（州ごとに最初の有限の点検）
            if has2 and r.get("s2BasisKind") == 2 and r.get("s2RhoSource") == 1 and reg not in baked and r["s2BakeRhoRelDiff"] == r["s2BakeRhoRelDiff"]:
                baked.add(reg); C["G3a 焼いた州"] += 1
                if r["s2BakeRhoRelDiff"] <= 1e-9 and r["s2BakeSigmaDiff"] <= 1e-9: C["G3a 一致"] += 1
            if r["hf"] == r["hf"]:
                C["G3c 行"] += 1
                if abs(r["hf"] - 1.0) > 1e-6: C["G3c hf≠1"] += 1
            if has2 and r.get("s2BasisKind") == 2 and r.get("s2RhoSource") == 1:
                se, si = r["s2SigmaEngine"], r["s2SigmaInst"]
                if si == si and se == se:
                    k = "G2 σ＝1" if si >= 1.0 else "G2 σ＜1"
                    C[k + " 行"] += 1
                    if abs(se - si) <= 1e-9: C[k + " 一致"] += 1
                e, ue, ub = r["s2EEnd"], r["s2UnempEngine"], r["s2UnempB"]
                if e == e and ue == ue:
                    if e > 0:
                        C["G4 E＞0 行"] += 1
                        if abs(ue - ub) <= 1e-9: C["G4 E＞0 一致"] += 1
                    else:
                        C["G4 E≦0 行"] += 1
                        if ue <= 1e-12: C["G4 E≦0 一致"] += 1
                L[("記録 ρ", win(r["y"]))].append(r["s2RhoInst"]); L[("記録 新÷旧", win(r["y"]))].append(r["s2NewOverOld"])
                L[("記録 T÷T0", win(r["y"]))].append(r["s2TechHead"] / r["s2Tech0Inst"] if r["s2Tech0Inst"] > 0 else float("nan"))
                L[("記録 EffFill", win(r["y"]))].append(r["s2EffFillHead"])
            if has2 and q == 0 and r.get("s2RhoSource") == 1: L[("記録 EffFill₀", "q0")].append(r["s2EffFill0Inst"])
            key = (q, reg)
            s0s = [x[key]["s"] for x in B if key in x]
            if len(s0s) < 3 or any(v != v for v in s0s) or r["s"] != r["s"]: C["基線とヌル腕のどれかに無い（外した）"] += 1; continue
            w = max(s0s) - min(s0s); d = r["s"] - s0s[0]
            L[("記録 市場の買いの割合 腕", win(r["y"]))].append(r["ms"]); L[("記録 市場の買いの割合 基線", win(r["y"]))].append(B[0][key]["ms"])
            if q == 0:
                C["G3b q0 行"] += 1
                if abs(d) <= w: C["G3b q0 内"] += 1
                continue
            o = open0.get(reg)
            if o is None or r["d1"] != r["d1"] or o["d1"] != o["d1"]: continue
            if r["d1"] < o["d1"]:
                C["G1 P1 行"] += 1; L["G1 d"].append(d); L["G1 w"].append(w)
                if d < -w: C["G1a 外"] += 1
                if has2 and r.get("s2BasisKind") == 2:
                    C["G1b P1 焼いた行"] += 1
                    if r["s2SigmaEngine"] == 0: C["G1b σ＝0"] += 1
                    if r["d1"] == 0: C["G1b D1＝0"] += 1
            if r["cap"] > 0 and o["cap"] > 0 and abs(r["sold"] / r["cap"] - o["sold"] / o["cap"]) <= 0.01:
                C["G5 P5 行"] += 1; L["G5 d"].append(d); L["G5 w"].append(w)
        del A, B
    OUT = []; P = OUT.append; ok = {}
    def rate(a, b): return C[a] / C[b] if C[b] else float("nan")
    P("(849) 門（腕 %s・基線 %s・N1 %s・N2 %s・種 %d・母集合の下限の倍率 %.2f・計器の末尾の列 %s）" % (arm, b, n1, n2, nseed, scale, "あり" if has2all else "なし"))
    g1a = rate("G1a 外", "G1 P1 行"); ok["G1a"] = g1a >= 0.5
    P("G1a 働いた（下側）：P1 %d・外 %d（%.4f）≧0.5 → %s・d 中央 %.4f・w 中央 %.4f" % (C["G1 P1 行"], C["G1a 外"], g1a, ok["G1a"], med(L["G1 d"]), med(L["G1 w"])))
    g1bs, g1bd = rate("G1b σ＝0", "G1b P1 焼いた行"), rate("G1b D1＝0", "G1b P1 焼いた行")
    ok["G1b"] = C["G1b P1 焼いた行"] > 0 and g1bs <= g1bd + 0.001
    P("G1b 上側：P1 の焼いた行 %d・σ＝0 %.5f ≦ D1＝0 %.5f＋0.001 → %s" % (C["G1b P1 焼いた行"], g1bs, g1bd, ok["G1b"]))
    ok["G2"] = (C["G2 σ＜1 行"] >= 100000 * scale and C["G2 σ＝1 行"] >= 100 * scale
                and C["G2 σ＜1 一致"] == C["G2 σ＜1 行"] and C["G2 σ＝1 一致"] == C["G2 σ＝1 行"])
    P("G2 実装の一致：σ＜1 %d 行（一致 %d・下限 %d）・σ＝1 %d 行（一致 %d・下限 %d） → %s" % (C["G2 σ＜1 行"], C["G2 σ＜1 一致"], int(100000 * scale), C["G2 σ＝1 行"], C["G2 σ＝1 一致"], int(100 * scale), ok["G2"]))
    ok["G3a"] = C["G3a 焼いた州"] >= 2000 * scale and C["G3a 一致"] == C["G3a 焼いた州"]
    P("G3a 焼いた月：焼いた州 %d（一致 %d・下限 %d） → %s" % (C["G3a 焼いた州"], C["G3a 一致"], int(2000 * scale), ok["G3a"]))
    g3b = rate("G3b q0 内", "G3b q0 行"); ok["G3b"] = g3b >= 0.5
    P("G3b 最初の四半期：q0 %d 行・幅の内 %d（%.4f）≧0.5 → %s" % (C["G3b q0 行"], C["G3b q0 内"], g3b, ok["G3b"]))
    g3c = rate("G3c hf≠1", "G3c 行"); ok["G3c"] = g3c >= 0.5
    P("G3c 収穫係数が効く：行 %d・|hf−1|＞1e-6 %d（%.4f）≧0.5 → %s" % (C["G3c 行"], C["G3c hf≠1"], g3c, ok["G3c"]))
    ok["G4"] = (C["G4 E＞0 行"] >= 400 * scale and C["G4 E≦0 行"] >= 100000 * scale
                and C["G4 E＞0 一致"] == C["G4 E＞0 行"] and C["G4 E≦0 一致"] == C["G4 E≦0 行"])
    P("G4 失業 (b′)：E＞0 %d 行（一致 %d・下限 %d）・E≦0 %d 行（一致 %d・下限 %d） → %s" % (C["G4 E＞0 行"], C["G4 E＞0 一致"], int(400 * scale), C["G4 E≦0 行"], C["G4 E≦0 一致"], int(100000 * scale), ok["G4"]))
    g5d, g5w = med(L["G5 d"]), med(L["G5 w"]); ok["G5"] = C["G5 P5 行"] > 0 and abs(g5d) <= g5w
    P("G5 対照：P5 %d 行・|d の中央| %.4f ≦ w の中央 %.4f → %s" % (C["G5 P5 行"], abs(g5d), g5w, ok["G5"]))
    P("外した行（基線とヌル腕のどれかに無い）：%d" % C["基線とヌル腕のどれかに無い（外した）"])
    P("門の判定：%s（%s）" % ("全て通る" if all(ok.values()) else "落ちた門あり", "・".join("%s=%s" % (k, "通" if v else "落") for k, v in ok.items())))
    P("-- 記録（門ではない）")
    for wn in ("1783-85", "1786-99", "1800-49", "1850-99", "1900-50"):
        P("  %s：ρ 中央 %.4f・新÷旧 中央 %.4f・T÷T0 中央 %.4f・EffFill 中央 %.4f・市場の買いの割合 腕 %.4f／基線 %.4f" % (
            wn, med(L[("記録 ρ", wn)]), med(L[("記録 新÷旧", wn)]), med(L[("記録 T÷T0", wn)]), med(L[("記録 EffFill", wn)]),
            med(L[("記録 市場の買いの割合 腕", wn)]), med(L[("記録 市場の買いの割合 基線", wn)])))
    e0 = L[("記録 EffFill₀", "q0")]
    P("  助走の終わりの EffFill（計器が焼いた州・q0）：行 %d・中央 %.4f・＞0 の割合 %.4f" % (len(e0), med(e0), sum(1 for x in e0 if x > 0) / len(e0) if e0 else float("nan")))
    with open(out, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(OUT) + "\n")
    print("\n".join(OUT))

if __name__ == "__main__":
    main(*sys.argv[1:8])
