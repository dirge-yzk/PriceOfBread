# (1189) S12-30：英の職人と工場労働者の 1 人あたり月の所得（class_s の earnedPerMonth÷sizeK）と職人の人数を、1790・1795・1800 年の最後の時点で種ごとに並べる（中央）。
import csv, os, sys, statistics
sys.stdout.reconfigure(newline='\n')
B = sys.argv[1]; N = sys.argv[2] if len(sys.argv) > 2 else 'イギリス'
res = {}
for s in range(1, 11):
    p = os.path.join(B, f'class_s{s}.tsv')
    last = {}
    for x in csv.DictReader(open(p, encoding='utf-8'), delimiter='\t'):
        if x['nation'] != N or x['time'] == 'open': continue
        y = int(x['year'])
        if y not in (1790, 1795, 1800): continue
        q = int(x['time'].lstrip('q'))
        last.setdefault((y, x['className']), []).append((q, float(x['sizeK']), float(x['earnedPerMonth'])))
    for (y, c), v in last.items():
        q, sz, e = max(v)
        res.setdefault((y, c), []).append((sz, e / sz if sz > 1e-9 else float('nan')))
for y in (1790, 1795, 1800):
    a = res.get((y, '職人'), []); w = res.get((y, '工場労働者'), [])
    am = statistics.median([x[1] for x in a if x[1] == x[1]] or [float('nan')]); wm = statistics.median([x[1] for x in w if x[1] == x[1]] or [float('nan')])
    print(f'{N} {y}: 職人の人数K の中央 {statistics.median([x[0] for x in a]):.1f}・1 人あたり月の所得 職人 {am:.2f}・工場労働者 {wm:.2f}・比 {am / wm:.2f}（種 {len(a)}）')
