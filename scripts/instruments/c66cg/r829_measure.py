"""(829) 形 U の素案の物差しの比べ（記録・読みの結論の後に、事前登録 [6] の〈物差しの低さが大きい方を推奨〉のために数える）。
支える出来事の窓 W で、州の物差し 2 案（A＝costPriceLevel×localPriceMultiplier・B＝regionPriceLevel）を、同じ国・同じ四半期の全ての州の人口加重の平均で割った比の中央を出す。
使い方: python -I r829_measure.py <計器の生> <events.tsv> <出力.tsv>"""
import sys, os, statistics
raw, evp, outp = sys.argv[1], sys.argv[2], sys.argv[3]
def fl(x):
    try: return float(x)
    except: return float('nan')
ev = [d for d in (dict(zip(h, l.rstrip('\n').split('\t'))) for h in [None] for l in []) ]
lines = open(evp, encoding='utf-8').read().splitlines(); h = lines[0].split('\t')
ev = [dict(zip(h, l.split('\t'))) for l in lines[1:]]
o = open(outp, 'w', encoding='utf-8', newline='\n'); o.write('seed\tregion\tverdict\tE1\tratioA_median\tratioB_median\n')
for sd in sorted({int(e['seed']) for e in ev}):
    rows = {}
    with open(os.path.join(raw, 'cearn_s%d.tsv' % sd), encoding='utf-8') as f:
        hh = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hh)}
        for line in f:
            a = line.rstrip('\n').split('\t'); q = int(a[ix['quarter']])
            rows.setdefault(q, []).append((a[ix['nation']], a[ix['region']], fl(a[ix['popK']]), fl(a[ix['costPriceLevel']]) * fl(a[ix['localPriceMultiplier']]), fl(a[ix['regionPriceLevel']])))
    for e in [e for e in ev if int(e['seed']) == sd]:
        q0 = int(e['onsetQ']); ra, rb = [], []
        for q in range(q0 - 8, q0):
            rr = rows.get(q, []); me = [x for x in rr if x[1] == e['region']]
            if not me: continue
            nat = me[0][0]; same = [x for x in rr if x[0] == nat and x[2] > 0]
            P = sum(x[2] for x in same)
            if P <= 0: continue
            ma = sum(x[2] * x[3] for x in same) / P; mb = sum(x[2] * x[4] for x in same) / P
            ra.append(me[0][3] / ma); rb.append(me[0][4] / mb)
        o.write('%d\t%s\t%s\t%s\t%.4g\t%.4g\n' % (sd, e['region'], e['verdict'], e['E1'], statistics.median(ra) if ra else float('nan'), statistics.median(rb) if rb else float('nan')))
o.close(); print(open(outp, encoding='utf-8').read())
