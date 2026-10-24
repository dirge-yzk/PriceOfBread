# (533) 止める線 ③ 片方向の錠（pf2 の読みなおし・反証役 pf2〔bv〕中#1）：法の段の張り付き＝国×四半期の行の数。
#   nation_s の serfEmancipation（全ての国）と reform_s の landReformLawCode・serfLawCode（挙げた 9 国）。
#   鳴る＝B がヌル腕（A・N1・N2）の最大の 2 倍以上かつ差が全行の 0.5% 以上（ar/stop.py の ⑥③ と同じ作法）。
import csv, glob, os, sys
def rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for x in r:
            if len(x) == len(h): yield dict(zip(h, x))
def count(d, tab, col, pred, nat=None):
    n = tot = 0
    for p in glob.glob(os.path.join(d, f'{tab}_s*.tsv')):
        for x in rows(p):
            if nat and x.get('nation') != nat: continue
            try: v = float(x[col])
            except (KeyError, ValueError): continue
            tot += 1
            if pred(v): n += 1
    return n, tot
arms = [a.split('=', 1) for a in sys.argv[1:]]
tests = [('nation', 'serfEmancipation', '==0（なし）', lambda v: v == 0, None),
         ('nation', 'serfEmancipation', '<=1（なし・限定的）', lambda v: v <= 1, None),
         ('reform', 'serfLawCode', '==0', lambda v: v == 0, None),
         ('reform', 'landReformLawCode', '==0', lambda v: v == 0, None),
         ('reform', 'landReformLawCode', '==2（最大）', lambda v: v == 2, None),
         ('reform', 'serfLawCode', '==0（ロシア）', lambda v: v == 0, 'ロシア'),
         ('reform', 'landReformLawCode', '==0（日本）', lambda v: v == 0, '日本')]
for tab, col, lab, pr, nat in tests:
    res = [(k, *count(d, tab, col, pr, nat)) for k, d in arms]
    s = '  '.join(f'{k}={n}/{t}' for k, n, t in res)
    null = max(n for k, n, t in res[:-1]); b = res[-1][1]; tot = max(1, res[-1][2])
    ring = b >= 2 * null and (b - null) >= 0.005 * tot
    print(f'{tab}.{col} {lab}: {s}  → {"鳴る" if ring else "鳴らない"}')
