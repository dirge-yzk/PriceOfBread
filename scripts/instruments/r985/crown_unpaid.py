# (985) c89sd 国家の枠の赤字の未払いの額を読む（読むだけ・生を書き換えない）
# 使い方: python -X utf8 -I crown_unpaid.py <keepの親> <出力フォルダ> [腕 ...]
# 母集合: landshare_s に行がある国×四半期（助走の月は landshare が数えない）。fiscal_s の月 m（1 始まり）は四半期 (m-1)//3。
# 国家の枠の赤字の月の国庫の払い C＝crownDeficitStatePerQuarter（InstrumentsLandshare.cs:146・Σ|VA|·f·p_m・p_m>0 の月だけ）。
# 未払い U_m＝C_m·(1−p_m)/p_m（EconomyStep.cs:2794）。C は四半期の和＝月に割れない＝区間 [C·(1−pmax)/pmax, C·(1−pmin+)/pmin+]（(1−p)/p は p について減る）。
#   四半期の 3 月の p が全て 1 なら U＝0（正確）。p＝0 の月は C に入らない＝観測できない（月の数だけ数える）。
# 国有の分の見込み Uc＝Σ claimStateLossPerMonth·(1−p_m)（Simulation.cs:1873＝先月の LastCost−LastRevenue×f の見込み＝推）。
import sys, os, csv, math
from collections import defaultdict

root, outd = sys.argv[1], sys.argv[2]
arms = sys.argv[3:] or ['base_ins', 'n1', 'n2']
os.makedirs(outd, exist_ok=True)
W = {'ロシア': (0.53, 0.0, 0.0, 0.0, 0.47), 'プロイセン': (0.61, 0.02, 0.076, 0.0, 0.294)}  # world.json の LandOwnership（正規化は和＝1 のまま）
CLS = ['Noble', 'Clergy', 'Peasant', 'Capitalist', 'Other']

def rd(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r)
        ix = {k: i for i, k in enumerate(h) if k}
        for row in r:
            yield ix, row

def fl(x):
    try: return float(x)
    except: return float('nan')

keys = ['C', 'Ulo', 'Uhi', 'A', 'rev', 'gdp', 'recRent', 'recRentAll', 'Uc', 'claimLoss', 'q', 'qOpen', 'mP0', 'mPlt1', 'prop', 'qC', 'natSkip']
tot = {}       # (arm, seed, nation) -> dict
byyear = defaultdict(lambda: defaultdict(float))  # (arm, nation, year) -> key -> sum over seeds
nonfin = 0
for arm in arms:
    for s in range(1, 11):
        d = os.path.join(root, arm)
        fm = {}
        for ix, row in rd(os.path.join(d, f'fiscal_s{s}.tsv')):
            m = int(row[ix['monthIndex']]); nat = row[ix['nation']]
            fm.setdefault((nat, (m - 1) // 3), []).append((fl(row[ix['payRateShare']]), fl(row[ix['claimStateLossPerMonth']]), fl(row[ix['revenueBasePerMonth']])))
        gdp = {}
        for ix, row in rd(os.path.join(d, f'nation_s{s}.tsv')):
            gdp[(row[ix['nation']], int(row[ix['quarter']]))] = fl(row[ix['nominalGdpPerYear']]) / 4.0
        for ix, row in rd(os.path.join(d, f'landshare_s{s}.tsv')):
            nat = row[ix['nation']]; q = int(row[ix['quarter']]); y = int(float(row[ix['year']]))
            C = fl(row[ix['crownDeficitStatePerQuarter']]); A = fl(row[ix['crownAllActualPerQuarter']])
            prop = fl(row[ix['crownProppedRgoMonths']]); skip = fl(row[ix['crownNationalizedSkipCount']])
            ms = fm.get((nat, q))
            if ms is None:
                continue  # 計器の財政の 9 国の外（fiscal_s が無い）
            ps = [p for p, _, _ in ms]
            for v in ps + [C, A]:
                if not math.isfinite(v): nonfin += 1
            ppos = [p for p in ps if p > 0]
            mP0 = sum(1 for p in ps if not (p > 0)); mPlt1 = sum(1 for p in ps if p < 1)
            if C > 0 and mPlt1 > 0 and ppos:
                pmax, pmin = max(ppos), min(ppos)
                Ulo = C * (1 - pmax) / pmax; Uhi = C * (1 - pmin) / pmin; qOpen = 1 if pmax != pmin else 0
            else:
                Ulo = Uhi = 0.0; qOpen = 0
            Uc = sum(cl * (1 - p) for p, cl, _ in ms if math.isfinite(cl) and math.isfinite(p))
            cl = sum(c for _, c, _ in ms if math.isfinite(c))
            rev = sum(r for _, _, r in ms if math.isfinite(r))
            w = W.get(nat)
            def lr(pre):
                return sum(fl(row[ix[pre + c + 'PerQuarter']]) * (1 if w is None else (1 if (w[k] > 0) else 0)) for k, c in enumerate(CLS) if w is None or (k < 4 and w[k] > 0))
            recRent = lr('landRentActualFieldRegion') if w else 0.0
            recRentAll = lr('landRentActual') if w else 0.0
            v = dict(C=C, Ulo=Ulo, Uhi=Uhi, A=A, rev=rev, gdp=gdp.get((nat, q), float('nan')), recRent=recRent, recRentAll=recRentAll,
                     Uc=Uc, claimLoss=cl, q=1, qOpen=qOpen if (C > 0 and mPlt1 > 0) else 0, mP0=mP0, mPlt1=mPlt1, prop=prop, qC=1 if C > 0 else 0, natSkip=skip)
            t = tot.setdefault((arm, s, nat), defaultdict(float))
            for k in keys: t[k] += v[k] if math.isfinite(v[k]) else 0.0
            b = byyear[(arm, nat, y)]
            for k in keys: b[k] += v[k] if math.isfinite(v[k]) else 0.0

def r(a, b): return a / b if b else float('nan')
with open(os.path.join(outd, 'per_seed.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    f.write('arm\tseed\tnation\t' + '\t'.join(keys) + '\n')
    for (arm, s, nat), t in sorted(tot.items()):
        f.write(f'{arm}\t{s}\t{nat}\t' + '\t'.join(f'{t[k]:.6g}' for k in keys) + '\n')
with open(os.path.join(outd, 'by_year.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    f.write('arm\tnation\tyear\t' + '\t'.join(keys) + '\tUhi_over_rev\tUhi_over_recRent\n')
    for (arm, nat, y), b in sorted(byyear.items()):
        if b['C'] > 0 or b['Uc'] > 0 or b['mPlt1'] > 0:
            f.write(f'{arm}\t{nat}\t{y}\t' + '\t'.join(f'{b[k]:.6g}' for k in keys) + f"\t{r(b['Uhi'], b['rev']):.4g}\t{r(b['Uhi'], b['recRent']):.4g}\n")
# 要約：腕×国（10 種の和÷和）と、種ごとの Uhi の最小・最大
agg = defaultdict(lambda: defaultdict(float)); per = defaultdict(list)
for (arm, s, nat), t in tot.items():
    for k in keys: agg[(arm, nat)][k] += t[k]
    per[(arm, nat)].append(t['Uhi'])
with open(os.path.join(outd, 'summary.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    f.write('arm\tnation\tC\tUlo\tUhi\tA\tUhi/(C+Uhi)\tUhi/A\tUhi/rev\tUhi/gdp\tUhi/recRent\tUhi/recRentAll\tUc\tUc/claimLoss\tUc/rev\tqC\tqOpen\tmPlt1\tmP0\tprop\tUhi_seed_min\tUhi_seed_max\tnatSkip\n')
    for (arm, nat), a in sorted(agg.items(), key=lambda kv: (kv[0][1], kv[0][0])):
        f.write('\t'.join([arm, nat] + [f'{x:.4g}' for x in [a['C'], a['Ulo'], a['Uhi'], a['A'], r(a['Uhi'], a['C'] + a['Uhi']), r(a['Uhi'], a['A']), r(a['Uhi'], a['rev']), r(a['Uhi'], a['gdp']),
                 r(a['Uhi'], a['recRent']), r(a['Uhi'], a['recRentAll']), a['Uc'], r(a['Uc'], a['claimLoss']), r(a['Uc'], a['rev']), a['qC'], a['qOpen'], a['mPlt1'], a['mP0'], a['prop'], min(per[(arm, nat)]), max(per[(arm, nat)]), a['natSkip']]]) + '\n')
print('nonfinite', nonfin)
