# (488) 死の側の段2 の読み手（読むだけ）：計器つきの生のフォルダから 粗出生・粗死亡・うち飢えに帰せる死・乳児死亡・州の飢えと死（mort_s）を種の中央値で出す。
# 使い方: python -I an_dx488.py <計器の生のフォルダ> [種の数=10]
# 出力：①国×期間の 粗出生・粗死亡・うち飢え・うち他（pop_s）②乳児死亡（age_s の 0 歳の層の死÷出生）③G2-3：充足 0.6 未満が 2 行以上続く州の死の率 ÷ 直前の充足 0.9 以上の 4 行の死の率（mort_s・州の続きの中央値）
#   ④国の人口の初め・中・終わり（pop_s の popK の期間の平均）⑤G2-R1：1783-99 の粗死亡 ÷ 1800-49 の粗死亡
# 限界：mort_s の死は平常の死だけ（疫病・戦死は入らない）。挙げた国は四半期の行・他の国は年末の行（monthsCount 12）。旗 0 の生には mort_s が無い（G2-3 は旗 1 だけ）。
import csv, sys, os, statistics as st, collections
sys.stdout.reconfigure(encoding="utf-8")
D = sys.argv[1]; NS = int(sys.argv[2]) if len(sys.argv) > 2 else 10
NATIONS = ["イギリス", "フランス", "プロイセン", "ロシア", "日本", "清"]
eras = [(1783, 1799), (1800, 1849), (1850, 1899), (1900, 1949)]
def rows(name, s):
    p = f"{D}/{name}_s{s}.tsv"
    if not os.path.exists(p): return
    with open(p, encoding="utf-8") as f:
        for r in csv.DictReader(f, delimiter="\t"): yield r
def fl(x):
    try: return float(x)
    except (TypeError, ValueError): return float("nan")
# ① pop_s
acc = collections.defaultdict(lambda: collections.defaultdict(list))
for s in range(1, NS + 1):
    per = collections.defaultdict(lambda: collections.defaultdict(float))
    for r in rows("pop", s):
        if r["scope"] != "N": continue
        y = int(r["year"]); n = r["nation"]
        for i, (a, b) in enumerate(eras):
            if a <= y <= b:
                k = per[(n, i)]; m = fl(r["monthsCount"])
                k["pop"] += fl(r["popK"]) * m / 12.0
                for c in ["birthsK", "deathsNormalK", "deathsFamineK", "deathsOtherK", "warDeadK"]:
                    v = fl(r[c]); k[c] += 0 if v != v else v
    for key, k in per.items():
        if k["pop"] <= 0: continue
        t = acc[key]
        t["cbr"].append(1000 * k["birthsK"] / k["pop"])
        t["cdr"].append(1000 * (k["deathsNormalK"] + k["deathsFamineK"] + k["deathsOtherK"]) / k["pop"])
        t["fam"].append(1000 * k["deathsFamineK"] / k["pop"]); t["oth"].append(1000 * k["deathsOtherK"] / k["pop"])
        t["pop"].append(k["pop"])
print("# ① 国×期間（種の中央値・‰）CBR・CDR・うち飢えに帰せる死 fam・うち疫病ほか oth")
for n in NATIONS:
    for i, (a, b) in enumerate(eras):
        t = acc.get((n, i))
        if t: print(f"{n}\t{a}-{b}\tCBR {st.median(t['cbr']):.1f}\tCDR {st.median(t['cdr']):.1f}\tfam {st.median(t['fam']):.2f}\toth {st.median(t['oth']):.2f}\tn={len(t['cdr'])}")
# ⑤ G2-R1
print("# ⑤ G2-R1：1783-99 の粗死亡 ÷ 1800-49 の粗死亡（種の中央値）")
for n in NATIONS:
    a, b = acc.get((n, 0)), acc.get((n, 1))
    if a and b: print(f"{n}\t{st.median(a['cdr']) / st.median(b['cdr']):.2f}")
# ② 乳児
inf = collections.defaultdict(list)
for s in range(1, NS + 1):
    per = collections.defaultdict(lambda: [0.0, 0.0])
    for r in rows("age", s):
        if r["scope"] == "B" and r["bandIndex"] == "0":
            y = int(r["year"]); n = r["nation"]
            for i, (a, b) in enumerate(eras):
                if a <= y <= b:
                    d, bi = fl(r["deathsK"]), fl(r["birthsK"])
                    per[(n, i)][0] += 0 if d != d else d; per[(n, i)][1] += 0 if bi != bi else bi
    for k, (d, bi) in per.items():
        if bi > 0: inf[k].append(1000 * d / bi)
print("# ② 乳児死亡（0 歳の層の死÷出生・‰・種の中央値）")
for n in NATIONS:
    print(n, " ".join(f"{a}-{b}:{st.median(inf[(n, i)]):.0f}" for i, (a, b) in enumerate(eras) if inf.get((n, i))))
# ③ G2-3（追記 [A11] で出し直した形・[A3] の形は参考へ）：国×年の群（州が 8 以上）ごとに、州の主食の充足（その年の mort_s の平均）が低い 4 分の 1 と高い 4 分の 1 を取り、
#   飢えに帰せる超過 H ＝ Σ deathsHungerK ÷ Σ（deathsK − deathsHungerK）。見込み Hexp ＝ Φσ̄ × 4 分の 1 の州の (1−充足)^6 の平均（Φσ̄ ＝ (6÷L)÷Σh − 1 ＝ 標準のピラミッドで加重した飢えの利得・表の手の写し）。
#   判定：Hexp_lo ≥ 0.02 の群（飢えが深い群）のうち、①H_lo(観測) ≥ 0.5 × Hexp_lo ②H_lo(観測) > H_hi(観測)、の両方を満たす群の割合が 80% 以上（群が 100 未満なら判定不能）。
#   記録：死の率の比（低充足 4 分の 1 ÷ 高充足 4 分の 1）の中央値と 1 超の割合・農村の国だけ。旧の形（[A3] 前：充足 0.6 未満が 2 行以上続く州÷直前の充足 0.9 以上の 4 行）。
L_LEVEL = 1.0946
PYR = [0.0300, 0.1317, 0.1150, 0.1005, 0.0903, 0.1568, 0.1273, 0.1025, 0.0770, 0.0474, 0.0215]
HAND = [0.220, 0.040, 0.012, 0.007, 0.009, 0.012, 0.016, 0.022, 0.036, 0.065, 0.130]
GAIN = (6.0 / L_LEVEL) / sum(p * h for p, h in zip(PYR, HAND)) - 1.0
def starv(f):
    d = 1.0 - f
    if not (d > 0): return 0.0
    return min(d, 1.0) ** 6
ratios = []; ratiosRural = []
old_ratios = []
deep = 0; deepOk = 0; deepDir = 0; deepBoth = 0; allGroups = 0
Hlo_all = []
for s in range(1, NS + 1):
    ser = collections.defaultdict(list)
    for r in rows("mort", s):
        ser[(r["nation"], r["regionId"])].append(r)
    grp = collections.defaultdict(list)
    for (n, rid), lst in ser.items():
        by_year = collections.defaultdict(list)
        for r in lst: by_year[int(r["year"])].append(r)
        for y, rr in by_year.items():
            d = sum(fl(r["deathsK"]) for r in rr); h = sum(fl(r["deathsHungerK"]) for r in rr)
            py = sum(fl(r["popMeanK"]) * fl(r["monthsCount"]) / 12.0 for r in rr)
            if py > 0 and d == d and h == h:
                grp[(n, y)].append((sum(fl(r["foodFill"]) for r in rr) / len(rr), d, h, py, fl(rr[-1]["urbanShare"])))
    for (n, y), lst in grp.items():
        if len(lst) < 8: continue
        lst.sort(key=lambda t: t[0]); q = max(1, len(lst) // 4)
        lo, hi = lst[:q], lst[-q:]
        rl = sum(t[1] for t in lo) / sum(t[3] for t in lo); rh = sum(t[1] for t in hi) / sum(t[3] for t in hi)
        if rh > 0:
            ratios.append(rl / rh)
            if sum(t[4] for t in lst) / len(lst) < 0.15: ratiosRural.append(rl / rh)
        allGroups += 1
        nl = sum(t[1] - t[2] for t in lo); nh = sum(t[1] - t[2] for t in hi)
        if nl <= 0 or nh <= 0: continue
        Hlo = sum(t[2] for t in lo) / nl; Hhi = sum(t[2] for t in hi) / nh
        Hexp = GAIN * sum(starv(t[0]) for t in lo) / len(lo)
        Hlo_all.append(Hlo)
        if Hexp >= 0.02:
            deep += 1; ok1 = Hlo >= 0.5 * Hexp; ok2 = Hlo > Hhi
            deepOk += ok1; deepDir += ok2; deepBoth += (ok1 and ok2)
    for key, lst in ser.items():
        lst.sort(key=lambda r: (int(r["year"]), int(r["quarter"])))
        def rate(sl):
            d = sum(fl(r["deathsK"]) for r in sl); py = sum(fl(r["popMeanK"]) * fl(r["monthsCount"]) / 12.0 for r in sl)
            return d / py if py > 0 else float("nan")
        i = 0
        while i < len(lst):
            if fl(lst[i]["foodFill"]) < 0.6:
                j = i
                while j < len(lst) and fl(lst[j]["foodFill"]) < 0.6: j += 1
                if j - i >= 2 and i >= 4 and all(fl(r["foodFill"]) >= 0.9 for r in lst[i - 4:i]):
                    a, b2 = rate(lst[i:j]), rate(lst[i - 4:i])
                    if b2 > 0 and a == a: old_ratios.append(a / b2)
                i = j
            else: i += 1
if allGroups:
    print(f"# ③ G2-3：国×年の群 {allGroups}（Φσ̄={GAIN:.1f}）・飢えが深い群（Hexp_lo ≥ 0.02）{deep}")
    if deep >= 100:
        print(f"#    判定：①H_lo ≥ 0.5×Hexp_lo の群 {deepOk / deep * 100:.0f}%・②H_lo > H_hi の群 {deepDir / deep * 100:.0f}%・両方 {deepBoth / deep * 100:.0f}%（80% 以上で通る）")
    else:
        print("#    判定不能（飢えが深い群が 100 未満）")
    print(f"#    記録：観測の H_lo の中央値 {st.median(Hlo_all) if Hlo_all else float('nan'):.4f}・死の率の比（低充足 4 分の 1 ÷ 高充足 4 分の 1）の中央値 {st.median(ratios):.3f}（1 超の群 {sum(1 for x in ratios if x > 1) / len(ratios) * 100:.0f}%）／農村の国（都市の割合 15% 未満）{len(ratiosRural)} 群・中央値 {st.median(ratiosRural) if ratiosRural else float('nan'):.3f}")
    print(f"#    参考（旧の形）：充足 0.6 未満が 2 行以上続く州 {len(old_ratios)} 件" + (f"・中央値 {st.median(old_ratios):.2f}" if old_ratios else ""))
else:
    print("# ③ G2-3：群なし（mort_s が無い〔旗 0〕か、州が 8 以上の群がない）")
