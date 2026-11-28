# (1429) 記録：接触なしの戦争（最後の行の pushSumPositiveFlag＝0）の〈表の最後まで続いた数〉と〈60 か月以上の数〉。旧の列だけを読む（基線とヌル腕も同じ台本）。
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from paper_nx import rows_of, runs
d = sys.argv[1]; cont = ge60 = 0
for s in range(1, 11):
    ix, rows = rows_of(os.path.join(d, f'wexit_s{s}.tsv')); lastm = max(int(r[ix['month']]) for r in rows)
    for run in runs(ix, rows):
        if run[-1][ix['pushSumPositiveFlag']] == '0':
            if int(run[-1][ix['month']]) == lastm: cont += 1
            if len(run) >= 60: ge60 += 1
print(f'{d}\tcontNoContactEnd={cont}\tnoContactGe60={ge60}')
