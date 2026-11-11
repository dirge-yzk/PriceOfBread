# (872) c72ds 紙の台本（読むだけ・シミュを回さない・エンジンの関数を呼ばない）
# 読む生：subst_s の 8 表の生（種 1〜10）
#   base＝D:/PoB_work/keep_852/base_ins（m852 の盤面・旗 0・計器の反実仮想 ρ・σ）
#   n1・n2＝D:/PoB_work/keep_852/n1・n2（m852 のヌル腕・旗 0）
#   arm＝D:/PoB_work/keep_859_rej/c70ssB_10/ins（m852 の盤面の SubsistenceStage2＝1・採らなかった腕）
# 出す物：
#   [R] σ の再現（min(1, ρ×U×T÷(必要量×人数)) と s2SigmaInst の差）
#   [O] 開幕（各州の最初の行）の食べる割合の分布
#   [W] 窓ごとの cap÷人数の漂い fC・技術の比 T/T0・売れ残りの比・σ/d0・切る前の σ が 1 を超える行（余り E_p＞0）
#   [C] 技術の比と σ/d0・売れた÷能力の順位相関（窓ごと）
#   [N] σ/d0 の窓の中央の 3 本（base・n1・n2）の幅 w と腕の値
#   [T] 格ごとの fC の中央
# 使い方：python paper872.py <出力の .txt>
import csv, math, sys, os
from array import array

SETS = [
    ("base", "D:/PoB_work/keep_852/base_ins"),
    ("n1", "D:/PoB_work/keep_852/n1"),
    ("n2", "D:/PoB_work/keep_852/n2"),
    ("arm", "D:/PoB_work/keep_859_rej/c70ssB_10/ins"),
]
WINS = [(1783, 1785), (1786, 1799), (1800, 1849), (1850, 1899), (1900, 1950)]
COLS = ["quarter", "year", "nation", "region", "tier", "subPopK", "subRateWeighted", "stapleNeedPerCapita",
        "engineSubOutputPerMonth", "s2RhoSource", "s2D0Inst", "s2RhoInst", "s2SigmaInst", "s2CapHead", "s2SoldHead",
        "s2UHead", "s2TechHead", "s2Tech0Inst", "s2HeadsEnd", "soldOverCap"]


def fnum(s):
    try:
        v = float(s)
    except ValueError:
        return float("nan")
    return v


def win_of(y):
    for i, (a, b) in enumerate(WINS):
        if a <= y <= b:
            return i
    return -1


def pct(arr, q):
    if not arr:
        return float("nan")
    s = sorted(arr)
    k = (len(s) - 1) * q
    lo = int(math.floor(k)); hi = min(lo + 1, len(s) - 1)
    return s[lo] + (s[hi] - s[lo]) * (k - lo)


def wpct(vals, wts, q):
    pairs = sorted((v, w) for v, w in zip(vals, wts) if v == v and w > 0)
    if not pairs:
        return float("nan")
    tot = sum(w for _, w in pairs); acc = 0.0
    for v, w in pairs:
        acc += w
        if acc >= q * tot:
            return v
    return pairs[-1][0]


def ranks(a):
    idx = sorted(range(len(a)), key=lambda i: a[i])
    r = [0.0] * len(a)
    i = 0
    while i < len(idx):
        j = i
        while j + 1 < len(idx) and a[idx[j + 1]] == a[idx[i]]:
            j += 1
        avg = (i + j) / 2.0
        for k in range(i, j + 1):
            r[idx[k]] = avg
        i = j + 1
    return r


def spearman(x, y):
    n = len(x)
    if n < 3:
        return float("nan")
    rx, ry = ranks(x), ranks(y)
    mx = sum(rx) / n; my = sum(ry) / n
    sxy = sum((a - mx) * (b - my) for a, b in zip(rx, ry))
    sxx = sum((a - mx) ** 2 for a in rx); syy = sum((b - my) ** 2 for b in ry)
    return sxy / math.sqrt(sxx * syy) if sxx > 0 and syy > 0 else float("nan")


def read_set(name, d):
    W = [{k: array("d") for k in ("fC", "TT0", "UU0", "sd", "sraw", "soc", "dsoc", "ltt", "lsd")} for _ in WINS]
    tierFC = {}
    opening = {"d0": [], "w": [], "eat": [], "nation": [], "sraw": []}
    rep = {"n": 0, "max": 0.0, "gt1e9": 0, "gt1e3": 0}
    seeds_read = 0
    for sd in range(1, 11):
        p = os.path.join(d, "subst_s%d.tsv" % sd)
        if not os.path.exists(p):
            continue
        seeds_read += 1
        ref = {}
        with open(p, "r", encoding="utf-8", newline="") as f:
            rd = csv.reader(f, delimiter="\t")
            hdr = next(rd)
            ix = {c: hdr.index(c) for c in COLS}
            for row in rd:
                src = fnum(row[ix["s2RhoSource"]])
                if not (src == 1 or src == 2):
                    continue
                rho = fnum(row[ix["s2RhoInst"]]); d0 = fnum(row[ix["s2D0Inst"]])
                cap = fnum(row[ix["s2CapHead"]]); sold = fnum(row[ix["s2SoldHead"]]); u = fnum(row[ix["s2UHead"]])
                t = fnum(row[ix["s2TechHead"]]); heads = fnum(row[ix["s2HeadsEnd"]]); need = fnum(row[ix["stapleNeedPerCapita"]])
                sig = fnum(row[ix["s2SigmaInst"]])
                if not (rho > 0 and d0 > 0 and cap > 0 and heads > 0 and t > 0 and need > 0 and u == u):
                    continue
                y = int(fnum(row[ix["year"]])); wi = win_of(y)
                sraw = rho * u * t / (need * heads)
                if sig == sig:
                    e = abs(min(1.0, sraw) - sig)
                    rep["n"] += 1; rep["max"] = max(rep["max"], e)
                    if e > 1e-9: rep["gt1e9"] += 1
                    if e > 1e-3: rep["gt1e3"] += 1
                key = row[ix["region"]]
                soc = min(1.0, max(0.0, sold / cap))
                if key not in ref:
                    ref[key] = (cap / heads, t, 1.0 - soc, soc)
                    sub = fnum(row[ix["subPopK"]])
                    opening["d0"].append(d0); opening["w"].append(sub if sub > 0 else 0.0)
                    eng = fnum(row[ix["engineSubOutputPerMonth"]])
                    opening["eat"].append(eng / (need * sub) if sub > 0 else float("nan"))
                    opening["nation"].append(row[ix["nation"]]); opening["sraw"].append(sraw)
                c0, t0, u0, soc0 = ref[key]
                if wi < 0:
                    continue
                fc = (cap / heads) / c0; tt = t / t0
                uu = (1.0 - soc) / u0 if u0 > 0 else float("nan")
                s_d = (sig / d0) if sig == sig else float("nan")
                w = W[wi]
                w["fC"].append(fc); w["TT0"].append(tt); w["UU0"].append(uu); w["sd"].append(s_d)
                w["sraw"].append(sraw); w["soc"].append(soc); w["dsoc"].append(soc - soc0)
                w["ltt"].append(math.log(tt)); w["lsd"].append(math.log(s_d) if s_d > 0 else float("nan"))
                tier = row[ix["tier"]]
                tierFC.setdefault((tier, wi), array("d")).append(fc)
    return W, tierFC, opening, rep, seeds_read


def fmt(v, n=4):
    return ("%." + str(n) + "f") % v if v == v else "nan"


def main():
    out = sys.argv[1]
    lines = []
    def P(s=""):
        lines.append(s)
    P("(872) c72ds 紙の数（読むだけ）・盤面 m852・種 1〜10・行＝州×四半期・担い手の行（s2RhoSource が 1 か 2）だけ")
    P("⚠限界：σraw＝ρ×U(月の頭)×T(月の頭)÷(必要量×人数〔s2HeadsEnd＝四半期の末の頭数〕)＝四半期の末の月の値・州の鍵は州の名（持ち主が変わっても同じ鍵）・行の中央（人口の重みなし・開幕だけ人口の重み）")
    res = {}
    for name, d in SETS:
        res[name] = read_set(name, d)
    for name, _ in SETS:
        W, tierFC, op, rep, ns = res[name]
        P("")
        P("== %s（種 %d 本）" % (name, ns))
        P("[R] 再現：行 %d・|min(1,σraw)−s2SigmaInst| の最大 %.3g・1e-9 超 %d 行・1e-3 超 %d 行" % (rep["n"], rep["max"], rep["gt1e9"], rep["gt1e3"]))
        vals = {}
        for v in op["d0"]:
            vals[round(v, 4)] = vals.get(round(v, 4), 0) + 1
        P("[O] 開幕の d0（焼く元の割合）の州の数：" + "・".join("%s=%d" % (k, vals[k]) for k in sorted(vals)))
        nat = {}
        for n_, v in zip(op["nation"], op["d0"]):
            if abs(v - 0.7) > 1e-9:
                nat[(n_, round(v, 4))] = nat.get((n_, round(v, 4)), 0) + 1
        P("[O] 0.7 でない d0 の国（国・値・州×種）：" + "・".join("%s %s(%d)" % (k[0], k[1], c) for k, c in sorted(nat.items())))
        P("[O] 開幕の行の食べた量÷(必要量×人数)（収穫係数を含む・エンジンの自給の産出）人口の重み p10/p50/p90：%s / %s / %s" % (
            fmt(wpct(op["eat"], op["w"], 0.1)), fmt(wpct(op["eat"], op["w"], 0.5)), fmt(wpct(op["eat"], op["w"], 0.9))))
        P("[O] 開幕の行の σraw（切る前）p10/p50/p90：%s / %s / %s・1 超の州 %d／%d" % (
            fmt(pct(op["sraw"], 0.1)), fmt(pct(op["sraw"], 0.5)), fmt(pct(op["sraw"], 0.9)), sum(1 for v in op["sraw"] if v > 1), len(op["sraw"])))
        P("[W] 窓｜行｜fC p10/p50/p90｜T/T0 p50/p90/最大・1.2 超の割合｜(1−soc)/(1−soc0) p50｜σ/d0 p10/p50/p90｜σraw＞1 の割合（余り E_p＞0）・その行の余り÷作った量の平均")
        for wi, (a, b) in enumerate(WINS):
            w = W[wi]; n = len(w["fC"])
            if n == 0:
                P("  %d-%d｜0" % (a, b)); continue
            sr = w["sraw"]; over = [v for v in sr if v > 1]
            tt = list(w["TT0"])
            P("  %d-%d｜%d｜%s/%s/%s｜%s/%s/%s・%s｜%s｜%s/%s/%s｜%s・%s" % (
                a, b, n, fmt(pct(w["fC"], .1)), fmt(pct(w["fC"], .5)), fmt(pct(w["fC"], .9)),
                fmt(pct(tt, .5)), fmt(pct(tt, .9)), fmt(max(tt)), fmt(sum(1 for v in tt if v > 1.2) / n),
                fmt(pct([v for v in w["UU0"] if v == v], .5)),
                fmt(pct([v for v in w["sd"] if v == v], .1)), fmt(pct([v for v in w["sd"] if v == v], .5)), fmt(pct([v for v in w["sd"] if v == v], .9)),
                fmt(len(over) / n, 5), fmt(sum(1 - 1 / v for v in over) / len(over) if over else float("nan"))))
        P("[C] 窓｜順位相関 ρs(log T/T0, log σ/d0)｜ρs(log T/T0, Δsoc)｜ρs(Δsoc, log σ/d0)")
        for wi, (a, b) in enumerate(WINS):
            w = W[wi]
            idx = [i for i in range(len(w["ltt"])) if w["lsd"][i] == w["lsd"][i]]
            if len(idx) > 200000:
                step = len(idx) // 200000 + 1; idx = idx[::step]
            x = [w["ltt"][i] for i in idx]; y = [w["lsd"][i] for i in idx]; z = [w["dsoc"][i] for i in idx]
            P("  %d-%d｜%s｜%s｜%s（行 %d）" % (a, b, fmt(spearman(x, y), 3), fmt(spearman(x, z), 3), fmt(spearman(z, y), 3), len(idx)))
        P("[T] 格ごとの fC の中央（窓の順）")
        tiers = sorted(set(k[0] for k in tierFC))
        for tr in tiers:
            P("  %s｜%s" % (tr, "・".join(fmt(pct(tierFC.get((tr, wi), []), .5)) for wi in range(len(WINS)))))
    P("")
    P("== [N] σ/d0 の窓の中央：base・n1・n2（旗 0 の反実仮想）の範囲 w と腕")
    for wi, (a, b) in enumerate(WINS):
        med = {}
        for name, _ in SETS:
            sdv = [v for v in res[name][0][wi]["sd"] if v == v]
            med[name] = pct(sdv, .5)
        nul = [med["base"], med["n1"], med["n2"]]
        P("  %d-%d｜base %s・n1 %s・n2 %s｜w %s｜arm %s｜arm−base %s" % (a, b, fmt(med["base"]), fmt(med["n1"]), fmt(med["n2"]),
            fmt(max(nul) - min(nul), 5), fmt(med["arm"]), fmt(med["arm"] - med["base"], 5)))
    P("== [N2] fC の窓の中央：base・n1・n2・arm")
    for wi, (a, b) in enumerate(WINS):
        P("  %d-%d｜%s" % (a, b, "・".join("%s %s" % (name, fmt(pct(res[name][0][wi]["fC"], .5))) for name, _ in SETS)))
    data = ("\n".join(lines) + "\n").encode("utf-8")
    with open(out, "wb") as f:
        f.write(data)


main()
