# (898) c76ds：r858/cornint858.py の写し（表示名だけ m873 へ替えた・式は同じ）。
# (858) c70ds 読むだけの台本：cornint_s を m852（keep_852/base_ins）と旧のヌル腕 m839（keep_839/n1・n2）で読み直し、
# 地主の穀物法・関税の項の各案（今の式・C1〔σ〕・C1 の上限〔σ=±1〕・C1′〔反実仮想の近似の上限〕）の値の分布を表にする。
# 書くのは標準出力だけ。エンジンを呼ばない。生は読むだけ。
# 使い方：PYTHONUTF8=1 python -I cornint858.py <基線の dir> [<ヌル腕の dir> ...]
import sys, os, math, statistics as st

SEEDS = range(1, 11)
COEF = 1.0                      # RentProtectionWeightCoef（Coefficients.cs:1962）
CORN = {0: 0.0, 1: 0.5, 2: 1.0}  # InternationalMarket.CornFactor（:171）×CornLawImportPenalty 1.0（Coefficients.cs:256）
PERIODS = [(1783, 1814), (1815, 1846), (1847, 1879), (1880, 1913), (1914, 1950)]
FOCUS = ["イギリス", "プロイセン", "ロシア", "アメリカ", "フランス", "オーストリア", "清", "日本", "オスマン"]

def f(x):
    try:
        v = float(x)
    except ValueError:
        return float("nan")
    return v

def q(vals, p):
    v = sorted(x for x in vals if not math.isnan(x))
    if not v:
        return float("nan")
    k = (len(v) - 1) * p
    lo = int(math.floor(k)); hi = int(math.ceil(k))
    return v[lo] if lo == hi else v[lo] + (v[hi] - v[lo]) * (k - lo)

def load(d):
    rows = []; header = None
    for s in SEEDS:
        p = os.path.join(d, f"cornint_s{s}.tsv")
        if not os.path.exists(p):
            continue
        with open(p, "rb") as fh:
            lines = fh.read().decode("utf-8").splitlines()
        h = lines[0].split("\t")
        if header is None:
            header = h
        elif h[:len(header)] != header[:len(h)]:
            print(f"!! 見出しが種で違う: {p}")
        idx = {n: i for i, n in enumerate(h)}
        for ln in lines[1:]:
            c = ln.split("\t")
            r = {n: c[i] for n, i in idx.items() if i < len(c)}
            r["seed"] = s
            rows.append(r)
    return header, rows

def tariff_map(d):
    # scalars_s の tariffTargetIndex（関税の目標＝実際の率の近似）を (種, 四半期, 国) で引く
    m = {}
    for s in SEEDS:
        p = os.path.join(d, f"scalars_s{s}.tsv")
        if not os.path.exists(p):
            continue
        with open(p, "rb") as fh:
            lines = fh.read().decode("utf-8").splitlines()
        h = lines[0].split("\t"); ix = {n: i for i, n in enumerate(h)}
        if "tariffTargetIndex" not in ix:
            return {}
        for ln in lines[1:]:
            c = ln.split("\t")
            m[(s, c[ix["quarter"]], c[ix["nation"]])] = f(c[ix["tariffTargetIndex"]])
    return m

def derive(r, tar):
    g = lambda k: f(r.get(k, "nan"))
    tr = g("rentDomTotalPerMonth")
    rexp = g("rentExpPerMonth0") + g("rentExpPerMonth1") + g("rentExpPerMonth2")
    rtot = tr + rexp
    gd0 = g("rentGrainDomPerMonth0"); ge0 = g("rentGrainExpPerMonth0")
    code = int(f(r.get("cornLawsCode", "0")))
    cf = CORN.get(code, 0.0)
    cf_dn = CORN.get(max(0, code - 1), 0.0); cf_up = CORN.get(min(2, code + 1), 0.0)
    tau = tar.get((r["seed"], r["quarter"], r["nation"]), float("nan"))
    if math.isnan(tau):
        tau = 0.0
    out = {}
    out["now"] = g("cornPrefNow0")
    out["c1"] = g("c1CornPref0")
    out["int0"] = g("c1InterestGrain0")
    out["sig"] = g("sigmaGrainRentW")
    out["tnow"] = g("tariffLandTermNow0"); out["tc1"] = g("c1TariffLand0")
    # C1 の上限＝σ を ±1 にしても届く大きさ＝係数×（ブロック 0 の穀物の地代）÷（国の地代の和）
    out["c1max"] = COEF * (gd0 + ge0) / rtot if rtot > 0 else float("nan")
    # C1′ の近似（向きは出せない＝大きさの上限だけ）：国内で売れた穀物の地代×次の段の楔の差÷(1＋今の楔)÷国の地代の和
    #   下げる向き（今の段を 1 つ戻したら失う上限）と上げる向き（1 つ進めたら得る上限）。楔＝関税の目標＋穀物法。
    w_now = 1.0 + tau + cf
    out["cfdn"] = COEF * gd0 * (cf - cf_dn) / w_now / rtot if rtot > 0 else float("nan")
    out["cfup"] = COEF * gd0 * (cf_up - cf) / w_now / rtot if rtot > 0 else float("nan")
    out["code"] = code
    out["year"] = int(f(r["year"]))
    out["grainShare0"] = (gd0 + ge0) / (g("rentDomPerMonth0") + g("rentExpPerMonth0")) if (g("rentDomPerMonth0") + g("rentExpPerMonth0")) > 0 else float("nan")
    out["expShare0"] = ge0 / (gd0 + ge0) if (gd0 + ge0) > 0 else float("nan")
    return out

def share(vals, pred):
    v = [x for x in vals if not math.isnan(x)]
    return (sum(1 for x in v if pred(x)) / len(v)) if v else float("nan"), len(v)

def summarize(name, d):
    header, rows = load(d)
    if not rows:
        print(f"## {name}: {d} に cornint_s が無い"); return None
    tar = tariff_map(d)
    print(f"## {name}  dir={d}  行={len(rows)}  列={len(header)}  関税の目標の表={'あり' if tar else 'なし'}")
    D = [(r, derive(r, tar)) for r in rows]
    trade = [(r, x) for r, x in D if not math.isnan(x["int0"])]
    print(f"- 貿易の配列のある行（C1 が数えられる行）={len(trade)}（{len(trade)/len(D):.3f}）")
    i0 = [x["int0"] for _, x in trade]
    for lab, pr in [("厳密に 0", lambda v: v == 0.0), ("正（輸入競合に傾く）", lambda v: v > 0), ("負（輸出に傾く）", lambda v: v < 0)]:
        s, n = share(i0, pr); print(f"- ブロック 0 の C1 の利害 c1InterestGrain0 が {lab}: {s:.3f}（{n} 行）")
    # 地代の付いた穀物を持つ行に限る
    hasg = [(r, x) for r, x in trade if x["c1max"] > 0]
    s, n = share([x["int0"] for _, x in hasg], lambda v: v == 0.0)
    print(f"- うちブロック 0 に穀物の地代がある行={n}・その中で C1 が厳密に 0 の割合={s:.3f}")
    s, n = share([x["now"] for _, x in trade], lambda v: v > 0)
    print(f"- 今の式（cornPrefNow0）が正の割合={s:.3f}（ブロック 0 は −0 の札が無いので地代があれば常に正）")
    s, n = share([x["c1"] for _, x in trade], lambda v: v < 0)
    print(f"- C1 の式（c1CornPref0）が負の割合={s:.3f}")
    s, n = share([x["cfdn"] for _, x in trade], lambda v: v == 0.0)
    s2, _ = share([x["cfup"] for _, x in trade], lambda v: v == 0.0)
    print(f"- C1′ 近似の上限が厳密に 0 の割合：下げる向き {s:.3f}・上げる向き {s2:.3f}（0 になるのは穀物の地代 0 か、その向きに段が無い行だけ）")
    print()
    print("| 時期 | 行 | 今の式 中央（p10〜p90） | C1 中央（p10〜p90） | C1 の上限 中央（p90・最大） | C1′ 下げ 中央（p90） | C1′ 上げ 中央（p90） | σ 地代加重 中央 | 穀物の地代の割合 中央 | 穀物の地代の輸出の割合 中央 |")
    print("|---|---|---|---|---|---|---|---|---|---|")
    for a, b in PERIODS:
        xs = [x for _, x in trade if a <= x["year"] <= b]
        if not xs:
            continue
        col = lambda k: [x[k] for x in xs]
        print(f"| {a}〜{b} | {len(xs)} | {q(col('now'),.5):.4f}（{q(col('now'),.1):.4f}〜{q(col('now'),.9):.4f}） | {q(col('c1'),.5):.5f}（{q(col('c1'),.1):.5f}〜{q(col('c1'),.9):.5f}） | {q(col('c1max'),.5):.4f}（{q(col('c1max'),.9):.4f}・{max([v for v in col('c1max') if not math.isnan(v)] or [float('nan')]):.3f}） | {q(col('cfdn'),.5):.4f}（{q(col('cfdn'),.9):.4f}） | {q(col('cfup'),.5):.4f}（{q(col('cfup'),.9):.4f}） | {q(col('sig'),.5):.4f} | {q(col('grainShare0'),.5):.3f} | {q(col('expShare0'),.5):.3f} |")
    print()
    print("| 国 | 時期 | 行 | 穀物法の段 中央 | 今の式 中央 | C1 中央 | C1 の正の割合 | C1 の上限 中央 | C1′ 下げ 中央 | C1′ 上げ 中央 | σ 地代加重 中央 |")
    print("|---|---|---|---|---|---|---|---|---|---|---|")
    for nat in FOCUS:
        for a, b in PERIODS:
            xs = [x for r, x in trade if r["nation"] == nat and a <= x["year"] <= b]
            if not xs:
                continue
            col = lambda k: [x[k] for x in xs]
            pos, _ = share(col("int0"), lambda v: v > 0)
            print(f"| {nat} | {a}〜{b} | {len(xs)} | {q([float(x['code']) for x in xs],.5):.1f} | {q(col('now'),.5):.4f} | {q(col('c1'),.5):.5f} | {pos:.3f} | {q(col('c1max'),.5):.4f} | {q(col('cfdn'),.5):.4f} | {q(col('cfup'),.5):.4f} | {q(col('sig'),.5):.4f} |")
    print()
    # 穀物法の段の動き（振動の門の基線）：国×世紀あたりの段の変化の回数と、向きの反転の回数（種ごと）
    seq = {}
    for r, x in D:
        seq.setdefault((r["seed"], r["nation"]), []).append((int(f(r["quarter"])), x["year"], x["code"], x["now"], x["c1"]))
    changes = []; flips = []; c1flips = []; nowflips = []; top = 0; tot = 0
    for k, v in seq.items():
        v.sort()
        yrs = (v[-1][1] - v[0][1] + 1) / 100.0
        if yrs <= 0.1:
            continue
        ch = sum(1 for i in range(1, len(v)) if v[i][2] != v[i-1][2])
        dirs = [1 if v[i][2] > v[i-1][2] else -1 for i in range(1, len(v)) if v[i][2] != v[i-1][2]]
        fl = sum(1 for i in range(1, len(dirs)) if dirs[i] != dirs[i-1])
        sg = lambda z: 0 if math.isnan(z) else (1 if z > 0 else (-1 if z < 0 else 0))
        c1s = [sg(t[4]) for t in v if not math.isnan(t[4])]
        c1f = sum(1 for i in range(1, len(c1s)) if c1s[i] != c1s[i-1] and c1s[i] != 0 and c1s[i-1] != 0)
        nws = [sg(t[3]) for t in v if not math.isnan(t[3])]
        nwf = sum(1 for i in range(1, len(nws)) if nws[i] != nws[i-1] and nws[i] != 0 and nws[i-1] != 0)
        changes.append(ch / yrs); flips.append(fl / yrs); c1flips.append(c1f / yrs); nowflips.append(nwf / yrs)
        top += sum(1 for t in v if t[2] == 2); tot += len(v)
    print(f"- 国×種の系列={len(changes)}・穀物法の段の変化 回/世紀 中央 {q(changes,.5):.2f}・p90 {q(changes,.9):.2f}・最大 {max(changes):.1f}")
    print(f"- 段の向きの反転 回/世紀 中央 {q(flips,.5):.2f}・p90 {q(flips,.9):.2f}・最大 {max(flips):.1f}")
    print(f"- Pref の符号の反転（正↔負・0 を除く）回/世紀：今の式 中央 {q(nowflips,.5):.2f}・p90 {q(nowflips,.9):.2f}・最大 {max(nowflips):.1f}／C1 中央 {q(c1flips,.5):.2f}・p90 {q(c1flips,.9):.2f}・最大 {max(c1flips):.1f}")
    print(f"- 穀物法が全面（段 2）の国×四半期の割合 {top/tot:.3f}（{top}/{tot}）")
    # なしへ落ちた後に段が上がった系列（戻り）の数。系列は国×種（短い系列＝建国した国も含む）
    fell = 0; back = 0; backlong = 0; ex = []
    for k, v in seq.items():
        i0 = next((i for i, t in enumerate(v) if t[2] == 0), None)
        if i0 is None:
            continue
        fell += 1
        ups = sum(1 for i in range(i0 + 1, len(v)) if v[i][2] > v[i-1][2])
        if ups > 0:
            back += 1
            if (v[-1][1] - v[0][1]) >= 20:
                backlong += 1
            if len(ex) < 8:
                ex.append(f"種{k[0]} {k[1]}（{v[0][1]}〜{v[-1][1]}・なし{v[i0][1]}・上げ{ups}回）")
    print(f"- なしへ落ちた系列 {fell}・その後に段が上がった系列 {back}（うち 20 年以上の系列 {backlong}）・例：{'、'.join(ex)}")
    print()
    return True

if __name__ == "__main__":
    args = sys.argv[1:]
    names = ["m873 基線（keep_873/base_ins）", "m873 N1（keep_873/n1）", "m873 N2（keep_873/n2）"]
    for i, d in enumerate(args):
        summarize(names[i] if i < len(names) else d, d)
