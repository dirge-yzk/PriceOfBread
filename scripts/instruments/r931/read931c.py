# (931) 読むだけ：0 の続きの入り口の前 80 四半期の軌道（1 人あたりの貯蓄・稼ぎ・税の流出・人頭税の請求・税率）と、
#   続きの中の inFromPops と残差の分布（どの州×四半期に偏るか）を cearn_s で読む。区間の定義は read931a.py と同じ。
#   使い方: python -X utf8 -I read931c.py <ins フォルダ> <種の起点> <名札>
import sys, os, statistics as st, collections as C
M = sys.argv[1]; SB = int(sys.argv[2]); TAG = sys.argv[3]
POP = 100.0; K = 4
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')
BINS = [(-80, -61), (-60, -41), (-40, -25), (-24, -13), (-12, -7), (-6, -3), (-2, -1), (0, 0)]
def binof(k):
    for a, b in BINS:
        if a <= k <= b: return (a, b)
    return None
T = C.defaultdict(lambda: C.defaultdict(list))   # 帯 -> 欄 -> 値
fromPops = []; resid = []; fpTop = C.Counter()
for s in range(SB, SB + 10):
    f = open(os.path.join(M, f'region_s{s}.tsv'), encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
    d = C.defaultdict(dict)
    for l in f:
        r = l.rstrip('\n').split('\t')
        if len(r) < len(h): continue
        d[r[ix['region']]][int(r[ix['quarter']])] = (fl(r[ix['factoryOutputRefValuePerMonth']]) + fl(r[ix['extractionOutputRefValuePerMonth']]), fl(r[ix['populationK']]))
    f.close()
    want = C.defaultdict(list)
    for reg, dd in d.items():
        run = []
        qs = sorted(dd)
        def close(run):
            if len(run) >= K:
                for k in range(-80, 1):
                    b = binof(k)
                    if b: want[(reg, run[0] + k)].append(('t', b))
                for q in run: want[(reg, q)].append(('run', None))
        for q in qs:
            v = dd[q]
            if v[0] == 0.0 and v[1] >= POP:
                if run and run[-1] == q - 1: run.append(q)
                else: close(run); run = [q]
            else: close(run); run = []
        close(run)
    del d
    p = os.path.join(M, f'cearn_s{s}.tsv')
    if not os.path.exists(p): continue
    f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
    for l in f:
        r = l.rstrip('\n').split('\t')
        if len(r) < len(h): continue
        k = (r[ix['region']], int(r[ix['quarter']]))
        if k not in want: continue
        pop = fl(r[ix['popK']])
        if not pop > 0: continue
        for role, b in want[k]:
            if role == 't':
                t = T[b]
                t['savingsPerCap'].append(fl(r[ix['savingsStart']]) / pop)
                t['earnedGrossPerCapPerMonth'].append(fl(r[ix['earnedGrossPerMonth']]) / pop)
                t['outTaxPerCapPerQ'].append(fl(r[ix['outTaxPerQuarter']]) / pop)
                t['outTaxOverSavingsStart'].append(fl(r[ix['outTaxPerQuarter']]) / fl(r[ix['savingsStart']]) if fl(r[ix['savingsStart']]) > 0 else float('nan'))
                t['pollClaimPerCapPerMonth'].append(fl(r[ix['pollClaimPerMonth']]) / pop)
                t['taxRate'].append(fl(r[ix['taxRate']])); t['pollTaxLevel'].append(fl(r[ix['pollTaxLevel']]))
                t['inExportPerCapPerQ'].append(fl(r[ix['inExportPerQuarter']]) / pop)
                t['inMarketPerCapPerQ'].append(fl(r[ix['inMarketPerQuarter']]) / pop)
                t['staplePriceOverMc'].append(fl(r[ix['staplePrice']]) / fl(r[ix['stapleMarginalCost']]) if fl(r[ix['stapleMarginalCost']]) > 0 else float('nan'))
                t['belowSelfCostShare'].append(fl(r[ix['belowSelfCostShare']]))
                t['localPriceMultiplier'].append(fl(r[ix['localPriceMultiplier']]))
            else:
                v = fl(r[ix['inFromPopsPerQuarter']]); fromPops.append(v); resid.append(fl(r[ix['residualPerQuarter']]))
                if v > 0: fpTop[(s, r[ix['region']], r[ix['nation']])] += v
    f.close()
o = [f'== {TAG}  {M}', '入り口からの相対の四半期の帯ごとの中央（savingsStart は四半期の頭）']
cols = ['savingsPerCap', 'earnedGrossPerCapPerMonth', 'outTaxPerCapPerQ', 'outTaxOverSavingsStart', 'pollClaimPerCapPerMonth', 'taxRate', 'pollTaxLevel',
        'inExportPerCapPerQ', 'inMarketPerCapPerQ', 'staplePriceOverMc', 'belowSelfCostShare', 'localPriceMultiplier']
o.append('帯\tn\t' + '\t'.join(cols))
for b in BINS:
    t = T.get(b, {})
    o.append(f'{b[0]}..{b[1]}\t{len(t.get("savingsPerCap", []))}\t' + '\t'.join(f'{med(t.get(c, [])):.3g}' for c in cols))
fp = sorted(fromPops)
o.append(f'続きの中の inFromPops：州×四半期 {len(fp)}・正 {sum(1 for x in fp if x > 0)}・中央 {med(fp):.3g}・最大 {fp[-1] if fp else 0:.4g}・和 {sum(fp):.4g}')
o.append(f'続きの中の残差：中央 {med(resid):.3g}・最小 {min(resid) if resid else 0:.4g}・最大 {max(resid) if resid else 0:.4g}・和 {sum(x for x in resid if x == x):.4g}')
o.append('inFromPops の和の大きい州（種・州・持ち主）: ' + ' '.join(f'{k}:{v:.4g}' for k, v in fpTop.most_common(5)))
open(os.path.join(os.path.dirname(os.path.abspath(__file__)), f'read931c_{TAG}_out.txt'), 'w', encoding='utf-8').write('\n'.join(o) + '\n')
print('\n'.join(o))
