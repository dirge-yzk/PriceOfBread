# (791) c10cp 止める線の読み：nation_s の名目 GDP（nominalGdpPerYear）の 1 四半期の比（国×種ごとの系列・nationUid が鍵）の最大と 100 倍以上・1e10 倍以上・非有限の行の数。
#   使い方: python -I -X utf8 gdp_jump.py <ins フォルダ> [名前の出力 0/1]
#   建国の最初の四半期（系列の最初の行）は比を取らない。比が 100 以上の行は国・年・種を出す（建国の直後の型はヌル腕の件数と比べる＝基準 1・N1 2・N2 2・波59 c9op の反証役の集計）。
import csv, glob, math, os, sys
d = sys.argv[1]
seeds = sorted(int(os.path.basename(p)[len('nation_s'):-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))
j100 = j1e10 = nonf = 0; rmax = (0, '', 0, 0, 0); big = []
for s in seeds:
    with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
        iu, inm, iy, iq, ig = ix['nationUid'], ix['nation'], ix['year'], ix['quarter'], ix['nominalGdpPerYear']
        prev = {}
        for x in r:
            try: g = float(x[ig])
            except ValueError: g = float('nan')
            u = x[iu]
            if not math.isfinite(g): nonf += 1; continue
            if u in prev and prev[u] > 0:
                ra = g / prev[u]
                if ra >= 100: j100 += 1; big.append((s, x[inm], int(x[iy]), int(x[iq]), ra))
                if ra >= 1e10: j1e10 += 1
                if ra > rmax[0]: rmax = (ra, x[inm], int(x[iy]), int(x[iq]), s)
            prev[u] = g
print(f'{d}：名目 GDP の 1 四半期の比 最大 {rmax[0]:.3f}（{rmax[1]} {rmax[2]}年 Q{rmax[3]} 種{rmax[4]}）・100 以上 {j100}・1e10 以上 {j1e10}・非有限 {nonf}')
for b in big[:20]: print('  ', b)
