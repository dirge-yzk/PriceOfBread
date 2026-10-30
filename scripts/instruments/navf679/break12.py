#!/usr/bin/env python
"""(679) 線 navf4：〈壊す〉の物差し ①② の読み手（読むだけ・ar/stop.py の ⑥①⑥② の関数をそのまま呼ぶ＝遅い R 系の記録は呼ばない）。
使い方：python -X utf8 -I break12.py <A＝旗 0 の基線の ins> <N1 の ins> <N2 の ins> <B の ins> [--elig]
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
for fn in (stop.line1, stop.line2):
    fn(arms); print()
