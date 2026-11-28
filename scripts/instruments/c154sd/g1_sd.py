# (1435) c154sd 門 G1（史実の桁・暴力の道 3 本を別々に・全ての国と 5 大国）の数え口。読むだけ。
#   使い方: python g1_sd.py <ins のフォルダ> [種の数=10] [種の起点=1]
#   R1 勢力の蜂起＝dcont_s の factionRevoltsCum の国ごとの最大（累計）の和・国ごとの集中（上位 5 国）
#   R2 一揆＝facfloor2_s の riotFire の国×四半期の和（表が無い木では〈表なし〉）
#   R3 首都の蜂起＝coupgate_s の route=3 の行の数
import sys, os, csv
csv.field_size_limit(1 << 30)
P5 = {"イギリス", "フランス", "プロイセン", "オーストリア", "ロシア"}
d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10; s0 = int(sys.argv[3]) if len(sys.argv) > 3 else 1
def rows(t, s):
    p = os.path.join(d, f"{t}_s{s}.tsv")
    if not os.path.exists(p): return None
    with open(p, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r)
        return h, list(r)
R1 = {}; R2a = 0; R2p = 0; R3a = 0; R3p = 0; miss2 = 0; miss1 = 0; miss3 = 0
for s in range(s0, s0 + ns):
    x = rows("dcont", s)
    if x is None: miss1 += 1
    else:
        h, rs = x; i_n = h.index("nation"); i_c = h.index("factionRevoltsCum"); mx = {}
        for r in rs: mx[r[i_n]] = max(mx.get(r[i_n], 0), int(float(r[i_c])))
        for n, v in mx.items(): R1[n] = R1.get(n, 0) + v
    x = rows("facfloor2", s)
    if x is None: miss2 += 1
    else:
        h, rs = x; i_n = h.index("nation"); i_f = h.index("riotFire")
        for r in rs:
            v = int(float(r[i_f])); R2a += v
            if r[i_n] in P5: R2p += v
    x = rows("coupgate", s)
    if x is None: miss3 += 1
    else:
        h, rs = x; i_n = h.index("nation"); i_r = h.index("route")
        for r in rs:
            if r[i_r] == "3":
                R3a += 1
                if r[i_n] in P5: R3p += 1
r1a = sum(R1.values()); r1p = sum(v for n, v in R1.items() if n in P5)
top = sorted(R1.items(), key=lambda x: -x[1])[:5]
print(f"ins={d} 種={s0}..{s0+ns-1} 表なし dcont={miss1} facfloor2={miss2} coupgate={miss3}")
print(f"R1\t全て {r1a}\t5大国 {r1p}\t上位5国 " + "・".join(f"{n} {v}" for n, v in top))
print(f"R2\t全て {R2a if miss2 == 0 else '表なし'}\t5大国 {R2p if miss2 == 0 else '表なし'}")
print(f"R3\t全て {R3a}\t5大国 {R3p}")
print(f"G1ROW\t{r1a}\t{r1p}\t{R2a if miss2 == 0 else -1}\t{R2p if miss2 == 0 else -1}\t{R3a}\t{R3p}")
