import sys, glob, os, collections
sys.path.insert(0, 'D:/PoB_work/wt_e/scripts/instruments')
from instr import rows, num
D = sys.argv[1]
tot = collections.Counter(); bynat = collections.defaultdict(collections.Counter); seeds=collections.defaultdict(collections.Counter)
cols = ['stateFacilityStartedCount','stateFacilityStartedGateClosedCount','stateFacilityCompletedCount','stateFacilityCompletedGateClosedCount','stateFacilityCompletedGateClosedAtStartCount']
for sd, r in rows(D, 'nation'):
    for c in cols:
        v = int(num(r[c])); tot[c] += v; seeds[sd][c] += v; bynat[r['nation']][c] += v
print('TOTAL', {c[13:]: tot[c] for c in cols})
for sd in sorted(seeds): print('seed', sd, [seeds[sd][c] for c in cols])
top = sorted(bynat.items(), key=lambda kv: -kv[1][cols[3]])[:8]
for n, c in top: print(n, [c[x] for x in cols])
