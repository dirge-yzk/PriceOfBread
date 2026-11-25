# (1360) c143hr 読む（コミットしない・門にしない・エンジンの関数を呼ばない）：
#   1 つの（種・国）の時短（ShorterHours）の段が動いた四半期ごとに、同じ四半期の盤面（monlaw・lrlock・parlseat）と
#   その年の年末の断面（scalars・reform＝小国は年に 1 行）を並べる。
#   使い方：python q1_trace.py <ins> <種> <国の名>
import sys, os
ins, s, nat = sys.argv[1], int(sys.argv[2]), sys.argv[3]
def rows(t):
    p = os.path.join(ins, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return
    with open(p, 'rb') as fh:
        h = fh.readline().rstrip(b'\r\n').decode('utf-8').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            c = line.rstrip(b'\r\n').decode('utf-8').split('\t')
            if c[ix['nation']] == nat: yield ix, c
lr = {c[ix['quarter']]: (c[ix['atWarFlag']], c[ix['ruptureWindowQuarters']]) for ix, c in rows('lrlock')}
ps = {c[ix['quarter']]: (c[ix['parlHeadCode']], c[ix['seatSumEnd']], c[ix['seatConsEnd']]) for ix, c in rows('parlseat')}
sc = {}
for ix, c in rows('scalars'):
    sc[c[ix['year']]] = {k: c[ix[k]] for k in ('militancyMean', 'legitimacy', 'dynMandate', 'workHoursValue', 'atWar', 'polityCode')}
rf = {}
for ix, c in rows('reform'):
    rf[c[ix['year']]] = {k: c[ix[k]] for k in ('mandateStockIndex', 'mandateMaxIndex', 'mandateSpentNewIndex', 'concedeSpendIndex', 'wallPassAxes', 'supportShare', 'blockedLawCount')}
def f(x):
    try: return f'{float(x):.3f}'
    except ValueError: return x
prev = None; pol = None; n = 0
print('四半期\t年\t季\t段 前→後\t政体\t戦時\t断絶の窓\t議会の頭\t席の和\t保守の席\t年末の闘争度\t正統性\t委任\t実労働時間\t政治資本の在庫\t上限\t新たな支出\t譲歩の支出\t壁を越えた軸\t後退の通し番号')
for ix, c in rows('monlaw'):
    q = c[ix['quarter']]; y = c[ix['year']]; v = int(float(c[ix['ShorterHours']])); po = c[ix['polityCode']]
    if prev is not None and v != prev:
        if v > prev: n += 1
        a = lr.get(q, ('', '')); b = ps.get(q, ('', '', '')); d = sc.get(y, {}); e = rf.get(y, {})
        print('\t'.join([q, y, str(int(q) % 4), f'{prev}→{v}', (po if po == pol else f'{pol}→{po}'), a[0], a[1], b[0], f(b[1]), f(b[2]),
                         f(d.get('militancyMean', '')), f(d.get('legitimacy', '')), f(d.get('dynMandate', '')), f(d.get('workHoursValue', '')),
                         f(e.get('mandateStockIndex', '')), f(e.get('mandateMaxIndex', '')), f(e.get('mandateSpentNewIndex', '')), f(e.get('concedeSpendIndex', '')),
                         e.get('wallPassAxes', ''), str(n) if v > prev else '']))
    prev = v; pol = po
