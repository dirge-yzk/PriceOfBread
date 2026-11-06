# (903) 記録：名目 GDP の分子の痩せ（人口 100 千人以上で名目 GDP が人口あたり 0.1 未満〔/千人・年〕の国×四半期・系列の最初の 4 行を除く）と、そこから 1 四半期で 100 倍以上の跳び（読むだけ）
import sys, os, csv, glob
for arg in sys.argv[1:]:
    k, d = arg.split("=", 1); thin = 0; jumps = []; nat = set()
    for f in sorted(glob.glob(os.path.join(d, "nation_s*.tsv"))):
        s = os.path.basename(f)[8:-4]; seq = {}
        with open(f, encoding="utf-8") as fh:
            r = csv.DictReader(fh, delimiter="\t")
            for x in r: seq.setdefault(x["nation"], []).append(x)
        for nm, rs in seq.items():
            for i, x in enumerate(rs):
                try: g = float(x["nominalGdpPerYear"]); p = float(x["populationK"])
                except ValueError: continue
                if i >= 4 and p >= 100 and g < 0.1 * p: thin += 1; nat.add((s, nm))
                if i >= 1:
                    try: g0 = float(rs[i-1]["nominalGdpPerYear"])
                    except ValueError: continue
                    if g0 > 0 and g / g0 >= 100: jumps.append((s, nm, x["year"], i, round(g / g0, 1), int(float(rs[i-1]["civilWarMonths"]))))
    print(f"{k}: 痩せの国×四半期 {thin}・国 {len(nat)}・100 倍以上の跳び {len(jumps)} {jumps[:6]}")
