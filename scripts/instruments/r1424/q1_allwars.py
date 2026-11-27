# (1424) 問い1 の補：全ての戦争（wexit の組）を〈接触の無い月の割合〉と〈押しの釣り合い〉で分け、長さと終わり方を数える（読むだけ）。
# 押しの釣り合い＝|Σd|/Σ|d|（接触の月だけ・0＝行ったり来たり・1＝一方向）。運 0 の木では d＝0.10×pushShare。
import sys, os, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lk_gate import load, groups, endpath
d = sys.argv[1]; s0, s1 = map(int, sys.argv[2].split('-'))
tab = collections.defaultdict(collections.Counter); longrows = []
for s in range(s0, s1 + 1):
    ix, rows = load(f'{d}/wexit_s{s}.tsv')
    for g, rs in groups(ix, rows).items():
        n = len(rs); zero = sum(1 for x in rs if x[ix['pushSumPositiveFlag']] == '0')
        sc = [float(x[ix['scoreIndex']]) for x in rs]; ds = [sc[k] - sc[k - 1] for k in range(1, n)]
        sa = sum(abs(x) for x in ds); bal = abs(sum(ds)) / sa if sa > 0 else 1.0
        cls = 'noContact' if zero == n else ('partContact' if zero else ('balanced' if bal < 0.3 else 'oneway'))
        ep = endpath(ix, rs[-1]); ep = 'running' if int(rs[-1][ix['month']]) == 2003 and ep == 'other' else ep
        col = any(x[ix['oldCollapseAFlag']] == '1' or x[ix['oldCollapseBFlag']] == '1' for x in rs)
        b = 'ge60' if n >= 60 else ('ge36' if n >= 36 else ('ge24' if n >= 24 else 'lt24'))
        t = tab[cls]; t['n'] += 1; t[b] += 1; t['end_' + ep] += 1; t['monthsSum'] += n
        if col and ep in ('running', 'other') : t['colFlagNoEnd'] += 1
        if n >= 36: longrows.append(f's{s} {g} cls={cls} n={n} end={ep} bal={bal:.2f} zero={zero} maxAbsS={max(abs(x) for x in sc):.3f} colMonths={sum(1 for x in rs if x[ix["oldCollapseAFlag"]] == "1" or x[ix["oldCollapseBFlag"]] == "1")}')
for cls, t in tab.items():
    print(cls, 'n=%d meanMonths=%.1f' % (t['n'], t['monthsSum'] / t['n']), ' '.join(f'{k}={v}' for k, v in sorted(t.items()) if k not in ('n', 'monthsSum')))
print('# 36 か月以上の戦争'); [print(x) for x in longrows]
