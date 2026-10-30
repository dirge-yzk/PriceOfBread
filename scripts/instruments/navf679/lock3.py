#!/usr/bin/env python
"""(679) 線 navf4：README ★(397) の〈壊す〉の物差し ③ 片方向の錠 の読み手（読むだけ）。
  ③ 最大の段に張り付く国×四半期：monlaw_s（全ての国×四半期・段の法の全列）で、列ごとに〈全ての腕の和集合での最大の段〉を取り、
     その段に居る国×四半期の行を数えて法の列にわたって足す。B の合計が ヌル腕（N1・N2）の最大の 2 倍以上、かつ差が全行の 0.5% 以上なら鳴る（ar/stop.py ⑥③・m533/lock533.py と同じ作法の差の床）。
  使い方：python -X utf8 -I lock3.py <B の ins> <A の ins> <N1 の ins> <N2 の ins> [種の起点=1] [種の数=10]
  出力は標準出力。判定行は〈鳴る〉〈鳴らない〉。記録として列ごとの内訳（B が多い順の上位 8）を出す。
"""
import csv, glob, os, sys
from collections import defaultdict

SKIP_PREFIX = ('lawGateOpen:',)
SKIP_COLS = {'quarter', 'year', 'nationUid', 'nationIndex', 'nation', 'polityCode', 'blockadeRiskShare', 'EducationLawTarget'}


def law_cols(h):
    return [c for c in h if c not in SKIP_COLS and not c.startswith(SKIP_PREFIX) and not c.endswith('Target')]   # 〜Target は実際の段の目標＝同じ法を二重に数えない


def scan(d, seeds):
    """列 → {値: 行数}・全行数"""
    hist = defaultdict(lambda: defaultdict(int)); tot = 0
    for s in seeds:
        p = os.path.join(d, f'monlaw_s{s}.tsv')
        if not os.path.exists(p):
            continue
        with open(p, encoding='utf-8-sig', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r)
            cols = law_cols(h); ix = [h.index(c) for c in cols]
            for x in r:
                if len(x) != len(h):
                    continue
                tot += 1
                for c, i in zip(cols, ix):
                    try:
                        v = float(x[i])
                    except ValueError:
                        continue
                    if v == v:
                        hist[c][v] += 1
    return hist, tot


def main():
    a = sys.argv[1:]
    if len(a) < 4:
        print(__doc__); sys.exit(2)
    sb = int(a[4]) if len(a) > 4 else 1; ns = int(a[5]) if len(a) > 5 else 10
    seeds = list(range(sb, sb + ns))
    arms = [('B', a[0]), ('A', a[1]), ('N1', a[2]), ('N2', a[3])]
    H = {}; T = {}
    for k, d in arms:
        H[k], T[k] = scan(d, seeds)
    cols = sorted(set().union(*[set(H[k]) for k in H]))
    top = {c: max(v for k in H for v in H[k].get(c, {})) for c in cols if any(H[k].get(c) for k in H)}
    tot = {k: sum(H[k][c].get(top[c], 0) for c in top) for k in H}
    print('③ 片方向の錠（段の法の列ごとの最大の段に居る国×四半期の行数の和・monlaw_s・種 %d〜%d）' % (seeds[0], seeds[-1]))
    print('   列の数=%d  全行 B=%d A=%d N1=%d N2=%d' % (len(top), T['B'], T['A'], T['N1'], T['N2']))
    print('   合計 B=%d  A=%d  N1=%d  N2=%d' % (tot['B'], tot['A'], tot['N1'], tot['N2']))
    nmax = max(tot['N1'], tot['N2'])
    floor = 0.005 * T['B']
    fires = tot['B'] >= 2 * nmax and tot['B'] - nmax >= floor
    print('   B ÷ max(N1,N2) = %.3f   差=%d（床 全行の 0.5%% = %.0f）  → %s' % (tot['B'] / nmax if nmax else float('inf'), tot['B'] - nmax, floor, '鳴る' if fires else '鳴らない'))
    fired = [c for c in top if H['B'][c].get(top[c], 0) >= 2 * max(H['N1'][c].get(top[c], 0), H['N2'][c].get(top[c], 0))
             and H['B'][c].get(top[c], 0) - max(H['N1'][c].get(top[c], 0), H['N2'][c].get(top[c], 0)) >= floor]
    print('   列ごとに同じ作法で鳴る列（B ≥ ヌル最大の 2 倍かつ差 ≥ 全行の 0.5%%）= %d 列 %s' % (len(fired), fired))
    rows = sorted(top, key=lambda c: -(H['B'][c].get(top[c], 0) - max(H['N1'][c].get(top[c], 0), H['N2'][c].get(top[c], 0))))
    print('   列ごと（最大の段・B / A / N1 / N2）上位 8（B−ヌル最大 の大きい順）')
    for c in rows[:8]:
        t = top[c]
        print('     %-26s 最大の段=%g  B=%d A=%d N1=%d N2=%d' % (c, t, H['B'][c].get(t, 0), H['A'][c].get(t, 0), H['N1'][c].get(t, 0), H['N2'][c].get(t, 0)))


main()
