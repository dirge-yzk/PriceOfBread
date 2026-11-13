# (1093) c104al 問い3：国家の調達（InternationalMarket.Procure の国庫の買い＝Colonial.cs:199・LastDemand と LastExport の両方に足す・InternationalMarket.cs:149・:156）の大きさ（読むだけ・近似）
# 使い方: python -X utf8 -I q3proc.py <出力> 名前=insフォルダ ...
# procF＝flow_s の kind=giveIncome・sourceFile=Colonial.cs・sourceLine=199 の amountPerMonth（sellr と同じ四半期）×fxRate（nation_s の同じ四半期）＝(1058) q23.py と同じ定義。
# Ea＝sellr の (州・財) を 1 つにした Σ prevExportAll×px（U1b の分子）／ALinc＝Σ max(0, sellRBoth−sellROld)×px（A-L の売れる量の増分）。px＝nationgood の priceMeanGold。
import sys, os, math, statistics as st
from collections import defaultdict
MAJ = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '清']
YEARS = ['1870', '1913', '1950']
OUT = sys.argv[1]; ARMS = [a.split('=', 1) for a in sys.argv[2:]]


def rd(p):
    f = open(p, encoding='utf-8', newline='')
    h = f.readline().rstrip('\r\n').split('\t')
    return f, {k: i for i, k in enumerate(h)}


def one(d, s):
    px = {}
    f, ix = rd(os.path.join(d, 'nationgood_s%d.tsv' % s))
    for ln in f:
        v = ln.rstrip('\r\n').split('\t')
        if v[0] not in YEARS: continue
        try: p = float(v[ix['priceMeanGold']])
        except ValueError: continue
        if p > 0: px[(v[0], v[1], v[2])] = p
    f.close()
    seen = set(); Ea = defaultdict(float); AL = defaultdict(float); qof = {}
    f, ix = rd(os.path.join(d, 'sellr_s%d.tsv' % s))
    for ln in f:
        v = ln.rstrip('\r\n').split('\t')
        y = v[0]
        if y not in YEARS: continue
        qof[y] = int(v[ix['q']])
        k = (y, v[ix['region']], v[ix['good']])
        if k in seen: continue
        seen.add(k)
        n = v[ix['ownerNation']]
        if n not in MAJ: continue
        p = px.get((y, n, v[ix['good']]))
        if not p: continue
        Ea[(y, n)] += float(v[ix['prevExportAll']]) * p
        AL[(y, n)] += max(0.0, float(v[ix['sellRBoth']]) - float(v[ix['sellROld']])) * p
    f.close()
    qs = {q: y for y, q in qof.items()}
    fx = {}
    f, ix = rd(os.path.join(d, 'nation_s%d.tsv' % s))
    for ln in f:
        v = ln.rstrip('\r\n').split('\t')
        q = int(v[ix['quarter']])
        if q in qs and v[ix['nation']] in MAJ: fx[(qs[q], v[ix['nation']])] = float(v[ix['fxRate']])
    f.close()
    pf = defaultdict(float); nmatch = 0
    f, ix = rd(os.path.join(d, 'flow_s%d.tsv' % s))
    for ln in f:
        v = ln.rstrip('\r\n').split('\t')
        if v[ix['kind']] != 'giveIncome' or v[ix['sourceFile']] != 'Colonial.cs' or v[ix['sourceLine']] != '199': continue
        q = int(v[ix['quarter']])
        if q in qs and v[ix['nation']] in MAJ:
            pf[(qs[q], v[ix['nation']])] += abs(float(v[ix['amountPerMonth']])); nmatch += 1
    f.close()
    res = {}
    for y in YEARS:
        T = defaultdict(float)
        for n in MAJ:
            g = fx.get((y, n), float('nan'))
            row = dict(procF=pf[(y, n)] * g, Ea=Ea[(y, n)], AL=AL[(y, n)])
            res[(y, n)] = row
            for k2, val in row.items():
                if math.isfinite(val): T[k2] += val
        res[(y, '6大国')] = dict(T)
    return res, nmatch


def med(xs):
    xs = [x for x in xs if math.isfinite(x)]
    return st.median(xs) if xs else float('nan')


def r(a, b):
    return a / b if b > 0 else float('nan')


o = open(OUT, 'w', encoding='utf-8', newline='\n')
for nm, d in ARMS:
    per = []; nm_total = 0
    for s in range(1, 11):
        if not all(os.path.exists(os.path.join(d, '%s_s%d.tsv' % (t, s))) for t in ('nationgood', 'sellr', 'nation', 'flow')): continue
        res, nmatch = one(d, s); per.append(res); nm_total += nmatch
    o.write('== %s %s 種の数 %d・Colonial.cs:199 giveIncome の一致した行（6 大国・調べる四半期）%d\n' % (nm, d, len(per), nm_total))
    o.write('year\tnation\tprocF/Ea_med\tprocF/Ea_max\tprocF/ALinc_med\tprocF/ALinc_max\tALinc/Ea_med\n')
    for y in YEARS:
        for n in MAJ + ['6大国']:
            rows = [p[(y, n)] for p in per if (y, n) in p]
            a = [r(x['procF'], x['Ea']) for x in rows]; b = [r(x['procF'], x['AL']) for x in rows]; c = [r(x['AL'], x['Ea']) for x in rows]
            fin = lambda z: [t for t in z if math.isfinite(t)]
            o.write('%s\t%s\t%.5f\t%.5f\t%.5f\t%.5f\t%.4f\n' % (y, n, med(a), max(fin(a)) if fin(a) else float('nan'), med(b), max(fin(b)) if fin(b) else float('nan'), med(c)))
    o.flush()
o.close()
