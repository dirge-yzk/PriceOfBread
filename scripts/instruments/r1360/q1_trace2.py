# (1360) 読む：q1_trace の型に、四半期の表（sf の与党・席・scalars・reform）を四半期の鍵で足す（大国は四半期ごと・小国は年に 1 行＝年の鍵で引く）。
#   使い方：python q1_trace2.py <ins> <種> <国の名> [前後の四半期の幅=0]
import sys, os
ins, s, nat = sys.argv[1], int(sys.argv[2]), sys.argv[3]
W = int(sys.argv[4]) if len(sys.argv) > 4 else 0
def load(t, cols):
    p = os.path.join(ins, f'{t}_s{s}.tsv'); byq = {}; byy = {}
    if not os.path.exists(p): return byq, byy
    with open(p, 'rb') as fh:
        h = fh.readline().rstrip(b'\r\n').decode('utf-8').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            c = line.rstrip(b'\r\n').decode('utf-8').split('\t')
            if c[ix['nation']] != nat: continue
            d = [c[ix[k]] if k in ix else '' for k in cols]
            byq[c[ix['quarter']]] = d; byy[c[ix['year']]] = d
    return byq, byy
T = [('sf', ['governingParty', 'seatCons', 'seatLib', 'seatSoc']), ('lrlock', ['atWarFlag', 'ruptureWindowQuarters']),
     ('scalars', ['militancyMean', 'legitimacy', 'dynMandate', 'workHoursValue']),
     ('reform', ['mandateStockIndex', 'mandateMaxIndex', 'mandateSpentNewIndex', 'concedeSpendIndex', 'wallPassAxes', 'supportShare'])]
L = [load(t, c) for t, c in T]
hdr = ['四半期', '年', '段', '政体'] + [f'{t}:{k}' for t, c in T for k in c]
print('\t'.join(hdr))
def f(x):
    try: return f'{float(x):.3f}'
    except ValueError: return x
seq = []
p = os.path.join(ins, f'monlaw_s{s}.tsv')
with open(p, 'rb') as fh:
    h = fh.readline().rstrip(b'\r\n').decode('utf-8').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for line in fh:
        c = line.rstrip(b'\r\n').decode('utf-8').split('\t')
        if c[ix['nation']] == nat: seq.append((c[ix['quarter']], c[ix['year']], int(float(c[ix['ShorterHours']])), c[ix['polityCode']]))
show = set()
for i in range(1, len(seq)):
    if seq[i][2] != seq[i - 1][2]:
        for j in range(max(0, i - 1 - W), min(len(seq), i + 1 + W)): show.add(j)
last = -2
for j in sorted(show):
    if j != last + 1: print('…')
    q, y, v, po = seq[j]
    row = [q, y, str(v), po]
    for byq, byy in L:
        d = byq.get(q) or byy.get(y) or []
        row += [f(x) for x in d] if d else []
    print('\t'.join(row)); last = j
