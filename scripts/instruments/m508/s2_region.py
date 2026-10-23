# (508) 線 fd の S2（発散）を州ごとに読む（反証役 fd〔bv〕中#1 の処置＝州ごとの読みの出力を measurements に残す）。
# 定義（fd の追記 3 の字面）：州の人口 populationK の試合中の最大が、比べる側（旗 0 の世界・同じ種・同じ州名）のその州の最大の 10 倍を超えたら発散。
#   国の総人口でなく州ごと＝領土の移り変わり（州が別の国へ移る）で国の和が跳ねる形を除く。州名は試合を通して同じ物を指す。
# 使い方: PYTHONUTF8=1 python s2_region.py <腕の ins> <比べる側の ins> [線=10]
import os, sys, csv
csv.field_size_limit(1 << 30)
A, B = sys.argv[1], sys.argv[2]
LINE = float(sys.argv[3]) if len(sys.argv) > 3 else 10.0


def peaks(d, sd):
    p = os.path.join(d, f"region_s{sd}.tsv")
    if not os.path.exists(p):
        return None
    out = {}
    with open(p, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t")
        h = next(r)
        ir, ip, iq = h.index("region"), h.index("populationK"), h.index("quarter")
        for row in r:
            try:
                v = float(row[ip])
            except (ValueError, IndexError):
                continue
            k = row[ir]
            if k not in out or v > out[k][0]:
                out[k] = (v, row[iq])
    return out


tot = 0; over = []; worst = (0.0, None); nonfin = 0; seeds = 0
for sd in range(1, 21):
    a = peaks(A, sd); b = peaks(B, sd)
    if a is None or b is None:
        continue
    seeds += 1
    for k, (v, q) in a.items():
        if not (v == v) or v in (float("inf"), float("-inf")):
            nonfin += 1; continue
        if k not in b or not (b[k][0] > 0):
            continue
        tot += 1
        ratio = v / b[k][0]
        if ratio > worst[0]:
            worst = (ratio, (sd, k, q, v, b[k][0]))
        if ratio > LINE:
            over.append((sd, k, q, v, b[k][0], ratio))
print(f"# S2 州ごと：腕 {A} ／比べる側 {B}・種 {seeds}・比べた州 {tot}・非有限 {nonfin}")
print(f"超えた州（線 {LINE:g} 倍）：{len(over)}")
for o in over[:50]:
    print("  種 %d %s 四半期 %s 腕の最大 %.1f 千人・比べる側の最大 %.1f・比 %.2f" % o)
if worst[1]:
    sd, k, q, v, bv = worst[1]
    print(f"最大の比 {worst[0]:.3f}（種 {sd}・{k}・四半期 {q}・腕の最大 {v:.1f}・比べる側 {bv:.1f}）")
