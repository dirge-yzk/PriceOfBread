# (603) 線 pfix の読み手（読むだけ）：計器つきの生の money_s<種>.tsv（NATIONS の 9 国・四半期ごと）から、価格水準の発散の観測量を種ごとに出す。
#   使い方：python g_pfix.py <ins_dir> [<ins_dir> ...]   （例：D:/PoB_work/keep_600/m600_ins）
#   出す量（事前登録 pfix603_20261028_prereg.txt §7 の G1〜G3・G5）：
#     rows        四半期の行数（time が q で始まる行）
#     bound_viol  G3＝priceLevelAfterIndex > max(max(Before, cyclicalRatio) × (1 + W×3), 床) の行数（W＝FiatInflationWeight 0.6・月 3 回の圧が各 1 以下の物の上限）
#     jump100     G1（9 国の側）＝ln(After/Before) ≥ ln 100 の行数
#     maxdlnp     ln(After/Before) の最大（国・四半期つき）
#     maxP        priceLevelAfterIndex の最大（国・四半期つき）
#     nonfinite   P が有限でない行
#     broad_le0   broadMoneyStock ≤ 0 の行（G5 の記録）
#     gdp_le0     gdpRealPerMonth ≤ 1e-9 の行（旗 PriceTrendHoldAtZeroGdp が働く入口）
#   係数は引数で変えられる：W=0.6 FLOOR=0.4 は事前登録の既定（Coefficients.FiatInflationWeight・PriceLevelFloor）。
import csv, sys, math, os, glob
sys.stdout.reconfigure(encoding='utf-8')
W = float(os.environ.get('PFIX_W', '0.6')); FLOOR = float(os.environ.get('PFIX_FLOOR', '0.4'))
def f(x):
    try: return float(x)
    except: return float('nan')
def scan(path):
    o = dict(rows=0, bound_viol=0, jump100=0, nonfinite=0, broad_le0=0, gdp_le0=0, maxd=(-1e99, '', ''), maxp=(0.0, '', ''))
    for r in csv.DictReader(open(path, encoding='utf-8'), delimiter='\t'):
        if not r['time'].startswith('q'): continue
        pb, pa, cy = f(r['priceLevelBeforeIndex']), f(r['priceLevelAfterIndex']), f(r['cyclicalRatio'])
        if pb != pb or pa != pa: continue
        o['rows'] += 1
        if not math.isfinite(pa): o['nonfinite'] += 1; continue
        if not (pa <= max(max(pb, cy) * (1 + W * 3), FLOOR) * (1 + 1e-9)): o['bound_viol'] += 1
        if pb > 0 and pa > 0:
            d = math.log(pa / pb)
            if d >= math.log(100): o['jump100'] += 1
            if d > o['maxd'][0]: o['maxd'] = (d, r['nation'], r['time'])
        if pa > o['maxp'][0]: o['maxp'] = (pa, r['nation'], r['time'])
        if f(r['broadMoneyStock']) <= 0: o['broad_le0'] += 1
        if f(r['gdpRealPerMonth']) <= 1e-9: o['gdp_le0'] += 1
    return o
for d in sys.argv[1:]:
    print('##', d)
    tot = dict(rows=0, bound_viol=0, jump100=0, nonfinite=0, broad_le0=0, gdp_le0=0)
    for p in sorted(glob.glob(os.path.join(d, 'money_s*.tsv')), key=lambda s: int(''.join(c for c in os.path.basename(s) if c.isdigit()) or 0)):
        o = scan(p)
        for k in tot: tot[k] += o[k]
        print(os.path.basename(p), 'rows', o['rows'], 'bound_viol', o['bound_viol'], 'jump100', o['jump100'], 'nonfinite', o['nonfinite'],
              'maxdlnp %.3f(%s %s)' % o['maxd'], 'maxP %.4g(%s %s)' % o['maxp'], 'broad<=0', o['broad_le0'], 'gdp<=1e-9', o['gdp_le0'])
    print('TOTAL', tot)
