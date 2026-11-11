"""(809) c13hl：選んだ事象の州の 5 年ごとの詳しい時系列（読むだけ）。
使い方: python -I r809_detail.py <出力.tsv> <ラベル>|<ディレクトリ>|<種>|<州> ...
列：人口・農村・都市（人口−農村）・持ち主・格・1 人あたりの稼ぎ・持ち主の本土（植民地格を除く州）の 1 人あたりの稼ぎ・その比・
    工場の産出（参照の値段）÷人口・工場の稼働数・倒産数・採取の産出÷人口・機会・残る値打ち・賃金・賃金のプレミアム・住居・雇用・主食・
    5 年の正味の移民（出−入・州の間＋入植＋越境）・5 年の残り（減りの側の正）。表を自分で読む（エンジンの関数は呼ばない）。"""
import sys, os, collections, math
def fl(x):
    try: return float(x)
    except: return float('nan')
out = sys.argv[1]
reqs = [a.split('|', 3) for a in sys.argv[2:]]
byfile = collections.defaultdict(list)
for lab, d, sd, reg in reqs: byfile[(d, int(sd))].append((lab, reg))
o = open(out, 'w', encoding='utf-8', newline='\n')
o.write('\t'.join(['set', 'seed', 'region', 'y5', 'nation', 'tier', 'popK', 'ruralK', 'urbanK', 'earnPC', 'ownerMainEarnPC', 'earnRatio', 'wpremCol',
                   'factOutPC', 'factActive', 'factBankrupt', 'extrOutPC', 'opp', 'keep', 'wage', 'hous', 'emp', 'food', 'netMigK_5y', 'otherK_5y']) + '\n')
for (d, sd), lst in byfile.items():
    want = set(r for _, r in lst)
    rows = collections.defaultdict(list)
    main = collections.defaultdict(lambda: [0.0, 0.0])   # (q, nation) -> [earn, pop] 本土（植民地格を除く）
    with open(os.path.join(d, 'region_s%d.tsv' % sd), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            a = line.rstrip('\n').split('\t')
            q = int(a[ix['quarter']]); nat = a[ix['nation']]; tier = a[ix['tier']]
            pop = fl(a[ix['populationK']]); earn = fl(a[ix['earnedPerMonth']])
            if tier != 'Colonial' and math.isfinite(pop) and math.isfinite(earn): m = main[(q, nat)]; m[0] += earn; m[1] += pop
            if a[ix['region']] in want:
                rows[a[ix['region']]].append(dict(q=q, yr=int(a[ix['year']]), nat=nat, tier=tier, pop=pop, rural=fl(a[ix['ruralPopulationK']]), earn=earn,
                    fo=fl(a[ix['factoryOutputRefValuePerMonth']]), fa=fl(a[ix['factoryActiveCount']]), fb=fl(a[ix['factoryBankruptCount']]), eo=fl(a[ix['extractionOutputRefValuePerMonth']]),
                    opp=fl(a[ix['opportunityIndex']]), keep=fl(a[ix['holdingFactor']]), wage=fl(a[ix['wageFactor']]), wp=fl(a[ix['wagePremium']]), hous=fl(a[ix['housingFactor']]),
                    emp=fl(a[ix['employmentShare']]), food=fl(a[ix['foodFillShare']]),
                    nm=fl(a[ix['migrationOutKPerQuarter']]) - fl(a[ix['migrationInKPerQuarter']]) + fl(a[ix['settlerOutKPerQuarter']]) - fl(a[ix['settlerInKPerQuarter']])
                       + fl(a[ix['crossBorderOutKPerQuarter']]) - fl(a[ix['crossBorderInKPerQuarter']])))
    for lab, reg in lst:
        rr = rows[reg]
        g = collections.defaultdict(list)
        for r in rr: g[(r['yr'] - 1780) // 5 * 5 + 1780].append(r)
        prevPop = None
        for y in sorted(g):
            s = g[y]; z = s[-1]
            nm = sum(r['nm'] for r in s)
            oth = (-(z['pop'] - prevPop) - nm) if prevPop is not None else float('nan')
            prevPop = z['pop']
            m = main.get((z['q'], z['nat']), [0, 0])
            mpc = m[0] / m[1] if m[1] > 0 else float('nan')
            pc = z['earn'] / z['pop'] if z['pop'] > 0 else float('nan')
            f = lambda v, p='.3g': format(v, p) if math.isfinite(v) else 'nan'
            o.write('\t'.join([lab, str(sd), reg, str(y), z['nat'], z['tier'], f(z['pop'], '.4g'), f(z['rural'], '.4g'), f(z['pop'] - z['rural'], '.4g'), f(pc), f(mpc),
                               f(pc / mpc) if mpc > 0 else 'nan', f(z['wp']), f(z['fo'] / z['pop']) if z['pop'] > 0 else 'nan', f(z['fa']), f(z['fb']),
                               f(z['eo'] / z['pop']) if z['pop'] > 0 else 'nan', f(z['opp']), f(z['keep']), f(z['wage']), f(z['hous']), f(z['emp']), f(z['food']), f(nm), f(oth)]) + '\n')
o.close()
print('書いた', out)
