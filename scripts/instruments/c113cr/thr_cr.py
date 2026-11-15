# (1151) c113cr 閾値の台本（事前登録 §2 の式を先に書き、数はこの出力から写す）。表を読むだけ。
# 使い方: python -X utf8 -I thr_cr.py <M の生> <N1 の生> <N2 の生> > thr_cr.txt
#   ヌル腕＝m1150 の M（基線）・N1・N2 の 3 本（README のヌル腕の幅・m1142 §2 の式）。
#   C15_hi・G1p_hi＝3 本の値の 最大×(1＋2×(幅−1))（幅＝最大÷最小・最小が 0 なら 2×最大）＝onset_cr.upper。
#   C8c_hi＝−(最大＋幅)（3 本の C8c″〈同じ州の集合・国の中の対照・O＝0・平時に限らない〉の最大と、最大−最小）＝符号を反した上側。
#   データの州（dataStateFlag＝1）は C15・G1′ から除く（v の訂正 1）。参考に N1・N2 の 2 本だけの値も # 行に書く。
import sys, os, subprocess, statistics as st
sys.stdout.reconfigure(newline='\n')
D = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, D)
import onset_cr
M, N1, N2 = sys.argv[1:4]
on = {k: onset_cr.onset(v) for k, v in (('M', M), ('N1', N1), ('N2', N2))}
c8 = {}
for k, v in (('M', M), ('N1', N1), ('N2', N2)):
    c8[k] = onset_cr.c8c(v)
print('# (1151) thr_cr.py 閾値（式は事前登録 §2）')
for k in ('M', 'N1', 'N2'):
    print(f"# {k}: C15 {on[k]['C15']:.6g}（データの州 {on[k]['C15_data']:.6g}・講和の付与 {on[k]['C15_grant']:.6g}）・G1′ {on[k]['G1p_sum']}（種ごと {on[k]['G1p_per']}）・C8c″ {c8[k][0]:.6g}・C8d″ {c8[k][1]:.6g}・種 {on[k]['seeds']}")
A = ('M', 'N1', 'N2')
v2 = [c8['N1'][0], c8['N2'][0]]
print(f"# 参考（N1・N2 の 2 本だけ）：C15 {onset_cr.upper([on['N1']['C15'], on['N2']['C15']]):.6g}・G1′ {onset_cr.upper([on['N1']['G1p_sum'], on['N2']['G1p_sum']]):.6g}・C8c″ {-(max(v2) + (max(v2) - min(v2))):.6g}")
print(f"C15_hi={onset_cr.upper([on[a]['C15'] for a in A])!r}")
print(f"G1p_hi={onset_cr.upper([on[a]['G1p_sum'] for a in A])!r}")
v = [c8[a][0] for a in A]
print(f"C8c_hi={-(max(v) + (max(v) - min(v)))!r}")
