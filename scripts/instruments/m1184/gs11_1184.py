# (1184) 合流（m1178/gs11_1178.py の写し・中身は同じ）
# (1178) 合流：種 11〜20 の門の閾値を、線の事前登録と同じ定義で同じ種の旗 0 の側（S11z）から取る（★(1150)・★(1157) の型・予言 measurements/m1178_20261116_prereg_merge.txt [3]）。
#   k1：python gs11_1178.py k1 <S11 の gates_k1.txt> <S11z の gates_k1.txt>
#       G1 ≧ ⌈1.2×S11z の G1⌉・G2 0・G3b ≦ S11z の G3b＋2・G5 植民地格 ≧ ⌊S11z÷2⌋・本国の中核 ≧ ⌊S11z÷2⌋（G3a は記録）
#   hd：python gs11_1178.py hd <S11 の paper.json> <S11z の paper.json> <M.json> <N1.json> <N2.json> <F1.json> <F2.json>
#       W は種 1〜10 の物（hd.py の W＝1＋2×(最大÷最小−1)）・G1 ＜ S11z÷W1・G2 ≧ S11z÷W2・G3 ≦ S11z×W3
import json, math, sys
sys.stdout.reconfigure(newline='\n')
mode = sys.argv[1]
if mode == 'k1':
    def rd(p):
        d = {}
        for ln in open(p, encoding='utf-8'):
            t = ln.rstrip('\n').split('\t')
            if len(t) >= 2 and not t[0].startswith('#'): d.setdefault(t[0], t[1])
        return d
    a, z = rd(sys.argv[2]), rd(sys.argv[3])
    k = lambda d, key: float(d[key])
    G1, G2, G3A, G3B = 'G1 働いた（下）', 'G2 形の一致（両）外れの行', 'G3a 上（行ごと）外れの行', 'G3b 上 本国の中核と編入で支配度が 0 に届いた州'
    G5C = 'G5 0 へ寄らない（下）成人男子以上×中核≤0.5×Colonial(k>0) 余り<0'
    G5K = 'G5 0 へ寄らない（下）成人男子以上×中核≤0.5×Core 余り<0'
    rows = []
    t = math.ceil(1.2 * k(z, G1)); rows.append((G1, k(a, G1), f'≧{t}（⌈1.2×S11z {k(z, G1):.0f}⌉）', k(a, G1) >= t))
    rows.append((G2, k(a, G2), '＝0', k(a, G2) == 0))
    t = k(z, G3B) + 2; rows.append((G3B, k(a, G3B), f'≦{t:.0f}（S11z {k(z, G3B):.0f}＋2）', k(a, G3B) <= t))
    for g in (G5C, G5K):
        t = math.floor(k(z, g) / 2); rows.append((g, k(a, g), f'≧{t}（⌊S11z {k(z, g):.0f}÷2⌋）', k(a, g) >= t))
    print(f'# (1178) 種 11〜20 の k1 の門（閾値は S11z〔旗 0〕から）')
    for n, v, th, ok in rows: print(f'{n}\t{v:.0f}\t{th}\t{"緑" if ok else "赤"}')
    print(f'記録 G3a 外れの行\t{k(a, G3A):.0f}（S11z {k(z, G3A):.0f}）')
    print(f'総合\t{"緑" if all(r[3] for r in rows) else "赤"}')
elif mode == 'hd':
    J = lambda p: json.load(open(p, encoding='utf-8'))
    A, Z, M, N1, N2, F1, F2 = map(J, sys.argv[2:9])
    W = lambda v: 1 + 2 * (max(v) / min(v) - 1)
    rows = []
    k = 'g1_war_prio_med'; w = W([M[k], N1[k], N2[k]]); lim = Z[k] / w; rows.append(('G1 働いた（下）戦時×優先財の在庫÷需要の中央', A[k], f'< {lim:.6g}（S11z {Z[k]:.6g}÷W {w:.6g}）', A[k] < lim))
    k = 'g2_arms_war_med'; w = W([M[k], N1[k], N2[k]]); lim = Z[k] / w; rows.append(('G2 壊さない（下）戦時の軍需の充足の中央', A[k], f'≧ {lim:.6g}（S11z {Z[k]:.6g}÷W {w:.6g}）', A[k] >= lim))
    k = 'g3_famine_war_perK'; w = W([M[k], F1[k], F2[k]]); lim = Z[k] * w; rows.append(('G3 壊さない（上）戦時の飢えの死÷人口', A[k], f'≦ {lim:.6g}（S11z {Z[k]:.6g}×W {w:.6g}）', A[k] <= lim))
    print('# (1178) 種 11〜20 の hd の門（W は種 1〜10・基準の値は S11z〔旗 0〕）')
    for n, v, th, ok in rows: print(f'{n}\t{v:.6g}\t{th}\t{"緑" if ok else "赤"}')
    print(f'総合\t{"緑" if all(r[3] for r in rows) else "赤"}')
else:
    raise SystemExit('mode は k1 か hd')
