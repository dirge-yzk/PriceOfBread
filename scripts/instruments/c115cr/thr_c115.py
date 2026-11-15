# (1165) c115cr 閾値（事前登録 §2 の式）。ヌル腕 3 本＝M（keep_1164/base_ins）・N1・N2（keep_1150/n1・n2＝全表のヌル腕）。表を読むだけ。
# 使い方: python -X utf8 -I thr_c115.py <M> <N1> <N2> > thr_c115.txt
#   E_hi_full＝3 本の全長（668 四半期・種 1〜10）の種の中央を upper（最大×(1＋2×(幅−1))・最小 0 なら 2×最大）。
#   E_hi_short＝3 本の窓 0〜79・種 1〜2 の種の中央を同じ式で。基準の盤面＝その腕自身（ヌル腕は旗 0）。
#   C8c_hi＝−(最大＋(最大−最小))（onset_cr.c8c・2 回目の式）。一つ抜きの空振り（1 本を腕と見なし残り 2 本で閾値）も書く。
import sys, os
sys.stdout.reconfigure(newline='\n')
D = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, D)
import e_c115, onset_cr
A = dict(zip(('M', 'N1', 'N2'), sys.argv[1:4]))
full = {k: e_c115.stat(v, None, range(1, 11), 668) for k, v in A.items()}
short = {k: e_c115.stat(v, None, range(1, 3), 80) for k, v in A.items()}
c8 = {k: onset_cr.c8c(v) for k, v in A.items()}
print('# (1165) thr_c115.py 閾値（式は事前登録 §2）')
for k in A:
    print(f"# {k}: E 全長 種の中央 {full[k][0]:.6g}（種ごと {[round(x[0], 5) for x in full[k][1]]}・母集合の州 {full[k][1][0][1]}）・E 窓 0〜79 種 1〜2 {short[k][0]:.6g}（{[round(x[0], 5) for x in short[k][1]]}）・C8c″ {c8[k][0]:.6g}")
def loo(d):
    miss = 0
    for k in d:
        rest = [d[j][0] for j in d if j != k]
        miss += d[k][0] > e_c115.upper(rest)
    return miss
print(f"# 一つ抜きの空振り（3 通り）：E 全長 {loo(full)}/3・E 窓 {loo(short)}/3")
print(f"E_hi_full={e_c115.upper([full[k][0] for k in A])!r}")
print(f"E_hi_short={e_c115.upper([short[k][0] for k in A])!r}")
v = [c8[k][0] for k in A]
print(f"C8c_hi={-(max(v) + (max(v) - min(v)))!r}")
