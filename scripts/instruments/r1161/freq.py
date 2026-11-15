# (1161) S22 の行の頻度（読むだけ・腕 0）。表の列名で読む（★(749)）。計器が書かない行を 0 と数えない。
# 使い方: python -X utf8 -I freq.py <生のフォルダ> <ラベル> [種の数=10]
import sys, os, csv, statistics as st
from collections import Counter, defaultdict
D, LAB = sys.argv[1], sys.argv[2]
NS = int(sys.argv[3]) if len(sys.argv) > 3 else 10
sys.stdout.reconfigure(newline='\n')
csv.field_size_limit(1 << 30)
def rows(t, s):
    p = os.path.join(D, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    return _rows(p)
def _rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        ix = {k: i for i, k in enumerate(h)}
        for row in r:
            if len(row) < len(h): continue
            yield ix, row
def fl(x):
    try: return float(x)
    except: return float('nan')
out = []
def P(*a): out.append('\t'.join(str(x) for x in a))
P('# arm', LAB, 'dir', D)
# 1. monlaw：通貨の法と資本移動の法（全ての国×四半期）
LAWN = {0: 'F', 1: 'S', 2: 'G'}
tr = Counter(); tgtdiff = 0; nq = 0; lawq = Counter(); firstG = []; everG = 0; cmtr = 0; cmq = Counter(); have = 0
for s in range(1, NS + 1):
    it = rows('monlaw', s)
    if it is None: continue
    have += 1
    last = {}; lastcm = {}; start = {}; fg = {}
    for ix, r in it:
        n = r[ix['nationUid']]; ms = int(r[ix['MonetaryStandard']]); mt = int(r[ix['MonetaryStandardTarget']]); cm = int(r[ix['CapitalMobility']]); y = int(r[ix['year']])
        nq += 1; lawq[LAWN[ms]] += 1; cmq[cm] += 1
        if mt != ms: tgtdiff += 1
        if n not in start: start[n] = ms
        if n in last and last[n] != ms: tr[LAWN[last[n]] + '>' + LAWN[ms]] += 1
        if n in lastcm and lastcm[n] != cm: cmtr += 1
        if ms == 2 and n not in fg and start[n] != 2: fg[n] = y
        last[n] = ms; lastcm[n] = cm
    everG += len(fg); firstG += list(fg.values())
if have:
    P('monlaw_seeds', have, 'nationQuarters', nq)
    P('lawShare', ' '.join(f'{k}={v/nq:.4f}' for k, v in sorted(lawq.items())))
    P('lawTransitions', ' '.join(f'{k}={v}' for k, v in sorted(tr.items())))
    P('becameGoldNations', everG, 'firstGoldYearMedian', st.median(firstG) if firstG else 'nan', 'p10', sorted(firstG)[len(firstG)//10] if firstG else 'nan')
    P('targetNeLawShare', f'{tgtdiff/nq:.4f}')
    P('capMobility', 'transitions', cmtr, 'codeShare', ' '.join(f'{k}={v/nq:.4f}' for k, v in sorted(cmq.items())))
else: P('monlaw', '表なし')
# 2. fiscal（国×月・計器の国）：兌換の出入り・兌換の連続量・切り下げ
susp = join = 0; mon = 0; ong = 0; mid = 0; devm = 0; devOnGold = 0; have = 0; convDrop = 0
for s in range(1, NS + 1):
    it = rows('fiscal', s)
    if it is None: continue
    have += 1; last = {}; lastc = {}
    for ix, r in it:
        n = r[ix['nation']]; og = fl(r[ix['onGold']]); cv = fl(r[ix['convertibilityShare']]); dv = fl(r[ix['devaluedPerMonth']])
        mon += 1
        if og == 1: ong += 1
        if 0.05 < cv < 0.95: mid += 1
        if dv > 0:
            devm += 1
            if og == 1: devOnGold += 1
        if n in last:
            if last[n] == 1 and og == 0: susp += 1
            if last[n] == 0 and og == 1: join += 1
        if n in lastc and lastc[n] - cv > 0.3: convDrop += 1
        last[n] = og; lastc[n] = cv
if have:
    P('fiscal_seeds', have, 'nationMonths', mon)
    P('onGold', f'share={ong/mon:.4f}', 'exits(1>0)', susp, 'joins(0>1)', join)
    P('convertibilityMid(0.05,0.95)', f'{mid/mon:.4f}', 'monthlyDrop>0.3', convDrop)
    P('devalued>0', f'months={devm} share={devm/mon:.4f}', 'whileOnGold', devOnGold)
else: P('fiscal', '表なし')
# 3. silverpar（全ての国・四半期×法）：法ごとの国の数と為替の天井・床
cnt = defaultdict(list); ceil = Counter(); floor = Counter(); have = 0
for s in range(1, NS + 1):
    it = rows('silverpar', s)
    if it is None: continue
    have += 1
    for ix, r in it:
        y = int(r[ix['year']]); q = int(r[ix['quarter']]); law = r[ix['law']]
        ceil[law] += int(fl(r[ix['eAtCeilCount']])); floor[law] += int(fl(r[ix['eAtFloorCount']]))
        if q % 4 == 0 and y in (1800, 1830, 1850, 1870, 1900, 1930, 1950): cnt[(y, law)].append(int(fl(r[ix['nationCount']])))
if have:
    P('silverpar_seeds', have)
    for y in (1800, 1830, 1850, 1870, 1900, 1930, 1950):
        P('nationsByLaw', y, ' '.join(f'{l}={st.mean(cnt[(y,l)]):.1f}' if cnt[(y, l)] else f'{l}=nan' for l in 'SGF'))
    P('fxAtCeil(nationQuarters)', ' '.join(f'{k}={v}' for k, v in sorted(ceil.items())), 'fxAtFloor', ' '.join(f'{k}={v}' for k, v in sorted(floor.items())))
else: P('silverpar', '表なし')
# 4. pclamp：物価水準の床と天井に当たった行
pc = Counter(); have = 0
for s in range(1, NS + 1):
    it = rows('pclamp', s)
    if it is None: continue
    have += 1
    for ix, r in it: pc[r[ix['kind']]] += 1
P('pclamp_seeds', have, ' '.join(f'{k}={v}' for k, v in sorted(pc.items())) if have else '表なし')
# 5. money（国×四半期・計器の国）：造幣と発行の起きた四半期
mint = fiat = nq = 0; have = 0; pmax = 0.0
for s in range(1, NS + 1):
    it = rows('money', s)
    if it is None: continue
    have += 1; lm = {}; lf = {}
    for ix, r in it:
        if r[ix['event']] != 'update': continue
        n = r[ix['nation']]; m = fl(r[ix['cumulativeGoldMintedStock']]); f = fl(r[ix['cumulativeFiatIssuedStock']])
        nq += 1
        if n in lm and m > lm[n] + 1e-9: mint += 1
        if n in lf and f > lf[n] + 1e-9: fiat += 1
        lm[n] = m; lf[n] = f
if have: P('money_seeds', have, 'nationQuarters', nq, 'mintIncreased', f'{mint} ({mint/nq:.4f})', 'fiatIssued', f'{fiat} ({fiat/nq:.4f})')
else: P('money', '表なし')
# 6. cons（州×四半期・国の値の写し）：与信の過熱度
hb = Counter(); hq = 0; have = 0
for s in range(1, NS + 1):
    it = rows('cons', s)
    if it is None: continue
    have += 1; seen = set()
    for ix, r in it:
        k = (r[ix['quarter']], r[ix['nation']])
        if k in seen: continue
        seen.add(k); h = fl(r[ix['financialHeatMean']])
        if h != h: continue
        hq += 1
        for th in (0.75, 0.9, 1.0, 1.49):
            if h > th: hb[th] += 1
if have: P('cons_seeds', have, 'nationQuarters', hq, 'heat>', ' '.join(f'{k}={v} ({v/hq:.4f})' for k, v in sorted(hb.items())))
else: P('cons', '表なし')
print('\n'.join(out))
