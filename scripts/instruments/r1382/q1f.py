# (1382) 問い 1 の続き：平時の厭戦>0.5 だけで閉じていた組の〈続いた塊〉（6 か月以上＝門が開けば時計が満ちた見込み）の数・交戦中の側の塊も別に
import sys; sys.path.insert(0, 'D:/PoB_work/r1382_work/scripts')
from load import *; from align import match
ep = {'peace': 0, 'war': 0}
for s in SEEDS:
    sc = {(int(v['quarter']), v['nation']): (fl(v['warExhaustion']), fl(v['atWar'])) for v in rows('scalars', s)}
    o, _ = match(s); iv = {}
    for b, a in o:
        e = int(b['month']); iv.setdefault(frozenset((a['sideA'], a['sideB'])), []).append((e - int(a['months']), e))
    runs = {}
    for v in rows('warpair', s):
        q = int(v['quarter']); ge, go = int(v['monthsGe140']), int(v['gateOpenMonths']); k = frozenset((v['nationA'], v['nationB']))
        kind = None
        if ge > go and not any(st <= q * 3 + 2 and q * 3 <= en for st, en in iv.get(k, [])):
            A, B = sc.get((q, v['nationA'])), sc.get((q, v['nationB']))
            if A and B:
                w = [x for x in (A, B) if x[0] > 0.5]
                if w: kind = 'war' if any(x[1] > 0 for x in w) else 'peace'
        r = runs.get(k)
        if kind and r and r[0] == kind and r[1] == q - 1: runs[k] = (kind, q, r[2] + ge - go)
        else:
            if r and r[2] >= 6: ep[r[0]] += 1
            runs[k] = (kind, q, ge - go) if kind else None
    for r in runs.values():
        if r and r[2] >= 6: ep[r[0]] += 1
print('6 か月以上続いた塊（scalars のある 22 国どうし・10 種）', ep)
