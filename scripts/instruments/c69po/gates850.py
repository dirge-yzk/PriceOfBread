# -*- coding: utf-8 -*-
# (850) c69po 門の判定（読むだけ）：p1_paper850.py の出力（--base --new M845=基線 N1=… N2=… <腕>=…）を読み、事前登録 [3] の門を数で判定する。
#   python -X utf8 -I gates850.py <p1_paper850.py の出力> <腕の名>
# 門：G0 点検（t_eng＝tNew の割合 ≥0.9999）・G1（ΔS＞W）・G1u（ΔS ≤ T＋W）・G2a（P_ctrl ≥1000 かつ t_eng＝tOld の割合 ≥0.9999）・G2b（|ΔC| ≤ W_C）・G3（記録）。
import sys, re
sys.stdout.reconfigure(encoding='utf-8')
txt = open(sys.argv[1], encoding='utf-8').read(); arm = sys.argv[2]
blocks = {}
for m in re.finditer(r'== 腕 (\S+) (\S+)\n(.*?)(?=\n== |\Z)', txt, re.S): blocks[m.group(1)] = m.group(3)
def num(b, pat):
    m = re.search(pat, b); return float(m.group(1)) if m else float('nan')
def SCG(name):
    b = blocks[name]
    return (num(b, r'S（[^）]*）([-\d.e+]+)'), num(b, r'C（[^）]*）([-\d.e+]+)'), num(b, r'G3（[^）]*）([-\d.e+]+)'))
names = list(blocks); base = names[0]
Sb, Cb, Gb = SCG(base); S1, C1, G1_ = SCG('N1'); S2, C2, G2_ = SCG('N2'); SB, CB, GB = SCG(arm)
W = max(abs(S1 - Sb), abs(S2 - Sb)); WC = max(abs(C1 - Cb), abs(C2 - Cb)); WG = max(abs(G1_ - Gb), abs(G2_ - Gb))
b = blocks[arm]
T = num(b, r'T（[^）]*）([-\d.e+]+)')
m = re.search(r'G0 点検 t_eng＝tNew（1e-8）(\d+) / (\d+)', b); g0ok, g0n = (int(m.group(1)), int(m.group(2))) if m else (0, 0)
m = re.search(r't_eng＝tOld (\d+) / (\d+)', b); g0old = int(m.group(1)) if m else 0
m = re.search(r'G2a P_ctrl (\d+) 行  t_eng＝tOld（1e-8）(\d+)', b); g2n, g2ok = (int(m.group(1)), int(m.group(2))) if m else (0, 0)
m = re.search(r'm_r の自前の数と militancyMeanIndex の一致 (\d+) / (\d+)', b); mrok, mrn = (int(m.group(1)), int(m.group(2))) if m else (0, 0)
dS, dC, dG = SB - Sb, CB - Cb, GB - Gb
def v(ok): return '通る' if ok else '落ちる'
print('腕 %s（基線 %s）' % (arm, base))
print('G0 点検 t_eng＝tNew %d/%d（割合 %.6f）＝%s  （t_eng＝tOld %d・m_r の一致 %d/%d）' % (g0ok, g0n, g0ok / g0n if g0n else float('nan'), v(g0n > 0 and g0ok / g0n >= 0.9999), g0old, mrok, mrn))
print('G1  ΔS %.6g ＞ W %.6g ＝%s' % (dS, W, v(dS > W)))
print('G1u ΔS %.6g ≤ T %.6g ＋ W %.6g ＝ %.6g ＝%s' % (dS, T, W, T + W, v(dS <= T + W)))
print('G2a P_ctrl %d 行・t_eng＝tOld %d（割合 %.6f）＝%s' % (g2n, g2ok, g2ok / g2n if g2n else float('nan'), v(g2n >= 1000 and g2ok / g2n >= 0.9999)))
print('G2b |ΔC| %.6g ≤ W_C %.6g ＝%s' % (abs(dC), WC, v(abs(dC) <= WC)))
print('G3（記録）ΔG3 %.6g（ヌル腕の幅 %.6g）' % (dG, WG))
