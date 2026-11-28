"""(1430) 問い 2：アメリカの州（持ち主がアメリカの行）の 農村÷耕地・人口・Holding・LandFactor の推移（10 年ごと・各 10 年の最初の四半期）。読むだけ。
python -I -X utf8 q2_east_rural.py <計器の出力ディレクトリ> <種の数=10> [国=アメリカ]
出力：州ごと・年代ごとに 種の中央値（最小〜最大）の 農村÷耕地 と 人口、と Holding・LandFactor の中央値。持ち主が替わった種は数えない（その四半期の nation で絞る）。"""
import sys, os, collections, statistics as st
d = sys.argv[1]; n = int(sys.argv[2]) if len(sys.argv) > 2 else 10; NAT = sys.argv[3] if len(sys.argv) > 3 else 'アメリカ'
acc = collections.defaultdict(lambda: collections.defaultdict(list))   # region -> decade -> [(ratio,pop,hold,land,rural)]
for s in range(1, n + 1):
    p = os.path.join(d, f'region_s{s}.tsv')
    if not os.path.exists(p):
        p2 = [f for f in os.listdir(d) if f.startswith('region_s') and f.endswith('.tsv')]
        continue
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            v = line.rstrip('\n').split('\t')
            if v[ix['nation']] != NAT: continue
            q = int(v[ix['quarter']]); yr = int(float(v[ix['year']]))
            if yr % 10 != 3 or q % 4 != 0: continue   # 1783・1793…の最初の四半期
            ar = float(v[ix['arableLand']]); ru = float(v[ix['ruralPopulationK']]); pop = float(v[ix['populationK']])
            acc[v[ix['region']]][yr].append((ru / ar if ar > 0 else float('nan'), pop, float(v[ix['holdingFactor']]), float(v[ix['landFactor']]), ru, ar))
def md(xs): return st.median(xs) if xs else float('nan')
for reg in sorted(acc, key=lambda r: -md([t[1] for t in acc[r][1783]]) if acc[r].get(1783) else 0):
    print(f'## {reg}（{NAT}の持つ種だけ）耕地 {md([t[5] for t in next(iter(acc[reg].values()))]):.0f}')
    print('年\t種\t農村÷耕地 中央(最小〜最大)\t人口 中央(最小〜最大)\t農村 中央\tHolding 中央\tLandFactor 中央')
    for yr in sorted(acc[reg]):
        t = acc[reg][yr]; r = [x[0] for x in t]; pp = [x[1] for x in t]
        print(f'{yr}\t{len(t)}\t{md(r):.3g}({min(r):.3g}〜{max(r):.3g})\t{md(pp):.4g}({min(pp):.4g}〜{max(pp):.4g})\t{md([x[4] for x in t]):.4g}\t{md([x[2] for x in t]):.3g}\t{md([x[3] for x in t]):.3g}')
