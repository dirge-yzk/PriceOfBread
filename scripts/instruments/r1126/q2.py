# (1126) 最低賃金の上げを AI が望んだ四半期（行 q の MinimumWageTarget > 行 q-1 の MinimumWage）で、通ったか（行 q の MinimumWage が上がったか）を
#   議会の項の符号の材料 PT＝社会党−自由党−0.5×保守党（行 q の sf_s の席＝その四半期の Parliament.Step の後・Government.Step が読む席）で分ける。
#   Pref(最低賃金, 族)＝大衆 +1・近代 −1・旧 −0.5（PolicyPreferences.cs:265-266）＝PT は ParliamentNet÷ParliamentLawWeight と同じ符号と比。
#   効いていた政体＝行 q-1 の polityCode。計器が書かない行（sf_s の無い四半期）は数えない。
#   python -X utf8 -I q2.py <出力の頭> <年の始め-終わり> <名=生 ins> [...]
import sys, os, csv, collections
sys.stdout.reconfigure(newline='\n')
ELECT = {3, 4}
out = sys.argv[1]; Y0, Y1 = map(int, sys.argv[2].split('-'))
NAT9 = None

def rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            if len(x) >= len(h): yield ix, x

def binof(pt):
    if pt is None: return 'none'
    if pt < 1e-9 and pt > -1e-9: return '0(empty)'
    for lo in (-1.0, -0.6, -0.4, -0.2, 0.0, 0.2):
        pass
    edges = [-0.6, -0.4, -0.2, 0.0, 0.2]
    for e in edges:
        if pt < e: return f'<{e:+.1f}'
    return '>=+0.2'

res = collections.defaultdict(lambda: [0, 0])
gp = collections.defaultdict(lambda: [0, 0])
nat = collections.defaultdict(lambda: collections.Counter())
for a in sys.argv[3:]:
    name, d = a.split('=', 1)
    seeds = sorted(int(f[len('sf_s'):-4]) for f in os.listdir(d) if f.startswith('sf_s') and f.endswith('.tsv'))
    for s in seeds:
        seat = {}
        for ix, x in rows(os.path.join(d, f'sf_s{s}.tsv')):
            try: seat[(int(x[ix['quarter']]), x[ix['nation']])] = (float(x[ix['seatCons']]), float(x[ix['seatLib']]), float(x[ix['seatSoc']]), x[ix['governingParty']])
            except ValueError: pass
        by = collections.defaultdict(list)
        for ix, x in rows(os.path.join(d, f'monlaw_s{s}.tsv')):
            by[x[ix['nation']]].append((int(x[ix['quarter']]), int(x[ix['year']]), int(x[ix['polityCode']]), int(x[ix['MinimumWage']]), int(x[ix['MinimumWageTarget']])))
        for n, L in by.items():
            L.sort()
            for i in range(1, len(L)):
                q, y, pc, mw, tg = L[i]; pq, py, ppc, pmw, ptg = L[i - 1]
                if not (Y0 <= y <= Y1) or pq != q - 1: continue
                if (q, n) not in seat: continue
                if not (tg > pmw): continue
                passed = mw > pmw
                sc, sl, ss, g = seat[(q, n)]
                tot = sc + sl + ss
                el = ppc in ELECT
                pt = (ss - sl - 0.5 * sc) if el and tot > 1e-9 else (0.0 if el else None)
                b = binof(pt) if el else 'nonelective'
                r = res[(name, b)]; r[0] += 1; r[1] += passed
                if el and tot > 1e-9:
                    rr = gp[(name, g)]; rr[0] += 1; rr[1] += passed
                c = nat[(name, n)]; c['want'] += 1; c['pass'] += passed
                c['wantE' if el else 'wantN'] += 1; c['passE' if el else 'passN'] += passed
                if el and tot > 1e-9:
                    c['ptSum'] += pt; c['ptN'] += 1; c['socSum'] += ss / tot; c['libSum'] += sl / tot
                    c['ptPos'] += pt >= 0
names = [a.split('=', 1)[0] for a in sys.argv[3:]]
bins = ['nonelective', '0(empty)', '<-0.6', '<-0.4', '<-0.2', '<+0.0', '<+0.2', '>=+0.2']
with open(out + '_bins.tsv', 'w', encoding='utf-8', newline='\n') as f:
    f.write('arm\t' + '\t'.join(bins) + '\n')
    for nm in names:
        f.write(nm + '\t' + '\t'.join(f'{res[(nm, b)][1]}/{res[(nm, b)][0]}' for b in bins) + '\n')
    f.write('\narm\tgoverningParty(pass/want)\n')
    for nm in names:
        f.write(nm + '\t' + '  '.join(f'{g}:{v[1]}/{v[0]}' for (n2, g), v in sorted(gp.items()) if n2 == nm) + '\n')
with open(out + '_nat.tsv', 'w', encoding='utf-8', newline='\n') as f:
    f.write('arm\tnation\twantE\tpassE\twantN\tpassN\tptMean\tsocMean\tlibMean\tptPosShare\n')
    for (nm, n), c in sorted(nat.items(), key=lambda kv: (kv[0][1], kv[0][0])):
        k = c['ptN']
        f.write(f"{nm}\t{n}\t{c['wantE']}\t{c['passE']}\t{c['wantN']}\t{c['passN']}\t" + (f"{c['ptSum']/k:.3f}\t{c['socSum']/k:.3f}\t{c['libSum']/k:.3f}\t{c['ptPos']/k:.3f}" if k else '—\t—\t—\t—') + '\n')
