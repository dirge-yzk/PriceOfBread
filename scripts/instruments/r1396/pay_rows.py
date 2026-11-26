# (1396) c148dm：支払い率の項が立つ行の一覧（nation の四半期の平均 < 0.6 と、fiscal の月の値 < 0.6）。NaN の行は数だけ（計器の分母 0〈推〉）
import sys, os, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from load import rows, fl, SEEDS, BASE
base = sys.argv[1] if len(sys.argv) > 1 else BASE
nan_by = collections.Counter()
for s in SEEDS:
    for v in rows('nation', s, base):
        r = fl(v['payRateMeanShare'])
        if r != r:
            nan_by[(v['nation'], fl(v['totalPopulationK']) > 0)] += 1
        elif r < 0.6:
            print('q', s, v['quarter'], v['year'], v['nation'], f'{r:.3f}', 'war', v['atWarMonths'], 'civ', v['civilWarMonths'])
    for v in rows('fiscal', s, base):
        r = fl(v['payRateShare'])
        if not (r >= 0.6):
            print('m', s, v['monthIndex'], v['year'], v['nation'], f'{r:.3f}')
print('NaN の行（国・人口>0）', dict(nan_by))
