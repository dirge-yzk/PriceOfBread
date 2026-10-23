"""(spa) A′ 役務の不動点の閉じた式の記録の読み手（読むだけ）。使い方：python spa_metrics.py <計器の生のフォルダ>
読む：ed_s（教育の値段＝役務の代表・計器の国だけ・四半期）・class_s（担い手の所得）。
  教育の値段の振動＝ed_s の国の priceMean の四半期ごとの差の符号が前の四半期と反転した割合（差 0 は数えない）
  帯の端の張り付き＝ed_s の priceOverRefTimesLevelIndex が 2.0 以上（賃金の帯の天井の近く）・指数の 90 分位と最大の国×四半期の割合と、続いた四半期の数の平均
  担い手の所得＝class_s の教師・医者・官僚の earnedPerMonth÷sizeK（千人あたり）の中央値（国×時点）"""
import csv, glob, os, re, sys, statistics, collections
sys.stdout.reconfigure(encoding='utf-8')
csv.field_size_limit(1 << 30)
D = sys.argv[1]


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def seeds(prefix):
    out = {}
    for f in glob.glob(os.path.join(D, prefix + '_s[0-9]*.tsv')):
        out[int(re.search(r'_s(\d+)\.tsv$', f).group(1))] = f
    return dict(sorted(out.items()))


res = collections.defaultdict(dict)
for s, f in seeds('ed').items():
    ser = collections.defaultdict(list)
    with open(f, encoding='utf-8', newline='') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            ser[r['nation']].append((int(r['quarter']), fl(r['priceMean']), fl(r['priceOverRefTimesLevelIndex'])))
    flips = pairs = edge = tot = 0; runs = []; idx = []
    for k, v in ser.items():
        v.sort(); prev = 0; run = 0
        for (q0, p0, i0), (q1, p1, i1) in zip(v, v[1:]):
            d = p1 - p0; sg = (d > 0) - (d < 0)
            if sg != 0 and p0 == p0 and p1 == p1:
                if prev != 0:
                    pairs += 1
                    if sg != prev: flips += 1
                prev = sg
        for (q, p, i) in v:
            if i == i:
                tot += 1; idx.append(i)
                if i >= 2.0: edge += 1; run += 1
                elif run: runs.append(run); run = 0
        if run: runs.append(run)
    res[s]['eduFlip'] = 100 * flips / pairs if pairs else float('nan')
    res[s]['eduEdgeShare'] = 100 * edge / tot if tot else float('nan')
    res[s]['eduEdgeRunMean'] = sum(runs) / len(runs) if runs else 0
    idx.sort(); res[s]['eduIdxP90'] = idx[int(0.9 * (len(idx) - 1))] if idx else float('nan'); res[s]['eduIdxMax'] = idx[-1] if idx else float('nan')
for s, f in seeds('class').items():
    inc = collections.defaultdict(list)
    with open(f, encoding='utf-8', newline='') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            if r['className'] in ('教師', '医者', '官僚'):
                k = fl(r['sizeK']); e = fl(r['earnedPerMonth'])
                if k > 0 and e == e: inc[r['className']].append(e / k)
    for c in ('教師', '医者', '官僚'):
        res[s]['income_' + c] = statistics.median(inc[c]) if inc[c] else float('nan')
keys = sorted({k for v in res.values() for k in v})
print('seed\t' + '\t'.join(keys))
for s in sorted(res):
    print(f'{s}\t' + '\t'.join(f'{res[s].get(k, float("nan")):.4g}' for k in keys))
vals = {k: sorted(res[s][k] for s in res if k in res[s]) for k in keys}
print('median\t' + '\t'.join(f'{statistics.median(v):.4g}' if v else 'nan' for v in vals.values()))
