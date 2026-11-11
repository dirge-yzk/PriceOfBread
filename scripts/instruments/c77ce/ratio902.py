# (902) c77ce 記録：同じ盤面の新÷旧（計器の 9 国の admrule_s の国×四半期の平均・種の 10 本の和で）
#   python -X utf8 -I ratio902.py <新の ins> <旧の ins> [<ヌル腕の ins>...]
import sys, os, glob
COLS = ['ruleStateShare', 'ruleLocalShare', 'deliveryShare', 'stateReachShare', 'adminFillShare']
def mean(d):
    s = {c: 0.0 for c in COLS}; n = 0
    for p in glob.glob(os.path.join(d, 'admrule_s*.tsv')):
        L = open(p, 'rb').read().decode('utf-8').split('\n'); h = L[0].rstrip('\r').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for l in L[1:]:
            x = l.rstrip('\r').split('\t')
            if len(x) < len(h): continue
            v = [float(x[ix[c]]) for c in COLS]
            if any(t != t for t in v): continue
            for c, t in zip(COLS, v): s[c] += t
            n += 1
    return {c: s[c] / n for c in COLS}, n
new, nn = mean(sys.argv[1]); old, no = mean(sys.argv[2])
print('# admrule_s（計器の国×四半期の平均）新 %s（%d 行）÷旧 %s（%d 行）' % (sys.argv[1], nn, sys.argv[2], no))
for c in COLS: print('%s\t新 %.5f\t旧 %.5f\t新÷旧 %.5f' % (c, new[c], old[c], new[c] / old[c]))
for d in sys.argv[3:]:
    m, k = mean(d)
    print('ヌル腕 %s\t' % d + '\t'.join('%s÷旧 %.5f' % (c, m[c] / old[c]) for c in COLS))
