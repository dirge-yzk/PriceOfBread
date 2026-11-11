"""(809) c13hl：根 A（植民地の慢性の飢え＝自給の割合が土地の持ち分に応じない）の紙の台本（旗 0 の盤面で門が落ちることの確かめ・読むだけ）。
使い方: python -I r809_paperA.py <ev_type.tsv> <出力.tsv> <ラベル>=<ディレクトリ> ...
飢えの型（an_h2 の原因〈飢え〉）の事象ごとに、ピークから 1/4 未満までの窓の四半期で
  ①主食の充足 food と 残る値打ち keep（Territory.Holding＝max(1,耕地÷農村)^0.7）の傾き（food を log(keep) に最小二乗）＝旗 0 の値
  ②旗 1 の形〈自給＝必要量×自給率 0.7×収穫×(LandFactor×Holding)＝(耕地÷農村)^0.7 を上限なしで〉の紙の見積り：自給の割合 0.7×keep（必要量で切る）が
    飢えの門 0.8 を超える四半期の割合（収穫・天候の因子は 1 と置く＝近似）
  を出す。エンジンの関数は呼ばない（region_s の列だけ）。"""
import sys, os, collections, math
def fl(x):
    try: return float(x)
    except: return float('nan')
evp, out = sys.argv[1], sys.argv[2]
sets = dict(a.split('=', 1) for a in sys.argv[3:])
ev = []
with open(evp, encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for line in fh:
        x = line.rstrip('\n').split('\t')
        if x[ix['cause']] == '飢え': ev.append((x[ix['set']], int(x[ix['seed']]), x[ix['region']], int(x[ix['peakYear']]), int(x[ix['dropYear']])))
o = open(out, 'w', encoding='utf-8', newline='\n')
o.write('set\tseed\tregion\tquarters\tfoodMean\tkeepStart\tkeepEnd\tslope_food_per_logkeep\tshareFoodBelowGate\tpaperShareSelf07xKeepAboveGate\n')
for lab, sd, reg, y0, y1 in ev:
    rows = []
    with open(os.path.join(sets[lab], 'region_s%d.tsv' % sd), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            a = line.rstrip('\n').split('\t')
            if a[ix['region']] != reg: continue
            y = int(a[ix['year']])
            if y0 <= y <= y1: rows.append((fl(a[ix['foodFillShare']]), fl(a[ix['holdingFactor']])))
    rows = [r for r in rows if math.isfinite(r[0]) and math.isfinite(r[1]) and r[1] > 0]
    n = len(rows)
    xs = [math.log(r[1]) for r in rows]; ys = [r[0] for r in rows]
    mx = sum(xs) / n; my = sum(ys) / n
    sxx = sum((x - mx) ** 2 for x in xs); sxy = sum((x - mx) * (y - my) for x, y in zip(xs, ys))
    slope = sxy / sxx if sxx > 0 else float('nan')
    below = sum(1 for y in ys if y < 0.8) / n
    paper = sum(1 for r in rows if min(1.0, 0.7 * r[1]) >= 0.8) / n
    o.write(f'{lab}\t{sd}\t{reg}\t{n}\t{my:.3f}\t{rows[0][1]:.3f}\t{rows[-1][1]:.3f}\t{slope:.4f}\t{below:.3f}\t{paper:.3f}\n')
o.close(); print('書いた', out)
