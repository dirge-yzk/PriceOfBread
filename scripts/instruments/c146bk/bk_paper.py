# (1378) c146bk 倒産した工場の数と労働の失業の割合を種ごとに数える（読むだけ・エンジンの関数を呼ばない）。
# 使い方: python bk_paper.py <計器の生のフォルダ> [<比べる生のフォルダ>]
# 列: seed・倒産の数の四半期あたりの世界の和の平均・倒産の数の増えた和・減った和（領土の移りを含む）・工場の数の平均・平易な労働の失業の割合の平均（国×四半期の単純平均）
import sys, glob, os, re

def seeds(d):
    out = {}
    for f in glob.glob(os.path.join(d, 'factory_s*.tsv')):
        m = re.search(r'_s(\d+)\.tsv$', f)
        if m: out[int(m.group(1))] = f
    return out

def read_factory(f):
    h = None; byq = {}; prev = {}; up = dn = 0.0; nf = {}
    for line in open(f, encoding='utf-8'):
        c = line.rstrip('\n').split('\t')
        if h is None: h = {k: i for i, k in enumerate(c)}; continue
        q = c[h['quarter']]; n = c[h['nationUid']]
        b = float(c[h['factoryBankruptCount']]); a = float(c[h['factoryCount']])
        byq[q] = byq.get(q, 0.0) + b; nf[q] = nf.get(q, 0.0) + a
        if n in prev:
            if b < prev[n]: dn += prev[n] - b
            elif b > prev[n]: up += b - prev[n]
        prev[n] = b
    nq = max(len(byq), 1)
    return sum(byq.values()) / nq, up, dn, sum(nf.values()) / nq

def read_unemp(f):
    if not os.path.exists(f): return float('nan')
    h = None; s = 0.0; k = 0
    for line in open(f, encoding='utf-8'):
        c = line.rstrip('\n').split('\t')
        if h is None: h = {x: i for i, x in enumerate(c)}; continue
        try: v = float(c[h['plainUnemploymentShare']])
        except ValueError: continue
        if v == v: s += v; k += 1
    return s / k if k else float('nan')

def summary(d):
    rows = {}
    for sd, f in sorted(seeds(d).items()):
        bmean, up, dn, fmean = read_factory(f)
        u = read_unemp(os.path.join(d, f'labor_s{sd}.tsv'))
        rows[sd] = (bmean, up, dn, fmean, u)
    return rows

a = summary(sys.argv[1])
b = summary(sys.argv[2]) if len(sys.argv) > 2 else None
print('seed\tbankruptMean\tbankruptUp\tbankruptDown\tfactoryMean\tplainUnempMean' + ('\tbankruptMean_new_over_old\tunemp_new_over_old' if b else ''))
tot = [0.0] * 5; totb = [0.0] * 5
for sd, r in a.items():
    line = f'{sd}\t' + '\t'.join(f'{x:.4f}' for x in r)
    for i in range(5): tot[i] += r[i]
    if b and sd in b:
        o = b[sd]
        for i in range(5): totb[i] += o[i]
        line += f'\t{r[0] / o[0] if o[0] else float("nan"):.4f}\t{r[4] / o[4] if o[4] else float("nan"):.4f}'
    print(line)
n = max(len(a), 1)
print('mean\t' + '\t'.join(f'{x / n:.4f}' for x in tot) + (f'\t{tot[0] / totb[0]:.4f}\t{tot[4] / totb[4]:.4f}' if b and totb[0] and totb[4] else ''))
