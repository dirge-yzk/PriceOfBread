# (882) c74ts 新しい門 GW〈働いた・世界の κ・T÷T0＞1.1 の行〉と G5h〈壊さない・σ の外の倍率＝収穫係数の |ln H|〉（agg882.py の json を読む・エンジンの関数を呼ばない）
#   GW（両側）：窓ごと・種ごとに κ＝1−Σ(sp×σ)÷Σsp（RhoSource＝1・BasisKind＝2・T÷T0＞1.1 の行）。
#     働いた側：中央_種(κ_腕−κ0_基線) ＞ 中央_種(W)　W＝ヌル 3 本（基線・N1・N2・旗 0）の κ0 の範囲（種ごと）
#     上限の側：|中央_種(κ_腕−κ′_基線)| ≦ 0.5×E　κ′＝基線の盤面の Σ2′ の紙・E＝中央_種(κ′_基線−κ0_基線)＝紙の効き（README ★(773)＝上限はその形に固有の誤りの量：効きを 2 倍にする Mu6 の誤りは E 1 回分＝その半分）。記録：W′＝ヌル 3 本の κ′ の範囲
#     評価する窓＝閾値の表（--make-thresholds）が置く：行の下限・一つ抜きの 6 通りで〈正しい実装の紙が落ちる〉か〈旗 0 が通る〉が 2 通り以上の窓は外す。評価する窓が 2 以上・全て通る。
#   G5h（壊さない）：窓ごと・種ごとに h＝Σ(sp×|ln H|)÷Σsp（H＝engineSubOutputPerMonth÷nominalSubPerMonth・同じ母集合の全ての T の行）。
#     |中央_種(h_腕−h_基線)| ≦ 0.5×中央_種(h_基線)（σ の外の倍率 Mu2＝H を 2 回掛ける形の固有の誤り＝h が 2 倍の半分）・全ての窓（行 500 以上×種の倍率）。
#     記録：同じ差をヌル 3 本の h の範囲の中央と並べる（門ではない）。
#   呼び方：
#     閾値の表：python -X utf8 gw882.py --make-thresholds <ヌルの json（M・N1・N2）> <出力の表> [種 1-10]
#     紙       ：python -X utf8 gw882.py --paper <ヌルの json> <閾値の表> <出力> [種 1-10]   （OK・Mu0・Mu1・Mu2・Mu3・Mu5・Mu6 を基線の盤面の紙で）
#     腕       ：python -X utf8 gw882.py --arm <腕の json の名=パス> <ヌルの json> <閾値の表> <出力> [種 1-10]
#     （S11 は --base-name A11 でヌル腕が a11 の 1 本＝幅は数えられない＝〈近似・記録〉で基線の幅の表を使う）
import sys, json, statistics as st, os

WINS = ("1783-85", "1786-99", "1800-49", "1850-99", "1900-50")
PRE = "k_t3_"           # T÷T0＞1.1 の升（agg882.py）
NMIN = 1000             # 窓×10 種の行の下限（種の倍率で縮める）
H_REL = 0.5             # G5h の上限＝h_基線の半分（Mu2 の固有の誤り＝h_基線そのもの の半分）

def kap(a, key, pre=PRE):
    return 1 - a[pre + key] / a[pre + "sp"] if a and a.get(pre + "sp", 0) > 0 else float("nan")

def hh(a):
    return a["h_abs"] / a["h_sp"] if a and a.get("h_sp", 0) > 0 else float("nan")

def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float("nan")

def seeds_of(s):
    a, b = s.split("-"); return [str(i) for i in range(int(a), int(b) + 1)]

MUKEY = {"OK": "s2", "Mu0": "s0", "Mu1": "m1", "Mu2": "s2", "Mu3": "m3", "Mu5": "s2", "Mu6": "m6"}

def gw_window(arm, base, others, wn, seeds, arm_key="s0"):
    """arm・base・others は {種: {窓: 和}}。戻り (行, 働いた側の中央 Δ, W, 上限の側の中央, W′)。others は幅に入る盤面（base を含めて数える）。"""
    D, U, Wv, W2, E = [], [], [], [], []; n = 0
    for s in seeds:
        a = arm.get(s, {}).get(wn); b = base.get(s, {}).get(wn); oo = [o.get(s, {}).get(wn) for o in others]
        if not a or not b or not all(oo): continue
        ka = kap(a, arm_key); k0 = [kap(b, "s0")] + [kap(o, "s0") for o in oo]; k2 = [kap(b, "s2")] + [kap(o, "s2") for o in oo]
        if any(v != v for v in [ka] + k0 + k2): continue
        D.append(ka - k0[0]); U.append(ka - k2[0]); Wv.append(max(k0) - min(k0)); W2.append(max(k2) - min(k2)); E.append(k2[0] - k0[0]); n += a.get(PRE + "n", 0)
    return n, med(D), med(Wv), med(U), med(W2), med(E)

U_REL = 0.5             # GW の上限の側＝紙の効き E の半分（Mu6 の固有の誤り＝E 1 回分）

def gw_pass(n, d, w, u, w2, e, nmin):
    work = n >= nmin and d == d and w == w and d > w
    upper = n >= nmin and u == u and e == e and e > 0 and abs(u) <= U_REL * e
    return work, upper

def make_thresholds(nulljson, out, seeds):
    R = json.load(open(nulljson, encoding="utf-8")); B = ("M", "N1", "N2"); sc = len(seeds) / 10.0
    triples = [(a, x, y) for a in B for x in B for y in B if len({a, x, y}) == 3]
    L = ["# (882) gw882.py の閾値（旗 0〔S1-a・m873〕の base_ins・N1・N2 の生だけ・agg882.py の json・腕の数は使わない）",
         "# GW の評価する窓：行（T÷T0＞1.1・10 種）≧%d×種の倍率 かつ 一つ抜きの 6 通り（腕＝1 本の盤面・基線＝もう 1 本・幅＝基線と残りの 1 本の 2 本）で" % NMIN,
         "#   〈正しい実装の紙（腕の盤面の κ′）が働いた側か上限の側で落ちる〉通りが 2 未満 かつ〈旗 0（腕の盤面の κ0）が働いた側で通る〉通りが 2 未満 の窓だけ評価する（1 は評価・0 は外す）。"]
    for wn in WINS:
        okfail = 0; mu0pass = 0; per = []
        for (a, x, y) in triples:
            A, X, Y = R[a], R[x], R[y]
            n, d, w, u, w2, e = gw_window(A, X, [Y], wn, seeds, "s2")
            wk, up = gw_pass(n, d, w, u, w2, e, NMIN * sc)
            if not (wk and up): okfail += 1
            n0, d0, w0, u0, w20, e0 = gw_window(A, X, [Y], wn, seeds, "s0")
            wk0, _ = gw_pass(n0, d0, w0, u0, w20, e0, NMIN * sc)
            if wk0: mu0pass += 1
            n6, d6, w6, u6, w26, e6 = gw_window(A, X, [Y], wn, seeds, "m6")
            _, up6 = gw_pass(n6, d6, w6, u6, w26, e6, NMIN * sc)
            per.append("%s%s%s：OK Δ %.5f W %.5f U %.5f 0.5E %.5f W′ %.5f／旗0 Δ %.5f／Mu6 U %.5f 上限 %s" % (a, x, y, d, w, u, U_REL * e, w2, d0, u6, "緑" if up6 else "赤"))
        n, d, w, u, w2, e = gw_window(R["M"], R["M"], [R["N1"], R["N2"]], wn, seeds, "s2")
        ev = 1 if (n >= NMIN * sc and okfail < 2 and mu0pass < 2) else 0
        L.append("GW_eval_%s\t%d\t行 %d・紙（基線の盤面）の効き E %.5f・W %.5f・E÷W %.2f・W′ %.5f（記録）・一つ抜き：正しい実装の紙が落ちた通り %d／6・旗 0 が働いた側で通った通り %d／6" % (
            wn, ev, n, e, w, e / w if w > 0 else float("inf"), w2, okfail, mu0pass))
        L.append("GW_W_%s\t%.6f\t種 1〜10 のヌル 3 本の κ0 の範囲の中央（S11 の読み〔ヌル腕が a11 の 1 本〕で幅の代わりに使う＝近似・記録）" % (wn, w))
        for p in per: L.append("#   " + p)
    # G5h の記録の幅（ヌル 3 本の h の範囲の中央）と一つ抜き
    for wn in WINS:
        rng, hb = [], []
        for s in seeds:
            v = [hh(R[b].get(s, {}).get(wn)) for b in B]
            if any(x != x for x in v): continue
            rng.append(max(v) - min(v)); hb.append(v[0])
        fails = 0
        for (a, x, y) in triples:
            dd = med([hh(R[a][s][wn]) - hh(R[x][s][wn]) for s in seeds if wn in R[a].get(s, {}) and wn in R[x].get(s, {})])
            hx = med([hh(R[x][s][wn]) for s in seeds if wn in R[x].get(s, {})])
            if not (abs(dd) <= H_REL * hx): fails += 1
        L.append("G5h_null_%s\t%.6f\th_基線の中央 %.5f・上限 %.5f（×%.1f）・ヌル 3 本の範囲の中央 %.6f（記録）・一つ抜きの空振り %d／6" % (wn, med(rng), med(hb), H_REL * med(hb), H_REL, med(rng), fails))
    with open(out, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(L) + "\n")
    return L

def read_thr(p):
    T = {}
    for line in open(p, encoding="utf-8"):
        if line.startswith("#") or "\t" not in line: continue
        k, v = line.rstrip("\n").split("\t")[:2]
        try: T[k] = float(v)
        except ValueError: pass
    return T

def evaluate(arm, armkey, R, T, seeds, hmul=1.0, label=""):
    """arm＝{種: {窓: 和}}・armkey＝腕の σ の和の鍵（腕の json は s0＝エンジンの σ・紙は MUKEY）。hmul＝G5h の紙の突然変異（Mu2 は h を 2 倍）。"""
    sc = len(seeds) / 10.0; ok = {}; L = []
    nev = 0; allp = True
    for wn in WINS:
        n, d, w, u, w2, e = gw_window(arm, R["M"], [R["N1"], R["N2"]], wn, seeds, armkey)
        wk, up = gw_pass(n, d, w, u, w2, e, NMIN * sc)
        ev = T.get("GW_eval_" + wn, 0) == 1
        L.append("  GW 窓 %s：行 %d・働いた側 中央(κ腕−κ0基線) %.5f ＞ W %.5f → %s・上限の側 |中央(κ腕−κ′基線)| %.5f ≦ 0.5×E %.5f → %s（記録：W′ %.5f の%s）→ %s" % (
            wn, n, d, w, wk, abs(u), U_REL * e if e == e else float("nan"), up, w2, "内" if abs(u) <= w2 else "外", ("通る" if wk and up else "落ちる") if ev else "評価しない（閾値の表）"))
        if ev: nev += 1; allp = allp and wk and up
    ok["GW"] = nev >= 2 and allp
    L.append("GW 働いた（世界の κ・T÷T0＞1.1）：評価した窓 %d（2 以上）・全て通る %s → %s" % (nev, allp, ok["GW"]))
    allh = True; nh = 0
    for wn in WINS:
        dd, hb, ww = [], [], []
        for s in seeds:
            a = arm.get(s, {}).get(wn); b = R["M"].get(s, {}).get(wn)
            if not a or not b: continue
            ha = hh(a) * hmul; h0 = hh(b)
            dd.append(ha - h0); hb.append(h0); ww.append(max(hh(R[k][s][wn]) for k in ("M", "N1", "N2")) - min(hh(R[k][s][wn]) for k in ("M", "N1", "N2")))
        nrow = sum(arm.get(s, {}).get(wn, {}).get("h_n", 0) for s in seeds)
        if nrow < 500 * sc: L.append("  G5h 窓 %s：行 %d（下限 %d に足りない）→ 評価しない" % (wn, nrow, int(500 * sc))); continue
        nh += 1; p = abs(med(dd)) <= H_REL * med(hb); allh = allh and p
        L.append("  G5h 窓 %s：行 %d・|中央(h腕−h基線)| %.5f ≦ %.1f×h基線 %.5f → %s（記録：ヌル 3 本の範囲の中央 %.5f の %s）" % (
            wn, nrow, abs(med(dd)), H_REL, H_REL * med(hb), "通る" if p else "落ちる", med(ww), "内" if abs(med(dd)) <= med(ww) else "外"))
    ok["G5h"] = nh >= 2 and allh
    L.append("G5h 壊さない（σ の外の倍率・収穫係数の |ln H|）：評価した窓 %d・全て通る %s → %s" % (nh, allh, ok["G5h"]))
    L.insert(0, "== %s：%s" % (label, "・".join("%s=%s" % (k, "緑" if v else "赤") for k, v in ok.items())))
    return ok, L

def main():
    a = sys.argv[1:]
    if a[0] == "--make-thresholds":
        seeds = seeds_of(a[3] if len(a) > 3 else "1-10"); L = make_thresholds(a[1], a[2], seeds); print("\n".join(L)); return 0
    if a[0] == "--paper":
        R = json.load(open(a[1], encoding="utf-8")); T = read_thr(a[2]); out = a[3]; seeds = seeds_of(a[4] if len(a) > 4 else "1-10")
        ALL = []
        for mut in ("OK", "Mu0", "Mu1", "Mu2", "Mu3", "Mu5", "Mu6"):
            ok, L = evaluate(R["M"], MUKEY[mut], R, T, seeds, 2.0 if mut == "Mu2" else 1.0, mut)
            ALL += L + [""]
        with open(out, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(ALL) + "\n")
        print("\n".join(ALL)); return 0
    if a[0] == "--s11":   # 近似・記録：腕の json・a11 の json（名 A11）・閾値の表・出力・種 11-20
        nm, p = a[1].split("=", 1); RA = json.load(open(p, encoding="utf-8"))[nm]; A11 = json.load(open(a[2], encoding="utf-8"))["A11"]; T = read_thr(a[3]); out = a[4]
        seeds = seeds_of(a[5] if len(a) > 5 else "11-20"); L = ["(882) S11 の読み（近似・記録＝ヌル腕が a11 の 1 本・幅 W は種 1〜10 の表の値）：腕 %s" % p]
        allp = True; nev = 0
        for wn in WINS:
            D, U, E = [], [], []; n = 0
            for s in seeds:
                x, b = RA.get(s, {}).get(wn), A11.get(s, {}).get(wn)
                if not x or not b: continue
                ka, k0, k2 = kap(x, "s0"), kap(b, "s0"), kap(b, "s2")
                if any(v != v for v in (ka, k0, k2)): continue
                D.append(ka - k0); U.append(ka - k2); E.append(k2 - k0); n += x.get(PRE + "n", 0)
            w = T.get("GW_W_" + wn, float("nan")); ev = T.get("GW_eval_" + wn, 0) == 1
            wk, up = gw_pass(n, med(D), w, med(U), float("nan"), med(E), NMIN)
            L.append("  GW 窓 %s：行 %d・中央(κ腕−κ0 a11) %.5f ＞ W（表）%.5f → %s・|中央(κ腕−κ′ a11)| %.5f ≦ 0.5×E %.5f → %s → %s" % (
                wn, n, med(D), w, wk, abs(med(U)), U_REL * med(E), up, ("通る" if wk and up else "落ちる") if ev else "評価しない"))
            if ev: nev += 1; allp = allp and wk and up
        L.append("GW（S11・近似）：評価した窓 %d・全て通る %s" % (nev, allp))
        for wn in WINS:
            dd = [hh(RA[s][wn]) - hh(A11[s][wn]) for s in seeds if wn in RA.get(s, {}) and wn in A11.get(s, {})]
            hb = [hh(A11[s][wn]) for s in seeds if wn in A11.get(s, {})]
            L.append("  G5h 窓 %s：|中央(h腕−h a11)| %.5f ≦ 0.5×h a11 %.5f → %s" % (wn, abs(med(dd)), H_REL * med(hb), abs(med(dd)) <= H_REL * med(hb)))
        with open(out, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(L) + "\n")
        print("\n".join(L)); return 0
    if a[0] == "--arm":
        nm, p = a[1].split("=", 1); RA = json.load(open(p, encoding="utf-8")); R = json.load(open(a[2], encoding="utf-8")); T = read_thr(a[3]); out = a[4]
        seeds = seeds_of(a[5] if len(a) > 5 else "1-10")
        ok, L = evaluate(RA[nm], "s0", R, T, seeds, 1.0, "腕 " + nm)
        with open(out, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(L) + "\n")
        print("\n".join(L)); return 0 if all(ok.values()) else 1
    raise SystemExit("引数")

if __name__ == "__main__":
    sys.exit(main())
