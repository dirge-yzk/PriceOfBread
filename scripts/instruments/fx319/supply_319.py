import sys, os, collections
sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
from instr import rows, num
D = sys.argv[1]
c = collections.Counter()
for sd, r in rows(D, 'nation'):
    s = num(r['supplyFillShare']); p = num(r['populationK'])
    if s == s and s < 0.3 and p >= 100: c[sd] += 1
print(sorted(c.items()), 'seeds>=1000:', sum(1 for v in c.values() if v >= 1000))
