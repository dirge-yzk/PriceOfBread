# (1063) c101py 形 Y の門 G1（両側）を procx_s から数える（事前登録 measurements/m1063_20261112_prereg_py.txt の §5）。
#   上限：termActual ≦ want×(1＋1e-9)＋tol（違反 0 行で通る）／下限：termActual ≧ want×(1−1e-9)−tol（違反 0 行で通る）
#   母集合の床：want＞0 の行が全ての種で 1 行以上。
#   使い方：python gates_py.py <計器の出力フォルダ> [種の数 10]
import os, sys

d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10
tot = {'rows': 0, 'buy': 0, 'up': 0, 'lo': 0}
seeds_with_rows = 0; worst_up = 0.0; worst_lo = 0.0; ex_up = None; ex_lo = None
sum_want = sum_buy = sum_old = sum_act = sum_new = 0.0
flags = set(); missing = []
for s in range(1, ns + 1):
    p = os.path.join(d, 'procx_s%d.tsv' % s)
    if not os.path.exists(p): missing.append(s); continue
    n_s = 0
    with open(p, encoding='utf-8') as f:
        hdr = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hdr)}
        for line in f:
            v = line.rstrip('\n').split('\t')
            want = float(v[ix['want']]); buy = float(v[ix['buy']]); tol = float(v[ix['tol']])
            act = float(v[ix['termActual']]); old = float(v[ix['termOld']]); new = float(v[ix['termNew']])
            flags.add(v[ix['flag']])
            if not (want > 0): continue
            n_s += 1; tot['rows'] += 1
            if buy > 0: tot['buy'] += 1
            sum_want += want; sum_buy += buy; sum_old += old; sum_act += act; sum_new += new
            up = act - (want * (1 + 1e-9) + tol); lo = (want * (1 - 1e-9) - tol) - act
            if not (up <= 0):
                tot['up'] += 1
                r = (act / want - 1)
                if r > worst_up: worst_up = r; ex_up = (s, v[ix['quarter']], v[ix['nation']], v[ix['region']], v[ix['good']], want, buy, act)
            if not (lo <= 0):
                tot['lo'] += 1
                r = 1 - act / want
                if r > worst_lo: worst_lo = r; ex_lo = (s, v[ix['quarter']], v[ix['nation']], v[ix['region']], v[ix['good']], want, buy, act)
    if n_s > 0: seeds_with_rows += 1
print('procx_s の門 G1（%s）・旗の値 %s・欠けた種 %s' % (d, ','.join(sorted(flags)), missing or 'なし'))
print('  母集合 want>0 の行 %d（buy>0 %d）・行のある種 %d/%d' % (tot['rows'], tot['buy'], seeds_with_rows, ns))
print('  G1 上限の違反 %d 行（最大 termActual/want−1 = %.6g）例 %s' % (tot['up'], worst_up, ex_up))
print('  G1 下限の違反 %d 行（最大 1−termActual/want = %.6g）例 %s' % (tot['lo'], worst_lo, ex_lo))
if sum_want > 0:
    print('  Σ：termActual/want %.9f・termOld/want %.9f・termNew/want %.9f・buy/want %.6f' % (sum_act / sum_want, sum_old / sum_want, sum_new / sum_want, sum_buy / sum_want))
floor_ok = seeds_with_rows == ns and not missing
print('  判定：上限 %s・下限 %s・母集合の床 %s' % ('通る' if tot['up'] == 0 else '落ちる', '通る' if tot['lo'] == 0 else '落ちる', '通る' if floor_ok else '落ちる'))
