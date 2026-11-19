# (1244) c126pf 計器 capskip_s の要約（読むだけ）：種ごとの飛ばした回数・値段で足さなかった回数・申し出の重さ・影の量・足した量・足した÷影（同じ盤面の新÷旧の材料）、国の上位、年代ごと。
# 使い方: python -X utf8 -I capskip_sum.py <ins> <種の始め> <種の終わり>
import sys, os, collections
INS, S0, S1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
tot = collections.Counter(); nat = collections.Counter(); dec = collections.Counter(); jam = collections.Counter()
print('seed\tskips\tpricedOut\tofferW\tshadowW\taddedW\tadded/shadow')
for s in range(S0, S1 + 1):
    p = os.path.join(INS, f'capskip_s{s}.tsv')
    if not os.path.exists(p): print(f'{s}\t(無い)'); continue
    c = collections.Counter()
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            r = line.rstrip('\n').split('\t')
            for k in ('skips', 'skipsPricedOut', 'offerWeight', 'shadowWeight', 'addedWeight'): c[k] += fl(r[ix[k]])
            nat[r[ix['nation']]] += fl(r[ix['addedWeight']]); dec[int(r[ix['year']]) // 10 * 10] += fl(r[ix['addedWeight']])
            if r[ix['good']] == 'コーヒー': jam[r[ix['nation']]] += fl(r[ix['shadowWeight']])
    tot.update(c)
    rt = c['addedWeight'] / c['shadowWeight'] if c['shadowWeight'] > 0 else float('nan')
    print(f"{s}\t{c['skips']:.0f}\t{c['skipsPricedOut']:.0f}\t{c['offerWeight']:.6g}\t{c['shadowWeight']:.6g}\t{c['addedWeight']:.6g}\t{rt:.4f}")
rt = tot['addedWeight'] / tot['shadowWeight'] if tot['shadowWeight'] > 0 else float('nan')
print(f"和\t{tot['skips']:.0f}\t{tot['skipsPricedOut']:.0f}\t{tot['offerWeight']:.6g}\t{tot['shadowWeight']:.6g}\t{tot['addedWeight']:.6g}\t{rt:.4f}")
print('足した量の国の上位: ' + ', '.join(f'{k} {v:.4g}' for k, v in nat.most_common(8)))
print('足した量の年代: ' + ', '.join(f'{k} {v:.4g}' for k, v in sorted(dec.items())))
print('コーヒーの影の量の国の上位: ' + ', '.join(f'{k} {v:.4g}' for k, v in jam.most_common(5)))
