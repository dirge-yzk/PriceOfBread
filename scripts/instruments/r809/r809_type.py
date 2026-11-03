"""(809) c13hl：1/4 未満の事象（ev_summary.tsv の例外の外）を型に分ける材料（読むだけ）。
使い方: python -I r809_type.py <ev_summary.tsv> <出力.tsv> <ラベル>=<ディレクトリ> ...
各事象について、ピークの四半期と 1/4 未満へ落ちた四半期の 都市（人口−農村）・農村・工場の産出÷人口・採取の産出÷人口・1 人あたりの稼ぎ÷持ち主の本土・
賃金のプレミアム・残る値打ち（Holding）を並べ、持ち主の交代の前後 20 四半期の工場の産出÷人口の比を出す。表を自分で読む。"""
import sys, os, collections, math
def fl(x):
    try: return float(x)
    except: return float('nan')
evp, out = sys.argv[1], sys.argv[2]
sets = dict(a.split('=', 1) for a in sys.argv[3:])
ev = []
with open(evp, encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for line in fh:
        x = line.rstrip('\n').split('\t')
        if x[ix['exempt']] == '1': continue
        ev.append(dict(set=x[ix['set']], seed=int(x[ix['seed']]), reg=x[ix['region']], pky=int(x[ix['peakYear']]), dy=int(x[ix['dropYear']]), pk=fl(x[ix['peakK']]), dk=fl(x[ix['dropK']]),
                       mig=fl(x[ix['migShare']]), cause=x[ix['cause_anh2']], ch=x[ix['ownerChanges']]))
by = collections.defaultdict(list)
for e in ev: by[(e['set'], e['seed'])].append(e)
o = open(out, 'w', encoding='utf-8', newline='\n')
o.write('\t'.join(['set', 'seed', 'region', 'cause', 'migShare', 'tierPeak', 'peakK', 'peakYear', 'dropYear', 'urbanPeakK', 'urbanDropK', 'ruralPeakK', 'ruralDropK', 'urbanLossShareOfD',
                   'factPCpeak', 'factPCdrop', 'extrPCpeak', 'extrPCdrop', 'earnRatioPeak', 'earnRatioDrop', 'wpremPeak', 'wpremDrop', 'keepPeak', 'keepDrop', 'foodPeak', 'foodDrop',
                   'transferYear', 'factPCbefore20q', 'factPCafter20q', 'earnRatioBefore20q', 'earnRatioAfter20q']) + '\n')
for (lab, sd), lst in by.items():
    d = sets[lab]
    want = set(e['reg'] for e in lst)
    rows = collections.defaultdict(list); main = collections.defaultdict(lambda: [0.0, 0.0])
    with open(os.path.join(d, 'region_s%d.tsv' % sd), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            a = line.rstrip('\n').split('\t')
            q = int(a[ix['quarter']]); nat = a[ix['nation']]; tier = a[ix['tier']]
            pop = fl(a[ix['populationK']]); earn = fl(a[ix['earnedPerMonth']])
            if tier != 'Colonial' and math.isfinite(pop) and math.isfinite(earn): m = main[(q, nat)]; m[0] += earn; m[1] += pop
            if a[ix['region']] in want:
                rows[a[ix['region']]].append(dict(q=q, yr=int(a[ix['year']]), nat=nat, tier=tier, pop=pop, rural=fl(a[ix['ruralPopulationK']]), earn=earn,
                                                  fo=fl(a[ix['factoryOutputRefValuePerMonth']]), eo=fl(a[ix['extractionOutputRefValuePerMonth']]), wp=fl(a[ix['wagePremium']]),
                                                  keep=fl(a[ix['holdingFactor']]), food=fl(a[ix['foodFillShare']])))
    for e in lst:
        rr = rows[e['reg']]
        def er(r):
            m = main.get((r['q'], r['nat']), [0, 0]); mpc = m[0] / m[1] if m[1] > 0 else float('nan')
            return (r['earn'] / r['pop']) / mpc if r['pop'] > 0 and mpc > 0 else float('nan')
        # ピーク（an_h2 と同じ：初めて最大になった行）と落ちた行
        pk = 0; pkI = 0; dI = None
        for i, r in enumerate(rr):
            if not math.isfinite(r['pop']): continue
            if r['pop'] > pk: pk = r['pop']; pkI = i
            if pk >= 1 and r['pop'] < pk / 4: dI = i; break
        a, z = rr[pkI], rr[dI]
        D = a['pop'] - z['pop']
        ul = ((a['pop'] - a['rural']) - (z['pop'] - z['rural'])) / D if D > 0 else float('nan')
        tY = ''; fb = fa = rb = ra = float('nan')
        for i in range(pkI + 1, dI + 1):
            if rr[i]['nat'] != rr[i - 1]['nat']:
                tY = str(rr[i]['yr'])
                pre = rr[max(0, i - 20):i]; post = rr[i:i + 20]
                fb = sum(r['fo'] for r in pre) / max(1e-9, sum(r['pop'] for r in pre)); fa = sum(r['fo'] for r in post) / max(1e-9, sum(r['pop'] for r in post))
                rb = sum(er(r) for r in pre) / len(pre); ra = sum(er(r) for r in post) / len(post)
                break
        f = lambda v: format(v, '.3g') if isinstance(v, float) and math.isfinite(v) else ('nan' if isinstance(v, float) else str(v))
        pc = lambda r, k: r[k] / r['pop'] if r['pop'] > 0 else float('nan')
        o.write('\t'.join(f(v) for v in [lab, sd, e['reg'], e['cause'], e['mig'], a['tier'], a['pop'], a['yr'], z['yr'], a['pop'] - a['rural'], z['pop'] - z['rural'], a['rural'], z['rural'], ul,
                                          pc(a, 'fo'), pc(z, 'fo'), pc(a, 'eo'), pc(z, 'eo'), er(a), er(z), a['wp'], z['wp'], a['keep'], z['keep'], a['food'], z['food'], tY, fb, fa, rb, ra]) + '\n')
o.close(); print('書いた', out)
