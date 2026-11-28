# (1437) 問い1 の 2 枚目：内陸（portFlag 0）×1900 年に本国格（Core・Integrated）の州の、線路の止まり方（読むだけ）
# 入力：<raw>/trrail_s*.tsv・trstat_s*.tsv・<out>/tier1900.tsv（region_s の 1900 年の最初の四半期の tier）
# 出力：<out>/q1b_core.tsv（種×州）・q1b_pick.txt（名簿）
# 名簿の条件：1850〜1950 の needEnd>0 の年の railTrackEnd÷needEnd の中央値 <0.25 か、1950 年の線路が最大の 1/4 未満（敷いてから止まった型）。
import sys, csv, statistics as st, collections
raw, out = sys.argv[1], sys.argv[2]
port = {}
for s in range(1, 11):
    with open(f"{raw}/trstat_s{s}.tsv", encoding='utf-8') as f:
        for ln in f:
            c = ln.split('\t')
            if c[0] == 'G': port[c[3]] = max(port.get(c[3], 0), int(c[11] or 0))
tier = {}
with open(f"{out}/tier1900.tsv", encoding='utf-8') as f:
    for ln in f:
        s, n, rg, t, pop = ln.rstrip('\n').split('\t'); tier[(int(s), rg)] = (n, t, float(pop))
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except Exception: return 0.0
rows = []
for s in range(1, 11):
    with open(f"{raw}/trrail_s{s}.tsv", newline='', encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        acc = collections.defaultdict(lambda: collections.defaultdict(float))
        rat = collections.defaultdict(list); peak = collections.defaultdict(lambda: (0.0, 0)); end = {}
        for row in rd:
            y = int(row[ix['year']]); rg = row[ix['region']]
            if y < 1850: continue
            need = fl(row[ix['needEnd']]); tr = fl(row[ix['railTrackEnd']])
            if need > 0: rat[rg].append(tr / need); acc[rg]['sz'] += fl(row[ix['procureStockZeroGoodMonths']]); acc[rg]['n'] += 1
            if tr > peak[rg][0]: peak[rg] = (tr, y)
            if y == 1950: end[rg] = (tr, need)
            for k in ('procureWantUnitsPerYear', 'procureGotUnitsPerYear', 'procureSavingsBindGoodMonths', 'natNoSellerStockGoodMonths'): acc[rg][k] += fl(row[ix[k]])
            for k in ('natWant', 'natGot', 'natCalls'):
                for b in ('Low', 'Mid', 'High'): acc[rg][k] += fl(row[ix[k + 'Phi' + b]])
        for rg, a in acc.items():
            if a['n'] < 20 or port.get(rg, 1) != 0: continue
            n, t, pop = tier.get((s, rg), ('', '', 0.0))
            if t not in ('Core', 'Integrated'): continue
            e = end.get(rg, (0.0, 0.0))
            rows.append(dict(seed=s, nation=n, region=rg, tier=t, pop1900K=pop, med=st.median(rat[rg]), peak=peak[rg][0], peakY=peak[rg][1], end=e[0], needEnd=e[1],
                             szPerYr=a['sz'] / a['n'], want=a['procureWantUnitsPerYear'], got=a['procureGotUnitsPerYear'], natW=a['natWant'], natG=a['natGot'], natCalls=a['natCalls'],
                             savPerYr=a['procureSavingsBindGoodMonths'] / a['n'], noSellerPerYr=a['natNoSellerStockGoodMonths'] / a['n']))
keys = list(rows[0].keys())
with open(f"{out}/q1b_core.tsv", 'w', encoding='utf-8', newline='') as f:
    f.write('\t'.join(keys) + '\n')
    for r in rows: f.write('\t'.join(f"{r[k]:.4g}" if isinstance(r[k], float) else str(r[k]) for k in keys) + '\n')
hit = collections.defaultdict(list)
for r in rows:
    stopped = r['peak'] > 0 and r['end'] < 0.25 * r['peak']
    if r['med'] < 0.25 or stopped: hit[r['region']].append((r, stopped))
pick = sorted(hit.items(), key=lambda kv: (-len(kv[1]), st.median([x[0]['med'] for x in kv[1]])))
with open(f"{out}/q1b_pick.txt", 'w', encoding='utf-8') as f:
    f.write(f"内陸×本国格の州×種の行 {len(rows)}・州 {len(set(r['region'] for r in rows))}・条件に当たる州 {len(hit)}\n")
    f.write("region\tnations\tseedsHit\tseeds(停=敷いてから1/4未満)\tmedRatio\tmedPop1900K\tmedStockZeroPerYr(24=満)\tgot/want\tnatGot/natWant\tnatCalls\tsavBindPerYr\n")
    for rg, lst in pick[:40]:
        W = sum(x[0]['want'] for x in lst); G = sum(x[0]['got'] for x in lst); NW = sum(x[0]['natW'] for x in lst); NG = sum(x[0]['natG'] for x in lst)
        f.write(f"{rg}\t{'/'.join(sorted(set(x[0]['nation'] for x in lst)))}\t{len(lst)}\t{','.join(str(x[0]['seed']) + ('停' if x[1] else '') for x in lst)}\t{st.median([x[0]['med'] for x in lst]):.3f}\t{st.median([x[0]['pop1900K'] for x in lst]):.0f}\t{st.median([x[0]['szPerYr'] for x in lst]):.1f}\t{G / W if W > 0 else float('nan'):.3f}\t{NG / NW if NW > 0 else float('nan'):.3f}\t{sum(x[0]['natCalls'] for x in lst):.0f}\t{st.median([x[0]['savPerYr'] for x in lst]):.1f}\n")
print('ok', len(rows), len(hit))
