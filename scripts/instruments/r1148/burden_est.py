# (1148) 開幕の植民地の海運の占有（World.ColonialShipBurden）を world.json と places.json から見積もる（読むだけ）
# 式は Colonial.cs:19-36 を文から写す：占有＝0.12×Σ(海路 km÷1万)×(州の人口÷ColonyUnitPop)。海路の km の代わりに大圏の km（下側）と ×1.5（上側の仮）を並べる。帆走の時代（汽船の割引なし）。
import sys, os, json, math
SIM = os.path.join(sys.argv[1], 'PriceOfBread.SimCore')
sys.stdout.reconfigure(newline='\n')
W = json.load(open(os.path.join(SIM, 'world.json'), encoding='utf-8'))
P = {p['name']: (p['lat'], p['lon']) for p in json.load(open(os.path.join(SIM, 'places.json'), encoding='utf-8'))}
cols = [c for n in W['nations'] for c in n.get('initialColonies') or []] + [c for t in W['abstractTargets'] for c in t['colonies']]
unit = sum(c['popK'] for c in cols) / len(cols)
def gc(a, b):
    la1, lo1, la2, lo2 = map(math.radians, (a[0], a[1], b[0], b[1]))
    h = math.sin((la2 - la1) / 2) ** 2 + math.cos(la1) * math.cos(la2) * math.sin((lo2 - lo1) / 2) ** 2
    return 2 * 6371 * math.asin(math.sqrt(h))
print('ColonyUnitPop(K)\t%.1f\tcolonies\t%d' % (unit, len(cols)))
print('nation\tcolonies\tsumPopK\tburden_gc\tburden_gcx1.5\tshipDivisor_gc(1/(1+b))\tmissingCoord')
for n in W['nations']:
    ic = n.get('initialColonies') or []
    if not ic: continue
    cap = (n['latitude'], n['longitude']); b = 0; miss = 0
    for c in ic:
        if c['name'] not in P: miss += 1; continue
        b += 0.12 * gc(cap, P[c['name']]) / 10000 * c['popK'] / unit
    print('%s\t%d\t%.0f\t%.2f\t%.2f\t%.3f\t%d' % (n['name'], len(ic), sum(c['popK'] for c in ic), b, 1.5 * b, 1 / (1 + b), miss))
