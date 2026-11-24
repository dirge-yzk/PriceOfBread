# (1348) 合流の記録の読み手（読むだけ・門にしない・エンジンの関数を呼ばない）：
#   ① 開幕の国営工場の着工＝nation_s の stateFacilityStartedCount（quarter 0 の和・quarter 0〜3 の和）＝線 wd の反証役 v2 の中（aib_s の startedCount は AI が目標を置いた回）。
#   ② 守られた法 7 軸の後退を〈同じ国の前の行と政体コード polityCode が同じ四半期〉と〈変わった四半期〉に分けて数える（monlaw_s）＝線 lr の反証役 v の低①。
#   使い方：python rec_m1348.py <ins> <種の起点> <種の数>
import sys, os
ins, sb, n = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
DOWN = ['Suffrage', 'WomenRights', 'SocialSecurity', 'ColonialSuffrage', 'MigrantNativeSuffrage']
UP = ['ShorterHours', 'ChildLabor']
def rd(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\r\n').split('\t')
        for line in f:
            yield h, line.rstrip('\r\n').split('\t')
q0 = q03 = 0; same = {a: 0 for a in DOWN + UP}; chg = {a: 0 for a in DOWN + UP}
for s in range(sb, sb + n):
    a0 = a3 = 0
    p = os.path.join(ins, f'nation_s{s}.tsv')
    for h, r in rd(p):
        iq = h.index('quarter'); ic = h.index('stateFacilityStartedCount')
        q = int(r[iq])
        if q > 3: continue
        v = float(r[ic] or 0)
        a3 += v
        if q == 0: a0 += v
    q0 += a0; q03 += a3
    prev = {}
    for h, r in rd(os.path.join(ins, f'monlaw_s{s}.tsv')):
        u = r[h.index('nationUid')]; pc = r[h.index('polityCode')]
        cur = {a: float(r[h.index(a)]) for a in DOWN + UP}
        if u in prev:
            pp, ppc = prev[u]
            for a in DOWN:
                if cur[a] < pp[a]: (same if pc == ppc else chg)[a] += 1
            for a in UP:
                if cur[a] > pp[a]: (same if pc == ppc else chg)[a] += 1
        prev[u] = (cur, pc)
    print(f'種 {s}\t開幕の着工 q0 {a0:g}\tq0〜3 {a3:g}')
print(f'和\t開幕の着工 q0 {q0:g}\tq0〜3 {q03:g}')
print('軸\t後退（政体が同じ四半期）\t後退（政体が変わった四半期）')
for a in DOWN + UP: print(f'{a}\t{same[a]}\t{chg[a]}')
