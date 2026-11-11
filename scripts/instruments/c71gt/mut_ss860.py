# (860) c71gt 固定の盤面（紙の台本）：c69ss の門 gates_ss860.py を、合成した subst_s と region_s で突然変異ごとに回す（mut_ss860.py の写し＋G4m の分母の試験・事前登録 [1-2][3]）。
#   python -X utf8 -I mut_ss860.py <作業フォルダ> <出力の表> [閾値の表]
# 正しい実装の列を式で合成し（エンジンの式を台本の中に書く＝期待値を同じ出所から作らない：門は列と列の突き合わせ、合成は式）、
# 実装の 1 項を替えたときに列がどう変わるかを式で書いて盤面を作る。実の生は読まない。
import sys, os, random, math, importlib.util, shutil
sys.dont_write_bytecode = True
sd = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("g", os.path.join(sd, "gates_ss860.py")); g = importlib.util.module_from_spec(spec); spec.loader.exec_module(g)

REAL_HDR = "D:/PoB_work/keep_852/base_ins/subst_s1.tsv"
NATIONS = ["甲", "乙", "丙"]; NREG = 48; NQ = 60; NEED = 1.0; SP = 100.0

def clamp01(x): return 0.0 if not (x > 0) else 1.0 if x > 1 else x

def F(x):
    return "nan" if x != x else "%.10g" % x

def header():
    with open(REAL_HDR, encoding="utf-8") as f: return f.readline().rstrip("\r\n").split("\t")

def build(root, variant, seed=1):
    """variant：correct・M1・M2・M3・M5・M6・empty・escape（M5 の食い違う行を全て持ち主が変わった行にする）・rehome_oldmask（正しい実装＋持ち主が変わった行）。"""
    rng = random.Random(seed * 7919)
    H = header(); hx = {h: i for i, h in enumerate(H)}
    arm, base, n1, n2 = [os.path.join(root, variant, d) for d in ("arm", "base", "n1", "n2")]
    for d in (arm, base, n1, n2): os.makedirs(d, exist_ok=True)
    mut = {"rehome_oldmask": "correct", "escape": "M5"}.get(variant, variant)
    regs = []
    for k in range(NREG):
        nat = NATIONS[k % 3]
        kind = k % 10
        d0 = 0.9 if kind == 0 else 0.5 if kind == 1 else rng.uniform(0.4, 0.7)    # 種類 0＝σ＝1 に張り付く州・種類 1＝持ち主が変わる州（普段は失業が出ない）
        r0 = 0.9 if kind == 0 else 0.5 if kind == 1 else rng.uniform(0.35, 0.65)
        regs.append({"name": "州%02d" % k, "nat": nat, "d0": d0, "r0": r0, "cap": 1000.0 * rng.uniform(0.8, 1.2), "ef0": rng.uniform(0.0, 0.5), "kind": kind,
                     "rehome_q": 30 + k if kind == 1 else None, "pf0": rng.uniform(0.9, 1.1)})
    rows_arm, rows_b = [], {"base": [], "n1": [], "n2": []}; own = []
    for q in range(NQ):
        for R in regs:
            cap = R["cap"]; ef = R["ef0"] + 0.003 * q; T = 1 + 0.2 * ef; T0 = 1 + 0.2 * R["ef0"]
            r = R["r0"] if q == 0 else (min(0.2 + 0.1 * (q % 3), 1.0) if R["kind"] == 0 and q > 5 else rng.uniform(0.2, 0.95) if rng.random() > 0.03 else 1.0)
            if R["kind"] == 1: r = rng.uniform(0.45, 0.6)
            pf = R["pf0"] if q == 0 else rng.uniform(0.9, 1.1)
            produced = cap * pf; sold = cap * r
            U0 = cap * (1 - R["r0"]); U = cap * (1 - r)
            rehomed = R["rehome_q"] is not None and q == R["rehome_q"]
            nat_idx = ("%d" % (NATIONS.index(R["nat"]) + (10 if R["rehome_q"] is not None and q >= R["rehome_q"] else 0)))
            own.append((q, R["name"], nat_idx))
            soldEnd = 0.0 if rehomed else sold      # Rehome が LastSold を 0 に戻す（Territory.cs:266）
            uEnd = cap * (1 - soldEnd / cap)
            d0 = R["d0"]
            # 計器（正しい式）
            rho_i = d0 * NEED * SP / (U0 * T0) if U0 > 0 else float("nan")
            sig_i = min(1.0, rho_i * U * T / (NEED * SP)) if rho_i == rho_i else float("nan")
            lneed_i = NEED * SP / (rho_i * T)
            e_i = uEnd - lneed_i; ub = clamp01(e_i / cap)
            # エンジン（変異で替える）
            D2 = lambda s_, p_: clamp01(1 - s_ / p_)
            U0e, Ue, Te, T0e = U0, U, T, T0
            if mut == "M2": U0e, Ue = cap, cap
            if mut == "M3": U0e, Ue = cap * D2(cap * R["r0"], cap * R["pf0"]), cap * D2(sold, produced)
            if mut == "M6": Te, T0e = 1.0, 1.0
            rho_e = 0.0 if mut == "M1" else d0 * NEED * SP / (U0e * T0e)
            sig_e = min(1.0, rho_e * Ue * Te / (NEED * SP))
            if rho_e > 0:
                lneed_e = NEED * SP / (rho_e * Te)
                # 失業は Rehome の前の売れた量（エンジンの (b′)）で書く
                e_e = (cap * (1 - sold / cap)) - lneed_e
                if mut == "M5": e_e = (cap * (1 - sold / cap)) - lneed_e * 1.05      # 収穫係数を外さない旧の式
            else:
                e_e = -float("inf")
            ue = clamp01(e_e / cap) if e_e > 0 else 0.0
            hf = 1.05
            nom = sig_e * SP * NEED; eng = nom * hf
            bake_sig = abs(sig_e - d0) if q >= 0 else 0.0     # 焼いた月の食べる割合と d0 の差（焼いた月だけの値を同じ値で書き続ける）
            # 焼いた月（q＝0 の州の最初の点検）の値：エンジンが焼いた月の σ は rho_e×U0e×T0e÷(need×size)
            sig_bake = min(1.0, rho_e * U0e * T0e / (NEED * SP))
            bake_sig = abs(sig_bake - d0); bake_rho = abs(rho_e / rho_i - 1.0) if rho_i == rho_i else float("nan")
            vals = {"quarter": str(q), "year": str(1783 + q // 4), "nation": R["nat"] if R["rehome_q"] is None or q < R["rehome_q"] else R["nat"] + "新",
                    "region": R["name"], "tier": "Core", "subPopK": F(SP), "subRateWeighted": F(sig_e), "farmCapPerMonth": F(cap), "farmSoldPerMonth": F(sold),
                    "farmProducedPerMonth": F(produced), "soldOverCap": F(sold / cap), "unsoldD1Share": F(1 - sold / cap), "unsoldD2Share": F(D2(sold, produced)), "producedZeroFlag": "0",
                    "stapleNeedPerCapita": F(NEED), "engineSubOutputPerMonth": F(eng), "nominalSubPerMonth": F(nom), "marketShareOfNeed": F(1 - sig_e),
                    "s2BasisKind": "2", "s2BasisEngine": F(rho_e), "s2D0Inst": F(d0), "s2RhoInst": F(rho_i), "s2RhoSource": "1", "s2SigmaEngine": F(sig_e), "s2SigmaInst": F(sig_i),
                    "s2TechHead": F(T), "s2Tech0Inst": F(T0), "s2EffFillHead": F(ef), "s2EffFill0Inst": F(R["ef0"]), "s2BakeSigmaDiff": F(bake_sig), "s2BakeRhoRelDiff": F(bake_rho),
                    "s2CapEnd": F(cap), "s2SoldEnd": F(soldEnd), "s2UEnd": F(uEnd), "s2TechEnd": F(T), "s2HeadsEnd": F(SP), "s2LneedEnd": F(lneed_i), "s2EEnd": F(e_i),
                    "s2UnempEngine": F(ue), "s2UnempB": F(ub), "s2NewOverOld": F(sig_e / d0)}
            if variant == "empty": vals.update({"s2BasisKind": "0", "s2RhoSource": "0", "s2BakeRhoRelDiff": "nan", "s2EEnd": "nan"})
            rows_arm.append((vals, H))
            # 基線・ヌル腕（旗 0）：食べる割合は開幕の d0 のまま・ヌル腕は雑音だけ違う
            for nm in ("base", "n1", "n2"):
                eps = 0.0 if nm == "base" else rng.gauss(0, 0.03)
                s0 = d0 * hf * (1 + eps)
                v0 = dict(vals); v0.update({"s2BasisKind": "0", "s2RhoSource": "0", "engineSubOutputPerMonth": F(s0 * NEED * SP), "nominalSubPerMonth": F(s0 * NEED * SP / hf),
                                            "s2SigmaEngine": "nan", "s2BakeRhoRelDiff": "nan", "s2EEnd": "nan"})
                rows_b[nm].append((v0, H))
    # 印の乱用：M5 の食い違う行を全て持ち主が変わった行にする
    if variant == "escape":
        bad = {(i) for i, (v, _) in enumerate(rows_arm) if float(v["s2EEnd"]) > 0 and abs(float(v["s2UnempEngine"]) - float(v["s2UnempB"])) > 1e-9}
        badkeys = {(int(rows_arm[i][0]["quarter"]), rows_arm[i][0]["region"]) for i in bad}
        own = [(q, rg, ("99" if (q, rg) in badkeys else idx)) for (q, rg, idx) in own]
    def write(dirp, rows):
        with open(os.path.join(dirp, "subst_s1.tsv"), "w", encoding="utf-8", newline="\n") as f:
            f.write("\t".join(H) + "\n")
            for v, h in rows: f.write("\t".join(v.get(c, "nan") for c in h) + "\n")
    write(arm, rows_arm); write(base, rows_b["base"]); write(n1, rows_b["n1"]); write(n2, rows_b["n2"])
    with open(os.path.join(arm, "region_s1.tsv"), "w", encoding="utf-8", newline="\n") as f:
        f.write("quarter\tregion\tnationIndex\n")
        for q, rg, idx in own: f.write("%d\t%s\t%s\n" % (q, rg, idx))
    return arm, base, n1, n2

SCALE = 0.02
PRED = [   # (変異, 赤になる門の予言＝全て赤であること, 緑のままの門の予言)
    ("correct", [], ["G1a", "G1b", "G2", "G3a", "G4", "G4m", "G5"]),
    ("M1", ["G1b", "G2", "G3a"], []),
    ("M2", ["G1a", "G2", "G3a"], ["G5"]),
    ("M3", ["G2", "G3a"], []),
    ("M5", ["G4"], ["G1a", "G1b", "G2", "G3a", "G5"]),
    ("M6", ["G2", "G3a"], []),
    ("empty", ["G1b", "G2", "G3a", "G4"], []),
    ("escape", ["G4m"], []),
]

def main():
    work, out = sys.argv[1], sys.argv[2]
    thr = g.read_thr(sys.argv[3]) if len(sys.argv) > 3 else {}
    shutil.rmtree(work, ignore_errors=True); os.makedirs(work)
    L = ["(860) 固定の盤面（合成）の突然変異の表・c69ss の門 gates_ss860.py（種 1・母集合の下限の倍率 %.2f・G4m の閾値 %s）" % (SCALE, thr.get("G4m_limit", "なし")),
         "変異\t予言：赤\t予言：緑\t実際に赤\t実際に緑\t予言との差"]
    allok = True
    res = {}
    for var, red, green in PRED:
        a, b, n1, n2 = build(work, var)
        ok, C, OUT = g.run_gates(a, b, n1, n2, 1, SCALE, thr)
        reds = [k for k, v in ok.items() if not v]; greens = [k for k, v in ok.items() if v]
        miss_r = [k for k in red if k not in reds]; miss_g = [k for k in green if k not in greens]; extra = [k for k in reds if k not in red and var not in ("correct",)]
        diff = ("予言の赤が緑：" + ",".join(miss_r) + "　") * bool(miss_r) + ("予言の緑が赤：" + ",".join(miss_g) + "　") * bool(miss_g) + ("予言に書かなかった赤：" + ",".join(extra)) * bool(extra)
        L.append("%s\t%s\t%s\t%s\t%s\t%s" % (var, ",".join(red) or "-", ",".join(green) or "-", ",".join(reds) or "-", ",".join(greens) or "-", diff or "予言どおり"))
        res[var] = (ok, C, OUT); allok &= not miss_r and not miss_g
    # 旧の 57 行の型：持ち主が変わった行を外さない旧の G4（mask＝偽）は正しい実装でも赤・新しい G4 は緑・印（region_s）が無ければ新しい G4 は門にならない（赤）
    a, b, n1, n2 = build(work, "rehome_oldmask")
    ok_new, C1, _ = g.run_gates(a, b, n1, n2, 1, SCALE, thr)
    ok_leg, C3, _ = g.run_gates(a, b, n1, n2, 1, SCALE, thr, mask=False)
    os.remove(os.path.join(a, "region_s1.tsv"))
    ok_old, C2, _ = g.run_gates(a, b, n1, n2, 1, SCALE, thr)
    L.append("旧の型（正しい実装＋持ち主が変わった州・旧の G4＝外さない）	旧 G4 赤	-	旧 G4 %s・新 G4 %s	-	%s" % ("緑" if ok_leg["G4"] else "赤", "緑" if ok_new["G4"] else "赤", "予言どおり" if (not ok_leg["G4"] and ok_new["G4"]) else "予言の外れ"))
    L.append("印が無い盤面（region_s を消した）	G4・G4m 赤	-	G4 %s・G4m %s	-	%s" % ("緑" if ok_old["G4"] else "赤", "緑" if ok_old["G4m"] else "赤", "予言どおり" if not ok_old["G4"] and not ok_old["G4m"] else "予言の外れ"))
    allok &= ok_new["G4"] and not ok_leg["G4"] and not ok_old["G4"] and not ok_old["G4m"]
    # G4m の分母の試験（事前登録 [3]）：BasisKind が 0 の行を含む盤面で、門の G4m の候補数と閾値側 mask_share の候補数が一致する。
    #   突然変異＝門の G4m の候補に BasisKind＝2 を足した旧の写し（テキストの置換）では食い違って赤。
    a, b, n1, n2 = build(work, "rehome_oldmask")
    sp = os.path.join(a, "subst_s1.tsv"); lines = open(sp, encoding="utf-8").read().split(chr(10))
    hdr = lines[0].split(chr(9)); ib = hdr.index("s2BasisKind"); iq = hdr.index("quarter")
    out_lines = [lines[0]]
    for ln in lines[1:]:
        if not ln: continue
        c = ln.split(chr(9))
        if int(c[iq]) % 2 == 1: c[ib] = "0"
        out_lines.append(chr(9).join(c))
    open(sp, "w", encoding="utf-8", newline=chr(10)).write(chr(10).join(out_lines) + chr(10))
    okd, Cd, _ = g.run_gates(a, b, n1, n2, 1, SCALE, thr)
    msh = g.mask_share(a, 1)
    same = Cd["G4m 候補 E＞0"] == msh["候補 E＞0"] and Cd["G4m 外した E＞0"] == msh["外した E＞0"] and Cd["G4m 候補 E＞0"] > Cd["G4 候補 E＞0"]
    txt = open(os.path.join(sd, "gates_ss860.py"), encoding="utf-8").read()
    old_pred = 'if has2 and cand(r) and r["s2EEnd"] > 0:'
    assert txt.count(old_pred) == 1
    mt = txt.replace(old_pred, 'if has2 and r.get("s2BasisKind") == 2 and cand(r) and r["s2EEnd"] > 0:')
    mp = os.path.join(work, "gates_ss860_mutant.py"); open(mp, "w", encoding="utf-8", newline=chr(10)).write(mt)
    sp2 = importlib.util.spec_from_file_location("gm", mp); gm = importlib.util.module_from_spec(sp2); sp2.loader.exec_module(gm)
    okm, Cm, _ = gm.run_gates(a, b, n1, n2, 1, SCALE, thr)
    same_m = Cm["G4m 候補 E＞0"] == msh["候補 E＞0"]
    L.append("G4m の分母の一致（BasisKind が 0 の行を含む盤面）	門＝閾値の候補数	-	門 %d・閾値側 %d（G4 の候補 %d）	-	%s" % (Cd["G4m 候補 E＞0"], msh["候補 E＞0"], Cd["G4 候補 E＞0"], "予言どおり" if same else "予言の外れ"))
    L.append("G4m の分母の突然変異（門に BasisKind＝2 を足した旧）	食い違い	-	門 %d・閾値側 %d	-	%s" % (Cm["G4m 候補 E＞0"], msh["候補 E＞0"], "予言どおり（食い違って赤）" if not same_m else "予言の外れ（食い違わない＝試験が拾わない）"))
    allok &= same and not same_m
    L.append("旧の型の盤面：外した E＞0 %d／候補 E＞0 %d・外した E≦0 %d" % (C1["G4 外した E＞0"], C1["G4 候補 E＞0"], C1["G4 外した E≦0"]))
    L.append("判定：" + ("予言どおり（赤が必要な所は全て赤・緑が必要な所は全て緑）" if allok else "予言と違う所あり"))
    with open(out, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(L) + "\n")
    print("\n".join(L))
    for var in ("correct",): print("\n".join(res[var][2]))
    return 0 if allok else 1

if __name__ == "__main__":
    sys.exit(main())
