# (657) 線 igi3：腕の ins フォルダの要約（読むだけ）。補給崩壊（populationK≥100・supplyFillShare<0.3 の国×四半期）・価格水準（nation_s の priceLevel）の絶対の最大・四半期の比の最大・
# 開幕（その国の最初の行）の 1000 倍以上の件数・非有限の件数・人口の最大の倍率（開幕比）。使い方：python arm_sum.py <ins> [種の起点=1] [種の数=10]
import sys, csv, math, collections
d = sys.argv[1]; s0 = int(sys.argv[2]) if len(sys.argv) > 2 else 1; n = int(sys.argv[3]) if len(sys.argv) > 3 else 10
csv.field_size_limit(1 << 30)
sup = []; pmax = (0, '', 0, 0); rmax = (0, '', 0, 0); k1000 = []; nonfin = 0; popx = (0, '', 0)
for s in range(s0, s0 + n):
    with open(f"{d}/nation_s{s}.tsv", 'rb') as fh:
        rd = csv.reader(fh.read().decode('utf-8').splitlines(), delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        iq, inat, ipop, isup, ipl, iy = ix['quarter'], ix['nation'], ix['populationK'], ix['supplyFillShare'], ix['priceLevel'], ix['year']
        c = 0; first = {}; last = {}; firstpop = {}; hit = set()
        for r in rd:
            try: pop = float(r[ipop]); sf = float(r[isup]); pl = float(r[ipl])
            except ValueError: nonfin += 1; continue
            if not (math.isfinite(pl) and math.isfinite(pop)): nonfin += 1; continue
            if pop >= 100 and sf < 0.3: c += 1
            nat = r[inat]; q = int(r[iq])
            if pl > pmax[0]: pmax = (pl, nat, int(r[iy]), s)
            if nat in last and last[nat] > 0:
                ratio = pl / last[nat] if pl > last[nat] else last[nat] / pl
                if ratio > rmax[0]: rmax = (ratio, nat, int(r[iy]), s)
            last[nat] = pl
            if nat not in first: first[nat] = pl; firstpop[nat] = pop
            elif first[nat] > 0 and pl >= 1000 * first[nat]: hit.add(nat)
            if nat in firstpop and firstpop[nat] > 100 and pop / firstpop[nat] > popx[0]: popx = (pop / firstpop[nat], nat, s)
        sup.append(c); k1000.append(len(hit))
print(f"補給崩壊(国×四半期,種ごと) 合計 {sum(sup)} {sup}")
print(f"価格水準の最大 {pmax[0]:.3f} ({pmax[1]} {pmax[2]}年 種{pmax[3]})・四半期の比の最大 {rmax[0]:.2f} ({rmax[1]} {rmax[2]}年 種{rmax[3]})・開幕の1000倍以上の国の数(種ごと) {k1000}・非有限の行 {nonfin}")
print(f"人口の開幕比の最大 {popx[0]:.2f} ({popx[1]} 種{popx[2]})")
