# -*- coding: utf-8 -*-
# (842) c68id 紙の台本：cgrp_s の Started でない四半期の行の見込みを、基準の生（修正前の cgrp_s）から代理で数える。
# 代理＝savingsStart が厳密に 0 かつ monthsClosed が 0（その四半期に月の窓が 1 度も閉じていない＝台帳が月初の登録の前）の行。
# 使い方：python cgrp_nan_paper.py <基準の生のフォルダ> [<腕の生のフォルダ>]
#   基準だけ：種ごとの代理の行数と、その行の residualPerQuarter の絶対値の和の中央（nan に替わる見込みの行）を出す。
#   腕も渡す：基準と腕の cgrp_s を行ごとに比べ、違う行の数・違う列の集合・違う行が代理の行の集合に含まれるかを出す。
# 限界：代理は〈台帳が始まっていない〉の厳密な定義（CearnAcc.Started）ではない。違う行が代理の集合の部分集合であること（腕の違い ⊂ 代理）を確かめる。
import sys, os
sys.stdout.reconfigure(encoding='utf-8')
base = sys.argv[1]
arm = sys.argv[2] if len(sys.argv) > 2 else None
SEEDS = range(1, 11)

def rows(path):
    with open(path, 'rb') as f:
        head = f.readline().decode('utf-8').rstrip('\r\n').split('\t')
        for raw in f:
            yield head, raw.decode('utf-8').rstrip('\r\n').split('\t')

def key(head, a):
    d = dict(zip(head, a))
    return (d['quarter'], d['nation'], d['region'], d['group']), d

tot_rows = tot_proxy = 0
tot_diff = tot_diff_in_proxy = 0
diff_cols = {}
for s in SEEDS:
    bp = os.path.join(base, 'cgrp_s%d.tsv' % s)
    if not os.path.exists(bp):
        print('欠け 基準 種', s); continue
    proxy = {}
    n = 0
    bmap = {}
    for head, a in rows(bp):
        k, d = key(head, a)
        n += 1
        bmap[k] = a
        if float(d['savingsStart']) == 0.0 and int(d['monthsClosed']) == 0:
            proxy[k] = float(d['residualPerQuarter'])
    tot_rows += n; tot_proxy += len(proxy)
    print('種 %d 基準の行 %d  代理の行（savingsStart=0 かつ monthsClosed=0）%d' % (s, n, len(proxy)))
    if arm:
        ap = os.path.join(arm, 'cgrp_s%d.tsv' % s)
        if not os.path.exists(ap):
            print('  欠け 腕 種', s); continue
        nd = nd_in = 0
        for head, a in rows(ap):
            k, d = key(head, a)
            b = bmap.get(k)
            if b is None:
                print('  腕にだけある行', k); continue
            if a != b:
                nd += 1
                if k in proxy: nd_in += 1
                for i, (x, y) in enumerate(zip(a, b)):
                    if x != y: diff_cols[head[i]] = diff_cols.get(head[i], 0) + 1
        tot_diff += nd; tot_diff_in_proxy += nd_in
        print('  腕と基準で違う行 %d（うち代理の集合の中 %d）' % (nd, nd_in))
print('合計 基準の行 %d  代理の行 %d' % (tot_rows, tot_proxy))
if arm:
    print('合計 違う行 %d  代理の集合の中 %d  違う列 %s' % (tot_diff, tot_diff_in_proxy, sorted(diff_cols.items())))
