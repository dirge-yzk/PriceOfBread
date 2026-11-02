# (784) 読み手 r783 の台本（使い捨ての写し・元は Temp）・入力のパスは当時の固定の値（keep_778）
import sys, json, math, collections
sys.stdout.reconfigure(encoding='utf-8')
D, sd, REG, PL = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
pl = {p['name']: (p['lat'], p['lon']) for p in json.load(open(PL, encoding='utf-8'))}
def gc(a, b):
    if a not in pl or b not in pl: return float('nan')
    (la1, lo1), (la2, lo2) = pl[a], pl[b]
    p1, p2 = math.radians(la1), math.radians(la2); dl = math.radians(lo2 - lo1)
    return 6371 * math.acos(min(1, math.sin(p1)*math.sin(p2) + math.cos(p1)*math.cos(p2)*math.cos(dl)))
rows = []
with open(f'{D}/region_s{sd}.tsv', encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for l in fh:
        c = l.rstrip('\n').split('\t')
        if c[ix['region']] != REG: continue
        g = lambda k: float(c[ix[k]])
        rows.append((int(c[0]), int(c[1]), c[ix['nation']], c[ix['tier']], g('populationK'), g('ruralPopulationK'), g('pushFactor'), g('holdingFactor'), g('wagePremium'), g('opportunityIndex'), g('migrationOutKPerQuarter'), g('migrationInKPerQuarter'), g('crossBorderOutKPerQuarter')))
rows.sort()
print('owner timeline:')
prev = None
for r in rows:
    if r[2] != prev: print(f'  {r[1]} -> {r[2]} ({r[3]}) pop {r[4]:.0f}'); prev = r[2]
pk = max(rows, key=lambda r: r[4]); mn = min(rows, key=lambda r: r[4])
print(f'peak {pk[4]:.0f} @{pk[1]} ({pk[2]})  min {mn[4]:.0f} @{mn[1]}  end {rows[-1][4]:.0f} urban_end {rows[-1][4]-rows[-1][5]:.0f}')
# decade table
dec = collections.OrderedDict()
for r in rows:
    k = r[1] // 10 * 10
    d = dec.setdefault(k, [r[2], r[4], r[4] - r[5], 0, 0, 0, 0, 0, 0, 0])
    d[0] = r[2]; d[3] += r[10]; d[4] += r[11]; d[5] += r[12]; d[6] += r[6]; d[7] += 1; d[8] = r[4]; d[9] = r[7]
print('decade\towner\tpop0\turban0\tdomOut\tdomIn\txbOut\tpushMean\tpopEnd\tholdEnd')
for k, d in dec.items(): print(f'{k}\t{d[0]}\t{d[1]:.0f}\t{d[2]:.0f}\t{d[3]:.0f}\t{d[4]:.0f}\t{d[5]:.1f}\t{d[6]/d[7]:.2f}\t{d[8]:.0f}\t{d[9]:.2f}')
dst = collections.Counter()
with open(f'{D}/migration_s{sd}.tsv', encoding='utf-8') as fh:
    fh.readline()
    for l in fh:
        q, a, b, mv, km = l.rstrip('\n').split('\t')
        if a == REG: dst[(b, km)] += float(mv)
print('top domestic destinations (total K, gcKm, routeKm):')
for (b, km), v in dst.most_common(6): print(f'  {b}\t{v:.0f}\t{gc(REG, b):.0f}\t{float(km):.0f}')
