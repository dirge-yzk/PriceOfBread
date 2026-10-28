# (619) 線 igi：全ての国の価格水準の発散の読み（読むだけ）。nation_s の priceLevel を国ごとに並べ、最大値・四半期の比の最大（1 四半期 100 倍以上・非有限・1e10 倍以上を数える）。
# 使い方：python s2_all.py <ins> [種の数=10]
import sys, csv, math
d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10
pmax = 0; rmax = 0; nonfin = 0; ge100 = 0; ge1e10 = 0; where = ''
for s in range(1, ns + 1):
    with open(f"{d}/nation_s{s}.tsv", encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        iq, inn, ip = h.index('quarter'), h.index('nation'), h.index('priceLevel')
        last = {}
        for row in r:
            try: v = float(row[ip])
            except: nonfin += 1; continue
            if not math.isfinite(v): nonfin += 1; continue
            n = row[inn]; q = int(row[iq])
            if v > pmax: pmax = v; where = f"s{s} {n} q{q}"
            if v > 1e10: ge1e10 += 1
            if n in last and last[n][1] > 0:
                dq = q - last[n][0]
                ratio = v / last[n][1]
                if dq == 1 or dq == 0:
                    if ratio > rmax: rmax = ratio
                    if ratio >= 100: ge100 += 1
            last[n] = (q, v)
print(f"priceLevel max {pmax:.3f} ({where}) quarter_ratio_max {rmax:.3f} ratio>=100 rows {ge100} nonfinite {nonfin} >=1e10 rows {ge1e10}")
