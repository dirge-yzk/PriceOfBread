# (1101) c105s08 S08 輸送と地理の頻度（読むだけ）。呼び方：python -X utf8 -I freq.py <腕の名> <生のフォルダ> <出力>
# 表ごとの母集合は prereg.txt の追記 1 のとおり。財の番号は読まない。
import csv, sys, os, statistics as st
from collections import defaultdict

arm, root, outp = sys.argv[1], sys.argv[2], sys.argv[3]
csv.field_size_limit(10**8)
seeds = sorted({int(f.split('_s')[1].split('.')[0]) for f in os.listdir(root) if f.startswith('trbin_s')})
out = open(outp, 'w', encoding='utf-8', newline='\n')

def P(*a):
    out.write('\t'.join(str(x) for x in a) + '\n'); out.flush()

def rows(t, s):
    p = os.path.join(root, f'{t}_s{s}.tsv')
    if not os.path.exists(p):
        return None, None
    f = open(p, encoding='utf-8', newline='')
    r = csv.reader(f, delimiter='\t')
    h = next(r)
    return {k: i for i, k in enumerate(h)}, r

def fl(x):
    try:
        return float(x)
    except ValueError:
        return float('nan')

def pct(a, b):
    return f'{100.0 * a / b:.2f}%' if b else 'n/a'

P('arm', arm, 'seeds', ','.join(map(str, seeds)))

# H002 陸直行・H001 は trroute
tot = defaultdict(float)
seaKmOnLand = 0.0
for s in seeds:
    h, r = rows('trbin', s)
    for row in r:
        rc, cb = row[h['routeCode']], row[h['crossBorderFlag']]
        v = fl(row[h['valueGoldPerYear']])
        tot[(rc, cb)] += v
        y = int(row[h['year']])
        tot[(rc, cb, 'pre1850' if y < 1850 else 'post1850')] += v
        if rc == '0':
            seaKmOnLand += fl(row[h['weightKmSeaRealPerYear']])
for cb in ('0', '1'):
    a, b = tot[('0', cb)], tot[('0', cb)] + tot[('1', cb)]
    P('H002', f'landDirectValueShare_cross{cb}', pct(a, b))
    for era in ('pre1850', 'post1850'):
        a, b = tot[('0', cb, era)], tot[('0', cb, era)] + tot[('1', cb, era)]
        P('H002', f'landDirectValueShare_cross{cb}_{era}', pct(a, b))
P('H002', 'check_routeCode0_seaKmSum', seaKmOnLand)

# trroute：H001・H003・H004
seaV = seaVland = 0.0; allV = terrV = 0.0
ratio = defaultdict(float); terrCode = defaultdict(float)
landV = landVmid = 0.0
for s in seeds:
    h, r = rows('trroute', s)
    for row in r:
        if row[h['sellerRegion']].startswith('('):
            continue
        v = fl(row[h['valueGoldPerYear']]); allV += v
        rc = row[h['routeCode']]
        if rc == '1':
            seaV += v
            if fl(row[h['realLandKm']]) > 0: seaVland += v
        else:
            landV += v
        ts, tb = fl(row[h['terrainCostSeller']]), fl(row[h['terrainCostBuyer']])
        if abs(ts - 1) > 1e-9 or abs(tb - 1) > 1e-9: terrV += v
        ratio[round(fl(row[h['landSeaRatioSeller']]), 4)] += v
        terrCode[row[h['sellerTerrainCode']]] += v
P('H001', 'seaRouteValueShareWithLandLeg', pct(seaVland, seaV))
P('H003', 'valueShareTerrainCostNot1', pct(terrV, allV))
P('H003', 'sellerTerrainCodeValueShare', ' '.join(f'{k}:{pct(v, allV)}' for k, v in sorted(terrCode.items())))
P('H004', 'landSeaRatioSellerValueShare', ' '.join(f'{k}:{pct(v, allV)}' for k, v in sorted(ratio.items())))

# trnation：H017・H035
rail = 0; ny = 0; firstRail = {}; recv = defaultdict(float); recvPos = 0
for s in seeds:
    h, r = rows('trnation', s)
    for row in r:
        ny += 1
        n, y = row[h['nation']], int(row[h['year']])
        if row[h['railGateFlag']] == '1':
            rail += 1
            k = (s, n)
            if k not in firstRail or y < firstRail[k]: firstRail[k] = y
        fr = fl(row[h['freightReceivedGoldPerYear']])
        if fr > 0: recvPos += 1
        recv[n] += fr if fr == fr else 0
P('H035', 'railGateNationYearShare', pct(rail, ny), 'rows', ny)
fy = sorted(firstRail.values())
if fy:
    P('H035', 'firstRailYear_min_med_max', fy[0], st.median(fy), fy[-1], 'nationSeedCount', len(fy))
for nm in ('イギリス', 'フランス', 'プロイセン', 'アメリカ', 'ロシア', '清', '日本', 'オーストリア', 'オスマン帝国'):
    ys = sorted(v for (s, n), v in firstRail.items() if n == nm)
    if ys: P('H035', f'firstRailYear_{nm}', ' '.join(map(str, ys)))
totR = sum(recv.values())
top = sorted(recv.items(), key=lambda kv: -kv[1])[:5]
P('H017', 'freightReceivedPosNationYearShare', pct(recvPos, ny))
P('H017', 'top5Receivers', ' '.join(f'{n}:{pct(v, totR)}' for n, v in top))

# scalars：H016
n1 = nall = 0
for s in seeds:
    h, r = rows('scalars', s)
    for row in r:
        nall += 1
        if abs(fl(row[h['freightRevolution']]) - 1) > 1e-12: n1 += 1
P('H016', 'freightRevolutionNot1Rows', n1, 'of', nall)

# monlaw：H019 給付の入力
b1 = ball = 0
for s in seeds:
    h, r = rows('monlaw', s)
    for row in r:
        ball += 1
        if fl(row[h['blockadeRiskShare']]) > 0: b1 += 1
P('H019', 'blockadeRiskSharePosRows', b1, 'of', ball)

# canal：H022
for s in seeds:
    h, r = rows('canal', s)
    P('H022', f'canal_s{s}', ' '.join(f"{row[h['canalId']]}:{row[h['openedYear']]}" for row in r))

# flow（9国）：H018・H019・H023・H026・H017
lines = {('BilateralMarket.cs', '1172'): 'H018_merchantCommission', ('BilateralMarket.cs', '1215'): 'H019_premium',
         ('BilateralMarket.cs', '1213'): 'H026_portFee', ('BilateralMarket.cs', '1209'): 'H017_freightToShipOwners',
         ('MultiNationSim.cs', '1447'): 'H023_canalDividend', ('MultiNationSim.cs', '1371'): 'H022_canalInvest',
         ('MultiNationSim.cs', '1493'): 'H026_portInvestCap', ('MultiNationSim.cs', '1494'): 'H026_portInvestState',
         ('MultiNationSim.cs', '1525'): 'H019_insurancePayout'}
nq = set(); hit = defaultdict(set); amt = defaultdict(float)
for s in seeds:
    h, r = rows('flow', s)
    for row in r:
        k = (s, row[h['quarter']], row[h['nation']]); nq.add(k)
        key = lines.get((row[h['sourceFile']], row[h['sourceLine']]))
        if key and fl(row[h['amountPerMonth']]) != 0:
            hit[key].add(k)
            if row[h['kind']] in ('giveIncome', 'give'): amt[key] += fl(row[h['amountPerMonth']])
for key in sorted(set(lines.values())):
    P('flow', key, pct(len(hit[key]), len(nq)), 'nationQuarters', len(hit[key]), 'of', len(nq), 'sumGivePerMonth', f'{amt[key]:.6g}')
# 結果の後の変更（名乗る）：H023 の分母を〈スエズが開いた後の国×四半期〉に狭める（prereg の追記 1 の分母は全ての国×四半期だった）
suezQ = {}
for s in seeds:
    h, r = rows('canal', s)
    for row in r:
        if row[h['canalId']] == 'suez': suezQ[s] = int(row[h['openedQuarter']])
post = {k for k in nq if suezQ.get(k[0], -1) >= 0 and int(k[1]) >= suezQ[k[0]]}
P('flow', 'H023_canalDividend_afterSuez', pct(len(hit['H023_canalDividend'] & post), len(post)), 'nationQuarters', len(hit['H023_canalDividend'] & post), 'of', len(post))
dq = sorted({int(k[1]) for k in hit['H023_canalDividend']})
P('flow', 'H023_canalDividend_firstQuarter', dq[0] if dq else 'none')

# port（9国の港の州×四半期）：H026・H027・H036・H037
pr = built = gate = thr = pot = thrLt1 = 0; thrv = []
for s in seeds:
    h, r = rows('port', s)
    for row in r:
        pr += 1
        if fl(row[h['builtCapacity']]) > 0: built += 1
        if row[h['congestedGate']] == '1': gate += 1
        if fl(row[h['portThroughput']]) > 0: thr += 1
        if fl(row[h['tradePotentialWeight']]) > 0: pot += 1
        t = fl(row[h['tradeThrottleIndex']])
        if t < 1: thrLt1 += 1; thrv.append(t)
P('H026', 'builtCapacityPosShare', pct(built, pr), 'congestedGateShare', pct(gate, pr), 'rows', pr)
P('H027', 'portThroughputPosShare', pct(thr, pr))
P('H036', 'tradePotentialPosShare', pct(pot, pr))
P('H037', 'throttleLt1Share', pct(thrLt1, pr), 'medianThrottleWhenLt1', f'{st.median(thrv):.4f}' if thrv else 'n/a')

# portch：H029・H030
pc = diff = dec = decn = 0
for s in seeds:
    h, r = rows('portch', s)
    for row in r:
        pc += 1
        if row[h['portUsed']] != row[h['portGc']]: diff += 1
        d = fl(row[h['railDecayUsedIndex']])
        if d == d:
            decn += 1
            if d < 1 - 1e-12: dec += 1
P('H029', 'portUsedNeGcShare', pct(diff, pc), 'rows', pc)
P('H030', 'railDecayUsedLt1Share', pct(dec, decn), 'rows', decn)

# stock：H032
sr = rpos = spos = 0
for s in seeds:
    h, r = rows('stock', s)
    for row in r:
        sr += 1
        if fl(row[h['railCapacityStock']]) > 0: rpos += 1
        if fl(row[h['steamFillShare']]) > 0: spos += 1
P('H032', 'railCapacityPosShare', pct(rpos, sr), 'steamFillPosShare', pct(spos, sr), 'rows', sr)

# rulepower：H028（近似）
rr = rdiff = rboth = 0
for s in seeds:
    p = os.path.join(root, f'rulepower_s{s}.tsv')
    with open(p, encoding='utf-8', newline='') as f:
        hdr = f.readline().rstrip('\n').split('\t')
        if 'rhoNoFreezeIndex' not in hdr:
            P('H028', 'rhoNoFreezeIndex', 'no-column', f'seed{s}'); break
        i1 = hdr.index('rhoNewIndex'); i2 = hdr.index('rhoNoFreezeIndex')
        for line in f:
            c = line.rstrip('\n').split('\t'); rr += 1
            a, b = fl(c[i1]), fl(c[i2])
            if a == a and b == b:
                rboth += 1
                if abs(a - b) > 1e-12: rdiff += 1
P('H028', 'rhoNoFreezeNeRhoNewShare', pct(rdiff, rboth), 'rowsBothFinite', rboth, 'rows', rr)
P('done')
out.close()
