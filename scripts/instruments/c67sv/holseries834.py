# (834) 空洞化 1/10 未満の件の振り分け（読むだけ）：1 州の人口の系列を基線 keep_832/base_ins と腕 B の生で種ごとに並べる（ピーク・ピーク後の最小・比）。
#   使い方：python -I -X utf8 holseries834.py <州の名>   （パスは (834) の腕 B の生と基線の生に固定）
import sys, csv
def series(p, name):
    out = []
    with open(p, encoding='utf-8') as f:
        r = csv.DictReader(f, delimiter='\t')
        for row in r:
            if row['region'] == name: out.append((int(row['quarter']), int(row['year']), row['nation'], row['tier'], float(row['populationK'])))
    return out
name = sys.argv[1]
for lab, base in (('基線 keep_832', 'D:/PoB_work/keep_832/base_ins'), ('腕 B', 'D:/PoB_work/c67sv_armB/out_c67svB_10/ins')):
    for s in range(1, 11):
        sr = series(f'{base}/region_s{s}.tsv', name)
        if not sr: continue
        pk = max(sr, key=lambda x: x[4]); after = [x for x in sr if x[0] >= pk[0]]; mn = min(after, key=lambda x: x[4])
        print(f'{lab} s{s}: ピーク {pk[4]:.1f}({pk[1]}・{pk[2]}) → ピーク後の最小 {mn[4]:.1f}({mn[1]}・{mn[2]}) 比 {mn[4]/pk[4]:.3f} 最後 {sr[-1][4]:.1f}({sr[-1][1]}・{sr[-1][2]})')
