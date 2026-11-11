# (961) 合流：腕の成績表・.cost・.manifest・注視の表と読みの出力を measurements へ写す（バイトのまま・新しい基線 m961 の名で）。
#   python -X utf8 copy961.py <作業フォルダ> <写し先の measurements か記録のフォルダ> <読みのフォルダ（写し先の下）>
#   基線＝copy961.py D:/PoB_work/m961_work2 D:/PoB_work/wt_m961/measurements m961_20261108
#   外した合わせた木の記録＝copy961.py D:/PoB_work/m961_work D:/PoB_work/wt_m961/measurements/m961_20261108/combo_kp_lt .
import os, sys
W, M, sub0 = sys.argv[1], sys.argv[2], sys.argv[3]; D = M + '/' + sub0
def cp(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    open(dst, 'wb').write(open(src, 'rb').read()); print('cp', os.path.basename(src), '->', dst[len(M) + 1:])
for src, dst in [('b5_p5_5.txt', 'b5_20261108_m961'), ('b10_p10_10.txt', 'b10_20261108_m961'), ('b10_mM_10.txt', 'b10_20261108_m961_ins'),
                 ('b10_n1_10.txt', 'b10_20261108_m961_n1_ins'), ('b10_n2_10.txt', 'b10_20261108_m961_n2_ins'), ('b10_mS_10_s11.txt', 'b10s11_20261108_m961_ins')]:
    for ext in ('', '.cost', '.manifest'):
        cp(W + '/' + src + ext, M + '/' + dst + ext)
for src, dst in [('mon_mM_10.txt', 'mon_20261108_m961.txt'), ('mon_n1_10.txt', 'mon_20261108_m961_n1.txt'), ('mon_n2_10.txt', 'mon_20261108_m961_n2.txt'), ('mon_mS_10_s11.txt', 'mon_20261108_m961s11.txt')]:
    if os.path.exists(W + '/' + src):
        cp(W + '/' + src, M + '/' + dst)
for tag, sub, suf in [('mM', 'armM', ''), ('mS', 'armS11', ''), ('n1', 'null', '_n1'), ('n2', 'null', '_n2')]:
    r = W + '/read/' + tag
    for f in sorted(os.listdir(r)):
        if f.endswith('.log') or os.path.isdir(r + '/' + f):
            continue
        b, e = os.path.splitext(f)
        cp(r + '/' + f, D + '/' + sub + '/' + b + suf + e)
for src, dst in [('b10_alt0_10.txt', 'ablation/b10_alt0_10.txt'), ('b10_akp0_10.txt', 'ablation/b10_akp0_10.txt'),
                 ('b10_alt0_10.txt.cost', 'ablation/b10_alt0_10.txt.cost'), ('b10_akp0_10.txt.cost', 'ablation/b10_akp0_10.txt.cost'),
                 ('arms_start.txt', 'arms_start.txt'), ('ratchet.txt', 'ratchet.txt')]:
    if os.path.exists(W + '/' + src):
        cp(W + '/' + src, D + '/' + dst)
