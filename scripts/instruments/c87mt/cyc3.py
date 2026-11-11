# 紙の台本：pclamp_s の kind=floor で cyclicalRatio＜1e-3 の行を全ての国で数える（(D) の型の痕＝初期化を GDP 0 で置いた国）
import sys, os
d, a0, a1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
tot = 0; nf = 0
for s in range(a0, a1 + 1):
    p = os.path.join(d, f'pclamp_s{s}.tsv')
    if not os.path.exists(p): continue
    nf += 1
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for l in f:
            x = l.rstrip('\n').split('\t')
            if x[ix['kind']] == 'floor' and float(x[ix['cyclicalRatio']]) < 1e-3: tot += 1
print(d, 'pclamp の表', nf, 'cyc<1e-3 の行', tot, sep='\t')
