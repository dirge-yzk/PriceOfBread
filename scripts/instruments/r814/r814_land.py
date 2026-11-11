"""(814) (iv) 材料：州×四半期で LandFactor が上限 1 に当たる割合（耕地÷農村≥1＝生産は労働が律速）・耕地÷農村の分位・雇用の中央を格ごとに（読むだけ）。
使い方: python -I r814_land.py <出力.tsv> <ラベル>=<ディレクトリ> ...（耕地の未設定 1e8 以上は除く・人口 1 千人未満は除く）"""
import sys, os, math, collections
def fl(x):
    try: return float(x)
    except: return float('nan')
out = sys.argv[1]
acc = collections.defaultdict(lambda: [0, 0, [], [], 0])
for lab, d in (a.split('=', 1) for a in sys.argv[2:]):
    for sd in range(1, 21):
        p = os.path.join(d, 'region_s%d.tsv' % sd)
        if not os.path.exists(p): continue
        with open(p, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for n, line in enumerate(fh):
                a = line.rstrip('\n').split('\t')
                ar = fl(a[ix['arableLand']]); pop = fl(a[ix['populationK']]); rur = fl(a[ix['ruralPopulationK']])
                if not (ar < 1e8) or not (pop >= 1): continue
                k = (lab, a[ix['tier']]); A = acc[k]
                A[0] += 1; lf = fl(a[ix['landFactor']])
                if lf >= 1.0: A[1] += 1
                if n % 7 == 0: A[2].append(ar / max(1.0, rur)); A[3].append(fl(a[ix['employmentShare']]))
                if fl(a[ix['foodFillShare']]) < 0.8: A[4] += 1
o = open(out, 'w', encoding='utf-8', newline='\n')
o.write('set\ttier\tregionQuarters\tlandFactorAt1Share\tarablePerRuralP10\tP50\tP90\tempMedian\tfoodBelow08Share\n')
for (lab, t), A in sorted(acc.items()):
    v = sorted(x for x in A[2] if math.isfinite(x)); e = sorted(x for x in A[3] if math.isfinite(x))
    q = lambda f: format(v[int(f * (len(v) - 1))], '.3g') if v else 'nan'
    o.write('\t'.join([lab, t, str(A[0]), format(A[1] / A[0], '.3f'), q(0.1), q(0.5), q(0.9), format(e[len(e)//2], '.3f') if e else 'nan', format(A[4] / A[0], '.3f')]) + '\n')
o.close(); print('書いた', out)
