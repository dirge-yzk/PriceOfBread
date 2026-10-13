"""(294) 読むだけ：nation_s の国×四半期の行を 利払い÷歳入 の帯に分けて数える（借入の門の線を動かすと門が開く行の数・全国への響きの見積もり）
python gate_cells.py <ins> → 帯＝<0.55 / 0.55-0.67 / 0.67-0.70 / 0.70-0.75 / >=0.75（利払い・歳入が正の行だけ）"""
import sys, glob, collections, os
cnt = collections.Counter(); nat = collections.defaultdict(collections.Counter); tot = 0
for p in glob.glob(os.path.join(sys.argv[1], 'nation_s*.tsv')):
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
        for ln in fh:
            r = ln.rstrip('\n').split('\t')
            try: I = float(r[ix['interestMeanPerMonth']]); R = float(r[ix['revenueBaseMeanPerMonth']])
            except ValueError: continue
            if R <= 0 or I <= 0: continue
            x = I / R; n = r[ix['nation']]; tot += 1
            b = '<0.55' if x < 0.55 else '0.55-0.67' if x < 0.67 else '0.67-0.70' if x < 0.70 else '0.70-0.75' if x < 0.75 else '>=0.75'
            cnt[b] += 1; nat[n][b] += 1
print('行数', tot, dict(cnt))
for n, c in nat.items():
    if c['0.55-0.67'] or c['0.67-0.70'] or c['0.70-0.75']: print(n, dict(c))
