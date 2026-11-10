# (1015) c94rh 州の持ち主の年表：挙げた州について、種ごとに最初の持ち主から変わった最初の年と、その後の持ち主の列（年末の四半期）。
# 使い方: python -X utf8 -I rh_owner.py <生のフォルダ> <種の範囲 1-10> <州,州,...> <出力のファイル>
import sys, os
RAW, RNG, REGS, OUT = sys.argv[1:5]
lo, hi = map(int, RNG.split('-')); REGS = REGS.split(',')
o = open(OUT, 'w', encoding='utf-8', newline='\n')
o.write(f'# rh_owner raw={RAW} seeds={RNG}（q%4==3・activeFlag 1）\nseed\tregion\towner1784\tfirstChangeYear\tsequence(年:持ち主)\n')
for s in range(lo, hi + 1):
    p = os.path.join(RAW, f'region_s{s}.tsv')
    if not os.path.exists(p): continue
    seq = {r: [] for r in REGS}
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for line in f:
            r = line.rstrip('\n').split('\t')
            reg = r[ix['region']]
            if reg not in seq or int(r[ix['quarter']]) % 4 != 3 or r[ix['activeFlag']] != '1': continue
            y, n = int(r[ix['year']]), r[ix['nation']]
            if not seq[reg] or seq[reg][-1][1] != n: seq[reg].append((y, n))
    for reg in REGS:
        sq = seq[reg]
        if not sq: o.write(f'{s}\t{reg}\t-\t-\t-\n'); continue
        fc = sq[1][0] if len(sq) > 1 else '-'
        o.write(f'{s}\t{reg}\t{sq[0][1]}\t{fc}\t' + ' '.join(f'{y}:{n}' for y, n in sq) + '\n')
o.close(); print('ok', OUT)
