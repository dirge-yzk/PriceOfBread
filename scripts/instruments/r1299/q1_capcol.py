# (1299) 問い1：首都の州（region_s の国ごとの最初の行＝World.Regions[0]）が植民地格（Colonial）の国×四半期を数える。
# 使い方: python -X utf8 -I q1_capcol.py <生のフォルダ> <種の最初> <種の最後> <出力の接頭辞>
# 出力: <接頭辞>_rows.tsv（国×四半期）・<接頭辞>_runs.tsv（同じ国・同じ首都の連続区間）・<接頭辞>_sum.txt
import sys, csv, os, collections

raw, s0, s1, out = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]

def rd(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r)
        ix = {k: i for i, k in enumerate(h)}
        for row in r:
            yield ix, row

rows_out = []
runs_out = []
summ = []
for s in range(s0, s1 + 1):
    rp = os.path.join(raw, f'region_s{s}.tsv'); np_ = os.path.join(raw, f'nation_s{s}.tsv')
    if not os.path.exists(rp):
        summ.append(f'seed {s}: region_s が無い'); continue
    # 国×四半期の州の列
    regs = collections.OrderedDict()   # (q, ni) -> list of (region, tier, pop)
    meta = {}
    for ix, row in rd(rp):
        q = int(row[ix['quarter']]); ni = int(row[ix['nationIndex']])
        k = (q, ni)
        if k not in regs:
            regs[k] = []; meta[k] = (row[ix['year']], row[ix['nation']])
        regs[k].append((row[ix['region']], row[ix['tier']], float(row[ix['populationK']])))
    cw = {}
    for ix, row in rd(np_):
        q = int(row[ix['quarter']]); ni = int(row[ix['nationIndex']])
        cw[(q, ni)] = int(float(row[ix['civilWarMonths']] or 0))
    # 一度でも内戦に入った国の名（区間の分類に使う）
    everCw = collections.defaultdict(list)
    for (q, ni), m in cw.items():
        if m > 0: everCw[meta.get((q, ni), ('', ''))[1]].append(q)
    firstQ = {}
    for (q, ni), (y, n) in meta.items():
        if n not in firstQ or q < firstQ[n]: firstQ[n] = q
    cnt = collections.Counter()
    hits = []
    for k, lst in regs.items():
        q, ni = k; y, n = meta[k]
        cap = lst[0]
        if cap[1] != 'Colonial': continue
        m = cw.get(k, -1)
        ncol = sum(1 for x in lst if x[1] == 'Colonial'); nhome = len(lst) - ncol
        homePop = sum(x[2] for x in lst if x[1] != 'Colonial'); totPop = sum(x[2] for x in lst)
        sov = 'cw' if m > 0 else 'sov'
        hits.append((s, q, y, ni, n, cap[0], cap[2], m, len(lst), ncol, nhome, homePop, totPop, sov, firstQ[n]))
        cnt[sov] += 1
    for h in hits: rows_out.append(h)
    # 区間
    hits.sort(key=lambda h: (h[4], h[1]))
    cur = None
    def flush(c):
        if c: runs_out.append(c)
    for h in hits:
        s_, q, y, ni, n, capn, capPop, m, nr, ncol, nhome, hp, tp, sov, fq = h
        if cur and cur['n'] == n and cur['cap'] == capn and q == cur['q1'] + 1:
            cur['q1'] = q; cur['y1'] = y; cur['len'] += 1; cur['cwq'] += (1 if sov == 'cw' else 0); cur['nhomeMax'] = max(cur['nhomeMax'], nhome); cur['nrMax'] = max(cur['nrMax'], nr)
        else:
            flush(cur)
            cur = dict(s=s_, n=n, cap=capn, q0=q, q1=q, y0=y, y1=y, len=1, cwq=(1 if sov == 'cw' else 0), nhomeMax=nhome, nrMax=nr, firstQ=fq, capPop0=capPop)
    flush(cur)
    summ.append(f'seed {s}: 首都が植民地格の国×四半期 {len(hits)}（内戦の月あり {cnt["cw"]}・内戦の月なし＝主権 {cnt["sov"]}）')

with open(out + '_rows.tsv', 'w', encoding='utf-8', newline='\n') as f:
    f.write('seed\tquarter\tyear\tnationIndex\tnation\tcapital\tcapitalPopK\tcivilWarMonths\tregions\tcolonialRegions\thomeRegions\thomePopK\ttotalPopK\tstate\tnationFirstQuarter\n')
    for h in rows_out: f.write('\t'.join(str(x) for x in h) + '\n')
with open(out + '_runs.tsv', 'w', encoding='utf-8', newline='\n') as f:
    f.write('seed\tnation\tcapital\tq0\tq1\ty0\ty1\tquarters\tcivilWarQuarters\tsovereignQuarters\thomeRegionsMax\tregionsMax\tnationFirstQuarter\tcapitalPopK0\n')
    for c in runs_out:
        f.write('\t'.join(str(x) for x in (c['s'], c['n'], c['cap'], c['q0'], c['q1'], c['y0'], c['y1'], c['len'], c['cwq'], c['len'] - c['cwq'], c['nhomeMax'], c['nrMax'], c['firstQ'], round(c['capPop0'], 1))) + '\n')
tot = len(rows_out); sov = sum(1 for h in rows_out if h[13] == 'sov')
summ.append(f'合計 {tot}（主権 {sov}・内戦中 {tot - sov}）・区間 {len(runs_out)}')
with open(out + '_sum.txt', 'w', encoding='utf-8', newline='\n') as f:
    f.write('\n'.join(summ) + '\n')
print('\n'.join(summ[-1:]))
