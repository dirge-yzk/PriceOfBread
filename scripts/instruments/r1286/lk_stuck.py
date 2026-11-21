# (1286) 1950 年まで本位の外に残った国の続きの中で、印刷の無い月（φ＝1）と支払い率≧0.85 の月を数える（読むだけ）
# 使い方: python -X utf8 -I lk_stuck.py <生> <種の下> <種の上> <出力>
import sys, os, csv, math
from collections import defaultdict
RAW, LO, HI, OUT = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]


def read(p):
    with open(p, encoding='utf-8', newline='') as fh:
        r = csv.reader(fh, delimiter='\t'); h = next(r)
        return {k: i for i, k in enumerate(h)}, list(r)


def fl(x):
    try:
        return float(x)
    except ValueError:
        return math.nan


out = ['seed\tnation\tfrom\tmonths\tnoPrintMonths\tnoPrintFunded085\tlongestNoPrintRun\tmedInterestToRevenue']
for s in range(LO, HI + 1):
    ix, rows = read(os.path.join(RAW, f'fiscal_s{s}.tsv'))
    byn = defaultdict(list)
    for r in rows:
        byn[r[ix['nation']]].append(r)
    for nat, rs in byn.items():
        rs.sort(key=lambda r: int(r[ix['monthIndex']]))
        gold = [fl(r[ix['onGold']]) > 0.5 for r in rs]
        if gold[-1]:
            continue
        j = len(rs) - 1
        while j > 0 and not gold[j - 1]:
            j -= 1
        if len(rs) - j < 120:
            continue
        np_ = npf = run = best = 0; ir = []
        for r in rs[j:]:
            d = fl(r[ix['devaluedPerMonth']]); pr = fl(r[ix['payRateShare']])
            x = fl(r[ix['interestToRevenueShareNow']])
            if x == x:
                ir.append(x)
            if not (d > 0):
                np_ += 1; run += 1; best = max(best, run)
                if pr >= 0.85:
                    npf += 1
            else:
                run = 0
        ir.sort()
        out.append(f'{s}\t{nat}\t{rs[j][ix["year"]]}\t{len(rs) - j}\t{np_}\t{npf}\t{best}\t{ir[len(ir) // 2] if ir else math.nan:.3f}')
with open(OUT, 'w', encoding='utf-8', newline='\n') as o:
    o.write('\n'.join(out) + '\n')
print('ok')
