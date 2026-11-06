# (898) c76ds 読むだけの台本：地主の政治の段 C-0′ の紙の数を盤面 m873 で取り直す（cornint898.py・status898.py の外の項目）。
# 書くのは標準出力だけ。エンジンを呼ばない。生は読むだけ。
# 使い方：python -I -X utf8 paper898.py <dir> [<dir> ...]（keep_873/base_ins・n1・n2）
# 項目：
#  P1 流れの近似の 3 つの型（①′ 純輸入・②′ 穀物が国境を越えない・③′ 純輸出）の国×四半期の割合（値段の型〔着値と国内の値段〕は cornint_s に列が無い＝〈C-0′ の計器が出す〉）
#  P2 穀物法の段の分布と全面の張り付きの割合
#  P3 G2 の母集合〈③（穀物を輸出している）かつ穀物法が限定以上〉の件数と、M3〔S↓ の max を外す〕が |選好|≧0.05 に届くのに要る |S↓| の分布
#  P4 G3 の母集合（穀物法の段を上げた国×四半期・その前の四半期の流れの型・なしの後に上がった系列）
#  P5 旧支配層の今の穀物法の選好（cornPrefNow0）の分布（時期ごと・段ごと）
#  P6 S↑ の近似（輸入の割合 m を掛けた上限）の 0↔正 の切り替わり 回/世紀（訂正 9 の物差し）と、σ の符号の反転との並べ
#  P7 旧支配層の街頭の重み（fm_s の |netStreetTradIndex|＝street×Power[0]・Coercion の旧支配層の選好が 1.0 のため）と、穀物法の一歩の純支持の差（街頭の分だけ・議会の分は C-0′ の計器が出す）
# 分母：国の地代の和は rentDomTotalPerMonth（＝エンジンの Build の Tr と同じ集合・POP の max(0, LandRentPerMonth) の和）。
import sys, os, math

SEEDS = range(1, 11)
COEF = 1.0                       # RentProtectionWeightCoef（Coefficients.cs:1963）
CORN = {0: 0.0, 1: 0.5, 2: 1.0}   # InternationalMarket.CornFactor（:171）×CornLawImportPenalty 1.0（Coefficients.cs:256）
SPEND = 0.5                      # MandateSpendRate（Coefficients.cs:2912）
PERIODS = [(1783, 1814), (1815, 1846), (1847, 1879), (1880, 1913), (1914, 1950)]
FOCUS = ["イギリス", "プロイセン", "ロシア", "アメリカ", "フランス", "オーストリア", "清", "日本", "オスマン"]

def f(x):
    try:
        return float(x)
    except ValueError:
        return float("nan")

def q(vals, p):
    v = sorted(x for x in vals if not math.isnan(x))
    if not v:
        return float("nan")
    k = (len(v) - 1) * p
    lo = int(math.floor(k)); hi = int(math.ceil(k))
    return v[lo] if lo == hi else v[lo] + (v[hi] - v[lo]) * (k - lo)

def rd(p):
    with open(p, "rb") as fh:
        lines = fh.read().decode("utf-8").splitlines()
    h = lines[0].split("\t"); ix = {n: i for i, n in enumerate(h)}
    return ix, [ln.split("\t") for ln in lines[1:]]

def per_of(y):
    return next((pp for pp in PERIODS if pp[0] <= y <= pp[1]), None)

def load(d):
    rows = []
    for s in SEEDS:
        p = os.path.join(d, f"cornint_s{s}.tsv")
        if not os.path.exists(p):
            continue
        ix, rr = rd(p)
        tar = {}
        ps = os.path.join(d, f"scalars_s{s}.tsv")
        if os.path.exists(ps):
            jx, sr = rd(ps)
            for c in sr:
                tar[(c[jx["quarter"]], c[jx["nation"]])] = f(c[jx["tariffTargetIndex"]])
        fm = {}
        pf = os.path.join(d, f"fm_s{s}.tsv")
        if os.path.exists(pf):
            kx, fr = rd(pf)
            for c in fr:
                fm[(c[kx["quarter"]], c[kx["nation"]])] = abs(f(c[kx["netStreetTradIndex"]]))
        for c in rr:
            g = lambda k: f(c[ix[k]])
            qq = c[ix["quarter"]]; nat = c[ix["nation"]]
            r = dict(seed=s, q=int(f(qq)), year=int(g("year")), nation=nat, code=int(g("cornLawsCode")),
                     tr=g("rentDomTotalPerMonth"), gd0=g("rentGrainDomPerMonth0"), ge0=g("rentGrainExpPerMonth0"),
                     gimp=g("grainImportValue"), gexp=g("grainExportValue"), gprod=g("grainProducedValue"),
                     now0=g("cornPrefNow0"), int0=g("c1InterestGrain0"),
                     tau=tar.get((qq, nat), float("nan")), w0=fm.get((qq, nat), float("nan")))
            rows.append(r)
    return rows

def ftype(r):
    gi, ge = r["gimp"], r["gexp"]
    if math.isnan(gi) or math.isnan(ge):
        return None
    if gi > ge: return 1
    if ge > gi: return 3
    return 2   # 両方 0（または等しい）

def derive(r):
    tr = r["tr"]; code = r["code"]
    cf = CORN.get(code, 0.0); cfdn = CORN.get(max(0, code - 1), 0.0); cfup = CORN.get(min(2, code + 1), 0.0)
    tau = r["tau"] if not math.isnan(r["tau"]) else 0.0
    wnow = 1.0 + tau + cf
    gshare = r["gd0"] / tr if tr > 1e-9 else float("nan")       # ブロック 0 の穀物の地代÷国の地代の和（エンジンの Tr）
    sdn = (cf - cfdn) / wnow                                      # S↓ の上限（楔の差÷今の楔の倍率・値段の張り付きを 1 と置いた上限）
    gi, gp = r["gimp"], r["gprod"]
    m = gi / (gi + gp) if not math.isnan(gi) and not math.isnan(gp) and gi + gp > 0 else float("nan")
    sup = m * (cfup - cf) / wnow if not math.isnan(m) else float("nan")   # S↑ の上限（輸入の割合 m を掛ける）
    return dict(gshare=gshare, sdn=sdn, sup=sup, m=m,
                prefdn=COEF * gshare * sdn if not math.isnan(gshare) else float("nan"),
                prefup=COEF * gshare * sup if not math.isnan(gshare) and not math.isnan(sup) else float("nan"))

def fmt(x, k=3):
    return "nan" if math.isnan(x) else f"{x:.{k}f}"

def summarize(name, d):
    rows = load(d)
    print(f"## {name}  dir={d}  行={len(rows)}")
    if not rows:
        return
    for r in rows:
        r.update(derive(r)); r["ft"] = ftype(r)
    has = [r for r in rows if r["ft"] is not None and r["gd0"] + (r["ge0"] if not math.isnan(r["ge0"]) else 0) > 0]
    # P1
    print("### P1 流れの近似の 3 つの型（貿易の配列があり、ブロック 0 に穀物の地代がある行）")
    print("| 時期 | 行 | ①′ 純輸入 | ②′ 越えない | ③′ 純輸出 | 穀物の輸出 grainExportValue＞0 |")
    print("|---|---|---|---|---|---|")
    for a, b in PERIODS + [(1783, 1950)]:
        xs = [r for r in has if a <= r["year"] <= b]
        if not xs: continue
        n = len(xs)
        c1 = sum(1 for r in xs if r["ft"] == 1); c2 = sum(1 for r in xs if r["ft"] == 2); c3 = sum(1 for r in xs if r["ft"] == 3)
        ex = sum(1 for r in xs if r["gexp"] > 0)
        print(f"| {a}〜{b} | {n} | {c1/n:.3f} | {c2/n:.3f} | {c3/n:.3f} | {ex/n:.3f} |")
    print()
    # P2
    print("### P2 穀物法の段の分布（全ての国×四半期）")
    n = len(rows)
    cnt = [sum(1 for r in rows if r["code"] == k) for k in (0, 1, 2)]
    print(f"- なし {cnt[0]/n:.3f}・限定 {cnt[1]/n:.3f}・全面 {cnt[2]/n:.3f}（{n} 行）＝全面の張り付きの割合 {cnt[2]/n:.3f}")
    for a, b in PERIODS:
        xs = [r for r in rows if a <= r["year"] <= b]
        if not xs: continue
        m_ = len(xs); cc = [sum(1 for r in xs if r["code"] == k) / m_ for k in (0, 1, 2)]
        print(f"  - {a}〜{b}：なし {cc[0]:.3f}・限定 {cc[1]:.3f}・全面 {cc[2]:.3f}（{m_}）")
    print()
    # P3
    print("### P3 G2 の母集合〈③（grainExportValue＞0）かつ穀物法が限定以上〉")
    for lab, pred in [("③＝grainExportValue＞0", lambda r: r["gexp"] > 0), ("③′＝純輸出", lambda r: r["ft"] == 3)]:
        pop = [r for r in has if pred(r) and r["code"] >= 1]
        ser = {(r["seed"], r["nation"]) for r in pop}
        need = [0.05 / (COEF * r["gshare"]) for r in pop if r["gshare"] > 0]
        nat = {}
        for r in pop: nat[r["nation"]] = nat.get(r["nation"], 0) + 1
        top = sorted(nat.items(), key=lambda kv: -kv[1])[:8]
        print(f"- {lab}：国×四半期 {len(pop)}・国×種の系列 {len(ser)}・時期別 " + "・".join(f"{a}〜{b} {sum(1 for r in pop if a <= r['year'] <= b)}" for a, b in PERIODS))
        print(f"  - M3 で |選好|≧0.05 になるのに要る |S↓|＝0.05÷(穀物の地代÷国の地代) 中央 {fmt(q(need,.5))}（p10 {fmt(q(need,.1))}・p90 {fmt(q(need,.9))}）")
        print(f"  - ブロック 0 の穀物の地代÷国の地代 中央 {fmt(q([r['gshare'] for r in pop],.5))}・多い国 " + "、".join(f"{k} {v}" for k, v in top))
    print()
    # P4
    print("### P4 G3 の母集合（穀物法の段を上げた国×四半期）")
    seq = {}
    for r in rows: seq.setdefault((r["seed"], r["nation"]), []).append(r)
    ups = []; fell = 0; back = 0
    for k, v in seq.items():
        v.sort(key=lambda r: r["q"])
        seen0 = False; had_back = False
        for i in range(1, len(v)):
            if v[i]["code"] > v[i-1]["code"]:
                ups.append((v[i-1], v[i], seen0))
                if seen0: had_back = True
            if v[i-1]["code"] == 0: seen0 = True
        if v[0]["code"] == 0 or any(r["code"] == 0 for r in v): fell += 1
        if had_back: back += 1
    up_after0 = [u for u in ups if u[2]]
    prev1 = [u for u in ups if u[0]["ft"] == 1]
    prev1_after0 = [u for u in up_after0 if u[0]["ft"] == 1]
    print(f"- 段を上げた国×四半期 {len(ups)}（うち なし を一度通った後 {len(up_after0)}）・上げる前の四半期が ①′ 純輸入 {len(prev1)}（なしの後で ①′ {len(prev1_after0)}）")
    print(f"- なしを一度通った系列 {fell}・その後に上がった系列 {back}")
    print()
    # P5
    print("### P5 旧支配層の今の穀物法の選好 cornPrefNow0（貿易の配列のある行・時期ごと・段ごと）")
    print("| 時期 | 行 | 中央 | p10 | p90 | なしの行の中央 | 限定以上の行の中央 |")
    print("|---|---|---|---|---|---|---|")
    tr_ = [r for r in rows if r["ft"] is not None]
    for a, b in PERIODS:
        xs = [r for r in tr_ if a <= r["year"] <= b]
        if not xs: continue
        v = [r["now0"] for r in xs]
        print(f"| {a}〜{b} | {len(xs)} | {fmt(q(v,.5))} | {fmt(q(v,.1))} | {fmt(q(v,.9))} | {fmt(q([r['now0'] for r in xs if r['code']==0],.5))} | {fmt(q([r['now0'] for r in xs if r['code']>=1],.5))} |")
    print()
    # P5b C1′ の上限（エンジンの Tr で割る）
    print("### P5b C1′ の上限（ブロック 0・エンジンの Tr で割る・値段の張り付きを 1 と置いた上限＝真の値はそれ以下）")
    print("| 時期 | 下げる向き（限定以上の行）中央（p90） | 上げる向き（全面でない行・m を掛ける）中央（p90） | 上げる向きで 0 の割合 |")
    print("|---|---|---|---|")
    for a, b in PERIODS:
        dn = [r["prefdn"] for r in has if a <= r["year"] <= b and r["code"] >= 1]
        up = [r["prefup"] for r in has if a <= r["year"] <= b and r["code"] <= 1]
        z = sum(1 for x in up if x == 0) / len(up) if up else float("nan")
        print(f"| {a}〜{b} | {fmt(q(dn,.5),4)}（{fmt(q(dn,.9),4)}・{len(dn)}） | {fmt(q(up,.5),4)}（{fmt(q(up,.9),4)}・{len(up)}） | {fmt(z)} |")
    print()
    # P6
    print("### P6 S↑ の近似の揺れ（回/世紀）と σ の符号の反転")
    onoff = []; sflip = []; jumps = []
    for k, v in seq.items():
        yrs = (v[-1]["year"] - v[0]["year"] + 1) / 100.0
        if yrs <= 0.1: continue
        su = [r["prefup"] for r in v if r["code"] <= 1 and not math.isnan(r["prefup"])]
        oo = sum(1 for i in range(1, len(su)) if (su[i] > 0) != (su[i-1] > 0))
        onoff.append(oo / yrs)
        jj = sum(1 for i in range(1, len(su)) if su[i] > 0 and su[i-1] > 0 and abs(su[i] - su[i-1]) > 0.5 * max(su[i], su[i-1]))
        jumps.append(jj / yrs)
        sg = [(1 if r["int0"] > 0 else -1 if r["int0"] < 0 else 0) for r in v if not math.isnan(r["int0"])]
        sf = sum(1 for i in range(1, len(sg)) if sg[i] != sg[i-1] and sg[i] != 0 and sg[i-1] != 0)
        sflip.append(sf / yrs)
    print(f"- 系列 {len(onoff)}：S↑ の近似の 0↔正 の切り替わり 回/世紀 中央 {fmt(q(onoff,.5),2)}・p90 {fmt(q(onoff,.9),2)}・最大 {fmt(max(onoff),1)}")
    print(f"- S↑ の近似が正のまま前の四半期から半分以上動いた回/世紀 中央 {fmt(q(jumps,.5),2)}・p90 {fmt(q(jumps,.9),2)}・最大 {fmt(max(jumps),1)}")
    print(f"- σ（C1 の利害）の符号の反転 回/世紀 中央 {fmt(q(sflip,.5),2)}・p90 {fmt(q(sflip,.9),2)}・最大 {fmt(max(sflip),1)}")
    print()
    # P7
    print("### P7 旧支配層の街頭の重み w0＝|netStreetTradIndex|（fm_s の国）と、穀物法の一歩の純支持の差（街頭の分だけ）")
    print("| 国 | 時期 | 行 | w0 中央 | 撤廃の一歩：旧の旧支配層の項 −w0×now0 中央 | 新（上限）−w0×C1′下げ 中央 | 差（撤廃が安くなる分・純支持）中央 | 代価の差＝差×0.5 中央 | 上げる一歩：旧 +w0×now0 中央 | 新（上限）+w0×C1′上げ 中央 |")
    print("|---|---|---|---|---|---|---|---|---|---|")
    for nat in FOCUS:
        for a, b in PERIODS:
            xs = [r for r in has if r["nation"] == nat and a <= r["year"] <= b and not math.isnan(r["w0"])]
            if not xs: continue
            w0 = [r["w0"] for r in xs]
            old_dn = [-r["w0"] * r["now0"] for r in xs if r["code"] >= 1]
            new_dn = [-r["w0"] * r["prefdn"] for r in xs if r["code"] >= 1]
            dd = [r["w0"] * (r["now0"] - r["prefdn"]) for r in xs if r["code"] >= 1]
            old_up = [r["w0"] * r["now0"] for r in xs if r["code"] <= 1]
            new_up = [r["w0"] * r["prefup"] for r in xs if r["code"] <= 1 and not math.isnan(r["prefup"])]
            print(f"| {nat} | {a}〜{b} | {len(xs)} | {fmt(q(w0,.5))} | {fmt(q(old_dn,.5))}（{len(old_dn)}） | {fmt(q(new_dn,.5))} | {fmt(q(dd,.5))} | {fmt(q([x*SPEND for x in dd],.5))} | {fmt(q(old_up,.5))}（{len(old_up)}） | {fmt(q(new_up,.5))} |")
    print()

if __name__ == "__main__":
    names = ["m873 基線（keep_873/base_ins）", "m873 N1（keep_873/n1）", "m873 N2（keep_873/n2）"]
    for i, d in enumerate(sys.argv[1:]):
        summarize(names[i] if i < len(names) else d, d)
