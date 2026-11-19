# (1249) 段 0・読むだけ：nrB の生は消えた＝(1238) の政体の割合（measurements/m1238_20261119/out/q1_polity_*.txt＝英と世界の 1900-50 の四半期の割合）から、
#  英×世界の国 1 組・1 月あたりの IdeologyTension の期待値を独立の近似で出す（3 本を同じ近似で並べる）。
import ast, sys
IDEO = {0: 0.0, 1: 0.15, 2: 0.0, 3: 0.45, 4: 0.60, 5: 0.35, 6: 1.0, 7: 0.2, 8: 0.50}
for tag in ("base", "n1", "nrB"):
    L = open(f"measurements/m1238_20261119/out/q1_polity_{tag}.txt", encoding="utf-8").read().splitlines()
    uk = ast.literal_eval(L[0].split(" ", 1)[1]); wd = ast.literal_eval(L[1].split(" ", 1)[1])
    e = 0.0
    for a, pa in uk.items():
        for b, pb in wd.items():
            e += pa * pb * (-0.30 if a == b else 0.22 * abs(IDEO[int(a)] - IDEO[int(b)]))
    print(f"{tag}\t英×世界の 1 組・1 月の期待値 {e:+.4f}")
