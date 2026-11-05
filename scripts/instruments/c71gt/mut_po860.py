# (860) c71gt 固定の盤面（紙の台本・mut_po853.py の写し＋carry の盤面）：c69po の門 gates_po860.py を、合成した secession_s と rule_s で突然変異ごとに回す（事前登録 [2-2]）。
#   python -X utf8 -I mut_po860.py <作業フォルダ> <出力の表> [窓の長さ=40]
# 計器の列は正しい式で書く（正しい order_r・旧の order）。エンジンの無統治の割合 ruleUngovShare は変異ごとの目標式で 2.5% ずつ寄せて作る（期待値を列から作らない）。
# 実の生は読まない。ヌル腕＝旗 0 の側（旧の目標）で初期値の揺らしだけが違う。
import sys, os, random, math, importlib.util, shutil
sys.dont_write_bytecode = True
sd = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("g", os.path.join(sd, "gates_po860.py")); g = importlib.util.module_from_spec(spec); spec.loader.exec_module(g)

NAT = ["甲", "乙", "丙", "丁", "戊", "己"]; NREG = 8; Q = 130; SPEED = 0.025
RBASE = {"甲": 3.0, "乙": 2.5, "丙": 1.0, "丁": 0.5, "戊": 1.3, "己": 2.0}
def c01(x): return 0.0 if not (x > 0) else 1.0 if x > 1 else x
def F(x): return "nan" if x != x else "%.10g" % x

def world(variant):
    rng = random.Random(8531)
    W = {}
    for n in NAT:
        regs = []
        for r in range(NREG):
            cap = r == 0
            regs.append({"name": "%s%d" % (n, r), "pop": 300.0 if cap else rng.uniform(50, 150), "mb": 0.8 if cap else rng.uniform(0.4, 1.2),
                         "reach": 1.0 if cap else rng.uniform(0.4, 0.9), "ung0": 0.0 if cap else rng.uniform(0.1, 0.5), "ph": rng.uniform(0, 6)})
        W[n] = regs
    def R(n, q):
        if variant == "empty": return 0.2
        if variant == "escape" and q % 30 == 29: return 0.3       # 全ての国が 30 四半期ごとに 1 四半期だけ不足＝L 以上の窓が作れない
        if variant == "carry" and n == "戊" and 20 <= q <= 59: return 0.1     # 窓の前の不足が長い（持ち越しが大きい）正しい実装
        if n == "戊" and 40 <= q <= 45: return 0.3
        return RBASE[n]
    return W, R

def build(root, variant, flag0=False, eps=0.0, seed=0, mut=None):
    """flag0＝旧の目標で回す側（M′・ヌル腕）。mut＝変異（correct・M1・M2・M3・M7）。eps＝初期の無統治の揺らし。"""
    W, R = world(variant); rng = random.Random(1000 + seed)
    d = os.path.join(root, variant + ("" if mut is None else "_" + mut) + ("_f0_%d" % seed if flag0 else "")); os.makedirs(d, exist_ok=True)
    srows, rrows = [], []
    for n in NAT:
        regs = W[n]; ung = [r["ung0"] * (1 + (rng.gauss(0, eps) if eps else 0.0)) for r in regs]
        for q in range(Q):
            Rq = R(n, q); Rprev = R(n, q - 1) if q > 0 else Rq
            m = [r["mb"] * (1 + 0.05 * math.sin(q / 7.0 + r["ph"])) for r in regs]
            mbar = sum(r["pop"] * mi for r, mi in zip(regs, m)) / sum(r["pop"] for r in regs)
            for i, r in enumerate(regs):
                before = ung[i]; reach = r["reach"]
                oraw = Rq * mbar / m[i]; orderR = c01(oraw); orderOld = c01(Rprev)
                tOld = (1 - orderOld) * (1 - reach); tNew = (1 - orderR) * (1 - reach)
                if i == 0: t = 0.0
                elif flag0: t = tOld
                elif mut in (None, "correct"): t = tNew
                elif mut in ("M1", "M4"): t = (1 - c01(Rq)) * (1 - reach)
                elif mut == "M2": t = (1 - orderR * max(0.0, 1 - m[i])) * (1 - reach)
                elif mut == "M3": t = (1 - c01(c01(Rq) * mbar / m[i])) * (1 - reach)
                elif mut == "M7": t = (1 - oraw) * (1 - reach)
                else: raise SystemExit("変異が分からない " + str(mut))
                ung[i] = before * (1 - SPEED) + SPEED * t
                rrows.append("\t".join([str(q), n, r["name"], F(r["pop"]), F(ung[i]), "0"]))
                if i == 0: continue    # 首都の行は secession_s に無い
                srows.append("\t".join([str(q), n, r["name"], F(m[i]), F(Rq), F(orderOld), F(mbar), F(m[i]), F(orderR), F(reach), F(tOld), F(tNew), F(before), "0"]))
    sh = "quarter\tnation\tregion\tmilitancyMeanIndex\torderRawNowIndex\torderOldIndex\tmilitancyNationMeanIndex\tmilitancyRegionOwnIndex\torderRegionNowIndex\treachOwnShare\tungovTargetOldShare\tungovTargetNewShare\truleUngovBeforeShare\truleForeignBeforeShare"
    with open(os.path.join(d, "secession_s1.tsv"), "w", encoding="utf-8", newline="\n") as f: f.write(sh + "\n" + "\n".join(srows) + "\n")
    with open(os.path.join(d, "rule_s1.tsv"), "w", encoding="utf-8", newline="\n") as f: f.write("quarter\tnation\tregion\tpopK\truleUngovShare\truleForeignShare\n" + "\n".join(rrows) + "\n")
    return d

def main():
    global Q
    work, out = sys.argv[1], sys.argv[2]; L = int(sys.argv[3]) if len(sys.argv) > 3 else 40
    Q = max(130, 2 * L + 20)      # 窓 L の後に母集合の行が残るだけの四半期を作る
    shutil.rmtree(work, ignore_errors=True); os.makedirs(work)
    base = build(work, "correct", True, 0.0, 0); n1 = build(work, "correct", True, 0.02, 1); n2 = build(work, "correct", True, 0.02, 2)
    lines = ["(860) 固定の盤面（合成・6 国×8 州・%d 四半期・窓 L＝%d・母集合の下限の倍率 0.2）の突然変異の表・c69po の門 gates_po860.py" % (Q, L),
             "盤面\t予言：赤\t予言：緑\t実際に赤\t実際に緑\t予言との差"]
    cache = {}; allok = True
    PRED = [("correct", "exact", [], ["G0", "G1", "G1u", "G2a", "G2b"]),
            ("M1", "exact", ["G0", "G1"], []), ("M4", "exact", ["G0", "G1"], []),
            ("M2", "exact", ["G0", "G2a", "G2b"], []), ("M3", "exact", ["G0", "G2a", "G2b"], []), ("M7", "exact", ["G0", "G1u", "G2a", "G2b"], [])]
    for mut, mode, red, green in PRED:
        arm = build(work, "correct", False, 0.0, 0, mut)
        ok, O = g.run_gates(arm, base, n1, n2, 1, 0.2, L, mode, cache)
        reds = [k for k, v in ok.items() if not v]; greens = [k for k, v in ok.items() if v]
        mr = [k for k in red if k not in reds]; mg = [k for k in green if k not in greens]; ex = [k for k in reds if k not in red and mut != "correct"]
        diff = ("予言の赤が緑：" + ",".join(mr) + "　") * bool(mr) + ("予言の緑が赤：" + ",".join(mg) + "　") * bool(mg) + ("予言に書かなかった赤：" + ",".join(ex)) * bool(ex)
        lines.append("%s（%s）\t%s\t%s\t%s\t%s\t%s" % (mut, mode, ",".join(red) or "-", ",".join(green) or "-", ",".join(reds) or "-", ",".join(greens) or "-", diff or "予言どおり"))
        allok &= not mr and not mg
        if mut == "correct": ok_correct = ok; O_correct = O
    # 門の突然変異：窓の読みを最後の四半期だけにする（途中に不足のある盤面＝戊の 40〜45 の不足の後）
    arm = build(work, "correct", False, 0.0, 0, "correct")
    ok_last, O_last = g.run_gates(arm, base, n1, n2, 1, 0.2, L, "last", cache)
    lines.append("correct・窓の読みを最後の四半期だけにした門（G2b の突然変異）\tG2b\t-\t%s\t%s\t%s" % (",".join(k for k, v in ok_last.items() if not v) or "-", ",".join(k for k, v in ok_last.items() if v), "予言どおり" if not ok_last["G2b"] else "予言の外れ（G2b が緑のまま＝この盤面では門の突然変異を拾わない）"))
    allok &= not ok_last["G2b"]
    # 空の盤面・逃がした盤面（旗 0 の側も同じ世界で作り直す）
    for var in ("empty", "escape"):
        b0 = build(work, var, True, 0.0, 0); a1 = build(work, var, True, 0.02, 1); a2 = build(work, var, True, 0.02, 2); arm = build(work, var, False, 0.0, 0, "correct")
        ok, O = g.run_gates(arm, b0, a1, a2, 1, 0.2, L, "exact", {})
        reds = [k for k, v in ok.items() if not v]
        want = ["G2a", "G2b"] if var == "empty" else ["G2b"]
        lines.append("%s の盤面\t%s\t-\t%s\t%s\t%s" % (var, ",".join(want), ",".join(reds) or "-", ",".join(k for k, v in ok.items() if v) or "-", "予言どおり" if all(w in reds for w in want) else "予言の外れ"))
        allok &= all(w in reds for w in want)
    # carry の盤面＝正しい実装だが窓の前の不足が長い（持ち越しが大きい）。形 c は持ち越しを差し引くので全て緑（853 の形 G2b0 の赤は記録）
    b0 = build(work, "carry", True, 0.0, 0); a1 = build(work, "carry", True, 0.02, 1); a2 = build(work, "carry", True, 0.02, 2); arm = build(work, "carry", False, 0.0, 0, "correct")
    ok, O = g.run_gates(arm, b0, a1, a2, 1, 0.2, L, "exact", {})
    reds = [k for k, v in ok.items() if not v]
    g2b0 = [x for x in O if x.startswith("G2b0")][0]
    lines.append("carry の盤面（正しい実装・窓の前の不足が長い）\t-\tG0,G1,G1u,G2a,G2b\t%s\t%s\t%s" % (",".join(reds) or "-", ",".join(k for k, v in ok.items() if v) or "-", "予言どおり" if not reds else "予言の外れ"))
    lines.append("  " + g2b0)
    allok &= not reds
    lines.append("判定：" + ("予言どおり（赤が必要な所は全て赤・緑が必要な所は全て緑）" if allok else "予言と違う所あり"))
    lines.append("--- 正しい盤面の門の出力")
    lines += O_correct
    open(out, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n"); print("\n".join(lines))
    return 0 if allok else 1

if __name__ == "__main__":
    sys.exit(main())
