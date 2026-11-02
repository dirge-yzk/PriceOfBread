#!/usr/bin/env python3
# (795) 紙の予言：比の対数平均を金鉱と銀の取引量で重みづけたら rel はどうなるか（読むだけ・同じ盤面＝挟みありの腕 A の R 行）。
# 重み w1＝金鉱の需要 dAu・w2＝min(金鉱の需要, 銀の需要)・w3＝min(dAu,prAu,dAg,prAg) を産出も含めた取引の両側。種ごとに rel を出して最大を表に。
import sys, math
from collections import defaultdict
S = defaultdict(lambda: defaultdict(lambda: [0.0]*6 + [0, 0.0]))
for line in open(sys.argv[1], encoding='utf-8'):
    f = line.rstrip('\n').split('\t')
    if f[0] != 'R' or f[6] != '1': continue
    s, q = int(f[1]), int(f[2]); dAg, prAg, dAu, prAu = map(float, (f[9], f[10], f[11], f[12])); l = math.log(float(f[15]))
    a = S[q][s]
    for i, w in enumerate((dAu, min(dAu, dAg), min(dAu, prAu, dAg, prAg) if min(dAu, prAu, dAg, prAg) > 0 else 0.0)):
        a[2*i] += w * l; a[2*i+1] += w
    a[6] += 1; a[7] += l
print('q\t等重み rel（種の最大）\tw1 金鉱の需要\tw2 min(金鉱,銀の需要)\tw3 両側の取引（産出も）')
for q in sorted(S):
    def r(i):
        v = [math.exp(a[2*i]/a[2*i+1]) if a[2*i+1] > 0 else float('nan') for a in S[q].values()]
        return f'{min(v):.4f}〜{max(v):.4f}'
    eq = [math.exp(a[7]/a[6]) for a in S[q].values()]
    print(f'{q}\t{min(eq):.4f}〜{max(eq):.4f}\t{r(0)}\t{r(1)}\t{r(2)}')
