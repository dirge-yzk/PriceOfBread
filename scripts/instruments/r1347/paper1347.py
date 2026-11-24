# (1347) c141hl 設計の材料：丙＝自給の世帯の労働の配分を値段と賃金の比で（紙・読むだけ）
# 生：keep_1341/base_ins（m1341 の M・種 1〜10）の subst・labor・fd。エンジンの関数は呼ばない（式は紙の側で書く）。
# 使い方：python -I paper1347.py <base_ins のフォルダ> <出力の txt>
import sys, math, statistics

BASE, OUT = sys.argv[1], sys.argv[2]
SEEDS = range(1, 11)
NATS = ("ロシア", "清")
T_CEIL = 1.3   # 1＋EffBoost（Coefficients.p02:39 の 0.3）＝T の上限（EffFill＝1）
SLOPE = 0.5    # SupplySlope（Coefficients.cs:16）＝今の人力の供給曲線の満額の値段＝mc×(1＋これ)

def rows(path, need):
    with open(path, encoding="utf-8") as fh:
        h = fh.readline().rstrip("\n").split("\t")
        ix = [h.index(c) for c in need]
        for line in fh:
            v = line.rstrip("\n").split("\t")
            yield [v[i] for i in ix]

def fl(s):
    try: return float(s)
    except ValueError: return float("nan")

def ok(*a): return all(x == x and not math.isinf(x) for x in a)

SC = ["base", "C1", "C2", "UB", "UBT"]
out = []
def P(*a): out.append(" ".join(str(x) for x in a))

per_seed = {}     # (nat, period, sc, key) -> [値 per seed]
open_seed = {}    # (scope, key) -> [値 per seed]
q4_seed = {}
approx_err = []

def add(d, k, v): d.setdefault(k, []).append(v)

for sd in SEEDS:
    # 国×四半期の賃金＝Σ売れた×値段÷Σ売れた（labor の farm の列）
    wage = {}; ldem = {}
    for q, nat, sold, bill, dem in rows(f"{BASE}/labor_s{sd}.tsv", ["quarter", "nation", "farmSoldPerMonth", "farmWageBillPerMonth", "farmDemandPerMonth"]):
        s, b = fl(sold), fl(bill)
        if s > 0 and ok(b): wage[(q, nat)] = b / s
        ldem[(q, nat)] = fl(dem)
    cols = ["quarter", "year", "nation", "region", "s2BasisKind", "s2BasisEngine", "s2CapEnd", "s2SoldEnd", "s2UEnd", "s2TechEnd",
            "s3HarvestInst", "s3SrawInst", "s3QInst", "s3DInst", "s3EInst", "stapleNeedWeightedPrice", "farmProducedPerMonth", "s2RhoInst"]
    R = []
    for v in rows(f"{BASE}/subst_s{sd}.tsv", cols):
        q, yr, nat, reg, kind = v[0], int(v[1]), v[2], v[3], v[4]
        if kind != "2": continue
        rho, cap, sold, U, T, f, sraw, Q, D, E, p, prod, rhoI = map(fl, v[5:18])
        R.append((q, yr, nat, reg, rho, cap, sold, U, T, f, sraw, Q, D, E, p, prod, rhoI))
    # 国×四半期の主食の値段＝Σ売れた×p̄÷Σ売れた（賃金と同じ重み＝州の物価の水準が比で消える前提）
    pn = {}
    for r in R:
        q, nat, sold, p = r[0], r[2], r[6], r[14]
        if sold > 0 and ok(p): a = pn.setdefault((q, nat), [0.0, 0.0]); a[0] += sold * p; a[1] += sold
    seen = set()
    agg = {}
    for (q, yr, nat, reg, rho, cap, sold, U, T, f, sraw, Q, D, E, p, prod, rhoI) in R:
        if not ok(rho, cap, sold, U, T, f, sraw, Q) or not (cap > 0) or not (U > 0) or not (sraw > 0) or not (f > 0): continue
        w = wage.get((q, nat)); a = pn.get((q, nat))
        if w is None or a is None or not (a[1] > 0): continue
        z = w / (a[0] / a[1])                     # 賃金÷主食の値段（国の比）
        r_vw = rho * T * f / z                    # v_f÷w＝p̄ρTf̄÷w
        x = sraw * f; nN = Q / x                  # nN＝必要量×人数（Q＝必要量×人数×x）
        Eb = nN * max(0.0, x - 1.0)
        if E > 0 or Eb > 0: approx_err.append(abs(Eb - E) / max(Eb, E))
        a1 = 1.0 / (1.0 + r_vw)                   # 丙：売る分＝能力×w÷(w＋v_f)
        a2 = min(1.0, 1.0 / ((1.0 + SLOPE) * r_vw))  # 今の曲線の形で mc を v_f に替えた形
        s1 = min(sold, cap * a1); s2 = min(sold, cap * a2)
        xs = {"base": x, "C1": x * (cap - s1) / U, "C2": x * (cap - s2) / U, "UB": x * cap / U, "UBT": x * cap / U * T_CEIL / T}
        ds = {"base": 0.0, "C1": sold - s1, "C2": sold - s2, "UB": sold, "UBT": sold}
        # 開幕（その州の最初の段 2 の行）
        if reg not in seen:
            seen.add(reg)
            for scope in ("全て",) + ((nat,) if nat in NATS else ()):
                k = agg.setdefault(("open", scope), {"n": 0.0, "c1ok": 0.0, "c2ok": 0.0, "sold": 0.0, "d1": 0.0, "d2": 0.0, "cap": 0.0, "prod": 0.0, "a1c": 0.0, "rvw": []})
                k["n"] += nN; k["c1ok"] += nN * (cap * a1 >= sold); k["c2ok"] += nN * (cap * a2 >= sold)
                k["sold"] += sold; k["d1"] += sold - s1; k["d2"] += sold - s2; k["cap"] += cap; k["prod"] += prod if prod == prod else 0.0
                k["a1c"] += a1 * cap; k["rvw"].append(r_vw)
        yi = int(yr)
        for nat2 in ((nat,) if nat in NATS else ()):
            for per, lo, hi in (("1913", 1913, 1913), ("1900-13", 1900, 1913)):
                if not (lo <= yi <= hi): continue
                for sc in SC:
                    xx = xs[sc]
                    k = agg.setdefault((nat2, per, sc), {"n": 0.0, "nx": 0.0, "gt1": 0.0, "E": 0.0, "Q": 0.0, "ds": 0.0, "sold": 0.0, "half": 0.0, "dD": 0.0, "bind": 0.0, "cap": 0.0, "rvw": []})
                    k["n"] += nN; k["nx"] += nN * xx; k["gt1"] += nN * (xx > 1.0)
                    k["E"] += nN * max(0.0, xx - 1.0); k["Q"] += nN * xx
                    k["ds"] += ds[sc]; k["sold"] += sold; k["half"] += nN * (ds[sc] > 0.5 * sold)
                    k["dD"] += nN * (min(1.0, xx) - min(1.0, x)); k["cap"] += cap
                    k["bind"] += cap * ((cap * a1 < sold) if sc == "C1" else (cap * a2 < sold) if sc == "C2" else 0)
                    if sc == "base": k["rvw"].append(r_vw)
    for key, k in agg.items():
        if key[0] == "open":
            add(open_seed, (key[1], "人口で O′≧売れた（C1）"), k["c1ok"] / k["n"])
            add(open_seed, (key[1], "人口で O′≧売れた（C2）"), k["c2ok"] / k["n"])
            add(open_seed, (key[1], "売れた の減り（C1）÷売れた"), k["d1"] / k["sold"])
            add(open_seed, (key[1], "売れた の減り（C2）÷売れた"), k["d2"] / k["sold"])
            add(open_seed, (key[1], "売れた÷能力"), k["sold"] / k["cap"])
            add(open_seed, (key[1], "今の出した÷能力"), k["prod"] / k["cap"])
            add(open_seed, (key[1], "C1 の売る分 a（能力加重）"), k["a1c"] / k["cap"])
            add(open_seed, (key[1], "v_f÷w の中央"), statistics.median(k["rvw"]))
            continue
        nat, per, sc = key
        add(per_seed, (nat, per, sc, "x̄"), k["nx"] / k["n"])
        add(per_seed, (nat, per, sc, "x＞1 の人口"), k["gt1"] / k["n"])
        add(per_seed, (nat, per, sc, "κ_E 影"), k["E"] / k["Q"])
        add(per_seed, (nat, per, sc, "売れた の減り÷売れた"), k["ds"] / k["sold"])
        add(per_seed, (nat, per, sc, "売れた が半分より減る人口"), k["half"] / k["n"])
        add(per_seed, (nat, per, sc, "ΔD÷必要量"), k["dD"] / k["n"])
        if sc in ("C1", "C2"): add(per_seed, (nat, per, sc, "供給が縛る州の能力の割合"), k["bind"] / k["cap"])
        if sc == "base": add(per_seed, (nat, per, sc, "v_f÷w の中央"), statistics.median(k["rvw"]))
    # 問い 4：fd の市場へ出す必要量（1913・国）と比べる
    fdk = {}
    for q, yr, nat, need, mneed, sub in rows(f"{BASE}/fd_s{sd}.tsv", ["quarter", "year", "nation", "stapleNeedPerMonth", "marketNeedPerMonth", "subPerMonth"]):
        if yr != "1913" or nat not in NATS: continue
        a = fdk.setdefault(nat, [0.0, 0.0, 0.0]); a[0] += fl(need); a[1] += fl(mneed); a[2] += fl(sub)
    for nat, a in fdk.items():
        add(q4_seed, (nat, "市場へ出す必要量÷主食の必要量"), a[1] / a[0])
    for nat in NATS:
        # 売れた の減り（労働の単位）を RGO の産出の上限（÷0.4＝Catalog.FarmLaborPerOutput）に換えて、自給の増え ΔD と比べる（同じ月の財の単位）
        k = agg.get((nat, "1913", "C1"))
        if k is None: continue
        add(q4_seed, (nat, "C1 の RGO の産出の減りの上限（売れた の減り÷0.4）÷ΔD"), (k["ds"] / 0.4) / max(k["dD"], 1e-12) if k["dD"] > 0 else float("nan"))
        dsum = sum(v for (q, n2), v in ldem.items() if n2 == nat and q in {r[0] for r in R if r[1] == 1913 and r[2] == nat})
        add(q4_seed, (nat, "C1 の売れた の減り÷RGO の農業労働の需要（1913）"), k["ds"] / dsum if dsum > 0 else float("nan"))

def med(v): v = [x for x in v if x == x]; return statistics.median(v) if v else float("nan")
def fmt(x): return "nan" if x != x else (f"{x:.3g}" if abs(x) < 1e-2 and x != 0 else f"{x:.3f}")

P("# paper1347：丙の紙（m1341 の M・keep_1341/base_ins・種 1〜10・段 2 の行＝s2BasisKind 2）・値は種の中央〔最小〜最大〕")
P("# 場面：base＝今・C1＝丙（売る分＝能力×w÷(w＋v_f)）・C2＝今の曲線の形で mc を v_f に替えた（min(1, w÷(1.5 v_f))）・UB＝売れた 0（U＝能力）・UBT＝UB＋T を上限 1.3 に")
P("# 全て需要と値段を今のまま（部分均衡）＝売れた′＝min(売れた, 能力×a)・x′＝x×(能力−売れた′)÷U。E は f̄ で近似（財ごとの f の違いは作物の健全度だけ）")
P(f"# E の近似の誤差（|E近似−s3EInst|÷max）：中央 {fmt(med(approx_err))}・p90 {fmt(sorted(approx_err)[int(0.9*len(approx_err))] if approx_err else float('nan'))}・行 {len(approx_err)}")
P("")
P("## 開幕（各州の最初の段 2 の行＝焼いた月の次の四半期の末）")
for (scope, key), v in sorted(open_seed.items()):
    P(f"{scope}\t{key}\t{fmt(med(v))}\t〔{fmt(min(v))}〜{fmt(max(v))}〕")
P("")
P("## 1913・1900-13（露・清）")
for (nat, per, sc, key), v in sorted(per_seed.items(), key=lambda t: (t[0][0], t[0][1], SC.index(t[0][2]), t[0][3])):
    P(f"{nat}\t{per}\t{sc}\t{key}\t{fmt(med(v))}\t〔{fmt(min(v))}〜{fmt(max(v))}〕")
P("")
P("## 問い 4（1913・C1）")
for (nat, key), v in sorted(q4_seed.items()):
    P(f"{nat}\t{key}\t{fmt(med(v))}\t〔{fmt(min(v))}〜{fmt(max(v))}〕")
with open(OUT, "wb") as fh:
    fh.write(("\n".join(out) + "\n").encode("utf-8"))
