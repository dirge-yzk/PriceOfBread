# (1063) c101py 形 Y の事前登録の材料（読むだけ）：G1 の母集合を今の盤面（keep_1053/base_ins）で数え直す。
#   G1 の母集合＝国家の調達（Procure の buyer==null）が買えた（buy>0）国×四半期。
#   今の盤面に計器 procx_s は無い＝代わりに flow_s の Colonial.cs:199（BuyUpkeepGood→Procure の呼び手）の giveIncome の額＞0 の国×四半期を数える
#   （代金＝buy×値段＞0 ⇔ buy＞0。海軍の燃料の道 Military.cs:321 は NavalFuelPerGauge＝0 で走らない）。
#   使い方：python g1pop.py <計器の出力フォルダ>
import os, sys
from collections import defaultdict

d = sys.argv[1]
tot_rows = 0; nations_all = set(); per_seed = []
by_nation = defaultdict(int)
for s in range(1, 21):
    p = os.path.join(d, 'flow_s%d.tsv' % s)
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8') as f:
        hdr = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hdr)}
        nq = defaultdict(float)
        for line in f:
            v = line.rstrip('\n').split('\t')
            if v[ix['sourceFile']] != 'Colonial.cs' or v[ix['sourceLine']] != '199' or v[ix['kind']] != 'giveIncome': continue
            nq[(v[ix['nation']], v[ix['quarter']])] += float(v[ix['amountPerMonth']])
    pos = [k for k, a in nq.items() if a > 0]
    for (n, q) in pos: by_nation[n] += 1; nations_all.add(n)
    per_seed.append((s, len(pos), len({n for n, _ in pos})))
    tot_rows += len(pos)
print('G1 の母集合（国×四半期・国家の調達の代金＞0）＝flow_s Colonial.cs:199 giveIncome')
for s, k, nn in per_seed: print('  種 %d：%d 行・%d 国' % (s, k, nn))
print('  合計 %d 行・種 %d 本・国 %d' % (tot_rows, len(per_seed), len(nations_all)))
print('  国ごと（上位 12）：' + '・'.join('%s %d' % (n, c) for n, c in sorted(by_nation.items(), key=lambda x: -x[1])[:12]))
