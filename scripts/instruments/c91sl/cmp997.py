# (997) c91sl 生の比べの内訳（読むだけ）：rawcmp.py が〈違う〉とした表（cearn_s・flow_s・landsale_s）について、違う行の数と違う列の名を数える。
#   予言 P2（事前登録 m997 [3]）：landsale_s は写しの列（saleCopyPaidPerQuarter・salePaidCopyMaxRelDiff）だけ・cearn と flow は行番号の列だけ（★(988)）。
# 使い方: python cmp997.py <腕の ins> <比べる側の ins> <種の起点> [種の数=10]
import sys, os, collections
A, B, s0 = sys.argv[1], sys.argv[2], int(sys.argv[3]); ns = int(sys.argv[4]) if len(sys.argv) > 4 else 10
for t in ('landsale_s', 'cearn_s', 'flow_s'):
    cols = collections.Counter(); nrow = 0; nlen = 0; ndiffrows = 0
    for s in range(s0, s0 + ns):
        if not os.path.exists(os.path.join(A, f'{t}{s}.tsv')): continue   # S11 は POB_CEARN=0＝cearn_s が無い
        la = open(os.path.join(A, f'{t}{s}.tsv'), 'rb').read().split(b'\n'); lb = open(os.path.join(B, f'{t}{s}.tsv'), 'rb').read().split(b'\n')
        h = la[0].decode('utf-8').rstrip('\r').split('\t')
        if len(la) != len(lb): nlen += 1
        for x, y in zip(la[1:], lb[1:]):
            nrow += 1
            if x == y: continue
            ndiffrows += 1
            xa, yb = x.split(b'\t'), y.split(b'\t')
            if len(xa) != len(yb): cols['(列の数が違う)'] += 1; continue
            for k, (u, v) in enumerate(zip(xa, yb)):
                if u != v: cols[h[k] if k < len(h) else f'#{k}'] += 1
    print(f'{t}: 行 {nrow}・違う行 {ndiffrows}・行の数が違う種 {nlen}・違う列 {dict(cols.most_common(8))}')
