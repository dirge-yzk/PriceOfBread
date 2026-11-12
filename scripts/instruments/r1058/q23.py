# (1058) c100pq 問い2・問い3 の台本（読むだけ・近似）
# 使い方: python -X utf8 -I q23.py <出力> 名前=insフォルダ ...
# U1a・U1b は c99ov の gates_ov.py（origin/c99ov scripts/instruments/c99ov/gates_ov.py）と同じ式を年ごとに取る：
#   U1a＝6 大国 Σ crossExportQty×px ÷ Σ producedQty×px（nationgood・px＝priceMeanGold）
#   U1b＝sellr の行を（州・財）で 1 つにし 6 大国 Σ prevExportAll×px ÷ Σ lastProduced×px
# 国家の調達の価額（金建て・近似）：
#   procF＝flow_s の Colonial.cs:199 giveIncome の amountPerMonth（sellr と同じ四半期 q・国の通貨の月平均）×fxRate（nation_s の同じ q）
#        ＝国家の Procure の全て（植民地・侵略・海軍）。国営持分→国庫の分（stateCut）は入らない＝少なめ。
#   ledC＝fiscal_s の月 3q+2（四半期の 2 か月目＝sellr の PrevExport が覆う月の見込み・±1 か月の近似）の |ledgerColonialUpkeepPerMonth|×fx＝植民地の分の buy 側（LastExport）
#   wantC＝同じ月の claimColonialUpkeepPerMonth×payRateShare×fx＝植民地の分の want 側（LastDemand）
# U1b_adj＝(Σ prevExportAll×px − procF)÷Σ lastProduced×px（国家の調達の Procure は全て首都州〔w.Region〕＝国の和から引ける）
import sys, os, math, statistics as st
from collections import defaultdict
MAJ = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '清']
YEARS = ['1870', '1913', '1950']
OUT = sys.argv[1]; ARMS = [a.split('=', 1) for a in sys.argv[2:]]


def rd(p):
    f = open(p, encoding='utf-8')
    h = f.readline().rstrip('\n').split('\t')
    return f, {k: i for i, k in enumerate(h)}


def one(d, s):
    res = {}
    px = {}; P = defaultdict(float); Ec = defaultdict(float)
    f, ix = rd(os.path.join(d, 'nationgood_s%d.tsv' % s))
    for ln in f:
        v = ln.rstrip('\n').split('\t')
        y = v[0]
        if y not in YEARS: continue
        p = float(v[ix['priceMeanGold']])
        if not (p > 0): continue
        px[(y, v[1], v[2])] = p
        if v[1] in MAJ:
            P[(y, v[1])] += float(v[ix['producedQty']]) * p; Ec[(y, v[1])] += float(v[ix['crossExportQty']]) * p
    f.close()
    seen = set(); P2 = defaultdict(float); Ea = defaultdict(float); qof = {}
    f, ix = rd(os.path.join(d, 'sellr_s%d.tsv' % s))
    iy, iq, ir, inat, ig, iE, iP = ix['year'], ix['q'], ix['region'], ix['ownerNation'], ix['good'], ix['prevExportAll'], ix['lastProduced']
    for ln in f:
        v = ln.rstrip('\n').split('\t')
        y = v[iy]
        if y not in YEARS: continue
        qof[y] = int(v[iq])
        key = (y, v[ir], v[ig])
        if key in seen: continue
        seen.add(key)
        nat = v[inat]
        if nat in MAJ:
            p = px.get((y, nat, v[ig]))
            if p:
                P2[(y, nat)] += float(v[iP]) * p; Ea[(y, nat)] += float(v[iE]) * p
    f.close()
    qs = {q: y for y, q in qof.items()}
    fx = {}
    f, ix = rd(os.path.join(d, 'nation_s%d.tsv' % s))
    for ln in f:
        v = ln.rstrip('\n').split('\t')
        q = int(v[ix['quarter']])
        if q in qs and v[ix['nation']] in MAJ: fx[(qs[q], v[ix['nation']])] = float(v[ix['fxRate']])
    f.close()
    pf = defaultdict(float)
    f, ix = rd(os.path.join(d, 'flow_s%d.tsv' % s))
    for ln in f:
        v = ln.rstrip('\n').split('\t')
        if v[ix['sourceFile']] != 'Colonial.cs' or v[ix['sourceLine']] != '199' or v[ix['kind']] != 'giveIncome': continue
        q = int(v[ix['quarter']])
        if q in qs and v[ix['nation']] in MAJ: pf[(qs[q], v[ix['nation']])] += abs(float(v[ix['amountPerMonth']]))
    f.close()
    mon = {3 * q + 2: y for q, y in qs.items()}
    led = defaultdict(float); want = defaultdict(float)
    f, ix = rd(os.path.join(d, 'fiscal_s%d.tsv' % s))
    for ln in f:
        v = ln.rstrip('\n').split('\t')
        m = int(v[ix['monthIndex']])
        if m in mon and v[ix['nation']] in MAJ:
            y = mon[m]; n = v[ix['nation']]
            led[(y, n)] += abs(float(v[ix['ledgerColonialUpkeepPerMonth']]))
            want[(y, n)] += float(v[ix['claimColonialUpkeepPerMonth']]) * float(v[ix['payRateShare']])
    f.close()
    for y in YEARS:
        T = defaultdict(float)
        for n in MAJ:
            g = fx.get((y, n), float('nan'))
            row = dict(P=P[(y, n)], Ec=Ec[(y, n)], P2=P2[(y, n)], Ea=Ea[(y, n)],
                       procF=pf[(y, n)] * g, ledC=led[(y, n)] * g, wantC=want[(y, n)] * g)
            res[(y, n)] = row
            for k, val in row.items():
                if math.isfinite(val): T[k] += val
        res[(y, '6大国')] = dict(T)
    return res


def ratio(a, b):
    return a / b if b > 0 else float('nan')


def med(xs):
    xs = [x for x in xs if math.isfinite(x)]
    return st.median(xs) if xs else float('nan')


out = []
allres = {}
for nm, d in ARMS:
    ss = [s for s in range(1, 11) if all(os.path.exists(os.path.join(d, '%s_s%d.tsv' % (t, s))) for t in ('nationgood', 'sellr', 'nation', 'flow', 'fiscal'))]
    out.append('== %s %s 種 %s' % (nm, d, ss))
    per = [one(d, s) for s in ss]
    allres[nm] = per
    for y in YEARS:
        for n in MAJ + ['6大国']:
            rows = [r[(y, n)] for r in per if (y, n) in r]
            sh_all = med([ratio(r['procF'], r['Ea']) for r in rows])
            sh_cr = med([ratio(r['procF'], r['Ec']) for r in rows])
            ledsh = med([ratio(r['ledC'], r['Ea']) for r in rows])
            wsh = med([ratio(r['wantC'], r['ledC']) for r in rows])
            u1a = med([ratio(r['Ec'], r['P']) for r in rows])
            u1b = med([ratio(r['Ea'], r['P2']) for r in rows])
            u1bx = med([ratio(r['Ea'] - r['procF'], r['P2']) for r in rows])
            out.append('  %s %-6s 調達÷州の外の全て %.4f 調達÷越境 %.4f 植民地buy÷州の外 %.4f 植民地want÷buy %.2f U1a %.6f U1b %.6f U1b_adj %.6f' % (y, n, sh_all, sh_cr, ledsh, wsh, u1a, u1b, u1bx))
    for y in YEARS:
        out.append('  %s 6大国 種ごとの 調達÷州の外の全て %s' % (y, ' '.join('%.4f' % ratio(r[(y, '6大国')]['procF'], r[(y, '6大国')]['Ea']) for r in per)))

# 問い3：新÷旧（腕÷base）と幅の上端（gates_ov と同じ形：max(n1÷base, n2÷base, base÷n1, base÷n2)）を U1b と U1b_adj で
def mstat(nm, y, key):
    per = allres.get(nm)
    if per is None: return float('nan')
    if key == 'U1b': return med([ratio(r[(y, '6大国')]['Ea'], r[(y, '6大国')]['P2']) for r in per])
    if key == 'U1b_adj': return med([ratio(r[(y, '6大国')]['Ea'] - r[(y, '6大国')]['procF'], r[(y, '6大国')]['P2']) for r in per])
    if key == 'U1a': return med([ratio(r[(y, '6大国')]['Ec'], r[(y, '6大国')]['P']) for r in per])


for prefix in ('', '53'):
    b, n1, n2 = 'base' + prefix, 'n1' + prefix, 'n2' + prefix
    if b not in allres: continue
    for arm in [a for a, _ in ARMS if a not in ('base', 'n1', 'n2', 'base53', 'n153', 'n253')]:
        for y in YEARS:
            for key in ('U1a', 'U1b', 'U1b_adj'):
                vb, v1, v2, va = mstat(b, y, key), mstat(n1, y, key), mstat(n2, y, key), mstat(arm, y, key)
                top = max(v1 / vb, v2 / vb, vb / v1, vb / v2)
                out.append('問い3 %s 対 %s %s %-7s 腕 %.6f base %.6f n1 %.6f n2 %.6f 新÷旧 %.4f 幅の上端 %.4f %s' % (arm, b, y, key, va, vb, v1, v2, va / vb, top, '緑' if va / vb > top else '赤'))
with open(OUT, 'w', encoding='utf-8') as f:
    f.write('# (1058) q23.py %s\n' % ' '.join(sys.argv[2:]))
    f.write('\n'.join(out) + '\n')
print(len(out))
