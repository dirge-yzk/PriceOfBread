# (1402) uk_traj.py の出力（uk_<腕>.tsv）から、種ごとに型（R＝不履行なしで 0・D＝0 の年か前 5 年に不履行・-＝0 にならない）・0 の年・再び借りた年・1913 と 1950 の国債÷GDP(%) を出し、腕ごとに数える。
#   python -X utf8 -I uk_R.py <out> <腕...>
import sys, os, csv
O = sys.argv[1]
for arm in sys.argv[2:]:
    T = {}
    for x in csv.DictReader(open(os.path.join(O, f'uk_{arm}.tsv'), encoding='utf-8'), delimiter='\t'):
        T.setdefault(int(x['seed']), {})[int(x['year'])] = x
    cnt = {'R': [], 'D': [], '-': []}
    for s in sorted(T):
        Y = T[s]; ys = sorted(Y); fl = lambda y, k: float(Y[y][k])
        z = next((y for y in ys if fl(y, 'debtEnd') <= 1e-9 and y > 1785), None)
        dz = z is not None and any(fl(y, 'defMax') > (fl(y - 1, 'defMax') if y - 1 in Y else 0) for y in ys if z - 5 <= y <= z)
        typ = '-' if z is None else ('D' if dz else 'R')
        back = next((y for y in ys if z and y > z and fl(y, 'debtEnd') > 1e-9), None) if z else None
        cnt[typ].append((s, z))
        print(f'{arm}\t{s}\t{typ}\t{z}\t{back}\t{fl(1913, "debtPct"):.0f}\t{fl(max(ys), "debtPct"):.0f}')
    by = lambda L, Y: sum(1 for s, z in L if z <= Y)
    print(f'## {arm} R {len(cnt["R"])}（1879 まで {by(cnt["R"], 1879)}・1913 まで {by(cnt["R"], 1913)}）・D {len(cnt["D"])}（1913 まで {by(cnt["D"], 1913)}）・0 にならない {len(cnt["-"])}')
