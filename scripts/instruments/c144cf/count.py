# (1366) 記録：cwend_s の endPath の区分ごとの件数・collapseFlag と forced の重なり。使い方：count.py <フォルダ> （cwend_s*.tsv を全て読む）
import sys, glob, collections
d = sys.argv[1]
c = collections.Counter(); ov = collections.Counter(); n = 0; flagcol = None
for f in sorted(glob.glob(d + '/cwend_s*.tsv')):
    L = [x for x in open(f, encoding='utf-8').read().split('\n') if x]
    H = L[0].split('\t')
    for l in L[1:]:
        x = l.split('\t'); n += 1
        p = x[H.index('endPath')]; c[p] += 1
        if 'collapseFlag' in H: ov[(p, x[H.index('collapseFlag')])] += 1
print('行', n, '区分', dict(c))
if ov: print('区分×collapseFlag', dict(ov))
