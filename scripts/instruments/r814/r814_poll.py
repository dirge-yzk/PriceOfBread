"""(814) 凍結の連続（zero_all.tsv の longestRunQ≥40）の持ち主の国の人頭税の段（nation_s の pollTaxEndShare）を、連続の始まりの 10 年前・始まり・終わりで並べる（読むだけ）。
対照＝同じ組・種の全ての国×年の pollTaxEndShare の中央。人頭税の 1 人あたりの請求＝PollTaxRate 0.15×段×Size（Fiscal.cs:801）を 1 人あたりの稼ぎと並べる。
使い方: python -I r814_poll.py <zero_all.tsv> <出力.tsv> <ラベル>=<ディレクトリ> ..."""
import sys, os, csv, collections, math
def fl(x):
    try: return float(x)
    except: return float('nan')
zp, out = sys.argv[1], sys.argv[2]; sets = dict(a.split('=', 1) for a in sys.argv[3:])
ev = [r for r in csv.DictReader(open(zp, encoding='utf-8'), delimiter='\t') if int(r['longestRunQ']) >= 40 and r['set'] in sets]
cache = {}
def nat(lab, sd):
    k = (lab, sd)
    if k in cache: return cache[k]
    P = collections.defaultdict(dict); allv = []
    with open(os.path.join(sets[lab], 'nation_s%d.tsv' % sd), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
        for line in fh:
            a = line.rstrip('\n').split('\t')
            if int(a[ix['quarter']]) % 4: continue
            v = fl(a[ix['pollTaxEndShare']]); P[a[ix['nation']]][int(a[ix['year']])] = v; allv.append(v)
    allv = sorted(x for x in allv if math.isfinite(x)); cache[k] = (P, allv[len(allv)//2] if allv else float('nan'), sum(1 for x in allv if x > 0.05) / max(1, len(allv)))
    return cache[k]
o = open(out, 'w', encoding='utf-8', newline='\n')
o.write('set\tseed\tregion\towner\trunStartYear\trunEndYear\tpoll10yBefore\tpollAtStart\tpollAtEnd\tpollClaimPerKAtStart\tallNationYearsPollMedian\tallNationYearsPollAbove005Share\n')
for r in ev:
    sd = int(r['seed']); P, m, s05 = nat(r['set'], sd); y0 = int(r['runStartYear']); y1 = int(r['runEndYear']); n = r['nationFirstZero']
    g = lambda y: P.get(n, {}).get(y, float('nan'))
    f = lambda v: format(v, '.3g') if isinstance(v, float) else str(v)
    o.write('\t'.join(f(v) for v in [r['set'], sd, r['region'], n, y0, y1, g(y0 - 10), g(y0), g(y1), 0.15 * g(y0), m, s05]) + '\n')
o.close(); print('書いた', out)
