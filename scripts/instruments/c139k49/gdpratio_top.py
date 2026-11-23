# 使い方: python -X utf8 -I gdpratio_top.py <rawdir> [件数 8]
# (1331) c139k49 記録（読むだけ）：nation_s の nominalGdpPerYear の 1 四半期の比（前の行が正の時だけ）の上位を種・国つきで書く。
#  止める線 100 倍の〈それ以外〉の件が、基準とヌル腕で同じ型（小さな国の鋸の歯）としてどこまで近づいているかを並べる材料。採否には使わない。
import sys, os, glob
raw = sys.argv[1]; k = int(sys.argv[2]) if len(sys.argv) > 2 else 8
rows = []; over = {10: 0, 30: 0, 50: 0, 100: 0}
for p in sorted(glob.glob(f"{raw}/nation_s*.tsv")):
    s = os.path.basename(p)[8:-4]
    if not s.isdigit(): continue
    prev = {}
    with open(p, encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {c: i for i, c in enumerate(h)}
        for l in f:
            c = l.rstrip("\n").split("\t"); n = c[ix["nation"]]
            try: g = float(c[ix["nominalGdpPerYear"]])
            except ValueError: continue
            g0 = prev.get(n); prev[n] = g
            if g0 is None or not (g0 > 0) or not (g > 0): continue
            r = g / g0
            for t in over:
                if r >= t: over[t] += 1
            rows.append((r, int(s), n, c[ix["year"]], c[ix["quarter"]]))
rows.sort(reverse=True)
print(f"# gdpratio_top {raw}  比≧10 {over[10]}・≧30 {over[30]}・≧50 {over[50]}・≧100 {over[100]}（前の行が正・今の行が正の時だけ）")
for r in rows[:k]: print(f"{r[0]:.1f}\t種{r[1]}\t{r[2]}\t{r[3]}\tq{r[4]}")
