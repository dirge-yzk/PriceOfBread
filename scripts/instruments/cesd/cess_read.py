# ces1 (717)：割譲の表 cess_s・講和の表 cessp_s の読み手（読むだけ・シミュは回さない）
# 使い方：python -X utf8 -I cess_read.py <出力 tsv> <ins のフォルダ>...（各フォルダは cess_s{n}.tsv・cessp_s{n}.tsv を持つ。種の番号は自動検出）
# 設計書 docs/202610/20261031/20261031_割譲の候補と費用_設計.md §3-1 の限界 (a)〜(g) と §3-2 の静的な見込みを、エンジンの実際の値（cess_s）で数え直す。
# 出力：key／value の長い表（dir・seed・key・value）＝種ごと＋seed=ALL（和）。標準出力へは seed=ALL の行だけ出す。
# 群（group）：r1＝経路1（ColonyList）／r2＝経路2（植民地格の州の複数割譲）／r3h・r4h＝経路3・4で本国格（Core・Integrated）／r3c・r4c＝経路3・4で植民地格。home＝r3h＋r4h。
import sys, csv, glob, os, re, math, collections

OUT = sys.argv[1]; DIRS = sys.argv[2:]


def f(x):
    try: return float(x)
    except Exception: return float("nan")


def group(r):
    route = r["route"]
    if route == "1": return "r1"
    if route == "2": return "r2"
    return ("r%sh" if r["tier"] in ("Core", "Integrated") else "r%sc") % route


def load(d, s):
    with open(os.path.join(d, f"cess_s{s}.tsv"), "rb") as fh:
        lines = fh.read().decode("utf-8").splitlines()
    h = lines[0].split("\t")
    return [dict(zip(h, l.split("\t"))) for l in lines[1:] if l]


def loadp(d, s):
    with open(os.path.join(d, f"cessp_s{s}.tsv"), "rb") as fh:
        lines = fh.read().decode("utf-8").splitlines()
    h = lines[0].split("\t")
    return [dict(zip(h, l.split("\t"))) for l in lines[1:] if l]


def ratio_bucket(x):
    return "lt0.1" if x < 0.1 else "0.1-0.3" if x < 0.3 else "0.3-0.5" if x < 0.5 else "ge0.5"


def count_seed(rows, peaces):
    M = collections.Counter()
    S = collections.defaultdict(float)   # 平均用の和
    M["peaces"] = len(peaces)
    M["peaces_loserAbstract"] = sum(1 for p in peaces if p["loserIsNationFlag"] == "0")
    M["peaces_withR12"] = sum(1 for p in peaces if int(p["n1Count"]) + int(p["n2Count"]) > 0)
    M["peaces_withR34"] = sum(1 for p in peaces if int(p["n3Count"]) + int(p["n4Count"]) > 0)
    M["peaces_withBoth"] = sum(1 for p in peaces if int(p["n1Count"]) + int(p["n2Count"]) > 0 and int(p["n3Count"]) + int(p["n4Count"]) > 0)
    for r in rows:
        g = group(r)
        gs = [g]
        if g in ("r3h", "r4h"): gs.append("home")
        if g in ("r3c", "r4c"): gs.append("cede_col")
        gs.append("all")
        adj = f(r["landAdjFlag"]); port = f(r["portFlag"]); can = r["canCrossFlag"] == "1"
        same = f(r["sameLandmassFlag"]); sea = f(r["seaKm"]); gcKm = f(r["gcKm"]); share = f(r["worldNavShare"]); rel = f(r["relSeaControlShare"])
        e2 = f(r["e2OkFlag"]); alt = int(r["e2AltCount"])
        if r["route"] == "1" and not math.isnan(adj): e2 = 1.0 if (adj == 1 or can) else 0.0   # ①は港を問わない（eead4ddc の cess_s は港の規則のままだった＝読み手で取り直す）
        budA = f(r["budgetAfterIndex"]); c2 = f(r["costE2ShadowIndex"]); ch = f(r["costHcdShadowIndex"]); ce = f(r["costEngineIndex"])
        far = (not math.isnan(sea)) and sea >= 3000
        altAff = int(r["e2AltAffordCount"])
        farweak = far and share < 0.5
        for k in gs:
            M[f"n|{k}"] += 1
            if r["loserIsNationFlag"] == "0": M[f"loserAbstract|{k}"] += 1
            if same == 1: M[f"sameLandmass|{k}"] += 1
            if same == 1 and adj == 0: M[f"sameLandmass_notAdj|{k}"] += 1
            if same == 0: M[f"seaCrossing|{k}"] += 1
            if same == 0 and not can: M[f"seaCrossing_notCanCross|{k}"] += 1     # 限界 (a)：海越えなのに上陸の門を通らない
            if adj == 1: M[f"adj|{k}"] += 1
            if adj == 0: M[f"notAdj|{k}"] += 1
            if adj == 0 and port == 1: M[f"notAdj_port|{k}"] += 1
            if adj == 0 and port == 0: M[f"notAdj_inland|{k}"] += 1                # 案 E2 でどの経路でも取れない
            if adj == 0 and math.isnan(port): M[f"notAdj_portUnknown|{k}"] += 1
            if r["sameNodeFlag"] == "1": M[f"sameNode|{k}"] += 1
            if math.isnan(adj): M[f"noCoord|{k}"] += 1
            if e2 == 0: M[f"e2Drop|{k}"] += 1
            if e2 == 0 and alt > 0: M[f"e2Drop_reselect|{k}"] += 1
            if e2 == 0 and alt == 0: M[f"e2Drop_none|{k}"] += 1
            if e2 == 1: M[f"e2Ok|{k}"] += 1
            if far: M[f"far3000|{k}"] += 1
            if same == 1 and adj == 0 and port == 1: M[f"sameLandmass_notAdj_port|{k}"] += 1
            if same == 1 and adj == 0 and port == 0: M[f"sameLandmass_notAdj_inland|{k}"] += 1
            if farweak and same == 0: M[f"farweak_seaCrossing|{k}"] += 1
            if farweak and same == 0 and e2 == 0: M[f"farweak_seaCrossing_e2Drop|{k}"] += 1
            # 静的な見込み（落ちた州の代わりの選び直しを数える）：E2 の規則だけ／E2＋予算 1 本（③④は①②の残りから払う）
            if r["route"] in ("3", "4"):
                keepE2 = (e2 == 1) or alt > 0
                afford = (not math.isnan(c2)) and c2 <= budA
                keepBud = (e2 == 1 and afford) or altAff > 0
                if keepE2: M[f"keepE2|{k}"] += 1
                if keepBud: M[f"keepE2Budget|{k}"] += 1
                if e2 == 1 and not afford: M[f"e2Ok_butUnaffordable|{k}"] += 1
            if farweak:
                M[f"farweak|{k}"] += 1
                if e2 == 0: M[f"farweak_e2Drop|{k}"] += 1
            if far and same == 0: M[f"far3000_sea_ratio_{ratio_bucket(share)}|{k}"] += 1
            if r["colonialReachOnlyFlag"] == "1": M[f"viaColonialReach|{k}"] += 1
            if not math.isnan(budA) and not math.isnan(c2) and r["route"] in ("3", "4"):
                if c2 > budA: M[f"shadowCostOverBudgetLeft|{k}"] += 1
            if r["route"] in ("3", "4"):
                M[f"budgetLeftSum|{k}"] += budA; M[f"costE2Sum|{k}"] += c2
            S[f"resourceCount|{k}"] += f(r["resourceCount"]); S[f"popUnits|{k}"] += f(r["popUnitsIndex"])
            if not math.isnan(sea) and not math.isnan(gcKm) and gcKm > 0 and same == 0:
                S[f"seaOverGc|{k}"] += sea / gcKm; M[f"seaOverGc_n|{k}"] += 1
            if not math.isnan(f(r["landKm"])) and not math.isinf(f(r["landKm"])) and gcKm > 0 and same == 1:
                S[f"landOverGc|{k}"] += f(r["landKm"]) / gcKm; M[f"landOverGc_n|{k}"] += 1
            # e2 の費用が hcd と違う（陸の隣りで距離の項が消える）件数
            if not math.isnan(c2) and not math.isnan(ch) and abs(c2 - ch) > 1e-9: M[f"e2CostLowerThanHcd|{k}"] += 1
            if not math.isnan(ce) and not math.isnan(ch) and abs(ce - ch) > 1e-9 and r["route"] in ("1", "2"): M[f"engineCostNotHcd|{k}"] += 1
    for k, v in S.items(): M["mean_" + k] = v / max(1, M["n|" + k.split("|")[1]])
    for k in list(M):
        if k.startswith("mean_seaOverGc") : M[k] = S[k[5:]] / max(1, M["seaOverGc_n|" + k.split("|")[1]])
        if k.startswith("mean_landOverGc"): M[k] = S[k[5:]] / max(1, M["landOverGc_n|" + k.split("|")[1]])
    return M


out = open(OUT, "w", encoding="utf-8", newline="\n")
print("dir\tseed\tkey\tvalue", file=out)
GROUPS = ["r1", "r2", "r3h", "r4h", "r3c", "r4c", "home", "cede_col", "all"]
for d in DIRS:
    dd = d.rstrip("/\\")
    key = os.path.basename(os.path.dirname(dd)) + "/" + os.path.basename(dd)
    seeds = sorted(int(re.search(r"cess_s(\d+)\.tsv", x).group(1)) for x in glob.glob(os.path.join(d, "cess_s*.tsv")))
    allrows, allp = [], []
    total = collections.Counter()
    for s in seeds:
        rows, peaces = load(d, s), loadp(d, s)
        M = count_seed(rows, peaces)
        for k, v in sorted(M.items()):
            print(f"{key}\t{s}\t{k}\t{v:.10g}", file=out)
            if not k.startswith("mean_"): total[k] += v
        allrows += rows; allp += peaces
    M = count_seed(allrows, allp)   # 平均の列は和から取り直す（種ごとの平均の平均にしない）
    for k, v in sorted(M.items()):
        print(f"{key}\tALL\t{k}\t{v:.10g}", file=out)
        if not k.startswith("mean_"): assert abs(v - total[k]) < 1e-6, (k, v, total[k])   # 種の和と全体の数えが一致
    # 標準出力：キー×群の表
    print(f"# {key}（種 {seeds[0]}..{seeds[-1]}・{len(seeds)} 種・行 {len(allrows)}）")
    print("key\t" + "\t".join(GROUPS))
    for k in sorted({x.split("|")[0] for x in M if "|" in x}):
        print(k + "\t" + "\t".join(("%.4g" % M[k + "|" + g]) if (k + "|" + g) in M else "-" for g in GROUPS))
    for k in sorted(x for x in M if "|" not in x): print(f"{k}\t{M[k]:.6g}")
out.close()
