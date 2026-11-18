# 使い方: python -X utf8 -I capext.py <rawdir> <seeds 1-10> <out>
# 国ごと: 資本家(classId 3)の大きさのピークと最後・消滅の年（ピークの 1/1000 未満に初めて落ちた年）・
#   最後の 8 四半期の満足度の平均（satneeds_s）・農民と工場労働者の1人あたり稼ぎ（1900 年と最後）
import sys, math
raw, s0, s1, out = sys.argv[1], int(sys.argv[2].split('-')[0]), int(sys.argv[2].split('-')[1]), sys.argv[3]
def fl(x):
    try: return float(x)
    except: return float("nan")
res = []
for s in range(s0, s1+1):
    cap = {}; earn = {}
    try: f = open(f"{raw}/class_s{s}.tsv", encoding="utf-8")
    except FileNotFoundError: continue
    with f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k:i for i,k in enumerate(h)}
        for l in f:
            c = l.rstrip("\n").split("\t")
            if c[0] == "open": continue
            n, y, k = c[1], int(c[2]), int(c[3])
            if k == 3: cap.setdefault(n, []).append((y, fl(c[ix["sizeK"]]), fl(c[ix["savingsStock"]])))
            if k in (1, 2):
                d = earn.setdefault(n, {}); e = d.setdefault(y, [0.0, 0.0]); e[0] += fl(c[ix["earnedPerMonth"]]); e[1] += fl(c[ix["sizeK"]])
    sat = {}
    try:
        with open(f"{raw}/satneeds_s{s}.tsv", encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix2 = {k:i for i,k in enumerate(h)}
            for l in f:
                c = l.rstrip("\n").split("\t"); sat.setdefault(c[ix2["nation"]], []).append((fl(c[ix2["popK"]]), fl(c[ix2["satisfaction"]])))
    except FileNotFoundError: pass
    for n, v in cap.items():
        pk = max(x[1] for x in v); last = v[-1]
        ext = next((y for y, sz, sv in v if pk > 0 and sz < pk/1000 and y > v[0][0]), None)
        sv = sat.get(n, [])
        tail = [x[1] for x in sv[-8:]]
        sm = sum(tail)/len(tail) if tail else float("nan")
        e = earn.get(n, {}); ys = sorted(e)
        def pc(y): return e[y][0]/e[y][1] if y in e and e[y][1] > 0 else float("nan")
        e1900 = pc(1900) if 1900 in e else float("nan")
        res.append((s, n, pk, last[0], last[1], ext, sm, sv[-1][0] if sv else float("nan"), e1900, pc(ys[-1]) if ys else float("nan")))
with open(out, "w", encoding="utf-8", newline="\n") as o:
    o.write("seed\tnation\tcapPeakK\tlastYear\tcapLastK\textinctYear\tsatLast8\tpopK\tmassEarnPcM1900\tmassEarnPcMLast\n")
    for r in res:
        o.write("\t".join(str(x) if not isinstance(x, float) else ("nan" if not math.isfinite(x) else f"{x:.4g}") for x in r) + "\n")
# 要約: 最後まで生き残った国（lastYear==1950）で、消滅あり／なしごとの満足度の分布
alive = [r for r in res if r[3] >= 1949 and math.isfinite(r[6])]
ex = [r for r in alive if r[5] is not None]; nx = [r for r in alive if r[5] is None]
def q(v, p): v = sorted(v); return v[int(p*(len(v)-1))] if v else float("nan")
for name, g in (("消滅あり", ex), ("消滅なし", nx)):
    v = [r[6] for r in g]
    print(name, "国×種", len(g), "満足度 中央", round(q(v,.5),3), "下位10%", round(q(v,.1),3), "<0.05 の数", sum(1 for x in v if x < 0.05), "<0.02", sum(1 for x in v if x < 0.02))
low = [r for r in alive if r[6] < 0.05]
print("満足度<0.05 の国×種", len(low), "うち資本家の消滅あり", sum(1 for r in low if r[5] is not None))
for r in sorted(low, key=lambda r: r[6]): print("  ", r[0], r[1], "消滅", r[5], "満足", round(r[6],3), "人口", round(r[7]), "稼ぎ1900", r[8], "最後", r[9])
