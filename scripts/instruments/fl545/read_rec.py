# (545) 線 fl の記録 R1(差替)・R6(国別の人口)・R8 の読み手（採否に使わない）
# 使い方：python read_rec.py <基線の ins> <腕 B の ins> [国,国,...]  既定＝分類A+Bの43国のうち主な物
import sys, csv, os, statistics
sys.stdout.reconfigure(encoding='utf-8'); csv.field_size_limit(1<<30)
base, arm = sys.argv[1], sys.argv[2]
NS = sys.argv[3].split(',') if len(sys.argv) > 3 and sys.argv[3] != '-' else ['ポーランド','デンマーク','両シチリア','ビルマ','シャム','マラータ同盟','ハイデラバード','スイス','エチオピア','バイエルン','シク連邦','朝鮮']
def rd(d, t, s):
    with open(os.path.join(d, '%s_s%d.tsv' % (t, s)), encoding='utf-8', newline='') as f:
        return list(csv.DictReader(f, delimiter='\t'))
def f(x):
    try: return float(x)
    except Exception: return float('nan')
def popAt(rows, n, y):
    v = [f(r['populationK']) for r in rows if r['nation'] == n and r['year'] == str(y)]
    return v[-1] if v else float('nan')
def fam(rows, n):
    return sum(f(r['deathsFamineK']) for r in rows if r['scope'] == 'N' and r['nation'] == n and 1783 <= int(r['year']) <= 1800)
def fac(rows, n, y):
    v = [f(r['factoryCount']) for r in rows if r['nation'] == n and r['year'] == str(y)]
    return v[-1] if v else float('nan')
def med(x):
    x = [a for a in x if a == a]
    return statistics.median(x) if x else float('nan')
YS = (1800, 1850, 1914, 1950)
S0 = int(sys.argv[4]) if len(sys.argv) > 4 else 1
RP = {n: {y: [] for y in YS} for n in NS}; RF = {n: [0.0, 0.0] for n in NS}; RFA = {n: {y: [0, 0] for y in (1793, 1850)} for n in NS}
for s in range(S0, S0 + 10):
    nb, na = rd(base, 'nation', s), rd(arm, 'nation', s)
    pb, pa = rd(base, 'pop', s), rd(arm, 'pop', s)
    fb, fa = rd(base, 'factory', s), rd(arm, 'factory', s)
    for n in NS:
        for y in YS:
            b, a = popAt(nb, n, y), popAt(na, n, y)
            if b == b and a == a and b > 0: RP[n][y].append(a / b)
        RF[n][0] += fam(pb, n); RF[n][1] += fam(pa, n)
        for y in RFA[n]:
            b, a = fac(fb, n, y), fac(fa, n, y)
            if b == b and a == a: RFA[n][y][0] += b; RFA[n][y][1] += a
for n in NS:
    print('%-8s 人口B/基線(中央値) ' % n + ' '.join('%d:%.3f' % (y, med(RP[n][y])) for y in YS) +
          ' | 飢饉の死者1783-1800(千・10種和) 基線%.1f B%.1f' % tuple(RF[n]) +
          ' | 工場数(10種和) ' + ' '.join('%d:基線%d→B%d' % (y, RFA[n][y][0], RFA[n][y][1]) for y in RFA[n]))
