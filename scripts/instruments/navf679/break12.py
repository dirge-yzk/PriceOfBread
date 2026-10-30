#!/usr/bin/env python
"""(679) 線 navf4：〈壊す〉の物差し ①② の読み手（読むだけ・ar/stop.py の ⑥①⑥② の関数をそのまま呼ぶ＝遅い R 系の記録は呼ばない）。
使い方：python -X utf8 -I break12.py <A＝旗 0 の基線の ins> <N1 の ins> <N2 の ins> <B の ins> [--elig] [--borrowed]
  --elig：①で鳴り得る国（A・N1・N2 の全てが 2 以下）の一覧と、鳴り得ない国（どれかが 3 以上）の数を出す。"""
import importlib.util, os, sys
here = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location('stop', os.path.join(here, '..', 'ar', 'stop.py'))
stop = importlib.util.module_from_spec(spec); spec.loader.exec_module(stop)
arms = [a for a in sys.argv[1:] if not a.startswith('--')][:4]
assert len(arms) == 4, __doc__
if '--elig' in sys.argv:
    names = ['A', 'N1', 'N2']
    cnt = {a: stop.defaultdict(int) for a in names}; label = {}
    for a, d in zip(names, arms[:3]):
        for s in stop.seeds_of(d):
            nat = stop.load_idx(d, s); last_q = max(q for nm, v in nat.values() for q, *_ in v)
            for idx, (nm, rows) in nat.items():
                if max((r[2] for r in rows if r[2] == r[2]), default=0) < 500: continue
                label[idx] = nm
                if stop.collapsed([(r[0], r[2]) for r in rows], last_q) or stop.collapsed([(r[0], r[8]) for r in rows], last_q): cnt[a][idx] += 1
    ok = [n for n in label if max(cnt[a][n] for a in names) <= 2]; no = [n for n in label if n not in ok]
    print('①で鳴り得る国（人口のピーク 50 万人以上・A・N1・N2 の崩れの種数が全て 2 以下）= %d 国 / 母集団 %d 国' % (len(ok), len(label)))
    print('   鳴り得ない国（どれかが 3 以上）= %d 国：%s' % (len(no), ','.join('%s(%d/%d/%d)' % (label[n], cnt['A'][n], cnt['N1'][n], cnt['N2'][n]) for n in sorted(no, key=lambda n: -max(cnt[a][n] for a in names))[:12]) + ' ほか'))
    print('   鳴り得る国：' + ','.join(label[n] for n in sorted(ok)))
    sys.exit(0)
BORROWED = '--borrowed' in sys.argv   # B11：A は A11（種 11〜20）・N1・N2 は種 1〜10 の借りた幅（追記 2）
if BORROWED:
    print('※ 借りた幅：A＝A11（種 11〜20）・N1・N2＝種 1〜10。種の集合が違う腕どうしの比べ（追記 2）。')
stop.line1(arms); print()
stop.line2(arms)
# 追記 2：② の〈判定不能〉の段。字面どおりに鳴り（stop.line2 の式）、かつ超過（B の中央値 − A・N1・N2 の最大）が絶対値で 0.02 未満なら判定不能（採らず・止めず・作者に聞く）。
names = ['A', 'N1', 'N2', 'B']
for year in (1850, 1900, 1950):
    med = {}; same = {}
    for a, d in zip(names, arms):
        sh = []; who = stop.defaultdict(int)
        for sd in stop.seeds_of(d):
            n, v = stop.top_share(d, sd, year)
            if n is not None: sh.append(v); who[n] += 1
        med[a] = stop.statistics.median(sh) if sh else float('nan'); same[a] = max(who.values()) if who else 0
    nulls = [med['A'], med['N1'], med['N2']]; width = max(nulls) - min(nulls)
    over = med['B'] - max(nulls)
    lit = med['B'] > max(nulls) + 2 * width and same['B'] >= 8
    verdict = ('鳴る' if over >= 0.02 else '判定不能（字面どおり鳴るが超過 %.4f < 0.02・作者に聞く）' % over) if lit else '鳴らない'
    print('   ②の段 %d  超過=%.4f  幅=%.4f  → %s' % (year, over, width, verdict))
