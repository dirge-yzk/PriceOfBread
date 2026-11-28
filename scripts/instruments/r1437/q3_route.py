# (1437) 問い3 (b) と問い4（輪の疑い ②）：名簿の州×種を 10 年ごとに追う（読むだけ）
# region_s（四半期）：その年の 4 四半期の平均＝populationK・regionShareEnd（清算の容量と船の取り分＝LastTradePotential÷Σ）・gdpOutputShareEnd（線路の need の取り分）・
#   transportCapEnd（r.LastTransportCap）・tradeThrottleEnd（min(1,cap÷potential)）。
# trroute（10 年ごとの年×売り手と買い手の州の組・財の列は無い）：買い手として入った組の数・成約の数・重さ・荷価、うち同じ国の売り手の分。
# 国の中の比べ：同じ年・同じ国の州の regionShare÷gdpOutputShare の中央値。
# 出力：<out>/<tag>_q3_track.tsv（名簿の州×種×10 年）・<out>/<tag>_q3_sum.txt（型ごとの要約）
import sys, csv, collections, statistics as st
raw, out, tag = sys.argv[1], sys.argv[2], sys.argv[3]
roster = {}
with open(f"{out}/q1c_rows.tsv", encoding='utf-8') as f:
    for r in csv.DictReader(f, delimiter='\t'): roster[(int(r['seed']), r['region'])] = r['tier']
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except Exception: return 0.0
YEARS = list(range(1850, 1951, 10))
rows = []
for s in range(1, 11):
    mine = {rg for (ss, rg) in roster if ss == s}
    if not mine: continue
    reg = collections.defaultdict(lambda: collections.defaultdict(float)); natOf = {}
    with open(f"{raw}/region_s{s}.tsv", newline='', encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        for row in rd:
            y = int(row[ix['year']])
            if y not in YEARS: continue
            rg = row[ix['region']]; n = row[ix['nation']]
            a = reg[(y, n, rg)]; a['q'] += 1
            for k in ('populationK', 'regionShareEnd', 'gdpOutputShareEnd', 'transportCapEnd', 'tradeThrottleEnd'): a[k] += fl(row[ix[k]])
    ratioNat = collections.defaultdict(list); where = {}
    for (y, n, rg), a in reg.items():
        q = a['q']
        if a['gdpOutputShareEnd'] > 0: ratioNat[(y, n)].append((a['regionShareEnd'] / q) / (a['gdpOutputShareEnd'] / q))
        if rg in mine: where[(y, rg)] = (n, {k: v / q for k, v in a.items() if k != 'q'})
    tro = collections.defaultdict(lambda: collections.defaultdict(float))
    with open(f"{raw}/trroute_s{s}.tsv", newline='', encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        for row in rd:
            y = int(row[ix['year']])
            if y not in YEARS: continue
            b = row[ix['buyerRegion']]; sl = row[ix['sellerRegion']]
            for side, rg, other in (('in', b, row[ix['sellerNation']]), ('out', sl, row[ix['buyerNation']])):
                if rg not in mine: continue
                own = row[ix['buyerNation']] if side == 'in' else row[ix['sellerNation']]
                t = tro[(y, rg)]; t[side + 'Pairs'] += 1; t[side + 'Deals'] += fl(row[ix['dealsCountPerYear']]); t[side + 'Wt'] += fl(row[ix['weightUnitsPerYear']]); t[side + 'Val'] += fl(row[ix['valueGoldPerYear']])
                if other == own: t[side + 'WtDom'] += fl(row[ix['weightUnitsPerYear']]); t[side + 'PairsDom'] += 1
                t[side + 'Freight'] += fl(row[ix['freightGoldPerYear']])
    for rg in mine:
        for y in YEARS:
            if (y, rg) not in where: continue
            n, a = where[(y, rg)]; t = tro[(y, rg)]
            med = st.median(ratioNat[(y, n)]) if ratioNat[(y, n)] else float('nan')
            rs, gs = a['regionShareEnd'], a['gdpOutputShareEnd']
            rows.append(dict(seed=s, region=rg, tier=roster[(s, rg)], year=y, nation=n, popK=a['populationK'], regionShare=rs, gdpOutShare=gs, shareRatio=rs / gs if gs > 0 else float('nan'), natMedShareRatio=med,
                             cap=a['transportCapEnd'], throttle=a['tradeThrottleEnd'], inPairs=t['inPairs'], inPairsDom=t['inPairsDom'], inDeals=t['inDeals'], inWt=t['inWt'], inWtDom=t['inWtDom'], inVal=t['inVal'],
                             outPairs=t['outPairs'], outWt=t['outWt'], wtInPerCap=t['inWt'] / a['transportCapEnd'] if a['transportCapEnd'] > 0 else float('nan')))
keys = list(rows[0].keys())
with open(f"{out}/{tag}_q3_track.tsv", 'w', encoding='utf-8', newline='') as f:
    f.write('\t'.join(keys) + '\n')
    for r in rows: f.write('\t'.join(f"{r[k]:.4g}" if isinstance(r[k], float) else str(r[k]) for k in keys) + '\n')
with open(f"{out}/{tag}_q3_sum.txt", 'w', encoding='utf-8') as f:
    f.write("型\t年\t州×種\tshareRatio の中央\t国の中央の中央\tthrottle の中央\t買い手の組 0 の割合\t買い手の組の中央\t同じ国の売り手の組の中央\t入った重さ÷cap の中央（年の重さ÷四半期末の cap）\n")
    for tierLab, cond in (('本国格', lambda t: t in ('Core', 'Integrated')), ('植民地格', lambda t: t == 'Colonial')):
        for y in YEARS:
            L = [r for r in rows if r['year'] == y and cond(r['tier'])]
            if not L: continue
            m = lambda k: st.median([r[k] for r in L if r[k] == r[k]]) if any(r[k] == r[k] for r in L) else float('nan')
            f.write(f"{tierLab}\t{y}\t{len(L)}\t{m('shareRatio'):.3f}\t{m('natMedShareRatio'):.3f}\t{m('throttle'):.3f}\t{sum(1 for r in L if r['inPairs'] == 0) / len(L):.2f}\t{m('inPairs'):.0f}\t{m('inPairsDom'):.0f}\t{m('wtInPerCap'):.3f}\n")
print('ok', len(rows))
