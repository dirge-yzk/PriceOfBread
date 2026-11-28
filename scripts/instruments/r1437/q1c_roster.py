# (1437) 問い1 の名簿（最終）：内陸（trstat の portFlag が 10 種で 0）・waterFlag 0・1870〜1950 年（機関車の発見の後〈推〉）
# 届かない年＝needEnd>0 かつ railTrackEnd÷needEnd<0.25。州×種で〈届かない年〉が 1870〜1950 の needEnd>0 の年の半分以上を〈止まった州×種〉と呼ぶ。
# 名簿＝止まった種の数で並べた上位（本国格〔1900 年の tier が Core・Integrated〕と植民地格を分けて各 10）。
# 調達の内訳（届かない年の財×月）：在庫 0（procureStockZeroGoodMonths）・貯蓄で削った（procureSavingsBindGoodMonths）・残り（在庫が want より少ないか全て買えた）。
# 出力：<out>/q1c_rows.tsv（止まった州×種）・<out>/q1c_roster.txt
import sys, csv, statistics as st, collections
raw, out = sys.argv[1], sys.argv[2]
port = {}
for s in range(1, 11):
    with open(f"{raw}/trstat_s{s}.tsv", encoding='utf-8') as f:
        for ln in f:
            c = ln.split('\t')
            if c[0] == 'G': port[c[3]] = max(port.get(c[3], 0), int(c[11] or 0))
tier = {}
with open(f"{out}/tier1900.tsv", encoding='utf-8') as f:
    for ln in f:
        s, n, rg, t, pop = ln.rstrip('\n').split('\t'); tier[(int(s), rg)] = (n, t, float(pop))
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except Exception: return 0.0
stopped = []
for s in range(1, 11):
    with open(f"{raw}/trrail_s{s}.tsv", newline='', encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        A = collections.defaultdict(lambda: collections.defaultdict(float))
        for row in rd:
            y = int(row[ix['year']]); rg = row[ix['region']]
            if y < 1870 or row[ix['waterFlag']] != '0' or port.get(rg, 1) != 0: continue
            need = fl(row[ix['needEnd']]); tr = fl(row[ix['railTrackEnd']])
            if not need > 0: continue
            a = A[rg]; a['n'] += 1; a['nation'] = row[ix['nation']]
            if tr / need < 0.25:
                a['bad'] += 1; a['gm'] += fl(row[ix['monthsCount']]) * 2
                a['sz'] += fl(row[ix['procureStockZeroGoodMonths']]); a['sb'] += fl(row[ix['procureSavingsBindGoodMonths']])
                a['want'] += fl(row[ix['procureWantUnitsPerYear']]); a['got'] += fl(row[ix['procureGotUnitsPerYear']])
                for b in ('Low', 'Mid', 'High'):
                    a['natW'] += fl(row[ix['natWantPhi' + b]]); a['natG'] += fl(row[ix['natGotPhi' + b]]); a['natC'] += fl(row[ix['natCallsPhi' + b]])
                a['nos'] += fl(row[ix['natNoSellerStockGoodMonths']])
        for rg, a in A.items():
            if a['n'] >= 20 and a['bad'] >= 0.5 * a['n']:
                n, t, pop = tier.get((s, rg), (a['nation'], '?', 0.0))
                stopped.append(dict(seed=s, nation=a['nation'], region=rg, tier=t, pop1900K=pop, years=a['n'], badYears=a['bad'], goodMonths=a['gm'], stockZero=a['sz'], savBind=a['sb'],
                                    want=a['want'], got=a['got'], natCalls=a['natC'], natWant=a['natW'], natGot=a['natG'], natNoSeller=a['nos']))
keys = list(stopped[0].keys())
with open(f"{out}/q1c_rows.tsv", 'w', encoding='utf-8', newline='') as f:
    f.write('\t'.join(keys) + '\n')
    for r in stopped: f.write('\t'.join(f"{r[k]:.6g}" if isinstance(r[k], float) else str(r[k]) for k in keys) + '\n')
by = collections.defaultdict(list)
for r in stopped: by[r['region']].append(r)
def line(rg, l):
    S = lambda k: sum(x[k] for x in l)
    gm = S('goodMonths')
    return (f"{rg}\t{'/'.join(sorted(set(x['nation'] for x in l)))}\t{'/'.join(sorted(set(x['tier'] for x in l)))}\t{len(l)}\t{','.join(str(x['seed']) for x in l)}\t"
            f"{st.median(x['pop1900K'] for x in l):.0f}\t{S('badYears') / S('years'):.2f}\t{S('stockZero') / gm:.2f}\t{S('savBind') / gm:.2f}\t{1 - (S('stockZero') + S('savBind')) / gm:.2f}\t"
            f"{S('got') / S('want') if S('want') > 0 else float('nan'):.3f}\t{S('natGot') / S('natWant') if S('natWant') > 0 else float('nan'):.3f}\t{S('natNoSeller') / gm:.2f}")
with open(f"{out}/q1c_roster.txt", 'w', encoding='utf-8') as f:
    f.write(f"止まった州×種 {len(stopped)}（本国格 {sum(1 for r in stopped if r['tier'] in ('Core', 'Integrated'))}・植民地格 {sum(1 for r in stopped if r['tier'] == 'Colonial')}・1900 年に無い {sum(1 for r in stopped if r['tier'] == '?')}）・州 {len(by)}\n")
    f.write("region\tnations\ttier1900\tnSeeds\tseeds\tmedPop1900K\tbadYearShare\tstockZeroShare\tsavBindShare\trestShare\tgot/want\tnatGot/natWant\tnatNoSellerShare\n")
    for lab, cond in (('本国格', lambda l: any(x['tier'] in ('Core', 'Integrated') for x in l)), ('植民地格ほか', lambda l: not any(x['tier'] in ('Core', 'Integrated') for x in l))):
        f.write(f"--- {lab}\n")
        for rg, l in sorted(((rg, l) for rg, l in by.items() if cond(l)), key=lambda kv: -len(kv[1]))[:12]: f.write(line(rg, l) + '\n')
    allc = [x for x in stopped]
    S = lambda k, L: sum(x[k] for x in L)
    for lab, L in (('全て', allc), ('本国格', [x for x in allc if x['tier'] in ('Core', 'Integrated')]), ('植民地格', [x for x in allc if x['tier'] == 'Colonial'])):
        if not L: continue
        gm = S('goodMonths', L)
        f.write(f"和 {lab}：州×種 {len(L)}・在庫 0 {S('stockZero', L) / gm:.2f}・貯蓄 {S('savBind', L) / gm:.2f}・残り {1 - (S('stockZero', L) + S('savBind', L)) / gm:.2f}・got/want {S('got', L) / S('want', L):.3f}・natGot/natWant {S('natGot', L) / max(1e-9, S('natWant', L)):.3f}・natGot÷want {S('natGot', L) / S('want', L):.3f}\n")
print('ok', len(stopped))
