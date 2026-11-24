# (1354) c142tp 設計の材料：T の道（効率化材を値段の限界で買う凹の産出・天井なし）を紙で。読むだけ。
# 生：keep_1341/base_ins（m1341 の M・種 1〜10）の subst・labor・nationgood。エンジンの関数は呼ばない（式は紙の側で書く）。
# 使い方：python -I paper1354.py <base_ins のフォルダ> <出力の txt>
import sys, math, statistics

BASE, OUT = sys.argv[1], sys.argv[2]
SEEDS = range(1, 11)
NATS = ("ロシア", "清", "イギリス")
EB, EK = 0.3, 0.08            # EffBoost・EffPerOutput（Coefficients.p02:39・:38）＝新しい数は置かない
G = EB / EK                   # 入る条件の比 3.75（EconomyStep.cs:1359 と同じ：EB×p_out＞EK×p_in ⇔ G×R＞1）
STAPLE_RGO = ("米", "野菜", "ジャガイモ", "トウモロコシ")   # 主食の RGO（Tier＝Staple・投入なし）＝StapleEffTech の母集合
BOOST = ("肥料", "馬")        # Catalog.RgoBoosterGoods（作物）
PERIODS = (("開幕", None), ("1830", 1830), ("1850", 1850), ("1880", 1880), ("1900", 1900), ("1913", 1913))

def rows(path, need):
    with open(path, encoding="utf-8") as fh:
        h = fh.readline().rstrip("\n").split("\t"); ix = [h.index(c) for c in need]
        for line in fh:
            v = line.rstrip("\n").split("\t"); yield [v[i] for i in ix]

def fl(s):
    try: return float(s)
    except ValueError: return float("nan")

def ok(*a): return all(x == x and not math.isinf(x) for x in a)

def b_now(r): return 1.0 + EB * r if True else 0                 # 今：1＋EB×EffFill（EffFill≦1）
def b_pow(R, r):                                                  # 案 A：B(e)＝(1＋e/k)^EB・e*/k＝(G R)^(1/(1−EB))−1・買えた率 r
    if not (G * R > 1): return 1.0
    return (1.0 + r * ((G * R) ** (1.0 / (1.0 - EB)) - 1.0)) ** EB
def b_log(R, r):                                                  # 案 A′：B(e)＝1＋EB ln(1＋e/k)・e*/k＝G R−1
    if not (G * R > 1): return 1.0
    return 1.0 + EB * math.log(1.0 + r * (G * R - 1.0))

IG, IS = 1.2, 2.5             # IndProductivityGain・IndProductivityScale（Coefficients.p04_trade.cs:50・:51）
def ind_mult(I): return 1.0 + IG * I / (1.0 + I / IS)          # EconomyStep.cs:841〜843・:2517（cap && !indOff の時）
SC = ("今", "丙だけ(GE)", "丙だけ(PE)", "A冪", "A冪(満)", "A対数", "A冪+丙(GE)", "A冪+丙(PE)",
      "K", "K+丙(GE)", "K+丙(PE)", "K+A冪", "K+A冪+丙(GE)", "K+A冪+丙(PE)")
per = {}
def add(k, v): per.setdefault(k, []).append(v)

for sd in SEEDS:
    # 国×年の比 R＝主食の RGO の値段（産出加重）÷効率化材の安い方（産出＞0 の物）
    pr = {}
    for yr, nat, good, q, p in rows(f"{BASE}/nationgood_s{sd}.tsv", ["year", "nation", "good", "producedQty", "priceMeanGold"]):
        if nat not in NATS or yr == "year": continue
        q, p = fl(q), fl(p)
        d = pr.setdefault((int(yr), nat), {"sw": 0.0, "s": 0.0, "b": []})
        if good in STAPLE_RGO and q > 0 and ok(p) and p > 0: d["sw"] += q * p; d["s"] += q
        if good in BOOST and q > 0 and ok(p) and p > 0: d["b"].append(p)
    Rt = {k: d["sw"] / d["s"] / min(d["b"]) for k, d in pr.items() if d["s"] > 0 and d["b"]}
    yrs = {}
    for (y, n) in Rt: yrs.setdefault(n, []).append(y)
    def R_of(nat, y):
        ys = yrs.get(nat)
        if not ys: return float("nan")
        yb = min(ys, key=lambda t: abs(t - y)); return Rt[(yb, nat)]
    # 国×四半期の工業化度（score_s の meanIndustrialization＝人口加重の TotalIndustrialization・World.cs:2218）＝州の ProductiveIndustrialization の代わり（系列が無い・上ずれ）
    ind = {}
    for y, nat, I in rows(f"{BASE}/score_s{sd}.tsv", ["year", "nation", "meanIndustrialization"]):
        if nat in NATS and ok(fl(I)): ind[(int(y), nat)] = fl(I)
    # 国×四半期：賃金 w＝Σ売れた×値段÷Σ売れた・w/mc（farmPriceOverMcIndex）・需要÷売れた
    lab = {}
    for q, nat, sold, bill, dem, pmc in rows(f"{BASE}/labor_s{sd}.tsv", ["quarter", "nation", "farmSoldPerMonth", "farmWageBillPerMonth", "farmDemandPerMonth", "farmPriceOverMcIndex"]):
        if nat not in NATS: continue
        s, b, d, m = fl(sold), fl(bill), fl(dem), fl(pmc)
        if s > 0 and ok(b, d, m): lab[(q, nat)] = (b / s, m, d / s)
    cols = ["quarter", "year", "nation", "region", "s2BasisKind", "s2BasisEngine", "s2CapEnd", "s2SoldEnd", "s2UEnd", "s2TechEnd", "s2Tech0Inst",
            "s2EffFillHead", "s2EffFill0Inst", "s3HarvestInst", "s3SrawInst", "s3QInst", "stapleNeedWeightedPrice"]
    R = []
    for v in rows(f"{BASE}/subst_s{sd}.tsv", cols):
        if v[2] not in NATS or v[4] != "2": continue
        R.append((v[0], int(v[1]), v[2], v[3]) + tuple(map(fl, v[5:])))
    pn = {}
    for r in R:
        sold, p = r[6], r[15]
        if sold > 0 and ok(p): a = pn.setdefault((r[0], r[2]), [0.0, 0.0]); a[0] += sold * p; a[1] += sold
    first = {}; firstq = {}
    for r in R:
        if r[3] not in first: first[r[3]] = r[1]; firstq[r[3]] = r[0]
    agg = {}
    for (q, yr, nat, reg, rho, cap, sold, U, T, T0, eff, eff0, f, sraw, Q, p) in R:
        if not ok(rho, cap, sold, U, T, T0, f, sraw, Q) or not (cap > 0 and U > 0 and sraw > 0 and f > 0 and T > 0 and T0 > 0): continue
        L = lab.get((q, nat)); a = pn.get((q, nat))
        if L is None or a is None or not (a[1] > 0): continue
        w, wmc, ds = L; pbar = a[0] / a[1]; z = w / pbar
        pers = [nm for nm, y in PERIODS if (y is None and yr == first[reg] and int(q) <= 3) or (y is not None and yr == y)]
        if not pers: continue
        x = sraw * f; nN = Q / x
        Rn, R0 = R_of(nat, yr), R_of(nat, first[reg])
        r_t = eff if ok(eff) else 0.0; r_0 = eff0 if ok(eff0) else 0.0
        gP = (b_pow(Rn, r_t) / b_pow(R0, r_0)) / (T / T0)
        gF = (b_pow(Rn, 1.0) / b_pow(R0, 1.0)) / (T / T0)
        gL = (b_log(Rn, r_t) / b_log(R0, r_0)) / (T / T0)
        vw = rho * T * f / z
        s = sold / cap
        def chei(g, mode):
            av = 1.0 / (1.0 + vw * g)
            s2 = cap * av if mode == "GE" else min(sold * ds, cap * av)
            return x * g * (cap - s2) / U, av
        xs = {"今": x, "A冪": x * gP, "A冪(満)": x * gF, "A対数": x * gL}
        xs["丙だけ(GE)"], a0 = chei(1.0, "GE"); xs["丙だけ(PE)"], _ = chei(1.0, "PE")
        xs["A冪+丙(GE)"], aP = chei(gP, "GE"); xs["A冪+丙(PE)"], _ = chei(gP, "PE")
        It, I0 = ind.get((yr, nat)), ind.get((first[reg], nat))
        if It is None or I0 is None: continue
        gK = ind_mult(It) / ind_mult(I0)
        xs["K"] = x * gK; xs["K+丙(GE)"], aK = chei(gK, "GE"); xs["K+丙(PE)"], _ = chei(gK, "PE")
        xs["K+A冪"] = x * gK * gP; xs["K+A冪+丙(GE)"], aKP = chei(gK * gP, "GE"); xs["K+A冪+丙(PE)"], _ = chei(gK * gP, "PE")
        for pe in pers:
            k = agg.setdefault((nat, pe), {"n": 0.0, "cap": 0.0, "sold": 0.0, "T": 0.0, "eff": 0.0, "gP": 0.0, "gF": 0.0, "gL": 0.0,
                                           "a0": 0.0, "aP": 0.0, "rev0": 0.0, "revP": 0.0, "z": [], "wmc": [], "R": [], "vw": [],
                                           "buy0": 0.0, "buyK": 0.0, "buyKP": 0.0, "gK": 0.0, "aK": 0.0, "aKP": 0.0, "revK": 0.0, "revKP": 0.0, "I": [], "zfix": 0.0, "zfixK": 0.0, "zw": 0.0,
                                           **{f"x{c}": 0.0 for c in SC}, **{f"E{c}": 0.0 for c in SC}})
            k["n"] += nN; k["cap"] += cap; k["sold"] += sold; k["T"] += nN * T; k["eff"] += nN * r_t
            k["gP"] += nN * gP; k["gF"] += nN * gF; k["gL"] += nN * gL
            k["a0"] += cap * a0; k["aP"] += cap * aP; k["rev0"] += nN * (a0 < s); k["revP"] += nN * (aP < s)
            k["z"].append(z); k["wmc"].append(wmc); k["R"].append(Rn); k["vw"].append(vw)
            k["buy0"] += nN * max(0.0, 1.0 - x); k["buyK"] += nN * max(0.0, 1.0 - x * gK); k["buyKP"] += nN * max(0.0, 1.0 - x * gK * gP)
            k["gK"] += nN * gK; k["aK"] += cap * aK; k["aKP"] += cap * aKP; k["revK"] += nN * (aK < s); k["revKP"] += nN * (aKP < s); k["I"].append(It)
            # 丙で需要が量を決める極（売れた据え置き・賃金が動く）：w′＝v_f×s÷(1−s)＝z′＝ρTf̄×g×s÷(1−s)（売れた加重）
            if s < 1: k["zfix"] += sold * rho * T * f * s / (1 - s); k["zfixK"] += sold * rho * T * f * gK * s / (1 - s); k["zw"] += sold
            for c in SC:
                k[f"x{c}"] += nN * xs[c]; k[f"E{c}"] += nN * max(0.0, xs[c] - 1.0)
    for (nat, pe), k in agg.items():
        n = k["n"]
        add((nat, pe, "T（今）"), k["T"] / n); add((nat, pe, "EffFill（今）"), k["eff"] / n)
        add((nat, pe, "R＝主食÷効率化材"), statistics.median(k["R"]))
        add((nat, pe, "z＝w÷p̄"), statistics.median(k["z"])); add((nat, pe, "w÷mc"), statistics.median(k["wmc"]))
        add((nat, pe, "mc÷p̄"), statistics.median(k["z"]) / statistics.median(k["wmc"]))
        add((nat, pe, "売れた÷能力 s"), k["sold"] / k["cap"]); add((nat, pe, "v_f÷w（今）"), statistics.median(k["vw"]))
        add((nat, pe, "g 冪"), k["gP"] / n); add((nat, pe, "g 冪(満)"), k["gF"] / n); add((nat, pe, "g 対数"), k["gL"] / n)
        add((nat, pe, "丙の a（今の T）"), k["a0"] / k["cap"]); add((nat, pe, "丙の a（A冪）"), k["aP"] / k["cap"])
        add((nat, pe, "a＜s の人口（今の T）"), k["rev0"] / n); add((nat, pe, "a＜s の人口（A冪）"), k["revP"] / n)
        add((nat, pe, "農民の市場の主食の買い÷必要量（今）"), k["buy0"] / n)
        if k["buy0"] > 0: add((nat, pe, "農民の市場の主食の買い K÷今"), k["buyK"] / k["buy0"]); add((nat, pe, "農民の市場の主食の買い K+A冪÷今"), k["buyKP"] / k["buy0"])
        add((nat, pe, "工業化度 I（国）"), statistics.median(k["I"])); add((nat, pe, "g K"), k["gK"] / n)
        add((nat, pe, "丙の a（K）"), k["aK"] / k["cap"]); add((nat, pe, "丙の a（K+A冪）"), k["aKP"] / k["cap"])
        add((nat, pe, "a＜s の人口（K）"), k["revK"] / n); add((nat, pe, "a＜s の人口（K+A冪）"), k["revKP"] / n)
        if k["zw"] > 0: add((nat, pe, "z′ 丙・需要固定（今の T）"), k["zfix"] / k["zw"]); add((nat, pe, "z′ 丙・需要固定（K）"), k["zfixK"] / k["zw"])
        for c in SC:
            add((nat, pe, f"x̄ {c}"), k[f"x{c}"] / n); add((nat, pe, f"κ_E {c}"), k[f"E{c}"] / k[f"x{c}"])

def fmt(v):
    v = sorted(v); m = statistics.median(v)
    return f"{m:.3g}〔{v[0]:.3g}〜{v[-1]:.3g}〕n{len(v)}"
out = ["# (1354) 紙：国×期×量の種の中央〔最小〜最大〕n＝種の数。x̄ と κ_E は必要量×人数の加重（paper1347 と同じ）。R は nationgood の年の値（近い年）。",
       "# 案 A：B(e)＝(1＋e/k)^0.3（冪）・1＋0.3 ln(1＋e/k)（対数）・買えた率＝今の EffFill。(満)＝買えた率 1。g＝(B_t/B_0)/(T_t/T_0)＝今の x に掛ける比（焼きの ρ は B_0 で焼き直す）。",
       "# 丙：a＝1÷(1＋v_f÷w)。GE＝売れた′＝能力×a（需要が賃金で追う極）・PE＝売れた′＝min(売れた×需要÷売れた, 能力×a)（賃金を据えた極）。"]
keys = sorted({k[2] for k in per}, key=lambda s: list(dict.fromkeys(k[2] for k in per)).index(s))
for nat in NATS:
    for pe, _ in PERIODS:
        if not any(k[0] == nat and k[1] == pe for k in per): continue
        out.append(f"## {nat} {pe}")
        for q in keys:
            v = per.get((nat, pe, q))
            if v: out.append(f"  {q}: {fmt(v)}")
with open(OUT, "wb") as fh: fh.write(("\n".join(out) + "\n").encode("utf-8"))
print("書いた", OUT, len(out), "行")
