#!/usr/bin/env python
"""(679) 線 navf4：README ★(397) の〈壊す〉の物差し ③ 片方向の錠 の読み手（読むだけ）。追記 2（腕の前）で直した版。
  ③ 最大の段に張り付く国×四半期：monlaw_s（全ての国×四半期・段の法の全列）で、列ごとに〈全ての腕の和集合での最大の段〉を取り、
     その段に居る国×四半期の行を数えて法の列にわたって足す。**腕ごとにその腕の全行で割った割合**で比べる（種の集合や行数が違う腕どうしでも比べられる）。
     鳴る＝B の割合が ヌル腕（N1・N2）の割合の最大の 2 倍以上、かつ差が 0.005 以上（ar/stop.py ⑥③・m533/lock533.py と同じ差の床の作法）。
  使い方：python -X utf8 -I lock3.py <B の ins> <A の ins> <N1 の ins> <N2 の ins> [--b-seeds 1-10] [--a-seeds 1-10] [--n-seeds 1-10]
     種 1〜10 の判定＝既定。B11（借りた幅）＝--b-seeds 11-20 --a-seeds 11-20（N1・N2 は種 1〜10 のまま借りる）。
  出力は標準出力。判定行は〈鳴る〉〈鳴らない〉。記録として列ごとの内訳（B が多い順の上位 8）を出す。
"""
import csv, os, sys
from collections import defaultdict

SKIP_PREFIX = ('lawGateOpen:',)
SKIP_COLS = {'quarter', 'year', 'nationUid', 'nationIndex', 'nation', 'polityCode', 'blockadeRiskShare', 'EducationLawTarget'}
FLOOR = 0.005   # 差の床（割合）


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
            r = csv.reader(f, delimiter='	'); h = next(r)
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


def opt(a, name, default):
    if name not in a:
        return default
    v = a[a.index(name) + 1]; lo, hi = v.split('-')
    return list(range(int(lo), int(hi) + 1))


def main():
    a = sys.argv[1:]
    pos = []; skip = False
    for x in a:
        if skip: skip = False; continue
        if x in ('--b-seeds', '--a-seeds', '--n-seeds'): skip = True; continue
        pos.append(x)
    if len(pos) < 4:
        print(__doc__); sys.exit(2)
    sds = {'B': opt(a, '--b-seeds', list(range(1, 11))), 'A': opt(a, '--a-seeds', list(range(1, 11))),
           'N1': opt(a, '--n-seeds', list(range(1, 11))), 'N2': opt(a, '--n-seeds', list(range(1, 11)))}
    arms = [('B', pos[0]), ('A', pos[1]), ('N1', pos[2]), ('N2', pos[3])]
    H = {}; T = {}
    for k, d in arms:
        H[k], T[k] = scan(d, sds[k])
        if T[k] == 0:
            print('腕 %s の行が 0（種 %s の monlaw_s が無い）＝読めない' % (k, sds[k])); sys.exit(3)
    cols = sorted(set().union(*[set(H[k]) for k in H]))
    top = {c: max(v for k in H for v in H[k].get(c, {})) for c in cols if any(H[k].get(c) for k in H)}
    tot = {k: sum(H[k][c].get(top[c], 0) for c in top) for k in H}
    frac = {k: tot[k] / T[k] for k in H}
    print('③ 片方向の錠（段の法の列ごとの最大の段に居る国×四半期の行数の和 ÷ その腕の全行・monlaw_s）')
    print('   種：' + '  '.join('%s=%d〜%d' % (k, sds[k][0], sds[k][-1]) for k in H))
    print('   列の数=%d  全行 B=%d A=%d N1=%d N2=%d' % (len(top), T['B'], T['A'], T['N1'], T['N2']))
    print('   合計(行数) B=%d  A=%d  N1=%d  N2=%d' % (tot['B'], tot['A'], tot['N1'], tot['N2']))
    print('   割合 B=%.5f  A=%.5f  N1=%.5f  N2=%.5f' % (frac['B'], frac['A'], frac['N1'], frac['N2']))
    nmax = max(frac['N1'], frac['N2'])
    fires = frac['B'] >= 2 * nmax and frac['B'] - nmax >= FLOOR
    print('   B ÷ max(N1,N2) = %.3f   差=%.5f（床 %.3f）  → %s' % (frac['B'] / nmax if nmax else float('inf'), frac['B'] - nmax, FLOOR, '鳴る' if fires else '鳴らない'))

    def fr(k, c):
        return H[k][c].get(top[c], 0) / T[k]
    fired = [c for c in top if fr('B', c) >= 2 * max(fr('N1', c), fr('N2', c)) and fr('B', c) - max(fr('N1', c), fr('N2', c)) >= FLOOR]
    print('   列ごとに同じ作法で鳴る列（B ≥ ヌル最大の 2 倍かつ差 ≥ %.3f）= %d 列 %s' % (FLOOR, len(fired), fired))
    print('   判定（合計が鳴る or 鳴る列がある）= %s' % ('鳴る' if fires or fired else '鳴らない'))
    rows = sorted(top, key=lambda c: -(fr('B', c) - max(fr('N1', c), fr('N2', c))))
    print('   列ごと（最大の段・割合 B / A / N1 / N2）上位 8（B−ヌル最大 の大きい順）')
    for c in rows[:8]:
        t = top[c]
        print('     %-26s 最大の段=%g  B=%.5f A=%.5f N1=%.5f N2=%.5f' % (c, t, fr('B', c), fr('A', c), fr('N1', c), fr('N2', c)))


main()
