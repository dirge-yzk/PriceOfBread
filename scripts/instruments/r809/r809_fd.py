"""(809) c13hl：事象の州の主食（fd_s）と疫病（epi_s）の 10 年ごとの時系列（読むだけ）。
使い方: python -I r809_fd.py <出力.tsv> <ラベル>|<ディレクトリ>|<種>|<州> ...
列：充足 0.8 未満の人口の割合（belowNewK÷popK）・新しい台帳の充足（fillNew）・収穫の平均・自給の割合・
    充足 0.8 未満の人口に (143) の飢えの超過死亡の式をその州の平均の充足で当てた率（下限の見積り＝州の中の POP の散らばりを見ない）・疫病の死（千人／10 年）。"""
import sys, os, collections, math
def fl(x):
    try: return float(x)
    except: return float('nan')
out = sys.argv[1]
reqs = [a.split('|', 3) for a in sys.argv[2:]]
o = open(out, 'w', encoding='utf-8', newline='\n')
o.write('set\tseed\tregion\tdecade\tnation\tpopK\tbelowShare\tfillNew\tharvest\tsubsistence\tformulaExcessPerMilleYr_atRegionMeanFill\tepiK_10y\n')
for lab, d, sd, reg in reqs:
    sd = int(sd)
    g = collections.defaultdict(list)
    p = os.path.join(d, 'fd_s%d.tsv' % sd)
    if os.path.exists(p):
        with open(p, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for line in fh:
                a = line.rstrip('\n').split('\t')
                if a[ix['region']] != reg: continue
                y = int(a[ix['year']]) // 10 * 10
                g[y].append((a[ix['nation']], fl(a[ix['popK']]), fl(a[ix['belowNewK']]), fl(a[ix['fillNew']]), fl(a[ix['harvestMean']]), fl(a[ix['subsistenceMean']])))
    ep = collections.defaultdict(float)
    pe = os.path.join(d, 'epi_s%d.tsv' % sd)
    with open(pe, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            a = line.rstrip('\n').split('\t')
            if a[ix['region']] == reg: ep[int(a[ix['year']]) // 10 * 10] += fl(a[ix['deadK']])
    for y in sorted(set(g) | set(ep)):
        s = g.get(y, [])
        if s:
            pop = sum(x[1] for x in s) / len(s); bl = sum(x[2] for x in s) / max(1e-9, sum(x[1] for x in s)); fn = sum(x[3] for x in s) / len(s)
            hv = sum(x[4] for x in s) / len(s); sb = sum(x[5] for x in s) / len(s)
            fe = 0.9 * ((0.8 - fn) / 0.8) ** 4 * 1000 if fn < 0.8 else 0.0
            o.write(f'{lab}\t{sd}\t{reg}\t{y}\t{s[-1][0]}\t{pop:.4g}\t{bl:.3f}\t{fn:.3f}\t{hv:.3f}\t{sb:.3f}\t{fe:.3g}\t{ep.get(y, 0):.3g}\n')
        else:
            o.write(f'{lab}\t{sd}\t{reg}\t{y}\t-\tnan\tnan\tnan\tnan\tnan\tnan\t{ep.get(y, 0):.3g}\n')
o.close(); print('書いた', out)
