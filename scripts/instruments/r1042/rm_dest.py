# (1042) 行き先の国・出身の国・組の上位（xmig_s）と、region_s の越境と入植の列の国ごとの和（3 腕×10 種・和÷和）
import sys, collections
ARMS = {'B': 'D:/PoB_work/keep_1035/base_ins', 'N1': 'D:/PoB_work/keep_1027/n1', 'N2': 'D:/PoB_work/keep_1027/n2'}
to = {a: collections.Counter() for a in ARMS}; fr = {a: collections.Counter() for a in ARMS}; pr = {a: collections.Counter() for a in ARMS}
colin = {a: collections.Counter() for a in ARMS}
tot = collections.Counter(); seeds_to = collections.defaultdict(collections.Counter)
reg = {a: collections.defaultdict(lambda: [0.0, 0.0, 0.0]) for a in ARMS}
for a, d in ARMS.items():
    for s in range(1, 11):
        with open(f'{d}/xmig_s{s}.tsv', 'rb') as f:
            f.readline()
            for line in f:
                x = line.decode('utf-8').rstrip('\r\n').split('\t'); mv = float(x[5])
                if not (mv > 0): continue
                to[a][x[4]] += mv; fr[a][x[3]] += mv; pr[a][(x[3], x[4])] += mv; tot[a] += mv; seeds_to[(a, s)][x[4]] += mv
        with open(f'{d}/region_s{s}.tsv', 'rb') as f:
            h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for line in f:
                x = line.decode('utf-8').rstrip('\r\n').split('\t'); n = x[ix['nation']]
                v = reg[a][n]
                v[0] += float(x[ix['crossBorderInKPerQuarter']]); v[1] += float(x[ix['settlerInKPerQuarter']]) if 'settlerInKPerQuarter' in ix else 0.0; v[2] += float(x[ix['crossBorderOutKPerQuarter']])
print('# 行き先の国の割合（上位 12・B の順）')
for n, v in to['B'].most_common(12):
    ss = [seeds_to[('B', s)][n] / sum(seeds_to[('B', s)].values()) for s in range(1, 11)]
    print(n, ' '.join(f'{a} {to[a][n] / tot[a]:.3f}' for a in ARMS), f'B種 {min(ss):.3f}〜{max(ss):.3f}')
print('# 出身の国の割合（上位 12）')
for n, v in fr['B'].most_common(12): print(n, ' '.join(f'{a} {fr[a][n] / tot[a]:.3f}' for a in ARMS))
print('# 組の割合（上位 15）')
for k, v in pr['B'].most_common(15): print(k, ' '.join(f'{a} {pr[a][k] / tot[a]:.3f}' for a in ARMS))
print('# region_s の国ごとの和（千人・10 種・B）：越境の入り・入植の入り・越境の出（上位 12 を越境の入りで）')
for n, v in sorted(reg['B'].items(), key=lambda t: -t[1][0])[:12]: print(n, [round(z) for z in v])
print('アメリカ', {a: [round(z) for z in reg[a]['アメリカ']] for a in ARMS})
