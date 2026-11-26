# (1381) 読む線：1 州の prodcap_s を財×レシピ×年で集計（工場＝isRgo 0）。使い方 python q1_prodcap.py <prodcap_州の抜き出し(見出しなし)> <見出しの元> <出力>
import sys, collections
src, hdr, out = sys.argv[1], sys.argv[2], sys.argv[3]
h = open(hdr, encoding='utf-8').readline().rstrip('\n').split('\t')
I = {k: i for i, k in enumerate(h)}
agg = collections.defaultdict(lambda: [0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0])
for line in open(src, encoding='utf-8'):
    c = line.rstrip('\n').split('\t')
    key = (c[I['year']], c[I['q']], c[I['isRgo']], c[I['good']], c[I['recipe']], c[I['class']])
    a = agg[key]; a[0] += 1
    f = lambda s: float(s) if s not in ('', 'NaN') else 0.0
    cap, outp = float(c[I['capacity']]), float(c[I['output']])
    a[1] += cap; a[2] += outp
    a[3] += f(c[I['lastInputFill']]) * cap; a[4] += f(c[I['profitFactor']]) * cap; a[5] += f(c[I['marginalCost']]) * cap; a[6] += f(c[I['scale']])
with open(out, 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('year\tq\tisRgo\tgood\trecipe\tclass\tn\tcapacity\toutput\tinputFillW\tprofitFactorW\tmcW\tscaleSum\n')
    for k in sorted(agg):
        a = agg[k]; w = a[1] if a[1] > 0 else 1
        fo.write('\t'.join(k) + '\t%d\t%.2f\t%.2f\t%.3f\t%.3f\t%.3f\t%.2f\n' % (a[0], a[1], a[2], a[3]/w, a[4]/w, a[5]/w, a[6]))
