# (1381) 読む線：バーミンガムの年の平均（region・merch・subst・popclose の抜き出しから）。使い方 python q1_annual.py <作業フォルダ> <出力>
import sys, collections
w, out = sys.argv[1], sys.argv[2]
def load(fn, cols):
    rows = {}
    with open('%s/%s' % (w, fn), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); I = {k: i for i, k in enumerate(h)}
        for l in fh:
            c = l.rstrip('\n').split('\t'); q = int(c[0])
            if q in rows: continue
            rows[q] = {k: float(c[I[k]]) if c[I[k]] not in ('', 'NaN') else 0.0 for k in cols}; rows[q]['year'] = int(c[I['year']])
    return rows
R = load('region_bham_s2.tsv', ['populationK', 'ruralPopulationK', 'factoryOutputRefValuePerMonth', 'foodFillShare', 'migrationOutKPerQuarter', 'employmentShare', 'wageFactor'])
M = load('merch_bham_s2.tsv', ['workerK', 'merchantK', 'artisanK', 'workerIncomePerCap'])
S = load('subst_bham_s2.tsv', ['subPopK'])
P = load('popclose_bham_s2.tsv', ['birthsK', 'deathsNormalK', 'deathsFamineK', 'migrationOutK', 'migrationInK'])
A = collections.defaultdict(lambda: collections.defaultdict(list))
for q, r in R.items():
    y = r['year']
    for k, v in r.items(): A[y][k].append(v)
    for src in (M, S, P):
        if q in src:
            for k, v in src[q].items():
                if k != 'year': A[y][k].append(v)
cols = ['populationK', 'ruralPopulationK', 'workerK', 'merchantK', 'artisanK', 'subPopK', 'factoryOutputRefValuePerMonth', 'foutPerWorker', 'foodFillShare', 'employmentShare', 'workerIncomePerCap', 'migrationOutK', 'migrationInK', 'birthsK', 'deathsNormalK', 'deathsFamineK']
with open(out, 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('year\t' + '\t'.join(cols) + '\n')
    for y in sorted(A):
        a = A[y]; m = lambda k: sum(a[k]) / len(a[k]) if a[k] else 0.0; s = lambda k: sum(a[k])
        row = []
        for k in cols:
            if k == 'foutPerWorker': row.append('%.2f' % (m('factoryOutputRefValuePerMonth') / max(1e-9, m('workerK'))))
            elif k in ('migrationOutK', 'migrationInK', 'birthsK', 'deathsNormalK', 'deathsFamineK'): row.append('%.1f' % s(k))
            else: row.append('%.2f' % m(k))
        fo.write('%d\t' % y + '\t'.join(row) + '\n')
