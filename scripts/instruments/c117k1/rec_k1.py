# (1179) c117k1 記録の読み（門にしない・読むだけ）：内戦の件数と戦った月の和（wars_s の civilWarFlag）・
#   本国の中核で中核の割合≧0.999 の州に関税の項の圧が立つ行（tier=Core・coreShareMeanShare≧0.999・tariffTermIndex＞0・pressureIndex＞1e-6）の数と 1861 年のアメリカの同じ行・圧の中央。
# 引数：生のフォルダ=名 ...（種 1〜10 か 11〜20 は有る物を読む）
import sys, os, csv
def rd(p):
    with open(p, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r: yield ix, x
for arg in sys.argv[1:]:
    d, tag = arg.split("=", 1) if "=" in arg else (arg, arg)
    cw = 0; cwm = 0.0; seeds = 0; coreT = 0; us61 = 0; us61p = 0; usp = []
    for s in range(1, 21):
        w = os.path.join(d, f"wars_s{s}.tsv")
        if os.path.exists(w):
            seeds += 1
            for ix, x in rd(w):
                if x[ix["civilWarFlag"]] in ("1", "True", "true"): cw += 1; cwm += float(x[ix["months"]])
        sp = os.path.join(d, f"secession_s{s}.tsv")
        if os.path.exists(sp):
            for ix, x in rd(sp):
                if x[ix["tier"]] != "Core": continue
                t = float(x[ix["tariffTermIndex"]]); p = float(x[ix["pressureIndex"]])
                hit = t > 0 and p > 1e-6
                if float(x[ix["coreShareMeanShare"]]) >= 0.999: coreT += hit
                if x[ix["nation"]] == "アメリカ" and x[ix["year"]] == "1861":
                    us61 += 1; us61p += hit; usp.append(p)
    med = sorted(usp)[len(usp) // 2] if usp else float("nan")
    print(f"{tag}\t種 {seeds}\t内戦 {cw} 件\t内戦の月の和 {cwm:.0f}\t中核≧0.999 の本国の中核で関税の項と圧＞1e-6 の行 {coreT}\t1861 年のアメリカの本国の中核の行 {us61}（うち関税の項と圧＞1e-6 {us61p}・圧の中央 {med:.4g}・最大 {max(usp) if usp else float('nan'):.4g}）")
