# (1382) 問い 1 の続き：平時の兵の充足 MR の分布（stock・atWarFlag 0）・MR<0.5 の割合（案 C の門の当たり方）
import sys; sys.path.insert(0, 'D:/PoB_work/r1382_work/scripts')
from load import *
mr = []; lo = n = 0; lo_ex = 0
for s in SEEDS:
    for v in rows('stock', s):
        if not v['time'].startswith('q') or fl(v['atWarFlag']) != 0: continue
        x = fl(v['militaryReadinessShare'])
        if x == x: mr.append(x); n += 1; lo += x < 0.5
print('平時の MR', pct(mr, (.1, .25, .5, .75, .9)), 'MR<0.5', f'{lo/n:.1%}')
