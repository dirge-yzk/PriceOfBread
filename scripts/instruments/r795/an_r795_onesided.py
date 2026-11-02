#!/usr/bin/env python3
# (795) 片側の市場（金鉱の需要が産出の 1% 未満）の州が比の対数平均に占める寄与（読むだけ）。使い方：python an_r795_onesided.py <probe.tsv>
import sys, math
from collections import defaultdict
agg = defaultdict(lambda: [0, 0, 0.0, 0.0, 0, 0.0])
for line in open(sys.argv[1], encoding='utf-8'):
    f = line.rstrip('\n').split('\t')
    if f[0] != 'R' or f[6] != '1': continue
    q = int(f[2]); dAu, prAu = float(f[11]), float(f[12]); l = math.log(float(f[15]))
    a = agg[q]; a[0] += 1; a[2] += l
    one = prAu > 0 and dAu < 0.01 * prAu
    if one: a[1] += 1; a[3] += l
    if float(f[8]) / 0.54 < 0.05: a[4] += 1; a[5] += l
print('q\t州（inc・5種の計）\t片側（金鉱の需要<産出の1%）\tΣln 全\tΣln 片側\t片側を除いた rel\t金鉱<0.05×ref の州\tその Σln')
for q in sorted(agg):
    n, n1, s, s1, n2, s2 = agg[q]
    rest = math.exp((s - s1) / (n - n1)) if n > n1 else float('nan')
    print(f'{q}\t{n}\t{n1}\t{s:.1f}\t{s1:.1f}\t{rest:.4f}\t{n2}\t{s2:.1f}')
