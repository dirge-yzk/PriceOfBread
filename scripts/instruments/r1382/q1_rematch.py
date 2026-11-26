# (1382) 問い 1：講和の後の同じ組の再戦の間隔と件数・開戦の門の厭戦（>0.5）が解ける月との突き合わせ（読むだけ）
import sys, math; sys.path.insert(0, 'D:/PoB_work/r1382_work/scripts')
from load import *; from align import match
KEEP, THR, TRIG = 0.97, 0.5, 6
def unlock(ex):  # 平時の減衰だけで厭戦が THR 以下に戻る月数（他の戦争に入らない仮定）
    return 0 if not (ex > THR) else math.ceil(math.log(THR / ex) / math.log(KEEP))
peaces = 0; rem = []; exgt = 0; allstarts = 0
for s in SEEDS:
    o, _ = match(s); wars = [a for _, a in o]
    starts = {}
    for b, a in o:
        st = int(b['month']) - int(a['months']); starts.setdefault(frozenset((a['sideA'], a['sideB'])), []).append(st)
    for w in rows('wars', s): pass
    for b, a in o:
        if b['civilFlag'] != '0': continue
        peaces += 1; key = frozenset((b['sideA'], b['sideB'])); m = int(b['month'])
        exA, exB = fl(b['exA']), fl(b['exB']); u = max(unlock(exA), unlock(exB))
        if u > 0: exgt += 1
        nxt = [x for x in starts[key] if x > m]
        coll = b['collapsedIterFlag'] == '1'
        if nxt: rem.append((min(nxt) - m, u, coll, exA, exB, s))
        else: rem.append((None, u, coll, exA, exB, s))
print('国と国の講和', peaces, '講和の時に片方でも厭戦>0.5', exgt, f'{exgt/peaces:.1%}')
got = [r for r in rem if r[0] is not None]
print('同じ組の再戦あり', len(got), f'{len(got)/peaces:.1%}')
for lim in (12, 24, 60, 120):
    print(f'  {lim} か月以内', sum(1 for r in got if r[0] <= lim))
print('再戦の間隔（月）', pct([r[0] for r in got], (.1, .25, .5, .75, .9)))
g0 = [r[0] for r in got if r[1] == 0]; g1 = [r for r in got if r[1] > 0]
print('講和の時 厭戦<=0.5（門が開いている）', pct(g0, (.1, .25, .5, .75)))
print('講和の時 厭戦>0.5 の組', pct([r[0] for r in g1], (.1, .25, .5, .75)), '解けるまでの月', pct([r[1] for r in g1]))
near = [r for r in g1 if r[1] <= r[0] <= r[1] + TRIG + 6]
early = [r for r in g1 if r[0] < r[1]]
print('厭戦>0.5 の組で 再戦が解けた月〜+12 か月の間', len(near), ' 解ける前（他の道＝連鎖の参戦ほか）', len(early))
# 門を消した時の見込み：厭戦>0.5 の組の再戦が、門の開いていた組と同じ緊張の戻りの速さで起きるとしたら
print('全講和の 厭戦>0.5 の側の数', sum((r[3] > THR) + (r[4] > THR) for r in rem), ' 両側', sum((r[3] > THR) and (r[4] > THR) for r in rem))
print('崩壊の講和のうち 厭戦>0.5', sum(1 for r in rem if r[2] and r[1] > 0), '/', sum(1 for r in rem if r[2]))
