# (778) c7hl の静的 2 検査（反証役の提案・記録）：新しく陸になる画素が 60.1N より南（陸路が変わる対の側は c7hl の線と反証役が数えた）
# python static776.py <旧 sea.bin> <新 sea.bin> <旧 land.bin> <新 land.bin>
import sys
W, H = 3600, 1800
def bits(p):
    b = open(p, 'rb').read(); return b
def diff(a, b, name):
    A, B = bits(a), bits(b); ch = []
    for i in range(len(A)):
        x = A[i] ^ B[i]
        if not x: continue
        for k in range(8):
            if x >> k & 1:
                idx = i * 8 + k; row, col = divmod(idx, W)
                lat = 90 - (row + 0.5) * 0.1; lon = -180 + (col + 0.5) * 0.1
                ch.append((lat, lon, (B[i] >> k) & 1))
    toland = sum(1 for c in ch if c[2] == 1); tosea = len(ch) - toland
    south = sum(1 for c in ch if c[2] == 1 and c[0] < 60.1)
    print(f'{name}: 変わった画素 {len(ch)}（陸へ {toland}・海へ {tosea}）・60.1N より南の新しい陸 {south}・緯度 {min(c[0] for c in ch):.2f}〜{max(c[0] for c in ch):.2f}・経度 {min(c[1] for c in ch):.2f}〜{max(c[1] for c in ch):.2f}')
diff(sys.argv[1], sys.argv[2], '海路用 landmask.bin')
diff(sys.argv[3], sys.argv[4], '陸路用 landmask_land.bin')
# 対の側（ヘルシンキを含まない陸路の変化 0）は c7hl の線（有向 360 対＝全てヘルシンキ）と反証役の数え直しで確かめ済み＝ここでは数えない（m776_20261102_paper_hel_pairs.tsv はヘルシンキを出発点とする対だけの表で、この検査の母集合にならない）。
