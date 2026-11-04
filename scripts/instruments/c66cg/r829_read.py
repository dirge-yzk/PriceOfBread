"""(829) 読み（事前登録 measurements/m829_20261104_prereg_cg.txt の [6]＝読む前に固定した形を決める表）。
仮説〈定額の請求（人頭税）が払う群の貯蓄を毎月空にする〉を、m826 の生で選び直した出来事 15（pre_events826.tsv の event＝1）で読む。
使い方: python -I r829_read.py <計器の生のフォルダ（cearn_s・cgrp_s）> <pre_events826.tsv> <出力フォルダ>
出力: events.tsv（出来事ごとの g*・E1・E2・対照・S1〜S3・記録の列）・controls.tsv（対照ごとの E1）・summary.txt（結論の規則）。"""
import sys, os, statistics
raw, evp, outd = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(outd, exist_ok=True)
GROUPS = ['mass', 'modernElite', 'closed', 'soldier']
NEED = 10   # 出来事 15 のうち 10 以上（指示の 6 のうち 4 と同じ 2/3）
def fl(x):
    try: return float(x)
    except: return float('nan')

events = []
with open(evp, encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t')
    for line in f:
        d = dict(zip(h, line.rstrip('\n').split('\t')))
        if d.get('event') == '1': events.append((int(d['seed']), d['region'], d['nation'], int(d['onsetQ'])))

KEEP = ['popKMonths', 'savingsMonthEndSum', 'windowNetSum', 'monthResidualSum', 'emptiedPopKMonths', 'claimedPopKMonths', 'pollClaimPerQuarter', 'incomeTaxUpperPerQuarter',
        'outTaxPerQuarter', 'outDuesPerQuarter', 'outColonialRentPerQuarter', 'savingsMonthStartSum']
ev_out, ct_out = [], []
for sd in sorted({e[0] for e in events}):
    # cearn_s：州×四半期の格・国・凍結の印
    reg = {}
    with open(os.path.join(raw, 'cearn_s%d.tsv' % sd), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            a = line.rstrip('\n').split('\t')
            reg[(a[ix['region']], int(a[ix['quarter']]))] = (a[ix['nation']], a[ix['tier']], a[ix['frozenFlag']])
    grp = {}
    with open(os.path.join(raw, 'cgrp_s%d.tsv' % sd), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            a = line.rstrip('\n').split('\t')
            grp[(a[ix['region']], int(a[ix['quarter']]), a[ix['group']])] = {k: fl(a[ix[k]]) for k in KEEP}
    regions = sorted({k[0] for k in reg})
    def S(region, W, g, col): return sum(grp.get((region, q, g), {}).get(col, 0.0) for q in W)
    def E1(region, W, g):
        cl = S(region, W, g, 'claimedPopKMonths'); return S(region, W, g, 'emptiedPopKMonths') / cl if cl > 0 else 0.0
    for (s, region, nation0, q0) in [e for e in events if e[0] == sd]:
        W = [q for q in range(q0 - 8, q0) if any((region, q, g) in grp for g in GROUPS)]
        row = {'seed': sd, 'region': region, 'nation': nation0, 'onsetQ': q0, 'nW': len(W)}
        if len(W) < 4:
            row.update({'verdict': '判定不能', 'support': 0}); ev_out.append(row); continue
        polls = [S(region, W, g, 'pollClaimPerQuarter') for g in GROUPS]
        if max(polls) > 0: gi = max(range(4), key=lambda i: (polls[i], -i))
        else:
            taxes = [S(region, W, g, 'outTaxPerQuarter') for g in GROUPS]; gi = max(range(4), key=lambda i: (taxes[i], -i))
        gs = GROUPS[gi]
        e1 = E1(region, W, gs)
        pl, it = S(region, W, gs, 'pollClaimPerQuarter'), S(region, W, gs, 'incomeTaxUpperPerQuarter')
        e2 = pl / (pl + it) if pl + it > 0 else 0.0
        # 対照：同じ種・同じ国（四半期ごとに出来事の州と同じ国）・W の全てで Colonial・frozenFlag 0・g* の claimed の和が正・出来事の州を除く
        cts = []
        for rg in regions:
            if rg == region: continue
            ok = True
            for q in W:
                me, other = reg.get((region, q)), reg.get((rg, q))
                if other is None or me is None or other[0] != me[0] or other[1] != 'Colonial' or other[2] != '0': ok = False; break
            if not ok or not (S(rg, W, gs, 'claimedPopKMonths') > 0): continue
            v = E1(rg, W, gs); cts.append(v); ct_out.append({'seed': sd, 'event': region, 'control': rg, 'group': gs, 'E1': v})
        med = statistics.median(cts) if cts else float('nan')
        s1, s2 = e1 >= 0.5, e2 >= 0.5
        s3 = len(cts) >= 3 and (e1 - med) >= 0.25
        tot_end = sum(S(region, W, g, 'savingsMonthEndSum') for g in GROUPS)
        tot = lambda col: sum(S(region, W, g, col) for g in GROUPS)
        share = lambda col: S(region, W, gs, col) / tot(col) if tot(col) > 0 else float('nan')
        row.update({'gStar': gs, 'E1': e1, 'E2': e2, 'nControls': len(cts), 'controlE1Median': med, 'S1': int(s1), 'S2': int(s2), 'S3': int(s3),
                    'support': int(s1 and s2 and s3), 'verdict': '支える' if (s1 and s2 and s3) else ('対照なし' if len(cts) < 3 else '支えない'),
                    'gStarSavingsShare': S(region, W, gs, 'savingsMonthEndSum') / tot_end if tot_end > 0 else float('nan'),
                    'closedSavingsShare': S(region, W, 'closed', 'savingsMonthEndSum') / tot_end if tot_end > 0 else float('nan'),
                    'gStarWindowNet': S(region, W, gs, 'windowNetSum'), 'gStarTaxShare': share('outTaxPerQuarter'), 'gStarDuesShare': share('outDuesPerQuarter'),
                    'gStarRentShare': share('outColonialRentPerQuarter'), 'gStarPoll': pl, 'gStarIncTaxUpper': it, 'gStarOutTax': S(region, W, gs, 'outTaxPerQuarter'),
                    'gStarMonthStart': S(region, W, gs, 'savingsMonthStartSum'), 'gStarMonthEnd': S(region, W, gs, 'savingsMonthEndSum'),
                    'maxAbsMonthResidual': max(abs(grp.get((region, q, g), {}).get('monthResidualSum', 0.0)) for q in W for g in GROUPS)})
        ev_out.append(row)

cols = ['seed', 'region', 'nation', 'onsetQ', 'nW', 'gStar', 'E1', 'E2', 'nControls', 'controlE1Median', 'S1', 'S2', 'S3', 'support', 'verdict',
        'gStarSavingsShare', 'closedSavingsShare', 'gStarWindowNet', 'gStarTaxShare', 'gStarDuesShare', 'gStarRentShare', 'gStarPoll', 'gStarIncTaxUpper', 'gStarOutTax',
        'gStarMonthStart', 'gStarMonthEnd', 'maxAbsMonthResidual']
def fmt(v): return ('%.4g' % v) if isinstance(v, float) else str(v)
with open(os.path.join(outd, 'events.tsv'), 'w', encoding='utf-8', newline='\n') as o:
    o.write('\t'.join(cols) + '\n')
    for r in ev_out: o.write('\t'.join(fmt(r.get(c, '-')) for c in cols) + '\n')
with open(os.path.join(outd, 'controls.tsv'), 'w', encoding='utf-8', newline='\n') as o:
    o.write('seed\tevent\tcontrol\tgroup\tE1\n')
    for r in ct_out: o.write('%d\t%s\t%s\t%s\t%.4g\n' % (r['seed'], r['event'], r['control'], r['group'], r['E1']))
n_sup = sum(r['support'] for r in ev_out)
lines = ['(829) 読みの結論（事前登録 [6] の規則）',
         '出来事 %d・支える %d・必要 %d → %s' % (len(ev_out), n_sup, NEED, '仮説を支える（形 U の素案を書く）' if n_sup >= NEED else '記録（仮説を支えない）'),
         'S1（E1≧0.5）%d・S2（E2≧0.5）%d・S3（対照 3 以上かつ差≧0.25）%d・対照なし %d・判定不能 %d' % (
             sum(r.get('S1', 0) for r in ev_out), sum(r.get('S2', 0) for r in ev_out), sum(r.get('S3', 0) for r in ev_out),
             sum(1 for r in ev_out if r['verdict'] == '対照なし'), sum(1 for r in ev_out if r['verdict'] == '判定不能'))]
open(os.path.join(outd, 'summary.txt'), 'w', encoding='utf-8', newline='\n').write('\n'.join(lines) + '\n')
print('\n'.join(lines))
