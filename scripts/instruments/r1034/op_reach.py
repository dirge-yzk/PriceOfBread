# (1034) c97op：案 A が届く財と届かない財を使った額で分ける（読むだけ）
# 財ごとに世界（全ての国）の越境の輸出÷使った額を種ごとに求め、閾値 0.5%・2% で分けた〈使った額の割合〉と〈国の余りの額の割合〉を出す。
# 使い方: python op_reach.py <pre> <出力>
import sys, os, statistics as st
from collections import defaultdict
PRE, OUT = sys.argv[1], sys.argv[2]
res = defaultdict(lambda: defaultdict(list))
for arm in ('base_ins', 'n1', 'n2'):
    for s in range(1, 11):
        acc = defaultdict(lambda: defaultdict(float))
        with open(os.path.join(PRE, 'ng_%s_s%d.tsv' % (arm, s)), encoding='utf-8') as f:
            for ln in f:
                v = ln.rstrip('\n').split('\t')
                if v[1] == 'ALL': continue
                P, D, EQ, px = float(v[4]), float(v[5]), float(v[11]), float(v[12])
                if not (px == px) or px <= 0: continue
                a = acc[(v[0], v[2])]; a['D'] += D * px; a['E'] += EQ * px; a['sur'] += max(0.0, P - D) * px
        for y in ('1870', '1913'):
            tD = sum(a['D'] for (yy, g), a in acc.items() if yy == y); tS = sum(a['sur'] for (yy, g), a in acc.items() if yy == y)
            for th in (0.005, 0.02):
                lowD = sum(a['D'] for (yy, g), a in acc.items() if yy == y and a['D'] > 0 and a['E'] / a['D'] < th)
                lowS = sum(a['sur'] for (yy, g), a in acc.items() if yy == y and a['D'] > 0 and a['E'] / a['D'] < th)
                res[(y, th)][arm].append((lowD / tD, lowS / tS))
with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
    f.write('year\t閾値(越境の輸出÷使った)\t腕\t閾値未満の財の使った額の割合(中央値 最小 最大)\t閾値未満の財の国の余りの割合(中央値)\n')
    for (y, th), d in sorted(res.items()):
        for arm in ('base_ins', 'n1', 'n2'):
            L = d[arm]; a = [x[0] for x in L]; b = [x[1] for x in L]
            f.write('%s\t%.3f\t%s\t%.3f %.3f %.3f\t%.3f\n' % (y, th, arm, st.median(a), min(a), max(a), st.median(b)))
print('done')
