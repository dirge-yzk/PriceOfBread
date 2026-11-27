# 英の国債の型（R＝返し切って 0・D＝不履行の年か前 5 年に 0・- ＝0 にならない）を keep_1404 の fiscal_s で数え直す（生を読むだけ）。
# 使い方: python uk_debt.py <出力 tsv>
import sys
K = 'D:/PoB_work/keep_1404'
ARMS = [('M', 'base_ins', range(1, 11)), ('N1', 'n1', range(1, 11)), ('N2', 'n2', range(1, 11)), ('S11', 'a11', range(11, 21))]
ZERO = 1.0   # 国債がこの額未満を 0 と読む（貨幣の単位・初期の英の国債は約 93 万）
def rows(p):
    f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for l in f:
        r = l.rstrip('\n').split('\t')
        if r[ix['nation']] == 'イギリス': yield ix, r
out = []
for arm, d, seeds in ARMS:
    for s in seeds:
        first0 = reb = None; defy = []; last_def = 0; n0m = 0; mind = None; prev0 = False
        for ix, r in rows(f'{K}/{d}/fiscal_s{s}.tsv'):
            y = int(r[ix['year']]); g = float(r[ix['govDebtStock']]); dc = float(r[ix['defaultCount']])
            if dc > last_def: defy.append(y); last_def = dc
            z = g < ZERO
            if z: n0m += 1
            if z and first0 is None: first0 = y
            if first0 is not None and prev0 and not z and reb is None: reb = y
            prev0 = z; mind = g if mind is None else min(mind, g)
        if first0 is None: t = '-'
        elif any(first0 - 5 <= y <= first0 for y in defy): t = 'D'
        else: t = 'R'
        out.append((arm, s, t, first0, reb, len(defy), n0m))
with open(sys.argv[1], 'w', encoding='utf-8', newline='\n') as f:
    f.write('arm\tseed\ttype\tfirstZeroYear\treborrowYear\tdefaults\tzeroRows\n')
    for o in out: f.write('\t'.join(str(x) for x in o) + '\n')
for arm, _, _ in ARMS:
    A = [o for o in out if o[0] == arm]
    print(arm, 'R', sum(o[2] == 'R' for o in A), 'R<=1879', sum(o[2] == 'R' and o[3] <= 1879 for o in A),
          'R<=1913', sum(o[2] == 'R' and o[3] <= 1913 for o in A), 'D', sum(o[2] == 'D' for o in A), '-', sum(o[2] == '-' for o in A),
          'R_reborrow', sum(o[2] == 'R' and o[4] is not None for o in A))
print('total R', sum(o[2] == 'R' for o in out), '/', len(out), ' D', sum(o[2] == 'D' for o in out), ' -', sum(o[2] == '-' for o in out))
