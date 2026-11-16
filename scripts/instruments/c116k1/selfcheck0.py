# (1173) 計器の自己検査（旗 0 の生）：本体の圧 pressureIndex と計器の旧の規則 pressureRuleOldIndex の相対の差 ＞1e-8 の行を数える（ident の旗 0 の生で）。
import sys, os, csv
RAW, S0, S1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
n = bad = 0; worst = 0.0
for s in range(S0, S1 + 1):
    with open(os.path.join(RAW, f"secession_s{s}.tsv"), encoding="utf-8") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); a = h.index("pressureIndex"); b = h.index("pressureRuleOldIndex")
        for x in r:
            p, o = float(x[a]), float(x[b]); n += 1
            d = 0.0 if p == o else abs(p - o) / max(abs(o), 1e-12)
            worst = max(worst, d); bad += d > 1e-8
print(f"行 {n}・本体の圧と旧の規則の相対の差 >1e-8 の行 {bad}・最大 {worst:.3g}")
