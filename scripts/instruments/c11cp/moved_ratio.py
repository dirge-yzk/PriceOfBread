# (800) c11cp 実際に移った賠償 repMovedIndex ÷ 期待 repNewIndex の分布（N>=2 の事象）。会議の削り（<1）と条約港の倍率（>1）が掛かる事象は 1 から外れる＝値の山（ratio を 4 桁に丸めた最頻値）と、1 のままの事象の割合を出す。
#   使い方: python -I moved_ratio.py <ins フォルダ>
import sys, os, glob, csv, collections
d = sys.argv[1]
ev = []
for f in sorted(glob.glob(os.path.join(d, 'cpeace_s*.tsv'))):
    for r in csv.DictReader(open(f, encoding='utf-8'), delimiter='\t'):
        if int(r['nCollapse']) >= 2 and float(r['repNewIndex']) > 0: ev.append(r)
q = [float(e['repMovedIndex']) / float(e['repNewIndex']) for e in ev]
c = collections.Counter(round(x, 4) for x in q)
print(f'# {d}・N>=2 の事象 {len(q)}')
print('比の最頻値（丸め 4 桁）:', c.most_common(8))
one = sum(1 for x in q if abs(x - 1) < 1e-4)
print(f'比が 1（±1e-4）の事象 {one}（{one / max(1, len(q)):.3f}）・1 未満 {sum(1 for x in q if x < 1 - 1e-4)}・1 超 {sum(1 for x in q if x > 1 + 1e-4)}・最小 {min(q):.4f}・最大 {max(q):.4f}')
