"""(272) 反証役 vb#2：fullPush は向きを見ない＝解禁の間の下りの一歩（目標＜現在）にも政治予算の係数を掛けない。
計器つき10種の nation_s（全ての国）から、解禁の行（warStageOpenAfterGovFlag=1）の軍事費の一歩を結果×向きで数える（読むだけ）。
使い方：python vb2_down.py <計器の出力ディレクトリ>"""
import csv, glob, os, sys
from collections import Counter
d = sys.argv[1]
tot, uk = Counter(), Counter()
for p in sorted(glob.glob(os.path.join(d, 'nation_s*.tsv'))):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {n: i for i, n in enumerate(h)}
        for row in r:
            if row[ix['warStageOpenAfterGovFlag']] != '1': continue
            oc = int(row[ix['militaryMoveOutcomeCode']])
            inc = row[ix['militaryMoveIncreasingFlag']]
            k = (oc, 'up' if inc == '1' else 'down')
            tot[k] += 1
            if row[ix['nation']] == 'イギリス': uk[k] += 1
for name, c in (('全ての国', tot), ('イギリス', uk)):
    moved = sum(v for (oc, _), v in c.items() if oc in (3, 4))
    down = sum(v for (oc, dr), v in c.items() if oc in (3, 4) and dr == 'down')
    print(f'{name}: 解禁の行 {sum(c.values())}・動いた（結果3・4）{moved}・うち下り {down}')
    for k in sorted(c): print(f'  結果{k[0]} {k[1]}: {c[k]}')
