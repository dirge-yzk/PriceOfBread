# (1232) 紙：案丙に〈住民の押し返し〉＝外国の目標×(1−ρ_R)（ρ_R＝p÷(p＋h)・段 E の抵抗の比）を掛けた時の e* の度数。読むだけ。
# 使い方: python -X utf8 -I paper_p2.py <INSRAW> <種の下> <種の上> <出力>
# p＝secession_s の pressureIndex・h＝holdShareIndex（支配度の積分が読む 2 つ・(1190) M2-1 で 15,860 行一致を確かめた列）。
import sys, statistics
from collections import defaultdict
ins, lo, hi, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
out = []
def P(s): out.append(s)
def fl(x):
    try:
        v = float(x); return v if v == v else None
    except ValueError:
        return None
GRID = [i / 100 for i in range(101)]
def estar(l, u, pi, rr, mode):
    for e in GRID:
        f = e * pi * (l + u) * (1 - rr)
        loc = l * (1 - e) + e * pi * (l + u) * rr   # 押し返した分は在地へ
        st = 1 - l - u
        if mode == 'min':
            if f > max(st, loc, u * (1 - e)): return e
        else:
            if f > max(st + loc, u * (1 - e)): return e
    return None
lu = defaultdict(list); rho = defaultdict(list)
for s in range(lo, hi + 1):
    with open(f"{ins}/rule_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip("\n").split("\t")
            y = int(float(c[ix["year"]]))
            if not (1850 <= y <= 1914): continue
            l = fl(c[ix["tgtLocalShare"]]); u = fl(c[ix["tgtUngovShare"]])
            if l is None or u is None: continue
            lu[(s, c[ix["nation"]], c[ix["region"]], c[ix["tier"]])].append((l, u))
    with open(f"{ins}/secession_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip("\n").split("\t")
            y = int(float(c[ix["year"]]))
            if not (1850 <= y <= 1914): continue
            p = fl(c[ix["pressureIndex"]]); hh = fl(c[ix["holdShareIndex"]])
            if p is None or hh is None or p + hh <= 0: continue
            rho[(s, c[ix["nation"]], c[ix["region"]])].append(p / (p + hh))
BINS = [0.4, 0.6, 0.8, 1.0]
have = 0; rr_all = []
res = defaultdict(lambda: defaultdict(int))
for k, v in lu.items():
    l = statistics.median(a for a, _ in v); u = statistics.median(b for _, b in v)
    rv = rho.get(k[:3])
    if not rv: continue
    have += 1; rr = statistics.median(rv); rr_all.append(rr)
    for pi in (1.0, 0.44):
        for mode in ('min', 'max'):
            for tag, r_ in (("丙", 0.0), ("丙×(1−ρ_R)", rr)):
                e = estar(l, u, pi, r_, mode)
                b = next((x for x in BINS if e is not None and e <= x), 'never')
                res[(tag, pi, mode)][b] += 1
rr_all.sort()
P(f"州（種×国×州・1850〜1914 の中央）で secession_s の ρ_R が取れた数 {have}（rule_s の州 {len(lu)}）・ρ_R の中央 {rr_all[len(rr_all)//2]:.3f}・上 1/4 {rr_all[3*len(rr_all)//4]:.3f}・上 1/10 {rr_all[9*len(rr_all)//10]:.3f}")
for key in sorted(res):
    d = res[key]
    P(f"  {key[0]} π{key[1]} {key[2]}: " + "  ".join(f"e*≦{b}:{d[b]}" for b in BINS) + f"  届かない:{d['never']}")
with open(outp, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(out) + "\n")
