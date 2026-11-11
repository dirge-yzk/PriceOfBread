# (968) 合流：腕の成績表・.cost・.manifest・注視の表・読みの出力・NG の新しい基準を measurements へ写す（バイトのまま・新しい基線 m968 の名で・copy961.py の型）。
#   python -X utf8 copy968.py <作業フォルダ> <measurements>
import os, sys
W, M = sys.argv[1], sys.argv[2]; D = M + '/m968_20261108'
def cp(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    open(dst, 'wb').write(open(src, 'rb').read()); print('cp', os.path.basename(src), '->', dst[len(M) + 1:])
for src, dst in [('b5_p5_5.txt', 'b5_20261108_m968'), ('b10_p10_10.txt', 'b10_20261108_m968'), ('b10_mM_10.txt', 'b10_20261108_m968_ins'),
                 ('b10_n1_10.txt', 'b10_20261108_m968_n1_ins'), ('b10_n2_10.txt', 'b10_20261108_m968_n2_ins'), ('b10_mS_10_s11.txt', 'b10s11_20261108_m968_ins')]:
    for ext in ('', '.cost', '.manifest'):
        cp(W + '/' + src + ext, M + '/' + dst + ext)
for src, dst in [('mon_mM_10.txt', 'mon_20261108_m968.txt'), ('mon_n1_10.txt', 'mon_20261108_m968_n1.txt'), ('mon_n2_10.txt', 'mon_20261108_m968_n2.txt'), ('mon_mS_10_s11.txt', 'mon_20261108_m968s11.txt')]:
    if os.path.exists(W + '/' + src):
        cp(W + '/' + src, M + '/' + dst)
for tag, sub, suf in [('mM', 'armM', ''), ('mS', 'armS11', ''), ('n1', 'null', '_n1'), ('n2', 'null', '_n2')]:
    r = W + '/read/' + tag
    for f in sorted(os.listdir(r)):
        if f.endswith('.log') or os.path.isdir(r + '/' + f):
            continue
        b, e = os.path.splitext(f)
        cp(r + '/' + f, D + '/' + sub + '/' + b + suf + e)
for f in sorted(os.listdir(W + '/ng_mM')):
    if f.endswith('_mM.txt'):
        cp(W + '/ng_mM/' + f, M + '/ng968_20261108/' + f[:-len('_mM.txt')] + '_m968.txt')
for src, dst in [('arms_start.txt', 'arms_start.txt'), ('rawcmp_M_vs_c86ltB.txt', 'rawcmp_M_vs_c86ltB.txt'), ('rawcmp_S11_vs_c86ltS.txt', 'rawcmp_S11_vs_c86ltS.txt'),
                 ('suite.mark.log', 'suite_summary.txt'), ('ratchet.mark.log', 'ratchet_summary.txt'), ('deleted.txt', 'deleted.txt'), ('created.txt', 'created.txt')]:
    if os.path.exists(W + '/' + src):
        cp(W + '/' + src, D + '/' + dst)
