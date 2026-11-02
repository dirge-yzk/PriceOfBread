# (784) 読み手 r783 の台本（使い捨ての写し・元は Temp）・入力のパスは当時の固定の値（keep_778）
"""Rotterdam (N1 seed2) domestic gap decomposition: real vs counterfactual (push=1, route km)."""
import sys, json, math, collections
D = sys.argv[1]; seed = sys.argv[2]; REG = sys.argv[3]
pl = {p['name']: (p['lat'], p['lon']) for p in json.load(open(sys.argv[4], encoding='utf-8'))}
def gc(a, b):
    (la1, lo1), (la2, lo2) = pl[a], pl[b]
    p1, p2 = math.radians(la1), math.radians(la2); dl = math.radians(lo2 - lo1)
    return 6371 * math.acos(min(1, math.sin(p1)*math.sin(p2) + math.cos(p1)*math.cos(p2)*math.cos(dl)))
route = {}
with open(f'{D}/migration_s{seed}.tsv', encoding='utf-8') as fh:
    fh.readline()
    for l in fh:
        q, a, b, mv, km = l.rstrip('\n').split('\t')
        if a == REG: route[b] = float(km)
rows = collections.defaultdict(dict)
with open(f'{D}/region_s{seed}.tsv', encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for l in fh:
        c = l.rstrip('\n').split('\t'); q = int(c[0])
        if q < 400 or q > 520 or q % 4: continue
        rows[q][c[ix['region']]] = (c[ix['nation']], float(c[ix['opportunityIndex']]), float(c[ix['holdingFactor']]), float(c[ix['pushFactor']]), c[ix['tier']], float(c[ix['migrationOutKPerQuarter']]), float(c[ix['populationK']]))
print('year\tROTstay\tROTpush\tbestDest\tgcKm\trouteKm\tgapReal(Era1)\tgapRouteKm\tgapPush1\tmOutK')
for q in sorted(rows):
    R = rows[q]; nat, o, hd, pu, t, mo, pop = R[REG]
    stay = o * hd
    best = None
    for name, (n2, o2, *_ ) in R.items():
        if n2 != nat or name == REG or name not in pl: continue
        g = gc(REG, name); fr = 0.15 * g / 10000
        gr = o2 - stay - 0.15 - fr
        rk = route.get(name, float('nan'))
        grr = o2 - stay - 0.15 - 0.15 * rk / 10000 if rk == rk else float('nan')
        gp = o2 - stay / max(pu, 1e-9) - 0.15 - fr
        if best is None or gr > best[2]: best = (name, g, gr, rk, grr, gp)
    name, g, gr, rk, grr, gp = best
    print(f'{1783+q//4}\t{stay:.2f}\t{pu:.2f}\t{name}\t{g:.0f}\t{rk:.0f}\t{gr:+.3f}\t{grr:+.3f}\t{gp:+.3f}\t{mo:.1f}')
