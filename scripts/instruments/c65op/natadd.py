# (820) c65op 記録：国の依存の在庫（opium_s の N 行の nationalAddictionIndex＝人口加重）の種の中央を年ごとに並べる（読むだけ・腕と基線を同じ台本で）。
#   python -X utf8 -I scripts/instruments/c65op/natadd.py 名前=ins ...
import sys, glob, os, statistics as st
from collections import defaultdict
YEARS = (1800, 1840, 1880, 1913, 1940)
NATS = ('清', 'イギリス', 'インド', 'マラータ同盟', 'オマーン', 'ネーデルラント', 'オスマン', 'フランス', '日本')
for arg in sys.argv[1:]:
    name, d = arg.split('=', 1)
    per = defaultdict(list)   # (国, 年) -> 種ごとの値
    nban = defaultdict(list)
    for f in sorted(glob.glob(os.path.join(d, 'opium_s*.tsv'))):
        fh = open(f, encoding='utf-8'); h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        got = {}
        for line in fh:
            v = line.rstrip('\n').split('\t')
            if v[ix['scope']] != 'N': continue
            y = int(v[ix['year']])
            if y in YEARS: got[(v[ix['nation']], y)] = (float(v[ix['nationalAddictionIndex']]), float(v[ix['possessionLawCode']]))
        for k, (a, law) in got.items(): per[k].append(a); nban[k].append(law)
    print(f'== {name}（{d}）：国の依存の在庫の種の中央〔保有の法の段の中央〕')
    for n in NATS:
        cells = []
        for y in YEARS:
            xs = per.get((n, y), [])
            cells.append(f'{y} {st.median(xs):.4f}〔{st.median(nban[(n, y)]):.0f}〕({len(xs)})' if xs else f'{y} -')
        print(f'  {n}：' + '・'.join(cells))
