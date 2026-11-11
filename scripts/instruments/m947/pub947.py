# (947) 合流：新しい基線 m947 の成績表・注視の表・NG の基準・読みの出力を measurements へ写す（バイトのまま・道具の出力の改行は変えない）。
import os, shutil
W = 'D:/PoB_work/m947'; T = 'D:/PoB_work/wt_m947/measurements'
def cp(a, b):
    os.makedirs(os.path.dirname(b), exist_ok=True); shutil.copyfile(a, b); print('cp', os.path.basename(a), '->', b[len(T) + 1:])
for src, dst in (('b10_mM_10.txt', 'b10_20261108_m947_ins'), ('b10_p10_10.txt', 'b10_20261108_m947'), ('b5_p5_5.txt', 'b5_20261108_m947'),
                 ('b10_n1_10.txt', 'b10_20261108_m947_n1_ins'), ('b10_n2_10.txt', 'b10_20261108_m947_n2_ins'), ('b10_mS_10_s11.txt', 'b10s11_20261108_m947_ins')):
    for ext in ('', '.cost', '.manifest'):
        cp(W + '/' + src + ext, T + '/' + dst + ext)
for src, dst in (('mon_mM_10.txt', 'mon_20261108_m947.txt'), ('mon_n1_10.txt', 'mon_20261108_m947_n1.txt'), ('mon_n2_10.txt', 'mon_20261108_m947_n2.txt'), ('mon_mS_10_s11.txt', 'mon_20261108_m947s11.txt')):
    cp(W + '/' + src, T + '/' + dst)
for f in sorted(os.listdir(W + '/ng_m947')):
    cp(W + '/ng_m947/' + f, T + '/ng947_20261108/' + f)
R = T + '/m947_20261108'
for tag, sub, suf in (('mM', 'armM', ''), ('mS', 'armS11', ''), ('n1', 'null', '_n1'), ('n2', 'null', '_n2')):
    for f in sorted(os.listdir(W + '/read/' + tag)):
        p = W + '/read/' + tag + '/' + f
        if os.path.isdir(p) or f.endswith('.log'):
            continue
        b, e = os.path.splitext(f)
        cp(p, R + '/' + sub + '/' + b + suf + e)
for f in ('cmp_mM_icB.txt', 'cmp_mS_icS.txt', 'removed_lines.txt', 'arms_start.txt'):
    cp(W + '/' + f, R + '/' + f)
