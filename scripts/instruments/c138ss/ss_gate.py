# (1325) c138ss 自給の世帯の家計 3-1 の門と紙の読み手（読むだけ・エンジンの関数を呼ばない）。
# 使い方: python -X utf8 -I ss_gate.py <ins（D:/ の形）> <種の起点> <種の数>
# 読む表：subst_s（s3 の列と、3-1 の器で足した列 s3ESoldInst・s3EOfferedEngine・s3ESoldEngine・s3RPaidEngine・s3QEngine・s3CrossInst）・class_s。
#  H1（史実の桁の線・この線が一番動かす量）：κ_E＝売れた E÷Q（エンジンの実際の側＝s3ESoldEngine÷s3QEngine の州の和・1913 年の 4 四半期）。錨〈推〉露 0.15・清 0.3＝線は露≧0.015・清≧0.03。
#      影（計器の写し＝s3EInst÷s3QInst・担い手の POP だけ）も並べる。列が無い表（旗 0 の前の基線）は実際の側＝0。
#  H2（史実の桁の線・壊さない側）：1913 年の農業の外の割合＝1−農民÷（兵士を除く全ての階級）・9 国。錨〈推〉。線＝錨÷10 以上・錨×10 以下（1 を超える側は空）。
#  G1（実際の側・⑮）：売った E が出した E を超える行 0・出した E が正の行の数・表の欠け。
import sys, os, statistics
from collections import defaultdict
ins, sb, n = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
KAPPA = {'ロシア': 0.15, '清': 0.3}
NONFARM = {'イギリス': 0.9, 'フランス': 0.6, 'プロイセン': 0.65, 'アメリカ': 0.7, 'ロシア': 0.25, '日本': 0.4, '清': 0.2, 'オスマン': 0.2, 'オーストリア': 0.45}
def fnum(x):
    try:
        v = float(x); return v if v == v else None
    except Exception: return None
kap = defaultdict(dict); kapS = defaultdict(dict); nf = defaultdict(dict)
offered_rows = sold_gt = missing = 0; seeds_cols = 0; tot_off = tot_sold = tot_paid = 0.0
for sd in range(sb, sb + n):
    p = os.path.join(ins, f'subst_s{sd}.tsv')
    if not os.path.exists(p): missing += 1; continue
    acc = defaultdict(lambda: [0.0, 0.0, 0.0, 0.0])
    with open(p, 'r', encoding='utf-8', newline='') as f:
        h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        has = 's3ESoldEngine' in ix
        if has: seeds_cols += 1
        for line in f:
            c = line.rstrip('\r\n').split('\t')
            y = int(c[ix['year']]); nat = c[ix['nation']]
            if has:
                off = fnum(c[ix['s3EOfferedEngine']]) or 0.0; so = fnum(c[ix['s3ESoldEngine']]) or 0.0
                if off > 0: offered_rows += 1
                if so > off * (1 + 1e-9) + 1e-12: sold_gt += 1
                tot_off += off; tot_sold += so; tot_paid += fnum(c[ix['s3RPaidEngine']]) or 0.0
            if y != 1913: continue
            a = acc[nat]
            if has:
                a[0] += fnum(c[ix['s3ESoldEngine']]) or 0.0; a[1] += fnum(c[ix['s3QEngine']]) or 0.0
            a[2] += fnum(c[ix['s3EInst']]) or 0.0; a[3] += fnum(c[ix['s3QInst']]) or 0.0
    for nat, a in acc.items():
        kap[nat][sd] = a[0] / a[1] if a[1] > 0 else 0.0
        kapS[nat][sd] = a[2] / a[3] if a[3] > 0 else 0.0
    pc = os.path.join(ins, f'class_s{sd}.tsv')
    if not os.path.exists(pc): missing += 1; continue
    cs = defaultdict(lambda: [0.0, 0.0])
    with open(pc, 'r', encoding='utf-8', newline='') as f:
        h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\r\n').split('\t')
            if c[ix['time']] == 'open' or c[ix['year']] != '1913': continue
            cl = c[ix['className']]
            if cl == '兵士': continue
            v = fnum(c[ix['sizeK']]) or 0.0
            a = cs[c[ix['nation']]]; a[1] += v
            if cl == '農民': a[0] += v
    for nat, a in cs.items():
        if a[1] > 0: nf[nat][sd] = 1.0 - a[0] / a[1]
def med(d): return statistics.median(d.values()) if d else float('nan')
print(f'# (1325) ss_gate ins={ins} 種 {sb}..{sb+n-1} 表の欠け={missing} 3-1 の列のある種={seeds_cols}')
print(f'G1 出した E が正の行={offered_rows} 売った E が出した E を超える行={sold_gt} 出した E の和={tot_off:.6g} 売った E の和={tot_sold:.6g} 払った R の和={tot_paid:.6g}')
red = 0
print('H1 κ_E 1913（実際の側の種の中央・影の中央・最小・最大・線）')
for nat, a in KAPPA.items():
    m = med(kap[nat]); ms = med(kapS[nat]); lo = a / 10
    vs = list(kap[nat].values())
    ok = m == m and m >= lo
    print(f'  {nat}\t実際 {m:.6g}\t影 {ms:.6g}\t最小 {min(vs) if vs else float("nan"):.6g}\t最大 {max(vs) if vs else float("nan"):.6g}\t線 ≧{lo:.4g}\t{"内" if ok else "外"}')
print('H2 農業の外の割合 1913（種の中央・最小・最大・線）')
for nat, a in NONFARM.items():
    m = med(nf[nat]); lo, hi = a / 10, min(1.0, a * 10)
    vs = list(nf[nat].values())
    ok = m == m and lo <= m <= hi
    if not ok: red += 1
    print(f'  {nat}\t{m:.4f}\t最小 {min(vs) if vs else float("nan"):.4f}\t最大 {max(vs) if vs else float("nan"):.4f}\t線 {lo:.3f}〜{hi:.3f}\t{"内" if ok else "外"}')
print(f'H2 の外の国={red}')
print(f'判定 G1={"緑" if missing == 0 and sold_gt == 0 and offered_rows > 0 and seeds_cols == n else "赤"} H2={"緑" if red == 0 and missing == 0 else "赤"}')
