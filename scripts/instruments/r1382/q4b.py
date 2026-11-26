# (1382) 問い 4 の続き：案 Θ1（符号つき Ḡ）の目標 Clamp01(0.5 − 0.8×livingDrop) と e の係数 (目標−0.5)×2 の分布（定常の目標で読む＝Toward の遅れは見ない〈推〉）
import sys; sys.path.insert(0, 'D:/PoB_work/r1382_work/scripts')
from load import *
ld, ec = [], []
for s in SEEDS:
    for v in rows('dcont', s):
        x = fl(v['livingDropMeanIndex'])
        if x == x: ld.append(x); t = max(0, min(1, 0.5 - 0.8 * x)); ec.append((t - 0.5) * 2)
P = (.01, .1, .25, .5, .75, .9, .99)
print('livingDrop（符号つき・R−s の平均）', pct(ld, P)); print('Θ1 の e の係数', pct(ec, P), ' 負の割合', f"{sum(e < 0 for e in ec)/len(ec):.1%}")
