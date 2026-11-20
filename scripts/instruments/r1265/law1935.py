# (1265) 記録：ある四半期に生きている国（mintcap_s に行がある国）の本位の法（0 不換・1 銀・2 金）と onGold を種ごとに数える。
# 使い方: python -X utf8 -I law1935.py <生> <種0> <種1> <四半期>
import sys, csv, collections
raw, s0, s1, q = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
tot = collections.Counter(); names = collections.Counter()
for s in range(s0, s1 + 1):
    for r in csv.DictReader(open(f'{raw}/mintcap_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        if r['q'] != q: continue
        tot[(r['law'], r['onGold'])] += 1; names[r['nation']] += 1
print(f'# {raw} 種 {s0}〜{s1} 四半期 {q}：国×種 {sum(tot.values())}（種あたり {sum(tot.values())/(s1-s0+1):.1f}）・(法,onGold) {dict(sorted(tot.items()))}')
print('# 国（出た種の数）', ' '.join(f'{k}{v}' for k, v in names.most_common()))
