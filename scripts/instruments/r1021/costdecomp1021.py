# (1021) c95cf：計算量の件数の行の旧÷新と、tradeskip の門の和の分解（読むだけ・集計の写しから計算）
# 使い方：python -X utf8 -I costdecomp1021.py <出力ファイル> <リポジトリの根> <fill1021_out.txt>
import os, re, sys
out, root, fillout = sys.argv[1], sys.argv[2], sys.argv[3]
M = os.path.join(root, 'measurements')
names = ['価格：財の歩', '価格：日次の写像の回', '清算：見た買い手行', '清算：着値の充填', '生産：生産者×月']
def cost(fn):
    R = {}
    with open(os.path.join(M, fn), 'rb') as f:
        for ln in f.read().decode('utf-8').splitlines():
            for n in names:
                if ln.strip().startswith(n):
                    p = ln.strip().split('\t'); R[n] = (float(p[1]), float(p[2]), float(p[3]))
    return R
runs = {'m1001': 'b10_20261109_m1001_ins.cost', 'm1017z': 'b10_20261110_m1017z_ins.cost', 'm1017': 'b10_20261110_m1017_ins.cost',
        'N1': 'b10_20261110_m1017_n1_ins.cost', 'N2': 'b10_20261110_m1017_n2_ins.cost', 'S11旧': 'b10s11_20261109_m1001_ins.cost', 'S11新': 'b10s11_20261110_m1017_ins.cost'}
C = {k: cost(v) for k, v in runs.items()}
o = open(out, 'w', encoding='utf-8', newline='\n')
def P(*a): print(*a, file=o)
P('[A] .cost の件数（1 試合の平均・助走を含む）と 新÷旧')
for n in names:
    b = C['m1001'][n][0]
    P(f'  {n}: 旧 {b:.6g}・新 {C["m1017"][n][0]:.6g}（{C["m1017"][n][0] / b:.4f}）・N1 {C["N1"][n][0] / b:.4f}・N2 {C["N2"][n][0] / b:.4f}・S11 新÷旧 {C["S11新"][n][0] / C["S11旧"][n][0]:.4f}・m1017z÷旧 {C["m1017z"][n][0] / b:.4f}・秒の差 {C["m1017"][n][2] - C["m1001"][n][2]:+.2f}')
f0, f1 = C['m1001']['清算：着値の充填'][0], C['m1017']['清算：着値の充填'][0]
r0, r1 = C['m1001']['清算：見た買い手行'][0], C['m1017']['清算：見た買い手行'][0]
P(f'  充填÷台帳の買い手行：旧 {f0 / r0:.4f}・新 {f1 / r1:.4f}（{(f1 / r1) / (f0 / r0):.4f} 倍）')
# 旧の tradeskip の和（costattr.txt＝keep_1011/base_ins・readg_m1006.txt の R2＝keep_1006/base_ins・どちらも .cost の件数が m1001 と同じ盤面）
ca = {}
with open(os.path.join(M, 'm1014_20261110', 'costattr.txt'), 'rb') as f:
    for ln in f.read().decode('utf-8').splitlines():
        m = re.match(r'(\w+): 基準 (\S+) 腕 (\S+)', ln)
        if m: ca[m.group(1)] = (float(m.group(2)), float(m.group(3)))
rg = open(os.path.join(M, 'm1006_20261110', 'readg_m1006.txt'), 'rb').read().decode('utf-8')
m = re.search(r"'sold': (\S+), 'cap': (\S+), 'wedge': (\S+), 'passed': (\S+)\}", rg)
old = dict(zip(['sold', 'cap', 'wedge', 'passed'], [float(v) for v in m.groups()]))
fo = open(fillout, 'rb').read().decode('utf-8').split('== n1')[0]
m2 = re.search(r'sold (\S+) cap (\S+) wedge (\S+) passed (\S+)・', fo)
new = dict(zip(['sold', 'cap', 'wedge', 'passed'], [float(v) for v in m2.groups()]))
P('[B] tradeskip（調べる年の第 4 四半期の最初の清算だけ）の越境の売り手の最初の門の和：旧（readg_m1006 R2）と新（keep_1017/base_ins）')
for k in old: P(f'  {k}: 旧 {old[k]:.6g}・新 {new[k]:.6g}・新÷旧 {new[k] / old[k]:.4f}')
s0, s1 = ca['rowScan']; p0, p1 = old['passed'], new['passed']
P(f'  楔を通った越境の売り手÷走査行：旧 {p0 / s0:.3f}・新 {p1 / s1:.3f}（{(p1 / s1) / (p0 / s0):.4f} 倍）')
dP = p1 - p0; byRows = (s1 - s0) * (p0 / s0); byPer = (p1 / s1 - p0 / s0) * s1
P(f'  楔を通った売り手の増分 {dP:.6g} の分解：走査行の増加の分 {byRows:.6g}（{byRows / dP:.3f}）・行あたりの増加の分 {byPer:.6g}（{byPer / dP:.3f}）')
P('[C] 走査行の増分の内訳（tradeskip の行の結果は排他・旧＝costattr の基準）')
d = {k: ca[k][1] - ca[k][0] for k in ('rows', 'rowIdle', 'rowNoNeed', 'rowBudget0', 'rowScan')}
P('  ' + '・'.join(f'{k} {v:+.6g}' for k, v in d.items()))
P(f'  走査行の増分 {d["rowScan"]:+.6g}＝行の増加 {d["rows"]:+.6g}（{d["rows"] / d["rowScan"]:.3f}）−番で飛ばした行の差 {d["rowIdle"]:+.6g}（{-d["rowIdle"] / d["rowScan"]:.3f}）−予算 0 の差 {d["rowBudget0"]:+.6g}（{-d["rowBudget0"] / d["rowScan"]:.3f}）−需要なしの差 {d["rowNoNeed"]:+.6g}（{-d["rowNoNeed"] / d["rowScan"]:.3f}）')
o.close()
