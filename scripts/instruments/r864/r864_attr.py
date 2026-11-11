# (864) c71sr：c70ss の G5（対照）の帰属を読む台本（読むだけ・腕 0 本・事前登録 D:/PoB_work/r864_work/prereg_sr.txt）
# 呼び方：python -X utf8 -I r864_attr.py <出力フォルダ> [種の数=10]
# 定義は gates_ss853.py:150〜182 を自分で書き直した物（import しない）。最初に P5 の行の数・|d の中央|・w の中央を門の出力と突き合わせる。
import sys, os, collections, math

ARM = "D:/PoB_work/keep_859_rej/c70ssB_10/ins"
B852 = "D:/PoB_work/keep_852/base_ins"; N1_852 = "D:/PoB_work/keep_852/n1"; N2_852 = "D:/PoB_work/keep_852/n2"
B839 = "D:/PoB_work/keep_839/base_ins"; N1_839 = "D:/PoB_work/keep_839/n1"; N2_839 = "D:/PoB_work/keep_839/n2"
NAN = float("nan")

def fnum(s):
    try: return float(s)
    except ValueError: return NAN

def hdr(f):
    h = f.readline().rstrip("\r\n").split("\t"); return {k: i for i, k in enumerate(h)}

def win(y):
    return "1783-85" if y <= 1785 else "1786-99" if y <= 1799 else "1800-49" if y <= 1849 else "1850-99" if y <= 1899 else "1900-50"
WINS = ("1783-85", "1786-99", "1800-49", "1850-99", "1900-50")

def med(xs):
    xs = sorted(x for x in xs if x == x); n = len(xs)
    return NAN if n == 0 else xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])

def load_region(base, seed):
    """region_s：(q, region) → (nationIndex, 移民の純〔in−out〕÷人口)。"""
    D = {}
    p = os.path.join(base, "region_s%d.tsv" % seed)
    if not os.path.exists(p): return None
    with open(p, encoding="utf-8") as f:
        ix = hdr(f); iq, ir, ii, ip, mi, mo = ix["quarter"], ix["region"], ix["nationIndex"], ix["populationK"], ix["migrationInKPerQuarter"], ix["migrationOutKPerQuarter"]
        for line in f:
            c = line.rstrip("\r\n").split("\t")
            pop = fnum(c[ip]); net = fnum(c[mi]) - fnum(c[mo])
            D[(int(c[iq]), c[ir])] = (sys.intern(c[ii]), net / pop if pop > 0 else NAN)
    return D

def load_subst(base, seed, full):
    """subst_s：鍵 (q, 国/州) → 辞書。full＝腕（末尾の列も読む）。"""
    D = {}
    with open(os.path.join(base, "subst_s%d.tsv" % seed), encoding="utf-8") as f:
        ix = hdr(f)
        has2 = "s2SigmaEngine" in ix
        for line in f:
            c = line.rstrip("\r\n").split("\t")
            sp = fnum(c[ix["subPopK"]])
            if not (sp > 0): continue
            need = fnum(c[ix["stapleNeedPerCapita"]]); eng = fnum(c[ix["engineSubOutputPerMonth"]])
            r = {"q": int(c[ix["quarter"]]), "y": int(c[ix["year"]]), "reg": c[ix["region"]], "tier": c[ix["tier"]], "sp": sp,
                 "s": eng / (need * sp) if need * sp > 0 and eng == eng else NAN,
                 "rate": fnum(c[ix["subRateWeighted"]]), "cap": fnum(c[ix["farmCapPerMonth"]]), "sold": fnum(c[ix["farmSoldPerMonth"]]),
                 "d1": fnum(c[ix["unsoldD1Share"]]), "ms": fnum(c[ix["marketShareOfNeed"]]), "px": fnum(c[ix["stapleNeedWeightedPrice"]]), "need": need}
            if full and has2:
                for k in ("s2BasisKind", "s2RhoSource"): r[k] = int(c[ix[k]])
                for k in ("s2BasisEngine", "s2D0Inst", "s2RhoInst", "s2SigmaEngine", "s2SigmaInst", "s2CapHead", "s2SoldHead", "s2UHead", "s2TechHead", "s2Tech0Inst"):
                    r[k] = fnum(c[ix[k]])
            D[(r["q"], c[ix["nation"]] + "/" + r["reg"])] = r
    return D

def soc(r): return r["sold"] / r["cap"] if r["cap"] > 0 else NAN

def p5b_rows(B, N1, N2):
    """P5b＝基線の行のうち q≧1・基線の q0 の行が在る・N1 と N2 に同じ鍵・3 本の s が有限・D1 有限・cap＞0・|soc−soc0|≦0.01。→ [(鍵, 年, w, s_B)]"""
    open0 = {k[1]: r for k, r in B.items() if k[0] == 0}
    out = []
    for k, r in B.items():
        if k[0] == 0: continue
        if k not in N1 or k not in N2: continue
        sb, s1, s2 = r["s"], N1[k]["s"], N2[k]["s"]
        if sb != sb or s1 != s1 or s2 != s2: continue
        o = open0.get(k[1])
        if o is None or r["d1"] != r["d1"] or o["d1"] != o["d1"]: continue
        if r["cap"] > 0 and o["cap"] > 0 and abs(soc(r) - soc(o)) <= 0.01:
            out.append((k, r["y"], max(sb, s1, s2) - min(sb, s1, s2), sb))
    return out

def main(outdir, nseed=10):
    nseed = int(nseed); os.makedirs(outdir, exist_ok=True)
    R = []          # P5a の行
    P5b852 = []; P5b839 = []; ov = collections.Counter()
    excl = 0
    for seed in range(1, nseed + 1):
        A = load_subst(ARM, seed, True); RA = load_region(ARM, seed)
        B = load_subst(B852, seed, False); N1 = load_subst(N1_852, seed, False); N2 = load_subst(N2_852, seed, False); RB = load_region(B852, seed)
        # 持ち主が変わった行（gates_ss853 の masked と同じ規則：四半期の順に州の名で一つ前の nationIndex と比べる）
        masked = {}; last = {}
        for k in sorted(A.keys(), key=lambda kk: kk[0]):
            q, key = k; reg = A[k]["reg"]
            o = RA.get((q, reg)) if RA is not None else None
            if o is None: masked[k] = True; continue
            pv = last.get(reg)
            masked[k] = (pv is None and q > 0) or (pv is not None and pv != o[0]); last[reg] = o[0]
        open0 = {k[1]: r for k, r in A.items() if k[0] == 0}
        Akeys = set()
        for k, r in A.items():
            q, key = k
            Bs = [x.get(k) for x in (B, N1, N2)]
            if any(b is None for b in Bs) or any(b["s"] != b["s"] for b in Bs) or r["s"] != r["s"]: excl += 1; continue
            if q == 0: continue
            o = open0.get(key)
            if o is None or r["d1"] != r["d1"] or o["d1"] != o["d1"]: continue
            if not (r["cap"] > 0 and o["cap"] > 0): continue
            dsoc = soc(r) - soc(o)
            if abs(dsoc) > 0.01: continue
            Akeys.add(k)
            sb, s1, s2 = (b["s"] for b in Bs)
            w = max(sb, s1, s2) - min(sb, s1, s2); d = r["s"] - sb
            baked = r.get("s2BasisKind") == 2 and r.get("s2RhoSource") == 1
            H = r["s"] / r["rate"] if r["rate"] > 0 else NAN
            d0 = r.get("s2D0Inst", NAN); sig = r.get("s2SigmaEngine", NAN)
            sole = baked and abs(r["rate"] - sig) <= 1e-9
            TT0 = r["s2TechHead"] / r["s2Tech0Inst"] if baked and r.get("s2Tech0Inst", 0) > 0 else NAN
            if not baked: dF = 0.0; dT = 0.0
            elif sole and H == H and d0 == d0: dF = r["s"] - d0 * H; dT = d0 * H * (TT0 - 1.0)
            else: dF = NAN; dT = NAN
            dB = d - dF if dF == dF else NAN
            # 頭の値の D1 の比（q0 の行の末の soc を開幕の基準に・近似）と cap÷人数の比（q0 と比べる）
            socH = r["s2SoldHead"] / r["s2CapHead"] if baked and r.get("s2CapHead", 0) > 0 else NAN
            fD = (1.0 - socH) / (1.0 - soc(o)) if socH == socH and soc(o) < 1 else NAN
            fC = (r["cap"] / r["sp"]) / (o["cap"] / o["sp"]) if o["sp"] > 0 and o["cap"] > 0 else NAN
            ra = RA.get((q, r["reg"])) if RA else None; rb = RB.get((q, r["reg"])) if RB else None
            mig = (ra[1] - rb[1]) if ra and rb else NAN
            M1 = masked.get(k, False)
            M2 = baked and r.get("s2SoldHead", 1) == 0 and r.get("s2CapHead", 0) > 0
            M3 = r["d1"] == 0 or o["d1"] == 0
            M4 = 0.005 < abs(dsoc) <= 0.01
            R.append({"seed": seed, "q": q, "y": r["y"], "w5": win(r["y"]), "nation": key.split("/")[0], "tier": r["tier"], "baked": baked, "sole": sole,
                      "d": d, "w": w, "dF": dF, "dT": dT, "dB": dB, "dsoc": dsoc, "soc0": soc(o), "M1": M1, "M2": M2, "M3": M3, "M4": M4,
                      "TT0": TT0, "fD": fD, "fC": fC, "sB": sb, "sig_d0": sig / d0 if baked and d0 > 0 else NAN, "cap1": baked and sig >= 1.0,
                      "pxr": r["px"] / Bs[0]["px"] if Bs[0]["px"] > 0 else NAN, "msd": r["ms"] - Bs[0]["ms"], "spr": r["sp"] / Bs[0]["sp"] if Bs[0]["sp"] > 0 else NAN,
                      "mig": mig, "w2_bn1": abs(sb - s1), "w2_bn2": abs(sb - s2), "w2_n1n2": abs(s1 - s2), "w839": NAN, "key": k})
        # P5b（852）と腕の P5a の重なり
        pb = p5b_rows(B, N1, N2)
        for (k, y, w, sb) in pb:
            P5b852.append((y, w)); ov["P5b852"] += 1
            if k in Akeys: ov["P5b852∩P5a"] += 1
        ov["P5a"] += len(Akeys)
        del A, B, N1, N2, RA, RB
        # 旧ヌル腕（m839 の盤面）
        B9 = load_subst(B839, seed, False); M9 = load_subst(N1_839, seed, False); K9 = load_subst(N2_839, seed, False)
        for (k, y, w, sb) in p5b_rows(B9, M9, K9): P5b839.append((y, w))
        for row in R:
            if row["seed"] != seed: continue
            k = row["key"]
            xs = [x.get(k) for x in (B9, M9, K9)]
            if all(x is not None and x["s"] == x["s"] for x in xs):
                ss = [x["s"] for x in xs]; row["w839"] = max(ss) - min(ss); row["w839_bn1"] = abs(ss[0] - ss[1]); row["w839_bn2"] = abs(ss[0] - ss[2]); row["w839_n1n2"] = abs(ss[1] - ss[2])
        del B9, M9, K9
        print("seed %d done P5a=%d" % (seed, len(R)), flush=True)
    report(outdir, R, P5b852, P5b839, ov, excl)

def cellstats(rows):
    d = [r["d"] for r in rows]; w = [r["w"] for r in rows]
    md, mw = med(d), med(w)
    return len(rows), md, mw, (abs(md) <= mw) if md == md and mw == mw else None

def report(outdir, R, P5b852, P5b839, ov, excl):
    L = []; P = L.append
    n, md, mw, ok = cellstats(R)
    P("(864) G5 の帰属（腕 %s・基線 %s・N1 %s・N2 %s・種 10）" % (ARM, B852, N1_852, N2_852))
    P("[検算] P5 %d 行・d の中央 %.4f（|%.4f|）・w の中央 %.4f・G5 %s（門の出力：122630・0.0111・0.0103・落）・外した行 %d（門 728937）" % (n, md, abs(md), mw, "通" if ok else "落", excl))
    # [1] 分母
    P(""); P("[1] 分母（P5 の行の数）")
    def tab(name, keyf, order=None, minn=0):
        G = collections.defaultdict(list)
        for r in R: G[keyf(r)].append(r)
        ks = order if order else sorted(G, key=lambda k: -len(G[k]))
        P("-- %s" % name)
        P("   区分\t行\t割合\td 中央\tw 中央\t|d|≦w\tdF 中央\tdT 中央\tdB 中央\tw=0 割合\td=0 割合")
        out = []
        for k in ks:
            g = G.get(k, [])
            if not g: continue
            nn, a, b, okk = cellstats(g)
            dF = med([r["dF"] for r in g]); dT = med([r["dT"] for r in g]); dB = med([r["dB"] for r in g])
            w0 = sum(1 for r in g if r["w"] == 0) / nn; d0 = sum(1 for r in g if r["d"] == 0) / nn
            P("   %s\t%d\t%.4f\t%.5f\t%.5f\t%s\t%.5f\t%.5f\t%.5f\t%.3f\t%.3f" % (k, nn, nn / len(R), a, b, "通" if okk else "落", dF, dT, dB, w0, d0))
            out.append((k, nn, a, b))
        return out
    tw = tab("窓", lambda r: r["w5"], WINS)
    tt = tab("格", lambda r: r["tier"])
    top = [k for k, _ in collections.Counter(r["nation"] for r in R).most_common(10)]
    tn = tab("国（上位 10＋残り）", lambda r: r["nation"] if r["nation"] in top else "（残り）", top + ["（残り）"])
    tab("焼いた印（s2BasisKind＝2・RhoSource＝1）・担い手が 1 つ", lambda r: ("焼いた" if r["baked"] else "焼いていない") + ("・1 つ" if r["sole"] else ""))
    tab("Δsoc の刻み", lambda r: "[-0.01,-0.005)" if r["dsoc"] < -0.005 else "[-0.005,0)" if r["dsoc"] < 0 else "[0,0.005)" if r["dsoc"] < 0.005 else "[0.005,0.01]",
        ["[-0.01,-0.005)", "[-0.005,0)", "[0,0.005)", "[0.005,0.01]"])
    tab("開幕の soc0 の帯", lambda r: "<0.25" if r["soc0"] < 0.25 else "0.25-0.5" if r["soc0"] < 0.5 else "0.5-0.75" if r["soc0"] < 0.75 else "0.75-0.9" if r["soc0"] < 0.9 else ">=0.9",
        ["<0.25", "0.25-0.5", "0.5-0.75", "0.75-0.9", ">=0.9"])
    for m in ("M1", "M2", "M3", "M4"): tab("印 %s" % m, lambda r, m=m: m + (" 該当" if r[m] else " 外"), [m + " 該当", m + " 外"])
    tab("σ が 1 に張り付く（min が効く）", lambda r: "σ≧1" if r["cap1"] else "σ<1")
    tab("窓×焼いた", lambda r: r["w5"] + ("・焼いた" if r["baked"] else "・焼いていない"), [w + s for w in WINS for s in ("・焼いた", "・焼いていない")])
    # [2] 判別
    P(""); P("[2] 判別")
    sgn = (md > 0) - (md < 0)
    def uni(cells, name):
        cs = [(k, nn, a) for k, nn, a, b in cells if nn >= 500]
        tot = sum(nn for _, nn, _ in cs); same = sum(nn for _, nn, a in cs if ((a > 0) - (a < 0)) == sgn)
        P("   %s：500 行以上のセル %d・全体の中央と同じ符号の行の重み %.4f（%d／%d）・符号の内訳 %s" % (name, len(cs), same / tot if tot else NAN, same, tot,
            "・".join("%s %+.4f" % (k, a) for k, _, a in cs)))
        return tot and same / tot >= 0.9
    u = [uni(tw, "窓"), uni(tt, "格"), uni(tn, "国")]
    P("   課題の文の規則（一様）：窓 %s・格 %s・国 %s → %s" % (tuple("一様" if x else "一様でない" for x in u) + ("全て一様＝〈形の側〉の条件" if all(u) else "一様でない区分あり",)))
    for m in ("M1", "M2", "M3", "M4"):
        rest = [r for r in R if not r[m]]; nn, a, b, okk = cellstats(rest)
        sm = sum(r["d"] for r in R if r[m]); st = sum(r["d"] for r in R)
        P("   印 %s：行 %d（%.5f）・除いた後 P5 %d・d 中央 %.5f・w 中央 %.5f → %s・印の行の d の和の割合 %.4f" % (m, len(R) - nn, (len(R) - nn) / len(R), nn, a, b, "通" if okk else "落", sm / st if st else NAN))
    rest = [r for r in R if not (r["M1"] or r["M2"] or r["M3"])]; nn, a, b, okk = cellstats(rest)
    P("   印 M1∪M2∪M3 を除く：P5 %d・d 中央 %.5f・w 中央 %.5f → %s" % (nn, a, b, "通" if okk else "落"))
    # 規則 0
    ex = [r for r in R if r["dF"] == r["dF"]]
    P("   規則 0（チャネル）：dF が厳密に出る行 %d／%d（焼いていない行は dF＝0・焼いて担い手が 1 つの行は厳密・焼いて担い手が複数は外す %d）" % (len(ex), len(R), len(R) - len(ex)))
    nn, a, b, okk = cellstats(ex)
    P("     その行の G5：d 中央 %.5f・w 中央 %.5f → %s" % (a, b, "通" if okk else "落"))
    for nm, f in (("dF（形の腕の盤面での直の効き）", "dF"), ("dT（T の比だけ）", "dT"), ("dB（盤面の差＝旧の規則なら）", "dB")):
        P("     %s 中央 %.5f・平均 %.5f" % (nm, med([r[f] for r in ex]), sum(r[f] for r in ex) / len(ex) if ex else NAN))
    exc = [r for r in ex if r["baked"] and r["TT0"] == r["TT0"] and r["fC"] == r["fC"] and r["fD"] == r["fD"] and not r["cap1"]]
    for r in exc:
        h0 = r["dF"] + r["dB"] - r["d"]  # 0（恒等の点検）
        base = (r["s"] if False else 0)
    def comp(r):
        k = (r["d"] - r["dB"]) / (r["sig_d0"] - 1.0) if abs(r["sig_d0"] - 1.0) > 1e-12 else NAN  # d0×H
        return k
    rows_k = [(r, comp(r)) for r in exc]
    rows_k = [(r, k) for r, k in rows_k if k == k]
    P("     チャネルの加法の分け（一次の近似・焼いた・担い手 1 つ・σ＜1 の行 %d）：d0×H＝dF÷(σ/d0−1)" % len(rows_k))
    for wn in WINS + ("全体",):
        g = [(r, k) for r, k in rows_k if wn == "全体" or r["w5"] == wn]
        if not g: continue
        cT = med([k * (r["TT0"] - 1) for r, k in g]); cC = med([k * (r["fC"] - 1) for r, k in g]); cD = med([k * (r["fD"] - 1) for r, k in g])
        res = med([r["dF"] - k * (r["TT0"] * r["fC"] * r["fD"] - 1) for r, k in g])
        P("       %s：行 %d・dF 中央 %.5f・T の項 %.5f・cap÷人数の項 %.5f・D1 の項 %.5f・残り（頭と末の月のずれ・q0 の基準）%.5f・w 中央 %.5f" % (wn, len(g), med([r["dF"] for r, _ in g]), cT, cC, cD, res, med([r["w"] for r, _ in g])))
    dmT = med([r["d"] - r["dT"] for r in ex]); dB = med([r["dB"] for r in ex])
    P("     d−dT の中央 %.5f・|dB の中央| %.5f ≦ w の中央 %.5f → 規則 0 は %s" % (dmT, abs(dB), b, "〈物差しの側〉" if abs(dB) <= b else "〈形の側〉"))
    for wn in WINS:
        g = [r for r in ex if r["w5"] == wn]
        if not g: continue
        P("     %s：行 %d・d %.5f・dF %.5f・dT %.5f・dB %.5f・w %.5f・dB は %s・T/T0 %.4f・fD %.4f・fC %.4f・σ/d0 %.4f" % (wn, len(g), med([r["d"] for r in g]), med([r["dF"] for r in g]),
            med([r["dT"] for r in g]), med([r["dB"] for r in g]), med([r["w"] for r in g]), "内" if abs(med([r["dB"] for r in g])) <= med([r["w"] for r in g]) else "外",
            med([r["TT0"] for r in g]), med([r["fD"] for r in g]), med([r["fC"] for r in g]), med([r["sig_d0"] for r in g])))
    P("   種ごと：")
    for sd in range(1, 11):
        g = [r for r in R if r["seed"] == sd]
        if g:
            nn, a, b, okk = cellstats(g)
            P("     種 %d：P5 %d・d 中央 %.5f・w 中央 %.5f → %s・dB 中央 %.5f・1900-50 の行の割合 %.3f" % (sd, nn, a, b, "通" if okk else "落", med([r["dB"] for r in g]), sum(1 for r in g if r["w5"] == "1900-50") / nn))
    # Δsoc の傾き（最小二乗・d と dF）
    def slope(xs, ys):
        pts = [(x, y) for x, y in zip(xs, ys) if x == x and y == y]
        if len(pts) < 3: return NAN
        mx = sum(x for x, _ in pts) / len(pts); my = sum(y for _, y in pts) / len(pts)
        sxx = sum((x - mx) ** 2 for x, _ in pts); sxy = sum((x - mx) * (y - my) for x, y in pts)
        return sxy / sxx if sxx > 0 else NAN
    P("   Δsoc への傾き：d %.4f・dF %.4f・dB %.4f（全行・dF と dB は厳密な行）" % (slope([r["dsoc"] for r in R], [r["d"] for r in R]),
        slope([r["dsoc"] for r in ex], [r["dF"] for r in ex]), slope([r["dsoc"] for r in ex], [r["dB"] for r in ex])))
    # [3] 幅
    P(""); P("[3] 幅の問い")
    P("   P5a（腕の q0）%d・P5b852（基線の q0）%d・重なり %d" % (ov["P5a"], ov["P5b852"], ov["P5b852∩P5a"]))
    def wtab(name, pairs):
        P("-- %s：全体 行 %d・w 中央 %.5f・w=0 割合 %.3f" % (name, len(pairs), med([w for _, w in pairs]), sum(1 for _, w in pairs if w == 0) / len(pairs) if pairs else NAN))
        for wn in WINS:
            g = [w for y, w in pairs if win(y) == wn]
            if g: P("   %s\t行 %d\t割合 %.4f\tw 中央 %.5f\tw=0 %.3f" % (wn, len(g), len(g) / len(pairs), med(g), sum(1 for w in g if w == 0) / len(g)))
    wtab("P5a・852 の 3 本", [(r["y"], r["w"]) for r in R])
    wtab("P5b852・852 の 3 本", P5b852)
    wtab("P5b839・839 の 3 本（m839 の盤面）", P5b839)
    k9 = [(r["y"], r["w839"]) for r in R if r["w839"] == r["w839"]]
    P("   腕の P5a の鍵で 839 の 3 本が揃う行 %d／%d" % (len(k9), len(R)))
    wtab("P5a の鍵・839 の 3 本", k9)
    P("   P5a の同じ行（839 も揃う）の 852 の w 中央 %.5f" % med([r["w"] for r in R if r["w839"] == r["w839"]]))
    g9 = [r for r in R if r["w839"] == r["w839"]]
    P("   839 の 2 本の差の中央：|B−N1| %.5f・|B−N2| %.5f・|N1−N2| %.5f（基線 M′ と N1・N2 の盤面が同じなら 3 つは近い）" % (med([r["w839_bn1"] for r in g9]), med([r["w839_bn2"] for r in g9]), med([r["w839_n1n2"] for r in g9])))
    for wn in WINS:
        g = [r for r in g9 if r["w5"] == wn]
        if g: P("     %s：839 |B−N1| %.5f・|B−N2| %.5f・|N1−N2| %.5f／852 |B−N1| %.5f・|B−N2| %.5f・|N1−N2| %.5f" % (wn, med([r["w839_bn1"] for r in g]), med([r["w839_bn2"] for r in g]), med([r["w839_n1n2"] for r in g]),
            med([r["w2_bn1"] for r in g]), med([r["w2_bn2"] for r in g]), med([r["w2_n1n2"] for r in g])))
    P("   粒度：P5a の 3 本の範囲の中央 %.5f・2 本の差の中央 |B−N1| %.5f・|B−N2| %.5f・|N1−N2| %.5f" % (mw, med([r["w2_bn1"] for r in R]), med([r["w2_bn2"] for r in R]), med([r["w2_n1n2"] for r in R])))
    # 窓の構成で全体の中央がどう決まるか：P5a の w を P5b852 の窓の割合で重みを付け直した中央（標本を割合に合わせて引かない＝窓ごとの w の分布をそのまま重みで混ぜる）
    def wmix(rows_by_win, weights):
        pts = []
        for wn, ws in rows_by_win.items():
            if not ws or weights.get(wn, 0) == 0: continue
            wt = weights[wn] / len(ws)
            pts += [(w, wt) for w in ws]
        pts.sort(); tot = sum(wt for _, wt in pts); acc = 0
        for w, wt in pts:
            acc += wt
            if acc >= tot / 2: return w
        return NAN
    byw = collections.defaultdict(list)
    for r in R: byw[r["w5"]].append(r["w"])
    cb = collections.Counter(win(y) for y, _ in P5b852)
    P("   P5a の w を P5b852 の窓の構成で混ぜ直した中央 %.5f（P5a の構成のまま %.5f）" % (wmix(byw, {k: v / len(P5b852) for k, v in cb.items()}), mw))
    bydw = collections.defaultdict(list)
    for r in R: bydw[r["w5"]].append(r["d"])
    # 経路
    P(""); P("[3'] 経路（dB を列の帯で割る・相関・厳密な行・焼いた行だけ）")
    exb = [r for r in ex if r["baked"]]
    def bands(name, f, cuts):
        P("-- %s" % name)
        prev = -math.inf
        for c in cuts + [math.inf]:
            g = [r for r in exb if r[f] == r[f] and prev <= r[f] < c]
            if g: P("   [%s, %s)\t行 %d\tdB 中央 %.5f\td 中央 %.5f\tw 中央 %.5f" % (prev, c, len(g), med([r["dB"] for r in g]), med([r["d"] for r in g]), med([r["w"] for r in g])))
            prev = c
        g = [r for r in exb if r[f] != r[f]]
        if g: P("   nan\t行 %d\tdB 中央 %.5f" % (len(g), med([r["dB"] for r in g])))
    bands("主食の値段の比 A÷B", "pxr", [0.9, 0.98, 1.02, 1.1])
    bands("市場の買いの割合の差 A−B", "msd", [-0.05, -0.01, 0.01, 0.05])
    bands("担い手の人数の比 A÷B", "spr", [0.95, 0.99, 1.01, 1.05])
    bands("移民の純÷人口の差 A−B（四半期）", "mig", [-0.002, -0.0002, 0.0002, 0.002])
    P("   印 M1 の行（焼いた・厳密）%d・dB 中央 %.5f" % (sum(1 for r in exb if r["M1"]), med([r["dB"] for r in exb if r["M1"]])))
    with open(os.path.join(outdir, "out_attr.txt"), "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(L) + "\n")
    print("\n".join(L))

if __name__ == "__main__":
    a = sys.argv[1:]
    main(a[0], a[1] if len(a) > 1 else 10)
