# r789: 都市の人口（人口−農村）がピークの 1/4 未満へ落ちた州（都市のピーク≧UMIN 千人）を、減りの始まりの年の
#   〈州の 1 人あたり所得の対国平均（wagePremium）〉と〈土地の持ち分 holdingFactor〉で分ける。読むだけ。
# 引数：<label=dir> ...　環境変数 UMIN（既定 300）
# 減りの始まり yd＝ピークの後で都市が初めてピークの 0.9 を割った年（年平均）。
#   所得が先（ロッテルダム型）＝yd の wagePremium < 0.8 かつ [yd−10, yd] の最大の wagePremium から 0.3 以上下げた
#   所得は高いまま（チャールストン型）＝yd の wagePremium ≥ 1.0
import sys, os, collections
sys.stdout.reconfigure(encoding='utf-8')
UMIN = float(os.environ.get('UMIN', '300'))
f = float
rows = []
for arg in sys.argv[1:]:
    lab, D = arg.split('=', 1)
    for fn in sorted(os.listdir(D)):
        if not (fn.startswith('region_s') and fn.endswith('.tsv')): continue
        sd = fn[8:-4]
        A = collections.defaultdict(lambda: collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0.0, 0, '', '']))
        with open(os.path.join(D, fn), encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for l in fh:
                c = l.rstrip('\n').split('\t')
                a = A[c[ix['region']]][int(c[ix['year']])]
                pop = f(c[ix['populationK']]); a[0] += pop - f(c[ix['ruralPopulationK']]); a[1] += f(c[ix['wagePremium']]); a[2] += f(c[ix['holdingFactor']]); a[3] += f(c[ix['opportunityIndex']]); a[4] += 1
                a[5] = c[ix['nation']]; a[6] = c[ix['tier']]
        for reg, Y in A.items():
            ys = sorted(Y); U = {y: Y[y][0] / Y[y][4] for y in ys}
            yp = max(ys, key=lambda y: U[y]); Up = U[yp]
            if Up < UMIN: continue
            after = [y for y in ys if y >= yp]
            ym = min(after, key=lambda y: U[y]); r = U[ym] / Up
            if not (r < 0.25): continue
            yd = next(y for y in after if U[y] < 0.9 * Up)
            W = {y: Y[y][1] / Y[y][4] for y in ys}
            wmax = max(W[y] for y in ys if yd - 10 <= y <= yd)
            typ = 'income-first' if (W[yd] < 0.8 and wmax - W[yd] >= 0.3) else 'high-income' if W[yd] >= 1.0 else 'other'
            rows.append((lab, sd, reg, Y[yp][5], Y[yd][5], Y[yd][6], yp, round(Up), yd, ym, f'{r:.3f}', f'{wmax:.2f}', f'{W[yd]:.2f}', f'{Y[yd][2] / Y[yd][4]:.2f}', f'{Y[yd][3] / Y[yd][4]:.3f}', typ))
print('label\tseed\tregion\townerPeak\townerDecline\ttierDecline\tyPeak\turbanPeakK\tyDecline\tyMin\tminOverPeak\twagePremMax10\twagePremDecline\tholdDecline\toppDecline\ttype')
for r in rows: print(*r, sep='\t')
print('## 件数', sorted(collections.Counter((r[0], r[-1]) for r in rows).items()))
