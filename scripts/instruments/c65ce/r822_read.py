"""(822) 植民地の稼ぎの表 cearn_s を、事前登録 measurements/m822_20261104_prereg_ce.txt の [6] の形の表で読む（読むだけ）。
使い方: python -I r822_read.py <cearn の生のディレクトリ> <zero_all.tsv> <出力のディレクトリ> [<測った木のルート>]
対象＝zero_all.tsv の set=base で longestRunQ>=40 の州（事前登録の 12 州）。窓 W_in＝runStartYear〜runEndYear・W_pre＝その 10 年前〜前の年・W＝W_pre∪W_in。
出力：forms.tsv（州ごとの量と形）・contrast.tsv（同じ国・種・年の床の植民地の凍っていない州との対照）・routes.tsv（州ごとの outTop1Route の多い順）・summary.txt。
"""
import sys, os, math, collections, statistics
def fl(x):
    try: return float(x)
    except: return float('nan')
raw, zero_all, outdir = sys.argv[1], sys.argv[2], sys.argv[3]
tree = sys.argv[4] if len(sys.argv) > 4 else None
os.makedirs(outdir, exist_ok=True)
OUTS = ['outTaxPerQuarter','outBondPerQuarter','outDuesPerQuarter','outColonialRentPerQuarter','outCapitalPerQuarter','outImportPerQuarter','outMigrationPerQuarter','outOtherPerQuarter']
INS = ['inMarketPerQuarter','inExportPerQuarter','inStatePerQuarter','inCreditPerQuarter','inFromPopsPerQuarter','inMigrationPerQuarter','inOtherPerQuarter']
ALLOUT = ['outMarketPerQuarter'] + OUTS
# 対象の州（事前登録）
targets = []
with open(zero_all, encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t'); ix = {k:i for i,k in enumerate(h)}
    for line in f:
        a = line.rstrip('\n').split('\t')
        if a[ix['set']] == 'base' and int(a[ix['longestRunQ']]) >= 40:
            targets.append((int(a[ix['seed']]), a[ix['region']], int(a[ix['runStartYear']]), int(a[ix['runEndYear']])))
seeds = sorted({t[0] for t in targets})
def load(seed):
    p = os.path.join(raw, 'cearn_s%d.tsv' % seed)
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        rows = [dict(zip(h, line.rstrip('\n').split('\t'))) for line in f]
    return rows
def src_line(file, line):
    if not tree: return ''
    p = os.path.join(tree, 'PriceOfBread.SimCore', file)
    try:
        with open(p, encoding='utf-8') as f: L = f.readlines()
        s = L[int(line)-1].strip()
        return s[:140]
    except Exception: return ''
forms, contrast_rows, route_rows = [], [], []
for seed in seeds:
    rows = load(seed)
    by = collections.defaultdict(list)
    for r in rows: by[r['region']].append(r)
    for (sd, reg, y0, y1) in [t for t in targets if t[0] == seed]:
        rr = by.get(reg, [])
        W = [r for r in rr if y0 - 10 <= int(r['year']) <= y1]
        Win = [r for r in rr if y0 <= int(r['year']) <= y1]
        gross = sum(sum(fl(r[c]) for c in INS + ALLOUT) for r in W)
        resid = sum(abs(fl(r['residualPerQuarter'])) for r in W if math.isfinite(fl(r['residualPerQuarter'])))
        closure = resid / gross if gross > 0 else float('nan')
        O = {c: sum(fl(r[c]) for r in W) for c in OUTS}
        mkt = sum(fl(r['outMarketPerQuarter']) - fl(r['inMarketPerQuarter']) for r in W)
        O['marketNet'] = max(0.0, mkt)
        kstar = max(O, key=lambda k: O[k])
        R = sum(fl(r['inStatePerQuarter']) for r in W) + sum(fl(r['stateMarketSpendPerQuarter']) for r in W)
        poll = sum(3 * fl(r['pollClaimPerMonth']) for r in W)
        g = []
        for r in Win:
            s0, s1 = fl(r['savingsStart']), fl(r['savings'])
            if s0 > 0 and s1 > 0: g.append(-math.log(s1 / s0) / 3)
        gin = statistics.median(g) if g else float('nan')
        floor = sum(int(r['localPriceAtFloorFlag']) for r in Win) / len(Win) if Win else float('nan')
        bsc = [fl(r['belowSelfCostShare']) for r in Win if math.isfinite(fl(r['belowSelfCostShare']))]
        bscm = statistics.median(bsc) if bsc else float('nan')
        pc = [fl(r['savingsPerCapitaOverNation']) for r in Win if math.isfinite(fl(r['savingsPerCapitaOverNation']))]
        pcm = statistics.median(pc) if pc else float('nan')
        frz = [int(r['frozenFlag']) for r in rr]
        best = cur = 0
        for v in frz:
            cur = cur + 1 if v else 0; best = max(best, cur)
        tax = O['outTaxPerQuarter']
        names = []
        if not (closure <= 1e-6): names.append('閉じない')
        else:
            if gin < 0.003 and floor >= 0.95 and bscm >= 0.9: names.append('F')
            if kstar == 'outTaxPerQuarter' and R < 0.1 * tax: names.append('T')
            if kstar == 'outTaxPerQuarter' and poll >= 0.5 * tax: names.append('U')
            if kstar in ('outCapitalPerQuarter', 'outBondPerQuarter'): names.append('I')
            if not names: names.append('記録(' + kstar + ')')
        prim = names[0]
        nations = sorted({r['nation'] for r in Win})
        forms.append([str(seed), reg, '/'.join(nations), str(y0), str(y1), str(len(W)), '%.3g' % closure, kstar, '%.4g' % O[kstar], '%.4g' % tax, '%.4g' % R, '%.4g' % poll,
                      '%.4g' % gin, '%.3f' % floor, '%.3f' % bscm, '%.3g' % pcm, str(best), prim, '+'.join(names)]
                     + ['%.4g' % O[c] for c in OUTS] + ['%.4g' % O['marketNet']])
        # 経路の上位（W の outTop1Route の額の和）
        rt = collections.Counter()
        for r in W:
            if r['outTop1Route'] != '-': rt[r['outTop1Route']] += fl(r['outTop1PerQuarter'])
            if r['outTop2Route'] != '-': rt[r['outTop2Route']] += fl(r['outTop2PerQuarter'])
        for k, v in rt.most_common(4):
            f_, l_ = k.split(':')[0], k.split(':')[1]
            route_rows.append([str(seed), reg, k, '%.4g' % v, src_line(f_, l_)])
        # 床の対照：同じ持ち主の国・同じ種・同じ年の植民地格で床に当たり凍っていない州
        nat_years = {(r['nation'], r['year']) for r in Win}
        cmp_pc, cmp_bsc = [], []
        for r in rows:
            if r['region'] == reg or r['tier'] != 'Colonial': continue
            if (r['nation'], r['year']) not in nat_years: continue
            if r['localPriceAtFloorFlag'] != '1' or r['frozenFlag'] == '1': continue
            v = fl(r['savingsPerCapitaOverNation']); b = fl(r['belowSelfCostShare'])
            if math.isfinite(v): cmp_pc.append(v)
            if math.isfinite(b): cmp_bsc.append(b)
        contrast_rows.append([str(seed), reg, '%.3g' % pcm, '%.3f' % bscm, str(len(cmp_pc)), '%.3g' % (statistics.median(cmp_pc) if cmp_pc else float('nan')),
                              '%.3f' % (statistics.median(cmp_bsc) if cmp_bsc else float('nan'))])
H = ['seed','region','nation','y0','y1','nQ','closure','kstar','kstarSum','outTax','returnR','pollClaim','gInPerMonth','floorShare','belowSelfCostMed','perCapOverNationMed','frozenLongestQ','form','formsAll'] + OUTS + ['marketNet']
with open(os.path.join(outdir, 'forms.tsv'), 'w', encoding='utf-8', newline='\n') as o:
    o.write('\t'.join(H) + '\n'); [o.write('\t'.join(x) + '\n') for x in forms]
with open(os.path.join(outdir, 'contrast.tsv'), 'w', encoding='utf-8', newline='\n') as o:
    o.write('\t'.join(['seed','region','frozenPerCapMed','frozenBelowSelfCostMed','nonFrozenFloorRows','nonFrozenPerCapMed','nonFrozenBelowSelfCostMed']) + '\n'); [o.write('\t'.join(x) + '\n') for x in contrast_rows]
with open(os.path.join(outdir, 'routes.tsv'), 'w', encoding='utf-8', newline='\n') as o:
    o.write('\t'.join(['seed','region','route','sumPerQuarterInW','sourceLine']) + '\n'); [o.write('\t'.join(x) + '\n') for x in route_rows]
cnt = collections.Counter(x[17] for x in forms)
with open(os.path.join(outdir, 'summary.txt'), 'w', encoding='utf-8', newline='\n') as o:
    o.write('対象の州 %d（事前登録＝12）\n' % len(forms))
    for k, v in cnt.most_common(): o.write('主の形 %s：%d 州\n' % (k, v))
    top = cnt.most_common(1)[0] if cnt else ('-', 0)
    o.write('結論の規則（同じ形が 7 州以上）：%s\n' % (('形 ' + top[0]) if top[1] >= 7 and not top[0].startswith('記録') and top[0] != '閉じない' else '記録'))
print(open(os.path.join(outdir, 'summary.txt'), encoding='utf-8').read())
