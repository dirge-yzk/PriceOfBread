# (508) 線 fd の ③ の観測量（反証役 fd〔bv〕中#2 の処置＝M4 は式から決まる配線の確かめ＝働いた証拠には予算の後の量を見る）。
# 読む：fd_s の demandPerMonth（予算の後に実際に買えた主食の量・式からは決まらない）÷stapleNeedPerMonth を、
#   同じ州の〈天候の凶作の四半期〉（weatherHarvestMean<0.9）と〈平年の四半期〉（0.95〜1.05）で比べる（州ごとの中央値の対）。
#   併せて、凶作の四半期の充足 fillNew を旗 0 の世界（比べる側の region_s foodFillShare・同じ種・州・四半期）と比べる。
# 使い方: PYTHONUTF8=1 python fd_drought.py <腕の ins> <旗 0 の世界の ins>
import os, sys, csv, statistics as st
csv.field_size_limit(1 << 30)
A, Z = sys.argv[1], sys.argv[2]


def rows(p):
    with open(p, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r)
        for x in r:
            if len(x) == len(h):
                yield dict(zip(h, x))


pairs = []; dfill = []; nreg = 0
for sd in range(1, 21):
    pf = os.path.join(A, f"fd_s{sd}.tsv")
    if not os.path.exists(pf):
        continue
    zf = {}
    pz = os.path.join(Z, f"region_s{sd}.tsv")
    if os.path.exists(pz):
        for r in rows(pz):
            zf[(r["region"], r["quarter"])] = r["foodFillShare"]
    by = {}
    for r in rows(pf):
        try:
            w = float(r["weatherHarvestMean"]); need = float(r["stapleNeedPerMonth"]); dem = float(r["demandPerMonth"]); fn = float(r["fillNew"])
        except ValueError:
            continue
        if not (need > 0):
            continue
        k = r["region"]
        by.setdefault(k, ([], []))
        if w < 0.9:
            by[k][0].append(dem / need)
            z = zf.get((k, r["quarter"]))
            try:
                zv = float(z)
                if zv == zv: dfill.append(fn - zv)
            except (TypeError, ValueError):
                pass
        elif 0.95 <= w <= 1.05:
            by[k][1].append(dem / need)
    for k, (dr, nm) in by.items():
        if dr and nm:
            pairs.append(st.median(dr) - st.median(nm)); nreg += 1
pos = sum(1 for d in pairs if d > 0)
print(f"# (508) ③ の観測量：腕 {A}・旗 0 の世界 {Z}")
print(f"予算の後に買えた主食÷必要量：凶作と平年の四半期が両方ある州×種 {nreg}・凶作の方が多い {pos}（{pos / max(1, nreg):.3f}）・差の中央値 {st.median(pairs) if pairs else float('nan'):+.4f}")
print(f"凶作の四半期の充足 fillNew − 旗 0 の世界の foodFillShare：行 {len(dfill)}・中央値 {st.median(dfill) if dfill else float('nan'):+.4f}・正の割合 {sum(1 for d in dfill if d > 0) / max(1, len(dfill)):.3f}（①③を合わせた効果・軌道が割れた 2 つの世界の比較）")
