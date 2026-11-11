# (844) c68ds 読むだけの紙の台本 2/3：段 1′（ρ と時期の見込み・E_p＞0 の件数・失業の式の案・土地税の基数の開幕の増分・3 割のうち地代と税で説明できる割合）
# 入力：m839 の計器つき 10 種の生（D:/PoB_work/keep_839/base_ins の subst_s・cornint_s・nation_s・labor_s）と world.json。シミュは回さない・エンジンの関数を呼ばない。
# 使い方：python -I paper844_subst.py <base_ins のフォルダ> <world.json> <出力.txt>
# 定義（下書き 20261105 §1-2 の S1-a を州の粒度で近似＝POP ごとの ρ_p の代わりに州の ρ_r）：
#   U(t)＝unsoldD1Share×farmCapPerMonth（能力の基準 D1 の売れ残りの能力）
#   ρ_r＝subRate0×必要量×subPop0÷U0（その州が最初に現れた四半期＝1783 年の q0 で焼く・技術の倍率は 1 とした〈名目〉＝EffFill の列はどの計器にも無い）
#   Q(t)＝ρ_r×U(t)（名目）・Qh(t)＝Q×hf(t)・hf＝engineSubOutputPerMonth÷nominalSubPerMonth（収穫係数・動物性の補正・財の門）
#   食べる割合 s＝min(1, Q÷(必要量×subPop))（州×四半期で min を取ってから中央と人口加重を出す）
# ⚠限界：①州の粒度（担い手の POP が複数の州は 1 本の ρ_r）②技術の倍率の比は未測（EffFill の列なし）＝E_p は倍率 1（下界）と 1.3（上界）で数える
#   ③開幕の後に現れた州（植民地の州化・割譲で名前が初めて出た州）は焼く瞬間が無い＝数から外して件数を出す ④税は subst_s の上界の列（人頭税・所得税の上界）
import sys, os, json, math, collections

def med(xs):
    xs = [x for x in xs if x == x]
    if not xs: return float("nan")
    xs.sort(); n = len(xs)
    return xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])

def q(xs, p):
    xs = sorted(x for x in xs if x == x)
    if not xs: return float("nan")
    return xs[min(len(xs) - 1, int(p * len(xs)))]

def fnum(s):
    try: return float(s)
    except ValueError: return float("nan")

WINDOWS = [(1783, 1785, "開幕1783-85"), (1786, 1799, "1786-99"), (1800, 1849, "1800-49"), (1850, 1899, "1850-99"), (1900, 1950, "1900-50"), (1786, 1849, "1786-1849（c67ss の窓）")]

def win_of(y):
    return [w for w in WINDOWS if w[0] <= y <= w[1]]

SE_CAP, LR_CAP, SERF_FLOOR = 0.5, 0.3, 0.15
def rentcap(level, full):
    return full if level == "Full" else (full + (1 - full) * 0.5 if level == "Limited" else 1.0)

def main(base, wpath, opath):
    out = []; P = out.append
    world = json.load(open(wpath, encoding="utf-8"))
    reff = {}
    for n in world["nations"]:
        fls = n.get("farmLordShare", 0.6); se = n.get("initialSerfEmancipation") or "Full"
        reff[n["name"]] = min(max(0.3, min(fls, 1 - SERF_FLOOR)), min(1.0, rentcap(se, SE_CAP)))
    S = collections.defaultdict(lambda: collections.defaultdict(list))   # 窓→量→値の列
    W = collections.defaultdict(lambda: collections.defaultdict(float))  # 窓→和
    cnt = collections.Counter()
    natq = {}   # (seed, quarter, nation) → [poll, inc, rentO, den]
    open83 = collections.defaultdict(lambda: [0.0] * 8)   # (seed, nation) → 1783 q0 の和：V_sub・R・poll+inc・den・needValue・subPop・engineSub・nominal
    for seed in range(1, 11):
        path = os.path.join(base, "subst_s%d.tsv" % seed)
        with open(path, encoding="utf-8") as f:
            hdr = f.readline().rstrip("\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
            basek = {}
            for line in f:
                c = line.rstrip("\n").split("\t")
                qq = int(c[ix["quarter"]]); y = int(c[ix["year"]]); nat = c[ix["nation"]]; reg = c[ix["region"]]
                sp = fnum(c[ix["subPopK"]]); rate = fnum(c[ix["subRateWeighted"]])
                cap = fnum(c[ix["farmCapPerMonth"]]); d1 = fnum(c[ix["unsoldD1Share"]]); d2 = fnum(c[ix["unsoldD2Share"]])
                need = fnum(c[ix["stapleNeedPerCapita"]]); eng = fnum(c[ix["engineSubOutputPerMonth"]]); nom = fnum(c[ix["nominalSubPerMonth"]])
                poll = fnum(c[ix["pollClaimPerMonth"]]); inc = fnum(c[ix["incomeTaxUpperPerMonth"]])
                ro = fnum(c[ix["rentToOthersPerMonth"]]); rf = fnum(c[ix["rentToFarmersPerMonth"]])
                px = fnum(c[ix["stapleNeedWeightedPrice"]]); nv = fnum(c[ix["stapleNeedValuePerMonth"]]); ms = fnum(c[ix["marketShareOfNeed"]])
                if sp > 0:
                    k = natq.setdefault((seed, qq, nat), [0.0, 0.0, 0.0, 0.0])
                    k[0] += poll; k[1] += inc; k[2] += ro
                    if ms == ms and nv == nv: k[3] += ms * nv
                if qq == 0 and sp > 0:
                    o = open83[(seed, nat)]
                    if eng == eng and px == px: o[0] += eng * px
                    o[1] += max(0.0, ro) + max(0.0, rf); o[2] += poll + inc
                    if ms == ms and nv == nv: o[3] += ms * nv
                    if nv == nv: o[4] += nv
                    o[5] += sp; o[6] += eng if eng == eng else 0; o[7] += nom if nom == nom else 0
                if not (sp > 0) or not (cap > 0) or d1 != d1: continue
                U = d1 * cap
                if reg not in basek:
                    if y <= 1783 and U > 0 and rate > 0: basek[reg] = (rate * need * sp / U, d1, cap / sp)
                    else: basek[reg] = None
                b = basek[reg]
                ws = win_of(y)
                if b is None:
                    for w in ws: cnt[(w[2], "開幕の後に現れた州か U0＝0（数から外した）")] += 1
                    continue
                rho, d10, cpc0 = b
                hf = eng / nom if nom > 0 else float("nan")
                Q = rho * U; den = need * sp
                rn = Q / den; rh = rn * hf if hf == hf else float("nan")
                sn = min(1.0, rn); sh = min(1.0, rh) if rh == rh else float("nan")
                seng = eng / den if den > 0 else float("nan")
                for w in ws:
                    key = w[2]; L = S[key]
                    L["rho"].append(rho); L["s_nom"].append(sn); L["s_h"].append(sh); L["s_eng"].append(seng)
                    L["d1ratio"].append(d1 / d10 if d10 > 0 else float("nan")); L["cpcratio"].append((cap / sp) / cpc0 if cpc0 > 0 else float("nan"))
                    L["qpc_h"].append(rh * need if rh == rh else float("nan"))
                    A = W[key]; A["pop"] += sp; A["s_nom"] += sp * sn
                    if sh == sh: A["s_h"] += sp * sh; A["pop_h"] += sp
                    if seng == seng: A["s_eng"] += sp * seng; A["pop_e"] += sp
                    cnt[(key, "州×四半期")] += 1
                    if rh == rh and rh > 1: cnt[(key, "E_p＞0（倍率 1・下界）")] += 1
                    if rh == rh and rh * 1.3 > 1: cnt[(key, "E_p＞0（倍率 1.3・上界）")] += 1
                    if rn > 1: cnt[(key, "E_p＞0（名目・収穫係数 1）")] += 1
                    gain = sh - seng if (sh == sh and seng == seng) else float("nan")
                    L["gain"].append(gain)
                    if gain == gain and gain > 0.01: cnt[(key, "上げ＞0.01")] += 1
                    if gain == gain and gain < -0.01: cnt[(key, "下げ＜−0.01")] += 1
                    if d1 >= 0.99:
                        cnt[(key, "凍結（D1≧0.99）")] += 1; L["s_h_frozen"].append(sh); L["s_eng_frozen"].append(seng); L["d10_frozen"].append(d10); L["gain_frozen"].append(gain)
                    if d2 > rate: cnt[(key, "今の式で農民の失業＞0（D2＞自給率）")] += 1
                    L["d2"].append(d2)
                    if rh == rh and hf > 0 and rho > 0:
                        ub = max(0.0, min(1.0, (U - den / (rho * hf)) / cap))
                        if ub > 0: cnt[(key, "案(b) 失業＞0")] += 1; L["ub_pos"].append(ub)
    # cornint：主食の RGO の地代の割合
    grain = {}
    for seed in range(1, 11):
        path = os.path.join(base, "cornint_s%d.tsv" % seed)
        with open(path, encoding="utf-8") as f:
            hdr = f.readline().rstrip("\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
            for line in f:
                c = line.rstrip("\n").split("\t")
                k = (seed, int(c[ix["quarter"]]), c[ix["nation"]])
                g = [fnum(c[ix["rentGrainDomPerMonth%d" % b]]) for b in range(3)]
                tot = fnum(c[ix["rentDomTotalPerMonth"]])
                grain[k] = (g, tot, int(c[ix["year"]]))
    E = collections.defaultdict(lambda: collections.defaultdict(list)); EW = collections.defaultdict(lambda: collections.defaultdict(float))
    for k, (poll, inc, ro, den) in natq.items():
        if k not in grain or not (den > 0): continue
        g, tot, y = grain[k]
        gs = sum(x for x in g if x == x)
        share = gs / tot if tot > 0 else float("nan")
        roG = ro * share if share == share else float("nan")
        g01 = (g[0] if g[0] == g[0] else 0) + (g[1] if g[1] == g[1] else 0)
        for w in win_of(y):
            key = w[2]; L = E[key]
            L["all"].append((poll + inc + ro) / den); L["tax"].append((poll + inc) / den)
            L["grainRatio"].append((poll + inc + roG) / den if roG == roG else float("nan")); L["grain01"].append((poll + inc + g01) / den)
            L["grainShare"].append(share)
            A = EW[key]; A["den"] += den; A["all"] += poll + inc + ro; A["tax"] += poll + inc; A["g01"] += poll + inc + g01
            if roG == roG: A["gR"] += poll + inc + roG; A["denR"] += den
    # nation_s：開幕の土地税の率と直接税
    ltax = {}
    for seed in range(1, 11):
        path = os.path.join(base, "nation_s%d.tsv" % seed)
        with open(path, encoding="utf-8") as f:
            hdr = f.readline().rstrip("\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
            for line in f:
                c = line.rstrip("\n").split("\t")
                if c[ix["quarter"]] != "0": continue
                ltax[(seed, c[ix["nation"]])] = (fnum(c[ix["landTaxEndShare"]]), fnum(c[ix["directTaxNetMeanPerMonth"]]))
    # labor_s：農民の失業
    lab = []
    for seed in range(1, 11):
        path = os.path.join(base, "labor_s%d.tsv" % seed)
        with open(path, encoding="utf-8") as f:
            hdr = f.readline().rstrip("\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
            for line in f:
                c = line.rstrip("\n").split("\t"); lab.append(fnum(c[ix["farmUnemploymentShare"]]))

    P("(844) 段 1′ の紙の数（m839＝keep_839/base_ins の計器つき 10 種・台本 paper844_subst.py・シミュは回していない）")
    P("")
    P("[A] ρ と食べる割合の時期の見込み（S1-a を州の粒度で・min(1) は州×四半期で取ってから集計）")
    for w in WINDOWS:
        key = w[2]; L = S[key]; A = W[key]
        if not L: continue
        P("-- %s：州×四半期 %d（外した %d）" % (key, cnt[(key, "州×四半期")], cnt[(key, "開幕の後に現れた州か U0＝0（数から外した）")]))
        P("   ρ_r（売れ残りの能力 1 単位あたりの主食・名目）中央 %.4f・p10 %.4f・p90 %.4f（市場の RGO は 1÷0.4＝2.5＝中央は %.1f 分の 1）" % (med(L["rho"]), q(L["rho"], .1), q(L["rho"], .9), 2.5 / med(L["rho"])))
        P("   食べる割合 名目（収穫係数 1）：中央 %.3f・人口加重 %.3f" % (med(L["s_nom"]), A["s_nom"] / A["pop"]))
        P("   食べる割合 収穫係数つき：中央 %.3f・人口加重 %.3f・p10 %.3f・p90 %.3f" % (med(L["s_h"]), A["s_h"] / A["pop_h"], q(L["s_h"], .1), q(L["s_h"], .9)))
        P("   今のエンジンの食べる割合（engineSub÷必要量）：中央 %.3f・人口加重 %.3f" % (med(L["s_eng"]), A["s_eng"] / A["pop_e"]))
        P("   1 人あたりの作った量（収穫係数つき・min 前・月）中央 %.3f（必要量 1.10）" % med(L["qpc_h"]))
        P("   分解：D1÷D1_0 中央 %.3f・1 人あたりの能力÷開幕 中央 %.3f" % (med(L["d1ratio"]), med(L["cpcratio"])))
        nf = cnt[(key, "凍結（D1≧0.99）")]
        P("   凍結（D1≧0.99）%d 件：食べる割合 収穫係数つき 中央 %.3f（今のエンジン %.3f）・その州の開幕の D1_0 中央 %.3f・上げ（S1−今）中央 %.4f・p90 %.4f" % (nf, med(L["s_h_frozen"]), med(L["s_eng_frozen"]), med(L["d10_frozen"]), med(L["gain_frozen"]), q(L["gain_frozen"], .9)))
        P("   上げ（S1 の食べる割合−今のエンジン・同じ州×四半期）：中央 %.4f・p10 %.4f・p90 %.4f・＞0.01 の割合 %.2f%%・＜−0.01 の割合 %.2f%%" % (med(L["gain"]), q(L["gain"], .1), q(L["gain"], .9), 100 * cnt[(key, "上げ＞0.01")] / cnt[(key, "州×四半期")], 100 * cnt[(key, "下げ＜−0.01")] / cnt[(key, "州×四半期")]))
        n = cnt[(key, "州×四半期")]
        P("   E_p＞0 の州×四半期：名目 %d（%.2f%%）・収穫係数つき倍率 1（下界）%d（%.2f%%）・倍率 1.3（EffFill 0→1 の上界）%d（%.2f%%）" % (
            cnt[(key, "E_p＞0（名目・収穫係数 1）")], 100 * cnt[(key, "E_p＞0（名目・収穫係数 1）")] / n,
            cnt[(key, "E_p＞0（倍率 1・下界）")], 100 * cnt[(key, "E_p＞0（倍率 1・下界）")] / n,
            cnt[(key, "E_p＞0（倍率 1.3・上界）")], 100 * cnt[(key, "E_p＞0（倍率 1.3・上界）")] / n))
        P("   失業の式：今の式で農民の失業＞0 の州×四半期 %d（%.2f%%）・案(b) で＞0 %d（%.2f%%・正の中央 %.3f）・案(c)＝D2 そのまま 中央 %.3f・p90 %.3f" % (
            cnt[(key, "今の式で農民の失業＞0（D2＞自給率）")], 100 * cnt[(key, "今の式で農民の失業＞0（D2＞自給率）")] / n,
            cnt[(key, "案(b) 失業＞0")], 100 * cnt[(key, "案(b) 失業＞0")] / n, med(L["ub_pos"]), med(L["d2"]), q(L["d2"], .9)))
    P("")
    lp = [x for x in lab if x == x]
    P("[A′] labor_s の farmUnemploymentShare（計器 9 国×四半期・10 種）：行 %d・0 より大 %d（%.3f%%）・最大 %.4f" % (len(lp), sum(1 for x in lp if x > 0), 100 * sum(1 for x in lp if x > 0) / max(1, len(lp)), max(lp) if lp else float("nan")))
    P("")
    P("[B] 3 割（市場に出す必要量の値）のうち地代と税で説明できる割合（国×四半期・中央と Σ の比）")
    P("   列：全ての地代（c67ss の定義を国の粒度で）・税だけ・主食の地代〈比の近似〉（rentToOthers×Σ穀物の地代÷国内の地代）・主食の地代〈ブロック 0＋1〉（cornint の rentGrainDom0＋1）・穀物の地代の割合")
    for w in WINDOWS:
        key = w[2]; L = E[key]; A = EW[key]
        if not L: continue
        P("-- %s（国×四半期 %d）：全て 中央 %.3f（Σ %.3f）・税だけ 中央 %.3f（Σ %.3f）・主食〈比〉中央 %.3f（Σ %.3f）・主食〈0＋1〉中央 %.3f（Σ %.3f）・穀物の割合 中央 %.3f" % (
            key, len(L["all"]), med(L["all"]), A["all"] / A["den"], med(L["tax"]), A["tax"] / A["den"], med(L["grainRatio"]), A["gR"] / A["denR"], med(L["grain01"]), A["g01"] / A["den"], med(L["grainShare"])))
    P("")
    P("[C] 土地税の基数を自給の収穫へ広げたときの開幕（1783 q0）の増分（国ごと・10 種の中央）")
    P("   V＝自給の収穫の値（engineSub×主食の値段）・R＝今の基数（全 POP の地代の和）・率＝nation_s の landTaxEndShare（q0）・r＝非賦存の主食 RGO の実効の地代率（paper844_rent.py）")
    P("   列：国・率・V÷R（総額の基数の増え）・r×V÷R（取り分）・Δ税（総額）÷農民の今の請求（人頭税＋所得税の上界）・Δ税（取り分）÷同・Δ税（総額）÷直接税・Δ税（総額）÷必要量の値・t＝農民の請求÷必要量の値・s＝r＋t・0.7÷(1−s)")
    per = collections.defaultdict(lambda: collections.defaultdict(list))
    spawned = set()
    for (seed, nat), o in open83.items():
        if nat not in reff: spawned.add(nat); continue   # 開幕の q0 に建った国（種 9 の蘭の植民地の共和国ほか）＝world.json に無い＝外す
        V, R, claim, den, nv = o[0], o[1], o[2], o[3], o[4]
        rate, dtax = ltax.get((seed, nat), (float("nan"), float("nan")))
        r = reff.get(nat, float("nan"))
        d = per[nat]
        d["rate"].append(rate)
        d["VR"].append(V / R if R > 0 else float("nan")); d["rVR"].append(r * V / R if R > 0 else float("nan"))
        dt = rate * V if rate == rate else float("nan")
        d["dClaim"].append(dt / claim if claim > 0 else float("nan")); d["dClaimR"].append(dt * r / claim if claim > 0 else float("nan"))
        d["dDirect"].append(dt / dtax if dtax > 0 else float("nan")); d["dNeed"].append(dt / nv if nv > 0 else float("nan"))
        t = claim / nv if nv > 0 else float("nan"); d["t"].append(t)
    allv = collections.defaultdict(list)
    for nat in sorted(per, key=lambda n: -med(per[n]["dNeed"]) if med(per[n]["dNeed"]) == med(per[n]["dNeed"]) else 0):
        d = per[nat]; r = reff.get(nat, float("nan")); t = med(d["t"]); s = r + t
        P("   %s\t%.3f\t%.2f\t%.2f\t%s\t%s\t%s\t%.3f\t%.3f\t%.3f\t%s" % (nat, med(d["rate"]), med(d["VR"]), med(d["rVR"]),
            ("%.2f" % med(d["dClaim"])) if med(d["dClaim"]) == med(d["dClaim"]) else "nan", ("%.2f" % med(d["dClaimR"])) if med(d["dClaimR"]) == med(d["dClaimR"]) else "nan",
            ("%.3f" % med(d["dDirect"])) if med(d["dDirect"]) == med(d["dDirect"]) else "nan", med(d["dNeed"]), t, s, ("%.2f" % (0.7 / (1 - s))) if s < 1 else "定義できない"))
        for kx in ("VR", "rVR", "dClaim", "dClaimR", "dDirect", "dNeed", "t"): allv[kx].append(med(d[kx]))
        allv["s"].append(s)
    P("   全ての国の中央：V÷R %.2f・r×V÷R %.2f・Δ税（総額）÷農民の請求 %.2f・（取り分）%.2f・÷直接税 %.3f・÷必要量の値 %.3f・t %.3f・s %.3f" % tuple(med(allv[k]) for k in ("VR", "rVR", "dClaim", "dClaimR", "dDirect", "dNeed", "t", "s")))
    P("   率が 0 の国の数：%d／%d・外した q0 の建国：%s" % (sum(1 for n in per if med(per[n]["rate"]) == 0), len(per), "・".join(sorted(spawned))))
    P("   ⚠t は subst_s の列（人頭税＋所得税の上界）だけ＝十分の一税（0.05×前の月の所得・Fiscal.TitheClaim）を含まない。c67ds の概算（農民の所得 1.02／単位・必要量の値 1.54）なら十分の一税は必要量の値の約 0.033。")
    open(opath, "wb").write(("\n".join(out) + "\n").encode("utf-8"))

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3])
