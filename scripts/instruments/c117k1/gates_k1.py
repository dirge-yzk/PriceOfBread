# (1179) c117k1 門の台本：K1-F2 の 2 回目（包摂を分離の圧に 2 回掛けない・旗 SecessionInclusionOnce）。
# 元＝scripts/instruments/c116k1/gates_k1.py（(1173)）。数え方は同じ。変えたのは閾値の置き方だけ（★(1178)：閾値は〈同じ木の旗 0 の値〉に掛ける形）。
# 引数：腕の生 種の始め 種の終わり 出力 組の名 錨の生（同じ木の旗 0＝線の腕では keep_1178/base_ins） [法の表の置き場（省略は腕の生）] [錨の法の表の置き場（省略は錨の生）]
# 読むだけ・エンジンを呼ばない。比 R は事前登録 measurements/m1179_20261116_prereg_k1.txt に固定（m1178 の基線・N1・N2 の旗 0 で数えた）。
import sys, os, csv, math
from collections import defaultdict

RAW, S0, S1, OUT, TAG, ANCH = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5], sys.argv[6]
LAWRAW = sys.argv[7] if len(sys.argv) > 7 else RAW
ALAWRAW = sys.argv[8] if len(sys.argv) > 8 else ANCH
THR = 1.43
# 事前登録で固定した比（m1178：基線 2,489・N1 1,476・N2 2,358／G5 植民地格 100・12・76／本国の中核 29・35・22）
R_G1 = 2358.0 / 2489.0          # N1・N2 の最大÷基線
R_G5MIN = {"Colonial(k>0)": 12.0 / 100.0, "Core": 22.0 / 29.0}   # N1・N2 の最小÷基線（0 へ寄らない＝(1173) の〈最小の半分〉の形）
R_G5MAX = {"Colonial(k>0)": 76.0 / 100.0, "Core": 35.0 / 29.0}   # 記録：指示の字のままの形（最大÷基線×1.2）
G3B_MAX = 2
REL = 1e-8
UK = "イギリス"

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

def count(raw, lawraw, s0, s1, full):
    cnt = defaultdict(int); g5 = defaultdict(int); hit = defaultdict(set); g1nat = defaultdict(int)
    g2bad = []; seeds = 0; hasNew = None
    for seed in range(s0, s1 + 1):
        sec = os.path.join(raw, f"secession_s{seed}.tsv")
        if not os.path.exists(sec): continue
        seeds += 1
        law = {}; prev = {}; changed = set()
        for ix, x in rd(os.path.join(lawraw, f"sf_s{seed}.tsv")):
            q = int(x[ix["quarter"]]); n = x[ix["nation"]]
            pair = (int(float(x[ix["colonialSuffrageLaw"]])), int(float(x[ix["migrantNativeSuffrageLaw"]])))
            law[(q, n)] = pair
            if n in prev and prev[n] != pair: changed.add((q, n))
            prev[n] = pair
        suf = {}
        for ix, x in rd(os.path.join(lawraw, f"monlaw_s{seed}.tsv")):
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
                if nat != UK: cnt["記録 G1 英を除く"] += neg
                if hs >= 0.01: cnt["記録 G1 holdShare≥0.01"] += neg
                if neg: g1nat[nat] += 1
            if tier == "Core":
                cnt["記録 本国の中核 余り<0"] += neg
                cnt["記録 本国の中核 圧>閾値"] += p > THR
                if hs >= 0.01: cnt["G3c(記録) 本国の中核×holdShare≥0.01 余り<0"] += neg
                if fnum(x[ix["dominanceShare"]]) < 0.9: cnt["記録 本国の中核 支配度<0.9 の行"] += 1
            if tier == "Integrated": cnt["記録 編入 余り<0"] += neg; cnt["記録 編入 圧>閾値"] += p > THR
            if tier != "Colonial" and k <= 0.9: cnt["記録 非植民地×中核≤0.9 余り<0"] += neg
            if suf.get((q, nat), -1) >= 2 and k <= 0.5: g5[grp] += neg
            if x[ix["goFlag"]] == "1": hit[grp].add((seed, nat, reg))
            if full and hasNew:
                once = fnum(x[ix["pressureRuleOnceIndex"]]); old = fnum(x[ix["pressureRuleOldIndex"]])
                mb = fnum(x[ix["milNationMeanIndex"]]); mr = fnum(x[ix["milNationRebuiltOnceIndex"]])
                if rel(p, once) > REL or rel(mb, mr) > REL:
                    if (q, nat) in changed: cnt["G2 法の変わった四半期の外れ（許す）"] += 1
                    else:
                        cnt["G2 外れの行"] += 1
                        if len(g2bad) < 8: g2bad.append(f"s{seed} q{q} {nat}/{reg} {tier} 圧 {p:.10g} 1回 {once:.10g} 民族 {mb:.10g} 組み直し {mr:.10g}")
                mg = law.get((q, nat), (0, 0))[1]
                inclOld = 1.0 if mg == 1 else min(max(k, 0.0), 1.0)
                lim = old / (1.0 - inclOld) if 1.0 - inclOld > 1e-12 else once
                if p > lim * (1 + REL) + 1e-15: cnt["記録 G3a（許し 1e-8）外れの行"] += 1
                bnd = REL + 1e-9 / max(1.0 - inclOld, 1e-12) if 1.0 - inclOld > 1e-12 else REL
                if p > lim * (1 + bnd) + 1e-15: cnt["記録 G3a（(1173) 追記 2 の許し）外れの行"] += 1
                if once > old * (1 + 1e-6) + 1e-12: cnt["記録 旧と1回の規則が違う行"] += 1
    return cnt, g5, hit, g1nat, g2bad, seeds, hasNew

cnt, g5, hit, g1nat, g2bad, seeds, hasNew = count(RAW, LAWRAW, S0, S1, True)
acnt, ag5, ahit, _, _, aseeds, _ = count(ANCH, ALAWRAW, S0, S1, False)
aG1 = acnt["G1 植民地格(中核>0) 余り<0"]
G1_MIN = math.ceil(aG1 * R_G1 * 1.2 - 1e-9)
G5_MIN = {g: math.ceil(ag5[g] * R_G5MIN[g] * 0.5 - 1e-9) for g in R_G5MIN}
G5_LIT = {g: math.ceil(ag5[g] * R_G5MAX[g] * 1.2 - 1e-9) for g in R_G5MAX}
G1 = cnt["G1 植民地格(中核>0) 余り<0"]
G3b = len(hit["Core"]) + len(hit["Integrated"])
res = [("G1 働いた（下）", G1, f"≧{G1_MIN}", G1 >= G1_MIN)]
if hasNew: res.append(("G2 形の一致（両）外れの行", cnt["G2 外れの行"], "＝0", cnt["G2 外れの行"] == 0))
else: res.append(("G2 形の一致（両）", "列なし", "＝0", None))
res.append(("G3b 上 本国の中核と編入で支配度が 0 に届いた州", G3b, f"≦{G3B_MAX}", G3b <= G3B_MAX))
for g, m in G5_MIN.items(): res.append((f"G5 0 へ寄らない（下）成人男子以上×中核≤0.5×{g} 余り<0", g5[g], f"≧{m}", g5[g] >= m))
with open(OUT, "w", encoding="utf-8", newline="\n") as o:
    o.write(f"# (1179) c117k1 門 組={TAG} 生={RAW} 種={S0}〜{S1}（読めた種 {seeds}）末尾 5 列={'あり' if hasNew else 'なし'} 錨={ANCH}（読めた種 {aseeds}）\n")
    o.write(f"# 錨（同じ木の旗 0）：G1 {aG1}・G5 植民地格 {ag5['Colonial(k>0)']}・本国の中核 {ag5['Core']}\n")
    for name, v, line, ok in res:
        o.write(f"{name}\t{v}\t{line}\t{'緑' if ok else ('赤' if ok is False else '判定不能')}\n")
    o.write("# 記録\n")
    for g, m in G5_LIT.items(): o.write(f"記録 G5 の字のままの形（錨×最大÷基線×1.2）{g}\t{g5[g]}\t≧{m}\t{'通る' if g5[g] >= m else '通らない'}\n")
    for k2 in sorted(cnt): o.write(f"{k2}\t{cnt[k2]}\n")
    for g in sorted(g5): o.write(f"記録 G5 の群 {g}\t{g5[g]}\n")
    for g in sorted(hit): o.write(f"記録 支配度が 0 に届いた州（goFlag）{g}\t{len(hit[g])}\n")
    for n, v in sorted(g1nat.items(), key=lambda t: -t[1])[:8]: o.write(f"記録 G1 の国別 {n}\t{v}\n")
    for s in g2bad: o.write("G2 の外れの例\t" + s + "\n")
    allok = all(ok for _, _, _, ok in res if ok is not None)
    o.write(f"総合\t{'緑' if allok else '赤'}\n")
print(open(OUT, encoding="utf-8").read())
