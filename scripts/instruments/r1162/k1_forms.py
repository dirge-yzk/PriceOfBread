# (1199) 注：(1193) で欄（国民意識）が消えた・旧の生でだけ動く（中身は変えていない）
# (1162) c114k1 読むだけ：K1（分離の圧の参政権の割引）の形を紙で当てる。腕 0・1 次の近似（力学の連鎖は数えない）。
# 引数：生のフォルダ 種の始め 種の終わり 出力 組の名
# 読む表：secession_s（州の行・CheckSecession の瞬間）・sf_s（国の colonialSuffrageLaw・migrantNativeSuffrageLaw）・monlaw_s（Suffrage）
# 形（AI の案・確証なし）
#   F0：今の式（エンジンの pressureIndex そのまま）
#   F1：★(1157)④＝州の版を人の版へ揃える（州の包摂＝非兵士の POP の大きさで重みづけた InterestGroups.SuffrageInclusion の平均）
#       Core/Integrated：今と同じ（Migrant Granted→1・他は中核の割合）。Colonial：ColonialSuffrage None→0・OwnNationals→中核の割合・Full→1
#       F1p＝圧の割引 :893 だけを差し替えた 1 次の近似。F1m＝:165（少数派の闘争度）も同じ関数で差し替えた分を闘争度の目標の差で上から押さえた物（上限の見込み）
#   F2：包摂を 1 回だけ掛ける＝:893 の圧の割引を消し、:165 の闘争度の側（F1 の関数）だけに残す。F2p＝:893 を外しただけ（Colonial は :165 の差も F2m で足す）
# 限界（台本が自分で申告する）：①共有分母（restive）は今の行のまま ②闘争度の差は目標の差（Toward の追いつきと 0〜1 の切りは数えない＝上限）
#   ③(1−中核) の POP の平均は州の平均で置く ④分離が起きた後の帰結は入らない（最初に 0 に届く州だけ） ⑤年末だけの国は 4 四半期を同じ margin で積む
import sys, os, csv, statistics as st
from collections import defaultdict

RAW, S0, S1, OUT, TAG = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5]
THR, SPD, MM = 1.43, 0.025, 0.2
FORMS = ("F0", "F1p", "F1m", "F2p", "F2m", "F0G", "F1G", "F2G", "M3", "M4", "M5")
# M5＝突然変異〈:893 の割引を逆向き 1÷(1−包摂) で掛ける〉（上の門が拾う誤り）
# M3＝突然変異〈包摂をどこにも掛けない〉（:165 も :893 も外す）。M4＝突然変異〈参政権の段（Suffrage≧成人男子）で包摂 1〉を :165 と :893 の両方に（台帳の SecessionSuffrageFromLaw の形）
# G＝反実仮想〈全ての国が MigrantNativeSuffrage＝Granted〉（1907 年の問い）。F0G＝今の州の版で包摂 1＝圧 0（全ての格）。
#   F1G＝Colonial は ColonialSuffrage のまま・他は包摂 1＝圧 0。F2G＝:893 を外し、闘争度の少数派の項だけが包摂で変わる（Colonial は F2m と同じ）

def fnum(s):
    try: return float(s)
    except ValueError: return float("nan")

def rd(path):
    with open(path, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r: yield ix, x

def med(v):
    v = [a for a in v if a == a]
    return st.median(v) if v else float("nan")

agg = defaultdict(lambda: defaultdict(int))     # (tier群, 量) → 件数
vals = defaultdict(lambda: defaultdict(list))   # (群) → 量 → 値の列
hits = defaultdict(lambda: defaultdict(int))    # 形 → tier群 → 0 に届いた州の数
hitNat = defaultdict(lambda: defaultdict(int))
check = defaultdict(int)
seedsRead = 0
for seed in range(S0, S1 + 1):
    sec = os.path.join(RAW, f"secession_s{seed}.tsv")
    if not os.path.exists(sec): continue
    seedsRead += 1
    law = {}
    for ix, x in rd(os.path.join(RAW, f"sf_s{seed}.tsv")):
        law[(int(x[ix["quarter"]]), x[ix["nation"]])] = (int(float(x[ix["colonialSuffrageLaw"]])), int(float(x[ix["migrantNativeSuffrageLaw"]])))
    suf = {}
    for ix, x in rd(os.path.join(RAW, f"monlaw_s{seed}.tsv")):
        suf[(int(x[ix["quarter"]]), x[ix["nation"]])] = int(float(x[ix["Suffrage"]]))
    series = defaultdict(list)
    for ix, x in rd(sec):
        q = int(x[ix["quarter"]]); nat = x[ix["nation"]]; reg = x[ix["region"]]; tier = x[ix["tier"]]
        k = fnum(x[ix["coreShareMeanShare"]]); I0 = fnum(x[ix["suffrageInclusionShare"]]); sf0 = fnum(x[ix["suffrageFactorShare"]])
        p = fnum(x[ix["pressureIndex"]]); hs = fnum(x[ix["holdShareIndex"]]); mil = fnum(x[ix["militancyMeanIndex"]])
        dom = fnum(x[ix["dominanceShare"]]); go = x[ix["goFlag"]]; gap = fnum(x[ix["pressureRebuildGapIndex"]])
        cs, mg = law.get((q, nat), (None, None))
        s2pre = suf.get((q, nat), -1)
        if cs is None: check["法の行が無い"] += 1; continue
        # 今の州の版の再現（エンジンの列と突き合わせる）
        I0r = 1.0 if mg == 1 else k
        check["行"] += 1
        if abs(min(max(I0r, 0), 1) - I0) > 1e-9: check["今の包摂の再現の不一致"] += 1
        if abs(gap) > 1e-9: check["圧の組み直しの差>1e-9"] += 1
        # 人の版（揃えた形）
        if tier == "Colonial":
            I1 = 0.0 if cs == 0 else (k if cs == 1 else 1.0)
        else:
            I1 = 1.0 if mg == 1 else k
        I1 = min(max(I1, 0.0), 1.0)
        sf1 = 1.0 - I1
        # :893 の前の圧を列の項から組む（関税の項＋民族の項）×意識の割引×大衆の飽和＝割引が 0 の行（中核 1）も組める
        base = (fnum(x[ix["tariffTermIndex"]]) + fnum(x[ix["ethnicTermIndex"]])) * fnum(x[ix["consciousnessFactorShare"]]) * fnum(x[ix["crowdFactorShare"]])
        if not (sf0 > 1e-12):
            check["割引が 0 の行（中核 1・項から組んだ）"] += 1
        else:
            check["項から組んだ圧×割引と本体の圧の相対の差>1e-9"] += 1 if abs(base * sf0 - p) > 1e-9 * max(1e-12, abs(p)) + 1e-15 else 0
        # 闘争度の差（:165 の少数派の項・目標の差の上限）
        dmil = MM * (1.0 - k) * (I0 - I1)
        mfac = (mil + dmil) / mil if mil > 1e-12 else 1.0
        if not (mfac >= 0): mfac = 0.0
        pf = {"F0": p, "F1p": base * sf1, "F1m": base * sf1 * mfac, "F2p": base, "F2m": base * mfac}
        mfacG = (mil + MM * (1.0 - k) * (I0 - 1.0)) / mil if mil > 1e-12 else 1.0
        if not (mfacG >= 0): mfacG = 0.0
        pf["F0G"] = 0.0
        pf["F1G"] = pf["F1m"] if tier == "Colonial" else 0.0
        pf["F2G"] = pf["F2m"] if tier == "Colonial" else base * mfacG
        m3 = (mil + MM * (1.0 - k) * I0) / mil if mil > 1e-12 else 1.0
        pf["M3"] = base * (m3 if m3 >= 0 else 0.0)
        I4 = 1.0 if s2pre >= 2 else I0
        m4 = (mil + MM * (1.0 - k) * (I0 - I4)) / mil if mil > 1e-12 else 1.0
        pf["M4"] = base * (1.0 - I4) * (m4 if m4 >= 0 else 0.0)
        pf["M5"] = base / sf0 if sf0 > 1e-12 else float("nan")
        if tier == "Colonial": grp = "Colonial(k>0)" if k > 1e-9 else "Colonial(k=0)"
        else: grp = tier
        s2 = suf.get((q, nat), -1)
        groups = [grp, "all"]
        if s2 >= 2 and k <= 0.5: groups.append("成人男子以上の国×中核≤0.5")
        if tier != "Colonial" and k <= 0.9: groups.append("非植民地×中核≤0.9")
        for gg in groups:
            a = agg[gg]; a["行"] += 1
            for f in FORMS:
                v = pf[f]
                if v != v: a[f + ":組めない"] += 1; continue
                if v > THR: a[f + ":圧>閾値"] += 1
                if hs - v < 0: a[f + ":余り<0"] += 1
            if p > 1e-12 and base == base:
                vals[gg]["F1m/F0"].append(pf["F1m"] / p); vals[gg]["F2m/F0"].append(pf["F2m"] / p); vals[gg]["F2G/F0"].append(pf["F2G"] / p)
            vals[gg]["F0"].append(p)
            for f in ("F1m", "F2m"):
                if pf[f] == pf[f]: vals[gg][f].append(pf[f])
        series[(nat, reg)].append((q, dom, hs, pf, grp, go))
    # 支配度の積分の予言（最初に 0 に届く州）
    for (nat, reg), ser in series.items():
        ser.sort(key=lambda z: z[0])
        D = {}; done = set(); prevq = None; prevdom = None
        realHit = False
        for (q, dom, hs, pf, grp, go) in ser:
            if go == "1" and not realHit: hits["実際の goFlag"][grp] += 1; realHit = True
            if prevq is None or q - prevq > 4:
                for f in FORMS: D[f] = dom
                prevq = q; prevdom = dom; continue
            dq = q - prevq; prevq = q
            if dq == 1 and prevdom is not None:
                dchk = prevdom + (hs - pf["F0"]) * SPD
                dchk = 0.0 if not (dchk > 0) else (1.0 if dchk > 1 else dchk)
                check["支配度の 1 四半期の積み直し：行"] += 1
                if abs(dchk - dom) > 1e-6: check["支配度の 1 四半期の積み直し：差>1e-6"] += 1
            elif dq > 1: check["支配度：飛びのある行（年末だけの国ほか）"] += 1
            prevdom = dom
            for f in FORMS:
                if f in done: continue
                v = pf[f]
                if v != v: v = pf["F0"]
                d = D[f] + (hs - v) * SPD * dq
                d = 0.0 if not (d > 0) else (1.0 if d > 1 else d)
                D[f] = d
                if d == 0.0:
                    done.add(f); hits[f][grp] += 1; hitNat[f][(grp, nat)] += 1

with open(OUT, "w", encoding="utf-8", newline="\n") as o:
    o.write(f"# (1162) K1 の形の紙の当て 組={TAG} 生={RAW} 種={S0}〜{S1}（読めた種 {seedsRead}）\n")
    o.write("# 検算：" + "・".join(f"{k}={v}" for k, v in sorted(check.items())) + "\n")
    o.write("# 群ごとの州×四半期の件数（圧>閾値 1.43・余り＝holdShare−圧<0）\n")
    for gg in sorted(agg):
        a = agg[gg]
        o.write(f"{gg}\t行={a['行']}\t" + "\t".join(f"{f}: 圧>閾値 {a[f + ':圧>閾値']} 余り<0 {a[f + ':余り<0']}" for f in FORMS) + "\n")
    o.write("# 群ごとの圧の中央（F0・F1m・F2m）と新÷旧の中央\n")
    for gg in sorted(vals):
        v = vals[gg]
        o.write(f"{gg}\tF0 中央={med(v['F0']):.4g}\tF1m 中央={med(v['F1m']):.4g}\tF2m 中央={med(v['F2m']):.4g}\tF1m/F0 中央={med(v['F1m/F0']):.4g}\tF2m/F0 中央={med(v['F2m/F0']):.4g}\tF2G/F0 中央={med(v['F2G/F0']):.4g}\n")
    o.write("# 支配度の積分の予言：最初に 0 に届く州の数（種の和）＝群ごと\n")
    keys = sorted({g for f in hits for g in hits[f]})
    for f in ("実際の goFlag",) + FORMS:
        o.write(f"{f}\t" + "\t".join(f"{g}={hits[f].get(g, 0)}" for g in keys) + f"\t計={sum(hits[f].values())}\n")
    for f in ("F1m", "F2m"):
        top = sorted(hitNat[f].items(), key=lambda z: -z[1])[:12]
        o.write(f"# {f} で 0 に届く州の多い (群, 国)：" + "・".join(f"{g}/{n} {c}" for (g, n), c in top) + "\n")
