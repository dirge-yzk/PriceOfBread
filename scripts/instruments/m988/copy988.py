# (968) 合流：腕の成績表・.cost・.manifest・注視の表・読みの出力・NG の新しい基準を measurements へ写す（バイトのまま・新しい基線 m968 の名で・copy961.py の型）。
# (988) 合流（copy981.py の写し・名前を m988 へ・生の比べ・cpop_route・c89ls の no-go・CEARN=1 の種 11〜20 の門の読みを足した）
# (975) 合流（copy968.py の写し・名前を m975 へ・アブレーションの読み read/abl と led・bx_read・ukdef の記録を足した）
#   python -X utf8 copy968.py <作業フォルダ> <measurements>
import os, sys
W, M = sys.argv[1], sys.argv[2]; D = M + '/m988_20261109'
def cp(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    open(dst, 'wb').write(open(src, 'rb').read()); print('cp', os.path.basename(src), '->', dst[len(M) + 1:])
for src, dst in [('b5_p5_5.txt', 'b5_20261109_m988'), ('b10_p10_10.txt', 'b10_20261109_m988'), ('b10_mM_10.txt', 'b10_20261109_m988_ins'),
                 ('b10_n1_10.txt', 'b10_20261109_m988_n1_ins'), ('b10_n2_10.txt', 'b10_20261109_m988_n2_ins'), ('b10_mS_10_s11.txt', 'b10s11_20261109_m988_ins')]:
    for ext in ('', '.cost', '.manifest'):
        cp(W + '/' + src + ext, M + '/' + dst + ext)
for src, dst in [('mon_mM_10.txt', 'mon_20261109_m988.txt'), ('mon_n1_10.txt', 'mon_20261109_m988_n1.txt'), ('mon_n2_10.txt', 'mon_20261109_m988_n2.txt'), ('mon_mS_10_s11.txt', 'mon_20261109_m988s11.txt')]:
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
        cp(W + '/ng_mM/' + f, M + '/ng988_20261109/' + f[:-len('_mM.txt')] + '_m988.txt')
for src, dst in [('arms_start.txt', 'arms_start.txt'), ('deleted.txt', 'deleted.txt'), ('created.txt', 'created.txt')]:
    if os.path.exists(W + '/' + src):
        cp(W + '/' + src, D + '/' + dst)
for src, dst in [('rawcmp_M_vs_c89ddB.txt', 'rawcmp_M_vs_c89ddB.txt'), ('rawcmp_S11_vs_c89ddS11.txt', 'rawcmp_S11_vs_c89ddS11.txt'), ('cpop_route.txt', 'cpop_route.txt'),
                 ('suite_summary.txt', 'suite_summary.txt'), ('read/nogo983.txt', 'armM/nogo983.txt'), ('read/sc/gate_mSc.txt', 'armSc/gate984_mSc.txt'),
                 ('read/sc/gate_zSc.txt', 'armSc/gate984_zSc.txt'), ('read/sc/judge.txt', 'armSc/judge984.txt'), ('read/sc/rawcmp_mSc_vs_mS.txt', 'armSc/rawcmp_mSc_vs_mS.txt'),
                 ('read/sc/rawcmp_zSc_vs_a11.txt', 'armSc/rawcmp_zSc_vs_a11.txt'), ('ratchet.mark.log', 'ratchet.txt')]:
    if os.path.exists(W + '/' + src):
        cp(W + '/' + src, D + '/' + dst)
    else:
        print('無い', src)
