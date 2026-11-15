# (1156) keep_1150/base_ins の tb_s を 10 種・全期間で足す（読むだけ）
import sys, os
OUT = sys.argv[1]
D = 'D:/PoB_work/keep_1150/base_ins'
COLS = ['rows', 'unclassified', 'sk0', 'sk1', 'sk2', 'ps0', 'ps1', 'ps2', 'a1Close', 'smallD', 'smallDSkip', 'evalSkip', 'evalAll', 'evalDone', 'u1Deals', 'u1Rel']
tot = {k: 0.0 for k in COLS}; per = {}
for s in range(1, 11):
    with open(os.path.join(D, f'tb_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        ps = {k: 0.0 for k in COLS}
        for line in f:
            c = line.rstrip('\n').split('\t')
            for k in COLS:
                v = float(c[ix[k]]); tot[k] += v; ps[k] += v
        per[s] = ps
L = ['# keep_1150/base_ins tb_s の 10 種・全期間の和（旗 0・d05c6bea9）']
L.append('\t'.join(COLS)); L.append('\t'.join(f'{tot[k]:.0f}' for k in COLS))
L.append('# 種ごと sk0 sk1 sk2 evalSkip')
for s, ps in per.items(): L.append(f's{s}\t{ps["sk0"]:.0f}\t{ps["sk1"]:.0f}\t{ps["sk2"]:.0f}\t{ps["evalSkip"]:.0f}')
L.append(f'# sk0+sk1（旧の門が飛ばし A-1 の上界が正）＝{tot["sk0"]+tot["sk1"]:.0f}・その 1/4＝{(tot["sk0"]+tot["sk1"])/4:.0f}・sk1 の 1/4＝{tot["sk1"]/4:.0f}')
with open(os.path.join(OUT, 'tbsum1150.txt'), 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
print('\n'.join(L[:3]))
