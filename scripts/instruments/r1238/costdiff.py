# (1238) 読むだけ：2 つの .cost の計数の行（名前・件数・単価・推定）を名前で突き合わせ、推定の差の大きい順に並べる。引数：基 腕
import sys, re
def rd(p):
    d = {}; tot = None
    for ln in open(p, encoding="utf-8"):
        m = re.match(r"^  (.+?)\t([0-9.]+)\t([0-9.]+)\t([0-9.]+)\s*$", ln)
        if m: d[m.group(1)] = (float(m.group(2)), float(m.group(3)), float(m.group(4)))
        m2 = re.match(r"^推定の計算量（1試合）: ([0-9.]+)", ln)
        if m2: tot = float(m2.group(1))
    return d, tot
a, ta = rd(sys.argv[1]); b, tb = rd(sys.argv[2])
print(f"推定 基 {ta}・腕 {tb}・差 {tb - ta:+.1f}")
rows = []
for k in set(a) | set(b):
    x = a.get(k, (0, 0, 0)); y = b.get(k, (0, 0, 0))
    rows.append((y[2] - x[2], k[:60], x[0], y[0], x[1], y[1], x[2], y[2]))
rows.sort(key=lambda r: -abs(r[0]))
for r in rows[:14]:
    print(f"{r[0]:+7.2f}\t{r[1]}\t件 {r[2]:.0f}→{r[3]:.0f}（×{(r[3]/r[2] if r[2] else float('nan')):.3f}）\t単価 {r[4]}→{r[5]}\t秒 {r[6]:.2f}→{r[7]:.2f}")
