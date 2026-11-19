# (1238) 読むだけ：q1_seedtab.tsv の列ごとに腕どうしの順位の比べ（Mann-Whitney の U と並べ替えの両側 p・20000 回・種 1238 の固定）
import sys, csv, random
rows = list(csv.reader(open(sys.argv[1], encoding="utf-8"), delimiter="\t")); hdr = rows[0]; rows = rows[1:]
def col(arm, j): return [float(r[j]) for r in rows if r[0] == arm]
def U(x, y): return sum((a > b) + 0.5 * (a == b) for a in x for b in y)
rng = random.Random(1238)
def p(x, y, n=20000):
    u0 = abs(U(x, y) - len(x) * len(y) / 2); z = x + y; k = 0
    for _ in range(n):
        rng.shuffle(z); k += abs(U(z[:len(x)], z[len(x):]) - len(x) * len(y) / 2) >= u0 - 1e-12
    return k / n
for j in range(2, len(hdr)):
    out = [hdr[j]]
    for a, b in (("nrB","n1"),("nrB","base"),("base","n1")):
        x, y = col(a, j), col(b, j); out.append(f"{a}÷{b} 中央 {sorted(x)[4]/2+sorted(x)[5]/2:.2f}/{sorted(y)[4]/2+sorted(y)[5]/2:.2f} U={U(x,y):.0f} p={p(x,y):.3f}")
    print("\t".join(out))
