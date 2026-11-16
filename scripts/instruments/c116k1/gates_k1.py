# (1173) c116k1 門の台本：K1-F2（包摂を分離の圧に 2 回掛けない・旗 SecessionInclusionOnce）。
# 引数：計器の生のフォルダ（secession_s・sf_s・monlaw_s）種の始め 種の終わり 出力 組の名
# 読むだけ・エンジンを呼ばない。門の閾値は事前登録 measurements/m1173_20261116_prereg_k1.txt の固定の値（腕の前に m1171・N1・N2 の紙で数えた）。
# 末尾 5 列（inclusionPersonMeanShare・pressureRuleOldIndex・pressureRuleOnceIndex・milNationMeanIndex・milNationRebuiltOnceIndex）が無い生では G2・G3a は〈列なし〉。
import sys, os, csv
from collections import defaultdict

RAW, S0, S1, OUT, TAG = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5]
LAWRAW = sys.argv[6] if len(sys.argv) > 6 else RAW   # sf_s・monlaw_s の置き場（ident の刈り込みで消えた時は基線の生＝全行一致の表）
THR = 1.43
G1_MIN, G3B_MAX, G3C_REC = 3796, 2, 551
G5_MIN = {"Colonial(k>0)": 19, "Core": 7}
REL = 1e-8

def fnum(s):
    if s == "nan" or s == "": return float("nan")
    if s == "inf": return float("inf")
    if s == "-inf": return float("-inf")
    return float(s)

def rel(a, b):
    if a == b: return 0.0
    return abs(a - b) / max(abs(b), 1e-12)

def rd(path):
    with open(path, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r: yield ix, x

cnt = defaultdict(int)
g5 = defaultdict(int)
hitRegions = defaultdict(set)
g2bad = []; g3abad = []
seedsRead = 0
hasNew = None
for seed in range(S0, S1 + 1):
    sec = os.path.join(RAW, f"secession_s{seed}.tsv")
    if not os.path.exists(sec): continue
    seedsRead += 1
    law = {}; prev = {}; changed = set()
    for ix, x in rd(os.path.join(LAWRAW, f"sf_s{seed}.tsv")):
        q = int(x[ix["quarter"]]); n = x[ix["nation"]]
        pair = (int(float(x[ix["colonialSuffrageLaw"]])), int(float(x[ix["migrantNativeSuffrageLaw"]])))
        law[(q, n)] = pair
        if n in prev and prev[n] != pair: changed.add((q, n))
        prev[n] = pair
    suf = {}
    for ix, x in rd(os.path.join(LAWRAW, f"monlaw_s{seed}.tsv")):
        suf[(int(x[ix["quarter"]]), x[ix["nation"]])] = int(float(x[ix["Suffrage"]]))
    cnt["法の変わった国×四半期"] += len(changed)
    for ix, x in rd(sec):
        if hasNew is None: hasNew = "pressureRuleOnceIndex" in ix
        q = int(x[ix["quarter"]]); nat = x[ix["nation"]]; reg = x[ix["region"]]; tier = x[ix["tier"]]
        k = fnum(x[ix["coreShareMeanShare"]]); p = fnum(x[ix["pressureIndex"]]); hs = fnum(x[ix["holdShareIndex"]])
        cnt["行"] += 1
        if not (p == p) or p in (float("inf"), float("-inf")): cnt["圧が非有限"] += 1
        grp = ("Colonial(k>0)" if k > 1e-9 else "Colonial(k=0)") if tier == "Colonial" else tier
        neg = hs - p < 0
        if grp == "Colonial(k>0)":
            cnt["G1 植民地格(中核>0) 余り<0"] += neg
            cnt["G1b 植民地格(中核>0) 圧>閾値"] += p > THR
        if tier == "Core":
            cnt["記録 本国の中核 余り<0"] += neg
            cnt["記録 本国の中核 圧>閾値"] += p > THR
            if hs >= 0.01: cnt["G3c(記録) 本国の中核×holdShare≥0.01 余り<0"] += neg
        if tier == "Integrated": cnt["記録 編入 余り<0"] += neg; cnt["記録 編入 圧>閾値"] += p > THR
        if tier != "Colonial" and k <= 0.9: cnt["記録 非植民地×中核≤0.9 余り<0"] += neg
        if suf.get((q, nat), -1) >= 2 and k <= 0.5: g5[grp] += neg
        if x[ix["goFlag"]] == "1": hitRegions[grp].add((seed, nat, reg))
        if hasNew:
            once = fnum(x[ix["pressureRuleOnceIndex"]]); old = fnum(x[ix["pressureRuleOldIndex"]])
            mb = fnum(x[ix["milNationMeanIndex"]]); mr = fnum(x[ix["milNationRebuiltOnceIndex"]])
            r1 = rel(p, once); r2 = rel(mb, mr)
            if r1 > REL or r2 > REL:
                if (q, nat) in changed: cnt["G2 法の変わった四半期の外れ（許す）"] += 1
                else:
                    cnt["G2 外れの行"] += 1
                    if len(g2bad) < 8: g2bad.append(f"s{seed} q{q} {nat}/{reg} {tier} 圧 {p:.10g} 1回 {once:.10g} 民族 {mb:.10g} 組み直し {mr:.10g}")
            # G3a：本体の圧 ≦ 旧の規則の圧÷(1−旧の包摂)×(1＋許し)。旧の包摂＝付与なら 1・他は中核の平均（coreShareMeanShare）
            mg = law.get((q, nat), (0, 0))[1]
            inclOld = 1.0 if mg == 1 else min(max(k, 0.0), 1.0)
            if 1.0 - inclOld > 1e-12: lim = old / (1.0 - inclOld)
            else: lim = once
            if p > lim * (1 + REL) + 1e-15: cnt["記録 G3a（元の物差し・許し 1e-8）外れの行"] += 1
            # 追記 2（結果の後の変更・緩める側）：列 coreShareMeanShare は有効 10 桁＝1÷(1−k) で丸めが拡がる＝許しを 1e-8＋1e-9÷(1−k) に
            bnd = REL + 1e-9 / max(1.0 - inclOld, 1e-12) if 1.0 - inclOld > 1e-12 else REL
            if p > lim * (1 + bnd) + 1e-15:
                cnt["G3a 外れの行"] += 1
                if len(g3abad) < 8: g3abad.append(f"s{seed} q{q} {nat}/{reg} {tier} 圧 {p:.10g} 上限 {lim:.10g}")
            if once > old * (1 + 1e-6) + 1e-12: cnt["記録 旧と1回の規則が違う行"] += 1

G1 = cnt["G1 植民地格(中核>0) 余り<0"]
G3b = len(hitRegions["Core"]) + len(hitRegions["Integrated"])
res = []
res.append(("G1 働いた（下）", G1, f"≧{G1_MIN}", G1 >= G1_MIN))
if hasNew:
    res.append(("G2 形の一致（両）外れの行", cnt["G2 外れの行"], "＝0", cnt["G2 外れの行"] == 0))
    res.append(("G3a 上（行ごと）外れの行", cnt["G3a 外れの行"], "＝0", cnt["G3a 外れの行"] == 0))
else:
    res.append(("G2 形の一致（両）", "列なし", "＝0", None)); res.append(("G3a 上（行ごと）", "列なし", "＝0", None))
res.append(("G3b 上 本国の中核と編入で支配度が 0 に届いた州", G3b, f"≦{G3B_MAX}", G3b <= G3B_MAX))
for g, m in G5_MIN.items(): res.append((f"G5 0 へ寄らない（下）成人男子以上×中核≤0.5×{g} 余り<0", g5[g], f"≧{m}", g5[g] >= m))
with open(OUT, "w", encoding="utf-8", newline="\n") as o:
    o.write(f"# (1173) c116k1 門 組={TAG} 生={RAW} 種={S0}〜{S1}（読めた種 {seedsRead}）末尾 5 列={'あり' if hasNew else 'なし'}\n")
    for name, v, line, ok in res:
        o.write(f"{name}\t{v}\t{line}\t{'緑' if ok else ('赤' if ok is False else '判定不能')}\n")
    o.write("# 記録\n")
    for k2 in sorted(cnt): o.write(f"{k2}\t{cnt[k2]}\n")
    for g in sorted(g5): o.write(f"記録 G5 の群 {g}\t{g5[g]}\n")
    for g in sorted(hitRegions): o.write(f"記録 支配度が 0 に届いた州（goFlag）{g}\t{len(hitRegions[g])}\n")
    for s in g2bad: o.write("G2 の外れの例\t" + s + "\n")
    for s in g3abad: o.write("G3a の外れの例\t" + s + "\n")
    allok = all(ok for _, _, _, ok in res if ok is not None)
    o.write(f"総合\t{'緑' if allok else '赤'}\n")
print(open(OUT, encoding="utf-8").read())
