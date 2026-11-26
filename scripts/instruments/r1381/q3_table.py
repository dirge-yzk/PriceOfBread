# (1381) 読む線：q3_all.sh の出力を腕×種×州で 1 表にする（吸い込みの割合・年ごと）。使い方 python q3_table.py <q3 フォルダ> <出力>
import sys, os, re, collections
d, out = sys.argv[1], sys.argv[2]; Y = (1830, 1840, 1850, 1860, 1870, 1880, 1900)
rows = []
for fn in sorted(os.listdir(d)):
    m = re.match(r'sink_(M|N1_|N2_|S11_)(\d+)\.tsv$', fn)
    if not m: continue
    arm = m.group(1).rstrip('_'); sd = int(m.group(2)); v = collections.defaultdict(dict)
    for l in open(os.path.join(d, fn), encoding='utf-8').readlines()[1:]:
        c = l.rstrip('\n').split('\t'); v[c[1]][int(c[0])] = (float(c[4]), c[5].split(',')[0])
    for r in v: rows.append((arm, sd, r, v[r]))
with open(out, 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('arm\tseed\tregion\t' + '\t'.join(str(y) for y in Y) + '\ttop1850\n')
    for arm, sd, r, v in sorted(rows, key=lambda t: (t[2], t[0], t[1])):
        fo.write('%s\t%d\t%s\t' % (arm, sd, r) + '\t'.join('%.2f' % v[y][0] if y in v else '' for y in Y) + '\t%s\n' % (v.get(1850, (0, ''))[1]))
