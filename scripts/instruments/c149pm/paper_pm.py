# (1400) c149pm の紙の台本：基線の生 pxgoods（国×財×月の 値段÷(参照×物価) の中央値）から、
# 閉じた式の小刻み（MarketFlow.SubStep）の月あたりの回数が刻み 15 と 30 で変わる帯に入る行を数える。
# 回数は MarketFlow.cs の式を写した（エンジンの関数は呼ばない）：g＝FlowAdjustSpeed×PriceElasticity/30×amp・hs×g≦0.1・hs は h の約数。
import sys, math, glob, os
FA, PE, DIV, STEP = 0.48, 0.5, 30.0, 0.1
LNREF, AC, AE, AP = 2.9957, 0.7, 8.0, 3.0
LNCEIL = math.log(100.0); INNER = 0.5
def amp(u):
    nd = min(1.0, abs(u) / LNREF)
    return AC + (AE - AC) * (1.0 if nd == 1.0 else nd ** AP)
def hs_of(h, u):
    g = FA * PE / DIV * amp(u); hs = h
    while hs > 1 and hs * g > STEP:
        hs -= 1
        while hs > 1 and h % hs != 0: hs -= 1
    return hs
def per_month(h, u):
    if abs(u) / LNCEIL >= INNER: return None   # 帯の内側の外＝日次の写像（閉じた式でない）
    return (30 // h) * (h // hs_of(h, u))
raw = sys.argv[1]
tot = {}; n_all = 0; n_inner = 0
for p in sorted(glob.glob(os.path.join(raw, 'pxgoods_s*.tsv'))):
    with open(p, encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            if c[ix['tier']] == 'Service': continue
            try: r = float(c[ix['priceOverRefTimesLevelMedianRatio']])
            except ValueError: continue
            if not (r > 0): continue
            n_all += 1; u = math.log(r)
            a, b = per_month(15, u), per_month(30, u)
            if a is None: continue
            n_inner += 1
            tot[(a, b)] = tot.get((a, b), 0) + 1
print(f'国×財×月の行（役務を除く）={n_all}・帯の内側（閉じた式に入りうる）={n_inner}')
for (a, b), n in sorted(tot.items()):
    print(f'  月の小刻み 15→30: {a}→{b}  行 {n}  ({100.0*n/max(n_inner,1):.2f}%)')
# 帯の境（値段÷参照）
for lo, hi in ((0.8333, 1.25), (1.25, 2.0833)):
    for amp_t in (lo, hi):
        nd = ((amp_t - AC) / (AE - AC)) ** (1 / AP); u = nd * LNREF
        print(f'  amp={amp_t}: 値段÷参照 {math.exp(u):.3f} 倍・{math.exp(-u):.3f} 倍')
