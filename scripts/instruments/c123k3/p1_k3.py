# (1222) c123k3 予言 P1・P2 の読み手：腕 B の表を基線の生と種ごとに突き合わせ、バイト一致か・最初に違う四半期（年）を出す。
#   使い方: python -X utf8 -I p1_k3.py <腕 B の ins> <基線の ins> [表の頭=nation_s,scalars_s,region_s]
import sys, os
A, B = sys.argv[1], sys.argv[2]
TABS = (sys.argv[3] if len(sys.argv) > 3 else 'nation_s,scalars_s,region_s').split(',')
for s in range(1, 11):
    out = []
    for t in TABS:
        pa, pb = os.path.join(A, f'{t}{s}.tsv'), os.path.join(B, f'{t}{s}.tsv')
        if not (os.path.exists(pa) and os.path.exists(pb)): out.append(f'{t} 欠け'); continue
        a = open(pa, 'rb').read().split(b'\n'); b = open(pb, 'rb').read().split(b'\n')
        if a == b: out.append(f'{t} 一致'); continue
        i = next((k for k in range(min(len(a), len(b))) if a[k] != b[k]), min(len(a), len(b)))
        row = (a[i] if i < len(a) else b[i]).split(b'\t')
        out.append(f'{t} 違う（最初の行 {i}・四半期 {row[0].decode()}・年 {row[1].decode() if len(row) > 1 else "?"}）')
    print(f'種 {s}：' + '・'.join(out))
