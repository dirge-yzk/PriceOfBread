# (877) c73ts 門の台本（自給の段 2′ Σ2′＝技術の比 T を自給の 3 か所から外す・事前登録 measurements/m877_20261106_prereg_ts.txt）
# 写しの元：scripts/instruments/c72gt/gates_ss867.py（(867) c72gt）。G1a・G1b・G4m の式は 1 字も変えずに写した（関数 run_gates の該当行）。
# 867 からの直し（事前登録 [2] に diff を引く）：
#   ・計器 subst_s の s2 の列は S1-a の式（T を掛ける）で書かれる（InstrumentsSubst.cs は触れない）＝Σ2′ の量は台本が列から数える：
#       ρ′＝s2RhoInst×s2Tech0Inst（計器が焼いた ρ を T0 で戻す＝d0×need×n÷U）
#       σ′＝min(1, s2SigmaInst×s2Tech0Inst÷s2TechHead)（s2SigmaInst＜1 のとき＝張り付かない行）・s2SigmaInst≧1 かつ T≦T0 なら σ′＝1・s2SigmaInst≧1 かつ T＞T0 は〈決まらない行〉（σ′∈[T0÷T, 1]）
#       ub′＝clamp01((s2UEnd−need×s2HeadsEnd÷ρ′)÷s2CapEnd)（能力 0 以下は 0）・E′＝s2UEnd−need×s2HeadsEnd÷ρ′
#   ・G2：エンジンの σ（s2SigmaEngine）と σ′ が 1e-9（決まらない行は別勘定：σ_e が [T0÷T−1e-9, 1+1e-9] の内）。
#   ・G3a：ρ の点検を |s2BasisEngine÷ρ′−1|≦1e-9 へ（S1-a の s2BakeRhoRelDiff は旗 1 で |T0−1| になる）。s2BakeSigmaDiff（焼いた月の σ＝d0）はそのまま。
#   ・G4：s2EEnd・s2UnempB を E′・ub′ へ（S1-a の lneed は T を掛ける）。母集合と印の外しは同じ。
#   ・G5b：dB＝d0×(H_腕−H_基線)（867 の dB＝H_腕×d0−s_基線 は、867 の基線〔段 2 の旗 0＝s_基線＝H_基線×d0〕では同じ値・m873 の基線は S1-a＝s_基線に σ の項が残るので書き直した）。
#   ・G5p：紙の x を Σ2′ の x′＝H×(σ′−d0) へ（決まらない行は nan＝外す）。閾値の wp はヌル 3 本の x′ の範囲。
#   ・GC（新しい）：商業化の向き（両側＋上限）。・対照（新しい）。G4m の閾値は m873 の 3 本で数え直す（規則は同じ＝3 本の割合の最大の 3 倍）。
# 呼び方：
#   門            ：python -X utf8 gates_ts877.py <腕の ins> <基線の ins> <ヌル腕1> <ヌル腕2> <出力> [種の数=10] [閾値の表=gates_ts877_thresholds.tsv] [種の起点=1]
#   閾値の表      ：python -X utf8 gates_ts877.py --make-thresholds <基線> <N1> <N2> <出力の表> [種の数=10]
#   紙の突然変異  ：python -X utf8 gates_ts877.py --paper-mut <基線> <N1> <N2> <出力> [種の数=10] [閾値の表] [突然変異の一覧=OK,Mu0,Mu1,Mu2,Mu3,Mu5,Mu6]
import sys, os, collections, math

NEED_TOL = 1e-9
WINS = ("1783-85", "1786-99", "1800-49", "1850-99", "1900-50")

def fnum(s):
    try: return float(s)
    except ValueError: return float("nan")

def isf(x): return x == x and x not in (float("inf"), float("-inf"))

def read_header(f):
    hdr = f.readline().rstrip("\r\n").split("\t"); return {h: i for i, h in enumerate(hdr)}

def win(y):
    return "1783-85" if y <= 1785 else "1786-99" if y <= 1799 else "1800-49" if y <= 1849 else "1850-99" if y <= 1899 else "1900-50"

def med(xs):
    xs = sorted(x for x in xs if x == x); n = len(xs)
    return float("nan") if n == 0 else xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])

def sigma2(si, t0, t, mut=None, raw_mul=1.0):
    """Σ2′ の σ′ を S1-a の計器の σ から戻す。戻り (σ′, 決まったか, 下限)。mut＝紙の突然変異（None＝正しい形）。"""
    if not (si == si and t0 == t0 and t == t and t > 0 and t0 > 0): return float("nan"), False, float("nan")
    if mut == "Mu0":   # 旗 0＝S1-a そのもの
        return si, True, si
    f = t0 / t
    if mut == "Mu6": f = f * f          # 効きを 2 倍（T の比を 2 回）
    if mut == "Mu1": f = t0            # σ だけ T を残す（焼きは T なし）＝raw_i×T0
    f *= raw_mul
    if si < 1.0: v = min(1.0, si * f); return v, True, v
    if f >= 1.0: return 1.0, True, 1.0
    return float("nan"), False, min(1.0, f)

def owners(base, seed):
    D = {}
    p = os.path.join(base, "region_s%d.tsv" % seed)
    if not os.path.exists(p): return None
    with open(p, encoding="utf-8") as f:
        ix = read_header(f); iq, ir, ii = ix["quarter"], ix["region"], ix["nationIndex"]
        for line in f:
            c = line.rstrip("\r\n").split("\t"); D[(int(c[iq]), c[ir])] = sys.intern(c[ii])
    return D

COLS2 = ("s2BasisEngine", "s2D0Inst", "s2RhoInst", "s2SigmaEngine", "s2SigmaInst", "s2UHead", "s2TechHead", "s2Tech0Inst", "s2EffFillHead", "s2EffFill0Inst",
         "s2BakeSigmaDiff", "s2BakeRhoRelDiff", "s2CapEnd", "s2UEnd", "s2TechEnd", "s2HeadsEnd", "s2EEnd", "s2UnempEngine", "s2UnempB", "s2NewOverOld")

def load_rows(base, seed, with_owner=False, mut=None):
    """subst_s の 1 種：キー (q, 国/州) → 行の辞書（Σ2′ の量を足す）。with_owner＝真なら region_s の持ち主が変わった行に印。mut＝紙の突然変異の名（基線の行を腕に見立てる）。"""
    D = {}
    own = owners(base, seed) if with_owner else None
    last = {}; lastq = -1
    with open(os.path.join(base, "subst_s%d.tsv" % seed), encoding="utf-8") as f:
        ix = read_header(f)
        if "s2SigmaEngine" not in ix: raise SystemExit("subst_s に s2 の列が無い：" + base)
        g = {k: ix[k] for k in ("quarter", "year", "nation", "region", "subPopK", "stapleNeedPerCapita", "engineSubOutputPerMonth", "nominalSubPerMonth",
                                "farmCapPerMonth", "farmSoldPerMonth", "unsoldD1Share", "marketShareOfNeed", "s2BasisKind", "s2RhoSource") + COLS2}
        for line in f:
            c = line.rstrip("\r\n").split("\t")
            sp = fnum(c[g["subPopK"]])
            if not (sp > 0): continue
            q = int(c[g["quarter"]]); reg = c[g["region"]]
            if q < lastq: raise SystemExit("subst_s の行が四半期の順でない：" + base)
            lastq = q
            need = fnum(c[g["stapleNeedPerCapita"]]); eng = fnum(c[g["engineSubOutputPerMonth"]]); nom = fnum(c[g["nominalSubPerMonth"]])
            r = {"y": int(c[g["year"]]), "nation": c[g["nation"]], "sp": sp, "need": need,
                 "s": eng / (need * sp) if need * sp > 0 and eng == eng else float("nan"),
                 "d1": fnum(c[g["unsoldD1Share"]]), "cap": fnum(c[g["farmCapPerMonth"]]), "sold": fnum(c[g["farmSoldPerMonth"]]),
                 "hf": eng / nom if nom > 0 else float("nan"), "ms": fnum(c[g["marketShareOfNeed"]]), "masked": False,
                 "kind": int(c[g["s2BasisKind"]]), "rs": int(c[g["s2RhoSource"]])}
            for k in COLS2: r[k] = fnum(c[g[k]])
            if own is not None:
                o = own.get((q, reg))
                if o is None: r["masked"] = True
                else:
                    pv = last.get(reg); r["masked"] = (pv is None and q > 0) or (pv is not None and pv != o); last[reg] = o
            # Σ2′ の量（計器の列から台本が数える）
            sg, det, lo = sigma2(r["s2SigmaInst"], r["s2Tech0Inst"], r["s2TechHead"], None)
            r["sig2"], r["det2"], r["lo2"] = sg, det, lo
            r["rho2"] = r["s2RhoInst"] * r["s2Tech0Inst"] if r["rs"] == 1 else float("nan")
            capE, uE, hE = r["s2CapEnd"], r["s2UEnd"], r["s2HeadsEnd"]
            if r["rho2"] == r["rho2"] and r["rho2"] > 0 and hE == hE:
                e2 = uE - need * hE / r["rho2"]; r["e2"] = e2
                r["ub2"] = (min(1.0, max(0.0, e2 / capE)) if capE > 0 else 0.0)
            else: r["e2"] = float("nan"); r["ub2"] = float("nan")
            # 紙の dF（Σ2′）＝H×(σ′−d0)（RhoSource＝1 の行だけ）
            d0 = r["s2D0Inst"]
            r["x2"] = r["hf"] * (sg - d0) if (r["rs"] == 1 and d0 > 0 and r["hf"] == r["hf"] and sg == sg) else float("nan")
            r["x1"] = r["hf"] * (r["s2SigmaInst"] - d0) if (r["rs"] == 1 and d0 > 0 and r["hf"] == r["hf"] and r["s2SigmaInst"] == r["s2SigmaInst"]) else float("nan")
            if mut is not None:   # 紙の突然変異：基線の行を腕の行に置き換える（エンジンの σ と s を突然変異の式で作る）
                apply_mut(r, mut)
            D[(q, r["nation"] + "/" + reg)] = r
    return D

def apply_mut(r, mut):
    """紙の突然変異：基線（S1-a の盤面）の行から、その形のエンジンが書いたはずの σ_e・s・H・ρ_e・失業を作る（盤面の応答は入れない＝紙）。"""
    if r["rs"] != 1 or r["kind"] != 2: return
    d0 = r["s2D0Inst"]; T0, T = r["s2Tech0Inst"], r["s2TechHead"]
    si = r["s2SigmaInst"]; hf = r["hf"]
    if mut == "Mu0":   # 旗 0（S1-a）：エンジンは基線のまま
        return
    if mut == "OK":    # 正しい実装（紙）：σ_e＝σ′・ρ_e＝ρ′・失業＝ub′
        sg = r["sig2"] if r["det2"] else r["lo2"]; rho = r["rho2"]; ub = r["ub2"]; H = hf
    elif mut == "Mu1":  # σ だけ T を残す（焼き・lneed は T なし）
        sg, _, lo = sigma2(si, T0, T, "Mu1"); sg = sg if sg == sg else lo; rho = r["rho2"]; ub = r["ub2"]; H = hf
    elif mut == "Mu2":  # σ の外の倍率（自給の産出に収穫係数をもう 1 回）
        sg = r["sig2"] if r["det2"] else r["lo2"]; rho = r["rho2"]; ub = r["ub2"]; H = hf * hf
    elif mut == "Mu3":  # U を開幕の値に固定（σ＝min(1, ρ′×U0÷(need×n))＝σ′×U0÷U）
        u0 = r.get("_u0", float("nan")); uh = r["s2UHead"]
        sg0 = r["sig2"] if r["det2"] else r["lo2"]
        sg = min(1.0, sg0 * u0 / uh) if (uh > 0 and u0 == u0 and sg0 < 1.0) else sg0
        rho = r["rho2"]; ub = r["ub2"]; H = hf
    elif mut == "Mu5":  # σ と焼きは T なし・lneed に T を残す
        sg = r["sig2"] if r["det2"] else r["lo2"]; rho = r["rho2"]; H = hf
        capE, uE, hE, tE = r["s2CapEnd"], r["s2UEnd"], r["s2HeadsEnd"], r["s2TechEnd"]
        e5 = uE - r["need"] * hE / (rho * tE) if rho > 0 else float("nan")
        ub = (min(1.0, max(0.0, e5 / capE)) if capE > 0 else 0.0) if e5 == e5 else float("nan")
    elif mut == "Mu6":  # 効きを 2 倍
        sg, _, lo = sigma2(si, T0, T, "Mu6"); sg = sg if sg == sg else lo; rho = r["rho2"]; ub = r["ub2"]; H = hf
    else: raise SystemExit("知らない突然変異：" + mut)
    # 自給の担い手が 1 つの POP の州の近似：s＝H×σ（他の担い手の欄の分は基線の差のまま残す）
    r["s"] = r["s"] - hf * si + H * sg if (r["s"] == r["s"] and si == si) else r["s"]
    r["hf"] = H; r["s2SigmaEngine"] = sg; r["s2BasisEngine"] = rho; r["s2UnempEngine"] = ub
    if mut == "Mu1": r["s2BakeSigmaDiff"] = abs(d0 * T0 - d0)   # 焼いた月の σ＝d0×T0
    else: r["s2BakeSigmaDiff"] = r["s2BakeSigmaDiff"]

def light(r):
    """基線・ヌル腕の 1 行から門が読む物だけ。"""
    return (r["s"], r["d1"], r["cap"], r["sold"], r["ms"], r["y"], r["x2"], r["hf"], r["sig2"] if r["det2"] else r["lo2"], r["sp"], r["s2D0Inst"], r["rs"], r["kind"], r["nation"], r["x1"])

def load_light(base, seed):
    return {k: light(r) for k, r in load_rows(base, seed).items()}

def cand(r):
    """G4m の候補の述語（867 と同じ）：s2RhoSource＝1 かつ s2EEnd が有限（s2BasisKind は問わない）。"""
    return r.get("rs") == 1 and r.get("s2EEnd", float("nan")) == r.get("s2EEnd", float("nan"))

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

# ---------------------------------------------------------------- GC（商業化の向き）の升
def gc_cells(rows_by_board, sig_key):
    """1 種の盤面ごとに 国×窓 の升：κ＝1−Σ(sp×σ)÷Σsp（RhoSource＝1・BasisKind＝2 の行）・soc＝Σsold÷Σcap。戻り {国: {窓: (κ, soc, 行)}}。"""
    acc = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0.0, 0])
    for (q, key), r in rows_by_board.items():
        if isinstance(r, tuple):
            s_, d1, cap, sold, ms, y, x2, hf, sg2, sp, d0, rs, kind, nation, x1 = r
            sg = sg2
        else:
            y, sp, rs, kind, nation, cap, sold = r["y"], r["sp"], r["rs"], r["kind"], r["nation"], r["cap"], r["sold"]
            sg = r[sig_key]
        if rs != 1 or kind != 2 or not (sg == sg) or not (cap > 0) or not (sold == sold): continue
        a = acc[(nation, win(y))]; a[0] += sp * sg; a[1] += sp; a[2] += sold; a[3] += cap; a[4] += 1
    out = collections.defaultdict(dict)
    for (nat, wn), a in acc.items():
        if a[1] > 0 and a[3] > 0: out[nat][wn] = (1.0 - a[0] / a[1], a[2] / a[3], a[4])
    return out

def gc_delta(cells):
    """升ごとの開幕（1783-85）からの差 (Δκ, Δsoc)。"""
    D = {}
    for nat, ws in cells.items():
        o = ws.get("1783-85")
        if o is None: continue
        for wn in WINS[1:]:
            v = ws.get(wn)
            if v is None: continue
            D[(nat, wn)] = (v[0] - o[0], v[1] - o[1])
    return D

# ---------------------------------------------------------------- 門
def run_gates(arm, b, n1, n2, nseed=10, thr=None, seed0=1, mut=None):
    """門を数えて (ok, C, 出力の行)。mut＝紙の突然変異（腕＝基線の行を突然変異の式で作った物）。"""
    nseed = int(nseed); seed0 = int(seed0); thr = thr or {}
    C = collections.Counter(); L = collections.defaultdict(list); ownall = True
    GCd = []   # (種, 国, 窓, Δκ 腕, Δκ 紙, Δsoc 基線)
    for seed in range(seed0, seed0 + nseed):
        if mut is None:
            A = load_rows(arm, seed, True); own_ok = os.path.exists(os.path.join(arm, "region_s%d.tsv" % seed))
        else:
            A = load_rows(b, seed, True, None); own_ok = True
            u0 = {}
            for (q, reg), r in sorted(A.items()):
                if r["rs"] == 1 and reg not in u0 and r["kind"] == 2: u0[reg] = r["s2UHead"]
            for (q, reg), r in A.items():
                r["_u0"] = u0.get(reg, float("nan")); apply_mut(r, mut)
        ownall = ownall and own_ok
        B = [load_light(x, seed) for x in (b, n1, n2)]
        open0 = {}
        for (q, reg), r in A.items():
            if q == 0: open0[reg] = r
        baked = set()
        for (q, reg), r in A.items():
            # G3a（Σ2′）：焼いた州ごとに 1 回
            if r["kind"] == 2 and r["rs"] == 1 and reg not in baked and r["rho2"] == r["rho2"]:
                baked.add(reg); C["G3a 焼いた州"] += 1
                rel = abs(r["s2BasisEngine"] / r["rho2"] - 1.0) if r["rho2"] > 0 else float("inf")
                if rel <= 1e-9 and r["s2BakeSigmaDiff"] <= 1e-9: C["G3a 一致"] += 1
                if r["s2BakeRhoRelDiff"] <= 1e-9: C["G3a（S1-a の列・記録）一致"] += 1
            if cand(r) and r["s2EEnd"] > 0:
                C["G4m 候補 E＞0"] += 1
                if r["masked"]: C["G4m 外した E＞0"] += 1
            if r["kind"] == 2 and r["rs"] == 1:
                se = r["s2SigmaEngine"]
                if se == se and r["s2SigmaInst"] == r["s2SigmaInst"]:
                    if r["det2"]:
                        k = "G2 σ＝1" if r["sig2"] >= 1.0 else "G2 σ＜1"
                        C[k + " 行"] += 1
                        if abs(se - r["sig2"]) <= NEED_TOL: C[k + " 一致"] += 1
                    else:
                        C["G2 決まらない行"] += 1
                        if r["lo2"] - NEED_TOL <= se <= 1.0 + NEED_TOL: C["G2 決まらない行 幅の内"] += 1
                    if abs(se - r["s2SigmaInst"]) <= NEED_TOL: C["G2（S1-a の列・記録）一致"] += 1
                e, ue, ub = r["e2"], r["s2UnempEngine"], r["ub2"]
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
                L[("記録 T÷T0", win(r["y"]))].append(r["s2TechHead"] / r["s2Tech0Inst"] if r["s2Tech0Inst"] > 0 else float("nan"))
                L[("記録 σ÷d0 腕", win(r["y"]))].append(se / r["s2D0Inst"] if r["s2D0Inst"] > 0 else float("nan"))
                if se >= 1.0: C[("記録 σ＝1 の行", win(r["y"]))] += 1
                C[("記録 焼いた行", win(r["y"]))] += 1
            key = (q, reg)
            s0s = [x[key][0] for x in B if key in x]
            if len(s0s) < 3 or any(v != v for v in s0s) or r["s"] != r["s"]: C["基線とヌル腕のどれかに無い（外した）"] += 1; continue
            w = max(s0s) - min(s0s); d = r["s"] - s0s[0]
            L[("記録 市場の買いの割合 腕", win(r["y"]))].append(r["ms"]); L[("記録 市場の買いの割合 基線", win(r["y"]))].append(B[0][key][4])
            if q == 0: continue
            o = open0.get(reg)
            if o is None or r["d1"] != r["d1"] or o["d1"] != o["d1"]: continue
            if r["d1"] < o["d1"]:
                C["G1 P1 行"] += 1; L["G1 d"].append(d); L["G1 w"].append(w)
                if d < -w: C["G1a 外"] += 1
                if r["kind"] == 2:
                    C["G1b P1 焼いた行"] += 1
                    if r["s2SigmaEngine"] == 0: C["G1b σ＝0"] += 1
                    if r["d1"] == 0: C["G1b D1＝0"] += 1
            if r["cap"] > 0 and o["cap"] > 0 and abs(r["sold"] / r["cap"] - o["sold"] / o["cap"]) <= 0.01:
                C["G5 P5 行"] += 1; L["G5 w"].append(w)
                hb = B[0][key][7]; d0 = r["s2D0Inst"]
                if r["hf"] == r["hf"] and hb == hb and d0 == d0:
                    L["G5b dB"].append(d0 * (r["hf"] - hb)); C["G5b 行"] += 1
                xs = [x[key][6] for x in B]
                if r["rs"] == 1 and d0 > 0 and r["hf"] == r["hf"] and all(v == v for v in xs):
                    dF = r["s"] - r["hf"] * d0; wn5 = win(r["y"])
                    C[("記録 G5p の行", wn5)] += 1
                    if r["s2Tech0Inst"] > 0 and r["s2TechHead"] > r["s2Tech0Inst"] * (1 + 1e-9): C[("記録 G5p の行 T＞T0", wn5)] += 1
                    L[("G5p e", wn5)].append(dF - xs[0]); L[("G5p wp", wn5)].append(max(xs) - min(xs))
                    x1b = B[0][key][14]
                    if x1b == x1b: L[("G5p e（S1-a の紙・記録）", wn5)].append(dF - x1b)
            # 対照：Σ2′ の比 σ′÷d0 が 1±0.02 の内の行（基線の紙）で、腕の σ_e÷d0 と基線の σ′÷d0 の差
            if r["kind"] == 2 and r["rs"] == 1 and r["s2D0Inst"] > 0:
                sb = [x[key][8] / r["s2D0Inst"] for x in B]
                if all(v == v for v in sb) and abs(sb[0] - 1.0) <= 0.02 and r["s2SigmaEngine"] == r["s2SigmaEngine"]:
                    L["対照 差"].append(r["s2SigmaEngine"] / r["s2D0Inst"] - sb[0]); L["対照 幅"].append(max(sb) - min(sb))
        # GC の升（この種）
        ca = gc_delta(gc_cells(A, "s2SigmaEngine")); cb = [gc_delta(gc_cells(x, None)) for x in B]
        for k, (dka, dsa) in ca.items():
            if not all(k in x for x in cb): continue
            GCd.append((seed, k[0], k[1], dka, cb[0][k][0], cb[0][k][1]))
        del A, B
    OUT = []; P = OUT.append; ok = {}
    def rate(a, bb): return C[a] / C[bb] if C[bb] else float("nan")
    P("(877) 門 ts（腕 %s・基線 %s・N1 %s・N2 %s・種 %d〜%d・紙の突然変異 %s・region_s %s）" % (arm, b, n1, n2, seed0, seed0 + nseed - 1, mut or "なし", "あり" if ownall else "なし"))
    g1a = rate("G1a 外", "G1 P1 行"); ok["G1a"] = g1a >= 0.5
    P("G1a 働いた（下側）：P1 %d・外 %d（%.4f）≧0.5 → %s・d 中央 %.4f・w 中央 %.4f" % (C["G1 P1 行"], C["G1a 外"], g1a, ok["G1a"], med(L["G1 d"]), med(L["G1 w"])))
    g1bs, g1bd = rate("G1b σ＝0", "G1b P1 焼いた行"), rate("G1b D1＝0", "G1b P1 焼いた行")
    ok["G1b"] = C["G1b P1 焼いた行"] > 0 and g1bs <= g1bd + 0.001
    P("G1b 上側：P1 の焼いた行 %d・σ＝0 %.5f ≦ D1＝0 %.5f＋0.001 → %s" % (C["G1b P1 焼いた行"], g1bs, g1bd, ok["G1b"]))
    sc = nseed / 10.0
    ok["G2"] = (C["G2 σ＜1 行"] >= 100000 * sc and C["G2 σ＝1 行"] >= 100 * sc and C["G2 σ＜1 一致"] == C["G2 σ＜1 行"] and C["G2 σ＝1 一致"] == C["G2 σ＝1 行"]
                and C["G2 決まらない行 幅の内"] == C["G2 決まらない行"])
    P("G2 実装の一致（Σ2′）：σ＜1 %d 行（一致 %d・下限 %d）・σ＝1 %d 行（一致 %d・下限 %d）・決まらない行 %d（幅の内 %d） → %s" % (
        C["G2 σ＜1 行"], C["G2 σ＜1 一致"], int(100000 * sc), C["G2 σ＝1 行"], C["G2 σ＝1 一致"], int(100 * sc), C["G2 決まらない行"], C["G2 決まらない行 幅の内"], ok["G2"]))
    ok["G3a"] = C["G3a 焼いた州"] >= 2000 * sc and C["G3a 一致"] == C["G3a 焼いた州"]
    P("G3a 焼いた月（Σ2′ の ρ′）：焼いた州 %d（一致 %d・下限 %d） → %s" % (C["G3a 焼いた州"], C["G3a 一致"], int(2000 * sc), ok["G3a"]))
    ok["G4"] = (ownall and C["G4 E＞0 行"] >= 400 * sc and C["G4 E≦0 行"] >= 100000 * sc and C["G4 E＞0 一致"] == C["G4 E＞0 行"] and C["G4 E≦0 一致"] == C["G4 E≦0 行"])
    P("G4 失業 (b′)（Σ2′ の lneed′・持ち主が変わった行を外した後）：E＞0 %d 行（一致 %d・下限 %d）・E≦0 %d 行（一致 %d・下限 %d）・外した E＞0 %d・外した E≦0 %d → %s"
      % (C["G4 E＞0 行"], C["G4 E＞0 一致"], int(400 * sc), C["G4 E≦0 行"], C["G4 E≦0 一致"], int(100000 * sc), C["G4 外した E＞0"], C["G4 外した E≦0"], ok["G4"]))
    lim = thr.get("G4m_limit", float("nan")); sh = rate("G4m 外した E＞0", "G4m 候補 E＞0")
    ok["G4m"] = (ownall and lim == lim and (sh == sh) and sh <= lim)
    P("G4m 印の乱用の止め：外した E＞0 の割合 %.5f（%d／%d）≦ 閾値 %.5f → %s" % (sh, C["G4m 外した E＞0"], C["G4m 候補 E＞0"], lim, ok["G4m"]))
    g5w = med(L["G5 w"]); g5bd = med(L["G5b dB"])
    ok["G5b"] = C["G5b 行"] > 0 and abs(g5bd) <= g5w
    P("G5b 壊さない側（σ の外の経路）：P5 の行 %d・|dB＝d0×(H腕−H基線) の中央| %.5f ≦ w の中央 %.5f → %s" % (C["G5b 行"], abs(g5bd), g5w, ok["G5b"]))
    nev = 0; allp = True
    for wn in WINS:
        ee = L[("G5p e", wn)]; wpm = med(L[("G5p wp", wn)]); em = med(ee)
        excl = thr.get("G5p_exclude_" + wn, 0) == 1; evalw = len(ee) >= 500 * sc and not excl
        passw = evalw and abs(em) <= wpm
        e1 = med(L[("G5p e（S1-a の紙・記録）", wn)])
        P("  G5p 窓 %s：行 %d・|e の中央| %.5f ≦ wp の中央 %.5f → %s（記録：S1-a の紙との e の中央 %.5f）" % (wn, len(ee), abs(em), wpm,
          ("評価しない（%s）" % ("空振りで外す窓" if excl else "行が足りない")) if not evalw else ("通る" if passw else "落ちる"), e1))
        if evalw: nev += 1; allp = allp and passw
    ok["G5p"] = nev >= 2 and allp
    P("G5p 働いた側の大きさ：評価した窓 %d（2 以上が要る）・全て通る %s → %s" % (nev, allp, ok["G5p"]))
    # GC
    wk, ws = thr.get("GC_w_kappa", float("nan")), thr.get("GC_w_soc", float("nan"))
    up = [(a - p) for (_, _, _, a, p, ds) in GCd if ds > ws]; dn = [(a - p) for (_, _, _, a, p, ds) in GCd if ds < -ws]
    nup_min, ndn_min = thr.get("GC_n_up_min", float("inf")), thr.get("GC_n_down_min", float("inf"))
    mu, mdn, mall = med(up), med(dn), med(up + dn)
    gc_up = len(up) >= nup_min * sc and mu >= -wk
    gc_dn = len(dn) >= ndn_min * sc and mdn <= wk
    gc_ab = len(up) + len(dn) > 0 and abs(mall) <= wk
    ok["GC"] = wk == wk and gc_up and gc_dn and gc_ab
    P("GC 商業化の向き：w_κ %.5f・w_soc %.5f・上がった升 %d（下限 %d）中央(Δκ腕−Δκ紙) %.5f ≧ −w_κ → %s・下がった升 %d（下限 %d）中央 %.5f ≦ w_κ → %s・両方の升 %d の |中央| %.5f ≦ w_κ → %s → %s" % (
        wk, ws, len(up), int(nup_min * sc) if nup_min != float("inf") else -1, mu, gc_up, len(dn), int(ndn_min * sc) if ndn_min != float("inf") else -1, mdn, gc_dn, len(up) + len(dn), abs(mall), gc_ab, ok["GC"]))
    cd, cw = med(L["対照 差"]), med(L["対照 幅"])
    ok["対照"] = len(L["対照 差"]) >= 1000 * sc and abs(cd) <= cw
    P("対照（σ′÷d0 が 1±0.02 の行）：行 %d（下限 %d）・|σ_e÷d0−σ′÷d0 の中央| %.5f ≦ 幅の中央 %.5f → %s" % (len(L["対照 差"]), int(1000 * sc), abs(cd), cw, ok["対照"]))
    P("外した行（基線とヌル腕のどれかに無い）：%d" % C["基線とヌル腕のどれかに無い（外した）"])
    P("門の判定：%s（%s）" % ("全て通る" if all(ok.values()) else "落ちた門あり", "・".join("%s=%s" % (k, "通" if v else "落") for k, v in ok.items())))
    P("-- 記録（門ではない）")
    P("G5p の母集合（P5・RhoSource＝1）のうち T＞T0 の行：%s" % "・".join("%s %d／%d（%.3f）" % (wn, C[("記録 G5p の行 T＞T0", wn)], C[("記録 G5p の行", wn)], C[("記録 G5p の行 T＞T0", wn)] / C[("記録 G5p の行", wn)] if C[("記録 G5p の行", wn)] else float("nan")) for wn in WINS))
    P("G2（S1-a の列そのまま・記録）：一致 %d／σ の行 %d・G3a（S1-a の列・記録）一致 %d／%d" % (C["G2（S1-a の列・記録）一致"], C["G2 σ＜1 行"] + C["G2 σ＝1 行"] + C["G2 決まらない行"], C["G3a（S1-a の列・記録）一致"], C["G3a 焼いた州"]))
    for wn in WINS:
        P("  %s：T÷T0 中央 %.4f・σ÷d0 腕 中央 %.4f・σ＝1 の行 %d／%d・市場の買いの割合 腕 %.4f／基線 %.4f" % (wn, med(L[("記録 T÷T0", wn)]), med(L[("記録 σ÷d0 腕", wn)]),
          C[("記録 σ＝1 の行", wn)], C[("記録 焼いた行", wn)], med(L[("記録 市場の買いの割合 腕", wn)]), med(L[("記録 市場の買いの割合 基線", wn)])))
    return ok, C, OUT, GCd

# ---------------------------------------------------------------- 閾値の表（旗 0 の 3 本だけから・腕を見ない）
def make_thresholds(b, n1, n2, out, nseed=10):
    dirs = {"M": b, "N1": n1, "N2": n2}
    triples = [(a, x, y) for a in dirs for x in dirs for y in dirs if len({a, x, y}) == 3]
    E = collections.defaultdict(list); Wp = collections.defaultdict(list)
    GCrng_k = []; GCrng_s = []; GCbase = []
    msC = collections.Counter(); msh = {}
    for seed in range(1, int(nseed) + 1):
        R = {k: load_rows(v, seed, True) for k, v in dirs.items()}
        for k, rows in R.items():
            for r in rows.values():
                if cand(r):
                    msC[(k, "候補")] += 1
                    if r["s2EEnd"] > 0:
                        msC[(k, "候補 E＞0")] += 1
                        if r["masked"]: msC[(k, "外した E＞0")] += 1
        D = {k: {kk: light(r) for kk, r in rows.items()} for k, rows in R.items()}
        del R
        # G5p の一つ抜き（867 の loo_g5p と同じ形・x を x′ に）
        for t in triples:
            a, x, y = t; A, Bb, Cc = D[a], D[x], D[y]
            open0 = {reg: r for (q, reg), r in A.items() if q == 0}
            for key, r in A.items():
                q, reg = key
                if q == 0 or key not in Bb or key not in Cc: continue
                o = open0.get(reg)
                if o is None or r[1] != r[1] or o[1] != o[1]: continue
                if not (r[2] > 0 and o[2] > 0 and abs(r[3] / r[2] - o[3] / o[2]) <= 0.01): continue
                xa, xb, xc = r[6], Bb[key][6], Cc[key][6]
                if xa != xa or xb != xb or xc != xc: continue
                wn = win(r[5]); E[(t, wn)].append(xa - xb); Wp[(t, wn)].append(abs(xb - xc))
        # GC の幅（σ′ の紙の Δκ と Δsoc のヌル 3 本の範囲）
        G = {k: gc_delta(gc_cells(v, None)) for k, v in D.items()}
        for kk in G["M"]:
            if not all(kk in G[z] for z in G): continue
            dk = [G[z][kk][0] for z in ("M", "N1", "N2")]; ds = [G[z][kk][1] for z in ("M", "N1", "N2")]
            GCrng_k.append(max(dk) - min(dk)); GCrng_s.append(max(ds) - min(ds)); GCbase.append((seed, kk, G["M"][kk], G["N1"][kk], G["N2"][kk]))
        del D, G
    lines = ["# (877) gates_ts877.py の閾値（旗 0〔S1-a・m873〕の base_ins・N1・N2 の生だけから --make-thresholds が規則で置いた値・腕の数は使わない）",
             "# G5p の外す窓＝6 通りの一つ抜きで落ちた通りが 2 以上（≧1/3）の窓（867 と同じ規則・x を Σ2′ の x′ へ）。"]
    for wn in WINS:
        fails = 0; per = []
        for t in triples:
            e, w = med(E[(t, wn)]), med(Wp[(t, wn)]); per.append((len(E[(t, wn)]), abs(e), w))
            if not (abs(e) <= w): fails += 1
        lines.append("G5p_exclude_%s\t%d\t落ちた通り %d／6（行 %s・|e|／wp %s）" % (wn, 1 if fails >= 2 else 0, fails, "・".join(str(p[0]) for p in per), "・".join("%.4f／%.4f" % (p[1], p[2]) for p in per)))
    sh = [msC[(k, "外した E＞0")] / msC[(k, "候補 E＞0")] if msC[(k, "候補 E＞0")] else float("nan") for k in ("M", "N1", "N2")]
    lines.append("G4m_limit\t%.6f\tmax(3本の割合 %s)の3倍（867 と同じ規則・m873 の 3 本で数え直した）" % (3 * max(sh), "・".join("%.6f" % s for s in sh)))
    wk = med(GCrng_k); ws = med(GCrng_s)
    lines.append("GC_w_kappa\t%.6f\t升（種×国×窓・開幕の窓を除く）ごとのヌル 3 本の Δκ′ の範囲の中央（升 %d）" % (wk, len(GCrng_k)))
    lines.append("GC_w_soc\t%.6f\t同じ升の Δsoc の範囲の中央" % ws)
    nup = sum(1 for (_, _, m, _, _) in GCbase if m[1] > ws); ndn = sum(1 for (_, _, m, _, _) in GCbase if m[1] < -ws)
    lines.append("GC_n_up_min\t%d\t基線の紙で Δsoc＞w_soc の升 %d の半分（腕の升の数の下限）" % (nup // 2, nup))
    lines.append("GC_n_down_min\t%d\t基線の紙で Δsoc＜−w_soc の升 %d の半分" % (ndn // 2, ndn))
    # GC の一つ抜き（ヌル 3 本を順に腕・基線にした 6 通り：腕の Δκ も紙の σ′ で数える＝正しい実装の見立て）
    idx = {"M": 2, "N1": 3, "N2": 4}; gcfail = collections.Counter()
    for t in triples:
        a, x, y = t
        upv = [g[idx[a]][0] - g[idx[x]][0] for g in GCbase if g[idx[x]][1] > ws]; dnv = [g[idx[a]][0] - g[idx[x]][0] for g in GCbase if g[idx[x]][1] < -ws]
        mu, md_, ma = med(upv), med(dnv), med(upv + dnv)
        okk = mu >= -wk and md_ <= wk and abs(ma) <= wk
        gcfail[okk] += 1
        lines.append("# GC の一つ抜き %s：上 %d 中央 %.5f・下 %d 中央 %.5f・両方 |中央| %.5f → %s" % ("".join(t), len(upv), mu, len(dnv), md_, abs(ma), "緑" if okk else "赤"))
    lines.append("# GC の一つ抜きの空振り（赤になった通り）：%d／6（w_κ は 3 本全ての範囲から＝一つ抜きの通りに対して甘い側）" % gcfail[False])
    with open(out, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(lines) + "\n")
    return lines

def main():
    a = sys.argv[1:]
    if a and a[0] == "--make-thresholds":
        lines = make_thresholds(a[1], a[2], a[3], a[4], a[5] if len(a) > 5 else 10); print("\n".join(lines)); return 0
    if a and a[0] == "--paper-mut":
        b, n1, n2, out = a[1:5]; nseed = a[5] if len(a) > 5 else 10
        thr = read_thr(a[6] if len(a) > 6 else os.path.join(os.path.dirname(os.path.abspath(__file__)), "gates_ts877_thresholds.tsv"))
        ALL = []
        muts = a[7].split(",") if len(a) > 7 else ["OK", "Mu0", "Mu1", "Mu2", "Mu3", "Mu5", "Mu6"]
        for mut in muts:
            ok, C, OUT, _ = run_gates(b, b, n1, n2, nseed, thr, 1, mut)
            ALL += OUT + ["== %s：%s" % (mut, "・".join("%s=%s" % (k, "緑" if v else "赤") for k, v in ok.items())), ""]
        with open(out, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(ALL) + "\n")
        print("\n".join(x for x in ALL if x.startswith("=="))); return 0
    arm, b, n1, n2, out = a[:5]; nseed = a[5] if len(a) > 5 else 10
    thr = read_thr(a[6] if len(a) > 6 else os.path.join(os.path.dirname(os.path.abspath(__file__)), "gates_ts877_thresholds.tsv"))
    seed0 = a[7] if len(a) > 7 else 1
    ok, C, OUT, GCd = run_gates(arm, b, n1, n2, nseed, thr, seed0)
    with open(out, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(OUT) + "\n")
    with open(out + ".gc.tsv", "w", encoding="utf-8", newline="\n") as f:
        f.write("seed\tnation\twindow\tdKappaArm\tdKappaPaper\tdSocBase\n")
        for g in GCd: f.write("%d\t%s\t%s\t%.6g\t%.6g\t%.6g\n" % g)
    print("\n".join(OUT)); return 0

if __name__ == "__main__":
    sys.exit(main())
