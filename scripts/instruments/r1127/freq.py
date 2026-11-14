# (1127) S05 投資と建設：行ごとの頻度を生の計器の表から数える（読むだけ・pandas なし）
# 使い方: python freq.py <生のフォルダ> <名> > out_freq_<名>.txt
import csv, sys, os, math, statistics as st
csv.field_size_limit(10**9)
D = sys.argv[1]; NAME = sys.argv[2]
S0 = int(sys.argv[3]) if len(sys.argv) > 3 else 1
SEEDS = range(S0, S0 + 10)

def rows(t, s):
    p = os.path.join(D, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    f = open(p, encoding='utf-8', newline='')
    r = csv.reader(f, delimiter='\t')
    h = next(r)
    ix = {k: i for i, k in enumerate(h)}
    def gen():
        for row in r:
            if len(row) != len(h): continue
            yield row
    return ix, gen()

def fl(x):
    try:
        v = float(x)
        return v if math.isfinite(v) else None
    except: return None

def pct(a, b): return f'{100.0*a/b:.2f}%' if b else 'n/a'
def rng(vals):
    v = [x for x in vals if x is not None]
    return f'{min(v):.4g}〜{max(v):.4g}' if v else 'n/a'

out = []
def P(*a): out.append(' '.join(str(x) for x in a))
P(f'# 生 {D}（{NAME}）')

# ---- 頻度を種ごとに数え、種の範囲で出す
acc = {}
def add(key, s, v): acc.setdefault(key, {})[s] = v

# 鉄道の発見の最初の年（国ごと・E 行・機関車）
railyear = {}
for s in SEEDS:
    g = rows('disc', s)
    if g is None: continue
    ix, it = g; ry = {}
    for row in it:
        if row[ix['kind']] == 'E' and row[ix['row']] == '機関車':
            n = row[ix['nation']]; y = int(row[ix['year']])
            if n not in ry or y < ry[n]: ry[n] = y
    railyear[s] = ry

for s in SEEDS:
    # A. cons（州×四半期）
    g = rows('cons', s)
    if g is not None:
        ix, it = g
        J = Dz = 0; nj = nb = 0; cr = ht = 0; nrow = 0; abs_lt = abs_n = 0; absv = []; hur = []
        for row in it:
            j = fl(row[ix['judgedMonthsCount']]) or 0; d = fl(row[ix['desiringMonthsCount']]) or 0
            if j <= 0: continue
            J += j; Dz += d; nj += 1
            b = fl(row[ix['builtUnitsPerMonth']]) or 0
            if b > 0: nb += 1
            if (fl(row[ix['crowdingPremiumMean']]) or 0) > 1e-9: cr += 1
            if (fl(row[ix['financialHeatMean']]) or 0) > 1e-9: ht += 1
            h = fl(row[ix['hurdleMean']]);
            if h is not None: hur.append(h)
            if d > 0:
                a = fl(row[ix['absorbFactorMean']])
                if a is not None:
                    abs_n += 1; absv.append(a)
                    if a < 0.999: abs_lt += 1
        add('H001_desiring_month_share', s, Dz / J if J else None)
        add('H001_built_rowshare', s, nb / nj if nj else None)
        add('H001_crowding_rowshare', s, cr / nj if nj else None)
        add('H001_heat_rowshare', s, ht / nj if nj else None)
        add('H001_hurdle_median', s, st.median(hur) if hur else None)
        add('H002_absorb_lt1_share', s, abs_lt / abs_n if abs_n else None)
        add('H002_absorb_median', s, st.median(absv) if absv else None)
    # consf（国×範囲）
    g = rows('consf', s)
    if g is not None:
        ix, it = g; des = bb = 0; dm = bm = 0.0
        for row in it:
            des += fl(row[ix['desiringRegionMonthsCount']]) or 0
            bb += fl(row[ix['budgetBoundRegionMonthsCount']]) or 0
            dm += fl(row[ix['desiredMoneySum']]) or 0
            bm += fl(row[ix['budgetBoundCutMoneySum']]) or 0
        add('H008_budget_bound_month_share', s, bb / des if des else None)
        add('H008_cut_money_share', s, bm / dm if dm else None)
    # B. nation の建設の配給
    g = rows('nation', s)
    if g is not None:
        ix, it = g; nq = lim = labl = 0; who = {}
        cols = ['Steel', 'Machine', 'Cement', 'Lumber', 'Labor']
        for row in it:
            bu = fl(row[ix['constructionBuiltUnitsPerMonth']]) or 0
            if bu <= 0: continue
            rs = [fl(row[ix['constructionRation' + c + 'Mean']]) for c in cols]
            if any(r is None for r in rs): continue
            nq += 1
            m = min(rs)
            if m < 0.99:
                lim += 1; k = cols[rs.index(m)]; who[k] = who.get(k, 0) + 1
            if rs[4] < 0.99: labl += 1
        add('H005_min_ration_lt099_share', s, lim / nq if nq else None)
        add('H005_binding_input', s, ','.join(f'{k}:{v}' for k, v in sorted(who.items(), key=lambda x: -x[1])) if who else '-')
        add('H006_labor_ration_lt099_share', s, labl / nq if nq else None)
        add('H005_nq', s, nq)
    # C. spend の追加の費目（年の束×国×階級）
    g = rows('spend', s)
    if g is not None:
        ix, it = g; c = {}; pre = {}; post = {}
        for row in it:
            if row[ix['kind']] != 'extra': continue
            nm = row[ix['slotName']]; pay = fl(row[ix['payPerMonth']]) or 0
            c.setdefault(nm, [0, 0]); c[nm][0] += 1
            if pay > 0: c[nm][1] += 1
            if nm == '建設に付く線路と機関車':
                y = 1783 + int(row[ix['firstQuarter']]) // 4
                ry = railyear.get(s, {}).get(row[ix['nation']])
                if ry is None or y < ry: pre[0] = pre.get(0, 0) + pay; pre[1] = pre.get(1, 0) + (1 if pay > 0 else 0)
                else: post[0] = post.get(0, 0) + pay
        for nm, (n, p) in c.items(): add('spend_' + nm + '_pay>0', s, f'{p}/{n}')
        tot = pre.get(0, 0) + post.get(0, 0)
        add('H009_rail_pay_before_raildisc_share', s, pre.get(0, 0) / tot if tot else None)
        add('H009_rail_rows_before_raildisc_pay>0', s, pre.get(1, 0))
    # D. fiscal（国×月）
    g = rows('fiscal', s)
    if g is not None:
        ix, it = g; n = 0; k = {}
        keys = ['ledgerFactoryBuildPerMonth', 'ledgerSubsidyPerMonth', 'ledgerStateFactoryPerMonth', 'ledgerMegaprojectPerMonth',
                'ledgerModernizationPerMonth', 'claimStateConstructionPerMonth', 'claimAdminPerMonth', 'claimInfraPerMonth',
                'claimElecInfraPerMonth', 'claimLastMonthFactoryBuildPerMonth', 'claimLastMonthSubsidyPerMonth', 'claimStateLossPerMonth']
        firstel = []
        for row in it:
            n += 1
            for kk in keys:
                v = fl(row[ix[kk]]) or 0
                if abs(v) > 1e-9: k[kk] = k.get(kk, 0) + 1
        for kk in keys: add('fiscal_' + kk + '!=0', s, k.get(kk, 0) / n if n else None)
    # E. eq
    g = rows('eq', s)
    if g is not None:
        ix, it = g; comp = 0.0; nq = nc = 0
        for row in it:
            v = fl(row[ix['completedScale']]) or 0; comp += v; nq += 1
            if v > 0: nc += 1
        add('H016_completedScale_sum', s, comp); add('H016_completed_nq_share', s, nc / nq if nq else None)
    # F. factory
    g = rows('factory', s)
    if g is not None:
        ix, it = g; nq = nbq = 0; B = C = 0.0; built = op = 0.0; lastB = {}
        for row in it:
            c = fl(row[ix['factoryCount']]) or 0
            if c <= 0: continue
            b = fl(row[ix['factoryBankruptCount']]) or 0
            nq += 1; C += c; B += b
            if b > 0: nbq += 1
        add('H024_bankrupt_nq_share', s, nbq / nq if nq else None)
        add('H024_bankrupt_of_factories', s, B / C if C else None)
    # G. sbr（地代と賃金）
    g = rows('sbr', s)
    if g is not None:
        ix, it = g; n = nr = 0; va = rent = 0.0; res = 0.0
        for row in it:
            if (fl(row[ix['rgoN']]) or 0) <= 0: continue
            v = fl(row[ix['rgoVa']]) or 0; rr = fl(row[ix['rgoRent']]) or 0; w = fl(row[ix['rgoWages']]) or 0
            n += 1
            if rr > 0: nr += 1
            va += v; rent += rr
            if abs(v) > 1e-6: res = max(res, abs(v - rr - w) / abs(v))
        add('H018_rent>0_rowshare', s, nr / n if n else None)
        add('H018_rent_of_va', s, rent / va if va else None)
        add('H022_max_rel_resid_va-rent-wages', s, res)
    # H. minecap
    g = rows('minecap', s)
    if g is not None:
        ix, it = g; n = n15 = 0; o = o15 = 0.0
        for row in it:
            if (fl(row[ix['producers']]) or 0) <= 0: continue
            n += 1
            if (fl(row[ix['builtOver15N']]) or 0) > 0: n15 += 1
            o += fl(row[ix['outputSum']]) or 0; o15 += fl(row[ix['builtOver15Output']]) or 0
        add('H026_rows_builtOver15>0', s, n15 / n if n else None)
        add('H026_output_share_over15', s, o15 / o if o else None)
    # I. armsfill
    g = rows('armsfill', s)
    if g is not None:
        ix, it = g; n = lt = 0; gl = 0
        for row in it:
            for e in ['rifle', 'ammo', 'artillery', 'horse']:
                if (fl(row[ix[e + 'OrderQty']]) or 0) <= 0: continue
                n += 1
                a = fl(row[ix[e + 'PriceGateShare']])
                if a is not None and a < 0.999: lt += 1
        add('H021_priceGate<0.999_share', s, lt / n if n else None)
    # J. scalars
    g = rows('scalars', s)
    if g is not None:
        ix, it = g; n = m = 0
        for row in it:
            n += 1
            if row[ix['nationalizedMaskCode']] not in ('0', '', '0.0'): m += 1
        add('H023_nationalizedMask!=0_share', s, m / n if n else None)
    # K. flow
    g = rows('flow', s)
    if g is not None:
        ix, it = g; c = {}
        for row in it:
            k = row[ix['sourceFile']]
            if k in ('Colonial.cs', 'World.cs', 'Megaprojects.cs'):
                kk = k + ':' + row[ix['sourceLine']] + '|' + row[ix['kind']]
                c[kk] = c.get(kk, 0) + 1
        for kk, v in c.items(): add('flow_' + kk + '_rows', s, v)
        add('flow_Megaprojects.cs_rows', s, sum(v for kk, v in c.items() if kk.startswith('Megaprojects')))
    # L. stock
    g = rows('stock', s)
    if g is not None:
        ix, it = g; v = []
        for row in it:
            x = fl(row[ix['capitalEquipFillShare']])
            if x is not None: v.append(x)
        add('H019_capitalEquipFill_median', s, st.median(v) if v else None)
        add('H019_capitalEquipFill_lt099_share', s, sum(1 for x in v if x < 0.99) / len(v) if v else None)

for key in sorted(acc):
    vals = acc[key]
    nums = [v for v in vals.values() if isinstance(v, (int, float)) and v is not None]
    if nums and len(nums) == len(vals):
        P(f'{key}\t種{len(vals)}\t{min(nums):.4g}〜{max(nums):.4g}\tmedian {st.median(nums):.4g}')
    else:
        P(f'{key}\t種{len(vals)}\t' + ' '.join(f's{k}={v}' for k, v in sorted(vals.items())))
print('\n'.join(out))
