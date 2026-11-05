# (860) c71gt 門の台本 ss（読むだけ・事前登録 measurements/m860_20261105_prereg_gt.txt [1-2]・gates_ss853.py〔(853) c70gt〕の写しの G4m の分母を揃えた物）
# 853 からの直し：G4m の候補の述語を閾値（mask_share）と同じ関数 cand に揃える（旗 0 では s2BasisKind が全行 0 なので門の旧の述語は旗 0 で数えられなかった）。G5 ほかの式は 1 字も変えない。
# 以下は 853 の注（849 からの直し）：
# 呼び方：python -X utf8 gates_ss860.py <腕の ins> <基線の ins> <ヌル腕1> <ヌル腕2> <出力> [種の数=10] [母集合の下限の倍率=1] [閾値の表=gates_ss860_thresholds.tsv]
#   一つ抜き ：python -X utf8 gates_ss860.py --loo <基線の ins> <ヌル腕1> <ヌル腕2> <出力> [種の数=10]
#   印の割合 ：python -X utf8 gates_ss860.py --mask-share <ins> [種の数=10]
# 849 からの直し（規則は事前登録 [1]）：
#   ① G4 の母集合から〈持ち主が変わった行〉を外す（region_s の nationIndex が同じ州の一つ前の subst_s の行と違う行・最初の行が q＞0 の行）＝Territory.Rehome が LastSold を 0 に戻し、計器が Rehome の後を読む行。
#   ② G4m：外した行の割合（E＞0 の候補のうち）≦ 閾値の表の G4m_limit（旗 0 の基線・N1・N2 の割合の最大の 3 倍）＝印を逃げ道に使う盤面を落とす。
#   ③ G3b・G3c は記録（門の表から外す・値は出し続ける）。
# 門：G1a・G1b・G2・G3a・G4・G4m・G5。
import sys, os, collections

def fnum(s):
    try: return float(s)
    except ValueError: return float("nan")

def read_header(f):
    hdr = f.readline().rstrip("\r\n").split("\t"); return {h: i for i, h in enumerate(hdr)}

def owners(base, seed):
    """region_s の (quarter, region) → nationIndex（文字列）。"""
    D = {}
    p = os.path.join(base, "region_s%d.tsv" % seed)
    if not os.path.exists(p): return None
    with open(p, encoding="utf-8") as f:
        ix = read_header(f); iq, ir, ii = ix["quarter"], ix["region"], ix["nationIndex"]
        for line in f:
            c = line.rstrip("\r\n").split("\t"); D[(int(c[iq]), c[ir])] = sys.intern(c[ii])
    return D

def load_arm(base, seed, with_owner=True):
    """腕の subst_s（全ての列）。masked＝持ち主が変わった行（region_s が無ければ印を作れない＝owner_ok を偽にする）。"""
    D = {}
    own = owners(base, seed) if with_owner else None
    p = os.path.join(base, "subst_s%d.tsv" % seed)
    last = {}; lastq = -1
    with open(p, encoding="utf-8") as f:
        ix = read_header(f)
        has2 = "s2SigmaEngine" in ix
        for line in f:
            c = line.rstrip("\r\n").split("\t")
            sp = fnum(c[ix["subPopK"]])
            if not (sp > 0): continue
            q = int(c[ix["quarter"]]); reg = c[ix["region"]]
            if q < lastq: raise SystemExit("subst_s の行が四半期の順でない：" + p)
            lastq = q
            need = fnum(c[ix["stapleNeedPerCapita"]]); eng = fnum(c[ix["engineSubOutputPerMonth"]]); nom = fnum(c[ix["nominalSubPerMonth"]])
            cap = fnum(c[ix["farmCapPerMonth"]]); sold = fnum(c[ix["farmSoldPerMonth"]])
            r = {"y": int(c[ix["year"]]), "sp": sp, "s": eng / (need * sp) if need * sp > 0 and eng == eng else float("nan"),
                 "d1": fnum(c[ix["unsoldD1Share"]]), "cap": cap, "sold": sold, "hf": eng / nom if nom > 0 else float("nan"),
                 "ms": fnum(c[ix["marketShareOfNeed"]]), "masked": False}
            if own is not None:
                o = own.get((q, reg))
                if o is None: r["masked"] = True   # region_s に行が無い＝持ち主が分からない行は外す（件数を名乗る）
                else:
                    pv = last.get(reg)
                    r["masked"] = (pv is None and q > 0) or (pv is not None and pv != o)
                    last[reg] = o
            if has2:
                for k in ("s2BasisKind", "s2RhoSource"): r[k] = int(c[ix[k]])
                for k in ("s2BasisEngine", "s2D0Inst", "s2RhoInst", "s2SigmaEngine", "s2SigmaInst", "s2TechHead", "s2Tech0Inst", "s2EffFillHead", "s2EffFill0Inst",
                          "s2BakeSigmaDiff", "s2BakeRhoRelDiff", "s2EEnd", "s2UnempEngine", "s2UnempB", "s2NewOverOld", "s2CapEnd"):
                    r[k] = fnum(c[ix[k]])
            D[(q, c[ix["nation"]] + "/" + reg)] = r
    return D, has2, own is not None

def load_light(base, seed):
    """基線・ヌル腕の subst_s（必要な列だけ）：キー(q, 国/州) → (s, d1, cap, sold, ms, year)。"""
    D = {}
    with open(os.path.join(base, "subst_s%d.tsv" % seed), encoding="utf-8") as f:
        ix = read_header(f)
        a, b, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10 = (ix[k] for k in ("subPopK", "stapleNeedPerCapita", "engineSubOutputPerMonth", "farmCapPerMonth", "farmSoldPerMonth",
            "unsoldD1Share", "marketShareOfNeed", "year", "quarter", "nation", "region", "subPopK"))
        for line in f:
            c = line.rstrip("\r\n").split("\t")
            sp = fnum(c[a])
            if not (sp > 0): continue
            need = fnum(c[b]); eng = fnum(c[c1])
            s = eng / (need * sp) if need * sp > 0 and eng == eng else float("nan")
            D[(int(c[c7]), c[c8] + "/" + c[c9])] = (s, fnum(c[c4]), fnum(c[c2]), fnum(c[c3]), fnum(c[c5]), int(c[c6]))
    return D

def cand(r):
    """G4m の候補の述語（閾値の mask_share と門の run_gates が同じこの関数を呼ぶ）：s2RhoSource＝1 かつ s2EEnd が有限（s2BasisKind は問わない）。"""
    return r.get("s2RhoSource") == 1 and r.get("s2EEnd", float("nan")) == r.get("s2EEnd", float("nan"))

def med(xs):
    xs = sorted(x for x in xs if x == x); n = len(xs)
    return float("nan") if n == 0 else xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])

def win(y):
    return "1783-85" if y <= 1785 else "1786-99" if y <= 1799 else "1800-49" if y <= 1849 else "1850-99" if y <= 1899 else "1900-50"

def read_thr(path):
    T = {}
    if path and os.path.exists(path):
        with open(path, encoding="utf-8") as f:
            for line in f:
                if line.startswith("#") or "\t" not in line: continue
                k, v = line.rstrip("\r\n").split("\t")[:2]
                try: T[k] = float(v)
                except ValueError: pass
    return T

def run_gates(arm, b, n1, n2, nseed=10, scale=1.0, thr=None, mask=True):
    """門を数えて (ok の辞書, 数え, 出力の行) を返す。mask＝偽は 849 の旧の G4（持ち主が変わった行を外さない・G4m なし）＝固定の盤面で旧の型を赤にするためだけの口。"""
    nseed = int(nseed); scale = float(scale); thr = thr or {}
    C = collections.Counter(); L = collections.defaultdict(list); has2all = True; ownall = True
    for seed in range(1, nseed + 1):
        A, has2, own_ok = load_arm(arm, seed, mask); has2all = has2all and has2; ownall = ownall and (own_ok or not mask)
        B = [load_light(x, seed) for x in (b, n1, n2)]
        open0 = {}
        for (q, reg), r in A.items():
            if q == 0: open0[reg] = r
        baked = set()
        for (q, reg), r in A.items():
            if has2 and r.get("s2BasisKind") == 2 and r.get("s2RhoSource") == 1 and reg not in baked and r["s2BakeRhoRelDiff"] == r["s2BakeRhoRelDiff"]:
                baked.add(reg); C["G3a 焼いた州"] += 1
                if r["s2BakeRhoRelDiff"] <= 1e-9 and r["s2BakeSigmaDiff"] <= 1e-9: C["G3a 一致"] += 1
            if r["hf"] == r["hf"]:
                C["G3c 行"] += 1
                if abs(r["hf"] - 1.0) > 1e-6: C["G3c hf≠1"] += 1
            if has2 and cand(r) and r["s2EEnd"] > 0:
                C["G4m 候補 E＞0"] += 1
                if r["masked"]: C["G4m 外した E＞0"] += 1
            if has2 and r.get("s2BasisKind") == 2 and r.get("s2RhoSource") == 1:
                se, si = r["s2SigmaEngine"], r["s2SigmaInst"]
                if si == si and se == se:
                    k = "G2 σ＝1" if si >= 1.0 else "G2 σ＜1"
                    C[k + " 行"] += 1
                    if abs(se - si) <= 1e-9: C[k + " 一致"] += 1
                e, ue, ub = r["s2EEnd"], r["s2UnempEngine"], r["s2UnempB"]
                if e == e and ue == ue:
                    pos = e > 0
                    C["G4 候補 E＞0" if pos else "G4 候補 E≦0"] += 1
                    if r["masked"]:
                        C["G4 外した E＞0" if pos else "G4 外した E≦0"] += 1
                    elif pos:
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
            s0s = [x[key][0] for x in B if key in x]
            if len(s0s) < 3 or any(v != v for v in s0s) or r["s"] != r["s"]: C["基線とヌル腕のどれかに無い（外した）"] += 1; continue
            w = max(s0s) - min(s0s); d = r["s"] - s0s[0]
            L[("記録 市場の買いの割合 腕", win(r["y"]))].append(r["ms"]); L[("記録 市場の買いの割合 基線", win(r["y"]))].append(B[0][key][4])
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
    def rate(a, bb): return C[a] / C[bb] if C[bb] else float("nan")
    P("(860) 門 ss（腕 %s・基線 %s・N1 %s・N2 %s・種 %d・母集合の下限の倍率 %.4g・計器の末尾の列 %s・region_s %s）" % (arm, b, n1, n2, nseed, scale, "あり" if has2all else "なし", "あり" if ownall else "なし"))
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
    ok["G4"] = (ownall and C["G4 E＞0 行"] >= 400 * scale and C["G4 E≦0 行"] >= 100000 * scale
                and C["G4 E＞0 一致"] == C["G4 E＞0 行"] and C["G4 E≦0 一致"] == C["G4 E≦0 行"])
    P("G4 失業 (b′)（持ち主が変わった行を外した後）：E＞0 %d 行（一致 %d・下限 %d）・E≦0 %d 行（一致 %d・下限 %d）・外した E＞0 %d・外した E≦0 %d → %s"
      % (C["G4 E＞0 行"], C["G4 E＞0 一致"], int(400 * scale), C["G4 E≦0 行"], C["G4 E≦0 一致"], int(100000 * scale), C["G4 外した E＞0"], C["G4 外した E≦0"], ok["G4"]))
    lim = thr.get("G4m_limit", float("nan")); sh = rate("G4m 外した E＞0", "G4m 候補 E＞0")
    ok["G4m"] = (not mask) or (ownall and lim == lim and (sh == sh) and sh <= lim)
    P("G4m 印の乱用の止め（候補＝RhoSource＝1 かつ E 有限・BasisKind を問わない）：外した E＞0 の割合 %.5f（%d／%d）≦ 閾値 %.5f → %s" % (sh, C["G4m 外した E＞0"], C["G4m 候補 E＞0"], lim, ok["G4m"]))
    g5d, g5w = med(L["G5 d"]), med(L["G5 w"]); ok["G5"] = C["G5 P5 行"] > 0 and abs(g5d) <= g5w
    P("G5 対照：P5 %d 行・|d の中央| %.4f ≦ w の中央 %.4f → %s" % (C["G5 P5 行"], abs(g5d), g5w, ok["G5"]))
    P("外した行（基線とヌル腕のどれかに無い）：%d" % C["基線とヌル腕のどれかに無い（外した）"])
    P("門の判定：%s（%s）" % ("全て通る" if all(ok.values()) else "落ちた門あり", "・".join("%s=%s" % (k, "通" if v else "落") for k, v in ok.items())))
    P("-- 記録（門ではない）")
    g3b = rate("G3b q0 内", "G3b q0 行"); g3c = rate("G3c hf≠1", "G3c 行")
    P("G3b（記録）最初の四半期：q0 %d 行・幅の内 %d（%.4f）" % (C["G3b q0 行"], C["G3b q0 内"], g3b))
    P("G3c（記録）収穫係数が効く：行 %d・|hf−1|＞1e-6 %d（%.4f）" % (C["G3c 行"], C["G3c hf≠1"], g3c))
    for wn in ("1783-85", "1786-99", "1800-49", "1850-99", "1900-50"):
        P("  %s：ρ 中央 %.4f・新÷旧 中央 %.4f・T÷T0 中央 %.4f・EffFill 中央 %.4f・市場の買いの割合 腕 %.4f／基線 %.4f" % (
            wn, med(L[("記録 ρ", wn)]), med(L[("記録 新÷旧", wn)]), med(L[("記録 T÷T0", wn)]), med(L[("記録 EffFill", wn)]),
            med(L[("記録 市場の買いの割合 腕", wn)]), med(L[("記録 市場の買いの割合 基線", wn)])))
    e0 = L[("記録 EffFill₀", "q0")]
    P("  助走の終わりの EffFill（計器が焼いた州・q0）：行 %d・中央 %.4f・＞0 の割合 %.4f" % (len(e0), med(e0), sum(1 for x in e0 if x > 0) / len(e0) if e0 else float("nan")))
    return ok, C, OUT

def mask_share(ins, nseed=10):
    """旗 0 の生で印の割合を数える（G4m の閾値の元・候補＝RhoSource＝1 かつ E が有限の行のうち E＞0）。"""
    C = collections.Counter()
    for seed in range(1, int(nseed) + 1):
        A, has2, own_ok = load_arm(ins, seed)
        if not own_ok: raise SystemExit("region_s が無い：" + ins)
        for r in A.values():
            if has2 and cand(r):
                C["候補"] += 1
                if r["s2EEnd"] > 0:
                    C["候補 E＞0"] += 1
                    if r["masked"]: C["外した E＞0"] += 1
                elif r["masked"]: C["外した E≦0"] += 1
        del A
    return C

def loo(base, n1, n2, nseed=10):
    """旗 0 の 3 本を順に腕にした 6 通りの G1a の割合と G5（腕・基線・相方の順・幅＝|基線−相方|）。"""
    dirs = {"M": base, "N1": n1, "N2": n2}
    triples = [(a, b, c) for a in dirs for b in dirs for c in dirs if len({a, b, c}) == 3]
    acc = {t: collections.Counter() for t in triples}; L = {t: collections.defaultdict(list) for t in triples}
    for seed in range(1, int(nseed) + 1):
        D = {k: load_light(v, seed) for k, v in dirs.items()}
        for t in triples:
            a, b, c = t; A, Bb, Cc = D[a], D[b], D[c]
            open0 = {reg: r for (q, reg), r in A.items() if q == 0}
            for (q, reg), r in A.items():
                if q == 0 or (q, reg) not in Bb or (q, reg) not in Cc: continue
                sa, sb, sc = r[0], Bb[(q, reg)][0], Cc[(q, reg)][0]
                if sa != sa or sb != sb or sc != sc: continue
                w = abs(sb - sc); d = sa - sb
                o = open0.get(reg)
                if o is None or r[1] != r[1] or o[1] != o[1]: continue
                if r[1] < o[1]:
                    acc[t]["P1"] += 1
                    if d < -w: acc[t]["G1a 外"] += 1
                if r[2] > 0 and o[2] > 0 and abs(r[3] / r[2] - o[3] / o[2]) <= 0.01:
                    acc[t]["P5"] += 1; L[t]["d"].append(d); L[t]["w"].append(w)
        del D
    res = []
    for t in triples:
        c = acc[t]; g1a = c["G1a 外"] / c["P1"] if c["P1"] else float("nan"); g5d, g5w = med(L[t]["d"]), med(L[t]["w"])
        res.append((t, c["P1"], g1a, g1a >= 0.5, abs(g5d), g5w, abs(g5d) <= g5w))
    return res

def main():
    a = sys.argv[1:]
    if a and a[0] == "--mask-share":
        C = mask_share(a[1], a[2] if len(a) > 2 else 10)
        sh = C["外した E＞0"] / C["候補 E＞0"] if C["候補 E＞0"] else float("nan")
        print("印の割合 %s：候補 %d・候補 E＞0 %d・外した E＞0 %d（%.6f）・外した E≦0 %d" % (a[1], C["候補"], C["候補 E＞0"], C["外した E＞0"], sh, C["外した E≦0"]))
        return 0
    if a and a[0] == "--loo":
        base, n1, n2, out = a[1:5]; nseed = a[5] if len(a) > 5 else 10
        res = loo(base, n1, n2, nseed); lines = ["(860) 旗 0 の 3 本の一つ抜き（腕・基線・相方・G1a は P1 の外の割合≧0.5・G5 は |d の中央|≦w の中央）種 %s" % nseed]
        g1 = g5 = 0
        for t, p1, g1a, okg1, d5, w5, okg5 in res:
            lines.append("%s・%s・%s：P1 %d・G1a %.4f → %s・G5 |d| %.5f ≦ w %.5f → %s" % (t[0], t[1], t[2], p1, g1a, "緑" if okg1 else "赤", d5, w5, "緑" if okg5 else "赤"))
            g1 += okg1; g5 += okg5
        lines.append("空振り（緑になった通り）：G1a %d／6・G5 %d／6（粒度は 1/6）" % (g1, g5))
        with open(out, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(lines) + "\n")
        print("\n".join(lines)); return 0
    arm, b, n1, n2, out = a[:5]; nseed = a[5] if len(a) > 5 else 10; scale = a[6] if len(a) > 6 else 1.0
    thr = read_thr(a[7] if len(a) > 7 else os.path.join(os.path.dirname(os.path.abspath(__file__)), "gates_ss860_thresholds.tsv"))
    ok, C, OUT = run_gates(arm, b, n1, n2, nseed, scale, thr)
    with open(out, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(OUT) + "\n")
    print("\n".join(OUT)); return 0

if __name__ == "__main__":
    sys.exit(main())
