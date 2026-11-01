# (754) c4pl の紙の予言の台本（読むだけ・開幕の盤面）。routedump_344.tsv.gz（全 344 州の対の gc・land）と world.json から、
# 開幕の内陸州ごとに〈大圏で選ぶ港〉と〈陸路で選ぶ港〉を並べ、鉄道の減衰（1/(1+k·km/N)）の新÷旧を数える。
# python -X utf8 m754_20261102_paper.py
import gzip, json, math, os, statistics as st, collections
HERE = os.path.dirname(os.path.abspath(__file__))
R = {}
with gzip.open(os.path.join(HERE, 'hol744_20261101', 'routedump_344.tsv.gz'), 'rt', encoding='utf-8') as fh:
    next(fh)
    for ln in fh:
        c = ln.rstrip('\n').split('\t')
        R[(c[0], c[1])] = (float(c[4]), float(c[5]), c[2], c[3])   # gc, land, portA, portB
states = sorted({a for a, _ in R})
port = {a: R[(a, b)][2] for (a, b) in R}   # a の港（portA）
isPort = {a for a in states if port[a] == a}
W = json.load(open(os.path.join(HERE, '..', 'PriceOfBread.SimCore', 'world.json'), encoding='utf-8'))
nat = {}
for n in W['nations']:
    for p in n.get('provinces', []): nat[p['city']] = n['name']
    for c in n.get('initialColonies', []): nat.setdefault(c['name'], n['name'])
miss = [s for s in states if s not in nat]
K, N = 1.0, 10000.0
dec = lambda km: 1.0 / (1.0 + K * km / N)
rows = []; changed = []; basin_note = []
for a in states:
    if a in isPort or a not in nat: continue
    ports = [p for p in isPort if nat.get(p) == nat[a] and (a, p) in R]
    if not ports: continue
    # 旧＝大圏の最小（先着＝states 順）・新＝陸路が有限の港のうち陸路の最小（無ければ旧）
    pg = min(ports, key=lambda p: (R[(a, p)][0], states.index(p)))
    fin = [p for p in ports if math.isfinite(R[(a, p)][1])]
    pl = min(fin, key=lambda p: (R[(a, p)][1], states.index(p))) if fin else pg
    gc = R[(a, pg)][0]
    landOld = R[(a, pg)][1] if math.isfinite(R[(a, pg)][1]) else gc   # 旧の PortLandKm
    landNew = R[(a, pl)][1] if math.isfinite(R[(a, pl)][1]) else R[(a, pl)][0]
    old = dec(gc); new = dec(landNew)
    rows.append((a, nat[a], pg, pl, gc, landOld, landNew, new / old))
    if pg != pl: changed.append((a, nat[a], pg, pl, gc, landNew))
print('states', len(states), 'ports', len(isPort), 'noNation', len(miss), miss[:5])
print('inland states', len(rows))
print('port changes', len(changed))
for c in changed: print('  ', c[1], c[0], c[2], '->', c[3], f'gc={c[4]:.0f} landNew={c[5]:.0f}')
rt = [r[7] for r in rows]
print('ratio median %.3f min %.3f  n<0.99: %d  sum(1-ratio)=%.3f' % (st.median(rt), min(rt), sum(1 for x in rt if x < 0.99), sum(1 - x for x in rt)))
mn = min(rows, key=lambda r: r[7]); print('min row', mn)
