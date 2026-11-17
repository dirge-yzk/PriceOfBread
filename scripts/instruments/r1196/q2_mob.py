# (1196) c119ra 問い2：徴兵の門（NationBuildMobilizationWeight）と、内戦・分離・飢えの帰属を、同じ盤面の表で鎖の順に読む（読むだけ）。
#  鎖：①兵（nation_s soldierK÷totalPopulationK）→②労働の抜け（labor_s farmUnemploymentShare・region_s ruralPopulationK）
#      →③食料（region_s foodFillShare の人口の重み）→④財政（fiscal_s claimSoldierPayPerMonth÷claimTotalPerMonth・borrowShortfallPerMonth）
#      →⑤分離の圧（secession_s pressureIndex・goFlag）→結果（wars_s の内戦 civil・分離 secession を国ごとに・親＝sideA）。
#  計器が書かない行を 0 と数えない（欠けは nan のまま・件数を名乗る）。
#  使い方: python -X utf8 -I q2_mob.py <名>=<ins（D:/…）> [<名>=<ins> ...] --seeds=1-10 --out=<出力の頭>
import sys, csv, os, collections, math, statistics
sys.stdout.reconfigure(newline='\n')
args = [a for a in sys.argv[1:] if not a.startswith('--')]
opt = dict(a[2:].split('=', 1) for a in sys.argv[1:] if a.startswith('--'))
s0, s1 = map(int, opt.get('seeds', '1-10').split('-')); SEEDS = range(s0, s1 + 1); OUT = opt['out']


def fl(v):
    try: return float(v)
    except (TypeError, ValueError): return float('nan')


def rd(path):
    if not os.path.exists(path): return
    with open(path, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for c in r:
            if len(c) >= len(h): yield ix, c


def arm(ins, s):
    out = collections.defaultdict(lambda: collections.defaultdict(float))
    warq = collections.defaultdict(set); sp = collections.defaultdict(list); spw = collections.defaultdict(list)
    for ix, c in rd(os.path.join(ins, f'nation_s{s}.tsv')):
        n = c[ix['nation']]; q = int(c[ix['quarter']]); y = int(c[ix['year']])
        tp = fl(c[ix['totalPopulationK']]); so = fl(c[ix['soldierK']])
        if not (tp > 0) or so != so: continue
        r = so / tp; sp[n].append(r)
        if fl(c[ix['atWarMonths']]) > 0:
            warq[n].add(q); spw[n].append(r)
            if c[ix['mobilizedFlag']] == '1': out[n]['mobQ'] += 1
        if 1914 <= y <= 1918: out[n]['ww1max'] = max(out[n].get('ww1max', 0), r)
    for n in sp:
        out[n]['warQ'] = len(warq[n]); out[n]['solPopAll'] = statistics.fmean(sp[n])
        out[n]['solPopWar'] = statistics.fmean(spw[n]) if spw[n] else float('nan')
        out[n]['solPopMax'] = max(sp[n])
    # ③食料・②農村（州の行は計器の国の毎四半期＋全ての国の年の標本＝戦争の四半期に限ると計器の国に寄る＝名乗る）
    ff = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0.0])  # 戦争の四半期の Σpop·fill, Σpop, 平時の同じ
    fmin = {}
    for ix, c in rd(os.path.join(ins, f'region_s{s}.tsv')):
        n = c[ix['nation']]; q = int(c[ix['quarter']]); p = fl(c[ix['populationK']]); f = fl(c[ix['foodFillShare']])
        if not (p > 0) or f != f: continue
        k = 0 if q in warq[n] else 2
        ff[n][k] += p * f; ff[n][k + 1] += p
        if f < fmin.get(n, 9): fmin[n] = f
    for n, v in ff.items():
        out[n]['foodWar'] = v[0] / v[1] if v[1] > 0 else float('nan')
        out[n]['foodPeace'] = v[2] / v[3] if v[3] > 0 else float('nan')
        out[n]['foodMin'] = fmin.get(n, float('nan'))
    lw = collections.defaultdict(list); lp = collections.defaultdict(list)
    for ix, c in rd(os.path.join(ins, f'labor_s{s}.tsv')):
        n = c[ix['nation']]; q = int(c[ix['quarter']]); u = fl(c[ix['farmUnemploymentShare']])
        if u != u: continue
        (lw if q in warq[n] else lp)[n].append(u)
    for n in set(lw) | set(lp):
        out[n]['farmUnempWar'] = statistics.fmean(lw[n]) if lw[n] else float('nan')
        out[n]['farmUnempPeace'] = statistics.fmean(lp[n]) if lp[n] else float('nan')
    fs = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0])
    for ix, c in rd(os.path.join(ins, f'fiscal_s{s}.tsv')):
        n = c[ix['nation']]; q = int(c[ix['monthIndex']]) // 3
        if q not in warq[n]: continue
        a = fl(c[ix['claimSoldierPayPerMonth']]); t = fl(c[ix['claimTotalPerMonth']]); b = fl(c[ix['borrowShortfallPerMonth']])
        if a == a and t == t and t > 0: fs[n][0] += a; fs[n][1] += t
        if b == b and b > 0: fs[n][3] += 1
    for n, v in fs.items():
        out[n]['soldierPayShareWar'] = v[0] / v[1] if v[1] > 0 else float('nan'); out[n]['shortfallMonthsWar'] = v[3]
    pr = collections.defaultdict(list); go = collections.Counter()
    for ix, c in rd(os.path.join(ins, f'secession_s{s}.tsv')):
        n = c[ix['nation']]; v = fl(c[ix['pressureIndex']])
        if v == v: pr[n].append(v)
        if c[ix['goFlag']] == '1': go[n] += 1
    for n in pr:
        L = sorted(pr[n]); out[n]['pressP90'] = L[int(0.9 * (len(L) - 1))]; out[n]['goRows'] = go[n]
    for ix, c in rd(os.path.join(ins, f'wars_s{s}.tsv')):
        if c[ix['civilWarFlag']] != '1': continue
        k = 'secession' if c[ix['entryCode']] == 'secession' else 'civil'
        out[c[ix['sideA']]][k] += 1
    return out


COLS = ['warQ', 'mobQ', 'solPopAll', 'solPopWar', 'solPopMax', 'ww1max', 'farmUnempWar', 'farmUnempPeace', 'foodWar', 'foodPeace', 'foodMin',
        'soldierPayShareWar', 'shortfallMonthsWar', 'pressP90', 'goRows', 'civil', 'secession']
names = []; data = {}
for a in args:
    nm, d = a.split('=', 1); names.append(nm)
    for s in SEEDS:
        data[(nm, s)] = arm(d, s) if os.path.exists(os.path.join(d, f'nation_s{s}.tsv')) else None
with open(OUT + '_by_seed_nation.tsv', 'w', encoding='utf-8', newline='\n') as f:
    f.write('arm\tseed\tnation\t' + '\t'.join(COLS) + '\n')
    for nm in names:
        for s in SEEDS:
            o = data[(nm, s)]
            if o is None: f.write(f'{nm}\t{s}\t欠け\n'); continue
            for n in sorted(o):
                f.write(f'{nm}\t{s}\t{n}\t' + '\t'.join(('%.6g' % o[n][k]) if k in o[n] else 'nan' for k in COLS) + '\n')
# 種ごとの和と中央（国の和は件数・率は国の中央）
with open(OUT + '_by_seed.tsv', 'w', encoding='utf-8', newline='\n') as f:
    f.write('arm\tseed\tcivil\tsecession\tmobQ\tsolPopWar_med\tfoodWar_med\tfarmUnempWar_med\tpressP90_med\tsoldierPayShareWar_med\n')
    for nm in names:
        for s in SEEDS:
            o = data[(nm, s)]
            if o is None: continue
            def med(k):
                L = [v[k] for v in o.values() if k in v and v[k] == v[k]]
                return statistics.median(L) if L else float('nan')
            f.write('\t'.join([nm, str(s), str(int(sum(v.get('civil', 0) for v in o.values()))), str(int(sum(v.get('secession', 0) for v in o.values()))),
                               str(int(sum(v.get('mobQ', 0) for v in o.values())))] + ['%.4g' % med(k) for k in ('solPopWar', 'foodWar', 'farmUnempWar', 'pressP90', 'soldierPayShareWar')]) + '\n')
# 国ごと（種の和・中央）＝合流が k3・nd の腕を読む物差し
with open(OUT + '_by_nation.tsv', 'w', encoding='utf-8', newline='\n') as f:
    f.write('arm\tnation\tseedsPresent\tcivil_sum\tsecession_sum\tmobQ_sum\tsolPopWar_med\tww1max_med\tfoodWar_med\tfoodMin_min\tfarmUnempWar_med\tpressP90_med\n')
    for nm in names:
        allN = sorted({n for s in SEEDS if data[(nm, s)] for n in data[(nm, s)]})
        for n in allN:
            rows = [data[(nm, s)][n] for s in SEEDS if data[(nm, s)] and n in data[(nm, s)]]
            def med(k):
                L = [v[k] for v in rows if k in v and v[k] == v[k]]
                return statistics.median(L) if L else float('nan')
            fm = [v['foodMin'] for v in rows if 'foodMin' in v]
            f.write('\t'.join([nm, n, str(len(rows)), str(int(sum(v.get('civil', 0) for v in rows))), str(int(sum(v.get('secession', 0) for v in rows))),
                               str(int(sum(v.get('mobQ', 0) for v in rows)))] + ['%.4g' % med(k) for k in ('solPopWar', 'ww1max', 'foodWar')]
                              + ['%.4g' % (min(fm) if fm else float('nan'))] + ['%.4g' % med(k) for k in ('farmUnempWar', 'pressP90')]) + '\n')
for nm in names:
    tot = collections.Counter()
    for s in SEEDS:
        o = data[(nm, s)]
        if not o: continue
        for v in o.values():
            for k in ('civil', 'secession', 'mobQ'): tot[k] += v.get(k, 0)
    print(nm, dict(tot))
