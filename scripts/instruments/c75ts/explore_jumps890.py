# (890) 探索（旗 0 とヌル腕だけ）：名目 GDP の四半期の上向きの比 ≧ THR の跳びを全て並べ、型の特徴を出す。
#   使い方: python -X utf8 explore_jumps.py <THR> <名前>=<ins> ...
import csv, glob, math, os, sys
N = 8
def num(s):
    try: return float(s)
    except ValueError: return float('nan')
thr = float(sys.argv[1])
print("name\tseed\tnation\tyear\tq\tage\tratio\tpopR\tdip\tover\trealR\tpriceR\tprevG\tpeakG\tnewG")
for arg in sys.argv[2:]:
    name, d = arg.split('=', 1)
    for p in sorted(glob.glob(os.path.join(d, 'nation_s*.tsv'))):
        s = int(os.path.basename(p)[len('nation_s'):-4])
        with open(p, encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            hist = {}; first = {}
            for x in r:
                u = x[ix['nationUid']]; q = int(x[ix['quarter']])
                if u not in first: first[u] = q
                g = num(x[ix['nominalGdpPerYear']]); pop = num(x[ix['populationK']])
                rg = num(x[ix['realGdpPerYear']]); pl = num(x[ix['priceLevel']])
                if not math.isfinite(g): continue
                H = hist.setdefault(u, [])
                if H and H[-1][0] > 0:
                    ra = g / H[-1][0]
                    if ra >= thr:
                        prevg = H[-1][0]
                        win = [h[0] for h in H[-(N + 1):-1]]
                        peak = max(win) if win else float('nan')
                        dip = prevg / peak if win and peak > 0 else float('nan')
                        over = g / max([h[0] for h in H[-(N + 1):]])
                        popr = pop / H[-1][1] if H[-1][1] > 0 else float('nan')
                        realr = rg / H[-1][2] if H[-1][2] > 0 else float('nan')
                        pricer = pl / H[-1][3] if H[-1][3] > 0 else float('nan')
                        print("%s\t%d\t%s\t%s\t%d\t%d\t%.1f\t%.4f\t%.4g\t%.4g\t%.4g\t%.4g\t%.4g\t%.4g\t%.4g" % (name, s, x[ix['nation']], x[ix['year']], q, q - first[u], ra, popr, dip, over, realr, pricer, prevg, peak, g))
                H.append((g, pop, rg, pl))
