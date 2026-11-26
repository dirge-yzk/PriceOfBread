import sys; sys.path.insert(0, 'D:/PoB_work/r1382_work/scripts')
from load import *; from align import match
cat = {'交戦中の側が厭戦>0.5': 0, '平時（講和の後の減衰）だけで厭戦>0.5': 0}; pairsq = set(); hi = 0; wclock = 0
for s in SEEDS:
    sc = {(int(v['quarter']), v['nation']): (fl(v['warExhaustion']), fl(v['atWar'])) for v in rows('scalars', s)}
    o, _ = match(s); iv = {}
    for b, a in o:
        e = int(b['month']); iv.setdefault(frozenset((a['sideA'], a['sideB'])), []).append((e - int(a['months']), e))
    for v in rows('warpair', s):
        q = int(v['quarter']); ge, go = int(v['monthsGe140']), int(v['gateOpenMonths'])
        if ge <= go: continue
        k = frozenset((v['nationA'], v['nationB']))
        if any(st <= q * 3 + 2 and q * 3 <= en for st, en in iv.get(k, [])): continue
        A, B = sc.get((q, v['nationA'])), sc.get((q, v['nationB']))
        if A is None or B is None: continue
        w = [x for x in (A, B) if x[0] > 0.5]
        if not w: continue
        n = ge - go
        if any(x[1] > 0 for x in w): cat['交戦中の側が厭戦>0.5'] += n
        else: cat['平時（講和の後の減衰）だけで厭戦>0.5'] += n; pairsq.add((s, k)); hi += (ge - go) >= 3
print(cat, '平時の側の組（種×組）', len(pairsq), '四半期の中で3か月とも閉じた', hi)
