# (1277) c131pf（(1244) c126pf の写し・列 shadowAllWeight・dedupWeight を足した）計器 capskip_s の要約（読むだけ）：種ごとの飛ばした回数・値段で足さなかった回数・申し出の重さ・影の量・足した量・差し引き・足した÷影（同じ盤面の新÷旧の材料）、国の上位、年代ごと。
# 使い方: python -X utf8 -I capskip_sum.py <ins> <種の始め> <種の終わり>
import sys, os, collections
INS, S0, S1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
K = ('skips', 'skipsPricedOut', 'offerWeight', 'shadowAllWeight', 'shadowWeight', 'addedWeight', 'dedupWeight')
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
tot = collections.Counter(); nat = collections.Counter(); dec = collections.Counter(); jam = collections.Counter()
print('seed\t' + '\t'.join(K) + '\tadded/shadow\tdedup/added')
def line(nm, c):
    rt = c['addedWeight'] / c['shadowWeight'] if c['shadowWeight'] > 0 else float('nan')
    rd = c['dedupWeight'] / c['addedWeight'] if c['addedWeight'] > 0 else float('nan')
    print(f'{nm}\t' + '\t'.join(f'{c[k]:.6g}' for k in K) + f'\t{rt:.4f}\t{rd:.4f}')
for s in range(S0, S1 + 1):
    p = os.path.join(INS, f'capskip_s{s}.tsv')
    if not os.path.exists(p): print(f'{s}\t(無い)'); continue
    c = collections.Counter()
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        assert all(k in ix for k in K), f'列が足りない {p}'
        for ln in f:
            r = ln.rstrip('\n').split('\t')
            assert len(r) == len(h), f'列の数が見出しと違う {p}'
            for k in K: c[k] += fl(r[ix[k]])
            nat[r[ix['nation']]] += fl(r[ix['addedWeight']]); dec[int(r[ix['year']]) // 10 * 10] += fl(r[ix['addedWeight']])
            if r[ix['good']] == 'コーヒー': jam[r[ix['nation']]] += fl(r[ix['shadowWeight']])
    tot.update(c)
    line(str(s), c)
line('和', tot)
print('足した量の国の上位: ' + ', '.join(f'{k} {v:.4g}' for k, v in nat.most_common(8)))
print('足した量の年代: ' + ', '.join(f'{k} {v:.4g}' for k, v in sorted(dec.items())))
print('コーヒーの影の量の国の上位: ' + ', '.join(f'{k} {v:.4g}' for k, v in jam.most_common(5)))
