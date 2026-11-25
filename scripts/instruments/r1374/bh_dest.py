# (1374) 読む線：migration_s で 1 州から出た人の行き先（期間ごと・上位）と入ってきた元。使い方 python bh_dest.py <ins> <種> <州> <年の始め> <年の終わり>
import sys, collections
d, sd, reg, y0, y1 = sys.argv[1], int(sys.argv[2]), sys.argv[3], int(sys.argv[4]), int(sys.argv[5])
q0, q1 = (y0 - 1783) * 4, (y1 - 1783) * 4 + 3
O = collections.Counter(); I = collections.Counter(); OQ = collections.Counter()
with open('%s/migration_s%d.tsv' % (d, sd), encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t')
    for line in fh:
        c = line.rstrip('\n').split('\t'); q = int(c[0])
        if not (q0 <= q <= q1): continue
        v = float(c[3])
        if c[1] == reg: O[c[2]] += v; OQ[q] += v
        if c[2] == reg: I[c[1]] += v
print('出る %.1f 入る %.1f' % (sum(O.values()), sum(I.values())))
print('行き先: ' + '  '.join('%s %.1f' % kv for kv in O.most_common(8)))
print('元: ' + '  '.join('%s %.1f' % kv for kv in I.most_common(5)))
print('出の多い四半期: ' + '  '.join('q%d(%d) %.1f' % (q, 1783 + q // 4, v) for q, v in sorted(OQ.items(), key=lambda kv: -kv[1])[:10]))
