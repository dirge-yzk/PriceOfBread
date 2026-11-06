# (917) c79db 読む線：B1〈相続人が債務も受ける〉の紙の材料。読むだけ・エンジンの関数を呼ばない・門を作らない。
#   python -X utf8 -I read917_heir.py <名> <ins フォルダ> [--seeds 1 10]
#   cpop_s の州×四半期で、空の商人（popK ≦1e-12）の末の債務 D と、同じ州の人の居る POP（popK ＞1e-12）の人数の和・末の貯蓄の和・末の債務の和を並べる。
#   相続人＝EmptyPopEstate.Step の受け手と同じ（人数 ＞1e-12・州の人数の和 ＞1e-9 のときだけ移る）。四半期の末の断面＝月の段の順の近似（記録）。
import sys, os
from collections import defaultdict
args = sys.argv[1:]
s0, s1 = 1, 10
if '--seeds' in args:
    i = args.index('--seeds'); s0, s1 = int(args[i + 1]), int(args[i + 2]); del args[i:i + 3]
tag, d = args[0], args[1]
DEAD = 1e-12
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def q(v, p):
    if not v: return float('nan')
    v = sorted(v); return v[min(len(v) - 1, int(p * (len(v) - 1)))]
rows = noHeir = cover = 0; ratio = []; Dsum = Ssum = 0.0; liveDebtSum = 0.0; liveDebtRows = 0; liveSavSum = 0.0
emptyOtherDebt = 0; emptyOtherDebtSum = 0.0
seeds = 0
for s in range(s0, s1 + 1):
    p = os.path.join(d, f'cpop_s{s}.tsv')
    if not os.path.exists(p): continue
    seeds += 1
    cur = None; acc = None
    def flush(acc):
        global rows, noHeir, cover, Dsum, Ssum, liveDebtSum, liveDebtRows, liveSavSum
        if acc is None or not (acc['D'] > 1e-9): return
        rows += 1; Dsum += acc['D']
        if not (acc['live'] > 1e-9): noHeir += 1; return
        Ssum += acc['sav']; liveSavSum += acc['sav']
        if acc['sav'] >= acc['D']: cover += 1
        ratio.append(acc['sav'] / acc['D'])
        liveDebtSum += acc['ldebt']
        if acc['ldebt'] > 1e-9: liveDebtRows += 1
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        iq, ir, icl, ipk, isv, idb = ix['quarter'], ix['region'], ix['className'], ix['popK'], ix['savings'], ix['debt']
        for line in f:
            x = line.rstrip('\n').split('\t')
            k = (x[iq], x[ir])
            if k != cur:
                flush(acc); cur = k; acc = {'D': 0.0, 'live': 0.0, 'sav': 0.0, 'ldebt': 0.0}
            pk = fl(x[ipk]); db = fl(x[idb])
            if pk > DEAD:
                acc['live'] += pk; acc['sav'] += max(0.0, fl(x[isv])); acc['ldebt'] += max(0.0, db)
            elif x[icl] == '商人':
                if db > 0: acc['D'] += db
            elif db > 1e-9:
                emptyOtherDebt += 1; emptyOtherDebtSum += db
        flush(acc)
r = lambda a, b: a / b if b else float('nan')
print(f'# 名 {tag}・フォルダ {d}・種 {seeds}（州×四半期の行が州ごとに続けて並ぶ前提＝cpop_s の書き順）')
print(f'空の商人の末の債務 ＞1e-9 の州×四半期 {rows}・相続人なし（州の人の居る人数 ≦1e-9）{noHeir}（{r(noHeir, rows):.4f}）')
print(f'相続人ありの中で、相続人の末の貯蓄の和 ≧ 空の商人の債務 {cover}（{r(cover, rows - noHeir):.4f}）・貯蓄÷債務 10 点 {q(ratio, .1):.4g}・中央 {q(ratio, .5):.4g}・90 点 {q(ratio, .9):.4g}')
print(f'額：空の商人の債務の和 {Dsum:.6g}・相続人の貯蓄の和 {Ssum:.6g}・相続人の債務の和 {liveDebtSum:.6g}（相続人に債務のある州×四半期 {liveDebtRows}）')
print(f'商人以外の空の POP で債務 ＞1e-9 の行 {emptyOtherDebt}・額の和 {emptyOtherDebtSum:.6g}')
