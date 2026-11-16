# (1173) c116k1 同じ盤面の新÷旧（記録・読むだけ）：secession_s の末尾 5 列で、行ごとに 1 回の規則の圧÷旧の規則の圧と、
# 民族の闘争度の項（人の版の包摂）÷旧の州の版で組んだ民族の項（MinorityMilitancy×(1−中核)×(1−旧の包摂) の近似＝州の中核の平均で置く）を群ごとに中央で出す。
# 引数：生のフォルダ 種の始め 種の終わり
import sys, os, csv, statistics as st
from collections import defaultdict
RAW, S0, S1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
pr = defaultdict(list); n = defaultdict(int); zeroOld = defaultdict(int)
for s in range(S0, S1 + 1):
    with open(os.path.join(RAW, f"secession_s{s}.tsv"), encoding="utf-8") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            tier = x[ix["tier"]]; k = float(x[ix["coreShareMeanShare"]])
            g = ("Colonial(k>0)" if k > 1e-9 else "Colonial(k=0)") if tier == "Colonial" else tier
            once = float(x[ix["pressureRuleOnceIndex"]]); old = float(x[ix["pressureRuleOldIndex"]])
            n[g] += 1
            if old > 1e-300: pr[g].append(once / old)
            elif once > 0: zeroOld[g] += 1
for g in sorted(n):
    v = pr[g]
    print(f"{g}\t行 {n[g]}\t圧の 1回÷旧 の中央 {st.median(v) if v else float('nan'):.4g}\t旧が 0 で 1回が正の行 {zeroOld[g]}")
