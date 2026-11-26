import sys; sys.path.insert(0, 'D:/PoB_work/r1382_work/scripts')
from load import *
def match(s):
    w = list(rows('wars', s)); c = list(rows('cpmed', s)); used = set(); out = []
    for b in c:
        key = frozenset((b['sideA'], b['sideB'])); m = int(b['month']); hit = None
        for i, a in enumerate(w):
            if i in used or frozenset((a['sideA'], a['sideB'])) != key or a['civilWarFlag'] != b['civilFlag']: continue
            st = m - int(a['months'])
            if abs((1783 + st // 12) - int(a['startYear'])) <= 1: hit = i; break
        if hit is not None: used.add(hit)
        out.append((b, w[hit] if hit is not None else None))
    return out, len(w)
if __name__ == '__main__':
    tm = tn = tw = 0
    for s in SEEDS:
        o, nw = match(s); tm += sum(1 for _, a in o if a); tn += len(o); tw += nw
    print('cpmed', tn, 'matched', tm, 'wars', tw)
