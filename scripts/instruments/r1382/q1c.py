# (1382) 問い 1 の続き：全ての組で、緊張>=140 なのに門が閉じていた月のうち、片方の厭戦>0.5（scalars に行のある国だけ）だった月
import sys; sys.path.insert(0, 'D:/PoB_work/r1382_work/scripts')
from load import *; from align import match
closed = closed_w = closed_known = 0; wclock6 = 0; pq = 0
for s in SEEDS:
    ex = {(int(v['quarter']), v['nation']): fl(v['warExhaustion']) for v in rows('scalars', s)}
    o, _ = match(s); iv = {}
    for b, a in o:
        e = int(b['month']); iv.setdefault(frozenset((a['sideA'], a['sideB'])), []).append((e - int(a['months']), e))
    for v in rows('warpair', s):
        q = int(v['quarter']); ge, go = int(v['monthsGe140']), int(v['gateOpenMonths'])
        if ge <= go: continue
        k = frozenset((v['nationA'], v['nationB']))
        if any(st <= q * 3 + 2 and q * 3 <= en for st, en in iv.get(k, [])): continue   # その組が交戦中の四半期は除く
        n = ge - go; closed += n
        ea, eb = ex.get((q, v['nationA'])), ex.get((q, v['nationB']))
        if ea is None or eb is None: continue
        closed_known += n
        if ea > 0.5 or eb > 0.5: closed_w += n; pq += 1
print('緊張>=140 で門が閉じ・その組は交戦していない月', closed, ' 両国に scalars の行がある月', closed_known, ' うち片方の厭戦>0.5', closed_w, '四半期', pq)
