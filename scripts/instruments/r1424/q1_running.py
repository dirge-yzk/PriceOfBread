# (1424) 問い1：1950 年に続いている戦争（wars_s verdictCode＝−1）を 1 本ずつ wexit_s の行と突き合わせて読む（読むだけ・コミットしない）。
# pushShare は d＝S−前の S（運 0 の木では d＝0.10×pushShare）から逆算する（旗 WarLuck 0 の木だけで正しい）。
import sys, os, collections, statistics
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lk_gate import load, groups
d = sys.argv[1]; s0, s1 = map(int, sys.argv[2].split('-')); haswx = os.path.exists(f'{d}/wexit_s{s0}.tsv')
agg = collections.Counter(); months_run = []; months_all = []
for s in range(s0, s1 + 1):
    ix, wr = load(f'{d}/wars_s{s}.tsv')
    for r in wr: months_all.append(int(r[ix['months']]))
    run = [r for r in wr if r[ix['verdictCode']] == '-1']
    gl = {}
    if haswx:
        wi, wrows = load(f'{d}/wexit_s{s}.tsv')
        for g, rs in groups(wi, wrows).items():
            if int(rs[-1][wi['month']]) == 2003: gl[(g[0], g[1])] = rs
    for r in run:
        a, b, civ, mo, sy = r[ix['sideA']], r[ix['sideB']], r[ix['civilWarFlag']], int(r[ix['months']]), r[ix['startYear']]
        months_run.append(mo); agg['civil' if civ == '1' else 'foreign'] += 1; agg['entry_' + r[ix['entryCode']]] += 1
        line = f's{s} {a} vs {b} civil={civ} start={sy} months={mo} entry={r[ix["entryCode"]]}'
        rs = gl.get((a, b)) or gl.get((b, a))
        if rs:
            sc = [float(x[wi['scoreIndex']]) for x in rs]; ds = [sc[k] - sc[k - 1] for k in range(1, len(sc))]
            ps = [dd / 0.10 for dd in ds]; zero = sum(1 for x in rs if x[wi['pushSumPositiveFlag']] == '0')
            f = lambda c: sum(1 for x in rs if x[wi[c]] == '1')
            last = rs[-1]
            line += (f' wxRows={len(rs)} S0={sc[0]:+.3f} Send={sc[-1]:+.3f} maxAbsS={max(abs(x) for x in sc):.3f} meanPS={statistics.mean(ps) if ps else 0:+.3f} meanAbsPS={statistics.mean(abs(x) for x in ps) if ps else 0:.3f}'
                     f' noContactMonths={zero} colA={f("oldCollapseAFlag")} colB={f("oldCollapseBFlag")} wearyM={f("oldWearyFlag")} crisisAend={last[wi["oldCrisisAFlag"]]} crisisBend={last[wi["oldCrisisBFlag"]]} kA={float(last[wi["kA"]]):.3f} kB={float(last[wi["kB"]]):.3f}')
            agg['wx_matched'] += 1; agg['noContactAll' if zero == len(rs) else ('noContactSome' if zero else 'contactAll')] += 1
            if max(abs(x) for x in sc) < 1.0: agg['neverDecisive'] += 1
            if f('oldCollapseAFlag') or f('oldCollapseBFlag'): agg['hadCollapseFlag'] += 1
        print(line)
print('SUM', dict(agg))
mr = sorted(months_run); print('RUNNING n=%d months median=%s max=%s  >=24:%d >=60:%d' % (len(mr), mr[len(mr)//2] if mr else '-', mr[-1] if mr else '-', sum(1 for x in mr if x >= 24), sum(1 for x in mr if x >= 60)))
print('ALL wars=%d meanMonths=%.2f' % (len(months_all), statistics.mean(months_all)))
