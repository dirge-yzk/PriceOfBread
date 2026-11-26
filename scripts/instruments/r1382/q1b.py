# (1382) 問い 1 の続き：厭戦の窓（講和〜厭戦が 0.5 に戻る月）の間に、その組の緊張が 140 以上だった月（＝厭戦だけが門を閉じていた見込みの月）
import sys, math; sys.path.insert(0, 'D:/PoB_work/r1382_work/scripts')
from load import *; from align import match
from q1_rematch import unlock
tot = blocked = blkmon = 0; sub = st = 0; short0 = short1 = n0 = n1 = 0; s24_0 = s24_1 = 0
gemon_all = 0
for s in SEEDS:
    wp = {}
    for v in rows('warpair', s):
        wp[(int(v['quarter']), frozenset((v['nationA'], v['nationB'])))] = (int(v['monthsGe140']), int(v['gateOpenMonths']), int(v['starts']), int(v['submits']))
    for v in rows('warband', s): sub += int(v['submits']); st += int(v['start_main'])
    o, _ = match(s)
    starts = {}
    for b, a in o: starts.setdefault(frozenset((a['sideA'], a['sideB'])), []).append(int(b['month']) - int(a['months']))
    for b, a in o:
        if b['civilFlag'] != '0': continue
        k = frozenset((b['sideA'], b['sideB'])); m = int(b['month']); u = max(unlock(fl(b['exA'])), unlock(fl(b['exB'])))
        nxt = [x for x in starts[k] if x > m]; iv = min(nxt) - m if nxt else None
        if u == 0:
            n0 += 1; short0 += iv is not None and iv <= 12; s24_0 += iv is not None and iv <= 24
        else:
            n1 += 1; short1 += iv is not None and iv <= 12; s24_1 += iv is not None and iv <= 24
        if u == 0: continue
        tot += 1; ge = 0
        for q in range(m // 3 + 1, (m + u) // 3 + 1):
            r = wp.get((q, k));
            if r: ge += r[0]
        if ge > 0: blocked += 1; blkmon += ge
print('厭戦>0.5 の講和', tot, '窓の間に緊張>=140 の月があった講和', blocked, '月の合計', blkmon)
print('12か月以内の再戦の率  門が開いていた講和', f'{short0/n0:.2%}', '厭戦>0.5 の講和', f'{short1/n1:.2%}')
print('24か月以内の再戦の率  門が開いていた講和', f'{s24_0/n0:.2%}', '厭戦>0.5 の講和', f'{s24_1/n1:.2%}')
print('屈服（warband submits）', sub, '緊張の開戦 start_main', st)
