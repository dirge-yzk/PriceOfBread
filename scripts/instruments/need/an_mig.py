"""(158) 国内の移民の距離と上限の読み手。python an_mig.py <mig のディレクトリ>..."""
import sys, glob, collections, os
for d in sys.argv[1:]:
    T = collections.Counter(); top = collections.Counter(); km = {}
    for f in sorted(glob.glob(os.path.join(d, 'm*.txt'))):
        for line in open(f, encoding='utf-8'):
            t = line.rstrip('\n').split('\t')
            if t[0] == 'N':
                T['pl'] += float(t[4]); T['mv'] += float(t[5]); T['mk'] += float(t[6]); T['md'] += float(t[7]); T['m1'] += float(t[8]); T['m2'] += float(t[9])
            elif t[0] == 'P':
                k = (f[-6:-4].strip('m'), t[2], t[3], t[4]); top[k] += float(t[7]); km[k] = float(t[5])
    print(f'## {d}: 計画 {T["pl"]:.0f} 移した {T["mv"]:.0f}（上限で縮んだ割合 {100*(1-T["mv"]/T["pl"]):.1f}%）・平均距離 {T["mk"]/T["mv"]:.0f}km・平均の減衰 {T["md"]/T["mv"]:.3f}・1000km超 {100*T["m1"]/T["mv"]:.1f}%・2000km超 {100*T["m2"]/T["mv"]:.1f}%')
    lg = [(v, k) for k, v in top.items() if km[k] > 2000]; lg.sort(reverse=True)
    for v, k in lg[:8]: print(f'   s{k[0]} {k[1]} {k[2]}→{k[3]} {km[k]:.0f}km 移した {v:.0f}')
