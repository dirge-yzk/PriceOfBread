# (1422) c152sd 門 G1（史実の桁・暴力の道 3 本の和・5 大国）の数え口。読むだけ。
#   使い方: python paper_g1.py <ins のフォルダ> [種の数=10] [種の起点=1]
#   勢力の蜂起＝dcont_s の factionRevoltsCum の国ごとの最大（累計）・首都の蜂起＝coupgate_s の route=3 の行の数・
#   一揆の条件の州×四半期＝facfloor_s の riotHeatRegions の和（表が無い木では〈表なし〉と書く）。
import sys, os, csv
csv.field_size_limit(1 << 30)
P5 = ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア"]
d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10; s0 = int(sys.argv[3]) if len(sys.argv) > 3 else 1
def rows(t, s):
    p = os.path.join(d, f"{t}_s{s}.tsv")
    if not os.path.exists(p): return None
    with open(p, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r)
        return h, list(r)
tot = {"revolt": 0, "route3": 0, "riot": 0}; riotMissing = 0; per = {n: [0, 0, 0] for n in P5}
for s in range(s0, s0 + ns):
    x = rows("dcont", s)
    if x:
        h, rs = x; i_n = h.index("nation"); i_c = h.index("factionRevoltsCum")
        mx = {}
        for r in rs:
            if r[i_n] in per: mx[r[i_n]] = max(mx.get(r[i_n], 0), int(float(r[i_c])))
        for n, v in mx.items(): per[n][0] += v; tot["revolt"] += v
    x = rows("coupgate", s)
    if x:
        h, rs = x; i_n = h.index("nation"); i_r = h.index("route")
        for r in rs:
            if r[i_n] in per and r[i_r] == "3": per[r[i_n]][1] += 1; tot["route3"] += 1
    x = rows("facfloor", s)
    if x is None: riotMissing += 1
    else:
        h, rs = x; i_n = h.index("nation"); i_h = h.index("riotHeatRegions")
        for r in rs:
            if r[i_n] in per: v = int(float(r[i_h])); per[r[i_n]][2] += v; tot["riot"] += v
print(f"ins={d} 種={s0}..{s0+ns-1}")
for n in P5: print(f"{n}\t蜂起 {per[n][0]}\t首都の蜂起 {per[n][1]}\t一揆の条件 {per[n][2]}")
print(f"和：勢力の蜂起 {tot['revolt']}・首都の蜂起 route3 {tot['route3']}・一揆の条件の州×四半期 {tot['riot'] if riotMissing == 0 else '表なし（' + str(riotMissing) + ' 種）'}")
print(f"G1_SUM\t{tot['revolt'] + tot['route3'] + tot['riot']}\triotMissing={riotMissing}")
