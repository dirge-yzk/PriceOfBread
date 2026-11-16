# (1173) G3a の外れの帰属：列 coreShareMeanShare（G10＝有効 10 桁）の丸めが 1÷(1−中核) で拡がる量と比べる（読むだけ）。
# 引数：生のフォルダ 種の始め 種の終わり。行ごとに rel＝本体の圧÷上限−1 と、丸めの上限 b＝1e-8＋1e-9÷(1−k)（k の丸め 5e-11 と old・本体の列の丸め 5e-10×2 を広めに取った）を比べる。
import sys, os, csv
RAW, S0, S1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
n = v = vb = 0; worstRatio = 0.0; worstRel = 0.0; once_ok = 0
for s in range(S0, S1 + 1):
    with open(os.path.join(RAW, f"secession_s{s}.tsv"), encoding="utf-8") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            p = float(x[ix["pressureIndex"]]); old = float(x[ix["pressureRuleOldIndex"]]); once = float(x[ix["pressureRuleOnceIndex"]]); k = min(max(float(x[ix["coreShareMeanShare"]]), 0.0), 1.0)
            n += 1
            lim = old / (1 - k) if 1 - k > 1e-12 else once
            if p > lim * (1 + 1e-8) + 1e-15:
                v += 1
                rel = p / lim - 1; b = 1e-8 + 1e-9 / max(1 - k, 1e-12)
                worstRel = max(worstRel, rel); worstRatio = max(worstRatio, rel / b)
                if rel > b: vb += 1
                if p <= once * (1 + 1e-8) + 1e-15: once_ok += 1
print(f"行 {n}・G3a の外れ {v}・そのうち丸めの上限を超える行 {vb}・外れの rel の最大 {worstRel:.3g}・rel÷丸めの上限の最大 {worstRatio:.3g}・外れの行のうち本体≦1 回の規則×(1+1e-8) {once_ok}")
