# (1382) 問い 1 の続き：実際の量の記憶＝講和の前後の兵の充足（militaryReadinessShare）と兵の数（soldierK）。stock に毎四半期の行がある国だけ
import sys; sys.path.insert(0, 'D:/PoB_work/r1382_work/scripts')
from load import *; from align import match
dr, ds, rec, n = [], [], [], 0
for s in SEEDS:
    st = {}
    for v in rows('stock', s):
        t = v['time']
        if not t.startswith('q'): continue
        q = int(t[1:]); st.setdefault(v['nation'], {})[q] = (fl(v['militaryReadinessShare']), fl(v['soldierK']))
    full = {k for k, d in st.items() if len(d) > 600}
    o, _ = match(s)
    for b, a in o:
        if b['civilFlag'] != '0': continue
        e = int(b['month']) // 3; sq = (int(b['month']) - int(a['months'])) // 3
        for side in (b['sideA'], b['sideB']):
            if side not in full: continue
            d = st[side]; pre = d.get(sq - 1); post = d.get(e + 1)
            if not pre or not post or not (pre[0] > 0) or not (pre[1] > 0): continue
            n += 1; dr.append(post[0] / pre[0]); ds.append(post[1] / pre[1])
            k = next((j for j in range(1, 41) if d.get(e + j) and d[e + j][0] >= pre[0]), 41); rec.append(k)
print('側の数', n); print('講和の次の四半期の充足 ÷ 開戦の前の四半期', pct(dr, (.1, .25, .5, .75, .9)))
print('兵の数の比', pct(ds, (.1, .25, .5, .75, .9))); print('充足が開戦の前に戻るまでの四半期（41＝10年で戻らない）', pct(rec, (.1, .25, .5, .75, .9)))
