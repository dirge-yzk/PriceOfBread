# (884) c74ds 読むだけの紙の台本：土地税の算定の基準 A1 を盤面 m873 で取り直す（シミュを回さない・エンジンの関数を呼ばない）
# 入力：m873 の計器つき 10 種の生（D:/PoB_work/keep_873/base_ins・n1・n2 の subst_s・nation_s・cornint_s・base_ins の cpop_s）と world.json（main 496d5ec8 の写し）。
# 使い方：python -X utf8 -I paper884.py <world.json> <出力.txt> <subst844.txt> <腕の名=フォルダ> [<腕の名=フォルダ> ...]
# 式は (844) paper844_subst.py の [C] と同じ（V・R・率・r の定義）に、S1-a の後の盤面で読む列を 2 本並べる：
#   V_eng＝Σ engineSubOutputPerMonth（＝Region.LastSubsistenceOutput・収穫係数と補正を含む）×主食の値段（stapleNeedWeightedPrice）
#   V_sig＝Σ nominalSubPerMonth（＝Σ Size×食べる割合 σ×主食の名目の必要量・収穫係数 1）×主食の値段＝S1-a の σ で読んだ値
#   R＝Σ max(0, rentToOthers)＋max(0, rentToFarmers)（全 POP の LandRentPerMonth の和＝今の土地税の基数）・r＝非賦存の主食 RGO の実効の地代率（paper844_rent.py の式）
# ⚠限界：①四半期の末の月の値②R は担い手か農業労働の売り手のいる州の行だけ（それ以外の州の地代は入らない＝(844) と同じ）③輸出の地代は R に入らない（cornint_s の rentExp で別に数える）
#   ④t（農民の請求）は人頭税と所得税の上界・十分の一税なし⑤cpop_s は植民地格の州の POP だけ（請求＞手持ちの数は母集合の一部）
import sys, os, json, collections

def med(xs):
    xs = sorted(x for x in xs if x == x)
    if not xs: return float("nan")
    n = len(xs)
    return xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])

def qt(xs, p):
    xs = sorted(x for x in xs if x == x)
    if not xs: return float("nan")
    return xs[min(len(xs) - 1, int(p * len(xs)))]

def fnum(s):
    try: return float(s)
    except ValueError: return float("nan")

def ff(x, d=2):
    return ("%." + str(d) + "f") % x if x == x else "nan"

SE_CAP, SERF_FLOOR = 0.5, 0.15
def rentcap(level, full):
    return full if level == "Full" else (full + (1 - full) * 0.5 if level == "Limited" else 1.0)

WINDOWS = [(1783, 1785, "1783-85"), (1786, 1799, "1786-99"), (1800, 1849, "1800-49"), (1850, 1899, "1850-99"), (1900, 1950, "1900-50")]
def wins(y): return [w[2] for w in WINDOWS if w[0] <= y <= w[1]]

def read_arm(base, reff):
    open83 = collections.defaultdict(lambda: [0.0] * 9)   # (seed,nat) → V_eng, R, claim, nv, V_sig, subPop, subPop×σ, engineSub, nominal
    winr = collections.defaultdict(lambda: collections.defaultdict(lambda: [0.0, 0.0, 0.0]))   # (seed,nat) → win → [V_eng, V_sig, R]（四半期の和）
    for seed in range(1, 11):
        p = os.path.join(base, "subst_s%d.tsv" % seed)
        if not os.path.exists(p): continue
        with open(p, encoding="utf-8") as f:
            hdr = f.readline().rstrip("\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
            for line in f:
                c = line.rstrip("\n").split("\t")
                qq = int(c[ix["quarter"]]); y = int(c[ix["year"]]); nat = c[ix["nation"]]
                sp = fnum(c[ix["subPopK"]])
                if not (sp > 0): continue
                rate = fnum(c[ix["subRateWeighted"]])
                eng = fnum(c[ix["engineSubOutputPerMonth"]]); nom = fnum(c[ix["nominalSubPerMonth"]])
                poll = fnum(c[ix["pollClaimPerMonth"]]); inc = fnum(c[ix["incomeTaxUpperPerMonth"]])
                ro = fnum(c[ix["rentToOthersPerMonth"]]); rf = fnum(c[ix["rentToFarmersPerMonth"]])
                px = fnum(c[ix["stapleNeedWeightedPrice"]]); nv = fnum(c[ix["stapleNeedValuePerMonth"]])
                R = max(0.0, ro) + max(0.0, rf)
                ve = eng * px if (eng == eng and px == px) else 0.0
                vs = nom * px if (nom == nom and px == px) else 0.0
                for wn in wins(y):
                    a = winr[(seed, nat)][wn]; a[0] += ve; a[1] += vs; a[2] += R
                if qq == 0:
                    o = open83[(seed, nat)]
                    o[0] += ve; o[1] += R; o[2] += poll + inc
                    if nv == nv: o[3] += nv
                    o[4] += vs; o[5] += sp
                    if rate == rate: o[6] += sp * rate
                    o[7] += eng if eng == eng else 0; o[8] += nom if nom == nom else 0
    ltax = {}
    for seed in range(1, 11):
        p = os.path.join(base, "nation_s%d.tsv" % seed)
        if not os.path.exists(p): continue
        with open(p, encoding="utf-8") as f:
            hdr = f.readline().rstrip("\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
            for line in f:
                c = line.rstrip("\n").split("\t")
                if c[ix["quarter"]] != "0": continue
                ltax[(seed, c[ix["nation"]])] = (fnum(c[ix["landTaxEndShare"]]), fnum(c[ix["directTaxNetMeanPerMonth"]]))
    exp = collections.defaultdict(lambda: collections.defaultdict(lambda: [0.0, 0.0]))   # (seed,nat) → win → [dom, exp]
    exp0 = {}
    for seed in range(1, 11):
        p = os.path.join(base, "cornint_s%d.tsv" % seed)
        if not os.path.exists(p): continue
        with open(p, encoding="utf-8") as f:
            hdr = f.readline().rstrip("\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
            for line in f:
                c = line.rstrip("\n").split("\t")
                y = int(c[ix["year"]]); nat = c[ix["nation"]]
                dom = fnum(c[ix["rentDomTotalPerMonth"]])
                ex = sum(v for v in (fnum(c[ix["rentExpPerMonth%d" % b]]) for b in range(3)) if v == v)
                if not (dom == dom): dom = 0.0
                for wn in wins(y):
                    a = exp[(seed, nat)][wn]; a[0] += max(0.0, dom); a[1] += max(0.0, ex)
                if c[ix["quarter"]] == "0": exp0[(seed, nat)] = (max(0.0, dom), max(0.0, ex))
    return open83, winr, ltax, exp, exp0

def read_cpop(base):
    # 植民地格の州の POP×四半期：自給の割合＞0・人数＞0 で、請求の上界（人頭＋所得の上界）＞四半期の初めの貯蓄（＝Ĉ_p＞M_p の近似）の数
    cnt = collections.defaultdict(lambda: [0, 0, 0.0])
    for seed in range(1, 11):
        p = os.path.join(base, "cpop_s%d.tsv" % seed)
        if not os.path.exists(p): continue
        with open(p, encoding="utf-8") as f:
            hdr = f.readline().rstrip("\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
            for line in f:
                c = line.rstrip("\n").split("\t")
                if not (fnum(c[ix["popK"]]) > 1e-12): continue
                if not (fnum(c[ix["subsistenceRate"]]) > 0): continue
                y = int(c[ix["year"]])
                claim = fnum(c[ix["pollClaimPerQuarter"]]) + fnum(c[ix["incomeTaxUpperPerQuarter"]])
                sav = max(0.0, fnum(c[ix["savingsStart"]]))
                for wn in wins(y):
                    a = cnt[wn]; a[0] += 1
                    if claim == claim and claim > sav: a[1] += 1; a[2] += claim - sav
    return cnt

def parse844(path):
    t = {}
    on = False
    with open(path, encoding="utf-8") as f:
        for line in f:
            if line.startswith("[C]"): on = True; continue
            if not on: continue
            s = line.strip().split("\t")
            if len(s) == 11:
                t[s[0]] = (fnum(s[2]), fnum(s[3]), fnum(s[6]))   # V÷R・r×V÷R・Δ税（総額）÷直接税
    return t

def main():
    wpath, opath, p844 = sys.argv[1], sys.argv[2], sys.argv[3]
    arms = [a.split("=", 1) for a in sys.argv[4:]]
    world = json.load(open(wpath, encoding="utf-8"))
    reff = {}; lts = {}
    for n in world["nations"]:
        fls = n.get("farmLordShare", 0.6); se = n.get("initialSerfEmancipation") or "Full"
        reff[n["name"]] = min(max(0.3, min(fls, 1 - SERF_FLOOR)), min(1.0, rentcap(se, SE_CAP)))
        lts[n["name"]] = n.get("landTaxStage", 1)
    t844 = parse844(p844)
    out = []; P = out.append
    P("(884) c74ds 紙の数：土地税の算定の基準 A1 を盤面 m873 で取り直す（計器つき 10 種の生・台本 paper884.py・シミュは回していない）")
    P("腕：" + "・".join("%s=%s" % (a, b) for a, b in arms))
    P("")
    res = {}
    for name, base in arms:
        res[name] = read_arm(base, reff)
    # [C′] 開幕の表（腕ごと）
    per_arm_nat = {}
    for name, base in arms:
        open83, winr, ltax, exp, exp0 = res[name]
        per = collections.defaultdict(lambda: collections.defaultdict(list)); spawned = set()
        for (seed, nat), o in open83.items():
            if nat not in reff: spawned.add(nat); continue
            Ve, R, claim, nv, Vs, sp, sps = o[0], o[1], o[2], o[3], o[4], o[5], o[6]
            rate, dtax = ltax.get((seed, nat), (float("nan"), float("nan")))
            r = reff[nat]; d = per[nat]
            d["rate"].append(rate); d["sigma"].append(sps / sp if sp > 0 else float("nan"))
            d["VRe"].append(Ve / R if R > 0 else float("nan")); d["rVRe"].append(r * Ve / R if R > 0 else float("nan"))
            d["VRs"].append(Vs / R if R > 0 else float("nan")); d["rVRs"].append(r * Vs / R if R > 0 else float("nan"))
            d["dDir"].append(rate * Ve / dtax if dtax > 0 and rate == rate else float("nan"))
            d["dDirR"].append(rate * r * Ve / dtax if dtax > 0 and rate == rate else float("nan"))
            d["landDir"].append(rate * R / dtax if dtax > 0 and rate == rate else float("nan"))
            e = exp0.get((seed, nat))
            d["expShare"].append(e[1] / (e[0] + e[1]) if e and (e[0] + e[1]) > 0 else float("nan"))
        per_arm_nat[name] = (per, spawned)
    base_name = arms[0][0]
    per, spawned = per_arm_nat[base_name]
    P("[C′] 開幕（1783 q0）に土地税の基数を自給の収穫へ広げたときの増分（国ごと・10 種の中央・腕 %s）" % base_name)
    P("   列：国・landTaxStage（world.json）・率（nation_s q0）・σ（担い手の人口加重の食べる割合）・r・V_eng÷R・r×V_eng÷R（取り分）・V_sig÷R・r×V_sig÷R・"
      "今の土地税の上界÷直接税（率×R÷直接税）・Δ税（取り分）÷直接税・輸出の地代の割合（cornint q0）・m839 の r×V÷R（(844) [C]）・m873÷m839")
    rows = []
    for nat in per:
        d = per[nat]
        rows.append((nat, med(d["rVRe"])))
    rows.sort(key=lambda x: -x[1] if x[1] == x[1] else 0)
    allv = collections.defaultdict(list)
    for nat, _ in rows:
        d = per[nat]; r = reff[nat]
        old = t844.get(nat, (float("nan"),) * 3)
        rv = med(d["rVRe"])
        P("   %s\t%s\t%.3f\t%.3f\t%.3f\t%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s" % (nat, lts[nat], med(d["rate"]), med(d["sigma"]), r,
            ff(med(d["VRe"])), ff(rv), ff(med(d["VRs"])), ff(med(d["rVRs"])), ff(med(d["landDir"]), 3), ff(med(d["dDirR"]), 2),
            ff(med(d["expShare"]), 3), ff(old[1]), ff(rv / old[1] if old[1] == old[1] and old[1] > 0 else float("nan"), 3)))
        for k in ("VRe", "rVRe", "VRs", "rVRs", "landDir", "dDirR", "dDir", "sigma", "expShare"): allv[k].append(med(d[k]))
        allv["old"].append(old[1])
    P("   全ての国の中央：V_eng÷R %s・r×V_eng÷R %s・V_sig÷R %s・r×V_sig÷R %s・σ %s・今の土地税の上界÷直接税 %s・Δ税（取り分）÷直接税 %s・Δ税（総額）÷直接税 %s・輸出の地代の割合 %s・m839 の r×V÷R %s" % (
        ff(med(allv["VRe"])), ff(med(allv["rVRe"])), ff(med(allv["VRs"])), ff(med(allv["rVRs"])), ff(med(allv["sigma"]), 3), ff(med(allv["landDir"]), 3),
        ff(med(allv["dDirR"])), ff(med(allv["dDir"])), ff(med(allv["expShare"]), 3), ff(med(allv["old"]))))
    big10 = [n for n, v in rows if v == v and v > 10]; big30 = [n for n, v in rows if v == v and v >= 30]
    P("   取り分（r×V_eng÷R）が 10 倍超：%d／%d 国（%s）" % (len(big10), len(rows), "・".join(big10)))
    P("   同 30 倍以上：%d 国（%s）・最大 %s %s" % (len(big30), "・".join(big30), rows[0][0], ff(rows[0][1])))
    old10 = sorted(n for n, v in t844.items() if v[1] == v[1] and v[1] > 10)
    P("   m839（(844) [C]）で 10 倍超の %d 国との差：m873 だけ %s・m839 だけ %s" % (len(old10), "・".join(sorted(set(big10) - set(old10))) or "なし", "・".join(sorted(set(old10) - set(big10))) or "なし"))
    P("   外した q0 の建国：%s" % ("・".join(sorted(spawned)) or "なし"))
    P("")
    # ヌル腕の幅（取り分の中央の国ごとの腕の差）
    if len(arms) > 1:
        P("[N] ヌル腕の幅：取り分 r×V_eng÷R の国ごとの中央（腕ごと）と、全ての国の中央")
        names = [a for a, _ in arms]
        P("   国\t" + "\t".join(names) + "\t腕の間の最大÷最小")
        for nat, _ in rows[:20]:
            vs = [med(per_arm_nat[a][0][nat]["rVRe"]) if nat in per_arm_nat[a][0] else float("nan") for a in names]
            ok = [v for v in vs if v == v and v > 0]
            P("   %s\t%s\t%s" % (nat, "\t".join(ff(v) for v in vs), ff(max(ok) / min(ok), 3) if ok else "nan"))
        cen = []
        for a in names:
            pa = per_arm_nat[a][0]
            cen.append(med([med(pa[n]["rVRe"]) for n in pa]))
        P("   全ての国の中央\t" + "\t".join(ff(v) for v in cen))
        P("")
    # [T] 時期ごと（腕 base）：V_eng÷R・V_sig÷R・輸出の地代の割合（種と国の中央）
    open83, winr, ltax, exp, exp0 = res[base_name]
    P("[T] 時期ごとの基数の比（腕 %s・(種,国) ごとに窓の四半期の和の比→国ごとに種の中央→国の中央と p10・p90）" % base_name)
    for _, _, wn in WINDOWS:
        natv = collections.defaultdict(lambda: collections.defaultdict(list))
        for (seed, nat), dd in winr.items():
            if nat not in reff or wn not in dd: continue
            Ve, Vs, R = dd[wn]
            r = reff[nat]
            natv[nat]["VRe"].append(Ve / R if R > 0 else float("nan")); natv[nat]["rVRe"].append(r * Ve / R if R > 0 else float("nan"))
            natv[nat]["VRs"].append(Vs / R if R > 0 else float("nan"))
            e = exp.get((seed, nat), {}).get(wn)
            natv[nat]["exp"].append(e[1] / (e[0] + e[1]) if e and (e[0] + e[1]) > 0 else float("nan"))
        m = {k: [med(natv[n][k]) for n in natv] for k in ("VRe", "rVRe", "VRs", "exp")}
        P("   %s（国 %d）：V_eng÷R 中央 %s（p10 %s・p90 %s）・r×V_eng÷R 中央 %s（p90 %s）・V_sig÷R 中央 %s・輸出の地代÷全ての地代 中央 %s（p90 %s）" % (
            wn, len(natv), ff(med(m["VRe"])), ff(qt(m["VRe"], .1)), ff(qt(m["VRe"], .9)), ff(med(m["rVRe"])), ff(qt(m["rVRe"], .9)), ff(med(m["VRs"])),
            ff(med(m["exp"]), 3), ff(qt(m["exp"], .9), 3)))
    P("")
    # [F] 植民地格の州の POP：請求の上界＞手持ち（Ĉ_p＞M_p の近似）
    cnt = read_cpop(arms[0][1])
    P("[F] 植民地格の州の POP×四半期（cpop_s・自給の割合＞0・人数＞1e-12）で、請求の上界（人頭＋所得の上界・四半期）＞四半期の初めの貯蓄の数（腕 %s・Ĉ_p＞M_p の近似・当月の土地税と十分の一税と仲介者の取り分は含まない）" % base_name)
    for _, _, wn in WINDOWS:
        a = cnt.get(wn)
        if not a or a[0] == 0: continue
        P("   %s：POP×四半期 %d・請求＞手持ち %d（%.2f%%）・不足の和（四半期・各国の通貨の名目の和＝桁の目安だけ）%.4g" % (wn, a[0], a[1], 100.0 * a[1] / a[0], a[2]))
    open(opath, "wb").write(("\n".join(out) + "\n").encode("utf-8"))

if __name__ == "__main__":
    main()
