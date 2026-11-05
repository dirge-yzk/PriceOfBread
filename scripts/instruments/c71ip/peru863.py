# (863) c71ip 記録の台本：ペルーの型（州のお金が 4 四半期以上続けて 1e-6 未満で、人口 10 万〔popK 100〕以上の州×四半期）
#   python -I peru863.py <ins フォルダ> [種 1 10]   cearn_s の savings（四半期の末の州の POP の在庫の和）と popK を読む。州は名前で続ける（持ち主の国が替わっても同じ州）。
#   作者〈Sen 型の現金不足として消さずに見張る〉の読み直し＝記録（止める線ではない）。
import sys, os
from collections import defaultdict
d = sys.argv[1]; s0 = int(sys.argv[2]) if len(sys.argv) > 2 else 1; s1 = int(sys.argv[3]) if len(sys.argv) > 3 else 10
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
tot = 0; regs = 0; colTot = 0; out = []
for s in range(s0, s1 + 1):
    p = os.path.join(d, f'cearn_s{s}.tsv')
    if not os.path.exists(p): out.append(f'# 種 {s} 表なし'); continue
    rows = defaultdict(list)
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); iq, ireg, it, ipk, isv = h.index('quarter'), h.index('region'), h.index('tier'), h.index('popK'), h.index('savings')
        for line in f:
            x = line.rstrip('\n').split('\t')
            rows[x[ireg]].append((int(x[iq]), fl(x[ipk]), fl(x[isv]), x[it]))
    n = 0; nr = 0; nc = 0
    for reg, v in rows.items():
        v.sort(); hit = False
        seq = []
        for q, pk, sv, t in v:
            ok = (sv < 1e-6) and (pk >= 100)
            if ok and seq and q == seq[-1][0] + 1: seq.append((q, t))
            elif ok:
                if len(seq) >= 4: n += len(seq); nc += sum(1 for _, tt in seq if tt == 'Colonial'); hit = True
                seq = [(q, t)]
            else:
                if len(seq) >= 4: n += len(seq); nc += sum(1 for _, tt in seq if tt == 'Colonial'); hit = True
                seq = []
        if len(seq) >= 4: n += len(seq); nc += sum(1 for _, tt in seq if tt == 'Colonial'); hit = True
        if hit: nr += 1; out.append(f'州\t種 {s}\t{reg}')
    out.append(f'種\t{s}\t州×四半期 {n}（植民地格 {nc}）\t州 {nr}')
    tot += n; regs += nr; colTot += nc
out.append(f'計\t州×四半期 {tot}（植民地格 {colTot}）\t州（種ごとの和） {regs}')
print('\n'.join(out))
