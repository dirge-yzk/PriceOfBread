# (1013) recipeout_s の読み手：ident の生（計器つき 10 種）から T1（量の保存）・T3（級と倍率）・級の行数・G3 の基線・級別の単位費用を数える。門ではなく記録。
import csv, sys, os, statistics, collections
ins = sys.argv[1]; seeds = range(1, 11)
MIN = {'鉄鉱石','石炭','銅鉱','鉛鉱','金','硫黄','石灰石','珪砂','粘土','原油','クロム','錫鉱','硝石','銀'}
def rd(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for row in r: yield dict(zip(h, row))
def fl(x): return float('nan') if x == 'nan' else float(x)
tot_t1 = bad_t1 = pos_t1 = 0; cls_rows = collections.Counter(); bad_t3 = collections.Counter(); n_rows = 0
ex_all = collections.Counter(); ex_src = collections.Counter()
cost = collections.defaultdict(list); ab = collections.defaultdict(list)
worst = []
for s in seeds:
    p = os.path.join(ins, f'recipeout_s{s}.tsv')
    if not os.path.exists(p): print('欠け', p); continue
    osum = collections.defaultdict(float); has = collections.defaultdict(set)
    for r in rd(p):
        n_rows += 1; c = r['class']; cls_rows[c] += 1
        k = (r['year'], r['nation'], r['good']); o = fl(r['outputSum']); osum[k] += o
        if c in ('E', 'X') and int(r['producers']) > 0: has[(r['year'], r['nation'], r['good'])].add(c)
        e = fl(r['endowFromProductivity'])
        if c == 'F': ok = e != e
        elif c == 'E' or c == 'X': ok = e > 1 + 1e-9
        elif c == 'N': ok = abs(e - 0.4) < 1e-9
        elif c == 'S': ok = abs(e - 1.0) < 1e-9
        elif c == 'Z': ok = e == 0
        else: ok = False
        if not ok: bad_t3[c] += 1
        if r['year'] in ('1790', '1913') and c in ('E', 'N', 'S') and o > 0 and r['recipe'] not in ('(既定)',):
            cost[(r['year'], c, r['good'])].append((o, fl(r['unitCostOutW']), fl(r['unitCostIfAbundance'])))
    for r in rd(os.path.join(ins, f'nationgood_s{s}.tsv')):
        k = (r['year'], r['nation'], r['good']); want = fl(r['producedQty']); got = osum.get(k, 0.0)
        tot_t1 += 1; pos_t1 += want > 1e-6
        if abs(got - want) > 1e-9 * max(abs(want), 1e-300) + 1e-12:
            bad_t1 += 1
            if len(worst) < 8: worst.append((s, k, got, want))
        if r['good'] in ('鉄鉱石', '石炭'):
            q = fl(r['crossExportQty']); ex_all[(r['good'])] += q
            if has.get(k): ex_src[(r['good'])] += q
print(f'行数（10 種の合計）{n_rows}・種あたり {n_rows/10:.0f}')
print('級別の行数', dict(cls_rows))
print(f'T1 量の保存：調べた (種,年,国,財) {tot_t1}・産出が正 {pos_t1}・食い違い {bad_t1}', worst)
print('T3 粗い突き合わせ（級と endowFromProductivity）の食い違い', dict(bad_t3) or 0)
for g in ('鉄鉱石', '石炭'):
    print(f'G3 基線 {g}：越境の輸出量のうち、級 E か X の行を持つ国が出した割合 {ex_src[g]/ex_all[g] if ex_all[g] else float("nan"):.4f}（輸出量合計 {ex_all[g]:.1f}）')
def wmed(v):
    v = sorted(v, key=lambda t: t[1]); tot = sum(t[0] for t in v); a = 0
    for o, c, _ in v:
        a += o
        if a >= tot / 2: return c
for yr in ('1790', '1913'):
    print(f'--- {yr}：財ごとの級別の産出重みつき中央値の単位費用（E／N／S と E÷N、E の豊富側費用÷単位費用）')
    gs = sorted({k[2] for k in cost if k[0] == yr})
    rows = []
    for g in gs:
        ms = {c: wmed([(a, b, d) for (a, b, d) in cost[(yr, c, g)] if b == b]) for c in 'ENS' if cost.get((yr, c, g))}
        if 'E' in ms and ('N' in ms or 'S' in ms):
            ab_e = statistics.median([d / b for (a, b, d) in cost[(yr, 'E', g)] if b > 0])
            rows.append((g, ms.get('E'), ms.get('N'), ms.get('S'), ab_e))
    for g, e, n, s_, a in rows[:40]:
        print(f'  {g}{"(鉱物)" if g in MIN else ""}\tE={e:.4f}\tN={"-" if n is None else f"{n:.4f}"}\tS={"-" if s_ is None else f"{s_:.4f}"}\tE/N={"-" if n is None else f"{e/n:.3f}"}\t豊富側/単位={a:.3f}')
    em = [r[1] for r in rows if r[0] in MIN]; en = [r[1] for r in rows if r[0] not in MIN]
    print(f'  級 E の単位費用の中央値：鉱物 {statistics.median(em) if em else float("nan"):.4f}（紙 0.285）・鉱物以外 {statistics.median(en) if en else float("nan"):.4f}（紙 0.297）')
