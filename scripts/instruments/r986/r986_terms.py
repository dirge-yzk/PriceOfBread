# (1199) 注：(1193) で欄（国民意識）が消えた・旧の生でだけ動く（中身は変えていない）
# (986) c89ci 植民地の独立の圧力を項ごとに読む（読むだけ）。
# 使い方: python -X utf8 -I r986_terms.py <生のフォルダ> <種の最初> <種の最後> <出力のフォルダ> <組の名>
# 事前登録 D:/PoB_work/r986_work/prereg.txt のとおり。年末の行（quarter%4==3）・1800〜1830・保持中の行を主に集計。
import sys, os, statistics as st, collections as co

raw, s0, s1, out, tag = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5]
os.makedirs(out, exist_ok=True)

SA = "ヌエバ・エスパーニャ銀地帯 中央メキシコ キューバ ペルー ヌエバ・グラナダ ラ・プラタ プエルトリコ カラカス ルイジアナ・フロリダ チリ サンタフェ エルパソ ロレート レオン ボゴタ キト イキトス ラパス トゥクマン アスンシオン カルメン・デ・パタゴネス プンタアレナス チロエ".split()
BR = "ブラジル アマゾン マナウス サルヴァドール サンルイス ゴイアス クイアバ".split()
SD = "北部サン=ドマング 南部サン=ドマング".split()
GROUP = {}
for n in SA: GROUP[n] = ("SA", "スペイン")
for n in BR: GROUP[n] = ("BR", "ポルトガル")
for n in SD: GROUP[n] = ("SD", "フランス")
GN = ["SA", "BR", "SD"]
Y0, Y1 = 1800, 1830
HOLDPERPOP, THR, REFPOP, DSPEED = 0.000005, 1.43, 1000.0, 0.025   # Coefficients.cs の今の値

COLS = ["legitimacyIndex", "totalPopK", "coerceIndex", "holdBaseIndex", "holdBaseRebuildGapIndex", "stanceIndex", "sharedDivIndex", "restiveCount",
        "popK", "crowdK", "crowdFactorShare", "militancyMeanIndex", "cultureDistanceIndex", "coreShareMeanShare", "consciousnessMeanIndex", "suffrageInclusionShare",
        "tariffInterestIndex", "tariffGapIndex", "tariffTermIndex", "ethnicTermIndex", "consciousnessFactorShare", "suffrageFactorShare",
        "pressureIndex", "pressureRebuildGapIndex", "effHoldBeforeShareIndex", "holdShareIndex", "marginIndex", "dominanceShare", "goFlag", "capitalDistanceKm"]
DER = ["legitTerm", "coerceTerm", "holdSumGap", "proj", "raw", "rawAfterCons", "rawAfterSuff", "holdFloorNoLegit", "kLegit", "kCoerceOnly", "milNeeded", "milNeededGap2"]

# 数えた後に足した列（事前登録の外・advisor の指摘の c の直し＝Coercion＝BaseCoercion×Legitimacy×財政×戦争＝威信を下げると治安維持力の項も下がる）
#   kLegit＝威信（Legitimacy）に掛ける倍率で、引き留め＝圧力 になる値（両方の項が下がる）。kCoerceOnly＝威信を据え置き Coercion の他の因数（財政・戦争の消耗）だけに掛ける倍率（威信の項だけで圧力を超えるなら nan＝届かない）
def solve_k(legit, c, mult, pressure, both):
    def h(k): return (legit * (k if both else 1.0) + THR * c * k / (c * k + REFPOP)) * mult
    if not (pressure > 0): return float("nan")
    if h(0.0) >= pressure: return float("nan")
    if h(1.0) <= pressure: return 1.0
    lo, hi = 0.0, 1.0
    for _ in range(60):
        mid = (lo + hi) / 2
        if h(mid) > pressure: hi = mid
        else: lo = mid
    return (lo + hi) / 2

def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float("nan")

def f(x):
    try: return float(x)
    except: return float("nan")

per_seed = {}      # seed -> group -> dict col -> list
sums = {}          # seed -> group -> dict sumname -> value
other = co.Counter()   # (group, owner) -> rows（保持中でない行）
path = co.defaultdict(lambda: co.defaultdict(list))   # (year) -> list of (margin, dom) per seed median later
pathseed = {}      # seed -> year -> list margin / dom for SA held
need_q = co.defaultdict(list)
gapmax = co.defaultdict(float)
spain = {}         # seed -> year -> dict（国の列）

for s in range(s0, s1 + 1):
    p = os.path.join(raw, f"secession_s{s}.tsv")
    per_seed[s] = {g: co.defaultdict(list) for g in GN}
    sums[s] = {g: co.Counter() for g in GN}
    pathseed[s] = co.defaultdict(lambda: co.defaultdict(list))
    spain[s] = {}
    with open(p, encoding="utf-8") as fh:
        hdr = fh.readline().rstrip("\n").split("\t")
        ix = {h: i for i, h in enumerate(hdr)}
        for line in fh:
            a = line.rstrip("\n").split("\t")
            q = int(a[0]); y = int(a[1])
            if q % 4 != 3: continue
            nat = a[ix["nation"]]
            if nat == "スペイン" and 1800 <= y <= 1830 and y not in spain[s]:
                spain[s][y] = {k: f(a[ix[k]]) for k in ("legitimacyIndex", "totalPopK", "coerceIndex", "holdBaseIndex", "sharedDivIndex", "restiveCount")}
            reg = a[ix["region"]]
            if reg not in GROUP or not (Y0 <= y <= Y1): continue
            g, owner0 = GROUP[reg]
            if nat != owner0:
                other[(tag, s, g, nat, a[ix["tier"]])] += 1
                continue
            v = {k: f(a[ix[k]]) for k in COLS}
            legit = v["legitimacyIndex"] / (1 + v["totalPopK"] * HOLDPERPOP)
            c = v["coerceIndex"]
            cterm = THR * c / (c + REFPOP)
            proj = v["effHoldBeforeShareIndex"] / v["holdBaseIndex"] if v["holdBaseIndex"] > 0 else float("nan")
            raw_ = v["tariffTermIndex"] + v["ethnicTermIndex"]
            v.update(legitTerm=legit, coerceTerm=cterm, holdSumGap=abs(legit + cterm - v["holdBaseIndex"]), proj=proj, raw=raw_,
                     rawAfterCons=raw_ * v["consciousnessFactorShare"], rawAfterSuff=raw_ * v["consciousnessFactorShare"] * v["suffrageFactorShare"],
                     holdFloorNoLegit=cterm * proj / v["sharedDivIndex"],
                     kLegit=solve_k(legit, c, proj / v["sharedDivIndex"], v["pressureIndex"], True),
                     milNeeded=(v["militancyMeanIndex"] * v["holdShareIndex"] / v["pressureIndex"]) if v["pressureIndex"] > 0 else float("nan"),
                     milNeededGap2=v["holdShareIndex"] / ((2 * (1 + v["cultureDistanceIndex"]) + 3.0 * 0.2 * v["cultureDistanceIndex"] * (1 - v["coreShareMeanShare"]))
                                   * v["consciousnessFactorShare"] * v["suffrageFactorShare"] * v["crowdFactorShare"]) if v["crowdFactorShare"] > 0 else float("nan"),
                     kCoerceOnly=solve_k(legit, c, proj / v["sharedDivIndex"], v["pressureIndex"], False))
            for k, x in v.items(): per_seed[s][g][k].append(x)
            S = sums[s][g]
            S["n"] += 1; S["tariff"] += v["tariffTermIndex"]; S["ethnic"] += v["ethnicTermIndex"]; S["raw"] += raw_
            S["afterCons"] += v["rawAfterCons"]; S["afterSuff"] += v["rawAfterSuff"]; S["pressure"] += v["pressureIndex"]
            S["hold"] += v["holdShareIndex"]; S["legitTerm"] += legit; S["coerceTerm"] += cterm; S["holdBase"] += v["holdBaseIndex"]
            S["effBefore"] += v["effHoldBeforeShareIndex"]; S["floor"] += v["holdFloorNoLegit"]
            S["mil1"] += 1 if v["milNeeded"] == v["milNeeded"] and v["milNeeded"] <= 1 else 0
            S["kCnan"] += 0 if v["kCoerceOnly"] == v["kCoerceOnly"] else 1
            S["negMargin"] += 1 if v["marginIndex"] < 0 else 0
            gapmax[(g, "pressureRebuildGap")] = max(gapmax[(g, "pressureRebuildGap")], abs(v["pressureRebuildGapIndex"]) if v["pressureRebuildGapIndex"] == v["pressureRebuildGapIndex"] else 0)
            gapmax[(g, "holdBaseRebuildGap")] = max(gapmax[(g, "holdBaseRebuildGap")], abs(v["holdBaseRebuildGapIndex"]) if v["holdBaseRebuildGapIndex"] == v["holdBaseRebuildGapIndex"] else 0)
            gapmax[(g, "holdSumGap(台本の組み直し)")] = max(gapmax[(g, "holdSumGap(台本の組み直し)")], v["holdSumGap"])
            if g == "SA":
                pathseed[s][y]["margin"].append(v["marginIndex"]); pathseed[s][y]["dom"].append(v["dominanceShare"])
                pathseed[s][y]["pressure"].append(v["pressureIndex"]); pathseed[s][y]["hold"].append(v["holdShareIndex"])

def agg(vals):
    vals = [x for x in vals if x == x]
    return (st.median(vals), min(vals), max(vals)) if vals else (float("nan"),) * 3

o = []
o.append(f"# (986) 組 {tag} 種 {s0}〜{s1} 生 {raw}・年末の行・{Y0}〜{Y1}・保持中（今の持ち主＝開幕の持ち主）")
o.append("## 表1 列の中央（種ごとの中央→種の中央［最小・最大］）")
for g in GN:
    o.append(f"### 群 {g}  行数（州×年）の種の中央 {agg([sums[s][g]['n'] for s in sums])[0]}")
    for k in COLS + DER:
        vs = [med(per_seed[s][g][k]) for s in per_seed if per_seed[s][g][k]]
        m, lo, hi = agg(vs)
        o.append(f"{g}\t{k}\t{m:.4g}\t[{lo:.4g}, {hi:.4g}]\t種数 {len(vs)}")
o.append("## 表2 和÷和（種ごと→種の中央［最小・最大］）")
RAT = [("tariff/raw", "tariff", "raw"), ("ethnic/raw", "ethnic", "raw"), ("afterCons/raw（意識の割引の後）", "afterCons", "raw"),
       ("afterSuff/afterCons（選挙権の割引）", "afterSuff", "afterCons"), ("pressure/afterSuff（大衆の飽和）", "pressure", "afterSuff"),
       ("pressure/raw（割引の合計）", "pressure", "raw"), ("pressure/hold（圧力÷引き留め）", "pressure", "hold"),
       ("legitTerm/holdBase", "legitTerm", "holdBase"), ("coerceTerm/holdBase", "coerceTerm", "holdBase"),
       ("effBefore/holdBase（投射）", "effBefore", "holdBase"), ("hold/effBefore（共有の分母）", "hold", "effBefore"),
       ("floor/pressure（威信0の引き留め÷圧力）", "floor", "pressure"), ("negMargin/n（margin が負の行の割合）", "negMargin", "n"), ("kCnan/n（財政と戦争だけでは届かない行の割合・事前登録の外）", "kCnan", "n"), ("mil1/n（今の関税の乖離のまま闘争度 1 以下で届く行の割合・事前登録の外）", "mil1", "n")]
for g in GN:
    for name, a_, b_ in RAT:
        vs = [sums[s][g][a_] / sums[s][g][b_] for s in sums if sums[s][g][b_] > 0]
        m, lo, hi = agg(vs)
        o.append(f"{g}\t{name}\t{m:.4g}\t[{lo:.4g}, {hi:.4g}]\t種数 {len(vs)}")
o.append("## 表3 検算（絶対値の最大）")
for (g, k), x in sorted(gapmax.items()): o.append(f"{g}\t{k}\t{x:.3g}")
o.append("## 表4 群 SA の道のり（年ごとの州の中央→種の中央）・要る四半期数＝1÷(−margin×0.025)（負のときだけ）")
o.append("year\tmargin\tdominance\tpressure\thold\t要る四半期\t種数")
for y in range(Y0, Y1 + 1):
    ms = [med(pathseed[s][y]["margin"]) for s in pathseed if pathseed[s][y]["margin"]]
    ds = [med(pathseed[s][y]["dom"]) for s in pathseed if pathseed[s][y]["dom"]]
    ps = [med(pathseed[s][y]["pressure"]) for s in pathseed if pathseed[s][y]["pressure"]]
    hs = [med(pathseed[s][y]["hold"]) for s in pathseed if pathseed[s][y]["hold"]]
    if not ms: o.append(f"{y}\t-\t-\t-\t-\t-\t0"); continue
    mm = med(ms)
    nq = f"{1/(-mm*DSPEED):.0f}" if mm < 0 else "—"
    o.append(f"{y}\t{mm:.4g}\t{med(ds):.4g}\t{med(ps):.4g}\t{med(hs):.4g}\t{nq}\t{len(ms)}")
o.append("## 表5 保持中でない行（年末・1800〜1830）＝群・今の持ち主・格・行数（種の和）")
oc = co.Counter()
for (t, s, g, nat, tier), n in other.items(): oc[(g, nat, tier)] += n
for (g, nat, tier), n in sorted(oc.items(), key=lambda kv: -kv[1])[:40]: o.append(f"{g}\t{nat}\t{tier}\t{n}")
o.append("## 表6 スペインの国の列（年末・種の中央）")
o.append("year\tlegitimacy\ttotalPopK\tcoerce\tholdBase\tsharedDiv\trestive\t種数")
for y in range(1800, 1831):
    rows = [spain[s][y] for s in spain if y in spain[s]]
    if not rows: continue
    o.append(f"{y}\t" + "\t".join(f"{med([r[k] for r in rows]):.4g}" for k in ("legitimacyIndex", "totalPopK", "coerceIndex", "holdBaseIndex", "sharedDivIndex", "restiveCount")) + f"\t{len(rows)}")
with open(os.path.join(out, f"terms_{tag}.txt"), "w", encoding="utf-8", newline="\n") as fo: fo.write("\n".join(o) + "\n")
print("wrote", os.path.join(out, f"terms_{tag}.txt"), len(o))
