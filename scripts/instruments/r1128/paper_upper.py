# (1128) 紙の台本：T1 の上側の門の候補（R4・R5・U1）が、正しい形（A-1）と突然変異（M2・M3・M4・M10）でどう動くかを 1 行の算術で並べる。
#   エンジンの関数は呼ばない。式は事前登録の下書き（T0とT1_下書き §3・material §4）の文から書いた。
#   1 つの行＝1 つの州×財×清算（売り手 1・量の上限なし・access=1・予算は縛らない・最初の反復 I=0）。
#   P＝今月の国内の産出 O。P̂(p)＝O＋0.15×(target(p)−O)。Q＝min(P, P̂(u′))。TS＝D（TargetStockBuffer 1.0・StockpileFactor 1）。
#   旧（旗 0）  : X＝max(0, min(D·r − P,  TS + D − P − S))
#   A-1         : X＝max(0, min(D·r − Q,  TS + D·√r − Q − S))
#   M2（Q=P）   : X＝max(0, min(D·r − P,  TS + D·√r − P − S))
#   M3（在庫の項を外す・B-a）: X＝max(0, D·r − Q)
#   M4（A-2 全量を最安へ・u′<mc の行）: X＝max(0, D + TS − S)
#   M10（Q=P−(P̂(lp)−P̂(u′))）: X＝max(0, min(D·r − Q10, TS + D·√r − Q10 − S))
#   U1＝(S + X − (TS + D·√r − Q_計器))÷D（計器の Q は事前登録の min の形で独立に計算）＞1e-9 なら赤。
#   R4＝X÷D（再輸出 0）＞2 なら赤。R5＝月末の (S_end − TS)÷D・S_end＝S + X + P_next − D（P_next＝Q＝見込みどおり）。
#   R5 の閾値＝ヌル腕の最大（keep_1122・輸入した行・D の重みつき 99 分位）：1870 0.6771・1913 0.8508・全ての年 0.9999（out_r45.txt）。
import math, itertools

R5_MAX = {'1870': 0.6771, '1913': 0.8508, 'all': 0.9999}
K = 0.15


def row(D, r, pd, s0, grow, cut):
    """pd＝P/D・s0＝S/TS・grow＝target(lp)/O・cut＝target(u′)/target(lp)（0＝u′<mc で採算 0）"""
    TS = D; O = pd * D; P = O; S = s0 * TS
    tl = grow * O; tu = cut * tl
    ph = lambda t: O + K * (t - O)
    Qi = min(P, ph(tu))
    sq = math.sqrt(r)
    X = {}
    X['旧'] = max(0.0, min(D * r - P, TS + D - P - S))
    X['A-1'] = max(0.0, min(D * r - Qi, TS + D * sq - Qi - S))
    X['M2'] = max(0.0, min(D * r - P, TS + D * sq - P - S))
    X['M3'] = max(0.0, D * r - Qi)
    X['M4'] = max(0.0, D + TS - S) if cut == 0 else X['旧']
    Q10 = P - (ph(tl) - ph(tu))
    X['M10'] = max(0.0, min(D * r - Q10, TS + D * sq - Q10 - S))
    out = {}
    for k, x in X.items():
        U1 = (S + x - (TS + D * sq - Qi)) / D
        R4 = x / D
        R5 = (S + x + Qi - D - TS) / D
        out[k] = (U1, R4, R5)
    return out


grid = list(itertools.product([0.8, 1.0, 1.5, 2.0, 4.0], [0.0, 0.3, 0.6, 0.9], [0.0, 1.0], [1.0, 3.0], [0.0, 0.5]))
arms = ['旧', 'A-1', 'M2', 'M3', 'M4', 'M10']
red = {a: {'U1': 0, 'R4': 0, 'R5_1870': 0, 'R5_1913': 0, 'R5_all': 0} for a in arms}
for r, pd, s0, grow, cut in grid:
    o = row(100.0, r, pd, s0, grow, cut)
    for a in arms:
        U1, R4, R5 = o[a]
        if U1 > 1e-9 and R4 > 0: red[a]['U1'] += 1   # U1 は成約した行（X>0）だけ＝もとから在庫が上界を越えていた行を数えない
        if R4 > 2: red[a]['R4'] += 1
        if R5 > R5_MAX['1870']: red[a]['R5_1870'] += 1
        if R5 > R5_MAX['1913']: red[a]['R5_1913'] += 1
        if R5 > R5_MAX['all']: red[a]['R5_all'] += 1
print('格子 %d 行（r∈{0.8,1,1.5,2,4}×P/D∈{0,.3,.6,.9}×S/TS∈{0,1}×伸び∈{1,3}×u′の採算∈{0,0.5}）' % len(grid))
print('腕  ' + '  '.join('%s' % k for k in red['旧']))
for a in arms:
    print('%-4s ' % a + '  '.join('%d' % v for v in red[a].values()))
# 代表の行
print('代表の行（D=100・S=TS・伸び 1・u′<mc で採算 0）: r P/D  腕:U1 R4 R5')
for r, pd in ((1.0, 0.9), (1.5, 0.6), (2.0, 0.3), (4.0, 0.0), (4.0, 0.9)):
    o = row(100.0, r, pd, 1.0, 1.0, 0.0)
    print('r %.1f P/D %.1f  ' % (r, pd) + '  '.join('%s:%.2f %.2f %.2f' % (a, *o[a]) for a in arms))
print('伸びている行（伸び 3・S=0・u′<mc）: r P/D  腕:U1 R4 R5')
for r, pd in ((1.0, 0.6), (1.5, 0.9), (2.0, 0.6)):
    o = row(100.0, r, pd, 0.0, 3.0, 0.0)
    print('r %.1f P/D %.1f  ' % (r, pd) + '  '.join('%s:%.2f %.2f %.2f' % (a, *o[a]) for a in arms))

# 問い3 W2(a) の母集合〈UB_Bb≦0＜UB_A1〉（上界は行の門の時点・u_min＝ここでは u′）で、成約（X>0）の行の数を腕ごとに数える。
#   UB_Bb＝TS + D·√r − P − S・UB_A1＝TS + D·√r − min(P, P̂(u′)) − S（計器が自分で計算）。
pop = 0; dealt = {a: 0 for a in arms}
for r, pd, s0, grow, cut in grid:
    D = 100.0; TS = D; O = pd * D; P = O; S = s0 * TS
    tl = grow * O; tu = cut * tl; Qi = min(P, O + K * (tu - O)); sq = math.sqrt(r)
    ubb = TS + D * sq - P - S; uba = TS + D * sq - Qi - S
    if not (ubb <= 0 < uba): continue
    pop += 1
    o = row(D, r, pd, s0, grow, cut)
    for a in arms:
        if o[a][1] > 1e-12: dealt[a] += 1
print('W2(a) の母集合〈UB_Bb≦0＜UB_A1〉の行 %d・そのうち成約した行: ' % pop + '  '.join('%s %d' % (a, dealt[a]) for a in arms))
