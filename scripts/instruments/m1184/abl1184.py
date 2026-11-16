# (1184) 合流：★(1143) のアブレーション（t1 と hd は在庫の目標 TargetStock を共有）の 4 隅と旧・新のヌル腕を 1 枚に並べる（記録・採否に使わない）。
#   python -X utf8 -I abl1184.py <作業フォルダ> <リポジトリの木>
#   4 隅＝M（t1 1・hd 0.0＝keep_1184/base_ins）・A_hd（t1 1・hd 0.5＝m1178＝keep_1178/base_ins の紙）・At1（t1 0・hd 0.0）・A0（t1 0・hd 0.5）。
import json, os, re, sys
W, R = sys.argv[1:3]
def pj(p): return json.load(open(p, encoding='utf-8'))
def t1(p):
    t = open(p, encoding='utf-8').read()
    s = re.search(r'retreatRowSum ([0-9.e+]+)', t); n = re.search(r'retreatRowN (\d+)', t); r = re.search(r'rho_X ([0-9.]+) ≧', t)
    return float(s.group(1)), int(n.group(1)), float(r.group(1))
def food(p):
    t = open(p, encoding='utf-8').read(); m = re.search(r'戦時 \d+/\d+=([0-9.]+)', t); return float(m.group(1))
H = R + '/measurements/m1184_20261116/hdpaper'
rows = [('M（t1 1・hd 0）', pj(W + '/read/M/paper_hd.json'), t1(W + '/abl/t1_n_base.txt')),
        ('A_hd＝m1178（t1 1・hd 0.5）', pj(H + '/paper_M.json'), t1(W + '/abl/t1_o_base.txt')),
        ('At1（t1 0・hd 0）', pj(W + '/abl/paper_At1.json'), t1(W + '/abl/t1_At1.txt')),
        ('A0（t1 0・hd 0.5）', pj(W + '/abl/paper_A0.json'), t1(W + '/abl/t1_A0.txt')),
        ('旧 N1（m1178・hd 0.5）', pj(H + '/paper_N1.json'), t1(W + '/abl/t1_o_n1.txt')),
        ('旧 N2（m1178・hd 0.5）', pj(H + '/paper_N2.json'), t1(W + '/abl/t1_o_n2.txt')),
        ('新 N1（m1184・hd 0）', pj(W + '/abl/paper_nN1.json'), t1(W + '/abl/t1_n_n1.txt')),
        ('新 N2（m1184・hd 0）', pj(W + '/abl/paper_nN2.json'), t1(W + '/abl/t1_n_n2.txt'))]
K = ['g1_war_prio_med', 'g2_arms_war_med', 'g3_famine_war_perK', 'rec_tb_impQ']
print('腕\t' + '\t'.join(K) + '\tt1 退きの和\tt1 退いた行\tρ_X')
for name, p, (s, n, r) in rows:
    print(name + '\t' + '\t'.join(f'{p[k]:.6g}' for k in K) + f'\t{s:.6g}\t{n}\t{r:.6f}')
v = {name: (p, t) for name, p, t in rows}
def ratio(a, b, k): return v[a][0][k] / v[b][0][k]
print('# 比（hd の効き＝hd 0÷hd 0.5）：t1 1 の盤面 vs t1 0 の盤面＝足し算なら同じ比')
for k in K:
    print(f'{k}\tt1 1：{ratio("M（t1 1・hd 0）", "A_hd＝m1178（t1 1・hd 0.5）", k):.4f}\tt1 0：{ratio("At1（t1 0・hd 0）", "A0（t1 0・hd 0.5）", k):.4f}')
for i, lab in ((0, '退きの和'), (2, 'ρ_X')):
    a = v['M（t1 1・hd 0）'][1][i] / v['A_hd＝m1178（t1 1・hd 0.5）'][1][i]
    print(f't1 {lab}\thd 0÷hd 0.5（t1 1 の盤面）：{a:.4f}')
