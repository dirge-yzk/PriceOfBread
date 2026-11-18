# (1203) 問い② 兵の席（soldiervac_s・月ごとの州の和を四半期で束ねた物）を法の段ごと・国ごとに集計し、nation_s の州の数（植民地格）と兵の数を並べる
# nation_s の欄が空の行は数えない（0 と数えない）
# 使い方: python -X utf8 -I svac.py <生> <種0> <種1> <出力> [国名,国名...]
import sys, csv, collections
raw, s0, s1, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
focus = sys.argv[5].split(',') if len(sys.argv) > 5 else []
F = ['regions','zeroStart','zeroVac','zeroVacColonial','zeroFilled','fillZeroK','vacZeroK','overfillZeroK','fillPosK']
def num(x):
    try: return float(x)
    except (TypeError, ValueError): return None   # 欄がずれた行・空の行は数えない
agg = collections.defaultdict(lambda: collections.Counter())
for s in range(s0, s1 + 1):
    nat = {}
    with open(f'{raw}/nation_s{s}.tsv', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            nat[(r['year'], r['nation'], r['quarter'])] = r
    natq = {}
    for (y, n, q), r in nat.items(): natq.setdefault((n, int(q)), r)
    with open(f'{raw}/soldiervac_s{s}.tsv', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            n = r['nation']; law = r['lawCode']; war = r['atWarFlag']
            nr = natq.get((n, int(r['q'])))
            keys = [('law', law, war)] + ([('nat', n, law)] if n in focus else [])
            for k in keys:
                c = agg[k]; c['rows'] += 1
                for x in F: c[x] += float(r[x])
                v = [num(nr.get(x)) for x in ('coreRegions','integratedRegions','colonialRegions','soldierK','populationK')] if nr else [None]
                if all(x is not None for x in v):
                    c['colonialRegions'] += v[2]; c['allRegions'] += v[0] + v[1] + v[2]
                    c['soldierK'] += float(nr['soldierK']); c['popK'] += float(nr['populationK']); c['nrows'] += 1
with open(outp, 'w', encoding='utf-8', newline='\n') as o:
    o.write('kind\tkey\tlaw\trows\tregionsPerRow\tallRegionsPerRow\tcolonialRegionsPerRow\tzeroStartPerRow\tzeroVacPerRow\tzeroVacColonialPerRow\tzeroFilledShareOfZeroVac\tfillZeroK_over_vacZeroK\tsoldier_per_pop\n')
    for k in sorted(agg):
        c = agg[k]; R = c['rows']; N = max(1, c['nrows'])
        o.write(f"{k[0]}\t{k[1]}\t{k[2]}\t{R}\t{c['regions']/R:.2f}\t{c['allRegions']/N:.2f}\t{c['colonialRegions']/N:.2f}\t{c['zeroStart']/R:.2f}\t{c['zeroVac']/R:.2f}\t{c['zeroVacColonial']/R:.2f}\t{(c['zeroFilled']/c['zeroVac'] if c['zeroVac'] else float('nan')):.3f}\t{(c['fillZeroK']/c['vacZeroK'] if c['vacZeroK'] else float('nan')):.3f}\t{(c['soldierK']/c['popK'] if c['popK'] else float('nan')):.4f}\n")
