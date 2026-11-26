# (1389) 問い 1：G2 の盾（今は開く門を国難が閉じる）＝緊張の開戦（門が開いていた四半期の開戦）のうち、どちらかが国難だった物。全体と 22 国の比（2.55 の取り直し）
import sys; sys.path.insert(0, 'D:/PoB_work/r1389_work/scripts')
from load import *; from align import match
t = {'starts_go': 0, 'any': 0, 'both': 0, 'anyp': 0, 'bothp': 0, 'civil': 0, 'legit': 0, 'food': 0, 'pay': 0, 'known': 0, 'closed_all': 0, 'closed_known': 0}
per_seed = []
for s in SEEDS:
    cm = crisis_map(s); sc = {(int(v['quarter']), v['nation']) for v in rows('scalars', s)}
    o, _ = match(s); iv = {}
    for b, a in o:
        e = int(b['month']); iv.setdefault(frozenset((a['sideA'], a['sideB'])), []).append((e - int(a['months']), e))
    n0 = t['starts_go']; a0 = t['any']
    for v in rows('warpair', s):
        q = int(v['quarter']); ge, go, ns = int(v['monthsGe140']), int(v['gateOpenMonths']), int(v['starts']); A, B = v['nationA'], v['nationB']
        if ge > go and not any(x <= q * 3 + 2 and q * 3 <= y for x, y in iv.get(frozenset((A, B)), [])):
            t['closed_all'] += ge - go
            if (q, A) in sc and (q, B) in sc: t['closed_known'] += ge - go
        if not (ns and go): continue
        t['starts_go'] += ns
        if (q, A) not in cm or (q, B) not in cm: continue
        t['known'] += ns; a, b = cm[(q, A)], cm[(q, B)]
        t['any'] += ns * (a[0] or b[0]); t['both'] += ns * (a[0] and b[0])
        for i, k in enumerate(('civil', 'food', 'pay', 'legit')): t[k] += ns * (a[1][i] or b[1][i])
        pa, pb = cm.get((q - 1, A), (False,)), cm.get((q - 1, B), (False,))
        t['anyp'] += ns * (pa[0] or pb[0]); t['bothp'] += ns * (pa[0] and pb[0])
    per_seed.append((t['starts_go'] - n0, t['any'] - a0))
print(t); print('種ごと（門の開いた四半期の開戦・どちらかが国難）', per_seed)
print('全体÷22 国', f"{t['closed_all']/max(1,t['closed_known']):.3f}")
